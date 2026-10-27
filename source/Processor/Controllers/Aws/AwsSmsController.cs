
using Amazon.PinpointSMSVoiceV2;
using Amazon.PinpointSMSVoiceV2.Model;
using Contensive.Models.Db;
using Contensive.Processor.Controllers.Aws;
using Contensive.Processor.Extensions;
using Contensive.Processor.Models.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// Send SMS text messages with AWS End User Messaging SMS (SDK AWSSDK.PinpointSMSVoiceV2, SendTextMessage).
    /// Each send returns a MessageId, uses the site's origination identity and configuration set.
    /// </summary>
    public class AwsSmsController {
        //
        //====================================================================================================
        /// <summary>
        /// the site settings used to build a send request
        /// </summary>
        public class AwsSmsSettings {
            /// <summary>
            /// phone number id/arn, E.164 number, pool id/arn or sender id. Required.
            /// </summary>
            public string originationIdentity { get; set; } = "";
            /// <summary>
            /// configuration set name. blank = omitted, texts are sent without events
            /// </summary>
            public string configurationSet { get; set; } = "";
            /// <summary>
            /// max price in USD per message part. blank = omitted
            /// </summary>
            public string maxPrice { get; set; } = "";
            /// <summary>
            /// aws region system name, like us-east-1. blank = the server's region
            /// </summary>
            public string region { get; set; } = "";
        }
        //
        //====================================================================================================
        /// <summary>
        /// read the aws sms settings from the site properties
        /// </summary>
        /// <param name="core"></param>
        /// <returns></returns>
        public static AwsSmsSettings getSettings(CoreController core) {
            var cp = core.cpParent;
            return new AwsSmsSettings {
                originationIdentity = (cp.Site.GetText(Constants.sitePropertySmsAwsOriginationIdentity, "") ?? "").Trim(),
                configurationSet = (cp.Site.GetText(Constants.sitePropertySmsAwsConfigurationSet, "") ?? "").Trim(),
                maxPrice = (cp.Site.GetText(Constants.sitePropertySmsAwsMaxPrice, "") ?? "").Trim(),
                region = (cp.Site.GetText(Constants.sitePropertySmsAwsRegion, "") ?? "").Trim()
            };
        }
        //
        //====================================================================================================
        /// <summary>
        /// Build the SendTextMessage request. Pure function, no AWS calls.
        /// </summary>
        /// <param name="e164">destination phone, E.164</param>
        /// <param name="body"></param>
        /// <param name="settings"></param>
        /// <param name="appName">returned in events as Context["app"] so a site can ignore events that are not its own</param>
        /// <param name="logGuid">the ccguid of the Text Message Log row, returned in events as Context["log"]</param>
        /// <returns></returns>
        public static SendTextMessageRequest buildRequest(string e164, string body, AwsSmsSettings settings, string appName, string logGuid) {
            var request = new SendTextMessageRequest {
                DestinationPhoneNumber = e164,
                OriginationIdentity = settings.originationIdentity,
                MessageBody = body,
                MessageType = MessageType.TRANSACTIONAL
            };
            if (!string.IsNullOrWhiteSpace(settings.configurationSet)) { request.ConfigurationSetName = settings.configurationSet.Trim(); }
            if (!string.IsNullOrWhiteSpace(settings.maxPrice)) { request.MaxPrice = settings.maxPrice.Trim(); }
            //
            // -- context is returned in the delivery events (verify)
            var context = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(appName)) { context["app"] = appName; }
            if (!string.IsNullOrWhiteSpace(logGuid)) { context["log"] = logGuid; }
            if (context.Count > 0) { request.Context = context; }
            return request;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Send one SMS text message through AWS End User Messaging. Never throws, always returns a result.
        /// Retries are handled by the text message queue only.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="e164">destination phone, E.164</param>
        /// <param name="body"></param>
        /// <param name="log">the Text Message Log row for this send. Its ccguid is passed as Context["log"]</param>
        /// <returns></returns>
        public static SmsProviderResult send(CoreController core, string e164, string body, TextMessageLogModel log) {
            try {
                AwsSmsSettings settings = getSettings(core);
                if (string.IsNullOrEmpty(settings.originationIdentity)) {
                    return new SmsProviderResult {
                        success = false,
                        providerId = (int)TextMessageLogModel.ProviderEnum.Aws,
                        errorCode = "NOT_CONFIGURED",
                        errorMessage = "AWS SMS Origination Identity is not set in site settings.",
                        isRetryable = false
                    };
                }
                //
                // -- region, site setting else the server's region
                string regionName = !string.IsNullOrEmpty(settings.region) ? settings.region : (core.cpParent.ServerConfig.awsRegionName ?? "");
                if (string.IsNullOrWhiteSpace(regionName)) {
                    return new SmsProviderResult {
                        success = false,
                        providerId = (int)TextMessageLogModel.ProviderEnum.Aws,
                        errorCode = "NO_REGION",
                        errorMessage = "No AWS region is set. Set AWS SMS Region in site settings or the server's AWS region.",
                        isRetryable = false
                    };
                }
                Amazon.RegionEndpoint region = Amazon.RegionEndpoint.GetBySystemName(regionName.Trim());
                var config = new AmazonPinpointSMSVoiceV2Config {
                    RegionEndpoint = region,
                    Timeout = TimeSpan.FromSeconds(10),
                    MaxErrorRetry = 2
                };
                var cred = AwsCredentialController.getCredentials(core);
                //
                SendTextMessageRequest request = buildRequest(e164, body, settings, core.appConfig.name, log?.ccguid);
                using (AmazonPinpointSMSVoiceV2Client client = (cred == null) ? new AmazonPinpointSMSVoiceV2Client(config) : new AmazonPinpointSMSVoiceV2Client(cred, config)) {
                    SendTextMessageResponse response = client.SendTextMessageAsync(request).waitSynchronously();
                    return new SmsProviderResult {
                        success = true,
                        providerId = (int)TextMessageLogModel.ProviderEnum.Aws,
                        providerMessageId = response?.MessageId ?? ""
                    };
                }
            } catch (Exception ex) {
                SmsProviderResult result = mapException(ex, out bool isConfigurationError);
                if (isConfigurationError) {
                    //
                    // -- permissions or a missing resource are a misconfiguration the site administrator must fix
                    core.cpParent.Site.ErrorReport(ex, $"AWS SMS send failed, {result.errorCode}");
                } else if (!result.isOptedOut && !result.isRetryable && string.IsNullOrEmpty(result.errorCode)) {
                    core.cpParent.Site.ErrorReport(ex, "AWS SMS send failed, unexpected exception");
                } else {
                    logger.Warn($"{core.logCommonMessage},AwsSmsController.send, [{result.errorCode}] {result.errorMessage}");
                }
                return result;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Map an exception thrown by the SDK to a provider result. Pure function, no AWS calls. 
        /// An AggregateException (from the synchronous wait) is unwrapped first.
        /// - ConflictException reason DESTINATION_PHONE_NUMBER_OPTED_OUT: isOptedOut, not retryable
        /// - ValidationException: errorCode = reason, not retryable
        /// - ThrottlingException, ServiceQuotaExceededException, InternalServerException: retryable
        /// - AccessDeniedException, ResourceNotFoundException: not retryable, isConfigurationError
        /// - any other exception: not retryable, errorCode blank
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="isConfigurationError">true if the error is a misconfiguration that should be reported to the site administrator</param>
        /// <returns></returns>
        public static SmsProviderResult mapException(Exception ex, out bool isConfigurationError) {
            isConfigurationError = false;
            if (ex is AggregateException aggregate) {
                Exception inner = aggregate.Flatten().InnerExceptions.FirstOrDefault();
                if (inner != null) { ex = inner; }
            }
            var result = new SmsProviderResult {
                success = false,
                providerId = (int)TextMessageLogModel.ProviderEnum.Aws,
                errorMessage = ex?.Message ?? ""
            };
            if (ex is ConflictException conflict) {
                result.errorCode = conflict.Reason?.Value ?? "CONFLICT";
                result.isRetryable = false;
                if (string.Equals(result.errorCode, "DESTINATION_PHONE_NUMBER_OPTED_OUT", StringComparison.OrdinalIgnoreCase)) {
                    result.isOptedOut = true;
                    result.errorMessage = "The recipient has opted out of text messages.";
                }
            } else if (ex is ValidationException validation) {
                result.errorCode = validation.Reason?.Value ?? "VALIDATION";
                result.isRetryable = false;
            } else if (ex is ThrottlingException) {
                result.errorCode = "THROTTLING";
                result.isRetryable = true;
            } else if (ex is ServiceQuotaExceededException quota) {
                result.errorCode = quota.Reason?.Value ?? "SERVICE_QUOTA_EXCEEDED";
                result.isRetryable = true;
            } else if (ex is InternalServerException) {
                result.errorCode = "INTERNAL_SERVER_ERROR";
                result.isRetryable = true;
            } else if (ex is AccessDeniedException access) {
                result.errorCode = access.Reason?.Value ?? "ACCESS_DENIED";
                result.errorMessage = "AWS denied access to text messaging. Check the AWS permissions for this server.";
                result.isRetryable = false;
                isConfigurationError = true;
            } else if (ex is ResourceNotFoundException) {
                result.errorCode = "RESOURCE_NOT_FOUND";
                result.errorMessage = "An AWS text messaging resource was not found. Check AWS SMS Origination Identity and AWS SMS Configuration Set in site settings.";
                result.isRetryable = false;
                isConfigurationError = true;
            } else {
                result.errorCode = "";
                result.errorMessage = "Unexpected error sending the text message.";
                result.isRetryable = false;
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
