
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using NLog;
using System;
using System.Collections.Generic;
using System.Data;

namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// Housekeep this content
    /// </summary>
    public static class ContentFieldClass {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        /// <summary>
        /// execute hourly tasks
        /// </summary>
        /// <param name="core"></param>
        public static void executeHourlyTasks(HouseKeepEnvironmentModel env) {
            try {
                //
                env.log("Housekeep, executeHourlyTasks, Content");
                //
            } catch (Exception ex) {
                logger.Error(ex, $"{env.core.logCommonMessage}");
                LogController.logAlarm(env.core, "Housekeep, exception, ex [" + ex + "]");
                throw;

            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// execute Daily Tasks
        /// </summary>
        /// <param name="core"></param>
        /// <param name="env"></param>
        public static void executeDailyTasks(HouseKeepEnvironmentModel env) {
            try {
                //
                env.log("HousekeepDaily, content fields");
                //
                env.log("Deleting content fields with no content.");
                string sql = "delete from ccfields from ccfields left join cccontent on cccontent.id=ccfields.contentId where cccontent.id is null";
                env.core.db.executeNonQuery(sql);
                //
                // -- verify text field lengths match textLength metadata
                verifyTextFieldLengths(env);
                //
                // -- verify longtext fields are nvarchar(max) in the database
                verifyLongTextFieldsAreMax(env);

            } catch (Exception ex) {
                logger.Error(ex, $"{env.core.logCommonMessage}");
                LogController.logAlarm(env.core, "Housekeep, exception, ex [" + ex + "]");
                throw;

            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// For text fields with textLength set, verify the database column length matches.
        /// If the db column is shorter than textLength, widen it. Handles index drop/recreate.
        /// </summary>
        private static void verifyTextFieldLengths(HouseKeepEnvironmentModel env) {
            try {
                env.log("HousekeepDaily, verifying text field lengths");
                //
                // -- query text-type fields that have a textLength set, joined to get the sql table name
                string sql = "select f.name as fieldName, f.textLength, t.name as tableName"
                    + " from ccfields f"
                    + " inner join cccontent c on c.id = f.contentId"
                    + " inner join cctables t on t.id = c.contentTableId"
                    + " where f.textLength > 0"
                    + " and f.type in (2,6,10,11,16,17,18,19,20)";
                using DataTable dt = env.core.db.executeQuery(sql);
                if (!DbController.isDataTableOk(dt)) { return; }
                //
                // -- cache table schemas to avoid repeated lookups
                var schemaCache = new Dictionary<string, TableSchemaModel>(StringComparer.InvariantCultureIgnoreCase);
                foreach (DataRow row in dt.Rows) {
                    string fieldName = GenericController.getText(row["fieldName"]);
                    int textLength = GenericController.getInteger(row["textLength"]);
                    string tableName = GenericController.getText(row["tableName"]);
                    if (string.IsNullOrEmpty(fieldName) || string.IsNullOrEmpty(tableName) || textLength <= 0) { continue; }
                    //
                    if (!schemaCache.ContainsKey(tableName)) {
                        var schema = TableSchemaModel.getTableSchema(env.core, tableName, "default");
                        if (schema == null) { continue; }
                        schemaCache[tableName] = schema;
                    }
                    var tableSchema = schemaCache[tableName];
                    //
                    // -- find the column in the schema
                    TableSchemaModel.ColumnSchemaModel matchedColumn = null;
                    foreach (var column in tableSchema.columns) {
                        if (column.COLUMN_NAME.Equals(fieldName, StringComparison.InvariantCultureIgnoreCase)) {
                            matchedColumn = column;
                            break;
                        }
                    }
                    if (matchedColumn == null) { continue; }
                    if (matchedColumn.CHARACTER_MAXIMUM_LENGTH >= textLength) { continue; }
                    //
                    // -- column is shorter than required, widen it
                    env.log($"HousekeepDaily, widening [{tableName}].[{fieldName}] from nvarchar({matchedColumn.CHARACTER_MAXIMUM_LENGTH}) to nvarchar({textLength})");
                    //
                    // -- drop all indexes referencing this column (key or INCLUDE columns)
                    // -- track key-column indexes so they can be recreated after the alter
                    var droppedKeyIndexes = new List<TableSchemaModel.IndexSchemaModel>();
                    foreach (TableSchemaModel.IndexSchemaModel index in tableSchema.indexes) {
                        if (index.indexKeyList.Contains(matchedColumn.COLUMN_NAME)) {
                            droppedKeyIndexes.Add(index);
                        }
                    }
                    env.core.db.dropIndexesReferencingColumn(tableName, fieldName);
                    //
                    // -- alter the column
                    env.core.db.executeNonQuery($"ALTER TABLE {tableName} ALTER COLUMN {fieldName} nvarchar({textLength}) NULL");
                    //
                    // -- recreate dropped key-column indexes (INCLUDE-only indexes are not
                    // -- tracked in sp_helpindex schema, so they cannot be recreated here)
                    foreach (var index in droppedKeyIndexes) {
                        env.core.db.createSQLIndex(tableName, index.index_name, index.index_keys);
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{env.core.logCommonMessage}");
                LogController.logAlarm(env.core, $"Housekeep, verifyTextFieldLengths exception, ex [{ex}]");
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// LongText (3), HTML (21), and HTMLCode (23) fields should be nvarchar(max) in SQL Server.
        /// If the database column is a shorter nvarchar, widen it to nvarchar(max).
        /// This handles cases where a field was created with a limited size before being changed to LongText.
        /// In SQL Server, CHARACTER_MAXIMUM_LENGTH = -1 means nvarchar(max).
        /// </summary>
        private static void verifyLongTextFieldsAreMax(HouseKeepEnvironmentModel env) {
            try {
                env.log("HousekeepDaily, verifying longtext fields are nvarchar(max)");
                //
                // -- query LongText, HTML, and HTMLCode fields joined to get the sql table name
                // -- type 3=LongText, 21=HTML, 23=HTMLCode
                string sql = "select f.name as fieldName, t.name as tableName"
                    + " from ccfields f"
                    + " inner join cccontent c on c.id = f.contentId"
                    + " inner join cctables t on t.id = c.contentTableId"
                    + " where f.type in (3,21,23)";
                using DataTable dt = env.core.db.executeQuery(sql);
                if (!DbController.isDataTableOk(dt)) { return; }
                //
                // -- cache table schemas to avoid repeated lookups
                var schemaCache = new Dictionary<string, TableSchemaModel>(StringComparer.InvariantCultureIgnoreCase);
                foreach (DataRow row in dt.Rows) {
                    string fieldName = GenericController.getText(row["fieldName"]);
                    string tableName = GenericController.getText(row["tableName"]);
                    if (string.IsNullOrEmpty(fieldName) || string.IsNullOrEmpty(tableName)) { continue; }
                    //
                    if (!schemaCache.ContainsKey(tableName)) {
                        var schema = TableSchemaModel.getTableSchema(env.core, tableName, "default");
                        if (schema == null) { continue; }
                        schemaCache[tableName] = schema;
                    }
                    var tableSchema = schemaCache[tableName];
                    //
                    // -- find the column in the schema
                    TableSchemaModel.ColumnSchemaModel matchedColumn = null;
                    foreach (var column in tableSchema.columns) {
                        if (column.COLUMN_NAME.Equals(fieldName, StringComparison.InvariantCultureIgnoreCase)) {
                            matchedColumn = column;
                            break;
                        }
                    }
                    if (matchedColumn == null) { continue; }
                    //
                    string dataType = matchedColumn.DATA_TYPE?.ToLowerInvariant() ?? "";
                    bool isNvarchar = dataType.Equals("nvarchar");
                    bool isVarchar = dataType.Equals("varchar");
                    bool isLegacyText = dataType.Equals("text");
                    bool isLegacyNtext = dataType.Equals("ntext");
                    //
                    // -- skip if metadata is out of sync with db (column is not a text type at all)
                    if (!isNvarchar && !isVarchar && !isLegacyText && !isLegacyNtext) { continue; }
                    //
                    // -- CHARACTER_MAXIMUM_LENGTH of -1 means max/unlimited, skip if already nvarchar(max)
                    if (isNvarchar && matchedColumn.CHARACTER_MAXIMUM_LENGTH == -1) { continue; }
                    //
                    // -- column needs to be widened to nvarchar(max)
                    env.log($"HousekeepDaily, widening [{tableName}].[{fieldName}] from {dataType}({matchedColumn.CHARACTER_MAXIMUM_LENGTH}) to nvarchar(max)");
                    //
                    // -- drop all indexes referencing this column (key or INCLUDE columns)
                    env.core.db.dropIndexesReferencingColumn(tableName, fieldName);
                    //
                    // -- legacy text/ntext cannot be altered directly to nvarchar(max),
                    // -- convert to varchar(max) first (SQL Server allows text->varchar(max))
                    if (isLegacyText || isLegacyNtext) {
                        env.core.db.executeNonQuery($"ALTER TABLE {tableName} ALTER COLUMN {fieldName} varchar(max) NULL");
                    }
                    //
                    // -- alter the column to nvarchar(max)
                    env.core.db.executeNonQuery($"ALTER TABLE {tableName} ALTER COLUMN {fieldName} nvarchar(max) NULL");
                    //
                    // -- do not recreate dropped indexes because the column is now
                    // -- nvarchar(max), which SQL Server does not allow as an index key
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{env.core.logCommonMessage}");
                LogController.logAlarm(env.core, $"Housekeep, verifyLongTextFieldsAreMax exception, ex [{ex}]");
                throw;
            }
        }
    }
}
