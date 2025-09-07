
using System;

namespace Contensive.Models.Db {
    //
    public class EmailLogModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("email log", "ccemaillog", "default", false);
        //
        //====================================================================================================
        public DateTime? dateBlockExpires { get; set; }
        public int emailDropId { get; set; }
        public int emailId { get; set; }
        public string fromAddress { get; set; }
        public int logType { get; set; }
        public int memberId { get; set; }
        public string sendStatus { get; set; }
        public string subject { get; set; }
        public string toAddress { get; set; }
        public int visitId { get; set; }
        public string body { get; set; }
    }
}
