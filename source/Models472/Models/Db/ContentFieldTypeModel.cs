
namespace Contensive.Models.Db {
    //
    public class ContentFieldTypeModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Content Field Types", "ccfieldtypes", "default", false);
        //
        //====================================================================================================
        // -- instance properties (must be properties not fields)
        /// <summary>
        /// Default addon for this content type
        /// </summary>
        public int editoraddonid { get; set; }
    }
}
