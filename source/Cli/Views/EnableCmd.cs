
using Contensive.Processor;
using System;

namespace Contensive.CLI {
    static class EnableCmd {
        //
        // ====================================================================================================
        /// <summary>
        /// help text for this command
        /// </summary>
        internal static readonly string  helpText = ""
            + Environment.NewLine
            + Environment.NewLine + "--enable (-e)"
            + Environment.NewLine + "    Set the appConfig to enable=true for this application. For all applications, or just one if specified with -a"
            + "";
        //
        // ====================================================================================================
        /// <summary>
        /// Repair a single or all apps, forcing full install to include up-to-date collections (to fix broken collection addons)
        /// </summary>
        /// <param name="appName"></param>
        /// <param name="repair"></param>
        public static void execute(Contensive.Processor.CPClass cpServer, string appName) {
            //
            if(string.IsNullOrEmpty(appName)) {
                Console.WriteLine("You must specify an application name with -a appName");
                return;
            }
            //
            // -- disable app
            if (!cpServer.core.serverConfig.apps.ContainsKey(appName)) {
                Console.WriteLine("The application [" + appName + "] was not found in this server group.");
                return;
            }
            cpServer.core.serverConfig.apps[appName].enabled = true;
            cpServer.core.serverConfig.save(cpServer.core);
        }
    }
}
