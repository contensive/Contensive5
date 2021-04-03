
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
        public static void executeHourlyTasks(CoreController core) {
            try {
                //
                LogController.logInfo(core, "Housekeep, executeHourlyTasks, Visits");
                //
            } catch (Exception ex) {
                LogController.logError(core, ex);
                LogController.logAlarm(core, "Housekeep, exception, ex [" + ex + "]");
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
        public static void executeDailyTasks(CoreController core, HouseKeepEnvironmentModel env) {
            try {
                //
                LogController.logInfo(core, "Housekeep, executeDailyTasks");
                {
                    //
                    LogController.logInfo(core, "Delete visits with no DateAdded");
                    //
                    int loopCnt = 0;
                    int recordsAffected = 0;
                    do {
                        core.db.sqlCommandTimeout = 180;
                        core.db.executeNonQuery("delete top (1000) from ccvisits where (DateAdded is null)", ref recordsAffected);
                    } while (recordsAffected != 0 && ++loopCnt < 100);
                }
                {
                    //
                    LogController.logInfo(core, "Delete visits with no visitor, 2-days old to allow visit-summary");
                    //
                    int loopCnt = 0;
                    int recordsAffected = 0;
                    do {
                        core.db.sqlCommandTimeout = 180;
                        core.db.executeNonQuery("delete top (1000) from ccvisits from ccvisits v left join ccvisitors r on r.id=v.visitorid where (r.id is null) and (v.DateAdded<DATEADD(day,-2,CAST(GETDATE() AS DATE)))",ref recordsAffected);
                    } while (recordsAffected != 0 && ++loopCnt < 100);
                }
                {
                    //
                    LogController.logInfo(core, "Delete bot visits, 2-days old to allow visit-summary");
                    //
                    int loopCnt = 0;
                    int recordsAffected = 0;
                    do {
                        core.db.sqlCommandTimeout = 180;
                        core.db.executeNonQuery("delete top (1000) from ccvisits from ccvisits v where (v.bot>0) and (v.DateAdded<DATEADD(day,-2,CAST(GETDATE() AS DATE)))", ref recordsAffected);
                    } while (recordsAffected != 0 && ++loopCnt < 100);
                }
                {
                    //
                    LogController.logInfo(core, "delete visits with no people (no functional use to site beyond reporting, which is limited past archive date)");
                    //
                    int loopCnt = 0;
                    int recordsAffected = 0;
                    do {
                        core.db.sqlCommandTimeout = 180;
                        core.db.executeNonQuery("delete top (1000) from ccvisits from ccvisits v left join ccmembers m on m.id=v.memberid where (m.id is null) and (v.DateAdded<DATEADD(day,-2,CAST(GETDATE() AS DATE)))", ref recordsAffected);
                    } while (recordsAffected != 0 && ++loopCnt < 100);
                }
                if (env.archiveDeleteNoCookie) {
                    //
                    LogController.logInfo(core, "Deleting visits with no cookie support older than Midnight, Two Days Ago");
                    //
                    int loopCnt = 0;
                    int recordsAffected = 0;
                    do {
                        core.db.sqlCommandTimeout = 180;
                        core.db.executeNonQuery("delete top (1000) from ccvisits where (CookieSupport=0)and(LastVisitTime<DATEADD(day,-2,CAST(GETDATE() AS DATE)))", ref recordsAffected);
                    } while (recordsAffected != 0 && ++loopCnt < 100);
                }
                DateTime OldestVisitDate = default;
                //
                // Get Oldest Visit
                using (var csData = new CsModel(core)) {
                    if (csData.openSql(core.db.getSQLSelect("ccVisits", "DateAdded", "", "dateadded", "", 1))) {
                        OldestVisitDate = csData.getDate("DateAdded").Date;
                    }
                }
                //
                // Remove old visit records
                //   if > 30 days in visit table, limit one pass to just 30 days
                //   this is to prevent the entire server from being bogged down for one site change
                //
                if (OldestVisitDate == DateTime.MinValue) {
                    LogController.logInfo(core, "No visit records were removed because no visit records were found while requesting the oldest visit.");
                } else {
                    DateTime ArchiveDate = core.dateTimeNowMockable.AddDays(-env.archiveAgeDays).Date;
                    int DaystoRemove = encodeInteger(ArchiveDate.Subtract(OldestVisitDate).TotalDays);
                    if (DaystoRemove > 30) {
                        ArchiveDate = OldestVisitDate.AddDays(30);
                    }
                    if (OldestVisitDate >= ArchiveDate) {
                        LogController.logInfo(core, "No records were removed because Oldest Visit Date [" + OldestVisitDate + "] >= ArchiveDate [" + ArchiveDate + "].");
                    } else {
                        LogController.logInfo(core, "Removing records from [" + OldestVisitDate + "] to [" + ArchiveDate + "].");
                        DateTime SingleDate = default;
                        SingleDate = OldestVisitDate;
                        do {
                            houseKeep_App_Daily_RemoveVisitRecords(core, SingleDate);
                            SingleDate = SingleDate.AddDays(1);
                        } while (SingleDate < ArchiveDate);
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                LogController.logAlarm(core, "Housekeep, exception, ex [" + ex + "]");
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
        public static void houseKeep_App_Daily_RemoveVisitRecords(CoreController core, DateTime DeleteBeforeDate) {
            try {
                //
                int TimeoutSave = 0;
                string DeleteBeforeDateSQL = null;
                string appName = null;
                string SQLTablePeople = null;
                //
                // Set long timeout (30 min) needed for heavy work on big tables
                TimeoutSave = core.db.sqlCommandTimeout;
                core.db.sqlCommandTimeout = 1800;
                //
                SQLTablePeople = MetadataController.getContentTablename(core, "People");
                //
                appName = core.appConfig.name;
                DeleteBeforeDateSQL = DbController.encodeSQLDate(DeleteBeforeDate);
                //
                // Visits older then archive age
                //
                LogController.logInfo(core, "Deleting visits before [" + DeleteBeforeDateSQL + "]");
                core.db.deleteTableRecordChunks("ccVisits", "(DateAdded<" + DeleteBeforeDateSQL + ")", 1000, 10000);
                //
                // Viewings with visits before the first
                //
                LogController.logInfo(core, "Deleting viewings with visitIDs lower then the lowest ccVisits.ID");
                core.db.deleteTableRecordChunks("ccviewings", "(visitid<(select min(ID) from ccvisits))", 1000, 10000);

                //
                // restore sved timeout
                //
                core.db.sqlCommandTimeout = TimeoutSave;
                return;
                //
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
        }
    }
}