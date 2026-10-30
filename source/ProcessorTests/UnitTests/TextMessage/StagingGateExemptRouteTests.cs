using Contensive.Processor.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Contensive.Processor.Tests.UnitTests.TextMessage;

/// <summary>
/// The SNS endpoint and the tracked link redirect bypass the staging gate, nothing else does.
/// </summary>
[TestClass]
public class StagingGateExemptRouteTests {
    //
    [TestMethod]
    [DataRow("sms/aws/sns", true)]
    [DataRow("SMS/AWS/SNS", true)]
    [DataRow("tl", true)]
    [DataRow("tl/abcdefghij0123", true)]
    [DataRow("tl/abcdefghij0123/more", false)]
    [DataRow("tlx", false)]
    [DataRow("sms/aws", false)]
    [DataRow("sms/aws/sns/x", false)]
    [DataRow("oauth/register", false)]
    [DataRow("", false)]
    [DataRow(null, false)]
    public void isGateExemptRoute(string route, bool expected) {
        Assert.AreEqual(expected, StagingGateController.isGateExemptRoute(route));
    }
}
