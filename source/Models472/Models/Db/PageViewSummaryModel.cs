
namespace Contensive.Models.Db {
    /// <summary>
    /// Summary tables for page viewings
    /// </summary>
    public class PageViewSummaryModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Page View Summary", "ccPageViewSummary", "default", false);
    }
}
