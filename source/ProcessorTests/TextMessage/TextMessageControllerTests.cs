
using Contensive.Models.Db;
using Contensive.Processor;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using static Tests.TestConstants;

namespace Tests {
    [TestClass]
    public class TextMessageControllerTests {
        //
        [TestMethod]
        public void blockedList_test1() {
            using (CPClass cp = new(testAppName)) {
                // arrange
                cp.core.mockTextMessages = true;
                DbBaseModel.deleteRows<SystemTextMessageModel>(cp, "(1=1)");
                DbBaseModel.deleteRows<GroupTextMessageModel>(cp, "(1=1)");
                DbBaseModel.deleteRows<TaskModel>(cp, "(1=1)");
                DbBaseModel.deleteRows<TextMessageQueueModel>(cp, "(1=1)");
                string test1 = GenericController.getRandomInteger(cp.core).ToString();
                string test2 = GenericController.getRandomInteger(cp.core).ToString();
                Assert.AreEqual(0, cp.core.mockTextMessageList.Count);
                //
                // act
                TextMessageController.addToBlockList(cp.core, test1);
                // assert
                Assert.IsTrue(TextMessageController.isOnBlockedList(cp.core, test1));
                Assert.IsFalse(TextMessageController.isOnBlockedList(cp.core, test2));
            }
        }
        //
        [TestMethod]
        public void verifyPhone_test1() {
            using (CPClass cp = new(testAppName)) {
                // arrange
                cp.core.mockTextMessages = true;
                DbBaseModel.deleteRows<SystemTextMessageModel>(cp, "(1=1)");
                DbBaseModel.deleteRows<GroupTextMessageModel>(cp, "(1=1)");
                DbBaseModel.deleteRows<TaskModel>(cp, "(1=1)");
                DbBaseModel.deleteRows<TextMessageQueueModel>(cp, "(1=1)");
                string test1 = GenericController.getRandomInteger(cp.core).ToString();
                string test2 = GenericController.getRandomInteger(cp.core).ToString();
                Assert.AreEqual(0, cp.core.mockTextMessageList.Count);
                //
                // act - assert
                Assert.IsTrue(TextMessageController.verifyPhone(cp.core, test1));
                Assert.IsFalse(TextMessageController.verifyPhone(cp.core, ""));
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
                string testPhone = GenericController.getRandomInteger(cp.core).ToString();
                string testMessage = GenericController.getRandomInteger(cp.core).ToString();
                Assert.AreEqual(0, cp.core.mockTextMessageList.Count);
                //
                // act - assert
                string userError = "";
                bool response = SmsController.sendMessage(cp.core, new TextMessageSendRequest {
                    attempts = 1,
                    textBody = testMessage,
                    textMessageId = 0,
                    toMemberId = 0,
                    toPhone = testPhone
                }, ref userError);
                //
                // assert
                Assert.AreEqual("", userError );
                Assert.IsTrue(response);
                Assert.IsTrue(cp.core.mockTextMessageList.Count == 1);
                Assert.AreEqual(cp.core.mockTextMessageList.First().textMessageRequest.textBody, testMessage);
                Assert.AreEqual(cp.core.mockTextMessageList.First().textMessageRequest.toPhone, testPhone);
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
                string testPhone = GenericController.getRandomInteger(cp.core).ToString();
                string testMessage = GenericController.getRandomInteger(cp.core).ToString();
                Assert.AreEqual(0, cp.core.mockTextMessageList.Count);
                //
                // act - assert
                PersonModel recipient = DbBaseModel.addDefault<PersonModel>(cp);
                recipient.cellPhone = testPhone;
                recipient.blockTextMessage = false;
                recipient.save(cp);
                string userErrorMessage = "";
                bool response = TextMessageController.queuePersonTextMessage(cp.core, recipient, testMessage, false, 0, ref userErrorMessage, "unit test send");
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
                string testPhone = GenericController.getRandomInteger(cp.core).ToString();
                string testMessage = GenericController.getRandomInteger(cp.core).ToString();
                Assert.AreEqual(0, cp.core.mockTextMessageList.Count);
                //
                // act - assert
                PersonModel recipient = DbBaseModel.addDefault<PersonModel>(cp);
                recipient.cellPhone = testPhone;
                recipient.blockTextMessage = false;
                recipient.save(cp);
                string userErrorMessage = "";
                bool response = TextMessageController.queuePersonTextMessage(cp.core, recipient, testMessage, true, 0, ref userErrorMessage, "unit test send");
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
                string testPhone = GenericController.getRandomInteger(cp.core).ToString();
                string testMessage = GenericController.getRandomInteger(cp.core).ToString();
                Assert.AreEqual(0, cp.core.mockTextMessageList.Count);
                //
                // act - assert
                PersonModel recipient = DbBaseModel.addDefault<PersonModel>(cp);
                recipient.cellPhone = testPhone;
                recipient.blockTextMessage = true;
                recipient.save(cp);
                string userErrorMessage = "";
                bool response = TextMessageController.queuePersonTextMessage(cp.core, recipient, testMessage, false, 0, ref userErrorMessage, "unit test send");
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
                string testName = GenericController.getRandomInteger(cp.core).ToString();
                string testPhone = GenericController.getRandomInteger(cp.core).ToString();
                string testMessage = GenericController.getRandomInteger(cp.core).ToString();
                string testAppendCopy = GenericController.getRandomInteger(cp.core).ToString();
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
                testGroup.name = GenericController.getRandomInteger(cp.core).ToString();
                testGroup.caption = GenericController.getRandomInteger(cp.core).ToString();
                testGroup.save(cp);
                // -- create groupMember1
                var user1 = DbBaseModel.addDefault<PersonModel>(cp);
                user1.name = GenericController.getRandomInteger(cp.core).ToString();
                user1.cellPhone = GenericController.getRandomInteger(cp.core).ToString();
                user1.blockTextMessage = false;
                user1.save(cp);
                // -- create groupMember2
                var user2 = DbBaseModel.addDefault<PersonModel>(cp);
                user2.name = GenericController.getRandomInteger(cp.core).ToString();
                user2.cellPhone = GenericController.getRandomInteger(cp.core).ToString();
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
                text.body = GenericController.getRandomInteger(cp.core).ToString();
                text.name = GenericController.getRandomInteger(cp.core).ToString();
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
                testGroup.name = GenericController.getRandomInteger(cp.core).ToString();
                testGroup.caption = GenericController.getRandomInteger(cp.core).ToString();
                testGroup.save(cp);
                // -- create groupMember1
                var user1 = DbBaseModel.addDefault<PersonModel>(cp);
                user1.name = GenericController.getRandomInteger(cp.core).ToString();
                user1.cellPhone = GenericController.getRandomInteger(cp.core).ToString();
                user1.blockTextMessage = false;
                user1.save(cp);
                // -- create groupMember2
                var user2 = DbBaseModel.addDefault<PersonModel>(cp);
                user2.name = GenericController.getRandomInteger(cp.core).ToString();
                user2.cellPhone = GenericController.getRandomInteger(cp.core).ToString();
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
                text.body = GenericController.getRandomInteger(cp.core).ToString();
                text.name = GenericController.getRandomInteger(cp.core).ToString();
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
                cp.Addon.Execute(TestConstants.addonGuidTextMessageSendTask);
                //
                // assert
                Assert.IsTrue(cp.core.mockTextMessageList.Count == 2);
                // there are 2 texts, we don't know which is first
                bool user1Pass = false;
                bool user2Pass = false;
                foreach (var mock in cp.core.mockTextMessageList) {
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
    }
}
