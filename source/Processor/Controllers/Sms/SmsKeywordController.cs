using System;
using System.Collections.Generic;

namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// Classify an inbound text as an opt-out, opt-in or help keyword. Pure static.
    /// The whole message must be the keyword (after trim, upper case and stripping trailing . and !), so "Stop please" is not an opt-out.
    /// The site never replies to keywords. With self-managed opt-outs off, AWS sends the STOP and HELP replies.
    /// </summary>
    public static class SmsKeywordController {
        //
        public enum KeywordEnum {
            None = 0,
            OptOut = 1,
            OptIn = 2,
            Help = 3
        }
        //
        private static readonly HashSet<string> optOutKeywords = new HashSet<string>(StringComparer.Ordinal) { "STOP", "STOPALL", "UNSUBSCRIBE", "CANCEL", "END", "QUIT", "OPTOUT", "OPT-OUT", "REVOKE" };
        private static readonly HashSet<string> optInKeywords = new HashSet<string>(StringComparer.Ordinal) { "START", "UNSTOP" };
        private static readonly HashSet<string> helpKeywords = new HashSet<string>(StringComparer.Ordinal) { "HELP", "INFO" };
        //
        //====================================================================================================
        /// <summary>
        /// trim, upper case, strip trailing . and !
        /// </summary>
        public static string normalize(string messageBody) {
            if (string.IsNullOrEmpty(messageBody)) { return ""; }
            return messageBody.Trim().ToUpperInvariant().TrimEnd('.', '!').Trim();
        }
        //
        //====================================================================================================
        /// <summary>
        /// Classify the message body.
        /// </summary>
        public static KeywordEnum classify(string messageBody) {
            string text = normalize(messageBody);
            if (text.Length == 0) { return KeywordEnum.None; }
            if (optOutKeywords.Contains(text)) { return KeywordEnum.OptOut; }
            if (optInKeywords.Contains(text)) { return KeywordEnum.OptIn; }
            if (helpKeywords.Contains(text)) { return KeywordEnum.Help; }
            return KeywordEnum.None;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Classify the message body, and also treat it as an opt-out if the provider reported an opt-out keyword (AWS inbound messageKeyword, semantics: verify).
        /// An opt-out always wins, because texting someone who said STOP is the worse mistake.
        /// </summary>
        public static KeywordEnum classify(string messageBody, string providerKeyword) {
            if (classify(providerKeyword) == KeywordEnum.OptOut) { return KeywordEnum.OptOut; }
            return classify(messageBody);
        }
        //
        //====================================================================================================
        /// <summary>
        /// The smsKeyword event argument for a received message: START, HELP or empty.
        /// </summary>
        public static string getEventKeyword(KeywordEnum keyword) {
            switch (keyword) {
                case KeywordEnum.OptIn: return "START";
                case KeywordEnum.Help: return "HELP";
                default: return "";
            }
        }
    }
}
