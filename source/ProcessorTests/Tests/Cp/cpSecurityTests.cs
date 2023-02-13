using Contensive.Processor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Tests.TestConstants;

namespace Tests {
    [TestClass]
    public class cpSecurityTests {
        //====================================================================================================
        /// <summary>
        ///  encode one-way
        /// </summary>
        [TestMethod]
        public void cp_security_encodeOneWay() {
            // arrange
            using (var cp = new CPClass(testAppName)) {
                // arrange
                string srcA = cp.Utils.GetRandomInteger().ToString();
                string srcB = cp.Utils.GetRandomInteger().ToString();
                // act
                string hashA1 = cp.Security.EncryptOneWay(srcA);
                string hashA2 = cp.Security.EncryptOneWay(srcA);
                string hashB = cp.Security.EncryptOneWay(srcB);
                // assert
                Assert.IsFalse(string.IsNullOrEmpty(srcA));
                Assert.IsFalse(string.IsNullOrEmpty(srcB));
                Assert.IsFalse(string.IsNullOrEmpty(hashA1));
                Assert.IsFalse(string.IsNullOrEmpty(hashA2));
                Assert.IsFalse(string.IsNullOrEmpty(hashB));
                Assert.AreNotEqual(srcA, hashA1);
                Assert.AreNotEqual(srcA, hashA2);
                Assert.AreNotEqual(srcA, hashB);
                Assert.AreNotEqual(srcB, hashA1);
                Assert.AreNotEqual(srcB, hashA2);
                Assert.AreNotEqual(srcB, hashB);
                Assert.AreEqual(hashA1, hashA2);
            }
        }
        [TestMethod]
        public void cp_security_encodeOneWay_salt() {
            // arrange
            using (var cp = new CPClass(testAppName)) {
                // arrange
                string salt1 = cp.Utils.GetRandomInteger().ToString();
                string salt2 = cp.Utils.GetRandomInteger().ToString();
                string srcA = cp.Utils.GetRandomInteger().ToString();
                // act
                string hashA1 = cp.Security.EncryptOneWay(srcA, salt1);
                string hashA2 = cp.Security.EncryptOneWay(srcA, salt2);
                string hashA1b = cp.Security.EncryptOneWay(srcA, salt1);
                string hashA2b = cp.Security.EncryptOneWay(srcA, salt2);
                // assert
                // -- nothing empty
                Assert.IsFalse(string.IsNullOrEmpty(salt1));
                Assert.IsFalse(string.IsNullOrEmpty(salt2));
                Assert.IsFalse(string.IsNullOrEmpty(srcA));
                Assert.IsFalse(string.IsNullOrEmpty(hashA1));
                Assert.IsFalse(string.IsNullOrEmpty(hashA2));
                // nothing matches
                Assert.AreNotEqual(salt1, salt2);
                Assert.AreNotEqual(salt1, srcA);
                Assert.AreNotEqual(salt1, hashA1);
                Assert.AreNotEqual(salt1, hashA2);
                Assert.AreNotEqual(salt2, srcA);
                Assert.AreNotEqual(salt2, hashA1);
                Assert.AreNotEqual(salt2, hashA2);
                Assert.AreNotEqual(srcA, hashA1);
                Assert.AreNotEqual(srcA, hashA2);
                Assert.AreNotEqual(hashA1, hashA2);
                //repeatable
                Assert.AreEqual(hashA1, hashA1b);
                Assert.AreEqual(hashA2, hashA2b);
            }
        }
    }
}
