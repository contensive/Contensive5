
namespace Contensive.Models.Db {
    //
    public class LibraryFolderRuleModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Library Folder Rule", "ccLibraryFolderRules", "default", false);
        //
        //====================================================================================================
        //
        public int folderId { get; set; }
        public int groupId { get; set; }
    }
}
