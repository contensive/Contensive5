

namespace Contensive.Models.Db {
    //
    public class EmailDropModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Email Drops", "ccemaildrops", "default", false);
        //
        //====================================================================================================
        public int emailId { get; set; }
    }
}
