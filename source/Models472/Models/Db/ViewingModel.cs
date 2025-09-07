
using System;

namespace Contensive.Models.Db {
    public class ViewingModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("viewings", "ccviewings", "default", false);
        //
        //====================================================================================================
        public bool excludeFromAnalytics { get; set; }
        public string form { get; set; }
        public string host { get; set; }
        public int memberId { get; set; }
        public string page { get; set; }
        public int pageTime { get; set; }
        public string pageTitle { get; set; }
        public string path { get; set; }
        public string queryString { get; set; }
        public int recordId { get; set; }
        public string referer { get; set; }
        public bool stateOK { get; set; }
        public int visitId { get; set; }
        public int visitorId { get; set; }
    }
}
