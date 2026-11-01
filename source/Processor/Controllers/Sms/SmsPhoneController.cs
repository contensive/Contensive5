
using System.Text;
using System.Text.RegularExpressions;

namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// Phone number normalization to E.164 for text messaging. Pure static, no dependencies.
    /// No libphonenumber. This does not validate that the number is a mobile number. Carrier failures come back as Failed events.
    /// </summary>
    public static class SmsPhoneController {
        //
        private static readonly Regex e164Pattern = new Regex(@"^\+[1-9]\d{7,14}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        //
        //====================================================================================================
        /// <summary>
        /// Convert a phone number as typed (any format) to E.164, for example +17033039974. Returns empty string if the number cannot be converted.
        /// Rules, in order:
        /// 1. trim, empty returns empty
        /// 2. keep the digits, remember if the first non-space character is +
        /// 3. leading + returns +digits
        /// 4. digits that start with 00 return +digits without the 00
        /// 5. default country 1 (NANP): 10 digits returns +1digits, 11 digits starting with 1 returns +digits, anything else returns empty
        /// 6. other country codes: strip one leading 0 (trunk prefix) and return +cc digits
        /// 7. the result must match ^\+[1-9]\d{7,14}$ else returns empty
        /// </summary>
        /// <param name="input">The phone number in any format</param>
        /// <param name="defaultCountryCode">The country calling code to assume when the number has none. Blank is treated as 1.</param>
        /// <returns></returns>
        public static string toE164(string input, string defaultCountryCode) {
            if (string.IsNullOrWhiteSpace(input)) { return ""; }
            string trimmed = input.Trim();
            bool hasPlus = trimmed[0] == '+';
            var digitBuilder = new StringBuilder();
            foreach (char c in trimmed) {
                if (c >= '0' && c <= '9') { digitBuilder.Append(c); }
            }
            string digits = digitBuilder.ToString();
            if (digits.Length == 0) { return ""; }
            string result;
            if (hasPlus) {
                result = $"+{digits}";
            } else if (digits.StartsWith("00")) {
                result = $"+{digits.Substring(2)}";
            } else {
                string countryCode = cleanCountryCode(defaultCountryCode);
                if (countryCode == "1") {
                    //
                    // -- NANP
                    if (digits.Length == 10) {
                        result = $"+1{digits}";
                    } else if (digits.Length == 11 && digits[0] == '1') {
                        result = $"+{digits}";
                    } else {
                        return "";
                    }
                } else {
                    //
                    // -- other country, strip one leading trunk-prefix 0
                    string national = digits.StartsWith("0") ? digits.Substring(1) : digits;
                    result = $"+{countryCode}{national}";
                }
            }
            return e164Pattern.IsMatch(result) ? result : "";
        }
        //
        //====================================================================================================
        /// <summary>
        /// true if the string is already a valid E.164 number
        /// </summary>
        /// <param name="e164"></param>
        /// <returns></returns>
        public static bool isE164(string e164) {
            return !string.IsNullOrEmpty(e164) && e164Pattern.IsMatch(e164);
        }
        //
        //====================================================================================================
        /// <summary>
        /// reduce the country code setting to digits only. blank returns 1
        /// </summary>
        private static string cleanCountryCode(string countryCode) {
            var digitBuilder = new StringBuilder();
            if (!string.IsNullOrEmpty(countryCode)) {
                foreach (char c in countryCode) {
                    if (c >= '0' && c <= '9') { digitBuilder.Append(c); }
                }
            }
            return digitBuilder.Length == 0 ? "1" : digitBuilder.ToString();
        }
    }
}
