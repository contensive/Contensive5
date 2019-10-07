﻿
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Contensive.Processor.Controllers;
using static Tests.testConstants;
using System.Linq;
using Contensive.Processor;
using Contensive.Processor.Models.Domain;
using Contensive.Models.Db;

namespace Contensive.ProcessorTests.UnitTests.ControllerTests {
    [TestClass]
    public class emailControllerTests {
        //
        [TestMethod]
        public void Controllers_Email_GetBlockedList_test1() {
            using (CPClass cp = new CPClass(testAppName)) {
                cp.core.mockEmail = true;
                // arrange
                string test1 = GenericController.GetRandomInteger(cp.core).ToString() + "@kma.net";
                string test2 = GenericController.GetRandomInteger(cp.core).ToString() + "@kma.net";
                // act
                EmailController.addToBlockList(cp.core, test1);
                string blockList = Processor.Controllers.EmailController.getBlockList(cp.core);
                // assert
                Assert.IsTrue(EmailController.isOnBlockedList(cp.core, test1));
                Assert.IsFalse(EmailController.isOnBlockedList(cp.core, test2));
            }
        }
        //
        [TestMethod]
        public void Controllers_Email_VerifyEmailAddress_test1() {
            using (CPClass cp = new CPClass(testAppName)) {
                cp.core.mockEmail = true;
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
        public void Controllers_Email_queueAdHocEmail_test1() {
            using (CPClass cp = new CPClass(testAppName)) {
                cp.core.mockEmail = true;
                // arrange
                string body = GenericController.GetRandomInteger(cp.core).ToString() ;
                string sendStatus = "";
                string ResultLogFilename = "";
                // act
                EmailController.queueAdHocEmail(cp.core,"Unit Test",0, "to@kma.net", "from@kma.net", "subject", body,"bounce@kma.net","replyTo@kma.net", ResultLogFilename, true, true, 0, ref sendStatus);
                Contensive.BaseClasses.AddonBaseClass addon = new Contensive.Addons.Email.ProcessEmailClass();
                addon.Execute(cp);
                // assert
                Assert.AreEqual(1, cp.core.mockEmailList.Count);
                MockEmailClass sentEmail = cp.core.mockEmailList.First();
                Assert.IsTrue(string.IsNullOrEmpty(sentEmail.AttachmentFilename));
                Assert.AreEqual("to@kma.net", sentEmail.email.toAddress);
                Assert.AreEqual("from@kma.net", sentEmail.email.fromAddress);
                Assert.AreEqual("bounce@kma.net", sentEmail.email.BounceAddress);
                Assert.AreEqual("replyTo@kma.net", sentEmail.email.replyToAddress);
                Assert.AreEqual("subject", sentEmail.email.subject);
                Assert.AreEqual(body, sentEmail.email.textBody);
            }
        }
        //
        [TestMethod]
        public void Controllers_Email_queuePersonEmail_test1() {
            using (CPClass cp = new CPClass(testAppName)) {
                cp.core.mockEmail = true;
                // arrange
                string body = GenericController.GetRandomInteger(cp.core).ToString();
                var toPerson = DbBaseModel.addDefault<PersonModel>(cp, ContentMetadataModel.getDefaultValueDict(cp.core, PersonModel.tableMetadata.contentName));
                Assert.IsNotNull(toPerson);
                toPerson.email = GenericController.GetRandomInteger(cp.core).ToString() + "@kma.net";
                toPerson.firstName = GenericController.GetRandomInteger(cp.core).ToString();
                toPerson.lastName = GenericController.GetRandomInteger(cp.core).ToString();
                toPerson.save(cp);
                string sendStatus = "";
                // act
                Assert.IsTrue( EmailController.queuePersonEmail(cp.core, "Function Test", toPerson, "from@kma.net", "subject", body, "bounce@kma.net", "replyTo@kma.net", true, true, 0, "", true, ref sendStatus));
                Contensive.BaseClasses.AddonBaseClass addon = new Contensive.Addons.Email.ProcessEmailClass();
                addon.Execute(cp);
                // assert
                Assert.AreEqual(1, cp.core.mockEmailList.Count);
                MockEmailClass sentEmail = cp.core.mockEmailList.First();
                Assert.IsTrue(string.IsNullOrEmpty(sentEmail.AttachmentFilename));
                Assert.AreEqual( toPerson.email, sentEmail.email.toAddress);
                Assert.AreEqual("from@kma.net", sentEmail.email.fromAddress);
                Assert.AreEqual("bounce@kma.net", sentEmail.email.BounceAddress);
                Assert.AreEqual("replyTo@kma.net", sentEmail.email.replyToAddress);
                Assert.AreEqual("subject", sentEmail.email.subject);
                Assert.AreEqual(body, sentEmail.email.textBody);
            }
        }
    }
}
