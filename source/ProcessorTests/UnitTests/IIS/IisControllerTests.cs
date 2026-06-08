using Contensive.Processor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.Administration;

namespace Contensive.Processor.Tests.UnitTests.IIS;

[TestClass]
//
// ------- requires elevated permissions
//
public class IISControllerUnitTests {
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
