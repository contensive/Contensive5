
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Contensive.Processor.Tests.UnitTests.Security;
//
//====================================================================================================
/// <summary>
/// Unit tests for cookie-less visit fingerprinting decisions (SessionFingerprintPolicy).
/// SECURITY FIX: a cookie-less request matched by IP+UserAgent could reuse another person's authenticated visit.
/// Fingerprinting must only group bots, never reuse an authenticated, Guest or real-person visit, never send
/// a reused session's cookies to another client, and honor site property "Allow Visit Fingerprinting".
/// No database required. SessionFingerprintPolicy.cs is compiled into this project as a linked source file
/// because Processor is strong-named and this test project is not.
/// </summary>
//====================================================================================================
//
[TestClass()]
public class SessionFingerprintPolicyTests {
    //
    // -- canReuseFingerprintVisit(fingerprintingEnabled, matchedVisitorIsBot, matchedVisitorCookieSupport, visitorMemberIsShareable,
    //    hasMatchedVisit, matchedVisitIsBot, matchedVisitAuthenticated, visitMemberIsShareable)
    //
    private static bool canReuse(bool enabled = true, bool visitorBot = true, bool visitorCookieSupport = false, bool visitorMemberShareable = true, bool hasVisit = true, bool visitBot = true, bool visitAuthenticated = false, bool visitMemberShareable = true) {
        return SessionFingerprintPolicy.canReuseFingerprintVisit(
            fingerprintingEnabled: enabled,
            matchedVisitorIsBot: visitorBot,
            matchedVisitorCookieSupport: visitorCookieSupport,
            visitorMemberIsShareable: visitorMemberShareable,
            hasMatchedVisit: hasVisit,
            matchedVisitIsBot: visitBot,
            matchedVisitAuthenticated: visitAuthenticated,
            visitMemberIsShareable: visitMemberShareable);
    }
    //
    [TestMethod]
    public void Fingerprint_Reuse_BotVisitorBotVisit_IsAllowed() {
        Assert.IsTrue(canReuse());
    }
    //
    [TestMethod]
    public void Fingerprint_Reuse_BotVisitorNoVisit_IsAllowed() {
        // -- no visit in the last hour: visitor reused, a new visit is created; visit-only inputs are ignored
        Assert.IsTrue(canReuse(hasVisit: false, visitBot: false, visitAuthenticated: true, visitMemberShareable: false));
    }
    //
    [TestMethod]
    public void Fingerprint_Reuse_AuthenticatedVisit_IsRefused() {
        Assert.IsFalse(canReuse(visitAuthenticated: true));
    }
    //
    [TestMethod]
    public void Fingerprint_Reuse_HumanVisitor_IsRefused() {
        Assert.IsFalse(canReuse(visitorBot: false));
    }
    //
    [TestMethod]
    public void Fingerprint_Reuse_NonBotVisit_IsRefused() {
        // -- S1: the reused visit must itself be a bot visit
        Assert.IsFalse(canReuse(visitBot: false));
    }
    //
    [TestMethod]
    public void Fingerprint_Reuse_VisitorThatReturnedCookies_IsRefused() {
        // -- B1: a visitor whose cookies came back belongs to that cookie client
        Assert.IsFalse(canReuse(visitorCookieSupport: true));
        Assert.IsFalse(canReuse(visitorCookieSupport: true, hasVisit: false));
    }
    //
    [TestMethod]
    public void Fingerprint_Reuse_NonShareableVisitorMember_IsRefused() {
        Assert.IsFalse(canReuse(visitorMemberShareable: false));
        Assert.IsFalse(canReuse(visitorMemberShareable: false, hasVisit: false));
    }
    //
    [TestMethod]
    public void Fingerprint_Reuse_NonShareableVisitMember_IsRefused() {
        // -- e.g. a bot visitor whose visit member is a Contact
        Assert.IsFalse(canReuse(visitMemberShareable: false));
    }
    //
    [TestMethod]
    public void Fingerprint_Reuse_KillSwitchOff_IsRefused() {
        Assert.IsFalse(canReuse(enabled: false));
        Assert.IsFalse(canReuse(enabled: false, hasVisit: false));
    }
    //
    [TestMethod]
    public void Fingerprint_Reuse_FullDecisionTable() {
        int allowed = 0;
        var bools = new[] { false, true };
        foreach (bool enabled in bools) {
            foreach (bool visitorBot in bools) {
                foreach (bool visitorCookie in bools) {
                    foreach (bool visitorMember in bools) {
                        foreach (bool hasVisit in bools) {
                            foreach (bool visitBot in bools) {
                                foreach (bool visitAuth in bools) {
                                    foreach (bool visitMember in bools) {
                                        bool result = canReuse(enabled, visitorBot, visitorCookie, visitorMember, hasVisit, visitBot, visitAuth, visitMember);
                                        bool expected = enabled && visitorBot && !visitorCookie && visitorMember && (!hasVisit || (visitBot && !visitAuth && visitMember));
                                        Assert.AreEqual(expected, result, $"enabled[{enabled}] visitorBot[{visitorBot}] visitorCookie[{visitorCookie}] visitorMember[{visitorMember}] hasVisit[{hasVisit}] visitBot[{visitBot}] visitAuth[{visitAuth}] visitMember[{visitMember}]");
                                        if (result) { allowed++; }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        // -- with a visit: exactly one combination; without a visit: the 8 visit-only combinations
        Assert.AreEqual(9, allowed);
    }
    //
    // -- shouldSendSessionCookies(fingerprintReused)
    //
    [TestMethod]
    public void Fingerprint_Cookies_NotSentOnFingerprintReuse() {
        Assert.IsFalse(SessionFingerprintPolicy.shouldSendSessionCookies(fingerprintReused: true));
    }
    //
    [TestMethod]
    public void Fingerprint_Cookies_SentForNormalSession() {
        Assert.IsTrue(SessionFingerprintPolicy.shouldSendSessionCookies(fingerprintReused: false));
    }
    //
    // -- isBotPerson(personTypeId, isAdmin, isDeveloper)
    //
    [TestMethod]
    public void Fingerprint_BotPerson_OnlyBotTypeQualifies() {
        Assert.IsTrue(SessionFingerprintPolicy.isBotPerson((int)PersonTypeEnum.Bot, false, false));
        Assert.IsFalse(SessionFingerprintPolicy.isBotPerson((int)PersonTypeEnum.Guest, false, false));
        Assert.IsFalse(SessionFingerprintPolicy.isBotPerson((int)PersonTypeEnum.Contact, false, false));
        Assert.IsFalse(SessionFingerprintPolicy.isBotPerson((int)PersonTypeEnum.Unknown, false, false));
        Assert.IsFalse(SessionFingerprintPolicy.isBotPerson(99, false, false));
    }
    //
    [TestMethod]
    public void Fingerprint_BotPerson_AdminOrDeveloper_IsNotBot() {
        Assert.IsFalse(SessionFingerprintPolicy.isBotPerson((int)PersonTypeEnum.Bot, true, false));
        Assert.IsFalse(SessionFingerprintPolicy.isBotPerson((int)PersonTypeEnum.Bot, false, true));
    }
    //
    // -- isShareableFingerprintMember(personFound, memberId, personTypeId, isAdmin, isDeveloper)
    //
    [TestMethod]
    public void Fingerprint_Member_FoundBotPerson_IsShareable() {
        Assert.IsTrue(SessionFingerprintPolicy.isShareableFingerprintMember(true, 10, (int)PersonTypeEnum.Bot, false, false));
    }
    //
    [TestMethod]
    public void Fingerprint_Member_GuestPerson_IsNotShareable() {
        // -- S1: a Guest can hold human-entered data (form, cart)
        Assert.IsFalse(SessionFingerprintPolicy.isShareableFingerprintMember(true, 10, (int)PersonTypeEnum.Guest, false, false));
    }
    //
    [TestMethod]
    public void Fingerprint_Member_NoMemberOrMissingPerson_IsNotShareable() {
        Assert.IsFalse(SessionFingerprintPolicy.isShareableFingerprintMember(false, 0, 0, false, false));
        Assert.IsFalse(SessionFingerprintPolicy.isShareableFingerprintMember(true, 0, (int)PersonTypeEnum.Bot, false, false));
        Assert.IsFalse(SessionFingerprintPolicy.isShareableFingerprintMember(false, 10, (int)PersonTypeEnum.Bot, false, false));
    }
    //
    [TestMethod]
    public void Fingerprint_Member_ContactAdminDeveloper_IsNotShareable() {
        Assert.IsFalse(SessionFingerprintPolicy.isShareableFingerprintMember(true, 10, (int)PersonTypeEnum.Contact, false, false));
        Assert.IsFalse(SessionFingerprintPolicy.isShareableFingerprintMember(true, 10, (int)PersonTypeEnum.Bot, true, false));
        Assert.IsFalse(SessionFingerprintPolicy.isShareableFingerprintMember(true, 10, (int)PersonTypeEnum.Bot, false, true));
    }
    //
    // -- isGuestOrBotPerson(personTypeId, isAdmin, isDeveloper): used for the save decision only
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
