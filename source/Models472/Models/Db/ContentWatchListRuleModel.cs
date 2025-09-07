
namespace Contensive.Models.Db {
    //
    public class ContentWatchListRuleModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Content Watch List Rules", "ccContentWatchListRules", "default", false);
        //
        //====================================================================================================
        /// <summary>
        /// field properties
        /// </summary>
        public int contentWatchId { get; set; }
        public int contentWatchListId { get; set; }
    }
}
