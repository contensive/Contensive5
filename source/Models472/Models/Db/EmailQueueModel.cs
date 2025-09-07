
using Contensive.BaseClasses;
using System;
using System.Collections.Generic;
using System.Data;

namespace Contensive.Models.Db {
    //
    public class EmailQueueModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("email queue", "ccemailqueue", "default", false);
        //
        //====================================================================================================
        public string toAddress { get; set; }
        public string subject { get; set; }
        public string content { get; set; }
        public bool immediate { get; set;  }
        public int attempts { get; set; }
        /// <summary>
        /// The sending process clears all expired keys.
        /// The process then creates an process expiration date. It can send records until that expiration.
        /// The sending process sets its key, and a key expiration date in records with blank keys.
        /// Then while the process has not expired, it selects back the selected record based on that key and the expiration date and sends the record. 
        /// </summary>
        public string sendingProcessKey { get; set; }
        public DateTime? sendingProcessExpiration { get; set; }
        //
        /// <summary>
        /// mark as set of record that were unmarked with this process key and process expiration
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="sendingProcessKey"></param>
        /// <param name="sendingProcessExpiration"></param>
        public static void setProcessKey(CPBaseClass cp, string sendingProcessKey, DateTime sendingProcessExpiration, int recordsToMark) {
            try {
                cp.Db.ExecuteNonQuery($"update top ({recordsToMark}) ccEmailQueue set sendingProcessKey={cp.Db.EncodeSQLText(sendingProcessKey)},sendingProcessExpiration={cp.Db.EncodeSQLDate(sendingProcessExpiration)} where sendingProcessKey is null and sendingProcessExpiration is null");
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
        //
        /// <summary>
        /// clear the process key and process expiration for records with expired keys
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="sendingProcessExpiration"></param>
        public static void clearExpiredKeys(CPBaseClass cp, DateTime sendingProcessExpiration) {
            try {
                cp.Db.ExecuteNonQuery($"update ccEmailQueue set sendingProcessKey=null,sendingProcessExpiration=null where sendingProcessExpiration<{cp.Db.EncodeSQLDate(sendingProcessExpiration)}");
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
        //
        /// <summary>
        /// select a set of records for this sending process that have not expired
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="sendingProcessKey"></param>
        /// <param name="sendingProcessExpiration"></param>
        /// <param name="recordsToSelect"></param>
        /// <returns></returns>
        public static List<EmailQueueModel> selectRecordsToSend(CPBaseClass cp, string sendingProcessKey, int recordsToSelect) {
            try {
                return createList<EmailQueueModel>(cp, $"(sendingProcessKey={cp.Db.EncodeSQLText(sendingProcessKey)})and(sendingProcessExpiration>{cp.Db.EncodeSQLDate(DateTime.Now)})", "immediate,id asc", recordsToSelect, 1);
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
    }
}
