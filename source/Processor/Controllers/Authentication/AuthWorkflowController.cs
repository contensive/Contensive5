
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
    public static class AuthWorkflowController {
        //
        //=============================================================================
        /// <summary>
        /// Create and process the login page.
        /// On successful login, the user is authenticated and an empty string is returned.
        /// If the login was not successful, the login form is returned
        /// </summary>
        /// <returns></returns>
        public static string processGetAuthWorkflow(CoreController core, bool forceDefaultLoginForm, bool blockNoPasswordMode) {
            try {
                //
                logger.Trace($"{core.logCommonMessage},loginController.getLoginForm, forceDefaultLoginForm [" + forceDefaultLoginForm + "], requirePassword [" + blockNoPasswordMode + "]");
                //
                if (forceDefaultLoginForm || core.siteProperties.loginPageAddonId == 0) {
                    //
                    // -- use default login
                    return processGetAuthWorkflow_default(core, blockNoPasswordMode);
                }
                //
                // -- Custom Login
                AddonModel addon = core.cacheRuntime.addonCache.create(core.siteProperties.loginPageAddonId);
                if (addon == null) {
                    //
                    // -- custom login not valid, use default login
                    return processGetAuthWorkflow_default(core, blockNoPasswordMode);
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
        private static string processGetAuthWorkflow_default(CoreController core, bool blockNoPasswordMode) {
            try {
                //
                logger.Trace($"{core.logCommonMessage},loginController.getLoginForm_Default, requirePassword [" + blockNoPasswordMode + "]");
                //
                // -- process auth workflow forms
                //
                string userErrorMessage = "";
                string processFormType = core.docProperties.getText("type");
                if (processFormType == FormTypeLogin) {
                    //
                    // -- process a previous login for instance, and return blank if it is successful (legacy workflow)
                    string requestUsername = core.cpParent.Doc.GetText("username");
                    //
                    if (core.session.user.username == requestUsername && core.session.isAuthenticated) {
                        //
                        // -- default login is intercepted during default-auth-event, during core-constructor. If this user is authenticagted, skip process
                        return "";
                    }
                    string requestPassword = core.cpParent.Doc.GetText("password");
                    bool passwordRequestValid = core.cpParent.Doc.IsProperty("password");
                    if (LoginWorkflowController.processLogin(core, requestUsername, requestPassword, passwordRequestValid, ref userErrorMessage)) {
                        return "";
                    }
                }
                if (processFormType == FormTypePasswordRecovery) {
                    //
                    // -- process send password
                    string requestEmail = core.cpParent.Doc.GetText("email").Trim();
                    if (string.IsNullOrWhiteSpace(requestEmail)) {
                        userErrorMessage = "Email is required.";
                    } else if (!EmailController.verifyEmailAddress(core, requestEmail)) {
                        userErrorMessage = "Email is not valid.";
                    } else if (PasswordRecoveryWorkflowController.processPasswordRecoveryForm(core, requestEmail, ref userErrorMessage)) {
                        //
                        // -- display the password recovery instructions page. Access to set-password can only happen from the email
                        return core.cpParent.Mustache.Render(Properties.Resources.Layout_PasswordResetSent, new { email = requestEmail, action = core.cpParent.Request.QueryString });
                    }
                }
                if (processFormType == FormTypeLoginByEmailOtpRequest) {
                    //
                    // -- process OTP email request: generate OTP, send email, show code form
                    string otpEmail = core.cpParent.Doc.GetText("email").Trim();
                    if (string.IsNullOrWhiteSpace(otpEmail)) {
                        userErrorMessage = "Email is required.";
                    } else if (!EmailController.verifyEmailAddress(core, otpEmail)) {
                        userErrorMessage = "Email is not valid.";
                    } else {
                        //
                        // -- generate 6-digit OTP
                        var random = new Random();
                        string otpCode = random.Next(100000, 999999).ToString();
                        //
                        // -- create OTP record (reuses same table as page-blocking email login)
                        var otpRecord = DbBaseModel.addDefault<LoginByEmailOtpModel>(core.cpParent);
                        otpRecord.name = $"OTP for {otpEmail}";
                        otpRecord.email = otpEmail;
                        otpRecord.otp = otpCode;
                        otpRecord.expires = DateTime.Now.AddMinutes(10);
                        otpRecord.used = false;
                        otpRecord.save(core.cpParent);
                        //
                        // -- send OTP email (same response whether user exists or not for security)
                        string emailSubject = "Your Access Code";
                        string emailBody = $"<p>You requested access to {core.cpParent.Request.Host}.</p>"
                            + $"<p>Your one-time access code is: <b>{otpCode}</b></p>"
                            + "<p>This code will expire in 10 minutes.</p>"
                            + "<p>If you did not request this code, please ignore this email.</p>";
                        core.cpParent.Email.send(otpEmail, core.cpParent.Email.fromAddressDefault, emailSubject, emailBody);
                        //
                        // -- show OTP code entry form
                        return getLoginOtpCodeForm(core, otpEmail, "");
                    }
                    //
                    // -- email validation failed, show OTP email form with error
                    return getLoginOtpEmailForm(core, userErrorMessage);
                }
                if (processFormType == FormTypeLoginByEmailOtpVerify) {
                    //
                    // -- process OTP code verification
                    string otpEmail = core.cpParent.Doc.GetText("email").Trim();
                    string otpCode = core.cpParent.Doc.GetText("otp").Trim();
                    if (string.IsNullOrWhiteSpace(otpEmail) || string.IsNullOrWhiteSpace(otpCode)) {
                        return getLoginOtpCodeForm(core, otpEmail, "Please enter your access code.");
                    }
                    //
                    // -- find matching OTP record (unused, any expiration)
                    string otpQuery = $"select top 1 id, expires from ccLoginByEmailOtp where email={DbController.encodeSQLText(otpEmail)} and otp={DbController.encodeSQLText(otpCode)} and (used=0 or used is null) order by id desc";
                    int otpRecordId = 0;
                    DateTime otpExpires = DateTime.MinValue;
                    using (var csData = new CsModel(core)) {
                        if (csData.openSql(otpQuery)) {
                            otpRecordId = csData.getInteger("id");
                            otpExpires = csData.getDate("expires");
                        }
                    }
                    if (otpRecordId == 0) {
                        return getLoginOtpCodeForm(core, otpEmail, "Invalid access code. Please check your code and try again.");
                    }
                    if (otpExpires < DateTime.Now) {
                        //
                        // -- OTP has expired, redirect to email form so user can request a new code
                        return getLoginOtpEmailForm(core, "Your access code has expired. Please request a new code.");
                    }
                    //
                    // -- mark OTP as used
                    var usedOtpRecord = DbBaseModel.create<LoginByEmailOtpModel>(core.cpParent, otpRecordId);
                    if (usedOtpRecord != null) {
                        usedOtpRecord.used = true;
                        usedOtpRecord.save(core.cpParent);
                    }
                    //
                    // -- find user by email
                    int userId = 0;
                    string findUserSQL = $"select top 1 id from ccmembers where email={DbController.encodeSQLText(otpEmail)} order by dateadded desc";
                    using (var csData = new CsModel(core)) {
                        if (csData.openSql(findUserSQL)) {
                            userId = csData.getInteger("id");
                        }
                    }
                    if (userId == 0) {
                        //
                        // -- no user found with this email, create a new user
                        var newUser = DbBaseModel.addDefault<PersonModel>(core.cpParent);
                        newUser.email = otpEmail;
                        newUser.name = otpEmail;
                        newUser.save(core.cpParent);
                        userId = newUser.id;
                    }
                    //
                    // -- log in the user
                    core.cpParent.User.LoginByID(userId);
                    return "";
                }
                //
                // -- check if the user clicked the "Login with Email Code" link
                //
                bool allowLoginByEmailOtp = core.siteProperties.getBoolean(sitePropertyName_AllowLoginByEmailOtp, true);
                string requestMethod = core.docProperties.getText("method");
                if (allowLoginByEmailOtp && requestMethod == "loginbyemailotp") {
                    return getLoginOtpEmailForm(core, userErrorMessage);
                }
                //
                // -- get next auth workflow form
                //
                bool allowAutoLogin = core.siteProperties.getBoolean(sitePropertyName_AllowAutoLogin, false);
                bool allowEmailLogin = core.siteProperties.getBoolean(sitePropertyName_AllowEmailLogin, false);
                bool allowNoPasswordLogin = !blockNoPasswordMode && core.siteProperties.getBoolean(sitePropertyName_AllowNoPasswordLogin, false);
                //
                string layout;
                if (allowEmailLogin && allowNoPasswordLogin && allowAutoLogin) {
                    //
                    // -- email, no-password, auto
                    //
                    layout = LayoutController.getLayout(core.cpParent, "{4145AC0A-7FDC-44A1-9B4E-F11FA0EEFF4B}", "login email nopassword auto", "BaseAssets/login_email_nopassword_auto.html", "");
                } else if (allowEmailLogin && allowNoPasswordLogin && !allowAutoLogin) {
                    //
                    // -- email, no-password, no-auto
                    //
                    layout = LayoutController.getLayout(core.cpParent, "{3B0366A5-56CE-47D4-BD08-77DA8079A282}", "login email nopassword", "BaseAssets/login_email_nopassword.html", "");
                } else if (allowEmailLogin && !allowNoPasswordLogin && allowAutoLogin) {
                    //
                    // -- email, password, auto
                    //
                    layout = LayoutController.getLayout(core.cpParent, "{796BEA4D-9D0B-4D26-B96B-4CA31BEDDF55}", "login email password auto", "BaseAssets/login_email_password_auto.html", "");
                } else if (allowEmailLogin && !allowNoPasswordLogin && !allowAutoLogin) {
                    //
                    // -- email, password, no-auto
                    //
                    layout = LayoutController.getLayout(core.cpParent, "{C989B493-1A08-44C9-8980-054482AB897B}", "login email password", "BaseAssets/login_email_password.html", "");
                } else if (!allowEmailLogin && allowNoPasswordLogin && allowAutoLogin) {
                    //
                    // -- username, no-password, auto
                    //
                    layout = LayoutController.getLayout(core.cpParent, "{440338A7-6690-4664-8443-55B12B8ACB80}", "login username nopassword auto", "BaseAssets/login_username_nopassword_auto.html", "");
                } else if (!allowEmailLogin && allowNoPasswordLogin && !allowAutoLogin) {
                    //
                    // -- username, no-password, no-auto
                    //
                    layout = LayoutController.getLayout(core.cpParent, "{DB680883-1DF4-46B9-9A92-5D1CFB7F693A}", "login username nopassword", "BaseAssets/login_username_nopassword.html", "");
                } else if (!allowEmailLogin && !allowNoPasswordLogin && allowAutoLogin) {
                    //
                    // -- username, password, auto
                    //
                    layout = LayoutController.getLayout(core.cpParent, "{B26AD8CC-90DE-4AF0-9AD3-2A8511777C3C}", "login username password auto", "BaseAssets/login_username_password_auto.html", "");
                } else {
                    //
                    // -- username, password, no-auto
                    //
                    layout = LayoutController.getLayout(core.cpParent, "{1E9C7EA7-04E0-46BB-AA45-88387D9DFC69}", "login username password", "BaseAssets/login_username_password.html", "");
                }
                //
                // -- add user errors and template variables
                layout = MustacheController.renderStringToString(layout, new { userError = userErrorMessage, allowLoginByEmailOtp });
                layout += HtmlController.inputHidden("Type", FormTypeLogin);
                //
                // -- wrap in form that sumbits to the same request URL, to return to the same page after login
                string action = core.cpParent.Request.QueryString;
                //action = GenericController.modifyQueryString(action, "method", "");
                string result = HtmlController.form(core, layout, action);
                //
                // -- Password Form
                if (core.siteProperties.getBoolean("allowPasswordEmail", true)) {
                    result += PasswordRecoveryWorkflowController.getPasswordRecoveryForm(core);
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
        //====================================================================================================
        /// <summary>
        /// Returns the OTP email entry form wrapped in the standard login container
        /// </summary>
        private static string getLoginOtpEmailForm(CoreController core, string userErrorMessage) {
            string layout = LayoutController.getLayout(core.cpParent, layoutLoginOtpEmailGuid, layoutLoginOtpEmailName, layoutLoginOtpEmailCdnPathFilename, "");
            layout = MustacheController.renderStringToString(layout, new { userError = userErrorMessage });
            layout += HtmlController.inputHidden("Type", FormTypeLoginByEmailOtpRequest);
            string action = core.cpParent.Request.QueryString;
            string result = HtmlController.form(core, layout, action);
            result = HtmlController.div(result, "ccLoginFormCon pt-4");
            return ""
                + "<div style=\"width:100%;padding:50px 20px\">"
                + "<div class=\"ccCon bg-light pt-0 pb-2\" style=\"max-width:400px;margin:0 auto 0 auto;border:1px solid #bbb;border-radius:5px;\">"
                + result
                + "</div>"
                + "</div>";
        }
        //
        //====================================================================================================
        /// <summary>
        /// Returns the OTP code verification form wrapped in the standard login container
        /// </summary>
        private static string getLoginOtpCodeForm(CoreController core, string otpEmail, string userErrorMessage) {
            string layout = LayoutController.getLayout(core.cpParent, layoutLoginOtpCodeGuid, layoutLoginOtpCodeName, layoutLoginOtpCodeCdnPathFilename, "");
            layout = MustacheController.renderStringToString(layout, new { userError = userErrorMessage, otpEmail });
            layout += HtmlController.inputHidden("Type", FormTypeLoginByEmailOtpVerify);
            string action = core.cpParent.Request.QueryString;
            string result = HtmlController.form(core, layout, action);
            result = HtmlController.div(result, "ccLoginFormCon pt-4");
            return ""
                + "<div style=\"width:100%;padding:50px 20px\">"
                + "<div class=\"ccCon bg-light pt-0 pb-2\" style=\"max-width:400px;margin:0 auto 0 auto;border:1px solid #bbb;border-radius:5px;\">"
                + result
                + "</div>"
                + "</div>";
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
