
using Contensive.Models.Attributes;

namespace Contensive.Models.Db {
    /// <summary>
    /// Stores text block content used in design block layouts.
    /// Table: dbText, Content: db Text
    /// </summary>
    public class DbTextModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("db Text", "dbText", "default", false);
        //
        //====================================================================================================
        /// <summary>
        /// The HTML text content of this text block
        /// </summary>
        public string text { get; set; }
    }
}
