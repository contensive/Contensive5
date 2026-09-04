
using System;
using System.Collections.Generic;
using Contensive.Processor.Models.Domain;

namespace Contensive.CLI {
    static class AppsJsonCmd {
        //
        // ====================================================================================================
        /// <summary>
        /// help text for this command
        /// </summary>
        internal static readonly string helpText = ""
            + Environment.NewLine
            + Environment.NewLine + "--appsjson"
            + Environment.NewLine + "    output the applications dictionary as JSON (for scripting/automation)"
            + "";
        //
        // ====================================================================================================
        /// <summary>
        /// Output the apps dictionary from serverConfig as JSON to stdout.
        /// This resolves config through AWS Secrets Manager when useSecretManager is enabled,
        /// so callers (like deployment scripts) get the full app list without reading config.json directly.
        /// </summary>
        public static void execute(Contensive.Processor.CPClass cpServer) {
            if (!cpServer.serverOk) {
                Console.Error.WriteLine("Server configuration not loaded. Run cc --configure");
                return;
            }
            //
            // -- build a lightweight dictionary with only the fields deployment scripts need
            var appsOutput = new Dictionary<string, object>();
            foreach (var kvp in cpServer.core.serverConfig.apps) {
                AppConfigModel app = (AppConfigModel)kvp.Value;
                appsOutput[kvp.Key] = new {
                    name = app.name,
                    enabled = app.enabled,
                    localWwwPath = app.localWwwPath,
                    localAppPath = app.localAppPath,
                    effectiveAppPath = app.effectiveAppPath
                };
            }
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(appsOutput, Newtonsoft.Json.Formatting.Indented);
            Console.Write(json);
        }
    }
}
