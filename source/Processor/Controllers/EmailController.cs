
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Exceptions;
using Contensive.Processor.Models.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static Contensive.Processor.Constants;
using static Newtonsoft.Json.JsonConvert;
using Amazon.SimpleEmail;
using System.Net;
//
namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// manage email send process
    /// </summary>
    public static class EmailController {
        //
        //====================================================================================================
        //
        public static void unblockEmailAddress(CoreController core, string recipientRawEmail) {
            if (string.IsNullOrEmpty(recipientRawEmail)) { return; }
            string recipientSimpleEmail = EmailController.getSimpleEmailFromFriendlyEmail(core.cpParent, recipientRawEmail);
            List<PersonModel> recipientList = DbBaseModel.createList<PersonModel>(core.cpParent, "(email=" + DbController.encodeSQLText(recipientSimpleEmail) + ")");
            foreach (var recipient in recipientList) {
                //
                // -- mark the person blocked
                recipient.allowBulkEmail = true;
                recipient.save(core.cpParent, 0);
                //
                // -- add to block list
                removeFromBlockList(core, recipientSimpleEmail);
                {
                    //
                    // -- email log and activity log for ad-hack email
                    EmailLogModel log = DbBaseModel.addDefault<EmailLogModel>(core.cpParent);
                    log.name = "User " + recipient.name + ", email " + recipientSimpleEmail + " unblocked at " + core.doc.profileStartTime.ToString();
                    log.emailDropId = 0;
                    log.emailId = 0;
                    log.memberId = recipient.id;
                    log.logType = EmailLogTypeBlockRequest;
                    log.visitId = core.cpParent.Visit.Id;
                    log.save(core.cpParent);
                    //
                    LogController.addActivityCompleted(core, "Email unblocked", log.name, recipient.id, (int)ActivityLogModel.ActivityLogTypeEnum.ContactUpdate);
                }
            }
        }
        //
        //====================================================================================================
        //
        /// <summary>
        /// Block an email address - adding to block list and block all matching users
        /// </summary>
        /// <param name="core"></param>
        /// <param name="emailAddress"></param>
        public static void blockEmailAddress(CoreController core, string recipientRawEmail, int emailDropId) {
            if (string.IsNullOrEmpty(recipientRawEmail)) { return; }
            string recipientSimpleEmail = EmailController.getSimpleEmailFromFriendlyEmail(core.cpParent, recipientRawEmail);
            List<PersonModel> recipientList = DbBaseModel.createList<PersonModel>(core.cpParent, "(email=" + DbController.encodeSQLText(recipientSimpleEmail) + ")");
            foreach (var recipient in recipientList) {
                //
                // -- mark the person blocked
                recipient.allowBulkEmail = false;
                recipient.save(core.cpParent, 0);
                //
                // -- add to block list
                addToBlockList(core, recipientSimpleEmail);
                //
                // -- log entry
                if (emailDropId != 0) {
                    //
                    // -- email log and activity log for email-drop
                    EmailDropModel emailDrop = DbBaseModel.create<EmailDropModel>(core.cpParent, emailDropId);
                    if (emailDrop != null) {
                        EmailLogModel log = DbBaseModel.addDefault<EmailLogModel>(core.cpParent);
                        log.name = "User " + recipient.name + ", email " + recipientSimpleEmail + " clicked linked spam block from email drop " + emailDrop.name + " at " + core.doc.profileStartTime.ToString();
                        log.emailDropId = emailDrop.id;
                        log.emailId = emailDrop.emailId;
                        log.memberId = recipient.id;
                        log.logType = EmailLogTypeBlockRequest;
                        log.visitId = core.cpParent.Visit.Id;
                        log.save(core.cpParent);
                        //
                        LogController.addActivityCompleted(core, "Email blocked", log.name, recipient.id, (int)ActivityLogModel.ActivityLogTypeEnum.ContactUpdate);
                        return;
                    }
                }
                {
                    //
                    // -- email log and activity log for ad-hack email
                    EmailLogModel log = DbBaseModel.addDefault<EmailLogModel>(core.cpParent);
                    log.name = "User " + recipient.name + ", email " + recipientSimpleEmail + " clicked linked spam block from ad-hoc email at " + core.doc.profileStartTime.ToString();
                    log.emailDropId = 0;
                    log.emailId = 0;
                    log.memberId = recipient.id;
                    log.logType = EmailLogTypeBlockRequest;
                    log.visitId = core.cpParent.Visit.Id;
                    log.save(core.cpParent);
                    //
                    LogController.addActivityCompleted(core, "Email blocked", log.name, recipient.id, (int)ActivityLogModel.ActivityLogTypeEnum.ContactUpdate);
                }
            }
        }
        //
        public static void blockEmailAddress(CoreController core, string recipientRawEmail) {
            blockEmailAddress(core, recipientRawEmail, 0);
        }
        //
        //====================================================================================================
        /// <summary>
        /// true if the email is on the blocked list.
        /// Caller must log the error
        /// </summary>
        /// <param name="core"></param>
        /// <param name="friendlyEmailAddress"></param>
        /// <returns></returns>
        public static bool isOnBlockedList(CoreController core, string friendlyEmailAddress) {
            string simpleEmailAddress = getSimpleEmailFromFriendlyEmail(core.cpParent, friendlyEmailAddress);
            using DataTable dt = core.db.executeQuery("select top 1 id from emailbouncelist where email=" + DbController.encodeSQLText(simpleEmailAddress));
            if ((dt?.Rows == null) || (dt.Rows.Count == 0)) { return false; }
            return true;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Add to block list
        /// </summary>
        /// <param name="core"></param>
        /// <param name="friendlyEmailAddress"></param>
        public static void removeFromBlockList(CoreController core, string friendlyEmailAddress) {
            string simpleEmailAddress = getSimpleEmailFromFriendlyEmail(core.cpParent, friendlyEmailAddress);
            if (string.IsNullOrEmpty(simpleEmailAddress)) { return; }
            if (!verifyEmailAddress(core, simpleEmailAddress)) { return; }
            if (!isOnBlockedList(core, simpleEmailAddress)) { return; }
            //
            // -- remove them to the list
            core.db.executeNonQuery("delete from emailbouncelist where email=" + DbController.encodeSQLText(simpleEmailAddress));
        }
        //
        //====================================================================================================
        /// <summary>
        /// Add to block list
        /// </summary>
        /// <param name="core"></param>
        /// <param name="friendlyEmailAddress"></param>
        public static void addToBlockList(CoreController core, string friendlyEmailAddress) {
            string simpleEmailAddress = getSimpleEmailFromFriendlyEmail(core.cpParent, friendlyEmailAddress);
            if (string.IsNullOrEmpty(simpleEmailAddress)) { return; }
            if (!verifyEmailAddress(core, simpleEmailAddress)) { return; }
            if (isOnBlockedList(core, simpleEmailAddress)) { return; }
            //
            // -- add them to the list
            EmailBounceListModel block = DbBaseModel.addDefault<EmailBounceListModel>(core.cpParent);
            block.details = DateTime.Now.ToString() + " user requested email block, type: Permanent";
            block.name = friendlyEmailAddress;
            block.email = simpleEmailAddress;
            block.save(core.cpParent);
        }
        //
        //====================================================================================================
        /// <summary>
        /// return true if the email is valid (to-address and from-address)
        /// this method handles the logging
        /// </summary>
        /// <param name="core"></param>
        /// <param name="sendRequest"></param>
        /// <param name="returnUserWarning"></param>
        /// <returns></returns>
        public static bool tryIsValidEmail(CoreController core, EmailSendRequest sendRequest, ref string returnUserWarning) {
            try {
                if (!verifyEmailAddress(core, sendRequest.toAddress)) {
                    //
                    AddEmailLog(core, sendRequest, false, "No email sent because to-address is not valid: " + sendRequest.emailContextMessage);
                    //
                    returnUserWarning = "The to-address is not valid.";
                    return false;
                }
                if (!verifyEmailAddress(core, sendRequest.fromAddress)) {
                    if (!verifyEmailAddress(core, core.siteProperties.emailFromAddress)) {
                        //
                        AddEmailLog(core, sendRequest, false, "No email sent because from-address is not valid: " + sendRequest.emailContextMessage);
                        //
                        //
                        returnUserWarning = "The from-address is not valid.";
                        return false;
                    }
                    sendRequest.fromAddress = core.siteProperties.emailFromAddress;
                }
                return true;
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// email subject cannot have crlf
        /// </summary>
        public static string validateEmailSubject(CoreController core, string emailSubject) {
            try {
                if (string.IsNullOrWhiteSpace(emailSubject)) { return ""; }
                emailSubject = emailSubject.replace(windowsNewLine, "", StringComparison.InvariantCultureIgnoreCase);
                emailSubject = emailSubject.replace(unixNewLine, "", StringComparison.InvariantCultureIgnoreCase);
                emailSubject = emailSubject.replace(macNewLine, "", StringComparison.InvariantCultureIgnoreCase);
                //
                return emailSubject;
            } catch (Exception) {
                return emailSubject;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// email address must have at least one character before the @, and have a valid email domain
        /// Caller must log the error
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
        /// The caller must log the error
        /// </summary>
        public static bool verifyEmailDomain(CoreController core, string emailDomain) {
            try {
                if (string.IsNullOrWhiteSpace(emailDomain)) {
                    // -- no log, the caller must log
                    return false;
                }
                string[] SplitArray = emailDomain.Split('.');
                if (SplitArray.GetUpperBound(0) == 0) { return false; }
                if ((SplitArray[0].Length > 0) && (SplitArray[1].Length > 0)) { return true; }
                return false;
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Add an email to the queue
        /// </summary>
        /// <returns>false if the email is not sent successfully and the returnUserWarning argument contains a user compatible message. If true, the returnUserWanting may contain a user compatible message about email issues.</returns>
        public static bool sendAdHocEmail(CoreController core, string emailContextMessage, int loggedPersonId, string toAddress, string fromAddress, string subject, string body, string bounceAddress, string replyToAddress, string ignore, bool isImmediate, bool isHTML, int loggedEmailId, ref string userErrorMessage, int personalizeAddonId) {
            try {
                // -- validate arguments
                subject = validateEmailSubject(core, subject);
                if (!verifyEmailAddress(core, toAddress)) {
                    //
                    EmailSendRequest sendRequestLog = new() {
                        attempts = 0,
                        emailContextMessage = "Adhoc Email",
                        fromAddress = fromAddress,
                        subject = subject,
                        toAddress = toAddress,
                        emailId = loggedPersonId,
                        toMemberId = 0
                    };
                    AddEmailLog(core, sendRequestLog, false, "No email sent because to-address is not valid: " + sendRequestLog.emailContextMessage);
                    //
                    //
                    userErrorMessage = "Email not sent because the to-address is not valid [" + toAddress + "].";
                    logger.Info($"{core.logCommonMessage},queueAdHocEmail, NOT SENT [" + userErrorMessage + "], toAddress [" + toAddress + "], fromAddress [" + fromAddress + "], subject [" + subject + "]");
                    return false;
                }
                if (!verifyEmailAddress(core, fromAddress)) {
                    if (!verifyEmailAddress(core, core.siteProperties.emailFromAddress)) {
                        //
                        EmailSendRequest sendRequestLog = new() {
                            attempts = 0,
                            emailContextMessage = "Adhoc Email",
                            fromAddress = fromAddress,
                            subject = subject,
                            toAddress = toAddress,
                            emailId = 0,
                            toMemberId = 0
                        };
                        AddEmailLog(core, sendRequestLog, false, "No email sent because to-address is not valid: " + sendRequestLog.emailContextMessage);
                        //
                        //
                        userErrorMessage = "Email not sent because the from-address is not valid [" + fromAddress + "].";
                        logger.Info($"{core.logCommonMessage},queueAdHocEmail, NOT SENT [" + userErrorMessage + "], toAddress [" + toAddress + "], fromAddress [" + fromAddress + "], subject [" + subject + "]");
                        return false;
                    }
                    fromAddress = core.siteProperties.emailFromAddress;
                }
                if (isOnBlockedList(core, toAddress)) {
                    //
                    EmailSendRequest sendRequest1 = new() {
                        attempts = 0,
                        emailContextMessage = "Adhoc Email",
                        fromAddress = fromAddress,
                        subject = subject,
                        toAddress = toAddress,
                        emailId = 0,
                        toMemberId = 0
                    };
                    AddEmailLog(core, sendRequest1, false, "No email sent because to-address is not valid: " + sendRequest1.emailContextMessage);
                    //
                    userErrorMessage = "Email not sent because the address [" + toAddress + "] is blocked by this application.";
                    logger.Info($"{core.logCommonMessage},queueAdHocEmail, NOT SENT [" + userErrorMessage + "], toAddress [" + toAddress + "], fromAddress [" + fromAddress + "], subject [" + subject + "]");
                    return false;
                }
                //
                // Test for from-address / to-address matches
                if (GenericController.toLCase(fromAddress) == GenericController.toLCase(toAddress)) {
                    fromAddress = core.siteProperties.getText("EmailFromAddress", core.cpParent.ServerConfig.defaultEmailContact);
                    if (string.IsNullOrEmpty(fromAddress)) {
                        //
                        //
                        //
                        fromAddress = toAddress;
                        userErrorMessage = "The from-address is blank. This email was sent, but may be blocked by spam filtering.";
                        logger.Info($"{core.logCommonMessage},queueAdHocEmail, sent with warning [" + userErrorMessage + "], toAddress [" + toAddress + "], fromAddress [" + fromAddress + "], subject [" + subject + "]");
                    } else if (GenericController.toLCase(fromAddress) == GenericController.toLCase(toAddress)) {
                        //
                        //
                        //
                        userErrorMessage = "The from-address matches the to-address [" + fromAddress + "] . This email was sent, but may be blocked by spam filtering.";
                        logger.Info($"{core.logCommonMessage},queueAdHocEmail, sent with warning [" + userErrorMessage + "], toAddress [" + toAddress + "], fromAddress [" + fromAddress + "], subject [" + subject + "]");
                    } else {
                        //
                        //
                        //
                        userErrorMessage = "The from-address matches the to-address. The from-address was changed to [" + fromAddress + "] to prevent it from being blocked by spam filtering.";
                        logger.Info($"{core.logCommonMessage},queueAdHocEmail, sent with warning [" + userErrorMessage + "], toAddress [" + toAddress + "], fromAddress [" + fromAddress + "], subject [" + subject + "]");
                    }
                }
                object bodyRenderData = getRenderData(core, personalizeAddonId);
                string subjectRendered = encodeEmailSubjectText(core, subject, null, bodyRenderData);
                string htmlBody = encodeEmailBodyHtml(core, isHTML, body, "", null, "", false, bodyRenderData);
                string textBody = HtmlController.convertHtmlToText(core, htmlBody);
                EmailSendRequest sendRequest = new() {
                    attempts = 0,
                    bounceAddress = bounceAddress,
                    fromAddress = fromAddress,
                    htmlBody = htmlBody,
                    replyToAddress = replyToAddress,
                    subject = subjectRendered,
                    textBody = textBody,
                    toAddress = toAddress,
                    emailContextMessage = emailContextMessage,
                    emailDropId = 0,
                    emailId = 0,
                    toMemberId = 0,
                };
                if (isImmediate) {
                    return trySendImmediate(core, sendRequest, ref userErrorMessage);
                }
                queueEmail(core, false, sendRequest);
                return true;
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
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
        public static bool trySendPersonEmail(CoreController core, PersonModel recipient, string fromAddress, string subject, string body, string bounceAddress, string replyToAddress, bool Immediate, bool isHTML, int emailId, string template, bool addLinkAuthToAllLinks, ref string userErrorMessage, string queryStringForLinkAppend, string emailContextMessage, int personalizeAddonId) {
            try {
                // -- validate arguments
                if (recipient == null) {
                    //
                    EmailSendRequest sendRequest1 = new() {
                        attempts = 0,
                        emailContextMessage = $"Person Email to emailId [{emailId}]",
                        fromAddress = fromAddress,
                        subject = subject,
                        toAddress = recipient.email,
                        emailId = emailId,
                        toMemberId = recipient.id
                    };
                    AddEmailLog(core, sendRequest1, false, "No email sent because to-address is not valid: " + sendRequest1.emailContextMessage);
                    //
                    userErrorMessage = "The email was not sent because the recipient is not valid [empty], fromAddress [" + fromAddress + "], subject [" + subject + "], emailId [" + emailId + "]";
                    logger.Info($"{core.logCommonMessage},tryQueuePersonEmail, NOT SENT [" + userErrorMessage + "], toAddress [null], fromAddress [" + fromAddress + "], subject [" + subject + "]");
                    return false;
                }
                subject = validateEmailSubject(core, subject);
                if (!verifyEmailAddress(core, recipient.email)) {
                    //
                    EmailSendRequest sendRequest1 = new() {
                        attempts = 0,
                        emailContextMessage = $"Person Email to emailId [{emailId}]",
                        fromAddress = fromAddress,
                        subject = subject,
                        toAddress = recipient.email,
                        emailId = emailId,
                        toMemberId = recipient.id
                    };
                    AddEmailLog(core, sendRequest1, false, "No email sent because to-address is not valid: " + sendRequest1.emailContextMessage);
                    //
                    userErrorMessage = "Email not sent because the to-address is not valid, recipient [" + recipient.id + ", " + recipient.name + "], toAddress [" + recipient.email + "], fromAddress [" + fromAddress + "], subject [" + subject + "], emailId [" + emailId + "].";
                    logger.Info($"{core.logCommonMessage},tryQueuePersonEmail, NOT SENT [" + userErrorMessage + "], toAddress [" + recipient.email + "], fromAddress [" + fromAddress + "], subject [" + subject + "]");
                    return false;
                }
                if (!verifyEmailAddress(core, fromAddress)) {
                    if (!verifyEmailAddress(core, core.siteProperties.emailFromAddress)) {
                        //
                        EmailSendRequest sendRequest1 = new() {
                            attempts = 0,
                            emailContextMessage = $"Person Email to emailId [{emailId}]",
                            fromAddress = fromAddress,
                            subject = subject,
                            toAddress = recipient.email,
                            emailId = emailId,
                            toMemberId = recipient.id
                        };
                        AddEmailLog(core, sendRequest1, false, "No email sent because from-address is not valid: " + sendRequest1.emailContextMessage);
                        //
                        userErrorMessage = "Email not sent because the from-address is not valid, recipient [" + recipient.id + ", " + recipient.name + "], toAddress [" + recipient.email + "], fromAddress [" + fromAddress + "], subject [" + subject + "], emailId [" + emailId + "].";
                        logger.Info($"{core.logCommonMessage},tryQueuePersonEmail, NOT SENT [" + userErrorMessage + "], toAddress [" + recipient.email + "], fromAddress [" + fromAddress + "], subject [" + subject + "]");
                        return false;
                    }
                    fromAddress = core.siteProperties.emailFromAddress;
                }
                if (isOnBlockedList(core, recipient.email)) {
                    //
                    EmailSendRequest sendRequest1 = new() {
                        attempts = 0,
                        emailContextMessage = $"Person Email to emailId [{emailId}]",
                        fromAddress = fromAddress,
                        subject = subject,
                        toAddress = recipient.email,
                        emailId = emailId,
                        toMemberId = recipient.id
                    };
                    AddEmailLog(core, sendRequest1, false, "No email sent because to-address is on blocked list: " + sendRequest1.emailContextMessage);
                    //
                    //
                    userErrorMessage = "Email not sent because the to-address requested email block. See the Email Bounce List, recipient [" + recipient.id + ", " + recipient.name + "], toAddress [" + recipient.email + "], fromAddress [" + fromAddress + "], subject [" + subject + "], emailId [" + emailId + "].";
                    logger.Info($"{core.logCommonMessage},tryQueuePersonEmail, NOT SENT [" + userErrorMessage + "], toAddress [" + recipient.email + "], fromAddress [" + fromAddress + "], subject [" + subject + "]");
                    return false;
                }
                object bodyRenderData = getRenderData(core, personalizeAddonId);
                string subjectRendered = encodeEmailSubjectText(core, subject, recipient, bodyRenderData);
                string htmlBody = encodeEmailBodyHtml(core, isHTML, body, template, recipient, queryStringForLinkAppend, addLinkAuthToAllLinks, bodyRenderData);
                string textBody = HtmlController.convertHtmlToText(core, htmlBody);
                string recipientName = (!string.IsNullOrWhiteSpace(recipient.name) && !recipient.name.ToLower().Equals("guest")) ? recipient.name : string.Empty;
                if (string.IsNullOrWhiteSpace(recipientName)) {
                    recipientName = ""
                        + ((!string.IsNullOrWhiteSpace(recipient.firstName) && !recipient.firstName.ToLower().Equals("guest")) ? recipient.firstName : string.Empty)
                        + " "
                        + ((!string.IsNullOrWhiteSpace(recipient.lastName) && !recipient.lastName.ToLower().Equals("guest")) ? recipient.lastName : string.Empty);
                }
                recipientName = recipientName.Trim();
                string toAddress = "";
                if (recipient.email.Contains("<")) {
                    //
                    // -- person record include friendly email format
                    toAddress = recipient.email;
                } else {
                    //
                    // -- people record includes simple email format
                    toAddress = (string.IsNullOrWhiteSpace(recipientName)) ? recipient.email : "\"" + recipientName.Replace("\"", "") + "\" <" + recipient.email.Trim() + ">";
                }
                var sendRequest = new EmailSendRequest {
                    attempts = 0,
                    bounceAddress = bounceAddress,
                    emailId = emailId,
                    fromAddress = fromAddress,
                    htmlBody = htmlBody,
                    replyToAddress = replyToAddress,
                    subject = subjectRendered,
                    textBody = textBody,
                    toAddress = toAddress,
                    toMemberId = recipient.id,
                    emailContextMessage = emailContextMessage,
                    emailDropId = 0
                };
                if (!tryIsValidEmail(core, sendRequest, ref userErrorMessage)) {
                    //
                    // -- error sending (logging handled in tryIsValidEmail)
                    //
                    return false;
                }
                if (Immediate) {
                    //
                    // -- send immediate
                    return trySendImmediate(core, sendRequest, ref userErrorMessage);
                }
                //
                // -- add to queue
                queueEmail(core, false, sendRequest);
                return true;
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
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
        public static bool trySendPersonEmail(CoreController core, string emailContextMessage, PersonModel person, string fromAddress, string subject, string body, string bounceAddress, string replyToAddress, bool Immediate, bool isHTML, int emailIdOrZeroForLog, string template, bool EmailAllowLinkEID, ref string returnSendStatus, int personalizeAddonId) {
            return trySendPersonEmail(core, person, fromAddress, subject, body, bounceAddress, replyToAddress, Immediate, isHTML, emailIdOrZeroForLog, template, EmailAllowLinkEID, ref returnSendStatus, "", emailContextMessage, personalizeAddonId);
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
        public static bool trySendPersonEmail(CoreController core, string emailContextMessage, PersonModel person, string fromAddress, string subject, string body, string bounceAddress, string replyToAddress, bool Immediate, bool isHTML, int emailIdOrZeroForLog, string template, bool EmailAllowLinkEID, int personalizeAddonId) {
            string returnSendStatus = "";
            return trySendPersonEmail(core, person, fromAddress, subject, body, bounceAddress, replyToAddress, Immediate, isHTML, emailIdOrZeroForLog, template, EmailAllowLinkEID, ref returnSendStatus, "", emailContextMessage, personalizeAddonId);
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
        public static bool trySendSystemEmail(CoreController core, bool immediate, string emailName, string appendedCopy, int additionalMemberID, ref string userErrorMessage) {
            if (string.IsNullOrEmpty(emailName)) {
                //
                EmailSendRequest sendRequest1 = new() {
                    attempts = 0,
                    emailContextMessage = $"System Email to emailName [{emailName}]",
                    fromAddress = "",
                    subject = "",
                    toAddress = "",
                    emailId = 0,
                    toMemberId = additionalMemberID
                };
                AddEmailLog(core, sendRequest1, false, "No email sent because email name is blank: " + sendRequest1.emailContextMessage);
                //
                return false;
            }
            {
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
                        logger.Error($"{core.logCommonMessage}", new GenericException("No system email was found with the name [" + emailName + "]. A new email blank was created but not sent."));
                    }
                }
                return trySendSystemEmail(core, immediate, email, appendedCopy, additionalMemberID, ref userErrorMessage);
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
        public static bool trySendSystemEmail(CoreController core, bool immediate, string emailName, string appendedCopy, int additionalMemberID) {
            string userErrorMessage = "";
            return trySendSystemEmail(core, immediate, emailName, appendedCopy, additionalMemberID, ref userErrorMessage);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="emailName"></param>
        /// <param name="appendedCopy"></param>
        /// <returns></returns>
        public static bool trySendSystemEmail(CoreController core, bool immediate, string emailName, string appendedCopy) {
            string userErrorMessage = "";
            return trySendSystemEmail(core, immediate, emailName, appendedCopy, 0, ref userErrorMessage);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="emailName"></param>
        /// <returns></returns>
        public static bool trySendSystemEmail(CoreController core, bool immediate, string emailName) {
            string userErrorMessage = "";
            return trySendSystemEmail(core, immediate, emailName, "", 0, ref userErrorMessage);
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
        public static bool trySendSystemEmail(CoreController core, bool immediate, int emailid, string appendedCopy, int additionalMemberID, ref string userErrorMessage) {
            //
            // -- argument check. if emailid is 0, the configuration is set to not send, and this should not have been called. If non-zero and no email found, that is a data error.
            if (emailid == 0) {
                //
                EmailSendRequest sendRequest = new() {
                    attempts = 0,
                    emailContextMessage = "System Email",
                    fromAddress = "",
                    subject = "",
                    toAddress = "",
                    emailId = 0,
                    toMemberId = 0
                };
                AddEmailLog(core, sendRequest, false, "No email sent because the system email is not valid: " + sendRequest.emailContextMessage);
                //
                userErrorMessage = "The notification email could not be sent.";
                logger.Error($"{core.logCommonMessage}", new GenericException("No system email was found with the id [" + emailid + "]"));
                return false;
            }
            //
            SystemEmailModel email = DbBaseModel.create<SystemEmailModel>(core.cpParent, emailid);
            if (email == null) {
                //
                EmailSendRequest sendRequest = new() {
                    attempts = 0,
                    emailContextMessage = "System Email",
                    fromAddress = "",
                    subject = "",
                    toAddress = "",
                    emailId = 0,
                    toMemberId = 0
                };
                AddEmailLog(core, sendRequest, false, "No email sent because the system email is not valid: " + sendRequest.emailContextMessage);
                //
                userErrorMessage = "The notification email could not be sent.";
                logger.Error($"{core.logCommonMessage}", new GenericException("No system email was found with the id [" + emailid + "]"));
                return false;
            }
            return trySendSystemEmail(core, immediate, email, appendedCopy, additionalMemberID, ref userErrorMessage);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="emailid"></param>
        /// <param name="appendedCopy"></param>
        /// <param name="additionalMemberID"></param>
        /// <returns></returns>
        public static bool trySendSystemEmail(CoreController core, bool immediate, int emailid, string appendedCopy, int additionalMemberID) {
            string userErrorMessage = "";
            return trySendSystemEmail(core, immediate, emailid, appendedCopy, additionalMemberID, ref userErrorMessage);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="emailid"></param>
        /// <param name="appendedCopy"></param>
        /// <returns></returns>
        public static bool trySendSystemEmail(CoreController core, bool immediate, int emailid, string appendedCopy) {
            string userErrorMessage = "";
            return trySendSystemEmail(core, immediate, emailid, appendedCopy, 0, ref userErrorMessage);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="emailid"></param>
        /// <returns></returns>
        public static bool trySendSystemEmail(CoreController core, bool immediate, int emailid) {
            string userErrorMessage = "";
            return trySendSystemEmail(core, immediate, emailid, "", 0, ref userErrorMessage);
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
        /// <param name="userErrorMessage"></param>
        /// <returns>Admin message if something went wrong (email addresses checked, etc.</returns>
        public static bool trySendSystemEmail(CoreController core, bool immediate, SystemEmailModel email, string appendedCopy, int additionalMemberID, ref string userErrorMessage) {
            try {
                // -- validate arguments
                // -- an email with no subject is still valid
                //if (!verifyEmailSubject(core, email.subject)) {
                //    userErrorMessage = "The email subject is not valid.";
                //    return false;
                //}
                if (!verifyEmailAddress(core, email.fromAddress)) {
                    if (!verifyEmailAddress(core, core.siteProperties.emailFromAddress)) {
                        //
                        EmailSendRequest sendRequest = new() {
                            attempts = 0,
                            emailContextMessage = $"System Email [{email.id}, {email.name}]",
                            fromAddress = $"[{email.fromAddress}], default from address [{core.siteProperties.emailFromAddress}]",
                            subject = email.subject,
                            toAddress = "",
                            emailId = email.id,
                            toMemberId = 0
                        };
                        AddEmailLog(core, sendRequest, false, "No email sent because from-address is not valid: " + sendRequest.emailContextMessage);
                        //
                        userErrorMessage = $"Email not sent because the from-address is not valid [{email.fromAddress}].";
                        logger.Info($"{core.logCommonMessage},tryQueueSystemEmail, NOT SENT [" + userErrorMessage + "], email [" + email.name + "], fromAddress [" + email.fromAddress + "], subject [" + email.subject + "]");
                        return false;
                    }
                    email.fromAddress = core.siteProperties.emailFromAddress;
                }
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
                        //
                        EmailSendRequest sendRequest = new() {
                            attempts = 0,
                            emailContextMessage = $"System Email [{email.id}, {email.name}]",
                            fromAddress = $"[{email.fromAddress}], default from address [{core.siteProperties.emailFromAddress}]",
                            subject = email.subject,
                            toAddress = "",
                            emailId = email.id,
                            toMemberId = 0
                        };
                        AddEmailLog(core, sendRequest, false, $"Email not sent to additional user [{additionalMemberID}] because that user was not found." + sendRequest.emailContextMessage);
                        //
                        confirmationMessage.Append("&nbsp;&nbsp;Error: Not sent to additional user [#" + additionalMemberID + "] because the user record could not be found." + BR);
                    } else {
                        if (string.IsNullOrWhiteSpace(person.email)) {
                            //
                            EmailSendRequest sendRequest = new() {
                                attempts = 0,
                                emailContextMessage = $"System Email [{email.id}, {email.name}]",
                                fromAddress = $"[{email.fromAddress}], default from address [{core.siteProperties.emailFromAddress}]",
                                subject = email.subject,
                                toAddress = "",
                                emailId = email.id,
                                toMemberId = 0
                            };
                            AddEmailLog(core, sendRequest, false, $"Email not sent to additional user [{additionalMemberID}] has a blank email address." + sendRequest.emailContextMessage);
                            //
                            confirmationMessage.Append("&nbsp;&nbsp;Error: Not sent to additional user [#" + additionalMemberID + "] because their email address was blank." + BR);
                        } else if (!verifyEmailAddress(core, person.email)) {
                            //
                            EmailSendRequest sendRequest = new() {
                                attempts = 0,
                                emailContextMessage = $"System Email [{email.id}, {email.name}]",
                                fromAddress = $"[{email.fromAddress}], default from address [{core.siteProperties.emailFromAddress}]",
                                subject = email.subject,
                                toAddress = "",
                                emailId = email.id,
                                toMemberId = 0
                            };
                            AddEmailLog(core, sendRequest, false, $"Email not sent to additional user [{additionalMemberID}] does not have a valid email address." + sendRequest.emailContextMessage);
                            //
                            confirmationMessage.Append("&nbsp;&nbsp;Error: Not sent to additional user [#" + additionalMemberID + "] because their email address was invalid [" + person.email + "]." + BR);
                        } else {
                            string EmailStatus = "";
                            string queryStringForLinkAppend = "";
                            trySendPersonEmail(core, person, email.fromAddress, EmailSubjectSource, EmailBodySource, "", "", immediate, true, emailRecordId, EmailTemplateSource, emailAllowLinkEId, ref EmailStatus, queryStringForLinkAppend, "System Email", email.personalizeAddonId);
                            //
                            confirmationMessage.Append("&nbsp;&nbsp;Sent to " + person.name + " at " + person.email + ", Status = " + EmailStatus + BR);
                            //
                            LogController.addActivityCompleted(core, "System email sent", "System email sent [" + email.name + "]", person.id, (int)ActivityLogModel.ActivityLogTypeEnum.EmailTo);
                        }
                    }
                }
                //
                // --- Send message to everyone selected
                //
                confirmationMessage.Append(BR + "Recipients in selected System Email groups:" + BR);
                List<int> peopleIdList = PersonModel.createidListForEmail(core.cpParent, emailRecordId);
                if (peopleIdList.Count == 0) {
                    //
                    EmailSendRequest sendRequest = new() {
                        attempts = 0,
                        emailContextMessage = $"System Email [{email.id}, {email.name}]",
                        fromAddress = $"[{email.fromAddress}], default from address [{core.siteProperties.emailFromAddress}]",
                        subject = email.subject,
                        toAddress = "",
                        emailId = email.id,
                        toMemberId = 0
                    };
                    AddEmailLog(core, sendRequest, false, "No email sent because no people are in the group(s): " + sendRequest.emailContextMessage);
                    //
                    // -- do not return, let the confirmation message be sent to the admin
                }
                List<string> usedEmail = new();
                foreach (var personId in peopleIdList) {
                    var person = DbBaseModel.create<PersonModel>(core.cpParent, personId);
                    if (person == null) {
                        confirmationMessage.Append("&nbsp;&nbsp;Error: Not sent to user [#" + personId + "] because the user record could not be found." + BR);
                        continue;
                    }
                    string simpleEmail = EmailController.getSimpleEmailFromFriendlyEmail(core.cpParent, person.email);
                    if (string.IsNullOrWhiteSpace(simpleEmail)) {
                        confirmationMessage.Append("&nbsp;&nbsp;Error: Not sent to user [#" + personId + "] because their email address was blank." + BR);
                        continue;
                    }
                    if (!verifyEmailAddress(core, simpleEmail)) {
                        confirmationMessage.Append("&nbsp;&nbsp;Error: Not sent to user [#" + personId + "] because their email address was invalid [" + simpleEmail + "]." + BR);
                        continue;
                    }
                    if (usedEmail.Contains(simpleEmail)) {
                        continue;
                    }
                    usedEmail.Add(simpleEmail);
                    string EmailStatus = "";
                    string queryStringForLinkAppend = "";
                    trySendPersonEmail(core, person, email.fromAddress, EmailSubjectSource, EmailBodySource, "", "", immediate, true, emailRecordId, EmailTemplateSource, emailAllowLinkEId, ref EmailStatus, queryStringForLinkAppend, "System Email", email.personalizeAddonId);
                    confirmationMessage.Append("&nbsp;&nbsp;Sent to " + person.name + " at " + person.email + ", Status = " + EmailStatus + BR);
                    //
                    LogController.addActivityCompleted(core, "System email sent", "System email sent [" + email.name + "]", person.id, (int)ActivityLogModel.ActivityLogTypeEnum.EmailTo);
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
                        trySendPersonEmail(core, person, email.fromAddress, "System Email confirmation from " + core.appConfig.domainList[0], ConfirmBody, "", "", immediate, true, emailRecordId, "", false, ref EmailStatus, queryStringForLinkAppend, "System Email", email.personalizeAddonId);
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
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
        public static void sendConfirmationTestEmail(CoreController core, int EmailID, int confirmationMemberId) {
            try {
                // todo -- convert to model
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
                        if (CSTemplate.ok()) {
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
                    var DupList = new StringBuilder();
                    int BadCnt = 0;
                    var BadList = new StringBuilder();
                    string TotalList = "";
                    int contentControlId = csData.getInteger("contentControlId");
                    bool isGroupEmail = contentControlId.Equals(ContentMetadataModel.getContentId(core, "Group Email"));
                    var personIdList = PersonModel.createidListForEmail(core.cpParent, EmailID);
                    if (isGroupEmail && personIdList.Count.Equals(0)) {
                        ErrorController.addUserError(core, "There are no valid recipients of this email other than the confirmation address. Either no groups or topics were selected, or those selections contain no people with both a valid email addresses and 'Allow Group Email' enabled.");
                    } else {
                        List<string> usedEmails = new();
                        foreach (var personId in personIdList) {
                            var person = DbBaseModel.create<PersonModel>(core.cpParent, personId);
                            string simpleEmail = getSimpleEmailFromFriendlyEmail(core.cpParent, person.email);
                            string emailPersonName = person.name;
                            if (string.IsNullOrEmpty(emailPersonName)) {
                                emailPersonName = "no name (member id " + person.id + ")";
                            }
                            string EmailLine = person.email + " for " + emailPersonName;
                            if (string.IsNullOrEmpty(simpleEmail)) {
                                //
                                // -- blank email
                                BlankCnt += 1;
                            } else {
                                if (usedEmails.Contains(simpleEmail)) {
                                    //
                                    // -- dup
                                    DupCnt += 1;
                                    DupList.Append("<div class=i>" + person.email + "</div>" + BR);
                                } else {
                                    //
                                    // -- not dup
                                    usedEmails.Add(simpleEmail);
                                }
                            }
                            int EmailLen = simpleEmail.Length;
                            int Posat = GenericController.strInstr(1, simpleEmail, "@");
                            if (EmailLen < 6) {
                                BadCnt += 1;
                                BadList.Append(EmailLine + BR);
                            } else if ((Posat < 4) || (Posat > (EmailLen - 4))) {
                                BadCnt += 1;
                                BadList.Append(EmailLine + BR);
                            }
                            TotalList = TotalList + EmailLine + BR;
                            TotalCnt += 1;
                        }
                    }
                    string ConfirmFooter = "";
                    //
                    if (DupCnt == 1) {
                        ErrorController.addUserError(core, "There is 1 duplicate email address. See the test email for details.");
                        ConfirmFooter = ConfirmFooter + "<div style=\"clear:all\">WARNING: There is 1 duplicate email address. Only one email will be sent to each address. If the email includes personalization, or if you are using link authentication to automatically log in the user, you may want to correct duplicates to be sure the email is created correctly.<div style=\"margin:20px;\">" + DupList.ToString() + "</div></div>";
                    } else if (DupCnt > 1) {
                        ErrorController.addUserError(core, "There are " + DupCnt + " duplicate email addresses. See the test email for details");
                        ConfirmFooter = ConfirmFooter + "<div style=\"clear:all\">WARNING: There are " + DupCnt + " duplicate email addresses. Only one email will be sent to each address. If the email includes personalization, or if you are using link authentication to automatically log in the user, you may want to correct duplicates to be sure the email is created correctly.<div style=\"margin:20px;\">" + DupList.ToString() + "</div></div>";
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
                        ConfirmFooter += "<div style=\"clear:all\">WARNING: There is 1 blank email address.</div>";
                    } else if (BlankCnt > 1) {
                        ErrorController.addUserError(core, "There are " + DupCnt + " blank email addresses. See the test email for details.");
                        ConfirmFooter = ConfirmFooter + "<div style=\"clear:all\">WARNING: There are " + BlankCnt + " blank email addresses.</div>";
                    }
                    //
                    if (TotalCnt == 0) {
                        ConfirmFooter += "<div style=\"clear:all\">WARNING: There are no recipients for this email.</div>";
                    } else if (TotalCnt == 1) {
                        ConfirmFooter += "<div style=\"clear:all\">There is 1 recipient<div style=\"margin:20px;\">" + TotalList + "</div></div>";
                    } else {
                        ConfirmFooter += "<div style=\"clear:all\">There are " + TotalCnt + " recipients<div style=\"margin:20px;\">" + TotalList + "</div></div>";
                    }
                    //
                    if (confirmationMemberId == 0) {
                        ErrorController.addUserError(core, "No confirmation email was send because a Confirmation member is not selected");
                    } else {
                        PersonModel person = DbBaseModel.create<PersonModel>(core.cpParent, confirmationMemberId);
                        if (person == null) {
                            ErrorController.addUserError(core, "No confirmation email was send because a Confirmation member is not selected");
                        } else {
                            EmailBody += "<div style=\"clear:both;padding:10px;margin:10px;border:1px dashed #888;\">Administrator<br><br>" + ConfirmFooter + "</div>";
                            string queryStringForLinkAppend = "";
                            string sendStatus = "";
                            if (!trySendPersonEmail(core, person, csData.getText("FromAddress"), csData.getText("Subject"), EmailBody, "", "", true, true, EmailID, EmailTemplate, false, ref sendStatus, queryStringForLinkAppend, "Test Email", csData.getInteger("personalizeAddonId"))) {
                                ErrorController.addUserError(core, sendStatus);
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
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
        /// <param name="subject"></param>
        /// <param name="userErrorMessage"></param>
        public static bool trySendFormEmail(CoreController core, string toAddress, string fromAddress, string subject, out string userErrorMessage) {
            try {
                // -- validate arguments
                if (!verifyEmailAddress(core, toAddress)) {
                    //
                    EmailSendRequest sendRequest = new() {
                        attempts = 0,
                        emailContextMessage = "Form Email",
                        fromAddress = fromAddress,
                        subject = subject,
                        toAddress = toAddress,
                        emailId = 0,
                        toMemberId = 0
                    };
                    AddEmailLog(core, sendRequest, false, "No email sent because to-address is not valid: " + sendRequest.emailContextMessage);
                    //
                    userErrorMessage = "The to-address [" + toAddress + "] is not valid.";
                    return false;
                }
                subject = validateEmailSubject(core, subject);
                userErrorMessage = "";
                string Message = "";
                string emailSubjectWorking = subject;
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
                // todo -- add personalization
                int personalizeAddonId = 0;
                return sendAdHocEmail(core, "Form Submission Email", core.session.user.id, toAddress, fromAddress, emailSubjectWorking, Message, "", "", "", false, false, 0, ref userErrorMessage, personalizeAddonId);
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
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
        public static bool trySendGroupEmail(CoreController core, string groupCommaList, string fromAddress, string subject, string body, bool isImmediate, bool isHtml, ref string userErrorMessage, int personalizeAddonId) {
            try {
                if (string.IsNullOrWhiteSpace(groupCommaList)) {
                    //
                    EmailSendRequest sendRequest = new() {
                        attempts = 0,
                        bounceAddress = "",
                        emailContextMessage = "Group Email",
                        fromAddress = fromAddress,
                        replyToAddress = "",
                        subject = subject,
                        toAddress = "",
                        emailDropId = 0,
                        emailId = 0,
                        htmlBody = body,
                        textBody = "",
                        toMemberId = 0
                    };
                    AddEmailLog(core, sendRequest, false, "No email sent because no groups provided for to-address: " + sendRequest.emailContextMessage);
                    //
                    return true;
                }
                return trySendGroupEmail(core, groupCommaList.Split(',').ToList<string>().FindAll(t => !string.IsNullOrWhiteSpace(t)), fromAddress, subject, body, isImmediate, isHtml, ref userErrorMessage, personalizeAddonId);
            } catch (Exception ex) {
                //
                EmailSendRequest sendRequest = new() {
                    attempts = 0,
                    emailContextMessage = "Group Email",
                    fromAddress = fromAddress,
                    subject = subject,
                    toAddress = "",
                    emailId = 0,
                    htmlBody = "",
                    textBody = "",
                    toMemberId = 0
                };
                AddEmailLog(core, sendRequest, false, "No email sent because there was an exception: " + sendRequest.emailContextMessage);
                //
                logger.Error(ex, $"{core.logCommonMessage}");
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
        public static bool trySendGroupEmail(CoreController core, string groupCommaList, string fromAddress, string subject, string body, bool isImmediate, bool isHtml, int personalizeAddonId) {
            string userErrorMessage = "";
            return trySendGroupEmail(core, groupCommaList, fromAddress, subject, body, isImmediate, isHtml, ref userErrorMessage, personalizeAddonId);
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
        public static bool trySendGroupEmail(CoreController core, List<string> groupNameList, string fromAddress, string subject, string body, bool isImmediate, bool isHtml, ref string userErrorMessage, int personalizeAddonId) {
            try {
                if (groupNameList.Count <= 0) {
                    EmailSendRequest sendRequest = new() {
                        attempts = 0,
                        bounceAddress = "",
                        emailContextMessage = "Group Email",
                        fromAddress = fromAddress,
                        replyToAddress = "",
                        subject = subject,
                        toAddress = "",
                        emailDropId = 0,
                        emailId = 0,
                        htmlBody = body,
                        textBody = "",
                        toMemberId = 0
                    };
                    AddEmailLog(core, sendRequest, true, "No email sent because no groups provided for to-address: " + sendRequest.emailContextMessage);
                    return true;
                }
                foreach (var person in PersonModel.createListFromGroupNameList(core.cpParent, groupNameList, true)) {
                    trySendPersonEmail(core, "Group Email", person, fromAddress, subject, body, "", "", isImmediate, isHtml, 0, "", false, personalizeAddonId);
                }
                return true;
            } catch (Exception ex) {
                //
                EmailSendRequest sendRequest = new() {
                    attempts = 0,
                    bounceAddress = "",
                    emailContextMessage = "Group Email",
                    fromAddress = fromAddress,
                    replyToAddress = "",
                    subject = subject,
                    toAddress = "",
                    emailDropId = 0,
                    emailId = 0,
                    htmlBody = body,
                    textBody = "",
                    toMemberId = 0
                };
                AddEmailLog(core, sendRequest, false, "No email sent because there was an exception: " + sendRequest.emailContextMessage);
                //
                logger.Error(ex, $"{core.logCommonMessage}");
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
        public static bool trySendGroupEmail(CoreController core, List<int> groupIdList, string fromAddress, string subject, string body, bool isImmediate, bool isHtml, ref string userErrorMessage, int personalizeAddonId) {
            try {
                if (groupIdList.Count <= 0) { return true; }
                foreach (var person in PersonModel.createListFromGroupIdList(core.cpParent, groupIdList, true)) {
                    trySendPersonEmail(core, "Group Email", person, fromAddress, subject, body, "", "", isImmediate, isHtml, 0, "", false, personalizeAddonId);
                }
                return true;
            } catch (Exception ex) {
                //
                EmailSendRequest sendRequest = new() {
                    attempts = 0,
                    emailContextMessage = "Group Email",
                    fromAddress = fromAddress,
                    subject = subject,
                    toAddress = "",
                    emailId = 0,
                    toMemberId = 0
                };
                AddEmailLog(core, sendRequest, false, "No email sent because there was an exception: " + sendRequest.emailContextMessage);
                //
                logger.Error(ex, $"{core.logCommonMessage}");
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
        /// <param name="frontOfQueue">Add this email to the front of the send queue</param>
        /// <param name="email"></param>
        /// <param name="emailContextMessage">A short description of the email (Conditional Email, Group Email, Confirmation for Group Email, etc.)</param>
        private static void queueEmail(CoreController core, bool frontOfQueue, EmailSendRequest email) {
            try {
                var emailQueue = EmailQueueModel.addEmpty<EmailQueueModel>(core.cpParent);
                emailQueue.name = email.emailContextMessage;
                emailQueue.immediate = frontOfQueue;
                emailQueue.toAddress = email.toAddress;
                emailQueue.subject = email.subject;
                emailQueue.content = SerializeObject(email);
                emailQueue.attempts = email.attempts;
                emailQueue.save(core.cpParent);
                logger.Info($"{core.logCommonMessage},queueEmail, toAddress [" + email.toAddress + "], fromAddress [" + email.fromAddress + "], subject [" + email.subject + "]");
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// The key from the sending process that is assigned to this entry. 
        /// A record is assigned to a key for a period, defined by the expiration. 
        /// Only this process can send the record, and only for that period.
        /// </summary>
        /// <param name="core"></param>
        public static void sendImmediateFromQueue(CoreController core) {
            try {
                //
                // -- create a process key and process expiration
                string processKey = GenericController.getGUID();
                DateTime processExpiration = DateTime.Now.AddMinutes(1);
                //
                // -- clear key for records that expired over a minute ago,then mark a group of records for this process
                EmailQueueModel.clearExpiredKeys(core.cpParent, DateTime.Now.AddMinutes(-1));
                EmailQueueModel.setProcessKey(core.cpParent, processKey, processExpiration, 100);
                List<EmailQueueModel> queueEmailList = EmailQueueModel.selectRecordsToSend(core.cpParent, processKey, 100);
                if (queueEmailList.Count == 0) { return; }
                //
                using var sesClient = AwsSesController.getSesClient(core);
                foreach (EmailQueueModel queueEmail in queueEmailList) {
                    //
                    // -- this queue record is not shared with another process, send it
                    DbBaseModel.delete<EmailQueueModel>(core.cpParent, queueEmail.id);
                    EmailSendRequest sendRequest = Newtonsoft.Json.JsonConvert.DeserializeObject<EmailSendRequest>(queueEmail.content);
                    string reasonForFail = "";
                    trySendImmediate(core, sendRequest, ref reasonForFail);
                }
            } catch (Exception ex) {
                core.cpParent.Site.ErrorReport(ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Send email in email queue. Log errors and swallow, as this process is an async from the sending client
        /// </summary>
        /// <param name="core"></param>
        public static void sendImmediateFromQueue_Legacy(CoreController core) {
            //
            // -- only send a limited number (100?) and exit so if there is only one task running, sending email will not block all other processes
            // -- make it thread safe(r), from the samples, mark one record and read back marked record. Still exposed to a second process selecting the target-marked email before this process deletes
            // -- get a list of queue records that need to be sent right now. 
            // -- One at a time mark the first for this one sending process
            // -- read back the marked record and if it is there, then no other process is likely looking at it so it can be sent
            // -- this will help prevent duplicate sends, and if the process aborts, only one queued email per queue will be stuck
            List<EmailQueueModel> queueSampleList = DbBaseModel.createList<EmailQueueModel>(core.cpParent, "", "immediate,id asc", 100, 1);
            if (queueSampleList.Count == 0) { return; }
            //
            using var sesClient = AwsSesController.getSesClient(core);
            foreach (EmailQueueModel queueSample in queueSampleList) {
                //
                // -- mark the current sample and select back asa target if it marked, send or skip
                string targetGuid = GenericController.getGUID();
                core.db.update(EmailQueueModel.tableMetadata.tableNameLower, "(ccguid=" + DbController.encodeSQLText(queueSample.ccguid) + ")", new System.Collections.Specialized.NameValueCollection { { "ccguid", DbController.encodeSQLText(targetGuid) } });
                EmailQueueModel targetQueueRecord = DbBaseModel.create<EmailQueueModel>(core.cpParent, targetGuid);
                if (targetQueueRecord != null) {
                    //
                    // -- this queue record is not shared with another process, send it
                    DbBaseModel.delete<EmailQueueModel>(core.cpParent, targetQueueRecord.id);
                    EmailSendRequest sendRequest = Newtonsoft.Json.JsonConvert.DeserializeObject<EmailSendRequest>(targetQueueRecord.content);
                    string reasonForFail = "";
                    trySendImmediate(core, sendRequest, ref reasonForFail);
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Send the emails in the current Queue
        /// </summary>
        public static bool trySendImmediate(CoreController core, EmailSendRequest sendRequest, ref string userErrorMessage) {
            try {
                sendRequest.subject = validateEmailSubject(core, sendRequest.subject);
                if (!verifyEmailAddress(core, sendRequest.fromAddress)) {
                    if (!verifyEmailAddress(core, core.siteProperties.emailFromAddress)) {
                        //
                        // -- from address is reasonForFail
                        AddEmailLog(core, sendRequest, false, "No email sent because to-address is not valid: " + sendRequest.emailContextMessage);
                        //
                        userErrorMessage = $"email from-address is invalid [{sendRequest.fromAddress}]";
                        logger.Info($"{core.logCommonMessage},FAIL send emai:" + userErrorMessage);
                        return false;
                    }
                    sendRequest.fromAddress = core.siteProperties.emailFromAddress;
                }
                //
                // -- do the actual send
                if (!core.serverConfig.productionEnvironment && core.siteProperties.blockNonProductionEmail) {
                    //
                    // -- block send
                    // -- non-production server and email blocking enabled
                    AddEmailLog(core, sendRequest, true, $"Email blocked because non-production server and site setting, email, blocking true. context [{sendRequest.emailContextMessage}]");
                    logger.Info($"{core.logCommonMessage},Email blocked because non-production server and site setting, email, blocking true., toAddress [" + sendRequest.toAddress + "], fromAddress [" + sendRequest.fromAddress + "], subject [" + sendRequest.subject + "]");
                    return true;
                } else {
                    //
                    // -- allow send.
                    // -- either production server or email not blocked
                    if (!string.IsNullOrEmpty(core.siteProperties.testEmailAddress)) {
                        //
                        // -- override to test address
                        sendRequest.toAddress = core.siteProperties.testEmailAddress;
                    }
                    bool sendWithSES = core.siteProperties.getBoolean(Constants.sitePropertyName_SendEmailWithAmazonSES);
                    bool sendSuccess = false;
                    if (sendWithSES) {
                        //
                        // -- send with Amazon SES
                        sendSuccess = AwsSesController.send(core, sendRequest, ref userErrorMessage);
                    } else {
                        //
                        // --fall back to SMTP
                        sendSuccess = EmailSmtpController.send(core, sendRequest, ref userErrorMessage);
                    }
                    if (sendSuccess) {
                        //
                        // -- success, log the send
                        AddEmailLog(core, sendRequest, true, "Successfully sent: " + sendRequest.emailContextMessage);
                        logger.Info($"{core.logCommonMessage},sendEmailInQueue, send successful, toAddress [" + sendRequest.toAddress + "], fromAddress [" + sendRequest.fromAddress + "], subject [" + sendRequest.subject + "]");
                        return true;
                    }
                }
                //
                // -- fail, retry
                if (sendRequest.attempts >= 3) {
                    //
                    // -- too many retries, log error
                    string sendStatus = "Failed after 3 retries, reason [" + userErrorMessage + "]";
                    sendStatus = sendStatus.Substring(0, (sendStatus.Length > 254) ? 254 : sendStatus.Length);
                    var log = EmailLogModel.addDefault<EmailLogModel>(core.cpParent);
                    log.name = "Aborting unsuccessful send: " + sendRequest.emailContextMessage;
                    log.toAddress = sendRequest.toAddress;
                    log.fromAddress = sendRequest.fromAddress;
                    log.subject = sendRequest.subject;
                    log.body = sendRequest.htmlBody;
                    log.sendStatus = sendStatus;
                    log.logType = EmailLogTypeImmediateSend;
                    log.emailId = sendRequest.emailId;
                    log.memberId = sendRequest.toMemberId;
                    log.save(core.cpParent);
                    logger.Info($"{core.logCommonMessage},sendEmailInQueue, send FAILED [" + userErrorMessage + "], NOT resent because too many retries, toAddress [" + sendRequest.toAddress + "], fromAddress [" + sendRequest.fromAddress + "], subject [" + sendRequest.subject + "], attempts [" + sendRequest.attempts + "]");
                    return false;
                }
                //
                // -- fail, add back to end of queue for retry
                {
                    string sendStatus = "Retrying unsuccessful send (" + sendRequest.attempts + " of 3), reason [" + userErrorMessage + "]";
                    sendStatus = sendStatus.Substring(0, (sendStatus.Length > 254) ? 254 : sendStatus.Length);
                    sendRequest.attempts += 1;
                    var log = DbBaseModel.addDefault<EmailLogModel>(core.cpParent);
                    log.name = ("Failed send queued for retry: " + sendRequest.emailContextMessage).substringSafe(0, 254);
                    log.toAddress = sendRequest.toAddress;
                    log.fromAddress = sendRequest.fromAddress;
                    log.subject = sendRequest.subject.substringSafe(0, 254);
                    log.body = sendRequest.htmlBody;
                    log.sendStatus = sendStatus;
                    log.logType = EmailLogTypeImmediateSend;
                    log.emailId = sendRequest.emailId;
                    log.memberId = sendRequest.toMemberId;
                    log.save(core.cpParent);
                    queueEmail(core, false, sendRequest);
                    logger.Info($"{core.logCommonMessage},sendEmailInQueue, failed attempt (" + sendRequest.attempts + " of 3), reason [" + userErrorMessage + "], added to end of queue, toAddress [" + sendRequest.toAddress + "], fromAddress [" + sendRequest.fromAddress + "], subject [" + sendRequest.subject + "], attempts [" + sendRequest.attempts + "]");
                    return false;
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //
        // ==================================================================================================
        //
        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="sendRequest"></param>
        private static void AddEmailLog(CoreController core, EmailSendRequest sendRequest, bool success, string logMessage) {
            var log = DbBaseModel.addDefault<EmailLogModel>(core.cpParent);
            log.name = logMessage;
            log.toAddress = sendRequest.toAddress;
            log.fromAddress = sendRequest.fromAddress;
            log.subject = sendRequest.subject;
            log.body = sendRequest.htmlBody;
            log.sendStatus = success ? "ok" : "fail";
            log.logType = EmailLogTypeImmediateSend;
            log.emailId = sendRequest.emailId;
            log.memberId = sendRequest.toMemberId;
            log.emailDropId = sendRequest.emailDropId;
            log.save(core.cpParent);
        }

        //
        //====================================================================================================        
        /// <summary>
        /// create a simple text version of the email
        /// </summary>
        /// <param name="core"></param>
        /// <param name="isHTML">true if the body is html</param>
        /// <param name="subject"></param>
        /// <returns></returns>
        public static string encodeEmailSubjectText(CoreController core, string subject, PersonModel recipientNullable, object bodyRenderData) {
            if (string.IsNullOrWhiteSpace(subject)) { return ""; }
            subject = MustacheController.renderStringToString(subject, bodyRenderData);
            subject = ContentRenderController.renderHtmlForEmail(core, subject, recipientNullable, "", false);
            return subject;
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
        /// <param name="recipientNullable"></param>
        /// <param name="queryStringForLinkAppend"></param>
        /// <returns></returns>
        public static string encodeEmailBodyHtml(CoreController core, bool isHTML, string body, string template, PersonModel recipientNullable, string queryStringForLinkAppend, bool addLinkAuthToAllLinks, object bodyRenderData) {
            //
            // -- add www website address to root relative links 
            string webAddressProtocolDomain = HttpController.getWebAddressProtocolDomain(core);
            //
            // -- body
            if (!string.IsNullOrWhiteSpace(body)) {
                body = MustacheController.renderStringToString(body, bodyRenderData);
                body = ContentRenderController.renderHtmlForEmail(core, body, recipientNullable, queryStringForLinkAppend, addLinkAuthToAllLinks);
            }
            //
            // -- encode and merge template
            if (!string.IsNullOrWhiteSpace(template)) {
                //
                // hotfix - templates no longer have wysiwyg editors, so content may not be saved correctly - preprocess to convert wysiwyg content
                template = ContentRenderController.processWysiwygResponseForSave(core, template);
                //
                template = ContentRenderController.renderHtmlForEmail(core, template, recipientNullable, queryStringForLinkAppend, addLinkAuthToAllLinks);
                if (template.IndexOf(fpoContentBox) != -1) {
                    body = GenericController.strReplace(template, fpoContentBox, body);
                } else {
                    body = template + body;
                }
            }
            //
            // -- convert links to absolute links
            body = HtmlController.convertHrefSrcToAbsolute(body, webAddressProtocolDomain + "/");
            //
            // -- support legacy replace
            if (recipientNullable != null) {
                body = GenericController.strReplace(body, "#member_id#", recipientNullable.id.ToString());
                body = GenericController.strReplace(body, "#member_email#", recipientNullable.email);
            }
            if (!isHTML) {
                //
                // -- non html email, return a text version of the finished document
                return HtmlController.convertTextToHtml(body);
            }
            //
            // -- Spam Footer under template, remove the marker for any other place in the email then add it as needed
            bool AllowSpamFooter = true;
            if (AllowSpamFooter && recipientNullable != null) {
                //
                // non-authorable, default true - leave it as an option in case there is an important exception
                body += "<div style=\"padding:10px 0;\">" + GenericController.getLinkedText("<a href=\"" + webAddressProtocolDomain + "?" + rnEmailBlockRecipientEmail + "=" + GenericController.encodeRequestVariable(recipientNullable.email) + "\">", core.siteProperties.getText("EmailSpamFooter", DefaultSpamFooter)) + "</div>";
            }

            if (body.ToLower(CultureInfo.InvariantCulture).IndexOf("<html") >= 0) {
                //
                // -- isHtml and the document includes an html tag -- return as-is
                return body;
            }
            //
            // -- html without an html tag. wrap it
            // -- 230124 removed title tag (subject), because it adds subject to text version
            return "<html lang=\"en-US\">"
                + "<head>"
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
                            int personalizeAddonId = CSEmail.getInteger("personalizeAddonId");
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
                                List<string> usedEmails = new();
                                while (csPerson.ok()) {
                                    string sendToPersonEmail = csPerson.getText("Email");
                                    string sendToPersonName = csPerson.getText("name");
                                    int sendToPersonId = csPerson.getInteger("id");
                                    string simpleEmail = getSimpleEmailFromFriendlyEmail(core.cpParent, sendToPersonEmail);
                                    if (!string.IsNullOrEmpty(simpleEmail)) {
                                        if (usedEmails.Contains(simpleEmail)) {
                                            if (string.IsNullOrEmpty(sendToPersonName)) { sendToPersonName = "user #" + sendToPersonId; }
                                            EmailStatusList = EmailStatusList + "Not Sent to " + sendToPersonName + ", duplicate email address (" + sendToPersonEmail + ")" + BR;
                                        } else {
                                            usedEmails.Add(simpleEmail);
                                            EmailStatusList = EmailStatusList + sendEmailRecord_nonImmediate(core, "Group Email", sendToPersonId, emailId, DateTime.MinValue, EmailDropId, BounceAddress, EmailFrom, EmailTemplate, EmailFrom, EmailSubject, EmailCopy, CSEmail.getBoolean("AllowSpamFooter"), CSEmail.getBoolean("AddLinkEID"), "", personalizeAddonId) + BR;
                                            LogController.addActivityCompleted(core, "Group email sent", "Group email sent [" + CSEmail.getText("name") + "]", sendToPersonId, (int)ActivityLogModel.ActivityLogTypeEnum.EmailTo);
                                        }
                                    }
                                    csPerson.goNext();
                                }
                                csPerson.close();
                            }
                            //
                            // Send the confirmation
                            //
                            int ConfirmationMemberId = CSEmail.getInteger("testmemberid");
                            sendConfirmationEmail(core, ConfirmationMemberId, EmailDropId, EmailTemplate, EmailAddLinkEid, EmailSubject, EmailCopy, "", EmailFrom, EmailStatusList, "Group Email", personalizeAddonId);
                            CSEmail.goNext();
                        }
                    }
                    CSEmail.close();
                }
                return;
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
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
                logger.Info($"{core.logCommonMessage},processConditional_DaysAfterjoining, select emails to send to users, sql [" + sql + "]");
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
                            int personalizeAddonId = csEmail.getInteger("personalizeAddonId");
                            string EmailStatus = sendEmailRecord_nonImmediate(core, "Conditional Email", EmailMemberId, emailId, EmailDateExpires, 0, bounceAddress, FromAddress, EmailTemplate, FromAddress, EmailSubject, EmailCopy, csEmail.getBoolean("AllowSpamFooter"), EmailAddLinkEid, "", personalizeAddonId);
                            sendConfirmationEmail(core, ConfirmationMemberId, 0, EmailTemplate, EmailAddLinkEid, EmailSubject, EmailCopy, "", FromAddress, EmailStatus + "<BR>", "Conditional Email", personalizeAddonId);
                            emailsEffected++;
                            LogController.addActivityCompleted(core, "Conditional email sent", "Conditional email sent [" + csEmail.getText("name") + "]", EmailMemberId, (int)ActivityLogModel.ActivityLogTypeEnum.EmailTo);
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
                logger.Info($"{core.logCommonMessage},processConditional_DaysBeforeExpiration, select emails to send to users, sql [" + sql + "]");
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
                            int personalizeAddonId = csEmail.getInteger("personalizeAddonId");
                            string EmailStatus = sendEmailRecord_nonImmediate(core, "Conditional Email", EmailMemberId, emailId, EmailDateExpires, 0, bounceAddress, fromAddress, EmailTemplate, fromAddress, csEmail.getText("Subject"), csEmail.getText("CopyFilename"), csEmail.getBoolean("AllowSpamFooter"), csEmail.getBoolean("AddLinkEID"), "", personalizeAddonId);
                            emailsEffected++;
                            //
                            // -- send confirmation for this send
                            sendConfirmationEmail(core, ConfirmationMemberId, 0, EmailTemplate, EmailAddLinkEid, EmailSubject, EmailCopy, "", fromAddress, EmailStatus + "<BR>", "Conditional Email", personalizeAddonId);
                            //
                            LogController.addActivityCompleted(core, "Conditional email sent", "Conditional email sent [" + csEmail.getText("name") + "]", EmailMemberId, (int)ActivityLogModel.ActivityLogTypeEnum.EmailTo);
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
                logger.Error(ex, $"{core.logCommonMessage}");
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
        public static string sendEmailRecord_nonImmediate(CoreController core, string emailContextMessage, int sendToPersonId, int emailID, DateTime DateBlockExpires, int emailDropID, string BounceAddress, string ReplyToAddress, string EmailTemplate, string FromAddress, string EmailSubject, string EmailBody, bool AllowSpamFooter, bool EmailAllowLinkEID, string emailStyles, int personalizeAddonId) {
            PersonModel recipient = DbBaseModel.create<PersonModel>(core.cpParent, sendToPersonId);
            string returnStatus = "";
            //
            // -- open detect
            if (emailDropID != 0) {
                EmailBody += getEmailClickLink(core, emailDropID, sendToPersonId);
            }
            //
            // -- click detect
            string queryStringForLinkAppend = rnEmailClickFlag + "=" + emailDropID + "&" + rnEmailMemberId + "=" + sendToPersonId;
            //
            bool immediate = false;
            if (trySendPersonEmail(core, recipient, FromAddress, EmailSubject, EmailBody, BounceAddress, ReplyToAddress, immediate, true, emailID, EmailTemplate, EmailAllowLinkEID, ref returnStatus, queryStringForLinkAppend, emailContextMessage, personalizeAddonId)) {
                returnStatus = "Added to queue, email for " + recipient.name + " at " + recipient.email;
            }
            return returnStatus;
        }
        //
        //====================================================================================================
        /// <summary>
        /// return the 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="emailDropID"></param>
        /// <param name="sendToPersonId"></param>
        /// <returns></returns>
        public static string getEmailClickLink(CoreController core, int emailDropID, int sendToPersonId) {
            string webAddressProtocolDomain = HttpController.getWebAddressProtocolDomain(core);
            //string defaultPage = core.siteProperties.serverPageDefault;
            return core.siteProperties.getInteger("GroupEmailOpenTriggerMethod", 0) switch {
                1 => "<link rel=\"stylesheet\" type=\"text/css\" href=\"" + webAddressProtocolDomain + "?" + rnEmailOpenCssFlag + "=" + emailDropID + "&" + rnEmailMemberId + "=" + sendToPersonId + "\">",
                _ => "<img src=\"" + webAddressProtocolDomain + "?" + rnEmailOpenFlag + "=" + emailDropID + "&" + rnEmailMemberId + "=" + sendToPersonId + "\">",
            };
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
        public static void sendConfirmationEmail(CoreController core, int ConfirmationMemberID, int EmailDropID, string EmailTemplate, bool EmailAllowLinkEID, string EmailSubject, string emailBody, string emailStyles, string EmailFrom, string EmailStatusList, string emailContextMessage, int personalizeAddonId) {
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
                    EmailController.trySendPersonEmail(core, person, EmailFrom, "Email confirmation from " + HttpController.getWebAddressProtocolDomain(core), ConfirmBody, "", "", true, true, EmailDropID, EmailTemplate, EmailAllowLinkEID, ref sendStatus, queryStringForLinkAppend, emailContextMessage, personalizeAddonId);
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// convert "jay" <jay@contensive.com> to jay@contensive.com
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string getSimpleEmailFromFriendlyEmail(CPBaseClass cp, string source) {
            string result = source;
            int posStart = result.LastIndexOf("<", StringComparison.InvariantCulture);
            if (posStart < 0) { return result; }
            result = result.Substring(posStart + 1);
            int posEnd = result.IndexOf('>');
            if (posEnd < 0) { return ""; }
            return result.Substring(0, posEnd);
        }
        //
        public static object getRenderData(CoreController core, int personalizeAddonId) {
            if (personalizeAddonId == 0) { return null; }
            string dataSetJson = core.addon.execute(personalizeAddonId, new CPUtilsBaseClass.addonExecuteContext {
                addonType = CPUtilsBaseClass.addonContext.ContextEmail
            });
            return Newtonsoft.Json.JsonConvert.DeserializeObject(dataSetJson);
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static NLog.Logger logger { get; } = NLog.LogManager.GetCurrentClassLogger();
    }
}