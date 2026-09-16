
namespace Contensive.BaseClasses {
    public abstract class CPSecurityBaseClass {
        //
        //==========================================================================================
        /// <summary>
        /// Generate and return a random password string.
        /// </summary>
        public abstract string GetRandomPassword();
        //
        //==========================================================================================
        /// <summary>
        /// return an encrypted string. This is a hash.
        /// One-way encryption cannot be reversed. 
        /// This encrypted string is repeatable, so run the same encryption twice and the encryption will match
        /// </summary>
        /// <param name="unencryptedString"></param>
        /// <returns></returns>
        public abstract string EncryptOneWay(string unencryptedString);
        //
        //==========================================================================================
        /// <summary>
        /// return an encrypted string. This is a hash.
        /// One-way encryption cannot be reversed. 
        /// This encrypted string is repeatable, so run the same encryption twice and the encryption will match
        /// </summary>
        /// <param name="unencryptedString"></param>
        /// <param name="salt"></param>
        /// <returns></returns>
        public abstract string EncryptOneWay(string unencryptedString, string salt);
        //
        //==========================================================================================
        //
        /// <summary>
        /// return true if an encrypted string matches an unencrypted string.
        /// </summary>
        /// <param name="unencryptedString"></param>
        /// <param name="encryptedString"></param>
        /// <returns></returns>
        public abstract bool VerifyOneWay(string unencryptedString, string encryptedString);
        //
        //==========================================================================================
        /// <summary>
        /// Return an AES encrypted string. This is a symetric encryption.
        /// A value encrypted, can be decrypted back to the same string
        /// The result is not repeatable. If you encrypt the same source twice, the result may not be the same each time.
        /// </summary>
        /// <param name="unencryptedString"></param>
        /// <returns></returns>
        public abstract string EncryptTwoWay(string unencryptedString);
        //
        //==========================================================================================
        /// <summary>
        /// Return an AES decrypted string. This is a symetric encryption.
        /// A value encrypted, can be decrypted back to the same string
        /// The result is not repeatable. If you encrypt the same source twice, the result may not be the same each time.
        /// </summary>
        /// <param name="encryptedString"></param>
        /// <returns></returns>
        public abstract string DecryptTwoWay(string encryptedString);
        //
        //==========================================================================================
        /// <summary>
        /// Check if the honeypot field was filled (indicates bot). Returns true if spam detected.
        /// If spam is detected, the request is automatically logged and the visitor is flagged.
        /// The honeypot field name defaults to "website_url" but can be customized.
        /// </summary>
        /// <param name="honeypotFieldName">Optional custom honeypot field name (default: "website_url")</param>
        /// <returns>True if spam detected (honeypot was filled), false if legitimate user</returns>
        public abstract bool CheckHoneypot(string honeypotFieldName = "website_url");
        //
        //==========================================================================================
        /// <summary>
        /// Check if the visitor has submitted too recently. Returns true if rate limit exceeded.
        /// Uses visit-level storage tied to the specified action key.
        /// Automatically logs and flags the visitor if limit exceeded.
        /// </summary>
        /// <param name="actionKey">Unique key for this action (e.g., "contactFormSubmit", "commentPost")</param>
        /// <param name="cooldownSeconds">Seconds required between submissions (default: 15)</param>
        /// <returns>True if rate limit exceeded, false if OK to proceed</returns>
        public abstract bool CheckRateLimit(string actionKey, int cooldownSeconds = 15);
        //
        //==========================================================================================
        /// <summary>
        /// Record that the visitor successfully completed an action, starting the rate limit cooldown.
        /// Call this AFTER successful form processing.
        /// </summary>
        /// <param name="actionKey">Same key used in CheckRateLimit()</param>
        public abstract void RecordAction(string actionKey);
        //
        //==========================================================================================
        /// <summary>
        /// Generate HTML for a honeypot field. Insert this in your form template.
        /// The field is positioned off-screen and hidden from screen readers.
        /// </summary>
        /// <param name="fieldName">Field name (default: "website_url")</param>
        /// <returns>HTML string ready to insert in form</returns>
        public abstract string GetHoneypotHtml(string fieldName = "website_url");
        //
        //==========================================================================================
        /// <summary>
        /// Sanitize user input for safe display. Removes or encodes dangerous patterns.
        /// Use this when you need to display user-submitted content.
        /// </summary>
        /// <param name="userInput">Raw user input</param>
        /// <param name="mode">Sanitization mode: "display" (HTML encode), "url" (URL encode), "sql" (strip SQL keywords - still use ORM!)</param>
        /// <returns>Sanitized string safe for the specified context</returns>
        public abstract string SanitizeInput(string userInput, string mode = "display");
    }
}

