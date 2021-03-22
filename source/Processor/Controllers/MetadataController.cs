
using System;
using static Contensive.Processor.Constants;
using Contensive.Processor.Models.Domain;
using System.Data;
using Contensive.Processor.Exceptions;
using Contensive.BaseClasses;
using Contensive.Models.Db;
using System.Globalization;
//
namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// Static methods that support the metadata domain model.
    /// </summary>
    public class MetadataController {
        //
        //========================================================================
        /// <summary>
        /// Get a Contents Tablename from the ContentPointer
        /// </summary>
        /// <param name="core"></param>
        /// <param name="contentName"></param>
        /// <returns></returns>
        public static string getContentTablename(CoreController core, string contentName) {
            try {
                var meta = ContentMetadataModel.createByUniqueName(core, contentName);
                if (meta != null) { return meta.tableName; }
                return string.Empty;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// Get a Contents Name from the ContentID
        /// </summary>
        /// <param name="core"></param>
        /// <param name="contentID"></param>
        /// <returns></returns>
        public static string getContentNameByID(CoreController core, int contentID) {
            try {
                var meta = ContentMetadataModel.create(core, contentID);
                if (meta != null) { return meta.name; }
                return string.Empty;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //=============================================================
        /// <summary>
        /// Return a record name given the record id. If not record is found, blank is returned.
        /// </summary>
        public static string getRecordName(CoreController core, string ContentName, int recordID) {
            try {
                var meta = ContentMetadataModel.createByUniqueName(core, ContentName);
                if (meta == null) { return string.Empty; }
                return meta.getRecordName(core, recordID);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //=============================================================
        /// <summary>
        /// Return a record name given the guid. If not record is found, blank is returned.
        /// </summary>
        public static string getRecordName(CoreController core, string contentName, string recordGuid) {
            try {
                var meta = ContentMetadataModel.createByUniqueName(core, contentName);
                if (meta == null) { return string.Empty; }
                using (DataTable dt = core.db.executeQuery("select top 1 name from " + meta.tableName + " where ccguid=" + DbController.encodeSQLText(recordGuid) + " order by id")) {
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
        //=============================================================
        /// <summary>
        /// get the lowest recordId based on its name. If no record is found, 0 is returned
        /// </summary>
        /// <param name="contentName"></param>
        /// <param name="recordName"></param>
        /// <returns></returns>
        public static int getRecordIdByUniqueName(CoreController core, string contentName, string recordName) {
            try {
                if (String.IsNullOrWhiteSpace(recordName)) { return 0; }
                var meta = ContentMetadataModel.createByUniqueName(core, contentName);
                if ((meta == null) || (String.IsNullOrWhiteSpace(meta.tableName))) { return 0; }
                using (DataTable dt = core.db.executeQuery("select top 1 id from " + meta.tableName + " where name=" + DbController.encodeSQLText(recordName) + " order by id")) {
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
        // todo rename metadata as meta
        //========================================================================
        /// <summary>
        /// returns true if the metadata field exists
        /// </summary>
        /// <param name="ContentID"></param>
        /// <param name="FieldName"></param>
        /// <returns></returns>
        public static bool isMetadataField(CoreController core, int ContentID, string FieldName) {
            var meta = ContentMetadataModel.create(core, ContentID);
            if (meta == null) { return false; }
            return meta.fields.ContainsKey(FieldName.Trim().ToLower(CultureInfo.InvariantCulture));
        }
        //
        //========================================================================
        /// <summary>
        /// Delete Content Record
        /// </summary>
        /// <param name="contentName"></param>
        /// <param name="recordId"></param>
        /// <param name="userId"></param>
        //
        public static void deleteContentRecord(CoreController core, string contentName, int recordId, int userId = SystemMemberId) {
            var meta = ContentMetadataModel.createByUniqueName(core, contentName);
            if (meta == null) { return; }
            using (var db = new DbController(core, meta.dataSourceName)) {
                core.db.delete(recordId, meta.tableName);
            }
        }
        //
        //========================================================================
        /// <summary>
        /// 'deleteContentRecords
        /// </summary>
        /// <param name="contentName"></param>
        /// <param name="sqlCriteria"></param>
        /// <param name="userId"></param>
        //
        public static void deleteContentRecords(CoreController core, string contentName, string sqlCriteria, int userId = 0) {
            var meta = ContentMetadataModel.createByUniqueName(core, contentName);
            if (meta == null) { return; }
            using (var db = new DbController(core, meta.dataSourceName)) {
                core.db.deleteRows(meta.tableName, sqlCriteria);
            }
        }
        //
        //========================================================================
        /// <summary>
        /// Encode a value for a sql
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="fieldType"></param>
        /// <returns></returns>
        public static string encodeSQL(object expression, CPContentBaseClass.FieldTypeIdEnum fieldType = CPContentBaseClass.FieldTypeIdEnum.Text) {
            try {
                switch (fieldType) {
                    case CPContentBaseClass.FieldTypeIdEnum.Boolean:
                        return DbController.encodeSQLBoolean(GenericController.encodeBoolean(expression));
                    case CPContentBaseClass.FieldTypeIdEnum.Currency:
                    case CPContentBaseClass.FieldTypeIdEnum.Float:
                        return DbController.encodeSQLNumber(GenericController.encodeNumber(expression));
                    case CPContentBaseClass.FieldTypeIdEnum.AutoIdIncrement:
                    case CPContentBaseClass.FieldTypeIdEnum.Integer:
                    case CPContentBaseClass.FieldTypeIdEnum.Lookup:
                    case CPContentBaseClass.FieldTypeIdEnum.MemberSelect:
                        return DbController.encodeSQLNumber(GenericController.encodeInteger(expression));
                    case CPContentBaseClass.FieldTypeIdEnum.Date:
                        return DbController.encodeSQLDate(GenericController.encodeDate(expression));
                    case CPContentBaseClass.FieldTypeIdEnum.LongText:
                    case CPContentBaseClass.FieldTypeIdEnum.HTML:
                    case CPContentBaseClass.FieldTypeIdEnum.HTMLCode:
                        return DbController.encodeSQLText(GenericController.encodeText(expression));
                    case CPContentBaseClass.FieldTypeIdEnum.File:
                    case CPContentBaseClass.FieldTypeIdEnum.FileImage:
                    case CPContentBaseClass.FieldTypeIdEnum.Link:
                    case CPContentBaseClass.FieldTypeIdEnum.ResourceLink:
                    case CPContentBaseClass.FieldTypeIdEnum.Redirect:
                    case CPContentBaseClass.FieldTypeIdEnum.ManyToMany:
                    case CPContentBaseClass.FieldTypeIdEnum.Text:
                    case CPContentBaseClass.FieldTypeIdEnum.FileText:
                    case CPContentBaseClass.FieldTypeIdEnum.FileJavascript:
                    case CPContentBaseClass.FieldTypeIdEnum.FileXML:
                    case CPContentBaseClass.FieldTypeIdEnum.FileCSS:
                    case CPContentBaseClass.FieldTypeIdEnum.FileHTML:
                    case CPContentBaseClass.FieldTypeIdEnum.FileHTMLCode:
                        return DbController.encodeSQLText(GenericController.encodeText(expression));
                    default:
                        throw new GenericException("Unknown Field Type [" + fieldType + "");
                }
            } catch (Exception) {
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// Create a filename for the Virtual Directory
        /// </summary>
        /// <param name="contentName"></param>
        /// <param name="fieldName"></param>
        /// <param name="recordId"></param>
        /// <param name="originalFilename"></param>
        /// <returns></returns>
        public static string getVirtualFilename(CoreController core, string contentName, string fieldName, int recordId, string originalFilename = "") {
            try {
                if (string.IsNullOrEmpty(contentName.Trim())) { throw new ArgumentException("contentname cannot be blank"); }
                if (string.IsNullOrEmpty(fieldName.Trim())) { throw new ArgumentException("fieldname cannot be blank"); }
                if (recordId <= 0) { throw new ArgumentException("recordid is not valid"); }
                var meta = ContentMetadataModel.createByUniqueName(core, contentName);
                if (meta == null) { throw new ArgumentException("contentName is not valid"); }
                string workingFieldName = fieldName.Trim().ToLowerInvariant();
                if (!meta.fields.ContainsKey(workingFieldName)) { throw new ArgumentException("content metadata does not include field [" + fieldName + "]"); }
                if (string.IsNullOrEmpty(originalFilename)) { return FileController.getVirtualRecordUnixPathFilename(meta.tableName, fieldName, recordId, meta.fields[fieldName.ToLowerInvariant()].fieldTypeId); }
                return FileController.getVirtualRecordUnixPathFilename(meta.tableName, fieldName, recordId, originalFilename);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// get the id of the table record for a content
        /// </summary>
        /// <param name="contentName"></param>
        /// <returns></returns>
        public static int getContentTableID(CoreController core, string contentName) {
            var meta = ContentMetadataModel.createByUniqueName(core, contentName);
            if (meta == null) { return 0; }
            var table = DbBaseModel.createByUniqueName<TableModel>(core.cpParent, meta.tableName);
            if (table != null) { return table.id; }
            return 0;
        }
        //
        //==================================================================================================
        /// <summary>
        /// Remove this record from all watch lists
        /// </summary>
        /// <param name="ContentID"></param>
        /// <param name="RecordID"></param>
        //
        public static void deleteContentRules(CoreController core, ContentMetadataModel meta, int RecordID) {
            try {
                if (meta == null) { return; }
                if (RecordID <= 0) { throw new GenericException("RecordID [" + RecordID + "] where blank"); }
                string ContentRecordKey = meta.id.ToString() + "." + RecordID.ToString();
                //
                // ----- Table Specific rules
                switch (meta.tableName.ToUpperInvariant()) {
                    case "CCCONTENT":
                        //
                        deleteContentRecords(core, "Group Rules", "ContentID=" + RecordID);
                        break;
                    case "CCCONTENTWATCH":
                        //
                        deleteContentRecords(core, "Content Watch List Rules", "Contentwatchid=" + RecordID);
                        break;
                    case "CCCONTENTWATCHLISTS":
                        //
                        deleteContentRecords(core, "Content Watch List Rules", "Contentwatchlistid=" + RecordID);
                        break;
                    case "CCGROUPS":
                        //
                        deleteContentRecords(core, "Group Rules", "GroupID=" + RecordID);
                        deleteContentRecords(core, "Library Folder Rules", "GroupID=" + RecordID);
                        deleteContentRecords(core, "Member Rules", "GroupID=" + RecordID);
                        deleteContentRecords(core, "Page Content Block Rules", "GroupID=" + RecordID);
                        break;
                    case "CCLIBRARYFOLDERS":
                        //
                        deleteContentRecords(core, "Library Folder Rules", "FolderID=" + RecordID);
                        break;
                    case "CCMEMBERS":
                        //
                        deleteContentRecords(core, "Member Rules", "MemberID=" + RecordID);
                        deleteContentRecords(core, "Topic Habits", "MemberID=" + RecordID);
                        deleteContentRecords(core, "Member Topic Rules", "MemberID=" + RecordID);
                        break;
                    case "CCPAGECONTENT":
                        //
                        deleteContentRecords(core, "Page Content Block Rules", "RecordID=" + RecordID);
                        deleteContentRecords(core, "Page Content Topic Rules", "PageID=" + RecordID);
                        break;
                    case "CCSURVEYQUESTIONS":
                        //
                        deleteContentRecords(core, "Survey Results", "QuestionID=" + RecordID);
                        break;
                    case "CCSURVEYS":
                        //
                        deleteContentRecords(core, "Survey Questions", "SurveyID=" + RecordID);
                        break;
                    case "CCTOPICS":
                        //
                        deleteContentRecords(core, "Topic Habits", "TopicID=" + RecordID);
                        deleteContentRecords(core, "Page Content Topic Rules", "TopicID=" + RecordID);
                        deleteContentRecords(core, "Member Topic Rules", "TopicID=" + RecordID);
                        break;
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}