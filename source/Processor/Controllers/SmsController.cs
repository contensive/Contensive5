
using Contensive.BaseClasses;

namespace Contensive.Processor.Controllers {
    /// <summary>
    /// send SMS text messages
    /// </summary>
    public static class SmsController {
        /// <summary>
        /// send SMS text messages
        /// </summary>
        /// <returns></returns>
        public static bool sendMessage(CPBaseClass cp, string phoneNumber, string content) {
            try {
                int providerId = cp.Site.GetInteger("SMS Provider Id", 0);
                if (providerId.Equals(1)) {
                    return TwillioSmsController.sendMessage(cp, phoneNumber, content);
                }
                if (providerId.Equals(2)) {
                    return AwsSmsController.sendMessage(cp, phoneNumber, content);
                }
                cp.Site.ErrorReport("No sms provider selected");
                return false;
            } catch (System.Exception ex) {
                cp.Site.ErrorReport(ex);
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