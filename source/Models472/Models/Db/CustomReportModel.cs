
using System;

namespace Contensive.Models.Db {
    public class CustomReportModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Custom Reports", "ccCustomReports", "default", false);
        //
        //====================================================================================================
        // -- instance properties (must be properties not fields)
        //
        public string sqlQuery { get; set; }
    }
}
