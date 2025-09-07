
namespace Contensive.Models.Db {
    //
    public class MemberTopicRuleModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Member Topic Rules", "ccMemberTopicRules", "default", false);
        //
        //====================================================================================================
        /// <summary>
        /// field properties
        /// </summary>
        public int memberId { get; set; }
        public int topicId { get; set; }
    }
}
