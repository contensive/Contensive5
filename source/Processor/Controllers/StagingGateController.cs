
using System;

namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// Staging gate: blocks anonymous access to the site when a passphrase is configured.
    /// Visitors must enter the passphrase to view any page. Authenticated users and the
    /// admin route are always allowed through. The passphrase is set in Site Settings > Website.
    /// </summary>
    public static class StagingGateController {
        //
        //====================================================================================================
        /// <summary>
        /// Check if the staging gate should block this request.
        /// Returns empty string to allow the request through, or returns HTML to block it.
        /// </summary>
        public static string processGate(CoreController core, string normalizedRoute) {
            try {
                //
                // -- check if gate is active (passphrase is set)
                string passphrase = core.siteProperties.stagingGatePassphrase;
                if (string.IsNullOrWhiteSpace(passphrase)) { return string.Empty; }
                //
                // -- bypass admin route so admins can log in
                string adminRoute = RouteController.normalizeRoute(core.appConfig.adminRoute);
                if (!string.IsNullOrEmpty(adminRoute) && normalizedRoute.StartsWith(adminRoute, StringComparison.OrdinalIgnoreCase)) {
                    return string.Empty;
                }
                //
                // -- bypass authenticated users
                if (core.session.isAuthenticated) { return string.Empty; }
                //
                // -- bypass robots.txt and favicon.ico
                if (normalizedRoute.Equals("robots.txt", StringComparison.OrdinalIgnoreCase)
                    || normalizedRoute.Equals("favicon.ico", StringComparison.OrdinalIgnoreCase)) {
                    return string.Empty;
                }
                //
                // -- check staging gate cookie
                string cookieName = $"{core.session.cookiePrefix}stagingGate";
                string cookieValue = core.webServer.requestCookie(cookieName);
                if (!string.IsNullOrEmpty(cookieValue)) {
                    try {
                        string decrypted = SecurityController.decryptTwoWay(core, cookieValue);
                        if (!string.IsNullOrEmpty(decrypted) && decrypted.Equals($"stagingGate|{passphrase}", StringComparison.OrdinalIgnoreCase)) {
                            return string.Empty;
                        }
                    } catch (Exception) {
                        // -- cookie invalid or tampered, fall through to show form
                    }
                }
                //
                // -- check form POST for passphrase submission
                string submittedPassphrase = core.docProperties.getText("stagingGatePassphrase");
                if (!string.IsNullOrEmpty(submittedPassphrase)) {
                    if (submittedPassphrase.Equals(passphrase, StringComparison.OrdinalIgnoreCase)) {
                        //
                        // -- correct passphrase, set cookie and allow through
                        string encrypted = SecurityController.encryptTwoWay(core, $"stagingGate|{passphrase}");
                        core.webServer.addResponseCookie(cookieName, encrypted, core.dateTimeNowMockable.AddDays(30));
                        return string.Empty;
                    }
                    //
                    // -- wrong passphrase
                    return getStagingGateForm("Incorrect passphrase. Please try again.");
                }
                //
                // -- no cookie, no submission — show form
                return getStagingGateForm("");
            } catch (Exception ex) {
                LogController.log(core, $"StagingGateController.processGate exception, {ex.Message}", BaseClasses.CPLogBaseClass.LogLevel.Error);
                return string.Empty;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Returns a self-contained HTML page with a minimal passphrase form.
        /// Standalone HTML with no dependency on the template/layout system.
        /// </summary>
        private static string getStagingGateForm(string errorMessage) {
            string errorHtml = string.IsNullOrEmpty(errorMessage)
                ? ""
                : $"<p style=\"color:#c00;margin-bottom:16px;\">{System.Net.WebUtility.HtmlEncode(errorMessage)}</p>";
            return $@"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1"">
<title>Site Access</title>
<style>
    body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: #f5f5f5; margin: 0; display: flex; align-items: center; justify-content: center; min-height: 100vh; }}
    .gate {{ background: #fff; padding: 40px; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.1); text-align: center; max-width: 360px; width: 100%; }}
    .gate h1 {{ font-size: 20px; margin: 0 0 8px; color: #333; }}
    .gate p.desc {{ font-size: 14px; color: #666; margin: 0 0 24px; }}
    .gate input[type=text] {{ width: 100%; padding: 10px 12px; border: 1px solid #ccc; border-radius: 4px; font-size: 16px; box-sizing: border-box; margin-bottom: 16px; }}
    .gate button {{ width: 100%; padding: 10px; background: #333; color: #fff; border: none; border-radius: 4px; font-size: 16px; cursor: pointer; }}
    .gate button:hover {{ background: #555; }}
</style>
</head>
<body>
<div class=""gate"">
    <h1>Site Access</h1>
    <p class=""desc"">Enter the passphrase to continue.</p>
    {errorHtml}
    <form method=""post"">
        <input type=""text"" name=""stagingGatePassphrase"" placeholder=""Passphrase"" autofocus>
        <button type=""submit"">Continue</button>
    </form>
</div>
</body>
</html>";
        }
    }
}
