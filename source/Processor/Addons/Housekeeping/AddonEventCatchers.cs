
using Contensive.Processor.Controllers;
using System;
//
namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// Housekeep this content
    /// </summary>
    public static class AddonEventCatchersClass {
        //
        //====================================================================================================
        /// <summary>
        /// execute hourly tasks
        /// </summary>
        /// <param name="core"></param>
        public static void executeHourlyTasks(HouseKeepEnvironmentModel env) {
            try {
                //
                env.log("Housekeep, ccAddonEventCatchers");
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
                env.log("HousekeepDaily, ccAddonEventCatchers");
                //
                // -- addon trigger rules
                env.core.db.executeNonQuery("delete from ccAddonEventCatchers from ccAddonEventCatchers c left join ccAggregateFunctions a on a.id=c.addonId where a.id is null");
            } catch (Exception ex) {
                LogController.logError(env.core, ex);
                LogController.logAlarm(env.core, "Housekeep, exception, ex [" + ex + "]");
                throw;

            }
        }
    }
}