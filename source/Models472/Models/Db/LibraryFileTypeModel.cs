
namespace Contensive.Models.Db {
    //
    public class LibraryFileTypeModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Library File Types", "ccLibraryFileTypes", "default", false);
        //
        //====================================================================================================
        //
        public string downloadIconFilename { get; set; }
        public string extensionList { get; set; }
        public string iconFilename { get; set; }
        public bool isDownload { get; set; }
        public bool isFlash { get; set; }
        public bool isImage { get; set; }
        public bool isVideo { get; set; }
        public string mediaIconFilename { get; set; }
    }
}
