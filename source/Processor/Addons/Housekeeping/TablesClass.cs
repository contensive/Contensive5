
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using NLog;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using System;
using System.Data;

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
                    DataTable dt = env.core.db.executeQuery("select name from cctables");
                    if (dt?.Rows is null || dt.Rows.Count == 0) { return; }
                    foreach (DataRow dr in dt.Rows) {
                        string tableName = GenericController.encodeText(dr[0]);
                        //
                        // -- update empty guid fields
                        env.core.db.executeNonQuery($"update {tableName} set ccguid=lower('{{'+CONVERT(nvarchar(50), NEWID())+'}}') where ((ccGuid is null)or(ccguid=''))");
                        //
                        // -- update guid dups
                        env.core.db.executeNonQuery($"update b set ccguid=lower('{{'+CONVERT(nvarchar(50), NEWID())+'}}') from {tableName} a,{tableName} b where a.id<b.id and a.ccguid=b.ccguid");
                    }
                    //
                    env.log("Housekeep, ccTasks, delete tasks started and not finished with 1 day.");
                    //
                    env.core.db.executeNonQuery("delete from cctasks where (cmdRunner is not null)or(DateAdded<DATEADD(day,-1,CAST(GETDATE() AS DATE)))");
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{env.core.logCommonMessage}");
                LogController.logAlarm(env.core, "Housekeep, exception, ex [" + ex + "]");
                throw;

            }
        }
    }
}