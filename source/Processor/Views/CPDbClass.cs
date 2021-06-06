
using System;
using System.Data;
using Contensive.Processor.Controllers;
using Contensive.BaseClasses;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace Contensive.Processor {
    //
    //====================================================================================================
    /// <summary>
    /// Implements CPDbBaseClass. The default datasource is automatically setup as CP.Db and is disposed on cp dispose.
    /// Other datasources are implemented with CP.DbNew(datasourceName)
    /// </summary>
    public class CPDbClass : CPDbBaseClass, IDisposable {
        /// <summary>
        /// dependencies
        /// </summary>
        private readonly CPClass cp;
        /// <summary>
        /// All db controller calls go to this object. 
        /// If this instance was created with the default datasource, db is set the core.db
        /// </summary>
        internal DbController db;
        /// <summary>
        /// 
        /// </summary>
        public enum RemoteQueryType {
            sql = 1,
            openContent = 2,
            updateContent = 3,
            insertContent = 4
        }
        //
        //====================================================================================================
        /// <summary>
        /// Construct. This object is just a wrapper to create a consistent API passed to addon execution. The
        /// actual code is all within coreController. During startup, core loads and uses the default datasource
        /// at core.db. That is the only Db implemention used internally.
        /// If an Addon uses CP.Db, a new Db controller is used (so there can be 2 DbControllers on the default datasource)
        /// If an addon creates a new DbController instance using CP.Db New(datasource) a new instance of this class 
        /// creates a new instance of dbcontroller. There may be mulitple instances of dbcontroller pointing to the same Db,
        /// but this construct makes disposing more straight forward.
        /// </summary>
        /// <param name="cpParent"></param>
        public CPDbClass(CPClass cpParent, string dataSourceName) {
            cp = cpParent;
            db = new DbController(cp.core, dataSourceName);
        }
        //
        //====================================================================================================
        //
        public override string GetConnectionString()  {
            return db.getConnectionStringADONET(cp.core.appConfig.name);
        }
        //
        //====================================================================================================
        //
        public override DataTable ExecuteQuery(string sql, int startRecord, int maxRecords) {
            return db.executeQuery(sql, startRecord, maxRecords);
        }
        //
        //====================================================================================================
        //
        public override DataTable ExecuteQuery(string sql, int startRecord) {
            return db.executeQuery(sql, startRecord);
        }
        //
        //====================================================================================================
        //
        public override void ExecuteNonQuery(string sql, ref int recordsAffected) {
            db.executeNonQuery(sql, ref recordsAffected);
        }
        //
        //====================================================================================================
        //
        public override string GetRemoteQueryKey(string sql, int pageSize) {
            string returnKey = "";
            try {
                using (var cs = new CPCSClass(cp)) {
                    //
                    if (pageSize == 0) {
                        pageSize = 9999;
                    }
                    if (cs.Insert("Remote Queries")) {
                        returnKey = GenericController.getGUIDNaked();
                        int dataSourceId = cp.Content.GetRecordID("Data Sources", "");
                        cs.SetField("remotekey", returnKey);
                        cs.SetField("datasourceid", dataSourceId.ToString());
                        cs.SetField("sqlquery", sql);
                        cs.SetField("maxRows", pageSize.ToString());
                        cs.SetField("dateexpires", cp.core.dateTimeNowMockable.AddDays(1).ToString());
                        cs.SetField("QueryTypeID", RemoteQueryType.sql.ToString());
                        cs.SetField("VisitID", cp.Visit.Id.ToString());
                    }
                    cs.Close();
                    //
                }
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
            }
            return returnKey;
        }
        //
        //====================================================================================================
        //
        public override string EncodeSQLBoolean(bool SourceBoolean) {
            return DbController.encodeSQLBoolean(SourceBoolean);
        }
        //
        //====================================================================================================
        //
        public override string EncodeSQLDate(DateTime SourceDate) {
            return DbController.encodeSQLDate(SourceDate);
        }
        //
        //====================================================================================================
        //
        public override string EncodeSQLNumber(double SourceNumber) {
            return DbController.encodeSQLNumber(SourceNumber);
        }
        //
        //====================================================================================================
        //
        public override string EncodeSQLText(string SourceText) {
            return DbController.encodeSQLText(SourceText);
        }
        //
        //====================================================================================================
        //
        public override string EncodeSQLTextLike(string SourceText) {
            return DbController.encodeSqlTextLike(SourceText);
        }
        //
        //====================================================================================================
        //
        public override string GetRemoteQueryKey(string sql)
            => GetRemoteQueryKey(sql, DbController.sqlPageSizeDefault);
        //
        //====================================================================================================
        //
        public override int SQLTimeout {
            get {
                return db.sqlCommandTimeout;
            }
            set {
                db.sqlCommandTimeout = value;
            }
        }
        //
        //====================================================================================================
        //
        public override void Delete(string TableName, int RecordId) {
            db.delete(RecordId, TableName);
        }
        //
        //====================================================================================================
        //
        public override string EncodeSQLNumber(int SourceNumber) {
            return DbController.encodeSQLNumber(SourceNumber);
        }
        //
        //====================================================================================================
        //
        public override void ExecuteNonQuery(string sql) {
            db.executeNonQuery(sql);
        }
        //
        //====================================================================================================
        //
        public override async Task<int> ExecuteNonQueryAsync(string sql) {
            return await db.executeNonQueryAsync(sql);
        }
        //
        //====================================================================================================
        //
        public override DataTable ExecuteQuery(string sql) {
            return db.executeQuery(sql);
        }
        //
        //====================================================================================================
        //
        public override bool IsTable(string TableName) {
            return db.isSQLTable(TableName);
        }
        //
        //====================================================================================================
        //
        public override bool IsTableField(string TableName, string FieldName) {
            return db.isSQLTableField(TableName, FieldName);
        }
        //
        //====================================================================================================
        //
        public override DataTable ExecuteRemoteQuery(string remoteQueryKey) {
            return db.executeRemoteQuery(remoteQueryKey);
        }
        //
        //====================================================================================================
        //
        public override string CreateFieldPathFilename(string tableName, string fieldName, int recordId, CPContentBaseClass.FieldTypeIdEnum fieldType) {
            return FileController.getVirtualRecordUnixPathFilename(tableName, fieldName, recordId, fieldType);
        }
        //
        //====================================================================================================
        //
        public override string CreateUploadFieldPath(string tableName, string fieldName, int recordId) {
            //
            // -- the only two valid fieldtypes. All the other file-field types are text fields backed with a file, like .css
            return FileController.getVirtualRecordUnixPath(tableName, fieldName, recordId);
        }
        //
        //====================================================================================================
        //
        public override string CreateUploadFieldPathFilename(string tableName, string fieldName, int recordId, string filename) {
            //
            // -- the only two valid fieldtypes. All the other file-field types are text fields backed with a file, like .css
            return FileController.getVirtualRecordUnixPathFilename(tableName, fieldName, recordId, filename);
        }
        //
        //====================================================================================================
        //
        [Obsolete("Deprecated. Use CreateUploadFieldPathFilename with no fieldType or FileType", false)]
        public override string CreateUploadFieldPathFilename(string tableName, string fieldName, int recordId, string filename, CPContentBaseClass.FieldTypeIdEnum fieldType) {
            if ((fieldType == CPContentBaseClass.FieldTypeIdEnum.File) || (fieldType == CPContentBaseClass.FieldTypeIdEnum.FileImage)) {
                //
                // -- the only two valid fieldtypes. All the other file-field types are text fields backed with a file, like .css
                return FileController.getVirtualRecordUnixPathFilename(tableName, fieldName, recordId, filename);
            }
            //
            // -- techically, this is a mistake the developer made calling this method. These types do not upload
            return FileController.getVirtualRecordUnixPathFilename(tableName, fieldName, recordId, fieldType);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Insert a record and return the records id
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="createdByUserId"></param>
        /// <returns></returns>
        public override int Add(string tableName, int createdByUserId) {
            return db.insertGetId(tableName, createdByUserId);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Insert a record and return the record in a datatable
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="createdByUserId"></param>
        /// <returns></returns>
        public override DataTable Insert(string tableName, int createdByUserId) {
            return db.insert(tableName, createdByUserId);
        }
        //
        //====================================================================================================
        /// <summary>
        /// insert a record with values provided.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="sqlList"></param>
        /// <param name="createdByUserId"></param>
        /// <returns></returns>
        public override DataTable Insert(string tableName, NameValueCollection sqlList, int createdByUserId) {
            return db.insert(tableName, sqlList, createdByUserId);
        }
        //
        //====================================================================================================
        //
        public override void Update(string tableName, string criteria, NameValueCollection sqlList) {
            db.update(tableName, criteria, sqlList);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Update a table record. Async not implemented
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="criteria"></param>
        /// <param name="sqlList"></param>
        /// <param name="async">Not implemented</param>
        public override void Update(string tableName, string criteria, NameValueCollection sqlList, bool async) {
            db.update(tableName, criteria, sqlList);
        }
        //
        //====================================================================================================
        //
        public override void Delete(string tableName, string guid) {
            db.delete(tableName, guid);
        }
        //
        //====================================================================================================
        //
        public override void DeleteRows(string tableName, string sqlCriteria) {
            db.deleteRows(tableName, sqlCriteria);
        }
        //
        //====================================================================================================
        // deprecated
        //
        [Obsolete("deprecated. Convert to datatables and use executeQuery(), executeNonQuery(), or executeNonQueryAsync()", false)]
        public override object ExecuteSQL(string sql, string ignoreDataSourceName, string ignoreRetries, string ignorePageSize, string ignorePageNumber) {
            db.executeNonQuery(sql);
            return null;
        }
        //
        [Obsolete("Convert to datatables or use models", false)]
        public override object ExecuteSQL(string sql, string ignoreDataSourceName, string Retries, string PageSize) {
            db.executeNonQuery(sql);
            return null;
        }
        //
        [Obsolete("Convert to datatables or use models", false)]
        public override object ExecuteSQL(string sql, string ignoreDataSourceName, string Retries) {
            db.executeNonQuery(sql);
            return null;
        }
        //
        [Obsolete("Convert to datatables or use models", false)]
        public override object ExecuteSQL(string sql, string ignoreDataSourceName) {
            db.executeNonQuery(sql);
            return null;
        }
        //
        [Obsolete("Convert to datatables or use models", false)]
        public override object ExecuteSQL(string sql) {
            db.executeNonQuery(sql);
            return null;
        }
        //
        [Obsolete("Use GetConnectionString( dataSourceName )")]
        public override string DbGetConnectionString(string ignoreDataSourceName) {
            return GetConnectionString();
        }
        //
        [Obsolete("Only Sql Server currently supported", false)]
        public override int GetDataSourceType(string ignoreDataSourceName) {
            return db.getDataSourceType();
        }
        //
        [Obsolete("Use GetDataSourceType( dataSourceName )")]
        public override int DbGetDataSourceType(string ignoreDataSourceName) {
            return db.getDataSourceType();
        }
        //
        [Obsolete("Use Db.Content.GetTableId instead.", false)]
        public override int DbGetTableID(string TableName) {
            return GetTableID(TableName);
        }
        //
        [Obsolete("Use isTable instead", false)]
        public override bool DbIsTable(string ignoreDataSourceName, string TableName) {
            return IsTable(TableName);
        }
        //
        [Obsolete("Use isTableField instead", false)]
        public override bool DbIsTableField(string ignoreDataSourceName, string TableName, string FieldName) {
            return IsTableField(TableName, FieldName);
        }
        //
        [Obsolete("Deprecated. Use methods without explicit datasource.", false)]
        public override DataTable ExecuteQuery(string sql, string ignoreDataSourceName) {
            return db.executeQuery(sql);
        }
        //
        [Obsolete("Deprecated. Use methods without explicit datasource.", false)]
        public override DataTable ExecuteQuery(string sql, string ignoreDataSourceName, int startRecord) {
            return db.executeQuery(sql, startRecord);
        }
        //
        [Obsolete("Deprecated. Use methods without explicit datasource.", false)]
        public override DataTable ExecuteQuery(string sql, string ignoreDataSourceName, int startRecord, int maxRecords) {
            return db.executeQuery(sql, startRecord, maxRecords);
        }
        //
        [Obsolete("Deprecated. Use methods without explicit datasource.", false)]
        public override void Delete(string ignoreDataSourceName, string TableName, int RecordId) {
            db.delete(RecordId, TableName);
        }
        //
        [Obsolete("Deprecated. Use methods without explicit datasource.", false)]
        public override string GetConnectionString(string ignoreDataSourceName) {
            return db.getConnectionStringADONET(cp.core.appConfig.name);
        }
        //
        [Obsolete("Deprecated. Use CP.Content.GetTableId().", false)]
        public override int GetTableID(string TableName) {
            return DbController.getTableID(cp.core, TableName);
        }
        //
        [Obsolete("Deprecated. Use methods without explicit datasource.", false)]
        public override bool IsTable(string ignoreDataSourceName, string TableName) {
            return db.isSQLTable(TableName);
        }
        //
        [Obsolete("Deprecated. Use methods without explicit datasource.", false)]
        public override bool IsTableField(string ignoreDataSourceName, string TableName, string FieldName) {
            return db.isSQLTableField(TableName, FieldName);
        }
        //
        [Obsolete("Deprecated. Use methods without explicit datasource.", false)]
        public override string GetRemoteQueryKey(string sql, string ignoreDataSourceName, int pageSize) {
            string returnKey = "";
            try {
                using (var cs = new CPCSClass(cp)) {
                    //
                    if (pageSize == 0) {
                        pageSize = 9999;
                    }
                    if (cs.Insert("Remote Queries")) {
                        returnKey = GenericController.getGUIDNaked();
                        int dataSourceId = cp.Content.GetRecordID("Data Sources", db.dataSourceName);
                        cs.SetField("remotekey", returnKey);
                        cs.SetField("datasourceid", dataSourceId.ToString());
                        cs.SetField("sqlquery", sql);
                        cs.SetField("maxRows", pageSize.ToString());
                        cs.SetField("dateexpires", cp.core.dateTimeNowMockable.AddDays(1).ToString());
                        cs.SetField("QueryTypeID", RemoteQueryType.sql.ToString());
                        cs.SetField("VisitID", cp.Visit.Id.ToString());
                    }
                    cs.Close();
                    //
                }
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
            }
            return returnKey;
        }
        //
        [Obsolete("Deprecated. Use methods without explicit datasource.", false)]
        public override string GetRemoteQueryKey(string sql, string ignoreDataSourceName)
            => GetRemoteQueryKey(sql, DbController.sqlPageSizeDefault);
        //
        [Obsolete("Deprecated. Use methods without explicit datasource.", false)]
        public override void ExecuteNonQuery(string sql, string ignoreDataSourceName) {
            db.executeNonQuery(sql);
        }
        //
        [Obsolete("Deprecated. Use methods without explicit datasource.", false)]
        public override void ExecuteNonQuery(string sql, string ignoreDataSourceName, ref int recordsAffected) {
            db.executeNonQuery(sql, ref recordsAffected);
        }
        //
        [Obsolete("Deprecated. Use methods without explicit datasource.", false)]
        public override void ExecuteNonQueryAsync(string sql, string ignoreDataSourceName) {
            Task.Run(() => cp.core.db.executeNonQueryAsync(sql));
        }
        //
        [Obsolete("Deprecated. Use methods with FieldTypeIdEnum.", false)]
        public override string CreateFieldPathFilename(string tableName, string fieldName, int recordId, CPContentBaseClass.fileTypeIdEnum fieldType) {
            return FileController.getVirtualRecordUnixPathFilename(tableName, fieldName, recordId, (CPContentBaseClass.FieldTypeIdEnum)fieldType);
        }
        [Obsolete("Deprecated. Use methods with FieldTypeIdEnum.", false)]
        public override string CreateUploadFieldPathFilename(string tableName, string fieldName, int recordId, string filename, CPContentBaseClass.fileTypeIdEnum fieldType) {
            var fieldTypeCorrected = (CPContentBaseClass.FieldTypeIdEnum)fieldType;
            if ((fieldTypeCorrected == CPContentBaseClass.FieldTypeIdEnum.File) || (fieldTypeCorrected == CPContentBaseClass.FieldTypeIdEnum.FileImage)) {
                return FileController.getVirtualRecordUnixPathFilename(tableName, fieldName, recordId, filename);
            }
            return FileController.getVirtualRecordUnixPathFilename(tableName, fieldName, recordId, fieldTypeCorrected);
        }
        //
        //====================================================================================================
        //
        #region  IDisposable Support 
        //
        protected bool disposed_db;
        //====================================================================================================
        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing_db"></param>
        protected virtual void Dispose(bool disposing_db) {
            if (!this.disposed_db) {
                if (disposing_db) {
                    //
                    // call .dispose for managed objects
                    //
                }
                //
                // Add code here to release the unmanaged resource.
                //
            }
            this.disposed_db = true;
        }
        // Do not change or add Overridable to these methods.
        // Put cleanup code in Dispose(ByVal disposing As Boolean).
        public override void Dispose()  {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~CPDbClass()  {
            Dispose(false);
        }
        #endregion
        //
    }
}