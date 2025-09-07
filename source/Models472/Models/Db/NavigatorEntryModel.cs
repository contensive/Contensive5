
namespace Contensive.Models.Db {
    //
    public class NavigatorEntryModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Navigator Entries", "ccmenuentries", "default", false);
        //
        //====================================================================================================
        public int parentId { get; set; }
        public string navIconTitle { get; set; }
        public int navIconType { get; set; }
        public int addonId { get; set; }
        public bool adminOnly { get; set; }
        public int contentId { get; set; }
        public bool developerOnly { get; set; }
        public int helpAddonId { get; set; }
        public int helpCollectionId { get; set; }
        public int installedByCollectionId { get; set; }
        public string linkPage { get; set; }
        public bool newWindow { get; set; }
    }
}
