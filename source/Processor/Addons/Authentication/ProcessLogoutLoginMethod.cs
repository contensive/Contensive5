
using Contensive.BaseClasses;
using Contensive.Processor.Controllers;
using System;
using System.Collections.Generic;
using static Contensive.Processor.Constants;
//
namespace Contensive.Processor.Addons.Primitives {
    /// <summary>
    /// Process Logout/Login addons
    /// </summary>
    public class ProcessLogoutLoginMethodClass : Contensive.BaseClasses.AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// Process Logout/Login addons
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cp) {
            try {
                CoreController core = ((CPClass)cp).core;
                //
                // -- login
                AuthenticationController.logout(core, core.session);
                core.doc.continueProcessing = false;
                Dictionary<string, string> addonArguments = new Dictionary<string, string>();
                addonArguments.Add("Force Default Login", "false");
                return core.addon.execute(addonGuidLoginPage, new CPUtilsBaseClass.addonExecuteContext {
                    addonType = CPUtilsBaseClass.addonContext.ContextPage,
                    errorContextMessage = "processing logout/login method"
                });
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
    }
}
