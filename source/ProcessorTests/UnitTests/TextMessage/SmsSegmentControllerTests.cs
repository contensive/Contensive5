using Contensive.BaseModels;
using Contensive.Processor.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Contensive.Processor.Tests.UnitTests.TextMessage;

/// <summary>
/// pure tests, no app or provider required
/// </summary>
[TestClass]
public class SmsSegmentControllerTests {
    //
    [TestMethod]
    public void getInfo_empty() {
        SmsSegmentInfoModel info = SmsSegmentController.getInfo("");
        Assert.AreEqual(0, info.characters);
        Assert.AreEqual(0, info.segments);
        Assert.AreEqual("GSM-7", info.encoding);
        Assert.AreEqual(0, SmsSegmentController.getInfo(null).segments);
    }
    //
    [TestMethod]
    public void getInfo_gsm_160_vs_161() {
        SmsSegmentInfoModel info160 = SmsSegmentController.getInfo(new string('a', 160));
        Assert.AreEqual(160, info160.characters);
        Assert.AreEqual(1, info160.segments);
        Assert.AreEqual("GSM-7", info160.encoding);
        SmsSegmentInfoModel info161 = SmsSegmentController.getInfo(new string('a', 161));
        Assert.AreEqual(161, info161.characters);
        Assert.AreEqual(2, info161.segments);
        Assert.AreEqual(2, SmsSegmentController.getInfo(new string('a', 306)).segments);
        Assert.AreEqual(3, SmsSegmentController.getInfo(new string('a', 307)).segments);
    }
    //
    [TestMethod]
    public void getInfo_gsm_extensionCharacterCountsTwo() {
        SmsSegmentInfoModel info = SmsSegmentController.getInfo("[abc]");
        Assert.AreEqual(7, info.characters);
        Assert.AreEqual("GSM-7", info.encoding);
        // -- 159 basic + 1 extension char = 161 units, 2 segments
        SmsSegmentInfoModel over = SmsSegmentController.getInfo($"{new string('a', 159)}{{");
        Assert.AreEqual(161, over.characters);
        Assert.AreEqual(2, over.segments);
        // -- 158 basic + 1 extension char = 160 units, 1 segment
        Assert.AreEqual(1, SmsSegmentController.getInfo($"{new string('a', 158)}{{").segments);
        Assert.AreEqual(2, SmsSegmentController.getInfo("\u20AC").characters);
    }
    //
    [TestMethod]
    public void getInfo_gsm_accentedAndNewline() {
        SmsSegmentInfoModel info = SmsSegmentController.getInfo("caf\u00E9\r\nna\u00F1o");
        Assert.AreEqual("GSM-7", info.encoding);
        Assert.AreEqual(1, info.segments);
    }
    //
    [TestMethod]
    public void getInfo_emoji_forcesUcs2() {
        // -- grinning face, 2 UTF-16 units
        SmsSegmentInfoModel info = SmsSegmentController.getInfo("hi \uD83D\uDE00");
        Assert.AreEqual("UCS-2", info.encoding);
        Assert.AreEqual(5, info.characters);
        Assert.AreEqual(1, info.segments);
    }
    //
    [TestMethod]
    public void getInfo_ucs2_70_vs_71() {
        string emoji = "\uD83D\uDE00";
        // -- 68 + 2 = 70 units, 1 segment
        Assert.AreEqual(1, SmsSegmentController.getInfo($"{new string('a', 68)}{emoji}").segments);
        // -- 69 + 2 = 71 units, 2 segments of 67
        SmsSegmentInfoModel info = SmsSegmentController.getInfo($"{new string('a', 69)}{emoji}");
        Assert.AreEqual(71, info.characters);
        Assert.AreEqual(2, info.segments);
        // -- 134 units = 2 segments, 135 units = 3 segments
        Assert.AreEqual(2, SmsSegmentController.getInfo($"{new string('a', 132)}{emoji}").segments);
        Assert.AreEqual(3, SmsSegmentController.getInfo($"{new string('a', 133)}{emoji}").segments);
    }
}
