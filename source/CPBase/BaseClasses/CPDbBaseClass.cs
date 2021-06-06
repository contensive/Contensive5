
using System;
using System.Collections.Specialized;
using System.Data;
using System.Threading.Tasks;

namespace Contensive.BaseClasses {
    /// <summary>
    /// CP.Db - This object references the database directly
    /// </summary>
    /// <remarks></remarks>
    public abstract class CPDbBaseClass : IDisposable {
        //
        //====================================================================================================
        /// <summary>
        /// creates a pathFilename for a field that stores text content, like css, js, etc.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="fieldName"></param>
        /// <param name="recordId"></param>
        /// <param name="fieldTypeId"></param>
        /// <returns></returns>
        public abstract string CreateFieldPathFilename(string tableName, string fieldName, int recordId, CPContentBaseClass.FieldTypeIdEnum fieldTypeId);
        //
        //====================================================================================================
        /// <summary>
        /// creates the pathFilename for a field that holds the path and filename of an uploaded file (Only type=file and type=image)
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="fieldName"></param>
        /// <param name="recordId"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        public abstract string CreateUploadFieldPathFilename(string tableName, string fieldName, int recordId, string filename);
        //
        //====================================================================================================
        /// <summary>
        /// creates a path for a field that holds the path and filename of an uploaded file (Only type=file and type=image)
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="fieldName"></param>
        /// <param name="recordId"></param>
        /// <returns></returns>
        public abstract string CreateUploadFieldPath(string tableName, string fieldName, int recordId);
        //
        //====================================================================================================
        /// <summary>
        /// Add a record to a table in the default datasource and return its Id
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="createdByUserId"></param>
        /// <returns></returns>
        public abstract int Add(string tableName, int createdByUserId);
        //
        //====================================================================================================
        /// <summary>
        /// Add a record to a table and return the record in a datatable
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="createdByUserId"></param>
        /// <returns></returns>
        public abstract DataTable Insert(string tableName, int createdByUserId);
        //
        //====================================================================================================
        /// <summary>
        /// Add a record to a table and return the record in a datatable
        /// </summary>
        /// <param name="tableName">The table to be inserted.</param>
        /// <param name="sqlList">NameValue pairs to be inserted. Name is the field name. Value is saved to the field, formatted for an sql statement according to the correct type. ex sqlList.Add("name", cp.db.EncodeSqlText("Bob Smith"))</param>
        /// <param name="createdByUserId">The user id to be set in the created-by and modified-by fields</param>
        /// <returns></returns>
        public abstract DataTable Insert(string tableName, NameValueCollection sqlList, int createdByUserId);
        //
        //====================================================================================================
        /// <summary>
        /// update records in a table. Criteria is an sql compatible where-clause, the sqlList is a list of fields and values to update
        /// </summary>
        /// <param name="tableName">The table to be updated.</param>
        /// <param name="criteria">Sql compatible where clause to select the record(s) that will be updated.</param>
        /// <param name="sqlList">NameValue pairs. Name is the field name. Value is saved to the field, formatted for an sql statement according to the correct type. ex sqlList.Add("name", cp.db.EncodeSqlText("Bob Smith"))</param>
        public abstract void Update(string tableName, string criteria, NameValueCollection sqlList);
        //
        //====================================================================================================
        /// <summary>
        /// update records in a table. Criteria is an sql compatible where-clause, the sqlList is a list of fields and values to update. If Async is true, the query is queued for later
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="criteria"></param>
        /// <param name="sqlList"></param>
        /// <param name="Async"></param>
        public abstract void Update(string tableName, string criteria, NameValueCollection sqlList, bool Async);
        //
        //====================================================================================================
        /// <summary>
        /// Delete the record specified by tablename and recordId
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="recordId"></param>
        public abstract void Delete(string tableName, int recordId);
        //
        //====================================================================================================
        /// <summary>
        /// Delete the record specified by tablename and guid
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="guid"></param>
        public abstract void Delete(string tableName, string guid);
        //
        //====================================================================================================
        /// <summary>
        /// Delete the record specified by tablename and guid
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="sqlCriteria"></param>
        public abstract void DeleteRows(string tableName, string sqlCriteria);
        //
        //====================================================================================================
        /// <summary>
        /// get the connection string for the default device 
        /// </summary>
        /// <returns></returns>
        public abstract string GetConnectionString();
        //
        //====================================================================================================
        /// <summary>
        /// Return true if this is a valid database table in the current application
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public abstract bool IsTable(string tableName);
        //
        //====================================================================================================
        /// <summary>
        /// Return true if this is a valid database field in a valid table in the current application
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public abstract bool IsTableField(string tableName, string fieldName);
        //
        //====================================================================================================
        /// <summary>
        /// Encode a boolean value to be used in an sql Query for this application. Boolean fields are stored as integers in Contensive. Example 'select id from ccmembers where active='+EncodeSqlBoolean(true)
        /// </summary>
        /// <param name="sourceBoolean"></param>
        /// <returns></returns>
        public abstract string EncodeSQLBoolean(bool sourceBoolean);
        //
        //====================================================================================================
        /// <summary>
        /// Encode a date value to be used in an sql Query for this application. Example 'select id from ccmembers where dateadded>'+EncodeSqlDate( myBirthday )
        /// </summary>
        /// <param name="sourceDate"></param>
        /// <returns></returns>
		public abstract string EncodeSQLDate(DateTime sourceDate);
        //
        //====================================================================================================
        /// <summary>
        /// Encode a numeric value (integer or double) to be used in an sql Query for this application. Example 'select id from orders where totalAmount>'+EncodeSqlNumber( 1000.00 )
        /// </summary>
        /// <param name="sourceNumber"></param>
        /// <returns></returns>
		public abstract string EncodeSQLNumber(double sourceNumber);
        //
        //====================================================================================================
        /// <summary>
        /// Encode a numeric value (integer or double) to be used in an sql Query for this application. Example 'select id where id>'+EncodeSqlNumber( 1000 )
        /// </summary>
        /// <param name="sourceNumber"></param>
        /// <returns></returns>
        public abstract string EncodeSQLNumber(int sourceNumber);
        //
        //====================================================================================================
        /// <summary>
        /// Encode a text string value to be used in an sql Query for this application. Example 'select id where name='+EncodeSqlText( 'bob' )
        /// </summary>
        /// <param name="sourceText"></param>
        /// <returns></returns>
        public abstract string EncodeSQLText(string sourceText);
        //
        //====================================================================================================
        /// <summary>
        /// Encode a text string value to be used in an sql Query for this application. Example 'select id where name like '+EncodeSqlText( 'bob' )
        /// </summary>
        /// <param name="sourceText"></param>
        /// <returns></returns>
        public abstract string EncodeSQLTextLike(string sourceText);
        //
        //====================================================================================================
        /// <summary>
        /// Execute a query and return a datatable.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="startRecord">0 based start record</param>
        /// <param name="maxRecords"></param>
        /// <returns></returns>
        public abstract DataTable ExecuteQuery(string sql, int startRecord, int maxRecords);
        //
        //====================================================================================================
        /// <summary>
        /// Execute an sql query and return a datatable. Max records returned is 10M. 
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="startRecord">0 based start record</param>
        /// <returns></returns>
        public abstract DataTable ExecuteQuery(string sql, int startRecord);
        //
        //====================================================================================================
        /// <summary>
        /// Execute an sql query and return a datatable. Start at record 0,  max records returned is 10M. 
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public abstract DataTable ExecuteQuery(string sql);
        //
        //====================================================================================================
        /// <summary>
        /// Execute an sql command on a specific datasource. No data is returned.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="recordsAffected"></param>
        public abstract void ExecuteNonQuery(string sql, ref int recordsAffected);
        //
        //====================================================================================================
        /// <summary>
        /// Execute an sql command on a specific datasource. No data is returned.
        /// </summary>
        /// <param name="sql"></param>
        public abstract void ExecuteNonQuery(string sql);
        //
        //====================================================================================================
        /// <summary>
        /// Execute an sql command on a specific datasource, dotnet async task pattern.
        /// </summary>
        /// <param name="sql"></param>
        public abstract Task<int> ExecuteNonQueryAsync(string sql);
        //
        //====================================================================================================
        /// <summary>
        /// get or set the timeout in seconds for all Db methods in the current process
        /// </summary>
        public abstract int SQLTimeout { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// To give a remote method the ability to run an arbitrary query, store it as a remote query and executeRemoteQuery()
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public abstract string GetRemoteQueryKey(string sql, int pageSize);
        //
        //====================================================================================================
        /// <summary>
        /// To give a remote method the ability to run an arbitrary query, store it as a remote query and executeRemoteQuery()
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public abstract string GetRemoteQueryKey(string sql);
        //
        //====================================================================================================
        /// <summary>
        /// Execute a sql query stored with getRemoteQueryKey.
        /// </summary>
        /// <param name="remoteQueryKey"></param>
        /// <returns></returns>
        public abstract DataTable ExecuteRemoteQuery(string remoteQueryKey);
        //
        //====================================================================================================
        /// <summary>
        /// Support disposable for non-default datasources
        /// </summary>
        public abstract void Dispose();
        //
        //====================================================================================================
        // deprecated
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="SQL"></param>
        /// <param name="DataSourcename"></param>
        /// <param name="Retries"></param>
        /// <param name="PageSize"></param>
        /// <param name="PageNumber"></param>
        /// <returns></returns>
        [Obsolete("Convert to datatables and use executeQuery(), executeNonQuery(), or executeNonQueryAsync()", false)]
        public abstract object ExecuteSQL(string SQL, string DataSourcename, string Retries, string PageSize, string PageNumber);
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="SQL"></param>
        /// <param name="DataSourcename"></param>
        /// <param name="Retries"></param>
        /// <param name="PageSize"></param>
        /// <returns></returns>
        [Obsolete("Convert to datatables and use executeQuery(), executeNonQuery(), or executeNonQueryAsync()", false)]
        public abstract object ExecuteSQL(string SQL, string DataSourcename, string Retries, string PageSize);
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="SQL"></param>
        /// <param name="DataSourcename"></param>
        /// <param name="Retries"></param>
        /// <returns></returns>
        [Obsolete("Convert to datatables and use executeQuery(), executeNonQuery(), or executeNonQueryAsync()", false)]
        public abstract object ExecuteSQL(string SQL, string DataSourcename, string Retries);
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="SQL"></param>
        /// <param name="DataSourcename"></param>
        /// <returns></returns>
        [Obsolete("Convert to datatables and use executeQuery(), executeNonQuery(), or executeNonQueryAsync()", false)]
        public abstract object ExecuteSQL(string SQL, string DataSourcename);
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="SQL"></param>
        /// <returns></returns>
        [Obsolete("Convert to datatables and use executeQuery(), executeNonQuery(), or executeNonQueryAsync()", false)]
        public abstract object ExecuteSQL(string SQL);
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="DataSourcename"></param>
        /// <returns></returns>
        [Obsolete("Use GetConnectionString( dataSourceName )")]
        public abstract string DbGetConnectionString(string DataSourcename);
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="DataSourcename"></param>
        /// <returns></returns>
        [Obsolete("Use GetDataSourceType( dataSourceName )")]
        public abstract int DbGetDataSourceType(string DataSourcename);
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="TableName"></param>
        /// <returns></returns>
        [Obsolete("Use GetTableId instead.", false)]
        public abstract int DbGetTableID(string TableName);
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="DataSourcename"></param>
        /// <param name="TableName"></param>
        /// <returns></returns>
        [Obsolete("Use isTable instead", false)]
        public abstract bool DbIsTable(string DataSourcename, string TableName);
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="DataSourcename"></param>
        /// <param name="TableName"></param>
        /// <param name="FieldName"></param>
        /// <returns></returns>
        [Obsolete("Use isTableField instead", false)]
        public abstract bool DbIsTableField(string DataSourcename, string TableName, string FieldName);
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        [Obsolete("Deprecated. Use CP.Content.GetTableId().", false)]
        public abstract int GetTableID(string tableName);
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="DataSourcename"></param>
        /// <returns></returns>
        [Obsolete("Only Sql Server currently supported", false)]
        public abstract int GetDataSourceType(string DataSourcename);
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="dataSourcename"></param>
        /// <param name="tableName"></param>
        /// <param name="recordId"></param>
		[Obsolete("Deprecated. Use methods without datasource reference. Instead create non-default datasource with CP.DbNew(datasourcename).", false)]
        public abstract void Delete(string dataSourcename, string tableName, int recordId);
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="dataSourcename"></param>
        /// <returns></returns>
        [Obsolete("Deprecated. Use methods without datasource.", false)]
        public abstract string GetConnectionString(string dataSourcename);
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="dataSourcename"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        [Obsolete("Deprecated. Use methods without datasource.", false)]
        public abstract bool IsTable(string dataSourcename, string tableName);
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="dataSourcename"></param>
        /// <param name="tableName"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        [Obsolete("Deprecated. Use methods without datasource.", false)]
        public abstract bool IsTableField(string dataSourcename, string tableName, string fieldName);
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="dataSourceName"></param>
        /// <param name="startRecord"></param>
        /// <param name="maxRecords"></param>
        /// <returns></returns>
        [Obsolete("Deprecated. Use methods without datasource.", false)]
        public abstract DataTable ExecuteQuery(string sql, string dataSourceName, int startRecord, int maxRecords);
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="dataSourceName"></param>
        /// <param name="startRecord"></param>
        /// <returns></returns>
        [Obsolete("Deprecated. Use methods without datasource.", false)]
        public abstract DataTable ExecuteQuery(string sql, string dataSourceName, int startRecord);
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="dataSourceName"></param>
        /// <returns></returns>
        [Obsolete("Deprecated. Use methods without datasource.", false)]
        public abstract DataTable ExecuteQuery(string sql, string dataSourceName);
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="dataSourceName"></param>
        /// <param name="recordsAffected"></param>
        [Obsolete("Deprecated. Use methods without datasource.", false)]
        public abstract void ExecuteNonQuery(string sql, string dataSourceName, ref int recordsAffected);
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="dataSourceName"></param>
        [Obsolete("Deprecated. Use methods without datasource.", false)]
        public abstract void ExecuteNonQuery(string sql, string dataSourceName);
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="dataSourceName"></param>
        [Obsolete("Deprecated. Use methods without datasource.", false)]
        public abstract void ExecuteNonQueryAsync(string sql, string dataSourceName);
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="DataSourceName"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [Obsolete("Deprecated. Use methods without datasource.", false)]
        public abstract string GetRemoteQueryKey(string sql, string DataSourceName, int pageSize);
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="DataSourceName"></param>
        /// <returns></returns>
        [Obsolete("Deprecated. Use methods without datasource.", false)]
        public abstract string GetRemoteQueryKey(string sql, string DataSourceName);
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="fieldName"></param>
        /// <param name="recordId"></param>
        /// <param name="fieldType"></param>
        /// <returns></returns>
        [Obsolete("Deprecated. Use CreateUploadFieldPathFilename with no fieldType or FileType", false)]
        public abstract string CreateFieldPathFilename(string tableName, string fieldName, int recordId, CPContentBaseClass.fileTypeIdEnum fieldType);
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="fieldName"></param>
        /// <param name="recordId"></param>
        /// <param name="filename"></param>
        /// <param name="fileType"></param>
        /// <returns></returns>
        [Obsolete("Deprecated. Use CreateUploadFieldPathFilename with no fieldType or FileType", false)]
        public abstract string CreateUploadFieldPathFilename(string tableName, string fieldName, int recordId, string filename, CPContentBaseClass.fileTypeIdEnum fileType);
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="fieldName"></param>
        /// <param name="recordId"></param>
        /// <param name="filename"></param>
        /// <param name="fieldTypeId"></param>
        /// <returns></returns>
        [Obsolete("Deprecated. Use CreateUploadFieldPathFilename with no fieldType or FileType", false)]
        public abstract string CreateUploadFieldPathFilename(string tableName, string fieldName, int recordId, string filename, CPContentBaseClass.FieldTypeIdEnum fieldTypeId);
    }

}

