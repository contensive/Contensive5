
using Contensive.BaseClasses;
using Contensive.Processor.Controllers;
using NLog;
using System;
//
namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// support for housekeeping functions
    /// </summary>
    public class HousekeepTask : AddonBaseClass {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
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
                //
                env.log("Housekeep, start");
                //
                // -- hourly tasks
                env.log("Housekeep, check for hourly trigger -- forceHousekeep [" + env.forceHousekeep + "], dateTimeNowMockable.Hour [" + core.dateTimeNowMockable.Hour + "], baseHousekeepHourlyLastRunTime.Date [" + env.baseHousekeepHourlyLastRunTime.Date + "]");
                bool hasNotRunThisHour = (env.core.dateTimeNowMockable.Hour != env.baseHousekeepHourlyLastRunTime.Hour);
                if (env.forceHousekeep || hasNotRunThisHour) {
                    env.log("Housekeep, run hourly housekeep");
                    env.baseHousekeepHourlyLastRunTime = env.core.dateTimeNowMockable;
                    HousekeepHourlyTasksClass.executeHourlyTasks(env);
                }
                //
                // -- daily tasks
                env.log("Housekeep, check for daily trigger -- forceHousekeep [" + env.forceHousekeep + "], dateTimeNowMockable.Date [" + core.dateTimeNowMockable.Date + "], baseHousekeepDailyLastRunTime.Date [" + env.baseHousekeepDailyLastRunTime.Date + "], baseHousekeepRunHour [" + env.baseHousekeepRunHour + "], dateTimeNowMockable.Hour [" + core.dateTimeNowMockable.Hour + "]");
                bool hasNotRunToday = core.dateTimeNowMockable.Date > env.baseHousekeepDailyLastRunTime.Date;
                bool isAfterAlarmHour = env.baseHousekeepRunHour < core.dateTimeNowMockable.Hour;
                bool runDailyTasks = hasNotRunToday && isAfterAlarmHour;
                if (env.forceHousekeep || runDailyTasks) {
                    env.log("Housekeep, run daily housekeep");
                    env.baseHousekeepDailyLastRunTime = env.core.dateTimeNowMockable;
                    HousekeepDailyTasksClass.executeDailyTasks(env);
                }
                //
                env.log("Housekeep, done");
                return "";
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                LogController.logAlarm(core, "Housekeep, exception, ex [" + ex + "]");
                throw;
            }
        }
    }
}
