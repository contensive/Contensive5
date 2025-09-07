
using System;

namespace Contensive.Models.Db {
    //
    public class ContentWatchModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Content Watch", "ccContentWatch", "default", false);
        //
        //====================================================================================================
        /// <summary>
        /// field properties
        /// </summary>
        public bool allowWhatsNew { get; set; }
        public int clicks { get; set; }
        public int contentid { get; set; }
        public string contentRecordKey { get; set; }
        public string link { get; set; }
        public string linkLabel { get; set; }
        public int recordid { get; set; }
        public DateTime whatsNewDateExpires { get; set; }

    }
}
