
using System;
using System.Linq;
using Contensive.Processor;
#if NET
using Contensive.Processor.Controllers;
#endif

namespace Contensive.CLI {
    static class CompatibilityCmd {
        //
        // ====================================================================================================
        /// <summary>
        /// help text for this command
        /// </summary>
        internal static readonly string helpText = ""
            + Environment.NewLine
            + Environment.NewLine + "--compatibility (-c)"
            + Environment.NewLine + "    scan addon DLLs for .NET Core compatibility issues"
            + "";
        //
        // ====================================================================================================
        /// <summary>
        /// Scan addon assemblies for .NET Core compatibility and display severity-grouped results.
        /// </summary>
        public static void execute(Contensive.Processor.CPClass cpServer, string appName) {
#if NET
            try {
                var appNames = new System.Collections.Generic.List<string>();
                if (!string.IsNullOrEmpty(appName)) {
                    appNames.Add(appName);
                } else {
                    foreach (var kvp in cpServer.core.serverConfig.apps) {
                        appNames.Add(kvp.Key);
                    }
                }
                if (appNames.Count == 0) {
                    Console.WriteLine("No applications found to scan. Use -a to specify an application.");
                    return;
                }
                foreach (string scanAppName in appNames) {
                    using CPClass scanApp = new CPClass(scanAppName);
                    Console.WriteLine($"Scanning addon assemblies for application [{scanAppName}]...");
                    Console.WriteLine();
                    var issues = AddonCompatibilityScanner.ScanAddonCollections(scanApp.core);
                    if (issues.Count == 0) {
                        Console.WriteLine("No .NET Framework addon DLLs found. All addons are compatible.");
                        Console.WriteLine();
                        continue;
                    }
                    var highIssues = issues.Where(i => i.Severity == AddonCompatibilityScanner.CompatibilitySeverity.High).ToList();
                    var lowIssues = issues.Where(i => i.Severity == AddonCompatibilityScanner.CompatibilitySeverity.Low).ToList();
                    Console.WriteLine($"== Addon compatibility scan: {issues.Count} Framework DLL(s) found ==");
                    //
                    if (highIssues.Count > 0) {
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"  HIGH SEVERITY ({highIssues.Count} DLL(s)) - These reference assemblies not available in .NET Core:");
                        Console.ResetColor();
                        foreach (var issue in highIssues) {
                            Console.WriteLine($"    Collection: {issue.CollectionName}");
                            Console.WriteLine($"    DLL:        {issue.DllFileName} ({issue.TargetFramework})");
                            Console.WriteLine($"    References: {string.Join(", ", issue.UnsupportedReferences)}");
                            Console.WriteLine($"    Action:     Recompile targeting netstandard2.0 or net9.0, replacing unsupported APIs.");
                            Console.WriteLine();
                        }
                    }
                    //
                    if (lowIssues.Count > 0) {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"  LOW SEVERITY ({lowIssues.Count} DLL(s)) - Framework-targeted but likely compatible via shim:");
                        Console.ResetColor();
                        foreach (var issue in lowIssues) {
                            Console.WriteLine($"    Collection: {issue.CollectionName}");
                            Console.WriteLine($"    DLL:        {issue.DllFileName} ({issue.TargetFramework})");
                            Console.WriteLine();
                        }
                    }
                    //
                    Console.WriteLine("  To resolve all issues, recompile addons targeting netstandard2.0 or net9.0.");
                }
            } catch (Exception ex) {
                Console.WriteLine($"Error scanning addon compatibility: {ex.Message}");
            }
#else
            Console.WriteLine("The --compatibility command is only available in .NET Core builds.");
#endif
        }
    }
}
