
using System;
using System.Reflection;
using Contensive.Processor;
using Contensive.Processor.Controllers;

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
        }
    }
}
