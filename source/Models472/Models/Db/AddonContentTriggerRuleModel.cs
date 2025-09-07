
namespace Contensive.Models.Db {
    //
    public class AddonContentTriggerRuleModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Addon Content Trigger Rules", "ccAddonContentTriggerRules", "default", false);
        //
        //====================================================================================================
        public int addonId { get; set; }
        public int contentId { get; set; }
    }
}
