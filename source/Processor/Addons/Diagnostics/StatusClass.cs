
using Contensive.Models.Db;
using System;
using System.Globalization;
using System.Text;
//
namespace Contensive.Processor.Addons.Diagnostics {
    /// <summary>
    /// Run system diagnostics, including both the internal diagnostic class, and every addon with [] diagnostic set
    /// If every method returns the first two characters "OK", then the first two characters are OK,
    /// else the failing test is output and the status message should not include the characters (without "OK")
    /// </summary>
    public class StatusClass : Contensive.BaseClasses.AddonBaseClass {
        //
        private const string asdf = "";
        //
        //====================================================================================================
        /// <summary>
        /// Returns OK on success
        /// + available drive space
        /// + log size
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cp) {
            try {
                var resultList = new StringBuilder();
                var core = ((CPClass)(cp)).core;
                string pauseHint = " To pause alarm " + ((cp.User.IsAdmin) ? "set site property 'Diagnostics Pause Until Date' or [/status?pauseUntil=" + core.dateTimeNowMockable.AddHours(1) + "]." : "login as administrator.");
                cp.Response.SetType("text/plain");
                if (cp.Site.GetDate("Diagnostics pause until date") > core.dateTimeNowMockable) {
                    return "ok, diagnostics paused until " + cp.Site.GetDate("Diagnostics pause until date") + "." + Environment.NewLine + resultList.ToString();
                }
                foreach (var addon in DbBaseModel.createList<AddonModel>(core.cpParent, "(diagnostic>0)")) {
                    string testResult = core.addon.execute(addon, new BaseClasses.CPUtilsBaseClass.addonExecuteContext());
                    if (string.IsNullOrWhiteSpace(testResult)) { return "ERROR, diagnostic [" + addon.name + "] failed, it returned an empty result." + pauseHint; }
                    if (testResult.Length < 2) { return "ERROR, diagnostic [" + addon.name + "] failed, it returned an invalid result." + pauseHint; }
                    if (testResult.left(2).ToLower(CultureInfo.InvariantCulture) != "ok") { return "ERROR, diagnostic [" + addon.name + "] failed, it returned [" + testResult + "]" + pauseHint; }
                    resultList.AppendLine(testResult);
                }
                return "ok, all tests passed." + Environment.NewLine + resultList.ToString();
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return "ERROR, unexpected exception during diagnostics";
            }
        }
    }
}
