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
                // act
                // assert
                Assert.Fail();
            }
        }
        public void cp_security_encodeOneWay_salt() {
            // arrange
            using (var cp = new CPClass(testAppName)) {
                // arrange
                // act
                // assert
                Assert.Fail();
            }
        }
    }
}
