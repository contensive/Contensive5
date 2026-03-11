
using Contensive.Processor.Models.Domain;
using NLog;
using System;
using System.Collections.Generic;
using System.Xml;

namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// Handle resource file installation during collection install.
    /// </summary>
    public static class CollectionInstallResourceController {
        //
        //====================================================================================================
        /// <summary>
        /// Process a single resource node from the collection XML. Copies files to the appropriate
        /// file system (www, private, cdn, helpfiles) and tracks them in the resource manifest.
        /// </summary>
        internal static void installResourceNode(CoreController core, XmlNode metaDataSection, string collectionName, string collectionGuid, string collectionVersionFolder, ResourceManifestModel resourceManifest, HashSet<string> trackedFolders, List<string> assembliesInZip, ref string wwwFileList, ref string contentFileList, ref string execFileList) {
            //
            // set wwwfilelist, contentfilelist, execfilelist
            //
            string resourceType = XmlController.getXMLAttribute(core, metaDataSection, "type", "");
            string resourcePath = XmlController.getXMLAttribute(core, metaDataSection, "path", "");
            string filename = XmlController.getXMLAttribute(core, metaDataSection, "name", "");
            //
            logger.Info($"{core.logCommonMessage}, installCollectionFromAddonCollectionFolder [{collectionName}], resource found, name [{filename}], type [{resourceType}], path [{resourcePath}]");
            //
            filename = FileController.convertToDosSlash(filename);
            string SrcPath = "";
            string dstPath = resourcePath;
            int Pos = GenericController.strInstr(1, filename, "\\");
            if (Pos != 0) {
                //
                // Source path is in filename
                //
                SrcPath = filename.left(Pos - 1);
                filename = filename.Substring(Pos);
                if (string.IsNullOrEmpty(resourcePath)) {
                    //
                    // -- No Resource Path give, use the same folder structure from source
                    dstPath = SrcPath;
                } else {
                    //
                    // -- Copy file to resource path
                    dstPath = resourcePath;
                }
            }
            //
            // -- if the filename in the collection file is the wrong case, correct it now
            filename = core.privateFiles.correctFilenameCase(collectionVersionFolder + SrcPath + filename);
            //
            // == normalize dst
            string dstDosPath = FileController.normalizeDosPath(dstPath);
            //
            // --
            switch (resourceType.ToLowerInvariant()) {
                case "wwwfiles":
                case "wwwroot":
                case "www": {
                        wwwFileList += Environment.NewLine + dstDosPath + filename;
                        logger.Info($"{core.logCommonMessage}, CollectionName [{collectionName}], GUID [{collectionGuid}], pass 1, copying file to wwwFiles, src [{collectionVersionFolder}{SrcPath}], dst [{core.appConfig.localWwwPath}{dstDosPath}].");
                        core.privateFiles.copyFile(collectionVersionFolder + SrcPath + filename, dstDosPath + filename, core.wwwFiles);
                        if (GenericController.toLCase(filename.Substring(filename.Length - 4)) == ".zip") {
                            logger.Info($"{core.logCommonMessage}, installCollectionFromAddonCollectionFolder [{collectionName}], GUID [{collectionGuid}], pass 1, unzipping www file [{core.appConfig.localWwwPath}{dstDosPath}{filename}].");
                            if (!string.IsNullOrEmpty(dstDosPath)) {
                                resourceManifest.folders.Add(new ResourceManifestModel.ResourceManifestFolderEntry { type = "www", folderPath = dstDosPath });
                            }
                            ResourceManifestModel.unzipToTempThenCopy(core, core.wwwFiles, dstDosPath, dstDosPath + filename, "www", resourceManifest);
                            core.wwwFiles.deleteFile(dstDosPath + filename);
                        } else {
                            resourceManifest.resources.Add(new ResourceManifestModel.ResourceManifestEntry { type = "www", destinationPath = dstDosPath + filename });
                            if (!string.IsNullOrEmpty(dstDosPath) && !trackedFolders.Contains($"www::{dstDosPath}")) {
                                trackedFolders.Add($"www::{dstDosPath}");
                                resourceManifest.folders.Add(new ResourceManifestModel.ResourceManifestFolderEntry { type = "www", folderPath = dstDosPath });
                            }
                        }
                        break;
                    }
                case "privatefiles":
                case "private": {
                        contentFileList += Environment.NewLine + dstDosPath + filename;
                        logger.Info($"{core.logCommonMessage}, CollectionName [{collectionName}], GUID [{collectionGuid}], pass 1, copying file to privateFiles, src [{collectionVersionFolder}{SrcPath}], dst [{dstDosPath}].");
                        core.privateFiles.copyFile(collectionVersionFolder + SrcPath + filename, dstDosPath + filename);
                        if (GenericController.toLCase(filename.Substring(filename.Length - 4)) == ".zip") {
                            logger.Info($"{core.logCommonMessage}, CollectionName [{collectionName}], GUID [{collectionGuid}], pass 1, unzipping privateFiles file [{dstDosPath}{filename}].");
                            if (!string.IsNullOrEmpty(dstDosPath)) {
                                resourceManifest.folders.Add(new ResourceManifestModel.ResourceManifestFolderEntry { type = "private", folderPath = dstDosPath });
                                trackedFolders.Add($"private::{dstDosPath}");
                            }
                            ResourceManifestModel.unzipToTempThenCopy(core, core.privateFiles, dstDosPath, dstDosPath + filename, "private", resourceManifest);
                            core.privateFiles.deleteFile(dstDosPath + filename);
                        } else {
                            resourceManifest.resources.Add(new ResourceManifestModel.ResourceManifestEntry { type = "private", destinationPath = dstDosPath + filename });
                            if (!string.IsNullOrEmpty(dstDosPath) && !trackedFolders.Contains($"private::{dstDosPath}")) {
                                trackedFolders.Add($"private::{dstDosPath}");
                                resourceManifest.folders.Add(new ResourceManifestModel.ResourceManifestFolderEntry { type = "private", folderPath = dstDosPath });
                            }
                        }
                        break;
                    }
                case "file":
                case "files":
                case "cdnfiles":
                case "content": {
                        contentFileList += Environment.NewLine + dstDosPath + filename;
                        logger.Info($"{core.logCommonMessage}, CollectionName [{collectionName}], GUID [{collectionGuid}], pass 1, copying file to cdnFiles, src [{collectionVersionFolder}{SrcPath}], dst [{dstDosPath}].");
                        core.privateFiles.copyFile(collectionVersionFolder + SrcPath + filename, dstDosPath + filename, core.cdnFiles);
                        if (GenericController.toLCase(filename.Substring(filename.Length - 4)) == ".zip") {
                            logger.Info($"{core.logCommonMessage}, CollectionName [{collectionName}], GUID [{collectionGuid}], pass 1, unzipping cdnFiles [{dstDosPath}{filename}].");
                            if (!string.IsNullOrEmpty(dstDosPath)) {
                                resourceManifest.folders.Add(new ResourceManifestModel.ResourceManifestFolderEntry { type = "cdn", folderPath = dstDosPath });
                                trackedFolders.Add($"cdn::{dstDosPath}");
                            }
                            ResourceManifestModel.unzipToTempThenCopy(core, core.cdnFiles, dstDosPath, dstDosPath + filename, "cdn", resourceManifest);
                            core.cdnFiles.deleteFile(dstDosPath + filename);
                        } else {
                            resourceManifest.resources.Add(new ResourceManifestModel.ResourceManifestEntry { type = "cdn", destinationPath = dstDosPath + filename });
                            if (!string.IsNullOrEmpty(dstDosPath) && !trackedFolders.Contains($"cdn::{dstDosPath}")) {
                                trackedFolders.Add($"cdn::{dstDosPath}");
                                resourceManifest.folders.Add(new ResourceManifestModel.ResourceManifestFolderEntry { type = "cdn", folderPath = dstDosPath });
                            }
                        }
                        break;
                    }
                case "helpfiles":
                case "helpfile":
                case "help": {
                        //
                        // -- ignore the resource path for helpfiles, always install to helpFiles\
                        string helpFilesDstPath = "helpfiles\\";
                        //
                        // -- prefix filename with collection name
                        string originalFilename = filename;
                        filename = $"{collectionName}.{filename}";
                        //
                        logger.Info($"{core.logCommonMessage}, CollectionName [{collectionName}], GUID [{collectionGuid}], pass 1, copying file to privateFiles helpFiles, src [{collectionVersionFolder}{SrcPath}], dst [{helpFilesDstPath}].");
                        core.privateFiles.copyFile(collectionVersionFolder + SrcPath + originalFilename, helpFilesDstPath + filename);
                        if (GenericController.toLCase(filename.Substring(filename.Length - 4)) == ".zip") {
                            logger.Info($"{core.logCommonMessage}, CollectionName [{collectionName}], GUID [{collectionGuid}], pass 1, unzipping helpFiles file [{helpFilesDstPath}{filename}].");
                            resourceManifest.folders.Add(new ResourceManifestModel.ResourceManifestFolderEntry { type = "helpfiles", folderPath = helpFilesDstPath });
                            trackedFolders.Add($"helpfiles::{helpFilesDstPath}");
                            unzipHelpFilesToTempThenCopy(core, helpFilesDstPath, helpFilesDstPath + filename, collectionName, resourceManifest);
                            core.privateFiles.deleteFile(helpFilesDstPath + filename);
                        } else {
                            resourceManifest.resources.Add(new ResourceManifestModel.ResourceManifestEntry { type = "helpfiles", destinationPath = helpFilesDstPath + filename });
                            if (!string.IsNullOrEmpty(helpFilesDstPath) && !trackedFolders.Contains($"helpfiles::{helpFilesDstPath}")) {
                                trackedFolders.Add($"helpfiles::{helpFilesDstPath}");
                                resourceManifest.folders.Add(new ResourceManifestModel.ResourceManifestFolderEntry { type = "helpfiles", folderPath = helpFilesDstPath });
                            }
                        }
                        break;
                    }
                default: {
                        if (assembliesInZip.Contains(filename)) {
                            assembliesInZip.Remove(filename);
                        }
                        execFileList = execFileList + Environment.NewLine + filename;
                        break;
                    }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Save the resource manifest and clean up orphaned files from a previous version.
        /// Loads the old manifest, saves the new one, then deletes any files and empty folders
        /// that were in the old manifest but not in the new one.
        /// </summary>
        internal static void saveManifestAndCleanupOrphans(CoreController core, string collectionName, string collectionGuid, string collectionVersionFolder, ResourceManifestModel resourceManifest) {
            //
            // -- load old manifest before saving new one
            var oldManifest = ResourceManifestModel.load(core, collectionVersionFolder);
            //
            // -- save the new manifest
            ResourceManifestModel.save(core, collectionVersionFolder, resourceManifest);
            //
            // -- delete orphaned files from the previous version
            if (oldManifest != null && oldManifest.resources != null) {
                var newPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in resourceManifest.resources) {
                    newPaths.Add($"{entry.type}::{entry.destinationPath}");
                }
                foreach (var oldEntry in oldManifest.resources) {
                    if (!newPaths.Contains($"{oldEntry.type}::{oldEntry.destinationPath}")) {
                        switch (oldEntry.type.ToLowerInvariant()) {
                            case "www":
                                core.wwwFiles.deleteFile(oldEntry.destinationPath);
                                break;
                            case "private":
                                core.privateFiles.deleteFile(oldEntry.destinationPath);
                                break;
                            case "cdn":
                                core.cdnFiles.deleteFile(oldEntry.destinationPath);
                                break;
                            case "helpfiles":
                                core.privateFiles.deleteFile(oldEntry.destinationPath);
                                break;
                        }
                        logger.Info($"{core.logCommonMessage}, CollectionName [{collectionName}], GUID [{collectionGuid}], deleted orphaned resource [{oldEntry.type}::{oldEntry.destinationPath}]");
                    }
                }
            }
            //
            // -- delete orphaned folders from the previous version (only if empty)
            if (oldManifest != null && oldManifest.folders != null) {
                var newFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in resourceManifest.folders) {
                    newFolders.Add($"{entry.type}::{entry.folderPath}");
                }
                // -- process in reverse so deepest subfolders are checked first
                for (int i = oldManifest.folders.Count - 1; i >= 0; i--) {
                    var oldFolder = oldManifest.folders[i];
                    if (!newFolders.Contains($"{oldFolder.type}::{oldFolder.folderPath}")) {
                        FileController fileSystem = oldFolder.type.ToLowerInvariant() switch {
                            "www" => core.wwwFiles,
                            "private" => core.privateFiles,
                            "cdn" => core.cdnFiles,
                            "helpfiles" => core.privateFiles,
                            _ => null
                        };
                        if (fileSystem != null && fileSystem.getFileList(oldFolder.folderPath).Count == 0 && fileSystem.getFolderList(oldFolder.folderPath).Count == 0) {
                            fileSystem.deleteFolder(oldFolder.folderPath);
                            logger.Info($"{core.logCommonMessage}, CollectionName [{collectionName}], GUID [{collectionGuid}], deleted orphaned empty folder [{oldFolder.type}::{oldFolder.folderPath}]");
                        }
                    }
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Unzip a help file zip into a temp folder, prefix extracted files with collection name,
        /// then copy each file to the destination. New files are added to the manifest.
        /// </summary>
        internal static void unzipHelpFilesToTempThenCopy(CoreController core, string dstPath, string zipPathFilename, string collectionName, ResourceManifestModel resourceManifest) {
            string tempPath = $"installHelpZip{GenericController.getRandomInteger()}\\";
            try {
                core.tempFiles.createPath(tempPath);
                //
                // -- copy the zip to temp and extract there
                string zipFilename = System.IO.Path.GetFileName(zipPathFilename);
                core.privateFiles.copyFile(zipPathFilename, tempPath + zipFilename, core.tempFiles);
                core.tempFiles.unzipFile(tempPath + zipFilename);
                core.tempFiles.deleteFile(tempPath + zipFilename);
                //
                // -- prefix all extracted files in temp with collection name
                prefixTempHelpFiles(core, tempPath, collectionName);
                //
                // -- copy from temp to destination, tracking new files in manifest
                ResourceManifestModel.copyTempToDestRecursively(core, core.privateFiles, tempPath, dstPath, "helpfiles", resourceManifest, alwaysAddToManifest: true);
            } finally {
                core.tempFiles.deleteFolder(tempPath);
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Recursively prefix all files in a temp helpFiles folder with the collection name.
        /// Since all files in temp are newly extracted, no existing-files check is needed.
        /// </summary>
        internal static void prefixTempHelpFiles(CoreController core, string folderPath, string collectionName) {
            string prefix = $"{collectionName}.";
            foreach (var extractedFile in core.tempFiles.getFileList(folderPath)) {
                if (extractedFile.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) { continue; }
                string prefixedName = $"{prefix}{extractedFile.Name}";
                core.tempFiles.copyFile(folderPath + extractedFile.Name, folderPath + prefixedName);
                core.tempFiles.deleteFile(folderPath + extractedFile.Name);
            }
            foreach (var subFolder in core.tempFiles.getFolderList(folderPath)) {
                prefixTempHelpFiles(core, $"{folderPath}{subFolder.Name}\\", collectionName);
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
