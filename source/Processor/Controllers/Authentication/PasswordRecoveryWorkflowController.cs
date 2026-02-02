using Contensive.Models.Db;
using Contensive.Processor.Models.Domain;
using NLog.Layouts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                //
                logger.Trace($"{core.logCommonMessage},PasswordRecoveryWorkflowController.getPasswordRecoveryForm");
                //
                if (core.siteProperties.getBoolean("allowPasswordEmail", true)) {
                    returnResult += LayoutController.getLayout(core.cpParent, layoutRecoverPasswordGuid, layoutRecoverPasswordName, layoutRecoverPasswordCdnPathFilename, "");
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
                return trySendPasswordReset(core, requestEmail, ref userErrorMessage);
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
        /// <param name="usersWithSameEmail">A list of users with the same email address</param>
        /// <param name="passwordToken"></param>
        /// <param name="userErrorMessage"></param>
        /// <returns></returns>
        public static bool trySendPasswordReset(CoreController core, string requestEmail, ref string userErrorMessage) {
            try {
                //
                logger.Trace($"{core.logCommonMessage},PasswordRecoveryWorkflowController.trySendPasswordReset, requestEmail [{requestEmail}]");
                //
                if (string.IsNullOrEmpty(requestEmail)) {
                    userErrorMessage = "The email address cannot be blank";
                    return false;
                }
                string currentProtocolDomain = core.cpParent.Request.Protocol + core.cpParent.Request.Host;
                SystemEmailModel email = DbBaseModel.create<SystemEmailModel>(core.cpParent, emailGuidResetPassword);
                if (email is null) {
                    email = DbBaseModel.addDefault<SystemEmailModel>(core.cpParent);
                    email.ccguid = emailGuidResetPassword;
                    email.name = "Password Reset";
                    email.subject = "Password reset";
                    email.fromAddress = core.siteProperties.emailFromAddress;
                    email.copyFilename.content = $"<p>You received this email because there was a request at website {currentProtocolDomain} to reset your password.</p>";
                    email.save(core.cpParent);
                }
                //
                List<PersonModel> userList = DbBaseModel.createList<PersonModel>(core.cpParent, $"email={DbController.encodeSQLText(requestEmail)}", "id");
                string body = "";
                if (userList.Count == 0) {
                    userErrorMessage = $"There is no user with this email [{HtmlController.encodeHtml(requestEmail)}].";
                    return false;
                }
                if (userList.Count == 1) {
                    //
                    // -- 1 user
                    if (string.IsNullOrEmpty(userList.First().username)) {
                        //
                        // -- username is blank, send message to contact site admin
                        body = $"" +
                            $"<p>" +
                            $"An account was found but the username is blank. " +
                            $"Please contact the site administrator to have the account username updated." +
                            $"</p>";
                        return EmailController.trySendSystemEmail(core, true, email.id, body, userList.First().id, ref userErrorMessage);
                    }
                    //
                    // -- one user found, send password reset
                    var passwordToken = new PasswordTokenModel(core.cpParent, userList.First());
                    string resetUrl = $"{currentProtocolDomain}{endpointSetPassword}?passwordTokenKey={passwordToken.key}";
                    body = $"" +
                        $"<p>" +
                        $"An account was found with username: <b>{userList.First().username}</b>" +
                       $"</p><p>" +
                       $"If you requested this change, <a href=\"{resetUrl}\">please click here</a> to set a new password. If you did not request this change ignore this email." +
                       $"</p>";
                    return EmailController.trySendSystemEmail(core, true, email.id, body, userList.First().id, ref userErrorMessage);
                }
                //
                // -- multiple users found for this email address
                body = $"<p>Multiple accounts were found for this email address. Please select the correct user and continue to reset your password</p>";
                StringBuilder bodyContent = new StringBuilder();
                foreach (PersonModel user in userList) {
                    string name = string.IsNullOrEmpty(user.firstName + user.lastName) ? user.name : user.firstName + " " + user.lastName;
                    var passwordToken = new PasswordTokenModel(core.cpParent, user);
                    string resetUrl = $"{currentProtocolDomain}{endpointSetPassword}?passwordTokenKey={passwordToken.key}";
                    if (string.IsNullOrEmpty(user.username)) {
                        //
                        // -- username is blank, add to end of message
                        bodyContent.Append($"" +
                            $"<p>" +
                            $"<b>{name}</b>" +
                            $"<br>This user does not have a username configured. To setup this account please contact the site administrator." +
                            $"</p>");
                    } else {
                        //
                        // -- username is not blank, add to start of message
                        bodyContent.Insert(0, $"" +
                            $"<p>" +
                            $"<b>{name}</b>" +
                            $"<br>If you requested this change, <a href=\"{resetUrl}\">please click here</a> to set a new password.</p>");
                    }
                }
                return EmailController.trySendSystemEmail(core, true, email.id, body + bodyContent.ToString(), userList.First().id, ref userErrorMessage);
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

