﻿//
using System;
using System.Collections.Generic;
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using static Contensive.Processor.Controllers.AuthenticationController;
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
                string userErrorMessage = "";
                foreach (PersonModel user in DbBaseModel.createList<PersonModel>(core.cpParent, $"email={DbController.encodeSQLText(userEmail)}")) {
                    var authTokenInfo = new AuthTokenInfoModel(cp, user);
                    AuthTokenInfoModel.setVisitProperty(core.cpParent, authTokenInfo);
                    EmailController.trySendPasswordReset(core, user, authTokenInfo, ref userErrorMessage);
                }
                //
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
