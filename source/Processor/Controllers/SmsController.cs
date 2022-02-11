
using Contensive.BaseClasses;
using Contensive.Processor.Models.Domain;

namespace Contensive.Processor.Controllers {
    /// <summary>
    /// send SMS text messages
    /// </summary>
    public static class SmsController {
        /// <summary>
        /// send SMS text messages
        /// </summary>
        /// <returns></returns>
        public static bool sendMessage(CoreController core, TextMessageSendRequest textMessageRequest) {
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
                if (providerId.Equals(1)) {
                    return TwillioSmsController.sendMessage(core.cpParent, textMessageRequest.toPhone, textMessageRequest.textBody);
                }
                //
                // -- default to AWS (provider id 2)
                return AwsSmsController.sendMessage(core.cpParent, textMessageRequest.toPhone, textMessageRequest.textBody);
            } catch (System.Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}