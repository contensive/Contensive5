
namespace Contensive.Models.Db {
    //
    public class TemplateDomainRuleModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Template Domain Rules", "ccdomaintemplaterules", "default", false);
        //
        //====================================================================================================
        public int domainId { get; set; }
        public int templateId { get; set; }
    }
}
