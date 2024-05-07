
using Contensive.BaseClasses;
using Contensive.Processor.Models.Domain;

namespace Contensive.Processor.Controllers {
    /// <summary>
    /// send SMS text messages
    /// </summary>
    public static class SmsController {
        /// <summary>
        /// send SMS text messages. Returns true on success else returns userError
        /// </summary>
        /// <returns></returns>
        public static bool sendMessage(CoreController core, TextMessageSendRequest textMessageRequest, ref string userError) {
            try {
                if (core.mockTextMessages) {
                    //
                    // -- for unit tests, mock interface by adding to list
                    core.mockTextMessageList.Add(new MockTextMessageClass {
                        textMessageRequest = textMessageRequest
                    });
                    return true;
                }

                int providerId = core.cpParent.Site.GetInteger("SMS Provider Id", 0);
                switch (core.cpParent.Site.GetInteger("SMS Provider Id", 0)) {
                    case 1: {
                            //
                            // -- twillio
                            return TwillioSmsController.sendMessage(core.cpParent, textMessageRequest.toPhone, textMessageRequest.textBody, ref userError);
                        }
                    case 2: {
                            //
                            // -- aws
                            return AwsSmsController.sendMessage(core.cpParent, textMessageRequest.toPhone, textMessageRequest.textBody, ref userError);
                        }
                    default: {
                            //
                            // -- disabled
                            logger.Warn("Attempt to send a text message but not SMS provider is configured in site settings.");
                            userError = "Text messaging is disabled because no text message provider is configured.";
                            return false;
                        }
                }
            } catch (System.Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
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