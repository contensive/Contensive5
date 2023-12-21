
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Exceptions;
using Contensive.Processor.Models.Domain;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;
using static Contensive.Processor.Constants;
using static Contensive.Processor.Controllers.GenericController;

namespace Contensive.Processor.Controllers {
    //
    // todo - convert so each datasource has its own dbController - removing the datasource argument from every call
    // todo - this is only a type of database. Need support for noSql datasources also, and maybe read-only static file datasources (like a json file, or xml file)
    //==========================================================================================
    /// <summary>
    /// Data Access Layer for individual catalogs. Properties and Methods related to interfacing with the Database
    /// </summary>
    public partial class DbController : IDisposable {
        //
        /// <summary>
        /// dependencies
        /// </summary>
        private readonly CoreController core;
        /// <summary>
        /// The datasouorce used for this instance of the object
        /// </summary>
        internal string dataSourceName;
        //
        /// <summary>
        /// default page size. Page size is how many records are read in a single fetch.
        /// </summary>
        internal const int sqlPageSizeDefault = 9999999;
        //
        //========================================================================
        // Sql/Db
        //
        internal const string SQLTrue = "1";
        internal const string SQLFalse = "0";
        //
        /// <summary>
        /// set true when configured and tested - else db calls are skipped, simple lazy cached values
        /// </summary>
        private bool dbEnabled { get; set; } = true;
        //
        /// <summary>
        /// connection string, lazy cache
        /// </summary>
        private Dictionary<string, string> connectionStringDict { get; set; } = new Dictionary<string, string>();
        //
        /// <summary>
        /// above this threshold, queries are logged as slow
        /// </summary>
        public int sqlSlowThreshholdMsec { get; set; } = 1000;
        //
        /// <summary>
        /// above this threshold, queries are logged as slow
        /// </summary>
        public int sqlAsyncSlowThreshholdMsec { get; set; } = 10000;
        //
        /// <summary>
        /// timeout in seconds
        /// </summary>
        public int sqlCommandTimeout { get; set; } = 30;
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        //
        //==========================================================================================
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="core">dependencies</param>
        /// <param name="dataSourceName">The datasource. The default datasource is setup in the config file. Others are in the Datasources table</param>
        public DbController(CoreController core, string dataSourceName) {
            this.core = core;
            this.dataSourceName = dataSourceName;
        }
        //
        //====================================================================================================
        /// <summary>
        /// adonet connection string for this datasource. datasource represents the server, catalog is the application. 
        /// </summary>
        /// <returns>
        /// </returns>
        public string getConnectionStringADONET(string catalogName) {
            //
            // (OLEDB) OLE DB Provider for SQL Server > "Provider=sqloledb;Data Source=MyServerName;Initial Catalog=MyDatabaseName;User Id=MyUsername;Password=MyPassword;"
            //     https://www.codeproject.com/Articles/2304/ADO-Connection-Strings#OLE%20DB%20SqlServer
            //
            // (OLEDB) Microsoft OLE DB Provider for SQL Server connection strings > "Provider=sqloledb;Data Source=myServerAddress;Initial Catalog=myDataBase;User Id = myUsername;Password=myPassword;"
            //     https://www.connectionstrings.com/microsoft-ole-db-provider-for-sql-server-sqloledb/
            //
            // (ADONET) .NET Framework Data Provider for SQL Server > Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password = myPassword;
            //     https://www.connectionstrings.com/sql-server/
            //
            try {
                //
                // -- simple local cache so it does not have to be recreated each time
                string key = "catalog:" + catalogName + "/datasource:" + dataSourceName;
                if (connectionStringDict.ContainsKey(key)) { return connectionStringDict[key]; }
                //
                // -- lookup dataSource
                string normalizedDataSourceName = DataSourceModel.normalizeDataSourceName(dataSourceName);
                if (string.IsNullOrEmpty(normalizedDataSourceName) || (normalizedDataSourceName == "default")) {
                    //
                    // -- default datasource
                    string defaultConnString = core.dbServer.getConnectionStringADONET() + "Database=" + catalogName + ";";
                    connectionStringDict.Add(key, defaultConnString);
                    return defaultConnString;
                }
                //
                // -- custom datasource from Db in primary datasource
                if (!core.cacheRuntime.dataSourceDictionary.ContainsKey(normalizedDataSourceName)) {
                    //
                    // -- not found, this is a hard error
                    throw new GenericException("Datasource [" + normalizedDataSourceName + "] was not found.");
                }
                //
                // -- found in local cache
                var datasource = core.cacheRuntime.dataSourceDictionary[normalizedDataSourceName];
                string returnConnString = ""
                    + "server=tcp:" + datasource.endpoint + ";"
                    + "User Id=" + datasource.username + ";"
                    + "Password=" + datasource.password + ";"
                    + "Database=" + catalogName + ";";
                //
                // -- add certificate requirement, if true, set yes, if false, no not add it
                if (datasource.secure) {
                    returnConnString += "Encrypt=yes;";
                }
                connectionStringDict.Add(key, returnConnString);
                return returnConnString;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Execute a query (returns data)
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="startRecord">0 based start record</param>
        /// <param name="maxRecords"></param>
        /// <returns></returns>
        public DataTable executeQuery(string sql, int startRecord, int maxRecords) {
            int recordsReturned = 0;
            return executeQuery(sql, startRecord, maxRecords, ref recordsReturned);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Execute a query (returns data). max records 10M
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="startRecord">0 based start record</param>
        /// <returns></returns>
        public DataTable executeQuery(string sql, int startRecord) {
            int tempVar = 0;
            return executeQuery(sql, startRecord, DbController.sqlPageSizeDefault, ref tempVar);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Execute a query (returns data) on
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public DataTable executeQuery(string sql) {
            int tempVar = 0;
            return executeQuery(sql, 0, DbController.sqlPageSizeDefault, ref tempVar);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Execute a query (returns data)
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="startRecord">0 based start record</param>
        /// <param name="maxRecords">required. max number of records the client will support.</param>
        /// <param name="recordsAffected"></param>
        /// <returns></returns>
        public DataTable executeQuery(string sql, int startRecord, int maxRecords, ref int recordsAffected) {
            DataTable returnData = new();
            try {
                if (!dbEnabled) { return new DataTable(); }
                if (core.serverConfig == null) { throw new GenericException("Cannot execute Sql in dbController, servercong is null"); }
                if (core.appConfig == null) { throw new GenericException("Cannot execute Sql in dbController, appconfig is null"); }
                //
                // REFACTOR
                // consider writing cs intrface to sql dataReader object -- one row at a time, vaster.
                // https://msdn.microsoft.com/en-us/library/system.data.sqlclient.sqldatareader.aspx
                //
                Stopwatch sw = Stopwatch.StartNew();
                using (SqlConnection connSQL = new(getConnectionStringADONET(core.appConfig.name))) {
                    int retryCnt = 1;
                    bool success = false;
                    do {
                        try {
                            connSQL.Open();
                            success = true;
                        } catch (SqlException exSql) {
                            //
                            // network related error, retry once
                            try {
                                string errMsg = "executeQuery SqlException, retries left [" + retryCnt.ToString() + "], ex [" + exSql.ToString() + "]";
                                Logger.Error(exSql, LogController.processLogMessage(core, errMsg, true));
                            } catch (Exception) {
                                // -- swallow logging internal errors
                            }
                            //
                            if (retryCnt <= 0) { throw; }
                            retryCnt--;
                        } catch (Exception ex) {
                            LogController.logError(core, ex);
                            throw;
                        }
                    } while (!success && (retryCnt >= 0));
                    using (SqlCommand cmdSQL = new()) {
                        cmdSQL.CommandType = CommandType.Text;
                        cmdSQL.CommandText = sql;
                        cmdSQL.Connection = connSQL;
                        cmdSQL.CommandTimeout = sqlCommandTimeout;
                        using (SqlDataAdapter adptSQL = new(cmdSQL)) {
                            recordsAffected = adptSQL.Fill(startRecord, maxRecords, returnData);
                        }
                    }
                }
                try {
                    string logMsg = ", duration [" + sw.ElapsedMilliseconds + "ms], recordsAffected [" + recordsAffected + "], sql [" + sql.Replace("\r", " ").Replace("\n", " ") + "]";
                    if (sw.ElapsedMilliseconds > sqlSlowThreshholdMsec) {
                        Logger.Warn(LogController.processLogMessage(core, "Slow Query " + logMsg, true));
                    } else {
                        Logger.Debug(LogController.processLogMessage(core, logMsg, false));
                    }
                } catch (Exception) {
                    // -- swallow logging internal errors
                }
            } catch (Exception ex) {
                LogController.logError(core, new GenericException("Exception [" + ex.Message + "] executing sql [" + sql + "], datasource [" + dataSourceName + "], startRecord [" + startRecord + "], maxRecords [" + maxRecords + "], recordsReturned [" + recordsAffected + "]", ex));
                throw;
            }
            return returnData;
        }
        //
        //====================================================================================================
        /// <summary>
        /// execute sql
        /// </summary>
        /// <param name="sql"></param>
        public void executeNonQuery(string sql) {
            int recordsAffectedIgnore = 0;
            executeNonQuery(sql, ref recordsAffectedIgnore);
        }
        //
        //====================================================================================================
        /// <summary>
        /// execute a nonQuery command (non-record returning) and return records affected
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="recordsAffected"></param>
        public void executeNonQuery(string sql, ref int recordsAffected) {
            try {
                if (!dbEnabled) { return; }
                Stopwatch sw = Stopwatch.StartNew();
                using (SqlConnection connSQL = new(getConnectionStringADONET(core.appConfig.name))) {
                    connSQL.Open();
                    using (SqlCommand cmdSQL = new()) {
                        cmdSQL.CommandType = CommandType.Text;
                        cmdSQL.CommandText = sql;
                        cmdSQL.Connection = connSQL;
                        cmdSQL.CommandTimeout = sqlCommandTimeout;
                        recordsAffected = cmdSQL.ExecuteNonQuery();
                    }
                }
                try {
                    string logMsg = ", duration [" + sw.ElapsedMilliseconds + "ms], recordsAffected [" + recordsAffected + "], sql [" + sql.Replace("\r", " ").Replace("\n", " ") + "]";
                    if (sw.ElapsedMilliseconds > sqlSlowThreshholdMsec) {
                        Logger.Warn(LogController.processLogMessage(core, "Slow Query " + logMsg, true));
                    } else {
                        Logger.Debug(LogController.processLogMessage(core, logMsg, false));
                    }
                } catch (Exception) {
                    // -- swallow logging internal errors
                }
            } catch (Exception ex) {
                LogController.logError(core, new GenericException("Exception [" + ex.Message + "] executing sql [" + sql + "], datasource [" + dataSourceName + "], recordsAffected [" + recordsAffected + "]", ex));
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// execute sql on a specific datasource asynchonously. No data is returned.
        /// </summary>
        /// <param name="sql"></param>
        public async Task<int> executeNonQueryAsync(string sql) {
            try {
                int result = 0;
                if (!dbEnabled) { return result; }
                Stopwatch sw = Stopwatch.StartNew();
                using (SqlConnection connSQL = new(getConnectionStringADONET(core.appConfig.name))) {
                    connSQL.Open();
                    using (SqlCommand cmdSQL = new()) {
                        cmdSQL.CommandType = CommandType.Text;
                        cmdSQL.CommandText = sql;
                        cmdSQL.Connection = connSQL;
                        cmdSQL.CommandTimeout = sqlCommandTimeout;
                        result = await cmdSQL.ExecuteNonQueryAsync();
                    }
                }
                try {
                    string logMsg = ", duration [" + sw.ElapsedMilliseconds + "ms], recordsAffected [n/a], sql [" + sql.Replace("\r", " ").Replace("\n", " ") + "]";
                    if (sw.ElapsedMilliseconds > sqlSlowThreshholdMsec) {
                        Logger.Warn(LogController.processLogMessage(core, "Slow Query " + logMsg, true));
                    } else {
                        Logger.Debug(LogController.processLogMessage(core, logMsg, false));
                    }
                } catch (Exception) {
                    // -- swallow logging internal errors
                }
                return result;
            } catch (Exception ex) {
                LogController.logError(core, new GenericException("Exception [" + ex.Message + "] executing sql [" + sql + "], datasource [" + dataSourceName + "]", ex));
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// Update a record in a table
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="criteria"></param>
        /// <param name="sqlList"></param>
        public void update(string tableName, string criteria, NameValueCollection sqlList) {
            try {
                executeNonQuery("update " + tableName + " set " + sqlList.getNameValueList() + " where " + criteria + ";");
            } catch (Exception ex) {
                LogController.logError(core, new GenericException("Exception [" + ex.Message + "] updating table [" + tableName + "], criteria [" + criteria + "], dataSourceName [" + dataSourceName + "]", ex));
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// insert a record into a table and returns the ID
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="createdByUserId"></param>
        /// <returns></returns>
        public int insertGetId(string tableName, int createdByUserId) {
            try {
                using (DataTable dt = insert(tableName, createdByUserId)) {
                    if (dt.Rows.Count > 0) { return encodeInteger(dt.Rows[0]["id"]); }
                }
                return 0;
            } catch (Exception ex) {
                LogController.logError(core, new GenericException("Exception [" + ex.Message + "] inserting table [" + tableName + "], dataSourceName [" + dataSourceName + "]", ex));
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// Use to verify base fields (name, guid, etc) are in a namevalue set to be used for insert
        /// </summary>
        /// <param name="sqlList"></param>
        /// <returns></returns>
        public NameValueCollection verifyBaseSqlNameValueFields(NameValueCollection sqlList, int userId) {
            if (sqlList["ccguid"] == null) { sqlList.Add("ccguid", encodeSQLText(GenericController.getGUID())); };
            if (sqlList["dateadded"] == null) { sqlList.Add("dateadded", encodeSQLDate(core.dateTimeNowMockable)); };
            if (sqlList["modifieddate"] == null) { sqlList.Add("modifieddate", encodeSQLDate(core.dateTimeNowMockable)); };
            if (sqlList["createdby"] == null) { sqlList.Add("createdby", encodeSQLNumber(userId)); };
            if (sqlList["modifiedby"] == null) { sqlList.Add("modifiedby", encodeSQLNumber(userId)); };
            if (sqlList["contentcontrolid"] == null) { sqlList.Add("contentcontrolid", encodeSQLNumber(0)); };
            if (sqlList["name"] == null) { sqlList.Add("name", encodeSQLText("")); };
            if (sqlList["active"] == null) { sqlList.Add("active", encodeSQLBoolean(true)); };
            return sqlList;
        }
        //
        //========================================================================
        /// <summary>
        /// Insert a record in a table, select it and return a datatable. You must dispose the datatable.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="createdByUserId"></param>
        /// <returns></returns>
        public DataTable insert(string tableName, int createdByUserId) {
            try {
                return insert(tableName, new NameValueCollection(), createdByUserId);
            } catch (Exception ex) {
                LogController.logError(core, new GenericException("Exception [" + ex.Message + "] inserting table [" + tableName + "], dataSourceName [" + dataSourceName + "]", ex));
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// Insert a record in a table. There is no check for core fields
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="sqlList"></param>
        public DataTable insert(string tableName, NameValueCollection sqlList, int createdByUserId) {
            try {
                NameValueCollection sqlListWorking = verifyBaseSqlNameValueFields(sqlList, createdByUserId);
                if (sqlListWorking.Count == 0) {
                    throw new ArgumentException("Empty field list is not allowed for Db insert.");
                }
                if (string.IsNullOrEmpty(tableName)) {
                    throw new ArgumentException("Blank table name is not allowed for Db insert.");
                }
                string nameList = sqlListWorking.getNameList();
                if (nameList.Contains(",,")) {
                    throw new ArgumentException("Blank field names are not allowed for Db insert.");
                }
                string valueList = sqlListWorking.getValueList();
                if (nameList.Contains(",,")) {
                    throw new ArgumentException("Blank values are not allowed for Db insert.");
                }
                string sql = "insert into " + tableName + "(" + nameList + ") output inserted.* values(" + valueList + ")";
                return core.db.executeQuery(sql);
            } catch (Exception ex) {
                LogController.logError(core, new GenericException("Exception [" + ex.Message + "], inserting table [" + tableName + "], dataSourceName [" + dataSourceName + "]", ex));
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// Opens the table specified and returns the data in a datatable. Returns all the active records in the table. Find the content record first, just for the dataSource.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="criteria"></param>
        /// <param name="sortFieldList"></param>
        /// <param name="selectFieldList"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageNumber"></param>
        /// <returns></returns>
        public DataTable openTable(string tableName, string criteria, string sortFieldList, string selectFieldList, int pageSize, int pageNumber) {
            try {
                string sql = "select";
                if (string.IsNullOrEmpty(selectFieldList)) {
                    sql += " *";
                } else {
                    sql += " " + selectFieldList;
                }
                sql += " from " + tableName;
                if (!string.IsNullOrEmpty(criteria)) {
                    sql += " where (" + criteria + ")";
                }
                if (!string.IsNullOrEmpty(sortFieldList)) {
                    sql += " order by " + sortFieldList;
                }
                return executeQuery(sql, getStartRecord(pageSize, pageNumber), pageSize);
            } catch (Exception ex) {
                LogController.logError(core, new GenericException("Exception [" + ex.Message + "], opening table [" + tableName + "], dataSourceName [" + dataSourceName + "]", ex));
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// Returns true if the field exists in the table
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public bool isSQLTableField(string tableName, string fieldName) {
            try {
                TableSchemaModel tableSchema = TableSchemaModel.getTableSchema(core, tableName, dataSourceName);
                if (tableSchema == null) { return false; }
                return null != tableSchema.columns.Find(x => x.COLUMN_NAME.ToLowerInvariant() == fieldName.ToLowerInvariant());
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// Returns true if the table exists
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public bool isSQLTable(string tableName) {
            try {
                return null != TableSchemaModel.getTableSchema(core, tableName, dataSourceName);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //   
        //========================================================================
        /// <summary>
        /// Check for a table in a datasource
        /// if the table is missing, create the table and the core fields
        /// if NoAutoIncrement is false or missing, the ID field is created as an auto incremenet
        /// if NoAutoIncrement is true, ID is created an an long
        /// if the table is present, check all core fields
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="allowAutoIncrement"></param>
        public void createSQLTable(string tableName) {
            try {
                if (string.IsNullOrEmpty(tableName)) {
                    //
                    // -- tablename required
                    throw new ArgumentException("Tablename can not be blank.");
                }
                if (GenericController.strInstr(1, tableName, ".") != 0) {
                    //
                    // -- Remote table -- remote system controls remote tables
                    throw new ArgumentException("Tablename can not contain a period(.)");
                }
                //
                // -- Local table -- create if not in schema
                bool isNewTable = TableSchemaModel.getTableSchema(core, tableName, dataSourceName) == null;
                if (isNewTable) {
                    //
                    LogController.logInfo(core, "creating sql table [" + tableName + "], datasource [" + dataSourceName + "]");
                    //
                    executeNonQuery("Create Table " + tableName + "(ID " + getSQLAlterColumnType(CPContentBaseClass.FieldTypeIdEnum.AutoIdIncrement) + ");");
                }
                //
                // ----- Test the common fields required in all tables
                createSQLTableField(tableName, "id", CPContentBaseClass.FieldTypeIdEnum.AutoIdIncrement);
                createSQLTableField(tableName, "name", CPContentBaseClass.FieldTypeIdEnum.Text);
                createSQLTableField(tableName, "dateAdded", CPContentBaseClass.FieldTypeIdEnum.Date);
                createSQLTableField(tableName, "createdby", CPContentBaseClass.FieldTypeIdEnum.Integer);
                createSQLTableField(tableName, "modifiedBy", CPContentBaseClass.FieldTypeIdEnum.Integer);
                createSQLTableField(tableName, "modifiedDate", CPContentBaseClass.FieldTypeIdEnum.Date);
                createSQLTableField(tableName, "active", CPContentBaseClass.FieldTypeIdEnum.Boolean);
                createSQLTableField(tableName, "sortOrder", CPContentBaseClass.FieldTypeIdEnum.Text);
                createSQLTableField(tableName, "contentControlId", CPContentBaseClass.FieldTypeIdEnum.Integer);
                createSQLTableField(tableName, "ccGuid", CPContentBaseClass.FieldTypeIdEnum.Text);
                createSQLTableField(tableName, "createKey", CPContentBaseClass.FieldTypeIdEnum.Integer);
                //
                // ----- setup core indexes
                if (isNewTable) {
                    createSQLIndex(tableName, tableName + "Active", "active");
                    createSQLIndex(tableName, tableName + "Name", "name");
                    createSQLIndex(tableName, tableName + "SortOrder", "sortOrder");
                    createSQLIndex(tableName, tableName + "DateAdded", "dateAdded");
                    createSQLIndex(tableName, tableName + "ContentControlId", "contentControlId");
                    createSQLIndex(tableName, tableName + "ModifiedDate", "modifiedDate");
                    createSQLIndex(tableName, tableName + "CcGuid", "ccGuid");
                }
                //
                TableSchemaModel.tableSchemaListClear(core);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// Check for a field in a table in the database, if missing, create the field
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="fieldName"></param>
        /// <param name="fieldType"></param>
        /// <param name="clearMetadataCache">If true, the metadata cache is cleared on success.</param>
        public void createSQLTableField(string tableName, string fieldName, CPContentBaseClass.FieldTypeIdEnum fieldType, bool clearMetadataCache = false) {
            try {
                if ((fieldType == CPContentBaseClass.FieldTypeIdEnum.Redirect) || (fieldType == CPContentBaseClass.FieldTypeIdEnum.ManyToMany)) { return; }
                if (string.IsNullOrEmpty(tableName)) { throw new ArgumentException("Table Name cannot be blank."); }
                if (fieldType == 0) { throw new ArgumentException("invalid fieldtype [" + fieldType + "]"); }
                if (GenericController.strInstr(1, tableName, ".") != 0) { throw new ArgumentException("Table name cannot include a period(.)"); }
                if (string.IsNullOrEmpty(fieldName)) { throw new ArgumentException("Field name cannot be blank"); }
                if (!isSQLTableField(tableName, fieldName)) {
                    //
                    LogController.logInfo(core, "creating sql table field [" + fieldName + "],table [" + tableName + "], datasource [" + dataSourceName + "]");
                    //
                    executeNonQuery("ALTER TABLE " + tableName + " ADD " + fieldName + " " + getSQLAlterColumnType(fieldType));
                    TableSchemaModel.tableSchemaListClear(core);
                    //
                    if (clearMetadataCache) {
                        core.cache.invalidateAll();
                        core.cacheRuntime.clear();
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
        /// Delete (drop) a table
        /// </summary>
        /// <param name="tableName"></param>
        public void deleteTable(string tableName) {
            try {
                executeNonQuery("DROP TABLE " + tableName);
                core.cache.invalidateAll();
                core.cacheRuntime.clear();
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// Delete a table field from a table
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="fieldName"></param>
        /// <param name="deleteIndexesContainingField"></param>
        public void deleteTableField(string tableName, string fieldName, bool deleteIndexesContainingField) {
            try {
                if (!isSQLTableField(tableName, fieldName)) { return; }
                //
                // -- this is a valid field in this table/datasource
                if (!deleteIndexesContainingField) { return; }
                //
                // -- delete the indexes containing this field
                var tableSchema = Models.Domain.TableSchemaModel.getTableSchema(core, tableName, dataSourceName);
                if (tableSchema == null) { return; }
                //
                foreach (Models.Domain.TableSchemaModel.ColumnSchemaModel column in tableSchema.columns) {
                    if ((column.COLUMN_NAME.ToLowerInvariant() == fieldName.ToLowerInvariant())) {
                        //
                        LogController.logInfo(core, "deleteTableField, dropping conversion required, field [" + column.COLUMN_NAME + "], table [" + tableName + "]");
                        //
                        // these can be very long queries for big tables 
                        int sqlTimeout = core.cpParent.Db.SQLTimeout;
                        core.cpParent.Db.SQLTimeout = 1800;
                        //
                        // drop any indexes that use this field
                        foreach (Models.Domain.TableSchemaModel.IndexSchemaModel index in tableSchema.indexes) {
                            if (index.indexKeyList.Contains(column.COLUMN_NAME)) {
                                //
                                LogController.logInfo(core, "deleteTableField, dropping index [" + index.index_name + "], because it contains this field");
                                //
                                try {
                                    core.db.deleteIndex(tableName, index.index_name);
                                } catch (Exception ex) {
                                    LogController.logWarn(core, "deleteTableField, error dropping index, [" + ex + "]");
                                }
                            }
                        }
                        //
                        LogController.logInfo(core, "deleteTableField, dropping field");
                        //
                        try {
                            executeNonQuery("ALTER TABLE " + tableName + " DROP COLUMN " + fieldName + ";");
                        } catch (Exception exDrop) {
                            LogController.logWarn(core, exDrop, "deleteTableField, error dropping field");
                        }
                        //
                        core.cpParent.Db.SQLTimeout = sqlTimeout;
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
        /// Create an index on a table, Fieldnames is  a comma delimited list of fields
        /// </summary>
        /// <param name="TableName"></param>
        /// <param name="IndexName"></param>
        /// <param name="FieldNames"></param>
        /// <param name="clearMetaCache"></param>
        public void createSQLIndex(string TableName, string IndexName, string FieldNames, bool clearMetaCache = false) {
            try {
                //
                // -- argument check
                if (string.IsNullOrEmpty(TableName) && string.IsNullOrEmpty(IndexName) && string.IsNullOrEmpty(FieldNames)) { return; }
                //
                // -- select current tableschema
                TableSchemaModel ts = TableSchemaModel.getTableSchema(core, TableName, dataSourceName);
                if (ts == null) { return; }
                if (null != ts.indexes.Find(x => x.index_name.ToLowerInvariant() == IndexName.ToLowerInvariant())) { return; }
                //
                // -- index not found, create index
                executeNonQuery("CREATE INDEX [" + IndexName + "] ON [" + TableName + "]( " + FieldNames + " );");
                //
                // -- clear cache
                if (!clearMetaCache) { return; }
                core.cache.invalidateAll();
                core.cacheRuntime.clear();
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// Create a trigger that deletes a man-to-many rule if a foreign-key is not valid. Should be called on both foreign keys of 
        /// </summary>
        /// <param name="ruleTableName"></param>
        /// <param name="ruleField"></param>
        /// <param name="joinTable"></param>
        /// <param name="joinField"></param>
        public void createTriggerManyManyRule(string ruleTableName, string ruleField, string joinTable, string joinField) {
            string sql = Properties.Resources.sqlTriggerManyManyRule;
            sql = sql.Replace("ruleTableName", ruleTableName);
            sql = sql.Replace("ruleField", ruleField);
            sql = sql.Replace("joinTable", joinTable);
            sql = sql.Replace("joinField", joinField);
            executeNonQuery(sql);
        }
        //
        //========================================================================
        /// <summary>
        /// Get FieldDescritor from FieldType
        /// </summary>
        /// <param name="fieldType"></param>
        /// <returns></returns>
        public string getSQLAlterColumnType(CPContentBaseClass.FieldTypeIdEnum fieldType) {
            try {
                switch (fieldType) {
                    case CPContentBaseClass.FieldTypeIdEnum.Boolean:
                    case CPContentBaseClass.FieldTypeIdEnum.Integer:
                    case CPContentBaseClass.FieldTypeIdEnum.Lookup:
                    case CPContentBaseClass.FieldTypeIdEnum.MemberSelect: {
                            return "int null";
                        }
                    case CPContentBaseClass.FieldTypeIdEnum.Currency: {
                            return "float null";
                        }
                    case CPContentBaseClass.FieldTypeIdEnum.Date: {
                            return "datetime2(7) null";
                        }
                    case CPContentBaseClass.FieldTypeIdEnum.Float: {
                            return "float null";
                        }
                    case CPContentBaseClass.FieldTypeIdEnum.FileImage:
                    case CPContentBaseClass.FieldTypeIdEnum.Link:
                    case CPContentBaseClass.FieldTypeIdEnum.ResourceLink:
                    case CPContentBaseClass.FieldTypeIdEnum.Text:
                    case CPContentBaseClass.FieldTypeIdEnum.File:
                    case CPContentBaseClass.FieldTypeIdEnum.FileText:
                    case CPContentBaseClass.FieldTypeIdEnum.FileJavascript:
                    case CPContentBaseClass.FieldTypeIdEnum.FileXML:
                    case CPContentBaseClass.FieldTypeIdEnum.FileCSS:
                    case CPContentBaseClass.FieldTypeIdEnum.FileHTML:
                    case CPContentBaseClass.FieldTypeIdEnum.FileHTMLCode: {
                            return "nvarchar(255) NULL";
                        }
                    case CPContentBaseClass.FieldTypeIdEnum.LongText:
                    case CPContentBaseClass.FieldTypeIdEnum.HTML:
                    case CPContentBaseClass.FieldTypeIdEnum.HTMLCode: {
                            //
                            // ----- Longtext, depends on datasource
                            return "nvarchar(max) Null";
                        }
                    case CPContentBaseClass.FieldTypeIdEnum.AutoIdIncrement: {
                            //
                            // ----- autoincrement type, depends on datasource
                            //
                            return "int identity primary key";
                        }
                    case CPContentBaseClass.FieldTypeIdEnum.ManyToMany:
                    case CPContentBaseClass.FieldTypeIdEnum.Redirect: {
                            //
                            // -- types with no db data, 200402 changed from varchar(255)
                            return "int null";
                        }
                    default: {
                            //
                            // Invalid field type
                            //
                            throw new GenericException("Can not proceed because the field being created has an invalid FieldType [" + fieldType + "]");
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
        /// Delete an Index for a table
        /// </summary>
        /// <param name="TableName"></param>
        /// <param name="IndexName"></param>
        public void deleteIndex(string TableName, string IndexName) {
            try {
                TableSchemaModel ts = TableSchemaModel.getTableSchema(core, TableName, dataSourceName);
                if (ts == null) { return; }
                if (null == ts.indexes.Find(x => x.index_name.ToLowerInvariant() == IndexName.ToLowerInvariant())) { return; }
                switch (getDataSourceType()) {
                    case Constants.DataSourceTypeODBCAccess: {
                            executeNonQuery("DROP INDEX " + IndexName + " On " + TableName + ";");
                            break;
                        }
                    default: {
                            executeNonQuery("DROP INDEX [" + TableName + "].[" + IndexName + "];");
                            break;
                        }
                }
                core.cache.invalidateAll();
                core.cacheRuntime.clear();
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// Get a DataSource type (SQL Server, etc) from its Name
        /// </summary>
        /// <returns></returns>
        //
        public int getDataSourceType() {
            return DataSourceTypeODBCSQLServer;
        }
        //
        //========================================================================
        /// <summary>
        /// Get FieldType from ADO Field Type
        /// </summary>
        /// <param name="ADOFieldType"></param>
        /// <returns></returns>
        public CPContentBaseClass.FieldTypeIdEnum getFieldTypeIdByADOType(int ADOFieldType) {
            try {
                switch (ADOFieldType) {
                    case 2:
                    case 4:
                    case 5: {
                            return CPContentBaseClass.FieldTypeIdEnum.Float;
                        }
                    case 3:
                    case 6: {
                            return CPContentBaseClass.FieldTypeIdEnum.Integer;
                        }
                    case 11: {
                            return CPContentBaseClass.FieldTypeIdEnum.Boolean;
                        }
                    case 135: {
                            return CPContentBaseClass.FieldTypeIdEnum.Date;
                        }
                    case 201: {
                            return CPContentBaseClass.FieldTypeIdEnum.LongText;
                        }
                    default: {
                            return CPContentBaseClass.FieldTypeIdEnum.Text;
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
        /// Get FieldType from FieldTypeName
        /// </summary>
        /// <param name="FieldTypeName"></param>
        /// <returns></returns>
        public CPContentBaseClass.FieldTypeIdEnum getFieldTypeIdFromFieldTypeName(string FieldTypeName) {
            try {
                switch (GenericController.toLCase(FieldTypeName)) {
                    case Constants.FieldTypeNameLcaseBoolean: {
                            return CPContentBaseClass.FieldTypeIdEnum.Boolean;
                        }
                    case Constants.FieldTypeNameLcaseCurrency: {
                            return CPContentBaseClass.FieldTypeIdEnum.Currency;
                        }
                    case Constants.FieldTypeNameLcaseDate: {
                            return CPContentBaseClass.FieldTypeIdEnum.Date;
                        }
                    case Constants.FieldTypeNameLcaseFile: {
                            return CPContentBaseClass.FieldTypeIdEnum.File;
                        }
                    case Constants.FieldTypeNameLcaseFloat: {
                            return CPContentBaseClass.FieldTypeIdEnum.Float;
                        }
                    case Constants.FieldTypeNameLcaseImage: {
                            return CPContentBaseClass.FieldTypeIdEnum.FileImage;
                        }
                    case Constants.FieldTypeNameLcaseLink: {
                            return CPContentBaseClass.FieldTypeIdEnum.Link;
                        }
                    case Constants.FieldTypeNameLcaseResourceLink:
                    case "resource link": {
                            return CPContentBaseClass.FieldTypeIdEnum.ResourceLink;
                        }
                    case Constants.FieldTypeNameLcaseInteger: {
                            return CPContentBaseClass.FieldTypeIdEnum.Integer;
                        }
                    case Constants.FieldTypeNameLcaseLongText:
                    case "Long text": {
                            return CPContentBaseClass.FieldTypeIdEnum.LongText;
                        }
                    case Constants.FieldTypeNameLcaseLookup:
                    case "lookuplist":
                    case "lookup list": {
                            return CPContentBaseClass.FieldTypeIdEnum.Lookup;
                        }
                    case Constants.FieldTypeNameLcaseMemberSelect: {
                            return CPContentBaseClass.FieldTypeIdEnum.MemberSelect;
                        }
                    case Constants.FieldTypeNameLcaseRedirect: {
                            return CPContentBaseClass.FieldTypeIdEnum.Redirect;
                        }
                    case Constants.FieldTypeNameLcaseManyToMany: {
                            return CPContentBaseClass.FieldTypeIdEnum.ManyToMany;
                        }
                    case Constants.FieldTypeNameLcaseTextFile:
                    case "text file": {
                            return CPContentBaseClass.FieldTypeIdEnum.FileText;
                        }
                    case Constants.FieldTypeNameLcaseCSSFile:
                    case "css file": {
                            return CPContentBaseClass.FieldTypeIdEnum.FileCSS;
                        }
                    case Constants.FieldTypeNameLcaseXMLFile:
                    case "xml file": {
                            return CPContentBaseClass.FieldTypeIdEnum.FileXML;
                        }
                    case Constants.FieldTypeNameLcaseJavascriptFile:
                    case "javascript file":
                    case "js file":
                    case "jsfile": {
                            return CPContentBaseClass.FieldTypeIdEnum.FileJavascript;
                        }
                    case Constants.FieldTypeNameLcaseText: {
                            return CPContentBaseClass.FieldTypeIdEnum.Text;
                        }
                    case "autoincrement": {
                            return CPContentBaseClass.FieldTypeIdEnum.AutoIdIncrement;
                        }
                    case Constants.FieldTypeNameLcaseHTML: {
                            return CPContentBaseClass.FieldTypeIdEnum.HTML;
                        }
                    case Constants.FieldTypeNameLcaseHTMLCode: {
                            return CPContentBaseClass.FieldTypeIdEnum.HTMLCode;
                        }
                    case Constants.FieldTypeNameLcaseHTMLFile:
                    case "html file": {
                            return CPContentBaseClass.FieldTypeIdEnum.FileHTML;
                        }
                    case Constants.FieldTypeNameLcaseHTMLCodeFile:
                    case "html file code": {
                            return CPContentBaseClass.FieldTypeIdEnum.FileHTMLCode;
                        }
                    default: {
                            //
                            // Bad field type is a text field
                            //
                            return CPContentBaseClass.FieldTypeIdEnum.Text;
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
        /// get a field value from a dataTable row 
        /// </summary>
        /// <param name="dr"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static string getDataRowFieldText(DataRow dr, string fieldName) {
            if (string.IsNullOrWhiteSpace(fieldName)) { throw new ArgumentException("field name cannot be blank"); }
            return dr[fieldName].ToString();
        }
        //
        //========================================================================
        /// <summary>
        /// get a field value from a dataTable row, interpreted as an integer
        /// </summary>
        /// <param name="dr"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static int getDataRowFieldInteger(DataRow dr, string fieldName) => encodeInteger(getDataRowFieldText(dr, fieldName));
        //
        //========================================================================
        /// <summary>
        /// get a field value from a dataTable row, interpreted as a double
        /// </summary>
        /// <param name="dr"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static double getDataRowFieldNumber(DataRow dr, string fieldName) => encodeNumber(getDataRowFieldText(dr, fieldName));
        //
        //========================================================================
        /// <summary>
        /// get a field value from a dataTable row, interpreted as a boolean 
        /// </summary>
        /// <param name="dr"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static bool getDataRowFieldBoolean(DataRow dr, string fieldName) => encodeBoolean(getDataRowFieldText(dr, fieldName));
        //
        //========================================================================
        /// <summary>
        /// get a field value from a dataTable row, interpreted as a date
        /// </summary>
        /// <param name="dr"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static DateTime getDataRowFieldDate(DataRow dr, string fieldName) => encodeDate(getDataRowFieldText(dr, fieldName));
        //
        // ====================================================================================================
        /// <summary>
        /// filter an sqlcompatible string. 
        /// Empty strings are converted to null (string fields must be nullable)
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static string encodeSQLText(string expression) {
            if (expression == null) { return "null"; }
            string returnResult = GenericController.encodeText(expression);
            if (string.IsNullOrEmpty(returnResult)) { return "null"; }
            return "'" + GenericController.strReplace(returnResult, "'", "''") + "'";
        }
        //
        // ====================================================================================================
        /// <summary>
        /// encode a string for a like operation
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static string encodeSqlTextLike(string expression) {
            if (expression == null) { return ""; }
            string working = expression.replace("[", "[[]", StringComparison.InvariantCultureIgnoreCase);
            working = working.replace("%", "[%]", StringComparison.InvariantCultureIgnoreCase);
            working = working.replace("_", "[_]", StringComparison.InvariantCultureIgnoreCase);
            return encodeSQLText("%" + working + "%");
        }
        //
        // ====================================================================================================
        /// <summary>
        ///    encodeSQLDate
        /// </summary>
        /// <param name="expressionDate"></param>
        /// <returns></returns>
        //
        public static string encodeSQLDate(DateTime expressionDate) {
            if (Convert.IsDBNull(expressionDate)) { return "null"; }
            if (expressionDate == DateTime.MinValue) { return "null"; }
            return "'" + expressionDate.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'";
        }
        //
        //========================================================================
        /// <summary>
        /// encode a number in a sql statement
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        //
        public static string encodeSQLNumber(double? expression) {
            if (expression == null) { return "null"; }
            return expression.ToString();
        }
        //
        //========================================================================
        /// <summary>
        /// encode integer in a sql statement
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static string encodeSQLNumber(int? expression) {
            if (expression == null) { return "null"; }
            return expression.ToString();
        }
        //
        //========================================================================
        /// <summary>
        /// encode a boolean in a sql statement
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        //
        public static string encodeSQLBoolean(bool expression) {
            if (expression) { return SQLTrue; }
            return SQLFalse;
        }
        //
        //========================================================================
        /// <summary>
        /// delete a record
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="recordId"></param>
        //
        public void delete(int recordId, string tableName) {
            try {
                if (string.IsNullOrEmpty(tableName.Trim())) { throw new GenericException("tablename cannot be blank"); }
                if (recordId <= 0) { throw new GenericException("record id is not valid [" + recordId + "]"); }
                executeNonQuery("delete from " + tableName + " where id=" + recordId);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        /// <summary>
        /// delete a record based on a guid
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="tableName"></param>
        public void delete(string tableName, string guid) {
            try {
                if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(guid)) {
                    throw new GenericException("tablename and guid cannot be blank");
                }
                if (isGuid(tableName) && !isGuid(guid)) {
                    //
                    // legacy api had arguments reversed
                    string tmp = guid;
                    guid = tableName;
                    tableName = tmp;
                }
                // -- allow for non-guid formated guid values (can just be unique)
                executeNonQuery("delete from " + tableName + " where ccguid=" + encodeSQLText(guid));
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// Delete all table rows that match the sql criteria
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="criteria"></param>
        public void deleteRows(string tableName, string criteria) {
            try {
                if (string.IsNullOrEmpty(tableName)) { throw new ArgumentException("TableName cannot be blank"); }
                if (string.IsNullOrEmpty(criteria)) { throw new ArgumentException("Criteria cannot be blank"); }
                executeNonQuery("delete from " + tableName + " where " + criteria);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// Return an sql select based on the arguments
        /// </summary>
        /// <param name="from"></param>
        /// <param name="fieldList"></param>
        /// <param name="where"></param>
        /// <param name="orderBy"></param>
        /// <param name="groupBy"></param>
        /// <param name="recordLimit"></param>
        /// <returns></returns>
        public string getSQLSelect(string from, string fieldList, string where, string orderBy, string groupBy, int recordLimit) {
            string sql = "select";
            if (recordLimit != 0) { sql += " top " + recordLimit; }
            sql += (string.IsNullOrWhiteSpace(fieldList)) ? " *" : " " + fieldList;
            sql += " from " + from;
            if (!string.IsNullOrWhiteSpace(where)) { sql += " where " + where; }
            if (!string.IsNullOrWhiteSpace(orderBy)) { sql += " order by " + orderBy; }
            if (!string.IsNullOrWhiteSpace(groupBy)) { sql += " group by " + groupBy; }
            return sql;
        }
        /// <summary>
        /// Return an sql select based on the arguments
        /// </summary>
        /// <param name="from"></param>
        /// <param name="fieldList"></param>
        /// <param name="where"></param>
        /// <param name="orderBy"></param>
        /// <param name="groupBy"></param>
        /// <returns></returns>
        public string getSQLSelect(string from, string fieldList, string where, string orderBy, string groupBy) => getSQLSelect(from, fieldList, where, orderBy, groupBy, 0);
        /// <summary>
        /// Return an sql select based on the arguments
        /// </summary>
        /// <param name="from"></param>
        /// <param name="fieldList"></param>
        /// <param name="where"></param>
        /// <param name="orderBy"></param>
        /// <returns></returns>
        public string getSQLSelect(string from, string fieldList, string where, string orderBy) => getSQLSelect(from, fieldList, where, orderBy, "", 0);
        /// <summary>
        /// Return an sql select based on the arguments
        /// </summary>
        /// <param name="from"></param>
        /// <param name="fieldList"></param>
        /// <param name="where"></param>
        /// <returns></returns>
        public string getSQLSelect(string from, string fieldList, string where) => getSQLSelect(from, fieldList, where, "", "", 0);
        /// <summary>
        /// Return an sql select based on the arguments
        /// </summary>
        /// <param name="from"></param>
        /// <param name="fieldList"></param>
        /// <returns></returns>
        public string getSQLSelect(string from, string fieldList) => getSQLSelect(from, fieldList, "", "", "", 0);
        /// <summary>
        /// Return an sql select based on the arguments
        /// </summary>
        /// <param name="from"></param>
        /// <returns></returns>
        public string getSQLSelect(string from) => getSQLSelect(from, "", "", "", "", 0);
        //
        //========================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        //
        public DataTable getTableSchemaData(string tableName) {
            try {
                string connString = getConnectionStringADONET(core.appConfig.name);
                using SqlConnection connSQL = new(connString);
                connSQL.Open();
                return connSQL.GetSchema("Tables", new[] { core.appConfig.name, null, tableName, null });
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// Get a recordset with the table schema
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        //
        public DataTable getColumnSchemaData(string tableName) {
            try {
                if (string.IsNullOrEmpty(tableName.Trim())) { throw new ArgumentException("tablename cannot be blank"); }
                using SqlConnection connSQL = new(getConnectionStringADONET(core.appConfig.name));
                connSQL.Open();
                return connSQL.GetSchema("Columns", new[] { core.appConfig.name, null, tableName, null });
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// Get a recordset with the table schema
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public DataTable getIndexSchemaData(string tableName) {
            try {
                if (string.IsNullOrWhiteSpace(tableName.Trim())) { throw new ArgumentException("tablename cannot be blank"); }
                return executeQuery("sys.sp_helpindex @objname = N'" + tableName + "'");
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //=============================================================================
        /// <summary>
        /// get Sql Criteria for string that could be id, guid or name
        /// </summary>
        /// <param name="nameIdOrGuid"></param>
        /// <returns></returns>
        public string getNameIdOrGuidSqlCriteria(string nameIdOrGuid) {
            try {
                if (nameIdOrGuid.isNumeric()) { return "id=" + encodeSQLNumber(double.Parse(nameIdOrGuid)); }
                if (isGuid(nameIdOrGuid)) { return "ccGuid=" + encodeSQLText(nameIdOrGuid); }
                return "name=" + encodeSQLText(nameIdOrGuid);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// convert a dtaTable to a simple array - quick way to adapt old code
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        ///
        public string[,] convertDataTabletoArray(DataTable dt) {
            try {
                //
                // 20150717 check for no columns
                string[,] rows = { { } };
                if ((dt.Rows.Count > 0) && (dt.Columns.Count > 0)) {
                    int columnCnt = dt.Columns.Count;
                    int rowCnt = dt.Rows.Count;
                    // 20150717 change from rows(columnCnt,rowCnt) because other routines appear to use this count
                    rows = new string[columnCnt, rowCnt];
                    int rPtr = 0;
                    foreach (DataRow dr in dt.Rows) {
                        int cPtr = 0;
                        foreach (DataColumn cell in dt.Columns) {
                            rows[cPtr, rPtr] = GenericController.encodeText(dr[cell]);
                            cPtr += 1;
                        }
                        rPtr += 1;
                    }
                }
                return rows;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// DeleteTableRecordChunks
        /// </summary>
        /// <param name="TableName"></param>
        /// <param name="Criteria"></param>
        /// <param name="ChunkSize"></param>
        /// <param name="MaxChunkCount"></param>
        public void deleteTableRecordChunks(string TableName, string Criteria, int ChunkSize = 1000, int MaxChunkCount = 1000) {
            int DataSourceType = getDataSourceType();
            if ((DataSourceType != DataSourceTypeODBCSQLServer) && (DataSourceType != DataSourceTypeODBCAccess)) {
                //
                // If not SQL server, just delete them
                //
                deleteRows(TableName, Criteria);
                return;
            }
            //
            // ----- Clear up to date for the properties
            //
            int iChunkSize = ChunkSize;
            if (iChunkSize == 0) {
                iChunkSize = 1000;
            }
            int iChunkCount = MaxChunkCount;
            if (iChunkCount == 0) {
                iChunkCount = 1000;
            }
            //
            // Get an initial count and allow for timeout
            //
            int PreviousCount = -1;
            int LoopCount = 0;
            int CurrentCount = 0;
            string SQL = "select count(*) as RecordCount from " + TableName + " where " + Criteria;
            DataTable dt = executeQuery(SQL);
            if (dt.Rows.Count > 0) {
                CurrentCount = GenericController.encodeInteger(dt.Rows[0][0]);
            }
            while ((CurrentCount != 0) && (PreviousCount != CurrentCount) && (LoopCount < iChunkCount)) {
                if (getDataSourceType() == DataSourceTypeODBCMySQL) {
                    SQL = "delete from " + TableName + " where id in (select ID from " + TableName + " where " + Criteria + " limit " + iChunkSize + ")";
                } else {
                    SQL = "delete from " + TableName + " where id in (select top " + iChunkSize + " ID from " + TableName + " where " + Criteria + ")";
                }
                executeNonQuery(SQL);
                PreviousCount = CurrentCount;
                SQL = "select count(*) as RecordCount from " + TableName + " where " + Criteria;
                dt = executeQuery(SQL);
                if (dt.Rows.Count > 0) {
                    CurrentCount = GenericController.encodeInteger(dt.Rows[0][0]);
                }
                LoopCount = LoopCount + 1;
            }
            if ((CurrentCount != 0) && (PreviousCount == CurrentCount)) {
                //
                // records did not delete
                //
                LogController.logError(core, new GenericException("Error deleting record chunks. No records were deleted and the process was not complete."));
            } else if (LoopCount >= iChunkCount) {
                //
                // records did not delete
                //
                LogController.logError(core, new GenericException("Error deleting record chunks. The maximum chunk count was exceeded while deleting records."));
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// verify a name is a valid tablename
        /// </summary>
        /// <param name="sourceName"></param>
        /// <returns></returns>
        public static string encodeSqlTableName(string sourceName) {
            try {

                const string FirstCharSafeString = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
                const string SafeString = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_@#";
                //
                // remove nonsafe URL characters
                //
                string src = sourceName;
                string returnName = "";
                // first character
                int Ptr = 0;
                while (Ptr < src.Length) {
                    string TestChr = src.Substring(Ptr, 1);
                    Ptr += 1;
                    if (FirstCharSafeString.IndexOf(TestChr) >= 0) {
                        returnName += TestChr;
                        break;
                    }
                }
                // non-first character
                while (Ptr < src.Length) {
                    string TestChr = src.Substring(Ptr, 1);
                    Ptr += 1;
                    if (SafeString.IndexOf(TestChr) >= 0) {
                        returnName += TestChr;
                    }
                }
                return returnName;
            } catch (Exception ex) {
                throw new GenericException("Exception in encodeSqlTableName(" + sourceName + ")", ex);
            }
        }
        //
        //=================================================================================
        /// <summary>
        /// verify a datatable is valid
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static bool isDataTableOk(DataTable dt) {
            if (dt == null) { return false; }
            if (dt.Rows == null) { return false; }
            return (dt.Rows.Count > 0);
        }
        //
        //=================================================================================
        /// <summary>
        /// dispose datatable
        /// </summary>
        /// <param name="dt"></param>
        public static void closeDataTable(DataTable dt) {
            dt.Dispose();
        }
        //
        //====================================================================================================
        /// <summary>
        /// execute a query from the remoteQueryKey stored in the remoteQueryTable
        /// </summary>
        /// <param name="remoteQueryKey"></param>
        /// <returns></returns>
        public DataTable executeRemoteQuery(string remoteQueryKey) {
            DataTable result;
            try {
                var remoteQuery = DbBaseModel.create<RemoteQueryModel>(core.cpParent, remoteQueryKey);
                if (remoteQuery == null) {
                    throw new GenericException("remoteQuery was not found with key [" + remoteQueryKey + "]");
                } else {
                    result = executeQuery(remoteQuery.sqlQuery);
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                result = null;
            }
            return result;
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Return just the tablename from a tablename reference (database.object.tablename->tablename)
        /// </summary>
        /// <param name="DbObject"></param>
        /// <returns></returns>
        public static string getDbObjectTableName(string DbObject) {
            int Position = DbObject.LastIndexOf(".", StringComparison.InvariantCulture) + 1;
            if (Position > 0) {
                return DbObject.Substring(Position);
            }
            return "";
        }
        //
        //=============================================================================
        /// <summary>
        /// Get a ContentID from the ContentName using just the tables
        /// </summary>
        /// <param name="contentName"></param>
        /// <returns></returns>
        public static int getContentId(CoreController core, string contentName) {
            using (DataTable dt = core.db.executeQuery("select top 1 id from cccontent where name=" + encodeSQLText(contentName) + " order by id")) {
                if (dt.Rows.Count == 0) { return 0; }
                return getDataRowFieldInteger(dt.Rows[0], "id");
            }
        }
        //
        //=============================================================================
        /// <summary>
        /// Get a Content Field Id (id of the ccFields table record) using just the database
        /// </summary>
        /// <param name="contentId"></param>
        /// <param name="fieldName"></param>
        /// <param name="core"></param>
        /// <returns></returns>
        public static int getContentFieldId(CoreController core, int contentId, string fieldName) {
            if ((contentId <= 0) || (string.IsNullOrWhiteSpace(fieldName))) { return 0; }
            using (DataTable dt = core.db.executeQuery("select top 1 id from ccfields where (contentid=" + contentId + ")and(name=" + encodeSQLText(fieldName) + ") order by id")) {
                if (dt.Rows.Count == 0) { return 0; }
                return getDataRowFieldInteger(dt.Rows[0], "id");
            }
        }
        //
        //=============================================================================
        /// <summary>
        /// Get a Content Field Id (id of the ccFields table record) using just the database
        /// </summary>
        /// <param name="core"></param>
        /// <param name="contentName"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static int getContentFieldId(CoreController core, string contentName, string fieldName) {
            if ((string.IsNullOrWhiteSpace(contentName)) || (string.IsNullOrWhiteSpace(fieldName))) { return 0; }
            return getContentFieldId(core, getContentId(core, contentName), fieldName);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// get the id of the table in the cctable table
        /// </summary>
        /// <param name="core"></param>
        /// <param name="TableName"></param>
        /// <returns></returns>
        public static int getTableID(CoreController core, string TableName) {
            if ((string.IsNullOrWhiteSpace(TableName))) { return 0; }
            using DataTable dt = core.db.executeQuery("select top 1 id from cctables where name=" + encodeSQLText(TableName) + " order by id");
            if (dt.Rows.Count == 0) { return 0; }
            return getDataRowFieldInteger(dt.Rows[0], "id");
        }
        //
        // ====================================================================================================
        /// <summary>
        /// calculate startRecord (0-based) from pagesize and pagenumber (1 based). Required to convert older 1-based methods to current 0-based methods
        /// </summary>
        /// <param name="pageSize">records per page</param>
        /// <param name="pageNumber">1-based page number</param>
        /// <returns></returns>
        public static int getStartRecord(int pageSize, int pageNumber) {
            return pageSize * (pageNumber - 1);
        }
        /// <summary>
        /// model for simplest generic recorsd
        /// </summary>
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        //
        //
        #region  IDisposable Support 
        protected bool disposed;
        //
        //==========================================================================================
        /// <summary>
        /// dispose
        /// </summary>
        /// <param name="disposing"></param>
        /// <remarks></remarks>
        protected virtual void Dispose(bool disposing) {
            if (!this.disposed) {
                if (disposing) {
                    //
                    // ----- call .dispose for managed objects
                    //
                }
                //
                // Add code here to release the unmanaged resource.
                //
            }
            this.disposed = true;
        }
        // Do not change or add Overridable to these methods.
        // Put cleanup code in Dispose(ByVal disposing As Boolean).
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~DbController() {
            Dispose(false);
        }
        #endregion
    }
}
