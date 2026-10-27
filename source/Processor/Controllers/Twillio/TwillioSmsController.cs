
using Contensive.Models.Db;
using Contensive.Processor.Models.Domain;
using System;
using System.Net;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// Send SMS text messages through Twilio. Send-only: the Twilio SID is returned but there is no delivery, reply or STOP tracking.
    /// </summary>
    public static class TwillioSmsController {
        //
        /// <summary>
        /// Twilio error code returned when the recipient has replied STOP to the Twilio number (verify)
        /// </summary>
        public const int twilioErrorRecipientOptedOut = 21610;
        //
        //====================================================================================================
        /// <summary>
        /// Send one SMS text message through Twilio. Never throws, always returns a result.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="e164">destination phone, E.164. The pipeline has already normalized it.</param>
        /// <param name="content">cannot be blank.</param>
        /// <returns></returns>
        public static SmsProviderResult send(CoreController core, string e164, string content) {
            var cp = core.cpParent;
            try {
                //
                // -- argument check
                if (string.IsNullOrEmpty(e164)) {
                    return fail("EMPTY_PHONE", "The phone number is empty.");
                }
                if (string.IsNullOrEmpty(content)) {
                    return fail("EMPTY_BODY", "The message is empty.");
                }
                //
                // -- twillio settings
                string accountSid = cp.Site.GetText(Constants.sitePropertySmsTwilioAccountSid, "");
                string authToken = cp.Site.GetText(Constants.sitePropertySmsTwilioAuthToken, "");
                string twilioPhoneNumber = cp.Site.GetText(Constants.sitePropertySmsTwilioNumber, "");
                if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(twilioPhoneNumber)) {
                    cp.Site.ErrorReport("Twilio is the SMS provider but Twillio Account SID, Auth Token or Number is not set in site settings.");
                    return fail("NOT_CONFIGURED", "Twilio is not fully configured in site settings.");
                }
                //
                // -- send twilio
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                TwilioClient.Init(accountSid, authToken);
                MessageResource message = MessageResource.Create(new PhoneNumber(e164), accountSid, from: new PhoneNumber(twilioPhoneNumber), null, content);
                return new SmsProviderResult {
                    success = true,
                    providerId = (int)TextMessageLogModel.ProviderEnum.Twilio,
                    providerMessageId = message?.Sid ?? ""
                };
            } catch (Twilio.Exceptions.ApiException ex) {
                //
                // -- the recipient replied STOP to the Twilio number
                bool isOptedOut = (ex.Code == twilioErrorRecipientOptedOut);
                var result = fail(ex.Code.ToString(), isOptedOut ? "The recipient has opted out of text messages." : ex.Message);
                result.isOptedOut = isOptedOut;
                result.isRetryable = !isOptedOut && (ex.Status == 429 || ex.Status >= 500);
                return result;
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                var result = fail("", "Unexpected error sending the text message.");
                result.isRetryable = true;
                return result;
            }
        }
        //
        private static SmsProviderResult fail(string errorCode, string errorMessage) {
            return new SmsProviderResult {
                success = false,
                providerId = (int)TextMessageLogModel.ProviderEnum.Twilio,
                errorCode = errorCode,
                errorMessage = errorMessage,
                isRetryable = false
            };
        }
    }
}
