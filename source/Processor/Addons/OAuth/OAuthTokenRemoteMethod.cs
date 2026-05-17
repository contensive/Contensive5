
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using System;
using System.Security.Cryptography;
using System.Text;
//
namespace Contensive.Processor.Addons.OAuth {
    //
    /// <summary>
    /// Handles the OAuth 2.0 token endpoint (POST /oauth/token).
    /// Accepts authorization_code grant with PKCE (S256) and returns a Contensive bearer token.
    /// </summary>
    public class OAuthTokenRemoteMethod : AddonBaseClass {
        //
        public override object Execute(CPBaseClass cp) {
            try {
                cp.Response.SetType("application/json");
                cp.Response.AddHeader("Cache-Control", "no-store");
                //
                string grantType = cp.Doc.GetText("grant_type");
                if (!string.Equals(grantType, "authorization_code", StringComparison.OrdinalIgnoreCase)) {
                    cp.Response.SetStatus(WebServerController.httpResponseStatus.BadRequest);
                    return tokenError("unsupported_grant_type", "Only authorization_code is supported.");
                }
                //
                string code = cp.Doc.GetText("code");
                string redirectUri = cp.Doc.GetText("redirect_uri");
                string codeVerifier = cp.Doc.GetText("code_verifier");
                string clientId = cp.Doc.GetText("client_id");
                //
                if (string.IsNullOrWhiteSpace(code)) {
                    cp.Response.SetStatus(WebServerController.httpResponseStatus.BadRequest);
                    return tokenError("invalid_request", "code is required.");
                }
                if (string.IsNullOrWhiteSpace(codeVerifier)) {
                    cp.Response.SetStatus(WebServerController.httpResponseStatus.BadRequest);
                    return tokenError("invalid_request", "code_verifier is required.");
                }
                //
                // -- look up the authorization code
                var codeRecords = DbBaseModel.createList<OAuthCodeModel>(cp,
                    $"(code={DbController.encodeSQLText(code)})and(active>0)", "id", 1);
                //
                if (codeRecords.Count == 0) {
                    cp.Response.SetStatus(WebServerController.httpResponseStatus.BadRequest);
                    return tokenError("invalid_grant", "Authorization code not found or already used.");
                }
                var codeRecord = codeRecords[0];
                //
                // -- check expiry
                if (codeRecord.expiresAt < DateTime.UtcNow) {
                    DbBaseModel.delete<OAuthCodeModel>(cp, codeRecord.id);
                    cp.Response.SetStatus(WebServerController.httpResponseStatus.BadRequest);
                    return tokenError("invalid_grant", "Authorization code has expired.");
                }
                //
                // -- verify redirect_uri matches
                if (!string.IsNullOrWhiteSpace(redirectUri) && !string.Equals(redirectUri, codeRecord.redirectUri, StringComparison.Ordinal)) {
                    cp.Response.SetStatus(WebServerController.httpResponseStatus.BadRequest);
                    return tokenError("invalid_grant", "redirect_uri mismatch.");
                }
                //
                // -- verify client_id matches (if provided in both places)
                if (!string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(codeRecord.clientId)
                    && !string.Equals(clientId, codeRecord.clientId, StringComparison.Ordinal)) {
                    cp.Response.SetStatus(WebServerController.httpResponseStatus.BadRequest);
                    return tokenError("invalid_client", "client_id mismatch.");
                }
                //
                // -- verify PKCE: BASE64URL(SHA256(code_verifier)) == stored code_challenge
                if (!verifyPkce(codeVerifier, codeRecord.codeChallenge)) {
                    cp.Response.SetStatus(WebServerController.httpResponseStatus.BadRequest);
                    return tokenError("invalid_grant", "PKCE verification failed.");
                }
                //
                // -- single-use: delete the code record immediately
                int userId = codeRecord.userId;
                DbBaseModel.delete<OAuthCodeModel>(cp, codeRecord.id);
                //
                // -- create a Contensive bearer token for this user
                CoreController core = ((CPClass)cp).core;
                string randomKey = cp.Utils.GetRandomString(40);
                var person = DbBaseModel.create<PersonModel>(cp, userId);
                if (person == null) {
                    cp.Response.SetStatus(WebServerController.httpResponseStatus.InternalServerError);
                    return tokenError("server_error", "User not found.");
                }
                person.bearerToken = randomKey;
                person.save(cp);
                //
                DateTime expiry = core.dateTimeNowMockable.AddYears(1);
                string accessToken = BearerTokenAuthController.buildEncryptedToken(core, randomKey, expiry);
                int expiresInSeconds = (int)(expiry - core.dateTimeNowMockable).TotalSeconds;
                //
                return $@"{{""access_token"":""{escapeJson(accessToken)}"",""token_type"":""Bearer"",""expires_in"":{expiresInSeconds}}}";
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                cp.Response.SetStatus(WebServerController.httpResponseStatus.InternalServerError);
                return tokenError("server_error", ex.Message);
            }
        }
        //
        private static bool verifyPkce(string codeVerifier, string storedChallenge) {
            if (string.IsNullOrWhiteSpace(codeVerifier) || string.IsNullOrWhiteSpace(storedChallenge)) {
                return false;
            }
            byte[] verifierBytes = Encoding.ASCII.GetBytes(codeVerifier);
            byte[] hashBytes;
            using (var sha = SHA256.Create()) {
                hashBytes = sha.ComputeHash(verifierBytes);
            }
            string computed = Convert.ToBase64String(hashBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
            return string.Equals(computed, storedChallenge, StringComparison.Ordinal);
        }
        //
        private static string tokenError(string error, string description) {
            return $@"{{""error"":""{escapeJson(error)}"",""error_description"":""{escapeJson(description)}""}}";
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
