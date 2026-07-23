
using Contensive.BaseClasses;
using Contensive.BaseModels;
using Contensive.Models.Db;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
//
namespace Contensive.Addons.Status {
    /// <summary>
    /// Run system diagnostics, including both the internal diagnostic class, and every addon with diagnostic set.
    /// If every method returns the first two characters "OK", then the first two characters are OK,
    /// else the failing test is output and the status message should not include the characters (without "OK")
    /// </summary>
    public class StatusRemoteMethod : AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// Returns OK on success
        /// + available drive space
        /// + log size
        /// </summary>
        public override object Execute(CPBaseClass cp) {
            int hint = 0;
            try {
                var resultList = new StringBuilder();
                bool showDetail = cp.Site.GetBoolean("Status Endpoint Detail", true);
                hint = 10;
                if (cp.Site.GetDate("Diagnostics pause until date") > DateTime.Now) {
                    string pausedMessage = showDetail
                        ? $"ok, diagnostics paused until {cp.Site.GetDate("Diagnostics pause until date")}.{Environment.NewLine}{resultList}"
                        : "ok, diagnostics paused.";
                    return BuildResponse(cp, "ok", pausedMessage, showDetail);
                }
                hint = 20;
                foreach (var addon in DbBaseModel.createList<AddonModel>(cp, "(diagnostic>0)")) {
                    hint = 30;
                    string testResult = cp.Addon.Execute(addon.ccguid);
                    if (testResult.Length < 2) {
                        string errorMsg = showDetail
                            ? $"ERROR, diagnostic [{addon.name}] failed, it returned an invalid result."
                            : "ERROR, a diagnostic check failed.";
                        return BuildResponse(cp, "error", errorMsg, showDetail);
                    }
                    if (!testResult.Substring(0, 2).Equals("ok", StringComparison.OrdinalIgnoreCase)) {
                        string errorMsg = showDetail
                            ? $"ERROR, diagnostic [{addon.name}] failed, it returned [{testResult}]"
                            : "ERROR, a diagnostic check failed.";
                        return BuildResponse(cp, "error", errorMsg, showDetail);
                    }
                    resultList.AppendLine($"{testResult}, {addon.name}");
                }
                hint = 50;
                //
                // -- include the server diagnostic summary,
                // which is collected by the internal ServerDiagnosticsController and stored in the site property "ServerDiagnosticsStatus".
                // This will include any failed checks, such as drive space, log file size, domain bindings, alarms, and windows update status.
                //
                string diagnosticDetail = "";
                if (!GetServerDiagnosticsSummary(cp, ref diagnosticDetail)) {
                    string errorMsg = showDetail
                        ? $"ERROR, {diagnosticDetail}."
                        : "ERROR, server diagnostics failed.";
                    return BuildResponse(cp, "error", errorMsg, showDetail);
                }
                hint = 60;
                string successMessage = showDetail
                    ? $"ok, all tests passed.{Environment.NewLine}{resultList}{diagnosticDetail}"
                    : "ok, all tests passed.";
                return BuildResponse(cp, "ok", successMessage, showDetail);
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex, $"Diagnostics hint: {hint}");
                return "ERROR, unexpected exception during diagnostics.";
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Read the ServerDiagnosticsStatus site property and return a summary of all failed checks.
        /// Returns empty string if there are no fails
        /// </summary>
        private static bool GetServerDiagnosticsSummary(CPBaseClass cp, ref string diagnosticDetail) {
            int hint = 0;
            try {
                string json = cp.Site.GetText("ServerDiagnosticsStatus");
                if (string.IsNullOrEmpty(json)) {
                    diagnosticDetail = "ERROR: Server diagnostic status is unavailable. Verify the server task scheduler is running [cc.exe --serverDiagnostics] as administrator every hour.";
                    return false;
                }
                var status = JsonConvert.DeserializeObject<ServerDiagnosticsStatusModel>(json);
                if (status == null) {
                    diagnosticDetail = $"ERROR: Server diagnostic status is not valid {status}";
                    return false;
                }
                if (status.driveSpaceValid) {
                    diagnosticDetail += Environment.NewLine + "ok, drive space check passed.";
                } else {
                    diagnosticDetail += Environment.NewLine + status.driveSpaceErrorMessage;
                    return false;
                }
                if (string.IsNullOrEmpty(status.driveSpaceErrorMessage)) {
                    diagnosticDetail += Environment.NewLine + "ok, drive space check passed.";
                } else {
                    diagnosticDetail += Environment.NewLine + status.driveSpaceErrorMessage;
                    return false;
                }
                if (status.logFilesValid) {
                    diagnosticDetail += Environment.NewLine + "ok, log file check passed.";
                } else {
                    diagnosticDetail += Environment.NewLine + status.logFilesErrorMessage;
                    return false;
                }
                if (status.alarmsValid) {
                    diagnosticDetail += Environment.NewLine + "ok, alarms check passed.";
                } else {
                    diagnosticDetail += Environment.NewLine + status.alarmsErrorMessage;
                    return false;
                }
                if (status.domainBindingsValid) {
                    diagnosticDetail += Environment.NewLine + "ok, bindings check passed.";
                } else {
                    diagnosticDetail += Environment.NewLine + status.domainBindingsErrorMessage;
                    return false;
                }
                if (!status.windowsUpdateCheckSuccessful) {
                    diagnosticDetail += Environment.NewLine + $"warning, Windows update check failed: {status.windowsUpdateErrorMessage}";
                } else if (status.windowsUpdateCount > 0) {
                    diagnosticDetail += Environment.NewLine + $"warning, {status.windowsUpdateCount} Windows update(s) pending (last checked: {status.lastCheckDate:yyyy-MM-dd HH:mm})";
                } else {
                    diagnosticDetail += Environment.NewLine + $"ok, no Windows updates pending (last checked: {status.lastCheckDate:yyyy-MM-dd HH:mm})";
                }
                if (status.tlsValid) {
                    diagnosticDetail += Environment.NewLine + "ok, TLS check passed.";
                } else {
                    diagnosticDetail += Environment.NewLine + (string.IsNullOrEmpty(status.tlsErrorMessage) ? "ERROR, TLS check failed." : status.tlsErrorMessage);
                    return false;
                }
                return true;
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex, $"Diagnostics hint: {hint}");
                diagnosticDetail += Environment.NewLine + "ERROR: Exception while reading server diagnostics status.";
                return false;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Return plain text or JSON depending on the format query parameter
        /// </summary>
        private static string BuildResponse(CPBaseClass cp, string status, string message, bool showDetail) {
            string version = cp.Version;
            string format = cp.Doc.GetText("format");
            if (!format.Equals("json", StringComparison.OrdinalIgnoreCase)) {
                cp.Response.SetType("text/plain");
                string textResult = $"{message}{Environment.NewLine}contensive v{version}";
                textResult = textResult.ToLower(CultureInfo.InvariantCulture);
                textResult = textResult.Replace("error", "ERROR");
                textResult = Regex.Replace(textResult, @"(\r?\n){2,}", Environment.NewLine);
                return textResult;
            }
            //
            // -- return JSON response with performance metrics
            cp.Response.SetType("application/json");
            StatusResponseModel.StatusMetricsModel metricsModel = null;
            StatusResponseModel.StatusWindowsUpdatesModel windowsUpdates = null;
            StatusResponseModel.StatusSecurityModel security = null;
            StatusResponseModel.StatusPerformanceModel performance = null;
            StatusResponseModel.StatusReliabilityModel reliability = null;
            if (showDetail) {
                //
                // -- security/update-hygiene info (Site Monitor roadmap Phase 1). Informational only --
                // does not affect status/statusOk above, which stays reserved for uptime.
                security = SecurityDiagnosticsController.GetSecurityInfo(cp);
                //
                // -- availability/resource-headroom info (Site Monitor roadmap Phase 2). Informational only,
                // same as security above -- does not affect status/statusOk.
                performance = PerformanceDiagnosticsController.GetPerformanceInfo(cp);
                //
                // -- backup/reliability info (Site Monitor roadmap Phase 3, items 2-4). Informational only,
                // same as security/performance above -- does not affect status/statusOk.
                reliability = ReliabilityDiagnosticsController.GetReliabilityInfo(cp);
                //
                // -- read performance metrics from site property (written periodically by the Processor)
                metricsModel = GetMetricsFromSiteProperty(cp);
                //
                // -- include windows update status in JSON response
                try {
                    string serverDiagJson = cp.Site.GetText("ServerDiagnosticsStatus");
                    if (!string.IsNullOrEmpty(serverDiagJson)) {
                        var serverDiag = JsonConvert.DeserializeObject<ServerDiagnosticsStatusModel>(serverDiagJson);
                        if (serverDiag != null) {
                            bool checkSuccessful = serverDiag.windowsUpdateCheckSuccessful;
                            string errorMessage = serverDiag.windowsUpdateErrorMessage ?? "";
                            //
                            // -- Mirror the 7-day staleness threshold from the plain-text path.
                            // A dead scheduled task could leave months-old data with no indication.
                            TimeSpan timeSinceCheck = DateTime.Now - serverDiag.lastCheckDate;
                            if (checkSuccessful && timeSinceCheck.TotalDays > 7) {
                                checkSuccessful = false;
                                errorMessage = $"Last successful Windows update check was {timeSinceCheck.TotalDays:F0} days ago (last checked {serverDiag.lastCheckDate:yyyy-MM-dd HH:mm}) -- the server's scheduled diagnostics task may not be running.";
                            }
                            windowsUpdates = new StatusResponseModel.StatusWindowsUpdatesModel {
                                updatesAvailable = serverDiag.windowsUpdateCount > 0,
                                updateCount = serverDiag.windowsUpdateCount,
                                updateTitles = serverDiag.windowsUpdateTitles,
                                lastChecked = serverDiag.lastCheckDate,
                                checkSuccessful = checkSuccessful,
                                errorMessage = errorMessage
                            };
                        }
                    }
                } catch (Exception) {
                    // -- if we can't read the server diagnostics, leave windowsUpdates null
                }
            }
            var response = new StatusResponseModel {
                version = version,
                status = status,
                message = Regex.Replace(message, @"(\r?\n){2,}", Environment.NewLine).Trim(),
                metrics = metricsModel,
                windowsUpdates = windowsUpdates,
                security = security,
                performance = performance,
                reliability = reliability
            };
            return JsonConvert.SerializeObject(response);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Read the performance metrics from the site property written periodically by the Processor.
        /// Returns null if the property is missing or invalid.
        /// </summary>
        private static StatusResponseModel.StatusMetricsModel GetMetricsFromSiteProperty(CPBaseClass cp) {
            try {
                string json = cp.Site.GetText("PerformanceMetricsStatus");
                if (string.IsNullOrEmpty(json)) { return null; }
                return JsonConvert.DeserializeObject<StatusResponseModel.StatusMetricsModel>(json);
            } catch (Exception) {
                return null;
            }
        }
    }
}
