
using System;

namespace Contensive.Models.Db {
    //
    public class DownloadModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Downloads", "ccdownloads", "default", false);
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public FieldTypeTextFile filename { get; set; }
        public int requestedBy { get; set; }
        public DateTime? dateRequested { get; set; }
        public DateTime? dateCompleted { get; set; }
        public string resultMessage { get; set; }
    }
}
