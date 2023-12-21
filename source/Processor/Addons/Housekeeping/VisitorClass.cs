
using Contensive.Processor.Controllers;
using System;

namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// Housekeep this content
    /// </summary>
    public static class VisitorClass {
        //
        //====================================================================================================
        /// <summary>
        /// execute hourly tasks
        /// </summary>
        /// <param name="core"></param>
        public static void executeHourlyTasks(HouseKeepEnvironmentModel env) {
            try {
                //
                env.log("Housekeep, executeHourlyTasks, Visitor");
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
                env.log("Housekeep, visitors");
                //
                // delete nocookie visits
                // This must happen after the housekeep summarizing, and no sooner then 48 hours ago so all hits have been summarized before deleting
                //
                if (env.archiveDeleteNoCookie) {
                    //
                    // delete visitors from the non-cookie visits
                    //
                    env.log("Deleting visitors from visits with no cookie support older than Midnight, Two Days Ago");
                    string sql = "delete from ccvisitors from ccvisitors r,ccvisits v where r.id=v.visitorid and(v.CookieSupport=0)and(v.LastVisitTime<DATEADD(day,-2,CAST(GETDATE() AS DATE)))";
                    env.core.db.sqlCommandTimeout = 180;
                    env.core.db.executeNonQuery(sql);
                }
                {
                    env.log("delete visitors with no people (people from bot visits were removed so this removes visitors from bot visits, role of visitor is to connect visits and auto-login. )");
                    //
                    int recordsAffected = 0;
                    int cnt = 0;
                    do {
                        env.core.db.sqlCommandTimeout = 180;
                        env.core.db.executeNonQuery("delete top (1000) from ccvisitors from ccvisitors r left join ccmembers m on m.id=r.MemberID where m.id is null", ref recordsAffected);
                        cnt++;
                    } while ((recordsAffected != 0) && (cnt < 100));
                }
                {
                    env.log("delete visitors from bots. )");
                    //
                    int recordsAffected = 0;
                    int cnt = 0;
                    do {
                        env.core.db.sqlCommandTimeout = 180;
                        env.core.db.executeNonQuery("delete top (1000) from ccvisitors where bot>0", ref recordsAffected);
                        cnt++;
                    } while ((recordsAffected != 0) && (cnt < 100));
                }
                {
                    env.log("delete visitors from visit bots. )");
                    //
                    int recordsAffected = 0;
                    int cnt = 0;
                    do {
                        env.core.db.sqlCommandTimeout = 180;
                        env.core.db.executeNonQuery("delete top (1000) from ccvisitors from ccvisitors t left join ccvisits v on v.VisitorID=t.id where v.bot=1", ref recordsAffected);
                        cnt++;
                    } while ((recordsAffected != 0) && (cnt < 100));
                }
            } catch (Exception ex) {
                LogControllerX.logError(env.core, ex);
                LogControllerX.logAlarm(env.core, "Housekeep, exception, ex [" + ex + "]");
                throw;
            }
        }
    }
}