﻿
using Contensive.Processor.Controllers;
using NLog;
using System;
//
namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// Housekeep this content
    /// </summary>
    public static class AddonContentTriggerRuleClass {
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
                env.log("Housekeep, executeHourlyTasks, AddonContentTriggerRuleClass");
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
                env.log("HousekeepDaily, addoncontenttrigger rules");
                //
                // -- addon trigger rules
                env.core.db.executeNonQuery("delete from ccAddonContentTriggerRules where id in (select r.id from ccAddonContentTriggerRules r left join ccaggregatefunctions a on a.id = r.addonid where a.Id Is Null)");
                env.core.db.executeNonQuery("delete from ccAddonContentTriggerRules where id in (select r.id from ccAddonContentTriggerRules r left join cccontent c on c.id = r.contentid where c.id is null)");

            } catch (Exception ex) {
                logger.Error(ex, $"{env.core.logCommonMessage}");
                LogController.logAlarm(env.core, "Housekeep, exception, ex [" + ex + "]");
                throw;

            }
        }
    }
}