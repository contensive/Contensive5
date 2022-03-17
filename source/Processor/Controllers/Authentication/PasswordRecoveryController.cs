
using Contensive.Processor.Models.Domain;
using System;
using static Contensive.Processor.Constants;
using static Contensive.Processor.Controllers.GenericController;
//
namespace Contensive.Processor.Controllers {
    //
    //========================================================================
    /// <summary>
    /// Recover password form and process
    /// </summary>
    public static class PasswordRecoveryController {
        //
        //=============================================================================
        /// <summary>
        /// returns a simple email password form that can be stacked into the login page
        /// </summary>
        /// <returns></returns>
        public static string getPasswordRecoveryForm(CoreController core) {
            string returnResult = "";
            try {
                string QueryString = null;
                //
                if (core.siteProperties.getBoolean("allowPasswordEmail", true)) {
                    returnResult += Properties.Resources.defaultForgetPassword_html;
                    //
                    // write out all of the form input (except state) to hidden fields so they can be read after login
                    returnResult += HtmlController.inputHidden("Type", FormTypePasswordRecovery);
                    foreach (string formKey in core.docProperties.getKeyList()) {
                        var formValue = core.docProperties.getProperty(formKey);
                        if (formValue.propertyType == DocPropertyModel.DocPropertyTypesEnum.form) {
                            switch (toUCase(formValue.name)) {
                                case "S":
                                case "MA":
                                case "MB":
                                case "USERNAME":
                                case "PASSWORD":
                                case "EMAIL":
                                case "TYPE":
                                    break;
                                default:
                                    returnResult += HtmlController.inputHidden(formValue.name, formValue.value);
                                    break;
                            }
                        }
                    }
                    QueryString = core.doc.refreshQueryString;
                    QueryString = modifyQueryString(QueryString, "S", "");
                    QueryString = modifyQueryString(QueryString, "ccIPage", "");
                    returnResult = HtmlController.form(core, returnResult, QueryString);
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
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
        public static void processPasswordRecoveryForm(CoreController core) {
            try {
                string returnUserMessage = "";
                _ = sendPassword(core, core.docProperties.getText("email"), ref returnUserMessage);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Send the user's username and password to them
        /// </summary>
        /// <param name="core"></param>
        /// <param name="Email"></param>
        /// <param name="returnUserMessage"></param>
        /// <returns></returns>
        public static bool sendPassword(CoreController core, string Email, ref string returnUserMessage) {
            bool result = false;
            returnUserMessage = "";
            try {
                const string passwordChrs = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ012345678999999";
                const int passwordChrsLength = 62;
                //
                string workingEmail = GenericController.encodeText(Email);
                //
                string Message = "";
                string FromAddress = "";
                string subject = "";
                if (string.IsNullOrEmpty(workingEmail)) {
                    ErrorController.addUserError(core, "Please enter your email address before requesting your username and password.");
                } else {
                    int atPtr = GenericController.strInstr(1, workingEmail, "@");
                    if (atPtr < 2) {
                        //
                        // email not valid
                        //
                        ErrorController.addUserError(core, "Please enter a valid email address before requesting your username and password.");
                    } else {
                        string EMailName = strMid(workingEmail, 1, atPtr - 1);
                        //
                        LogController.addSiteActivity(core, "password request for email " + workingEmail, core.session.user.id );
                        //
                        bool allowEmailLogin = core.siteProperties.getBoolean(sitePropertyName_AllowEmailLogin, false);
                        int recordCnt = 0;
                        using (var csData = new CsModel(core)) {
                            string sqlCriteria = "(email=" + DbController.encodeSQLText(workingEmail) + ")";
                            sqlCriteria = sqlCriteria + "and((dateExpires is null)or(dateExpires>" + DbController.encodeSQLDate(core.dateTimeNowMockable) + "))";
                            csData.open("People", sqlCriteria, "ID", true, core.session.user.id, "username,password", 1);
                            if (!csData.ok()) {
                                //
                                // valid login account for this email not found
                                //
                                if (encodeText(strMid(workingEmail, atPtr + 1)).ToLowerInvariant() == "contensive.com") {
                                    //
                                    // look for expired account to renew
                                    //
                                    csData.close();
                                    csData.open("People", "((email=" + DbController.encodeSQLText(workingEmail) + "))", "ID");
                                    if (csData.ok()) {
                                        //
                                        // renew this old record
                                        //
                                        csData.set("developer", "1");
                                        csData.set("admin", "1");
                                        if (csData.getDate("dateExpires") > DateTime.MinValue) { csData.set("dateExpires", core.dateTimeNowMockable.AddDays(7).Date.ToString()); }
                                    } else {
                                        //
                                        // inject support record
                                        //
                                        csData.close();
                                        csData.insert("people");
                                        csData.set("name", "Contensive Support");
                                        csData.set("email", workingEmail);
                                        csData.set("developer", "1");
                                        csData.set("admin", "1");
                                        csData.set("dateExpires", core.dateTimeNowMockable.AddDays(7).Date.ToString());
                                    }
                                } else {
                                    ErrorController.addUserError(core, "No current user was found matching this email address. Please try again. ");
                                }
                            }
                            if (csData.ok()) {
                                FromAddress = core.siteProperties.getText("EmailFromAddress", "info@" + core.webServer.requestDomain);
                                subject = "Password Request at " + core.webServer.requestDomain;
                                Message = "";
                                while (csData.ok()) {
                                    bool updateUser = false;
                                    if (string.IsNullOrEmpty(Message)) {
                                        Message = "This email was sent in reply to a request at " + core.webServer.requestDomain + " for the username and password associated with this email address. ";
                                        Message += "If this request was made by you, please return to the login screen and use the following:\r\n";
                                        Message += Environment.NewLine;
                                    } else {
                                        Message += Environment.NewLine;
                                        Message += "Additional user accounts with the same email address: \r\n";
                                    }
                                    //
                                    // username
                                    //
                                    string Username = csData.getText("Username");
                                    bool usernameOK = true;
                                    int Ptr = 0;
                                    if (!allowEmailLogin) {
                                        if (Username != Username.Trim()) {
                                            Username = Username.Trim();
                                            updateUser = true;
                                        }
                                        if (string.IsNullOrEmpty(Username)) {
                                            usernameOK = false;
                                            Ptr = 0;
                                            while (!usernameOK && (Ptr < 100)) {
                                                Username = EMailName + encodeInteger(Math.Floor(encodeNumber(Microsoft.VisualBasic.VBMath.Rnd() * 9999)));
                                                usernameOK = !core.session.isLoginOK(Username, "test");
                                                Ptr += 1;
                                            }
                                            if (usernameOK) {
                                                updateUser = true;
                                            }
                                        }
                                        Message += " username: " + Username + Environment.NewLine;
                                    }
                                    if (usernameOK) {
                                        //
                                        // password
                                        //
                                        string Password = csData.getText("Password");
                                        if (Password.Trim() != Password) {
                                            Password = Password.Trim();
                                            updateUser = true;
                                        }
                                        if (string.IsNullOrEmpty(Password)) {
                                            for (Ptr = 0; Ptr <= 8; Ptr++) {
                                                int Index = encodeInteger(Microsoft.VisualBasic.VBMath.Rnd() * passwordChrsLength);
                                                Password += strMid(passwordChrs, Index, 1);
                                            }
                                            updateUser = true;
                                        }
                                        Message += " password: " + Password + Environment.NewLine;
                                        result = true;
                                        if (updateUser) {
                                            csData.set("username", Username);
                                            csData.set("password", Password);
                                        }
                                        recordCnt += 1;
                                    }
                                    csData.goNext();
                                }
                            }
                        }
                    }
                }
                if (result) {
                    string sendStatus = "";
                    EmailController.queueAdHocEmail(core, "Password Email", core.session.user.id, workingEmail, FromAddress, subject, Message, "", "", "", true, false, 0, ref sendStatus);
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
