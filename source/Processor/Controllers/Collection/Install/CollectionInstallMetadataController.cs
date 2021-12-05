
using Contensive.Models.Db;
using Contensive.Processor.Exceptions;
using Contensive.Processor.Models.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Xml;
using static Contensive.Processor.Constants;
using static Contensive.Processor.Controllers.GenericController;

namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// install addon collections.
    /// </summary>
    public static class CollectionInstallMetadataController {
        //
        //======================================================================================================
        //
        internal static void installMetaDataMiniCollectionFromXml(CoreController core, string srcXml, bool isNewBuild, bool reinstallDependencies, bool isBaseCollection, string logPrefix) {
            try {
                MetadataMiniCollectionModel newCollection = loadXML(core, srcXml, isBaseCollection, true, isNewBuild, logPrefix);
                installMetaDataMiniCollection_BuildDb(core, isBaseCollection, newCollection, isNewBuild, reinstallDependencies, logPrefix);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //======================================================================================================
        /// <summary>
        /// create a collection class from a collection xml file, metadata are added to the metadatas in the application collection
        /// </summary>
        public static MetadataMiniCollectionModel loadXML(CoreController core, string srcCollecionXml, bool isBaseCollection, bool setAllDataChanged, bool IsNewBuild, string logPrefix) {
            try {
                //
                LogController.logInfo(core, "Application: " + core.appConfig.name + ", Upgrademetadata_LoadDataToCollection");
                //
                var result = new MetadataMiniCollectionModel();
                if (string.IsNullOrEmpty(srcCollecionXml)) {
                    //
                    // -- empty collection is an error
                    throw (new GenericException("Upgrademetadata_LoadDataToCollection, srcCollectionXml is blank or null"));
                }
                var srcXmlDom = new XmlDocument { XmlResolver = null };
                try {
                    srcXmlDom.LoadXml(srcCollecionXml);
                } catch (Exception ex) {
                    //
                    // -- xml load error
                    string errMsg = "Upgrademetadata_LoadDataToCollection Error reading xml archive";
                    Logger.Error(ex, LogController.processLogMessage(core, errMsg, true));
                    throw new GenericException("Error in Upgrademetadata_LoadDataToCollection, during doc.loadXml()", ex);
                }
                if ((srcXmlDom.DocumentElement.Name.ToLowerInvariant() != CollectionFileRootNode) && (srcXmlDom.DocumentElement.Name.ToLowerInvariant() != "contensivecdef")) {
                    //
                    // -- root node must be collection (or legacy contensivemetadata)
                    LogController.logError(core, new GenericException("the archive file has a syntax error. Application name must be the first node."));
                } else {
                    result.isBaseCollection = isBaseCollection;
                    //
                    // Get Collection Name for logs
                    //
                    string collectionName = XmlController.getXMLAttribute(core, srcXmlDom.DocumentElement, "name", "");
                    if (string.IsNullOrEmpty(collectionName)) {
                        LogController.logInfo(core, "Upgrademetadata_LoadDataToCollection, Application: " + core.appConfig.name + ", Collection has no name");
                    }
                    result.name = collectionName;
                    //
                    foreach (XmlNode metaData_NodeWithinLoop in srcXmlDom.DocumentElement.ChildNodes) {
                        string NodeName = toLCase(metaData_NodeWithinLoop.Name);
                        string cdefActiveText = null;
                        switch (NodeName) {
                            case "cdef": {
                                    //
                                    // Content Definitions
                                    //
                                    string contentName = XmlController.getXMLAttribute(core, metaData_NodeWithinLoop, "name", "");
                                    if (string.IsNullOrEmpty(contentName)) {
                                        throw (new GenericException("Collection xml file load includes a content metadata node with no name."));
                                    }
                                    string contentGuid = XmlController.getXMLAttribute(core, metaData_NodeWithinLoop, "guid", "");
                                    ContentMetadataModel DefaultMetaData = null;
                                    if (!isBaseCollection) {
                                        //
                                        // -- if base collection loaded, attmpt load. Some cases during startup happen before content is available so exceptions should be skipped
                                        if (!string.IsNullOrWhiteSpace(contentGuid)) {
                                            DefaultMetaData = ContentMetadataModel.create(core, contentGuid);
                                        } else {
                                            DefaultMetaData = ContentMetadataModel.createByUniqueName(core, contentName);
                                        }
                                    }
                                    if (DefaultMetaData == null) {
                                        DefaultMetaData = new ContentMetadataModel {
                                            guid = contentGuid,
                                            name = contentName,
                                            active = true
                                        };
                                    }
                                    //
                                    // ----- Add metadata if not already there
                                    //
                                    if (!result.metaData.ContainsKey(contentName.ToLowerInvariant())) {
                                        result.metaData.Add(contentName.ToLowerInvariant(), new Models.Domain.ContentMetadataModel());
                                    }
                                    //
                                    // Get metadata attributes
                                    //
                                    ContentMetadataModel targetMetaData = result.metaData[contentName.ToLowerInvariant()];
                                    string activeDefaultText = "1";
                                    if (!(DefaultMetaData.active)) {
                                        activeDefaultText = "0";
                                    }
                                    cdefActiveText = XmlController.getXMLAttribute(core, metaData_NodeWithinLoop, "Active", activeDefaultText);
                                    if (string.IsNullOrEmpty(cdefActiveText)) { cdefActiveText = "1"; }
                                    targetMetaData.active = encodeBoolean(cdefActiveText);
                                    targetMetaData.activeOnly = true;
                                    //.adminColumns = ?
                                    targetMetaData.adminOnly = XmlController.getXMLAttributeBoolean(core, metaData_NodeWithinLoop, "AdminOnly", DefaultMetaData.adminOnly);
                                    targetMetaData.aliasId = "id";
                                    targetMetaData.aliasName = "name";
                                    targetMetaData.allowAdd = XmlController.getXMLAttributeBoolean(core, metaData_NodeWithinLoop, "AllowAdd", DefaultMetaData.allowAdd);
                                    targetMetaData.allowCalendarEvents = XmlController.getXMLAttributeBoolean(core, metaData_NodeWithinLoop, "AllowCalendarEvents", DefaultMetaData.allowCalendarEvents);
                                    targetMetaData.allowContentChildTool = XmlController.getXMLAttributeBoolean(core, metaData_NodeWithinLoop, "AllowContentChildTool", DefaultMetaData.allowContentChildTool);
                                    targetMetaData.allowContentTracking = XmlController.getXMLAttributeBoolean(core, metaData_NodeWithinLoop, "AllowContentTracking", DefaultMetaData.allowContentTracking);
                                    targetMetaData.allowDelete = XmlController.getXMLAttributeBoolean(core, metaData_NodeWithinLoop, "AllowDelete", DefaultMetaData.allowDelete);
                                    targetMetaData.allowTopicRules = XmlController.getXMLAttributeBoolean(core, metaData_NodeWithinLoop, "AllowTopicRules", DefaultMetaData.allowTopicRules);
                                    targetMetaData.guid = XmlController.getXMLAttribute(core, metaData_NodeWithinLoop, "guid", DefaultMetaData.guid);
                                    targetMetaData.dataChanged = setAllDataChanged;
                                    targetMetaData.legacyContentControlCriteria = "";
                                    targetMetaData.dataSourceName = XmlController.getXMLAttribute(core, metaData_NodeWithinLoop, "ContentDataSourceName", DefaultMetaData.dataSourceName);
                                    targetMetaData.tableName = XmlController.getXMLAttribute(core, metaData_NodeWithinLoop, "ContentTableName", DefaultMetaData.tableName);
                                    targetMetaData.dataSourceId = 0;
                                    targetMetaData.defaultSortMethod = XmlController.getXMLAttribute(core, metaData_NodeWithinLoop, "DefaultSortMethod", DefaultMetaData.defaultSortMethod);
                                    if ((targetMetaData.defaultSortMethod == null) || (targetMetaData.defaultSortMethod == "") || (targetMetaData.defaultSortMethod.ToLowerInvariant() == "name")) {
                                        targetMetaData.defaultSortMethod = "By Name";
                                    } else if (GenericController.toLCase(targetMetaData.defaultSortMethod) == "sortorder") {
                                        targetMetaData.defaultSortMethod = "By Alpha Sort Order Field";
                                    } else if (GenericController.toLCase(targetMetaData.defaultSortMethod) == "date") {
                                        targetMetaData.defaultSortMethod = "By Date";
                                    }
                                    targetMetaData.developerOnly = XmlController.getXMLAttributeBoolean(core, metaData_NodeWithinLoop, "DeveloperOnly", DefaultMetaData.developerOnly);
                                    targetMetaData.dropDownFieldList = XmlController.getXMLAttribute(core, metaData_NodeWithinLoop, "DropDownFieldList", DefaultMetaData.dropDownFieldList);
                                    targetMetaData.editorGroupName = XmlController.getXMLAttribute(core, metaData_NodeWithinLoop, "EditorGroupName", DefaultMetaData.editorGroupName);
                                    targetMetaData.fields = new Dictionary<string, Models.Domain.ContentFieldMetadataModel>();
                                    targetMetaData.iconLink = XmlController.getXMLAttribute(core, metaData_NodeWithinLoop, "IconLink", DefaultMetaData.iconLink);
                                    targetMetaData.iconHeight = XmlController.getXMLAttributeInteger(core,  metaData_NodeWithinLoop, "IconHeight", DefaultMetaData.iconHeight);
                                    targetMetaData.iconWidth = XmlController.getXMLAttributeInteger(core,  metaData_NodeWithinLoop, "IconWidth", DefaultMetaData.iconWidth);
                                    targetMetaData.iconSprites = XmlController.getXMLAttributeInteger(core,  metaData_NodeWithinLoop, "IconSprites", DefaultMetaData.iconSprites);
                                    targetMetaData.includesAFieldChange = false;
                                    targetMetaData.installedByCollectionGuid = XmlController.getXMLAttribute(core, metaData_NodeWithinLoop, "installedByCollection", DefaultMetaData.installedByCollectionGuid);
                                    targetMetaData.isBaseContent = isBaseCollection || XmlController.getXMLAttributeBoolean(core, metaData_NodeWithinLoop, "IsBaseContent", false);
                                    targetMetaData.isModifiedSinceInstalled = XmlController.getXMLAttributeBoolean(core, metaData_NodeWithinLoop, "IsModified", DefaultMetaData.isModifiedSinceInstalled);
                                    targetMetaData.name = contentName;
                                    targetMetaData.parentName = XmlController.getXMLAttribute(core, metaData_NodeWithinLoop, "Parent", DefaultMetaData.parentName);
                                    targetMetaData.whereClause = XmlController.getXMLAttribute(core, metaData_NodeWithinLoop, "WhereClause", DefaultMetaData.whereClause);
                                    //
                                    // -- determine id
                                    targetMetaData.id = DbController.getContentId(core, contentName);
                                    //
                                    // Get metadata field nodes
                                    //
                                    foreach (XmlNode MetaDataChildNode in metaData_NodeWithinLoop.ChildNodes) {
                                        //
                                        // ----- process metadata Field
                                        //
                                        if (textMatch(MetaDataChildNode.Name, "field")) {
                                            string FieldName = XmlController.getXMLAttribute(core, MetaDataChildNode, "Name", "");
                                            ContentFieldMetadataModel DefaultMetaDataField = null;
                                            //
                                            // try to find field in the defaultmetadata
                                            //
                                            if (DefaultMetaData.fields.ContainsKey(FieldName)) {
                                                DefaultMetaDataField = DefaultMetaData.fields[FieldName];
                                            } else {
                                                DefaultMetaDataField = new Models.Domain.ContentFieldMetadataModel();
                                            }
                                            //
                                            if (!(result.metaData[contentName.ToLowerInvariant()].fields.ContainsKey(FieldName.ToLowerInvariant()))) {
                                                result.metaData[contentName.ToLowerInvariant()].fields.Add(FieldName.ToLowerInvariant(), new Models.Domain.ContentFieldMetadataModel());
                                            }
                                            var metaDataField = result.metaData[contentName.ToLowerInvariant()].fields[FieldName.ToLowerInvariant()];
                                            metaDataField.nameLc = FieldName.ToLowerInvariant();
                                            string cdefFieldActiveText = XmlController.getXMLAttribute(core, MetaDataChildNode, "Active", (DefaultMetaDataField.active) ? "1" : "0");
                                            metaDataField.active = (string.IsNullOrEmpty(cdefFieldActiveText)) ? true : encodeBoolean(cdefFieldActiveText);
                                            //
                                            // Convert Field Descriptor (text) to field type (integer)
                                            //
                                            string defaultFieldTypeName = ContentFieldMetadataModel.getFieldTypeNameFromFieldTypeId(core, DefaultMetaDataField.fieldTypeId);
                                            string fieldTypeName = XmlController.getXMLAttribute(core, MetaDataChildNode, "FieldType", defaultFieldTypeName);
                                            metaDataField.fieldTypeId = core.db.getFieldTypeIdFromFieldTypeName(fieldTypeName);
                                            metaDataField.editSortPriority = XmlController.getXMLAttributeInteger(core,  MetaDataChildNode, "EditSortPriority", DefaultMetaDataField.editSortPriority);
                                            metaDataField.authorable = XmlController.getXMLAttributeBoolean(core, MetaDataChildNode, "Authorable", DefaultMetaDataField.authorable);
                                            metaDataField.caption = XmlController.getXMLAttribute(core, MetaDataChildNode, "Caption", DefaultMetaDataField.caption);
                                            metaDataField.defaultValue = XmlController.getXMLAttribute(core, MetaDataChildNode, "DefaultValue", DefaultMetaDataField.defaultValue);
                                            metaDataField.notEditable = XmlController.getXMLAttributeBoolean(core, MetaDataChildNode, "NotEditable", DefaultMetaDataField.notEditable);
                                            metaDataField.indexColumn = XmlController.getXMLAttributeInteger(core,  MetaDataChildNode, "IndexColumn", DefaultMetaDataField.indexColumn);
                                            metaDataField.indexWidth = XmlController.getXMLAttribute(core, MetaDataChildNode, "IndexWidth", DefaultMetaDataField.indexWidth);
                                            metaDataField.indexSortOrder = XmlController.getXMLAttributeInteger(core,  MetaDataChildNode, "IndexSortOrder", DefaultMetaDataField.indexSortOrder);
                                            metaDataField.redirectId = XmlController.getXMLAttribute(core, MetaDataChildNode, "RedirectID", DefaultMetaDataField.redirectId);
                                            metaDataField.redirectPath = XmlController.getXMLAttribute(core, MetaDataChildNode, "RedirectPath", DefaultMetaDataField.redirectPath);
                                            metaDataField.htmlContent = XmlController.getXMLAttributeBoolean(core, MetaDataChildNode, "HTMLContent", DefaultMetaDataField.htmlContent);
                                            metaDataField.uniqueName = XmlController.getXMLAttributeBoolean(core, MetaDataChildNode, "UniqueName", DefaultMetaDataField.uniqueName);
                                            metaDataField.password = XmlController.getXMLAttributeBoolean(core, MetaDataChildNode, "Password", DefaultMetaDataField.password);
                                            metaDataField.adminOnly = XmlController.getXMLAttributeBoolean(core, MetaDataChildNode, "AdminOnly", DefaultMetaDataField.adminOnly);
                                            metaDataField.developerOnly = XmlController.getXMLAttributeBoolean(core, MetaDataChildNode, "DeveloperOnly", DefaultMetaDataField.developerOnly);
                                            metaDataField.readOnly = XmlController.getXMLAttributeBoolean(core, MetaDataChildNode, "ReadOnly", DefaultMetaDataField.readOnly);
                                            metaDataField.required = XmlController.getXMLAttributeBoolean(core, MetaDataChildNode, "Required", DefaultMetaDataField.required);
                                            metaDataField.rssTitleField = XmlController.getXMLAttributeBoolean(core, MetaDataChildNode, "RSSTitle", DefaultMetaDataField.rssTitleField);
                                            metaDataField.rssDescriptionField = XmlController.getXMLAttributeBoolean(core, MetaDataChildNode, "RSSDescriptionField", DefaultMetaDataField.rssDescriptionField);
                                            string memberSelectGroup = XmlController.getXMLAttribute(core, MetaDataChildNode, "MemberSelectGroup", "");
                                            if (!string.IsNullOrEmpty(memberSelectGroup)) {
                                                metaDataField.memberSelectGroupName_set(core, memberSelectGroup);
                                            } else {
                                                // -- allow typo where "memberselectgroupid" is set to the name
                                                memberSelectGroup = XmlController.getXMLAttribute(core, MetaDataChildNode, "MemberSelectGroupId", "");
                                                if (!string.IsNullOrEmpty(memberSelectGroup) && (memberSelectGroup!="0") && encodeInteger(memberSelectGroup)==0) {
                                                    LogController.logError(core, new GenericException("CollectionInstallMetadataController.loadXML, node MemberSelectGroupId should be MemberSelectGroup"));
                                                    metaDataField.memberSelectGroupName_set(core, memberSelectGroup);
                                                }
                                            }
                                            metaDataField.editTabName = XmlController.getXMLAttribute(core, MetaDataChildNode, "EditTab", DefaultMetaDataField.editTabName);
                                            metaDataField.scramble = XmlController.getXMLAttributeBoolean(core, MetaDataChildNode, "Scramble", DefaultMetaDataField.scramble);
                                            metaDataField.lookupList = XmlController.getXMLAttribute(core, MetaDataChildNode, "LookupList", DefaultMetaDataField.lookupList);
                                            metaDataField.manyToManyRulePrimaryField = XmlController.getXMLAttribute(core, MetaDataChildNode, "ManyToManyRulePrimaryField", DefaultMetaDataField.manyToManyRulePrimaryField);
                                            metaDataField.manyToManyRuleSecondaryField = XmlController.getXMLAttribute(core, MetaDataChildNode, "ManyToManyRuleSecondaryField", DefaultMetaDataField.manyToManyRuleSecondaryField);
                                            metaDataField.set_lookupContentName(core, XmlController.getXMLAttribute(core, MetaDataChildNode, "LookupContent", DefaultMetaDataField.get_lookupContentName(core)));
                                            // isbase should be set if the base file is loading, regardless of the state of any isBaseField attribute -- which will be removed later
                                            // case 1 - when the application collection is loaded from the exported xml file, isbasefield must follow the export file although the data is not the base collection
                                            // case 2 - when the base file is loaded, all fields must include the attribute
                                            metaDataField.isBaseField = XmlController.getXMLAttributeBoolean(core, MetaDataChildNode, "IsBaseField", false) || isBaseCollection;
                                            metaDataField.set_redirectContentName(core, XmlController.getXMLAttribute(core, MetaDataChildNode, "RedirectContent", DefaultMetaDataField.get_redirectContentName(core)));
                                            metaDataField.set_manyToManyContentName(core, XmlController.getXMLAttribute(core, MetaDataChildNode, "ManyToManyContent", DefaultMetaDataField.get_manyToManyContentName(core)));
                                            metaDataField.set_manyToManyRuleContentName(core, XmlController.getXMLAttribute(core, MetaDataChildNode, "ManyToManyRuleContent", DefaultMetaDataField.get_manyToManyRuleContentName(core)));
                                            metaDataField.isModifiedSinceInstalled = XmlController.getXMLAttributeBoolean(core, MetaDataChildNode, "IsModified", DefaultMetaDataField.isModifiedSinceInstalled);
                                            metaDataField.installedByCollectionGuid = XmlController.getXMLAttribute(core, MetaDataChildNode, "installedByCollectionId", DefaultMetaDataField.installedByCollectionGuid);
                                            metaDataField.id = DbController.getContentFieldId(core, targetMetaData.id, metaDataField.nameLc);
                                            metaDataField.dataChanged = setAllDataChanged;
                                            //
                                            // ----- handle child nodes (help node)
                                            //
                                            metaDataField.helpCustom = "";
                                            metaDataField.helpDefault = "";
                                            foreach (XmlNode FieldChildNode in MetaDataChildNode.ChildNodes) {
                                                //
                                                // ----- process metadata Field
                                                //
                                                if (textMatch(FieldChildNode.Name, "HelpDefault")) {
                                                    metaDataField.helpDefault = FieldChildNode.InnerText;
                                                }
                                                if (textMatch(FieldChildNode.Name, "HelpCustom")) {
                                                    metaDataField.helpCustom = FieldChildNode.InnerText;
                                                }
                                                metaDataField.helpChanged = setAllDataChanged;
                                            }
                                        }
                                    }
                                    break;
                                }
                            case "sqlindex": {
                                    //
                                    // SQL Indexes
                                    //
                                    string IndexName = XmlController.getXMLAttribute(core, metaData_NodeWithinLoop, "indexname", "");
                                    string TableName = XmlController.getXMLAttribute(core, metaData_NodeWithinLoop, "tableName", "");
                                    string DataSourceName = XmlController.getXMLAttribute(core, metaData_NodeWithinLoop, "DataSourceName", "");
                                    if (string.IsNullOrEmpty(DataSourceName)) {
                                        DataSourceName = "default";
                                    }
                                    bool removeDup = false;
                                    MetadataMiniCollectionModel.MiniCollectionSQLIndexModel dupToRemove = new MetadataMiniCollectionModel.MiniCollectionSQLIndexModel();
                                    foreach (MetadataMiniCollectionModel.MiniCollectionSQLIndexModel index in result.sqlIndexes) {
                                        if (textMatch(index.indexName, IndexName) && textMatch(index.tableName, TableName) && textMatch(index.dataSourceName, DataSourceName)) {
                                            dupToRemove = index;
                                            removeDup = true;
                                            break;
                                        }
                                    }
                                    if (removeDup) {
                                        result.sqlIndexes.Remove(dupToRemove);
                                    }
                                    MetadataMiniCollectionModel.MiniCollectionSQLIndexModel newIndex = new MetadataMiniCollectionModel.MiniCollectionSQLIndexModel {
                                        indexName = IndexName,
                                        tableName = TableName,
                                        dataSourceName = DataSourceName,
                                        fieldNameList = XmlController.getXMLAttribute(core, metaData_NodeWithinLoop, "FieldNameList", "")
                                    };
                                    result.sqlIndexes.Add(newIndex);
                                    break;
                                }
                            case "adminmenu":
                            case "menuentry":
                            case "navigatorentry": {
                                    //
                                    // Admin Menus / Navigator Entries
                                    string MenuName = XmlController.getXMLAttribute(core, metaData_NodeWithinLoop, "Name", "");
                                    string MenuGuid = XmlController.getXMLAttribute(core, metaData_NodeWithinLoop, "guid", "");
                                    bool IsNavigator = (NodeName == "navigatorentry");
                                    string MenuKey = null;
                                    if (!IsNavigator) {
                                        MenuKey = toLCase(MenuName);
                                    } else {
                                        MenuKey = MenuGuid;
                                    }
                                    if (!result.menus.ContainsKey(MenuKey)) {
                                        cdefActiveText = XmlController.getXMLAttribute(core, metaData_NodeWithinLoop, "Active", "1");
                                        if (string.IsNullOrEmpty(cdefActiveText)) {
                                            cdefActiveText = "1";
                                        }
                                        result.menus.Add(MenuKey, new MetadataMiniCollectionModel.MiniCollectionMenuModel {
                                            dataChanged = setAllDataChanged,
                                            name = MenuName,
                                            guid = MenuGuid,
                                            key = MenuKey,
                                            active = GenericController.encodeBoolean(cdefActiveText),
                                            menuNameSpace = XmlController.getXMLAttribute(core, metaData_NodeWithinLoop, "NameSpace", ""),
                                            parentName = XmlController.getXMLAttribute(core, metaData_NodeWithinLoop, "ParentName", ""),
                                            contentName = XmlController.getXMLAttribute(core, metaData_NodeWithinLoop, "ContentName", ""),
                                            linkPage = XmlController.getXMLAttribute(core, metaData_NodeWithinLoop, "LinkPage", ""),
                                            sortOrder = XmlController.getXMLAttribute(core, metaData_NodeWithinLoop, "SortOrder", ""),
                                            adminOnly = XmlController.getXMLAttributeBoolean(core, metaData_NodeWithinLoop, "AdminOnly", false),
                                            developerOnly = XmlController.getXMLAttributeBoolean(core, metaData_NodeWithinLoop, "DeveloperOnly", false),
                                            newWindow = XmlController.getXMLAttributeBoolean(core, metaData_NodeWithinLoop, "NewWindow", false),
                                            addonName = XmlController.getXMLAttribute(core, metaData_NodeWithinLoop, "AddonName", ""),
                                            addonGuid = XmlController.getXMLAttribute(core, metaData_NodeWithinLoop, "AddonGuid", ""),
                                            navIconType = XmlController.getXMLAttribute(core, metaData_NodeWithinLoop, "NavIconType", ""),
                                            navIconTitle = XmlController.getXMLAttribute(core, metaData_NodeWithinLoop, "NavIconTitle", ""),
                                            isNavigator = IsNavigator
                                        });
                                    }
                                    break;
                                }
                            case "aggregatefunction":
                            case "addon": {
                                    // do nothing
                                    break;
                                }
                            case "style": {
                                    //
                                    // style sheet entries
                                    //
                                    string styleName = XmlController.getXMLAttribute(core, metaData_NodeWithinLoop, "Name", "");
                                    int Ptr = 0;
                                    if (result.styleCnt > 0) {
                                        for (Ptr = 0; Ptr < result.styleCnt; Ptr++) {
                                            if (textMatch(result.styles[Ptr].name, styleName)) {
                                                break;
                                            }
                                        }
                                    }
                                    if (Ptr >= result.styleCnt) {
                                        Ptr = result.styleCnt;
                                        result.styleCnt = result.styleCnt + 1;
                                        Array.Resize(ref result.styles, Ptr);
                                        result.styles[Ptr].name = styleName;
                                    }
                                    var tempVar5 = result.styles[Ptr];
                                    tempVar5.dataChanged = setAllDataChanged;
                                    tempVar5.overwrite = XmlController.getXMLAttributeBoolean(core, metaData_NodeWithinLoop, "Overwrite", false);
                                    tempVar5.copy = metaData_NodeWithinLoop.InnerText;
                                    break;
                                }
                            case "stylesheet": {
                                    //
                                    // style sheet in one entry
                                    //
                                    result.styleSheet = metaData_NodeWithinLoop.InnerText;
                                    break;
                                }
                            case "pagetemplate": {
                                    //
                                    string templateName = XmlController.getXMLAttribute(core, metaData_NodeWithinLoop, "Name", "");
                                    int Ptr = 0;
                                    if (result.pageTemplateCnt > 0) {
                                        for (Ptr = 0; Ptr < result.pageTemplateCnt; Ptr++) {
                                            if (textMatch(result.pageTemplates[Ptr].name, templateName)) {
                                                break;
                                            }
                                        }
                                    }
                                    if (Ptr >= result.pageTemplateCnt) {
                                        Ptr = result.pageTemplateCnt;
                                        result.pageTemplateCnt = result.pageTemplateCnt + 1;
                                        Array.Resize(ref result.pageTemplates, Ptr);
                                        result.pageTemplates[Ptr].name = templateName;
                                    }
                                    var tempVar6 = result.pageTemplates[Ptr];
                                    tempVar6.copy = XmlController.getXMLAttribute(core, metaData_NodeWithinLoop, "Copy", "");
                                    tempVar6.guid = XmlController.getXMLAttribute(core, metaData_NodeWithinLoop, "guid", "");
                                    tempVar6.style = XmlController.getXMLAttribute(core, metaData_NodeWithinLoop, "style", "");
                                    break;
                                }
                            default: {
                                    // do nothing
                                    break;
                                }
                        }
                    }
                    //
                    // Convert Menus.ParentName to Menu.menuNameSpace
                    //
                    foreach (var kvp in result.menus) {
                        MetadataMiniCollectionModel.MiniCollectionMenuModel menu = kvp.Value;
                        if (!string.IsNullOrEmpty(menu.parentName)) {
                            menu.menuNameSpace = GetMenuNameSpace(core, result.menus, menu, "");
                        }
                    }
                }
                return result;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //======================================================================================================
        /// <summary>
        /// Verify ccContent and ccFields records from the metadata nodes of a a collection file. This is the last step of loading teh metadata nodes of a collection file. ParentId field is set based on ParentName node.
        /// </summary>
        private static void installMetaDataMiniCollection_BuildDb(CoreController core, bool isBaseCollection, MetadataMiniCollectionModel Collection, bool isNewBuild, bool reinstallDependencies, string logPrefix) {
            try {
                //
                string logMsgContext = "installing MetaDataMiniCollection BuildDb, collection [" + Collection.name + "]";
                LogController.logInfo(core, "Application: " + core.appConfig.name + ", Upgrademetadata_BuildDbFromCollection");
                //
                //----------------------------------------------------------------------------------------------------------------------
                LogController.logInfo(core, "metadata Load, stage 1: create SQL tables in default datasource");
                //----------------------------------------------------------------------------------------------------------------------
                //
                {
                    foreach (KeyValuePair<string, ContentMetadataModel> metaKvp in Collection.metaData) {
                        if (string.IsNullOrWhiteSpace(metaKvp.Value.tableName)) {
                            LogController.logWarn(core, "Content [" + metaKvp.Value.name + "] in collection [" + Collection.name + "] cannot be added because the content tablename is empty.");
                            continue;
                        }
                        core.db.createSQLTable(metaKvp.Value.tableName);
                        foreach (KeyValuePair<string, ContentFieldMetadataModel> fieldKvp in metaKvp.Value.fields) {
                            if (string.IsNullOrWhiteSpace(fieldKvp.Value.nameLc)) {
                                LogController.logWarn(core, "Field [# " + fieldKvp.Value.id + "] in content [" + metaKvp.Value.name + "] in collection [" + Collection.name + "] cannot be added because the content tablename is empty.");
                                continue;
                            }
                            core.db.createSQLTableField(metaKvp.Value.tableName, fieldKvp.Value.nameLc, fieldKvp.Value.fieldTypeId);
                        }
                    }
                    core.clearMetaData();
                    core.cache.invalidateAll();
                }
                //
                //----------------------------------------------------------------------------------------------------------------------
                LogController.logInfo(core, "metadata Load, stage 2: if baseCollection, reset isBaseContent and isBaseField");
                //----------------------------------------------------------------------------------------------------------------------
                //
                if (isBaseCollection) {
                    core.db.executeNonQuery("update ccfields set isBaseField=0");
                    core.db.executeNonQuery("update ccContent set isBaseContent=0");
                }
                //
                //----------------------------------------------------------------------------------------------------------------------
                LogController.logInfo(core, "metadata Load, stage 3: Verify all metadata names in ccContent so GetContentID calls will succeed");
                //----------------------------------------------------------------------------------------------------------------------
                //
                List<string> installedContentList = new List<string>();
                using (DataTable rs = core.db.executeQuery("SELECT Name from ccContent where (active<>0)")) {
                    if (DbController.isDataTableOk(rs)) {
                        installedContentList = new List<string>(convertDataTableColumntoItemList(rs));
                    }
                }
                //
                foreach (var keypairvalue in Collection.metaData) {
                    if (keypairvalue.Value.dataChanged) {
                        LogController.logInfo(core, "adding metadata name [" + keypairvalue.Value.name + "]");
                        if (!installedContentList.Contains(keypairvalue.Value.name.ToLowerInvariant())) {
                            core.db.executeNonQuery("Insert into ccContent (name,ccguid,active,createkey)values(" + DbController.encodeSQLText(keypairvalue.Value.name) + "," + DbController.encodeSQLText(keypairvalue.Value.guid) + ",1,0);");
                            installedContentList.Add(keypairvalue.Value.name.ToLowerInvariant());
                        }
                    }
                }
                core.clearMetaData();
                core.cache.invalidateAll();
                //
                //----------------------------------------------------------------------------------------------------------------------
                LogController.logInfo(core, "metadata Load, stage 4: Verify content records required for Content Server");
                //----------------------------------------------------------------------------------------------------------------------
                //
                BuildController.verifySortMethods(core);
                BuildController.verifyContentFieldTypes(core);
                core.clearMetaData();
                core.cache.invalidateAll();
                //
                //----------------------------------------------------------------------------------------------------------------------
                LogController.logInfo(core, "metadata Load, stage 5: verify 'Content' content definition");
                //----------------------------------------------------------------------------------------------------------------------
                //
                foreach (var keypairvalue in Collection.metaData) {
                    if (keypairvalue.Value.name.ToLowerInvariant() == "content") {
                        installMetaDataMiniCollection_buildDb_saveMetaDataToDb(core, keypairvalue.Value, logMsgContext);
                        break;
                    }
                }
                core.clearMetaData();
                core.cache.invalidateAll();
                //
                //----------------------------------------------------------------------------------------------------------------------
                LogController.logInfo(core, "metadata Load, stage 6: Verify all definitions and fields");
                //----------------------------------------------------------------------------------------------------------------------
                //
                foreach (var keypairvalue in Collection.metaData) {
                    ContentMetadataModel workingMetaData = keypairvalue.Value;
                    bool fieldChanged = false;
                    if (!workingMetaData.dataChanged) {
                        foreach (var field in workingMetaData.fields) {
                            fieldChanged = field.Value.dataChanged;
                            if (fieldChanged) { break; }
                        }
                    }
                    if ((fieldChanged || workingMetaData.dataChanged) && (workingMetaData.name.ToLowerInvariant() != "content")) {
                        installMetaDataMiniCollection_buildDb_saveMetaDataToDb(core, workingMetaData, logMsgContext);
                    }
                }
                core.clearMetaData();
                core.cache.invalidateAll();
                //
                //----------------------------------------------------------------------------------------------------------------------
                LogController.logInfo(core, "metadata Load, stage 7: Verify all field help");
                //----------------------------------------------------------------------------------------------------------------------
                //
                int FieldHelpCId = MetadataController.getRecordIdByUniqueName(core, "content", "Content Field Help");
                foreach (var keypairvalue in Collection.metaData) {
                    ContentMetadataModel workingMetaData = keypairvalue.Value;
                    foreach (var fieldKeyValuePair in workingMetaData.fields) {
                        ContentFieldMetadataModel workingField = fieldKeyValuePair.Value;
                        if (workingField.helpChanged) {
                            int fieldId = 0;
                            using (var rs = core.db.executeQuery("select f.id from ccfields f left join cccontent c on c.id=f.contentid where (f.name=" + DbController.encodeSQLText(workingField.nameLc) + ")and(c.name=" + DbController.encodeSQLText(workingMetaData.name) + ") order by f.id")) {
                                if (DbController.isDataTableOk(rs)) {
                                    fieldId = GenericController.encodeInteger(DbController.getDataRowFieldText(rs.Rows[0], "id"));
                                }
                            }
                            if (fieldId == 0) {
                                LogController.logWarn(core, "Field help specified for a field that cannot be found, field [" + workingField.nameLc + "], content [" + workingMetaData.name + "]");
                            } else {
                                int FieldHelpId = 0;
                                using (var rs = core.db.executeQuery("select id from ccfieldhelp where fieldid=" + fieldId + " order by id")) {
                                    if (DbController.isDataTableOk(rs)) {
                                        FieldHelpId = GenericController.encodeInteger(rs.Rows[0]["id"]);
                                    } else {
                                        FieldHelpId = core.db.insertGetId("ccfieldhelp", 0);
                                    }
                                }
                                if (FieldHelpId != 0) {
                                    string Copy = workingField.helpCustom;
                                    if (string.IsNullOrEmpty(Copy)) { Copy = workingField.helpDefault; }
                                    core.db.executeNonQuery("update ccfieldhelp set active=1,contentcontrolid=" + FieldHelpCId + ",fieldid=" + fieldId + ",helpdefault=" + DbController.encodeSQLText(Copy) + " where id=" + FieldHelpId);
                                }
                            }
                        }
                    }
                }
                core.clearMetaData();
                core.cache.invalidateAll();
                //
                //----------------------------------------------------------------------------------------------------------------------
                LogController.logInfo(core, "metadata Load, stage 8: create SQL indexes");
                //----------------------------------------------------------------------------------------------------------------------
                //
                foreach (MetadataMiniCollectionModel.MiniCollectionSQLIndexModel index in Collection.sqlIndexes) {
                    if (index.dataChanged) {
                        using (var db = new DbController(core, index.dataSourceName)) {
                            LogController.logInfo(core, "creating index [" + index.indexName + "], fields [" + index.fieldNameList + "], on table [" + index.tableName + "]");
                            db.createSQLIndex(index.tableName, index.indexName, index.fieldNameList);
                        }
                    }
                }
                core.clearMetaData();
                core.cache.invalidateAll();
                //
                //----------------------------------------------------------------------------------------------------------------------
                LogController.logInfo(core, "metadata Load, stage 9: Verify All Menu Names, then all Menus");
                //----------------------------------------------------------------------------------------------------------------------
                //
                foreach (var kvp in Collection.menus) {
                    var menu = kvp.Value;
                    if (menu.dataChanged) {
                        LogController.logInfo(core, "creating navigator entry [" + menu.name + "], namespace [" + menu.menuNameSpace + "], guid [" + menu.guid + "]");
                        BuildController.verifyNavigatorEntry(core, menu, 0);
                    }
                }
                //
                //----------------------------------------------------------------------------------------------------------------------
                LogController.logInfo(core, "metadata Load, stage 9: Verify Styles");
                //----------------------------------------------------------------------------------------------------------------------
                //
                if (Collection.styleCnt > 0) {
                    string SiteStyles = core.cdnFiles.readFileText("templates/styles.css");
                    string[] SiteStyleSplit = { };
                    int SiteStyleCnt = 0;
                    if (!string.IsNullOrEmpty(SiteStyles.Trim(' '))) {
                        //
                        // Split with an extra character at the end to guarantee there is an extra split at the end
                        //
                        SiteStyleSplit = (SiteStyles + " ").Split('}');
                        SiteStyleCnt = SiteStyleSplit.GetUpperBound(0) + 1;
                    }
                    var StyleSheetAdd = new StringBuilder();
                    for (var Ptr = 0; Ptr < Collection.styleCnt; Ptr++) {
                        bool Found = false;
                        var tempVar4 = Collection.styles[Ptr];
                        if (tempVar4.dataChanged) {
                            string NewStyleName = tempVar4.name;
                            string NewStyleValue = tempVar4.copy;
                            NewStyleValue = GenericController.strReplace(NewStyleValue, "}", "");
                            NewStyleValue = GenericController.strReplace(NewStyleValue, "{", "");
                            if (SiteStyleCnt > 0) {
                                int SiteStylePtr = 0;
                                for (SiteStylePtr = 0; SiteStylePtr < SiteStyleCnt; SiteStylePtr++) {
                                    string StyleLine = SiteStyleSplit[SiteStylePtr];
                                    int PosNameLineEnd = StyleLine.LastIndexOf("{", StringComparison.InvariantCulture) + 1;
                                    if (PosNameLineEnd > 0) {
                                        int PosNameLineStart = StyleLine.LastIndexOf(Environment.NewLine, PosNameLineEnd - 1, StringComparison.InvariantCulture) + 1;
                                        if (PosNameLineStart > 0) {
                                            //
                                            // Check this site style for a match with the NewStyleName
                                            //
                                            PosNameLineStart = PosNameLineStart + 2;
                                            string TestStyleName = (StyleLine.Substring(PosNameLineStart - 1, PosNameLineEnd - PosNameLineStart)).Trim(' ');
                                            if (GenericController.toLCase(TestStyleName) == GenericController.toLCase(NewStyleName)) {
                                                Found = true;
                                                if (tempVar4.overwrite) {
                                                    //
                                                    // Found - Update style
                                                    //
                                                    SiteStyleSplit[SiteStylePtr] = Environment.NewLine + tempVar4.name + " {" + NewStyleValue;
                                                }
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            //
                            // Add or update the stylesheet
                            //
                            if (!Found) {
                                StyleSheetAdd.Append(Environment.NewLine + NewStyleName + " {" + NewStyleValue + "}");
                            }
                        }
                    }
                    SiteStyles = string.Join("}", SiteStyleSplit);
                    if (!StyleSheetAdd.Length.Equals(0)) {
                        SiteStyles = SiteStyles
                            + Environment.NewLine + "\r\n/*"
                            + Environment.NewLine + "Styles added " + core.dateTimeNowMockable + Environment.NewLine + "*/"
                            + Environment.NewLine + StyleSheetAdd;
                    }
                    core.wwwFiles.saveFile("templates/styles.css", SiteStyles);
                    //
                    // -- Update stylesheet cache
                    core.siteProperties.setProperty("StylesheetSerialNumber", "-1");
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Update a table from a collection metadata node
        /// </summary>
        internal static void installMetaDataMiniCollection_buildDb_saveMetaDataToDb(CoreController core, ContentMetadataModel contentMetadata, string logMsgContext) {
            try {
                //
                logMsgContext += ", updating db metadata for content [" + contentMetadata.name + "]";
                LogController.logInfo(core, logMsgContext);
                //
                // -- get contentid and protect content with IsBaseContent true
                {
                    if (contentMetadata.dataChanged) {
                        //
                        // -- update definition (use SingleRecord as an update flag)
                        var datasource = DataSourceModel.createByUniqueName(core.cpParent, contentMetadata.dataSourceName);
                        ContentMetadataModel.verifyContent_returnId(core, contentMetadata, logMsgContext);
                    }
                    //
                    // -- update Content Field Records and Content Field Help records
                    ContentMetadataModel metaDataFieldHelp = ContentMetadataModel.createByUniqueName(core, ContentFieldHelpModel.tableMetadata.contentName);
                    foreach (var nameValuePair in contentMetadata.fields) {
                        ContentFieldMetadataModel fieldMetadata = nameValuePair.Value;
                        if (fieldMetadata.dataChanged) {
                            contentMetadata.verifyContentField(core, fieldMetadata, false, logMsgContext);
                        }
                        //
                        // -- update content field help records
                        if (fieldMetadata.helpChanged) {
                            ContentFieldHelpModel fieldHelp = null;
                            var fieldHelpList = DbBaseModel.createList<ContentFieldHelpModel>(core.cpParent, "fieldid=" + fieldMetadata.id);
                            if (fieldHelpList.Count == 0) {
                                //
                                // -- no current field help record, if adding help, create record
                                if ((!string.IsNullOrWhiteSpace(fieldMetadata.helpDefault)) || (!string.IsNullOrWhiteSpace(fieldMetadata.helpCustom))) {
                                    fieldHelp = DbBaseModel.addEmpty<ContentFieldHelpModel>(core.cpParent);
                                    fieldHelp.helpDefault = fieldMetadata.helpDefault;
                                    fieldHelp.helpCustom = fieldMetadata.helpCustom;
                                    fieldHelp.save(core.cpParent);
                                }
                            } else {
                                //
                                // -- if help changed, save it
                                fieldHelp = fieldHelpList.First();
                                if ((!fieldHelp.helpCustom.Equals(fieldMetadata.helpCustom)) || !fieldHelp.helpDefault.Equals(fieldMetadata.helpDefault)) {
                                    fieldHelp.helpDefault = fieldMetadata.helpDefault;
                                    fieldHelp.helpCustom = fieldMetadata.helpCustom;
                                    fieldHelp.save(core.cpParent);
                                }
                            }
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
        //
        private static string GetMenuNameSpace(CoreController core, Dictionary<string, MetadataMiniCollectionModel.MiniCollectionMenuModel> menus, MetadataMiniCollectionModel.MiniCollectionMenuModel menu, string UsedIDList) {
            string returnAttr = "";
            try {
                string ParentName = null;
                int Ptr = 0;
                string Prefix = null;
                string LCaseParentName = null;

                //
                ParentName = menu.parentName;
                if (!string.IsNullOrEmpty(ParentName)) {
                    LCaseParentName = GenericController.toLCase(ParentName);
                    foreach (var kvp in menus) {
                        MetadataMiniCollectionModel.MiniCollectionMenuModel testMenu = kvp.Value;
                        if (GenericController.strInstr(1, "," + UsedIDList + ",", "," + Ptr + ",") == 0) {
                            if (LCaseParentName == GenericController.toLCase(testMenu.name) && (menu.isNavigator == testMenu.isNavigator)) {
                                Prefix = GetMenuNameSpace(core, menus, testMenu, UsedIDList + "," + menu.guid);
                                if (string.IsNullOrEmpty(Prefix)) {
                                    returnAttr = ParentName;
                                } else {
                                    returnAttr = Prefix + "." + ParentName;
                                }
                                break;
                            }
                        }

                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
            return returnAttr;
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
