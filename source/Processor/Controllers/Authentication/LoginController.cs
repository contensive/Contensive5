﻿
using Contensive.BaseClasses;
using Contensive.Models.Db;
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
                    result += "<p>If this email was found on the system an email was sent with login instructions.</p>";
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
                    // -- create the action query
                    string QueryString = GenericController.modifyQueryString(core.webServer.requestQueryString, RequestNameHardCodedPage, "", false);
                    QueryString = GenericController.modifyQueryString(QueryString, "requestbinary", "", false);
                    layout += HtmlController.inputHidden("Type", FormTypeLogin);
                    layout += HtmlController.inputHidden("email", core.session.user.email);
                    result += HtmlController.form(core, layout, QueryString);
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
                string returnHtml = "";
                int loginAddonId = 0;
                if (!forceDefaultLoginForm) {
                    loginAddonId = core.siteProperties.getInteger("Login Page AddonID");
                    if (loginAddonId != 0) {
                        //
                        // -- Custom Login
                        AddonModel addon = DbBaseModel.create<AddonModel>(core.cpParent, loginAddonId);
                        CPUtilsBaseClass.addonExecuteContext executeContext = new CPUtilsBaseClass.addonExecuteContext {
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
        /// <param name="passwordRequestValid">true if the request includes a password property. if true, the no-password mode is blocked and a password is required. Typically used to create an admin/developer login</param>
        /// <returns></returns>
        public static bool processLoginFormDefault(CoreController core, string requestUsername, string requestPassword, bool passwordRequestValid) {
            try {
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
                    passwordRequestValid
                );
                if (userId == 0) {
                    //
                    // -- gegtUserId failed, userError already added
                    if (core.session.isAuthenticated || core.session.isRecognized()) { core.session.logout(); }
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
                LogController.addSiteActivity(core, "successful username/password login", core.session.user.id, core.session.user.organizationId);
                return true;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
    }
}
