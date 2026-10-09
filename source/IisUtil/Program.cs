
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Web.Administration;

namespace Contensive.IisUtil {
    /// <summary>
    /// Standalone IIS management utility. Executes IIS operations that require
    /// Microsoft.Web.Administration, keeping this dependency out of Processor.dll.
    /// Called from Processor and CLI via Process.Start().
    ///
    /// Exit codes:
    ///   0 = success
    ///   1 = invalid arguments
    ///   2 = operation failed
    /// </summary>
    internal class Program {
        static int Main(string[] args) {
            if (args.Length == 0) {
                PrintUsage();
                return 1;
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
        // recycle --name <appName>
        // ====================================================================================================
        //
        static int CmdRecycle(string[] args) {
            string appName = GetArg(args, "--name");
            if (string.IsNullOrEmpty(appName)) {
                Console.Error.WriteLine("recycle requires --name");
                return 1;
            }
            using var serverManager = new ServerManager();
            foreach (ApplicationPool appPool in serverManager.ApplicationPools) {
                if (appPool.Name.Equals(appName, StringComparison.OrdinalIgnoreCase)) {
                    if (appPool.Start() == ObjectState.Started) {
                        appPool.Recycle();
                        Console.WriteLine($"Recycled app pool [{appName}]");
                    }
                    return 0;
                }
            }
            Console.Error.WriteLine($"App pool [{appName}] not found");
            return 2;
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
            using var serverManager = new ServerManager();
            foreach (ApplicationPool appPool in serverManager.ApplicationPools) {
                if (appPool.Name.Equals(poolName, StringComparison.OrdinalIgnoreCase)) {
                    if (appPool.State != ObjectState.Stopped && appPool.State != ObjectState.Stopping) {
                        appPool.Stop();
                    }
                    int maxWait = 30;
                    while (appPool.State != ObjectState.Stopped && maxWait > 0) {
                        System.Threading.Thread.Sleep(1000);
                        maxWait--;
                    }
                    Console.WriteLine($"Stopped app pool [{poolName}]");
                    return 0;
                }
            }
            Console.Error.WriteLine($"App pool [{poolName}] not found");
            return 2;
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
            using var serverManager = new ServerManager();
            foreach (ApplicationPool appPool in serverManager.ApplicationPools) {
                if (appPool.Name.Equals(poolName, StringComparison.OrdinalIgnoreCase)) {
                    serverManager.ApplicationPools.Remove(appPool);
                    serverManager.CommitChanges();
                    Console.WriteLine($"Deleted app pool [{poolName}]");
                    return 0;
                }
            }
            Console.Error.WriteLine($"App pool [{poolName}] not found");
            return 2;
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
            using var iisManager = new ServerManager();
            foreach (Site site in iisManager.Sites) {
                if (site.Name.Equals(appName, StringComparison.OrdinalIgnoreCase)) {
                    iisManager.Sites.Remove(site);
                    iisManager.CommitChanges();
                    Console.WriteLine($"Deleted site [{appName}]");
                    return 0;
                }
            }
            Console.Error.WriteLine($"Site [{appName}] not found");
            return 2;
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
            using var iisManager = new ServerManager();
            Site site = null;
            foreach (Site siteWithinLoop in iisManager.Sites) {
                if (siteWithinLoop.Name.Equals(appName, StringComparison.OrdinalIgnoreCase)) {
                    site = siteWithinLoop;
                    break;
                }
            }
            if (site == null) {
                Console.Error.WriteLine($"Site [{appName}] not found");
                return 2;
            }
            VerifyWebsiteBinding(site, domain);
            iisManager.CommitChanges();
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
                using var iisManager = new ServerManager();
                Site site = null;
                foreach (Site siteWithinLoop in iisManager.Sites) {
                    if (siteWithinLoop.Name.Equals(appName, StringComparison.OrdinalIgnoreCase)) {
                        site = siteWithinLoop;
                        break;
                    }
                }
                if (site == null) {
                    Console.WriteLine($"The IIS site [{appName}] was not found. Please create an IIS site with this name, and a binding for the domain [{domain}].");
                    return 3;
                }
                string bindingInformation = $"*:80:{domain}";
                string bindingProtocol = "http";
                foreach (Binding bindingWithinLoop in site.Bindings) {
                    if (string.Equals(bindingWithinLoop.BindingInformation, bindingInformation, StringComparison.OrdinalIgnoreCase) && string.Equals(bindingWithinLoop.Protocol, bindingProtocol, StringComparison.OrdinalIgnoreCase)) {
                        Console.WriteLine("valid");
                        return 0;
                    }
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
            using var iisManager = new ServerManager();
            Site site = null;
            foreach (Site siteWithinLoop in iisManager.Sites) {
                if (siteWithinLoop.Name.Equals(appName, StringComparison.OrdinalIgnoreCase)) {
                    site = siteWithinLoop;
                    break;
                }
            }
            if (site == null) {
                Console.Error.WriteLine($"Site [{appName}] not found");
                return 2;
            }
            VerifyWebsiteVirtualDirectory(site, appName, cdnPrefix, physicalPath);
            iisManager.CommitChanges();
            Console.WriteLine($"Verified CDN virtual directory [{cdnPrefix}] on site [{appName}]");
            return 0;
        }
        //
        // ====================================================================================================
        // sync-ip-blocks --name <siteName> --ip <addr> [--ip <addr> ...]
        // ====================================================================================================
        //
        static int CmdSyncIpBlocks(string[] args) {
            string siteName = GetArg(args, "--name");
            if (string.IsNullOrEmpty(siteName)) {
                Console.Error.WriteLine("sync-ip-blocks requires --name");
                return 1;
            }
            List<string> ipAddresses = GetAllArgs(args, "--ip");
            using var serverManager = new ServerManager();
            var config = serverManager.GetApplicationHostConfiguration();
            var ipSecuritySection = config.GetSection("system.webServer/security/ipSecurity", siteName);
            var ipSecurityCollection = ipSecuritySection.GetCollection();
            ipSecurityCollection.Clear();
            ipSecuritySection["allowUnlisted"] = true;
            foreach (var ipAddress in ipAddresses) {
                if (string.IsNullOrWhiteSpace(ipAddress)) { continue; }
                var addElement = ipSecurityCollection.CreateElement("add");
                addElement["ipAddress"] = ipAddress.Trim();
                addElement["allowed"] = false;
                ipSecurityCollection.Add(addElement);
            }
            serverManager.CommitChanges();
            Console.WriteLine($"Synced {ipAddresses.Count} IP blocks to site [{siteName}]");
            return 0;
        }
        //
        // ====================================================================================================
        // Shared IIS helpers (extracted from WebServerController)
        // ====================================================================================================
        //
        static void VerifyAppPool(string poolName, bool isFramework) {
            using var serverManager = new ServerManager();
            ApplicationPool appPool = null;
            bool poolFound = false;
            foreach (ApplicationPool appPoolWithinLoop in serverManager.ApplicationPools) {
                if (appPoolWithinLoop.Name == poolName) {
                    poolFound = true;
                    break;
                }
            }
            if (!poolFound) {
                appPool = serverManager.ApplicationPools.Add(poolName);
            } else {
                appPool = serverManager.ApplicationPools[poolName];
            }
            if (isFramework) {
                appPool.ManagedRuntimeVersion = "v4.0";
                appPool.Enable32BitAppOnWin64 = true;
            } else {
                appPool.ManagedRuntimeVersion = "";
                appPool.Enable32BitAppOnWin64 = false;
            }
            appPool.ManagedPipelineMode = ManagedPipelineMode.Integrated;
            serverManager.CommitChanges();
        }
        //
        static void VerifyWebsite(string appName, string domainName, string wwwRootPath, string appPool) {
            using var iisManager = new ServerManager();
            bool found = false;
            foreach (Site siteWithinLoop in iisManager.Sites) {
                if (siteWithinLoop.Name.Equals(appName, StringComparison.OrdinalIgnoreCase)) {
                    found = true;
                    break;
                }
            }
            if (!found) {
                iisManager.Sites.Add(appName, "http", $"*:80:{appName}", wwwRootPath);
            }
            Site site = iisManager.Sites[appName];
            VerifyWebsiteBinding(site, domainName);
            site.ApplicationDefaults.ApplicationPoolName = appPool;
            foreach (Application iisApp in site.Applications) {
                iisApp.ApplicationPoolName = appPool;
            }
            iisManager.CommitChanges();
        }
        //
        static void VerifyWebsiteBinding(Site site, string domainName) {
            string bindingInformation = $"*:80:{domainName}";
            string bindingProtocol = "http";
            bool found = false;
            foreach (Binding bindingWithinLoop in site.Bindings) {
                if ((bindingWithinLoop.BindingInformation == bindingInformation) && (bindingWithinLoop.Protocol == bindingProtocol)) {
                    found = true;
                    break;
                }
            }
            if (!found) {
                Binding binding = site.Bindings.CreateElement();
                binding.BindingInformation = bindingInformation;
                binding.Protocol = bindingProtocol;
                site.Bindings.Add(binding);
            }
        }
        //
        static void VerifyWebsiteVirtualDirectory(Site site, string appName, string virtualFolder, string physicalPath) {
            bool found = false;
            foreach (Application iisApp in site.Applications) {
                if (iisApp.ApplicationPoolName.Equals(appName, StringComparison.OrdinalIgnoreCase)) {
                    foreach (VirtualDirectory virtualDirectory in iisApp.VirtualDirectories) {
                        if (virtualDirectory.Path == virtualFolder) {
                            found = true;
                            break;
                        }
                    }
                    if (!found) {
                        List<string> appVirtualFolderSegments = virtualFolder.Split('/').ToList();
                        string newDirectoryPath = "";
                        foreach (string appVirtualFolderSegment in appVirtualFolderSegments) {
                            if (!string.IsNullOrEmpty(appVirtualFolderSegment)) {
                                newDirectoryPath += $"/{appVirtualFolderSegment}";
                                bool directoryFound = false;
                                foreach (VirtualDirectory currentDirectory in iisApp.VirtualDirectories) {
                                    if (currentDirectory.Path.Equals(newDirectoryPath, StringComparison.OrdinalIgnoreCase)) {
                                        directoryFound = true;
                                        break;
                                    }
                                }
                                if (!directoryFound) {
                                    iisApp.VirtualDirectories.Add(newDirectoryPath, physicalPath);
                                }
                            }
                        }
                    }
                }
                if (found) {
                    break;
                }
            }
        }
    }
}
