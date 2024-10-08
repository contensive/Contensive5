﻿
using Contensive.Processor.Controllers;
using NLog;
using System;
//
namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// Housekeep daily tasks
    /// </summary>
    public static class HousekeepDailyTasksClass {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// Housekeep daily tasks
        /// </summary>
        /// <param name="core"></param>
        /// <param name="env"></param>
        public static void executeDailyTasks(HouseKeepEnvironmentModel env) {
            int TimeoutSave = env.core.db.sqlCommandTimeout;
            try {
                env.core.db.sqlCommandTimeout = 1800;
                //
                env.log("executeDailyTasks, start");
                //

                //
                // -- summary (must be first)
                VisitSummaryClass.updateVisitSummary(env);
                ViewingSummaryClass.updateViewingSummary(env);
                //
                // -- people (must be before visits to delete users from bot visits
                PersonClass.executeDailyTasks(env);
                //
                // -- Addon folder
                AddonFolderClass.executeDailyTasks(env);
                //
                // -- metadata
                ContentFieldClass.executeDailyTasks(env);
                //
                // -- check guid on every table
                TablesClass.executeDailyTasks(env);
                //
                // -- content
                MenuEntryClass.executeDailyTasks(env);
                RemoteQueryClass.executeDailyTasks(env);
                PageContentClass.executeDailyTasks(env);
                AddonContentFieldTypeRuleClass.executeDailyTasks(env);
                AddonContentTriggerRuleClass.executeDailyTasks(env);
                ContentWatchClass.executeDailyTasks(env);
                EmailDropClass.executeDailyTasks(env);
                EmailLogClass.executeDailyTasks(env);
                FieldHelpClass.executeDailyTasks(env);
                GroupRulesClass.executeDailyTasks(env);
                MemberRuleClass.executeDailyTasks(env);
                MetadataClass.executeDailyTasks(env);
                LinkAliasClass.executeDailyTasks(env);
                AddonEventCatchersClass.executeDailyTasks(env);
                //
                // -- Properties
                UserProperyClass.executeDailyTasks(env);
                VisitPropertyClass.executeDailyTasks(env);
                VisitorPropertyClass.executeDailyTasks(env);
                //
                // -- visits, visitors, viewings
                VisitClass.executeDailyTasks(env);
                VisitorClass.executeDailyTasks(env);
                ViewingsClass.executeDailyTasks(env);
                //
                // -- verify filenames are valid, a bad filename can block the edit screen
                FilenameTestClass.executeDailyTasks(env);
                //
                // -- site warnings
                //
                // -- logs
                ActivityLogClass.executeDailyTasks(env);
                //
                env.log("executeDailyTasks, done");
            } catch (Exception ex) {
                logger.Error(ex, $"{env.core.logCommonMessage}");
                LogController.logAlarm(env.core, "Housekeep, exception, ex [" + ex + "]");
                throw;
            } finally {
                //
                // -- restore default timeout
                env.core.db.sqlCommandTimeout = TimeoutSave;
            }
        }
    }
}