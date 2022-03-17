
using Contensive.Processor.Controllers;
using System;

namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// Activity Log
    /// </summary>
    public static class TasksClass {
        //
        //====================================================================================================
        /// <summary>
        /// execute hourly tasks
        /// </summary>
        /// <param name="core"></param>
        public static void executeHourlyTasks(HouseKeepEnvironmentModel env) {
            try {
                //
                env.log("Housekeep, executeHourlyTasks, TasksClass");
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
                env.log("Housekeep, TasksClass");
                {
                    //
                    //
                    env.log("Housekeep, ccTasks, delete tasks started and not finished with 1 day.");
                    //
                    env.core.db.executeNonQuery("delete from cctasks where (cmdRunner is not null)or(DateAdded<DATEADD(day,-1,CAST(GETDATE() AS DATE)))");
                }
            } catch (Exception ex) {
                LogController.logError(env.core, ex);
                LogController.logAlarm(env.core, "Housekeep, exception, ex [" + ex + "]");
                throw;

            }
        }
    }
}