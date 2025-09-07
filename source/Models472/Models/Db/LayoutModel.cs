
namespace Contensive.Models.Db {
    //
    public class LayoutModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("layouts", "cclayouts", "default", true);
        //
        //====================================================================================================
        /// <summary>
        /// The default html compatible text file for this layout. This layout should be used when no other layout is provided, or when the site is set to Html Platform version 4 (bootstrap 4 based designs). The Html Platform version is a site property the coordinates resources like style and layouts for one set of standards (like Bootstrap 4 vs 5).
        /// </summary>
        public FieldTypeTextFile layout { get; set; }
        /// <summary>
        /// The html compatible text file for this layout when the site is set to Html Platform version 5 (bootstrap 5 based designs). The Html Platform version is a site property the coordinates resrources like style and layouts for one set of standards.
        /// </summary>
        public FieldTypeTextFile layoutPlatform5 { get; set; }
        /// <summary>
        /// the addon collection that installed this record
        /// </summary>
        public int installedByCollectionId { get; set; }
        /// <summary>
        /// deprecated. styles are implemented only through addons
        /// </summary>
        public string stylesFilename { get; set; }
    }
}
