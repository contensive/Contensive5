
namespace Contensive.Models.Db {
    //
    /// <summary>
    /// Registry of field types available in the system.
    /// Table: ccfieldtypes
    /// The name field holds the display name (e.g., "Integer", "Text", "Lookup").
    /// The id field corresponds to the ContentFieldModel.type value.
    ///
    /// Standard field type IDs:
    ///   1=Integer, 2=Text, 3=LongText, 4=Boolean, 5=Date, 6=File, 7=Lookup,
    ///   8=Redirect, 9=Currency, 10=FileText, 11=FileImage, 12=Float,
    ///   13=AutoIdIncrement, 14=ManyToMany, 15=MemberSelect, 16=FileCSS,
    ///   17=FileXML, 18=FileJavaScript, 19=Link, 20=ResourceLink, 21=HTML,
    ///   22=FileHTML, 23=HTMLCode, 24=FileHTMLCode
    ///
    /// Referenced by:
    /// - ContentFieldModel.type - the field type ID for each content field
    /// </summary>
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
