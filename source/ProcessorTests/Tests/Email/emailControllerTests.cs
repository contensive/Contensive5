
using Contensive.Models.Db;
using Contensive.Processor;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using static Tests.TestConstants;
using Tests;

namespace Tests {
    [TestClass]
    public class EmailControllerTests {
        //
        [TestMethod]
        public void groupEmail_Send_Test() {
            using (CPClass cp = new(testAppName)) {
                cp.core.mockEmail = true;
                string emailAddress1 = GenericController.getRandomInteger(cp.core).ToString() + "@kma.net";
                string emailAddress2 = GenericController.getRandomInteger(cp.core).ToString() + "@kma.net";
                TestController.testGroupEmail(cp, emailAddress1, emailAddress2);
                // assert
                Assert.AreEqual(2, cp.core.mockEmailList.Count);
            }
        }
        //
        [TestMethod]
        public void groupEmail_Duplicates_Test() {
            using (CPClass cp = new(testAppName)) {
                cp.core.mockEmail = true;
                string emailAddress1 = GenericController.getRandomInteger(cp.core).ToString() + "@kma.net";
                string emailAddress2 = emailAddress1;
                TestController.testGroupEmail(cp, emailAddress1, emailAddress2);
                // assert
                Assert.AreEqual(1, cp.core.mockEmailList.Count);
            }
        }
        //
        [TestMethod]
        public void groupEmail_Duplicate_Friendly_Test() {
            using (CPClass cp = new(testAppName)) {
                cp.core.mockEmail = true;
                string emailAddress1 = GenericController.getRandomInteger(cp.core).ToString() + "@kma.net";
                string emailAddress2 = "\"test\" <" + emailAddress1 + ">";
                TestController.testGroupEmail(cp, emailAddress1, emailAddress2);
                // assert
                Assert.AreEqual(1, cp.core.mockEmailList.Count);
            }
        }
        //
        [TestMethod]
        public void systemEmail_Send_Immediate_Test() {
            using (CPClass cp = new(testAppName)) {
                cp.core.mockEmail = true;
                string emailAddress1 = GenericController.getRandomInteger(cp.core).ToString() + "@kma.net";
                string emailAddress2 = GenericController.getRandomInteger(cp.core).ToString() + "@kma.net";
                TestController.testSystemEmail(cp, emailAddress1, emailAddress2, true);
                // assert
                Assert.AreEqual(2, cp.core.mockEmailList.Count);
            }
        }
        //
        [TestMethod]
        public void systemEmail_Send_NotImmediate_Test() {
            using (CPClass cp = new(testAppName)) {
                cp.core.mockEmail = true;
                string emailAddress1 = GenericController.getRandomInteger(cp.core).ToString() + "@kma.net";
                string emailAddress2 = GenericController.getRandomInteger(cp.core).ToString() + "@kma.net";
                TestController.testSystemEmail(cp, emailAddress1, emailAddress2, false);
                // assert
                Assert.AreEqual(2, cp.core.mockEmailList.Count);
            }
        }
        //
        [TestMethod]
        public void SystemEmail_Duplicates() {
            using (CPClass cp = new(testAppName)) {
                cp.core.mockEmail = true;
                string emailAddress1 = GenericController.getRandomInteger(cp.core).ToString() + "@kma.net";
                string emailAddress2 = emailAddress1;
                TestController.testSystemEmail(cp, emailAddress1, emailAddress2, true);
                // assert
                Assert.AreEqual(1, cp.core.mockEmailList.Count);
            }
        }
        //
        [TestMethod]
        public void SystemEmail_Duplicate_Friendly() {
            using (CPClass cp = new(testAppName)) {
                cp.core.mockEmail = true;
                string emailAddress1 = GenericController.getRandomInteger(cp.core).ToString() + "@kma.net";
                string emailAddress2 = "\"test\" <" + emailAddress1 + ">";
                TestController.testSystemEmail(cp, emailAddress1, emailAddress2, true);
                // assert
                Assert.AreEqual(1, cp.core.mockEmailList.Count);
            }
        }
        //
        [TestMethod]
        public void controllers_Email_BlockList_Add() {
            using (CPClass cp = new(testAppName)) {
                cp.core.mockEmail = true;
                DbBaseModel.deleteRows<EmailBounceListModel>(cp, "(1=1)");
                DbBaseModel.deleteRows<ActivityLogModel>(cp, "(1=1)");
                Assert.AreEqual(0, cp.core.mockEmailList.Count);
                // arrange
                string test1 = GenericController.getRandomInteger(cp.core).ToString() + "@kma.net";
                string test2 = GenericController.getRandomInteger(cp.core).ToString() + "@kma.net";
                // act
                EmailController.addToBlockList(cp.core, test1);
                // assert
                Assert.IsTrue(EmailController.isOnBlockedList(cp.core, test1));
                Assert.IsFalse(EmailController.isOnBlockedList(cp.core, test2));
            }
        }
        //
        [TestMethod]
        public void controllers_Email_BlockList_Remove_test1() {
            using (CPClass cp = new(testAppName)) {
                cp.core.mockEmail = true;
                DbBaseModel.deleteRows<EmailBounceListModel>(cp, "(1=1)");
                DbBaseModel.deleteRows<ActivityLogModel>(cp, "(1=1)");
                Assert.AreEqual(0, cp.core.mockEmailList.Count);
                // arrange
                string test1 = GenericController.getRandomInteger(cp.core).ToString() + "@kma.net";
                string test2 = GenericController.getRandomInteger(cp.core).ToString() + "@kma.net";
                // act
                EmailController.addToBlockList(cp.core, test1);
                EmailController.addToBlockList(cp.core, test2);
                // assert
                Assert.IsTrue(EmailController.isOnBlockedList(cp.core, test1));
                Assert.IsTrue(EmailController.isOnBlockedList(cp.core, test2));
                //
                EmailController.removeFromBlockList(cp.core, test1);
                // assert
                Assert.IsFalse(EmailController.isOnBlockedList(cp.core, test1));
                Assert.IsTrue(EmailController.isOnBlockedList(cp.core, test2));
            }
        }
        //
        [TestMethod]
        public void controllers_Email_BlockEmail() {
            using (CPClass cp = new(testAppName)) {
                cp.core.mockEmail = true;
                DbBaseModel.deleteRows<EmailBounceListModel>(cp, "(1=1)");
                DbBaseModel.deleteRows<ActivityLogModel>(cp, "(1=1)");
                Assert.AreEqual(0, cp.core.mockEmailList.Count);
                //
                // arrange
                string email1 = GenericController.getRandomInteger(cp.core).ToString() + "@kma.net";
                PersonModel person1 = DbBaseModel.addDefault<PersonModel>(cp);
                person1.email = email1;
                person1.allowBulkEmail = true;
                person1.save(cp);
                Assert.IsFalse(EmailController.isOnBlockedList(cp.core, email1));
                //
                // act
                EmailController.blockEmailAddress(cp.core, email1);
                //
                person1 = DbBaseModel.create<PersonModel>(cp, person1.id);
                Assert.IsTrue(EmailController.isOnBlockedList(cp.core, email1));
                Assert.IsFalse(person1.allowBulkEmail);
            }
        }
        //
        [TestMethod]
        public void controllers_Email_UnblockEmail() {
            using (CPClass cp = new(testAppName)) {
                cp.core.mockEmail = true;
                DbBaseModel.deleteRows<EmailBounceListModel>(cp, "(1=1)");
                DbBaseModel.deleteRows<ActivityLogModel>(cp, "(1=1)");
                Assert.AreEqual(0, cp.core.mockEmailList.Count);
                //
                // arrange
                string email1 = GenericController.getRandomInteger(cp.core).ToString() + "@kma.net";
                PersonModel person1 = DbBaseModel.addDefault<PersonModel>(cp);
                person1.email = email1;
                person1.save(cp);
                EmailController.blockEmailAddress(cp.core, email1);
                Assert.IsTrue(EmailController.isOnBlockedList(cp.core, email1));
                //
                // act
                EmailController.unblockEmailAddress(cp.core, email1);
                //
                person1 = DbBaseModel.create<PersonModel>(cp, person1.id);
                Assert.IsFalse(EmailController.isOnBlockedList(cp.core, email1));
                Assert.IsTrue(person1.allowBulkEmail);
            }
        }
        //
        [TestMethod]
        public void controllers_Email_BlockList() {
            using (CPClass cp = new(testAppName)) {
                cp.core.mockEmail = true;
                DbBaseModel.deleteRows<EmailBounceListModel>(cp, "(1=1)");
                DbBaseModel.deleteRows<ActivityLogModel>(cp, "(1=1)");
                Assert.AreEqual(0, cp.core.mockEmailList.Count);
                // arrange
                string test1 = GenericController.getRandomInteger(cp.core).ToString() + "@kma.net";
                string test2 = GenericController.getRandomInteger(cp.core).ToString() + "@kma.net";
                // act
                EmailController.addToBlockList(cp.core, test1);
                // assert
                Assert.IsTrue(EmailController.isOnBlockedList(cp.core, test1));
                Assert.IsFalse(EmailController.isOnBlockedList(cp.core, test2));
            }
        }
        //
        [TestMethod]
        public void controllers_Email_FriendlyToSimple() {
            using (CPClass cp = new(testAppName)) {
                cp.core.mockEmail = true;
                Assert.AreEqual("friendlyName@contensive.com", EmailController.getSimpleEmailFromFriendlyEmail(cp, "\"friendly name\" <friendlyName@contensive.com>"));
                Assert.AreEqual("friendlyName@contensive.com", EmailController.getSimpleEmailFromFriendlyEmail(cp, "\"<frie>ndly <nam>e\" <friendlyName@contensive.com>"));
                Assert.AreEqual("jay@contensive.com", EmailController.getSimpleEmailFromFriendlyEmail(cp, "jay@contensive.com"));
            }
        }
        //
        [TestMethod]
        public void controllers_Email_createListFromGroupList() {
            using (CPClass cp = new(testAppName)) {
                cp.core.mockEmail = true;
                DbBaseModel.deleteRows<SystemEmailModel>(cp, "(1=1)");
                DbBaseModel.deleteRows<ConditionalEmailModel>(cp, "(1=1)");
                DbBaseModel.deleteRows<GroupEmailModel>(cp, "(1=1)");
                DbBaseModel.deleteRows<TaskModel>(cp, "(1=1)");
                DbBaseModel.deleteRows<EmailQueueModel>(cp, "(1=1)");
                Assert.AreEqual(0, cp.core.mockEmailList.Count);
                // arrange
                string test1 = GenericController.getRandomInteger(cp.core).ToString() + "@kma.net";
                string test2 = GenericController.getRandomInteger(cp.core).ToString() + "@kma.net";
                //
                // -- group
                GroupModel group = DbBaseModel.addDefault<GroupModel>(cp);
                group.name = test1.ToString();
                group.caption = test1.ToString();
                group.ccguid = "{1234-1243-1234-1234}";
                group.save(cp);
                //
                // -- person
                PersonModel person = DbBaseModel.addDefault<PersonModel>(cp);
                person.name = test2.ToString();
                person.email = test2.ToString() + "@test.com";
                person.save(cp);
                //
                // -- join 1/1/2020 at 12:00 am, expire from group in 10 days later, 1/10/2020 at 12:00 am
                cp.Group.AddUser(group.id, person.id, ((DateTime)cp.core.dateTimeNowMockable).AddDays(10));
                // act/assert
                Assert.AreEqual(1, PersonModel.createListFromGroupIdList(cp, new List<int> { group.id }, false).Count);
                Assert.AreEqual(1, PersonModel.createListFromGroupIdList(cp, new List<int> { group.id, group.id }, false).Count);
                Assert.AreEqual(0, PersonModel.createListFromGroupIdList(cp, new List<int> { }, false).Count);
                Assert.AreEqual(0, PersonModel.createListFromGroupIdList(cp, new List<int> { 0 }, false).Count);
                Assert.AreEqual(0, PersonModel.createListFromGroupIdList(cp, new List<int> { 0, 0 }, false).Count);
                Assert.AreEqual(1, PersonModel.createListFromGroupIdList(cp, new List<int> { 0, group.id, 0 }, false).Count);
                //
                Assert.AreEqual(1, PersonModel.createListFromGroupNameList(cp, new List<string> { group.name }, false).Count);
                Assert.AreEqual(1, PersonModel.createListFromGroupNameList(cp, new List<string> { group.name, group.name }, false).Count);
                Assert.AreEqual(0, PersonModel.createListFromGroupNameList(cp, new List<string> { }, false).Count);
                Assert.AreEqual(0, PersonModel.createListFromGroupNameList(cp, new List<string> { "" }, false).Count);
                Assert.AreEqual(0, PersonModel.createListFromGroupNameList(cp, new List<string> { "", "" }, false).Count);
                Assert.AreEqual(1, PersonModel.createListFromGroupNameList(cp, new List<string> { "", group.name, "" }, false).Count);
            }
        }

