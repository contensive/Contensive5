
namespace Contensive.Models.Db {
    //
    public class LanguageModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("languages", "cclanguages", "default", true);
        //
        //====================================================================================================
        public string http_Accept_Language { get; set; }
    }
}
