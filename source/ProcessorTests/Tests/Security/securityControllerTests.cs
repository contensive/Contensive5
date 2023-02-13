
using Contensive.Processor;
using Contensive.Processor.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using static Tests.TestConstants;

namespace Tests {
    //
    //====================================================================================================
    //
    [TestClass()]
    public class securityControllerTests {

        //
        //====================================================================================================
        //
        [TestMethod]
        public void Controllers_Security_blank() {
            using (CPClass cp = new(testAppName)) {
                // arrange
                // act
                // assert
                Assert.AreEqual("", "");
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void Controllers_Security_twoWayEncode_des() {
            using (CPClass cp = new(testAppName)) {
                // arrange
                string source = "All work and no play makes johnny a dull boy.";
                // act
                string resultEncrypted = SecurityController.encryptTwoWay(cp.core, source, SecurityController.TwoWayCiphers.des);
                string resultEncryptedAes = SecurityController.encryptTwoWay(cp.core, source, SecurityController.TwoWayCiphers.aes);
                string resultDecrypted = SecurityController.decryptTwoWay(cp.core, resultEncrypted, SecurityController.TwoWayCiphers.des);
                string blankEncrypted = SecurityController.encryptTwoWay(cp.core, "", SecurityController.TwoWayCiphers.des);
                string blankDecrypted = SecurityController.decryptTwoWay(cp.core, "", SecurityController.TwoWayCiphers.des);
                string invalidDecrypted = SecurityController.decryptTwoWay(cp.core, source, SecurityController.TwoWayCiphers.des);
                // assert
                Assert.AreEqual(source, resultDecrypted);
                Assert.AreNotEqual(resultEncrypted, resultEncryptedAes);
                Assert.AreNotEqual("", blankEncrypted);
                Assert.AreNotEqual("", blankEncrypted);
                Assert.AreEqual("", blankDecrypted);
                Assert.AreEqual("", invalidDecrypted);
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void Controllers_Security_twoWayEncode_aes() {
            using (CPClass cp = new(testAppName)) {
                // arrange
                string source = "All work and no play makes johnny a dull boy again.";
                // act
                string resultEncrypted = SecurityController.encryptTwoWay(cp.core, source, SecurityController.TwoWayCiphers.aes);
                string resultEncryptedDes = SecurityController.encryptTwoWay(cp.core, source, SecurityController.TwoWayCiphers.des);
                string resultDecrypted = SecurityController.decryptTwoWay(cp.core, resultEncrypted, SecurityController.TwoWayCiphers.aes);
                string blankEncrypted = SecurityController.encryptTwoWay(cp.core, "", SecurityController.TwoWayCiphers.aes);
                string blankDecrypted = SecurityController.decryptTwoWay(cp.core, "", SecurityController.TwoWayCiphers.aes);
                string invalidDecrypted = SecurityController.decryptTwoWay(cp.core, source, SecurityController.TwoWayCiphers.aes);
                // assert
                Assert.AreEqual(source, resultDecrypted);
                Assert.AreNotEqual(resultEncrypted, resultEncryptedDes);
                Assert.AreEqual("", invalidDecrypted);
            }
        }
        //====================================================================================================
        /// <summary>
        /// coreSecurity
        /// </summary>
        [TestMethod]
        public void Controllers_Security_EncryptDecrypt() {
            // arrange
            using (CPClass cp = new(testAppName)) {
                int testNumber = 12345;
                DateTime testDate = new DateTime(1990, 8, 7, 8, 8, 8);
                //
                // act
                string token = SecurityController.encodeToken(cp.core, testNumber, testDate);
                //
                // assert
                var result = SecurityController.decodeToken(cp.core, token);
                Assert.AreEqual(testNumber, result.id);
                Assert.AreEqual(testDate, result.expires);
            }
        }

    }
}