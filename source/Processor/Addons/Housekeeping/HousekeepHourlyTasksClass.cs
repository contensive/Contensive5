﻿
using Contensive.Processor.Controllers;
using System;

namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// exeute housekeep tasks hourly
    /// </summary>
    public static class HousekeepHourlyTasksClass {
        //
        //====================================================================================================
        /// <summary>
        /// exeute housekeep tasks hourly
        /// </summary>
        /// <param name="core"></param>
        public static void executeHourlyTasks(HouseKeepEnvironmentModel env) {
            try {
                //
                env.log("executeHourlyTasks, start");
                //
                // -- summaries - must be first
                VisitSummaryClass.executeHourlyTasks(env);
                ViewingSummaryClass.executeHourlyTasks(env);
                //
                // -- people (before visits because it uses v.bots)
                PersonClass.executeHourlyTasks(env);
                //
                // -- delete temp files
                TempFilesClass.deleteFiles(env);
                //
                // -- Addon folder
                AddonFolderClass.executeHourlyTasks(env);
                //
                // -- metadata
                ContentFieldClass.executeHourlyTasks(env);
                //
                // -- content
                MenuEntryClass.executeHourlyTasks(env);
                RemoteQueryClass.executeHourlyTasks(env);
                PageContentClass.executeHourlyTasks(env);
                AddonContentFieldTypeRuleClass.executeHourlyTasks(env);
                AddonContentTriggerRuleClass.executeHourlyTasks(env);
                ContentWatchClass.executeHourlyTasks(env);
                EmailDropClass.executeHourlyTasks(env);
                EmailLogClass.executeHourlyTasks(env);
                FieldHelpClass.executeHourlyTasks(env);
                GroupRulesClass.executeHourlyTasks(env);
                MemberRuleClass.executeHourlyTasks(env);
                MetadataClass.executeHourlyTasks(env);
                LinkAliasClass.executeHourlyTasks(env);
                AddonEventCatchersClass.executeHourlyTasks(env);
                //
                // -- Properties
                UserProperyClass.executeHourlyTasks(env);
                VisitPropertyClass.executeHourlyTasks(env);
                VisitorPropertyClass.executeHourlyTasks(env);
                //
                // -- visits, visitors, viewings
                VisitClass.executeHourlyTasks(env);
                VisitorClass.executeHourlyTasks(env);
                ViewingsClass.executeHourlyTasks(env);
                //
                // -- logs
                ActivityLogClass.executeHourlyTasks(env);
                //
                env.log("executeHourlyTasks, done");
            } catch (Exception ex) {
                LogController.logError(env.core, ex);
                LogController.logAlarm(env.core, "Housekeep, exception, ex [" + ex + "]");
                throw;
            }
        }
    }
}