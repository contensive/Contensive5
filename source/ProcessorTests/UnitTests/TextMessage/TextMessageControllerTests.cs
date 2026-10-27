
using Contensive.BaseModels;
using Contensive.Models.Db;
using Contensive.Processor;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using Contensive.Processor.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using static Contensive.Processor.Tests.TestConstants;

namespace Contensive.Processor.Tests.UnitTests.TextMessage;

[TestClass]
public class TextMessageControllerTests {
    //
    /// <summary>
    /// a valid 10 digit US phone number, random so tests do not collide
    /// </summary>
    private static string getTestPhone() => $"703{GenericController.getRandomInteger(8999999) + 1000000}";
    //
    [TestMethod]
    public void blockedList_test1() {
        using (CPClass cp = new(testAppName)) {
            // arrange
            cp.core.mockTextMessages = true;
            string test1 = getTestPhone();
            string test2 = getTestPhone();
            string test1E164 = SmsPhoneController.toE164(test1, "1");
            string test2E164 = SmsPhoneController.toE164(test2, "1");
            try {
                // act
                TextMessageBlockListModel.add(cp, test1E164, (int)TextMessageBlockListModel.BlockSourceEnum.Admin, 0, "unit test");
                // assert
                Assert.IsTrue(TextMessageBlockListModel.isBlocked(cp, test1E164));
                Assert.IsFalse(TextMessageBlockListModel.isBlocked(cp, test2E164));
                Assert.IsTrue(cp.SMS.IsBlocked(test1));
                Assert.IsFalse(cp.SMS.IsBlocked(test2));
            } finally {
                TextMessageBlockListModel.remove(cp, test1E164);
            }
        }
    }
    //
    [TestMethod]
    public void send_test() {
        using (CPClass cp = new(testAppName)) {
            // arrange
            cp.core.mockTextMessages = true;
            DbBaseModel.deleteRows<SystemTextMessageModel>(cp, "(1=1)");
            DbBaseModel.deleteRows<GroupTextMessageModel>(cp, "(1=1)");
            DbBaseModel.deleteRows<TaskModel>(cp, "(1=1)");
            DbBaseModel.deleteRows<TextMessageQueueModel>(cp, "(1=1)");
            string testPhone = getTestPhone();
            string testMessage = GenericController.getRandomInteger().ToString();
            Assert.AreEqual(0, cp.core.mockTextMessageList.Count);
            //
            // act - assert
            SmsProviderResult response = SmsController.send(cp.core, SmsPhoneController.toE164(testPhone, "1"), testMessage, null);
            //
            // assert
            Assert.IsTrue(response.success);
            Assert.AreEqual("", response.errorMessage);
            Assert.IsTrue(cp.core.mockTextMessageList.Count == 1);
            Assert.AreEqual(cp.core.mockTextMessageList.First().textMessageRequest.textBody, testMessage);
            Assert.AreEqual(cp.core.mockTextMessageList.First().textMessageRequest.toPhone, SmsPhoneController.toE164(testPhone, "1"));
        }
    }
    //
    [TestMethod]
    public void sendPerson_test() {
        using (CPClass cp = new(testAppName)) {
            // arrange
            cp.core.mockTextMessages = true;
            DbBaseModel.deleteRows<SystemTextMessageModel>(cp, "(1=1)");
            DbBaseModel.deleteRows<GroupTextMessageModel>(cp, "(1=1)");
            DbBaseModel.deleteRows<TaskModel>(cp, "(1=1)");
            DbBaseModel.deleteRows<TextMessageQueueModel>(cp, "(1=1)");
            string testPhone = getTestPhone();
            string testMessage = GenericController.getRandomInteger().ToString();
            Assert.AreEqual(0, cp.core.mockTextMessageList.Count);
            //
            // act - assert
            PersonModel recipient = DbBaseModel.addDefault<PersonModel>(cp);
            recipient.cellPhone = testPhone;
            recipient.blockTextMessage = false;
            recipient.save(cp);
            string userErrorMessage = "";
            bool response = TextMessageController.queuePersonTextMessage(cp.core, recipient, testMessage, false, 0, 0, ref userErrorMessage, "unit test send");
            DbBaseModel.delete<PersonModel>(cp, recipient.id);
            //
            // assert
            Assert.IsTrue(string.IsNullOrEmpty(userErrorMessage));
            Assert.IsTrue(response);
            Assert.IsTrue(cp.core.mockTextMessageList.Count == 1);
            Assert.AreEqual(cp.core.mockTextMessageList.First().textMessageRequest.textBody, testMessage);
            Assert.AreEqual(cp.core.mockTextMessageList.First().textMessageRequest.toPhone, testPhone);
        }
    }
    //
    [TestMethod]
    public void sendPersonImmediate_test() {
        using (CPClass cp = new(testAppName)) {
            // arrange
            cp.core.mockTextMessages = true;
            DbBaseModel.deleteRows<SystemTextMessageModel>(cp, "(1=1)");
            DbBaseModel.deleteRows<GroupTextMessageModel>(cp, "(1=1)");
            DbBaseModel.deleteRows<TaskModel>(cp, "(1=1)");
            DbBaseModel.deleteRows<TextMessageQueueModel>(cp, "(1=1)");
            string testPhone = getTestPhone();
            string testMessage = GenericController.getRandomInteger().ToString();
            Assert.AreEqual(0, cp.core.mockTextMessageList.Count);
            //
            // act - assert
            PersonModel recipient = DbBaseModel.addDefault<PersonModel>(cp);
            recipient.cellPhone = testPhone;
            recipient.blockTextMessage = false;
            recipient.save(cp);
            string userErrorMessage = "";
            bool response = TextMessageController.queuePersonTextMessage(cp.core, recipient, testMessage, true, 0, 0, ref userErrorMessage, "unit test send");
            DbBaseModel.delete<PersonModel>(cp, recipient.id);
            //
            // assert
            Assert.IsTrue(string.IsNullOrEmpty(userErrorMessage));
            Assert.IsTrue(response);
            Assert.IsTrue(cp.core.mockTextMessageList.Count == 1);
            Assert.AreEqual(cp.core.mockTextMessageList.First().textMessageRequest.textBody, testMessage);
            Assert.AreEqual(cp.core.mockTextMessageList.First().textMessageRequest.toPhone, testPhone);
        }
    }
    //
    [TestMethod]
    public void sendPersonBlocked_test() {
        using (CPClass cp = new(testAppName)) {
            // arrange
            cp.core.mockTextMessages = true;
            DbBaseModel.deleteRows<SystemTextMessageModel>(cp, "(1=1)");
            DbBaseModel.deleteRows<GroupTextMessageModel>(cp, "(1=1)");
            DbBaseModel.deleteRows<TaskModel>(cp, "(1=1)");
            DbBaseModel.deleteRows<TextMessageQueueModel>(cp, "(1=1)");
            string testPhone = getTestPhone();
            string testMessage = GenericController.getRandomInteger().ToString();
            Assert.AreEqual(0, cp.core.mockTextMessageList.Count);
            //
            // act - assert
            PersonModel recipient = DbBaseModel.addDefault<PersonModel>(cp);
            recipient.cellPhone = testPhone;
            recipient.blockTextMessage = true;
            recipient.save(cp);
            string userErrorMessage = "";
            bool response = TextMessageController.queuePersonTextMessage(cp.core, recipient, testMessage, false, 0, 0, ref userErrorMessage, "unit test send");
            DbBaseModel.delete<PersonModel>(cp, recipient.id);
            //
            // assert
            Assert.IsTrue(!string.IsNullOrEmpty(userErrorMessage));
            Assert.IsFalse(response);
            Assert.IsTrue(cp.core.mockTextMessageList.Count == 0);
        }
    }
    //
    [TestMethod]
    public void sendSystemAddlUser_test() {
        using (CPClass cp = new(testAppName)) {
            // arrange
            cp.core.mockTextMessages = true;
            cp.core.mockEmail = true;
            DbBaseModel.deleteRows<SystemTextMessageModel>(cp, "(1=1)");
            DbBaseModel.deleteRows<GroupTextMessageModel>(cp, "(1=1)");
            DbBaseModel.deleteRows<TaskModel>(cp, "(1=1)");
            DbBaseModel.deleteRows<TextMessageQueueModel>(cp, "(1=1)");
            string testName = GenericController.getRandomInteger().ToString();
            string testPhone = getTestPhone();
            string testMessage = GenericController.getRandomInteger().ToString();
            string testAppendCopy = GenericController.getRandomInteger().ToString();
            Assert.AreEqual(0, cp.core.mockTextMessageList.Count);
            SystemTextMessageModel text = DbBaseModel.addDefault<SystemTextMessageModel>(cp);
            text.body = testMessage;
            text.name = testName;
            text.save(cp);
            //
            PersonModel addlRecipient = DbBaseModel.addDefault<PersonModel>(cp);
            addlRecipient.cellPhone = testPhone;
            addlRecipient.blockTextMessage = false;
            addlRecipient.save(cp);
            string userErrorMessage = "";
            bool response = TextMessageController.queueSystemTextMessage(cp.core, text, testAppendCopy, addlRecipient.id, ref userErrorMessage);
            DbBaseModel.delete<PersonModel>(cp, addlRecipient.id);
            //
            // act
            cp.Addon.Execute(TestConstants.addonGuidTextMessageSendTask);
            //
            // assert
            Assert.IsTrue(string.IsNullOrEmpty(userErrorMessage));
            Assert.IsTrue(response);
            Assert.IsTrue(cp.core.mockTextMessageList.Count == 1);
            Assert.AreEqual(testPhone, cp.core.mockTextMessageList[0].textMessageRequest.toPhone);
            Assert.AreEqual(testMessage + testAppendCopy, cp.core.mockTextMessageList[0].textMessageRequest.textBody);
            Assert.AreEqual(addlRecipient.id, cp.core.mockTextMessageList[0].textMessageRequest.toMemberId);
        }
    }
    //
    [TestMethod]
    public void sendSystem_test() {
        using (CPClass cp = new(testAppName)) {
            // arrange
            cp.core.mockTextMessages = true;
            cp.core.mockEmail = true;
            DbBaseModel.deleteRows<SystemTextMessageModel>(cp, "(1=1)");
            DbBaseModel.deleteRows<GroupTextMessageModel>(cp, "(1=1)");
            DbBaseModel.deleteRows<TaskModel>(cp, "(1=1)");
            DbBaseModel.deleteRows<TextMessageQueueModel>(cp, "(1=1)");
            Assert.AreEqual(0, cp.core.mockTextMessageList.Count);
            // -- create group
            var testGroup = DbBaseModel.addDefault<GroupModel>(cp);
            testGroup.name = GenericController.getRandomInteger().ToString();
            testGroup.caption = GenericController.getRandomInteger().ToString();
            testGroup.save(cp);
            // -- create groupMember1
            var user1 = DbBaseModel.addDefault<PersonModel>(cp);
            user1.name = GenericController.getRandomInteger().ToString();
            user1.cellPhone = getTestPhone();
            user1.blockTextMessage = false;
            user1.save(cp);
            // -- create groupMember2
            var user2 = DbBaseModel.addDefault<PersonModel>(cp);
            user2.name = GenericController.getRandomInteger().ToString();
            user2.cellPhone = getTestPhone();
            user2.blockTextMessage = false;
            user2.save(cp);
            // -- add groupMembers to group
            var rule1 = DbBaseModel.addDefault<MemberRuleModel>(cp);
            rule1.groupId = testGroup.id;
            rule1.memberId = user1.id;
            rule1.save(cp);
            var rule2 = DbBaseModel.addDefault<MemberRuleModel>(cp);
            rule2.groupId = testGroup.id;
            rule2.memberId = user2.id;
            rule2.save(cp);


            // -- create system text
            SystemTextMessageModel text = DbBaseModel.addDefault<SystemTextMessageModel>(cp);
            text.body = GenericController.getRandomInteger().ToString();
            text.name = GenericController.getRandomInteger().ToString();
            text.save(cp);
            // -- add group to text message
            var rule3 = DbBaseModel.addDefault<SystemTextMessageGroupRuleModel>(cp);
            rule3.groupId = testGroup.id;
            rule3.systemTextMessageId = text.id;
            rule3.save(cp);
            //
            // act
            string userErrorMessage = "";
            bool response = TextMessageController.queueSystemTextMessage(cp.core, text, "", 0, ref userErrorMessage);
            //
            // assert
            Assert.IsTrue(cp.core.mockTextMessageList.Count == 2);
            // there are 2 texts, we don't know which is first
            bool user1Pass = false;
            bool user2Pass = false;
            foreach ( var mock in cp.core.mockTextMessageList) {
                if (mock.textMessageRequest.toMemberId == user1.id) {
                    Assert.AreEqual(text.body, mock.textMessageRequest.textBody);
                    Assert.AreEqual(user1.cellPhone, mock.textMessageRequest.toPhone);
                    user1Pass = true;
                    continue;
                }
                if (mock.textMessageRequest.toMemberId == user2.id) {
                    Assert.AreEqual(text.body, mock.textMessageRequest.textBody);
                    Assert.AreEqual(user2.cellPhone, mock.textMessageRequest.toPhone);
                    user2Pass = true;
                    continue;
                }
                Assert.Fail("mock text message list contains a text that was not user1 or user2");
            }
            Assert.IsTrue(user1Pass);
            Assert.IsTrue(user2Pass);
        }
    }
    //
    [TestMethod]
    public void sendGroup_test() {
        using (CPClass cp = new(testAppName)) {
            // arrange
            cp.core.mockTextMessages = true;
            cp.core.mockEmail = true;
            DbBaseModel.deleteRows<SystemTextMessageModel>(cp, "(1=1)");
            DbBaseModel.deleteRows<GroupTextMessageModel>(cp, "(1=1)");
            DbBaseModel.deleteRows<TaskModel>(cp, "(1=1)");
            DbBaseModel.deleteRows<TextMessageQueueModel>(cp, "(1=1)");
            Assert.AreEqual(0, cp.core.mockTextMessageList.Count);
            // -- create group
            var testGroup = DbBaseModel.addDefault<GroupModel>(cp);
            testGroup.name = GenericController.getRandomInteger().ToString();
            testGroup.caption = GenericController.getRandomInteger().ToString();
            testGroup.save(cp);
            // -- create groupMember1
            var user1 = DbBaseModel.addDefault<PersonModel>(cp);
            user1.name = GenericController.getRandomInteger().ToString();
            user1.cellPhone = getTestPhone();
            user1.blockTextMessage = false;
            user1.save(cp);
            // -- create groupMember2
            var user2 = DbBaseModel.addDefault<PersonModel>(cp);
            user2.name = GenericController.getRandomInteger().ToString();
            user2.cellPhone = getTestPhone();
            user2.blockTextMessage = false;
            user2.save(cp);
            // -- add groupMembers to group
            var rule1 = DbBaseModel.addDefault<MemberRuleModel>(cp);
            rule1.groupId = testGroup.id;
            rule1.memberId = user1.id;
            rule1.save(cp);
            var rule2 = DbBaseModel.addDefault<MemberRuleModel>(cp);
            rule2.groupId = testGroup.id;
            rule2.memberId = user2.id;
            rule2.save(cp);
            // -- create group text
            GroupTextMessageModel text = DbBaseModel.addDefault<GroupTextMessageModel>(cp);
            text.body = GenericController.getRandomInteger().ToString();
            text.name = GenericController.getRandomInteger().ToString();
            text.submitted = true;
            text.sent = false;
            text.save(cp);
            // -- add group to text message
            var rule3 = DbBaseModel.addDefault<GroupTextMessageGroupRuleModel>(cp);
            rule3.groupId = testGroup.id;
            rule3.groupTextMessageId = text.id;
            rule3.save(cp);
            //
            // act
            cp.Addon.Execute(TestConstants.addonGuidTextMessageProcessTask);
            //
            // assert
            Assert.IsTrue(cp.core.mockTextMessageList.Count == 2);
            // there are 2 texts, we don't know which is first
            bool user1Pass = false;
            bool user2Pass = false;
            foreach (var mock in cp.core.mockTextMessageList) {
                if (mock.textMessageRequest.toMemberId == user1.id) {
                    Assert.AreEqual(text.body, mock.textMessageRequest.textBody);
                    Assert.AreEqual(SmsPhoneController.toE164(user1.cellPhone, "1"), mock.textMessageRequest.toPhone);
                    user1Pass = true;
                    continue;
                }
                if (mock.textMessageRequest.toMemberId == user2.id) {
                    Assert.AreEqual(text.body, mock.textMessageRequest.textBody);
                    Assert.AreEqual(SmsPhoneController.toE164(user2.cellPhone, "1"), mock.textMessageRequest.toPhone);
                    user2Pass = true;
                    continue;
                }
                Assert.Fail("mock text message list contains a text that was not user1 or user2");
            }
            Assert.IsTrue(user1Pass);
            Assert.IsTrue(user2Pass);
        }
    }
}
