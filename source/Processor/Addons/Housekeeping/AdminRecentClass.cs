
using Contensive.Processor.Controllers;
using NLog;
using System;

namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// Activity Log
    /// </summary>
    public static class AdminRecentClass {
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
                env.log("Housekeep, executeHourlyTasks, ccAdminRecents");
                //
                string sql = "" +
                    "delete " +
                    "   from ccAdminRecents" +
                    "from " +
                    "   ccAdminRecents r" +
                    "   left join cccontent c on c.id=SUBSTRING([href], 6, 99)" +
                    "where " +
                    "   href like 'cid=%'" +
                    "and (c.id is null)";
                env.core.db.executeNonQuery(sql);
                //
                sql = "" +
                    "delete " +
                    "   from ccAdminRecents" +
                    "from " +
                    "   ccAdminRecents r" +
                    "   left join ccAggregateFunctions c on c.id=SUBSTRING([href], 16, 99)" +
                    "where " +
                    "   href like '/admin?addonid=%'" +
                    "and (c.id is null)";
                env.core.db.executeNonQuery(sql);
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
                env.log("Housekeep, ccAdminRecents");
                //
            } catch (Exception ex) {
                logger.Error(ex, $"{env.core.logCommonMessage}");
                LogController.logAlarm(env.core, "Housekeep, exception, ex [" + ex + "]");
                throw;

            }
        }
    }
}