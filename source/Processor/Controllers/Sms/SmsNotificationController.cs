using Contensive.Models.Db;
using Contensive.Processor.Models.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using static Contensive.Processor.Constants;

namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// Process the Message of a verified AWS SNS Notification from AWS End User Messaging SMS: delivery events and two-way (inbound) messages.
    /// Also builds and throws the four text message events. Every event carries every sms argument key (empty when not applicable),
    /// and the log row is saved in its final state before the event is thrown, so catchers can reload it by smsTextMessageLogId.
    /// </summary>
    public static class SmsNotificationController {
        //
        //====================================================================================================
        /// <summary>
        /// what process() did. Used by the endpoint for logging and by tests.
        /// </summary>
        public class SmsNotificationResult {
            /// <summary>short description, for example "delivery Delivered", "inbound OptOut", "duplicate", "ignored unknown message"</summary>
            public string action { get; set; } = "";
            /// <summary>the log row updated or inserted, 0 if none</summary>
            public int textMessageLogId { get; set; }
            /// <summary>the arguments of each event thrown (smsEventName is the event name)</summary>
            public List<Dictionary<string, string>> eventsThrown { get; set; } = new List<Dictionary<string, string>>();
        }
        //
        //====================================================================================================
        /// <summary>
        /// every argument key passed with the text message events. All are always present.
        /// </summary>
        public static readonly string[] eventArgumentKeys = {
            "smsEventName", "smsTextMessageLogId", "smsMemberId", "smsMemberIdList", "smsPhone", "smsBody", "smsSource", "smsIsTest",
            "smsStatus", "smsStatusCode", "smsStatusDetail", "smsLinkUrl", "smsKeyword", "smsOptOutSource"
        };
        //
        /// <summary>delivery event types that mean the message failed (AWS list, verify)</summary>
        private static readonly HashSet<string> failedEventTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "TEXT_INVALID", "TEXT_INVALID_MESSAGE", "TEXT_UNREACHABLE", "TEXT_CARRIER_UNREACHABLE", "TEXT_BLOCKED", "TEXT_CARRIER_BLOCKED",
            "TEXT_SPAM", "TEXT_UNKNOWN", "TEXT_TTL_EXPIRED", "TEXT_PROTECT_BLOCKED"
        };
        //
        //====================================================================================================
        /// <summary>
        /// true if the topic arn is in the comma separated allow-list (trimmed, exact ordinal match). An empty allow-list allows nothing.
        /// </summary>
        public static bool isTopicAllowed(string topicArn, string allowList) {
            if (string.IsNullOrEmpty(topicArn) || string.IsNullOrWhiteSpace(allowList)) { return false; }
            foreach (string allowed in allowList.Split(',')) {
                if (allowed.Trim().Equals(topicArn, StringComparison.Ordinal)) { return true; }
            }
            return false;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Process the Message of an SNS Notification. A message with eventType is a delivery event, a message with inboundMessageId or originationNumber is an inbound text. Anything else is ignored.
        /// Exceptions are thrown to the caller (the endpoint returns 500 so SNS retries).
        /// </summary>
        public static SmsNotificationResult process(CoreController core, string messageJson) {
            if (string.IsNullOrWhiteSpace(messageJson)) { return new SmsNotificationResult { action = "ignored empty message" }; }
            JObject json;
            try {
                json = JObject.Parse(messageJson);
            } catch (JsonException) {
                return new SmsNotificationResult { action = "ignored message that is not JSON" };
            }
            if (hasKey(json, "eventType")) {
                SmsDeliveryEventModel deliveryEvent = toObject<SmsDeliveryEventModel>(core, json);
                if (deliveryEvent == null) { return new SmsNotificationResult { action = "ignored delivery event that could not be parsed" }; }
                return processDeliveryEvent(core, deliveryEvent);
            }
            if (hasKey(json, "inboundMessageId") || hasKey(json, "originationNumber")) {
                SmsInboundMessageModel inbound = toObject<SmsInboundMessageModel>(core, json);
                if (inbound == null) { return new SmsNotificationResult { action = "ignored inbound message that could not be parsed" }; }
                return processInbound(core, inbound);
            }
            return new SmsNotificationResult { action = "ignored unknown message" };
        }
        //
        /// <summary>
        /// tolerant conversion, null if a field has an unexpected type (a retry would fail the same way, so the message is ignored, not retried)
        /// </summary>
        private static T toObject<T>(CoreController core, JObject json) where T : class {
            try {
                return json.ToObject<T>();
            } catch (Exception ex) when (ex is JsonException || ex is FormatException || ex is InvalidCastException || ex is ArgumentException) {
                logger.Warn($"{core.logCommonMessage},SMS notification message could not be parsed as {typeof(T).Name}, {ex.Message}");
                return null;
            }
        }
        //
        private static bool hasKey(JObject json, string key)
            => json.Properties().Any(p => p.Name.Equals(key, StringComparison.OrdinalIgnoreCase));
        //
        //====================================================================================================
        /// <summary>
        /// A delivery event. Updates the outbound log row. Delivered and Failed are final: a later event for the row is ignored (replays are harmless).
        /// Throws Text Message Status Changed only when the row changes to Delivered or Failed, after the row is saved.
        /// </summary>
        public static SmsNotificationResult processDeliveryEvent(CoreController core, SmsDeliveryEventModel deliveryEvent) {
            var result = new SmsNotificationResult();
            if (deliveryEvent == null || string.IsNullOrWhiteSpace(deliveryEvent.eventType)) {
                result.action = "ignored delivery event without eventType";
                return result;
            }
            //
            // -- 1. defence in depth, an event for another app
            string contextApp = getContextValue(deliveryEvent.context, "app");
            if (!string.IsNullOrEmpty(contextApp) && !contextApp.Equals(core.appConfig.name, StringComparison.OrdinalIgnoreCase)) {
                result.action = "ignored delivery event for another app";
                return result;
            }
            //
            // -- 2. find the row, by message id, then by the log guid in the context
            TextMessageLogModel log = TextMessageLogModel.getByProviderMessageId(core.cpParent, deliveryEvent.messageId);
            if (log == null) {
                string contextLog = getContextValue(deliveryEvent.context, "log");
                if (isGuid(contextLog)) {
                    log = DbBaseModel.create<TextMessageLogModel>(core.cpParent, contextLog);
                }
            }
            if (log == null || log.isInbound) {
                logger.Info($"{core.logCommonMessage},SMS delivery event [{deliveryEvent.eventType}] for an unknown message id, ignored");
                result.action = "ignored delivery event for unknown message";
                return result;
            }
            result.textMessageLogId = log.id;
            //
            // -- 4. Delivered and Failed are final
            if (log.statusId == (int)TextMessageLogModel.StatusEnum.Delivered || log.statusId == (int)TextMessageLogModel.StatusEnum.Failed) {
                result.action = "ignored delivery event, row is final";
                return result;
            }
            //
            // -- 3. map the event type
            TextMessageLogModel.StatusEnum? newStatus = null;
            if (deliveryEvent.eventType.Equals("TEXT_DELIVERED", StringComparison.OrdinalIgnoreCase)) {
                newStatus = TextMessageLogModel.StatusEnum.Delivered;
            } else if (failedEventTypes.Contains(deliveryEvent.eventType)) {
                newStatus = TextMessageLogModel.StatusEnum.Failed;
            }
            //
            // -- 5. save
            log.providerStatusCode = truncate(deliveryEvent.eventType, 255);
            log.statusDetail = truncate(deliveryEvent.messageStatusDescription ?? "", 255);
            if (deliveryEvent.totalMessagePrice.HasValue) { log.price = deliveryEvent.totalMessagePrice.Value; }
            if (string.IsNullOrEmpty(log.providerMessageId) && !string.IsNullOrEmpty(deliveryEvent.messageId)) { log.providerMessageId = truncate(deliveryEvent.messageId, 255); }
            if (newStatus.HasValue) {
                log.statusId = (int)newStatus.Value;
                log.statusDate = core.dateTimeNowMockable;
                log.sendStatus = truncate((newStatus.Value == TextMessageLogModel.StatusEnum.Delivered) ? "Delivered" : $"Failed: {deliveryEvent.eventType} {deliveryEvent.messageStatusDescription}".Trim(), 255);
            }
            log.save(core.cpParent);
            if (!newStatus.HasValue) {
                result.action = $"delivery {deliveryEvent.eventType}, no status change";
                return result;
            }
            //
            // -- 6. event, after the row is saved
            var args = getEventArguments(eventNameTextMessageStatusChanged, log);
            args["smsStatus"] = (newStatus.Value == TextMessageLogModel.StatusEnum.Delivered) ? "Delivered" : "Failed";
            args["smsStatusCode"] = log.providerStatusCode ?? "";
            args["smsStatusDetail"] = log.statusDetail ?? "";
            throwEvent(core, args, result);
            result.action = $"delivery {args["smsStatus"]}";
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// A two-way (inbound) message. Saves an inbound log row, mirrors STOP into the block list and START out of it, then throws
        /// Text Message Opt Out (STOP) or Text Message Received (anything else, smsKeyword START/HELP/empty). The site never replies.
        /// </summary>
        public static SmsNotificationResult processInbound(CoreController core, SmsInboundMessageModel inbound) {
            var result = new SmsNotificationResult();
            if (inbound == null || string.IsNullOrWhiteSpace(inbound.originationNumber)) {
                result.action = "ignored inbound message without originationNumber";
                return result;
            }
            //
            // -- 1. idempotency
            if (!string.IsNullOrWhiteSpace(inbound.inboundMessageId)) {
                TextMessageLogModel existing = DbBaseModel.createFirstOfList<TextMessageLogModel>(core.cpParent, $"(isInbound<>0)and(providerMessageId={core.cpParent.Db.EncodeSQLText(inbound.inboundMessageId)})", "id");
                if (existing != null) {
                    result.action = "duplicate inbound message";
                    result.textMessageLogId = existing.id;
                    return result;
                }
            }
            //
            // -- 2. normalize
            string defaultCountryCode = TextMessageController.getDefaultCountryCode(core);
            string fromPhone = SmsPhoneController.toE164(inbound.originationNumber, defaultCountryCode);
            if (string.IsNullOrEmpty(fromPhone)) { fromPhone = truncate(inbound.originationNumber.Trim(), 255); }
            string toPhone = SmsPhoneController.toE164(inbound.destinationNumber, defaultCountryCode);
            if (string.IsNullOrEmpty(toPhone)) { toPhone = truncate((inbound.destinationNumber ?? "").Trim(), 255); }
            string body = inbound.messageBody ?? "";
            if (body.Length > TextMessageController.smsMaxBodyCharacters) { body = body.Substring(0, TextMessageController.smsMaxBodyCharacters); }
            //
            // -- 3. contact match, the person last texted at this number, else the newest person with this number
            List<int> memberIdList = SmsInboundController.getPeopleIdsByPhone(core, fromPhone);
            int memberId = TextMessageLogModel.getLastOutboundToPhone(core.cpParent, fromPhone)?.memberId ?? 0;
            if (memberId == 0) { memberId = memberIdList.FirstOrDefault(); }
            if (memberId > 0 && !memberIdList.Contains(memberId)) { memberIdList.Insert(0, memberId); }
            //
            // -- 5 (classify first so the row records it). 4. save the inbound row before any event
            SmsKeywordController.KeywordEnum keyword = SmsKeywordController.classify(body, inbound.messageKeyword);
            TextMessageLogModel log = DbBaseModel.addDefault<TextMessageLogModel>(core.cpParent);
            log.name = truncate($"Inbound {fromPhone}", 255);
            log.isInbound = true;
            log.isTest = false;
            log.source = "Inbound";
            log.fromPhone = fromPhone;
            log.toPhone = toPhone;
            log.body = body;
            log.providerMessageId = truncate(inbound.inboundMessageId ?? "", 255);
            log.memberId = memberId;
            log.statusId = (int)TextMessageLogModel.StatusEnum.Delivered;
            log.statusDate = core.dateTimeNowMockable;
            log.providerId = (int)TextMessageLogModel.ProviderEnum.Aws;
            log.sendStatus = (keyword == SmsKeywordController.KeywordEnum.None) ? "Received" : $"Received keyword {keyword}";
            log.providerStatusCode = truncate(inbound.messageKeyword ?? "", 255);
            log.save(core.cpParent);
            result.textMessageLogId = log.id;
            //
            // -- 5. keywords. Block list changes happen before the event, so a catcher sees the final block state
            string memberIdListText = string.Join(",", memberIdList);
            switch (keyword) {
                case SmsKeywordController.KeywordEnum.OptOut: {
                        TextMessageBlockListModel.add(core.cpParent, fromPhone, (int)TextMessageBlockListModel.BlockSourceEnum.KeywordStop, memberId, body);
                        var args = getEventArguments(eventNameTextMessageOptOut, log);
                        args["smsMemberIdList"] = memberIdListText;
                        args["smsOptOutSource"] = "Keyword";
                        throwEvent(core, args, result);
                        result.action = "inbound OptOut";
                        return result;
                    }
                case SmsKeywordController.KeywordEnum.OptIn: {
                        TextMessageBlockListModel.remove(core.cpParent, fromPhone);
                        break;
                    }
                default: {
                        break;
                    }
            }
            var receivedArgs = getEventArguments(eventNameTextMessageReceived, log);
            receivedArgs["smsMemberIdList"] = memberIdListText;
            receivedArgs["smsKeyword"] = SmsKeywordController.getEventKeyword(keyword);
            throwEvent(core, receivedArgs, result);
            result.action = $"inbound {keyword}";
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// The arguments for a text message event: every key in eventArgumentKeys, empty, with smsEventName set.
        /// </summary>
        public static Dictionary<string, string> getEventArguments(string eventName) {
            var args = new Dictionary<string, string>();
            foreach (string key in eventArgumentKeys) { args[key] = ""; }
            args["smsEventName"] = eventName ?? "";
            return args;
        }
        //
        //====================================================================================================
        /// <summary>
        /// The arguments for a text message event, filled from a log row: smsTextMessageLogId, smsMemberId, smsPhone (the contact: fromPhone for inbound, toPhone for outbound), smsSource, smsIsTest ("1"/"0"),
        /// and smsBody for inbound rows only. The other keys are empty.
        /// </summary>
        public static Dictionary<string, string> getEventArguments(string eventName, TextMessageLogModel log) {
            var args = getEventArguments(eventName);
            if (log == null) { return args; }
            args["smsTextMessageLogId"] = log.id.ToString();
            args["smsMemberId"] = log.memberId.ToString();
            args["smsMemberIdList"] = (log.memberId > 0) ? log.memberId.ToString() : "";
            args["smsPhone"] = (log.isInbound ? log.fromPhone : log.toPhone) ?? "";
            args["smsSource"] = log.source ?? "";
            args["smsIsTest"] = log.isTest ? "1" : "0";
            if (log.isInbound) { args["smsBody"] = log.body ?? ""; }
            return args;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Throw a text message event. A catcher failure never stops the caller. Records the arguments in the result, if one is given.
        /// </summary>
        public static string throwEvent(CoreController core, Dictionary<string, string> args, SmsNotificationResult result) {
            result?.eventsThrown.Add(args);
            try {
                return EventController.throwEventByName(core, args["smsEventName"], args);
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage},throwEvent [{args["smsEventName"]}]");
                return "";
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Throw Text Message Opt Out for a send the provider rejected because the recipient opted out (send pipeline step 9).
        /// The number is already on the block list and the outbound row is already saved as Blocked. smsBody is the provider code.
        /// </summary>
        public static void throwProviderOptOutEvent(CoreController core, TextMessageLogModel log, string e164, int memberId, string providerCode) {
            try {
                var args = getEventArguments(eventNameTextMessageOptOut, log);
                List<int> memberIdList = SmsInboundController.getPeopleIdsByPhone(core, e164);
                if (memberId > 0 && !memberIdList.Contains(memberId)) { memberIdList.Insert(0, memberId); }
                args["smsMemberId"] = memberId.ToString();
                args["smsMemberIdList"] = string.Join(",", memberIdList);
                args["smsPhone"] = e164 ?? "";
                args["smsBody"] = providerCode ?? "";
                args["smsOptOutSource"] = "Provider";
                throwEvent(core, args, null);
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage},throwProviderOptOutEvent");
            }
        }
        //
        //====================================================================================================
        //
        private static string getContextValue(Dictionary<string, string> context, string key) {
            if (context == null) { return ""; }
            foreach (var kvp in context) {
                if (kvp.Key != null && kvp.Key.Equals(key, StringComparison.OrdinalIgnoreCase)) { return kvp.Value ?? ""; }
            }
            return "";
        }
        //
        private static bool isGuid(string value) => !string.IsNullOrEmpty(value) && Guid.TryParse(value, out _);
        //
        private static string truncate(string text, int maxLength) {
            if (string.IsNullOrEmpty(text)) { return ""; }
            return (text.Length <= maxLength) ? text : text.Substring(0, maxLength);
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
