
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Models.Domain;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using static Contensive.Processor.Constants;
//
namespace Contensive.Processor.Controllers {
    //
    //========================================================================
    /// <summary>
    /// Create and process a login form.
    /// For admin access, the default login form is used.
    /// For public use, the getLoginPage returns a blank html page and runs either the default login addon, or a replacement that can be selected in site-settings
    /// </summary>
    public static class LoginWorkflowController {
        //
        //========================================================================
        /// <summary>
        /// Process the login form username and password.
        /// if successful, return true else false
        /// </summary>
        /// <param name="core"></param>
        /// <param name="requestUsername">The username submitted from the request</param>
        /// <param name="requestPassword">The password submitted from the request</param>
        /// <param name="requestIncludesPassword">true if the request includes a password property. if true, the no-password mode is blocked and a password is required. Typically used to create an admin/developer login</param>
        /// <returns></returns>
        public static bool processLogin(CoreController core, string requestUsername, string requestPassword, bool requestIncludesPassword, ref string UserErrorMessage) {
            try {
                //
                logger.Trace($"{core.logCommonMessage},loginController.processLoginFormDefault, requestUsername [" + requestUsername + "], requestPassword [" + requestPassword + "], requestIncludesPassword [" + requestIncludesPassword + "]");
                //
                if ((!core.session.visit.cookieSupport) && (core.session.visit.pageVisits > 1)) {
                    //
                    // -- no cookies
                    ErrorController.addUserError(core, "The login failed because cookies are disabled.");
                    return false;
                }
                //
                // -- attempt authentication use-cases
                int userId = AuthController.preflightAuthentication_returnUserId(core, core.session, requestUsername, requestPassword, requestIncludesPassword, ref UserErrorMessage);
                if (userId == 0) {
                    //
                    // -- getUserId failed, userError already added
                    // -- if login fails, do not logout. Current issue where a second longin process is running, fails, 
                    // -- and logs out because there is a 'username' collision with another addon which overwrites the global-space username variable
                    // -- informal survey of trusted sites leave you logged in if sign-on fails. 
                    logger.Info($"{core.logCommonMessage},{UserErrorMessage}");
                    return false;
                }
                if (!AuthController.authenticateById(core, core.session, userId)) {
                    ErrorController.addUserError(core, loginFailedError);
                    return false;
                }
                //
                // -- implement auto login - if login-success and siteproperty.allowAutoLogin and form.autologin and not person.login, then set person.autoLogin=true
                if (core.docProperties.getBoolean("autoLogin") && core.siteProperties.getBoolean(sitePropertyName_AllowAutoLogin)) {
                    PersonModel user = PersonModel.create<PersonModel>(core.cpParent, userId);
                    if ((user != null) && !user.autoLogin) {
                        user.autoLogin = true;
                        user.save(core.cpParent);
                    }
                }
                LogController.addActivityCompletedVisit(core, "Login", "successful username/password login", core.session.user.id);
                return true;
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
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
