
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
        /// <summary>
        /// files to install in cdn
        /// </summary>
        public string contentFileList { get; set; }
        /// <summary>
        /// list of records to export to data nodes
        /// </summary>
        public string dataRecordList { get; set; }
        /// <summary>
        /// files to export and install in collection folder
        /// </summary>
        public string execFileList { get; set; }
        /// <summary>
        /// help text
        /// </summary>
        public string help { get; set; }
        /// <summary>
        /// help link
        /// </summary>
        public string helpLink { get; set; }
        /// <summary>
        /// date of the last change
        /// </summary>
        public DateTime? lastChangeDate { get; set; }
        public string otherXML { get; set; }
        public bool system { get; set; }
        public bool updatable { get; set; }
        public string wwwFileList { get; set; }
        public int oninstalladdonid { get; set; }
    }
}
