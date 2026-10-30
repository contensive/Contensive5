
using Contensive.BaseClasses;
using System;

namespace Contensive.Models.Db {
    /// <summary>
    /// Phone numbers (E.164) that this site must never text. Replaces the legacy private file Config\TextMessageBlockList.txt.
    /// To unblock a number, delete its row.
    /// </summary>
    public class TextMessageBlockListModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Text Message Block List", "ccTextMessageBlockList", "default", false);
        //
        //====================================================================================================
        /// <summary>
        /// values for blockSourceId. Positional in the Source lookup list, never reorder.
        /// </summary>
        public enum BlockSourceEnum {
            /// <summary>The contact replied STOP or another opt-out keyword</summary>
            KeywordStop = 1,
            /// <summary>The provider rejected a send because the number opted out</summary>
            ProviderOptOut = 2,
            /// <summary>Added by an administrator</summary>
            Admin = 3,
            /// <summary>Migrated from the legacy private file</summary>
            Migrated = 4
        }
        //
        //====================================================================================================
        /// <summary>
        /// The blocked phone number, E.164.
        /// </summary>
        public string phone { get; set; }
        /// <summary>
        /// The date the number was blocked.
        /// </summary>
        public DateTime? dateBlocked { get; set; }
        /// <summary>
        /// How the number came to be blocked, see BlockSourceEnum.
        /// </summary>
        public int blockSourceId { get; set; }
        /// <summary>
        /// The best-match person at the time of the block. 0 if unknown.
        /// </summary>
        public int memberId { get; set; }
        /// <summary>
        /// Notes, for example the inbound text that caused the block.
        /// </summary>
        public string notes { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// True if the E.164 phone number is on the block list. A blank phone is never blocked.
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="e164"></param>
        /// <returns></returns>
        public static bool isBlocked(CPBaseClass cp, string e164) {
            if (string.IsNullOrWhiteSpace(e164)) { return false; }
            return getCount<TextMessageBlockListModel>(cp, $"(phone={cp.Db.EncodeSQLText(e164)})") > 0;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Add a number to the block list. A no-op if the number is already on the list or blank.
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="e164"></param>
        /// <param name="sourceId">see BlockSourceEnum</param>
        /// <param name="memberId"></param>
        /// <param name="notes"></param>
        /// <returns>true if a row was added</returns>
        public static bool add(CPBaseClass cp, string e164, int sourceId, int memberId, string notes) 
            => add(cp, e164, sourceId, memberId, notes, DateTime.Now);
        //
        //====================================================================================================
        /// <summary>
        /// Add a number to the block list with a specific block date (used by the migration). A no-op if the number is already on the list or blank.
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="e164"></param>
        /// <param name="sourceId">see BlockSourceEnum</param>
        /// <param name="memberId"></param>
        /// <param name="notes"></param>
        /// <param name="dateBlocked"></param>
        /// <returns>true if a row was added</returns>
        public static bool add(CPBaseClass cp, string e164, int sourceId, int memberId, string notes, DateTime dateBlocked) {
            if (string.IsNullOrWhiteSpace(e164)) { return false; }
            if (isBlocked(cp, e164)) { return false; }
            var row = addDefault<TextMessageBlockListModel>(cp);
            row.name = e164;
            row.phone = e164;
            row.dateBlocked = dateBlocked;
            row.blockSourceId = sourceId;
            row.memberId = memberId;
            row.notes = notes ?? "";
            row.save(cp);
            return true;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Remove a number from the block list. Deletes all rows for the number.
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="e164"></param>
        public static void remove(CPBaseClass cp, string e164) {
            if (string.IsNullOrWhiteSpace(e164)) { return; }
            deleteRows<TextMessageBlockListModel>(cp, $"(phone={cp.Db.EncodeSQLText(e164)})");
        }
    }
}
