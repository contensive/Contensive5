
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
                //
                // -- expect "Bearer <token>"
                if (string.IsNullOrWhiteSpace(authorizationHeader)) { return false; }
                if (!authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) { return false; }
                string encryptedToken = authorizationHeader.Substring(7).Trim();
                if (string.IsNullOrWhiteSpace(encryptedToken)) { return false; }
                //
                // -- decrypt
                string rawToken = SecurityController.decryptTwoWay(core, encryptedToken);
                if (string.IsNullOrWhiteSpace(rawToken)) { return false; }
                //
                // -- split into random-key and expiry
                int delimPos = rawToken.IndexOf(delimiter, StringComparison.Ordinal);
                if (delimPos < 0) { return false; }
                string randomKey = rawToken.Substring(0, delimPos);
                string expiryString = rawToken.Substring(delimPos + 1);
                if (string.IsNullOrWhiteSpace(randomKey) || string.IsNullOrWhiteSpace(expiryString)) { return false; }
                //
                // -- check expiry
                if (!DateTime.TryParseExact(expiryString, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime expiry)) { return false; }
                if (expiry < core.dateTimeNowMockable) {
                    core.webServer.setResponseStatus(WebServerController.httpResponseStatus.Unauthorized);
                    return false;
                }
                //
                // -- find the user whose bearerToken field matches the random key
                var userList = DbBaseModel.createList<PersonModel>(core.cpParent, $"(bearerToken={DbController.encodeSQLText(randomKey)})and(active>0)", "id", 2);
                if (userList.Count != 1) { return false; }
                int userId = userList[0].id;
                //
                // -- authenticate into the provided session
                if (!AuthController.authenticateById(core, session, userId)) { return false; }
                authenticatedUserId = userId;
                return true;
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                return false;
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
