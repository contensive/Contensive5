

//
using System.Collections.Generic;

namespace Contensive.Models.Db {
    //
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
        /// the table metadata
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
        /// if field type is lookup, this field is a foreign key into this table metadata
        /// </summary>
        public int lookupContentId { get; set; }
        /// <summary>
        /// if field type is lookup and lookupcontentid is 0, this is a 1-based comma separated list of index values
        /// </summary>
        public string lookupList { get; set; }
        /// <summary>
        /// for manytomany field types, the other content referenced by the m2m rules
        /// </summary>
        public int manyToManyContentId { get; set; }
        public int manyToManyRuleContentId { get; set; }
        public string manyToManyRulePrimaryField { get; set; }
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
        public int type { get; set; }
        public bool uniqueName { get; set; }
        public bool isBaseField { get; set; }
    }
}
