using Contensive.BaseModels;
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using Contensive.Processor.Controllers.Build;
using Contensive.Processor.Models.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using static Contensive.Processor.Tests.TestConstants;

namespace Contensive.Processor.Tests.UnitTests.TextMessage;

/// <summary>
/// integration tests of the send pipeline (cp.SMS.SendText), the block list and the block-list migration. 
/// Uses the test app with core.mockTextMessages so no provider is called.
/// </summary>
[TestClass]
public class TextMessageSendPipelineTests {
    //
    private static string getTestPhone() => $"703{GenericController.getRandomInteger(8999999) + 1000000}";
    //
    [TestMethod]
    public void sendText_mock_logsSentWithMessageId() {
        using (CPClass cp = new(testAppName)) {
            cp.core.mockTextMessages = true;
            string phone = getTestPhone();
            string e164 = SmsPhoneController.toE164(phone, "1");
            string body = $"pipeline test {GenericController.getRandomInteger()}";
            //
            SmsSendResultModel result = cp.SMS.SendText(new SmsSendRequestModel { toPhone = $"({phone.Substring(0, 3)}) {phone.Substring(3, 3)}-{phone.Substring(6)}", body = body, source = "unit test" });
            //
            Assert.IsTrue(result.success);
            Assert.AreEqual(SmsSendStatusEnum.Sent, result.status);
            Assert.AreEqual(e164, result.normalizedPhone);
            Assert.IsTrue(result.providerMessageId.StartsWith("mock-"));
            Assert.AreEqual(1, result.segments);
            Assert.IsTrue(result.textMessageLogId > 0);
            Assert.AreEqual(1, cp.core.mockTextMessageList.Count);
            Assert.AreEqual(e164, cp.core.mockTextMessageList.First().textMessageRequest.toPhone);
            TextMessageLogModel log = DbBaseModel.create<TextMessageLogModel>(cp, result.textMessageLogId);
            Assert.IsNotNull(log);
            Assert.AreEqual((int)TextMessageLogModel.StatusEnum.Sent, log.statusId);
            Assert.AreEqual(result.providerMessageId, log.providerMessageId);
            Assert.AreEqual(e164, log.toPhone);
            Assert.AreEqual(body, log.body);
            Assert.AreEqual("unit test", log.source);
            Assert.AreEqual("OK", log.sendStatus);
            Assert.AreEqual(1, log.segments);
            Assert.IsFalse(log.isInbound);
            Assert.AreEqual(log.id, TextMessageLogModel.getByProviderMessageId(cp, result.providerMessageId).id);
            DbBaseModel.delete<TextMessageLogModel>(cp, log.id);
        }
    }
    //
    [TestMethod]
    public void sendText_invalidPhone() {
        using (CPClass cp = new(testAppName)) {
            cp.core.mockTextMessages = true;
            SmsSendResultModel result = cp.SMS.SendText(new SmsSendRequestModel { toPhone = "123", body = "hello", source = "unit test" });
            Assert.IsFalse(result.success);
            Assert.AreEqual(SmsSendStatusEnum.InvalidPhone, result.status);
            Assert.AreEqual(0, cp.core.mockTextMessageList.Count);
            TextMessageLogModel log = DbBaseModel.create<TextMessageLogModel>(cp, result.textMessageLogId);
            Assert.AreEqual((int)TextMessageLogModel.StatusEnum.NotSent, log.statusId);
            DbBaseModel.delete<TextMessageLogModel>(cp, log.id);
        }
    }
    //
    [TestMethod]
    public void sendText_blockList_blocksAndUnblocks() {
        using (CPClass cp = new(testAppName)) {
            cp.core.mockTextMessages = true;
            string phone = getTestPhone();
            string e164 = SmsPhoneController.toE164(phone, "1");
            try {
                TextMessageBlockListModel.add(cp, e164, (int)TextMessageBlockListModel.BlockSourceEnum.KeywordStop, 0, "unit test");
                //
                SmsSendResultModel blocked = cp.SMS.SendText(new SmsSendRequestModel { toPhone = phone, body = "hello", source = "unit test" });
                Assert.IsFalse(blocked.success);
                Assert.AreEqual(SmsSendStatusEnum.Blocked, blocked.status);
                Assert.AreEqual(0, cp.core.mockTextMessageList.Count);
                TextMessageLogModel blockedLog = DbBaseModel.create<TextMessageLogModel>(cp, blocked.textMessageLogId);
                Assert.AreEqual((int)TextMessageLogModel.StatusEnum.Blocked, blockedLog.statusId);
                DbBaseModel.delete<TextMessageLogModel>(cp, blockedLog.id);
                //
                TextMessageBlockListModel.remove(cp, e164);
                SmsSendResultModel sent = cp.SMS.SendText(new SmsSendRequestModel { toPhone = phone, body = "hello", source = "unit test" });
                Assert.IsTrue(sent.success);
                DbBaseModel.delete<TextMessageLogModel>(cp, sent.textMessageLogId);
            } finally {
                TextMessageBlockListModel.remove(cp, e164);
            }
        }
    }
    //
    [TestMethod]
    public void sendText_personBlockFlag_blocks() {
        using (CPClass cp = new(testAppName)) {
            cp.core.mockTextMessages = true;
            PersonModel person = DbBaseModel.addDefault<PersonModel>(cp);
            person.blockTextMessage = true;
            person.save(cp);
            try {
                SmsSendResultModel result = cp.SMS.SendText(new SmsSendRequestModel { toPhone = getTestPhone(), toMemberId = person.id, body = "hello", source = "unit test" });
                Assert.IsFalse(result.success);
                Assert.AreEqual(SmsSendStatusEnum.Blocked, result.status);
                Assert.AreEqual(0, cp.core.mockTextMessageList.Count);
                DbBaseModel.delete<TextMessageLogModel>(cp, result.textMessageLogId);
            } finally {
                DbBaseModel.delete<PersonModel>(cp, person.id);
            }
        }
    }
    //
    [TestMethod]
    public void sendText_providerOff_isDisabledAndInert() {
        using (CPClass cp = new(testAppName)) {
            cp.core.mockTextMessages = false;
            int originalProvider = cp.Site.GetInteger("SMS Provider Id", 2);
            cp.Site.SetProperty("SMS Provider Id", 0);
            try {
                Assert.IsFalse(cp.SMS.IsEnabled);
                SmsSendResultModel result = cp.SMS.SendText(new SmsSendRequestModel { toPhone = getTestPhone(), body = "hello", source = "unit test" });
                Assert.IsFalse(result.success);
                Assert.AreEqual(SmsSendStatusEnum.Disabled, result.status);
                Assert.IsFalse(string.IsNullOrEmpty(result.userError));
                TextMessageLogModel log = DbBaseModel.create<TextMessageLogModel>(cp, result.textMessageLogId);
                Assert.AreEqual((int)TextMessageLogModel.StatusEnum.NotSent, log.statusId);
                DbBaseModel.delete<TextMessageLogModel>(cp, log.id);
            } finally {
                cp.Site.SetProperty("SMS Provider Id", originalProvider);
            }
        }
    }
    //
    [TestMethod]
    public void sendText_maxSegments_enforcedOnlyWhenRequested() {
        using (CPClass cp = new(testAppName)) {
            cp.core.mockTextMessages = true;
            string body = new string('a', 153 * 6 + 1);
            Assert.AreEqual(7, SmsSegmentController.getInfo(body).segments);
            //
            // -- interactive send, default max is 6
            SmsSendResultModel tooLong = cp.SMS.SendText(new SmsSendRequestModel { toPhone = getTestPhone(), body = body, source = "unit test" });
            Assert.IsFalse(tooLong.success);
            Assert.AreEqual(SmsSendStatusEnum.TooLong, tooLong.status);
            Assert.AreEqual(0, cp.core.mockTextMessageList.Count);
            DbBaseModel.delete<TextMessageLogModel>(cp, tooLong.textMessageLogId);
            //
            // -- queue path does not enforce
            var queued = TextMessageController.sendImmediate(cp.core, new TextMessageSendRequest { toPhone = getTestPhone(), textBody = body, source = "unit test" }, false);
            Assert.IsTrue(queued.success);
            Assert.AreEqual(7, queued.segments);
            DbBaseModel.delete<TextMessageLogModel>(cp, queued.textMessageLogId);
        }
    }
    //
    [TestMethod]
    public void sendText_longBodyTruncatedTo1600() {
        using (CPClass cp = new(testAppName)) {
            cp.core.mockTextMessages = true;
            var result = TextMessageController.sendImmediate(cp.core, new TextMessageSendRequest { toPhone = getTestPhone(), textBody = new string('a', 2000), source = "unit test" }, false);
            Assert.IsTrue(result.success);
            Assert.AreEqual(1600, cp.core.mockTextMessageList.First().textMessageRequest.textBody.Length);
            DbBaseModel.delete<TextMessageLogModel>(cp, result.textMessageLogId);
        }
    }
    //
    [TestMethod]
    public void obsoleteWrappers_stillSend() {
        using (CPClass cp = new(testAppName)) {
            cp.core.mockTextMessages = true;
            PersonModel person = DbBaseModel.addDefault<PersonModel>(cp);
            person.cellPhone = getTestPhone();
            person.blockTextMessage = false;
            person.save(cp);
            try {
#pragma warning disable CS0618
                Assert.IsTrue(cp.SMS.Send(getTestPhone(), "hello"));
                string userError = "";
                Assert.IsTrue(cp.SMS.SendUser(person.id, "hello", ref userError));
                Assert.AreEqual("", userError);
                Assert.IsFalse(cp.SMS.SendUser(0, "hello", ref userError));
                Assert.IsFalse(string.IsNullOrEmpty(userError));
#pragma warning restore CS0618
                Assert.AreEqual(2, cp.core.mockTextMessageList.Count);
            } finally {
                DbBaseModel.deleteRows<TextMessageLogModel>(cp, $"(memberId={person.id})or(source='cp.SMS.Send')");
                DbBaseModel.delete<PersonModel>(cp, person.id);
            }
        }
    }
    //
    [TestMethod]
    public void normalizePhone_and_segmentInfo_viaCp() {
        using (CPClass cp = new(testAppName)) {
            Assert.AreEqual("+17033039974", cp.SMS.NormalizePhone("(703) 303-9974"));
            Assert.AreEqual("", cp.SMS.NormalizePhone("nope"));
            Assert.AreEqual(1, cp.SMS.GetSegmentInfo("hello").segments);
        }
    }
    //
    [TestMethod]
    public void blockList_addIsIdempotent() {
        using (CPClass cp = new(testAppName)) {
            string e164 = SmsPhoneController.toE164(getTestPhone(), "1");
            try {
                Assert.IsTrue(TextMessageBlockListModel.add(cp, e164, (int)TextMessageBlockListModel.BlockSourceEnum.Admin, 0, ""));
                Assert.IsFalse(TextMessageBlockListModel.add(cp, e164, (int)TextMessageBlockListModel.BlockSourceEnum.Admin, 0, ""));
                Assert.AreEqual(1, DbBaseModel.getCount<TextMessageBlockListModel>(cp, $"(phone={cp.Db.EncodeSQLText(e164)})"));
                Assert.IsFalse(TextMessageBlockListModel.add(cp, "", (int)TextMessageBlockListModel.BlockSourceEnum.Admin, 0, ""));
                Assert.IsFalse(TextMessageBlockListModel.isBlocked(cp, ""));
            } finally {
                TextMessageBlockListModel.remove(cp, e164);
            }
            Assert.IsFalse(TextMessageBlockListModel.isBlocked(cp, e164));
        }
    }
    //
    [TestMethod]
    public void migrateBlockList_idempotent() {
        using (CPClass cp = new(testAppName)) {
            string phone1 = getTestPhone();
            string phone2 = getTestPhone();
            string e164_1 = SmsPhoneController.toE164(phone1, "1");
            string e164_2 = SmsPhoneController.toE164(phone2, "1");
            // -- legacy format: new-line, digits, tab, date. includes an invalid line and a blank date
            string fileContent = $"\r\n{phone1}\t1/2/2026 3:04:05 PM\r\n{phone2}\t\r\nabc\t1/2/2026\r\n12\t1/2/2026\r\n";
            try {
                TextMessageBlockListModel.remove(cp, e164_1);
                TextMessageBlockListModel.remove(cp, e164_2);
                //
                Assert.AreEqual(2, BuildDataMigrationController.migrateTextMessageBlockList(cp.core, fileContent));
                Assert.IsTrue(TextMessageBlockListModel.isBlocked(cp, e164_1));
                Assert.IsTrue(TextMessageBlockListModel.isBlocked(cp, e164_2));
                TextMessageBlockListModel row = DbBaseModel.createFirstOfList<TextMessageBlockListModel>(cp, $"(phone={cp.Db.EncodeSQLText(e164_1)})", "id");
                Assert.AreEqual((int)TextMessageBlockListModel.BlockSourceEnum.Migrated, row.blockSourceId);
                Assert.AreEqual(2026, row.dateBlocked.Value.Year);
                //
                // -- second run adds nothing, no duplicates
                Assert.AreEqual(0, BuildDataMigrationController.migrateTextMessageBlockList(cp.core, fileContent));
                Assert.AreEqual(1, DbBaseModel.getCount<TextMessageBlockListModel>(cp, $"(phone={cp.Db.EncodeSQLText(e164_1)})"));
                Assert.AreEqual(0, BuildDataMigrationController.migrateTextMessageBlockList(cp.core, ""));
                Assert.AreEqual(0, BuildDataMigrationController.migrateTextMessageBlockList(cp.core, null));
            } finally {
                TextMessageBlockListModel.remove(cp, e164_1);
                TextMessageBlockListModel.remove(cp, e164_2);
            }
        }
    }
    //
    /// <summary>
    /// the file migration is gated on the SMS Block List Migrated flag, not the build version: it runs while the flag is false, sets it after success, and does not run again
    /// </summary>
    [TestMethod]
    public void migrateBlockListOnce_gatedOnFlag() {
        using (CPClass cp = new(testAppName)) {
            string path = BuildDataMigrationController.legacyTextMessageBlockListPathFilename;
            string savedFile = cp.core.privateFiles.readFileText(path);
            bool savedFlag = cp.core.siteProperties.getBoolean(Constants.sitePropertySmsBlockListMigrated, false);
            string savedDataBuildVersion = cp.core.siteProperties.dataBuildVersion;
            string phone1 = getTestPhone();
            string phone2 = getTestPhone();
            string e164_1 = SmsPhoneController.toE164(phone1, "1");
            string e164_2 = SmsPhoneController.toE164(phone2, "1");
            try {
                TextMessageBlockListModel.remove(cp, e164_1);
                TextMessageBlockListModel.remove(cp, e164_2);
                // -- a newer build version must not skip it
                cp.core.siteProperties.dataBuildVersion = "99.1.1.1";
                cp.core.siteProperties.setProperty(Constants.sitePropertySmsBlockListMigrated, false);
                cp.core.privateFiles.saveFile(path, $"\r\n{phone1}\t1/2/2026 3:04:05 PM");
                //
                Assert.IsTrue(BuildDataMigrationController.migrateTextMessageBlockListOnce(cp.core, "unit test"));
                Assert.IsTrue(TextMessageBlockListModel.isBlocked(cp, e164_1));
                Assert.IsTrue(cp.core.siteProperties.getBoolean(Constants.sitePropertySmsBlockListMigrated, false));
                //
                // -- flag set, a later file change is not read again
                cp.core.privateFiles.saveFile(path, $"\r\n{phone2}\t1/2/2026 3:04:05 PM");
                Assert.IsTrue(BuildDataMigrationController.migrateTextMessageBlockListOnce(cp.core, "unit test"));
                Assert.IsFalse(TextMessageBlockListModel.isBlocked(cp, e164_2));
            } finally {
                TextMessageBlockListModel.remove(cp, e164_1);
                TextMessageBlockListModel.remove(cp, e164_2);
                if (string.IsNullOrEmpty(savedFile)) { cp.core.privateFiles.deleteFile(path); } else { cp.core.privateFiles.saveFile(path, savedFile); }
                cp.core.siteProperties.setProperty(Constants.sitePropertySmsBlockListMigrated, savedFlag);
                cp.core.siteProperties.dataBuildVersion = savedDataBuildVersion;
            }
        }
    }
}
