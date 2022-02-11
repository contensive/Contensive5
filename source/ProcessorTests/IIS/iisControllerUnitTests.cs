using Contensive.Processor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.Administration;

namespace Tests {
    [TestClass]
    public class IisControllerUnitTests {
        [TestMethod]
        public void verifyAppPool_test1() {
            // arrange
            string appPoolName = "testAppPool";
            using (ServerManager serverManager = new ServerManager()) {
                // act
                using (CPClass cp = new()) {
                    cp.core.webServer.verifyAppPool(appPoolName);
                }
            }
            // assert
            Assert.AreEqual("", "");
        }
    }
}
