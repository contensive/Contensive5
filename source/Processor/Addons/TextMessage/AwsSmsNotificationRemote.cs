using Contensive.BaseClasses;
using Contensive.Processor.Controllers;
using Contensive.Processor.Extensions;
using Contensive.Processor.Models.Domain;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using static Contensive.Processor.Constants;
//
namespace Contensive.Processor.Addons.TextMessage {
    //
    //====================================================================================================
    /// <summary>
    /// Remote method sms/aws/sns. AWS SNS posts AWS End User Messaging SMS delivery events and inbound texts here.
    /// Runs anonymous and never logs a user in. Order: size, allow-list configured, parse, topic allow-list, timestamp age, signature, idempotency, then process.
    /// Responses: 400 bad request, 403 not configured / topic not allowed / stale / bad signature, 200 ok or duplicate, 500 certificate unavailable or unexpected error (SNS retries).
    /// </summary>
    public class AwsSmsNotificationRemote : AddonBaseClass {
        //
        /// <summary>SNS messages are at most 256 KB</summary>
        public const int maxBodyLength = 300000;
        /// <summary>messages older than this are rejected as replays</summary>
        public static readonly TimeSpan maxMessageAge = TimeSpan.FromHours(1);
        /// <summary>allowed clock skew for a Timestamp in the future</summary>
        public static readonly TimeSpan maxFutureSkew = TimeSpan.FromMinutes(5);
        //
        private const string lastNotificationCacheKey = "sms-sns-lastnotification";
        //
        private static readonly HttpClient subscribeClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(10) };
        //
        //====================================================================================================
        //
        public override object Execute(CPBaseClass cp) {
            try {
                CoreController core = ((CPClass)cp).core;
                cp.Response.SetType("text/plain");
                cp.Response.AddHeader("Cache-Control", "no-store");
                //
                // -- 1. size
                string body = cp.Request.Body;
                if (string.IsNullOrEmpty(body) || body.Length > maxBodyLength) {
                    cp.Response.SetStatus(WebServerController.httpResponseStatus.BadRequest);
                    return "bad request";
                }
                //
                // -- 2. inert until the allow-list is set
                string allowList = cp.Site.GetText(sitePropertySmsAwsSnsTopicArns, "");
                if (string.IsNullOrWhiteSpace(allowList)) {
                    cp.Response.SetStatus(WebServerController.httpResponseStatus.Forbidden);
                    return "forbidden";
                }
                //
                // -- 3. parse
                SnsEnvelopeModel envelope;
                try {
                    envelope = JsonConvert.DeserializeObject<SnsEnvelopeModel>(body);
                } catch (JsonException) {
                    envelope = null;
                }
                if (envelope == null || string.IsNullOrEmpty(envelope.Type) || string.IsNullOrEmpty(envelope.MessageId)) {
                    cp.Response.SetStatus(WebServerController.httpResponseStatus.BadRequest);
                    return "bad request";
                }
                //
                // -- 4. topic allow-list, before any other work
                if (!SmsNotificationController.isTopicAllowed(envelope.TopicArn, allowList)) {
                    warnOncePerMinute(cp, core, "sms-sns-badtopic", "SMS SNS endpoint rejected a message from a topic that is not in the AWS SMS SNS Topic ARNs setting.");
                    cp.Response.SetStatus(WebServerController.httpResponseStatus.Forbidden);
                    return "forbidden";
                }
                //
                // -- 5a. replay guard, before the signature so a stale replay causes no certificate fetch (the signature covers the Timestamp)
                if (!SnsMessageVerifier.isTimestampFresh(envelope.Timestamp, DateTime.UtcNow, maxMessageAge, maxFutureSkew)) {
                    warnOncePerMinute(cp, core, "sms-sns-stale", "SMS SNS endpoint rejected a message with a Timestamp older than 1 hour or in the future.");
                    cp.Response.SetStatus(WebServerController.httpResponseStatus.Forbidden);
                    return "forbidden";
                }
                //
                // -- 5b. signature
                switch (SnsMessageVerifier.verifyDetailed(envelope)) {
                    case SnsMessageVerifier.VerifyResultEnum.Valid:
                        break;
                    case SnsMessageVerifier.VerifyResultEnum.CertificateUnavailable:
                        //
                        // -- not a bad message, the certificate could not be loaded now. 500 so SNS retries
                        warnOncePerMinute(cp, core, "sms-sns-nocert", "SMS SNS endpoint could not load the SNS signing certificate, message not processed.");
                        cp.Response.SetStatus(WebServerController.httpResponseStatus.InternalServerError);
                        return "error";
                    default:
                        warnOncePerMinute(cp, core, "sms-sns-badsig", "SMS SNS endpoint rejected a message with an invalid signature.");
                        cp.Response.SetStatus(WebServerController.httpResponseStatus.Forbidden);
                        return "forbidden";
                }
                //
                // -- 6. idempotency
                string idempotencyKey = $"sms-sns-msg/{envelope.MessageId}";
                if (!string.IsNullOrEmpty(cp.Cache.GetText(idempotencyKey))) {
                    return "duplicate";
                }
                //
                // -- 7. by type
                string summary;
                switch (envelope.Type) {
                    case "SubscriptionConfirmation": {
                            if (!SnsMessageVerifier.isValidSnsUrl(envelope.SubscribeURL, envelope.TopicArn, false)) {
                                cp.Response.SetStatus(WebServerController.httpResponseStatus.Forbidden);
                                return "forbidden";
                            }
                            using (HttpResponseMessage response = subscribeClient.GetAsync(envelope.SubscribeURL).waitSynchronously()) {
                                if (!response.IsSuccessStatusCode) {
                                    logger.Warn($"{core.logCommonMessage},SMS SNS subscription confirmation failed, status [{(int)response.StatusCode}], topic [{envelope.TopicArn}]");
                                    cp.Response.SetStatus(WebServerController.httpResponseStatus.InternalServerError);
                                    return "error";
                                }
                            }
                            logger.Info($"{core.logCommonMessage},SMS SNS subscription confirmed, topic [{envelope.TopicArn}]");
                            summary = $"SubscriptionConfirmation {envelope.TopicArn}";
                            break;
                        }
                    case "UnsubscribeConfirmation": {
                            logger.Warn($"{core.logCommonMessage},SMS SNS subscription was unsubscribed, topic [{envelope.TopicArn}]");
                            summary = $"UnsubscribeConfirmation {envelope.TopicArn}";
                            break;
                        }
                    case "Notification": {
                            SmsNotificationController.SmsNotificationResult result = SmsNotificationController.process(core, envelope.Message);
                            logger.Info($"{core.logCommonMessage},SMS SNS notification, message [{envelope.MessageId}], {result.action}, log [{result.textMessageLogId}]");
                            summary = $"Notification {result.action}";
                            break;
                        }
                    default: {
                            logger.Info($"{core.logCommonMessage},SMS SNS message type [{envelope.Type}] ignored");
                            summary = $"ignored type {envelope.Type}";
                            break;
                        }
                }
                cp.Cache.Store(idempotencyKey, "1", DateTime.Now.AddHours(48));
                //
                // -- 8. diagnostic. Written at most once a minute (a busy site gets several events per text), always for a subscription confirmation
                bool isSubscription = envelope.Type.Equals("SubscriptionConfirmation", StringComparison.Ordinal);
                if (isSubscription || string.IsNullOrEmpty(cp.Cache.GetText(lastNotificationCacheKey))) {
                    cp.Cache.Store(lastNotificationCacheKey, "1", DateTime.Now.AddSeconds(60));
                    cp.Site.SetProperty(sitePropertySmsLastNotificationReceived, truncate($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {summary}", 255));
                }
                return "ok";
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                cp.Response.SetStatus(WebServerController.httpResponseStatus.InternalServerError);
                return "error";
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// log a warning at most once a minute per key, so a flood of bad posts does not flood the log
        /// </summary>
        private static void warnOncePerMinute(CPBaseClass cp, CoreController core, string cacheKey, string message) {
            if (!string.IsNullOrEmpty(cp.Cache.GetText(cacheKey))) { return; }
            cp.Cache.Store(cacheKey, "1", DateTime.Now.AddSeconds(60));
            logger.Warn($"{core.logCommonMessage},{message} Remote IP [{cp.Request.RemoteIP}]");
        }
        //
        private static string truncate(string text, int maxLength) {
            if (string.IsNullOrEmpty(text)) { return ""; }
            return (text.Length <= maxLength) ? text : text.Substring(0, maxLength);
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
