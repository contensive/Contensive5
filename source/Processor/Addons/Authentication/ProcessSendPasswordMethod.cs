//
using System;
using System.Collections.Generic;
using Contensive.Models.Db;
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
            try {
                CoreController core = ((CPClass)cp).core;
                //
                // -- setup authToken and send reset link and code
                string userEmail = core.docProperties.getText("email");
                if (string.IsNullOrEmpty(userEmail)) { return ""; }
                //
                List<PersonModel> users = DbBaseModel.createList<PersonModel>(cp, $"email={DbController.encodeSQLText(userEmail)}");
                if (users.Count != 1) { return ""; }
                PersonModel user = users[0];
                //
                string authToken = PersonModel.createAuthToken(cp, user);
                string userErrorMessage = "";
                EmailController.trySendPasswordReset(core, user, authToken, ref userErrorMessage);
                //
                user.authToken = authToken;
                user.save(core.cpParent);
                core.doc.continueProcessing = false;
                return ""
                    + "<div style=\"width:300px;margin:100px auto 0 auto;\">"
                    + "<p>An attempt to send login information for email address '" + userEmail + "' has been made.</p>"
                    + "<p><a href=\"?" + core.doc.refreshQueryString + "\">Return to the Site.</a></p>"
                    + "</div>";
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
    }
}
