
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using Contensive.Processor.Exceptions;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Globalization;
using System.Linq;
using static Contensive.Processor.Constants;
using static Contensive.Processor.Controllers.GenericController;
//
namespace Contensive.Processor.Models.Domain {
    //
    //====================================================================================================
    /// <summary>
    /// content definitions - meta data
    /// </summary>
    public class ContentMetadataModel : ICloneable {
        //
        //====================================================================================================
        /// <summary>
        /// constructor to setup defaults for fields required
        /// </summary>
        public ContentMetadataModel() {
            // set defaults, create methods require name, table
            active = true;
        }
        //
        //====================================================================================================
        /// <summary>
        /// index in content table
        /// </summary>
        public int id { get; set; }
        //
        /// <summary>
        /// Name of Content
        /// </summary>
        public string name { get; set; }
        //
        /// <summary>
        /// the name of the content table
        /// </summary>
        public string tableName { get; set; }
        //
        /// <summary>
        /// The name of the datasource that stores this content (the name of the database connection)
        /// </summary>
        public string dataSourceName { get; set; }
        //
        /// <summary>
        /// Allow adding records
        /// </summary>
        public bool allowAdd { get; set; }
        //
        /// <summary>
        /// Allow deleting records
        /// </summary>
        public bool allowDelete { get; set; }
        //
        /// <summary>
        /// deprecate - filter records from the datasource for this content
        /// </summary>
        public string whereClause { get; set; }
        //
        /// <summary>
        /// name of sort method to use as default for queries against this content
        /// </summary>
        public string defaultSortMethod { get; set; }
        //
        /// <summary>
        /// 
        /// </summary>
        public bool activeOnly { get; set; }
        //
        /// <summary>
        /// Only allow administrators to modify content
        /// </summary>
        public bool adminOnly { get; set; }
        //
        /// <summary>
        /// Only allow developers to modify content
        /// </summary>
        public bool developerOnly { get; set; }
        //
        /// <summary>
        /// String used to populate select boxes
        /// </summary>
        public string dropDownFieldList { get; set; }
        //
        /// <summary>
        /// Group of members who administer Workflow Authoring
        /// </summary>
        public string editorGroupName { get; set; }
        //
        /// <summary>
        /// 
        /// </summary>
        public int dataSourceId { get; set; }
        //
        /// <summary>
        /// 
        /// </summary>
        private string _dataSourceName { get; set; } = "";
        //
        /// <summary>
        /// deprecate - Field Name of the required "name" field
        /// </summary>
        public string aliasName { get; set; }
        //
        /// <summary>
        /// deprecate - Field Name of the required "id" field
        /// </summary>
        public string aliasId { get; set; }
        //
        /// <summary>
        /// deprecate - For admin edit page
        /// </summary>
        public bool allowTopicRules { get; set; }
        //
        /// <summary>
        /// deprecate - For admin edit page
        /// </summary>
        public bool allowContentTracking { get; set; }
        //
        /// <summary>
        /// deprecate - For admin edit page
        /// </summary>
        public bool allowCalendarEvents { get; set; }
        //
        /// <summary>
        /// 
        /// </summary>
        public bool dataChanged { get; set; }
        //
        /// <summary>
        /// deprecate - if any fields().changed, this is set true to
        /// </summary>
        public bool includesAFieldChange { get; set; }
        //
        /// <summary>
        /// when false, the content is not included in queries
        /// </summary>
        public bool active { get; set; }
        //
        /// <summary>
        /// deprecate
        /// </summary>
        public bool allowContentChildTool { get; set; }
        //
        /// <summary>
        /// deprecate
        /// </summary>
        public bool isModifiedSinceInstalled { get; set; }
        //
        /// <summary>
        /// icon for content
        /// </summary>
        public string iconLink { get; set; }
        //
        /// <summary>
        /// icon for content
        /// </summary>
        public int iconWidth { get; set; }
        //
        /// <summary>
        /// icon for content
        /// </summary>
        public int iconHeight { get; set; }
        //
        /// <summary>
        /// icon for content
        /// </summary>
        public int iconSprites { get; set; }
        //
        /// <summary>
        /// 
        /// </summary>
        public string guid { get; set; }
        //
        /// <summary>
        /// deprecate, true if this was installed as part of the base collection. replaced with isntalledByCollectionGuid
        /// </summary>
        public bool isBaseContent { get; set; }
        //
        /// <summary>
        /// the guid of the collection that installed this content
        /// </summary>
        public string installedByCollectionGuid { get; set; }
        //
        /// <summary>
        /// deprecate one day - domain model metadata calculates hasChild from this. If hasChild is false, contentcontrolid is ignored and queries are from the whole table
        /// </summary>
        public int parentId { get; set; }
        //
        /// <summary>
        /// consider deprecation - read from xml, used to set parentId
        /// </summary>
        public string parentName { get; set; }
        //
        /// <summary>
        /// string that changes if any record in Content Definition changes, in memory only
        /// </summary>
        public string timeStamp { get; set; }
        //
        /// <summary>
        /// fields for this content
        /// </summary>
        public Dictionary<string, Models.Domain.ContentFieldMetadataModel> fields { get; set; } = new Dictionary<string, Models.Domain.ContentFieldMetadataModel>();
        //
        /// <summary>
        /// metadata for admin site editing columns
        /// !!!!! changed to string because dotnet json cannot serialize an integer key
        /// </summary>
        public SortedList<string, MetaAdminColumnClass> adminColumns { get; set; } = new SortedList<string, MetaAdminColumnClass>();
        //
        /// <summary>
        /// consider deprection - string created from ParentIDs used to select records. If we eliminate parentId, then the whole table belongs to the content. This will speed queries and simplify concepts
        /// </summary>
        public string legacyContentControlCriteria { get; set; }
        //
        /// <summary>
        /// 
        /// </summary>
        public List<string> selectList { get; set; } = new List<string>();
        //
        /// <summary>
        /// Field list used in OpenCSContent calls (all active field definitions)
        /// </summary>
        public string selectCommaList { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// consider deprecating - list of child content definitions. Not needed if we deprecate parentid
        /// </summary>
        /// <param name="core"></param>
        /// <returns></returns>
        public List<int> childIdList(CoreController core) {
            if (_childIdList != null) { return _childIdList; }
            _childIdList = new List<int>();
            using DataTable dt = core.db.executeQuery("select id from cccontent where parentid=" + id);
            foreach (DataRow row in dt.Rows) { _childIdList.Add(encodeInteger(row[0])); }
            return _childIdList;
        }
        private List<int> _childIdList = null;
        //
        //====================================================================================================
        /// <summary>
        /// metadata for column definition
        /// </summary>
        //
        public class MetaAdminColumnClass {
            public string Name;
            public int Width;
            public int SortPriority;
            public int SortDirection;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Create metadata object from cache or database for provided contentId
        /// </summary>
        /// <param name="core"></param>
        /// <param name="content"></param>
        /// <param name="loadInvalidFields"></param>
        /// <param name="forceDbLoad"></param>
        /// <returns></returns>
        public static ContentMetadataModel create(CoreController core, ContentModel content, bool loadInvalidFields, bool forceDbLoad) {
            ContentMetadataModel result = null;
            try {
                if (content == null) { return null; }
                if ((!forceDbLoad) && (core.metaDataDictionary.ContainsKey(content.id.ToString()))) { return core.metaDataDictionary[content.id.ToString()]; }
                if (core.metaDataDictionary.ContainsKey(content.id.ToString())) {
                    //
                    // -- key is already there, remove it first                        
                    core.metaDataDictionary.Remove(content.id.ToString());
                }
                if (!forceDbLoad) {
                    result = getCache(core, content.id);
                }
                if (result == null) {
                    //
                    // load Db version
                    //
                    string sql = "SELECT "
                        + "c.ID"
                        + ", c.Name"
                        + ", c.name"
                        + ", c.AllowAdd"
                        + ", c.DeveloperOnly"
                        + ", c.AdminOnly"
                        + ", c.AllowDelete"
                        + ", c.ParentID"
                        + ", c.DefaultSortMethodID"
                        + ", c.DropDownFieldList"
                        + ", ContentTable.Name AS ContentTableName"
                        + ", ContentDataSource.Name AS ContentDataSourceName"
                        + ", '' AS AuthoringTableName"
                        + ", '' AS AuthoringDataSourceName"
                        + ", 0 AS AllowWorkflowAuthoring"
                        + ", c.AllowCalendarEvents as AllowCalendarEvents"
                        + ", ContentTable.DataSourceID"
                        + ", ccSortMethods.OrderByClause as DefaultSortMethod"
                        + ", ccGroups.Name as EditorGroupName"
                        + ", c.AllowContentTracking as AllowContentTracking"
                        + ", c.AllowTopicRules as AllowTopicRules"
                        + ", ccAddonCollections.ccguid as addonCollectionGuid"
                        + ", c.isBaseContent"
                        + "";
                    //
                    sql += ""
                        + " from ((((ccContent c"
                        + " left join ccTables AS ContentTable ON c.ContentTableId = ContentTable.ID)"
                        + " left join ccDataSources AS ContentDataSource ON ContentTable.DataSourceId = ContentDataSource.ID)"
                        + " left join ccSortMethods ON c.DefaultSortMethodId = ccSortMethods.ID)"
                        + " left join ccGroups ON c.EditorGroupId = ccGroups.ID)"
                        + " left join ccAddonCollections ON c.installedByCollectionId = ccAddonCollections.ID"
                        + " where (c.Active<>0)"
                        + " and(c.id=" + content.id.ToString() + ")";
                    using (DataTable dtContent = core.db.executeQuery(sql)) {
                        if (dtContent.Rows.Count == 0) {
                            //
                            // metadata not found
                            //
                        } else {
                            DataRow contentRow = dtContent.Rows[0];
                            string contentName = encodeText(GenericController.encodeText(contentRow[1])).Trim(' ');
                            string contentTablename = GenericController.encodeText(contentRow[10]);
                            string defaultSortMethod = GenericController.encodeText(contentRow[17]);
                            if (string.IsNullOrEmpty(defaultSortMethod)) {
                                defaultSortMethod = "name";
                            }
                            //
                            result = new Models.Domain.ContentMetadataModel {
                                fields = new Dictionary<string, Models.Domain.ContentFieldMetadataModel>(),
                                selectList = new List<string>(),
                                adminColumns = new SortedList<string, MetaAdminColumnClass>(),
                                name = contentName,
                                id = content.id,
                                allowAdd = GenericController.encodeBoolean(contentRow[3]),
                                developerOnly = GenericController.encodeBoolean(contentRow[4]),
                                adminOnly = GenericController.encodeBoolean(contentRow[5]),
                                allowDelete = GenericController.encodeBoolean(contentRow[6]),
                                parentId = GenericController.encodeInteger(contentRow[7]),
                                dropDownFieldList = GenericController.toUCase(GenericController.encodeText(contentRow[9])),
                                tableName = GenericController.encodeText(contentTablename),
                                dataSourceName = "default",
                                allowCalendarEvents = GenericController.encodeBoolean(contentRow[15]),
                                defaultSortMethod = defaultSortMethod,
                                editorGroupName = GenericController.encodeText(contentRow[18]),
                                allowContentTracking = GenericController.encodeBoolean(contentRow[19]),
                                allowTopicRules = GenericController.encodeBoolean(contentRow[20]),
                                activeOnly = true,
                                aliasId = "ID",
                                aliasName = "NAME",
                                installedByCollectionGuid = encodeText(contentRow[21]),
                                isBaseContent = encodeBoolean(contentRow[22])
                            };
                            //
                            // load parent metadata fields first so we can overlay the current metadata field
                            //
                            if (result.parentId <= 0) {
                                result.parentId = -1;
                            } else {
                                Models.Domain.ContentMetadataModel parentMetaData = create(core, result.parentId, loadInvalidFields, forceDbLoad);
                                if (parentMetaData == null) {
                                    LogController.logError(core, new GenericException("ContentMetadataModel error, loading content [" + content.id + ", " + content.name + "], parentId [" + result.parentId + "] but no parent content found."));
                                } else {
                                    foreach (var keyvaluepair in parentMetaData.fields) {
                                        Models.Domain.ContentFieldMetadataModel parentField = keyvaluepair.Value;
                                        Models.Domain.ContentFieldMetadataModel childField = new();
                                        childField = (Models.Domain.ContentFieldMetadataModel)parentField.Clone();
                                        childField.inherited = true;
                                        result.fields.Add(childField.nameLc.ToLowerInvariant(), childField);
                                        if (!((parentField.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.ManyToMany) || (parentField.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.Redirect))) {
                                            if (!result.selectList.Contains(parentField.nameLc)) {
                                                result.selectList.Add(parentField.nameLc);
                                            }
                                        }
                                    }
                                }
                            }
                            //
                            // ----- now load all the Content Definition Fields
                            //
                            sql = "SELECT"
                                + " f.DeveloperOnly"
                                + ",f.UniqueName"
                                + ",f.TextBuffered"
                                + ",f.Password"
                                + ",f.IndexColumn"
                                + ",f.IndexWidth"
                                + ",f.IndexSortPriority"
                                + ",f.IndexSortDirection"
                                + ",f.AdminOnly"
                                + ",f.SortOrder"
                                + ",f.EditSortPriority"
                                + ",f.ContentID"
                                + ",f.ID"
                                + ",f.Name"
                                + ",f.Required"
                                + ",f.Type"
                                + ",f.Caption"
                                + ",f.readonly"
                                + ",f.LookupContentID"
                                + ",f.RedirectContentID"
                                + ",f.RedirectPath"
                                + ",f.RedirectID"
                                + ",f.DefaultValue"
                                + ",'' as HelpMessageDeprecated"
                                + ",f.Active"
                                + ",f.HTMLContent"
                                + ",f.NotEditable"
                                + ",f.authorable"
                                + ",f.ManyToManyContentID"
                                + ",f.ManyToManyRuleContentID"
                                + ",f.ManyToManyRulePrimaryField"
                                + ",f.ManyToManyRuleSecondaryField"
                                + ",f.RSSTitleField"
                                + ",f.RSSDescriptionField"
                                + ",f.EditTab"
                                + ",f.Scramble"
                                + ",f.MemberSelectGroupID"
                                + ",f.LookupList"
                                + ",f.IsBaseField"
                                + ",f.InstalledByCollectionID"
                                + ",h.helpDefault"
                                + ",h.helpCustom"
                                + ""
                                + " from ((ccFields f"
                                + " left join ccContent c ON f.ContentId = c.ID)"
                                + " left join ccfieldHelp h on h.fieldid=f.id)"
                                + ""
                                + " where"
                                + " (c.ID Is not Null)"
                                + " and(c.Active<>0)"
                                + " and(c.ID=" + content.id + ")"
                                + ""
                                + "";
                            //
                            if (!loadInvalidFields) {
                                sql += ""
                                        + " and(f.active<>0)"
                                        + " and(f.Type<>0)"
                                        + " and(f.name <>'')"
                                        + "";
                            }
                            sql += ""
                                    + " order by"
                                    + " f.ContentID,f.EditTab,f.EditSortPriority"
                                    + "";
                            using (var dtFields = core.db.executeQuery(sql)) {
                                if (dtFields.Rows.Count == 0) {
                                    //
                                } else {
                                    List<string> usedFields = new();
                                    foreach (DataRow fieldRow in dtFields.Rows) {
                                        string fieldName = GenericController.encodeText(fieldRow[13]);
                                        int fieldId = GenericController.encodeInteger(fieldRow[12]);
                                        string fieldNameLower = fieldName.ToLowerInvariant();
                                        bool skipDuplicateField = false;
                                        if (usedFields.Contains(fieldNameLower)) {
                                            //
                                            // this is a dup field for this content (not accounting for possibleinherited field) - keep the one with the lowest id
                                            //
                                            if (result.fields[fieldNameLower].id < fieldId) {
                                                //
                                                // this new field has a higher id, skip it
                                                //
                                                skipDuplicateField = true;
                                            } else {
                                                //
                                                // this new field has a lower id, remove the other one
                                                //
                                                result.fields.Remove(fieldNameLower);
                                            }
                                        }
                                        if (!skipDuplicateField) {
                                            //
                                            // only add the first field found, ordered by id
                                            //
                                            if (result.fields.ContainsKey(fieldNameLower)) {
                                                //
                                                // remove inherited field and replace it with field from this table
                                                //
                                                result.fields.Remove(fieldNameLower);
                                            }
                                            Models.Domain.ContentFieldMetadataModel field = new();
                                            int fieldIndexColumn = -1;
                                            CPContentBaseClass.FieldTypeIdEnum fieldTypeId = (CPContentBaseClass.FieldTypeIdEnum)GenericController.encodeInteger(fieldRow[15]);
                                            if (GenericController.encodeText(fieldRow[4]) != "") {
                                                fieldIndexColumn = GenericController.encodeInteger(fieldRow[4]);
                                            }
                                            //
                                            // translate htmlContent to fieldtypehtml
                                            //   this is also converted in upgrade, daily housekeep, addon install
                                            //
                                            bool fieldHtmlContent = GenericController.encodeBoolean(fieldRow[25]);
                                            if (fieldHtmlContent) {
                                                if (fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.LongText) {
                                                    fieldTypeId = CPContentBaseClass.FieldTypeIdEnum.HTMLCode;
                                                } else if (fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.FileText) {
                                                    fieldTypeId = CPContentBaseClass.FieldTypeIdEnum.FileHTMLCode;
                                                }
                                            }
                                            field.active = GenericController.encodeBoolean(fieldRow[24]);
                                            field.adminOnly = GenericController.encodeBoolean(fieldRow[8]);
                                            field.authorable = GenericController.encodeBoolean(fieldRow[27]);
                                            field.blockAccess = GenericController.encodeBoolean(fieldRow[38]);
                                            field.caption = GenericController.encodeText(fieldRow[16]);
                                            field.dataChanged = false;
                                            //.Changed
                                            field.contentId = content.id;
                                            field.defaultValue = GenericController.encodeText(fieldRow[22]);
                                            field.developerOnly = GenericController.encodeBoolean(fieldRow[0]);
                                            field.editSortPriority = GenericController.encodeInteger(fieldRow[10]);
                                            field.editTabName = GenericController.encodeText(fieldRow[34]);
                                            field.fieldTypeId = fieldTypeId;
                                            field.htmlContent = fieldHtmlContent;
                                            field.id = fieldId;
                                            field.indexColumn = fieldIndexColumn;
                                            field.indexSortDirection = GenericController.encodeInteger(fieldRow[7]);
                                            field.indexSortOrder = GenericController.encodeInteger(fieldRow[6]);
                                            field.indexWidth = GenericController.encodeText(GenericController.encodeInteger(GenericController.encodeText(fieldRow[5]).Replace("%", "")));
                                            field.inherited = false;
                                            field.installedByCollectionGuid = GenericController.encodeText(fieldRow[39]);
                                            field.isBaseField = GenericController.encodeBoolean(fieldRow[38]);
                                            field.isModifiedSinceInstalled = false;
                                            field.lookupContentId = GenericController.encodeInteger(fieldRow[18]);
                                            //.lookupContentName = ""
                                            field.lookupList = GenericController.encodeText(fieldRow[37]);
                                            field.manyToManyContentId = GenericController.encodeInteger(fieldRow[28]);
                                            field.manyToManyRuleContentId = GenericController.encodeInteger(fieldRow[29]);
                                            field.manyToManyRulePrimaryField = GenericController.encodeText(fieldRow[30]);
                                            field.manyToManyRuleSecondaryField = GenericController.encodeText(fieldRow[31]);
                                            field.memberSelectGroupId_set(core, GenericController.encodeInteger(fieldRow[36]));
                                            field.nameLc = fieldNameLower;
                                            field.notEditable = GenericController.encodeBoolean(fieldRow[26]);
                                            field.password = GenericController.encodeBoolean(fieldRow[3]);
                                            field.readOnly = GenericController.encodeBoolean(fieldRow[17]);
                                            field.redirectContentId = GenericController.encodeInteger(fieldRow[19]);
                                            //.RedirectContentName(core) = ""
                                            field.redirectId = GenericController.encodeText(fieldRow[21]);
                                            field.redirectPath = GenericController.encodeText(fieldRow[20]);
                                            field.required = GenericController.encodeBoolean(fieldRow[14]);
                                            field.rssTitleField = GenericController.encodeBoolean(fieldRow[32]);
                                            field.rssDescriptionField = GenericController.encodeBoolean(fieldRow[33]);
                                            field.scramble = GenericController.encodeBoolean(fieldRow[35]);
                                            field.textBuffered = GenericController.encodeBoolean(fieldRow[2]);
                                            field.uniqueName = GenericController.encodeBoolean(fieldRow[1]);
                                            //.ValueVariant
                                            //
                                            field.helpCustom = GenericController.encodeText(fieldRow[41]);
                                            field.helpDefault = GenericController.encodeText(fieldRow[40]);
                                            if (string.IsNullOrEmpty(field.helpCustom)) {
                                                field.helpMessage = field.helpDefault;
                                            } else {
                                                field.helpMessage = field.helpCustom;
                                            }
                                            field.helpChanged = false;
                                            result.fields.Add(fieldNameLower, field);
                                            if ((field.fieldTypeId != CPContentBaseClass.FieldTypeIdEnum.ManyToMany) && (field.fieldTypeId != CPContentBaseClass.FieldTypeIdEnum.Redirect) && (!result.selectList.Contains(fieldNameLower))) {
                                                //
                                                // add only fields that can be selected
                                                result.selectList.Add(fieldNameLower);
                                            }
                                        }
                                    }
                                    result.selectCommaList = string.Join(",", result.selectList);
                                }
                            }
                            //
                            // ----- Create the LegacyContentControlCriteria. For compatibility, if support=false, return (1=1)
                            result.legacyContentControlCriteria = (result.parentId <= 0) ? "(1=1)" : getLegacyContentControlCriteria(core, result.id, result.tableName, result.dataSourceName, new List<int> { result.parentId });
                            //
                            create_setAdminColumns(core, result);
                        }
                    }
                    setCache(core, content.id, result);
                }
                core.metaDataDictionary.Add(content.id.ToString(), result);
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
            return result;
        }
        //
        public static ContentMetadataModel create(CoreController core, ContentModel content, bool loadInvalidFields) => create(core, content, loadInvalidFields, false);
        //
        public static ContentMetadataModel create(CoreController core, ContentModel content) => create(core, content, false, false);
        //
        //====================================================================================================
        /// <summary>
        /// Create metadata object from cache or database for provided contentId
        /// </summary>
        /// <param name="core"></param>
        /// <param name="contentGuid"></param>
        /// <param name="loadInvalidFields"></param>
        /// <param name="forceDbLoad"></param>
        /// <returns></returns>
        public static ContentMetadataModel create(CoreController core, string contentGuid, bool loadInvalidFields, bool forceDbLoad) {
            var content = ContentModel.create<ContentModel>(core.cpParent, contentGuid);
            if (content == null) { return null; }
            return create(core, content, loadInvalidFields, forceDbLoad);
        }
        //
        public static ContentMetadataModel create(CoreController core, string contentGuid, bool loadInvalidFields) => create(core, contentGuid, loadInvalidFields, false);
        //
        public static ContentMetadataModel create(CoreController core, string contentGuid) => create(core, contentGuid, false, false);
        //
        //====================================================================================================
        /// <summary>
        /// Create metadata object from cache or database for provided contentId
        /// </summary>
        /// <param name="core"></param>
        /// <param name="contentId"></param>
        /// <param name="loadInvalidFields"></param>
        /// <param name="forceDbLoad"></param>
        /// <returns></returns>
        public static ContentMetadataModel create(CoreController core, int contentId, bool loadInvalidFields, bool forceDbLoad) {
            var content = ContentModel.create<ContentModel>(core.cpParent, contentId);
            if (content == null) { return null; }
            return create(core, content, loadInvalidFields, forceDbLoad);
        }
        //
        public static ContentMetadataModel create(CoreController core, int contentId, bool loadInvalidFields) => create(core, contentId, loadInvalidFields, false);
        //
        public static ContentMetadataModel create(CoreController core, int contentId) => create(core, contentId, false, false);
        //   
        //====================================================================================================
        /// <summary>
        /// get metadata from content name. If the metadata is not found, return nothing.
        /// </summary>
        /// <param name="contentName"></param>
        /// <returns></returns>
        public static ContentMetadataModel createByUniqueName(CoreController core, string contentName, bool loadInvalidFields, bool forceDbLoad) {
            var content = DbBaseModel.createByUniqueName<ContentModel>(core.cpParent, contentName);
            if (content == null) { return null; }
            return create(core, content, loadInvalidFields, forceDbLoad);
        }
        //
        public static ContentMetadataModel createByUniqueName(CoreController core, string contentName, bool loadInvalidFields) => createByUniqueName(core, contentName, loadInvalidFields, false);
        //
        public static ContentMetadataModel createByUniqueName(CoreController core, string contentName) => createByUniqueName(core, contentName, false, false);
        //
        //========================================================================
        /// <summary>
        /// Calculates the query criteria for a content with parentId set non-zero. DO NOT CALL if parentId is not 0.
        /// Dig into Content Definition Records and create an SQL Criteria statement for parent-child relationships.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="contentId"></param>
        /// <param name="contentTableName"></param>
        /// <param name="contentDAtaSourceName"></param>
        /// <param name="parentIdList"></param>
        /// <returns></returns>
        private static string getLegacyContentControlCriteria(CoreController core, int contentId, string contentTableName, string contentDAtaSourceName, List<int> parentIdList) {
            try {
                string returnCriteria = "(1=0)";
                if (contentId >= 0) {
                    if (!parentIdList.Contains(contentId)) {
                        returnCriteria = "";
                        //
                        // -- first contentid in list, include contentid 0
                        if (parentIdList.Count == 0) returnCriteria += "(" + contentTableName + ".contentcontrolId=0)or";
                        parentIdList.Add(contentId);
                        //
                        // -- add this content id to the list
                        returnCriteria += "(" + contentTableName + ".contentcontrolId=" + contentId + ")";
                        foreach (var childContent in ContentModel.createList<ContentModel>(core.cpParent, "(parentid=" + contentId + ")")) {
                            returnCriteria += "or" + getLegacyContentControlCriteria(core, childContent.id, contentTableName, contentDAtaSourceName, parentIdList);
                        }
                        parentIdList.Remove(contentId);
                        returnCriteria = "(" + returnCriteria + ")";
                    }
                }
                return returnCriteria;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// setup the admin column model for the create methods
        /// </summary>
        /// <param name="core"></param>
        /// <param name="metaData"></param>
        private static void create_setAdminColumns(CoreController core, ContentMetadataModel metaData) {
            try {
                if (metaData.id > 0) {
                    int cnt = 0;
                    int FieldWidthTotal = 0;
                    MetaAdminColumnClass adminColumn = null;
                    foreach (KeyValuePair<string, Models.Domain.ContentFieldMetadataModel> keyValuePair in metaData.fields) {
                        ContentFieldMetadataModel field = keyValuePair.Value;
                        bool FieldActive = field.active;
                        int FieldWidth = GenericController.encodeInteger(field.indexWidth);
                        if (FieldActive && (FieldWidth > 0)) {
                            FieldWidthTotal += FieldWidth;
                            adminColumn = new MetaAdminColumnClass {
                                Name = field.nameLc,
                                SortDirection = field.indexSortDirection,
                                SortPriority = GenericController.encodeInteger(field.indexSortOrder),
                                Width = FieldWidth
                            };
                            FieldWidthTotal += adminColumn.Width;
                            string key = (cnt + (adminColumn.SortPriority * 1000)).ToString().PadLeft(6, '0');
                            metaData.adminColumns.Add(key, adminColumn);
                        }
                        cnt += 1;
                    }
                    //
                    // Force the Name field as the only column
                    if (metaData.fields.Count > 0) {
                        if (metaData.adminColumns.Count == 0) {
                            //
                            // Force the Name field as the only column
                            //
                            if (metaData.fields.ContainsKey("name")) {
                                adminColumn = new MetaAdminColumnClass {
                                    Name = "Name",
                                    SortDirection = 1,
                                    SortPriority = 1,
                                    Width = 100
                                };
                                FieldWidthTotal += adminColumn.Width;
                                string key = ((1000)).ToString().PadLeft(6, '0');
                                metaData.adminColumns.Add(key, adminColumn);
                            }
                        }
                        //
                        // Normalize the column widths
                        //
                        foreach (var keyvaluepair in metaData.adminColumns) {
                            adminColumn = keyvaluepair.Value;
                            adminColumn.Width = encodeInteger(100 * ((double)adminColumn.Width / (double)FieldWidthTotal));
                        }
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Get cache key for a metadata object in cache
        /// </summary>
        /// <param name="contentId"></param>
        /// <returns></returns>
        public static CacheKeyHashClass createKeyHash(CoreController core, int contentId) {
            return core.cache.createKeyHash("metadata-" + contentId.ToString());
        }
        //
        //====================================================================================================
        /// <summary>
        /// invalidate the metadata record for a given contentid
        /// </summary>
        /// <param name="core"></param>
        /// <param name="contentId"></param>
        public static void invalidateCache(CoreController core, int contentId) {
            core.cache.invalidate(createKeyHash(core, contentId));
        }
        //
        //====================================================================================================
        /// <summary>
        /// update cache for a metadata model
        /// </summary>
        /// <param name="core"></param>
        /// <param name="contentId"></param>
        /// <param name="metaData"></param>
        public static void setCache(CoreController core, int contentId, ContentMetadataModel metaData) {
            core.cache.storeObject(createKeyHash(core, contentId), metaData);
        }
        //
        //====================================================================================================
        /// <summary>
        /// get a cache version of a metadata model
        /// </summary>
        /// <param name="core"></param>
        /// <param name="contentId"></param>
        /// <returns></returns>
        public static ContentMetadataModel getCache(CoreController core, int contentId) {
            ContentMetadataModel result = null;
            try {
                try {
                    result = core.cache.getObject<Models.Domain.ContentMetadataModel>(createKeyHash(core, contentId));
                } catch (Exception ex) {
                    LogController.logError(core, ex);
                }
            } catch (Exception) { }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// get content id from content name. In model not controller because controller calls model, not the other wau (see constants)
        /// </summary>
        /// <param name="contentName"></param>
        /// <returns></returns>
        public static int getContentId(CoreController core, string contentName) {
            try {
                if (string.IsNullOrWhiteSpace(contentName)) return 0;
                var nameLower = contentName.Trim().ToLowerInvariant();
                if (core.contentNameIdDictionary.ContainsKey(nameLower)) { return core.contentNameIdDictionary[nameLower]; }
                ContentModel content = DbBaseModel.createByUniqueName<ContentModel>(core.cpParent, contentName);
                if (content == null) { return 0; }
                core.contentNameIdDictionary.Add(nameLower, content.id);
                return content.id;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// return the meta field object specified in the fieldname. If it does not exist, return null
        /// </summary>
        /// <param name="core"></param>
        /// <param name="meta"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static ContentFieldMetadataModel getField(CoreController core, ContentMetadataModel meta, string fieldName) {
            if (meta == null) return null;
            if (!meta.fields.ContainsKey(fieldName.ToLower(CultureInfo.InvariantCulture))) return null;
            return meta.fields[fieldName.ToLower(CultureInfo.InvariantCulture)];
        }
        //
        //====================================================================================================
        /// <summary>
        /// create a clone of this object. Used for cases like cs copy
        /// </summary>
        /// <returns></returns>
        public Object Clone() {
            return MemberwiseClone();
        }
        //
        // ====================================================================================================================
        /// <summary>
        /// Verify the metadata field (in current instance, table field exists). Add the metadata field if needed (to ccField) and add the column to the Dbtable it represents
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="ContentName"></param>
        /// <param name="fieldMetadata"></param>
        /// <param name="blockCacheClear"></param>
        /// <param name="logMsgContext">A message to be added to log entries to help understand the context of the issue.</param>
        /// <returns></returns>
        public void verifyContentField(CoreController core, ContentFieldMetadataModel fieldMetadata, bool blockCacheClear, string logMsgContext) {
            try {
                if (fieldMetadata == null) {
                    throw (new GenericException("Could not create Field for content [" + name + "] because the field metadata is not valid."));
                }
                if (string.IsNullOrWhiteSpace(fieldMetadata.nameLc)) {
                    throw (new GenericException("Could not create Field for content [" + name + "] because the field metadata has a blank name."));
                }
                if (fieldMetadata.fieldTypeId <= 0) {
                    throw (new GenericException("Could not create Field [" + fieldMetadata.nameLc + "] for content [" + name + "] because the field type [" + fieldMetadata.fieldTypeId + "] is not valid."));
                }
                bool RecordIsBaseField = false;
                var contentFieldList = DbBaseModel.createList<ContentFieldModel>(core.cpParent, "(ContentID=" + DbController.encodeSQLNumber(id) + ")and(name=" + DbController.encodeSQLText(fieldMetadata.nameLc) + ")");
                if (contentFieldList.Count > 0) {
                    fieldMetadata.id = contentFieldList.First().id;
                    RecordIsBaseField = contentFieldList.First().isBaseField;
                }
                //
                // check if this is a non-base field updating a base field
                if ((!fieldMetadata.isBaseField) && (RecordIsBaseField)) {
                    //
                    // This update is not allowed
                    LogController.logDebug(core, "Warning, updating base field from non-base collection, context [" + logMsgContext + "], content [" + name + "], field [" + fieldMetadata.nameLc + "]");
                }
                using var db = new DbController(core, dataSourceName);
                //
                // -- Get the installedByCollectionId
                int InstalledByCollectionId = 0;
                if (!string.IsNullOrEmpty(fieldMetadata.installedByCollectionGuid)) {
                    var addonCollection = AddonCollectionModel.create<AddonCollectionModel>(core.cpParent, fieldMetadata.installedByCollectionGuid);
                    if (addonCollection != null) {
                        InstalledByCollectionId = addonCollection.id;
                    }
                }
                //
                // Create or update the Table Field
                if (fieldMetadata.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.Redirect) {
                    //
                    // Redirect Field
                } else if (fieldMetadata.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.ManyToMany) {
                    //
                    // ManyToMany Field
                } else {
                    //
                    // All other fields
                    db.createSQLTableField(tableName, fieldMetadata.nameLc, fieldMetadata.fieldTypeId);
                }
                //
                // create or update the field
                var sqlList = new NameValueCollection {
                            { "ACTIVE", DbController.encodeSQLBoolean(fieldMetadata.active) },
                            { "MODIFIEDBY", DbController.encodeSQLNumber(SystemMemberId) },
                            { "MODIFIEDDATE", DbController.encodeSQLDate(core.dateTimeNowMockable) },
                            { "TYPE", DbController.encodeSQLNumber((int)fieldMetadata.fieldTypeId) },
                            { "CAPTION", DbController.encodeSQLText(fieldMetadata.caption) },
                            { "ReadOnly", DbController.encodeSQLBoolean(fieldMetadata.readOnly) },
                            { "REQUIRED", DbController.encodeSQLBoolean(fieldMetadata.required) },
                            { "TEXTBUFFERED", DbController.SQLFalse },
                            { "PASSWORD", DbController.encodeSQLBoolean(fieldMetadata.password) },
                            { "EDITSORTPRIORITY", DbController.encodeSQLNumber(fieldMetadata.editSortPriority) },
                            { "ADMINONLY", DbController.encodeSQLBoolean(fieldMetadata.adminOnly) },
                            { "DEVELOPERONLY", DbController.encodeSQLBoolean(fieldMetadata.developerOnly) },
                            { "CONTENTCONTROLID", DbController.encodeSQLNumber(ContentMetadataModel.getContentId(core, "Content Fields")) },
                            { "DefaultValue", DbController.encodeSQLText(fieldMetadata.defaultValue) },
                            { "HTMLCONTENT", DbController.encodeSQLBoolean(fieldMetadata.htmlContent) },
                            { "NOTEDITABLE", DbController.encodeSQLBoolean(fieldMetadata.notEditable) },
                            { "AUTHORABLE", DbController.encodeSQLBoolean(fieldMetadata.authorable) },
                            { "INDEXCOLUMN", DbController.encodeSQLNumber(fieldMetadata.indexColumn) },
                            { "INDEXWIDTH", DbController.encodeSQLText(fieldMetadata.indexWidth) },
                            { "INDEXSORTPRIORITY", DbController.encodeSQLNumber(fieldMetadata.indexSortOrder) },
                            { "REDIRECTID", DbController.encodeSQLText(fieldMetadata.redirectId) },
                            { "REDIRECTPATH", DbController.encodeSQLText(fieldMetadata.redirectPath) },
                            { "UNIQUENAME", DbController.encodeSQLBoolean(fieldMetadata.uniqueName) },
                            { "RSSTITLEFIELD", DbController.encodeSQLBoolean(fieldMetadata.rssTitleField) },
                            { "RSSDESCRIPTIONFIELD", DbController.encodeSQLBoolean(fieldMetadata.rssDescriptionField) },
                            { "MEMBERSELECTGROUPID", DbController.encodeSQLNumber(fieldMetadata.memberSelectGroupId_get(core, name, fieldMetadata.nameLc)) },
                            { "installedByCollectionId", DbController.encodeSQLNumber(InstalledByCollectionId) },
                            { "EDITTAB", DbController.encodeSQLText(fieldMetadata.editTabName) },
                            { "SCRAMBLE", DbController.encodeSQLBoolean(false) },
                            { "ISBASEFIELD", DbController.encodeSQLBoolean(fieldMetadata.isBaseField) },
                            { "LOOKUPLIST", DbController.encodeSQLText(fieldMetadata.lookupList) }
                        };
                //
                // -- conditional fields
                switch (fieldMetadata.fieldTypeId) {
                    case CPContentBaseClass.FieldTypeIdEnum.Lookup:
                        //
                        // -- lookup field
                        //
                        string LookupContentName = fieldMetadata.get_lookupContentName(core);
                        int LookupContentId = 0;
                        if (!string.IsNullOrEmpty(LookupContentName)) {
                            LookupContentId = ContentMetadataModel.getContentId(core, LookupContentName);
                            if (LookupContentId <= 0) {
                                string errMsg = "Could not create lookup field [" + fieldMetadata.nameLc + "] for content definition [" + name + "] because no content definition was found For lookup-content [" + LookupContentName + "].";
                                Logger.Error(LogController.processLogMessage(core, errMsg, true));
                            }
                        }
                        sqlList.Add("LOOKUPCONTENTID", DbController.encodeSQLNumber(LookupContentId));
                        break;
                    case CPContentBaseClass.FieldTypeIdEnum.ManyToMany:
                        //
                        // -- many-to-many field
                        //
                        string ManyToManyContent = fieldMetadata.get_manyToManyContentName(core);
                        if (!string.IsNullOrEmpty(ManyToManyContent)) {
                            int ManyToManyContentId = getContentId(core, ManyToManyContent);
                            if (ManyToManyContentId <= 0) {
                                string errMsg = "Could not create many-to-many field [" + fieldMetadata.nameLc + "] for [" + name + "] because no content definition was found For many-to-many-content [" + ManyToManyContent + "].";
                                Logger.Error(LogController.processLogMessage(core, errMsg, true));

                            }
                            sqlList.Add("MANYTOMANYCONTENTID", DbController.encodeSQLNumber(ManyToManyContentId));
                        }
                        //
                        string ManyToManyRuleContent = fieldMetadata.get_manyToManyRuleContentName(core);
                        if (!string.IsNullOrEmpty(ManyToManyRuleContent)) {
                            int ManyToManyRuleContentId = getContentId(core, ManyToManyRuleContent);
                            if (ManyToManyRuleContentId <= 0) {
                                string errMsg = "Could not create many-to-many field [" + fieldMetadata.nameLc + "] for [" + name + "] because no content definition was found For many-to-many-rule-content [" + ManyToManyRuleContent + "].";
                                Logger.Error(LogController.processLogMessage(core, errMsg, true));
                            }
                            sqlList.Add("MANYTOMANYRULECONTENTID", DbController.encodeSQLNumber(ManyToManyRuleContentId));
                        }
                        sqlList.Add("MANYTOMANYRULEPRIMARYFIELD", DbController.encodeSQLText(fieldMetadata.manyToManyRulePrimaryField));
                        sqlList.Add("MANYTOMANYRULESECONDARYFIELD", DbController.encodeSQLText(fieldMetadata.manyToManyRuleSecondaryField));
                        break;
                    case CPContentBaseClass.FieldTypeIdEnum.Redirect:
                        //
                        // -- redirect field
                        string RedirectContentName = fieldMetadata.get_redirectContentName(core);
                        int RedirectContentId = 0;
                        if (!string.IsNullOrEmpty(RedirectContentName)) {
                            RedirectContentId = getContentId(core, RedirectContentName);
                            if (RedirectContentId <= 0) {
                                string errMsg = "Could not create redirect field [" + fieldMetadata.nameLc + "] for Content Definition [" + name + "] because no content definition was found For redirect-content [" + RedirectContentName + "].";
                                Logger.Error(LogController.processLogMessage(core, errMsg, true));
                            }
                        }
                        sqlList.Add("REDIRECTCONTENTID", DbController.encodeSQLNumber(RedirectContentId));
                        break;
                }
                //
                if (fieldMetadata.id == 0) {

                    sqlList.Add("NAME", DbController.encodeSQLText(fieldMetadata.nameLc));
                    sqlList.Add("CONTENTID", DbController.encodeSQLNumber(id));
                    sqlList.Add("CREATEKEY", "0");
                    sqlList.Add("DATEADDED", DbController.encodeSQLDate(core.dateTimeNowMockable));
                    sqlList.Add("CREATEDBY", DbController.encodeSQLNumber(SystemMemberId));
                    fieldMetadata.id = db.insertGetId("ccFields", 0);
                    //
                    if (!blockCacheClear) {
                        core.cache.invalidateAll();
                        core.clearMetaData();
                    }
                }
                if (fieldMetadata.id == 0) {
                    throw (new GenericException("Could not create Field [" + fieldMetadata.nameLc + "] because insert into ccfields failed."));
                }
                db.update("ccFields", "ID=" + fieldMetadata.id, sqlList);
                ContentFieldModel.invalidateCacheOfRecord<ContentFieldModel>(core.cpParent, fieldMetadata.id);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //
        //
        //========================================================================
        /// <summary>
        /// Verify a metadata entry (in ccContent) and return the id. If it does not exist, it is added with default values and the associated real tables and fields are verified
        /// </summary>
        /// <param name="core"></param>
        /// <param name="contentMetadata"></param>
        /// <returns></returns>
        public static int verifyContent_returnId(CoreController core, ContentMetadataModel contentMetadata, string logMsgContext) {
            try {
                //
                logMsgContext += ", verifying content [" + contentMetadata.name + "]";
                //
                if (string.IsNullOrWhiteSpace(contentMetadata.name)) { throw new GenericException("Content name can not be blank"); }
                if (string.IsNullOrWhiteSpace(contentMetadata.tableName)) { throw new GenericException("Content table name can not be blank"); }
                using (var db = new DbController(core, contentMetadata.dataSourceName)) {
                    //
                    // -- verify table
                    db.createSQLTable(contentMetadata.tableName);
                    //
                    // get contentId, guid, IsBaseContent
                    var content = DbBaseModel.createByUniqueName<ContentModel>(core.cpParent, contentMetadata.name);
                    if (content == null) {
                        content = ContentModel.addDefault<ContentModel>(core.cpParent, ContentMetadataModel.getDefaultValueDict(core, contentMetadata.name));
                        content.name = contentMetadata.name;
                        content.save(core.cpParent);
                    }
                    contentMetadata.id = content.id;
                    string contentGuid = content.ccguid;
                    bool ContentIsBaseContent = content.isBaseContent;
                    int ContentIDofContent = ContentMetadataModel.getContentId(core, "content");
                    //
                    // get parentId
                    int parentId = 0;
                    if (!string.IsNullOrEmpty(contentMetadata.parentName)) {
                        var parentContent = DbBaseModel.createByUniqueName<ContentModel>(core.cpParent, contentMetadata.parentName);
                        if (parentContent != null) { parentId = parentContent.id; }
                    }
                    //
                    // get InstalledByCollectionID
                    int InstalledByCollectionId = 0;
                    var collection = AddonCollectionModel.create<AddonCollectionModel>(core.cpParent, contentMetadata.installedByCollectionGuid);
                    if (collection != null) { InstalledByCollectionId = collection.id; }
                    //
                    // Get the table object for this content metadata, create one if missing
                    var table = DbBaseModel.createByUniqueName<TableModel>(core.cpParent, contentMetadata.tableName);
                    if (table == null) {
                        //
                        // -- table model not found, create it - only name and datasource matter
                        var tableMetaData = ContentMetadataModel.createByUniqueName(core, "tables");
                        if (tableMetaData == null) {
                            //
                            // -- table metadata not fouond, create without defaults
                            table = TableModel.addEmpty<TableModel>(core.cpParent);
                        } else {
                            //
                            // -- create model with table metadata defaults
                            table = TableModel.addDefault<TableModel>(core.cpParent, ContentMetadataModel.getDefaultValueDict(core, tableMetaData.name));
                        }
                        table.name = contentMetadata.tableName;
                        if (!DataSourceModel.isDataSourceDefault(contentMetadata.dataSourceName)) {
                            //
                            // -- is not the default datasource, open a datasource model for it to get the id
                            var dataSource = DbBaseModel.createByUniqueName<DataSourceModel>(core.cpParent, contentMetadata.dataSourceName);
                            if (dataSource == null) {
                                //
                                // -- datasource record does not exist, create it now
                                dataSource = DataSourceModel.addEmpty<DataSourceModel>(core.cpParent);
                                dataSource.name = contentMetadata.dataSourceName;
                                dataSource.save(core.cpParent);
                            }
                        }
                        table.save(core.cpParent);
                        content.contentTableId = table.id;
                        content.authoringTableId = table.id;
                        content.save(core.cpParent);
                    }
                    //
                    // sortmethod - First try lookup by name
                    int defaultSortMethodId = 0;
                    if (!string.IsNullOrEmpty(contentMetadata.defaultSortMethod)) {
                        var sortMethod = DbBaseModel.createByUniqueName<SortMethodModel>(core.cpParent, contentMetadata.defaultSortMethod);
                        if (sortMethod != null) { defaultSortMethodId = sortMethod.id; }
                    }
                    if (defaultSortMethodId == 0) {
                        //
                        // fallback - maybe they put the orderbyclause in (common mistake)
                        var sortMethodList = DbBaseModel.createList<SortMethodModel>(core.cpParent, "(OrderByClause=" + DbController.encodeSQLText(contentMetadata.defaultSortMethod) + ")and(active<>0)", "id");
                        if (sortMethodList.Count() > 0) { defaultSortMethodId = sortMethodList.First().id; }
                    }

                    //
                    // ----- update record
                    //
                    var sqlList = new NameValueCollection {
                        { "name", DbController.encodeSQLText(contentMetadata.name) },
                        { "CREATEKEY", "0" },
                        { "active", DbController.encodeSQLBoolean(contentMetadata.active) },
                        { "contentControlId", DbController.encodeSQLNumber(ContentIDofContent) },
                        { "AllowAdd", DbController.encodeSQLBoolean(contentMetadata.allowAdd) },
                        { "AllowDelete", DbController.encodeSQLBoolean(contentMetadata.allowDelete) },
                        { "AllowWorkflowAuthoring", DbController.encodeSQLBoolean(false) },
                        { "DeveloperOnly", DbController.encodeSQLBoolean(contentMetadata.developerOnly) },
                        { "AdminOnly", DbController.encodeSQLBoolean(contentMetadata.adminOnly) },
                        { "ParentID", DbController.encodeSQLNumber(parentId) },
                        { "DefaultSortMethodID", DbController.encodeSQLNumber(defaultSortMethodId) },
                        { "DropDownFieldList", DbController.encodeSQLText(encodeEmpty(contentMetadata.dropDownFieldList, "Name")) },
                        { "ContentTableID", DbController.encodeSQLNumber(table.id) },
                        { "AuthoringTableID", DbController.encodeSQLNumber(table.id) },
                        { "ModifiedDate", DbController.encodeSQLDate(core.dateTimeNowMockable) },
                        { "CreatedBy", DbController.encodeSQLNumber(SystemMemberId) },
                        { "ModifiedBy", DbController.encodeSQLNumber(SystemMemberId) },
                        { "AllowCalendarEvents", DbController.encodeSQLBoolean(contentMetadata.allowCalendarEvents) },
                        { "AllowContentTracking", DbController.encodeSQLBoolean(contentMetadata.allowContentTracking) },
                        { "AllowTopicRules", DbController.encodeSQLBoolean(contentMetadata.allowTopicRules) },
                        { "AllowContentChildTool", DbController.encodeSQLBoolean(contentMetadata.allowContentChildTool) },
                        { "IconLink", DbController.encodeSQLText(encodeEmpty(contentMetadata.iconLink, "")) },
                        { "IconHeight", DbController.encodeSQLNumber(contentMetadata.iconHeight) },
                        { "IconWidth", DbController.encodeSQLNumber(contentMetadata.iconWidth) },
                        { "IconSprites", DbController.encodeSQLNumber(contentMetadata.iconSprites) },
                        { "installedByCollectionid", DbController.encodeSQLNumber(InstalledByCollectionId) },
                        { "isBaseContent", DbController.encodeSQLBoolean(contentMetadata.isBaseContent) }
                    };
                    db.update("ccContent", "ID=" + contentMetadata.id, sqlList);
                    ContentModel.invalidateCacheOfRecord<ContentModel>(core.cpParent, contentMetadata.id);
                    //
                    // -- reload metadata
                    contentMetadata = create(core, contentMetadata.id, false, true);
                    //
                    // Verify Core Content Definition Fields
                    if (parentId < 1) {
                        //
                        // metadata does not inherit its fields, create what is needed for a non-inherited metadata
                        //
                        if (!contentMetadata.fields.ContainsKey("id")) {
                            ContentFieldMetadataModel fieldMetadata = new() {
                                nameLc = "id",
                                active = true,
                                fieldTypeId = CPContentBaseClass.FieldTypeIdEnum.AutoIdIncrement,
                                editSortPriority = 100,
                                authorable = false,
                                caption = "ID",
                                defaultValue = "",
                                isBaseField = contentMetadata.isBaseContent
                            };
                            contentMetadata.verifyContentField(core, fieldMetadata, true, logMsgContext);
                        }
                        //
                        if (!contentMetadata.fields.ContainsKey("name")) {
                            ContentFieldMetadataModel fieldMetadata = new() {
                                nameLc = "name",
                                active = true,
                                fieldTypeId = CPContentBaseClass.FieldTypeIdEnum.Text,
                                editSortPriority = 110,
                                authorable = true,
                                caption = "Name",
                                defaultValue = "",
                                isBaseField = contentMetadata.isBaseContent
                            };
                            contentMetadata.verifyContentField(core, fieldMetadata, true, logMsgContext);
                        }
                        //
                        if (!contentMetadata.fields.ContainsKey("active")) {
                            ContentFieldMetadataModel fieldMetadata = new() {
                                nameLc = "active",
                                active = true,
                                fieldTypeId = CPContentBaseClass.FieldTypeIdEnum.Boolean,
                                editSortPriority = 200,
                                authorable = true,
                                caption = "Active",
                                defaultValue = "1",
                                isBaseField = contentMetadata.isBaseContent
                            };
                            contentMetadata.verifyContentField(core, fieldMetadata, true, logMsgContext);
                        }
                        //
                        if (!contentMetadata.fields.ContainsKey("sortorder")) {
                            ContentFieldMetadataModel fieldMetadata = new() {
                                nameLc = "sortorder",
                                active = true,
                                fieldTypeId = CPContentBaseClass.FieldTypeIdEnum.Text,
                                editSortPriority = 2000,
                                authorable = false,
                                caption = "Alpha Sort Order",
                                defaultValue = "",
                                isBaseField = contentMetadata.isBaseContent
                            };
                            contentMetadata.verifyContentField(core, fieldMetadata, true, logMsgContext);
                        }
                        //
                        if (!contentMetadata.fields.ContainsKey("dateadded")) {
                            ContentFieldMetadataModel fieldMetadata = new() {
                                nameLc = "dateadded",
                                active = true,
                                fieldTypeId = CPContentBaseClass.FieldTypeIdEnum.Date,
                                editSortPriority = 9999,
                                authorable = false,
                                caption = "Date Added",
                                defaultValue = "",
                                isBaseField = contentMetadata.isBaseContent
                            };
                            contentMetadata.verifyContentField(core, fieldMetadata, true, logMsgContext);
                        }
                        if (!contentMetadata.fields.ContainsKey("createdby")) {
                            ContentFieldMetadataModel fieldMetadata = new() {
                                nameLc = "createdby",
                                active = true,
                                fieldTypeId = CPContentBaseClass.FieldTypeIdEnum.Lookup,
                                editSortPriority = 9999,
                                authorable = false,
                                caption = "Created By"
                            };
                            fieldMetadata.set_lookupContentName(core, "People");
                            fieldMetadata.defaultValue = "";
                            fieldMetadata.isBaseField = contentMetadata.isBaseContent;
                            contentMetadata.verifyContentField(core, fieldMetadata, true, logMsgContext);
                        }
                        if (!contentMetadata.fields.ContainsKey("modifieddate")) {
                            ContentFieldMetadataModel fieldMetadata = new() {
                                nameLc = "modifieddate",
                                active = true,
                                fieldTypeId = CPContentBaseClass.FieldTypeIdEnum.Date,
                                editSortPriority = 9999,
                                authorable = false,
                                caption = "Date Modified",
                                defaultValue = "",
                                isBaseField = contentMetadata.isBaseContent
                            };
                            contentMetadata.verifyContentField(core, fieldMetadata, true, logMsgContext);
                        }
                        if (!contentMetadata.fields.ContainsKey("modifiedby")) {
                            ContentFieldMetadataModel fieldMetadata = new() {
                                nameLc = "modifiedby",
                                active = true,
                                fieldTypeId = CPContentBaseClass.FieldTypeIdEnum.Lookup,
                                editSortPriority = 9999,
                                authorable = false,
                                caption = "Modified By"
                            };
                            fieldMetadata.set_lookupContentName(core, "People");
                            fieldMetadata.defaultValue = "";
                            fieldMetadata.isBaseField = contentMetadata.isBaseContent;
                            contentMetadata.verifyContentField(core, fieldMetadata, true, logMsgContext);
                        }
                        if (!contentMetadata.fields.ContainsKey("ContentControlId")) {
                            ContentFieldMetadataModel fieldMetadata = new() {
                                nameLc = "contentcontrolid",
                                active = true,
                                fieldTypeId = CPContentBaseClass.FieldTypeIdEnum.Lookup,
                                editSortPriority = 9999,
                                authorable = false,
                                caption = "Controlling Content"
                            };
                            fieldMetadata.set_lookupContentName(core, "Content");
                            fieldMetadata.defaultValue = "";
                            fieldMetadata.isBaseField = contentMetadata.isBaseContent;
                            contentMetadata.verifyContentField(core, fieldMetadata, true, logMsgContext);
                        }
                        if (!contentMetadata.fields.ContainsKey("CreateKey")) {
                            ContentFieldMetadataModel fieldMetadata = new() {
                                nameLc = "createkey",
                                active = true,
                                fieldTypeId = CPContentBaseClass.FieldTypeIdEnum.Integer,
                                editSortPriority = 9999,
                                authorable = false,
                                caption = "Create Key",
                                defaultValue = "",
                                isBaseField = contentMetadata.isBaseContent
                            };
                            contentMetadata.verifyContentField(core, fieldMetadata, true, logMsgContext);
                        }
                        if (!contentMetadata.fields.ContainsKey("ccGuid")) {
                            ContentFieldMetadataModel fieldMetadata = new() {
                                nameLc = "ccguid",
                                active = true,
                                fieldTypeId = CPContentBaseClass.FieldTypeIdEnum.Text,
                                editSortPriority = 9999,
                                authorable = false,
                                caption = "Guid",
                                defaultValue = "",
                                isBaseField = contentMetadata.isBaseContent
                            };
                            contentMetadata.verifyContentField(core, fieldMetadata, true, logMsgContext);
                        }
                    }
                }
                //
                // ----- Load metadata
                //
                ContentModel.invalidateCacheOfTable<ContentModel>(core.cpParent);
                ContentFieldModel.invalidateCacheOfTable<ContentFieldModel>(core.cpParent);
                core.clearMetaData();
                core.cache.invalidateAll();
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
            return contentMetadata.id;
        }
        //

        //
        //=============================================================================
        /// <summary>
        /// Imports the named table into the content system
        /// </summary>
        /// <param name="DataSourceName"></param>
        /// <param name="tableName"></param>
        /// <param name="ContentName"></param>
        //
        public static ContentMetadataModel createFromSQLTable(CoreController core, DataSourceModel DataSource, string tableName, string ContentName) {
            try {
                //
                string logMsgContext = "createFromSQLTable, ContentName [" + ContentName + "], tableName [" + tableName + "]";
                //
                // -- add a record if none found
                var contentMetadata = new ContentMetadataModel();
                using (var targetDb = new DbController(core, DataSource.name)) {
                    using (DataTable dt = targetDb.executeQuery("select top 1 * from " + tableName)) {
                        if (dt.Rows.Count == 0) {
                            var fieldList = new NameValueCollection {
                                { "name", DbController.encodeSQLText("test-record") }
                            };
                            core.db.insert(tableName, fieldList, 0);
                        }
                    }
                    using (DataTable dt = targetDb.executeQuery("select top 1 * from " + tableName)) {
                        if (dt.Rows.Count == 0) { throw new GenericException("Could not add a record To table [" + tableName + "]."); }
                        //
                        // -- Find/Create the Content Definition
                        contentMetadata.id = DbController.getContentId(core, ContentName);
                        if (contentMetadata.id <= 0) {
                            //
                            // -- Content definition not found, create it
                            contentMetadata.id = verifyContent_returnId(core, new ContentMetadataModel {
                                tableName = tableName,
                                name = ContentName,
                                active = true,
                                allowAdd = true,
                                allowDelete = true
                            }, logMsgContext);
                            // todo - verifyContent needs to return the model
                            contentMetadata = create(core, contentMetadata.id);
                            core.cache.invalidateAll();
                            core.clearMetaData();
                        }
                        //
                        // -- Create the ccFields records for the new table, locate the field in the content field table
                        using (DataTable dtFields = core.db.executeQuery("Select name from ccFields where ContentID=" + contentMetadata.id + ";")) {
                            //
                            // ----- verify all the table fields
                            foreach (DataColumn dcTableColumns in dt.Columns) {
                                //
                                // ----- see if the field is already in the content fields
                                string UcaseTableColumnName = GenericController.toUCase(dcTableColumns.ColumnName);
                                bool ContentFieldFound = false;
                                foreach (DataRow drContentRecords in dtFields.Rows) {
                                    if (GenericController.toUCase(GenericController.encodeText(drContentRecords["name"])) == UcaseTableColumnName) {
                                        ContentFieldFound = true;
                                        break;
                                    }
                                }
                                if (!ContentFieldFound) {
                                    //
                                    // -- create the content field
                                    ContentFieldMetadataModel.verifyContentFieldFromSqlTableField(core, contentMetadata, dcTableColumns.ColumnName, encodeInteger(dcTableColumns.DataType));
                                } else {
                                    //
                                    // -- touch field so upgrade does not delete it
                                    core.db.executeNonQuery("update ccFields Set CreateKey=0 where (Contentid=" + contentMetadata.id + ") And (name = " + DbController.encodeSQLText(UcaseTableColumnName) + ")");
                                }
                            }
                        }
                        //
                        // -- Fill ContentControlID fields with new ContentID
                        targetDb.executeQuery("Update " + tableName + " Set ContentControlID=" + contentMetadata.id + " where (ContentControlID Is null);");
                        //
                        // ----- Load metadata, Load only if the previous state of autoload was true, Leave Autoload false during load so more do not trigger
                        core.cache.invalidateAll();
                        core.clearMetaData();
                    }
                }
                return contentMetadata;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// When possible, create contentMetaData object and read property. When property is a variable, use this
        /// </summary>
        /// <param name="core"></param>
        /// <param name="contentName"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public string getContentProperty(CoreController core, string propertyName) {
            string result = "";
            //
            switch (GenericController.toUCase(encodeText(propertyName))) {
                case "CONTENTCONTROLCRITERIA":
                    result = legacyContentControlCriteria;
                    break;
                case "ACTIVEONLY":
                    result = activeOnly.ToString();
                    break;
                case "ADMINONLY":
                    result = adminOnly.ToString();
                    break;
                case "ALIASID":
                    result = aliasId;
                    break;
                case "ALIASNAME":
                    result = aliasName;
                    break;
                case "ALLOWADD":
                    result = allowAdd.ToString();
                    break;
                case "ALLOWDELETE":
                    result = allowDelete.ToString();
                    break;
                case "DATASOURCEID":
                    result = dataSourceId.ToString();
                    break;
                case "DEFAULTSORTMETHOD":
                    result = defaultSortMethod;
                    break;
                case "DEVELOPERONLY":
                    result = developerOnly.ToString();
                    break;
                case "FIELDCOUNT":
                    result = fields.Count.ToString();
                    break;
                case "ID":
                    result = id.ToString();
                    break;
                case "NAME":
                    result = name;
                    break;
                case "PARENTID":
                    result = parentId.ToString();
                    break;
                case "CONTENTTABLENAME":
                    result = tableName;
                    break;
                case "CONTENTDATASOURCENAME":
                    result = dataSourceName;
                    break;
                case "WHERECLAUSE":
                    result = whereClause;
                    break;
                case "DROPDOWNFIELDLIST":
                    result = dropDownFieldList;
                    break;
                case "SELECTFIELDLIST":
                    result = selectCommaList;
                    break;
                default:
                    break;
            }
            return result;
        }
        //
        //========================================================================
        /// <summary>
        /// Returns true if the current instance is a parent of the testContentId
        /// </summary>
        /// <param name="core"></param>
        /// <param name="testContentId"></param>
        /// <param name="parentContentId"></param>
        /// <returns></returns>
        public bool isParentOf(CoreController core, int testContentId) {
            try {
                if ((testContentId <= 0) || (id <= 0)) { return false; }
                if (testContentId == id) { return true; }
                if (childIdList(core).Count == 0) { return false; }
                if (!childIdList(core).Contains(testContentId)) { return false; }
                foreach (int childContentId in childIdList(core)) {
                    var childContent = create(core, childContentId);
                    if (childContent != null) {
                        if (childContent.isParentOf(core, childContent.id)) { return true; }
                    }
                }
                return false;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //=============================================================
        /// <summary>
        /// isContentFieldSupported
        /// </summary>
        /// <param name="core"></param>
        /// <param name="contentName"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public bool containsField(CoreController core, string fieldName) {
            return fields.ContainsKey(fieldName.ToLowerInvariant());
        }
        //
        //=============================================================================
        /// <summary>
        /// Create a child content from a parent content
        /// </summary>
        /// <param name="core"></param>
        /// <param name="childContentName"></param>
        /// <param name="parentContentName"></param>
        /// <param name="memberID"></param>
        public ContentMetadataModel createContentChild(CoreController core, string childContentName, int memberID) {
            try {
                //
                // -- test if the child already exists
                var childContent = DbBaseModel.createByUniqueName<ContentModel>(core.cpParent, childContentName);
                if (childContent != null) {
                    //
                    // -- child content already exists
                    if (childContent.parentId != id) { throw (new GenericException("Can not create Child Content [" + childContentName + "] because this content name is already in use.")); }
                    return createByUniqueName(core, childContentName);
                }
                //
                // -- convert this object to a child of the record it was opened with, and save
                childContent = DbBaseModel.createByUniqueName<ContentModel>(core.cpParent, name);
                childContent.parentId = childContent.id;
                childContent.name = childContentName;
                childContent.createdBy = childContent.modifiedBy = memberID;
                childContent.dateAdded = childContent.modifiedDate = core.dateTimeNowMockable;
                childContent.ccguid = getGUID();
                childContent.id = 0;
                childContent.save(core.cpParent);
                //
                // ----- Load metadata
                //
                core.cache.invalidateAll();
                core.clearMetaData();
                //
                return create(core, childContent);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                return null;
            }
        }
        //   
        //============================================================================================================
        /// <summary>
        /// set the content control Id for a record, all potentially all its child records (if parentid field exists)
        /// </summary>
        /// <param name="core"></param>
        /// <param name="contentId"></param>
        /// <param name="recordId"></param>
        /// <param name="newContentControlID"></param>
        /// <param name="UsedIDString"></param>
        public void setContentControlId(CoreController core, int recordId, int newContentControlID, string UsedIDString = "") {
            //
            // -- update the record
            core.db.executeNonQuery("update " + tableName + " set contentcontrolid=" + newContentControlID + " where id=" + recordId);
            //
            // -- fix content watch
            core.db.executeNonQuery("update ccContentWatch set ContentID=" + newContentControlID + ", ContentRecordKey='" + newContentControlID + "." + recordId + "' where ContentID=" + id + " and RecordID=" + recordId);
            //
            // -- if content includes a parentId field (like page content), update all child records to this meta.id
            if (fields.ContainsKey("parentid")) {
                using var dt = core.db.executeQuery("select id from " + tableName + " where parentid=" + recordId); 
                foreach (DataRow dr in dt.Rows) {
                    setContentControlId(core, DbController.getDataRowFieldInteger(dr, "id"), newContentControlID, UsedIDString);
                }
            }
        }
        //   
        //============================================================================================================
        /// <summary>
        /// Return a record name given the record id. If not record is found, blank is returned.
        /// </summary>
        public string getRecordName(CoreController core, int recordID) {
            try {
                using (DataTable dt = core.db.executeQuery("select name from " + tableName + " where id=" + recordID)) {
                    foreach (DataRow dr in dt.Rows) {
                        return DbController.getDataRowFieldText(dr, "name");
                    }
                }
                return string.Empty;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //   
        //============================================================================================================
        //
        public int getRecordId(CoreController core, string recordGuid) {
            try {
                if (string.IsNullOrWhiteSpace(recordGuid)) { return 0; }
                using (DataTable dt = core.db.executeQuery("select id from " + tableName + " where ccguid=" + DbController.encodeSQLText(recordGuid))) {

                    foreach (DataRow dr in dt.Rows) {
                        return DbController.getDataRowFieldInteger(dr, "id");
                    }
                }
                return 0;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //   
        //============================================================================================================
        //
        public int getRecordIdByUniqueName(CoreController core, string recordName) {
            try {
                using (DataTable dt = core.db.executeQuery("select id from " + tableName + " where name=" + DbController.encodeSqlTableName(recordName))) {
                    foreach (DataRow dr in dt.Rows) {
                        return DbController.getDataRowFieldInteger(dr, "id");
                    }
                }
                return 0;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //   
        //============================================================================================================
        //
        public static Dictionary<string, String> getDefaultValueDict(CoreController core, string contentName) {
            var defaultValueDict = new Dictionary<string, String>();
            ContentMetadataModel meta = createByUniqueName(core, contentName);
            if (meta == null) { return defaultValueDict; }
            foreach (var fieldKvp in meta.fields) {
                if (!string.IsNullOrWhiteSpace(fieldKvp.Value.defaultValue)) {
                    defaultValueDict.Add(fieldKvp.Key.ToLower(CultureInfo.InvariantCulture), fieldKvp.Value.defaultValue);
                }
            }
            return defaultValueDict;
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
