
using Contensive.Models.Db;
using System;
using System.Globalization;
//
namespace Contensive.Processor.Controllers {
    /// <summary>
    /// Validates bearer tokens created by the createBearerToken remote method.
    /// The token is a two-way encrypted string containing a 40-char random key and an expiration date,
    /// separated by a pipe delimiter. The 40-char key is stored in the user's bearerToken field.
    /// </summary>
    public static class BearerTokenAuthController {
        //
        private const string delimiter = "|";
        //
        //====================================================================================================
        /// <summary>
        /// Given a raw Authorization header value (e.g. "Bearer Abc123..."), decrypt and validate the
        /// bearer token. If valid and not expired, authenticates the matching user into the current session
        /// and returns true.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="session">The session to authenticate into. Passed explicitly so this method can be called during session construction before core.session is assigned.</param>
        /// <param name="authorizationHeader">The full value of the Authorization header.</param>
        /// <param name="authenticatedUserId">The id of the authenticated user, or 0 on failure.</param>
        public static bool tryAuthenticateByBearerToken(CoreController core, SessionController session, string authorizationHeader, out int authenticatedUserId) {
            authenticatedUserId = 0;
            try {
                PersonModel resolvedUser = tryResolveUserByBearerToken(core, authorizationHeader);
                if (resolvedUser == null) { return false; }
                //
                // -- authenticate into the provided session
                if (!AuthController.authenticateById(core, session, resolvedUser.id)) { return false; }
                authenticatedUserId = resolvedUser.id;
                return true;
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                return false;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Decrypt and validate a bearer token, returning the matching user without modifying
        /// any session state. Returns null if the token is missing, invalid, or expired.
        /// Used for early user identification before visit/visitor records are created.
        /// </summary>
        public static PersonModel tryResolveUserByBearerToken(CoreController core, string authorizationHeader) {
            try {
                //
                // -- expect "Bearer <token>"
                if (string.IsNullOrWhiteSpace(authorizationHeader)) { return null; }
                if (!authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) { return null; }
                string encryptedToken = authorizationHeader.Substring(7).Trim();
                if (string.IsNullOrWhiteSpace(encryptedToken)) { return null; }
                //
                // -- decrypt
                string rawToken = SecurityController.decryptTwoWay(core, encryptedToken);
                if (string.IsNullOrWhiteSpace(rawToken)) { return null; }
                //
                // -- split into random-key and expiry
                int delimPos = rawToken.IndexOf(delimiter, StringComparison.Ordinal);
                if (delimPos < 0) { return null; }
                string randomKey = rawToken.Substring(0, delimPos);
                string expiryString = rawToken.Substring(delimPos + 1);
                if (string.IsNullOrWhiteSpace(randomKey) || string.IsNullOrWhiteSpace(expiryString)) { return null; }
                //
                // -- check expiry
                if (!DateTime.TryParseExact(expiryString, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime expiry)) { return null; }
                if (expiry < core.dateTimeNowMockable) {
                    core.webServer.setResponseStatus(WebServerController.httpResponseStatus.Unauthorized);
                    return null;
                }
                //
                // -- find the user whose bearerToken field matches the random key
                var userList = DbBaseModel.createList<PersonModel>(core.cpParent, $"(bearerToken={DbController.encodeSQLText(randomKey)})and(active>0)", "id", 2);
                if (userList.Count != 1) { return null; }
                return userList[0];
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                return null;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Build the encrypted bearer token string to present to the user.
        /// Combines the 40-char randomKey with the expiry date and two-way encrypts it.
        /// </summary>
        public static string buildEncryptedToken(CoreController core, string randomKey, DateTime expiry) {
            string rawToken = $"{randomKey}{delimiter}{expiry:yyyy-MM-ddTHH:mm:ss}";
            return SecurityController.encryptTwoWay(core, rawToken);
        }
        //
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
