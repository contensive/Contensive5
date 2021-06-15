
using Contensive.Processor;
using System;
using System.Linq;

namespace Contensive.CLI {
    //
    static class IisResetCmd {
        //
        // ====================================================================================================
        /// <summary>
        /// help text for this command
        /// </summary>
        internal static readonly string helpText = ""
            + Environment.NewLine
            + Environment.NewLine + "--iisreset"
            + Environment.NewLine + "    Runs an iisreset, stopping and restarted the webserver (all sites). Requires elevated permissions."
            + "";
        //
        // ====================================================================================================
        /// <summary>
        /// manage the task scheduler service
        /// </summary>
        public static void execute(Contensive.Processor.CPClass cpServer) {
            //
            // -- use the first app
            if (cpServer.core.serverConfig.apps.Count > 0) {
                using (CPClass cp = new CPClass(cpServer.core.serverConfig.apps.First().Key)) {
                    cp.core.webServer.reset();
                }
            }
        }
    }
}