        //
        [TestMethod]
        public void processConditional_DaysBeforeExpire_Test() {
            using (CPClass cp = new(testAppName)) {
                //
                // arrange, now = 1/1/2020 at 12:00 am
                cp.core.mockEmail = true;
                DbBaseModel.deleteRows<SystemEmailModel>(cp, "(1=1)");
                DbBaseModel.deleteRows<ConditionalEmailModel>(cp, "(1=1)");
                DbBaseModel.deleteRows<GroupEmailModel>(cp, "(1=1)");
                DbBaseModel.deleteRows<TaskModel>(cp, "(1=1)");
                DbBaseModel.deleteRows<EmailQueueModel>(cp, "(1=1)");
                Assert.AreEqual(0, cp.core.mockEmailList.Count);
                cp.core.mockDateTimeNow(new DateTime(2020, 1, 1, 0, 0, 0));
                //
                // -- group
                GroupModel group = DbBaseModel.addDefault<GroupModel>(cp);
                group.name = "DaysAfterJoiningGroup";
                group.caption = "DaysAfterJoiningGroup";
                group.ccguid = "{1234-1243-1234-1234}";
                group.save(cp);
                //
                // -- person
                PersonModel person = DbBaseModel.addDefault<PersonModel>(cp);
                person.name = "user";
                person.email = "test@test.com";
                person.save(cp);
                //
                // -- join 1/1/2020 at 12:00 am, expire from group in 10 days later, 1/10/2020 at 12:00 am
                cp.Group.AddUser(group.id, person.id, ((DateTime)cp.core.dateTimeNowMockable).AddDays(10));
                //
                // -- setup conditional email, send 5 days before group expiration, so send after 1/5/2020 12:00am and before 1/6/2020 12:00am
                ConditionalEmailModel email = DbBaseModel.addDefault<ConditionalEmailModel>(cp);
                email.name = "ConditionalEmailTest";
                email.subject = "ConditionalEmailTest-subject";
                email.testMemberId = 0;
                email.conditionExpireDate = null;
                email.conditionPeriod = 5;
                email.fromAddress = "ConditionalEmailTest@kma.net";
                email.conditionId = 1;
                email.submitted = true;
                email.save(cp);
                //
                // -- setup email-group, associating this email to the 
                EmailGroupModel rule = DbBaseModel.addDefault<EmailGroupModel>(cp);
                rule.emailId = email.id;
                rule.groupId = group.id;
                rule.save(cp);
                //
                // act/asset
                EmailController.processConditionalEmail(cp.core);
                Assert.AreEqual(0, cp.core.mockEmailList.Count);
                //
                // 1/1/2020 at noon
                cp.core.mockDateTimeNow(cp.Utils.GetDateTimeMockable().AddDays(0.5));
                Assert.AreEqual(0, EmailController.processConditionalEmail(cp.core));
                //
                // 1/2/2020 at noon
                cp.core.mockDateTimeNow(cp.Utils.GetDateTimeMockable().AddDays(1));
                Assert.AreEqual(0, EmailController.processConditionalEmail(cp.core));
                //
                // 1/3/2020 at noon
                cp.core.mockDateTimeNow(cp.Utils.GetDateTimeMockable().AddDays(1));
                Assert.AreEqual(0, EmailController.processConditionalEmail(cp.core));
                //
                // 1/4/2020 at noon
                cp.core.mockDateTimeNow(cp.Utils.GetDateTimeMockable().AddDays(1));
                Assert.AreEqual(0, EmailController.processConditionalEmail(cp.core));
                //
                // 1/5/2020 at noon
                cp.core.mockDateTimeNow(cp.Utils.GetDateTimeMockable().AddDays(1));
                Assert.AreEqual(0, EmailController.processConditionalEmail(cp.core));
                //
                // 1/6/2020 at noon, must send one. If others are in the system, it 
                cp.core.mockDateTimeNow(cp.Utils.GetDateTimeMockable().AddDays(1));
                Assert.AreEqual(1, EmailController.processConditionalEmail(cp.core));
                //
                // 1/7/2020 at noon
                cp.core.mockDateTimeNow(cp.Utils.GetDateTimeMockable().AddDays(1));
                Assert.AreEqual(0, EmailController.processConditionalEmail(cp.core));
                //
                // -- cleanup
                DbBaseModel.deleteRows<SystemEmailModel>(cp, "(1=1)");
                DbBaseModel.deleteRows<ConditionalEmailModel>(cp, "(1=1)");
                DbBaseModel.deleteRows<GroupEmailModel>(cp, "(1=1)");
                DbBaseModel.deleteRows<TaskModel>(cp, "(1=1)");
                DbBaseModel.deleteRows<EmailQueueModel>(cp, "(1=1)");
            }
        }
        //
        [TestMethod]
        public void controllers_Email_VerifyEmailAddress_test1() {
            using (CPClass cp = new(testAppName)) {
                cp.core.mockEmail = true;
                DbBaseModel.deleteRows<SystemEmailModel>(cp, "(1=1)");
                DbBaseModel.deleteRows<ConditionalEmailModel>(cp, "(1=1)");
                DbBaseModel.deleteRows<GroupEmailModel>(cp, "(1=1)");
                DbBaseModel.deleteRows<TaskModel>(cp, "(1=1)");
                DbBaseModel.deleteRows<EmailQueueModel>(cp, "(1=1)");
                Assert.AreEqual(0, cp.core.mockEmailList.Count);
                // arrange
                // act
                // assert
                Assert.IsFalse(EmailController.verifyEmailAddress(cp.core, "123"));
                Assert.IsFalse(EmailController.verifyEmailAddress(cp.core, "123@"));
                Assert.IsFalse(EmailController.verifyEmailAddress(cp.core, "123@2"));
                Assert.IsFalse(EmailController.verifyEmailAddress(cp.core, "123@2."));
                Assert.IsTrue(EmailController.verifyEmailAddress(cp.core, "123@2.com"));
            }
        }
        //
        [TestMethod]
        public void controllers_Email_queueAdHocEmail_test1() {
            using (CPClass cp = new(testAppName)) {
                cp.core.mockEmail = true;
                DbBaseModel.deleteRows<SystemEmailModel>(cp, "(1=1)");
                DbBaseModel.deleteRows<ConditionalEmailModel>(cp, "(1=1)");
                DbBaseModel.deleteRows<GroupEmailModel>(cp, "(1=1)");
                DbBaseModel.deleteRows<TaskModel>(cp, "(1=1)");
                DbBaseModel.deleteRows<EmailQueueModel>(cp, "(1=1)");
                Assert.AreEqual(0, cp.core.mockEmailList.Count);
                // arrange
                string body = GenericController.getRandomInteger(cp.core).ToString();
                string sendStatus = "";
                string ResultLogFilename = "";
                // act
                EmailController.sendAdHocEmail(cp.core, "Unit Test", 0, "to@kma.net", "from@kma.net", "subject", body, "bounce@kma.net", "replyTo@kma.net", ResultLogFilename, true, true, 0, ref sendStatus,0);
                Contensive.BaseClasses.AddonBaseClass addon = new Contensive.Processor.Addons.Email.EmailSendTask();
                addon.Execute(cp);
                // assert
                Assert.AreEqual(1, cp.core.mockEmailList.Count);
                MockEmailClass sentEmail = cp.core.mockEmailList.First();
                Assert.IsTrue(string.IsNullOrEmpty(sentEmail.AttachmentFilename));
                Assert.AreEqual("to@kma.net", EmailController.getSimpleEmailFromFriendlyEmail(cp,sentEmail.email.toAddress));
                Assert.AreEqual("from@kma.net", EmailController.getSimpleEmailFromFriendlyEmail(cp,sentEmail.email.fromAddress));
                Assert.AreEqual("bounce@kma.net", EmailController.getSimpleEmailFromFriendlyEmail(cp,sentEmail.email.bounceAddress));
                Assert.AreEqual("replyTo@kma.net", EmailController.getSimpleEmailFromFriendlyEmail(cp,sentEmail.email.replyToAddress));
                Assert.AreEqual("subject", sentEmail.email.subject);
                Assert.IsTrue(sentEmail.email.textBody.Contains(body));
            }
        }
        //
        [TestMethod]
        public void controllers_Email_queuePersonEmail_test1() {
            using (CPClass cp = new(testAppName)) {
                cp.core.mockEmail = true;
                DbBaseModel.deleteRows<SystemEmailModel>(cp, "(1=1)");
                DbBaseModel.deleteRows<ConditionalEmailModel>(cp, "(1=1)");
                DbBaseModel.deleteRows<GroupEmailModel>(cp, "(1=1)");
                DbBaseModel.deleteRows<TaskModel>(cp, "(1=1)");
                DbBaseModel.deleteRows<EmailQueueModel>(cp, "(1=1)");
                Assert.AreEqual(0, cp.core.mockEmailList.Count);
                // arrange
                string body = GenericController.getRandomInteger(cp.core).ToString();
                var toPerson = DbBaseModel.addDefault<PersonModel>(cp, ContentMetadataModel.getDefaultValueDict(cp.core, PersonModel.tableMetadata.contentName));
                Assert.IsNotNull(toPerson);
                toPerson.email = GenericController.getRandomInteger(cp.core).ToString() + "@kma.net";
                toPerson.firstName = GenericController.getRandomInteger(cp.core).ToString();
                toPerson.lastName = GenericController.getRandomInteger(cp.core).ToString();
                toPerson.save(cp);
                string sendStatus = "";
                // act
                Assert.IsTrue(EmailController.trySendPersonEmail(cp.core, "Function Test", toPerson, "from@kma.net", "subject", body, "bounce@kma.net", "replyTo@kma.net", true, true, 0, "", true, ref sendStatus,0));
                Contensive.BaseClasses.AddonBaseClass addon = new Contensive.Processor.Addons.Email.EmailSendTask();
                addon.Execute(cp);
                // assert
                Assert.AreEqual(1, cp.core.mockEmailList.Count);
                MockEmailClass sentEmail = cp.core.mockEmailList.First();
                Assert.IsTrue(string.IsNullOrEmpty(sentEmail.AttachmentFilename));
                Assert.AreEqual(toPerson.email, EmailController.getSimpleEmailFromFriendlyEmail(cp,sentEmail.email.toAddress));
                Assert.AreEqual("from@kma.net", EmailController.getSimpleEmailFromFriendlyEmail(cp,sentEmail.email.fromAddress));
                Assert.AreEqual("bounce@kma.net", EmailController.getSimpleEmailFromFriendlyEmail(cp,sentEmail.email.bounceAddress));
                Assert.AreEqual("replyTo@kma.net", EmailController.getSimpleEmailFromFriendlyEmail(cp,sentEmail.email.replyToAddress));
                Assert.AreEqual("subject", sentEmail.email.subject);
                Assert.IsTrue(sentEmail.email.textBody.Contains(body));
            }
        }
        //
        [TestMethod]
        public void controllers_Email_queueSystemEmail_test1() {
            using (CPClass cp = new(testAppName)) {
                cp.core.mockEmail = true;
                DbBaseModel.deleteRows<SystemEmailModel>(cp, "(1=1)");
                DbBaseModel.deleteRows<ConditionalEmailModel>(cp, "(1=1)");
                DbBaseModel.deleteRows<GroupEmailModel>(cp, "(1=1)");
                DbBaseModel.deleteRows<TaskModel>(cp, "(1=1)");
                DbBaseModel.deleteRows<EmailQueueModel>(cp, "(1=1)");
                Assert.AreEqual(0, cp.core.mockEmailList.Count);
                // arrange
                string htmlBody = "a<b>1</b><br>2<p>3</p><div>4</div>";
                //
                var confirmPerson = DbBaseModel.addDefault<PersonModel>(cp);
                Assert.IsNotNull(confirmPerson);
                confirmPerson.email = GenericController.getRandomInteger(cp.core).ToString() + "@kma.net";
                confirmPerson.firstName = GenericController.getRandomInteger(cp.core).ToString();
                confirmPerson.lastName = GenericController.getRandomInteger(cp.core).ToString();
                confirmPerson.save(cp);
                //
                SystemEmailModel systemEmail = DbBaseModel.addDefault<SystemEmailModel>(cp);
                systemEmail.name = "system email test " + cp.Utils.GetRandomInteger();
                systemEmail.addLinkEId = false;
                systemEmail.allowSpamFooter = false;
                systemEmail.emailTemplateId = 0;
                systemEmail.fromAddress = GenericController.getRandomInteger(cp.core).ToString() + "@kma.net";
                systemEmail.subject = htmlBody;
                systemEmail.copyFilename.content = systemEmail.subject;
                systemEmail.testMemberId = confirmPerson.id;
                systemEmail.save(cp);
                //
                var toPerson = DbBaseModel.addDefault<PersonModel>(cp);
                Assert.IsNotNull(toPerson);
                toPerson.email = GenericController.getRandomInteger(cp.core).ToString() + "@kma.net";
                toPerson.firstName = GenericController.getRandomInteger(cp.core).ToString();
                toPerson.lastName = GenericController.getRandomInteger(cp.core).ToString();
                toPerson.save(cp);
                //
                var group = DbBaseModel.addDefault<GroupModel>(cp);
                group.name = "test group " + cp.Utils.GetRandomInteger();
                group.save(cp);
                //
                cp.Group.AddUser(group.id, toPerson.id);
                //
                var emailGroup = DbBaseModel.addDefault<Contensive.Models.Db.EmailGroupModel>(cp);
                emailGroup.groupId = group.id;
                emailGroup.emailId = systemEmail.id;
                emailGroup.save(cp);
                // act
                string userErrorMessage = "";
                int additionalMemberId = 0;
                string appendedCopy = "";
                // -- add systeme email to queue
                Assert.IsTrue(EmailController.trySendSystemEmail(cp.core, false, systemEmail.name, appendedCopy, additionalMemberId, ref userErrorMessage));
                // -- send system queue
                Contensive.BaseClasses.AddonBaseClass addon = new Contensive.Processor.Addons.Email.EmailSendTask();
                addon.Execute(cp);
                //
                // assert 2 emails, first the confirmation, then to-address
                Assert.AreEqual(2, cp.core.mockEmailList.Count);
                int foundCnt = 0;
                foreach (var sentEmail in cp.core.mockEmailList) {
                    {
                        //
                        // -- the confirmationl
                        if (confirmPerson.email == EmailController.getSimpleEmailFromFriendlyEmail(cp,sentEmail.email.toAddress)) {
                            foundCnt++;
                            Assert.AreEqual(confirmPerson.email, EmailController.getSimpleEmailFromFriendlyEmail(cp,sentEmail.email.toAddress));
                            Assert.AreEqual(systemEmail.fromAddress, EmailController.getSimpleEmailFromFriendlyEmail(cp,sentEmail.email.fromAddress));
                            Assert.AreNotEqual(-1, sentEmail.email.htmlBody.IndexOf(htmlBody));
                            Assert.IsTrue(string.IsNullOrEmpty(sentEmail.AttachmentFilename));
                            Assert.AreEqual("", EmailController.getSimpleEmailFromFriendlyEmail(cp,sentEmail.email.bounceAddress));
                            Assert.AreEqual("", EmailController.getSimpleEmailFromFriendlyEmail(cp,sentEmail.email.replyToAddress));
                        }
                    }
                    {
                        //
                        // -- the to-email
                        if (toPerson.email == EmailController.getSimpleEmailFromFriendlyEmail(cp,sentEmail.email.toAddress)) {
                            foundCnt++;
                            Assert.IsTrue(string.IsNullOrEmpty(sentEmail.AttachmentFilename));
                            Assert.AreEqual(toPerson.email, EmailController.getSimpleEmailFromFriendlyEmail(cp,sentEmail.email.toAddress));
                            Assert.AreEqual(systemEmail.fromAddress, EmailController.getSimpleEmailFromFriendlyEmail(cp,sentEmail.email.fromAddress));
                            Assert.AreEqual(systemEmail.subject, sentEmail.email.subject);
                            Assert.AreNotEqual(-1, sentEmail.email.htmlBody.IndexOf(htmlBody));
                            Assert.AreEqual("", EmailController.getSimpleEmailFromFriendlyEmail(cp,sentEmail.email.bounceAddress));
                            Assert.AreEqual("", EmailController.getSimpleEmailFromFriendlyEmail(cp,sentEmail.email.replyToAddress));
                        }
                    }
                }
                Assert.AreEqual(2, foundCnt, "Both the email and the confirmation were not found.");
            }
        }
    }
}
