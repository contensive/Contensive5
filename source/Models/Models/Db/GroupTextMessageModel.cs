
using Contensive.BaseClasses;
using System;
using System.Collections.Generic;

namespace Contensive.Models.Db {
    /// <summary>
    /// 
    /// </summary>
    public class GroupTextMessageModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("group text messages", "ccGroupTextMessages", "default", true);
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public string body { get; set; }
        /// <summary>
        /// The person who tests it, and receives the confirmation after a send
        /// </summary>
        public int testMemberID { get; set; }
        /// <summary>
        /// /
        /// </summary>
        public bool submitted { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool sent { get; set; }
        /// <summary>
        /// The date and time of the last test. Used to detect if the message has been tested
        /// </summary>
        public DateTime? lastSendTestDate { get; set; }
        //
        /// <summary>
        /// Set a text message submitted
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="recordId"></param>
        public static void setSubmitted(CPBaseClass cp, int recordId) {
            try {
                cp.Db.ExecuteQuery("update ccGroupTextMessages set submitted=1 where id=" + recordId);
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
        /// <summary>
        /// Return a list of group text messages that need to be sent
        /// </summary>
        public static List<GroupTextMessageModel> getGroupTextMessageList(CPBaseClass cp) {
            return createList<GroupTextMessageModel>(cp, "");

        }
    }
}
