
using System;

namespace Contensive.Models.Db {
    /// <summary>
    /// 
    /// </summary>
    public class SystemTextMessageModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("system text messages", "ccSystemTextMessages", "default", true);
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public string body { get; set; }
        /// <summary>
        /// The person who tests it, and receives the confirmation after a send
        /// </summary>
        public int testMemberId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public DateTime? scheduleDate { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public DateTime? expireDate { get; set; }
        /// <summary>
        /// The date and time of the last test. Used to detect if the message has been tested
        /// </summary>
        public DateTime? lastSendTestDate { get; set; }
    }
}
