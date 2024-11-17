
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using NLog;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using System;
using System.Data;
using System.Windows.Interop;

namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// Activity Log
    /// </summary>
    public static class TablesClass {
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
                        string tableName = GenericController.encodeText(dr[0]);
                        if (string.IsNullOrEmpty(tableName)) {
                            string msg = $"Housekeep, TablesClass, ERROR, cctables record with empty name, id [{GenericController.encodeText(dr[1])}]. Delete this cctables record and all ccFields with contentid [{GenericController.encodeText(dr[1])}].";
                            env.log(msg);
                            logger.Error(msg);
                            continue;
                        }
                        if (!env.core.db.isSQLTable(tableName)) {
                            string msg = $"Housekeep, TablesClass, ERROR, ccTable record [{tableName}] but Db catalog [{env.core.appConfig.name}] does not include this table. Create this table in the database and all ccfields records assocated to it.";
                            env.log(msg);
                            logger.Error(msg);
                            continue;
                        }
                        //
                        // -- update empty guid fields
                        int recordsAffected = 0;
                        env.core.db.executeNonQuery($"update {tableName} set ccguid=lower('{{'+CONVERT(nvarchar(50), NEWID())+'}}') where ((ccGuid is null)or(ccguid=''))", ref recordsAffected);
                        if (recordsAffected > 0) {
                            string msg = $"Housekeep, TablesClass, ERROR, [{recordsAffected}] records with blank ccguid found and fixed in table[{tableName}]. Find and fix the the process that creates records with blank ccguid";
                            env.log(msg);
                            logger.Error(msg);
                            continue;
                        }
                        //
                        // -- update guid dups
                        recordsAffected = 0;
                        env.core.db.executeNonQuery($"update b set ccguid=lower('{{'+CONVERT(nvarchar(50), NEWID())+'}}') from {tableName} a,{tableName} b where a.id<b.id and a.ccguid=b.ccguid", ref recordsAffected);
                        if (recordsAffected > 0) {
                            string msg = $"Housekeep, TablesClass, ERROR, [{recordsAffected}] records with duplicate ccguids found and fixed in table[{tableName}]. Find and fix the the process that creates records with dup ccguid";
                            env.log(msg);
                            logger.Error(msg);
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