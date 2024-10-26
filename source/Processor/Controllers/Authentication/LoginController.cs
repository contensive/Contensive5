
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
    public static class LoginController {
        //
        //=============================================================================
        /// <summary>
        /// Create and process the login page.
        /// On successful login, the user is authenticated and an empty string is returned.
        /// If the login was not successful, the login form is returned
        /// </summary>
        /// <returns></returns>
        public static string getLoginPage(CoreController core, bool forceDefaultLoginForm, bool blockNoPasswordMode) {
            try {
                //
                logger.Trace($"{core.logCommonMessage},loginController.getLoginForm, forceDefaultLoginForm [" + forceDefaultLoginForm + "], requirePassword [" + blockNoPasswordMode + "]");
                //
                if (forceDefaultLoginForm || core.siteProperties.loginPageAddonId == 0) {
                    //
                    // -- use default login
                    return getLoginPage_Default(core, blockNoPasswordMode);
                }
                //
                // -- Custom Login
                AddonModel addon = core.cacheRuntime.addonCache.create(core.siteProperties.loginPageAddonId);
                if (addon == null) {
                    //
                    // -- custom login not valid, use default login
                    return getLoginPage_Default(core, blockNoPasswordMode);
                }
                string result = core.addon.execute(addon, new() {
                    addonType = CPUtilsBaseClass.addonContext.ContextPage,
                    errorContextMessage = "calling login form addon [" + core.siteProperties.loginPageAddonId + "] from internal method"
                });
                if (!string.IsNullOrEmpty(result)) {
                    //
                    // -- non-empty result, display the login html
                    return result;
                }
                //
                // -- login addon return empty (successful), redirect back to this page (without a method)
                string qs = core.doc.refreshQueryString;
                qs = GenericController.modifyQueryString(qs, "method", "");
                qs = GenericController.modifyQueryString(qs, "RequestBinary", "");
                return core.webServer.redirect("?" + qs, "Login form success");
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// process and return the default login form. If processing is successful, a blank response is returned
        /// </summary>
        /// <param name="core"></param>
        /// <param name="blockNoPasswordMode">If true, the no-password mode is blocked and a password is required.</param>
        /// <returns></returns>
        private static string getLoginPage_Default(CoreController core, bool blockNoPasswordMode) {
            try {
                //
                
                logger.Trace($"{core.logCommonMessage},loginController.getLoginForm_Default, requirePassword [" + blockNoPasswordMode + "]");
                //
                string processFormType = core.docProperties.getText("type");
                if (processFormType == FormTypeLogin) {
                    //
                    // -- process a previous login for instance, and return blank if it is successful (legacy workflow)
                    string requestUsername = core.cpParent.Doc.GetText("username");
                    string requestPassword = core.cpParent.Doc.GetText("password");
                    bool passwordRequestValid = core.cpParent.Doc.IsProperty("password");
                    if (processLoginPage_Default(core, requestUsername, requestPassword, passwordRequestValid)) {
                        return "";
                    }
                }
                if (processFormType == FormTypePasswordRecovery) {
                    //
                    // -- process send password
                    string requestEmail = core.cpParent.Doc.GetText("email");
                    if (string.IsNullOrEmpty(requestEmail)) {
                        core.cpParent.UserError.Add("Email is required.");
                    } else {
                        // -- get first email order by id
                        List<PersonModel> emailUsers = DbBaseModel.createList<PersonModel>(core.cpParent, $"email={DbController.encodeSQLText(requestEmail)}","id");
                        if (emailUsers.Count == 0) {
                            core.cpParent.UserError.Add($"There is no login with this email [{HtmlController.encodeHtml(requestEmail)}].");
                        } else {
                            var authTokenInfo = new AuthTokenInfoModel(core.cpParent, emailUsers[0]);
                            AuthTokenInfoModel.setVisitProperty(core.cpParent, authTokenInfo);
                            PasswordRecoveryController.processPasswordRecoveryForm(core, authTokenInfo);
                            //
                            // -- display the password recovery instructions page. Access to set-password can only happen from the email
                            return core.cpParent.Mustache.Render(Properties.Resources.Layout_PasswordResetSent, new  { email = requestEmail });
                        }
                    }
                }
                //
                // todo -- convert to one mustache
                //
                // -- select the correct layout
                bool allowAutoLogin = core.siteProperties.getBoolean(sitePropertyName_AllowAutoLogin, false);
                bool allowEmailLogin = core.siteProperties.getBoolean(sitePropertyName_AllowEmailLogin, false);
                bool allowNoPasswordLogin = !blockNoPasswordMode && core.siteProperties.getBoolean(sitePropertyName_AllowNoPasswordLogin, false);
                //
                string layout;
                if (allowEmailLogin && allowNoPasswordLogin && allowAutoLogin) {
                    //
                    // -- email, no-password, auto
                    //
                    layout = Properties.Resources.login_email_nopassword_auto;
                } else if (allowEmailLogin && allowNoPasswordLogin && !allowAutoLogin) {
                    //
                    // -- email, no-password, no-auto
                    //
                    layout = Properties.Resources.login_email_nopassword;
                } else if (allowEmailLogin && !allowNoPasswordLogin && allowAutoLogin) {
                    //
                    // -- email, password, auto
                    //
                    layout = Properties.Resources.login_email_password_auto;
                } else if (allowEmailLogin && !allowNoPasswordLogin && !allowAutoLogin) {
                    //
                    // -- email, password, no-auto
                    //
                    layout = Properties.Resources.login_email_password;
                } else if (!allowEmailLogin && allowNoPasswordLogin && allowAutoLogin) {
                    //
                    // -- username, no-password, auto
                    //
                    layout = Properties.Resources.login_username_nopassword_auto;
                } else if (!allowEmailLogin && allowNoPasswordLogin && !allowAutoLogin) {
                    //
                    // -- username, no-password, no-auto
                    //
                    layout = Properties.Resources.login_username_nopassword;
                } else if (!allowEmailLogin && !allowNoPasswordLogin && allowAutoLogin) {
                    //
                    // -- username, password, auto
                    //
                    layout = Properties.Resources.login_username_password_auto;
                } else {
                    //
                    // -- username, password, no-auto
                    //
                    layout = Properties.Resources.login_username_password;
                }
                //
                // -- add user errors
                layout = layout.Replace("{{userError}}", core.doc.userErrorList.Count.Equals(0) ? "" : ErrorController.getUserError(core));
                layout += HtmlController.inputHidden("Type", FormTypeLogin);
                //
                // -- wrap in form that sumbits to the same request URL, to return to the same page after login
                string action = GenericController.modifyQueryString(core.cpParent.Request.QueryString, "method","");
                string result = HtmlController.form(core, layout, action);
                //
                // -- Password Form
                if (core.siteProperties.getBoolean("allowPasswordEmail", true)) {
                    result += PasswordRecoveryController.getPasswordRecoveryForm(core);
                }
                //
                result = HtmlController.div(result, "ccLoginFormCon pt-4");
                if (string.IsNullOrWhiteSpace(result)) { return result; }
                return ""
                    + "<div style=\"width:100%;padding:50px 20px\">"
                    + "<div class=\"ccCon bg-light pt-0 pb-2\" style=\"max-width:400px;margin:0 auto 0 auto;border:1px solid #bbb;border-radius:5px;\">"
                    + result
                    + "</div>"
                    + "</div>";
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
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
        public static bool processLoginPage_Default(CoreController core, string requestUsername, string requestPassword, bool requestIncludesPassword) {
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
                string UserErrorMessage = "";
                int userId = AuthenticationController.preflightAuthentication_returnUserId(core, core.session, requestUsername, requestPassword, requestIncludesPassword, ref UserErrorMessage);
                if (userId == 0) {
                    //
                    // -- getUserId failed, userError already added
                    // -- if login fails, do not logout. Current issue where a second longin process is running, fails, 
                    // -- and logs out because there is a 'username' collision with another addon which overwrites the global-space username variable
                    // -- informal survey of trusted sites leave you logged in if sign-on fails. 
                    logger.Info($"{core.logCommonMessage},{UserErrorMessage}");
                    return false;
                }
                if (!AuthenticationController.authenticateById(core, core.session, userId)) {
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
