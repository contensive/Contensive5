using Contensive.Processor;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
        // act
        using (CPClass cp = new()) {
            cp.core.webServer.verifyAppPool(appPoolName);
        }
        // assert
        Assert.AreEqual("", "");
    }
}
