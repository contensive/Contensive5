
using Contensive.Processor.Controllers;
using System;
using static Contensive.Processor.Controllers.GenericController;

namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// Housekeep visits
    /// </summary>
    public static class VisitClass {
        //
        //====================================================================================================
        /// <summary>
        /// execute hourly tasks
        /// </summary>
        /// <param name="core"></param>
        public static void executeHourlyTasks(HouseKeepEnvironmentModel env) {
            try {
                //
                env.log("Housekeep, executeHourlyTasks, Visits");
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
        /// Daily tasks
        /// </summary>
        /// <param name="core"></param>
        /// <param name="env"></param>
        public static void executeDailyTasks(HouseKeepEnvironmentModel env) {
            try {
                //
                env.log("Housekeep, executeDailyTasks");
                {
                    //
                    env.log("Delete visits with no DateAdded");
                    //
                    int loopCnt = 0;
                    int recordsAffected = 0;
                    do {
                        env.core.db.sqlCommandTimeout = 180;
                        env.core.db.executeNonQuery("delete top (1000) from ccvisits where (DateAdded is null)", ref recordsAffected);
                    } while (recordsAffected != 0 && ++loopCnt < 100);
                }
                {
                    //
                    env.log("Delete visits with no visitor, 2-days old to allow visit-summary");
                    //
                    int loopCnt = 0;
                    int recordsAffected = 0;
                    do {
                        env.core.db.sqlCommandTimeout = 180;
                        env.core.db.executeNonQuery("delete top (1000) from ccvisits from ccvisits v left join ccvisitors r on r.id=v.visitorid where (r.id is null) and (v.DateAdded<DATEADD(day,-2,CAST(GETDATE() AS DATE)))",ref recordsAffected);
                    } while (recordsAffected != 0 && ++loopCnt < 100);
                }

                {
                    //
                    env.log("Delete bot visits, 2-days old to allow visit-summary");
                    //
                    int loopCnt = 0;
                    int recordsAffected = 0;
                    do {
                        env.core.db.sqlCommandTimeout = 180;
                        env.core.db.executeNonQuery("delete top (1000) from ccvisits from ccvisits v where (v.bot>0) and (v.DateAdded<DATEADD(day,-2,CAST(GETDATE() AS DATE)))", ref recordsAffected);
                    } while (recordsAffected != 0 && ++loopCnt < 100);
                }
                {
                    //
                    env.log("delete visits with no people (no functional use to site beyond reporting, which is limited past archive date)");
                    //
                    int loopCnt = 0;
                    int recordsAffected = 0;
                    do {
                        env.core.db.sqlCommandTimeout = 180;
                        env.core.db.executeNonQuery("delete top (1000) from ccvisits from ccvisits v left join ccmembers m on m.id=v.memberid where (m.id is null) and (v.DateAdded<DATEADD(day,-2,CAST(GETDATE() AS DATE)))", ref recordsAffected);
                    } while (recordsAffected != 0 && ++loopCnt < 100);
                }
                if (env.archiveDeleteNoCookie) {
                    //
                    env.log("Deleting visits with no cookie support older than Midnight, Two Days Ago");
                    //
                    int loopCnt = 0;
                    int recordsAffected = 0;
                    do {
                        env.core.db.sqlCommandTimeout = 180;
                        env.core.db.executeNonQuery("delete top (1000) from ccvisits where (CookieSupport=0)and(LastVisitTime<DATEADD(day,-2,CAST(GETDATE() AS DATE)))", ref recordsAffected);
                    } while (recordsAffected != 0 && ++loopCnt < 100);
                }
                {
                    //
                    env.log("Housekeep, visits over 2 days old");
                    // 
                    int recordsAffected = 0;
                    int cnt = 0;
                    do {
                        env.core.db.sqlCommandTimeout = 180;
                        env.core.db.executeNonQuery("delete top (1000) from ccvisits where (LastVisitTime is null)or(LastVisitTime<DATEADD(day,-2,CAST(GETDATE() AS DATE)))");
                        cnt++;
                    } while ((recordsAffected != 0) && (cnt < 100));
                }
                {
                    //
                    env.log("Housekeep, visits from bots");
                    // 
                    int recordsAffected = 0;
                    int cnt = 0;
                    do {
                        env.core.db.sqlCommandTimeout = 180;
                        env.core.db.executeNonQuery("delete top (1000) from ccvisits from ccvisits v where (v.bot = 1)");
                        cnt++;
                    } while ((recordsAffected != 0) && (cnt < 100));
                }

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
        /// delete old visits
        /// </summary>
        /// <param name="core"></param>
        /// <param name="DeleteBeforeDate"></param>
        public static void houseKeep_App_Daily_RemoveVisitRecords(HouseKeepEnvironmentModel env, DateTime DeleteBeforeDate) {
            try {
                //
                int TimeoutSave = 0;
                string DeleteBeforeDateSQL = null;
                string appName = null;
                string SQLTablePeople = null;
                //
                // Set long timeout (30 min) needed for heavy work on big tables
                TimeoutSave = env.core.db.sqlCommandTimeout;
                env.core.db.sqlCommandTimeout = 1800;
                //
                SQLTablePeople = MetadataController.getContentTablename(env.core, "People");
                //
                appName = env.core.appConfig.name;
                DeleteBeforeDateSQL = DbControllerX.encodeSQLDate(DeleteBeforeDate);
                //
                // Visits older then archive age
                //
                env.log("Deleting visits before [" + DeleteBeforeDateSQL + "]");
                env.core.db.deleteTableRecordChunks("ccVisits", "(DateAdded<" + DeleteBeforeDateSQL + ")", 1000, 10000);
                //
                // Viewings with visits before the first
                //
                env.log("Deleting viewings with visitIDs lower then the lowest ccVisits.ID");
                env.core.db.deleteTableRecordChunks("ccviewings", "(visitid<(select min(ID) from ccvisits))", 1000, 10000);

                //
                // restore sved timeout
                //
                env.core.db.sqlCommandTimeout = TimeoutSave;
                return;
                //
            } catch (Exception ex) {
                LogControllerX.logError(env.core, ex);
            }
        }
    }
}