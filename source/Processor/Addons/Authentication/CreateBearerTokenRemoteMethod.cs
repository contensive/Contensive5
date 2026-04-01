
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using System;
//
namespace Contensive.Processor.Addons.Primitives {
    //
    //====================================================================================================
    /// <summary>
    /// Remote method that generates a bearer token for a given user.
    /// Called from the admin user edit page via the "Create Bearer Token" button.
    /// The encrypted token is returned to the client for use in Authorization headers.
    /// </summary>
    public class CreateBearerTokenRemoteMethod : AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// Generate a bearer token for the specified user.
        /// Request params: userId (integer, required)
        /// Returns JSON: { "success": bool, "message": string, "token": string, "expires": string }
        /// </summary>
        public override object Execute(CPBaseClass cp) {
            try {
                CoreController core = ((CPClass)cp).core;
                //
                // -- require admin or the user themselves
                if (!cp.User.IsAdmin) {
                    int requestUserId = cp.Doc.GetInteger("userId");
                    if (requestUserId == 0 || requestUserId != cp.User.Id) {
                        cp.Response.SetStatus(WebServerController.httpResponseStatus.Unauthorized);
                        return createJsonResponse(false, "Admin access required.", null, null);
                    }
                }
                //
                // -- get the target userId
                int userId = cp.Doc.GetInteger("userId");
                if (userId <= 0) {
                    return createJsonResponse(false, "A valid userId is required.", null, null);
                }
                //
                // -- load the person record
                var person = DbBaseModel.create<PersonModel>(cp, userId);
                if (person == null) {
                    return createJsonResponse(false, $"User #{userId} not found.", null, null);
                }
                //
                // -- generate a 40-character random key and store it on the user record
                string randomKey = cp.Utils.GetRandomString(40);
                person.bearerToken = randomKey;
                person.save(cp);
                //
                // -- build and encrypt the token: "{randomKey}|{expiry}"
                DateTime expiry = core.dateTimeNowMockable.AddYears(1);
                string encryptedToken = BearerTokenAuthController.buildEncryptedToken(core, randomKey, expiry);
                //
                return createJsonResponse(true, "Bearer token created.", encryptedToken, expiry.ToString("yyyy-MM-dd"));
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return createJsonResponse(false, $"Error: {ex.Message}", null, null);
            }
        }
        //
        private static string createJsonResponse(bool success, string message, string token, string expires) {
            string tokenJson = token != null ? $"\"{escapeJson(token)}\"" : "null";
            string expiresJson = expires != null ? $"\"{escapeJson(expires)}\"" : "null";
            return $"{{\"success\":{success.ToString().ToLower()},\"message\":\"{escapeJson(message)}\",\"token\":{tokenJson},\"expires\":{expiresJson}}}";
        }
        //
        private static string escapeJson(string value) {
            if (string.IsNullOrEmpty(value)) { return ""; }
            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }
    }
}
