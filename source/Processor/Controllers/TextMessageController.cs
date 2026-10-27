using Contensive.BaseModels;
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
        //====================================================================================================
        /// <summary>
        /// The default country calling code for phone numbers that have none, from the site setting SMS Default Country Code. Default 1.
        /// </summary>
        /// <param name="core"></param>
        /// <returns></returns>
        public static string getDefaultCountryCode(CoreController core) {
            string countryCode = core.cpParent.Site.GetText(sitePropertySmsDefaultCountryCode, "1");
            return string.IsNullOrWhiteSpace(countryCode) ? "1" : countryCode.Trim();
        }
        //
        //====================================================================================================
        /// <summary>
        /// Send one text message immediately. Returns the result, never throws for expected failures.
        /// Pipeline:
        /// 1. enabled (else Disabled, log row Not Sent)
        /// 2. normalize to E.164 (else InvalidPhone, log row Not Sent)
        /// 3. block list, and the person's blockTextMessage flag (else Blocked, log row Blocked)
        /// 4. log row first, status Queued
        /// 5. tracked links (hook for the tracked-link feature, no-op here)
        /// 6. segments. If enforceMaxSegments and over the SMS Max Segments setting, TooLong. Bodies over 1600 characters are truncated.
        /// 7. provider send
        /// 8. update the log row with provider id, message id, segments, status
        /// 9. provider opt-out adds the number to the block list
        /// </summary>
        /// <param name="core"></param>
        /// <param name="request"></param>
        /// <param name="enforceMaxSegments">true for interactive sends (cp.SMS.SendText). The queue passes false.</param>
        /// <returns></returns>
        public static SmsSendResultModel sendImmediate(CoreController core, TextMessageSendRequest request, bool enforceMaxSegments)
            => sendImmediate(core, request, enforceMaxSegments, out _);
        //
        //====================================================================================================
        /// <summary>
        /// Send one text message immediately. See the overload without the out argument for the pipeline.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="request"></param>
        /// <param name="enforceMaxSegments"></param>
        /// <param name="isRetryable">true only if the status is Failed and the provider says a later retry may succeed. Used by the queue.</param>
        /// <returns></returns>
        public static SmsSendResultModel sendImmediate(CoreController core, TextMessageSendRequest request, bool enforceMaxSegments, out bool isRetryable) {
            isRetryable = false;
            var result = new SmsSendResultModel { success = false, status = SmsSendStatusEnum.Failed };
            try {
                if (request == null) {
                    result.userError = "The text message request is empty.";
                    return result;
                }
                string source = getSource(request);
                string body = request.textBody ?? "";
                string e164 = SmsPhoneController.toE164(request.toPhone, getDefaultCountryCode(core));
                result.normalizedPhone = e164;
                //
                // -- 1. enabled
                if (!SmsController.isEnabled(core)) {
                    logger.Warn($"{core.logCommonMessage},Text message not sent, text messaging is not configured for this site. source [{source}]");
                    result.status = SmsSendStatusEnum.Disabled;
                    result.userError = "Text messaging is not configured for this site.";
                    result.textMessageLogId = addLog(core, request, source, e164, TextMessageLogModel.StatusEnum.NotSent, "Not sent: text messaging is not configured")?.id ?? 0;
                    return result;
                }
                //
                // -- 2. normalize
                if (string.IsNullOrEmpty(e164)) {
                    result.status = SmsSendStatusEnum.InvalidPhone;
                    result.userError = "The text message was not sent because the phone number is not valid.";
                    result.textMessageLogId = addLog(core, request, source, e164, TextMessageLogModel.StatusEnum.NotSent, "Not sent: invalid phone number")?.id ?? 0;
                    return result;
                }
                if (string.IsNullOrWhiteSpace(body)) {
                    result.status = SmsSendStatusEnum.Failed;
                    result.userError = "The text message was not sent because the message is empty.";
                    result.textMessageLogId = addLog(core, request, source, e164, TextMessageLogModel.StatusEnum.NotSent, "Not sent: empty message")?.id ?? 0;
                    return result;
                }
                //
                // -- 3. block list, then the person's opt-out flag
                if (TextMessageBlockListModel.isBlocked(core.cpParent, e164)) {
                    result.status = SmsSendStatusEnum.Blocked;
                    result.userError = "The text message was not sent because the phone is on this site's Text Message Block List.";
                    result.textMessageLogId = addLog(core, request, source, e164, TextMessageLogModel.StatusEnum.Blocked, "Blocked: phone is on the Text Message Block List")?.id ?? 0;
                    return result;
                }
                if (request.toMemberId > 0) {
                    PersonModel person = DbBaseModel.create<PersonModel>(core.cpParent, request.toMemberId);
                    if (person != null && person.blockTextMessage) {
                        result.status = SmsSendStatusEnum.Blocked;
                        result.userError = "The text message was not sent because the recipient has marked Block Text Messages.";
                        result.textMessageLogId = addLog(core, request, source, e164, TextMessageLogModel.StatusEnum.Blocked, "Blocked: recipient has marked Block Text Messages")?.id ?? 0;
                        return result;
                    }
                }
                //
                // -- 4. log row first. It gives an id and a ccguid for the provider context and tracked links
                TextMessageLogModel log = addLog(core, request, source, e164, TextMessageLogModel.StatusEnum.Queued, "Queued");
                result.textMessageLogId = log?.id ?? 0;
                //
                // -- 5. tracked links (hook for the tracked-link feature)
                body = applyTrackedLinks(core, request, body, log);
                //
                // -- 6. segments
                SmsSegmentInfoModel segmentInfo = SmsSegmentController.getInfo(body);
                if (enforceMaxSegments) {
                    int maxSegments = core.cpParent.Site.GetInteger(sitePropertySmsMaxSegments, 6);
                    if (maxSegments < 1) { maxSegments = 6; }
                    if (segmentInfo.segments > maxSegments) {
                        result.status = SmsSendStatusEnum.TooLong;
                        result.segments = segmentInfo.segments;
                        result.userError = $"The text message was not sent because it uses {segmentInfo.segments} segments. The maximum is {maxSegments}.";
                        updateLog(core, log, TextMessageLogModel.StatusEnum.NotSent, $"Not sent: {segmentInfo.segments} segments, maximum is {maxSegments}", "", "", segmentInfo.segments, 0);
                        return result;
                    }
                }
                string statusDetail = "";
                if (body.Length > smsMaxBodyCharacters) {
                    body = body.Substring(0, smsMaxBodyCharacters);
                    segmentInfo = SmsSegmentController.getInfo(body);
                    statusDetail = $"Body truncated to {smsMaxBodyCharacters} characters.";
                }
                result.segments = segmentInfo.segments;
                //
                // -- 7. provider
                SmsProviderResult providerResult = SmsController.send(core, e164, body, log);
                //
                // -- 8. update the log row. The message may already be sent, so a failure to update the log must not change the result
                result.providerMessageId = providerResult.providerMessageId ?? "";
                int providerId = (providerResult.providerId > 0) ? providerResult.providerId : SmsController.getProviderId(core);
                if (providerResult.success) {
                    updateLog(core, log, TextMessageLogModel.StatusEnum.Sent, "OK", "", statusDetail, segmentInfo.segments, providerId, providerResult.providerMessageId);
                    result.success = true;
                    result.status = SmsSendStatusEnum.Sent;
                    return result;
                }
                //
                // -- 9. provider opt-out
                if (providerResult.isOptedOut) {
                    TextMessageBlockListModel.add(core.cpParent, e164, (int)TextMessageBlockListModel.BlockSourceEnum.ProviderOptOut, request.toMemberId, $"Provider opt-out [{providerResult.errorCode}]");
                    updateLog(core, log, TextMessageLogModel.StatusEnum.Blocked, $"Blocked: recipient opted out [{providerResult.errorCode}]", providerResult.errorCode, providerResult.errorMessage, segmentInfo.segments, providerId);
                    throwOptOutEvent(core, log, e164, request.toMemberId, providerResult.errorCode);
                    result.status = SmsSendStatusEnum.Blocked;
                    result.userError = "The text message was not sent because the recipient has opted out of text messages.";
                    return result;
                }
                //
                // -- failed
                string failureMessage = string.IsNullOrWhiteSpace(providerResult.errorMessage) ? "The text message could not be sent." : providerResult.errorMessage;
                updateLog(core, log, TextMessageLogModel.StatusEnum.Failed, $"attempt {request.attempts}, {failureMessage}", providerResult.errorCode, failureMessage, segmentInfo.segments, providerId);
                result.status = SmsSendStatusEnum.Failed;
                result.userError = failureMessage;
                isRetryable = providerResult.isRetryable;
                return result;
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                isRetryable = false;
                result.success = false;
                result.status = SmsSendStatusEnum.Failed;
                result.userError = "Unexpected error sending the text message.";
                return result;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Hook for the tracked-link feature, pipeline step 5. Called after the log row exists and before segments are counted.
        /// Replace the site-domain urls in the body with tracked links when request.trackLinks is true and request.isTest is false, and return the new body.
        /// This version does nothing and returns the body unchanged.
        /// </summary>
        private static string applyTrackedLinks(CoreController core, TextMessageSendRequest request, string body, TextMessageLogModel log) {
            // -- TODO(tracked links): if (request.trackLinks && !request.isTest) { return SmsLinkController.replaceLinks(core, body, log.id, request.toMemberId); }
            return body;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Pipeline step 9. Called when the provider rejected a send because the recipient opted out, after the number is on the block list and the row is saved as Blocked.
        /// Throws "Text Message Opt Out" (smsOptOutSource=Provider, smsBody=provider code) so add-ons can clear consent.
        /// </summary>
        private static void throwOptOutEvent(CoreController core, TextMessageLogModel log, string e164, int memberId, string providerCode) {
            SmsNotificationController.throwProviderOptOutEvent(core, log, e164, memberId, providerCode);
        }
        //
        //====================================================================================================
        /// <summary>
        /// the log source label, max 50 characters. If the request has none, one is chosen from the request.
        /// </summary>
        private static string getSource(TextMessageSendRequest request) {
            string source = request.source;
            if (string.IsNullOrWhiteSpace(source)) {
                source = (request.systemTextMessageId > 0) ? "System Text Message" : (request.groupTextMessageId > 0) ? "Group Text Message" : "cp.SMS.SendText";
            }
            return truncate(source.Trim(), 50);
        }
        //
        //====================================================================================================
        /// <summary>
        /// truncate a string for a Text field. null returns empty string
        /// </summary>
        private static string truncate(string text, int maxLength) {
            if (string.IsNullOrEmpty(text)) { return ""; }
            return (text.Length <= maxLength) ? text : text.Substring(0, maxLength);
        }
        //
        //====================================================================================================
        /// <summary>
        /// the maximum body length sent to the provider. AWS limit (verify)
        /// </summary>
        public const int smsMaxBodyCharacters = 1600;
        //
        //====================================================================================================
        /// <summary>
        /// Add a row to the Text Message Log for an outbound message. Swallows errors (logging must not stop a send), returns null if the row could not be saved.
        /// </summary>
        private static TextMessageLogModel addLog(CoreController core, TextMessageSendRequest request, string source, string e164, TextMessageLogModel.StatusEnum status, string sendStatus) {
            try {
                TextMessageLogModel log = DbBaseModel.addDefault<TextMessageLogModel>(core.cpParent);
                string toPhone = string.IsNullOrEmpty(e164) ? (request.toPhone ?? "") : e164;
                log.name = truncate($"{source} {toPhone}".Trim(), 255);
                log.sendStatus = truncate(sendStatus, 255);
                log.body = request.textBody;
                log.memberId = request.toMemberId;
                log.systemTextMessageId = request.systemTextMessageId;
                log.groupTextMessageId = request.groupTextMessageId;
                log.toPhone = truncate(toPhone, 255);
                log.source = source;
                log.sentByMemberId = request.sentByMemberId;
                log.isTest = request.isTest;
                log.isInbound = false;
                log.providerId = SmsController.getProviderId(core);
                log.fromPhone = SmsController.getFromPhone(core);
                log.statusId = (int)status;
                log.statusDate = core.dateTimeNowMockable;
                log.save(core.cpParent);
                return log;
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage},addLog");
                return null;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Update the outbound log row after a send attempt. Swallows errors, the send result does not depend on the log.
        /// </summary>
        private static void updateLog(CoreController core, TextMessageLogModel log, TextMessageLogModel.StatusEnum status, string sendStatus, string providerStatusCode, string statusDetail, int segments, int providerId)
            => updateLog(core, log, status, sendStatus, providerStatusCode, statusDetail, segments, providerId, "");
        //
        private static void updateLog(CoreController core, TextMessageLogModel log, TextMessageLogModel.StatusEnum status, string sendStatus, string providerStatusCode, string statusDetail, int segments, int providerId, string providerMessageId) {
            try {
                if (log == null) { return; }
                log.statusId = (int)status;
                log.statusDate = core.dateTimeNowMockable;
                log.sendStatus = truncate(sendStatus, 255);
                log.providerStatusCode = truncate(providerStatusCode, 255);
                log.statusDetail = truncate(statusDetail, 255);
                log.segments = segments;
                if (providerId > 0) { log.providerId = providerId; }
                if (!string.IsNullOrEmpty(providerMessageId)) { log.providerMessageId = truncate(providerMessageId, 255); }
                log.save(core.cpParent);
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage},updateLog");
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
                    core.db.executeNonQuery("update ccGroupTextMessages set sent=1 where id=@id", new Dictionary<string, object> { { "@id", groupTextMessage.id } });
                    //
                    // -- send it to every in the groups and topics
                    List<string> recipientList = new();
                    string defaultCountryCode = getDefaultCountryCode(core);
                    string sql = "select Distinct ccMembers.id,ccMembers.cellPhone, ccMembers.name"
                        + " From ((((ccGroupTextMessages"
                        + " left join ccGroupTextMessageGroupRules on ccGroupTextMessageGroupRules.groupTextMessageId=ccGroupTextMessages.id)"
                        + " left join ccGroups on ccGroups.Id = ccGroupTextMessageGroupRules.GroupID)"
                        + " left join ccMemberRules on ccMemberRules.GroupID=ccGroups.Id)"
                        + " left join ccMembers on ccMembers.Id = ccMemberRules.memberId)"
                        + " Where (ccGroupTextMessages.ID=@groupTextMessageId)"
                        + " and (ccGroups.active<>0)"
                        + " and (ccMembers.active<>0)"
                        + " and ((ccMembers.blockTextMessage=0)or(ccMembers.blockTextMessage is null))"
                        + " and (ccMembers.cellPhone<>'')"
                        + " and ((ccMemberRules.DateExpires is null)or(ccMemberRules.DateExpires>@dateNow))"
                        + " order by ccMembers.cellPhone,ccMembers.id";
                    using (DataTable dt = core.db.executeQuery(sql, new Dictionary<string, object> { { "@groupTextMessageId", groupTextMessage.id }, { "@dateNow", core.dateTimeNowMockable } })) {
                        if ((dt?.Rows != null) && (dt.Rows.Count > 0)) {
                            foreach (DataRow row in dt.Rows) {
                                string recipientName = core.cpParent.Utils.EncodeText(row[2]);
                                string recipientPhone = SmsPhoneController.toE164(core.cpParent.Utils.EncodeText(row[1]), defaultCountryCode);
                                int recipientId = core.cpParent.Utils.EncodeInteger(row[0]);
                                if (!string.IsNullOrEmpty(recipientPhone)) {
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
                                recipientList.Add("Fail, invalid phone, " + recipientName + ", " + core.cpParent.Utils.EncodeText(row[1]));
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
        //
        public static bool queuePersonTextMessage(CoreController core, PersonModel recipient, string textBody, bool Immediate, int systemTextMessageId, int groupTextMessageId, ref string userErrorMessage, string contextMessage) {
            try {
                if (recipient == null) {
                    userErrorMessage = "The text message was not sent because the recipient could not be found.";
                    return false;
                }
                string recipientE164 = SmsPhoneController.toE164(recipient.cellPhone, getDefaultCountryCode(core));
                if (string.IsNullOrEmpty(recipientE164)) {
                    //
                    userErrorMessage = "The text message was not sent because the recipient's cell-phone is not valid, [" + recipient.name + "], cell phone [" + recipient.cellPhone + "].";
                    return false;
                }
                if (recipient.blockTextMessage) {
                    //
                    userErrorMessage = "The text message was not sent because the recipient has marked Block Text Messages, [" + recipient.name + "].";
                    return false;
                }
                if (TextMessageBlockListModel.isBlocked(core.cpParent, recipientE164)) {
                    //
                    userErrorMessage = "The text message was not sent because the phone is blocked by this application. See the Text Message Block List.";
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
                if (string.IsNullOrWhiteSpace(textMessage.source)) { textMessage.source = textMessageContextMessage; }
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
                core.db.executeNonQuery("update ccTextMessageQueue set sendSerialNumber=@serialNumber where id in (select top 100 id from ccTextMessageQueue where (sendSerialNumber is null) order by immediate,id)", new Dictionary<string, object> { { "@serialNumber", sendSerialNumber } });
                //
                foreach (TextMessageQueueModel textMessage in DbBaseModel.createList<TextMessageQueueModel>(core.cpParent, "sendSerialNumber=" + DbController.encodeSQLText(sendSerialNumber), "immediate,id")) {
                    TextMessageSendRequest request = DeserializeObject<TextMessageSendRequest>(textMessage.content);
                    if (request == null) {
                        //
                        // -- bugfix, if data does not deserialize, skip message
                        logger.Error(new ArgumentNullException("TextMessage read from TextMessageQueue has content that serialized to null, message skipped, textmessage.content [" + textMessage.content + "]"), $"{core.logCommonMessage}");
                        core.db.executeNonQuery("delete from ccTextMessageQueue where (id=@id)", new Dictionary<string, object> { { "@id", textMessage.id } });
                        continue;
                    }
                    //
                    // -- same pipeline as an immediate send, without the interactive max-segments limit. The pipeline writes the log row.
                    SmsSendResultModel sendResult = sendImmediate(core, request, false, out bool isRetryable);
                    if (sendResult.status == SmsSendStatusEnum.Failed && isRetryable) {
                        //
                        // -- setup retry, only for a failure the provider says may succeed later
                        core.db.executeNonQuery("update ccTextMessageQueue set attempts=attempts+1,sendSerialNumber=null  where ccguid=@ccguid", new Dictionary<string, object> { { "@ccguid", textMessage.ccguid } });
                        continue;
                    }
                    //
                    // -- sent, or a final result (blocked, disabled, invalid phone, not retryable failure). remove from the queue
                    core.db.executeNonQuery("delete from ccTextMessageQueue where ccguid=@ccguid", new Dictionary<string, object> { { "@ccguid", textMessage.ccguid } });
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            } finally {
                core.db.executeNonQuery("update ccTextMessageQueue set attempts=attempts+1,sendSerialNumber=null where sendSerialNumber=@serialNumber", new Dictionary<string, object> { { "@serialNumber", sendSerialNumber } });
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