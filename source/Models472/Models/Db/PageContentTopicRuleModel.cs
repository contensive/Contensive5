
namespace Contensive.Models.Db {
    //
    public class PageContentTopicRuleModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("page content topic rules", "ccpagecontenttopicrules", "default", false);
        //
        //====================================================================================================
        /// <summary>
        /// The page associated to this topic. Add to track users interested in this topic and to provide navigation to this topic.
        /// </summary>
        public int pageId { get; set; }
        /// <summary>
        /// The topic 
        /// </summary>
        public int topicId { get; set; }

    }
}
