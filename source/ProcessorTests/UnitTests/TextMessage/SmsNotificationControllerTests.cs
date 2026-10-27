using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using static Contensive.Processor.Tests.TestConstants;

namespace Contensive.Processor.Tests.UnitTests.TextMessage;

/// <summary>
/// SmsNotificationController: topic allow-list (pure), delivery events and inbound messages (test app).
/// The fixture JSON follows the AWS End User Messaging SMS event and two-way formats (field names: verify against captured payloads).
/// </summary>
[TestClass]
public class SmsNotificationControllerTests {
    //
    private static string getTestPhone() => $"+1703{GenericController.getRandomInteger(8999999) + 1000000}";
    //
    private static TextMessageLogModel addOutboundLog(CPClass cp, string e164, string providerMessageId) {
        var log = DbBaseModel.addDefault<TextMessageLogModel>(cp);
        log.name = "unit test";
        log.toPhone = e164;
        log.body = "unit test body";
        log.source = "unit test";
        log.providerMessageId = providerMessageId;
        log.statusId = (int)TextMessageLogModel.StatusEnum.Sent;
        log.isInbound = false;
        log.isTest = false;
        log.save(cp);
        return log;
    }
    //
    private static string getDeliveryJson(string eventType, string messageId, string e164, string app, string logGuid)
        => $$"""
        {"eventType":"{{eventType}}","eventVersion":"1.0","eventTimestamp":1793113200000,"isFinal":true,"originationPhoneNumber":"+12025550100","destinationPhoneNumber":"{{e164}}","isoCountryCode":"US","messageId":"{{messageId}}","messageEncoding":"GSM","messageType":"TRANSACTIONAL","messageStatus":"{{eventType}}","messageStatusDescription":"status for {{eventType}}","totalMessageParts":1,"totalMessagePrice":0.00581,"context":{"app":"{{app}}","log":"{{logGuid}}"}}
        """;
    //
    private static string getInboundJson(string from, string body, string inboundMessageId)
        => $$"""
        {"originationNumber":"{{from}}","destinationNumber":"+12025550100","messageKeyword":"KEYWORD_123456789012","messageBody":"{{body}}","inboundMessageId":"{{inboundMessageId}}","previousPublishedMessageId":"abc"}
        """;
    //
    private static void assertAllKeys(Dictionary<string, string> args) {
        foreach (string key in SmsNotificationController.eventArgumentKeys) {
            Assert.IsTrue(args.ContainsKey(key), $"missing event argument [{key}]");
            Assert.IsNotNull(args[key], $"null event argument [{key}]");
        }
    }
    //
    // -- pure
    //
    [TestMethod]
    public void isTopicAllowed_exactMatchOnly() {
        const string arn = "arn:aws:sns:us-east-1:123456789012:contensive-sms-c5test";
        Assert.IsTrue(SmsNotificationController.isTopicAllowed(arn, arn));
        Assert.IsTrue(SmsNotificationController.isTopicAllowed(arn, $"arn:aws:sns:us-east-1:123456789012:other , {arn} "));
        Assert.IsFalse(SmsNotificationController.isTopicAllowed(arn, ""));
        Assert.IsFalse(SmsNotificationController.isTopicAllowed(arn, "  "));
        Assert.IsFalse(SmsNotificationController.isTopicAllowed(arn, arn.ToUpperInvariant()));
        Assert.IsFalse(SmsNotificationController.isTopicAllowed(arn, $"{arn}-2"));
        Assert.IsFalse(SmsNotificationController.isTopicAllowed($"{arn}-2", arn));
        Assert.IsFalse(SmsNotificationController.isTopicAllowed("", arn));
        Assert.IsFalse(SmsNotificationController.isTopicAllowed(null, arn));
    }
    //
    [TestMethod]
    public void getEventArguments_allKeysEmpty() {
        var args = SmsNotificationController.getEventArguments("Text Message Received");
        assertAllKeys(args);
        Assert.AreEqual("Text Message Received", args["smsEventName"]);
        Assert.AreEqual("", args["smsBody"]);
        Assert.AreEqual(SmsNotificationController.eventArgumentKeys.Length, args.Count);
    }
    //
    [TestMethod]
    public void getEventArguments_fromOutboundLog_noBody() {
        var log = new TextMessageLogModel { id = 12, memberId = 34, toPhone = "+17035550100", fromPhone = "+12025550100", body = "secret", source = "CRM Send Text", isTest = true };
        var args = SmsNotificationController.getEventArguments("Text Message Status Changed", log);
        assertAllKeys(args);
        Assert.AreEqual("12", args["smsTextMessageLogId"]);
        Assert.AreEqual("34", args["smsMemberId"]);
        Assert.AreEqual("+17035550100", args["smsPhone"]);
        Assert.AreEqual("CRM Send Text", args["smsSource"]);
        Assert.AreEqual("1", args["smsIsTest"]);
        Assert.AreEqual("", args["smsBody"]);
    }
    //
    [TestMethod]
    public void process_notJsonOrUnknown_ignored() {
        using (CPClass cp = new(testAppName)) {
            Assert.AreEqual(0, SmsNotificationController.process(cp.core, "not json").eventsThrown.Count);
            Assert.AreEqual(0, SmsNotificationController.process(cp.core, "{\"foo\":1}").eventsThrown.Count);
            Assert.AreEqual(0, SmsNotificationController.process(cp.core, "").eventsThrown.Count);
            Assert.AreEqual(0, SmsNotificationController.process(cp.core, "{\"eventType\":\"TEXT_DELIVERED\",\"context\":{\"app\":{\"nested\":1}}}").eventsThrown.Count);
        }
    }
    //
    // -- delivery events
    //
    [TestMethod]
    public void delivery_sentToDelivered_thenReplayAndFailedIgnored() {
        using (CPClass cp = new(testAppName)) {
            string e164 = getTestPhone();
            string messageId = Guid.NewGuid().ToString();
            TextMessageLogModel log = addOutboundLog(cp, e164, messageId);
            try {
                // act
                var result = SmsNotificationController.process(cp.core, getDeliveryJson("TEXT_DELIVERED", messageId, e164, cp.core.appConfig.name, ""));
                // assert, row saved before the event
                TextMessageLogModel saved = DbBaseModel.create<TextMessageLogModel>(cp, log.id);
                Assert.AreEqual((int)TextMessageLogModel.StatusEnum.Delivered, saved.statusId);
                Assert.AreEqual("TEXT_DELIVERED", saved.providerStatusCode);
                Assert.AreEqual("status for TEXT_DELIVERED", saved.statusDetail);
                Assert.AreEqual(0.00581, saved.price, 0.000001);
                Assert.AreEqual(1, result.eventsThrown.Count);
                var args = result.eventsThrown[0];
                assertAllKeys(args);
                Assert.AreEqual("Text Message Status Changed", args["smsEventName"]);
                Assert.AreEqual(log.id.ToString(), args["smsTextMessageLogId"]);
                Assert.AreEqual("Delivered", args["smsStatus"]);
                Assert.AreEqual("TEXT_DELIVERED", args["smsStatusCode"]);
                Assert.AreEqual(e164, args["smsPhone"]);
                Assert.AreEqual("unit test", args["smsSource"]);
                Assert.AreEqual("0", args["smsIsTest"]);
                //
                // -- replay, no change and no event
                var replay = SmsNotificationController.process(cp.core, getDeliveryJson("TEXT_DELIVERED", messageId, e164, cp.core.appConfig.name, ""));
                Assert.AreEqual(0, replay.eventsThrown.Count);
                //
                // -- a failure after delivered is ignored
                var failedAfter = SmsNotificationController.process(cp.core, getDeliveryJson("TEXT_CARRIER_UNREACHABLE", messageId, e164, cp.core.appConfig.name, ""));
                Assert.AreEqual(0, failedAfter.eventsThrown.Count);
                Assert.AreEqual((int)TextMessageLogModel.StatusEnum.Delivered, DbBaseModel.create<TextMessageLogModel>(cp, log.id).statusId);
            } finally {
                DbBaseModel.delete<TextMessageLogModel>(cp, log.id);
            }
        }
    }
    //
    [TestMethod]
    public void delivery_failed_andIntermediateNoEvent() {
        using (CPClass cp = new(testAppName)) {
            string e164 = getTestPhone();
            string messageId = Guid.NewGuid().ToString();
            TextMessageLogModel log = addOutboundLog(cp, e164, messageId);
            try {
                var pending = SmsNotificationController.process(cp.core, getDeliveryJson("TEXT_PENDING", messageId, e164, cp.core.appConfig.name, ""));
                Assert.AreEqual(0, pending.eventsThrown.Count);
                Assert.AreEqual((int)TextMessageLogModel.StatusEnum.Sent, DbBaseModel.create<TextMessageLogModel>(cp, log.id).statusId);
                //
                var failed = SmsNotificationController.process(cp.core, getDeliveryJson("TEXT_CARRIER_UNREACHABLE", messageId, e164, cp.core.appConfig.name, ""));
                Assert.AreEqual(1, failed.eventsThrown.Count);
                Assert.AreEqual("Failed", failed.eventsThrown[0]["smsStatus"]);
                Assert.AreEqual("TEXT_CARRIER_UNREACHABLE", failed.eventsThrown[0]["smsStatusCode"]);
                Assert.AreEqual((int)TextMessageLogModel.StatusEnum.Failed, DbBaseModel.create<TextMessageLogModel>(cp, log.id).statusId);
            } finally {
                DbBaseModel.delete<TextMessageLogModel>(cp, log.id);
            }
        }
    }
    //
    [TestMethod]
    public void delivery_otherApp_unknownMessage_ignored_contextLogGuid_found() {
        using (CPClass cp = new(testAppName)) {
            string e164 = getTestPhone();
            string messageId = Guid.NewGuid().ToString();
            TextMessageLogModel log = addOutboundLog(cp, e164, "");
            try {
                // -- another app
                var otherApp = SmsNotificationController.process(cp.core, getDeliveryJson("TEXT_DELIVERED", messageId, e164, "someotherapp", log.ccguid));
                Assert.AreEqual(0, otherApp.eventsThrown.Count);
                // -- unknown message id and no log guid
                var unknown = SmsNotificationController.process(cp.core, getDeliveryJson("TEXT_DELIVERED", Guid.NewGuid().ToString(), e164, cp.core.appConfig.name, ""));
                Assert.AreEqual(0, unknown.eventsThrown.Count);
                // -- event before the message id was saved, found by the log guid in the context
                var byGuid = SmsNotificationController.process(cp.core, getDeliveryJson("TEXT_DELIVERED", messageId, e164, cp.core.appConfig.name, log.ccguid));
                Assert.AreEqual(1, byGuid.eventsThrown.Count);
                Assert.AreEqual(log.id, byGuid.textMessageLogId);
                Assert.AreEqual(messageId, DbBaseModel.create<TextMessageLogModel>(cp, log.id).providerMessageId);
            } finally {
                DbBaseModel.delete<TextMessageLogModel>(cp, log.id);
            }
        }
    }
    //
    // -- inbound
    //
    [TestMethod]
    public void inbound_stop_blocksAndThrowsOptOut_duplicateIgnored() {
        using (CPClass cp = new(testAppName)) {
            string e164 = getTestPhone();
            string inboundId = Guid.NewGuid().ToString();
            int logId = 0;
            try {
                var result = SmsNotificationController.process(cp.core, getInboundJson(e164, " Stop. ", inboundId));
                logId = result.textMessageLogId;
                // -- inbound row saved before the event
                TextMessageLogModel log = DbBaseModel.create<TextMessageLogModel>(cp, logId);
                Assert.IsNotNull(log);
                Assert.IsTrue(log.isInbound);
                Assert.AreEqual(e164, log.fromPhone);
                Assert.AreEqual("+12025550100", log.toPhone);
                Assert.AreEqual(" Stop. ", log.body);
                Assert.AreEqual("Inbound", log.source);
                Assert.AreEqual(inboundId, log.providerMessageId);
                Assert.AreEqual((int)TextMessageLogModel.StatusEnum.Delivered, log.statusId);
                // -- block list
                Assert.IsTrue(TextMessageBlockListModel.isBlocked(cp, e164));
                // -- event
                Assert.AreEqual(1, result.eventsThrown.Count);
                var args = result.eventsThrown[0];
                assertAllKeys(args);
                Assert.AreEqual("Text Message Opt Out", args["smsEventName"]);
                Assert.AreEqual(logId.ToString(), args["smsTextMessageLogId"]);
                Assert.AreEqual("Keyword", args["smsOptOutSource"]);
                Assert.AreEqual(e164, args["smsPhone"]);
                Assert.AreEqual(" Stop. ", args["smsBody"]);
                Assert.AreEqual("Inbound", args["smsSource"]);
                //
                // -- duplicate
                var duplicate = SmsNotificationController.process(cp.core, getInboundJson(e164, "STOP", inboundId));
                Assert.AreEqual(0, duplicate.eventsThrown.Count);
                Assert.AreEqual(logId, duplicate.textMessageLogId);
                //
                // -- start unblocks and throws received with START
                var start = SmsNotificationController.process(cp.core, getInboundJson(e164, "START", Guid.NewGuid().ToString()));
                Assert.IsFalse(TextMessageBlockListModel.isBlocked(cp, e164));
                Assert.AreEqual(1, start.eventsThrown.Count);
                Assert.AreEqual("Text Message Received", start.eventsThrown[0]["smsEventName"]);
                Assert.AreEqual("START", start.eventsThrown[0]["smsKeyword"]);
                Assert.AreEqual("", start.eventsThrown[0]["smsOptOutSource"]);
                DbBaseModel.delete<TextMessageLogModel>(cp, start.textMessageLogId);
            } finally {
                TextMessageBlockListModel.remove(cp, e164);
                if (logId > 0) { DbBaseModel.delete<TextMessageLogModel>(cp, logId); }
            }
        }
    }
    //
    [TestMethod]
    public void inbound_reply_matchesLastTextedPerson_throwsReceived() {
        using (CPClass cp = new(testAppName)) {
            string e164 = getTestPhone();
            var person = DbBaseModel.addDefault<PersonModel>(cp);
            person.name = "sms unit test";
            person.cellPhone = e164.Substring(2);
            person.save(cp);
            TextMessageLogModel outbound = addOutboundLog(cp, e164, Guid.NewGuid().ToString());
            outbound.memberId = person.id;
            outbound.save(cp);
            int inboundLogId = 0;
            try {
                var result = SmsNotificationController.process(cp.core, getInboundJson(e164, "Hi, call me back", Guid.NewGuid().ToString()));
                inboundLogId = result.textMessageLogId;
                Assert.AreEqual(person.id, DbBaseModel.create<TextMessageLogModel>(cp, inboundLogId).memberId);
                Assert.AreEqual(1, result.eventsThrown.Count);
                var args = result.eventsThrown[0];
                assertAllKeys(args);
                Assert.AreEqual("Text Message Received", args["smsEventName"]);
                Assert.AreEqual(person.id.ToString(), args["smsMemberId"]);
                Assert.IsTrue($",{args["smsMemberIdList"]},".Contains($",{person.id},"));
                Assert.AreEqual("Hi, call me back", args["smsBody"]);
                Assert.AreEqual("", args["smsKeyword"]);
                Assert.IsFalse(TextMessageBlockListModel.isBlocked(cp, e164));
            } finally {
                if (inboundLogId > 0) { DbBaseModel.delete<TextMessageLogModel>(cp, inboundLogId); }
                DbBaseModel.delete<TextMessageLogModel>(cp, outbound.id);
                DbBaseModel.delete<PersonModel>(cp, person.id);
            }
        }
    }
    //
    [TestMethod]
    public void inbound_help_throwsReceivedWithHelp() {
        using (CPClass cp = new(testAppName)) {
            string e164 = getTestPhone();
            var result = SmsNotificationController.process(cp.core, getInboundJson(e164, "help", Guid.NewGuid().ToString()));
            try {
                Assert.AreEqual(1, result.eventsThrown.Count);
                Assert.AreEqual("HELP", result.eventsThrown[0]["smsKeyword"]);
                Assert.IsFalse(TextMessageBlockListModel.isBlocked(cp, e164));
            } finally {
                DbBaseModel.delete<TextMessageLogModel>(cp, result.textMessageLogId);
            }
        }
    }
    //
    private static TextMessageLogModel addInboundLog(CPClass cp, string e164, string body, string inboundId) {
        var log = DbBaseModel.addDefault<TextMessageLogModel>(cp);
        log.name = "unit test inbound";
        log.isInbound = true;
        log.fromPhone = e164;
        log.toPhone = "+12025550100";
        log.body = body;
        log.source = "Inbound";
        log.providerMessageId = inboundId;
        log.statusId = (int)TextMessageLogModel.StatusEnum.Delivered;
        log.save(cp);
        return log;
    }
    //
    /// <summary>
    /// S1: a STOP whose first attempt saved the row but did not block (partial failure) is blocked on the SNS retry, and the opt-out event is thrown
    /// </summary>
    [TestMethod]
    public void inbound_stopRetryAfterPartialFailure_blocks() {
        using (CPClass cp = new(testAppName)) {
            string e164 = getTestPhone();
            string inboundId = Guid.NewGuid().ToString();
            TextMessageLogModel existing = addInboundLog(cp, e164, "STOP", inboundId);
            try {
                Assert.IsFalse(TextMessageBlockListModel.isBlocked(cp, e164));
                var retry = SmsNotificationController.process(cp.core, getInboundJson(e164, "STOP", inboundId));
                Assert.AreEqual(existing.id, retry.textMessageLogId);
                Assert.IsTrue(TextMessageBlockListModel.isBlocked(cp, e164));
                Assert.AreEqual(1, retry.eventsThrown.Count);
                Assert.AreEqual("Text Message Opt Out", retry.eventsThrown[0]["smsEventName"]);
                Assert.AreEqual(existing.id.ToString(), retry.eventsThrown[0]["smsTextMessageLogId"]);
                Assert.AreEqual("Keyword", retry.eventsThrown[0]["smsOptOutSource"]);
                //
                // -- a second retry, already blocked, does nothing
                var again = SmsNotificationController.process(cp.core, getInboundJson(e164, "STOP", inboundId));
                Assert.AreEqual(0, again.eventsThrown.Count);
            } finally {
                TextMessageBlockListModel.remove(cp, e164);
                DbBaseModel.delete<TextMessageLogModel>(cp, existing.id);
            }
        }
    }
    //
    /// <summary>
    /// S1: a late retry of a STOP must not undo a later START
    /// </summary>
    [TestMethod]
    public void inbound_stopRetryAfterStart_notReblocked() {
        using (CPClass cp = new(testAppName)) {
            string e164 = getTestPhone();
            string stopId = Guid.NewGuid().ToString();
            TextMessageLogModel stop = addInboundLog(cp, e164, "STOP", stopId);
            TextMessageLogModel start = addInboundLog(cp, e164, "START", Guid.NewGuid().ToString());
            try {
                var retry = SmsNotificationController.process(cp.core, getInboundJson(e164, "STOP", stopId));
                Assert.AreEqual(0, retry.eventsThrown.Count);
                Assert.IsFalse(TextMessageBlockListModel.isBlocked(cp, e164));
            } finally {
                TextMessageBlockListModel.remove(cp, e164);
                DbBaseModel.delete<TextMessageLogModel>(cp, stop.id);
                DbBaseModel.delete<TextMessageLogModel>(cp, start.id);
            }
        }
    }
    //
    /// <summary>
    /// C7: a body that looks like a date is stored exactly as sent
    /// </summary>
    [TestMethod]
    public void inbound_dateLikeBody_notReformatted() {
        using (CPClass cp = new(testAppName)) {
            string e164 = getTestPhone();
            var result = SmsNotificationController.process(cp.core, getInboundJson(e164, "2026-10-27T15:00:00Z", Guid.NewGuid().ToString()));
            try {
                Assert.AreEqual("2026-10-27T15:00:00Z", DbBaseModel.create<TextMessageLogModel>(cp, result.textMessageLogId).body);
            } finally {
                DbBaseModel.delete<TextMessageLogModel>(cp, result.textMessageLogId);
            }
        }
    }
}
