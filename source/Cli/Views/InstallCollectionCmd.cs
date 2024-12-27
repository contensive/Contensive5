
using System;
using System.Collections.Generic;
using Contensive.Processor;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;

namespace Contensive.CLI {
    static class InstallCmd {
        //
        // ====================================================================================================
        /// <summary>
        /// help text for this command
        /// </summary>
        internal static readonly string helpText = ""
            + Environment.NewLine
            + Environment.NewLine + "--install CollectionName"
            + Environment.NewLine + "    downloads and installed the addon collection named from the Contensive Support Library"
            + Environment.NewLine
            + Environment.NewLine + "--installQuick CollectionName"
            + Environment.NewLine + "    downloads and installed the addon collection named from the Contensive Support Library"
            + "";
        //
        // ====================================================================================================
        /// <summary>
        /// create a new app. If appname is provided, create the app with defaults. if not appname, prompt for defaults
        /// </summary>
        /// <param name="appName"></param>
        public static void execute(CPClass cpServer, string appName, string collectionName, bool skipCdefInstall) {
            try {
                //
                if (!cpServer.serverOk) {
                    Console.WriteLine("Server Configuration not loaded correctly. Please run --configure");
                    return;
                }
                //
                // -- determine guid of collection
                var collectionLibraryList = CollectionLibraryModel.getCollectionLibraryList(cpServer.core);
                if (collectionLibraryList == null) {
                    Console.WriteLine("The collection library server did not respond.");
                    return;
                }
                string collectionGuid = "";
                foreach (var collection in collectionLibraryList) {
                    if (collection.name.ToLowerInvariant() == collectionName.ToLowerInvariant()) {
                        collectionGuid = collection.guid;
                        break;
                    }
                }
                if (string.IsNullOrEmpty(collectionGuid)) {
                    Console.WriteLine("Collection was not found on the distribution server");
                    return;
                }
                string logPrefix = "CLI";
                var collectionsInstalledList = new List<string>();
                var nonCritialErrorList = new List<string>();
                if (string.IsNullOrEmpty(appName)) {
                    foreach (var kvp in cpServer.core.serverConfig.apps) {
                        using (CPClass cpApp = new(kvp.Key)) {
                            var context = new Stack<string>();
                            context.Push("command line interface install command [" + collectionName + ", " + collectionGuid + "]");
                            ErrorReturnModel returnErrorMessage = new();
                            CollectionLibraryController.installCollectionFromLibrary(cpApp.core, false, context, collectionGuid, ref returnErrorMessage, false, true, ref nonCritialErrorList, logPrefix, ref collectionsInstalledList, skipCdefInstall);
                            if (returnErrorMessage.hasErrors) {
                                Console.WriteLine("There was an error installing the collection: " + returnErrorMessage);
                            }
                            cpApp.Cache.InvalidateAll();
                        }
                    }
                } else {
                    using (CPClass cpApp = new(appName)) {
                        var context = new Stack<string>();
                        context.Push("command line interface install command [" + collectionName + ", " + collectionGuid + "]");
                        ErrorReturnModel returnErrorMessage = new();
                        CollectionLibraryController.installCollectionFromLibrary(cpApp.core, false, context, collectionGuid, ref returnErrorMessage, false, true, ref nonCritialErrorList, logPrefix, ref collectionsInstalledList, skipCdefInstall);
                        if (returnErrorMessage.hasErrors) {
                            Console.WriteLine("***** Error installing the collection: " + returnErrorMessage);
                        }
                        cpApp.Cache.InvalidateAll();
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine("Error: [" + ex + "]");
            }
        }
    }
}
