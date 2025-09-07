
using System;
using Contensive.Processor.Controllers;
using NLog;
//
namespace Contensive.Processor.Addons.AdminSite {
    public static class AdminErrorController {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        //===========================================================================
        //
        public static string get(CoreController core, string UserError, string DeveloperError) {
            string result = "";
            try {
                //
                if (!string.IsNullOrEmpty(DeveloperError)) {
                    logger.Error(new Exception(), $"{core.logCommonMessage},{DeveloperError}");
                }
                if (!string.IsNullOrEmpty(UserError)) {
                    logger.Info($"{core.logCommonMessage},{UserError}");
                    result = AdminDataModel.AdminFormErrorOpen + Processor.Controllers.ErrorController.getUserError(core) + AdminDataModel.AdminFormErrorClose;
                }
                //
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
            return result;
        }
    }
}
