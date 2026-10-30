
using System;
using NLog;
//
namespace Contensive.Processor.Addons.Tools {
    //
    //====================================================================================================
    /// <summary>
    /// Ajax callback addon for the log entry view of the Log File Tool. 
    /// LayoutBuilderList posts search, sort and paging to /LayoutBuilderCallback with the callbackAddonGuid, which runs this addon. 
    /// Hidden addon, it has no admin nav entry. It uses its own guid so the log view and the file list keep separate search and sort values.
    /// </summary>
    public class LogFileToolEntriesClass : Contensive.BaseClasses.AddonBaseClass {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        /// <summary>
        /// addon method, deliver the html of the log entry list. Admin only.
        /// </summary>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cpBase) {
            var cp = (CPClass)cpBase;
            try {
                if (!cp.User.IsAdmin) { return "You do not have permission."; }
                return LogEntryListView.getHtml(cp);
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
        }
    }
}
