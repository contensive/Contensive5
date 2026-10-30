
using System;
using NLog;
//
namespace Contensive.Processor.Addons.Tools {
    //
    //====================================================================================================
    /// <summary>
    /// Log File Tool. Two LayoutBuilderList screens: the list of log files and the parsed entries of one file. Admin only.
    /// logFile in the request selects the entries screen, otherwise the file list is shown.
    /// All file access is confined to the log root by LogFilePathController.
    /// </summary>
    public class LogFileToolClass : Contensive.BaseClasses.AddonBaseClass {
        //
        internal const int AdminFormToolLogFileView = 120;
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        /// <summary>
        /// addon method, deliver the html of the file list or of one log file
        /// </summary>
        /// <param name="cpBase"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cpBase) {
            var cp = (CPClass)cpBase;
            try {
                if (!cp.User.IsAdmin) { return "You do not have permission."; }
                if (cp.Doc.IsProperty("logFile")) { return LogEntryListView.getHtml(cp); }
                return LogFileListView.getHtml(cp);
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
        }
    }
}
