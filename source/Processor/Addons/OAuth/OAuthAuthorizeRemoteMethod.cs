
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using System;
using System.Net;
//
namespace Contensive.Processor.Addons.OAuth {
    //
    /// <summary>
    /// Handles the OAuth 2.0 authorization endpoint (/oauth/authorize).
    /// Supports the Authorization Code flow with PKCE (S256 only).
    ///
    /// GET:  shows a login form when not authenticated.
    /// POST: processes credentials; on success issues a code and redirects to redirect_uri.
    /// </summary>
    public class OAuthAuthorizeRemoteMethod : AddonBaseClass {
        //
        public override object Execute(CPBaseClass cp) {
            try {
                //
                // -- read OAuth params from query string or POST body
                string responseType = cp.Doc.GetText("response_type");
                string clientId = cp.Doc.GetText("client_id");
                string redirectUri = cp.Doc.GetText("redirect_uri");
                string codeChallenge = cp.Doc.GetText("code_challenge");
                string codeChallengeMethod = cp.Doc.GetText("code_challenge_method");
                string state = cp.Doc.GetText("state");
                //
                // -- validate required OAuth params
                if (string.IsNullOrWhiteSpace(responseType) || !responseType.Equals("code", StringComparison.OrdinalIgnoreCase)) {
                    return buildErrorPage("Invalid request: response_type must be 'code'.");
                }
                if (string.IsNullOrWhiteSpace(redirectUri)) {
                    return buildErrorPage("Invalid request: redirect_uri is required.");
                }
                if (string.IsNullOrWhiteSpace(codeChallenge)) {
                    return buildErrorPage("Invalid request: code_challenge is required (PKCE S256).");
                }
                if (!string.Equals(codeChallengeMethod, "S256", StringComparison.OrdinalIgnoreCase)) {
                    return redirectWithError(cp, redirectUri, state, "invalid_request", "Only S256 code_challenge_method is supported.");
                }
                //
                // -- if this is a POST, try to log in with the submitted credentials
                bool loginAttempted = false;
                bool loginSucceeded = false;
                string loginError = "";
                if (cp.Doc.IsProperty("username")) {
                    loginAttempted = true;
                    string username = cp.Doc.GetText("username");
                    string password = cp.Doc.GetText("password");
                    bool passwordProvided = cp.Doc.IsProperty("password");
                    LoginWorkflowController.processLogin(((CPClass)cp).core, username, password, passwordProvided, ref loginError);
                    loginSucceeded = cp.User.IsAuthenticated;
                }
                //
                // -- if user is authenticated (either already or after login), issue the code
                if (cp.User.IsAuthenticated) {
                    return issueAuthCode(cp, clientId, redirectUri, codeChallenge, codeChallengeMethod, state);
                }
                //
                // -- show login form
                string errorHtml = loginAttempted && !loginSucceeded
                    ? $"<div class=\"error\">{WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(loginError) ? "Invalid username or password." : loginError)}</div>"
                    : "";
                //
                string encodedResponseType = WebUtility.HtmlEncode(responseType);
                string encodedClientId = WebUtility.HtmlEncode(clientId ?? "");
                string encodedRedirectUri = WebUtility.HtmlEncode(redirectUri);
                string encodedCodeChallenge = WebUtility.HtmlEncode(codeChallenge);
                string encodedCodeChallengeMethod = WebUtility.HtmlEncode(codeChallengeMethod ?? "S256");
                string encodedState = WebUtility.HtmlEncode(state ?? "");
                string siteName = cp.Site.Name;
                //
                cp.Response.SetType("text/html");
                return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""utf-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1"" />
  <title>Sign In — {WebUtility.HtmlEncode(siteName)}</title>
  <style>
    *, *::before, *::after {{ box-sizing: border-box; }}
    body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; background: #f0f2f5; display: flex; align-items: center; justify-content: center; min-height: 100vh; margin: 0; }}
    .card {{ background: #fff; border-radius: 8px; box-shadow: 0 2px 12px rgba(0,0,0,.12); padding: 2rem; width: 100%; max-width: 380px; }}
    h1 {{ font-size: 1.5rem; margin: 0 0 1.5rem; color: #1a1a1a; }}
    .site {{ font-size: .875rem; color: #555; margin: 0 0 1.5rem; }}
    label {{ display: block; font-size: .875rem; font-weight: 500; margin-bottom: .3rem; color: #333; }}
    input[type=text], input[type=password] {{ display: block; width: 100%; padding: .5rem .75rem; border: 1px solid #ccc; border-radius: 4px; font-size: 1rem; margin-bottom: 1rem; outline: none; }}
    input[type=text]:focus, input[type=password]:focus {{ border-color: #0066cc; box-shadow: 0 0 0 2px rgba(0,102,204,.2); }}
    button {{ display: block; width: 100%; padding: .625rem; background: #0066cc; color: #fff; border: none; border-radius: 4px; font-size: 1rem; font-weight: 500; cursor: pointer; }}
    button:hover {{ background: #0052a3; }}
    .error {{ background: #fff0f0; border: 1px solid #ffcccc; border-radius: 4px; color: #c00; padding: .5rem .75rem; margin-bottom: 1rem; font-size: .875rem; }}
  </style>
</head>
<body>
  <div class=""card"">
    <p class=""site"">Sign in to {WebUtility.HtmlEncode(siteName)}</p>
    <h1>Sign In</h1>
    {errorHtml}
    <form method=""post"" action=""/oauth/authorize"">
      <input type=""hidden"" name=""response_type"" value=""{encodedResponseType}"" />
      <input type=""hidden"" name=""client_id"" value=""{encodedClientId}"" />
      <input type=""hidden"" name=""redirect_uri"" value=""{encodedRedirectUri}"" />
      <input type=""hidden"" name=""code_challenge"" value=""{encodedCodeChallenge}"" />
      <input type=""hidden"" name=""code_challenge_method"" value=""{encodedCodeChallengeMethod}"" />
      <input type=""hidden"" name=""state"" value=""{encodedState}"" />
      <label for=""username"">Username</label>
      <input type=""text"" id=""username"" name=""username"" autocomplete=""username"" autofocus />
      <label for=""password"">Password</label>
      <input type=""password"" id=""password"" name=""password"" autocomplete=""current-password"" />
      <button type=""submit"">Sign In</button>
    </form>
  </div>
</body>
</html>";
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return buildErrorPage($"An unexpected error occurred: {ex.Message}");
            }
        }
        //
        private static string issueAuthCode(CPBaseClass cp, string clientId, string redirectUri, string codeChallenge, string codeChallengeMethod, string state) {
            string code = cp.Utils.GetRandomString(32) + Guid.NewGuid().ToString("N");
            var record = DbBaseModel.addEmpty<OAuthCodeModel>(cp);
            record.name = $"OAuth code for user {cp.User.Id}";
            record.code = code;
            record.userId = cp.User.Id;
            record.codeChallenge = codeChallenge;
            record.codeChallengeMethod = codeChallengeMethod ?? "S256";
            record.redirectUri = redirectUri;
            record.clientId = clientId ?? "";
            record.expiresAt = DateTime.UtcNow.AddMinutes(5);
            record.save(cp);
            //
            string separator = redirectUri.Contains("?") ? "&" : "?";
            string location = $"{redirectUri}{separator}code={Uri.EscapeDataString(code)}";
            if (!string.IsNullOrWhiteSpace(state)) {
                location += $"&state={Uri.EscapeDataString(state)}";
            }
            cp.Response.Redirect(location);
            return "";
        }
        //
        private static string redirectWithError(CPBaseClass cp, string redirectUri, string state, string error, string description) {
            string separator = redirectUri.Contains("?") ? "&" : "?";
            string location = $"{redirectUri}{separator}error={Uri.EscapeDataString(error)}&error_description={Uri.EscapeDataString(description)}";
            if (!string.IsNullOrWhiteSpace(state)) {
                location += $"&state={Uri.EscapeDataString(state)}";
            }
            cp.Response.Redirect(location);
            return "";
        }
        //
        private static string buildErrorPage(string message) {
            return $@"<!DOCTYPE html><html><head><title>Authorization Error</title></head>
<body><h2>Authorization Error</h2><p>{WebUtility.HtmlEncode(message)}</p></body></html>";
        }
    }
}
