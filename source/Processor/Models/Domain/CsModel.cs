
using Contensive.BaseClasses;
using Contensive.Processor.Controllers;
using Contensive.Processor.Exceptions;
using Contensive.Processor.Models.Domain;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Globalization;
using System.Text;
using static Contensive.Processor.Controllers.GenericController;
//
namespace Contensive.Processor {
    //
    //==========================================================================================
    /// <summary>
    /// A layer on top of DbController that accesses data using the 'content' metadata
    /// </summary>
    public class CsModel : IDisposable {
        /// <summary>
        /// dependencies
        /// </summary>
        private readonly CoreController core;
        /// <summary>
        /// The user that opened this set
        /// </summary>
        private int userId;
        /// <summary>
        /// If true, it is in use
        /// </summary>
        private bool isOpen;
        /// <summary>
        /// Can be used to write to the record. True if opened with a content definition.
        /// </summary>
        private bool createdWithMetaData;
        /// <summary>
        /// Can be read. True if created with open() or openSql(), false if created with openForUpdate()
        /// </summary>
        private bool readable;
        ///// <summary>
        ///// ***** should be removed. This should be the same as metaData.name
        ///// </summary>
        private string contentName;
        /// <summary>
        /// If opened with a content name, this is that content's metadata
        /// </summary>
        private Models.Domain.ContentMetadataModel contentMeta;
        /// <summary>
        /// data that needs to be written to the database on the next save
        /// </summary>
        private Dictionary<string, string> writeCache;
        ///// <summary>
        ///// The read object, null when empty otherwise it needs to be disposed
        ///// </summary>
        private DataTable dt;
        /// <summary>
        /// Holds the SQL that created the result set
        /// </summary>
        private string sqlSource;
        /// <summary>
        /// 
        /// </summary>
        private int fieldPointer;
        /// <summary>
        /// 1-D array of the result field names
        /// </summary>
        private string[] fieldNames;
        /// <summary>
        /// number of columns in the fieldNames and readCache
        /// </summary>
        private int resultColumnCount;
        /// <summary>
        /// 2-D array of the result rows/columns
        /// </summary>
        private string[,] readCache;
        /// <summary>
        /// number of rows in the readCache
        /// </summary>
        private int readCacheRowCnt;
        /// <summary>
        /// Pointer to the current result row, first row is 0, BOF is -1
        /// </summary>
        private int readCacheRowPtr;
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="core"></param>
        public CsModel(CoreController core) {
            this.core = core;
            //
            // -- capture userId at the time data opened
            userId = core.session.user.id;
        }
        //
        //====================================================================================================
        /// <summary>
        /// close the read data set, save what needs to be written
        /// </summary>
        /// <param name="asyncSave"></param>
        public void close(bool asyncSave) {
            try {
                if (isOpen) {
                    save(asyncSave);
                    readCache = new string[,] { { }, { } };
                    writeCache = new Dictionary<string, string>();
                    resultColumnCount = 0;
                    readCacheRowCnt = 0;
                    readCacheRowPtr = -1;
                    isOpen = false;
                    if (dt != null) {
                        dt.Dispose();
                        dt = null;
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, LogController.processLogMessage(core, "exception", true));
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// close the read data set, save what needs to be written
        /// </summary>
        public void close() => close(false);
        //
        //====================================================================================================
        /// <summary>
        /// Copy the current dataset to a target set. Target must be already opened or inserted
        /// </summary>
        /// <param name="destination"></param>
        public void copyRecord(CsModel destination) {
            try {
                if (!isOpen) { throw new ArgumentException("source dataset is not valid"); }
                if (!destination.isOpen) { throw new ArgumentException("destination dataset is not valid"); }
                if (contentMeta == null) { throw new ArgumentException("copyRecord requires the source and destination datasets be created from an open or insert, not a sql."); }
                if (destination.contentMeta == null) { throw new ArgumentException("copyRecord requires the source and destination datasets be created from an open or insert, not a sql."); }
                //
                foreach (var kvp in contentMeta.fields) {
                    ContentFieldMetadataModel field = kvp.Value;
                    if (!field.nameLc.Equals("id")) {
                        //
                        // ----- fields to copy
                        //
                        switch (field.fieldTypeId) {
                            case CPContentBaseClass.FieldTypeIdEnum.Redirect:
                            case CPContentBaseClass.FieldTypeIdEnum.ManyToMany: {
                                    //
                                    // -- no data saved for these field types
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.File:
                            case CPContentBaseClass.FieldTypeIdEnum.FileImage:
                            case CPContentBaseClass.FieldTypeIdEnum.FileCSS:
                            case CPContentBaseClass.FieldTypeIdEnum.FileXML:
                            case CPContentBaseClass.FieldTypeIdEnum.FileJavascript: {
                                    //
                                    // ----- cdn file
                                    string SourceFilename = getFilename(field.nameLc, "", contentMeta.name, field.fieldTypeId);
                                    if (!string.IsNullOrEmpty(SourceFilename)) {
                                        string DestFilename = destination.getFilename(field.nameLc, "", destination.contentName, field.fieldTypeId);
                                        destination.set(field.nameLc, DestFilename);
                                        core.cdnFiles.copyFile(SourceFilename, DestFilename);
                                    }
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.FileText:
                            case CPContentBaseClass.FieldTypeIdEnum.FileHTML:
                            case CPContentBaseClass.FieldTypeIdEnum.FileHTMLCode: {
                                    //
                                    // ----- private file
                                    string SourceFilename = getFilename(field.nameLc, "", contentMeta.name, field.fieldTypeId);
                                    if (!string.IsNullOrEmpty(SourceFilename)) {
                                        string DestFilename = destination.getFilename(field.nameLc, "", destination.contentName, field.fieldTypeId);
                                        destination.set(field.nameLc, DestFilename);
                                        core.privateFiles.copyFile(SourceFilename, DestFilename);
                                    }
                                    break;
                                }
                            default: {
                                    //
                                    // ----- value
                                    destination.set(field.nameLc, getRawData(field.nameLc));
                                    break;
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
        //========================================================================
        /// <summary>
        /// delete the current record in the dataset
        /// </summary>
        public void deleteRecord() {
            try {
                if (!ok()) { throw new ArgumentException("Cannot delete because data set is empty or at End-Of-file"); }
                if (!createdWithMetaData) { throw new ArgumentException("Cannot delete because data set was created with a query."); }
                if (contentMeta == null) { throw new ArgumentException("Cannot delete because meta data is not valid."); }
                if (string.IsNullOrEmpty(contentMeta.name)) { throw new ArgumentException("Cannot delete because meta data name is blank."); }
                //
                // -- delete any files (if filename is part of select)
                foreach (var selectedFieldName in contentMeta.selectList) {
                    if (contentMeta.fields.ContainsKey(selectedFieldName.ToLowerInvariant())) {
                        var field = contentMeta.fields[selectedFieldName.ToLowerInvariant()];
                        switch (field.fieldTypeId) {
                            case CPContentBaseClass.FieldTypeIdEnum.File:
                            case CPContentBaseClass.FieldTypeIdEnum.FileImage:
                            case CPContentBaseClass.FieldTypeIdEnum.FileCSS:
                            case CPContentBaseClass.FieldTypeIdEnum.FileJavascript:
                            case CPContentBaseClass.FieldTypeIdEnum.FileXML: {
                                    //
                                    // cdn file
                                    string Filename = getRawData(field.nameLc);
                                    if (!string.IsNullOrEmpty(Filename)) { core.cdnFiles.deleteFile(Filename); }
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.FileText:
                            case CPContentBaseClass.FieldTypeIdEnum.FileHTML:
                            case CPContentBaseClass.FieldTypeIdEnum.FileHTMLCode: {
                                    //
                                    // private file
                                    string Filename = getRawData(field.nameLc);
                                    if (!string.IsNullOrEmpty(Filename)) { core.privateFiles.deleteFile(Filename); }
                                    break;
                                }
                            default: {
                                    // do nothing
                                    break;
                                }
                        }
                    }
                }
                //
                int recordId = getInteger("ID");
                using (var db = new DbController(core, contentMeta.dataSourceName)) {
                    db.delete(recordId, contentMeta.tableName);
                }
                MetadataController.deleteContentRules(core, contentMeta, recordId);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// Inserts a record in a table and return a dataset with just that record
        /// </summary>
        /// <param name="contentName"></param>
        /// <returns></returns>
        public bool insert(string contentName) {
            try {
                if (isOpen) { close(); }
                if (string.IsNullOrEmpty(contentName.Trim())) { throw new ArgumentException("Cannot insert new record because Content name is blank."); }
                var meta = ContentMetadataModel.createByUniqueName(core, contentName);
                if (meta == null) { throw new GenericException("Cannot insert new record because Content meta data cannot be found."); }
                if (meta.id <= 0) { throw new GenericException("Cannot insert new record because Content meta data is not valid."); }
                //
                // create default record in Live table
                var sqlList = new NameValueCollection();
                foreach (KeyValuePair<string, Models.Domain.ContentFieldMetadataModel> keyValuePair in meta.fields) {
                    ContentFieldMetadataModel field = keyValuePair.Value;
                    if (!string.IsNullOrEmpty(field.nameLc)) {
                        switch (field.nameLc) {
                            case "modifieddate":
                            case "dateadded": {
                                    //
                                    // -- 
                                    sqlList.Add(field.nameLc, DbController.encodeSQLDate(DateTime.Now));
                                    break;
                                }
                            case "contentcontrolid": {
                                    if (meta.parentId > 0) {
                                        sqlList.Add(field.nameLc, meta.id.ToString());
                                    }
                                    break;
                                }
                            default: {
                                    //
                                    // -- save defaultValue to the insert
                                    if (!string.IsNullOrEmpty(field.defaultValue)) {
                                        switch (field.fieldTypeId) {
                                            case CPContentBaseClass.FieldTypeIdEnum.AutoIdIncrement: {
                                                    //
                                                    // cannot insert an autoincremnt
                                                    break;
                                                }
                                            case CPContentBaseClass.FieldTypeIdEnum.Redirect:
                                            case CPContentBaseClass.FieldTypeIdEnum.ManyToMany: {
                                                    //
                                                    // ignore these fields, they have no associated DB field
                                                    break;
                                                }
                                            case CPContentBaseClass.FieldTypeIdEnum.Boolean: {
                                                    sqlList.Add(field.nameLc, DbController.encodeSQLBoolean(GenericController.encodeBoolean(field.defaultValue)));
                                                    break;
                                                }
                                            case CPContentBaseClass.FieldTypeIdEnum.Currency:
                                            case CPContentBaseClass.FieldTypeIdEnum.Float: {
                                                    sqlList.Add(field.nameLc, DbController.encodeSQLNumber(GenericController.encodeNumber(field.defaultValue)));
                                                    break;
                                                }
                                            case CPContentBaseClass.FieldTypeIdEnum.Integer:
                                            case CPContentBaseClass.FieldTypeIdEnum.MemberSelect: {
                                                    sqlList.Add(field.nameLc, DbController.encodeSQLNumber(GenericController.encodeInteger(field.defaultValue)));
                                                    break;
                                                }
                                            case CPContentBaseClass.FieldTypeIdEnum.Date: {
                                                    sqlList.Add(field.nameLc, DbController.encodeSQLDate(GenericController.encodeDate(field.defaultValue)));
                                                    break;
                                                }
                                            case CPContentBaseClass.FieldTypeIdEnum.Lookup: {
                                                    //
                                                    // refactor --
                                                    // This is a problem - the defaults should come in as the ID values, not the names
                                                    //   so a select can be added to the default configuration page
                                                    //
                                                    string DefaultValueText = GenericController.encodeText(field.defaultValue);
                                                    if (string.IsNullOrEmpty(DefaultValueText)) {
                                                        DefaultValueText = "null";
                                                    } else {
                                                        if (field.lookupContentId != 0) {
                                                            string LookupContentName = MetadataController.getContentNameByID(core, field.lookupContentId);
                                                            if (!string.IsNullOrEmpty(LookupContentName)) {
                                                                DefaultValueText = MetadataController.getRecordIdByUniqueName(core, LookupContentName, DefaultValueText).ToString();
                                                            }
                                                        } else if (field.lookupList != "") {
                                                            string UCaseDefaultValueText = GenericController.toUCase(DefaultValueText);
                                                            string[] lookups = field.lookupList.Split(',');

                                                            int Ptr = 0;
                                                            for (Ptr = 0; Ptr <= lookups.GetUpperBound(0); Ptr++) {
                                                                if (UCaseDefaultValueText == GenericController.toUCase(lookups[Ptr])) {
                                                                    DefaultValueText = (Ptr + 1).ToString();
                                                                }
                                                            }
                                                        }
                                                    }
                                                    sqlList.Add(field.nameLc, DefaultValueText);
                                                    break;
                                                }
                                            default: {
                                                    //
                                                    // else text
                                                    //
                                                    sqlList.Add(field.nameLc, DbController.encodeSQLText(field.defaultValue));
                                                    break;
                                                }
                                        }
                                    }
                                    break;
                                }
                        }
                    }
                }
                //
                using (var db = new DbController(core, meta.dataSourceName)) {
                    init();
                    dt = db.insert(meta.tableName, sqlList, userId);
                    this.readable = true;
                    this.createdWithMetaData = true;
                    this.contentName = contentName;
                    this.sqlSource = "select * from " + meta.tableName + " where id=" + ((int)dt.Rows[0]["id"]).ToString();
                    this.contentMeta = meta;
                    initAfterOpen();
                }
                return ok();
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// return the field value as boolean type
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public bool getBoolean(string fieldName) {
            return encodeBoolean(getRawData(fieldName));
        }
        //
        //====================================================================================================
        /// <summary>
        /// return the field value as an integer type
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public int getInteger(string fieldName) {
            return encodeInteger(getRawData(fieldName));
        }
        //
        //====================================================================================================
        /// <summary>
        /// return the field data as a double
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public double getNumber(string fieldName) {
            return encodeNumber(getRawData(fieldName));
        }
        //
        //====================================================================================================
        /// <summary>
        /// return the field data in DateTime format
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public DateTime getDate(string fieldName) {
            return encodeDate(getRawData(fieldName));
        }
        //
        //========================================================================
        /// <summary>
        /// Return a text field rendered for an Html page (executed embeded addons). For other rendering cases, use getText() and cp.utils.render methods.
        /// </summary>
        /// <param name="FieldName"></param>
        /// <returns></returns>
        public string getTextEncoded(string FieldName) {
            if (!isOpen) { throw new ArgumentException("source dataset is not valid"); }
            if (contentMeta == null) { throw new ArgumentException("Cannot return rendered html because dataset was created with a sql query."); }
            return ActiveContentController.renderHtmlForWeb(core, getText(FieldName), contentMeta.name, getInteger("id"), core.session.user.id, "", 0, CPUtilsBaseClass.addonContext.ContextPage);
        }
        //
        //========================================================================
        /// <summary>
        /// initialize a cs
        /// </summary>
        /// <returns></returns>
        private void init() {
            isOpen = true;
            contentName = "";
            contentMeta = null;
            dt = null;
            fieldNames = null;
            fieldPointer = 0;
            userId = core.session.user.id;
            readCache = null;
            readCacheRowCnt = 0;
            readCacheRowPtr = 0;
            resultColumnCount = 0;
            sqlSource = "";
            createdWithMetaData = false;
            writeCache = null;
        }
        //
        //========================================================================
        /// <summary>
        /// Move to the next row in the dataset
        /// </summary>
        /// <param name="asyncSave"></param>
        public void goNext(bool asyncSave) {
            try {
                if (!ok()) { throw new GenericException("Dataset is not valid."); }
                if (!this.readable) { throw new GenericException("Cannot move to next row because dataset is not readable."); }
                save(asyncSave);
                this.writeCache = new Dictionary<string, string>();
                this.readCacheRowPtr += 1;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        /// <summary>
        /// Move to the next row in the dataset
        /// </summary>
        public void goNext() => goNext(false);
        //
        //========================================================================
        /// <summary>
        /// Move to the first row in the dataset
        /// </summary>
        /// <param name="asyncSave"></param>
        public void goFirst(bool asyncSave) {
            try {
                save(asyncSave);
                this.writeCache = new Dictionary<string, string>();
                this.readCacheRowPtr = 0;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        /// <summary>
        /// Move to the first row in the dataset
        /// </summary>
        public void goFirst() => goFirst(false);
        //
        //========================================================================
        /// <summary>
        /// The value read directly from the field in the Db, or from the write cache.
        /// For textFilenames, this is the filename of the content.
        /// For lookups, this is the id
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public string getRawData(string fieldName) {
            string returnValue = "";
            try {
                string fieldNameTrim = fieldName.Trim();
                if (!ok()) {
                    throw new GenericException("Attempt To GetValue fieldname[" + fieldNameTrim + "], but the dataset Is empty Or does not point To a valid row");
                } else {

                    bool fieldFound = false;
                    if (this.writeCache.Count > 0) {
                        //
                        // ----- something has been set in buffer, check it first
                        if (this.writeCache.ContainsKey(fieldNameTrim.ToLowerInvariant())) {
                            returnValue = this.writeCache[fieldNameTrim.ToLowerInvariant()];
                            fieldFound = true;
                        }
                    }
                    if (!fieldFound) {
                        //
                        // ----- attempt read from readCache
                        if (!this.readable) {
                            //
                            // -- reading from write-only returns default value, because save there is legacy code that detects change bycomparing value to read cache
                            returnValue = "";
                        } else if (this.dt == null) {
                            throw new GenericException("Cannot read from a dataset because the data is not valid.");
                        } else {
                            if (!this.dt.Columns.Contains(fieldNameTrim.ToLowerInvariant())) {
                                if (this.createdWithMetaData) {
                                    var dtFieldList = new List<string>();
                                    foreach (DataColumn column in this.dt.Columns) { dtFieldList.Add(column.ColumnName); }
                                    throw new GenericException("Field [" + fieldNameTrim + "] was not found in [" + this.contentName + "] with selected fields [" + String.Join(",", dtFieldList.ToArray()) + "]");
                                } else {
                                    throw new GenericException("Field [" + fieldNameTrim + "] was not found in sql [" + this.sqlSource + "]");
                                }
                            } else {
                                returnValue = GenericController.encodeText(this.dt.Rows[this.readCacheRowPtr][fieldNameTrim.ToLowerInvariant()]);
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
            return returnValue;
        }
        //
        //========================================================================
        /// <summary>
        /// get the first fieldname in the CS, Returns null if there are no more
        /// </summary>
        /// <returns></returns>
        public string getFirstFieldName() {
            try {
                if (!ok()) { throw new GenericException("data set is not valid"); }
                this.fieldPointer = 0;
                return getNextFieldName();
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// get the next fieldname in the CS, Returns null if there are no more
        /// </summary>
        /// <returns></returns>
        public string getNextFieldName() {
            try {
                if (!ok()) { throw new GenericException("data set is not valid"); }
                while (this.fieldPointer < this.resultColumnCount) {
                    if (!string.IsNullOrWhiteSpace(this.fieldNames[this.fieldPointer])) { return this.fieldNames[this.fieldPointer]; }
                    this.fieldPointer += 1;
                }
                return "";
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// get the type of a field within a csv_ContentSet
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public CPContentBaseClass.FieldTypeIdEnum getFieldTypeId(string fieldName) {
            try {
                if (ok() && this.createdWithMetaData && !string.IsNullOrEmpty(this.contentMeta.name)) { return this.contentMeta.fields[fieldName.ToLowerInvariant()].fieldTypeId; }
                return 0;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// get the caption of a field within a csv_ContentSet
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public string getFieldCaption(string fieldName) {
            string returnResult = "";
            try {
                if (ok() && (this.createdWithMetaData) && (!string.IsNullOrEmpty(this.contentMeta.name))) {
                    returnResult = this.contentMeta.fields[fieldName.ToLowerInvariant()].caption;
                    if (string.IsNullOrEmpty(returnResult)) {
                        returnResult = fieldName;
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
            return returnResult;
        }
        //
        //========================================================================
        /// <summary>
        /// get a list of captions of fields within a data set
        /// </summary>
        /// <returns></returns>
        public string getSelectFieldList() {
            try {
                if (ok()) { return string.Join(",", this.fieldNames); }
                return string.Empty;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// get the caption of a field within a csv_ContentSet
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public bool isFieldSupported(string fieldName) {
            try {
                if (string.IsNullOrEmpty(fieldName)) { throw new ArgumentException("Field name cannot be blank"); }
                if (!ok()) { throw new ArgumentException("dataset is not valid"); }
                return GenericController.isInDelimitedString(getSelectFieldList(), fieldName, ",");
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// get the unix filename that backs the field specified. only valid for fields of TextFile and File type.
        /// Attempt to read the filename from the field
        /// if no filename, attempt to create it from the tablename-recordid
        /// if no recordid, create filename from a random
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="originalFilename"></param>
        /// <param name="contentName"></param>
        /// <returns></returns>
        public string getFilename(string fieldName, string originalFilename, string contentName, CPContentBaseClass.FieldTypeIdEnum fieldTypeId) {
            try {
                string TableName = null;
                int RecordID = 0;
                string fieldNameUpper = null;
                int LenOriginalFilename = 0;
                int LenFilename = 0;
                int Pos = 0;
                string returnFilename;
                //
                if (!ok()) {
                    throw new ArgumentException("the current data set is not valid.");
                } else if (string.IsNullOrEmpty(fieldName)) {
                    throw new ArgumentException("Fieldname Is blank");
                } else {
                    fieldNameUpper = GenericController.toUCase(fieldName.Trim(' '));
                    returnFilename = getRawData(fieldNameUpper);
                    if (!string.IsNullOrEmpty(returnFilename)) {
                        //
                        // ----- A filename came from the record
                        //
                        if (!string.IsNullOrEmpty(originalFilename)) {
                            //
                            // ----- there was an original filename, make sure it matches the one in the record
                            //
                            LenOriginalFilename = originalFilename.Length;
                            LenFilename = returnFilename.Length;
                            Pos = (1 + LenFilename - LenOriginalFilename);
                            if (Pos <= 0) {
                                //
                                // Original Filename changed, create a new 
                                //
                                returnFilename = "";
                            } else if (returnFilename.Substring(Pos - 1) != originalFilename) {
                                //
                                // Original Filename changed, create a new 
                                //
                                returnFilename = "";
                            }
                        }
                    }
                    if (string.IsNullOrEmpty(returnFilename)) {
                        //
                        // ----- no filename present, get id field
                        if (this.resultColumnCount > 0) {
                            for (var FieldPointer = 0; FieldPointer < this.resultColumnCount; FieldPointer++) {
                                if (GenericController.toUCase(this.fieldNames[FieldPointer]) == "ID") {
                                    RecordID = getInteger("ID");
                                    break;
                                }
                            }
                        }
                        //
                        // ----- Get tablename
                        //
                        string workingContentName = contentName;
                        if (this.createdWithMetaData) {
                            //
                            // Get tablename from Content Definition
                            //
                            workingContentName = this.contentMeta.name;
                            TableName = this.contentMeta.tableName;
                        } else if (!string.IsNullOrEmpty(workingContentName)) {
                            //
                            // CS is SQL-based, use the contentname
                            //
                            TableName = MetadataController.getContentTablename(core, workingContentName);
                        } else {
                            //
                            // no Contentname given
                            //
                            throw new GenericException("Cannot create a filename because no ContentName was given, and the dataset is sql-based.");
                        }
                        //
                        // ----- Create filename
                        //
                        CPContentBaseClass.FieldTypeIdEnum workingFieldTypeId = fieldTypeId;
                        if (workingFieldTypeId == 0) {
                            if (string.IsNullOrEmpty(workingContentName)) {
                                if (string.IsNullOrEmpty(originalFilename)) {
                                    workingFieldTypeId = CPContentBaseClass.FieldTypeIdEnum.Text;
                                } else {
                                    workingFieldTypeId = CPContentBaseClass.FieldTypeIdEnum.File;
                                }
                            } else if (this.createdWithMetaData) {
                                //
                                // -- get from metadata
                                workingFieldTypeId = this.contentMeta.fields[fieldName.ToLowerInvariant()].fieldTypeId;
                            } else {
                                //
                                // -- else assume text
                                if (string.IsNullOrEmpty(originalFilename)) {
                                    workingFieldTypeId = CPContentBaseClass.FieldTypeIdEnum.Text;
                                } else {
                                    workingFieldTypeId = CPContentBaseClass.FieldTypeIdEnum.File;
                                }
                            }
                        }
                        if (string.IsNullOrEmpty(originalFilename)) {
                            returnFilename = FileController.getVirtualRecordUnixPathFilename(TableName, fieldName, RecordID, workingFieldTypeId);
                        } else {
                            returnFilename = FileController.getVirtualRecordUnixPathFilename(TableName, fieldName, RecordID, originalFilename);
                        }
                    }
                }
                return returnFilename;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        /// <summary>
        /// get the filename that backs the field specified.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="originalFilename"></param>
        /// <param name="contentName"></param>
        /// <returns></returns>
        public string getFilename(string fieldName, string originalFilename, string contentName) => getFilename(fieldName, originalFilename, contentName, 0);
        /// <summary>
        /// get the unix filename that backs the field specified.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="originalFilename"></param>
        /// <returns></returns>
        public string getFilename(string fieldName, string originalFilename) => getFilename(fieldName, originalFilename, "", 0);
        /// <summary>
        /// get the unix filename that backs the field specified.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public string getFilename(string fieldName) => getFilename(fieldName, "");
        //
        //====================================================================================================
        /// <summary>
        /// if the field uses an underlying filename, use this method to set that filename. The content for the field will switch to that contained by the new file
        /// </summary>
        /// <param name="ignore"></param>
        /// <param name="fieldName"></param>
        /// <param name="filename"></param>
        public void setFilename(string fieldName, string filename) {
            try {
                if (!ok()) { throw new ArgumentException("dataset is not valid"); }
                if (string.IsNullOrEmpty(fieldName)) { throw new ArgumentException("fieldName cannot be blank"); }
                if (!this.createdWithMetaData) { throw new GenericException("Cannot set fields for a dataset based on a query."); }
                if (this.contentMeta == null) { throw new GenericException("Cannot set fields for a dataset based on a query."); }
                if (this.contentMeta.fields == null) { throw new GenericException("The dataset contains no fields."); }
                if (!this.contentMeta.fields.ContainsKey(fieldName.ToLowerInvariant())) { throw new GenericException("The dataset does not contain the field specified [" + fieldName.ToLowerInvariant() + "]."); }
                if (this.writeCache.ContainsKey(fieldName.ToLowerInvariant())) {
                    this.writeCache[fieldName.ToLowerInvariant()] = filename;
                    return;
                }
                this.writeCache.Add(fieldName.ToLowerInvariant(), filename);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================
        /// <summary>
        /// Save text to a field configured to use an external file. Use set() except when the data set is opened with a query.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="copy"></param>
        /// <param name="contentName"></param>
        public void setTextFile(string fieldName, string copy, string contentName) {
            try {
                if (!ok()) { throw new ArgumentException("dataset is not valid"); }
                if (string.IsNullOrEmpty(fieldName)) { throw new ArgumentException("field Name cannot be blank"); }
                if (string.IsNullOrEmpty(contentName)) { throw new ArgumentException("content Name cannot be blank"); }
                if (!createdWithMetaData) { throw new GenericException("Cannot save a data set created by a query."); }
                string OldFilename = getText(fieldName);
                string Filename = getFilename(fieldName, "", contentName, CPContentBaseClass.FieldTypeIdEnum.FileText);
                if (OldFilename != Filename) {
                    //
                    // Filename changed, mark record changed
                    //
                    core.cdnFiles.saveFile(Filename, copy);
                    set(fieldName, Filename);
                } else {
                    string OldCopy = core.cdnFiles.readFileText(Filename);
                    if (OldCopy != copy) {
                        //
                        // copy changed, mark record changed
                        //
                        core.cdnFiles.saveFile(Filename, copy);
                        set(fieldName, Filename);
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// true if csPointer is a valid dataset, and currently points to a valid row
        /// </summary>
        /// <returns></returns>
        public bool ok() {
            try {
                //
                // -- opened with openForUpdate. can be written but not read
                if (this.createdWithMetaData && !this.readable) { return this.isOpen; }
                //
                // -- normal open
                return this.isOpen && (this.readCacheRowPtr >= 0) && (this.readCacheRowPtr < this.readCacheRowCnt);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //       
        //========================================================================
        /// <summary>
        /// Returns the Source for the csv_ContentSet
        /// </summary>
        /// <returns></returns>
        public string getSql() {
            try {
                if (!ok()) { throw new ArgumentException("the dataset is not valid"); }
                return this.sqlSource;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// returns the field value as a string type.
        /// Use for fields having Text types.
        /// For data sets created with queries, the raw data is returned
        /// For fileType fields the content of the file is returned. To get the filename, use GetFilename()
        /// For lookup type fields, the id is converted into the text equivalent, the name of the foreign key or the indexed string in a select list. To get the lookup key, use getInteger().
        /// For date types, a blank is returned for minDate, and the time is removed from the string if time=12:00:00am
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        //
        public string getText(string fieldName) {
            try {
                if (!ok()) { throw new ArgumentException("Cannot read field because the dataset is not valid or end-of-file."); }
                if (string.IsNullOrEmpty(fieldName.Trim())) { throw new ArgumentException("Cannot read field because the fieldname cannot be blank."); }
                if (!this.createdWithMetaData) { return getRawData(fieldName); }
                if (!this.contentMeta.fields.ContainsKey(fieldName.ToLowerInvariant())) { return getRawData(fieldName); }
                var field = this.contentMeta.fields[fieldName.ToLowerInvariant()];
                //
                // -- many-to-many field, special case, return record id list
                if (field.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.ManyToMany) {
                    var result = new StringBuilder();
                    if (this.contentMeta.fields.ContainsKey("id")) {
                        int RecordId = encodeInteger(getRawData("id"));
                        string ContentName = MetadataController.getContentNameByID(core, field.manyToManyRuleContentId);
                        string DbTable = MetadataController.getContentTablename(core, ContentName);
                        string sql = "Select " + field.manyToManyRuleSecondaryField + " from " + DbTable + " where " + field.manyToManyRulePrimaryField + "=" + RecordId;
                        using DataTable dtResult = core.db.executeQuery(sql);
                        if (DbController.isDataTableOk(dtResult)) {
                            foreach (DataRow dr in dtResult.Rows) {
                                result.Append("," + dr[0].ToString());
                            }
                        }
                    }
                    string resultStr = result.ToString();
                    if (string.IsNullOrWhiteSpace(resultStr)) { return string.Empty; }
                    return resultStr.Substring(1);
                }
                //
                // -- redirect field, special case, no data
                if (field.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.Redirect) { return string.Empty; }
                string rawData = getRawData(fieldName);
                if (isNull(rawData)) { return string.Empty; }
                switch (field.fieldTypeId) {
                    case CPContentBaseClass.FieldTypeIdEnum.Boolean: {
                            //
                            // -- boolean
                            if (GenericController.encodeBoolean(rawData)) { return "Yes"; }
                            return "No";
                        }
                    case CPContentBaseClass.FieldTypeIdEnum.Date: {
                            //
                            // -- DateTime
                            DateTime dateValue = GenericController.encodeDate(rawData);
                            if (dateValue == DateTime.MinValue) { return string.Empty; }
                            if (dateValue.Equals(dateValue.Date)) { return dateValue.ToString("d"); }
                            return dateValue.ToString(CultureInfo.InvariantCulture);
                        }
                    case CPContentBaseClass.FieldTypeIdEnum.Lookup: {
                            //
                            // -- Lookup
                            if (!rawData.isNumeric()) { return string.Empty; }
                            if (field.lookupContentId > 0) {
                                string LookupContentName = MetadataController.getContentNameByID(core, field.lookupContentId);
                                if (!string.IsNullOrEmpty(LookupContentName)) {
                                    //
                                    // -- First try Lookup Content
                                    using var cs = new CsModel(core);
                                    if (cs.open(LookupContentName, "ID=" + DbController.encodeSQLNumber(encodeInteger(rawData)), "", true, 0, "name", 1)) {
                                        return cs.getText("name");
                                    }
                                }
                                return string.Empty;
                            }
                            if (!string.IsNullOrEmpty(field.lookupList)) {
                                //
                                // -- lookup list, index is 1-based (to be consistent with Db), but array is 0-based, so adjust
                                int FieldValueInteger = GenericController.encodeInteger(rawData) - 1;
                                if (FieldValueInteger >= 0) {
                                    string[] lookups = field.lookupList.Split(',');
                                    if (lookups.GetUpperBound(0) >= FieldValueInteger) {
                                        return lookups[FieldValueInteger];
                                    }
                                }
                            }
                            return string.Empty;
                        }
                    case CPContentBaseClass.FieldTypeIdEnum.MemberSelect: {
                            //
                            // -- member select
                            if (rawData.isNumeric()) { return MetadataController.getRecordName(core, "people", GenericController.encodeInteger(rawData)); }
                            return string.Empty;
                        }
                    case CPContentBaseClass.FieldTypeIdEnum.Currency: {
                            //
                            // -- currency
                            if (rawData.isNumeric()) { return rawData; }
                            return string.Empty;
                        }
                    case CPContentBaseClass.FieldTypeIdEnum.FileText:
                    case CPContentBaseClass.FieldTypeIdEnum.FileHTML:
                    case CPContentBaseClass.FieldTypeIdEnum.FileHTMLCode:
                    case CPContentBaseClass.FieldTypeIdEnum.FileCSS:
                    case CPContentBaseClass.FieldTypeIdEnum.FileXML:
                    case CPContentBaseClass.FieldTypeIdEnum.FileJavascript: {
                            //
                            // -- cdn file
                            return core.cdnFiles.readFileText(GenericController.encodeText(rawData));
                        }
                    case CPContentBaseClass.FieldTypeIdEnum.Text:
                    case CPContentBaseClass.FieldTypeIdEnum.LongText:
                    case CPContentBaseClass.FieldTypeIdEnum.HTML:
                    case CPContentBaseClass.FieldTypeIdEnum.HTMLCode: {
                            //
                            // -- text saved in database
                            return rawData;
                        }
                    case CPContentBaseClass.FieldTypeIdEnum.File:
                    case CPContentBaseClass.FieldTypeIdEnum.FileImage:
                    case CPContentBaseClass.FieldTypeIdEnum.Link:
                    case CPContentBaseClass.FieldTypeIdEnum.ResourceLink:
                    case CPContentBaseClass.FieldTypeIdEnum.AutoIdIncrement:
                    case CPContentBaseClass.FieldTypeIdEnum.Float:
                    case CPContentBaseClass.FieldTypeIdEnum.Integer: {
                            //
                            // -- other types returned in string format
                            return GenericController.encodeText(rawData);
                        }
                    case CPContentBaseClass.FieldTypeIdEnum.Redirect:
                    case CPContentBaseClass.FieldTypeIdEnum.ManyToMany: {
                            //
                            // This case is covered before the select - but leave this here as safety net
                            return string.Empty;
                        }
                    default: {
                            //
                            // Unknown field type
                            //
                            throw new GenericException("Cannot use field [" + fieldName + "] because the fieldType [" + field.fieldTypeId + "] is not valid.");
                        }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// Saves the value for the field. If the field uses a file, the content is saved to the file using the fields filename. To set a file-based field's filename, use setFieldFilename
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="fieldValue"></param>
        //
        public void set(string fieldName, string fieldValue) {
            try {
                if (!ok()) { throw new ArgumentException("dataset is not valid or End-Of-file."); }
                if (string.IsNullOrWhiteSpace(fieldName)) { throw new ArgumentException("fieldName cannnot be blank"); }
                if (!this.createdWithMetaData) { throw new GenericException("Cannot update a contentset created from a sql query."); }
                if (this.contentMeta == null) { throw new GenericException("Cannot update a contentset created with meta data."); }
                if (string.IsNullOrEmpty(this.contentMeta.name)) { throw new GenericException("Cannot update a contentset created with invalid meta data."); }
                string FieldNameLc = fieldName.Trim(' ').ToLowerInvariant();
                if (!this.contentMeta.fields.ContainsKey(FieldNameLc)) { throw new ArgumentException("The field [" + FieldNameLc + "] could not be found In content [" + this.contentMeta.name + "]"); }
                ContentFieldMetadataModel field = this.contentMeta.fields[FieldNameLc];
                string rawValueForDb = fieldValue ?? string.Empty;
                bool SetNeeded = false;
                switch (field.fieldTypeId) {
                    case CPContentBaseClass.FieldTypeIdEnum.AutoIdIncrement:
                    case CPContentBaseClass.FieldTypeIdEnum.Redirect:
                    case CPContentBaseClass.FieldTypeIdEnum.ManyToMany: {
                            //
                            // Never set
                            //
                            break;
                        }
                    case CPContentBaseClass.FieldTypeIdEnum.File:
                    case CPContentBaseClass.FieldTypeIdEnum.FileImage: {
                            //
                            // Always set
                            // Saved in the field is the filename to the file
                            SetNeeded = true;
                            break;
                        }
                    case CPContentBaseClass.FieldTypeIdEnum.FileText:
                    case CPContentBaseClass.FieldTypeIdEnum.FileHTML:
                    case CPContentBaseClass.FieldTypeIdEnum.FileHTMLCode:
                    case CPContentBaseClass.FieldTypeIdEnum.FileCSS:
                    case CPContentBaseClass.FieldTypeIdEnum.FileXML:
                    case CPContentBaseClass.FieldTypeIdEnum.FileJavascript: {
                            //
                            // public files - save as FieldTypeTextFile except if only white space, consider it blank
                            //
                            string PathFilename = null;
                            string FileExt = null;
                            int FilenameRev = 0;
                            string path = null;
                            int Pos = 0;
                            string pathFilenameOriginal = getRawData(field.nameLc);
                            PathFilename = pathFilenameOriginal;
                            string BlankTest = null;
                            BlankTest = rawValueForDb;
                            BlankTest = GenericController.strReplace(BlankTest, " ", "");
                            BlankTest = GenericController.strReplace(BlankTest, "\r", "");
                            BlankTest = GenericController.strReplace(BlankTest, "\n", "");
                            BlankTest = GenericController.strReplace(BlankTest, "\t", "");
                            if (string.IsNullOrEmpty(BlankTest)) {
                                if (!string.IsNullOrEmpty(PathFilename)) {
                                    core.cdnFiles.deleteFile(PathFilename);
                                    PathFilename = "";
                                }
                            } else {
                                if (string.IsNullOrEmpty(PathFilename)) {
                                    PathFilename = getFilename(field.nameLc, "", this.contentName, field.fieldTypeId);
                                }
                                if (PathFilename.left(1) == "/") {
                                    //
                                    // root file, do not include revision
                                    //
                                } else {
                                    //
                                    // content file, add a revision to the filename
                                    //
                                    Pos = PathFilename.LastIndexOf(".", StringComparison.InvariantCulture) + 1;
                                    if (Pos > 0) {
                                        FileExt = PathFilename.Substring(Pos);
                                        string fileNameNoExt = PathFilename.left(Pos - 1);
                                        Pos = fileNameNoExt.LastIndexOf("/", StringComparison.InvariantCulture) + 1;
                                        if (Pos > 0) {
                                            fileNameNoExt = fileNameNoExt.Substring(Pos);
                                            path = PathFilename.left(Pos);
                                            FilenameRev = 1;
                                            if (!fileNameNoExt.isNumeric()) {
                                                Pos = GenericController.strInstr(1, fileNameNoExt, ".r", 1);
                                                if (Pos > 0) {
                                                    FilenameRev = GenericController.encodeInteger(fileNameNoExt.Substring(Pos + 1));
                                                    FilenameRev += 1;
                                                    fileNameNoExt = fileNameNoExt.left(Pos - 1);
                                                }
                                            }
                                            string fileName = fileNameNoExt + ".r" + FilenameRev + "." + FileExt;
                                            path = GenericController.convertCdnUrlToCdnPathFilename(path);
                                            PathFilename = path + fileName;
                                        }
                                    }
                                }
                                if ((!string.IsNullOrEmpty(pathFilenameOriginal)) && (pathFilenameOriginal != PathFilename)) {
                                    pathFilenameOriginal = GenericController.convertCdnUrlToCdnPathFilename(pathFilenameOriginal);
                                    core.cdnFiles.deleteFile(pathFilenameOriginal);
                                }
                                core.cdnFiles.saveFile(PathFilename, rawValueForDb);
                            }
                            rawValueForDb = PathFilename;
                            SetNeeded = true;
                            break;
                        }
                    case CPContentBaseClass.FieldTypeIdEnum.Boolean: {
                            //
                            // Boolean - sepcial case, block on typed GetAlways set
                            if (GenericController.encodeBoolean(rawValueForDb) != getBoolean(field.nameLc)) {
                                SetNeeded = true;
                            }
                            break;
                        }
                    case CPContentBaseClass.FieldTypeIdEnum.Text: {
                            //
                            // Set if text of value changes
                            //
                            if (GenericController.encodeText(rawValueForDb) != getText(field.nameLc)) {
                                SetNeeded = true;
                            }
                            break;
                        }
                    case CPContentBaseClass.FieldTypeIdEnum.LongText:
                    case CPContentBaseClass.FieldTypeIdEnum.HTML:
                    case CPContentBaseClass.FieldTypeIdEnum.HTMLCode: {
                            //
                            // Set if text of value changes
                            //
                            if (GenericController.encodeText(rawValueForDb) != getText(field.nameLc)) {
                                SetNeeded = true;
                            }
                            break;
                        }
                    case CPContentBaseClass.FieldTypeIdEnum.Lookup: {
                            //
                            // -- Lookup, compare the integer value read with the getInteger from the read cache.
                            if (GenericController.encodeInteger(rawValueForDb) != getInteger(field.nameLc)) {
                                SetNeeded = true;
                            }
                            break;
                        }
                    default: {
                            //
                            // Set if text of value changes
                            //
                            if (GenericController.encodeText(rawValueForDb) != getText(field.nameLc)) {
                                SetNeeded = true;
                            }
                            break;
                        }
                }
                if (SetNeeded) {
                    //
                    // ----- set the new value into the row buffer
                    if (this.writeCache.ContainsKey(field.nameLc)) {
                        this.writeCache[field.nameLc] = rawValueForDb;
                    } else {
                        this.writeCache.Add(field.nameLc, rawValueForDb);
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        /// <summary>
        /// Saves the value for the field. If the field uses a file, the content is saved to the file using the fields filename. To set a file-based field's filename, use setFieldFilename
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="fieldValue"></param>
        public void set(string fieldName, int fieldValue) => set(fieldName, fieldValue.ToString(CultureInfo.InvariantCulture));
        /// <summary>
        /// Saves the value for the field. If the field uses a file, the content is saved to the file using the fields filename. To set a file-based field's filename, use setFieldFilename
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="fieldValue"></param>
        public void set(string fieldName, double fieldValue) => set(fieldName, fieldValue.ToString(CultureInfo.InvariantCulture));
        /// <summary>
        /// Saves the value for the field. If the field uses a file, the content is saved to the file using the fields filename. To set a file-based field's filename, use setFieldFilename
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="fieldValue"></param>
        public void set(string fieldName, DateTime fieldValue) => set(fieldName, fieldValue.ToString(CultureInfo.InvariantCulture));
        /// <summary>
        /// Saves the value for the field. If the field uses a file, the content is saved to the file using the fields filename. To set a file-based field's filename, use setFieldFilename
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="fieldValue"></param>
        public void set(string fieldName, bool fieldValue) => set(fieldName, fieldValue.ToString(CultureInfo.InvariantCulture));
        //
        //========================================================================
        /// <summary>
        /// rollback, or undo the changes to the current row
        /// </summary>
        public void rollBack() {
            try {
                if (!ok()) { throw new ArgumentException("dataset is not valid"); }
                this.writeCache.Clear();
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// Save the current CS Cache back to the database
        /// </summary>
        /// <param name="asyncSave"></param>
        /// <param name="blockClearCache"></param>
        public void save(bool asyncSave, bool blockClearCache) {
            try {
                if (!ok()) { return; }
                if (this.writeCache.Count == 0) { return; }
                if (contentMeta == null) {
                    Controllers.LogController.logTrace(core, "save, contentMeta NULL, sqlSource [" + sqlSource + "]");
                } else {
                    Controllers.LogController.logTrace(core, "save, enter, contentMeta.name [" + contentMeta.name + "], contentMeta.tableName [" + contentMeta.tableName + "], contentMeta.dataSourceName [" + contentMeta.dataSourceName + "], asyncSave [" + asyncSave + "], blockClearCache [" + blockClearCache + "]");
                }
                //
                if (!(this.createdWithMetaData)) { throw new ArgumentException("The dataset cannot be updated because it was created with a query and not a content table."); }
                if (this.contentMeta == null) { throw new ArgumentException("The dataset cannot be updated because it was not created from a valid content table."); }
                //
                // -- create the Db controller instance
                using (var db = new DbController(core, this.contentMeta.dataSourceName)) {
                    string sqlUpdate = "";
                    DateTime sqlModifiedDate = core.dateTimeNowMockable;
                    int sqlModifiedBy = this.userId;
                    bool AuthorableFieldUpdate = false;
                    int FieldFoundCount = 0;
                    var SQLCriteriaUnique = new StringBuilder();
                    var UniqueViolationFieldList = new StringBuilder();
                    string sqlDelimiter = "";
                    foreach (var keyValuePair in this.writeCache) {
                        string fieldName = keyValuePair.Key;
                        string ucaseFieldName = GenericController.toUCase(fieldName);
                        object writeCacheValue = keyValuePair.Value;
                        if (ucaseFieldName == "ID") {
                            //
                            // do not add to update, it is hardcoded to update where clause
                        } else if (ucaseFieldName == "MODIFIEDBY") {
                            //
                            // capture and block it - it is hardcoded in sql
                            //
                            AuthorableFieldUpdate = true;
                            sqlModifiedBy = GenericController.encodeInteger(writeCacheValue);
                        } else if (ucaseFieldName == "MODIFIEDDATE") {
                            //
                            // capture and block it - it is hardcoded in sql
                            //
                            AuthorableFieldUpdate = true;
                            sqlModifiedDate = GenericController.encodeDate(writeCacheValue);
                        } else {
                            //
                            // let these field be added to the sql
                            //
                            FieldFoundCount += 1;
                            Models.Domain.ContentFieldMetadataModel field = this.contentMeta.fields[fieldName.ToLowerInvariant()];
                            string SQLSetPair = "";
                            bool FieldAdminAuthorable = ((!field.readOnly) && (!field.notEditable) && (field.authorable));
                            //
                            // ----- Set SQLSetPair to the name=value pair for the SQL statement
                            //
                            switch (field.fieldTypeId) {
                                case CPContentBaseClass.FieldTypeIdEnum.Redirect:
                                case CPContentBaseClass.FieldTypeIdEnum.ManyToMany: {
                                        // do nothing
                                        break;
                                    }
                                case CPContentBaseClass.FieldTypeIdEnum.Integer:
                                case CPContentBaseClass.FieldTypeIdEnum.Lookup:
                                case CPContentBaseClass.FieldTypeIdEnum.AutoIdIncrement:
                                case CPContentBaseClass.FieldTypeIdEnum.MemberSelect: {
                                        //
                                        // -- allow nullable (if null, set db to null). for example, a field 'chargesremaining' could be blank or numeric
                                        if (string.IsNullOrWhiteSpace(encodeText(writeCacheValue))) {
                                            SQLSetPair = fieldName + "=null";
                                        } else {
                                            SQLSetPair = fieldName + "=" + DbController.encodeSQLNumber(encodeInteger(writeCacheValue));
                                        }
                                        break;
                                    }
                                case CPContentBaseClass.FieldTypeIdEnum.Currency:
                                case CPContentBaseClass.FieldTypeIdEnum.Float: {
                                        //
                                        // -- allow nullable (if null, set db to null). for example, a field 'chargesremaining' could be blank or numeric
                                        if (string.IsNullOrWhiteSpace(encodeText(writeCacheValue))) {
                                            SQLSetPair = fieldName + "=null";
                                        } else {
                                            SQLSetPair = fieldName + "=" + DbController.encodeSQLNumber(encodeNumber(writeCacheValue));
                                        }
                                        break;
                                    }
                                case CPContentBaseClass.FieldTypeIdEnum.Boolean: {
                                        SQLSetPair = fieldName + "=" + DbController.encodeSQLBoolean(encodeBoolean(writeCacheValue));
                                        break;
                                    }
                                case CPContentBaseClass.FieldTypeIdEnum.Date: {
                                        SQLSetPair = fieldName + "=" + DbController.encodeSQLDate(encodeDate(writeCacheValue));
                                        break;
                                    }
                                case CPContentBaseClass.FieldTypeIdEnum.Text: {
                                        string Copy = encodeText(writeCacheValue);
                                        if (Copy.Length > 255) {
                                            Copy = Copy.left(255);
                                        }
                                        if (field.scramble) {
                                            Copy = textScramble(core, Copy);
                                        }
                                        SQLSetPair = fieldName + "=" + DbController.encodeSQLText(Copy);
                                        break;
                                    }
                                case CPContentBaseClass.FieldTypeIdEnum.Link:
                                case CPContentBaseClass.FieldTypeIdEnum.ResourceLink:
                                case CPContentBaseClass.FieldTypeIdEnum.File:
                                case CPContentBaseClass.FieldTypeIdEnum.FileImage:
                                case CPContentBaseClass.FieldTypeIdEnum.FileText:
                                case CPContentBaseClass.FieldTypeIdEnum.FileCSS:
                                case CPContentBaseClass.FieldTypeIdEnum.FileXML:
                                case CPContentBaseClass.FieldTypeIdEnum.FileJavascript:
                                case CPContentBaseClass.FieldTypeIdEnum.FileHTML:
                                case CPContentBaseClass.FieldTypeIdEnum.FileHTMLCode: {
                                        string filename = encodeText(writeCacheValue);
                                        if (filename.Length > 255) {
                                            filename = filename.left(255);
                                        }
                                        SQLSetPair = fieldName + "=" + DbController.encodeSQLText(filename);
                                        break;
                                    }
                                case CPContentBaseClass.FieldTypeIdEnum.LongText:
                                case CPContentBaseClass.FieldTypeIdEnum.HTML:
                                case CPContentBaseClass.FieldTypeIdEnum.HTMLCode: {
                                        SQLSetPair = fieldName + "=" + DbController.encodeSQLText(GenericController.encodeText(writeCacheValue));
                                        break;
                                    }
                                default: {
                                        //
                                        // Invalid fieldtype
                                        //
                                        throw new GenericException("Can Not save this record because the field [" + field.nameLc + "] has an invalid field type Id [" + field.fieldTypeId + "]");
                                    }
                            }
                            if (!string.IsNullOrEmpty(SQLSetPair)) {
                                //
                                // ----- Set the new value in the 
                                //
                                if (this.resultColumnCount > 0) {
                                    for (int ColumnPtr = 0; ColumnPtr < this.resultColumnCount; ColumnPtr++) {
                                        if (this.fieldNames[ColumnPtr] == ucaseFieldName) {
                                            this.readCache[ColumnPtr, this.readCacheRowPtr] = writeCacheValue.ToString();
                                            break;
                                        }
                                    }
                                }
                                if (field.uniqueName && (GenericController.encodeText(writeCacheValue) != "")) {
                                    //
                                    // ----- set up for unique name check
                                    //

                                    if (SQLCriteriaUnique.Length > 0) {
                                        SQLCriteriaUnique.Append("Or");
                                        UniqueViolationFieldList.Append(",");
                                    }
                                    string writeCacheValueText = GenericController.encodeText(writeCacheValue);
                                    if (writeCacheValueText.Length < 255) {
                                        UniqueViolationFieldList.Append(field.nameLc + "=\"" + writeCacheValueText + "\"");
                                    } else {
                                        UniqueViolationFieldList.Append(field.nameLc + "=\"" + writeCacheValueText.left(255) + "...\"");
                                    }
                                    switch (field.fieldTypeId) {
                                        case CPContentBaseClass.FieldTypeIdEnum.Redirect:
                                        case CPContentBaseClass.FieldTypeIdEnum.ManyToMany: {
                                                break;
                                            }
                                        default: {
                                                SQLCriteriaUnique.Append("(" + field.nameLc + "=" + MetadataController.encodeSQL(writeCacheValue, field.fieldTypeId) + ")");
                                                break;
                                            }
                                    }
                                }
                                //
                                // ----- update live record
                                //
                                sqlUpdate = sqlUpdate + sqlDelimiter + SQLSetPair;
                                sqlDelimiter = ",";
                                if (FieldAdminAuthorable) {
                                    AuthorableFieldUpdate = true;
                                }
                            }
                        }
                    }
                    //
                    // ----- Set ModifiedBy,ModifiedDate Fields if an admin visible field has changed
                    if (AuthorableFieldUpdate) {
                        if (!string.IsNullOrEmpty(sqlUpdate)) {
                            //
                            // ----- Authorable Fields Updated in non-Authoring Mode, set Live Record Modified
                            //
                            sqlUpdate = sqlUpdate
                                + ",modifiedDate=" + DbController.encodeSQLDate(sqlModifiedDate)
                                + ",modifiedBy=" + DbController.encodeSQLNumber(sqlModifiedBy);
                        }
                    }
                    //
                    // -- get id from read cache or write cache. if id=0 save is insert, else save is update
                    int id = getInteger("ID");
                    //
                    // ----- Do the unique check on the content table, if necessary
                    //
                    if ((SQLCriteriaUnique.Length > 0)) {
                        string sqlUnique = "select id from " + this.contentMeta.tableName + " where (id<>" + id + ")and(" + SQLCriteriaUnique + ")and(" + this.contentMeta.legacyContentControlCriteria + ");";
                        using DataTable dtRecords = db.executeQuery(sqlUnique);
                        //
                        // -- unique violation
                        if (dtRecords.Rows.Count > 0) {
                            LogController.logWarn(core, "Can not save record to content [" + this.contentMeta.name + "] because it would create a non-unique record for one or more of the following field(s) [" + UniqueViolationFieldList + "]");
                            return;
                        }
                    }
                    //
                    Controllers.LogController.logTrace(core, "save, FieldFoundCount [" + FieldFoundCount + "], sqlUpdate [" + sqlUpdate + "]");
                    //
                    if (FieldFoundCount > 0) {
                        //
                        // ----- update live table (non-workflowauthoring and non-authorable fields)
                        //
                        if (!string.IsNullOrEmpty(sqlUpdate)) {
                            string sql = "update " + this.contentMeta.tableName + " set " + sqlUpdate + " where id=" + id + ";";
                            if (asyncSave) {
                                //
                                // -- send to queue or cache for execution later
                                db.executeNonQuery(sql);
                            } else {
                                db.executeNonQuery(sql);
                            }
                            //
                            // -- invalidate the special cache name used to detect a change in any record
                            core.cache.invalidateRecordKey(id, this.contentMeta.tableName, this.contentMeta.dataSourceName);
                            //
                            // -- save the table_LastModified key so objects like admin-nav can depend on this key and invalidate
                            core.cache.invalidateTableDependencyKey(this.contentMeta.tableName);
                        }
                    }
                }
                //
                logger.Trace(LogController.processLogMessage(core, "save, exit", false));
                //
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        public void save(bool asyncSave) => save(asyncSave, false);
        //
        public void save() => save(false, false);
        //
        //=====================================================================================================
        /// <summary>
        /// Initialize the csv_ContentSet Result Cache when it is first opened
        /// </summary>
        private void initAfterOpen() {
            try {
                this.resultColumnCount = 0;
                this.readCacheRowCnt = 0;
                this.readCacheRowPtr = -1;
                this.writeCache = new Dictionary<string, string>();
                this.fieldNames = Array.Empty<string>();
                if (this.dt != null) {
                    if (this.dt.Rows.Count > 0) {
                        this.resultColumnCount = this.dt.Columns.Count;
                        this.fieldNames = new String[this.resultColumnCount];
                        int ColumnPtr = 0;
                        foreach (DataColumn dc in this.dt.Columns) {
                            this.fieldNames[ColumnPtr] = GenericController.toUCase(dc.ColumnName);
                            ColumnPtr += 1;
                        }
                        // refactor -- convert interal storage to dt and assign -- will speedup open
                        this.readCache = core.db.convertDataTabletoArray(this.dt);
                        this.readCacheRowCnt = this.readCache.GetUpperBound(1) + 1;
                        this.readCacheRowPtr = 0;
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// Opens a csv_ContentSet with the Members of a group
        /// </summary>
        /// <param name="groupList"></param>
        /// <param name="sqlCriteria"></param>
        /// <param name="sortFieldList"></param>
        /// <param name="activeOnly"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageNumber">1 based page number</param>
        /// <returns></returns>
        public bool openGroupUsers(List<string> groupList, string sqlCriteria, string sortFieldList, bool activeOnly, int pageSize, int pageNumber) {
            try {
                if (groupList.Count > 0) {
                    pageSize = (pageSize > 0) ? pageSize : DbController.sqlPageSizeDefault;
                    pageNumber = (pageNumber > 0) ? pageNumber : 1;
                    //
                    // Build Inner Query to select distinct id needed
                    //
                    string SQL = "SELECT DISTINCT ccMembers.id"
                        + " FROM (ccMembers"
                        + " LEFT JOIN ccMemberRules ON ccMembers.Id = ccMemberRules.memberId)"
                        + " LEFT JOIN ccGroups ON ccMemberRules.GroupId = ccGroups.ID"
                        + " WHERE (ccMemberRules.Active<>0)AND(ccGroups.Active<>0)";
                    //
                    if (activeOnly) { SQL += "AND(ccMembers.Active<>0)"; }
                    //
                    string subQuery = "";
                    foreach (string groupName in groupList) {
                        if (!string.IsNullOrEmpty(groupName.Trim())) {
                            subQuery += "or(ccGroups.Name=" + DbController.encodeSQLText(groupName.Trim()) + ")";
                        }
                    }
                    if (!string.IsNullOrEmpty(subQuery)) {
                        SQL += "and(" + subQuery.Substring(2) + ")";
                    }
                    //
                    // -- group expiration
                    SQL += "and((ccMemberRules.DateExpires Is Null)or(ccMemberRules.DateExpires>" + DbController.encodeSQLDate(core.dateTimeNowMockable) + "))";
                    //
                    // Build outer query to get all ccmember fields
                    // Must do this inner/outer because if the table has a text field, it can not be in the distinct
                    //
                    SQL = "SELECT * from ccMembers where id in (" + SQL + ")";
                    if (!string.IsNullOrEmpty(sqlCriteria)) {
                        SQL += "and(" + sqlCriteria + ")";
                    }
                    if (!string.IsNullOrEmpty(sortFieldList)) {
                        SQL += " order by " + sortFieldList;
                    }
                    return openSql(SQL, "Default", pageSize, pageNumber);
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
            return false;
        }
        /// <summary>
        /// Opens a csv_ContentSet with the Members of a group
        /// </summary>
        /// <param name="groupList"></param>
        /// <param name="sqlCriteria"></param>
        /// <param name="sortFieldList"></param>
        /// <param name="activeOnly"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public bool openGroupUsers(List<string> groupList, string sqlCriteria, string sortFieldList, bool activeOnly, int pageSize)
            => openGroupUsers(groupList, sqlCriteria, sortFieldList, activeOnly, pageSize, 1);
        /// <summary>
        /// Opens a csv_ContentSet with the Members of a group
        /// </summary>
        /// <param name="groupList"></param>
        /// <param name="sqlCriteria"></param>
        /// <param name="sortFieldList"></param>
        /// <param name="activeOnly"></param>
        /// <returns></returns>
        public bool openGroupUsers(List<string> groupList, string sqlCriteria, string sortFieldList, bool activeOnly)
            => openGroupUsers(groupList, sqlCriteria, sortFieldList, activeOnly, DbController.sqlPageSizeDefault, 1);
        /// <summary>
        /// Opens a csv_ContentSet with the Members of a group
        /// </summary>
        /// <param name="groupList"></param>
        /// <param name="sqlCriteria"></param>
        /// <param name="sortFieldList"></param>
        /// <returns></returns>
        public bool openGroupUsers(List<string> groupList, string sqlCriteria, string sortFieldList)
            => openGroupUsers(groupList, sqlCriteria, sortFieldList, true, DbController.sqlPageSizeDefault, 1);
        /// <summary>
        /// Opens a csv_ContentSet with the Members of a group
        /// </summary>
        /// <param name="groupList"></param>
        /// <param name="sqlCriteria"></param>
        /// <returns></returns>
        public bool openGroupUsers(List<string> groupList, string sqlCriteria)
            => openGroupUsers(groupList, sqlCriteria, "", true, DbController.sqlPageSizeDefault, 1);
        /// <summary>
        /// Opens a csv_ContentSet with the Members of a group
        /// </summary>
        /// <param name="groupList"></param>
        /// <returns></returns>
        public bool openGroupUsers(List<string> groupList)
            => openGroupUsers(groupList, "", "", true, DbController.sqlPageSizeDefault, 1);
        //
        //========================================================================
        /// <summary>
        /// Return an array with all the data
        /// </summary>
        /// <returns></returns>
        public string[,] getRows() {
            return this.readCache;
        }
        //
        //========================================================================
        /// <summary>
        /// get the row count of the dataset
        /// </summary>
        /// <returns></returns>
        public int getRowCount() {
            if (ok()) { return this.readCacheRowCnt; }
            return 0;
        }
        //
        //========================================================================
        /// <summary>
        /// return the content name of a csv_ContentSet
        /// </summary>
        /// <returns></returns>
        //
        public string getContentName() {
            try {
                return this.contentName;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Return an edit link for the current record
        /// </summary>
        /// <param name="allowCut"></param>
        /// <returns></returns>
        public string getRecordEditLink(bool allowCut) {
            try {
                if (!ok()) { throw (new GenericException("Cannot create edit link because data set is not valid.")); }
                string ContentName = MetadataController.getContentNameByID(core, getInteger("contentcontrolid"));
                if (!string.IsNullOrEmpty(ContentName)) { return AdminUIController.getRecordEditAndCutAnchorTag(core, ContentName, getInteger("ID"), allowCut, getText("Name")); }
                return string.Empty;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        /// <summary>
        /// Return an edit link for the current record
        /// </summary>
        /// <returns></returns>
        public string getRecordEditLink() => getRecordEditLink(false);
        //
        // ====================================================================================================
        /// <summary>
        /// Save a field value from a form request. The field is processed as the type from the cs content definition opened.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="fieldName"></param>
        /// <param name="requestName"></param>
        public void setFormInput(CoreController core, string fieldName, string requestName) {
            if (!ok()) {
                throw new GenericException("Data is invalid, empty, or end-of-file");
            } else if (string.IsNullOrEmpty(fieldName.Trim(' '))) {
                throw new GenericException("FieldName is invalid or blank");
            } else {
                string LocalRequestName = requestName;
                if (string.IsNullOrEmpty(LocalRequestName)) {
                    LocalRequestName = fieldName;
                }
                switch (getFieldTypeId(fieldName)) {
                    case CPContentBaseClass.FieldTypeIdEnum.Boolean: {
                            //
                            // -- Boolean
                            set(fieldName, core.docProperties.getBoolean(LocalRequestName));
                            break;
                        }
                    case CPContentBaseClass.FieldTypeIdEnum.Currency:
                    case CPContentBaseClass.FieldTypeIdEnum.Float:
                    case CPContentBaseClass.FieldTypeIdEnum.Integer:
                    case CPContentBaseClass.FieldTypeIdEnum.Lookup:
                    case CPContentBaseClass.FieldTypeIdEnum.ManyToMany: {
                            //
                            // -- Numbers
                            set(fieldName, core.docProperties.getNumber(LocalRequestName));
                            break;
                        }
                    case CPContentBaseClass.FieldTypeIdEnum.Date: {
                            //
                            // -- Date
                            set(fieldName, core.docProperties.getDate(LocalRequestName));
                            break;
                        }
                    case CPContentBaseClass.FieldTypeIdEnum.File:
                    case CPContentBaseClass.FieldTypeIdEnum.FileImage: {
                            //
                            // -- upload file
                            if (core.docProperties.containsKey(LocalRequestName)) {
                                var docProperty = core.docProperties.getProperty(LocalRequestName);
                                string filename = docProperty.value;
                                //
                                // -- make filename compatible with dos and unix
                                filename = FileController.encodeDosFilename(filename);
                                filename = FileController.encodeUnixFilename(filename);
                                //
                                string unixPathFilename = getFilename(fieldName, filename);
                                string dosPathFilename = FileController.convertToDosSlash(unixPathFilename);
                                string dosPath = FileController.getPath(dosPathFilename);
                                //
                                // -- copy windowsTemp file to cdnFiles and force upload
                                var WindowsTempFiles = new FileController(core, System.IO.Path.GetTempPath());
                                WindowsTempFiles.copyFile(docProperty.windowsTempfilename, dosPathFilename, core.cdnFiles);
                                core.cdnFiles.upload(fieldName, dosPath, ref filename);
                                //
                                // -- update record with unix pathFilename to saved uploaded file
                                set(fieldName, unixPathFilename);
                            }
                            break;
                        }
                    default: {
                            //
                            // -- text files
                            set(fieldName, core.docProperties.getText(LocalRequestName));
                            break;
                        }
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// return a list of add links
        /// </summary>
        /// <param name="core"></param>
        /// <param name="ignore"></param>
        /// <param name="PresetNameValueList"></param>
        /// <param name="AllowPaste"></param>
        /// <returns></returns>
        public string getRecordAddLink(string PresetNameValueList, bool AllowPaste) {
            var result = new StringBuilder();
            try {
                if (string.IsNullOrEmpty(this.contentName)) { throw new GenericException("getRecordAddLink was called with a ContentSet that was created with an SQL statement. The function requires a ContentSet opened with an OpenCSContent."); }
                foreach (var AddLink in AdminUIController.getRecordAddAnchorTag(core, this.contentName, PresetNameValueList, AllowPaste)) {
                    result.Append(AddLink);
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
            return result.ToString();
        }
        //
        public string getRecordAddLink(string PresetNameValueList) => getRecordAddLink(PresetNameValueList, false);
        //
        public string getRecordAddLink() => getRecordAddLink("", false);
        //
        //====================================================================================================
        /// <summary>
        /// Open a contentwatch data set for list 'whats new'
        /// </summary>
        /// <param name="core"></param>
        /// <param name="SortFieldList"></param>
        /// <returns></returns>
        public bool openWhatsNew(string SortFieldList) {
            try {
                return openContentWatchList("What's New", SortFieldList);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Open a contentwatch data set
        /// </summary>
        /// <param name="core"></param>
        /// <param name="listName"></param>
        /// <param name="sortFieldList"></param>
        /// <param name="activeOnly"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageNumber">1 based page number</param>
        /// <returns></returns>
        public bool openContentWatchList(string listName, string sortFieldList, bool activeOnly, int pageSize, int pageNumber) {
            try {
                sortFieldList = encodeEmpty(sortFieldList, "dateadded").Trim(' ');
                pageSize = (pageSize > 0) ? pageSize : DbController.sqlPageSizeDefault;
                pageNumber = (pageNumber > 0) ? pageNumber : 1;
                //
                // ----- Add tablename to the front of SortFieldList fieldnames
                sortFieldList = " " + GenericController.strReplace(sortFieldList, ",", " , ") + " ";
                sortFieldList = GenericController.strReplace(sortFieldList, " ID ", " ccContentWatch.ID ", 1, 99, 1);
                sortFieldList = GenericController.strReplace(sortFieldList, " Link ", " ccContentWatch.Link ", 1, 99, 1);
                sortFieldList = GenericController.strReplace(sortFieldList, " LinkLabel ", " ccContentWatch.LinkLabel ", 1, 99, 1);
                sortFieldList = GenericController.strReplace(sortFieldList, " SortOrder ", " ccContentWatch.SortOrder ", 1, 99, 1);
                sortFieldList = GenericController.strReplace(sortFieldList, " DateAdded ", " ccContentWatch.DateAdded ", 1, 99, 1);
                sortFieldList = GenericController.strReplace(sortFieldList, " ContentID ", " ccContentWatch.ContentID ", 1, 99, 1);
                sortFieldList = GenericController.strReplace(sortFieldList, " RecordID ", " ccContentWatch.RecordID ", 1, 99, 1);
                sortFieldList = GenericController.strReplace(sortFieldList, " ModifiedDate ", " ccContentWatch.ModifiedDate ", 1, 99, 1);
                //
                // ----- Special case
                sortFieldList = GenericController.strReplace(sortFieldList, " name ", " ccContentWatch.LinkLabel ", 1, 99, 1);
                //
                string SQL = "SELECT"
                    + " ccContentWatch.ID AS ID"
                    + ",ccContentWatch.Link as Link"
                    + ",ccContentWatch.LinkLabel as LinkLabel"
                    + ",ccContentWatch.SortOrder as SortOrder"
                    + ",ccContentWatch.DateAdded as DateAdded"
                    + ",ccContentWatch.ContentID as ContentID"
                    + ",ccContentWatch.RecordID as RecordID"
                    + ",ccContentWatch.ModifiedDate as ModifiedDate"
                    + " FROM (ccContentWatchLists"
                    + " LEFT JOIN ccContentWatchListRules ON ccContentWatchLists.Id = ccContentWatchListRules.ContentWatchListID)"
                    + " LEFT JOIN ccContentWatch ON ccContentWatchListRules.ContentWatchId = ccContentWatch.ID"
                    + " WHERE (((ccContentWatchLists.Name)=" + DbController.encodeSQLText(listName) + ")"
                    + "AND ((ccContentWatchLists.Active)<>0)"
                    + "AND ((ccContentWatchListRules.Active)<>0)"
                    + "AND ((ccContentWatch.Active)<>0)"
                    + "AND (ccContentWatch.Link is not null)"
                    + "AND (ccContentWatch.LinkLabel is not null)"
                    + "AND ((ccContentWatch.WhatsNewDateExpires is null)or(ccContentWatch.WhatsNewDateExpires>" + DbController.encodeSQLDate(core.doc.profileStartTime) + "))"
                    + ")"
                    + " ORDER BY " + sortFieldList + ";";
                if (!openSql(SQL, "", pageSize, pageNumber)) {
                    //
                    // Check if listname exists
                    //
                    if (!this.open("Content Watch Lists", "name=" + DbController.encodeSQLText(listName), "ID", false, 0, "ID")) {
                        this.close();
                        if (this.insert("Content Watch Lists")) {
                            this.set("name", listName);
                        }
                    }
                    this.close();
                    return false;
                }
                return true;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        public bool openContentWatchList(string listName, string sortFieldList, bool activeOnly, int pageSize)
            => openContentWatchList(listName, sortFieldList, activeOnly, pageSize, 1);
        //
        public bool openContentWatchList(string listName, string sortFieldList, bool activeOnly)
            => openContentWatchList(listName, sortFieldList, activeOnly, DbController.sqlPageSizeDefault, 1);
        //
        public bool openContentWatchList(string listName, string sortFieldList)
            => openContentWatchList(listName, sortFieldList, true, DbController.sqlPageSizeDefault, 1);
        //
        public bool openContentWatchList(string listName)
            => openContentWatchList(listName, "", true, DbController.sqlPageSizeDefault, 1);
        //
        //========================================================================
        /// <summary>
        /// Open dataset for the record specified
        /// </summary>
        /// <param name="contentName"></param>
        /// <param name="recordId"></param>
        /// <param name="selectFieldList"></param>
        /// <returns></returns>
        public bool openRecord(string contentName, int recordId, string selectFieldList) {
            return open(contentName, "(ID=" + DbController.encodeSQLNumber(recordId) + ")", "", false, core.session.user.id, selectFieldList);
        }
        /// <summary>
        /// Open dataset for the record specified
        /// </summary>
        /// <param name="contentName"></param>
        /// <param name="recordId"></param>
        /// <returns></returns>
        public bool openRecord(string contentName, int recordId) {
            return open(contentName, "(ID=" + DbController.encodeSQLNumber(recordId) + ")", "", false, core.session.user.id);
        }
        //
        //========================================================================
        /// <summary>
        /// Open dataset for the record specified
        /// </summary>
        /// <param name="contentName"></param>
        /// <param name="recordGuid"></param>
        /// <param name="selectFieldList"></param>
        /// <returns></returns>
        public bool openRecord(string contentName, string recordGuid, string selectFieldList) {
            return open(contentName, "(ccguid=" + DbController.encodeSQLText(recordGuid) + ")", "", false, core.session.user.id, selectFieldList);
        }
        /// <summary>
        /// Open dataset for the record specified
        /// </summary>
        /// <param name="contentName"></param>
        /// <param name="recordGuid"></param>
        /// <returns></returns>
        public bool openRecord(string contentName, string recordGuid) {
            return open(contentName, "(ID=" + DbController.encodeSQLText(recordGuid) + ")", "", false, core.session.user.id);
        }
        //
        //========================================================================
        /// <summary>
        /// Opens a dataTable for the table/row definied by the contentname and criteria
        /// </summary>
        /// <param name="contentName"></param>
        /// <param name="sqlCriteria"></param>
        /// <param name="sqlOrderBy"></param>
        /// <param name="activeOnly"></param>
        /// <param name="memberId"></param>
        /// <param name="sqlSelectFieldList"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageNumber">1 based page number</param>
        /// <returns></returns>
        public bool open(string contentName, string sqlCriteria, string sqlOrderBy, bool activeOnly, int memberId, string sqlSelectFieldList, int pageSize, int pageNumber) {
            bool result = false;
            try {
                //
                // -- if a previous dataset is open, close it now
                if (isOpen) { close(); }
                //
                // -- verify arguments
                if (string.IsNullOrEmpty(contentName)) { throw new ArgumentException("ContentName cannot be blank"); }
                var contentMetaData = ContentMetadataModel.createByUniqueName(core, contentName);
                if (contentMetaData == null) { throw (new GenericException("No content found For [" + contentName + "]")); }
                if (contentMetaData.id <= 0) { throw (new GenericException("No content found For [" + contentName + "]")); }
                if (string.IsNullOrWhiteSpace(contentMetaData.tableName)) { throw (new GenericException("Content metadata [" + contentName + "] does not reference a valid table")); }
                sqlOrderBy = GenericController.encodeEmpty(sqlOrderBy, contentMetaData.defaultSortMethod);
                sqlSelectFieldList = GenericController.encodeEmpty(sqlSelectFieldList, contentMetaData.selectCommaList);
                pageSize = (pageSize > 0) ? pageSize : DbController.sqlPageSizeDefault;
                pageNumber = (pageNumber > 0) ? pageNumber : 1;
                //
                // -- verify the sortfields are in this table
                if (!string.IsNullOrEmpty(sqlOrderBy)) {
                    string[] SortFields = sqlOrderBy.Split(',');
                    for (int ptr = 0; ptr < SortFields.GetUpperBound(0) + 1; ptr++) {
                        string SortField = SortFields[ptr].ToLowerInvariant();
                        SortField = GenericController.strReplace(SortField, "asc", "", 1, 99, 1);
                        SortField = GenericController.strReplace(SortField, "desc", "", 1, 99, 1);
                        SortField = SortField.Trim(' ');
                        if (!contentMetaData.selectList.Contains(SortField)) { throw (new GenericException("The field [" + SortField + "] was used in sqlOrderBy for content [" + contentName + "], but the content does not include this field.")); }
                    }
                }
                //
                // -- fixup the criteria to include the ContentControlID(s) / EditSourceID
                string sqlContentCriteria = contentMetaData.legacyContentControlCriteria;
                if (string.IsNullOrEmpty(sqlContentCriteria)) {
                    sqlContentCriteria = "(1=1)";
                } else {
                    //
                    // -- remove tablename from contentcontrolcriteria - if in workflow mode, and authoringtable is different, this would be wrong, also makes sql smaller, and is not necessary
                    sqlContentCriteria = GenericController.strReplace(sqlContentCriteria, contentMetaData.tableName + ".", "");
                }
                if (!string.IsNullOrEmpty(sqlCriteria)) { sqlContentCriteria += "and(" + sqlCriteria + ")"; }
                if (activeOnly) { sqlContentCriteria += "and(active<>0)"; }
                //
                // -- Process Select Fields, make sure ContentControlID,ID,Name,Active are included
                sqlSelectFieldList = GenericController.strReplace(sqlSelectFieldList, "\t", " ");
                while (GenericController.strInstr(1, sqlSelectFieldList, " ,") != 0) { sqlSelectFieldList = GenericController.strReplace(sqlSelectFieldList, " ,", ","); }
                while (GenericController.strInstr(1, sqlSelectFieldList, ", ") != 0) { sqlSelectFieldList = GenericController.strReplace(sqlSelectFieldList, ", ", ","); }
                //
                // -- add required fields into select list
                if ((!string.IsNullOrEmpty(sqlSelectFieldList)) && (sqlSelectFieldList.IndexOf("*", System.StringComparison.OrdinalIgnoreCase) == -1)) {
                    string testList = ("," + sqlSelectFieldList + ",").ToLower(CultureInfo.InvariantCulture);
                    if (!testList.Contains(",contentcontrolid,")) { sqlSelectFieldList += ",contentcontrolid"; }
                    if (!testList.Contains(",name,")) { sqlSelectFieldList += ",name"; }
                    if (!testList.Contains(",id,")) { sqlSelectFieldList += ",id"; }
                    if (!testList.Contains(",active,")) { sqlSelectFieldList += ",active"; }
                }
                //
                // ----- If no select list, use *
                if (string.IsNullOrEmpty(sqlSelectFieldList)) { sqlSelectFieldList = "*"; }
                string sql = "select " + sqlSelectFieldList + " from " + contentMetaData.tableName + " where (" + sqlContentCriteria + ")" + (string.IsNullOrWhiteSpace(sqlOrderBy) ? "" : " order by " + sqlOrderBy);
                //
                // -- now open the sql
                if (openSql(sql, contentMetaData.dataSourceName, pageSize, pageNumber)) {
                    //
                    // -- correct the status
                    this.readable = true;
                    this.createdWithMetaData = true;
                    this.contentName = contentName;
                    this.contentMeta = contentMetaData;
                    this.sqlSource = sql;
                    return true;
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
            return result;
        }
        //
        public bool open(string contentName, string sqlCriteria, string sqlOrderBy, bool activeOnly, int memberId)
            => open(contentName, sqlCriteria, sqlOrderBy, activeOnly, memberId, "", DbController.sqlPageSizeDefault, 1);
        //
        public bool open(string contentName, string sqlCriteria, string sqlOrderBy, bool activeOnly, int memberId, string sqlSelectFieldList, int PageSize)
            => open(contentName, sqlCriteria, sqlOrderBy, activeOnly, memberId, sqlSelectFieldList, PageSize, 1);
        //
        public bool open(string contentName, string sqlCriteria, string sqlOrderBy, bool activeOnly, int memberId, string sqlSelectFieldList)
            => open(contentName, sqlCriteria, sqlOrderBy, activeOnly, memberId, sqlSelectFieldList, DbController.sqlPageSizeDefault, 1);
        //
        public bool open(string contentName, string sqlCriteria, string sqlOrderBy, bool activeOnly)
            => open(contentName, sqlCriteria, sqlOrderBy, activeOnly, 0, "", DbController.sqlPageSizeDefault, 1);
        //
        public bool open(string contentName, string sqlCriteria, string sqlOrderBy)
            => open(contentName, sqlCriteria, sqlOrderBy, true, 0, "", DbController.sqlPageSizeDefault, 1);
        //
        public bool open(string contentName, string sqlCriteria)
            => open(contentName, sqlCriteria, "", true, 0, "", DbController.sqlPageSizeDefault, 1);
        //
        public bool open(string contentName)
            => open(contentName, "", "", true, 0, "", DbController.sqlPageSizeDefault, 1);
        //
        //========================================================================
        /// <summary>
        /// openSql
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="dataSourceName"></param>
        /// <param name="pageSize">records per page</param>
        /// <param name="pageNumber">1 based page number</param>
        /// <returns></returns>
        public bool openSql(string sql, string dataSourceName, int pageSize, int pageNumber) {
            try {
                init();
                this.readable = true;
                this.createdWithMetaData = false;
                this.contentName = "";
                this.sqlSource = sql;
                pageSize = (pageSize > 0) ? pageSize : DbController.sqlPageSizeDefault;
                pageNumber = (pageNumber > 0) ? pageNumber : 1;
                //
                using (var db = new DbController(core, dataSourceName)) {
                    this.dt = core.db.executeQuery(sql, DbController.getStartRecord(pageSize, pageNumber), pageSize);
                }
                initAfterOpen();
                return ok();
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        public bool openSql(string sql, string dataSourceName, int pageSize) => openSql(sql, dataSourceName, pageSize, 1);
        //
        public bool openSql(string sql, string dataSourceName) => openSql(sql, dataSourceName, DbController.sqlPageSizeDefault, 1);
        //
        public bool openSql(string sql) => openSql(sql, "default", DbController.sqlPageSizeDefault, 1);
        //
        //========================================================================
        // Dispose
        //
        #region  IDisposable Support 
        //
        //====================================================================================================
        /// <summary>
        /// dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing) {
            if (!this.disposed) {
                if (disposing) {
                    //
                    // -- call .dispose for managed objects
                    close();
                    if (dt != null) {
                        dt.Dispose();
                        dt = null;
                    }
                }
                //
                // -- Add code here to release the unmanaged resource.
            }
            this.disposed = true;
        }
        protected bool disposed;
        // Do not change or add Overridable to these methods.
        // Put cleanup code in Dispose(ByVal disposing As Boolean).
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~CsModel() {
            Dispose(false);
        }
        #endregion
    }
}

