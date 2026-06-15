
using Contensive.Processor.Controllers;
using NLog;
using System;
using System.Collections.Generic;

namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// Housekeep this content
    /// </summary>
    public static class EmailLogClass {
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
                env.log("Housekeep, executeHourlyTasks, EmailLog");
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
                env.log("Housekeep, email log");
                //
                // email log for only 365 days
                env.core.db.executeNonQuery("delete from ccemaillog where (dateadded < DATEADD(day,-@ageDays,CAST(GETDATE() AS DATE)))", new Dictionary<string, object> { { "@ageDays", env.emailDropArchiveAgeDays } });
                //
                // clear email body field for emails older than 7 days
                env.log("Clear email body field for email logs older then " + env.emailLogBodyRetainDays + " days");
                DateTime emailLogBodyRetainDate = env.core.dateTimeNowMockable.AddDays(-env.emailLogBodyRetainDays).Date;
                env.core.db.executeNonQuery("update ccemaillog set body=null where dateadded<@retainDate", new Dictionary<string, object> { { "@retainDate", emailLogBodyRetainDate } });
            } catch (Exception ex) {
                logger.Error(ex, $"{env.core.logCommonMessage}");
                LogController.logAlarm(env.core, "Housekeep, exception, ex [" + ex + "]");
                throw;
            }
        }
    }
}