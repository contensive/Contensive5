using Contensive.Processor.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Contensive.Processor.Tests.UnitTests.TextMessage;

/// <summary>
/// pure tests, no app or provider required
/// </summary>
[TestClass]
public class SmsPhoneControllerTests {
    //
    [TestMethod]
    public void toE164_us_tenDigits() {
        Assert.AreEqual("+17033039974", SmsPhoneController.toE164("7033039974", "1"));
    }
    //
    [TestMethod]
    public void toE164_us_elevenDigits() {
        Assert.AreEqual("+17033039974", SmsPhoneController.toE164("17033039974", "1"));
    }
    //
    [TestMethod]
    public void toE164_us_formatted() {
        Assert.AreEqual("+17033039974", SmsPhoneController.toE164("(703) 303-9974", "1"));
        Assert.AreEqual("+17033039974", SmsPhoneController.toE164("  703.303.9974 ", "1"));
        Assert.AreEqual("+17033039974", SmsPhoneController.toE164("+1 (703) 303-9974", "1"));
    }
    //
    [TestMethod]
    public void toE164_us_blankCountryCodeDefaultsToOne() {
        Assert.AreEqual("+17033039974", SmsPhoneController.toE164("7033039974", ""));
        Assert.AreEqual("+17033039974", SmsPhoneController.toE164("7033039974", null));
    }
    //
    [TestMethod]
    public void toE164_international_plusAndDoubleZero() {
        Assert.AreEqual("+442071234567", SmsPhoneController.toE164("+44 20 7123 4567", "1"));
        Assert.AreEqual("+442071234567", SmsPhoneController.toE164("0044 20 7123 4567", "1"));
    }
    //
    [TestMethod]
    public void toE164_nonUsDefault_stripsTrunkZero() {
        Assert.AreEqual("+442071234567", SmsPhoneController.toE164("020 7123 4567", "44"));
        Assert.AreEqual("+442071234567", SmsPhoneController.toE164("2071234567", "+44"));
    }
    //
    [TestMethod]
    public void toE164_invalid_returnsEmpty() {
        Assert.AreEqual("", SmsPhoneController.toE164(null, "1"));
        Assert.AreEqual("", SmsPhoneController.toE164("", "1"));
        Assert.AreEqual("", SmsPhoneController.toE164("   ", "1"));
        Assert.AreEqual("", SmsPhoneController.toE164("abc", "1"));
        // -- too short and too long for NANP
        Assert.AreEqual("", SmsPhoneController.toE164("303-9974", "1"));
        Assert.AreEqual("", SmsPhoneController.toE164("270330399741", "1"));
        // -- 11 digits not starting with 1
        Assert.AreEqual("", SmsPhoneController.toE164("27033039974", "1"));
        // -- E.164 length limits
        Assert.AreEqual("", SmsPhoneController.toE164("+1234567", "1"));
        Assert.AreEqual("", SmsPhoneController.toE164("+1234567890123456", "1"));
        // -- country code cannot start with 0
        Assert.AreEqual("", SmsPhoneController.toE164("+0123456789", "1"));
    }
    //
    [TestMethod]
    public void toE164_lettersStripped() {
        Assert.AreEqual("+17033039974", SmsPhoneController.toE164("tel: 703-303-9974", "1"));
    }
    //
    [TestMethod]
    public void isE164_test() {
        Assert.IsTrue(SmsPhoneController.isE164("+17033039974"));
        Assert.IsFalse(SmsPhoneController.isE164("7033039974"));
        Assert.IsFalse(SmsPhoneController.isE164(""));
        Assert.IsFalse(SmsPhoneController.isE164(null));
    }
}
