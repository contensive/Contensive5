
using System;
//
namespace Contensive.Models.Db {
    //
    public class RemoteQueryModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("remote queries", "ccremotequeries", "default", false);
        //
        //====================================================================================================
        public bool allowInactiveRecords { get; set; }
        public int contentId { get; set; }
        public string criteria { get; set; }
        public int dataSourceId { get; set; }
        public DateTime? dateExpires { get; set; }
        public int maxRows { get; set; }
        public int queryTypeId { get; set; }
        public string remoteKey { get; set; }
        public string selectFieldList { get; set; }
        public string sortFieldList { get; set; }
        public string sqlQuery { get; set; }
        public int visitId { get; set; }
    }
}
