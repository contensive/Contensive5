
using System;

namespace Contensive.Models.Db {
    //
    public class AuthoringControlModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Authoring Controls", "ccauthoringcontrols", "default", false);
        //
        //====================================================================================================
        /// <summary>
        /// tableId/recordId
        /// </summary>
        public string contentRecordKey { get; set; }
        /// <summary>
        /// type of authoring control
        /// </summary>
        public int controlType { get; set; }
        /// <summary>
        /// date time when this lock expires
        /// </summary>
        public DateTime? dateExpires { get; set; }
    }
}
