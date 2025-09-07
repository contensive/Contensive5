
namespace Contensive.Models.Db {
    //
    public class EmailGroupModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Email Groups", "ccEmailGroups", "default", false);
        //
        //====================================================================================================
        /// <summary>
        /// field properties
        /// </summary>
        public int emailId { get; set; }
        public int groupId { get; set; }

    }
}
