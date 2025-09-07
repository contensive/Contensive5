
using System;

namespace Contensive.Models.Db {
    //
    public class EmailBounceListModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Email Bounce List", "EmailBounceList", "default", false);
        //
        //====================================================================================================
        /// <summary>
        /// field properties
        /// </summary>
        public string details { get; set; }
        public string email { get; set; }
        public int transient { get; set; }
        public DateTime transientFixDeadline { get; set; }

    }
}
