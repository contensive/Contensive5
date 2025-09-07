
namespace Contensive.Models.Db {
    /// <summary>
    /// Rules that connnect collections to content definitions
    /// </summary>
    public class AddonCollectionCDefRuleModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Add-on Collection CDef Rules", "ccAddonCollectionCDefRuleModel", "default", false);
        //
        //====================================================================================================
        //
    }
}
