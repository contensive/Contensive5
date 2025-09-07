//
using Contensive.Processor.Controllers;
using NLog;
using System;

namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// Housekeep this content
    /// </summary>
    public static class ContentWatchListRulesClass {
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
                env.log("Housekeep, executeHourlyTasks, ContentWatchListRules");
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
                // ContentWatchListRules with bad ContentWatchID
                //
                env.log("Deleting ContentWatchList Rules with bad ContentWatchID.");
                string sql = "delete ccContentWatchListRules"
                    + " From ccContentWatchListRules"
                    + " LEFT JOIN ccContentWatch on ccContentWatch.ID=ccContentWatchListRules.ContentWatchID"
                    + " WHERE (ccContentWatch.ID is null)";
                env.core.db.executeNonQuery(sql);
                //
                // ContentWatchListRules with bad ContentWatchListID
                //
                env.log("Deleting ContentWatchList Rules with bad ContentWatchListID.");
                sql = "delete ccContentWatchListRules"
                    + " From ccContentWatchListRules"
                    + " LEFT JOIN ccContentWatchLists on ccContentWatchLists.ID=ccContentWatchListRules.ContentWatchListID"
                    + " WHERE (ccContentWatchLists.ID is null)";
                env.core.db.executeNonQuery(sql);

            } catch (Exception ex) {
                logger.Error(ex, $"{env.core.logCommonMessage}");
                LogController.logAlarm(env.core, "Housekeep, exception, ex [" + ex + "]");
                throw;

            }
        }
    }
}