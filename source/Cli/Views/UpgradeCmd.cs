
using System;
using System.IO;
using System.Reflection;
using Contensive.Processor;
using Contensive.Processor.Controllers;
using Contensive.Processor.Controllers.Build;
using Contensive.Processor.Models.Domain;

namespace Contensive.CLI {
    static class UpgradeCmd {
        //
        // ====================================================================================================
        /// <summary>
        /// help text for this command
        /// </summary>
        internal static readonly string  helpText = ""
            + Environment.NewLine
            + Environment.NewLine + "--upgrade (-u)"
            + Environment.NewLine + "    upgrade all applications, or just one if specified with -a"
            + "";
        //
        // ====================================================================================================
        /// <summary>
        /// Upgrade a single or all apps, optionally forcing full install to include up-to-date collections (to fix broken collection addons)
        /// </summary>
        /// <param name="appName"></param>
        /// <param name="repair">if true, install base dependencies, reset critical site properties and reinstall all library collections.</param>
        public static void execute(Contensive.Processor.CPClass cp, string appName, bool repair) {
            //
            // -- verify program files folder
            string currentPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (!cp.core.serverConfig.programFilesPath.Equals(currentPath)) {
                cp.core.serverConfig.programFilesPath = currentPath;
                cp.core.serverConfig.save(cp.core);
            }
            if (!string.IsNullOrEmpty(appName)) {
                //
                // -- upgrade app
                using CPClass upgradeApp = new Contensive.Processor.CPClass(appName);
                if (upgradeApp.GetAppConfig().enabled) {
                    BuildController.upgrade(upgradeApp.core, false, repair);
                    upgradeApp.Cache.InvalidateAll();

                }
            } else {
                //
                // -- upgrade all apps
                foreach (var kvp in cp.core.serverConfig.apps) {
                    using CPClass upgradeApp = new CPClass(kvp.Key);
                    if (upgradeApp.GetAppConfig().enabled) {
                        BuildController.upgrade(upgradeApp.core, false, repair);
                        upgradeApp.Cache.InvalidateAll();
                    }
                }
            }
            //
            // -- check for Core apps with overlapping application/static file paths
            checkWebrootMigration(cp, appName);
            //
            // -- run compatibility scan after upgrade
            CompatibilityCmd.execute(cp, appName);
            //
            // -- check for orphan collections in Collections.xml with no matching database record
            checkOrphanCollections(cp, appName);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Check if any Core WebApi apps have application binaries and static files in the same folder.
        /// If so, warn the user and offer migration options.
        /// </summary>
        private static void checkWebrootMigration(CPClass cp, string appName) {
            void checkApp(string name) {
                if (!cp.core.serverConfig.apps.ContainsKey(name)) { return; }
                var appConfig = (AppConfigModel)cp.core.serverConfig.apps[name];
                if (!appConfig.enabled) { return; }
                //
                // -- skip if localAppPath is already set (already migrated)
                if (!string.IsNullOrEmpty(appConfig.localAppPath)) { return; }
                //
                // -- skip Framework apps (they have default.aspx in the www root)
                if (File.Exists(Path.Combine(appConfig.localWwwPath, "default.aspx"))) { return; }
                //
                // -- skip if WebApi.dll is not in localWwwPath (not a Core app or already separated)
                if (!File.Exists(Path.Combine(appConfig.localWwwPath, "WebApi.dll"))) { return; }
                //
                // -- Core app with overlapping paths detected
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  WARNING: Application [{name}] has application binaries and static files");
                Console.WriteLine($"  in the same folder [{appConfig.localWwwPath}].");
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine("  Option 1 (recommended): Separate them by running:");
                Console.WriteLine($"    cc -a {name} --migratewebroot");
                Console.WriteLine();
                Console.WriteLine("  Option 2: Keep the current layout by setting WebRootPath in appsettings.json:");
                Console.WriteLine($"    {appConfig.localWwwPath}appsettings.json");
                Console.WriteLine($"    \"Contensive\": {{ \"WebRootPath\": \".\" }}");
                Console.WriteLine();
            }
            if (!string.IsNullOrEmpty(appName)) {
                checkApp(appName);
            } else {
                foreach (var kvp in cp.core.serverConfig.apps) {
                    checkApp(kvp.Key);
                }
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Check for collections in the Collections.xml file that do not have a matching record
        /// in the ccAddonCollections database table. These are orphan entries that should be removed.
        /// </summary>
        private static void checkOrphanCollections(CPClass cp, string appName) {
            void checkApp(string name) {
                try {
                    using CPClass checkCp = new CPClass(name);
                    if (!checkCp.GetAppConfig().enabled) { return; }
                    var orphans = OrphanCollectionScanner.ScanForOrphans(checkCp.core);
                    if (orphans.Count == 0) { return; }
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  WARNING: {orphans.Count} collection(s) in Collections.xml have no matching record in the database [{name}]:");
                    Console.ResetColor();
                    foreach (var orphan in orphans) {
                        Console.WriteLine($"    Collection: {orphan.CollectionName}");
                        Console.WriteLine($"    GUID:       {orphan.CollectionGuid}");
                        if (!string.IsNullOrEmpty(orphan.FolderPath)) {
                            Console.WriteLine($"    Folder:     {orphan.FolderPath}");
                        }
                        Console.WriteLine();
                    }
                    //
                    // -- remove orphan entries from Collections.xml and delete their folders
                    OrphanCollectionScanner.RemoveOrphans(checkCp.core, orphans);
                    Console.WriteLine($"  Removed {orphans.Count} orphan collection(s) from Collections.xml and deleted their folders.");
                } catch (Exception ex) {
                    Console.WriteLine($"  Error checking orphan collections for [{name}]: {ex.Message}");
                }
            }
            if (!string.IsNullOrEmpty(appName)) {
                checkApp(appName);
            } else {
                foreach (var kvp in cp.core.serverConfig.apps) {
                    checkApp(kvp.Key);
                }
            }
        }
    }
}
