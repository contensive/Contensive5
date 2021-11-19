
using System;
using System.Collections.Generic;
using Contensive.Processor;
using Contensive.Processor.Controllers;

namespace Contensive.CLI {
    static class InstallFileCmd {
        //
        // ====================================================================================================
        /// <summary>
        /// help text for this command
        /// </summary>
        internal static string helpText = ""
            + Environment.NewLine
            + Environment.NewLine + "--installFile CollectionFileName.zip"
            + Environment.NewLine + "    installs the addon collection file"
            + "";
        //
        // ====================================================================================================
        /// <summary>
        /// create a new app. If appname is provided, create the app with defaults. if not appname, prompt for defaults
        /// </summary>
        /// <param name="appName"></param>
        public static void execute(CPClass cpServer, string appName, string collectionPhysicalPathFilename) {
            try {
                //
                if (!cpServer.serverOk) {
                    Console.WriteLine("Server Configuration not loaded correctly. Please run --configure");
                    return;
                }
                if (!System.IO.File.Exists(collectionPhysicalPathFilename)) {
                    Console.WriteLine("The file could not be found [" + collectionPhysicalPathFilename + "]");
                    return;
                }
                var collectionsInstalledList = new List<string>();
                var nonCritialErrorList = new List<string>();
                if (string.IsNullOrEmpty(appName)) {
                    foreach (var kvp in cpServer.core.serverConfig.apps) {
                        installCollectionFile(kvp.Key, collectionPhysicalPathFilename);
                    }
                } else {
                    installCollectionFile(appName, collectionPhysicalPathFilename);
                }
            } catch (Exception ex) {
                Console.WriteLine("Error: [" + ex + "]");
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// install a file to an app
        /// </summary>
        /// <param name="cpApp"></param>
        /// <param name="collectionPhysicalPathFilename"></param>
        private static void installCollectionFile( string appName, string collectionPhysicalPathFilename) {
            string logPrefix = "CLI";
            using (CPClass cpApp = new CPClass(appName)) {
                var contextLog = new Stack<string>();
                contextLog.Push("command line interface install command [" + collectionPhysicalPathFilename + "]");
                //
                // todo - this interface should all be tempFiles not private files (to avoid all the remote file system copies
                //
                // -- copy the file to private files
                string tempPath = "install" + Contensive.Processor.Controllers.GenericController.getGUIDNaked() + "\\";
                string tempPathFilename = tempPath + System.IO.Path.GetFileName(collectionPhysicalPathFilename);
                string tempPhysicalPathFilename = cpApp.TempFiles.PhysicalFilePath + tempPathFilename;
                if (!cpApp.TempFiles.FolderExists(tempPath)) {
                    cpApp.TempFiles.CreateFolder(tempPath);
                }
                if (System.IO.File.Exists(tempPhysicalPathFilename)) {
                    System.IO.File.Delete(tempPhysicalPathFilename);
                }
                System.IO.File.Copy(collectionPhysicalPathFilename, tempPhysicalPathFilename);
                cpApp.TempFiles.CopyLocalToRemote(tempPathFilename);
                //
                // -- build the collection folders for all collection files in the download path and created a list of collection Guids that need to be installed
                string errorMessage = "";
                var nonCriticalErrorList = new List<string>();
                var collectionsInstalled = new List<string>();
                string collectionGuidsInstalled = "";
                if (!CollectionInstallController.installCollectionFromTempFile(cpApp.core, false, contextLog, tempPathFilename, ref errorMessage, ref collectionGuidsInstalled, false, false, ref nonCriticalErrorList, logPrefix, ref collectionsInstalled)) {
                    if (!string.IsNullOrEmpty(errorMessage)) {
                        Console.WriteLine("***** Error installing the collection: " + errorMessage);
                    } else {
                        Console.WriteLine("***** Error installing the collection. The detail message available.");
                    }
                } else {
                    Console.WriteLine("Command line collection installation completed with no errors.");
                }
                cpApp.TempFiles.DeleteFile(tempPhysicalPathFilename);
                cpApp.Cache.InvalidateAll();
            }
        }

    }
}
