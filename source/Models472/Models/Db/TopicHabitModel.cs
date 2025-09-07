
namespace Contensive.Models.Db {
    //
    public class TopicHabitModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Topic Habits", "Topic Habits", "default", false);
        //
        //====================================================================================================
        /// <summary>
        /// field properties
        /// </summary>
        public string contentRecordKey { get; set; }
        public int memberId { get; set; }
        public int score { get; set; }
        public int topicId { get; set; }
        public int visitId { get; set; }
    }
}
