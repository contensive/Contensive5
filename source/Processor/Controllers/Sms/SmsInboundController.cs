using System;
using System.Collections.Generic;
using System.Data;

namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// Match an inbound text message to people by phone number.
    /// </summary>
    public static class SmsInboundController {
        //
        /// <summary>the most people returned (exact matches)</summary>
        public const int maxMatches = 50;
        /// <summary>safety bound on the rows read by the last-4-digit narrowing query</summary>
        public const int maxCandidates = 5000;
        //
        //====================================================================================================
        /// <summary>
        /// Ids of active people whose cell phone or phone normalizes to exactly this E.164 number, newest (highest id) first. Max 50 exact matches.
        /// Narrowed in SQL by the last 4 digits (parameterized), confirmed in code with SmsPhoneController.toE164. The 50 cap applies after the exact match,
        /// so a correct older person is not dropped because many people share the last 4 digits. The SQL candidates are bounded at maxCandidates (a safety limit, far above
        /// the expected count: about 1 in 10,000 people share the last 4 digits).
        /// </summary>
        /// <param name="core"></param>
        /// <param name="e164"></param>
        /// <returns></returns>
        public static List<int> getPeopleIdsByPhone(CoreController core, string e164) {
            var result = new List<int>();
            try {
                if (string.IsNullOrEmpty(e164)) { return result; }
                string digits = e164.Replace("+", "");
                if (digits.Length < 4) { return result; }
                string last4 = digits.Substring(digits.Length - 4);
                string defaultCountryCode = TextMessageController.getDefaultCountryCode(core);
                string sql = $"select top {maxCandidates} id, cellPhone, phone from ccMembers where (active<>0) and ((cellPhone like @p) or (phone like @p)) order by id desc";
                using (DataTable dt = core.db.executeQuery(sql, new Dictionary<string, object> { { "@p", $"%{last4}" } })) {
                    if (dt?.Rows == null) { return result; }
                    if (dt.Rows.Count >= maxCandidates) { logger.Warn($"{core.logCommonMessage},getPeopleIdsByPhone, candidate limit [{maxCandidates}] reached, older matches may be missed"); }
                    foreach (DataRow row in dt.Rows) {
                        if (result.Count >= maxMatches) { break; }
                        int id = GenericController.getInteger(row["id"]);
                        if (id <= 0 || result.Contains(id)) { continue; }
                        string cellPhone = SmsPhoneController.toE164(GenericController.getText(row["cellPhone"]), defaultCountryCode);
                        string phone = SmsPhoneController.toE164(GenericController.getText(row["phone"]), defaultCountryCode);
                        if (e164.Equals(cellPhone, StringComparison.Ordinal) || e164.Equals(phone, StringComparison.Ordinal)) {
                            result.Add(id);
                        }
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage},getPeopleIdsByPhone");
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
