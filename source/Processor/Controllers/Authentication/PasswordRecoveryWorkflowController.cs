
using Contensive.Models.Db;
using Contensive.Processor.Models.Domain;
using NLog.Layouts;
using System;
using System.Collections.Generic;
using static Contensive.Processor.Constants;
//
namespace Contensive.Processor.Controllers {
    //
    //========================================================================
    /// <summary>
    /// Recover password form and process
    /// </summary>
    public static class PasswordRecoveryWorkflowController {
        //
        //=============================================================================
        /// <summary>
        /// Send password recovery
        /// </summary>
        /// <returns></returns>
        public static string getPasswordRecoveryForm(CoreController core) {
            string returnResult = "";
            try {
                if (core.siteProperties.getBoolean("allowPasswordEmail", true)) {
                    returnResult += Properties.Resources.defaultForgetPassword_html;
                    returnResult += HtmlController.inputHidden("Type", FormTypePasswordRecovery);
                    return HtmlController.form(core, returnResult, core.cpParent.Request.QueryString);
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
            return returnResult;
        }
        //
        //========================================================================
        /// <summary>
        /// processes a simple email password form that can be stacked into the login page
        /// </summary>
        /// <param name="core"></param>
        public static bool processPasswordRecoveryForm(CoreController core, string requestEmail, ref string userErrorMessage) {
            try {
                if (string.IsNullOrEmpty(requestEmail)) {
                    userErrorMessage = "Email is required.";
                    return false;
                }
                // -- get first email order by id
                List<PersonModel> userList = DbBaseModel.createList<PersonModel>(core.cpParent, $"email={DbController.encodeSQLText(requestEmail)}", "id");
                if (userList.Count == 0) {
                    userErrorMessage = $"There is no login with this email [{HtmlController.encodeHtml(requestEmail)}].";
                    return false;
                }
                foreach (PersonModel user in userList) {
                    var authTokenInfo = new AuthTokenInfoModel(core.cpParent, user);
                    AuthTokenInfoModel.setVisitProperty(core.cpParent, authTokenInfo);
                    trySendPasswordReset(core, user, authTokenInfo, ref userErrorMessage, userList.Count > 1);
                }
                //
                // -- display the password recovery instructions page. Access to set-password can only happen from the email
                return true;
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //
        /// <summary>
        /// Send link to the set-password endpoint
        /// </summary>
        /// <param name="core"></param>
        /// <param name="user"></param>
        /// <param name="authToken"></param>
        /// <param name="userErrorMessage"></param>
        /// <returns></returns>
        public static bool trySendPasswordReset(CoreController core, PersonModel user, AuthTokenInfoModel authTokenInfo, ref string userErrorMessage, bool emailNotUnique) {
            try {
                string currentProtocolDomain = core.cpParent.Request.Protocol + core.cpParent.Request.Host;
                string resetUrl = $"{currentProtocolDomain}{endpointSetPassword}?authToken={authTokenInfo.text}";
                SystemEmailModel email = DbBaseModel.create<SystemEmailModel>(core.cpParent, emailGuidResetPassword);
                if (email is null) {
                    email = DbBaseModel.addDefault<SystemEmailModel>(core.cpParent);
                    email.ccguid = emailGuidResetPassword;
                    email.name = "Password Reset";
                    email.subject = "Password reset";
                    email.fromAddress = core.siteProperties.emailFromAddress;
                    email.copyFilename.content = $"<p>You received this email because there was a request at {core.cpParent.Request.Host} to reset your password.</p>";
                    email.save(core.cpParent);
                }
                string body = "";
                if (string.IsNullOrEmpty(user.username)) {
                    //
                    // -- email is blank, send message to contact site admin
                    body = $"<p>An account was found on this site matching your email address but the username is blank. Please contact the site administrator to have the account username updated.</p>";
                } else {
                    body = $"" +
                       $"<p>A user account was found on this site matching your email address with username: <b>{user.username}</b>.</p>" +
                       $"<p>If you requested this change, <a href=\"{resetUrl}\">click here</a> to set a new password. If you did not request this change ignore this email.</p>";
                    if (emailNotUnique) {
                        body += "" +
                        "<p>" +
                        "Multiple user accounts were found for your email. " +
                        "You were sent an email for each match. " +
                        "You may need to forward one of these emails to the user who requested the change. " +
                        "Once one of these emails has been clicked, the others are no longer valid. " +
                        "The update must be made from the same browser that requested the update. " +
                        "</p>" +
                        "";
                    }
                }
                return EmailController.trySendSystemEmail(core, true, email.id, body, user.id);
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

