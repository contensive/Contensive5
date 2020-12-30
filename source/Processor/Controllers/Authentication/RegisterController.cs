﻿
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Models.Domain;
using System;
using static Contensive.Processor.Constants;
using static Contensive.Processor.Controllers.GenericController;
//
namespace Contensive.Processor.Controllers {
    //
    //========================================================================
    /// <summary>
    /// Simple registration (join) form to setup the user's people record and if approporate, authenticate
    /// </summary>
    public static class RegisterController {
        //
        //========================================================================
        /// <summary>
        /// Simple registration (join) form to setup the user's people record and if approporate, authenticate
        /// </summary>
        /// <param name="core"></param>
        public static void processRegisterForm(CoreController core) {
            try {
                if (!core.siteProperties.getBoolean("AllowMemberJoin", false)) {
                    //
                    // -- public registration not allowed
                    ErrorController.addUserError(core, "This site does not accept public registration.");
                    return;
                }
                string ErrorMessage = "";
                int errorCode = 0;
                string loginForm_Username = core.docProperties.getText("username");
                string loginForm_Password = core.docProperties.getText("password");
                if (!core.session.isNewCredentialOK(loginForm_Username, loginForm_Password, ref ErrorMessage, ref errorCode)) {
                    //
                    // -- credentials are not valid
                    ErrorController.addUserError(core, ErrorMessage);
                    return;
                }
                if (!core.doc.userErrorList.Count.Equals(0)) {
                    //
                    // -- user error occured somewhere during the process, exit
                    return;
                }
                using (var csPerson = new CsModel(core)) {
                    if (!csPerson.open("people", "ID=" + core.session.user.id)) {
                        //
                        // -- user record not valid
                        LogController.logError(core, new Exception("Could not open the current members account to set the username and password."));
                        return;
                    }
                    if ((!string.IsNullOrEmpty(csPerson.getText("username"))) || !string.IsNullOrEmpty(csPerson.getText("password")) || csPerson.getBoolean("admin") || csPerson.getBoolean("developer")) {
                        //
                        // -- if the current account can be logged into, you can not join 'into' it
                        core.session.logout();
                    }
                    string FirstName = core.docProperties.getText("firstname");
                    string LastName = core.docProperties.getText("lastname");
                    csPerson.set("FirstName", FirstName);
                    csPerson.set("LastName", LastName);
                    csPerson.set("Name", FirstName + " " + LastName);
                    csPerson.set("Email", core.docProperties.getText("email"));
                    csPerson.set("username", loginForm_Username);
                    csPerson.set("password", loginForm_Password);
                    core.session.authenticateById(core.session.user.id, core.session);
                }
                DbBaseModel.invalidateCacheOfRecord<PersonModel>(core.cpParent, core.session.user.id);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
    }
}
