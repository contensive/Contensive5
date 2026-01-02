
using Contensive.Models.Db;
using Contensive.Processor;
using Contensive.Processor.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using static Tests.TestConstants;

namespace Tests {
    [TestClass()]
    public class CpUserTests {
        //
        //====================================================================================================
        /// <summary>
        ///   
        /// </summary>
        [TestMethod]
        public void cpUser_setPassword_minLength() {
            using (var cp = new CPClass(testAppName)) {
                // arrange
                cp.Site.SetProperty("allow plain text password", true);
                cp.core.siteProperties.passwordMinLength = 5;
                // act
                string returnMsg = "1234";
                Assert.IsFalse(cp.User.SetPassword("a", ref returnMsg));
                Assert.IsFalse(string.IsNullOrEmpty(returnMsg));
                Assert.IsFalse(cp.User.SetPassword("aB", ref returnMsg));
                Assert.IsFalse(string.IsNullOrEmpty(returnMsg));
                Assert.IsFalse(cp.User.SetPassword("aB!", ref returnMsg));
                Assert.IsFalse(string.IsNullOrEmpty(returnMsg));
                Assert.IsFalse(cp.User.SetPassword("aB!1", ref returnMsg));
                Assert.IsFalse(string.IsNullOrEmpty(returnMsg));
                Assert.IsTrue(cp.User.SetPassword("aB!12", ref returnMsg));
                Assert.IsTrue(string.IsNullOrEmpty(returnMsg));
                Assert.IsTrue(cp.User.SetPassword("aB!123", ref returnMsg));
                Assert.IsTrue(string.IsNullOrEmpty(returnMsg));
                Assert.IsTrue(cp.User.SetPassword("aB!1234", ref returnMsg));
                Assert.IsTrue(string.IsNullOrEmpty(returnMsg));
                Assert.IsTrue(cp.User.SetPassword("aB!12345", ref returnMsg));
                Assert.IsTrue(string.IsNullOrEmpty(returnMsg));
                Assert.IsTrue(cp.User.SetPassword("aB!123456", ref returnMsg));
                Assert.IsTrue(string.IsNullOrEmpty(returnMsg));
            }
        }
        //
        //====================================================================================================
        /// <summary>
        ///   
        /// </summary>
        [TestMethod]
        public void cpUser_setPassword_plainText() {
            using (var cp = new CPClass(testAppName)) {
                // arrange
                cp.Site.SetProperty("allow plain text password", true);
                cp.Site.SetProperty("allow auto create password hash", false);
                string testPassword = TestController.getRandomPassword(cp);
                string testUsername = cp.Utils.GetRandomInteger().ToString();
                PersonModel userBefore = DbBaseModel.create<PersonModel>(cp, cp.User.Id);
                userBefore.username = testUsername;
                userBefore.password = TestController.getRandomPassword(cp);
                userBefore.passwordHash = cp.Utils.GetRandomInteger().ToString();
                userBefore.save(cp);
                // act
                Assert.IsTrue(cp.User.SetPassword(testPassword));
                Assert.IsTrue(cp.User.Login(testUsername, testPassword));
                // assert
                PersonModel userAfter = DbBaseModel.create<PersonModel>(cp, cp.User.Id);
                Assert.AreEqual(testPassword, userAfter.password);
                Assert.AreEqual(string.Empty, userAfter.passwordHash);
                // cleanup
                cp.Db.ExecuteNonQuery($"update ccmembers set username=null,password=null,passwordhash=null where id={cp.User.Id}");
            }
        }
        //
        //====================================================================================================
        /// <summary>
        ///   
        /// </summary>
        [TestMethod]
        public void cpUser_setPassword_hash() {
            using (var cp = new CPClass(testAppName)) {
                // arrange
                cp.Site.SetProperty("allow plain text password", false);
                cp.Site.SetProperty("allow auto create password hash", false);
                cp.core.siteProperties.clearAdminPasswordOnHash = true;
                //
                string testPassword = TestController.getRandomPassword(cp);
                string testUsername = cp.Utils.GetRandomInteger().ToString();
                PersonModel userBefore = DbBaseModel.create<PersonModel>(cp, cp.User.Id);
                userBefore.username = testUsername;
                userBefore.password = TestController.getRandomPassword(cp);
                userBefore.passwordHash = cp.Utils.GetRandomInteger().ToString();
                userBefore.save(cp);
                // act
                Assert.IsTrue(cp.User.SetPassword(testPassword));
                Assert.IsTrue(cp.User.Login(testUsername, testPassword));
                // assert
                PersonModel userAfter = DbBaseModel.create<PersonModel>(cp, cp.User.Id);
                Assert.AreEqual(String.Empty, userAfter.password);
                Assert.AreNotEqual(string.Empty, userAfter.passwordHash);
                Assert.AreNotEqual(testPassword, userAfter.passwordHash);
                // cleanup
                cp.Db.ExecuteNonQuery($"update ccmembers set username=null,password=null,passwordhash=null where id={cp.User.Id}");
            }
        }
        //
        //====================================================================================================
        /// <summary>
        ///   
        /// </summary>
        [TestMethod]
        public void cpUser_setPassword_hash_autoConvert() {
            using (var cp = new CPClass(testAppName)) {
                // arrange
                cp.Site.SetProperty("allow plain text password", false);
                cp.Site.SetProperty("allow auto create password hash", true);
                string testPassword = TestController.getRandomPassword(cp);
                string testUsername = cp.Utils.GetRandomInteger().ToString();
                PersonModel userBefore = DbBaseModel.create<PersonModel>(cp, cp.User.Id);
                userBefore.username = testUsername;
                userBefore.password = testPassword;
                userBefore.passwordHash = "";
                userBefore.save(cp);
                // act
                Assert.IsTrue(cp.User.SetPassword(testPassword));
                Assert.IsTrue(cp.User.Login(testUsername, testPassword));
                // assert
                PersonModel userAfter = DbBaseModel.create<PersonModel>(cp, cp.User.Id);
                Assert.AreEqual(String.Empty, userAfter.password);
                Assert.AreNotEqual(string.Empty, userAfter.passwordHash);
                Assert.AreNotEqual(testPassword, userAfter.passwordHash);
                // cleanup
                cp.Db.ExecuteNonQuery($"update ccmembers set username=null,password=null,passwordhash=null where id={cp.User.Id}");
            }
        }
        //====================================================================================================
        /// <summary>
        ///  Test 1 - cp ok without application (cluster mode).
        /// </summary>
        [TestMethod]
        public void cpUser_GetDate() {
            using (var cp = new CPClass(testAppName)) {
                // arrange
                string testInput = DateTime.MinValue.ToString();
                cp.User.SetProperty("testDate", DateTime.MinValue.ToString());
                // act
                DateTime testoutput = cp.User.GetDate("testDate");
                // assert
                Assert.AreEqual(DateTime.MinValue, testoutput);
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void cpUser_IsInGroup_GroupName() {
            using (CPClass cp = new(testAppName)) {
                // arrange
                CoreController core = ((CPClass)cp).core;
                core.cache.invalidateAll();
                core.session.verifyUser();
                string groupGuid = cp.Utils.CreateGuid();
                string groupName = "Group-" + cp.Utils.GetRandomInteger().ToString();
                //
                cp.Group.verifyGroup(groupGuid, groupName);
                bool inGroupBeforeAddTest = cp.User.IsInGroup(groupName);
                // act
                cp.Group.AddUser(groupName);
                bool inGroupAfterAddWithoutAuth = cp.User.IsInGroup(groupName);
                // authenticate
                cp.User.LoginByID(cp.User.Id);
                // is in group after authenticating
                bool inGroupAfterAddWithAuth = cp.User.IsInGroup(groupName);
                //
                cp.Group.Delete(groupName);
                bool inGroupAfterDelete = cp.User.IsInGroup(groupName);
                // assert
                Assert.AreEqual(false, inGroupBeforeAddTest, "before add");
                Assert.AreEqual(false, inGroupAfterAddWithoutAuth, "before auth");
                Assert.AreEqual(true, inGroupAfterAddWithAuth, "after auth");
                Assert.AreEqual(false, inGroupAfterDelete, "after delete");
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void cpUser_IsInGroup_2() {
            using (CPClass cp = new(testAppName)) {
                // arrange
                CoreController core = ((CPClass)cp).core;
                core.cache.invalidateAll();
                core.session.verifyUser();
                string groupGuid = cp.Utils.CreateGuid();
                string groupName = "Group-" + cp.Utils.GetRandomInteger().ToString();
                //
                cp.Group.verifyGroup(groupGuid, groupName);
                bool inGroupBeforeAddTest = cp.User.IsInGroup(groupName);
                // act
                cp.Group.AddUser(groupName);
                bool inGroupAfterTestBeforeAuth = cp.User.IsInGroup(groupName);
                //
                cp.User.LoginByID(cp.User.Id);
                bool inGroupAfterTestAfterAuth = cp.User.IsInGroup(groupName);

                // assert
                Assert.AreEqual(false, inGroupBeforeAddTest);
                Assert.AreEqual(false, inGroupAfterTestBeforeAuth);
                Assert.AreEqual(true, inGroupAfterTestAfterAuth);

            }
        }
    }
}
