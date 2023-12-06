
using System;
using System.Collections;
using System.Collections.Generic;
using Contensive.Models.Db;
using Contensive.Processor;
using Contensive.Processor.Controllers;

namespace Contensive.CLI {
    static class AddFileCmd {
        //
        // ====================================================================================================
        /// <summary>
        /// help text for this command
        /// </summary>
        internal static string helpText = ""
            + Environment.NewLine
            + Environment.NewLine + "--addFile CollectionName filename"
            + Environment.NewLine + "    adds the file to the collection execution folder, typically an assembly DLL. If the filename is a zip, it is unzipped when installed"
            + "";
        //
        // ====================================================================================================
        /// <summary>
        /// add a file to collection folder
        /// </summary>
        /// <param name="appName"></param>
        public static void execute(CPClass cpServer, string appName, string collectionName, string currentPathFilenane, bool skipCdefInstall) {
            try {
                //
                if (!cpServer.serverOk) {
                    Console.WriteLine("Server Configuration not loaded correctly. Please run --configure");
                    return;
                }
                if (!System.IO.File.Exists(currentPathFilenane)) {
                    Console.WriteLine("The file could not be found [" + currentPathFilenane + "]");
                    return;
                }
                if (string.IsNullOrEmpty(appName)) {
                    foreach (var kvp in cpServer.core.serverConfig.apps) {
                        addFileToApp(cpServer, kvp.Key, currentPathFilenane, collectionName);
                    }
                } else {
                    addFileToApp(cpServer, appName, currentPathFilenane, collectionName);
                }

            } catch (Exception ex) {
                Console.WriteLine("Error: [" + ex + "]");
            }
        }
        //
        public static void addFileToApp( CPClass cpServer, string appName, string currentPathFilename, string collectionName) {
            using CPClass cpApp = new(appName);
            //
            AddonCollectionModel addonCollection = DbBaseModel.createByUniqueName<AddonCollectionModel>(cpApp, collectionName);
            if (addonCollection == null) {
                Console.WriteLine($"The addon collection could not be found [{collectionName}] in application [{appName}]");
                return;
            }
            string privateFilesCollectionExecutionFolder = AddonController.getPrivateFilesCollectionExecutionPath(cpApp.core, addonCollection, "", appName);
            if (string.IsNullOrEmpty(privateFilesCollectionExecutionFolder)) {
                Console.WriteLine($"The addon collection execution folder could not be found for collection [{collectionName}] in application [{appName}]");
                return;
            }
            //
            // -- copy the file to local temp files
            string tempPath = "tmpInstall" + GenericController.getGUIDNaked() + "\\";
            string tempPathFilename = tempPath + System.IO.Path.GetFileName(currentPathFilename);
            string tempPhysicalPathFilename = cpApp.TempFiles.PhysicalFilePath + tempPathFilename;
            string currentFilename = System.IO.Path.GetFileName(currentPathFilename);
            if (!cpApp.TempFiles.FolderExists(tempPath)) {
                cpApp.TempFiles.CreateFolder(tempPath);
            }
            if (System.IO.File.Exists(tempPhysicalPathFilename)) {
                System.IO.File.Delete(tempPhysicalPathFilename);
            }
            System.IO.File.Copy(currentPathFilename, tempPhysicalPathFilename);
            cpApp.TempFiles.CopyLocalToRemote(tempPathFilename);
            //
            if (System.IO.Path.GetExtension(currentPathFilename).ToLowerInvariant() == ".zip") {
                //
                // -- unpack a zip file
                cpApp.TempFiles.UnzipFile(tempPathFilename);
                cpApp.TempFiles.DeleteFile(tempPathFilename);
                //
                // -- copy all the unzipped files to the execution folder
                foreach( var file in cpApp.TempFiles.FileList(tempPath)) {
                    cpApp.TempFiles.Copy(tempPath + file.Name, privateFilesCollectionExecutionFolder + "\\" + file.Name, cpApp.PrivateFiles);
                }
            } else {
                //
                // --copy file to the folder
                cpApp.TempFiles.Copy(tempPathFilename, privateFilesCollectionExecutionFolder + "\\" + currentFilename, cpApp.PrivateFiles);
            }
            cpApp.TempFiles.DeleteFolder(tempPath);
        }
    }
}
