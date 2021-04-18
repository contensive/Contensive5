
using System;
//
namespace Contensive.Models.Db {
    /// <summary>
    /// records that represent addon collections
    /// </summary>
    public class AddonCollectionModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Add-on Collections", "ccaddoncollections", "default", true);
        //
        //====================================================================================================
        /// <summary>
        /// field properties
        /// </summary>
        public bool blockNavigatorNode { get; set; }
        public string contentFileList { get; set; }
        public string dataRecordList { get; set; }
        public string execFileList { get; set; }
        public string help { get; set; }
        public string helpLink { get; set; }
        public DateTime? lastChangeDate { get; set; }
        public string otherXML { get; set; }
        public bool system { get; set; }
        public bool updatable { get; set; }
        public string wwwFileList { get; set; }
        public int oninstalladdonid { get; set; }
    }
}
