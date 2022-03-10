
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Exceptions;
using Contensive.Processor.Models.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using static Contensive.Processor.Constants;
using static Newtonsoft.Json.JsonConvert;
//
namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// 
    /// </summary>
    public static class TextMessageController {
        //
        private const string blockListFilename = "Config\\SMSBlockList.txt";
        //
        //
        //====================================================================================================
        /// <summary>
        /// process group text, adding each to the queue
        /// </summary>
        /// <param name="core"></param>
        public static void processGroupTextMessage(CoreController core) {
            try {
                //
                // -- get list of all group texts messages that need to be sent
                foreach (var groupTextMessage in DbBaseModel.createList<GroupTextMessageModel>(core.cpParent, "((sent is null)or(sent=0))and(submitted<>0)")) {
                    //
                    // -- mark it sent
                    core.db.executeNonQuery("update ccGroupTextMessages set sent=1 where id=" + groupTextMessage.id);
                    //
                    // -- send it to every in the groups and topics
                    List<string> recipientList = new();
                    string sql = "select Distinct ccMembers.id,ccMembers.cellPhone, ccMembers.name"
                        + " From ((((ccGroupTextMessages"
                        + " left join ccGroupTextMessageGroupRules on ccGroupTextMessageGroupRules.groupTextMessageId=ccGroupTextMessages.id)"
                        + " left join ccGroups on ccGroups.Id = ccGroupTextMessageGroupRules.GroupID)"
                        + " left join ccMemberRules on ccMemberRules.GroupID=ccGroups.Id)"
                        + " left join ccMembers on ccMembers.Id = ccMemberRules.memberId)"
                        + " Where (ccGroupTextMessages.ID=" + groupTextMessage.id + ")"
                        + " and (ccGroups.active<>0)"
                        + " and (ccMembers.active<>0)"
                        + " and ((ccMembers.blockTextMessage=0)or(ccMembers.blockTextMessage is null))"
                        + " and (ccMembers.cellPhone<>'')"
                        + " and ((ccMemberRules.DateExpires is null)or(ccMemberRules.DateExpires>" + core.sqlDateTimeMockable + "))"
                        + " order by ccMembers.cellPhone,ccMembers.id";
                    using (DataTable dt = core.db.executeQuery(sql)) {
                        if ((dt?.Rows != null) && (dt.Rows.Count > 0)) {
                            foreach (DataRow row in dt.Rows) {
                                string recipientName = core.cpParent.Utils.EncodeText(row[2]);
                                string recipientPhone = normalizePhoneNumber(core.cpParent.Utils.EncodeText(row[1]));
                                int recipientId = core.cpParent.Utils.EncodeInteger(row[0]);
                                if (verifyPhone(core, recipientPhone)) {
                                    //
                                    // -- queue the text message
                                    var textMessageSendRequest = new TextMessageSendRequest {
                                        attempts = 0,
                                        textMessageId = groupTextMessage.id,
                                        textBody = groupTextMessage.body,
                                        toPhone = recipientPhone,
                                        toMemberId = recipientId
                                    };
                                    queueTextMessage(core, false, "Group Text Message", textMessageSendRequest);
                                    recipientList.Add("OK, " + recipientName + ", " + recipientPhone);
                                    continue;
                                }
                                recipientList.Add("Fail, invalid phone, " + recipientName + ", " + recipientPhone);
                            }
                        }
                    }
                    if (groupTextMessage.testMemberID > 0) {
                        var confirmPerson = DbBaseModel.create<PersonModel>(core.cpParent, groupTextMessage.testMemberID);
                        if (confirmPerson != null) {
                            sendConfirmation(core, confirmPerson, groupTextMessage.body, recipientList, groupTextMessage.id);
                        }
                    }
                }
                //
                // -- set the text message task to run now
                AddonModel.setRunNow(core.cpParent, addonGuidTextMessageSendTask);
                return;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// return list of phone numbers that should be blocked. format: phone - tab - dateBlocked - newLine
        /// </summary>
        /// <param name="core"></param>
        /// <returns></returns>
        public static string getBlockList(CoreController core) {
            if (!string.IsNullOrEmpty(core.doc.smsBlockListStore)) { return core.doc.smsBlockListStore; }
            core.doc.smsBlockListStore = core.privateFiles.readFileText(blockListFilename);
            if (string.IsNullOrEmpty(core.doc.smsBlockListStore)) {
                // -- if blank, add a new-line so the empty list can be cached
                core.doc.smsBlockListStore = windowsNewLine;
                core.privateFiles.saveFile(blockListFilename, core.doc.smsBlockListStore);
            }
            return core.doc.smsBlockListStore;
        }
        //
        //====================================================================================================
        /// <summary>
        /// true if the phone number is on the blocked list
        /// </summary>
        /// <param name="core"></param>
        /// <param name="phoneNumber"></param>
        /// <returns></returns>
        public static bool isOnBlockedList(CoreController core, string phoneNumber) {
            phoneNumber = normalizePhoneNumber(phoneNumber);
            return (getBlockList(core).IndexOf(Environment.NewLine + phoneNumber + "\t", StringComparison.CurrentCultureIgnoreCase) >= 0);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Add to block list
        /// </summary>
        /// <param name="core"></param>
        /// <param name="phoneNumber"></param>
        public static void addToBlockList(CoreController core, string phoneNumber) {
            var blockList = getBlockList(core);
            phoneNumber = normalizePhoneNumber(phoneNumber);
            if (!verifyPhone(core, phoneNumber)) {
                //
                // bad number
                //
            } else if (isOnBlockedList(core, phoneNumber)) {
                //
                // They are already in the list
                //
            } else {
                //
                // add them to the list
                //
                core.doc.smsBlockListStore = blockList + Environment.NewLine + phoneNumber + "\t" + core.dateTimeNowMockable;
                core.privateFiles.saveFile(blockListFilename, core.doc.smsBlockListStore);
                core.doc.smsBlockListStore = "";
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// remove text punctuation, etc. Attempt to convert (703) 303-9974 to 10 digit string
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <returns></returns>
        public static string normalizePhoneNumber(string phoneNumber) {
            string normalizedPhoneNumber = "";
            foreach (char validChr in phoneNumber) {
                if ("0123456789".Contains(validChr.ToString())) {
                    normalizedPhoneNumber += validChr;
                }
            }
            return normalizedPhoneNumber;
        }
        //
        //====================================================================================================
        /// <summary>
        /// need phone number rules
        /// </summary>
        public static bool verifyPhone(CoreController core, string cellPhone) {
            try {
                //The first digit should contain number between 7 to 9.
                //The rest 9 digit can contain any number between 0 to 9.
                //The mobile number can have 11 digits also by including 0 at the starting.
                //The mobile number can be of 12 digits also by including 91 at the starting.
                if (string.IsNullOrWhiteSpace(cellPhone)) { return false; }
                return true;
            } catch (Exception) {
                return false;
            }
        }
        //
        //====================================================================================================
        //
        public static bool queuePersonTextMessage(CoreController core, PersonModel recipient, string textBody, bool Immediate, int textMessageId, ref string userErrorMessage, string contextMessage) {
            try {
                if (recipient == null) {
                    userErrorMessage = "The text message was not sent because the recipient could not be found by thier id [" + recipient.id + "]";
                    return false;
                }
                if (!verifyPhone(core, recipient.cellPhone)) {
                    //
                    userErrorMessage = "The text message was not sent because the recipient's cell-phone is not valid, [" + recipient.name + "], cell phone [" + recipient.cellPhone + "].";
                    return false;
                }
                if (recipient.blockTextMessage) {
                    //
                    userErrorMessage = "The text message was not sent because the recipient has marked Block Text Messages, [" + recipient.name + "].";
                    return false;
                }
                if (0 != GenericController.strInstr(1, getBlockList(core), Environment.NewLine + recipient.cellPhone + Environment.NewLine, 1)) {
                    //
                    userErrorMessage = "The text message was not sent because the phone is blocked by this application. See the Blocked Phone Report.";
                    return false;
                }
                string recipientName = (!string.IsNullOrWhiteSpace(recipient.name) && !recipient.name.ToLower().Equals("guest")) ? recipient.name : string.Empty;
                if (string.IsNullOrWhiteSpace(recipientName)) {
                    recipientName = ""
                        + ((!string.IsNullOrWhiteSpace(recipient.firstName) && !recipient.firstName.ToLower().Equals("guest")) ? recipient.firstName : string.Empty)
                        + " "
                        + ((!string.IsNullOrWhiteSpace(recipient.lastName) && !recipient.lastName.ToLower().Equals("guest")) ? recipient.lastName : string.Empty);
                }
                recipientName = recipientName.Trim();
                //
                var textMessageSendRequest = new TextMessageSendRequest {
                    attempts = 0,
                    textMessageId = textMessageId,
                    textBody = textBody,
                    toPhone = recipient.cellPhone,
                    toMemberId = recipient.id
                };
                queueTextMessage(core, Immediate, contextMessage, textMessageSendRequest);
                return true;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        //
        public static bool sendConfirmation(CoreController core, PersonModel person, string originalTextMessageBody, List<string> recipientList, int textMessageId) {
            try {
                if (person == null) { return false; }
                //
                // --- Send the completion message to the administrator
                string ConfirmBody = "<div style=\"padding:10px;\">" + BR;
                ConfirmBody += "The follow System Text was sent." + BR;
                ConfirmBody += "" + BR;
                ConfirmBody += "Body:" + BR;
                ConfirmBody += "<div style=\"clear:all\">----------------------------------------------------------------------</div>" + BR;
                ConfirmBody += originalTextMessageBody + BR;
                ConfirmBody += "<div style=\"clear:all\">----------------------------------------------------------------------</div>" + BR;
                ConfirmBody += "--- recipient list ---" + BR;
                ConfirmBody += string.Join(BR, recipientList) + BR;
                ConfirmBody += "--- end of list ---" + BR;
                ConfirmBody += "</div>";
                //
                string emailStatus = "";
                EmailController.queueAdHocEmail(core, "Text Message Confirmation", person.id, person.email, core.siteProperties.emailFromAddress, "Text Message Sent", ConfirmBody, core.siteProperties.emailBounceAddress, core.siteProperties.emailFromAddress, "", true, true, 0, ref emailStatus);
                return queuePersonTextMessage(core, person, "System text complete from " + core.appConfig.domainList[0] + ". A confirmation email was sent to [" + person.email + "]", true, textMessageId, ref emailStatus, "System Text Confirmation");
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }

        }
        //
        //====================================================================================================
        //
        public static bool queueSystemTextMessage(CoreController core, SystemTextMessageModel textMessage, string appendedCopy, int additionalMemberID, ref string userErrorMessage) {
            try {
                var confirmationMessage = new StringBuilder();
                //
                // --- collect values needed for send
                string textBody = textMessage.body + appendedCopy;
                //
                // --- Send message to the additional member
                if (additionalMemberID != 0) {
                    confirmationMessage.Append(BR + "Primary Recipient:" + BR);
                    PersonModel person = DbBaseModel.create<PersonModel>(core.cpParent, additionalMemberID);
                    if (person == null) {
                        confirmationMessage.Append("&nbsp;&nbsp;Error: Not sent to additional user [#" + additionalMemberID + "] because the user record could not be found." + BR);
                    } else {
                        if (string.IsNullOrWhiteSpace(person.cellPhone)) {
                            confirmationMessage.Append("&nbsp;&nbsp;Error: Not sent to additional user [#" + additionalMemberID + "] because their phone number was blank." + BR);
                        } else {
                            string individualErrorMessage = "";
                            queuePersonTextMessage(core, person, textBody, true, textMessage.id, ref individualErrorMessage, "System Text Message Addl User");
                            confirmationMessage.Append("&nbsp;&nbsp;Sent to " + person.name + " at " + person.cellPhone + ", Status = " + individualErrorMessage + BR);
                        }
                    }
                }
                //
                // --- Send message to everyone selected
                //
                List<string> recipientList = new();
                confirmationMessage.Append(BR + "Recipients in selected groups:" + BR);
                List<int> peopleIdList = PersonModel.createidListForSystemTextMessage(core.cpParent, textMessage.id);
                foreach (var personId in peopleIdList) {
                    var person = DbBaseModel.create<PersonModel>(core.cpParent, personId);
                    if (person == null) {
                        confirmationMessage.Append("&nbsp;&nbsp;Error: Not sent to user [#" + additionalMemberID + "] because the user record could not be found." + BR);
                    } else {
                        if (string.IsNullOrWhiteSpace(person.cellPhone)) {
                            confirmationMessage.Append("&nbsp;&nbsp;Error: Not sent to user [#" + additionalMemberID + "] because their phone was blank." + BR);
                        } else {
                            recipientList.Add(person.name + ", " + person.cellPhone);
                            string status = "";
                            queuePersonTextMessage(core, person, textBody, false, textMessage.id, ref status, "System Text Message");
                            confirmationMessage.Append("&nbsp;&nbsp;Sent to " + person.name + " at " + person.cellPhone + ", Status = " + status + BR);
                        }
                    }
                }
                int confirmationMemberId = textMessage.testMemberId;
                //
                // --- Send the completion message to the administrator
                //
                if (confirmationMemberId != 0) {
                    PersonModel person = DbBaseModel.create<PersonModel>(core.cpParent, confirmationMemberId);
                    sendConfirmation(core, person, textMessage.body, recipientList, textMessage.id);
                }
                //
                // -- set the text message task to run now
                AddonModel.setRunNow(core.cpParent, addonGuidTextMessageSendTask);
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
            return true;
        }
        //
        //====================================================================================================
        /// <summary>
        /// add to queue
        /// </summary>
        /// <param name="core"></param>
        /// <param name="immediate"></param>
        /// <param name="textMessage"></param>
        /// <param name="textMessageContextMessage">A short description of the message for logging)</param>
        private static void queueTextMessage(CoreController core, bool immediate, string textMessageContextMessage, TextMessageSendRequest textMessage) {
            try {
                if (core.mockTextMessages) {
                    //
                    // -- mock the text message for testing
                    core.mockTextMessageList.Add(new MockTextMessageClass {
                        textMessageRequest = textMessage
                    });
                    return;
                }
                //
                // -- queue the text message for sending in a task
                var queue = DbBaseModel.addEmpty<TextMessageQueueModel>(core.cpParent);
                queue.name = textMessageContextMessage;
                queue.immediate = immediate;
                queue.toPhone = textMessage.toPhone;
                queue.content = SerializeObject(textMessage);
                queue.attempts = textMessage.attempts;
                queue.save(core.cpParent);
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Send the text messages in the current Queue
        /// </summary>
        public static void sendTextMessageQueue(CoreController core) {
            try {
                //
                // -- delete messages that have been retried 3 times
                core.db.executeNonQuery("delete from ccTextMessageQueue where (attempts>3)");
                //
                // -- mark the next 100 texts with this processes serial number. Then select them back to verify no other process tries to send them
                string sendSerialNumber = GenericController.getGUID();
                core.db.executeNonQuery("update ccTextMessageQueue set sendSerialNumber=" + DbController.encodeSQLText(sendSerialNumber) + " where id in (select top 100 id from ccTextMessageQueue where (sendSerialNumber is null) order by immediate,id)");
                //
                foreach (TextMessageQueueModel textMessage in DbBaseModel.createList<TextMessageQueueModel>(core.cpParent, "sendSerialNumber=" + DbController.encodeSQLText(sendSerialNumber), "immediate,id")) {
                    TextMessageSendRequest request = DeserializeObject<TextMessageSendRequest>(textMessage.content);
                    if(request == null ) {
                        //
                        // -- bugfix, if data does not deserialize, skip message
                        LogController.logError(core, new ArgumentNullException("TextMessage read from TextMessageQueue has content that serialized to null, message skipped, textmessage.content [" + textMessage.content + "]"));
                        core.db.executeNonQuery("delete from ccTextMessageQueue where (id=" + textMessage.id + ")");
                        appendTextMessageLog(core, request, false, "TextMessage read from TextMessageQueue has content that serialized to null, message skipped, textmessage.content [" + textMessage.content + "]");
                        continue;
                    }
                    string userError = "";
                    if (SmsController.sendMessage(core, request, ref userError)) {
                        //
                        // -- successful send
                        core.db.executeNonQuery("delete from ccTextMessageQueue where ccguid=" + DbController.encodeSQLText(textMessage.ccguid) + "");
                        appendTextMessageLog(core, request, true, "");
                        continue;
                    }
                    //
                    // -- setup retry
                    appendTextMessageLog(core, request, false, userError);
                    core.db.executeNonQuery("update ccTextMessageQueue set attempts=attempts+1,sendSerialNumber=null  where ccguid=" + DbController.encodeSQLText(textMessage.ccguid) + "");
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// atempt to log a text message to the log (swallow the write protect errors)
        /// </summary>
        /// <param name="core"></param>
        /// <param name="request"></param>
        public static void appendTextMessageLog(CoreController core, TextMessageSendRequest request, bool success, string userError) {
            try {
                string textMessageLogFilename = "textMessageLog\\" + DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString().PadLeft(2, '0') + DateTime.Now.Day.ToString().PadLeft(2, '0') + ".log";
                core.privateFiles.appendFile(textMessageLogFilename, DateTime.Now.ToString("yyyyMMddHHmmss") + "\t" + (success ? "ok" : "fail") + "\t" + userError + "\t" + request.toPhone + "\t" + request.textBody.Replace("\r", "").Replace("\n", " ") + "\r\n");
            } catch (Exception) {
                // swallow
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