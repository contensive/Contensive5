
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
                    // -- drop indexes containing this column
                    var droppedIndexes = new List<TableSchemaModel.IndexSchemaModel>();
                    foreach (TableSchemaModel.IndexSchemaModel index in tableSchema.indexes) {
                        if (index.indexKeyList.Contains(matchedColumn.COLUMN_NAME)) {
                            env.core.db.deleteIndex(tableName, index.index_name);
                            droppedIndexes.Add(index);
                        }
                    }
                    //
                    // -- alter the column
                    env.core.db.executeNonQuery($"ALTER TABLE {tableName} ALTER COLUMN {fieldName} nvarchar({textLength}) NULL");
                    //
                    // -- recreate dropped indexes
                    foreach (var index in droppedIndexes) {
                        env.core.db.createSQLIndex(tableName, index.index_name, index.index_keys);
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{env.core.logCommonMessage}");
                LogController.logAlarm(env.core, $"Housekeep, verifyTextFieldLengths exception, ex [{ex}]");
                throw;
            }
        }
    }
}
