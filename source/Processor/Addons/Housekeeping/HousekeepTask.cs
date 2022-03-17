
using Contensive.BaseClasses;
using Contensive.Processor.Controllers;
using System;
//
namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// support for housekeeping functions
    /// </summary>
    public class HousekeepTask : AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// addon interface
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(CPBaseClass cp) {
            CoreController core = ((CPClass)cp).core;
            try {
                //
                var env = new HouseKeepEnvironmentModel(core);
                int TimeoutSave = env.core.db.sqlCommandTimeout;
                env.core.db.sqlCommandTimeout = 1800;
                //
                env.log("Housekeep, start");
                //
                // -- hourly tasks
                HousekeepHourlyTasksClass.executeHourlyTasks(env);
                //
                // -- daily tasks
                env.log("Housekeep, check for daily trigger -- dateTimeNowMockable.Date [" + core.dateTimeNowMockable.Date + "], lastCheckDateTime.Date [" + env.lastCheckDateTime.Date + "], serverHousekeepHour [" + env.serverHousekeepHour + "], dateTimeNowMockable.Hour [" + core.dateTimeNowMockable.Hour + "]");
                bool hasNotRunToday = core.dateTimeNowMockable.Date > env.lastCheckDateTime.Date;
                bool isAfterAlarmHour = env.serverHousekeepHour < core.dateTimeNowMockable.Hour;
                bool runDailyTasks = hasNotRunToday && isAfterAlarmHour;
                if (env.forceHousekeep || runDailyTasks) {
                    HousekeepDailyTasksClass.executeDailyTasks(env);
                }
                //
                env.log("Housekeep, done");
                //
                env.core.db.sqlCommandTimeout = TimeoutSave;
                return "";
            } catch (Exception ex) {
                LogController.logError(core, ex);
                LogController.logAlarm(core, "Housekeep, exception, ex [" + ex + "]");
                throw;
            }
        }
    }
}
