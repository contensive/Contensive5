
namespace Contensive.Models.Db {
    //
    public class LibraryFileLogModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("library File log", "cclibrarydownloadlog", "default", false);
        //
        //====================================================================================================
        public int fileId { get; set; }
        public int memberId { get; set; }
        public int visitId { get; set; }
        public string fromUrl { get; set; }
    }
}
