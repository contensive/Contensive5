
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
        public static void executeHourlyTasks(CoreController core) {
            try {
                //
                LogController.logInfo(core, "executeHourlyTasks");
                //
                // -- summaries - must be first
                VisitSummaryClass.executeHourlyTasks(core);
                ViewingSummaryClass.executeHourlyTasks(core);
                //
                // -- people (before visits because it uses v.bots)
                PersonClass.executeHourlyTasks(core);
                //
                // -- delete temp files
                TempFilesClass.deleteFiles(core);
                //
                // -- Addon folder
                AddonFolderClass.executeHourlyTasks(core);
                //
                // -- metadata
                ContentFieldClass.executeHourlyTasks(core);
                //
                // -- content
                MenuEntryClass.executeHourlyTasks(core);
                RemoteQueryClass.executeHourlyTasks(core);
                PageContentClass.executeHourlyTasks(core);
                AddonContentFieldTypeRuleClass.executeHourlyTasks(core);
                AddonContentTriggerRuleClass.executeHourlyTasks(core);
                ContentWatchClass.executeHourlyTasks(core);
                EmailDropClass.executeHourlyTasks(core);
                EmailLogClass.executeHourlyTasks(core);
                FieldHelpClass.executeHourlyTasks(core);
                GroupRulesClass.executeHourlyTasks(core);
                MemberRuleClass.executeHourlyTasks(core);
                MetadataClass.executeHourlyTasks(core);
                LinkAliasClass.executeHourlyTasks(core);
                AddonEventCatchersClass.executeHourlyTasks(core);
                //
                // -- Properties
                UserProperyClass.executeHourlyTasks(core);
                VisitPropertyClass.executeHourlyTasks(core);
                VisitorPropertyClass.executeHourlyTasks(core);
                //
                // -- visits, visitors, viewings
                VisitClass.executeHourlyTasks(core);
                VisitorClass.executeHourlyTasks(core);
                ViewingsClass.executeHourlyTasks(core);
                //
                // -- logs
                ActivityLogClass.executeHourlyTasks(core);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                LogController.logAlarm(core, "Housekeep, exception, ex [" + ex + "]");
                throw;
            }
        }
    }
}