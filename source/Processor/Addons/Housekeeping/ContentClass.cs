
using Contensive.Processor.Controllers;
using NLog;
using System;
using System.Data;

namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// Housekeep this content
    /// </summary>
    public static class ContentClass {
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
                env.log("Housekeep, executeHourlyTasks, Content");
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
                env.log("HousekeepDaily, content fields");
                //
                env.log("Detect content records with duplicate names.");
                string sql = "select a.name, a.id as aId, b.id as bId from cccontent a, cccontent b where a.id<b.id and a.name=b.name";
                using ( DataTable dt = env.core.db.executeQuery(sql)) {
                    if(dt.Rows.Count>0) {
                        foreach (DataRow dr in dt.Rows) {
                            int aid = GenericController.getInteger(dr["aId"]);
                            int bid = GenericController.getInteger(dr["bId"]);
                            string name = GenericController.getText(dr["name"]);
                            env.log($"Duplicate content name [{name}] ids [{aid}] and [{bid}]");
                        }
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{env.core.logCommonMessage}");
                LogController.logAlarm(env.core, "Housekeep, exception, ex [" + ex + "]");
                throw;

            }
        }
    }
}