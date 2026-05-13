

//
using System.Collections.Generic;

namespace Contensive.Models.Db {
    //
    /// <summary>
    /// Field definitions for content definitions. Each record defines one field within a content definition.
    /// Table: ccfields
    ///
    /// Relationships:
    /// - contentId -> ContentModel (cccontent.id) - the content definition this field belongs to
    /// - lookupContentId -> ContentModel (cccontent.id) - for Lookup (type 7) and MemberSelect (type 15) fields, the target content
    /// - manyToManyContentId -> ContentModel (cccontent.id) - for ManyToMany (type 14) fields, the secondary content
    /// - manyToManyRuleContentId -> ContentModel (cccontent.id) - for ManyToMany fields, the rule table content
    /// - redirectContentId -> ContentModel (cccontent.id) - for Redirect (type 8) fields
    /// - editorAddonId -> AddonModel - custom editor addon
    /// - memberSelectGroupId -> group record for MemberSelect fields
    /// - installedByCollectionId -> the addon collection that installed this field
    ///
    /// Field type values (type property):
    ///   1=Integer, 2=Text, 3=LongText, 4=Boolean, 5=Date, 6=File, 7=Lookup,
    ///   8=Redirect, 9=Currency, 10=FileText, 11=FileImage, 12=Float,
    ///   13=AutoIdIncrement, 14=ManyToMany, 15=MemberSelect, 16=FileCSS,
    ///   17=FileXML, 18=FileJavaScript, 19=Link, 20=ResourceLink, 21=HTML,
    ///   22=FileHTML, 23=HTMLCode, 24=FileHTMLCode
    /// </summary>
    public class ContentFieldModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("content fields", "ccfields", "default", false);
        //
        //====================================================================================================
        /// <summary>
        /// admin editing restrited to admin or developer.
        /// </summary>
        public bool adminOnly { get; set; }
        /// <summary>
        /// admin editing disabled
        /// </summary>
        public bool authorable { get; set; }
        /// <summary>
        /// The admin editing caption
        /// </summary>
        public string caption { get; set; }
        /// <summary>
        /// FK to ContentModel (cccontent.id) - the content definition this field belongs to
        /// </summary>
        public int contentId { get; set; }
        /// <summary>
        /// value added to admin edit new records, cs.insert() and model.addDefault()
        /// </summary>
        public string defaultValue { get; set; }
        /// <summary>
        /// field only visible to developers, hidden to admin and content managers
        /// </summary>
        public bool developerOnly { get; set; }
        /// <summary>
        /// if adddon selected, this addon will be used as the admin editor
        /// </summary>
        public int editorAddonId { get; set; }
        /// <summary>
        /// in admin edit, this is used as the sort order after editTab
        /// </summary>
        public int editSortPriority { get; set; }
        /// <summary>
        /// admin edit, tabs at the top.
        /// </summary>
        public string editTab { get; set; }
        /// <summary>
        /// In edit-modal, left side is details-tab, right side is edit-groups. Groups do not 
        /// </summary>
        public string editGroup { get; set; }
        /// <summary>
        /// (legacy) previously used to set an html field as wysiwyg editor
        /// </summary>
        public bool htmlContent { get; set; }
        /// <summary>
        /// In admin list, the column number
        /// </summary>
        public int indexColumn { get; set; }
        /// <summary>
        /// if indexcolumn is valid, this is the default sort direction for this field
        /// </summary>
        public int indexSortDirection { get; set; }
        /// if indexcolumn is valid, this is the sorting priority 
        public int indexSortPriority { get; set; }
        /// <summary>
        /// if indexcolumn is valid, this is the relative width of the admin list
        /// </summary>
        public string indexWidth { get; set; }
        /// <summary>
        /// The id of the collection that installed this field
        /// </summary>
        public int installedByCollectionId { get; set; }
        /// <summary>
        /// FK to ContentModel (cccontent.id) - for Lookup (type 7) and MemberSelect (type 15) fields,
        /// the content definition that this field references. To find the actual database table,
        /// join: cccontent c on c.id = lookupContentId, then cctables t on t.id = c.contentTableId, use t.name
        /// </summary>
        public int lookupContentId { get; set; }
        /// <summary>
        /// if field type is lookup and lookupContentId is 0, this is a 1-based comma separated list of index values
        /// </summary>
        public string lookupList { get; set; }
        /// <summary>
        /// FK to ContentModel (cccontent.id) - for ManyToMany (type 14) fields, the secondary content referenced by the rule table
        /// </summary>
        public int manyToManyContentId { get; set; }
        /// <summary>
        /// FK to ContentModel (cccontent.id) - for ManyToMany fields, the rule table content definition
        /// </summary>
        public int manyToManyRuleContentId { get; set; }
        /// <summary>
        /// for ManyToMany fields, the field name in the rule table that references the primary content
        /// </summary>
        public string manyToManyRulePrimaryField { get; set; }
        /// <summary>
        /// for ManyToMany fields, the field name in the rule table that references the secondary content
        /// </summary>
        public string manyToManyRuleSecondaryField { get; set; }
        public int memberSelectGroupId { get; set; }
        public bool notEditable { get; set; }
        public bool password { get; set; }
        public bool readOnly { get; set; }
        public int redirectContentId { get; set; }
        public string redirectId { get; set; }
        public string redirectPath { get; set; }
        public bool required { get; set; }
        public bool rssDescriptionField { get; set; }
        public bool rssTitleField { get; set; }
        public bool scramble { get; set; }
        public bool textBuffered { get; set; }
        /// <summary>
        ///         Integer = 1,Text = 2,LongText = 3,Boolean = 4,Date = 5,File = 6,Lookup = 7,Redirect = 8, Currency = 9, FileText = 10,FileImage = 11, Float = 12,AutoIdIncrement = 13, ManyToMany = 14,
        ///         MemberSelect = 15,FileCSS = 16, FileXML = 17,FileJavaScript = 18,Link = 19,ResourceLink = 20,HTML = 21,FileHTML = 22,HTMLCode = 23, FileHTMLCode = 24
        /// </summary>
        public int type { get; set; }
        public bool uniqueName { get; set; }
        public bool isBaseField { get; set; }
    }
}
