
using System;
using System.Data;
using Contensive.BaseClasses;
using Contensive.Exceptions;
using Contensive.Models.Db;
using Contensive.Processor.Controllers;

using static Contensive.Processor.Constants;

namespace Contensive.Processor.Models.Domain {
    //
    //====================================================================================================
    /// <summary>
    /// The metadata for a field
    /// public properties are those read/written to the XML file
    /// public methods translate the properties as needed
    /// For example, a lookup field stores the name of the content it joins in . The site runs with the id of 
    /// that content. This model has the content 'name'
    /// </summary>
    public class ContentFieldMetadataModel : ICloneable, IComparable {
        /// <summary>
        /// Create a content metadata field with all default values
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static ContentFieldMetadataModel createDefault(CoreController core, string fieldName, CPContentBaseClass.FieldTypeIdEnum fieldType) {
            var fieldMeta = new ContentFieldMetadataModel {
                active = true,
                adminOnly = false,
                authorable = true,
                blockAccess = false,
                caption = fieldName,
                contentId = 0,
                developerOnly = false,
                editSortPriority = 9999,
                editTabName = "",
                fieldTypeId = fieldType,
                htmlContent = false,
                indexColumn = 0,
                indexSortDirection = 0,
                indexSortOrder = 0,
                indexWidth = "",
                installedByCollectionGuid = "",
                editorAddonGuid = "",
                isBaseField = false,
                lookupContentId = 0,
                LookupContentSqlFilter = "",
                lookupList = "",
                manyToManyContentId = 0,
                manyToManyRuleContentId = 0,
                manyToManyRulePrimaryField = "",
                manyToManyRuleSecondaryField = "",
                password = false,
                readOnly = false,
                redirectContentId = 0,
                redirectId = "",
                redirectPath = "",
                required = false,
                scramble = false,
                textBuffered = false,
                uniqueName = false
            };
            fieldMeta.memberSelectGroupId_set(core, 0);
            fieldMeta.nameLc = fieldName.ToLowerInvariant();
            fieldMeta.set_redirectContentName(core, "");
            fieldMeta.set_manyToManyContentName(core, "");
            fieldMeta.set_manyToManyRuleContentName(core, "");
            return fieldMeta;
        }
        //
        public string editorAddonGuid { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// name of the field, matches the database field name.
        /// </summary>
        public string nameLc { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// id of the ccField record that holds this metadata
        /// </summary>
        public int id { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// if the field is available in the admin area
        /// </summary>
        public bool active { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// The type of data the field holds
        /// </summary>
        public CPContentBaseClass.FieldTypeIdEnum fieldTypeId { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// The caption for displaying the field 
        /// </summary>
        public string caption { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// if true value cannot be written back to the database 
        /// </summary>
        public bool readOnly { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// if true a new record can save the fields value, but an edited record cannot be saved
        /// </summary>
        public bool notEditable { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// if true the record cannot be saved if thie field has a null value
        /// </summary>
        public bool required { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// string representation of the default value on a new record
        /// </summary>
        public string defaultValue { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// if true saves will be blocked if other records have this value for this field. An error is thrown so this should be a last resort
        /// </summary>
        public bool uniqueName { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// if true the value is run through RemoveControlCharacters() during rendering. (not during save)
        /// </summary>
        public bool textBuffered { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// field is treated as a password when edited
        /// </summary>
        public bool password { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// if type is REDIRECT, edit form creates a link, this is the field name that must match ID of this record. The edit screen will show a link 
        /// </summary>
        public string redirectId { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// if type is REDIRECT, this is the path to the next page (if blank, current page is used)
        /// </summary>
        public string redirectPath { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// if indexWidth>0, this is the column for the list form, 0 based
        /// </summary>
        public int indexColumn { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// the width of the column in the list form for this field. 
        /// </summary>
        public string indexWidth { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// alpha sort on index page
        /// </summary>
        public int indexSortOrder { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// 1 sorts forward, -1 backward
        /// </summary>
        public int indexSortDirection { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// if true, the edit and list forms only the field to admin
        /// </summary>
        public bool adminOnly { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// if true, the edit and list forms only the field to developer
        /// </summary>
        public bool developerOnly { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// deprecate -- (was - Field Reused to keep binary compatiblity - "IsBaseField" - if true this is a CDefBase field)
        /// </summary>
        public bool blockAccess { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// for html type, if true use a text editor, not a wysiwyg editor
        /// </summary>
        public bool htmlContent { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// if true this field is avaialble in the edit and list forms
        /// </summary>
        public bool authorable { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// if true this field takes its values from a parent, see ContentID
        /// </summary>
        public bool inherited { get; set; }
        //
        // todo this is a running metadata, not storage data. Should be contentName or guid
        //====================================================================================================
        /// <summary>
        /// This is the ID of the Content Def that defines these properties
        /// </summary>
        public int contentId { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// order for edit form
        /// </summary>
        public int editSortPriority { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// if many-to-many type, the field name in the rule table that matches this record's id (the foreign-key in the rule table that points to this record's id)
        /// </summary>
        public string manyToManyRulePrimaryField { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// if many-to-many type, the field name in the rule table that matches the joining record (the foreign-key in the rule table that points to the joining table's record id)
        /// </summary>
        public string manyToManyRuleSecondaryField { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// if true an RSS creating process can consider this field as the RSS entity title
        /// </summary>
        public bool rssTitleField { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// if true an RSS creating process can consider this field as the RSS entity description
        /// </summary>
        public bool rssDescriptionField { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// in the edit form, this field should appear in this edit tab
        /// </summary>
        public string editTabName { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// if true this field is saved in a two-way encoding format
        /// </summary>
        public bool scramble { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// for fieldtype lookup, if lookupcontent is null and this is not, this is a comma delimited list of options. The field's value is an index into the list, starting with 1
        /// </summary>
        public string lookupList { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// if true the field has changed and needs to be saved(?)
        /// </summary>
        public bool dataChanged { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// deprecate -- represents that the field was created by aoBase collection. replace with installedByCollectionGuid
        /// </summary>
        public bool isBaseField { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// Legacy - true if the field has been modified sinced it was installed
        /// </summary>
        public bool isModifiedSinceInstalled { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// todo wrong. this is a storage model, so the collection is know and does not need to be in the field model, guid of collection that installed this field
        /// </summary>
        public string installedByCollectionGuid { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// Help text for this field. This text is displayed with the field in the editor
        /// </summary>
        public string helpDefault { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// Legacy help field, deprecate
        /// </summary>
        public string helpCustom { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// Legacy help flag, deprecate
        /// </summary>
        public bool helpChanged { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// Legacy
        /// </summary>
        public string helpMessage { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// For redirect types, the content id where the field will redirect.
        /// </summary>
        public int redirectContentId { get; set; } // If TYPEREDIRECT, this is new contentID
        public string get_redirectContentName(CoreController core) {
            if (_redirectContentName == null) {
                if (redirectContentId > 0) {
                    _redirectContentName = "";
                    DataTable dt = core.db.executeQuery("select name from cccontent where id=" + redirectContentId.ToString());
                    if (dt.Rows.Count > 0) {
                        _redirectContentName = GenericController.encodeText(dt.Rows[0][0]);
                    }
                }
            }
            return _redirectContentName;
        }
        public void set_redirectContentName(CoreController core, string value) {
            _redirectContentName = value;
        }
        private string _redirectContentName = null;
        //
        //====================================================================================================
        /// <summary>
        /// For many-to-many type fields, this is the contentid of the secondary data table.
        /// Primary content is the content that contains the field being definied.
        /// Secondary content is the data table the primary is being connected to.
        /// Rule content is the table that has two foreign keys, one for the primary and one for the secondary
        /// </summary>
        public int manyToManyContentId { get; set; } // Content containing Secondary Records
        public string get_manyToManyContentName(CoreController core) {
            if (_manyToManyRuleContentName == null) {
                if (manyToManyContentId > 0) {
                    _manyToManyRuleContentName = "";
                    DataTable dt = core.db.executeQuery("select name from cccontent where id=" + manyToManyContentId.ToString());
                    if (dt.Rows.Count > 0) {
                        _manyToManyContentName = GenericController.encodeText(dt.Rows[0][0]);
                    }
                }
            }
            return _manyToManyContentName;
        }
        public void set_manyToManyContentName(CoreController core, string value) {
            _manyToManyContentName = value;
        }
        private string _manyToManyContentName = null;
        //
        //====================================================================================================
        /// <summary>
        /// For many-to-many fields, the contentid of the rule table. 
        /// Primary content is the content that contains the field being definied.
        /// Secondary content is the data table the primary is being connected to.
        /// Rule content is the table that has two foreign keys, one for the primary and one for the secondary
        /// </summary>
        public int manyToManyRuleContentId { get; set; }
        public string get_manyToManyRuleContentName(CoreController core) {
            if (_manyToManyRuleContentName == null) {
                if (manyToManyRuleContentId > 0) {
                    _manyToManyRuleContentName = "";
                    DataTable dt = core.db.executeQuery("select name from cccontent where id=" + manyToManyRuleContentId.ToString());
                    if (dt.Rows.Count > 0) {
                        _manyToManyRuleContentName = GenericController.encodeText(dt.Rows[0][0]);
                    }
                }
            }
            return _manyToManyRuleContentName;
        }
        public void set_manyToManyRuleContentName(CoreController core, string value) {
            _manyToManyRuleContentName = value;
        }
        private string _manyToManyRuleContentName = null;
        //
        //====================================================================================================
        /// <summary>
        /// For lookup types, this is the contentid for the connected table. This represents a foreignKey in this content
        /// </summary>
        public int lookupContentId { get; set; }
        public string LookupContentSqlFilter { get; set; }
        public string get_lookupContentName(CoreController core) {
            if ((_lookupContentName == null) && (lookupContentId > 0)) {
                _lookupContentName = "";
                var content = ContentModel.create<ContentModel>(core.cpParent, lookupContentId);
                if (content != null) { _lookupContentName = content.name; }
            }
            return _lookupContentName;
        }
        public void set_lookupContentName(CoreController core, string value) {
            _lookupContentName = value;
        }
        private string _lookupContentName = null;
        //
        //====================================================================================================
        /// <summary>
        /// For memberSelect type content. memberSelectGroup, name set by xml file load, name get for xml file save, id and name get and set in code
        /// </summary>
        public void memberSelectGroupName_set(CoreController core, string memberSelectGroupName) {
            if (_memberSelectGroupName != memberSelectGroupName) {
                _memberSelectGroupName = memberSelectGroupName;
                _memberSelectGroupId = null;
            }
        }
        /// <summary>
        /// For memberSelect type content. memberSelectGroup, name set by xml file load, name get for xml file save, id and name get and set in code
        /// </summary>
        public string memberSelectGroupName_get(CoreController core) {
            if ((_memberSelectGroupName == null) && (_memberSelectGroupId != null)) {
                if (_memberSelectGroupId == 0) {
                    _memberSelectGroupName = "";
                } else {
                    _memberSelectGroupName = MetadataController.getRecordName(core, "groups", GenericController.encodeInteger(_memberSelectGroupId));
                }
            }
            return (_memberSelectGroupName as string);
        }
        /// <summary>
        /// For memberSelect type content. memberSelectGroup, name set by xml file load, name get for xml file save, id and name get and set in code
        /// </summary>
        public void memberSelectGroupId_set(CoreController core, int memberSelectGroupId) {
            if (memberSelectGroupId != _memberSelectGroupId) {
                _memberSelectGroupId = memberSelectGroupId;
                _memberSelectGroupName = null;
            }
        }
        /// <summary>
        /// For memberSelect type content. memberSelectGroup, name set by xml file load, name get for xml file save, id and name get and set in code
        /// </summary>
        public int memberSelectGroupId_get(CoreController core, string contentName, string fieldName ) {
            if (_memberSelectGroupId != null) { return GenericController.encodeInteger(_memberSelectGroupId); }
            //
            // -- memberSelectGroupId is not provided
            if (string.IsNullOrEmpty(_memberSelectGroupName)) {
                //
                // -- memberSelectGroupName is not provided
                _memberSelectGroupId = 0;
                return 0;
            }
            //
            // -- memberSelectGroupName provided
            if (GenericController.isGuid(_memberSelectGroupName)) {
                //
                // -- the name is a guid, attempt to find group with this guid
                var groupByGuid = DbBaseModel.create<GroupModel>(core.cpParent, _memberSelectGroupName);
                if ( groupByGuid == null ) {
                    core.doc.userErrorList.Add("Content Definition [" + contentName + "] includes a field [" + fieldName + "] with MemberSelectGroup set to a guid, but site does not include this group.");
                    LogControllerX.logError(core, new GenericException("Content Definition [" + contentName + "] includes a field [" + fieldName + "] with MemberSelectGroup set to a guid, but site does not include this group."));
                    _memberSelectGroupId = 0;
                    return 0;
                }
            }
            //
            // -- a name was provided. if the group does not exisit, create it
            var groupByName = DbBaseModel.createByUniqueName<GroupModel>(core.cpParent, _memberSelectGroupName);
            if (groupByName == null) {
                groupByName = DbBaseModel.addDefault<GroupModel>(core.cpParent);
                groupByName.name = _memberSelectGroupName;
                groupByName.caption = _memberSelectGroupName;
                groupByName.save(core.cpParent);
                _memberSelectGroupId = groupByName.id;
                return groupByName.id;
            }
            if ( groupByName != null) {
                _memberSelectGroupId = groupByName.id;
                return GenericController.encodeInteger(_memberSelectGroupId);
            }
            //
            // -- memberSelectGroupName was not valid
            LogControllerX.logError(core, new GenericException("Content Definition [" + contentName + "] includes a field [" + fieldName + "] with MemberSelectGroup set to a guid, but site does not include this group."));
            core.doc.userErrorList.Add("Content Definition [" + contentName + "] includes a field [" + fieldName + "] with MemberSelectGroup set to a guid, but site does not include this group.");
            _memberSelectGroupId = 0;
            return 0;
        }
        private string _memberSelectGroupName = null;
        private int? _memberSelectGroupId = null;
        //
        //====================================================================================================
        /// <summary>
        /// Create a clone of this object
        /// </summary>
        public object Clone() {
            return this.MemberwiseClone();
        }
        //
        //====================================================================================================
        /// <summary>
        /// true if the object being comparied is the same object typea and the name field matches
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj) {
            Models.Domain.ContentFieldMetadataModel c = (Models.Domain.ContentFieldMetadataModel)obj;
            return string.Compare(this.nameLc.ToLowerInvariant(), c.nameLc.ToLowerInvariant());
        }
        //
        //========================================================================
        /// <summary>
        /// verify a fieldmetadata fields (ccfields) exists, given a real database field. Verifies the ccfield record
        /// </summary>
        /// <param name="contentName"></param>
        /// <param name="fieldName"></param>
        /// <param name="ADOFieldType"></param>
        public static void verifyContentFieldFromSqlTableField(CoreController core, ContentMetadataModel contentMetadata, string fieldName, int ADOFieldType) {
            try {
                //
                string logMsgContext = "Verifying content field from db field data,  [" + fieldName + "] in table [" + contentMetadata.tableName + "]";
                //
                ContentFieldMetadataModel field = new ContentFieldMetadataModel {
                    fieldTypeId = core.db.getFieldTypeIdByADOType(ADOFieldType),
                    caption = fieldName,
                    editSortPriority = 1000,
                    readOnly = false,
                    authorable = true,
                    adminOnly = false,
                    developerOnly = false,
                    textBuffered = false,
                    htmlContent = false,
                    contentId = contentMetadata.id
                };
                //
                switch (GenericController.toUCase(fieldName)) {
                    //
                    // --- Core fields
                    //
                    case "NAME":
                        field.caption = "Name";
                        field.editSortPriority = 100;
                        break;
                    case "ACTIVE":
                        field.caption = "Active";
                        field.editSortPriority = 200;
                        field.fieldTypeId = CPContentBaseClass.FieldTypeIdEnum.Boolean;
                        field.defaultValue = "1";
                        break;
                    case "DATEADDED":
                        field.caption = "Created";
                        field.readOnly = true;
                        field.editSortPriority = 5020;
                        break;
                    case "CREATEDBY":
                        field.caption = "Created By";
                        field.fieldTypeId = CPContentBaseClass.FieldTypeIdEnum.Lookup;
                        field.set_lookupContentName(core, "Members");
                        field.readOnly = true;
                        field.editSortPriority = 5030;
                        break;
                    case "MODIFIEDDATE":
                        field.caption = "Modified";
                        field.readOnly = true;
                        field.editSortPriority = 5040;
                        break;
                    case "MODIFIEDBY":
                        field.caption = "Modified By";
                        field.fieldTypeId = CPContentBaseClass.FieldTypeIdEnum.Lookup;
                        field.set_lookupContentName(core, "Members");
                        field.readOnly = true;
                        field.editSortPriority = 5050;
                        break;
                    case "ID":
                        field.caption = "Number";
                        field.readOnly = true;
                        field.editSortPriority = 5060;
                        field.authorable = true;
                        field.adminOnly = false;
                        field.developerOnly = true;
                        break;
                    case "CONTENTCONTROLID":
                        field.caption = "Content Definition";
                        field.fieldTypeId = CPContentBaseClass.FieldTypeIdEnum.Lookup;
                        field.set_lookupContentName(core, "Content");
                        field.editSortPriority = 5070;
                        field.authorable = true;
                        field.readOnly = false;
                        field.adminOnly = true;
                        field.developerOnly = true;
                        break;
                    case "CREATEKEY":
                        field.caption = "CreateKey";
                        field.readOnly = true;
                        field.editSortPriority = 5080;
                        field.authorable = false;
                        //
                        // --- fields related to body content
                        //
                        break;
                    case "HEADLINE":
                        field.caption = "Headline";
                        field.editSortPriority = 1000;
                        field.htmlContent = false;
                        break;
                    case "DATESTART":
                        field.caption = "Date Start";
                        field.editSortPriority = 1100;
                        break;
                    case "DATEEND":
                        field.caption = "Date End";
                        field.editSortPriority = 1200;
                        break;
                    case "PUBDATE":
                        field.caption = "Publish Date";
                        field.editSortPriority = 1300;
                        break;
                    case "ORGANIZATIONID":
                        field.caption = "Organization";
                        field.fieldTypeId = CPContentBaseClass.FieldTypeIdEnum.Lookup;
                        field.set_lookupContentName(core, "Organizations");
                        field.editSortPriority = 2005;
                        field.authorable = true;
                        field.readOnly = false;
                        break;
                    case "COPYFILENAME":
                        field.caption = "Copy";
                        field.fieldTypeId = CPContentBaseClass.FieldTypeIdEnum.FileHTML;
                        field.textBuffered = true;
                        field.editSortPriority = 2010;
                        break;
                    case "BRIEFFILENAME":
                        field.caption = "Overview";
                        field.fieldTypeId = CPContentBaseClass.FieldTypeIdEnum.FileHTML;
                        field.textBuffered = true;
                        field.editSortPriority = 2020;
                        field.htmlContent = false;
                        break;
                    case "IMAGEFILENAME":
                        field.caption = "Image";
                        field.fieldTypeId = CPContentBaseClass.FieldTypeIdEnum.File;
                        field.editSortPriority = 2040;
                        break;
                    case "THUMBNAILFILENAME":
                        field.caption = "Thumbnail";
                        field.fieldTypeId = CPContentBaseClass.FieldTypeIdEnum.File;
                        field.editSortPriority = 2050;
                        break;
                    case "CONTENTID":
                        field.caption = "Content";
                        field.fieldTypeId = CPContentBaseClass.FieldTypeIdEnum.Lookup;
                        field.set_lookupContentName(core, "Content");
                        field.readOnly = false;
                        field.editSortPriority = 2060;
                        //
                        // --- Record Features
                        //
                        break;
                    case "PARENTID":
                        field.caption = "Parent";
                        field.fieldTypeId = CPContentBaseClass.FieldTypeIdEnum.Lookup;
                        field.set_lookupContentName(core, contentMetadata.name);
                        field.readOnly = false;
                        field.editSortPriority = 3000;
                        break;
                    case "MEMBERID":
                        field.caption = "Member";
                        field.fieldTypeId = CPContentBaseClass.FieldTypeIdEnum.Lookup;
                        field.set_lookupContentName(core, "Members");
                        field.readOnly = false;
                        field.editSortPriority = 3005;
                        break;
                    case "CONTACTMEMBERID":
                        field.caption = "Contact";
                        field.fieldTypeId = CPContentBaseClass.FieldTypeIdEnum.Lookup;
                        field.set_lookupContentName(core, "Members");
                        field.readOnly = false;
                        field.editSortPriority = 3010;
                        break;
                    case "ALLOWBULKEMAIL":
                        field.caption = "Allow Bulk Email";
                        field.editSortPriority = 3020;
                        break;
                    case "ALLOWSEEALSO":
                        field.caption = "Allow See Also";
                        field.editSortPriority = 3030;
                        break;
                    case "ALLOWFEEDBACK":
                        field.caption = "Allow Feedback";
                        field.editSortPriority = 3040;
                        field.authorable = false;
                        break;
                    case "SORTORDER":
                        field.caption = "Alpha Sort Order";
                        field.editSortPriority = 3050;
                        //
                        // --- Display only information
                        //
                        break;
                    case "CLICKS":
                        field.caption = "Clicks";
                        field.readOnly = true;
                        field.editSortPriority = 5010;
                        field.defaultValue = "0";
                        break;
                }
                contentMetadata.verifyContentField(core, field, true, logMsgContext);
            } catch (Exception ex) {
                LogControllerX.logError(core, ex);
                throw;
            }
        }
        //========================================================================
        /// <summary>
        /// Get FieldDescritor from FieldType
        /// </summary>
        /// <param name="fieldType"></param>
        /// <returns></returns>
        //
        public static string getFieldTypeNameFromFieldTypeId(CoreController core, CPContentBaseClass.FieldTypeIdEnum fieldType) {
            string returnFieldTypeName = "";
            try {
                switch (fieldType) {
                    case CPContentBaseClass.FieldTypeIdEnum.Boolean:
                        returnFieldTypeName = FieldTypeNameBoolean;
                        break;
                    case CPContentBaseClass.FieldTypeIdEnum.Currency:
                        returnFieldTypeName = FieldTypeNameCurrency;
                        break;
                    case CPContentBaseClass.FieldTypeIdEnum.Date:
                        returnFieldTypeName = FieldTypeNameDate;
                        break;
                    case CPContentBaseClass.FieldTypeIdEnum.File:
                        returnFieldTypeName = FieldTypeNameFile;
                        break;
                    case CPContentBaseClass.FieldTypeIdEnum.Float:
                        returnFieldTypeName = FieldTypeNameFloat;
                        break;
                    case CPContentBaseClass.FieldTypeIdEnum.FileImage:
                        returnFieldTypeName = FieldTypeNameImage;
                        break;
                    case CPContentBaseClass.FieldTypeIdEnum.Link:
                        returnFieldTypeName = FieldTypeNameLink;
                        break;
                    case CPContentBaseClass.FieldTypeIdEnum.ResourceLink:
                        returnFieldTypeName = FieldTypeNameResourceLink;
                        break;
                    case CPContentBaseClass.FieldTypeIdEnum.Integer:
                        returnFieldTypeName = FieldTypeNameInteger;
                        break;
                    case CPContentBaseClass.FieldTypeIdEnum.LongText:
                        returnFieldTypeName = FieldTypeNameLongText;
                        break;
                    case CPContentBaseClass.FieldTypeIdEnum.Lookup:
                        returnFieldTypeName = FieldTypeNameLookup;
                        break;
                    case CPContentBaseClass.FieldTypeIdEnum.MemberSelect:
                        returnFieldTypeName = FieldTypeNameMemberSelect;
                        break;
                    case CPContentBaseClass.FieldTypeIdEnum.Redirect:
                        returnFieldTypeName = FieldTypeNameRedirect;
                        break;
                    case CPContentBaseClass.FieldTypeIdEnum.ManyToMany:
                        returnFieldTypeName = FieldTypeNameManyToMany;
                        break;
                    case CPContentBaseClass.FieldTypeIdEnum.FileText:
                        returnFieldTypeName = FieldTypeNameTextFile;
                        break;
                    case CPContentBaseClass.FieldTypeIdEnum.FileCSS:
                        returnFieldTypeName = FieldTypeNameCSSFile;
                        break;
                    case CPContentBaseClass.FieldTypeIdEnum.FileXML:
                        returnFieldTypeName = FieldTypeNameXMLFile;
                        break;
                    case CPContentBaseClass.FieldTypeIdEnum.FileJavascript:
                        returnFieldTypeName = FieldTypeNameJavascriptFile;
                        break;
                    case CPContentBaseClass.FieldTypeIdEnum.Text:
                        returnFieldTypeName = FieldTypeNameText;
                        break;
                    case CPContentBaseClass.FieldTypeIdEnum.HTML:
                        returnFieldTypeName = FieldTypeNameHTML;
                        break;
                    case CPContentBaseClass.FieldTypeIdEnum.HTMLCode:
                        returnFieldTypeName = FieldTypeNameHTMLCode;
                        break;
                    case CPContentBaseClass.FieldTypeIdEnum.FileHTML:
                        returnFieldTypeName = FieldTypeNameHTMLFile;
                        break;
                    case CPContentBaseClass.FieldTypeIdEnum.FileHTMLCode:
                        returnFieldTypeName = FieldTypeNameHTMLCodeFile;
                        break;
                    default:
                        if (fieldType == CPContentBaseClass.FieldTypeIdEnum.AutoIdIncrement) {
                            returnFieldTypeName = "AutoIncrement";
                        } else if (fieldType == CPContentBaseClass.FieldTypeIdEnum.MemberSelect) {
                            returnFieldTypeName = "MemberSelect";
                        } else {
                            //
                            // If field type is ignored, call it a text field
                            //
                            returnFieldTypeName = FieldTypeNameText;
                        }
                        break;
                }
            } catch (Exception ex) {
                LogControllerX.logError(core, ex); // "Unexpected exception")
                throw;
            }
            return returnFieldTypeName;
        }
        //
        public static string getDefaultValue(CoreController core, string contentName, string fieldName) {
            string defaultValue = "";
            int contentId = ContentMetadataModel.getContentId(core, contentName);
            string SQL = "select defaultvalue from ccfields where name=" + DbControllerX.encodeSQLText(fieldName) + " and contentid=(" + contentId + ")";
            using (var csData = new CsModel(core)) {
                csData.openSql(SQL);
                if (csData.ok()) {
                    defaultValue = csData.getText("defaultvalue");
                }
                csData.close();
            }
            return defaultValue;
        }
    }
}
