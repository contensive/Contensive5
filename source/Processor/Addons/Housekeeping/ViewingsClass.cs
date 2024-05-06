
using Contensive.Processor.Controllers;
using NLog;
using System;

namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// Housekeep this content
    /// </summary>
    public static class ViewingsClass {
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
                env.log("Housekeep, executeHourlyTasks, Viewings");
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
                env.log("Housekeep, viewings");
                //
                try {
                    //
                    // delete old viewings
                    env.core.db.sqlCommandTimeout = 1800;
                    env.core.db.executeNonQuery("delete from ccviewings where (dateadded < DATEADD(day,-" + env.archiveAgeDays + ",CAST(GETDATE() AS DATE)))");
                } catch (Exception) {
                    logger.Warn($"{env.core.logCommonMessage}, exception deleting old viewings");
                }
                //
                if (env.archiveDeleteNoCookie) {
                    //
                    env.log("Deleting viewings from visits with no cookie support older than Midnight, Two Days Ago");
                    //
                    // if this fails, continue with the rest of the work
                    try {
                        string sql = "delete from ccviewings from ccviewings h,ccvisits v where h.visitid=v.id and(v.CookieSupport=0)and(v.LastVisitTime<DATEADD(day,-2,CAST(GETDATE() AS DATE)))";
                        env.core.db.sqlCommandTimeout = 1800;
                        env.core.db.executeNonQuery(sql);
                    } catch (Exception) {
                        logger.Warn($"{env.core.logCommonMessage}, exception deleting viewings with no cookie");
                    }
                }
                //
                env.log("Deleting viewings with null or invalid VisitID");
                //
                try {
                    string sql = "delete from ccviewings  where (visitid=0 or visitid is null)";
                    env.core.db.sqlCommandTimeout = 1800;
                    env.core.db.executeNonQuery(sql);
                } catch (Exception) {
                    logger.Warn($"{env.core.logCommonMessage}, exception deleting viewings with invalid visits");
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{env.core.logCommonMessage}");
                LogController.logAlarm(env.core, "Housekeep, exception, ex [" + ex + "]");
                throw;
            }
        }
    }
}