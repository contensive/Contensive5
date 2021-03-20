
using Contensive.Models.Db;
using Contensive.Processor.Exceptions;
using Contensive.Processor.Models.Domain;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static Contensive.Processor.Constants;
using static Newtonsoft.Json.JsonConvert;
//
namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// manage email send process
    /// </summary>
    public static class EmailController {
        //
        private const string emailBlockListFilename = "Config\\SMTPBlockList.txt";
        //
        //====================================================================================================
        /// <summary>
        /// return list of emails that should be blocked. format: email - tab - dateBlocked - newLine
        /// </summary>
        /// <param name="core"></param>
        /// <returns></returns>
        public static string getBlockList(CoreController core) {
            if (!core.doc.emailBlockListStoreLoaded) {
                core.doc.emailBlockListStore = core.privateFiles.readFileText(emailBlockListFilename);
                core.doc.emailBlockListStoreLoaded = true;
            }
            return core.doc.emailBlockListStore;
            //
        }
        //
        //====================================================================================================
        /// <summary>
        /// true if the email is on the blocked list
        /// </summary>
        /// <param name="core"></param>
        /// <param name="emailAddress"></param>
        /// <returns></returns>
        public static bool isOnBlockedList(CoreController core, string emailAddress) {
            return (getBlockList(core).IndexOf(Environment.NewLine + emailAddress + "\t", StringComparison.CurrentCultureIgnoreCase) >= 0);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Add to block list
        /// </summary>
        /// <param name="core"></param>
        /// <param name="EmailAddress"></param>
        public static void addToBlockList(CoreController core, string EmailAddress) {
            var blockList = getBlockList(core);
            if (!verifyEmailAddress(core, EmailAddress)) {
                //
                // bad email address
                //
            } else if (isOnBlockedList(core, EmailAddress)) {
                //
                // They are already in the list
                //
            } else {
                //
                // add them to the list
                //
                core.doc.emailBlockListStore = blockList + Environment.NewLine + EmailAddress + "\t" + core.dateTimeNowMockable;
                core.privateFiles.saveFile(emailBlockListFilename, core.doc.emailBlockListStore);
                core.doc.emailBlockListStoreLoaded = false;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// return true if the email is valid (to-address and from-address)
        /// </summary>
        /// <param name="core"></param>
        /// <param name="email"></param>
        /// <param name="returnUserWarning"></param>
        /// <returns></returns>
        public static bool tryVerifyEmail(CoreController core, EmailSendDomainModel email, ref string returnUserWarning) {
            try {
                if (!verifyEmailAddress(core, email.toAddress)) {
                    //
                    returnUserWarning = "The to-address is not valid.";
                    return false;
                }
                if (!verifyEmailAddress(core, email.fromAddress)) {
                    //
                    returnUserWarning = "The from-address is not valid.";
                    return false;
                }
                return true;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// email address must have at least one character before the @, and have a valid email domain
        /// </summary>
        public static bool verifyEmailAddress(CoreController core, string EmailAddress) {
            try {
                if (string.IsNullOrWhiteSpace(EmailAddress)) { return false; }
                var EmailParts = new System.Net.Mail.MailAddress(EmailAddress);
                if (string.IsNullOrWhiteSpace(EmailParts.Host)) { return false; }
                if (!EmailParts.Host.Contains(".")) { return false; }
                if (EmailParts.Host.right(1).Equals(".")) { return false; }
                if (EmailParts.Host.left(1).Equals(".")) { return false; }
                if (string.IsNullOrWhiteSpace(EmailParts.Address)) { return false; }
                var emailRegex = new Regex(@"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9-]+(?:\.[a-zA-Z0-9-]+)*$", RegexOptions.Compiled);
                return emailRegex.IsMatch(EmailParts.Address);
            } catch (Exception) {
                return false;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Server must have at least 3 digits, and one dot in the middle
        /// </summary>
        public static bool verifyEmailDomain(CoreController core, string emailDomain) {
            try {
                if (string.IsNullOrWhiteSpace(emailDomain)) { return false; }
                string[] SplitArray = emailDomain.Split('.');
                if (SplitArray.GetUpperBound(0) == 0) { return false; }
                if ((SplitArray[0].Length > 0) && (SplitArray[1].Length > 0)) { return true; }
                return false;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Add an email to the queue
        /// </summary>
        /// <returns>false if the email is not sent successfully and the returnUserWarning argument contains a user compatible message. If true, the returnUserWanting may contain a user compatible message about email issues.</returns>
        public static bool queueAdHocEmail(CoreController core, string emailContextMessage, int loggedPersonId, string toAddress, string fromAddress, string subject, string body, string bounceAddress, string replyToAddress, string ignore, bool isImmediate, bool isHTML, int loggedEmailId, ref string returnSendStatus) {
            bool result = false;
            try {
                if (!verifyEmailAddress(core, toAddress)) {
                    //
                    returnSendStatus = "Email not sent because the to-address is not valid.";
                    LogController.logInfo(core, "queueAdHocEmail, NOT SENT [" + returnSendStatus + "], toAddress [" + toAddress + "], fromAddress [" + fromAddress + "], subject [" + subject + "]");
                } else if (!verifyEmailAddress(core, fromAddress)) {
                    //
                    returnSendStatus = "Email not sent because the from-address is not valid.";
                    LogController.logInfo(core, "queueAdHocEmail, NOT SENT [" + returnSendStatus + "], toAddress [" + toAddress + "], fromAddress [" + fromAddress + "], subject [" + subject + "]");
                } else if (0 != GenericController.strInstr(1, getBlockList(core), Environment.NewLine + toAddress + Environment.NewLine, 1)) {
                    //
                    returnSendStatus = "Email not sent because the to-address is blocked by this application. See the Blocked Email Report.";
                    LogController.logInfo(core, "queueAdHocEmail, NOT SENT [" + returnSendStatus + "], toAddress [" + toAddress + "], fromAddress [" + fromAddress + "], subject [" + subject + "]");
                } else {
                    //
                    // Test for from-address / to-address matches
                    if (GenericController.toLCase(fromAddress) == GenericController.toLCase(toAddress)) {
                        fromAddress = core.siteProperties.getText("EmailFromAddress", "");
                        if (string.IsNullOrEmpty(fromAddress)) {
                            //
                            //
                            //
                            fromAddress = toAddress;
                            returnSendStatus = "The from-address matches the to-address. This email was sent, but may be blocked by spam filtering.";
                            LogController.logInfo(core, "queueAdHocEmail, sent with warning [" + returnSendStatus + "], toAddress [" + toAddress + "], fromAddress [" + fromAddress + "], subject [" + subject + "]");
                        } else if (GenericController.toLCase(fromAddress) == GenericController.toLCase(toAddress)) {
                            //
                            //
                            //
                            returnSendStatus = "The from-address matches the to-address [" + fromAddress + "] . This email was sent, but may be blocked by spam filtering.";
                            LogController.logInfo(core, "queueAdHocEmail, sent with warning [" + returnSendStatus + "], toAddress [" + toAddress + "], fromAddress [" + fromAddress + "], subject [" + subject + "]");
                        } else {
                            //
                            //
                            //
                            returnSendStatus = "The from-address matches the to-address. The from-address was changed to [" + fromAddress + "] to prevent it from being blocked by spam filtering.";
                            LogController.logInfo(core, "queueAdHocEmail, sent with warning [" + returnSendStatus + "], toAddress [" + toAddress + "], fromAddress [" + fromAddress + "], subject [" + subject + "]");
                        }
                    }
                    string subjectRendered = encodeEmailTextBody(core, false, subject, null);
                    string htmlBody = encodeEmailHtmlBody(core, isHTML, body, "", subject, null, "", false);
                    string textBody = encodeEmailTextBody(core, isHTML, body, null);
                    queueEmail(core, isImmediate, emailContextMessage, new EmailSendDomainModel {
                        attempts = 0,
                        bounceAddress = bounceAddress,
                        fromAddress = fromAddress,
                        htmlBody = htmlBody,
                        replyToAddress = replyToAddress,
                        subject = subjectRendered,
                        textBody = textBody,
                        toAddress = toAddress
                    });
                    LogController.logInfo(core, "queueAdHocEmail, added to queue, toAddress [" + toAddress + "], fromAddress [" + fromAddress + "], subject [" + subject + "]");
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
        /// Send email to a memberId, returns ok if send is successful, otherwise returns the principle issue as a user error.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="recipient"></param>
        /// <param name="fromAddress"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="bounceAddress"></param>
        /// <param name="replyToAddress"></param>
        /// <param name="Immediate"></param>
        /// <param name="isHTML"></param>
        /// <param name="emailId"></param>
        /// <param name="template"></param>
        /// <param name="addLinkAuthToAllLinks"></param>
        /// <param name="userErrorMessage"></param>
        /// <param name="queryStringForLinkAppend"></param>
        /// <param name="emailContextMessage">Brief description for the log entry (Conditional Email, etc)</param>
        /// <returns> returns ok if send is successful, otherwise returns the principle issue as a user error</returns>
        public static bool tryQueuePersonEmail(CoreController core, PersonModel recipient, string fromAddress, string subject, string body, string bounceAddress, string replyToAddress, bool Immediate, bool isHTML, int emailId, string template, bool addLinkAuthToAllLinks, ref string userErrorMessage, string queryStringForLinkAppend, string emailContextMessage) {
            bool result = false;
            try {
                if (recipient == null) {
                    userErrorMessage = "The email was not sent because the recipient could not be found by thier id [" + recipient.id + "]";
                } else if (!verifyEmailAddress(core, recipient.email)) {
                    //
                    userErrorMessage = "Email not sent because the to-address is not valid.";
                } else if (!verifyEmailAddress(core, fromAddress)) {
                    //
                    userErrorMessage = "Email not sent because the from-address is not valid.";
                } else if (0 != GenericController.strInstr(1, getBlockList(core), Environment.NewLine + recipient.email + Environment.NewLine, 1)) {
                    //
                    userErrorMessage = "Email not sent because the to-address is blocked by this application. See the Blocked Email Report.";
                } else {
                    string subjectRendered = encodeEmailTextBody(core, false, subject, recipient);
                    string htmlBody = encodeEmailHtmlBody(core, isHTML, body, template, subject, recipient, queryStringForLinkAppend, addLinkAuthToAllLinks);
                    string textBody = encodeEmailTextBody(core, isHTML, body, recipient);
                    string recipientName = (!string.IsNullOrWhiteSpace(recipient.name) && !recipient.name.ToLower().Equals("guest")) ? recipient.name : string.Empty;
                    if (string.IsNullOrWhiteSpace(recipientName)) {
                        recipientName = ""
                            + ((!string.IsNullOrWhiteSpace(recipient.firstName) && !recipient.firstName.ToLower().Equals("guest")) ? recipient.firstName : string.Empty)
                            + " "
                            + ((!string.IsNullOrWhiteSpace(recipient.lastName) && !recipient.lastName.ToLower().Equals("guest")) ? recipient.lastName : string.Empty);
                    }
                    recipientName = recipientName.Trim();
                    var email = new EmailSendDomainModel {
                        attempts = 0,
                        bounceAddress = bounceAddress,
                        emailId = emailId,
                        fromAddress = fromAddress,
                        htmlBody = htmlBody,
                        replyToAddress = replyToAddress,
                        subject = subjectRendered,
                        textBody = textBody,
                        toAddress = (string.IsNullOrWhiteSpace(recipientName)) ? recipient.email : "\"" + recipientName.Replace("\"", "") + "\" <" + recipient.email.Trim() + ">",
                        toMemberId = recipient.id
                    };
                    if (tryVerifyEmail(core, email, ref userErrorMessage)) {
                        queueEmail(core, Immediate, emailContextMessage, email);
                        result = true;
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
            return result;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="emailContextMessage"></param>
        /// <param name="person"></param>
        /// <param name="fromAddress"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="bounceAddress"></param>
        /// <param name="replyToAddress"></param>
        /// <param name="Immediate"></param>
        /// <param name="isHTML"></param>
        /// <param name="emailIdOrZeroForLog"></param>
        /// <param name="template"></param>
        /// <param name="EmailAllowLinkEID"></param>
        /// <param name="returnSendStatus"></param>
        /// <returns></returns>
        public static bool tryQueuePersonEmail(CoreController core, string emailContextMessage, PersonModel person, string fromAddress, string subject, string body, string bounceAddress, string replyToAddress, bool Immediate, bool isHTML, int emailIdOrZeroForLog, string template, bool EmailAllowLinkEID, ref string returnSendStatus) {
            return tryQueuePersonEmail(core, person, fromAddress, subject, body, bounceAddress, replyToAddress, Immediate, isHTML, emailIdOrZeroForLog, template, EmailAllowLinkEID, ref returnSendStatus, "", emailContextMessage);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="emailContextMessage"></param>
        /// <param name="person"></param>
        /// <param name="fromAddress"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="bounceAddress"></param>
        /// <param name="replyToAddress"></param>
        /// <param name="Immediate"></param>
        /// <param name="isHTML"></param>
        /// <param name="emailIdOrZeroForLog"></param>
        /// <param name="template"></param>
        /// <param name="EmailAllowLinkEID"></param>
        /// <returns></returns>
        public static bool tryQueuePersonEmail(CoreController core, string emailContextMessage, PersonModel person, string fromAddress, string subject, string body, string bounceAddress, string replyToAddress, bool Immediate, bool isHTML, int emailIdOrZeroForLog, string template, bool EmailAllowLinkEID) {
            string returnSendStatus = "";
            return tryQueuePersonEmail(core, person, fromAddress, subject, body, bounceAddress, replyToAddress, Immediate, isHTML, emailIdOrZeroForLog, template, EmailAllowLinkEID, ref returnSendStatus, "", emailContextMessage);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Send System Email. System emails are admin editable emails that can be programmatically sent, or sent by application events like page visits.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="emailName"></param>
        /// <param name="appendedCopy"></param>
        /// <param name="additionalMemberID"></param>
        /// <param name="userErrorMessage"></param>
        /// <returns>Admin message if something went wrong (email addresses checked, etc.</returns>
        public static bool tryQueueSystemEmail(CoreController core, string emailName, string appendedCopy, int additionalMemberID, ref string userErrorMessage) {
            if (!String.IsNullOrEmpty(emailName)) {
                SystemEmailModel email = DbBaseModel.createByUniqueName<SystemEmailModel>(core.cpParent, emailName);
                if (email == null) {
                    if (emailName.isNumeric()) {
                        //
                        // -- compatibility for really ugly legacy nonsense where old interface has argument "EmailIdOrName".
                        email = DbBaseModel.create<SystemEmailModel>(core.cpParent, GenericController.encodeInteger(emailName));
                    }
                    if (email == null) {
                        //
                        // -- create new system email with this name - exposure of possible integer used as name
                        email = DbBaseModel.addDefault<SystemEmailModel>(core.cpParent, ContentMetadataModel.getDefaultValueDict(core, SystemEmailModel.tableMetadata.contentName));
                        email.name = emailName;
                        email.subject = emailName;
                        email.fromAddress = core.siteProperties.getText("EmailAdmin", "webmaster@" + core.appConfig.domainList[0]);
                        email.save(core.cpParent);
                        LogController.logError(core, new GenericException("No system email was found with the name [" + emailName + "]. A new email blank was created but not sent."));
                    }
                }
                return queueSystemEmail(core, email, appendedCopy, additionalMemberID, ref userErrorMessage);
            } else {
                return false;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="emailName"></param>
        /// <param name="appendedCopy"></param>
        /// <param name="additionalMemberID"></param>
        /// <returns></returns>
        public static bool tryQueueSystemEmail(CoreController core, string emailName, string appendedCopy, int additionalMemberID) {
            string userErrorMessage = "";
            return tryQueueSystemEmail(core, emailName, appendedCopy, additionalMemberID, ref userErrorMessage);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="emailName"></param>
        /// <param name="appendedCopy"></param>
        /// <returns></returns>
        public static bool tryQueueSystemEmail(CoreController core, string emailName, string appendedCopy) {
            string userErrorMessage = "";
            return tryQueueSystemEmail(core, emailName, appendedCopy, 0, ref userErrorMessage);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="emailName"></param>
        /// <returns></returns>
        public static bool tryQueueSystemEmail(CoreController core, string emailName) {
            string userErrorMessage = "";
            return tryQueueSystemEmail(core, emailName, "", 0, ref userErrorMessage);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="emailid"></param>
        /// <param name="appendedCopy"></param>
        /// <param name="additionalMemberID"></param>
        /// <param name="userErrorMessage"></param>
        /// <returns></returns>
        public static bool tryQueueSystemEmail(CoreController core, int emailid, string appendedCopy, int additionalMemberID, ref string userErrorMessage) {
            SystemEmailModel email = DbBaseModel.create<SystemEmailModel>(core.cpParent, emailid);
            if (email == null) {
                userErrorMessage = "The notification email could not be sent.";
                LogController.logError(core, new GenericException("No system email was found with the id [" + emailid + "]"));
                return false;
            }
            return queueSystemEmail(core, email, appendedCopy, additionalMemberID, ref userErrorMessage);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="emailid"></param>
        /// <param name="appendedCopy"></param>
        /// <param name="additionalMemberID"></param>
        /// <returns></returns>
        public static bool tryQueueSystemEmail(CoreController core, int emailid, string appendedCopy, int additionalMemberID) {
            string userErrorMessage = "";
            return tryQueueSystemEmail(core, emailid, appendedCopy, additionalMemberID, ref userErrorMessage);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="emailid"></param>
        /// <param name="appendedCopy"></param>
        /// <returns></returns>
        public static bool tryQueueSystemEmail(CoreController core, int emailid, string appendedCopy) {
            string userErrorMessage = "";
            return tryQueueSystemEmail(core, emailid, appendedCopy, 0, ref userErrorMessage);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="emailid"></param>
        /// <returns></returns>
        public static bool tryQueueSystemEmail(CoreController core, int emailid) {
            string userErrorMessage = "";
            return tryQueueSystemEmail(core, emailid, "", 0, ref userErrorMessage);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Send a system email
        /// </summary>
        /// <param name="core"></param>
        /// <param name="email"></param>
        /// <param name="appendedCopy"></param>
        /// <param name="additionalMemberID"></param>
        /// <returns>Admin message if something went wrong (email addresses checked, etc.</returns>
        public static bool queueSystemEmail(CoreController core, SystemEmailModel email, string appendedCopy, int additionalMemberID, ref string userErrorMessage) {
            try {
                string BounceAddress = core.siteProperties.emailBounceAddress;
                EmailTemplateModel emailTemplate = DbBaseModel.create<EmailTemplateModel>(core.cpParent, email.emailTemplateId);
                string EmailTemplateSource = "";
                if (emailTemplate != null) {
                    EmailTemplateSource = emailTemplate.bodyHTML;
                }
                if (string.IsNullOrWhiteSpace(EmailTemplateSource)) {
                    EmailTemplateSource = "<div style=\"padding:10px\"><ac type=content></div>";
                }
                var confirmationMessage = new StringBuilder();
                //
                // --- collect values needed for send
                int emailRecordId = email.id;
                string EmailSubjectSource = email.subject;
                string EmailBodySource = email.copyFilename.content + appendedCopy;
                bool emailAllowLinkEId = email.addLinkEId;
                //
                // --- Send message to the additional member
                if (additionalMemberID != 0) {
                    confirmationMessage.Append(BR + "Primary Recipient:" + BR);
                    PersonModel person = DbBaseModel.create<PersonModel>(core.cpParent, additionalMemberID);
                    if (person == null) {
                        confirmationMessage.Append("&nbsp;&nbsp;Error: Not sent to additional user [#" + additionalMemberID + "] because the user record could not be found." + BR);
                    } else {
                        if (string.IsNullOrWhiteSpace(person.email)) {
                            confirmationMessage.Append("&nbsp;&nbsp;Error: Not sent to additional user [#" + additionalMemberID + "] because their email address was blank." + BR);
                        } else {
                            string EmailStatus = "";
                            string queryStringForLinkAppend = "";
                            tryQueuePersonEmail(core, person, email.fromAddress, EmailSubjectSource, EmailBodySource, "", "", false, true, emailRecordId, EmailTemplateSource, emailAllowLinkEId, ref EmailStatus, queryStringForLinkAppend, "System Email");
                            confirmationMessage.Append("&nbsp;&nbsp;Sent to " + person.name + " at " + person.email + ", Status = " + EmailStatus + BR);
                        }
                    }
                }
                //
                // --- Send message to everyone selected
                //
                confirmationMessage.Append(BR + "Recipients in selected System Email groups:" + BR);
                List<int> peopleIdList = PersonModel.createidListForEmail(core.cpParent, emailRecordId);
                foreach (var personId in peopleIdList) {
                    var person = DbBaseModel.create<PersonModel>(core.cpParent, personId);
                    if (person == null) {
                        confirmationMessage.Append("&nbsp;&nbsp;Error: Not sent to user [#" + additionalMemberID + "] because the user record could not be found." + BR);
                    } else {
                        if (string.IsNullOrWhiteSpace(person.email)) {
                            confirmationMessage.Append("&nbsp;&nbsp;Error: Not sent to user [#" + additionalMemberID + "] because their email address was blank." + BR);
                        } else {
                            string EmailStatus = "";
                            string queryStringForLinkAppend = "";
                            tryQueuePersonEmail(core, person, email.fromAddress, EmailSubjectSource, EmailBodySource, "", "", false, true, emailRecordId, EmailTemplateSource, emailAllowLinkEId, ref EmailStatus, queryStringForLinkAppend, "System Email");
                            confirmationMessage.Append("&nbsp;&nbsp;Sent to " + person.name + " at " + person.email + ", Status = " + EmailStatus + BR);
                        }
                    }
                }
                int emailConfirmationMemberId = email.testMemberId;
                //
                // --- Send the completion message to the administrator
                //
                if (emailConfirmationMemberId != 0) {
                    PersonModel person = DbBaseModel.create<PersonModel>(core.cpParent, emailConfirmationMemberId);
                    if (person != null) {
                        string ConfirmBody = "<div style=\"padding:10px;\">" + BR;
                        ConfirmBody += "The follow System Email was sent." + BR;
                        ConfirmBody += "" + BR;
                        ConfirmBody += "If this email includes personalization, each email sent was personalized to it's recipient. This confirmation has been personalized to you." + BR;
                        ConfirmBody += "" + BR;
                        ConfirmBody += "Subject: " + EmailSubjectSource + BR;
                        ConfirmBody += "From: " + email.fromAddress + BR;
                        ConfirmBody += "Bounces return to: " + BounceAddress + BR;
                        ConfirmBody += "Body:" + BR;
                        ConfirmBody += "<div style=\"clear:all\">----------------------------------------------------------------------</div>" + BR;
                        ConfirmBody += EmailBodySource + BR;
                        ConfirmBody += "<div style=\"clear:all\">----------------------------------------------------------------------</div>" + BR;
                        ConfirmBody += "--- recipient list ---" + BR;
                        ConfirmBody += confirmationMessage.ToString() + BR;
                        ConfirmBody += "--- end of list ---" + BR;
                        ConfirmBody += "</div>";
                        //
                        string EmailStatus = "";
                        string queryStringForLinkAppend = "";
                        tryQueuePersonEmail(core, person, email.fromAddress, "System Email confirmation from " + core.appConfig.domainList[0], ConfirmBody, "", "", false, true, emailRecordId, "", false, ref EmailStatus, queryStringForLinkAppend, "System Email");
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
            return true;
        }
        //
        //====================================================================================================
        /// <summary>
        /// send the confirmation email as a test.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="EmailID"></param>
        /// <param name="confirmationMemberId"></param>
        public static void queueConfirmationTestEmail(CoreController core, int EmailID, int confirmationMemberId) {
            try {
                //
                using (var csData = new CsModel(core)) {
                    csData.openRecord("email", EmailID);
                    if (!csData.ok()) {
                        //
                        // -- email not found
                        ErrorController.addUserError(core, "There was a problem sending the email confirmation. The email record could not be found.");
                        return;
                    }
                    //
                    // merge in template
                    string EmailTemplate = "";
                    int EMailTemplateId = csData.getInteger("EmailTemplateID");
                    if (EMailTemplateId != 0) {
                        using var CSTemplate = new CsModel(core);
                        CSTemplate.openRecord("Email Templates", EMailTemplateId, "BodyHTML");
                        if (csData.ok()) {
                            EmailTemplate = CSTemplate.getText("BodyHTML");
                        }
                    }
                    //
                    // spam footer
                    string EmailBody = csData.getText("copyFilename");
                    //
                    // Confirm footer
                    //
                    int TotalCnt = 0;
                    int BlankCnt = 0;
                    int DupCnt = 0;
                    string DupList = "";
                    int BadCnt = 0;
                    var BadList = new StringBuilder();
                    string TotalList = "";
                    int contentControlId = csData.getInteger("contentControlId");
                    bool isGroupEmail = contentControlId.Equals(ContentMetadataModel.getContentId(core, "Group Email"));
                    var personIdList = PersonModel.createidListForEmail(core.cpParent, EmailID);
                    if (isGroupEmail && personIdList.Count.Equals(0)) {
                        ErrorController.addUserError(core, "There are no valid recipients of this email other than the confirmation address. Either no groups or topics were selected, or those selections contain no people with both a valid email addresses and 'Allow Group Email' enabled.");
                    } else {
                        string LastDupEmail = "";
                        foreach (var personId in personIdList) {
                            var person = DbBaseModel.create<PersonModel>(core.cpParent, personId);
                            string Emailtext = person.email;
                            string EMailName = person.name;
                            int emailMemberId = person.id;
                            if (string.IsNullOrEmpty(EMailName)) {
                                EMailName = "no name (member id " + emailMemberId + ")";
                            }
                            string EmailLine = Emailtext + " for " + EMailName;
                            string LastEmail = null;
                            if (string.IsNullOrEmpty(Emailtext)) {
                                BlankCnt += 1;
                            } else {
                                if (Emailtext == LastEmail) {
                                    DupCnt += 1;
                                    if (Emailtext != LastDupEmail) {
                                        DupList = DupList + "<div class=i>" + Emailtext + "</div>" + BR;
                                        LastDupEmail = Emailtext;
                                    }
                                }
                            }
                            int EmailLen = Emailtext.Length;
                            int Posat = GenericController.strInstr(1, Emailtext, "@");
                            int PosDot = Emailtext.LastIndexOf(".") + 1;
                            if (EmailLen < 6) {
                                BadCnt +=  1;
                                BadList.Append(EmailLine + BR);
                            } else if ((Posat < 4) || (Posat > (EmailLen - 4))) {
                                BadCnt +=  1;
                                BadList.Append(EmailLine + BR);
                            }
                            TotalList = TotalList + EmailLine + BR;
                            LastEmail = Emailtext;
                            TotalCnt += 1;
                        }
                    }
                    string ConfirmFooter = "";
                    //
                    if (DupCnt == 1) {
                        ErrorController.addUserError(core, "There is 1 duplicate email address. See the test email for details.");
                        ConfirmFooter = ConfirmFooter + "<div style=\"clear:all\">WARNING: There is 1 duplicate email address. Only one email will be sent to each address. If the email includes personalization, or if you are using link authentication to automatically log in the user, you may want to correct duplicates to be sure the email is created correctly.<div style=\"margin:20px;\">" + DupList + "</div></div>";
                    } else if (DupCnt > 1) {
                        ErrorController.addUserError(core, "There are " + DupCnt + " duplicate email addresses. See the test email for details");
                        ConfirmFooter = ConfirmFooter + "<div style=\"clear:all\">WARNING: There are " + DupCnt + " duplicate email addresses. Only one email will be sent to each address. If the email includes personalization, or if you are using link authentication to automatically log in the user, you may want to correct duplicates to be sure the email is created correctly.<div style=\"margin:20px;\">" + DupList + "</div></div>";
                    }
                    //
                    if (BadCnt == 1) {
                        ErrorController.addUserError(core, "There is 1 invalid email address. See the test email for details.");
                        ConfirmFooter = ConfirmFooter + "<div style=\"clear:all\">WARNING: There is 1 invalid email address<div style=\"margin:20px;\">" + BadList + "</div></div>";
                    } else if (BadCnt > 1) {
                        ErrorController.addUserError(core, "There are " + BadCnt + " invalid email addresses. See the test email for details");
                        ConfirmFooter = ConfirmFooter + "<div style=\"clear:all\">WARNING: There are " + BadCnt + " invalid email addresses<div style=\"margin:20px;\">" + BadList + "</div></div>";
                    }
                    //
                    if (BlankCnt == 1) {
                        ErrorController.addUserError(core, "There is 1 blank email address. See the test email for details");
                        ConfirmFooter = ConfirmFooter + "<div style=\"clear:all\">WARNING: There is 1 blank email address.</div>";
                    } else if (BlankCnt > 1) {
                        ErrorController.addUserError(core, "There are " + DupCnt + " blank email addresses. See the test email for details.");
                        ConfirmFooter = ConfirmFooter + "<div style=\"clear:all\">WARNING: There are " + BlankCnt + " blank email addresses.</div>";
                    }
                    //
                    if (TotalCnt == 0) {
                        ConfirmFooter = ConfirmFooter + "<div style=\"clear:all\">WARNING: There are no recipients for this email.</div>";
                    } else if (TotalCnt == 1) {
                        ConfirmFooter = ConfirmFooter + "<div style=\"clear:all\">There is 1 recipient<div style=\"margin:20px;\">" + TotalList + "</div></div>";
                    } else {
                        ConfirmFooter = ConfirmFooter + "<div style=\"clear:all\">There are " + TotalCnt + " recipients<div style=\"margin:20px;\">" + TotalList + "</div></div>";
                    }
                    //
                    if (confirmationMemberId == 0) {
                        ErrorController.addUserError(core, "No confirmation email was send because a Confirmation member is not selected");
                    } else {
                        PersonModel person = DbBaseModel.create<PersonModel>(core.cpParent, confirmationMemberId);
                        if (person == null) {
                            ErrorController.addUserError(core, "No confirmation email was send because a Confirmation member is not selected");
                        } else {
                            EmailBody = EmailBody + "<div style=\"clear:both;padding:10px;margin:10px;border:1px dashed #888;\">Administrator<br><br>" + ConfirmFooter + "</div>";
                            string queryStringForLinkAppend = "";
                            string sendStatus = "";
                            if (!tryQueuePersonEmail(core, person, csData.getText("FromAddress"), csData.getText("Subject"), EmailBody, "", "", true, true, EmailID, EmailTemplate, false, ref sendStatus, queryStringForLinkAppend, "Test Email")) {
                                ErrorController.addUserError(core, sendStatus);
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Send all doc.propoerties in an email. This could represent form submissions
        /// </summary>
        /// <param name="core"></param>
        /// <param name="toAddress"></param>
        /// <param name="fromAddress"></param>
        /// <param name="emailSubject"></param>
        /// <param name="userErrorMessage"></param>
        public static bool tryQueueFormEmail(CoreController core, string toAddress, string fromAddress, string emailSubject, out string userErrorMessage) {
            try {
                userErrorMessage = "";
                string Message = "";
                string emailSubjectWorking = emailSubject;
                if ((toAddress.IndexOf("@",StringComparison.InvariantCultureIgnoreCase) == -1)) {
                    toAddress = core.siteProperties.getText("TrapEmail");
                    emailSubjectWorking = "EmailForm with bad to-address";
                    Message = "Subject: " + emailSubjectWorking;
                    Message += Environment.NewLine;
                }
                Message += "The form was submitted " + core.doc.profileStartTime + Environment.NewLine;
                Message += Environment.NewLine;
                Message += "All text fields are included, completed or not.\r\n";
                Message += "Only those checkboxes that are checked are included.\r\n";
                Message += "Entries are not in the order they appeared on the form.\r\n";
                Message += Environment.NewLine;
                foreach (string key in core.docProperties.getKeyList()) {
                    var tempVar = core.docProperties.getProperty(key);
                    if (tempVar.propertyType == DocPropertyModel.DocPropertyTypesEnum.form) {
                        if (GenericController.toUCase(tempVar.value) == "ON") {
                            Message += tempVar.name + ": Yes\r\n\r\n";
                        } else {
                            Message += tempVar.name + ": " + tempVar.value + Environment.NewLine + System.Environment.NewLine;
                        }
                    }
                }
                return queueAdHocEmail(core, "Form Submission Email", core.session.user.id, toAddress, fromAddress, emailSubjectWorking, Message, "", "", "", false, false, 0, ref userErrorMessage);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// send an email to a group of people, each customized
        /// </summary>
        /// <param name="core"></param>
        /// <param name="groupCommaList"></param>
        /// <param name="fromAddress"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="isImmediate"></param>
        /// <param name="isHtml"></param>
        /// <param name="userErrorMessage"></param>
        public static bool tryQueueGroupEmail(CoreController core, string groupCommaList, string fromAddress, string subject, string body, bool isImmediate, bool isHtml, ref string userErrorMessage) {
            try {
                if (string.IsNullOrWhiteSpace(groupCommaList)) { return true; }
                return tryQueueGroupEmail(core, groupCommaList.Split(',').ToList<string>().FindAll(t => !string.IsNullOrWhiteSpace(t)), fromAddress, subject, body, isImmediate, isHtml, ref userErrorMessage);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                userErrorMessage = "There was an unknown error sending the email;";
                return false;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="groupCommaList"></param>
        /// <param name="fromAddress"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="isImmediate"></param>
        /// <param name="isHtml"></param>
        /// <returns></returns>
        public static bool tryQueueGroupEmail(CoreController core, string groupCommaList, string fromAddress, string subject, string body, bool isImmediate, bool isHtml) {
            string userErrorMessage = "";
            return tryQueueGroupEmail(core, groupCommaList, fromAddress, subject, body, isImmediate, isHtml, ref userErrorMessage);
        }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="groupNameList"></param>
        /// <param name="fromAddress"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="isImmediate"></param>
        /// <param name="isHtml"></param>
        /// <param name="userErrorMessage"></param>
        /// <returns></returns>
        public static bool tryQueueGroupEmail(CoreController core, List<string> groupNameList, string fromAddress, string subject, string body, bool isImmediate, bool isHtml, ref string userErrorMessage) {
            try {
                if (groupNameList.Count <= 0) { return true; }
                foreach (var person in PersonModel.createListFromGroupNameList(core.cpParent, groupNameList, true)) {
                    tryQueuePersonEmail(core, "Group Email", person, fromAddress, subject, body, "", "", isImmediate, isHtml, 0, "", false);
                }
                return true;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                userErrorMessage = "There was an unknown error sending the email;";
                return false;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="groupIdList"></param>
        /// <param name="fromAddress"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="isImmediate"></param>
        /// <param name="isHtml"></param>
        /// <param name="userErrorMessage"></param>
        /// <returns></returns>
        public static bool tryQueueGroupEmail(CoreController core, List<int> groupIdList, string fromAddress, string subject, string body, bool isImmediate, bool isHtml, ref string userErrorMessage) {
            try {
                if (groupIdList.Count <= 0) { return true; }
                foreach (var person in PersonModel.createListFromGroupIdList(core.cpParent, groupIdList, true)) {
                    tryQueuePersonEmail(core, "Group Email", person, fromAddress, subject, body, "", "", isImmediate, isHtml, 0, "", false);
                }
                return true;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                userErrorMessage = "There was an unknown error sending the email;";
                return false;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// add email to the email queue
        /// </summary>
        /// <param name="core"></param>
        /// <param name="immediate"></param>
        /// <param name="email"></param>
        /// <param name="emailContextMessage">A short description of the email (Conditional Email, Group Email, Confirmation for Group Email, etc.)</param>
        private static void queueEmail(CoreController core, bool immediate, string emailContextMessage, EmailSendDomainModel email) {
            try {
                var emailQueue = EmailQueueModel.addEmpty<EmailQueueModel>(core.cpParent);
                emailQueue.name = emailContextMessage;
                emailQueue.immediate = immediate;
                emailQueue.toAddress = email.toAddress;
                emailQueue.subject = email.subject;
                emailQueue.content = SerializeObject(email);
                emailQueue.attempts = email.attempts;
                emailQueue.save(core.cpParent);
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Send the emails in the current Queue
        /// </summary>
        public static void sendEmailInQueue(CoreController core) {
            try {
                //
                // -- only send a limited number (100?) and exit so if there is only one task running, sending email will not block all other processes
                // -- make it thread safe(r), from the samples, mark one record and read back marked record. Still exposed to a second process selecting the target-marked email before this process deletes
                // -- get a list of queue records that need to be sent right now. 
                // -- One at a time mark the first for this one sending process
                // -- read back the marked record and if it is there, then no other process is likely looking at it so it can be sent
                // -- this will help prevent duplicate sends, and if the process aborts, only one queued email per queue will be stuck
                List<EmailQueueModel> queueSampleList = DbBaseModel.createList<EmailQueueModel>(core.cpParent, "", "immediate,id desc", 100, 1);
                bool sendWithSES = core.siteProperties.getBoolean(Constants.sitePropertyName_SendEmailWithAmazonSES);
                //
                LogController.logInfo(core, "sending queued email with " + (sendWithSES ? "AWS SES" : "SMTP") + ", based on site property [" + Constants.sitePropertyName_SendEmailWithAmazonSES + "]");
                //
                using (var sesClient = AwsSesController.getSesClient(core)) {
                    foreach (EmailQueueModel queueSample in queueSampleList) {
                        //
                        // -- mark the current sample and select back asa target if it marked, send or skip
                        string targetGuid = GenericController.getGUID();
                        core.db.update(EmailQueueModel.tableMetadata.tableNameLower, "(ccguid=" + DbController.encodeSQLText(queueSample.ccguid) + ")", new System.Collections.Specialized.NameValueCollection() { { "ccguid", DbController.encodeSQLText(targetGuid) } });
                        EmailQueueModel targetQueueRecord = DbBaseModel.create<EmailQueueModel>(core.cpParent, targetGuid);
                        if (targetQueueRecord != null) {
                            //
                            // -- this queue record is not shared with another process, send it
                            DbBaseModel.delete<EmailQueueModel>(core.cpParent, targetQueueRecord.id);
                            int emailDropId = 0;
                            EmailSendDomainModel emailData = DeserializeObject<EmailSendDomainModel>(targetQueueRecord.content);
                            List<EmailDropModel> DropList = DbBaseModel.createList<EmailDropModel>(core.cpParent, "(emailId=" + emailData.emailId + ")", "id desc");
                            if (DropList.Count > 0) {
                                emailDropId = DropList.First().id;
                            }
                            string reasonForFail = "";
                            bool sendSuccess = false;
                            if (sendWithSES) {
                                //
                                // -- send with Amazon SES
                                sendSuccess = AwsSesController.send(core, sesClient, emailData, ref reasonForFail);
                            } else {
                                //
                                // --fall back to SMTP
                                sendSuccess = EmailSmtpController.send(core, emailData, ref reasonForFail);
                            }

                            if (sendSuccess) {
                                //
                                // -- success, log the send
                                var log = EmailLogModel.addDefault<EmailLogModel>(core.cpParent);
                                log.name = "Successfully sent: " + targetQueueRecord.name;
                                log.toAddress = emailData.toAddress;
                                log.fromAddress = emailData.fromAddress;
                                log.subject = emailData.subject;
                                log.body = emailData.htmlBody;
                                log.sendStatus = "ok";
                                log.logType = EmailLogTypeImmediateSend;
                                log.emailId = emailData.emailId;
                                log.memberId = emailData.toMemberId;
                                log.emailDropId = emailDropId;
                                log.save(core.cpParent);
                                LogController.logInfo(core, "sendEmailInQueue, send successful, toAddress [" + emailData.toAddress + "], fromAddress [" + emailData.fromAddress + "], subject [" + emailData.subject + "]");
                            } else {
                                //
                                // -- fail, retry
                                if (emailData.attempts >= 3) {
                                    //
                                    // -- too many retries, log error
                                    string sendStatus = "Failed after 3 retries, reason [" + reasonForFail + "]";
                                    sendStatus = sendStatus.Substring(0, (sendStatus.Length > 254) ? 254 : sendStatus.Length);
                                    var log = EmailLogModel.addDefault<EmailLogModel>(core.cpParent);
                                    log.name = "Aborting unsuccessful send: " + targetQueueRecord.name;
                                    log.toAddress = emailData.toAddress;
                                    log.fromAddress = emailData.fromAddress;
                                    log.subject = emailData.subject;
                                    log.body = emailData.htmlBody;
                                    log.sendStatus = sendStatus;
                                    log.logType = EmailLogTypeImmediateSend;
                                    log.emailId = emailData.emailId;
                                    log.memberId = emailData.toMemberId;
                                    log.save(core.cpParent);
                                    LogController.logInfo(core, "sendEmailInQueue, send FAILED [" + reasonForFail + "], NOT resent because too many retries, toAddress [" + emailData.toAddress + "], fromAddress [" + emailData.fromAddress + "], subject [" + emailData.subject + "], attempts [" + emailData.attempts + "]");
                                } else {
                                    //
                                    // -- fail, add back to end of queue for retry
                                    string sendStatus = "Retrying unsuccessful send (" + emailData.attempts + " of 3), reason [" + reasonForFail + "]";
                                    sendStatus = sendStatus.Substring(0, (sendStatus.Length > 254) ? 254 : sendStatus.Length);
                                    emailData.attempts += 1;
                                    var log = EmailLogModel.addDefault<EmailLogModel>(core.cpParent);
                                    log.name = "Failed send queued for retry: " + targetQueueRecord.name;
                                    log.toAddress = emailData.toAddress;
                                    log.fromAddress = emailData.fromAddress;
                                    log.subject = emailData.subject;
                                    log.body = emailData.htmlBody;
                                    log.sendStatus = sendStatus;
                                    log.logType = EmailLogTypeImmediateSend;
                                    log.emailId = emailData.emailId;
                                    log.memberId = emailData.toMemberId;
                                    log.save(core.cpParent);
                                    queueEmail(core, false, targetQueueRecord.name, emailData);
                                    LogController.logInfo(core, "sendEmailInQueue, failed attempt (" + emailData.attempts + " of 3), reason [" + reasonForFail + "], added to end of queue, toAddress [" + emailData.toAddress + "], fromAddress [" + emailData.fromAddress + "], subject [" + emailData.subject + "], attempts [" + emailData.attempts + "]");
                                }
                            }
                        }
                    }
                    return;
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
        }
        //
        //====================================================================================================        
        /// <summary>
        /// create a simple text version of the email
        /// </summary>
        /// <param name="core"></param>
        /// <param name="isHTML"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        public static string encodeEmailTextBody(CoreController core, bool isHTML, string body, PersonModel recipient) {
            int recipientId = (recipient == null) ? 0 : recipient.id;
            //
            // -- body
            if (!string.IsNullOrWhiteSpace(body)) {
                body = ActiveContentController.renderHtmlForEmail(core, body, recipientId, "", false);
            }
            //
            if (!isHTML) {
                return body;
            } else if (body.ToLower(CultureInfo.InvariantCulture).IndexOf("<html") >= 0) {
                //
                // -- isHtml, if the body includes an html tag, this is the entire body, just send it
                try {
                    return HtmlController.convertHtmlToText(core, body);
                } catch (Exception ex) {
                    LogController.logError(core, ex, "Nuglify error while creating text body from full html.");
                    return string.Empty;
                }
            } else {
                //
                // -- isHtml but no body tag, add an html wrapper
                try {
                    return HtmlController.convertHtmlToText(core, "<body>" + body + "</body>");
                } catch (Exception ex) {
                    LogController.logError(core, ex, "Nuglify error while creating text body from html body.");
                    return string.Empty;
                }
            }
        }
        //
        //====================================================================================================        
        /// <summary>
        /// create the final html document to be sent in the email body
        /// </summary>
        /// <param name="core"></param>
        /// <param name="isHTML"></param>
        /// <param name="body"></param>
        /// <param name="template"></param>
        /// <param name="subject"></param>
        /// <param name="recipient"></param>
        /// <param name="queryStringForLinkAppend"></param>
        /// <returns></returns>
        public static string encodeEmailHtmlBody(CoreController core, bool isHTML, string body, string template, string subject, PersonModel recipient, string queryStringForLinkAppend, bool addLinkAuthToAllLinks) {
            int recipientId = (recipient == null) ? 0 : recipient.id;
            string recipientEmail = (recipient == null) ? "" : recipient.email;
            //
            // -- add www website address to root relative links 
            string webAddressProtocolDomain = HttpController.getWebAddressProtocolDomain(core);
            //
            // -- subject
            if (!string.IsNullOrWhiteSpace(subject)) {
                subject = ActiveContentController.renderHtmlForEmail(core, subject, recipientId, queryStringForLinkAppend, false);
                subject = HtmlController.convertLinksToAbsolute(subject, webAddressProtocolDomain + "/");
                try {
                    subject = HtmlController.convertHtmlToText(core, "<body>" + subject + "</body>");
                } catch (Exception ex) {
                    LogController.logError(core, ex, "Nuglify error while creating text subject line.");
                    subject = string.Empty;
                }
                if (subject == null) { subject = string.Empty; }
                subject = subject.Trim();
            }
            //
            // -- body
            if (!string.IsNullOrWhiteSpace(body)) {
                body = ActiveContentController.renderHtmlForEmail(core, body, recipientId, queryStringForLinkAppend, addLinkAuthToAllLinks);
            }
            //
            // -- encode and merge template
            if (!string.IsNullOrWhiteSpace(template)) {
                //
                // hotfix - templates no longer have wysiwyg editors, so content may not be saved correctly - preprocess to convert wysiwyg content
                template = ActiveContentController.processWysiwygResponseForSave(core, template);
                //
                template = ActiveContentController.renderHtmlForEmail(core, template, recipientId, queryStringForLinkAppend, addLinkAuthToAllLinks);
                if (template.IndexOf(fpoContentBox) != -1) {
                    body = GenericController.strReplace(template, fpoContentBox, body);
                } else {
                    body = template + body;
                }
            }
            //
            // -- convert links to absolute links
            body = HtmlController.convertLinksToAbsolute(body, webAddressProtocolDomain + "/");
            //
            // -- support legacy replace
            body = GenericController.strReplace(body, "#member_id#", recipientId.ToString());
            body = GenericController.strReplace(body, "#member_email#", recipientEmail);
            if (!isHTML) {
                //
                // -- non html email, return a text version of the finished document
                return HtmlController.convertTextToHtml(body);
            }
            //
            // -- Spam Footer under template, remove the marker for any other place in the email then add it as needed
            bool AllowSpamFooter = true;
            if (AllowSpamFooter) {
                //
                // non-authorable, default true - leave it as an option in case there is an important exception
                body += "<div style=\"padding:10px 0;\">" + GenericController.getLinkedText("<a href=\"" + webAddressProtocolDomain + "?" + rnEmailBlockRecipientEmail + "=" + recipientEmail + "\">", core.siteProperties.getText("EmailSpamFooter", DefaultSpamFooter)) + "</div>";
            }

            if (body.ToLower(CultureInfo.InvariantCulture).IndexOf("<html") >= 0) {
                //
                // -- isHtml and the document includes an html tag -- return as-is
                return body;
            }
            //
            // -- html without an html tag. wrap it
            return "<html>"
                + "<head>"
                + "<Title>" + subject + "</Title>"
                + "<Base href=\"" + webAddressProtocolDomain + "\" >"
                + "</head>"
                + "<body class=\"ccBodyEmail\">" + body + "</body>"
                + "</html>";
        }
        //
        //====================================================================================================
        /// <summary>
        /// process group email, adding each to the email queue
        /// </summary>
        /// <param name="core"></param>
        public static void processGroupEmail(CoreController core) {
            try {
                //
                // Open the email records
                string Criteria = "(ccemail.active<>0)"
                    + " and ((ccemail.Sent is null)or(ccemail.Sent=0))"
                    + " and (ccemail.submitted<>0)"
                    + " and ((ccemail.scheduledate is null)or(ccemail.scheduledate<" + core.sqlDateTimeMockable + "))"
                    + " and ((ccemail.ConditionID is null)OR(ccemail.ConditionID=0))"
                    + "";
                using (var CSEmail = new CsModel(core)) {
                    CSEmail.open("Email", Criteria);
                    if (CSEmail.ok()) {
                        string BounceAddress = core.siteProperties.emailBounceAddress;
                        while (CSEmail.ok()) {
                            int emailId = CSEmail.getInteger("ID");
                            int EmailTemplateId = CSEmail.getInteger("EmailTemplateID");
                            string EmailTemplate = getEmailTemplate(core, EmailTemplateId);
                            bool EmailAddLinkEid = CSEmail.getBoolean("AddLinkEID");
                            string EmailFrom = CSEmail.getText("FromAddress");
                            string EmailSubject = CSEmail.getText("Subject");
                            //
                            // Mark this email sent and go to the next
                            CSEmail.set("sent", true);
                            CSEmail.save();
                            //
                            // Create Drop Record
                            int EmailDropId = 0;
                            using (var csDrop = new CsModel(core)) {
                                csDrop.insert("Email Drops");
                                if (csDrop.ok()) {
                                    EmailDropId = csDrop.getInteger("ID");
                                    DateTime ScheduleDate = CSEmail.getDate("ScheduleDate");
                                    if (ScheduleDate < DateTime.Parse("1/1/2000")) {
                                        ScheduleDate = DateTime.Parse("1/1/2000");
                                    }
                                    csDrop.set("Name", "Drop " + EmailDropId + " - Scheduled for " + ScheduleDate.ToString("") + " " + ScheduleDate.ToString(""));
                                    csDrop.set("EmailID", emailId);
                                }
                                csDrop.close();
                            }
                            string EmailStatusList = "";
                            string EmailCopy = CSEmail.getText("copyfilename");
                            using (var csPerson = new CsModel(core)) {
                                //
                                // Select all people in the groups for this email
                                string SQL = "select Distinct ccMembers.id,ccMembers.email, ccMembers.name"
                                    + " From ((((ccemail"
                                    + " left join ccEmailGroups on ccEmailGroups.EmailID=ccEmail.ID)"
                                    + " left join ccGroups on ccGroups.Id = ccEmailGroups.GroupID)"
                                    + " left join ccMemberRules on ccGroups.Id = ccMemberRules.GroupID)"
                                    + " left join ccMembers on ccMembers.Id = ccMemberRules.memberId)"
                                    + " Where (ccEmail.ID=" + emailId + ")"
                                    + " and (ccGroups.active<>0)"
                                    + " and (ccGroups.AllowBulkEmail<>0)"
                                    + " and (ccMembers.active<>0)"
                                    + " and (ccMembers.AllowBulkEmail<>0)"
                                    + " and (ccMembers.email<>'')"
                                    + " and ((ccMemberRules.DateExpires is null)or(ccMemberRules.DateExpires>" + core.sqlDateTimeMockable + "))"
                                    + " order by ccMembers.email,ccMembers.id";
                                csPerson.openSql(SQL);
                                //
                                // Send the email to all selected people
                                //
                                string LastEmail = null;
                                LastEmail = "empty";
                                while (csPerson.ok()) {
                                    int sendToPersonId = csPerson.getInteger("id");
                                    string sendToPersonEmail = csPerson.getText("Email");
                                    string sendToPersonName = csPerson.getText("name");
                                    if (sendToPersonEmail == LastEmail) {
                                        if (string.IsNullOrEmpty(sendToPersonName)) { sendToPersonName = "user #" + sendToPersonId; }
                                        EmailStatusList = EmailStatusList + "Not Sent to " + sendToPersonName + ", duplicate email address (" + sendToPersonEmail + ")" + BR;
                                    } else {
                                        EmailStatusList = EmailStatusList + queueEmailRecord(core, "Group Email", sendToPersonId, emailId, DateTime.MinValue, EmailDropId, BounceAddress, EmailFrom, EmailTemplate, EmailFrom, EmailSubject, EmailCopy, CSEmail.getBoolean("AllowSpamFooter"), CSEmail.getBoolean("AddLinkEID"), "") + BR;
                                    }
                                    LastEmail = sendToPersonEmail;
                                    csPerson.goNext();
                                }
                                csPerson.close();
                            }
                            //
                            // Send the confirmation
                            //
                            int ConfirmationMemberId = CSEmail.getInteger("testmemberid");
                            queueConfirmationEmail(core, ConfirmationMemberId, EmailDropId, EmailTemplate, EmailAddLinkEid, EmailSubject, EmailCopy, "", EmailFrom, EmailStatusList, "Group Email");
                            CSEmail.goNext();
                        }
                    }
                    CSEmail.close();
                }
                return;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw (new GenericException("Unexpected exception"));
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Send conditional email based on days after joining a group
        ///  Return the number of emails effected
        ///  sends email between the condition period date and date +1. if a conditional email is setup and there are already
        ///  peope in the group, they do not get the email if they are past the one day threshhold.
        ///  To keep them from only getting one, the log is used for the one day.
        ///  Housekeep logs far > 1 day
        /// </summary>
        /// <param name="core"></param>
        public static int processConditional_DaysAfterjoining(CoreController core) {
            int emailsEffected = 0;
            using (var csEmailList = new CsModel(core)) {
                string sql = Properties.Resources.sqlConditionalEmail_DaysAfterJoin;
                string bounceAddress = core.siteProperties.emailBounceAddress;
                sql = sql.Replace("{{sqldatenow}}", core.sqlDateTimeMockable);
                //
                // -- almost impossible to debug without a log entry
                LogController.logInfo(core, "processConditional_DaysAfterjoining, select emails to send to users, sql [" + sql + "]");
                //
                csEmailList.openSql(sql);
                while (csEmailList.ok()) {
                    int emailId = csEmailList.getInteger("EmailID");
                    int EmailMemberId = csEmailList.getInteger("MemberID");
                    DateTime EmailDateExpires = csEmailList.getDate("DateExpires");
                    //
                    using (var csEmail = new CsModel(core)) {
                        csEmail.openRecord("Conditional Email", emailId);
                        if (csEmail.ok()) {
                            int EmailTemplateId = csEmail.getInteger("EmailTemplateID");
                            string EmailTemplate = getEmailTemplate(core, EmailTemplateId);
                            string FromAddress = csEmail.getText("FromAddress");
                            int ConfirmationMemberId = csEmail.getInteger("testmemberid");
                            bool EmailAddLinkEid = csEmail.getBoolean("AddLinkEID");
                            string EmailSubject = csEmail.getText("Subject");
                            string EmailCopy = csEmail.getText("CopyFilename");
                            string EmailStatus = queueEmailRecord(core, "Conditional Email", EmailMemberId, emailId, EmailDateExpires, 0, bounceAddress, FromAddress, EmailTemplate, FromAddress, EmailSubject, EmailCopy, csEmail.getBoolean("AllowSpamFooter"), EmailAddLinkEid, "");
                            queueConfirmationEmail(core, ConfirmationMemberId, 0, EmailTemplate, EmailAddLinkEid, EmailSubject, EmailCopy, "", FromAddress, EmailStatus + "<BR>", "Conditional Email");
                            emailsEffected++;
                        }
                        csEmail.close();
                    }
                    //
                    csEmailList.goNext();
                }
                csEmailList.close();
            }
            return emailsEffected;
        }
        //
        //====================================================================================================
        /// <summary>
        /// send conditional emmails, return the count of emails effected
        /// </summary>
        /// <param name="core"></param>
        /// <returns></returns>
        public static int processConditional_DaysBeforeExpiration(CoreController core) {
            int emailsEffected = 0;
            string bounceAddress = core.siteProperties.emailBounceAddress;
            using (var csList = new CsModel(core)) {
                string sql = Properties.Resources.sqlConditionalEmail_DaysBeforeExpiration;
                sql = sql.Replace("{{sqldatenow}}", core.sqlDateTimeMockable);
                //
                // -- almost impossible to debug without a log entry
                LogController.logInfo(core, "processConditional_DaysBeforeExpiration, select emails to send to users, sql [" + sql + "]");
                //
                csList.openSql(sql);
                while (csList.ok()) {
                    int emailId = csList.getInteger("EmailID");
                    int EmailMemberId = csList.getInteger("MemberID");
                    DateTime EmailDateExpires = csList.getDate("DateExpires");
                    //
                    using (var csEmail = new CsModel(core)) {
                        csEmail.openRecord("Conditional Email", emailId);
                        if (csEmail.ok()) {
                            //
                            // -- send this conditional email
                            int EmailTemplateId = csEmail.getInteger("EmailTemplateID");
                            string EmailTemplate = getEmailTemplate(core, EmailTemplateId);
                            string fromAddress = csEmail.getText("FromAddress");
                            int ConfirmationMemberId = csEmail.getInteger("testmemberid");
                            bool EmailAddLinkEid = csEmail.getBoolean("AddLinkEID");
                            string EmailSubject = csEmail.getText("Subject");
                            string EmailCopy = csEmail.getText("CopyFilename");
                            string EmailStatus = queueEmailRecord(core, "Conditional Email", EmailMemberId, emailId, EmailDateExpires, 0, bounceAddress, fromAddress, EmailTemplate, fromAddress, csEmail.getText("Subject"), csEmail.getText("CopyFilename"), csEmail.getBoolean("AllowSpamFooter"), csEmail.getBoolean("AddLinkEID"), "");
                            emailsEffected++;
                            //
                            // -- send confirmation for this send
                            queueConfirmationEmail(core, ConfirmationMemberId, 0, EmailTemplate, EmailAddLinkEid, EmailSubject, EmailCopy, "", fromAddress, EmailStatus + "<BR>", "Conditional Email");
                        }
                        csEmail.close();
                    }
                    //
                    csList.goNext();
                }
                csList.close();
            }
            //
            // -- save this processing date to all email records to document last process, and as a way to block re-process of conditional email
            core.db.executeNonQuery("update ccemail set lastProcessDate=" + core.sqlDateTimeMockable);
            //
            return emailsEffected;
        }

        //
        //====================================================================================================
        /// <summary>
        /// process conditional email, adding each to the email queue. Return the number of emails effected
        /// </summary>
        /// <param name="core"></param>
        /// <param name="IsNewHour"></param>
        /// <param name="IsNewDay"></param>
        public static int processConditionalEmail(CoreController core) {
            int emailsEffected = 0;
            try {
                //
                // -- prepopulate new emails with processDate to prevent new emails from past triggering group joins
                core.db.executeNonQuery("update ccemail set lastProcessDate=" + core.sqlDateTimeMockable + " where (lastProcessDate is null)");
                //
                // Send Conditional Email - Offset days after Joining
                //
                emailsEffected += processConditional_DaysAfterjoining(core);
                //
                // Send Conditional Email - Offset days Before Expiration
                //
                emailsEffected += processConditional_DaysBeforeExpiration(core);
                //
                return emailsEffected;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                return emailsEffected;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Send email to a memberid
        /// </summary>
        /// <param name="sendToPersonId"></param>
        /// <param name="emailID"></param>
        /// <param name="DateBlockExpires"></param>
        /// <param name="emailDropID"></param>
        /// <param name="BounceAddress"></param>
        /// <param name="ReplyToAddress"></param>
        /// <param name="EmailTemplate"></param>
        /// <param name="FromAddress"></param>
        /// <param name="EmailSubject"></param>
        /// <param name="EmailBody"></param>
        /// <param name="AllowSpamFooter"></param>
        /// <param name="EmailAllowLinkEID"></param>
        /// <param name="emailStyles"></param>
        /// <returns>OK if successful, else returns user error.</returns>
        public static string queueEmailRecord(CoreController core, string emailContextMessage, int sendToPersonId, int emailID, DateTime DateBlockExpires, int emailDropID, string BounceAddress, string ReplyToAddress, string EmailTemplate, string FromAddress, string EmailSubject, string EmailBody, bool AllowSpamFooter, bool EmailAllowLinkEID, string emailStyles) {
            PersonModel recipient = DbBaseModel.create<PersonModel>(core.cpParent, sendToPersonId);
            string returnStatus = "";
            //
            // -- open detect
            if (emailDropID != 0) {
                string webAddressProtocolDomain = HttpController.getWebAddressProtocolDomain(core);
                string defaultPage = core.siteProperties.serverPageDefault;
                switch (core.siteProperties.getInteger("GroupEmailOpenTriggerMethod", 0)) {
                    case 1:
                        EmailBody += "<link rel=\"stylesheet\" type=\"text/css\" href=\"" + webAddressProtocolDomain + "?" + rnEmailOpenCssFlag + "=" + emailDropID + "&" + rnEmailMemberId + "=" + sendToPersonId + "\">";
                        break;
                    default:
                        EmailBody += "<img src=\"" + webAddressProtocolDomain + "?" + rnEmailOpenFlag + "=" + emailDropID + "&" + rnEmailMemberId + "=" + sendToPersonId + "\">";
                        break;
                }
            }
            //
            // -- click detect
            string queryStringForLinkAppend = rnEmailClickFlag + "=" + emailDropID + "&" + rnEmailMemberId + "=" + sendToPersonId;
            //
            if (EmailController.tryQueuePersonEmail(core, recipient, FromAddress, EmailSubject, EmailBody, BounceAddress, ReplyToAddress, false, true, emailID, EmailTemplate, EmailAllowLinkEID, ref returnStatus, queryStringForLinkAppend, emailContextMessage)) {
                returnStatus = "Added to queue, email for " + recipient.name + " at " + recipient.email;
            }
            return returnStatus;
        }
        //
        //====================================================================================================
        //
        public static string getEmailTemplate(CoreController core, int EmailTemplateID) {
            var emailTemplate = DbBaseModel.create<EmailTemplateModel>(core.cpParent, EmailTemplateID);
            if (emailTemplate != null) {
                return emailTemplate.bodyHTML;
            }
            return "";
        }
        //
        //====================================================================================================
        /// <summary>
        /// Send confirmation email 
        /// </summary>
        /// <param name="ConfirmationMemberID"></param>
        /// <param name="EmailDropID"></param>
        /// <param name="EmailTemplate"></param>
        /// <param name="EmailAllowLinkEID"></param>
        /// <param name="PrimaryLink"></param>
        /// <param name="EmailSubject"></param>
        /// <param name="emailBody"></param>
        /// <param name="emailStyles"></param>
        /// <param name="EmailFrom"></param>
        /// <param name="EmailStatusList"></param>
        public static void queueConfirmationEmail(CoreController core, int ConfirmationMemberID, int EmailDropID, string EmailTemplate, bool EmailAllowLinkEID, string EmailSubject, string emailBody, string emailStyles, string EmailFrom, string EmailStatusList, string emailContextMessage) {
            try {
                PersonModel person = DbBaseModel.create<PersonModel>(core.cpParent, ConfirmationMemberID);
                if (person != null) {
                    string ConfirmBody = ""
                        + "The follow email has been sent."
                        + BR
                        + BR + "If this email includes personalization, each email sent was personalized to its recipient. This confirmation has been personalized to you."
                        + BR
                        + BR + "Subject: " + EmailSubject
                        + BR + "From: "
                        + EmailFrom
                        + BR + "Body"
                        + BR + "----------------------------------------------------------------------"
                        + BR + emailBody
                        + BR + "----------------------------------------------------------------------"
                        + BR + "--- recipient list ---"
                        + BR + EmailStatusList
                        + "--- end of list ---"
                        + BR;
                    string queryStringForLinkAppend = rnEmailClickFlag + "=" + EmailDropID + "&" + rnEmailMemberId + "=" + person.id;
                    string sendStatus = "";
                    EmailController.tryQueuePersonEmail(core, person, EmailFrom, "Email confirmation from " + HttpController.getWebAddressProtocolDomain(core), ConfirmBody, "", "", true, true, EmailDropID, EmailTemplate, EmailAllowLinkEID, ref sendStatus, queryStringForLinkAppend, emailContextMessage);
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
    }
}