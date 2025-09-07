
namespace Contensive.Models.Db {
    //
    public class MetaKeywordModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Meta Keywords", "ccMetaKeywords", "default", false);
        //
        //====================================================================================================
        //
    }
}
