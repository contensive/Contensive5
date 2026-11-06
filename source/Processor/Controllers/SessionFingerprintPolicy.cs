
using Contensive.Models.Db;
//
namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// Pure decision rules for cookie-less (IP+UserAgent) visit fingerprinting.
    /// Fingerprinting exists only to group bot/crawler hits into one visitor/visit. It must never hand a
    /// human or authenticated visit to another request. No database or core access here so it can be unit tested.
    /// This file is also compiled into ProcessorTests (linked source) because Processor is strong-named.
    /// </summary>
    internal static class SessionFingerprintPolicy {
        //
        //====================================================================================================
        /// <summary>
        /// true if the person is a Bot or Guest person type and never an admin or developer. Used only for the
        /// fingerprint save decision (reuse requires isBotPerson). Unknown or Contact types are real people (fail closed).
        /// </summary>
        /// <param name="personTypeId">PersonModel.personTypeId</param>
        /// <param name="isAdmin">PersonModel.admin</param>
        /// <param name="isDeveloper">PersonModel.developer</param>
        internal static bool isGuestOrBotPerson(int personTypeId, bool isAdmin, bool isDeveloper) {
            if (isAdmin || isDeveloper) { return false; }
            return personTypeId == (int)PersonTypeEnum.Bot || personTypeId == (int)PersonTypeEnum.Guest;
        }
        //
        //====================================================================================================
        /// <summary>
        /// true if the person is a Bot person type and not an admin or developer. Only Bot persons may be
        /// shared through a fingerprint match. Guest persons can hold human-entered data (forms, carts) and
        /// are not shareable. Unknown, Contact and anything else return false (fail closed).
        /// </summary>
        /// <param name="personTypeId">PersonModel.personTypeId</param>
        /// <param name="isAdmin">PersonModel.admin</param>
        /// <param name="isDeveloper">PersonModel.developer</param>
        internal static bool isBotPerson(int personTypeId, bool isAdmin, bool isDeveloper) {
            if (isAdmin || isDeveloper) { return false; }
            return personTypeId == (int)PersonTypeEnum.Bot;
        }
        //
        //====================================================================================================
        /// <summary>
        /// true if the member attached to a fingerprint match (visitor.memberId or visit.memberId) may be shared.
        /// Bot visits always create a Bot person (SessionController, new user setup), so a shareable member is a
        /// loaded Bot person. memberId 0 (no person) and a missing person record return false (fail closed).
        /// </summary>
        /// <param name="personFound">the person record for memberId was loaded</param>
        /// <param name="memberId">the memberId on the matched visitor or visit</param>
        /// <param name="personTypeId">PersonModel.personTypeId (ignored if not found)</param>
        /// <param name="isAdmin">PersonModel.admin (ignored if not found)</param>
        /// <param name="isDeveloper">PersonModel.developer (ignored if not found)</param>
        internal static bool isShareableFingerprintMember(bool personFound, int memberId, int personTypeId, bool isAdmin, bool isDeveloper) {
            if (memberId <= 0 || !personFound) { return false; }
            return isBotPerson(personTypeId, isAdmin, isDeveloper);
        }
        //
        //====================================================================================================
        /// <summary>
        /// true if the visitor (and its latest visit, if any) found by a fingerprint match may be reused for this request.
        /// All conditions must hold, otherwise the caller starts a new visitor and visit.
        /// </summary>
        /// <param name="fingerprintingEnabled">site property Allow Visit Fingerprinting</param>
        /// <param name="matchedVisitorIsBot">the matched ccvisitors row has bot=1</param>
        /// <param name="matchedVisitorCookieSupport">the matched ccvisitors row has cookieSupport=1 (a client has returned its cookies, so it belongs to that client)</param>
        /// <param name="visitorMemberIsShareable">isShareableFingerprintMember for the matched visitor's memberId</param>
        /// <param name="hasMatchedVisit">a visit for the matched visitor exists within the last hour and would be reused</param>
        /// <param name="matchedVisitIsBot">the matched visit is flagged bot (ignored if there is no matched visit)</param>
        /// <param name="matchedVisitAuthenticated">the matched visit is authenticated (ignored if there is no matched visit)</param>
        /// <param name="visitMemberIsShareable">isShareableFingerprintMember for the matched visit's memberId (ignored if there is no matched visit)</param>
        internal static bool canReuseFingerprintVisit(bool fingerprintingEnabled, bool matchedVisitorIsBot, bool matchedVisitorCookieSupport, bool visitorMemberIsShareable, bool hasMatchedVisit, bool matchedVisitIsBot, bool matchedVisitAuthenticated, bool visitMemberIsShareable) {
            if (!fingerprintingEnabled || !matchedVisitorIsBot || matchedVisitorCookieSupport || !visitorMemberIsShareable) { return false; }
            if (!hasMatchedVisit) { return true; }
            return matchedVisitIsBot && !matchedVisitAuthenticated && visitMemberIsShareable;
        }
        //
        //====================================================================================================
        /// <summary>
        /// true if the visit and visitor cookies may be sent to the client for this request.
        /// A visitor or visit reused through a fingerprint match belongs to the client that created it. Sending
        /// its cookies to another client would let both share one session, so a later login by either is shared.
        /// </summary>
        /// <param name="fingerprintReused">this request reused a visitor (and maybe a visit) through a fingerprint match</param>
        internal static bool shouldSendSessionCookies(bool fingerprintReused) {
            return !fingerprintReused;
        }
        //
        //====================================================================================================
        /// <summary>
        /// true if the fingerprint hash should be written to the session visitor record.
        /// Only bot sessions are fingerprinted, and never an authenticated visit or a real person.
        /// </summary>
        /// <param name="fingerprintingEnabled">site property Allow Visit Fingerprinting</param>
        /// <param name="isFingerprintedSession">the request arrived with no cookies and no bearer token</param>
        /// <param name="hasFingerprintHash">a fingerprint hash was computed</param>
        /// <param name="visitorId">the session visitor id</param>
        /// <param name="visitIsBot">the session visit is flagged bot</param>
        /// <param name="visitorIsBot">the session visitor is flagged bot</param>
        /// <param name="visitAuthenticated">the session visit is authenticated</param>
        /// <param name="userIsGuestOrBot">the session user is a guest or bot (see isGuestOrBotPerson)</param>
        internal static bool shouldSaveFingerprint(bool fingerprintingEnabled, bool isFingerprintedSession, bool hasFingerprintHash, int visitorId, bool visitIsBot, bool visitorIsBot, bool visitAuthenticated, bool userIsGuestOrBot) {
            if (!fingerprintingEnabled || !isFingerprintedSession || !hasFingerprintHash || visitorId <= 0) { return false; }
            if (visitAuthenticated || !userIsGuestOrBot) { return false; }
            return visitIsBot || visitorIsBot;
        }
    }
}
