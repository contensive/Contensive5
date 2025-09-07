

namespace Contensive.Models.Db {
    //
    public class AddonTemplateRuleModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("add-on template rules", "ccAddontemplaterules", "default", false);
        //
        //====================================================================================================
        /// <summary>
        /// field properties
        /// </summary>
        public int addonId { get; set; }
        public int templateId { get; set; }
    }
}
