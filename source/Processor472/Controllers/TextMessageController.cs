using Contensive.Models.Db;
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
        private const string blockListPrivateFilePathFilename = "Config\\TextMessageBlockList.txt";
        //
        //====================================================================================================
        /// <summary>
        /// send text message. Returns false if there was an error, or the user is on the site's block list
        /// </summary>
        /// <param name="core"></param>
        /// <param name="request"></param>
        /// <param name="userError"></param>
        /// <returns></returns>
        public static bool sendImmediate(CoreController core, TextMessageSendRequest request, ref string userError, string logName) {
            try {
                if (isOnBlockedList(core, request.toPhone)) {
                    //
                    userError = "The text message was not sent because the phone is blocked by this application. See the Blocked Phone Report.";
                    logTextMessage(core, request, false, userError, "blocked: " + logName);
                    return false;
                }
                bool result = SmsController.sendMessage(core, request, ref userError);
                logTextMessage(core, request, result, userError, logName);
                return result;
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }

        }
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
                bool needToSend = false;
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
                                        systemTextMessageId = 0,
                                        groupTextMessageId = groupTextMessage.id,
                                        textBody = groupTextMessage.body,
                                        toPhone = recipientPhone,
                                        toMemberId = recipientId
                                    };
                                    queueTextMessage(core, false, "Group Text Message", textMessageSendRequest);
                                    recipientList.Add("OK, " + recipientName + ", " + recipientPhone);
                                    needToSend = true;
                                    continue;
                                }
                                recipientList.Add("Fail, invalid phone, " + recipientName + ", " + recipientPhone);
                            }
                        }
                    }
                    if (groupTextMessage.testMemberID > 0) {
                        var confirmPerson = DbBaseModel.create<PersonModel>(core.cpParent, groupTextMessage.testMemberID);
                        if (confirmPerson != null) {
                            // todo -- add personalization to text
                            int personalizeAddonId = 0;
                            sendConfirmation(core, confirmPerson, groupTextMessage.body, recipientList, 0, groupTextMessage.id, personalizeAddonId);
                            needToSend = true;
                        }
                    }
                }
                //
                // -- set the text message task to run now
                if (needToSend) { AddonModel.setRunNow(core.cpParent, addonGuidTextMessageSendTask); }
                return;
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
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
            if (!string.IsNullOrEmpty(core.doc.textMessageBlockListStore)) { return core.doc.textMessageBlockListStore; }
            core.doc.textMessageBlockListStore = core.privateFiles.readFileText(blockListPrivateFilePathFilename);
            if (string.IsNullOrEmpty(core.doc.textMessageBlockListStore)) {
                // -- if blank, add a new-line so the empty list can be cached
                core.doc.textMessageBlockListStore = windowsNewLine;
                core.privateFiles.saveFile(blockListPrivateFilePathFilename, core.doc.textMessageBlockListStore);
            }
            return core.doc.textMessageBlockListStore;
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
            return getBlockList(core).IndexOf(Environment.NewLine + phoneNumber + "\t", StringComparison.CurrentCultureIgnoreCase) >= 0;
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
                core.doc.textMessageBlockListStore = blockList + Environment.NewLine + phoneNumber + "\t" + core.dateTimeNowMockable;
                core.privateFiles.saveFile(blockListPrivateFilePathFilename, core.doc.textMessageBlockListStore);
                core.doc.textMessageBlockListStore = "";
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
        public static bool queuePersonTextMessage(CoreController core, PersonModel recipient, string textBody, bool Immediate, int systemTextMessageId, int groupTextMessageId, ref string userErrorMessage, string contextMessage) {
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
                if (isOnBlockedList(core, recipient.cellPhone)) {
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
                    systemTextMessageId = systemTextMessageId,
                    groupTextMessageId = groupTextMessageId,
                    textBody = textBody,
                    toPhone = recipient.cellPhone,
                    toMemberId = recipient.id
                };
                queueTextMessage(core, Immediate, contextMessage, textMessageSendRequest);
                return true;
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// send the confirmation for this text message.
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="person"></param>
        /// <param name="originalTextMessageBody"></param>
        /// <param name="recipientList"></param>
        /// <param name="systemTextMessageId"></param>
        /// <returns></returns>
        public static bool sendConfirmation(CoreController core, PersonModel person, string originalTextMessageBody, List<string> recipientList, int systemTextMessageId, int groupTextMessageId, int personalizeAddonId) {
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
                // -- queue email and text, run email and text send (no doc conext needed)
                string emailStatus = "";
                EmailController.sendAdHocEmail(core, "Text Message Confirmation", person.id, person.email, core.siteProperties.emailFromAddress, "Text Message Sent", ConfirmBody, core.siteProperties.emailBounceAddress, core.siteProperties.emailFromAddress, "", true, true, 0, ref emailStatus,personalizeAddonId);
                AddonModel.setRunNow(core.cpParent, addonGuidTextMessageSendTask);
                //
                bool result = queuePersonTextMessage(core, person, "Text message complete from " + core.appConfig.name + ". A detailed confirmation email was sent to [" + person.id + ", " + person.name + "]", true, systemTextMessageId, groupTextMessageId, ref emailStatus, "System Text Confirmation");
                AddonModel.setRunNow(core.cpParent, addonGuidEmailSendTask);
                //
                return result;
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
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
                bool needToSend = false;
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
                            queuePersonTextMessage(core, person, textBody, true, textMessage.id, 0, ref individualErrorMessage, "System Text Message Addl User");
                            confirmationMessage.Append("&nbsp;&nbsp;Sent to " + person.name + " at " + person.cellPhone + ", Status = " + individualErrorMessage + BR);
                            needToSend = true;
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
                            queuePersonTextMessage(core, person, textBody, false, textMessage.id, 0, ref status, "System Text Message");
                            confirmationMessage.Append("&nbsp;&nbsp;Sent to " + person.name + " at " + person.cellPhone + ", Status = " + status + BR);
                            needToSend = true;
                        }
                    }
                }
                int confirmationMemberId = textMessage.testMemberId;
                //
                // --- Send the completion message to the administrator
                //
                if (confirmationMemberId != 0) {
                    PersonModel person = DbBaseModel.create<PersonModel>(core.cpParent, confirmationMemberId);
                    // todo -- add personalization
                    int personalizeAddonId = 0;
                    sendConfirmation(core, person, textBody, recipientList, textMessage.id, 0, personalizeAddonId);
                    needToSend = true;
                }
                //
                // -- set the text message task to run now
                if (needToSend) { AddonModel.setRunNow(core.cpParent, addonGuidTextMessageSendTask); }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
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
                logger.Error(ex, $"{core.logCommonMessage}");
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Send the text messages in the current Queue
        /// </summary>
        public static void sendTextMessageQueue(CoreController core) {
            string sendSerialNumber = GenericController.getGUID();
            try {
                //
                // -- return if no messages
                using (DataTable dt = core.db.executeQuery("select top 1 id from ccTextMessageQueue")) {
                    if (dt?.Rows == null || dt.Rows.Count == 0) { return; }
                }
                //
                // -- delete messages that have been retried 3 times
                core.db.executeNonQuery("delete from ccTextMessageQueue where (attempts>=3)");
                //
                // -- mark the next 100 texts with this processes serial number. Then select them back to verify no other process tries to send them
                core.db.executeNonQuery("update ccTextMessageQueue set sendSerialNumber=" + DbController.encodeSQLText(sendSerialNumber) + " where id in (select top 100 id from ccTextMessageQueue where (sendSerialNumber is null) order by immediate,id)");
                //
                foreach (TextMessageQueueModel textMessage in DbBaseModel.createList<TextMessageQueueModel>(core.cpParent, "sendSerialNumber=" + DbController.encodeSQLText(sendSerialNumber), "immediate,id")) {
                    TextMessageSendRequest request = DeserializeObject<TextMessageSendRequest>(textMessage.content);
                    if (request == null) {
                        //
                        // -- bugfix, if data does not deserialize, skip message
                        logger.Error($"{core.logCommonMessage}", new ArgumentNullException("TextMessage read from TextMessageQueue has content that serialized to null, message skipped, textmessage.content [" + textMessage.content + "]"));
                        core.db.executeNonQuery("delete from ccTextMessageQueue where (id=" + textMessage.id + ")");
                        logTextMessage(core, request, false, "TextMessage read from TextMessageQueue has content that serialized to null, message skipped, textmessage.content [" + textMessage.content + "]", "Failed, message error");
                        continue;
                    }
                    string userError = "";
                    if (SmsController.sendMessage(core, request, ref userError)) {
                        //
                        // -- successful send
                        core.db.executeNonQuery("delete from ccTextMessageQueue where ccguid=" + DbController.encodeSQLText(textMessage.ccguid) + "");
                        logTextMessage(core, request, true, userError, "Sent to " + core.cpParent.Content.GetRecordName("people",request.toMemberId) + " " + request.toPhone);
                        continue;
                    }
                    //
                    // -- setup retry
                    logTextMessage(core, request, false, userError, "Failed " + core.cpParent.Content.GetRecordName("people", request.toMemberId) + " " + request.toPhone);
                    core.db.executeNonQuery("update ccTextMessageQueue set attempts=attempts+1,sendSerialNumber=null  where ccguid=" + DbController.encodeSQLText(textMessage.ccguid) + "");
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            } finally {
                core.db.executeNonQuery($"update ccTextMessageQueue set attempts=attempts+1,sendSerialNumber=null where sendSerialNumber={DbController.encodeSQLText(sendSerialNumber)}");
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// atempt to log a text message to the log (swallow the write protect errors)
        /// </summary>
        /// <param name="core"></param>
        /// <param name="request"></param>
        public static void logTextMessage(CoreController core, TextMessageSendRequest request, bool success, string userError, string logName) {
            try {
                TextMessageLogModel log = DbBaseModel.addDefault<TextMessageLogModel>(core.cpParent);
                log.sendStatus = success ? "OK" : "attempt " + request.attempts + ", " + userError;
                log.body = request.textBody;
                log.memberId = request.toMemberId;
                log.name = logName;
                log.systemTextMessageId = request.systemTextMessageId;
                log.groupTextMessageId = request.groupTextMessageId;
                log.toPhone = request.toPhone;
                log.save(core.cpParent);
            } catch (Exception ex) {
                logger.Error(ex, "appendTextMessageLog");
                // swallow
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static NLog.Logger logger { get; } = NLog.LogManager.GetCurrentClassLogger();
    }
}