//
using System;
using System.Collections.Generic;
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using static Contensive.Processor.Controllers.AuthController;
//
namespace Contensive.Processor.Addons.Primitives {
    public class ProcessSendPasswordMethodClass : Contensive.BaseClasses.AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cp) {
            try {
                CoreController core = ((CPClass)cp).core;
                //
                core.doc.continueProcessing = false;
                string userErrorMessage = "";
                string userEmail = core.docProperties.getText("email");
                if (!PasswordRecoveryWorkflowController.trySendPasswordReset(core, userEmail, ref userErrorMessage)) {
                    return ""
                        + "<div style=\"width:300px;margin:100px auto 0 auto;\">"
                        + $"<p>There was a problem sending login information for email address '{userEmail}'. {userErrorMessage}</p>"
                        + "<p><a href=\"?" + core.doc.refreshQueryString + "\">Return to the site and try again.</a></p>"
                        + "</div>";
                }
                //
                return ""
                    + "<div style=\"width:300px;margin:100px auto 0 auto;\">"
                    + "<p>An attempt to send login information for email address '" + userEmail + "' has been made. Check your email for instructions to continue.</p>"
                    + "<p><a href=\"?" + core.doc.refreshQueryString + "\">Return to the site.</a></p>"
                    + "</div>";
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
    }
}
