
using Contensive.BaseClasses;
using Contensive.Models.Db;
using NLog;
using System;
using static Contensive.Processor.Constants;
//
namespace Contensive.Processor.Controllers {
    //
    //========================================================================
    /// <summary>
    /// methods to manage common authentication.
    /// </summary>
    public static class LoginController {
        //
        //========================================================================
        /// <summary>
        /// A complete html page with the login form in the middle. If it processes successfully it returns and empty response to signal success.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="forceDefaultLogin"></param>
        /// <param name="requirePassword">If true, the no-password mode is blocked and a password login is required</param>
        /// <returns></returns>
        public static string getLoginPage(CoreController core, bool forceDefaultLogin, bool requirePassword) {
            try {
                //
                LogController.logTrace(core, "loginController.getLoginPage, enter");
                //
                string result;
                if (forceDefaultLogin) {
                    result = getLoginForm_Default(core, requirePassword);
                } else {
                    result = getLoginForm(core, false, requirePassword);
                }
                if (string.IsNullOrWhiteSpace(result)) { return result; }
                return "<div style=\"width:100%;padding:100px 0 0 0\"><div class=\"ccCon bg-light pt-2 pb-4\" style=\"width:400px;margin:0 auto 0 auto;border:1px solid #bbb;border-radius:5px;\">" + result + "</div></div>";
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// process and return the default login form. If processing is successful, a blank response is returned
        /// </summary>
        /// <param name="core"></param>
        /// <param name="requirePassword">If true, the no-password mode is blocked and a password is required.</param>
        /// <returns></returns>
        public static string getLoginForm_Default(CoreController core, bool requirePassword) {
            string result = "";
            try {
                //
                LogController.logTrace(core, "loginController.getLoginForm_Default, requirePassword [" + requirePassword + "]");
                //
                bool needLoginForm = true;
                string formType = core.docProperties.getText("type");
                if (formType == FormTypeLogin) {
                    //
                    // -- process a previous login for instance, and return blank if it is successful (legacy workflow)
                    string requestUsername = core.cpParent.Doc.GetText("username");
                    string requestPassword = core.cpParent.Doc.GetText("password");
                    bool passwordRequestValid = core.cpParent.Doc.IsProperty("password");
                    if (processLoginFormDefault(core, requestUsername, requestPassword, passwordRequestValid)) {
                        result = "";
                        needLoginForm = false;
                    }
                } else if (formType == FormTypePasswordRecovery) {
                    //
                    // -- process send password
                    PasswordRecoveryController.processPasswordRecoveryForm(core);
                    result += "<p>If this email address was found, an email was sent to it with login instructions.</p>";
                }
                if (needLoginForm) {
                    string layout;
                    //
                    // -- select the correct layout
                    bool allowAutoLogin = core.siteProperties.getBoolean(sitePropertyName_AllowAutoLogin, false);
                    bool allowEmailLogin = core.siteProperties.getBoolean(sitePropertyName_AllowEmailLogin, false);
                    bool allowNoPasswordLogin = !requirePassword && core.siteProperties.getBoolean(sitePropertyName_AllowNoPasswordLogin, false);
                    //
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
                    if (!core.doc.userErrorList.Count.Equals(0)) {
                        layout = layout.Replace("{{userError}}", ErrorController.getUserError(core));
                    } else {
                        layout = layout.Replace("{{userError}}", "");
                    }
                    //
                    // -- wrap in form
                    layout += HtmlController.inputHidden("Type", FormTypeLogin);
                    result += HtmlController.form(core, layout);
                    //
                    // ----- Password Form
                    if (core.siteProperties.getBoolean("allowPasswordEmail", true)) {
                        result += PasswordRecoveryController.getPasswordRecoveryForm(core);
                    }
                    //
                    result = HtmlController.div(result, "ccLoginFormCon");
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
            return result;
        }
        //
        //=============================================================================
        /// <summary>
        /// A login form that can be added to any page. This is just form with no surrounding border, etc. 
        /// </summary>
        /// <returns></returns>
        public static string getLoginForm(CoreController core, bool forceDefaultLoginForm, bool requirePassword) {
            try {
                //
                LogController.logTrace(core, "loginController.getLoginForm, forceDefaultLoginForm [" + forceDefaultLoginForm + "], requirePassword [" + requirePassword + "]");
                //
                string returnHtml = "";
                int loginAddonId = 0;
                if (!forceDefaultLoginForm) {
                    loginAddonId = core.siteProperties.getInteger("Login Page AddonID");
                    if (loginAddonId != 0) {
                        //
                        // -- Custom Login
                        AddonModel addon = DbBaseModel.create<AddonModel>(core.cpParent, loginAddonId);
                        CPUtilsBaseClass.addonExecuteContext executeContext = new() {
                            addonType = CPUtilsBaseClass.addonContext.ContextPage,
                            errorContextMessage = "calling login form addon [" + loginAddonId + "] from internal method"
                        };
                        returnHtml = core.addon.execute(addon, executeContext);
                        if (string.IsNullOrEmpty(returnHtml)) {
                            //
                            // -- login successful, redirect back to this page (without a method)
                            string QS = core.doc.refreshQueryString;
                            QS = GenericController.modifyQueryString(QS, "method", "");
                            QS = GenericController.modifyQueryString(QS, "RequestBinary", "");
                            //
                            return core.webServer.redirect("?" + QS, "Login form success");
                        }
                    }
                }
                if (loginAddonId == 0) {
                    //
                    // ----- When page loads, set focus on login username
                    //
                    returnHtml = getLoginForm_Default(core, requirePassword);
                }
                return returnHtml;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// Process the login form username and password
        /// </summary>
        /// <param name="core"></param>
        /// <param name="requestUsername">The username submitted from the request</param>
        /// <param name="requestPassword">The password submitted from the request</param>
        /// <param name="requestIncludesPassword">true if the request includes a password property. if true, the no-password mode is blocked and a password is required. Typically used to create an admin/developer login</param>
        /// <returns></returns>
        public static bool processLoginFormDefault(CoreController core, string requestUsername, string requestPassword, bool requestIncludesPassword) {
            try {
                //
                LogController.logTrace(core, "loginController.processLoginFormDefault, requestUsername [" + requestUsername + "], requestPassword [" + requestPassword + "], requestIncludesPassword [" + requestIncludesPassword + "]");
                //
                if ((!core.session.visit.cookieSupport) && (core.session.visit.pageVisits>1)) {
                    //
                    // -- no cookies
                    ErrorController.addUserError(core, "The login failed because cookies are disabled.");
                    return false;
                }
                if ((core.session.visit.loginAttempts >= core.siteProperties.maxVisitLoginAttempts)) {
                    //
                    // -- too many attempts
                    ErrorController.addUserError(core, "The login failed.");
                    return false;
                }
                //
                // -- attempt authentication use-cases
                int userId = core.session.getUserIdForUsernameCredentials(
                    requestUsername,
                    requestPassword,
                    requestIncludesPassword
                );
                if (userId == 0) {
                    //
                    // -- getUserId failed, userError already added
                    // -- if login fails, do not logout. Current issue where a second longin process is running, fails, 
                    // -- and logs out because there is a 'username' collision with another addon which overwrites the global-space username variable
                    // -- informal survey of trusted sites leave you logged in if sign-on fails. 
                    // -- i f   ( c o r e . s e s s i o n . i s A u t h e n t i c a t e d   | |   c o r e . s e s s i o n . i s R e c o g n i z e d ( ) )   {   c o r e . s e s s i o n . l o g o u t ( ) ;  }
                    core.session.visit.loginAttempts = core.session.visit.loginAttempts + 1;
                    core.session.visit.save(core.cpParent);
                    ErrorController.addUserError(core, loginFailedError);
                    return false;
                }
                if (!core.session.authenticateById(userId, core.session)) {
                    core.session.visit.loginAttempts = core.session.visit.loginAttempts + 1;
                    core.session.visit.save(core.cpParent);
                    ErrorController.addUserError(core, loginFailedError);
                    return false;
                }
                //
                // -- successful login
                core.session.visit.loginAttempts = 0;
                core.session.visit.save(core.cpParent);
                //
                // -- implement auto login - if login-success and siteproperty.allowAutoLogin and form.autologin and not person.login, then set person.autoLogin=true
                if (core.docProperties.getBoolean("autoLogin") && core.siteProperties.getBoolean(sitePropertyName_AllowAutoLogin)) {
                    PersonModel user = PersonModel.create<PersonModel>(core.cpParent, userId);
                    if ((user != null) && !user.autoLogin) {
                        user.autoLogin = true;
                        user.save(core.cpParent);
                    }
                }
                LogController.addActivity(core, "Login", "successful username/password login", core.session.user.id);
                return true;
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
