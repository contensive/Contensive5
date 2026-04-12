
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using Newtonsoft.Json;
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
                string pauseHint = $" To pause alarm {((cp.User.IsAdmin) ? $"set site property 'Diagnostics Pause Until Date' or [/status?pauseUntil={core.dateTimeNowMockable.AddHours(1)}]." : "login as administrator.")}";
                if (cp.Site.GetDate("Diagnostics pause until date") > core.dateTimeNowMockable) {
                    string pausedMessage = $"ok, diagnostics paused until {cp.Site.GetDate("Diagnostics pause until date")}.{Environment.NewLine}{resultList}";
                    return BuildResponse(cp, core, "ok", pausedMessage, pausedMessage);
                }
                foreach (var addon in DbBaseModel.createList<AddonModel>(core.cpParent, "(diagnostic>0)")) {
                    string testResult = core.addon.execute(addon, new BaseClasses.CPUtilsBaseClass.addonExecuteContext());
                    if (string.IsNullOrWhiteSpace(testResult)) {
                        string errorMsg = $"ERROR, diagnostic [{addon.name}] failed, it returned an empty result.{pauseHint}";
                        return BuildResponse(cp, core, "error", errorMsg, errorMsg);
                    }
                    if (testResult.Length < 2) {
                        string errorMsg = $"ERROR, diagnostic [{addon.name}] failed, it returned an invalid result.{pauseHint}";
                        return BuildResponse(cp, core, "error", errorMsg, errorMsg);
                    }
                    if (testResult.left(2).ToLower(CultureInfo.InvariantCulture) != "ok") {
                        string errorMsg = $"ERROR, diagnostic [{addon.name}] failed, it returned [{testResult}]{pauseHint}";
                        return BuildResponse(cp, core, "error", errorMsg, errorMsg);
                    }
                    resultList.AppendLine(testResult);
                }
                string successMessage = $"ok, all tests passed.{Environment.NewLine}{resultList}";
                return BuildResponse(cp, core, "ok", successMessage, successMessage);
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return "ERROR, unexpected exception during diagnostics";
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Return plain text or JSON depending on the format query parameter
        /// </summary>
        private static string BuildResponse(Contensive.BaseClasses.CPBaseClass cp, CoreController core, string status, string message, string diagnostics) {
            string format = cp.Doc.GetText("format");
            if (!format.Equals("json", StringComparison.OrdinalIgnoreCase)) {
                cp.Response.SetType("text/plain");
                return message;
            }
            //
            // -- return JSON response with performance metrics
            cp.Response.SetType("application/json");
            var metrics = PerformanceMetricsController.GetMetrics(core.appConfig.name);
            var response = new {
                status,
                message,
                metrics = new {
                    avgResponseTimeMs = metrics.AvgResponseTimeMs,
                    avgResponseTime5MinMs = metrics.AvgResponseTime5MinMs,
                    hitCount = metrics.HitCount,
                    hitCount5Min = metrics.HitCount5Min,
                    uptimeMinutes = metrics.UptimeMinutes
                },
                diagnostics
            };
            return JsonConvert.SerializeObject(response);
        }
    }
}
