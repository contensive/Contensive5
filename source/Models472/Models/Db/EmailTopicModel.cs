
namespace Contensive.Models.Db {
    //
    public class EmailTopicModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Email Topics", "ccEmailTopics", "default", false);
        //
        //====================================================================================================
        /// <summary>
        /// field properties
        /// </summary>
        public int emailId { get; set; }
        public int topicId { get; set; }

    }
}
