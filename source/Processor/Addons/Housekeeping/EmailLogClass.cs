
using Contensive.Processor.Controllers;
using System;

namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// Housekeep this content
    /// </summary>
    public static class EmailLogClass {
        //
        //====================================================================================================
        /// <summary>
        /// execute hourly tasks
        /// </summary>
        /// <param name="core"></param>
        public static void executeHourlyTasks(HouseKeepEnvironmentModel env) {
            try {
                //
                env.log("Housekeep, executeHourlyTasks, EmailLog");
                //
            } catch (Exception ex) {
                LogControllerX.logError(env.core, ex);
                LogControllerX.logAlarm(env.core, "Housekeep, exception, ex [" + ex + "]");
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
                env.log("Housekeep, email log");
                //
                // email log for only 365 days
                env.core.db.executeNonQuery("delete from ccemaillog where (dateadded < DATEADD(day,-" + env.emailDropArchiveAgeDays + ",CAST(GETDATE() AS DATE)))");
                //
                // clear email body field for emails older than 7 days
                env.log("Clear email body field for email logs older then " + env.emailLogBodyRetainDays + " days");
                DateTime emailLogBodyRetainDate = env.core.dateTimeNowMockable.AddDays(-env.emailLogBodyRetainDays).Date;
                env.core.db.executeNonQuery("update ccemaillog set body=null where dateadded<" + DbControllerX.encodeSQLDate(emailLogBodyRetainDate));
            } catch (Exception ex) {
                LogControllerX.logError(env.core, ex);
                LogControllerX.logAlarm(env.core, "Housekeep, exception, ex [" + ex + "]");
                throw;
            }
        }
    }
}