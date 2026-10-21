
namespace Contensive.Models.Db {
    //
    /// <summary>
    /// Menu model for the Menus content definition (ccMenus table).
    /// Inherits design block styling fields from SettingsBaseModel.
    /// </summary>
    public class MenuModel : SettingsBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Menus", "ccMenus", "default", false);
        //
        //====================================================================================================
        // Menu-specific functional fields
        //
        /// <summary>
        /// Layout assigned to this menu instance
        /// </summary>
        public int layoutId { get; set; }
        //
        /// <summary>
        /// Dropdown depth configuration
        /// </summary>
        public int depth { get; set; }
        //
        /// <summary>
        /// Add root page to tier dropdown
        /// </summary>
        public bool addRootToTier { get; set; }
        //
        /// <summary>
        /// Include blocked pages in flyout menus
        /// </summary>
        public bool includeBlockedFlyoutPages { get; set; }
        //
        //====================================================================================================
        // CSS class fields for menu styling
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
        //
        //====================================================================================================
        // Design block style fields inherited from SettingsBaseModel:
        // - backgroundImageFilename (string) - Background image for the widget
        // - themeStyleId (int) - Lookup to Design Block Themes for CSS class selector
        // - padTop (bool) - Add padding on top side
        // - padBottom (bool) - Add padding on bottom side
        // - padRight (bool) - Add padding on right side
        // - padLeft (bool) - Add padding on left side
        // - styleheight (string) - Hardcoded height with units
        // - asFullBleed (bool) - Full-width vs container
        // - anchorTag (string) - Optional anchor target ID
        // - animateStyleId (int) - Animation style
        // - animateSpeedId (int) - Animation speed
    }
}
