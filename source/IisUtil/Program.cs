
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Contensive.IisUtil {
    /// <summary>
    /// Standalone IIS management utility that uses appcmd.exe for all IIS operations.
    /// This avoids the Microsoft.Web.Administration NuGet package and its COM interop
    /// dependency, which fails on Windows Server 2025 without Web-Scripting-Tools.
    ///
    /// Called from Processor and CLI via Process.Start().
    ///
    /// Exit codes:
    ///   0 = success
    ///   1 = invalid arguments
    ///   2 = operation failed
    /// </summary>
    internal class Program {
        private static readonly string AppCmdPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            @"inetsrv\appcmd.exe"
        );
        //
        static int Main(string[] args) {
            if (args.Length == 0) {
                PrintUsage();
                return 1;
            }
            if (!File.Exists(AppCmdPath)) {
                Console.Error.WriteLine($"appcmd.exe not found at [{AppCmdPath}]. IIS must be installed.");
                return 2;
            }
            string command = args[0].ToLowerInvariant();
            try {
                switch (command) {
                    case "recycle":
                        return CmdRecycle(args);
                    case "verify-site":
                        return CmdVerifySite(args);
                    case "verify-apppool":
                        return CmdVerifyAppPool(args);
                    case "stop-apppool":
                        return CmdStopAppPool(args);
                    case "delete-apppool":
                        return CmdDeleteAppPool(args);
                    case "delete-site":
                        return CmdDeleteSite(args);
                    case "verify-binding":
                        return CmdVerifyBinding(args);
                    case "is-valid-binding":
                        return CmdIsValidBinding(args);
                    case "verify-cdn-vdir":
                        return CmdVerifyCdnVdir(args);
                    case "sync-ip-blocks":
                        return CmdSyncIpBlocks(args);
                    default:
                        Console.Error.WriteLine($"Unknown command: {command}");
                        PrintUsage();
                        return 1;
                }
            } catch (UnauthorizedAccessException ex) {
                Console.Error.WriteLine($"Access denied. Run as administrator. {ex.Message}");
                return 2;
            } catch (Exception ex) {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 2;
            }
        }
        //
        static void PrintUsage() {
            Console.WriteLine("Usage: iisutil <command> [options]");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  recycle --name <appName>");
            Console.WriteLine("  verify-site --name <appName> --domain <domain> --path <wwwRootPath> [--framework]");
            Console.WriteLine("  verify-apppool --name <poolName> [--framework]");
            Console.WriteLine("  stop-apppool --name <poolName>");
            Console.WriteLine("  delete-apppool --name <poolName>");
            Console.WriteLine("  delete-site --name <appName>");
            Console.WriteLine("  verify-binding --name <appName> --domain <domain>");
            Console.WriteLine("  is-valid-binding --name <appName> --domain <domain>");
            Console.WriteLine("  verify-cdn-vdir --name <appName> --cdn-prefix <prefix> --physical-path <path>");
            Console.WriteLine("  sync-ip-blocks --name <siteName> --ip <addr> [--ip <addr> ...]");
        }
        //
        // ====================================================================================================
        // Argument helpers
        // ====================================================================================================
        //
        static string GetArg(string[] args, string flag) {
            for (int i = 1; i < args.Length - 1; i++) {
                if (args[i].Equals(flag, StringComparison.OrdinalIgnoreCase)) {
                    return args[i + 1];
                }
            }
            return null;
        }
        //
        static bool HasFlag(string[] args, string flag) {
            for (int i = 1; i < args.Length; i++) {
                if (args[i].Equals(flag, StringComparison.OrdinalIgnoreCase)) {
                    return true;
                }
            }
            return false;
        }
        //
        static List<string> GetAllArgs(string[] args, string flag) {
            var result = new List<string>();
            for (int i = 1; i < args.Length - 1; i++) {
                if (args[i].Equals(flag, StringComparison.OrdinalIgnoreCase)) {
                    result.Add(args[i + 1]);
                }
            }
            return result;
        }
        //
        // ====================================================================================================
        // appcmd.exe runner
        // ====================================================================================================
        //
        /// <summary>
        /// Run appcmd.exe with the given arguments. Returns (exitCode, stdout, stderr).
        /// </summary>
        static (int exitCode, string stdout, string stderr) RunAppCmd(string arguments) {
            using var process = new Process();
            process.StartInfo.FileName = AppCmdPath;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();
            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            process.WaitForExit(300000);
            return (process.ExitCode, stdout.Trim(), stderr.Trim());
        }
        //
        /// <summary>
        /// Run appcmd.exe and throw on non-zero exit code.
        /// </summary>
        static string RunAppCmdOrThrow(string arguments) {
            var (exitCode, stdout, stderr) = RunAppCmd(arguments);
            if (exitCode != 0) {
                string message = !string.IsNullOrEmpty(stderr) ? stderr : stdout;
                throw new InvalidOperationException($"appcmd exited with code {exitCode}: {message}");
            }
            return stdout;
        }
        //
        // ====================================================================================================
        // Query helpers
        // ====================================================================================================
        //
        static bool AppPoolExists(string poolName) {
            var (exitCode, stdout, _) = RunAppCmd($"list apppool /name:\"{poolName}\"");
            return exitCode == 0 && !string.IsNullOrEmpty(stdout);
        }
        //
        static bool SiteExists(string siteName) {
            var (exitCode, stdout, _) = RunAppCmd($"list site /name:\"{siteName}\"");
            return exitCode == 0 && !string.IsNullOrEmpty(stdout);
        }
        //
        static bool BindingExists(string siteName, string bindingInfo, string protocol) {
            var (exitCode, stdout, _) = RunAppCmd($"list site /name:\"{siteName}\" /text:bindings");
            if (exitCode != 0 || string.IsNullOrEmpty(stdout)) { return false; }
            string target = $"{protocol}/{bindingInfo}";
            return stdout.IndexOf(target, StringComparison.OrdinalIgnoreCase) >= 0;
        }
        //
        static bool VirtualDirectoryExists(string siteName, string vdirPath) {
            var (exitCode, stdout, _) = RunAppCmd($"list vdir /app.name:\"{siteName}/\" /path:\"{vdirPath}\"");
            return exitCode == 0 && !string.IsNullOrEmpty(stdout);
        }
        //
        // ====================================================================================================
        // recycle --name <appName>
        // ====================================================================================================
        //
        static int CmdRecycle(string[] args) {
            string appName = GetArg(args, "--name");
            if (string.IsNullOrEmpty(appName)) {
                Console.Error.WriteLine("recycle requires --name");
                return 1;
            }
            if (!AppPoolExists(appName)) {
                Console.Error.WriteLine($"App pool [{appName}] not found");
                return 2;
            }
            RunAppCmdOrThrow($"recycle apppool /apppool.name:\"{appName}\"");
            Console.WriteLine($"Recycled app pool [{appName}]");
            return 0;
        }
        //
        // ====================================================================================================
        // verify-site --name <appName> --domain <domain> --path <wwwRootPath> [--framework]
        // ====================================================================================================
        //
        static int CmdVerifySite(string[] args) {
            string appName = GetArg(args, "--name");
            string domain = GetArg(args, "--domain");
            string path = GetArg(args, "--path");
            bool isFramework = HasFlag(args, "--framework");
            if (string.IsNullOrEmpty(appName) || string.IsNullOrEmpty(domain) || string.IsNullOrEmpty(path)) {
                Console.Error.WriteLine("verify-site requires --name, --domain, --path");
                return 1;
            }
            VerifyAppPool(appName, isFramework);
            VerifyWebsite(appName, domain, path, appName);
            Console.WriteLine($"Verified site [{appName}]");
            return 0;
        }
        //
        // ====================================================================================================
        // verify-apppool --name <poolName> [--framework]
        // ====================================================================================================
        //
        static int CmdVerifyAppPool(string[] args) {
            string poolName = GetArg(args, "--name");
            bool isFramework = HasFlag(args, "--framework");
            if (string.IsNullOrEmpty(poolName)) {
                Console.Error.WriteLine("verify-apppool requires --name");
                return 1;
            }
            VerifyAppPool(poolName, isFramework);
            Console.WriteLine($"Verified app pool [{poolName}]");
            return 0;
        }
        //
        // ====================================================================================================
        // stop-apppool --name <poolName>
        // ====================================================================================================
        //
        static int CmdStopAppPool(string[] args) {
            string poolName = GetArg(args, "--name");
            if (string.IsNullOrEmpty(poolName)) {
                Console.Error.WriteLine("stop-apppool requires --name");
                return 1;
            }
            if (!AppPoolExists(poolName)) {
                Console.WriteLine($"App pool [{poolName}] not found, nothing to stop");
                return 0;
            }
            // appcmd stop will return an error if already stopped; that's OK
            var (exitCode, stdout, stderr) = RunAppCmd($"stop apppool /apppool.name:\"{poolName}\"");
            if (exitCode != 0) {
                // already stopped is not an error for our purposes
                if (stdout.IndexOf("already stopped", StringComparison.OrdinalIgnoreCase) >= 0
                    || stderr.IndexOf("already stopped", StringComparison.OrdinalIgnoreCase) >= 0) {
                    Console.WriteLine($"App pool [{poolName}] already stopped");
                    return 0;
                }
                string message = !string.IsNullOrEmpty(stderr) ? stderr : stdout;
                throw new InvalidOperationException($"appcmd exited with code {exitCode}: {message}");
            }
            Console.WriteLine($"Stopped app pool [{poolName}]");
            return 0;
        }
        //
        // ====================================================================================================
        // delete-apppool --name <poolName>
        // ====================================================================================================
        //
        static int CmdDeleteAppPool(string[] args) {
            string poolName = GetArg(args, "--name");
            if (string.IsNullOrEmpty(poolName)) {
                Console.Error.WriteLine("delete-apppool requires --name");
                return 1;
            }
            if (!AppPoolExists(poolName)) {
                Console.WriteLine($"App pool [{poolName}] not found, nothing to delete");
                return 0;
            }
            RunAppCmdOrThrow($"delete apppool /apppool.name:\"{poolName}\"");
            Console.WriteLine($"Deleted app pool [{poolName}]");
            return 0;
        }
        //
        // ====================================================================================================
        // delete-site --name <appName>
        // ====================================================================================================
        //
        static int CmdDeleteSite(string[] args) {
            string appName = GetArg(args, "--name");
            if (string.IsNullOrEmpty(appName)) {
                Console.Error.WriteLine("delete-site requires --name");
                return 1;
            }
            if (!SiteExists(appName)) {
                Console.WriteLine($"Site [{appName}] not found, nothing to delete");
                return 0;
            }
            RunAppCmdOrThrow($"delete site /site.name:\"{appName}\"");
            Console.WriteLine($"Deleted site [{appName}]");
            return 0;
        }
        //
        // ====================================================================================================
        // verify-binding --name <appName> --domain <domain>
        // ====================================================================================================
        //
        static int CmdVerifyBinding(string[] args) {
            string appName = GetArg(args, "--name");
            string domain = GetArg(args, "--domain");
            if (string.IsNullOrEmpty(appName) || string.IsNullOrEmpty(domain)) {
                Console.Error.WriteLine("verify-binding requires --name, --domain");
                return 1;
            }
            if (!SiteExists(appName)) {
                Console.Error.WriteLine($"Site [{appName}] not found");
                return 2;
            }
            string bindingInfo = $"*:80:{domain}";
            if (!BindingExists(appName, bindingInfo, "http")) {
                RunAppCmdOrThrow($"set site /site.name:\"{appName}\" /+bindings.[protocol='http',bindingInformation='{bindingInfo}']");
            }
            Console.WriteLine($"Verified binding [{domain}] on site [{appName}]");
            return 0;
        }
        //
        // ====================================================================================================
        // is-valid-binding --name <appName> --domain <domain>
        // Returns exit code 0 if valid, 3 if not valid (with message on stdout)
        // ====================================================================================================
        //
        static int CmdIsValidBinding(string[] args) {
            string appName = GetArg(args, "--name");
            string domain = GetArg(args, "--domain");
            if (string.IsNullOrEmpty(appName) || string.IsNullOrEmpty(domain)) {
                Console.Error.WriteLine("is-valid-binding requires --name, --domain");
                return 1;
            }
            try {
                if (!SiteExists(appName)) {
                    Console.WriteLine($"The IIS site [{appName}] was not found. Please create an IIS site with this name, and a binding for the domain [{domain}].");
                    return 3;
                }
                string bindingInfo = $"*:80:{domain}";
                if (BindingExists(appName, bindingInfo, "http")) {
                    Console.WriteLine("valid");
                    return 0;
                }
                Console.WriteLine($"No binding was found for the domain [{domain}].");
                return 3;
            } catch (UnauthorizedAccessException) {
                // process is not elevated, cannot verify IIS bindings, skip the check
                Console.WriteLine("valid");
                return 0;
            }
        }
        //
        // ====================================================================================================
        // verify-cdn-vdir --name <appName> --cdn-prefix <prefix> --physical-path <path>
        // ====================================================================================================
        //
        static int CmdVerifyCdnVdir(string[] args) {
            string appName = GetArg(args, "--name");
            string cdnPrefix = GetArg(args, "--cdn-prefix");
            string physicalPath = GetArg(args, "--physical-path");
            if (string.IsNullOrEmpty(appName) || string.IsNullOrEmpty(cdnPrefix) || string.IsNullOrEmpty(physicalPath)) {
                Console.Error.WriteLine("verify-cdn-vdir requires --name, --cdn-prefix, --physical-path");
                return 1;
            }
            if (!SiteExists(appName)) {
                Console.Error.WriteLine($"Site [{appName}] not found");
                return 2;
            }
            // Build each segment of the virtual directory path
            List<string> segments = cdnPrefix.Split('/').Where(s => !string.IsNullOrEmpty(s)).ToList();
            string newDirectoryPath = "";
            foreach (string segment in segments) {
                newDirectoryPath += $"/{segment}";
                if (!VirtualDirectoryExists(appName, newDirectoryPath)) {
                    RunAppCmdOrThrow($"add vdir /app.name:\"{appName}/\" /path:\"{newDirectoryPath}\" /physicalPath:\"{physicalPath}\"");
                }
            }
            Console.WriteLine($"Verified CDN virtual directory [{cdnPrefix}] on site [{appName}]");
            return 0;
        }
        //
        // ====================================================================================================
        // sync-ip-blocks --name <siteName> --ip <addr> [--ip <addr> ...]
        // Uses appcmd to clear and reset the ipSecurity section for the site.
        // ====================================================================================================
        //
        static int CmdSyncIpBlocks(string[] args) {
            string siteName = GetArg(args, "--name");
            if (string.IsNullOrEmpty(siteName)) {
                Console.Error.WriteLine("sync-ip-blocks requires --name");
                return 1;
            }
            List<string> ipAddresses = GetAllArgs(args, "--ip")
                .Where(ip => !string.IsNullOrWhiteSpace(ip))
                .Select(ip => ip.Trim())
                .ToList();
            // Clear existing ipSecurity entries and set allowUnlisted=true
            RunAppCmdOrThrow($"clear config \"{siteName}\" /section:system.webServer/security/ipSecurity /commit:apphost");
            RunAppCmdOrThrow($"set config \"{siteName}\" /section:system.webServer/security/ipSecurity /allowUnlisted:true /commit:apphost");
            // Add each deny entry
            foreach (string ipAddress in ipAddresses) {
                RunAppCmdOrThrow($"set config \"{siteName}\" /section:system.webServer/security/ipSecurity /+\"[ipAddress='{ipAddress}',allowed='false']\" /commit:apphost");
            }
            Console.WriteLine($"Synced {ipAddresses.Count} IP blocks to site [{siteName}]");
            return 0;
        }
        //
        // ====================================================================================================
        // Shared IIS helpers
        // ====================================================================================================
        //
        static void VerifyAppPool(string poolName, bool isFramework) {
            if (!AppPoolExists(poolName)) {
                RunAppCmdOrThrow($"add apppool /name:\"{poolName}\"");
            }
            if (isFramework) {
                RunAppCmdOrThrow($"set apppool /apppool.name:\"{poolName}\" /managedRuntimeVersion:v4.0 /enable32BitAppOnWin64:true /managedPipelineMode:Integrated");
            } else {
                RunAppCmdOrThrow($"set apppool /apppool.name:\"{poolName}\" /managedRuntimeVersion: /enable32BitAppOnWin64:false /managedPipelineMode:Integrated");
            }
        }
        //
        static void VerifyWebsite(string appName, string domainName, string wwwRootPath, string appPool) {
            if (!SiteExists(appName)) {
                RunAppCmdOrThrow($"add site /name:\"{appName}\" /bindings:http/*:80:{appName} /physicalPath:\"{wwwRootPath}\"");
            }
            // Set the app pool for the site's root application
            RunAppCmdOrThrow($"set app /app.name:\"{appName}/\" /applicationPool:\"{appPool}\"");
            // Verify the domain binding
            string bindingInfo = $"*:80:{domainName}";
            if (!BindingExists(appName, bindingInfo, "http")) {
                RunAppCmdOrThrow($"set site /site.name:\"{appName}\" /+bindings.[protocol='http',bindingInformation='{bindingInfo}']");
            }
        }
    }
}
