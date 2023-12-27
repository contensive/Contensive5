using Contensive.Models.Db;
using System;
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
        /// on success, the user is logged into the new account
        /// of fail, a userError is added to the system describing the reason for the failure. 
        /// siteproperty: AllowMemberJoin - turn this feature on/off. If off, a userError is added.
        /// docProperty: username
        /// docProperty: password
        /// docProperty: firstName
        /// docProperty: lastName
        /// docProperty: email
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
                // todo -- use model no cs
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
                        AuthenticationController.logout(core, core.session);
                    }
                    string FirstName = core.docProperties.getText("firstname");
                    string LastName = core.docProperties.getText("lastname");
                    csPerson.set("FirstName", FirstName);
                    csPerson.set("LastName", LastName);
                    csPerson.set("Name", FirstName + " " + LastName);
                    csPerson.set("Email", core.docProperties.getText("email"));
                    csPerson.set("username", loginForm_Username);
                    csPerson.set("password", loginForm_Password);
                    AuthenticationController.authenticateById(core, core.session, core.session.user.id);
                }
                DbBaseModel.invalidateCacheOfRecord<PersonModel>(core.cpParent, core.session.user.id);
            } catch (Exception ex) {
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
