
namespace Contensive.Models.Db {
    //
    public class LibraryFilesModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("library Files", "cclibraryfiles", "default", false);
        //
        //====================================================================================================
        // -- instance properties
        public string altSizeList { get; set; }
        public string altText { get; set; }
        public int clicks { get; set; }
        public string description { get; set; }
        public string filename { get; set; }
        public int fileSize { get; set; }
        public int fileTypeId { get; set; }
        public int folderId { get; set; }
        public int height { get; set; }
        public int width { get; set; }
    }
}
