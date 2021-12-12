
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
    public class EmailControllerTests {
        //
        [TestMethod]
        public void processConditional_DaysBeforeExpire_Test() {
            using (CPClass cp = new(testAppName)) {
                //
                // arrange, now = 1/1/2020 at 12:00 am
                cp.core.mockEmail = true;
                DbBaseModel.deleteRows<SystemEmailModel>(cp, "");
                DbBaseModel.deleteRows<ConditionalEmailModel>(cp, "");
                DbBaseModel.deleteRows<GroupEmailModel>(cp, "");
                DbBaseModel.deleteRows<TaskModel>(cp, "");
                DbBaseModel.deleteRows<EmailQueueModel>(cp, "");
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
                DbBaseModel.deleteRows<SystemEmailModel>(cp, "");
                DbBaseModel.deleteRows<ConditionalEmailModel>(cp, "");
                DbBaseModel.deleteRows<GroupEmailModel>(cp, "");
                DbBaseModel.deleteRows<TaskModel>(cp, "");
                DbBaseModel.deleteRows<EmailQueueModel>(cp, "");
            }
        }
        //
        [TestMethod]
        public void controllers_Email_GetBlockedList_test1() {
            using (CPClass cp = new(testAppName)) {
                cp.core.mockEmail = true;
                DbBaseModel.deleteRows<SystemEmailModel>(cp, "");
                DbBaseModel.deleteRows<ConditionalEmailModel>(cp, "");
                DbBaseModel.deleteRows<GroupEmailModel>(cp, "");
                DbBaseModel.deleteRows<TaskModel>(cp, "");
                DbBaseModel.deleteRows<EmailQueueModel>(cp, "");
                Assert.AreEqual(0, cp.core.mockEmailList.Count);
                // arrange
                string test1 = GenericController.getRandomInteger(cp.core).ToString() + "@kma.net";
                string test2 = GenericController.getRandomInteger(cp.core).ToString() + "@kma.net";
                // act
                EmailController.addToBlockList(cp.core, test1);
                string blockList = Contensive.Processor.Controllers.EmailController.getBlockList(cp.core);
                // assert
                Assert.IsTrue(EmailController.isOnBlockedList(cp.core, test1));
                Assert.IsFalse(EmailController.isOnBlockedList(cp.core, test2));
            }
        }
        //
        [TestMethod]
        public void controllers_Email_VerifyEmailAddress_test1() {
            using (CPClass cp = new(testAppName)) {
                cp.core.mockEmail = true;
                DbBaseModel.deleteRows<SystemEmailModel>(cp, "");
                DbBaseModel.deleteRows<ConditionalEmailModel>(cp, "");
                DbBaseModel.deleteRows<GroupEmailModel>(cp, "");
                DbBaseModel.deleteRows<TaskModel>(cp, "");
                DbBaseModel.deleteRows<EmailQueueModel>(cp, "");
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
                DbBaseModel.deleteRows<SystemEmailModel>(cp, "");
                DbBaseModel.deleteRows<ConditionalEmailModel>(cp, "");
                DbBaseModel.deleteRows<GroupEmailModel>(cp, "");
                DbBaseModel.deleteRows<TaskModel>(cp, "");
                DbBaseModel.deleteRows<EmailQueueModel>(cp, "");
                Assert.AreEqual(0, cp.core.mockEmailList.Count);
                // arrange
                string body = GenericController.getRandomInteger(cp.core).ToString();
                string sendStatus = "";
                string ResultLogFilename = "";
                // act
                EmailController.queueAdHocEmail(cp.core, "Unit Test", 0, "to@kma.net", "from@kma.net", "subject", body, "bounce@kma.net", "replyTo@kma.net", ResultLogFilename, true, true, 0, ref sendStatus);
                Contensive.BaseClasses.AddonBaseClass addon = new Contensive.Processor.Addons.Email.ProcessEmailClass();
                addon.Execute(cp);
                // assert
                Assert.AreEqual(1, cp.core.mockEmailList.Count);
                MockEmailClass sentEmail = cp.core.mockEmailList.First();
                Assert.IsTrue(string.IsNullOrEmpty(sentEmail.AttachmentFilename));
                Assert.AreEqual("to@kma.net", getEmailPart(sentEmail.email.toAddress));
                Assert.AreEqual("from@kma.net", getEmailPart(sentEmail.email.fromAddress));
                Assert.AreEqual("bounce@kma.net", getEmailPart(sentEmail.email.bounceAddress));
                Assert.AreEqual("replyTo@kma.net", getEmailPart(sentEmail.email.replyToAddress));
                Assert.AreEqual("subject", sentEmail.email.subject);
                Assert.AreEqual(body, sentEmail.email.textBody);
            }
        }
        //
        [TestMethod]
        public void controllers_Email_queuePersonEmail_test1() {
            using (CPClass cp = new(testAppName)) {
                cp.core.mockEmail = true;
                DbBaseModel.deleteRows<SystemEmailModel>(cp, "");
                DbBaseModel.deleteRows<ConditionalEmailModel>(cp, "");
                DbBaseModel.deleteRows<GroupEmailModel>(cp, "");
                DbBaseModel.deleteRows<TaskModel>(cp, "");
                DbBaseModel.deleteRows<EmailQueueModel>(cp, "");
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
                Assert.IsTrue(EmailController.tryQueuePersonEmail(cp.core, "Function Test", toPerson, "from@kma.net", "subject", body, "bounce@kma.net", "replyTo@kma.net", true, true, 0, "", true, ref sendStatus));
                Contensive.BaseClasses.AddonBaseClass addon = new Contensive.Processor.Addons.Email.ProcessEmailClass();
                addon.Execute(cp);
                // assert
                Assert.AreEqual(1, cp.core.mockEmailList.Count);
                MockEmailClass sentEmail = cp.core.mockEmailList.First();
                Assert.IsTrue(string.IsNullOrEmpty(sentEmail.AttachmentFilename));
                Assert.AreEqual(toPerson.email, getEmailPart(sentEmail.email.toAddress));
                Assert.AreEqual("from@kma.net", getEmailPart(sentEmail.email.fromAddress));
                Assert.AreEqual("bounce@kma.net", getEmailPart(sentEmail.email.bounceAddress));
                Assert.AreEqual("replyTo@kma.net", getEmailPart(sentEmail.email.replyToAddress));
                Assert.AreEqual("subject", sentEmail.email.subject);
                Assert.AreEqual(body, sentEmail.email.textBody);
            }
        }
        //
        private string getEmailPart(string FriendlyEmailAddress) {
            if (string.IsNullOrEmpty(FriendlyEmailAddress)) return FriendlyEmailAddress;
            if (!FriendlyEmailAddress.Contains('<') || !FriendlyEmailAddress.Contains('>')) return FriendlyEmailAddress;
            int posStart = FriendlyEmailAddress.IndexOf('<');
            int posEnd = FriendlyEmailAddress.IndexOf('>');
            if (posStart > posEnd) return FriendlyEmailAddress;
            return FriendlyEmailAddress.Substring(posStart + 1, posEnd - posStart-1);

        }
        //
        [TestMethod]
        public void controllers_Email_queueSystemEmail_test1() {
            using (CPClass cp = new(testAppName)) {
                cp.core.mockEmail = true;
                DbBaseModel.deleteRows<SystemEmailModel>(cp, "");
                DbBaseModel.deleteRows<ConditionalEmailModel>(cp, "");
                DbBaseModel.deleteRows<GroupEmailModel>(cp, "");
                DbBaseModel.deleteRows<TaskModel>(cp, "");
                DbBaseModel.deleteRows<EmailQueueModel>(cp, "");
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
                Assert.IsTrue(EmailController.tryQueueSystemEmail(cp.core, systemEmail.name, appendedCopy, additionalMemberId, ref userErrorMessage));
                Contensive.BaseClasses.AddonBaseClass addon = new Contensive.Processor.Addons.Email.ProcessEmailClass();
                addon.Execute(cp);
                //
                // assert 2 emails, first the confirmation, then to-address
                Assert.AreEqual(2, cp.core.mockEmailList.Count);
                {
                    //
                    // -- the confirmationl
                    MockEmailClass sentEmail = cp.core.mockEmailList[0];
                    Assert.AreEqual(confirmPerson.email, getEmailPart(sentEmail.email.toAddress));
                    Assert.AreEqual(systemEmail.fromAddress, getEmailPart(sentEmail.email.fromAddress));
                    Assert.AreNotEqual(-1, sentEmail.email.htmlBody.IndexOf(htmlBody));
                    Assert.IsTrue(string.IsNullOrEmpty(sentEmail.AttachmentFilename));
                    Assert.AreEqual("", getEmailPart(sentEmail.email.bounceAddress));
                    Assert.AreEqual("", getEmailPart(sentEmail.email.replyToAddress));
                }
                {
                    //
                    // -- the to-email
                    MockEmailClass sentEmail = cp.core.mockEmailList[1];
                    Assert.IsTrue(string.IsNullOrEmpty(sentEmail.AttachmentFilename));
                    Assert.AreEqual(toPerson.email, getEmailPart(sentEmail.email.toAddress));
                    Assert.AreEqual(systemEmail.fromAddress, getEmailPart(sentEmail.email.fromAddress));
                    Assert.AreEqual(systemEmail.subject, sentEmail.email.subject);
                    Assert.AreNotEqual(-1, sentEmail.email.htmlBody.IndexOf(htmlBody));
                    Assert.AreEqual("", getEmailPart(sentEmail.email.bounceAddress));
                    Assert.AreEqual("", getEmailPart(sentEmail.email.replyToAddress));
                }
            }
        }
    }
}
