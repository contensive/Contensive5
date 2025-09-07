
namespace Contensive.Models.Db {
    //
    public class MenuModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Menus", "ccmenus", "default", false);
        //
        //====================================================================================================
        //
        public string classTopParentItem { get; set; }
        //
        public string classTopAnchor { get; set; }
        //
        public string classTopParentAnchor { get; set; }
        //
        public string dataToggleTopParentAnchor { get; set; }
        //
        public string classTierAnchor { get; set; }
        //
        public string classTopWrapper { get; set; }
        //
        public string classTopList { get; set; }
        //
        public string classTopItem { get; set; }
        //
        public string classItemActive { get; set; }
        //
        public string classTierList { get; set; }
        //
        public string classTierItem { get; set; }
        //
        public string classItemFirst { get; set; }
        //
        public string classItemLast { get; set; }
        //
        public string classItemHover { get; set; }
    }
}
