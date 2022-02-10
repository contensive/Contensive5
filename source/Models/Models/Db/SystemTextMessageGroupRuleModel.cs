
namespace Contensive.Models.Db {
    //
    public class SystemTextMessageGroupRuleModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("system text message group rules", "ccSystemTextMessageGroupRules", "default", false);
        //
        //====================================================================================================
        public int groupId { get; set; }
        public int systemTextMessageId { get; set; }
    }
}
