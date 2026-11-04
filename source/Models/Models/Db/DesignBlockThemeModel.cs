
namespace Contensive.Models.Db {
    /// <summary>
    /// Design Block Themes content definition (ccDesignBlockThemes table).
    /// Each record represents a CSS class selector that can be applied to a design block wrapper.
    /// Referenced by the themeStyleId lookup field in SettingsBaseModel.
    /// </summary>
    public class DesignBlockThemeModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Design Block Themes", "ccDesignBlockThemes", "default", false);
    }
}
