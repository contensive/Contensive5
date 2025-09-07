
namespace Contensive.Models.Db {
    //
    public class EmailTemplateModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("email templates", "cctemplates", "default", false);
        //
        //====================================================================================================
        public string bodyHTML { get; set; }
        public string source { get; set; }
    }
}
