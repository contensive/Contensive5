using Contensive.Processor.Controllers;
using NLog;
using System;
using System.Data;
using System.Text;

namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// Activity Log
    /// </summary>
    public static class TablesClass {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        // -- max number of detail records to log per table to prevent log flooding
        private const int maxDetailRecords = 50;
        //
        //====================================================================================================
        /// <summary>
        /// execute hourly tasks
        /// </summary>
        /// <param name="core"></param>
        public static void executeHourlyTasks(HouseKeepEnvironmentModel env) {
            try {
                //
                env.log("Housekeep, executeHourlyTasks, TablesClass");
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
                env.log("Housekeep, TablesClass");
                {
                    //
                    DataTable dt = env.core.db.executeQuery("select name,id from cctables");
                    if (dt?.Rows is null || dt.Rows.Count == 0) { return; }
                    foreach (DataRow dr in dt.Rows) {
                        string tableName = GenericController.getText(dr[0]);
                        int tableId = GenericController.getInteger(dr[1]);
                        string sql = "";
                        try {
                            if (string.IsNullOrEmpty(tableName)) {
                                string msg = $"Housekeep, TablesClass, ERROR, cctables record with empty name, id [{tableId}]. Delete this cctables record and all ccFields with contentid [{tableId}].";
                                env.log(msg);
                                logger.Error($"{env.core.logCommonMessage}, {msg}");
                                continue;
                            }
                            //
                            // -- create and verify missing sql tables found in cctables
                            env.core.db.createSQLTable(tableName);
                            //
                            // -- test success
                            if (!env.core.db.isSQLTable(tableName)) {
                                string msg = $"Housekeep, TablesClass, ERROR, ccTable record [{tableName}] but Db catalog [{env.core.appConfig.name}] does not include this table. Create this table in the database and all ccfields records assocated to it.";
                                env.log(msg);
                                logger.Error($"{env.core.logCommonMessage}, {msg}");
                                continue;
                            }
                            //
                            // -- detect and log empty guid fields before fixing
                            int recordsAffected = 0;
                            sql = $"select id, DateAdded, ModifiedDate, CreatedBy, ModifiedBy from {tableName} where ((ccGuid is null)or(ccguid=''))";
                            using (DataTable dtBlank = env.core.db.executeQuery(sql)) {
                                if (DbController.isDataTableOk(dtBlank)) {
                                    recordsAffected = dtBlank.Rows.Count;
                                    string msg = $"Housekeep, TablesClass, ERROR, [{recordsAffected}] records with blank ccguid found and fixed in table[{tableName}]. Find and fix the process that creates records with blank ccguid";
                                    env.log(msg);
                                    logger.Error($"{env.core.logCommonMessage}, {msg}");
                                    //
                                    // -- log detail for each problematic record (capped)
                                    int detailCount = 0;
                                    foreach (DataRow drDetail in dtBlank.Rows) {
                                        if (detailCount >= maxDetailRecords) {
                                            env.log($"Housekeep, TablesClass, ...and [{recordsAffected - maxDetailRecords}] more records not shown");
                                            break;
                                        }
                                        int recordId = GenericController.getInteger(drDetail["id"]);
                                        DateTime dateAdded = GenericController.getDate(drDetail["DateAdded"]);
                                        DateTime modifiedDate = GenericController.getDate(drDetail["ModifiedDate"]);
                                        int createdBy = GenericController.getInteger(drDetail["CreatedBy"]);
                                        int modifiedBy = GenericController.getInteger(drDetail["ModifiedBy"]);
                                        string dateAddedText = (dateAdded == DateTime.MinValue) ? "(null)" : dateAdded.ToString();
                                        string modifiedDateText = (modifiedDate == DateTime.MinValue) ? "(null)" : modifiedDate.ToString();
                                        env.log($"Housekeep, TablesClass, blank ccguid detail, table[{tableName}], id[{recordId}], DateAdded[{dateAddedText}], ModifiedDate[{modifiedDateText}], CreatedBy[{createdBy}], ModifiedBy[{modifiedBy}]");
                                        detailCount++;
                                    }
                                    //
                                    // -- fix the blank ccguids
                                    sql = $"update {tableName} set ccguid=lower('{{'+CONVERT(nvarchar(50), NEWID())+'}}') where ((ccGuid is null)or(ccguid=''))";
                                    env.core.db.executeNonQuery(sql);
                                    continue;
                                }
                            }
                            //
                            // -- detect and log duplicate guid fields before fixing
                            recordsAffected = 0;
                            sql = $"select b.id, b.ccguid, b.DateAdded, b.ModifiedDate, b.CreatedBy, b.ModifiedBy from {tableName} a, {tableName} b where a.id<b.id and a.ccguid=b.ccguid";
                            using (DataTable dtDup = env.core.db.executeQuery(sql)) {
                                if (DbController.isDataTableOk(dtDup)) {
                                    recordsAffected = dtDup.Rows.Count;
                                    string msg = $"Housekeep, TablesClass, ERROR, [{recordsAffected}] records with duplicate ccguids found and fixed in table[{tableName}]. Find and fix the process that creates records with dup ccguid";
                                    env.log(msg);
                                    logger.Error($"{env.core.logCommonMessage}, {msg}");
                                    //
                                    // -- log detail for each problematic record (capped)
                                    int detailCount = 0;
                                    foreach (DataRow drDetail in dtDup.Rows) {
                                        if (detailCount >= maxDetailRecords) {
                                            env.log($"Housekeep, TablesClass, ...and [{recordsAffected - maxDetailRecords}] more records not shown");
                                            break;
                                        }
                                        int recordId = GenericController.getInteger(drDetail["id"]);
                                        string dupGuid = GenericController.getText(drDetail["ccguid"]);
                                        DateTime dateAdded = GenericController.getDate(drDetail["DateAdded"]);
                                        DateTime modifiedDate = GenericController.getDate(drDetail["ModifiedDate"]);
                                        int createdBy = GenericController.getInteger(drDetail["CreatedBy"]);
                                        int modifiedBy = GenericController.getInteger(drDetail["ModifiedBy"]);
                                        string dateAddedText = (dateAdded == DateTime.MinValue) ? "(null)" : dateAdded.ToString();
                                        string modifiedDateText = (modifiedDate == DateTime.MinValue) ? "(null)" : modifiedDate.ToString();
                                        env.log($"Housekeep, TablesClass, dup ccguid detail, table[{tableName}], id[{recordId}], ccguid[{dupGuid}], DateAdded[{dateAddedText}], ModifiedDate[{modifiedDateText}], CreatedBy[{createdBy}], ModifiedBy[{modifiedBy}]");
                                        detailCount++;
                                    }
                                    //
                                    // -- fix the duplicate ccguids
                                    sql = $"update b set ccguid=lower('{{'+CONVERT(nvarchar(50), NEWID())+'}}') from {tableName} a,{tableName} b where a.id<b.id and a.ccguid=b.ccguid";
                                    env.core.db.executeNonQuery(sql);
                                    continue;
                                }
                            }
                        } catch (Exception ex) {
                            logger.Error(ex, $"Housekeep Error updating table {tableName}, ex [{ex}], last sql {sql}, {env.core.logCommonMessage}");
                            LogController.logAlarm(env.core, $"Housekeep Error updating table {tableName}, ex [{ex}], last sql {sql}, {env.core.logCommonMessage}");
                            // -- no not throw, test the other tables
                            continue;
                        }
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{env.core.logCommonMessage}");
                LogController.logAlarm(env.core, "Housekeep, exception, ex [" + ex + "]");
                throw;
            }
        }
    }
}