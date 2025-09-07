
namespace Contensive.Models.Db {
    //
    public class CopyContentModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Copy Content", "cccopycontent", "default", true);
        //
        //====================================================================================================
        //
        public string copy { get; set; }
    }
}
