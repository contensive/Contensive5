
namespace Contensive.Models.Db {
    /// <summary>
    /// Addon Categories
    /// </summary>
    public class AddonCategoryModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("add-on categories", "ccaddoncategories", "default", true);
    }
}
