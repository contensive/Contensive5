
using System.Collections.Generic;

namespace Contensive.Models.Db {
    //
    public class TextMessageQueueModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("text message queue", "cctextmessagequeue", "default", false);
        //
        //====================================================================================================
        public string toPhone { get; set; }
        public string content { get; set; }
        public bool immediate { get; set;  }
        public int attempts { get; set; }
        /// <summary>
        /// assigned during send. The sending process sets this then selects it back to gaurantee the same text isnt sent twice.
        /// the dateModified is also set and the serial number expires after 1 minute
        /// </summary>
        public string sendSerialNumber { get; set; }
    }
}
