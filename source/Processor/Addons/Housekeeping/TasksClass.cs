
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using NLog;
using System;
using System.Collections.Generic;

namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// Housekeeping for the Tasks table
    /// </summary>
    public static class TasksClass {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        /// <summary>
        /// execute hourly tasks
        /// </summary>
        public static void executeHourlyTasks(HouseKeepEnvironmentModel env) {
            try {
                env.log("Housekeep, executeHourlyTasks, TasksClass");
                //
                // -- clean up completed/failed tasks older than TaskRetentionMinutes
                int retentionMinutes = env.core.siteProperties.getInteger("TaskRetentionMinutes", 60);
                if (retentionMinutes > 0) {
                    env.log($"Housekeep, TasksClass, deleting completed/failed tasks older than {retentionMinutes} minutes.");
                    //
                    // -- first delete any cdn download files for tasks being removed
                    string selectSql = $"select id, downloadPathFilename from cctasks where (dateCompleted is not null) and (dateCompleted < DATEADD(minute, -{retentionMinutes}, GETDATE()))";
                    using (var dt = env.core.db.executeQuery(selectSql)) {
                        foreach (System.Data.DataRow row in dt.Rows) {
                            string downloadPath = row["downloadPathFilename"]?.ToString();
                            if (!string.IsNullOrEmpty(downloadPath)) {
                                try {
                                    env.core.cdnFiles.deleteFile(downloadPath);
                                } catch (Exception exFile) {
                                    logger.Warn(exFile, $"{env.core.logCommonMessage},Housekeep, TasksClass, error deleting cdn file [{downloadPath}]");
                                }
                            }
                        }
                    }
                    //
                    // -- delete the task records
                    env.core.db.executeNonQuery($"delete from cctasks where (dateCompleted is not null) and (dateCompleted < DATEADD(minute, -{retentionMinutes}, GETDATE()))");
                }
                //
                // -- mark orphaned running tasks as failed (started but no completion after 30 minutes)
                env.log("Housekeep, TasksClass, marking orphaned running tasks as failed.");
                env.core.db.executeNonQuery("update cctasks set dateCompleted = GETDATE(), failMessage = 'Task timed out (no completion received)' where (dateStarted is not null) and (dateCompleted is null) and (dateStarted < DATEADD(minute, -30, GETDATE()))");
                //
            } catch (Exception ex) {
                logger.Error(ex, $"{env.core.logCommonMessage}");
                LogController.logAlarm(env.core, $"Housekeep, exception, ex [{ex}]");
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// execute Daily Tasks
        /// </summary>
        public static void executeDailyTasks(HouseKeepEnvironmentModel env) {
            try {
                env.log("Housekeep, TasksClass, daily cleanup");
                //
                // -- delete very old tasks that somehow survived hourly cleanup (safety net, older than 7 days)
                env.core.db.executeNonQuery("delete from cctasks where (DateAdded < DATEADD(day, -7, GETDATE()))");
                //
            } catch (Exception ex) {
                logger.Error(ex, $"{env.core.logCommonMessage}");
                LogController.logAlarm(env.core, $"Housekeep, exception, ex [{ex}]");
                throw;
            }
        }
    }
}