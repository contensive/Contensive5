
using Contensive.Processor.Controllers;
using System;

namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// Activity Log
    /// </summary>
    public static class ActivityLogClass {
        //
        //====================================================================================================
        /// <summary>
        /// execute hourly tasks
        /// </summary>
        /// <param name="core"></param>
        public static void executeHourlyTasks(HouseKeepEnvironmentModel env) {
            try {
                //
                env.log("Housekeep, executeHourlyTasks, ActivityLogClass");
                //
            } catch (Exception ex) {
                LogController.logError(env.core, ex);
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
                env.log("Housekeep, activitylog");
                {
                    env.core.db.executeNonQuery("delete from ccactivitylog where message like 'modifying field%'");
                    env.core.db.executeNonQuery("update ccActivityLog set name=LEFT(message,30) where name is null");
                }
                {
                    //
                    env.log("Deleting activities with no member record.");
                    //
                    env.core.db.executeNonQuery("delete ccactivitylog from ccactivitylog left join ccmembers on ccmembers.id=ccactivitylog.memberid where (ccmembers.id is null)");
                }

            } catch (Exception ex) {
                LogController.logError(env.core, ex);
                LogController.logAlarm(env.core, "Housekeep, exception, ex [" + ex + "]");
                throw;

            }
        }
    }
}