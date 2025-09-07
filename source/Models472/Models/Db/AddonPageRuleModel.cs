
namespace Contensive.Models.Db {
    //
    public class AddonPageRuleModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("add-on page rules", "ccaddonpagerules", "default", false);
        //
        //====================================================================================================
        /// <summary>
        /// field properties
        /// </summary>
        public int addonId { get; set; }
        public int pageId { get; set; }
    }
}
