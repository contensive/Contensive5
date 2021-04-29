
using Contensive.Processor.Models.Domain;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Xml;
using static Contensive.BaseClasses.CPFileSystemBaseClass;
using static Contensive.Processor.Constants;

namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// manage the collection folder. The collection folder is in private files. This is where collections are installed, and where addon assemblys are run.
    /// </summary>
    public class CollectionFolderController {
        //
        //====================================================================================================
        /// <summary>
        /// Build collection folders for all collection files in a tempFiles folder
        /// - enumerate zip files in a folder and call buildCollectionFolderFromCollectionZip
        /// </summary>
        /// <param name="core"></param>
        /// <param name="sourceTempFolderPath"></param>
        /// <param name="CollectionLastChangeDate"></param>
        /// <param name="collectionsToInstall">A list of collection guids that need to be installed in the database. Any collection folders built are added to he collectionsToInstall list.</param>
        /// <param name="return_ErrorMessage"></param>
        /// <param name="collectionsInstalledList"></param>
        /// <param name="collectionsBuildingFolder">list of collection guids in the process of folder building. use to block recursive loop.</param>
        /// <returns></returns>
        public static bool buildCollectionFoldersFromCollectionZips(CoreController core, Stack<string> contextLog, string sourceTempFolderPath, DateTime CollectionLastChangeDate, ref List<string> collectionsToInstall, ref string return_ErrorMessage, ref List<string> collectionsInstalledList, ref List<string> collectionsBuildingFolder) {
            bool success = false;
            try {
                //
                contextLog.Push(MethodInfo.GetCurrentMethod().Name + ", [" + sourceTempFolderPath + "]");
                traceContextLog(core, contextLog);
                //
                if (core.tempFiles.pathExists(sourceTempFolderPath)) {
                    LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", BuildLocalCollectionFolder, processing files in private folder [" + sourceTempFolderPath + "]");
                    List<CPFileSystemClass.FileDetail> SrcFileNamelist = core.tempFiles.getFileList(sourceTempFolderPath);
                    foreach (CPFileSystemClass.FileDetail file in SrcFileNamelist) {
                        if ((file.Extension == ".zip") || (file.Extension == ".xml")) {
                            success = buildCollectionFolderFromCollectionZip(core, contextLog, sourceTempFolderPath + file.Name, CollectionLastChangeDate, ref return_ErrorMessage, ref collectionsToInstall, ref collectionsInstalledList, ref collectionsBuildingFolder);
                        }
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            } finally {
                contextLog.Pop();
            }
            return success;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Builds collection folders for a collectionZip file
        /// unzip a folder and if the collection is not in the collections installed or the collectionsToInstall, save the collection to the appropriate collection folder and add it to the collectionsToInstall
        /// </summary>
        /// <param name="core"></param>
        /// <param name="sourceTempFolderPathFilename"></param>
        /// <param name="CollectionLastChangeDate"></param>
        /// <param name="collectionGuid"></param>
        /// <param name="return_ErrorMessage"></param>
        /// <param name="collectionsDownloaded">collection guids that have been saved to the collection folder and need to be saved to the database druing this install.</param>
        /// <param name="collectionsInstalledList">collection guids that have been saved to the database during this install.</param>
        /// <param name="collectionsBuildingFolder">folder building is recursive. These are the collection guids whose folders are currently being built.</param>
        /// <returns></returns>
        public static bool buildCollectionFolderFromCollectionZip(CoreController core, Stack<string> contextLog, string sourceTempFolderPathFilename, DateTime CollectionLastChangeDate, ref string return_ErrorMessage, ref List<string> collectionsDownloaded, ref List<string> collectionsInstalledList, ref List<string> collectionsBuildingFolder) {
            try {
                //
                contextLog.Push(MethodInfo.GetCurrentMethod().Name + ", [" + sourceTempFolderPathFilename + "]");
                traceContextLog(core, contextLog);
                //
                string collectionPath = "";
                string collectionFilename = "";
                core.tempFiles.splitDosPathFilename(sourceTempFolderPathFilename, ref collectionPath, ref collectionFilename);
                string CollectionVersionFolderName = "";
                if (!core.tempFiles.pathExists(collectionPath)) {
                    //
                    // return false, The working folder is not there
                    return_ErrorMessage = "<p>There was a problem with the installation. The installation folder is not valid.</p>";
                    LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", BuildLocalCollectionFolder, CheckFileFolder was false for the temp folder [" + collectionPath + "]");
                    return false;
                }
                //
                LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", BuildLocalCollectionFolder, processing files in temp folder [" + collectionPath + "]");
                //
                // --move collection file to a temp directory
                // -- use try-finally to delete temp folder
                string tmpInstallPath = "tmpInstallCollection" + GenericController.getGUIDNaked() + "\\";
                try {
                    core.tempFiles.copyFile(sourceTempFolderPathFilename, tmpInstallPath + collectionFilename);
                    if (collectionFilename.ToLowerInvariant().Substring(collectionFilename.Length - 4) == ".zip") {
                        core.tempFiles.unzipFile(tmpInstallPath + collectionFilename);
                        core.tempFiles.deleteFile(tmpInstallPath + collectionFilename);
                    }
                    //
                    // -- find xml file in temp folder and process it
                    bool CollectionXmlFileFound = false;
                    foreach (FileDetail file in core.tempFiles.getFileList(tmpInstallPath)) {
                        if (file.Name.Substring(file.Name.Length - 4).ToLower(CultureInfo.InvariantCulture) == ".xml") {
                            //
                            LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", build collection folder for Collection file [" + file.Name + "]");
                            //
                            XmlDocument CollectionFile = new XmlDocument();
                            try {
                                CollectionFile.LoadXml(core.tempFiles.readFileText(tmpInstallPath + file.Name));
                            } catch (Exception ex) {
                                //
                                // -- There was a parse error in this xml file. Set the return message and the flag
                                // -- If another xml files shows up, and process OK it will cover this error
                                return_ErrorMessage = "There was a problem installing the Collection File [" + tmpInstallPath + file.Name + "]. The error reported was [" + ex.Message + "].";
                                LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", BuildLocalCollectionFolder, error reading collection [" + sourceTempFolderPathFilename + "]");
                                continue;
                            }
                            string CollectionFileBaseName = GenericController.toLCase(CollectionFile.DocumentElement.Name);
                            if ((CollectionFileBaseName != "contensivecdef") && (CollectionFileBaseName != CollectionFileRootNode) && (CollectionFileBaseName != GenericController.toLCase(CollectionFileRootNodeOld))) {
                                //
                                // -- Not a problem, this is just not a collection file
                                LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", BuildLocalCollectionFolder, xml base name wrong [" + CollectionFileBaseName + "]");
                                continue;
                            }
                            bool IsFound = false;
                            //
                            // Collection File
                            //
                            string Collectionname = XmlController.getXMLAttribute(core, ref IsFound, CollectionFile.DocumentElement, "name", "");
                            string collectionGuid = XmlController.getXMLAttribute(core, ref IsFound, CollectionFile.DocumentElement, "guid", Collectionname);
                            if ((!collectionsInstalledList.Contains(collectionGuid.ToLower(CultureInfo.InvariantCulture))) && (!collectionsDownloaded.Contains(collectionGuid.ToLower(CultureInfo.InvariantCulture)))) {
                                if (string.IsNullOrEmpty(Collectionname)) {
                                    //
                                    // ----- Error condition -- it must have a collection name
                                    //
                                    return_ErrorMessage = "<p>There was a problem with this Collection. The collection file does not have a collection name.</p>";
                                    LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", BuildLocalCollectionFolder, collection has no name");
                                    continue;
                                }
                                //
                                //------------------------------------------------------------------
                                // Build Collection folder structure in /Add-ons folder
                                //------------------------------------------------------------------
                                //
                                collectionsDownloaded.Add(collectionGuid.ToLower(CultureInfo.InvariantCulture));
                                CollectionXmlFileFound = true;
                                if (string.IsNullOrEmpty(collectionGuid)) {
                                    //
                                    // must have a guid
                                    collectionGuid = Collectionname;
                                }
                                //
                                //
                                CollectionVersionFolderName = verifyCollectionVersionFolderName(core, collectionGuid, Collectionname);
                                string CollectionVersionFolder = AddonController.getPrivateFilesAddonPath() + CollectionVersionFolderName;
                                //
                                core.tempFiles.copyPath(tmpInstallPath, CollectionVersionFolder, core.privateFiles);
                                //
                                // -- iterate through all nodes of this collection xml file and install all dependencies
                                foreach (XmlNode metaDataSection in CollectionFile.DocumentElement.ChildNodes) {
                                    string ChildCollectionGUID = null;
                                    string ChildCollectionName = null;
                                    bool Found = false;
                                    switch (GenericController.toLCase(metaDataSection.Name)) {
                                        case "resource":
                                            break;
                                        case "getcollection":
                                        case "importcollection":
                                            //
                                            // -- Download Collection file into install folder
                                            ChildCollectionName = XmlController.getXMLAttribute(core, ref Found, metaDataSection, "name", "");
                                            ChildCollectionGUID = XmlController.getXMLAttribute(core, ref Found, metaDataSection, "guid", metaDataSection.InnerText);
                                            if (string.IsNullOrEmpty(ChildCollectionGUID)) {
                                                ChildCollectionGUID = metaDataSection.InnerText;
                                            }
                                            ChildCollectionGUID = GenericController.normalizeGuid(ChildCollectionGUID);
                                            string statusMsg = "Installing collection [" + ChildCollectionName + ", " + ChildCollectionGUID + "] referenced from collection [" + Collectionname + "]";
                                            LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", BuildLocalCollectionFolder, getCollection or importcollection, childCollectionName [" + ChildCollectionName + "], childCollectionGuid [" + ChildCollectionGUID + "]");
                                            if (GenericController.strInstr(1, CollectionVersionFolder, ChildCollectionGUID, 1) == 0) {
                                                if (string.IsNullOrEmpty(ChildCollectionGUID)) {
                                                    //
                                                    // -- Needs a GUID to install
                                                    return_ErrorMessage = statusMsg + ". The installation can not continue because an imported collection could not be downloaded because it does not include a valid GUID.";
                                                    LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", BuildLocalCollectionFolder, return message [" + return_ErrorMessage + "]");
                                                } else {
                                                    if ((!collectionsBuildingFolder.Contains(ChildCollectionGUID)) && (!collectionsDownloaded.Contains(ChildCollectionGUID)) && (!collectionsInstalledList.Contains(ChildCollectionGUID))) {
                                                        //
                                                        // -- add to the list of building folders to block recursive loop
                                                        collectionsBuildingFolder.Add(ChildCollectionGUID);
                                                        LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", BuildLocalCollectionFolder, [" + ChildCollectionGUID + "], not found so needs to be installed");
                                                        //
                                                        // If it is not already installed, download and install it also
                                                        //
                                                        string workingTempPath = GenericController.getGUIDNaked() + "\\";
                                                        DateTime libraryCollectionLastModifiedDate = default;
                                                        try {
                                                            //
                                                            // try-finally to delete the working folder
                                                            if (!CollectionLibraryController.downloadCollectionFromLibrary(core, workingTempPath, ChildCollectionGUID, ref libraryCollectionLastModifiedDate, ref return_ErrorMessage)) {
                                                                //
                                                                // -- did not download correctly
                                                                LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", BuildLocalCollectionFolder, [" + statusMsg + "], downloadCollectionFiles returned error state, message [" + return_ErrorMessage + "]");
                                                                if (string.IsNullOrEmpty(return_ErrorMessage)) {
                                                                    return_ErrorMessage = statusMsg + ". The installation can not continue because there was an unknown error while downloading the necessary collection file, [" + ChildCollectionGUID + "].";
                                                                } else {
                                                                    return_ErrorMessage = statusMsg + ". The installation can not continue because there was an error while downloading the necessary collection file, guid [" + ChildCollectionGUID + "]. The error was [" + return_ErrorMessage + "]";
                                                                }
                                                            } else {
                                                                LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", BuildLocalCollectionFolder, libraryCollectionLastChangeDate [" + libraryCollectionLastModifiedDate.ToString() + "].");
                                                                bool installDependentCollection = true;
                                                                var localCollectionConfig = CollectionFolderModel.getCollectionFolderConfig(core, ChildCollectionGUID);
                                                                if (localCollectionConfig == null) {
                                                                    //
                                                                    // -- collection not installed, ok to install
                                                                    LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", BuildLocalCollectionFolder, collection");
                                                                } else {
                                                                    LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", BuildLocalCollectionFolder, localCollectionConfig.lastChangeDate [" + localCollectionConfig.lastChangeDate.ToString() + "].");
                                                                    if (localCollectionConfig.lastChangeDate < libraryCollectionLastModifiedDate) {
                                                                        //
                                                                        // -- downloaded collection is newer than installed collection, reinstall
                                                                        LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", BuildLocalCollectionFolder, **** local version is older than library, needs to reinstall.");
                                                                    } else {
                                                                        //
                                                                        // -- download is older than installed, skip the rest of the xml file processing
                                                                        installDependentCollection = false;
                                                                        LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", BuildLocalCollectionFolder, **** local version is newer or the same as library, can skip install.");
                                                                        break;
                                                                    }
                                                                }
                                                                if (installDependentCollection) {
                                                                    //
                                                                    // -- install the downloaded file
                                                                    LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", BuildLocalCollectionFolder, collection missing or needs to be updated.");
                                                                    if (!buildCollectionFoldersFromCollectionZips(core, contextLog, workingTempPath, libraryCollectionLastModifiedDate, ref collectionsDownloaded, ref return_ErrorMessage, ref collectionsInstalledList, ref collectionsBuildingFolder)) {
                                                                        LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", BuildLocalCollectionFolder, [" + statusMsg + "], BuildLocalCollectionFolder returned error state, message [" + return_ErrorMessage + "]");
                                                                        if (string.IsNullOrEmpty(return_ErrorMessage)) {
                                                                            return_ErrorMessage = statusMsg + ". The installation can not continue because there was an unknown error installing the included collection file, guid [" + ChildCollectionGUID + "].";
                                                                        } else {
                                                                            return_ErrorMessage = statusMsg + ". The installation can not continue because there was an unknown error installing the included collection file, guid [" + ChildCollectionGUID + "]. The error was [" + return_ErrorMessage + "]";
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        } catch (Exception) {
                                                            //
                                                            // -- exception in try-finally for folder handling, just rethrow to the catch for hte method
                                                            throw;
                                                        } finally {
                                                            //
                                                            // -- remove child installation working folder
                                                            core.tempFiles.deleteFolder(workingTempPath);
                                                            //
                                                            // -- no longer building this folder
                                                            collectionsBuildingFolder.Remove(ChildCollectionGUID);
                                                        }
                                                    }
                                                }
                                            }
                                            break;
                                    }
                                    if (!string.IsNullOrEmpty(return_ErrorMessage)) {
                                        //
                                        // -- if error during xml processing, skip the rest of the xml nodes and go to the next file.
                                        break;
                                    }
                                }
                            }
                            if (!string.IsNullOrEmpty(return_ErrorMessage)) {
                                //
                                // -- stop processing xml nodes if error
                                break;
                            }
                            //
                            // If the collection parsed correctly, update the Collections.xml file and exit loop
                            updateCollectionFolderConfig(core, Collectionname, collectionGuid, CollectionLastChangeDate, CollectionVersionFolderName);
                            break;
                        }
                        if (!string.IsNullOrEmpty(return_ErrorMessage)) {
                            //
                            // -- stop files if error
                            break;
                        }
                    }
                    // 
                    // -- all files finished
                    if (string.IsNullOrEmpty(return_ErrorMessage) && !CollectionXmlFileFound) {
                        //
                        // no errors, but xml file not found. Make an error
                        return_ErrorMessage = "<p>There was a problem with the installation. The collection zip was not downloaded successfully.</p>";
                    }
                } catch (Exception) {
                    throw;
                } finally {
                    //
                    // delete the tmp working folder
                    core.tempFiles.deleteFolder(tmpInstallPath);
                }
                //
                LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", BuildLocalCollectionFolder, Exiting with ErrorMessage [" + return_ErrorMessage + "]");
                //
                return string.IsNullOrEmpty(return_ErrorMessage);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            } finally {
                contextLog.Pop();
            }
        }
        //
        //====================================================================================================
        //
        public static void updateCollectionFolderConfig(CoreController core, string collectionname, string collectionGuid, DateTime collectionUpdatedDate, string collectionVersionFolderName) {
            try {
                XmlDocument Doc = new XmlDocument();
                try {
                    Doc.LoadXml(CollectionFolderModel.getCollectionFolderConfigXml(core));
                } catch (Exception ex) {
                    //
                    // -- exit, error loading file
                    string errMsg = "UpdateConfig, Error loading Collections.xml file";
                    logger.Error(ex, LogController.processLogMessage(core, errMsg, true));
                    return;
                }
                if (!Doc.DocumentElement.Name.ToLower(CultureInfo.InvariantCulture).Equals("collectionlist")) {
                    //
                    // -- exit, top node invalid
                    LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", UpdateConfig, The Collections.xml file has an invalid root node, [" + Doc.DocumentElement.Name + "] was received and [" + CollectionListRootNode + "] was expected.");
                    return;
                }
                bool collectionFound = false;
                foreach (XmlNode localListNode in Doc.DocumentElement.ChildNodes) {
                    if (localListNode.Name.ToLower(CultureInfo.InvariantCulture).Equals("collection")) {
                        //
                        // -- collection node
                        string localGuid = "";
                        foreach (XmlNode CollectionNode in localListNode.ChildNodes) {
                            if (CollectionNode.Name.ToLower(CultureInfo.InvariantCulture).Equals("guid")) {
                                localGuid = CollectionNode.InnerText.ToLower(CultureInfo.InvariantCulture);
                                break;
                            }
                        }
                        if (localGuid.Equals(collectionGuid.ToLower(CultureInfo.InvariantCulture))) {
                            collectionFound = true;
                            foreach (XmlNode collectionNode in localListNode.ChildNodes) {
                                switch (GenericController.toLCase(collectionNode.Name)) {
                                    case "name":
                                        collectionNode.InnerText = collectionname;
                                        break;
                                    case "lastchangedate":
                                        collectionNode.InnerText = collectionUpdatedDate.ToString();
                                        break;
                                    case "path":
                                        collectionNode.InnerText = collectionVersionFolderName;
                                        break;
                                }
                            }
                            break;
                        }
                    }
                }
                if (!collectionFound) {
                    XmlNode NewCollectionNode = Doc.CreateNode(XmlNodeType.Element, "collection", "");
                    //
                    XmlNode NewAttrNode = Doc.CreateNode(XmlNodeType.Element, "name", "");
                    NewAttrNode.InnerText = collectionname;
                    NewCollectionNode.AppendChild(NewAttrNode);
                    //
                    NewAttrNode = Doc.CreateNode(XmlNodeType.Element, "lastchangedate", "");
                    NewAttrNode.InnerText = collectionUpdatedDate.ToString();
                    NewCollectionNode.AppendChild(NewAttrNode);
                    //
                    NewAttrNode = Doc.CreateNode(XmlNodeType.Element, "guid", "");
                    NewAttrNode.InnerText = collectionGuid;
                    NewCollectionNode.AppendChild(NewAttrNode);
                    //
                    NewAttrNode = Doc.CreateNode(XmlNodeType.Element, "path", "");
                    NewAttrNode.InnerText = collectionVersionFolderName;
                    NewCollectionNode.AppendChild(NewAttrNode);
                    //
                    Doc.DocumentElement.AppendChild(NewCollectionNode);
                }
                //
                //
                // -- Save the result
                string LocalFilename = AddonController.getPrivateFilesAddonPath() + "Collections.xml";
                core.privateFiles.saveFile(LocalFilename, Doc.OuterXml);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Get the collection folder stored in the collection config file (xml file at root of the collection folder)
        /// </summary>
        /// <param name="core"></param>
        /// <param name="CollectionGuid"></param>
        /// <returns></returns>
        public static string getCollectionConfigFolderPath(CoreController core, string CollectionGuid) {
            var collectionFolder = CollectionFolderModel.getCollectionFolderConfig(core, CollectionGuid);
            if (collectionFolder != null) {
                if (string.IsNullOrWhiteSpace(collectionFolder.path)) { return string.Empty; }
                if (!collectionFolder.path.right(1).Equals("\\")) {
                    //
                    //-- tmp patch. non-empty path must end in a dos slash
                    return collectionFolder.path + "\\";
                }
                return collectionFolder.path;
            }
            return string.Empty;
        }
        //
        //======================================================================================================
        /// <summary>
        /// determine or create a collection version path (/private/addons/collectionFolder/collectionVersion) 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="collectionGuid"></param>
        /// <param name="CollectionName"></param>
        /// <returns></returns>
        public static string verifyCollectionVersionFolderName(CoreController core, string collectionGuid, string CollectionName) {
            collectionGuid = GenericController.normalizeGuid(collectionGuid);
            string CollectionVersionFolderName = getCollectionConfigFolderPath(core, collectionGuid);
            string CollectionFolderName = "";
            if (!string.IsNullOrEmpty(CollectionVersionFolderName)) {
                //
                // This is an upgrade
                //
                int Pos = GenericController.strInstr(1, CollectionVersionFolderName, "\\");
                if (Pos > 0) {
                    CollectionFolderName = CollectionVersionFolderName.left(Pos - 1);
                }
            } else {
                //
                // This is an install
                //
                CollectionFolderName = collectionGuid;
                CollectionFolderName = GenericController.strReplace(CollectionFolderName, "{", "");
                CollectionFolderName = GenericController.strReplace(CollectionFolderName, "}", "");
                CollectionFolderName = GenericController.strReplace(CollectionFolderName, "-", "");
                CollectionFolderName = GenericController.strReplace(CollectionFolderName, " ", "");
                CollectionFolderName = CollectionName + "_" + CollectionFolderName;
                CollectionFolderName = CollectionFolderName.ToLowerInvariant();
            }
            string CollectionFolder = AddonController.getPrivateFilesAddonPath() + CollectionFolderName + "\\";
            core.privateFiles.verifyPath(CollectionFolder);
            //
            // create a collection 'version' folder for these new files
            string TimeStamp = "";
            DateTime NowTime = default;
            NowTime = core.dateTimeNowMockable;
            int NowPart = NowTime.Year;
            TimeStamp += NowPart.ToString();
            NowPart = NowTime.Month;
            if (NowPart < 10) {
                TimeStamp += "0";
            }
            TimeStamp += NowPart.ToString();
            NowPart = NowTime.Day;
            if (NowPart < 10) {
                TimeStamp += "0";
            }
            TimeStamp += NowPart.ToString();
            NowPart = NowTime.Hour;
            if (NowPart < 10) {
                TimeStamp += "0";
            }
            TimeStamp += NowPart.ToString();
            NowPart = NowTime.Minute;
            if (NowPart < 10) {
                TimeStamp += "0";
            }
            TimeStamp += NowPart.ToString();
            NowPart = NowTime.Second;
            if (NowPart < 10) {
                TimeStamp += "0";
            }
            TimeStamp += NowPart.ToString();
            CollectionVersionFolderName = CollectionFolderName + "\\" + TimeStamp;
            string CollectionVersionFolder = AddonController.getPrivateFilesAddonPath() + CollectionVersionFolderName;
            string CollectionVersionPath = CollectionVersionFolder + "\\";
            core.privateFiles.createPath(CollectionVersionPath);
            return CollectionVersionFolderName;
        }
        //
        //======================================================================================================
        /// <summary>
        /// log the contextLog stack
        /// ContextLog stack is a tool to trace the collection installation to trace recursion
        /// </summary>
        /// <param name="core"></param>
        /// <param name="contextLog"></param>
        private static void traceContextLog(CoreController core, Stack<string> contextLog) {
            logger.Log(LogLevel.Info, LogController.processLogMessage(core, string.Join(",", contextLog), false));
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
