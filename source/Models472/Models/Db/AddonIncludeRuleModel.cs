
namespace Contensive.Models.Db {
    //
    public class AddonIncludeRuleModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Add-on Include Rules", "ccaddonincluderules", "default", false);
        //
        //====================================================================================================
        /// <summary>
        /// field properties
        /// </summary>
        public int addonId { get; set; }
        public int includedAddonId { get; set; }
    }
}
