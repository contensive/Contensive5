
using Contensive.Processor.Controllers;
using System;

namespace Contensive.Processor {
    public class CPSecurityClass : BaseClasses.CPSecurityBaseClass {
        //
        //
        private readonly Controllers.CoreController core;
        //
        // ====================================================================================================
        //
        public CPSecurityClass(Controllers.CoreController core) {
            this.core = core;
        }
        //
        // ====================================================================================================
        //
        public override string DecryptTwoWay(string encryptedString) {
            return SecurityController.decryptTwoWay(core, encryptedString);
        }
        //
        // ====================================================================================================
        //
        public override string EncryptOneWay(string unencryptedString) {
            return SecurityController.encryptOneWay(core, unencryptedString, "");
        }
        //
        // ====================================================================================================
        //
        public override string EncryptOneWay(string unencryptedString, string salt) {
            return SecurityController.encryptOneWay(core, unencryptedString, salt);
        }
        //
        // ====================================================================================================
        //
        public override string EncryptTwoWay(string unencryptedString) {
            return SecurityController.encryptTwoWay(core, unencryptedString);
        }
        //
        // ====================================================================================================
        //
        public override string GetRandomPassword() {
            throw new NotImplementedException();
        }
        //
        // ====================================================================================================
        //
        public override bool VerifyOneWay(string unencryptedString, string encryptedString) {
            return SecurityController.verifyOneWay(core, unencryptedString, encryptedString);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Check if the honeypot field was filled (indicates bot). Returns true if spam detected.
        /// </summary>
        public override bool CheckHoneypot(string honeypotFieldName = "website_url") {
            try {
                string honeypotValue = core.docProperties.getText(honeypotFieldName);
                if (string.IsNullOrEmpty(honeypotValue)) {
                    return false;
                }

                // Log the attempt
                LogController.log(core, $"Security.CheckHoneypot, honeypot field [{honeypotFieldName}] was filled, rejecting as spam", BaseClasses.CPLogBaseClass.LogLevel.Warn);

                // Set visit property for abuse detection integration
                core.visitProperty.setProperty("SpamDetected_Honeypot", true);

                return true;
            } catch (Exception ex) {
                LogController.log(core, $"Security.CheckHoneypot, exception [{ex.Message}]", BaseClasses.CPLogBaseClass.LogLevel.Error);
                return false;
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Check if the visitor has submitted too recently. Returns true if rate limit exceeded.
        /// </summary>
        public override bool CheckRateLimit(string actionKey, int cooldownSeconds = 15) {
            try {
                if (string.IsNullOrWhiteSpace(actionKey)) {
                    return false;
                }

                string rateLimitKey = $"rateLimit_{actionKey}";
                string lastActionStr = core.visitProperty.getText(rateLimitKey);

                if (string.IsNullOrEmpty(lastActionStr)) {
                    return false;
                }

                if (!DateTime.TryParse(lastActionStr, out DateTime lastAction)) {
                    return false;
                }

                double secondsSinceLastAction = (DateTime.Now - lastAction).TotalSeconds;
                if (secondsSinceLastAction < cooldownSeconds) {
                    LogController.log(core, $"Security.CheckRateLimit, action [{actionKey}] rate limited, {secondsSinceLastAction:F1}s since last action (cooldown: {cooldownSeconds}s)", BaseClasses.CPLogBaseClass.LogLevel.Warn);
                    core.visitProperty.setProperty("RateLimitViolation", true);
                    return true;
                }

                return false;
            } catch (Exception ex) {
                LogController.log(core, $"Security.CheckRateLimit, exception [{ex.Message}]", BaseClasses.CPLogBaseClass.LogLevel.Error);
                return false;
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Record that the visitor successfully completed an action, starting the rate limit cooldown.
        /// </summary>
        public override void RecordAction(string actionKey) {
            try {
                if (string.IsNullOrWhiteSpace(actionKey)) {
                    return;
                }

                string rateLimitKey = $"rateLimit_{actionKey}";
                core.visitProperty.setProperty(rateLimitKey, DateTime.Now.ToString("o"));
            } catch (Exception ex) {
                LogController.log(core, $"Security.RecordAction, exception [{ex.Message}]", BaseClasses.CPLogBaseClass.LogLevel.Error);
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Generate HTML for a honeypot field.
        /// </summary>
        public override string GetHoneypotHtml(string fieldName = "website_url") {
            try {
                return $"<div style=\"position:absolute;left:-9999px;\" aria-hidden=\"true\"><input type=\"text\" name=\"{HtmlController.encodeHtml(fieldName)}\" tabindex=\"-1\" autocomplete=\"off\" value=\"\"></div>";
            } catch (Exception ex) {
                LogController.log(core, $"Security.GetHoneypotHtml, exception [{ex.Message}]", BaseClasses.CPLogBaseClass.LogLevel.Error);
                return string.Empty;
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Sanitize user input for safe display or storage.
        /// </summary>
        public override string SanitizeInput(string userInput, string mode = "display") {
            try {
                if (string.IsNullOrEmpty(userInput)) {
                    return userInput;
                }

                switch (mode.ToLowerInvariant()) {
                    case "display":
                        return HtmlController.encodeHtml(userInput);
                    case "url":
                        return GenericController.encodeURL(userInput);
                    case "sql":
                        // Strip common SQL keywords (still use ORM for queries!)
                        string cleaned = userInput;
                        string[] sqlKeywords = { "union", "select", "drop", "delete", "update", "insert", "exec", "execute", "script", "--", "/*", "*/" };
                        foreach (string keyword in sqlKeywords) {
                            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, keyword, string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        }
                        return cleaned;
                    default:
                        return HtmlController.encodeHtml(userInput);
                }
            } catch (Exception ex) {
                LogController.log(core, $"Security.SanitizeInput, exception [{ex.Message}]", BaseClasses.CPLogBaseClass.LogLevel.Error);
                return string.Empty;
            }
        }
    }
}