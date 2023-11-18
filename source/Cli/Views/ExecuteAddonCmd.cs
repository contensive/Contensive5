
using System;
using Contensive.Processor;

namespace Contensive.CLI {
    static class ExecuteAddonCmd {
        //
        // ====================================================================================================
        /// <summary>
        /// help text for this command
        /// </summary>
        internal static readonly string  helpText = ""
            + Environment.NewLine
            + Environment.NewLine + "--execute addonGuid|addonName"
            + Environment.NewLine + "    Executes and addon by guid or name (guid attempted first). Requires a appName be set first with -a."
            + "";
        //
        // ====================================================================================================
        /// <summary>
        /// create a new app. If appname is provided, create the app with defaults. if not appname, prompt for defaults
        /// </summary>
        /// <param name="appName"></param>
        public static void execute(CPClass cpServer, string appName, string addonNameOrGuid) {
            try {
                if (!cpServer.core.serverConfig.apps.ContainsKey(appName)) {
                    Console.WriteLine("The application [" + appName + "] was not found in this server group.");
                    return;
                }
                if (string.IsNullOrWhiteSpace(addonNameOrGuid)) {
                    Console.WriteLine("ERROR, execute requires a parameter for the addon you want to run");
                } else {
                    Console.WriteLine("executing addon [" + addonNameOrGuid + "], app  [" + appName + "]");
                    using (var cp = new CPClass(appName)) {
                        Console.WriteLine(cp.executeAddon(addonNameOrGuid));
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine("Error: [" + ex + "]");
            }
        }
    }
}
