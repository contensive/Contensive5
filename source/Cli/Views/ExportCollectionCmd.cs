
using System;
using System.Collections.Generic;
using Amazon.SimpleEmail;
using Contensive.Models.Db;
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
                using (var cp = new CPClass(appName)) {
                    //
                    if (string.IsNullOrWhiteSpace(collectionName)) {
                        Console.WriteLine($"Please include a non-blank collection name to export");
                        return;
                    }
                    //
                    AddonCollectionModel collection = DbBaseModel.createByUniqueName<AddonCollectionModel>(cp, collectionName);
                    if (collection == null) {
                        Console.WriteLine($"The collection [{collectionName}] could not be found");
                        return;
                    }
                    string exportPathFilename = ExportController.createCollectionZip_returnCdnPathFilename(cp, collection);
                    Console.WriteLine($"Collection exported to file [{exportPathFilename}]");
                }

            } catch (Exception ex) {
                Console.WriteLine("Error: [" + ex + "]");
            }
        }
    }
}
