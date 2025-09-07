
namespace Contensive.Models.Db {
    //
    public class TopicModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("topics", "ccTopics", "default", false);
        //
        //====================================================================================================
        /// <summary>
        /// allow bulk email to be sent to people in this topic
        /// </summary>
        public bool allowBulkEmail { get; set; }
        /// <summary>
        /// Allow guests to join this topic
        /// </summary>
        public bool publicJoin { get; set; }

    }
}
