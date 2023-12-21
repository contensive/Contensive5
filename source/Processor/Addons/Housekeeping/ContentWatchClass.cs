//
using Contensive.Processor.Controllers;
using System;
//
namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// Housekeep this content
    /// </summary>
    public static class ContentWatchClass {
        //
        //====================================================================================================
        /// <summary>
        /// execute hourly tasks
        /// </summary>
        /// <param name="core"></param>
        public static void executeHourlyTasks(HouseKeepEnvironmentModel env) {
            try {
                //
                env.log("Housekeep, executeHourlyTasks, ContentWatch");
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
                env.log("HousekeepDaily, contentwatch");
                //
                using var csData = new CsModel(env.core); 
                string sql = "select cccontentwatch.id from cccontentwatch left join cccontent on cccontent.id=cccontentwatch.contentid  where (cccontent.id is null)or(cccontent.active=0)or(cccontent.active is null)";
                csData.openSql(sql);
                while (csData.ok()) {
                    MetadataController.deleteContentRecord(env.core, "Content Watch", csData.getInteger("ID"));
                    csData.goNext();
                }

            } catch (Exception ex) {
                LogControllerX.logError(env.core, ex);
                LogControllerX.logAlarm(env.core, "Housekeep, exception, ex [" + ex + "]");
                throw;

            }
        }
    }
}