
namespace Contensive.Models.Db {
    //
    public class MenuPageRuleModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Menu Page Rules", "ccmenupagerules", "default", false);
        //
        //====================================================================================================
        public int menuId { get; set; }
        public int pageId { get; set; }
    }
}
