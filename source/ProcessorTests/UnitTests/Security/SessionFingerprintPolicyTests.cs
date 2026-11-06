
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Contensive.Processor.Tests.UnitTests.Security;
//
//====================================================================================================
/// <summary>
/// Unit tests for cookie-less visit fingerprinting decisions (SessionFingerprintPolicy).
/// SECURITY FIX: a cookie-less request matched by IP+UserAgent could reuse another person's authenticated visit.
/// Fingerprinting must only group bots, never reuse an authenticated or real-person visit, and honor
/// site property "Allow Visit Fingerprinting". No database required.
/// </summary>
//====================================================================================================
//
[TestClass()]
public class SessionFingerprintPolicyTests {
    //
    // -- canReuseFingerprintVisit(fingerprintingEnabled, matchedVisitorIsBot, visitAuthenticated, memberIsGuestOrBot)
    //
    [TestMethod]
    public void Fingerprint_Reuse_AuthenticatedVisit_IsRefused() {
        Assert.IsFalse(SessionFingerprintPolicy.canReuseFingerprintVisit(true, true, true, true));
    }
    //
    [TestMethod]
    public void Fingerprint_Reuse_HumanVisitor_IsRefused() {
        Assert.IsFalse(SessionFingerprintPolicy.canReuseFingerprintVisit(true, false, false, true));
    }
    //
    [TestMethod]
    public void Fingerprint_Reuse_RealPersonMember_IsRefused() {
        Assert.IsFalse(SessionFingerprintPolicy.canReuseFingerprintVisit(true, true, false, false));
    }
    //
    [TestMethod]
    public void Fingerprint_Reuse_BotVisitorGuestVisit_IsAllowed() {
        Assert.IsTrue(SessionFingerprintPolicy.canReuseFingerprintVisit(true, true, false, true));
    }
    //
    [TestMethod]
    public void Fingerprint_Reuse_KillSwitchOff_IsRefused() {
        Assert.IsFalse(SessionFingerprintPolicy.canReuseFingerprintVisit(false, true, false, true));
    }
    //
    [TestMethod]
    public void Fingerprint_Reuse_OnlyOneCombinationAllowed() {
        int allowed = 0;
        foreach (bool enabled in new[] { false, true }) {
            foreach (bool bot in new[] { false, true }) {
                foreach (bool auth in new[] { false, true }) {
                    foreach (bool guest in new[] { false, true }) {
                        if (SessionFingerprintPolicy.canReuseFingerprintVisit(enabled, bot, auth, guest)) {
                            allowed++;
                            Assert.IsTrue(enabled && bot && !auth && guest, $"unexpected reuse enabled[{enabled}] bot[{bot}] auth[{auth}] guest[{guest}]");
                        }
                    }
                }
            }
        }
        Assert.AreEqual(1, allowed);
    }
    //
    // -- isGuestOrBotPerson(personTypeId, isAdmin, isDeveloper)
    //
    [TestMethod]
    public void Fingerprint_Person_GuestAndBot_AreShareable() {
        Assert.IsTrue(SessionFingerprintPolicy.isGuestOrBotPerson((int)PersonTypeEnum.Guest, false, false));
        Assert.IsTrue(SessionFingerprintPolicy.isGuestOrBotPerson((int)PersonTypeEnum.Bot, false, false));
    }
    //
    [TestMethod]
    public void Fingerprint_Person_ContactAndUnknown_AreNotShareable() {
        Assert.IsFalse(SessionFingerprintPolicy.isGuestOrBotPerson((int)PersonTypeEnum.Contact, false, false));
        Assert.IsFalse(SessionFingerprintPolicy.isGuestOrBotPerson((int)PersonTypeEnum.Unknown, false, false));
        Assert.IsFalse(SessionFingerprintPolicy.isGuestOrBotPerson(99, false, false));
    }
    //
    [TestMethod]
    public void Fingerprint_Person_AdminOrDeveloper_AreNeverShareable() {
        Assert.IsFalse(SessionFingerprintPolicy.isGuestOrBotPerson((int)PersonTypeEnum.Guest, true, false));
        Assert.IsFalse(SessionFingerprintPolicy.isGuestOrBotPerson((int)PersonTypeEnum.Bot, false, true));
    }
    //
    // -- shouldSaveFingerprint(enabled, isFingerprintedSession, hasHash, visitorId, visitIsBot, visitorIsBot, visitAuthenticated, userIsGuestOrBot)
    //
    [TestMethod]
    public void Fingerprint_Save_BotVisit_IsSaved() {
        Assert.IsTrue(SessionFingerprintPolicy.shouldSaveFingerprint(true, true, true, 10, true, false, false, true));
        Assert.IsTrue(SessionFingerprintPolicy.shouldSaveFingerprint(true, true, true, 10, false, true, false, true));
    }
    //
    [TestMethod]
    public void Fingerprint_Save_HumanVisit_IsNotSaved() {
        Assert.IsFalse(SessionFingerprintPolicy.shouldSaveFingerprint(true, true, true, 10, false, false, false, true));
    }
    //
    [TestMethod]
    public void Fingerprint_Save_KillSwitchOff_IsNotSaved() {
        Assert.IsFalse(SessionFingerprintPolicy.shouldSaveFingerprint(false, true, true, 10, true, true, false, true));
    }
    //
    [TestMethod]
    public void Fingerprint_Save_AuthenticatedOrRealPerson_IsNotSaved() {
        Assert.IsFalse(SessionFingerprintPolicy.shouldSaveFingerprint(true, true, true, 10, true, true, true, true));
        Assert.IsFalse(SessionFingerprintPolicy.shouldSaveFingerprint(true, true, true, 10, true, true, false, false));
    }
    //
    [TestMethod]
    public void Fingerprint_Save_NotFingerprintedOrNoHashOrNoVisitor_IsNotSaved() {
        Assert.IsFalse(SessionFingerprintPolicy.shouldSaveFingerprint(true, false, true, 10, true, true, false, true));
        Assert.IsFalse(SessionFingerprintPolicy.shouldSaveFingerprint(true, true, false, 10, true, true, false, true));
        Assert.IsFalse(SessionFingerprintPolicy.shouldSaveFingerprint(true, true, true, 0, true, true, false, true));
    }
}
