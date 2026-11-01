
using Contensive.Models.Db;
using Contensive.Processor.Models.Domain;

namespace Contensive.Processor.Controllers {
    /// <summary>
    /// Route text messages to the provider selected by the SMS Provider Id site setting (1=Twilio, 2=AWS End User Messaging, anything else is off).
    /// </summary>
    public static class SmsController {
        //
        //====================================================================================================
        /// <summary>
        /// the provider selected in site settings. 0=none, 1=Twilio, 2=AWS. See TextMessageLogModel.ProviderEnum
        /// </summary>
        /// <param name="core"></param>
        /// <returns></returns>
        public static int getProviderId(CoreController core) {
            return core.cpParent.Site.GetInteger(Constants.sitePropertySmsProviderId, 2);
        }
        //
        //====================================================================================================
        /// <summary>
        /// true if text messaging is configured and on.
        /// provider 1 needs all three Twilio properties. provider 2 needs AWS SMS Origination Identity. anything else is disabled.
        /// When core.mockTextMessages is set (unit tests) text messaging is always enabled.
        /// </summary>
        /// <param name="core"></param>
        /// <returns></returns>
        public static bool isEnabled(CoreController core) {
            if (core.mockTextMessages) { return true; }
            var cp = core.cpParent;
            switch (getProviderId(core)) {
                case 1: {
                        return !string.IsNullOrWhiteSpace(cp.Site.GetText(Constants.sitePropertySmsTwilioAccountSid, ""))
                            && !string.IsNullOrWhiteSpace(cp.Site.GetText(Constants.sitePropertySmsTwilioAuthToken, ""))
                            && !string.IsNullOrWhiteSpace(cp.Site.GetText(Constants.sitePropertySmsTwilioNumber, ""));
                    }
                case 2: {
                        return !string.IsNullOrWhiteSpace(cp.Site.GetText(Constants.sitePropertySmsAwsOriginationIdentity, ""));
                    }
                default: {
                        return false;
                    }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// the number texts are sent from, if it is known and in E.164 format, else blank
        /// </summary>
        /// <param name="core"></param>
        /// <returns></returns>
        public static string getFromPhone(CoreController core) {
            var cp = core.cpParent;
            string from = "";
            switch (getProviderId(core)) {
                case 1: { from = (cp.Site.GetText(Constants.sitePropertySmsTwilioNumber, "") ?? "").Trim(); break; }
                case 2: { from = (cp.Site.GetText(Constants.sitePropertySmsAwsOriginationIdentity, "") ?? "").Trim(); break; }
            }
            return SmsPhoneController.isE164(from) ? from : "";
        }
        //
        //====================================================================================================
        /// <summary>
        /// Hand one message to the provider. Never throws for provider errors, always returns a result.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="e164">destination phone, E.164</param>
        /// <param name="body"></param>
        /// <param name="log">the Text Message Log row for this send</param>
        /// <returns></returns>
        public static SmsProviderResult send(CoreController core, string e164, string body, TextMessageLogModel log)
            => send(core, e164, body, log, false);
        //
        //====================================================================================================
        /// <summary>
        /// Hand one message to the provider. Never throws for provider errors, always returns a result.
        /// isInteractive: an admin is waiting (cp.SMS.SendText), the AWS SDK does not retry.
        /// </summary>
        public static SmsProviderResult send(CoreController core, string e164, string body, TextMessageLogModel log, bool isInteractive) {
            try {
                if (core.mockTextMessages) {
                    //
                    // -- for unit tests, mock interface by adding to list
                    core.mockTextMessageList.Add(new MockTextMessageClass {
                        textMessageRequest = new TextMessageSendRequest {
                            toPhone = e164,
                            textBody = body,
                            toMemberId = log?.memberId ?? 0,
                            systemTextMessageId = log?.systemTextMessageId ?? 0,
                            groupTextMessageId = log?.groupTextMessageId ?? 0
                        }
                    });
                    return new SmsProviderResult {
                        success = true,
                        providerId = getProviderId(core),
                        providerMessageId = $"mock-{log?.id ?? 0}"
                    };
                }
                switch (getProviderId(core)) {
                    case 1: {
                            //
                            // -- twillio
                            return TwillioSmsController.send(core, e164, body);
                        }
                    case 2: {
                            //
                            // -- aws end user messaging
                            return AwsSmsController.send(core, e164, body, log, isInteractive);
                        }
                    default: {
                            //
                            // -- disabled
                            logger.Warn($"{core.logCommonMessage},Attempt to send a text message but no SMS provider is configured in site settings.");
                            return new SmsProviderResult {
                                success = false,
                                providerId = 0,
                                errorCode = "DISABLED",
                                errorMessage = "Text messaging is disabled because no text message provider is configured."
                            };
                        }
                }
            } catch (System.Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                return new SmsProviderResult {
                    success = false,
                    providerId = getProviderId(core),
                    errorCode = "",
                    errorMessage = "Unexpected error sending the text message.",
                    isRetryable = false
                };
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static NLog.Logger logger { get; } = NLog.LogManager.GetCurrentClassLogger();
    }
}
