
using System;

namespace Contensive.Models.Db {
    //
    public class SiteWarningModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Site Warnings", "ccSiteWarnings", "default", false);
        //
        //====================================================================================================
        //
        public int count { get; set; }
        public DateTime dateLastReported { get; set; }
        public string description { get; set; }
        public string generalKey { get; set; }
        public string location { get; set; }
        public int pageId { get; set; }
        public string shortDescription { get; set; }
        public string specificKey { get; set; }
        /// <summary>
        /// if true, turn on alarm
        /// </summary>
        public bool alarm { get; set; }
    }
}
