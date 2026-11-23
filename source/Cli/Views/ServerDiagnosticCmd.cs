
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Contensive.Processor;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using Contensive.Models.Db;
using Newtonsoft.Json;
using Contensive.BaseClasses;

namespace Contensive.CLI {
    static class ServerDiagnosticCmd {
        //
        // ====================================================================================================
        /// <summary>
        /// help text for this command
        /// </summary>
        internal static readonly string helpText = ""
            + Environment.NewLine
            + Environment.NewLine + "--serverdiagnostic"
            + Environment.NewLine + "    Run server diagnostics for all applications (drive space, log files, alarms, Windows updates, domain bindings)"
            + Environment.NewLine + "    Requires elevated permissions (run as administrator)"
            + "";
        //
        // ====================================================================================================
        /// <summary>
        /// Run server diagnostics for all applications.
        /// Checks server-level metrics and saves JSON results to each app's site property.
        /// </summary>
        /// <summary>
        /// Log file path under ProgramData so scheduled-task runs (no console) leave a trace.
        /// </summary>
        private static readonly string logFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Contensive", "Logs", "ServerDiagnostic.log");
        //
        /// <summary>
        /// Write a line to both the console and the diagnostic log file.
        /// </summary>
        private static void log(StreamWriter logWriter, string message) {
            Console.WriteLine(message);
            logWriter?.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {message}");
        }
        //
        public static void execute(CPClass cpServer) {
            StreamWriter logWriter = null;
            try {
                //
                // -- open log file (overwrite each run so it stays small)
                try {
                    Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));
                    logWriter = new StreamWriter(logFilePath, false) { AutoFlush = true };
                } catch (Exception exLog) {
                    Console.WriteLine($"WARNING: Could not open diagnostic log file [{logFilePath}]: {exLog.Message}");
                }
                //
                log(logWriter, "Running server diagnostics for all applications...");
                log(logWriter, $"Running as: {Environment.UserDomainName}\\{Environment.UserName}");
                log(logWriter, "");
                //
                // ===== Step 1: Check Windows Updates (once for the entire server) =====
                var windowsUpdateResult = checkWindowsUpdates();
                log(logWriter, $"Windows Updates: {windowsUpdateResult.message}");
                log(logWriter, "");
                //
                // ===== Step 2: Iterate through all applications =====
                int appCount = 0;
                int successCount = 0;
                int errorCount = 0;
                foreach (var kvp in cpServer.core.serverConfig.apps) {
                    AppConfigModel app = (AppConfigModel)kvp.Value;
                    appCount++;
                    if (!app.enabled) {
                        log(logWriter, $"[{appCount}/{cpServer.core.serverConfig.apps.Count}] Skipping disabled application: {app.name}");
                        log(logWriter, "");
                        continue;
                    }
                    log(logWriter, $"[{appCount}/{cpServer.core.serverConfig.apps.Count}] Processing application: {app.name}");
                    //
                    // -- each app is wrapped so one failure does not stop the others
                    CPClass cp = null;
                    CoreController core = null;
                    try {
                        cp = new CPClass(app.name);
                        core = cp.core;
                    } catch (Exception exApp) {
                        log(logWriter, $"  ERROR: Could not open application [{app.name}]: {exApp.Message}");
                        log(logWriter, $"  Stack: {exApp.StackTrace}");
                        errorCount++;
                        log(logWriter, "");
                        cp?.Dispose();
                        continue;
                    }
                    try {
                        //
                        // -- create status model with defaults (each check is independent)
                        var status = new ServerDiagnosticsStatusModel {
                            lastCheckDate = DateTime.Now,
                            windowsUpdateTitles = windowsUpdateResult.updateTitles,
                            windowsUpdateCount = windowsUpdateResult.updateCount,
                            windowsUpdateCheckSuccessful = windowsUpdateResult.checkSuccessful,
                            windowsUpdateErrorMessage = windowsUpdateResult.errorMessage,
                            domainBindingsValid = true,
                            domainBindingsErrorMessage = "",
                            driveSpaceValid = true,
                            driveSpaceErrorMessage = "",
                            logFilesValid = true,
                            logFilesErrorMessage = "",
                            alarmsValid = true,
                            alarmsErrorMessage = "",
                            tlsValid = true,
                            tlsErrorMessage = "",
                            allChecksSuccessful = false
                        };
                        //
                        // -- each check is wrapped individually so a failure in one does not skip the rest
                        //
                        // -- check drive space (10% free on C: and D:)
                        try {
                            checkDriveSpace(status);
                        } catch (Exception ex) {
                            status.driveSpaceValid = false;
                            status.driveSpaceErrorMessage = $"Unhandled exception: {ex.Message}";
                            log(logWriter, $"  ERROR in driveSpace check: {ex.Message}");
                        }
                        //
                        // -- check log file sizes
                        try {
                            checkLogFiles(core, status);
                        } catch (Exception ex) {
                            status.logFilesValid = false;
                            status.logFilesErrorMessage = $"Unhandled exception: {ex.Message}";
                            log(logWriter, $"  ERROR in logFiles check: {ex.Message}");
                        }
                        //
                        // -- check alarm folder
                        try {
                            checkAlarms(core, status);
                        } catch (Exception ex) {
                            status.alarmsValid = false;
                            status.alarmsErrorMessage = $"Unhandled exception: {ex.Message}";
                            log(logWriter, $"  ERROR in alarms check: {ex.Message}");
                        }
                        //
                        // -- check domain bindings for this app
                        try {
                            status.domainBindingsValid = testDomainBindings(cp, out string domainBindingError);
                            status.domainBindingsErrorMessage = domainBindingError;
                        } catch (Exception ex) {
                            status.domainBindingsValid = false;
                            status.domainBindingsErrorMessage = $"Unhandled exception: {ex.Message}";
                            log(logWriter, $"  ERROR in domainBindings check: {ex.Message}");
                        }
                        //
                        // -- check TLS configuration
                        try {
                            checkTls(status);
                        } catch (Exception ex) {
                            status.tlsValid = false;
                            status.tlsErrorMessage = $"Unhandled exception: {ex.Message}";
                            log(logWriter, $"  ERROR in TLS check: {ex.Message}");
                        }
                        //
                        // -- set overall status
                        status.allChecksSuccessful = status.windowsUpdateCheckSuccessful
                            && status.domainBindingsValid
                            && status.driveSpaceValid
                            && status.logFilesValid
                            && status.alarmsValid
                            && status.tlsValid;
                        //
                        // -- save to site property (even partial results are useful)
                        try {
                            string jsonStatus = JsonConvert.SerializeObject(status);
                            cp.Site.SetProperty("ServerDiagnosticsStatus", jsonStatus);
                        } catch (Exception ex) {
                            log(logWriter, $"  ERROR: Could not save diagnostics to site property: {ex.Message}");
                        }
                        //
                        // -- report results
                        if (status.allChecksSuccessful) {
                            log(logWriter, "  All checks passed");
                            successCount++;
                        } else {
                            log(logWriter, "  Issues found:");
                            if (!status.driveSpaceValid) {
                                log(logWriter, $"    - Drive space: {status.driveSpaceErrorMessage}");
                            }
                            if (!status.logFilesValid) {
                                log(logWriter, $"    - Log files: {status.logFilesErrorMessage}");
                            }
                            if (!status.alarmsValid) {
                                log(logWriter, $"    - Alarms: {status.alarmsErrorMessage}");
                            }
                            if (!status.windowsUpdateCheckSuccessful) {
                                log(logWriter, $"    - Windows updates: {status.windowsUpdateErrorMessage}");
                            }
                            if (!status.domainBindingsValid) {
                                log(logWriter, $"    - Domain bindings: {status.domainBindingsErrorMessage}");
                            }
                            if (!status.tlsValid) {
                                log(logWriter, $"    - TLS: {status.tlsErrorMessage}");
                            }
                            errorCount++;
                        }
                    } catch (Exception ex) {
                        log(logWriter, $"  ERROR processing app [{app.name}]: {ex.Message}");
                        log(logWriter, $"  Stack: {ex.StackTrace}");
                        errorCount++;
                    } finally {
                        cp?.Dispose();
                    }
                    log(logWriter, "");
                }
                //
                // ===== Summary =====
                log(logWriter, "================================================================================");
                log(logWriter, "Server Diagnostics Complete");
                log(logWriter, $"  Total applications: {appCount}");
                log(logWriter, $"  Successful: {successCount}");
                log(logWriter, $"  Errors: {errorCount}");
                log(logWriter, $"  Windows updates pending: {windowsUpdateResult.updateCount}");
                log(logWriter, "================================================================================");
            } catch (Exception ex) {
                string msg = $"ERROR: Unexpected error during server diagnostics: {ex.Message}\n  Stack: {ex.StackTrace}";
                Console.WriteLine(msg);
                logWriter?.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {msg}");
            } finally {
                logWriter?.Dispose();
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Check for 10% free on C: and D: drives
        /// </summary>
        private static void checkDriveSpace(ServerDiagnosticsStatusModel status) {
            try {
                if (Directory.Exists(@"c:\")) {
                    DriveInfo driveTest = new DriveInfo("c");
                    double freeSpace = Math.Round(100.0 * (Convert.ToDouble(driveTest.AvailableFreeSpace) / Convert.ToDouble(driveTest.TotalSize)), 2);
                    if (freeSpace < 10) {
                        status.driveSpaceValid = false;
                        status.driveSpaceErrorMessage = $"Drive-C does not have 10% free (current: {freeSpace}%)";
                        return;
                    }
                }
                if (Directory.Exists(@"d:\")) {
                    DriveInfo driveTest = new DriveInfo("d");
                    double freeSpace = Math.Round(100.0 * (Convert.ToDouble(driveTest.AvailableFreeSpace) / Convert.ToDouble(driveTest.TotalSize)), 2);
                    if (freeSpace < 10) {
                        status.driveSpaceValid = false;
                        status.driveSpaceErrorMessage = $"Drive-D does not have 10% free (current: {freeSpace}%)";
                        return;
                    }
                }
            } catch (Exception ex) {
                status.driveSpaceValid = false;
                status.driveSpaceErrorMessage = $"Failed to check drive space: {ex.Message}";
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Check that all log files are under 1MB
        /// </summary>
        private static void checkLogFiles(CoreController core, ServerDiagnosticsStatusModel status) {
            try {
                if (!core.programDataFiles.pathExists("Logs/")) {
                    core.programDataFiles.createPath("Logs/");
                }
                foreach (var fileDetail in core.programDataFiles.getFileList("Logs/")) {
                    if (fileDetail.Size > 1000000) {
                        status.logFilesValid = false;
                        status.logFilesErrorMessage = $"Log file size error [{fileDetail.Name}], size [{fileDetail.Size}]";
                        return;
                    }
                }
            } catch (Exception ex) {
                status.logFilesValid = false;
                status.logFilesErrorMessage = $"Failed to check log files: {ex.Message}";
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Check that the alarm folder is empty
        /// </summary>
        private static void checkAlarms(CoreController core, ServerDiagnosticsStatusModel status) {
            try {
                if (!core.programDataFiles.pathExists("Alarms/")) {
                    core.programDataFiles.createPath("Alarms/");
                }
                foreach (var alarmFile in core.programDataFiles.getFileList("Alarms/")) {
                    status.alarmsValid = false;
                    status.alarmsErrorMessage = $"Alarm folder is not empty, [{core.programDataFiles.readFileText($"Alarms/{alarmFile.Name}")}]";
                    return;
                }
            } catch (Exception ex) {
                status.alarmsValid = false;
                status.alarmsErrorMessage = $"Failed to check alarms: {ex.Message}";
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Check for Windows updates (server-wide check)
        /// </summary>
        private static WindowsUpdateResult checkWindowsUpdates() {
            var result = new WindowsUpdateResult {
                checkSuccessful = false,
                updateCount = 0,
                updateTitles = new List<string>(),
                errorMessage = "",
                message = ""
            };
            try {
                Type updateSessionType = Type.GetTypeFromProgID("Microsoft.Update.Session");
                if (updateSessionType == null) {
                    result.errorMessage = "Windows Update Agent not available";
                    result.message = "ERROR - Windows Update Agent not available on this system";
                    return result;
                }
                dynamic updateSession = Activator.CreateInstance(updateSessionType);
                dynamic updateSearcher = updateSession.CreateUpdateSearcher();
                dynamic searchResult = updateSearcher.Search("IsInstalled=0 and Type='Software' and IsHidden=0");
                result.updateCount = searchResult.Updates.Count;
                result.checkSuccessful = true;
                //
                // -- collect update titles (limit to 20)
                int maxTitles = Math.Min(20, result.updateCount);
                for (int i = 0; i < maxTitles; i++) {
                    dynamic update = searchResult.Updates[i];
                    result.updateTitles.Add(update.Title);
                    Marshal.ReleaseComObject(update);
                }
                //
                // -- clean up COM objects
                Marshal.ReleaseComObject(searchResult);
                Marshal.ReleaseComObject(updateSearcher);
                Marshal.ReleaseComObject(updateSession);
                if (result.updateCount == 0) {
                    result.message = "OK - No Windows updates pending";
                } else {
                    result.message = $"WARNING - {result.updateCount} Windows update(s) pending";
                }
            } catch (UnauthorizedAccessException uae) {
                result.errorMessage = $"Access denied. This command must be run with administrator privileges: {uae.Message}";
                result.message = $"ERROR - {result.errorMessage}";
            } catch (Exception ex) {
                result.errorMessage = $"Failed to check for updates: {ex.Message}";
                result.message = $"ERROR - {result.errorMessage}";
            }
            return result;
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Return true if the default domain is configured in config.json and is a registered binding in the web server.
        /// </summary>
        private static bool testDomainBindings(CPBaseClass cp, out string returnUserMessage) {
            returnUserMessage = "";
            string appName = cp.GetAppConfig().name;
            List<string> domainList = cp.GetAppConfig().domainList;
            List<DomainModel> domainModelList = DbBaseModel.createList<DomainModel>(cp, "name<>'*'");
            if (domainList.Count == 0) {
                returnUserMessage = $"This application [{appName}] has no default domain configured in config.json.";
                return false;
            }
            if (!WebServerController.isValidBinding(cp, appName, domainList.First(), ref returnUserMessage)) {
                returnUserMessage = $"For application [{appName}], the default domain in config.json [{domainList.First()}] is not a registered binding in the web server.";
                return false;
            }
            foreach (string domain in domainList) {
                if (!WebServerController.isValidBinding(cp, appName, domain, ref returnUserMessage)) {
                    returnUserMessage = $"For application [{appName}], the domain [{domain}] configured in config.json is not a registered binding in the web server.";
                    return false;
                }
                if (!domainModelList.Any(d => d.name.Equals(domain, StringComparison.InvariantCultureIgnoreCase))) {
                    returnUserMessage = $"For application [{appName}], the domain [{domain}] configured in config.json is not configured as a domain in the database.";
                    return false;
                }
            }
            foreach (DomainModel domainModel in domainModelList) {
                if (!WebServerController.isValidBinding(cp, appName, domainModel.name, ref returnUserMessage)) {
                    returnUserMessage = $"The domain [{domainModel.name}] configured in the database for application [{appName}] is not a registered binding in the web server.";
                    return false;
                }
            }
            return true;
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Check TLS configuration in the Windows registry.
        /// Verify TLS 1.2 is enabled and TLS 1.0 and 1.1 are disabled.
        /// </summary>
        private static void checkTls(ServerDiagnosticsStatusModel status) {
            try {
                string basePath = @"SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols";
                var errors = new List<string>();
                //
                // -- verify TLS 1.2 is not explicitly disabled
                using (RegistryKey tls12ServerKey = Registry.LocalMachine.OpenSubKey($@"{basePath}\TLS 1.2\Server")) {
                    if (tls12ServerKey != null) {
                        object enabled = tls12ServerKey.GetValue("Enabled");
                        if (enabled != null && Convert.ToInt32(enabled) == 0) {
                            errors.Add("TLS 1.2 Server is explicitly disabled");
                        }
                        object disabledByDefault = tls12ServerKey.GetValue("DisabledByDefault");
                        if (disabledByDefault != null && Convert.ToInt32(disabledByDefault) == 1) {
                            errors.Add("TLS 1.2 Server is disabled by default");
                        }
                    }
                }
                using (RegistryKey tls12ClientKey = Registry.LocalMachine.OpenSubKey($@"{basePath}\TLS 1.2\Client")) {
                    if (tls12ClientKey != null) {
                        object enabled = tls12ClientKey.GetValue("Enabled");
                        if (enabled != null && Convert.ToInt32(enabled) == 0) {
                            errors.Add("TLS 1.2 Client is explicitly disabled");
                        }
                        object disabledByDefault = tls12ClientKey.GetValue("DisabledByDefault");
                        if (disabledByDefault != null && Convert.ToInt32(disabledByDefault) == 1) {
                            errors.Add("TLS 1.2 Client is disabled by default");
                        }
                    }
                }
                //
                // -- verify TLS 1.0 is disabled
                checkLegacyTlsDisabled(basePath, "TLS 1.0", errors);
                //
                // -- verify TLS 1.1 is disabled
                checkLegacyTlsDisabled(basePath, "TLS 1.1", errors);
                //
                if (errors.Count > 0) {
                    status.tlsValid = false;
                    status.tlsErrorMessage = string.Join("; ", errors);
                }
            } catch (Exception ex) {
                status.tlsValid = false;
                status.tlsErrorMessage = $"Failed to check TLS configuration: {ex.Message}";
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Verify a legacy TLS version (1.0 or 1.1) is disabled in the registry.
        /// If the registry key does not exist, the protocol may still be enabled by the OS default.
        /// </summary>
        private static void checkLegacyTlsDisabled(string basePath, string protocolVersion, List<string> errors) {
            bool serverDisabled = false;
            using (RegistryKey serverKey = Registry.LocalMachine.OpenSubKey($@"{basePath}\{protocolVersion}\Server")) {
                if (serverKey != null) {
                    object enabled = serverKey.GetValue("Enabled");
                    if (enabled != null && Convert.ToInt32(enabled) == 0) {
                        serverDisabled = true;
                    }
                }
            }
            if (!serverDisabled) {
                errors.Add($"{protocolVersion} Server is not explicitly disabled");
            }
            //
            bool clientDisabled = false;
            using (RegistryKey clientKey = Registry.LocalMachine.OpenSubKey($@"{basePath}\{protocolVersion}\Client")) {
                if (clientKey != null) {
                    object enabled = clientKey.GetValue("Enabled");
                    if (enabled != null && Convert.ToInt32(enabled) == 0) {
                        clientDisabled = true;
                    }
                }
            }
            if (!clientDisabled) {
                errors.Add($"{protocolVersion} Client is not explicitly disabled");
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Windows update check result (internal to CLI command)
        /// </summary>
        private class WindowsUpdateResult {
            public bool checkSuccessful { get; set; }
            public int updateCount { get; set; }
            public List<string> updateTitles { get; set; }
            public string errorMessage { get; set; }
            public string message { get; set; }
        }
    }
}
