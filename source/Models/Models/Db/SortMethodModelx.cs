
namespace Contensive.Models.Db {
    //
    public class SortMethodModelx : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("sort methods", "ccSortMethods", "default", true);
        //
        //====================================================================================================
        public string orderByClause { get; set; }
    }
}
