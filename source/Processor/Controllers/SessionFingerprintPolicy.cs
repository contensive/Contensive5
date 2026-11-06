
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
        /// true if a person record may be shared through a fingerprint match: only Bot and Guest person types,
        /// and never an admin or developer. Unknown or Contact types are treated as real people (fail closed).
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
        /// true if the visitor (and its latest visit, if any) found by a fingerprint match may be reused for this request.
        /// All conditions must hold, otherwise the caller starts a new visitor and visit.
        /// </summary>
        /// <param name="fingerprintingEnabled">site property Allow Visit Fingerprinting</param>
        /// <param name="matchedVisitorIsBot">the matched ccvisitors row has bot=1</param>
        /// <param name="visitAuthenticated">the visit that would be reused is authenticated (false if there is no visit to reuse)</param>
        /// <param name="memberIsGuestOrBot">every person attached to the matched visitor and visit is a guest or bot (or none)</param>
        internal static bool canReuseFingerprintVisit(bool fingerprintingEnabled, bool matchedVisitorIsBot, bool visitAuthenticated, bool memberIsGuestOrBot) {
            return fingerprintingEnabled && matchedVisitorIsBot && !visitAuthenticated && memberIsGuestOrBot;
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
