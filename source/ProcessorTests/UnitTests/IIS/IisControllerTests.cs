using Contensive.Processor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Principal;

namespace Contensive.Processor.Tests.UnitTests.IIS;

[TestClass]
//
// ------- requires elevated permissions
//
public class IISControllerUnitTests {
    [TestMethod]
    public void verifyAppPool_test1() {
        // arrange
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        if (!principal.IsInRole(WindowsBuiltInRole.Administrator)) {
            Assert.Inconclusive("Test requires elevated (admin) permissions to access IIS configuration.");
        }
        string appPoolName = "testAppPool";
        // act
        using (CPClass cp = new()) {
            cp.core.webServer.verifyAppPool(appPoolName);
        }
        // assert
        Assert.AreEqual("", "");
    }
}
