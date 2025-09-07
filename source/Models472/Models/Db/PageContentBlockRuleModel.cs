
namespace Contensive.Models.Db {
    //
    public class PageContentBlockRuleModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Page Content Block Rules", "ccPageContentBlockRules", "default", false);
        //
        //====================================================================================================
        /// <summary>
        /// group allowed on this page
        /// </summary>
        public int groupId { get; set; }
        /// <summary>
        /// page
        /// </summary>
        public int recordId { get; set; }
    }
}
