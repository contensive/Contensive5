
namespace Contensive.Models.Db {
    //
    public class AddonCollectionParentRuleModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Addon Collection Parent Rules", "ccAddonCollectionParentRules", "default", false);
        //
        //====================================================================================================
        //
        public int childId { get; set; }
        public int parentId { get; set; }
    }
}
