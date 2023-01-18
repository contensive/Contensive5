
using System;
using System.Collections.Generic;
using Contensive.Processor;
using Contensive.Processor.Controllers;

namespace Contensive.CLI {
    static class ExportCollectionCmd {
        //
        // ====================================================================================================
        /// <summary>
        /// help text for this command
        /// </summary>
        internal readonly static string helpText = ""
            + Environment.NewLine
            + Environment.NewLine + "--exportcollection CollectionName"
            + Environment.NewLine + "    creates a collection zip file with the collections names"
            + "";
        //
        // ====================================================================================================
        /// <summary>
        /// create a new app. If appname is provided, create the app with defaults. if not appname, prompt for defaults
        /// </summary>
        /// <param name="appName"></param>
        public static void execute(CPClass cpServer, string appName, string collectionName) {
            try {
                //
                if (!cpServer.serverOk) {
                    Console.WriteLine("Server Configuration not loaded correctly. Please run --configure");
                    return;
                }
                //
                throw new NotImplementedException();
            } catch (Exception ex) {
                Console.WriteLine("Error: [" + ex + "]");
            }
        }
    }
}
