
using Contensive.Processor.Controllers;
using System;

namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// Housekeep this content
    /// </summary>
    public static class PageContentClass {
        //
        //====================================================================================================
        /// <summary>
        /// execute hourly tasks
        /// </summary>
        /// <param name="core"></param>
        public static void executeHourlyTasks(HouseKeepEnvironmentModel env) {
            try {
                //
                env.log("Housekeep, executeHourlyTasks, PageContent");
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
                env.log("HousekeepDaily, page content");
                {
                    //
                    // Move Archived pages from their current parent to their archive parent
                    //
                    bool NeedToClearCache = false;
                    string SQL = "select * from ccpagecontent where (( DateArchive is not null )and(DateArchive<" + env.core.sqlDateTimeMockable + "))and(active<>0)";
                    using (var csData = new CsModel(env.core)) {
                        csData.openSql(SQL);
                        while (csData.ok()) {
                            int RecordId = csData.getInteger("ID");
                            int ArchiveParentId = csData.getInteger("ArchiveParentID");
                            if (ArchiveParentId == 0) {
                                SQL = "update ccpagecontent set DateArchive=null where (id=" + RecordId + ")";
                                env.core.db.executeNonQuery(SQL);
                            } else {
                                SQL = "update ccpagecontent set ArchiveParentID=null,DateArchive=null,parentid=" + ArchiveParentId + " where (id=" + RecordId + ")";
                                env.core.db.executeNonQuery(SQL);
                                NeedToClearCache = true;
                            }
                            csData.goNext();
                        }
                        csData.close();
                    }
                    //
                    // Clear caches
                    //
                    if (NeedToClearCache) {
                        object emptyData = null;
                        env.core.cache.invalidate("Page Content");
                        env.core.cache.storeObject("PCC", emptyData);
                    }
                }
            } catch (Exception ex) {
                LogControllerX.logError(env.core, ex);
                LogControllerX.logAlarm(env.core, "Housekeep, exception, ex [" + ex + "]");
                throw;
            }
        }
    }
}