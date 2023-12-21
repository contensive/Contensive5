
using Contensive.Processor.Controllers;
using System;

namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// Housekeep this content
    /// </summary>
    public static class GroupRulesClass {
        //
        //====================================================================================================
        /// <summary>
        /// execute hourly tasks
        /// </summary>
        /// <param name="core"></param>
        public static void executeHourlyTasks(HouseKeepEnvironmentModel env) {
            try {
                //
                env.log("Housekeep, executeHourlyTasks, GroupRules");
                //
            } catch (Exception ex) {
                LogController.logError(env.core, ex);
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
                env.log("Housekeep, grouprules");
                //
                //
                // GroupRules with bad ContentID
                //   Handled record by record removed to prevent CDEF reload
                //
                env.log("Deleting Group Rules with bad ContentID.");
                string sql = "Select ccGroupRules.ID"
                    + " From ccGroupRules LEFT JOIN ccContent on ccContent.ID=ccGroupRules.ContentID"
                    + " WHERE (ccContent.ID is null)";
                using (var csData = new CsModel(env.core)) {
                    csData.openSql(sql);
                    while (csData.ok()) {
                        MetadataController.deleteContentRecord(env.core, "Group Rules", csData.getInteger("ID"));
                        csData.goNext();
                    }
                }
                //
                // GroupRules with bad GroupID
                //
                env.log("Deleting Group Rules with bad GroupID.");
                sql = "delete ccGroupRules"
                    + " From ccGroupRules"
                    + " LEFT JOIN ccgroups on ccgroups.ID=ccGroupRules.GroupID"
                    + " WHERE (ccgroups.ID is null)";
                env.core.db.executeNonQuery(sql);

            } catch (Exception ex) {
                LogController.logError(env.core, ex);
                LogController.logAlarm(env.core, "Housekeep, exception, ex [" + ex + "]");
                throw;
            }
        }
    }
}