using Contensive.Processor.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Contensive.Processor.Controllers.SmsKeywordController;

namespace Contensive.Processor.Tests.UnitTests.TextMessage;

[TestClass]
public class SmsKeywordControllerTests {
    //
    [TestMethod]
    [DataRow("stop", KeywordEnum.OptOut)]
    [DataRow(" STOP. ", KeywordEnum.OptOut)]
    [DataRow("Stop!", KeywordEnum.OptOut)]
    [DataRow("STOPALL", KeywordEnum.OptOut)]
    [DataRow("unsubscribe", KeywordEnum.OptOut)]
    [DataRow("Cancel", KeywordEnum.OptOut)]
    [DataRow("end", KeywordEnum.OptOut)]
    [DataRow("quit", KeywordEnum.OptOut)]
    [DataRow("opt-out", KeywordEnum.OptOut)]
    [DataRow("OPTOUT", KeywordEnum.OptOut)]
    [DataRow("revoke", KeywordEnum.OptOut)]
    [DataRow("Stop please", KeywordEnum.None)]
    [DataRow("don't stop", KeywordEnum.None)]
    [DataRow("UNSTOP", KeywordEnum.OptIn)]
    [DataRow("start", KeywordEnum.OptIn)]
    [DataRow("help", KeywordEnum.Help)]
    [DataRow("Info.", KeywordEnum.Help)]
    [DataRow("Hi, call me back", KeywordEnum.None)]
    [DataRow("", KeywordEnum.None)]
    [DataRow(null, KeywordEnum.None)]
    public void classify_body(string body, KeywordEnum expected) {
        Assert.AreEqual(expected, classify(body));
    }
    //
    [TestMethod]
    public void classify_providerKeywordOptOut_wins() {
        Assert.AreEqual(KeywordEnum.OptOut, classify("please stop texting me", "STOP"));
        Assert.AreEqual(KeywordEnum.OptOut, classify("START", "STOP"));
        Assert.AreEqual(KeywordEnum.OptIn, classify("START", ""));
        Assert.AreEqual(KeywordEnum.None, classify("hello", "JOIN"));
        Assert.AreEqual(KeywordEnum.Help, classify("help", null));
    }
    //
    [TestMethod]
    public void getEventKeyword_values() {
        Assert.AreEqual("START", getEventKeyword(KeywordEnum.OptIn));
        Assert.AreEqual("HELP", getEventKeyword(KeywordEnum.Help));
        Assert.AreEqual("", getEventKeyword(KeywordEnum.None));
        Assert.AreEqual("", getEventKeyword(KeywordEnum.OptOut));
    }
}
