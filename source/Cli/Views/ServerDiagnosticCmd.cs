
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
        public static void execute(CPClass cpServer) {
            try {
                Console.WriteLine("Running server diagnostics for all applications...");
                Console.WriteLine("");
                //
                // ===== Step 1: Check Windows Updates (once for the entire server) =====
                var windowsUpdateResult = checkWindowsUpdates();
                Console.WriteLine($"Windows Updates: {windowsUpdateResult.message}");
                Console.WriteLine("");
                //
                // ===== Step 2: Iterate through all applications =====
                int appCount = 0;
                int successCount = 0;
                int errorCount = 0;
                foreach (var kvp in cpServer.core.serverConfig.apps) {
                    AppConfigModel app = (AppConfigModel)kvp.Value;
                    appCount++;
                    if (!app.enabled) {
                        Console.WriteLine($"[{appCount}/{cpServer.core.serverConfig.apps.Count}] Skipping disabled application: {app.name}");
                        Console.WriteLine("");
                        continue;
                    }
                    Console.WriteLine($"[{appCount}/{cpServer.core.serverConfig.apps.Count}] Processing application: {app.name}");
                    try {
                        using (CPClass cp = new CPClass(app.name)) {
                            var core = cp.core;
                            //
                            // -- create status model
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
                            // -- check drive space (10% free on C: and D:)
                            checkDriveSpace(status);
                            //
                            // -- check log file sizes
                            checkLogFiles(core, status);
                            //
                            // -- check alarm folder
                            checkAlarms(core, status);
                            //
                            // -- check domain bindings for this app
                            status.domainBindingsValid = testDomainBindings(cp, out string domainBindingError);
                            status.domainBindingsErrorMessage = domainBindingError;
                            //
                            // -- check TLS configuration
                            checkTls(status);
                            //
                            // -- set overall status
                            status.allChecksSuccessful = status.windowsUpdateCheckSuccessful
                                && status.domainBindingsValid
                                && status.driveSpaceValid
                                && status.logFilesValid
                                && status.alarmsValid
                                && status.tlsValid;
                            //
                            // -- save to site property
                            string jsonStatus = JsonConvert.SerializeObject(status);
                            cp.Site.SetProperty("ServerDiagnosticsStatus", jsonStatus);
                            //
                            // -- report results
                            if (status.allChecksSuccessful) {
                                Console.WriteLine($"  All checks passed");
                                successCount++;
                            } else {
                                Console.WriteLine($"  Issues found:");
                                if (!status.driveSpaceValid) {
                                    Console.WriteLine($"    - Drive space: {status.driveSpaceErrorMessage}");
                                }
                                if (!status.logFilesValid) {
                                    Console.WriteLine($"    - Log files: {status.logFilesErrorMessage}");
                                }
                                if (!status.alarmsValid) {
                                    Console.WriteLine($"    - Alarms: {status.alarmsErrorMessage}");
                                }
                                if (!status.windowsUpdateCheckSuccessful) {
                                    Console.WriteLine($"    - Windows updates: {status.windowsUpdateErrorMessage}");
                                }
                                if (!status.domainBindingsValid) {
                                    Console.WriteLine($"    - Domain bindings: {status.domainBindingsErrorMessage}");
                                }
                                if (!status.tlsValid) {
                                    Console.WriteLine($"    - TLS: {status.tlsErrorMessage}");
                                }
                                errorCount++;
                            }
                        }
                    } catch (Exception ex) {
                        Console.WriteLine($"  Error processing app: {ex.Message}");
                        errorCount++;
                    }
                    Console.WriteLine("");
                }
                //
                // ===== Summary =====
                Console.WriteLine("================================================================================");
                Console.WriteLine($"Server Diagnostics Complete");
                Console.WriteLine($"  Total applications: {appCount}");
                Console.WriteLine($"  Successful: {successCount}");
                Console.WriteLine($"  Errors: {errorCount}");
                Console.WriteLine($"  Windows updates pending: {windowsUpdateResult.updateCount}");
                Console.WriteLine("================================================================================");
            } catch (Exception ex) {
                Console.WriteLine($"ERROR: Unexpected error during server diagnostics: {ex.Message}");
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
