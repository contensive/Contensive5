
namespace Contensive.Models.Db {
    //
    public class SystemTextMessageTopicRuleModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("system text message topic rules", "ccSystemTextMessageTopicRules", "default", false);
        //
        //====================================================================================================
        public int topicId { get; set; }
        public int systemTextMessageId { get; set; }
    }
}
