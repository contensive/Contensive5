
namespace Contensive.Models.Db {
    //
    public class GroupTextMessageGroupRuleModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("group text message group rules", "ccGroupTextMessageGroupRules", "default", false);
        //
        //====================================================================================================
        public int groupId { get; set; }
        public int groupTextMessageId { get; set; }
    }
}
