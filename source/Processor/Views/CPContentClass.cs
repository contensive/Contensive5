
using System;
using System.Collections.Generic;
using Contensive.Processor.Controllers;
using Contensive.BaseClasses;
using Contensive.Processor.Models.Domain;
using Contensive.Exceptions;
using Contensive.Models.Db;
using Contensive.CPBase.BaseModels;
using NLog;

namespace Contensive.Processor {
    public class CPContentClass : CPContentBaseClass {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        private readonly CPClass cp;

        public override LastestDateTrackerBaseModel LatestContentModifiedDate {
            get {
                return _LatestContentModifiedDate;
            }
            set { }
        }
        private readonly LastestDateTracker _LatestContentModifiedDate = new LastestDateTracker();
        //
        //====================================================================================================
        /// <summary>
        /// construct
        /// </summary>
        /// <param name="cp"></param>
        public CPContentClass(CPClass cp) {
            this.cp = cp;
        }
        //
        //====================================================================================================
        //
        public override int GetTableID(string tableName) {
            var table = DbBaseModel.createByUniqueName<TableModel>(cp, tableName);
            if (table == null) { return 0; }
            return table.id;
        }
        //
        //====================================================================================================
        //
        public override string GetCopy(string copyName, string DefaultContent) {
            return cp.core.html.getContentCopy(copyName, DefaultContent, cp.core.session.user.id, true, cp.core.session.isAuthenticated);
        }
        //
        public override string GetCopy(string copyName) {
            return cp.core.html.getContentCopy(copyName, "", cp.core.session.user.id, true, cp.core.session.isAuthenticated);
        }
        //
        //====================================================================================================
        //
        public override string GetCopy(string copyName, string defaultContent, int personalizationPeopleId) {
            return cp.core.html.getContentCopy(copyName, defaultContent, personalizationPeopleId, true, cp.core.session.isAuthenticated);
        }
        //
        //====================================================================================================
        //
        public override void SetCopy(string copyName, string content) {
            cp.core.html.setContentCopy(copyName, content);
        }
        //
        //====================================================================================================
        //
        public override string GetAddLink(string contentName, string presetNameValueList, bool allowPaste, bool isEditing, bool includechildContent) {
            string result = "";
            foreach (var link in AdminUIEditButtonController.getLegacyAddTab(cp.core, contentName, presetNameValueList, allowPaste, isEditing, includechildContent)) {
                result += link;
            }
            return result;
        }
        //
        //====================================================================================================
        //
        public override string GetAddLink(string contentName, string presetNameValueList, bool allowPaste, bool isEditing) {
            string result = "";
            foreach (var link in AdminUIEditButtonController.getLegacyAddTab(cp.core, contentName, presetNameValueList, allowPaste, isEditing, false)) {
                result += link;
            }
            return result;
        }
        //
        //====================================================================================================
        //
        public override string GetAddLink(string contentName, string presetNameValueList) {
            string result = "";
            bool isEditing = cp.core.session.isEditing(contentName);
            foreach (var link in AdminUIEditButtonController.getLegacyAddTab(cp.core, contentName, presetNameValueList, false, isEditing, false)) {
                result += link;
            }
            return result;
        }
        //
        //====================================================================================================
        //
        public override string GetAddLink(string contentName) {
            string result = "";
            bool isEditing = cp.core.session.isEditing(contentName);
            foreach (var link in AdminUIEditButtonController.getLegacyAddTab(cp.core, contentName, "", false, isEditing, false)) {
                result += link;
            }
            return result;
        }
        //
        //====================================================================================================
        //
        public override string GetAddLink(int contentId, string PresetNameValueList, bool includeChildContent) {
            bool isEditing = cp.core.session.isEditing(contentId.ToString());
            return GetAddLink(cp.Content.GetName(contentId), PresetNameValueList, false, isEditing, includeChildContent);
        }
        //
        //====================================================================================================
        //
        public override string GetAddLink(int contentId, string PresetNameValueList) {
            bool isEditing = cp.core.session.isEditing(contentId.ToString());
            return GetAddLink(cp.Content.GetName(contentId), PresetNameValueList, false, isEditing, false);
        }
        //
        //====================================================================================================
        //
        public override string GetAddLink(int contentId) {
            bool isEditing = cp.core.session.isEditing(contentId.ToString());
            return GetAddLink(cp.Content.GetName(contentId), "", false, isEditing, false);
        }
        //
        //====================================================================================================
        //
        public override string GetContentControlCriteria(string contentName) {
            var meta = ContentMetadataModel.createByUniqueName(cp.core, contentName);
            if (meta != null) { return meta.legacyContentControlCriteria; }
            return string.Empty;
        }
        //
        //====================================================================================================
        //
        [Obsolete("deprecated, instead create contentMeta, lookup field and use property of field", false)]
        public override string GetFieldProperty(string contentName, string fieldName, string propertyName) {
            throw new GenericException("getContentFieldProperty deprecated, instead create contentMeta, lookup field and use property of field");
        }
        //
        //====================================================================================================
        //
        public override string GetDataSource(string contentName) {
            var meta = ContentMetadataModel.createByUniqueName(cp.core, contentName);
            if (meta != null) { return meta.dataSourceName; }
            return string.Empty;
        }
        //
        //====================================================================================================
        //
        public override string GetEditLink(string contentName, string recordID, bool allowCut, string recordName, bool isEditing) {
            return AdminUIEditButtonController.getEditTab(cp.core, contentName, GenericController.encodeInteger(recordID), allowCut, recordName);
        }
        //
        public override string GetEditLink(string contentName, string recordID, bool allowCut, string recordName, bool isEditing, string customCaption) {
            return AdminUIEditButtonController.getEditTab(cp.core, contentName, GenericController.encodeInteger(recordID), allowCut, recordName, customCaption);
        }
        //
        //====================================================================================================
        //
        public override string GetEditLink(string contentName, int recordId) {
            return AdminUIEditButtonController.getEditTab(cp.core, contentName, recordId, false, "");
        }
        //
        public override string GetEditLink(string contentName, int recordId, string customCaption) {
            return AdminUIEditButtonController.getEditTab(cp.core, contentName, recordId, false, "",  customCaption);
        }
        //
        //====================================================================================================
        //
        public override string GetEditLink(string contentName, string recordGuid) {
            int recordId = MetadataController.getRecordId(cp.core, contentName, recordGuid);
            return AdminUIEditButtonController.getEditTab(cp.core, contentName, recordId, false, "");
            //
            //var contentMetadata = ContentMetadataModel.createByUniqueName(cp.core, contentName);
            //if (contentMetadata == null) { throw new GenericException("ContentName [" + contentName + "], but no content metadata found with this name."); }
            //return AdminUIEditButtonController.getLegacyRecordEditAnchorTag(cp.core, contentMetadata, recordGuid);
        }
        //
        public override string GetEditLink(string contentName, string recordGuid, string customCaption) {
            int recordId = MetadataController.getRecordId(cp.core, contentName, recordGuid);
            return AdminUIEditButtonController.getEditTab(cp.core, contentName, recordId, false, customCaption);
            //
            //var contentMetadata = ContentMetadataModel.createByUniqueName(cp.core, contentName);
            //if (contentMetadata == null) { throw new GenericException("ContentName [" + contentName + "], but no content metadata found with this name."); }
            //return AdminUIEditButtonController.getLegacyRecordEditAnchorTag(cp.core, contentMetadata, recordGuid, customCaption);
        }
        //
        //====================================================================================================
        //
        public override string GetEditLink(int contentId, int recordId) {
            var contentMetadata = ContentMetadataModel.create(cp.core, contentId);
            return AdminUIEditButtonController.getEditTab(cp.core, contentMetadata, recordId, false, "", "");
            //
            //if (contentMetadata == null) { throw new GenericException("contentId [" + contentId + "], but no content metadata found."); }
            //return AdminUIEditButtonController.getLegacyRecordEditAnchorTag(cp.core, contentMetadata, recordId);
        }
        //
        public override string GetEditLink(int contentId, int recordId, string customCaption) {
            var contentMetadata = ContentMetadataModel.create(cp.core, contentId);
            return AdminUIEditButtonController.getEditTab(cp.core, contentMetadata, recordId, false, "", customCaption);
            //
            //var contentMetadata = ContentMetadataModel.create(cp.core, contentId);
            //if (contentMetadata == null) { throw new GenericException("contentId [" + contentId + "], but no content metadata found."); }
            //return AdminUIEditButtonController.getLegacyRecordEditAnchorTag(cp.core, contentMetadata, recordId,  customCaption);
        }
        //
        //====================================================================================================
        //
        public override string GetEditLink(int contentId, string recordGuid) {
            var contentMetadata = ContentMetadataModel.create(cp.core, contentId);
            int recordId = MetadataController.getRecordId(cp.core, contentMetadata.name, recordGuid);
            return AdminUIEditButtonController.getEditTab(cp.core, contentMetadata, recordId, false, "", "");
            //
            //var contentMetadata = ContentMetadataModel.create(cp.core, contentId);
            //if (contentMetadata == null) { throw new GenericException("contentId [" + contentId + "], but no content metadata found."); }
            //return AdminUIEditButtonController.getLegacyRecordEditAnchorTag(cp.core, contentMetadata, recordGuid);
        }
        //
        public override string GetEditLink(int contentId, string recordGuid, string customCaption) {
            var contentMetadata = ContentMetadataModel.create(cp.core, contentId);
            int recordId = MetadataController.getRecordId(cp.core, contentMetadata.name, recordGuid);
            return AdminUIEditButtonController.getEditTab(cp.core, contentMetadata, recordId, false, "", "");
            //
            //var contentMetadata = ContentMetadataModel.create(cp.core, contentId);
            //if (contentMetadata == null) { throw new GenericException("contentId [" + contentId + "], but no content metadata found."); }
            //return AdminUIEditButtonController.getLegacyRecordEditAnchorTag(cp.core, contentMetadata, recordGuid, customCaption);
        }
        //
        //====================================================================================================
        //
        public override string GetLinkAliasByPageID(int pageID, string queryStringSuffix, string defaultLink) {
            return LinkAliasController.getLinkAlias(cp.core, pageID, queryStringSuffix, defaultLink);
        }
        //
        //====================================================================================================
        //
        public override string GetEditWrapper(string innerHtml) {
            return AdminUIEditButtonController.getEditWrapper(cp.core, innerHtml);
        }
        //
        //====================================================================================================
        //
        public override string GetEditWrapper(string innerHtml, string contentName, int recordId) {
            return AdminUIEditButtonController.getEditTabAndWrapper(cp.core, innerHtml, contentName, recordId);
        }
        //
        //====================================================================================================
        //
        public override string GetEditWrapper(string innerHtml, string contentName, int recordId, string customCaption) {
            return AdminUIEditButtonController.getEditTabAndWrapper(cp.core, innerHtml, contentName, recordId,  customCaption);
        }
        //
        //====================================================================================================
        //
        public override string GetEditWrapper(string innerHtml, string contentName, string recordGuid) {
            return AdminUIEditButtonController.getEditTabAndWrapper(cp.core, innerHtml, contentName, recordGuid);
        }
        //
        //====================================================================================================
        //
        public override string GetEditWrapper(string innerHtml, string contentName, string recordGuid, string customCaption) {
            return AdminUIEditButtonController.getEditTabAndWrapper(cp.core, innerHtml, contentName, recordGuid,  customCaption);
        }
        //
        //====================================================================================================
        //
        public override string GetEditWrapper(string innerHtml, int contentId, int recordId) {
            return AdminUIEditButtonController.getEditTabAndWrapper(cp.core, innerHtml, contentId, recordId);
        }
        //
        //====================================================================================================
        //
        public override string GetEditWrapper(string innerHtml, int contentId, int recordId, string customCaption) {
            return AdminUIEditButtonController.getEditTabAndWrapper(cp.core, innerHtml, contentId, recordId,  customCaption);
        }
        //
        //====================================================================================================
        //
        public override string GetEditWrapper(string innerHtml, int contentId, string recordGuid) {
            return AdminUIEditButtonController.getEditTabAndWrapper(cp.core, innerHtml, contentId, recordGuid);
        }
        //
        //====================================================================================================
        //
        public override string GetEditWrapper(string innerHtml, int contentId, string recordGuid, string customCaption) {
            return AdminUIEditButtonController.getEditTabAndWrapper(cp.core, innerHtml, contentId, recordGuid, customCaption);
        }
        //
        //====================================================================================================
        //
        public override string GetPageLink(int pageID, string queryStringSuffix, bool allowLinkAlias) {
            return PageManagerController.getPageLink(cp.core, pageID, queryStringSuffix, allowLinkAlias, false);
        }
        //
        public override string GetPageLink(int pageID, string queryStringSuffix) {
            return PageManagerController.getPageLink(cp.core, pageID, queryStringSuffix, true, false);
        }
        //
        public override string GetPageLink(int pageID) {
            return PageManagerController.getPageLink(cp.core, pageID, "", true, false);
        }
        //
        //====================================================================================================
        /// <summary>
        /// get a record id from its unique name. If a duplicate exists, the first ordered by id is returned
        /// </summary>
        /// <param name="contentName"></param>
        /// <param name="recordName"></param>
        /// <returns></returns>
        public override int GetRecordID(string contentName, string recordName) {
            return MetadataController.getRecordIdByUniqueName(cp.core, contentName, recordName);
        }
        //
        //====================================================================================================
        /// <summary>
        /// returns the matching record name if a match is found, otherwise blank. Does NOT validate the record.
        /// </summary>
        /// <param name="contentName"></param>
        /// <param name="recordID"></param>
        /// <returns></returns>
        public override string GetRecordName(string contentName, int recordID) {
            var meta = ContentMetadataModel.createByUniqueName(cp.core, contentName);
            if (meta == null) { return string.Empty; }
            return meta.getRecordName(cp.core, recordID);
        }
        //
        //====================================================================================================
        //
        public override string GetTable(string contentName) {
            var meta = ContentMetadataModel.createByUniqueName(cp.core, contentName);
            if (meta == null) { return string.Empty; }
            return meta.tableName;
        }
        //
        //====================================================================================================
        //
        public override bool IsField(string contentName, string fieldName) {
            var contentMetadata = Models.Domain.ContentMetadataModel.createByUniqueName(cp.core, contentName);
            return (contentMetadata == null) ? false : contentMetadata.containsField(cp.core, fieldName);
        }
        //
        //====================================================================================================
        //
        public override bool IsLocked(string contentName, string recordId) {
            var contentTable = TableModel.createByContentName(cp, contentName);
            if (contentTable != null) { return WorkflowController.isRecordLocked(cp.core, contentTable.id, GenericController.encodeInteger(recordId)); }
            return false;
        }
        //
        //====================================================================================================
        //
        public override bool IsChildContent(string childContentID, string parentContentID) {
            var parentMetadata = ContentMetadataModel.create(cp.core, parentContentID);
            return (parentMetadata == null) ? false : parentMetadata.isParentOf(cp.core, GenericController.encodeInteger(childContentID));
        }
        //
        //====================================================================================================
        //
        [Obsolete("Deprecated. Use CP.Layout.GetLayout", false)]
        public override string getLayout(string layoutName)
            => cp.Layout.GetLayoutByName(layoutName);
        //
        //====================================================================================================
        //
        [Obsolete("Deprecated. Use CP.Layout.GetLayout", false)]
        public override string getLayout(string layoutName, string defaultLayout) 
            => cp.Layout.GetLayoutByName(layoutName, defaultLayout);
        //
        //====================================================================================================
        /// <summary>
        /// Deprecated. Use CP.Layout.GetLayout
        /// </summary>
        /// <param name="layoutid"></param>
        /// <returns></returns>
        [Obsolete("Deprecated. Use CP.Layout.GetLayout", false)]
        public override string GetLayout(int layoutid) 
            => cp.Layout.GetLayout(layoutid);
        //
        //====================================================================================================
        //
        public override int AddRecord(string contentName, string recordName) {
            try {
                using CsModel cs = new(cp.core);
                if (cs.insert(contentName)) {
                    cs.set("name", recordName);
                    return cs.getInteger("id");
                }
                return 0;
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        //
        public override int AddRecord(string contentName) {
            try {
                using CsModel cs = new(cp.core);
                if (cs.insert(contentName)) {
                    return cs.getInteger("id");
                }
                return 0;
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        //
        public override void Delete(string contentName, string sqlCriteria) {
            MetadataController.deleteContentRecords(cp.core, contentName, sqlCriteria);
        }
        //
        //====================================================================================================
        //
        public override void DeleteContent(string contentName) {
            DbBaseModel.delete<ContentModel>(cp, ContentMetadataModel.getContentId(cp.core, contentName));
        }
        //
        //====================================================================================================
        //
        public override int AddContentField(string contentName, string fieldName, CPContentBaseClass.FieldTypeIdEnum fieldType) {
            var contentMetadata = ContentMetadataModel.createByUniqueName(cp.core, contentName);
            var fieldMeta = ContentFieldMetadataModel.createDefault(cp.core, fieldName, fieldType);
            contentMetadata.verifyContentField(cp.core, fieldMeta, false, "Api CPContent.AddContentField [" + contentName + "." + fieldName + "]");
            return fieldMeta.id;
        }
        //
        public override int AddContentField(string contentName, string fieldName, int fieldTypeId)
            => AddContentField(contentName, fieldName, (CPContentBaseClass.FieldTypeIdEnum)fieldTypeId);
        //
        //====================================================================================================
        //
        public override int AddContent(string contentName) {
            return AddContent(contentName, contentName.Replace(' '.ToString(), "").Replace(' '.ToString(), ""), "default");
        }
        //
        //====================================================================================================
        //
        public override int AddContent(string contentName, string sqlTableName) {
            return AddContent(contentName, sqlTableName, "default");
        }
        //
        //====================================================================================================
        //
        public override int AddContent(string contentName, string sqlTableName, string dataSourceName) {
            var tmpList = new List<string> { };
            DataSourceModel dataSource = DataSourceModel.createByUniqueName(cp, dataSourceName, ref tmpList);
            return ContentMetadataModel.verifyContent_returnId(cp.core, new Models.Domain.ContentMetadataModel {
                dataSourceName = dataSource.name,
                tableName = sqlTableName,
                name = contentName
            }, "Adding content [" + contentName + "], sqlTableName [" + sqlTableName + "]");
        }
        //
        //====================================================================================================
        //
        [Obsolete("Use GetEditLink() ", false)]
        public override string GetListLink(string contentName) {
            return GetEditLink(contentName, 0);
            //return AdminUIEditButtonController.getLegacyRecordEditAnchorTag(cp.core, Models.Domain.ContentMetadataModel.createByUniqueName(cp.core, contentName));
        }
        //
        //====================================================================================================
        //
        public override int GetID(string contentName) {
            var contentNameDict = cp.core.cacheRuntime.ContentNameDict;
            if (contentNameDict.ContainsKey(contentName.ToLowerInvariant())) { return contentNameDict[contentName.ToLowerInvariant()].id; }
            return 0;
        }
        //
        //====================================================================================================
        //
        public override string GetName(int contentId ) {
            Dictionary<int,ContentModel> contentIdDict = cp.core.cacheRuntime.ContentIdDict;
            if (contentIdDict.ContainsKey(contentId)) { return contentIdDict[contentId].name; }
            return "";
        }
        //
        //====================================================================================================
        // deprecated
        //
        [Obsolete("Use AddRecord( string contentName ) ", false)]
        public override int AddRecord(object ContentName) {
            return AddRecord(cp.Utils.EncodeText(ContentName));
        }
        //
        [Obsolete("workflow editing is deprecated", false)]
        public override bool IsWorkflow(string ContentName) {
            //
            // -- workflow no longer supported (but may come back)
            return false;
        }
        //
        [Obsolete("workflow editing is deprecated", false)]
        public override void PublishEdit(string ContentName, int RecordID) {
            // 
        }
        //
        [Obsolete("workflow editing is deprecated", false)]
        public override void SubmitEdit(string ContentName, int RecordID) {
            //
        }
        //
        [Obsolete("workflow editing is deprecated", false)]
        public override void AbortEdit(string ContentName, int RecordId) {
            // 
        }
        //
        [Obsolete("workflow editing is deprecated", false)]
        public override void ApproveEdit(string ContentName, int RecordId) {
            //
        }
        //
        [Obsolete("Deprecated, template link is not supported", false)]
        public override string GetTemplateLink(int TemplateID) {
            return "";
        }
        //
        [Obsolete("Deprecated, access model properties instead", false)]
        public override string GetProperty(string ContentName, string PropertyName) {
            var contentMetadata = ContentMetadataModel.createByUniqueName(cp.core, ContentName);
            if (contentMetadata == null) { return string.Empty; }
            return contentMetadata.getContentProperty(cp.core, PropertyName);
        }
        //
        [Obsolete("Deprecated, use methods tih FieldTypeIdEnum instead", false)]
        public override int AddContentField(string ContentName, string FieldName, fileTypeIdEnum fileTypeEnum) {
            return AddContentField(ContentName, FieldName, (FieldTypeIdEnum)fileTypeEnum);
        }

        public override string GetEditUrl(string contentName, int recordId) {
            return AdminUIEditButtonController.getEditUrl(cp.core, GetID(contentName), recordId);
        }

        public override string GetEditUrl(string contentName, string recordGuid) {
            return AdminUIEditButtonController.getEditUrl(cp.core, GetID(contentName), recordGuid);
        }

        public override string GetEditUrl(int contentId, int recordId) {
            return AdminUIEditButtonController.getEditUrl(cp.core, contentId, recordId);
        }

        public override string GetEditUrl(int contentId, string recordGuid) {
            return AdminUIEditButtonController.getEditUrl(cp.core, contentId, recordGuid);
        }
    }
}
