﻿
using Contensive.Processor.Controllers;
using System;
//
namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// Housekeep daily tasks
    /// </summary>
    public static class DailyTasksClass {
        /// <summary>
        /// Housekeep daily tasks
        /// </summary>
        /// <param name="core"></param>
        /// <param name="env"></param>
        public static void executeDailyTasks(CoreController core, HouseKeepEnvironmentModel env) {
            try {
                //
                LogController.logInfo(core, "executeDailyTasks");
                //
                // -- Download Updates
                SoftwareUpdatesClass.downloadAndInstall(core);
                //
                // -- Addon folder
                AddonFolderClass.executeDailyTasks(core);
                //
                // -- metadata
                ContentFieldClass.executeDailyTasks(core, env);
                //
                // -- content
                MenuEntryClass.executeDailyTasks(core, env);
                RemoteQueryClass.executeDailyTasks(core, env);
                PageContentClass.executeDailyTasks(core, env);
                AddonContentFieldTypeRuleClass.executeDailyTasks(core, env);
                AddonContentTriggerRuleClass.executeDailyTasks(core, env);
                ContentWatchClass.executeDailyTasks(core, env);
                EmailDropClass.executeDailyTasks(core, env);
                EmailLogClass.executeDailyTasks(core, env);
                FieldHelpClass.executeDailyTasks(core, env);
                GroupRulesClass.executeDailyTasks(core, env);
                MemberRuleClass.executeDailyTasks(core, env);
                MetadataClass.executeDailyTasks(core, env);
                LinkAliasClass.executeDailyTasks(core, env);
                //
                // -- Properties
                UserProperyClass.executeDailyTasks(core);
                VisitPropertyClass.executeDailyTasks(core);
                VisitorPropertyClass.executeDailyTasks(core);
                //
                // -- visits, visitors, viewings
                VisitClass.executeDailyTasks(core, env);
                VisitorClass.executeDailyTasks(core, env);
                ViewingsClass.executeDailyTasks(core, env);
                //
                // -- summary
                VisitSummaryClass.executeDailyTasks(core, env);
                ViewingSummaryClass.executeDailyTasks(core, env);
                //
                // -- logs
                ActivityLogClass.executeDailyTasks(core, env);
                //
                // -- people
                PersonClass.executeDailyTasks(core, env);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
    }
}