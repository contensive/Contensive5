
using System;

namespace Contensive.Models.Db {
    //
    public class TextMessageLogModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Text Message log", "ccTextMessagelog", "default", false);
        //
        //====================================================================================================
        public int systemTextMessageId { get; set; }
        public int groupTextMessageId { get; set; }
        public int memberId { get; set; }
        public string sendStatus { get; set; }
        public string toPhone { get; set; }
        public string body { get; set; }
    }
}
