
namespace Contensive.Models.Db {
    //
    public class GroupTextMessageTopicRuleModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("group text message topic rules", "ccGroupTextMessageTopicRules", "default", false);
        //
        //====================================================================================================
        public int topicId { get; set; }
        public int groupTextMessageId { get; set; }
    }
}
