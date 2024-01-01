//
using System;
using Contensive.Processor.Controllers;
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
            string result = "";
            try {
                CoreController core = ((CPClass)cp).core;
                //
                // -- send password
                string Emailtext = core.docProperties.getText("email");
                if (!string.IsNullOrEmpty(Emailtext)) {
                    string userErrorMessage = "";
                    EmailController.trySendPasswordReset(core, Emailtext, ref userErrorMessage);
                    result += ""
                        + "<div style=\"width:300px;margin:100px auto 0 auto;\">"
                        + "<p>An attempt to send login information for email address '" + Emailtext + "' has been made.</p>"
                        + "<p><a href=\"?" + core.doc.refreshQueryString + "\">Return to the Site.</a></p>"
                        + "</div>";
                    core.doc.continueProcessing = false;
                }
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
            }
            return result;
        }
    }
}
