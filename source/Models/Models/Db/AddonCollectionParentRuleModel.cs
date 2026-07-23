
namespace Contensive.Models.Db {
    //
    public class AddonCollectionParentRuleModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Add-on Collection Parent Rules", "ccAddonCollectionParentRules", "default", false);
        //
        //====================================================================================================
        //
        public int childId { get; set; }
        public int parentId { get; set; }
    }
}
