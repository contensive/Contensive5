
namespace Contensive.Models.Db {
    /// <summary>
    /// Content Watch List, a list of links related to content topics
    /// </summary>
    public class ContentWatchListModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Content Watch Lists", "ccContentWatchLists", "default", false);
    }
}
