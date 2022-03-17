
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Exceptions;
using Contensive.Processor.Models.Domain;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Xml;
using static Contensive.BaseClasses.CPFileSystemBaseClass;
using static Contensive.Processor.Constants;

namespace Contensive.Processor.Controllers {
    //
    // install = means everything nessesary
    // buildfolder = means download and build out site
    //
    // todo: rework how adds are installed, this change can be done after weave launch
    // - current addon folder is called local addon folder and not in shared environment /local/addons
    // - add a node to the (local) collection.xml with last collection installation datetime (files added after this starts install)
    // - in private files, new folder with zip files to install /private/collectionInstall
    // - local server checks the list and runs install on new zips, if remote file system, download and install
    // - addon manager just copies zip file into the /private/collectionInstall folder
    //
    // todo -- To make it easy to add code to a site, be able to upload DLL files. Get the class names, find the collection and install in the correct collection folder
    //
    // todo -- Even in collection files, auto discover DLL file classes and create addons out of them. Create/update collections, create collection xml and install.
    //
    //====================================================================================================
    /// <summary>
    /// install addon collections.
    /// </summary>
    public static class CollectionInstallController {
        /// <summary>
        /// class logger initialization
        /// </summary>
        private static readonly Logger logger = NLog.LogManager.GetCurrentClassLogger();
        //
        //======================================================================================================
        /// <summary>
        /// Install the base collection to this applicaiton
        /// create the base collection by zipping the program files folder, then install it
        /// calls installCollectionFromFile
        /// </summary>
        /// <param name="core"></param>
        /// <param name="contextLog">For logging. A list of reasons why this installation was called, the last explaining this call, the one before explain the reason the caller was installed.</param>
        /// <param name="isNewBuild"></param>
        /// <param name="reinstallDependencies"></param>
        /// <param name="nonCriticalErrorList"></param>
        /// <param name="logPrefix"></param>
        /// <param name="collectionsInstalledList">A list of collection guids that are already installed this pass. All collections that install will be added to it. </param>
        public static void installBaseCollection(CoreController core, Stack<string> contextLog, bool isNewBuild, bool reinstallDependencies, ref List<string> nonCriticalErrorList, string logPrefix, List<string> collectionsInstalledList) {
            try {
                contextLog.Push("installBaseCollection");
                traceContextLog(core, contextLog);
                //
                // -- new build
                const string baseCollectionXmlFilename = "aoBase51.xml";
                string baseCollectionXml = core.programFiles.readFileText(baseCollectionXmlFilename);
                if (string.IsNullOrEmpty(baseCollectionXml)) {
                    //
                    // -- base collection notfound
                    throw new GenericException("installBaseCollection, cannot load base collection [" + core.programFiles.localAbsRootPath + "aoBase51.xml]");
                }
                {
                    //
                    // -- Special Case - must install base collection metadata first because it builds the system that the system needs to do everything else
                    LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", installBaseCollection, install metadata first to verify system requirements");

                    CollectionInstallMetadataController.installMetaDataMiniCollectionFromXml(core, baseCollectionXml, isNewBuild, reinstallDependencies, true, logPrefix);
                }
                {
                    //
                    // now copy all program files to a temp folder and create a zip (xml plus any resources installed) and install
                    const string baseCollectionZipFilename = "aoBase51.zip";
                    string installTempPath = "installBaseCollection" + GenericController.getRandomInteger(core) + "\\";
                    try {
                        core.tempFiles.createPath(installTempPath);
                        core.programFiles.copyPath("\\", installTempPath, core.tempFiles);
                        //
                        // -- copy all files from subfolders to root to be compatible with installation system
                        foreach ( FolderDetail subFolder in  core.tempFiles.getFolderList(installTempPath) ) {
                            copyTempSubfoldersToRoot(core, installTempPath + subFolder.Name + "\\", installTempPath);
                        }                        
                        //
                        // -- remove files not needed: dll, exe, config, pdb as they will all be found in the program files path for built in addons
                        foreach ( FileDetail file in core.tempFiles.getFileList(installTempPath)) {
                            string filename = file.Name.ToLowerInvariant();
                            if ((filename.right(4)==".dll") || (filename.right(11) == ".dll.config") || (filename.right(4) == ".exe") || (filename.right(11) == ".exe.config") || (filename.right(4) == ".pdb")) {
                                core.tempFiles.deleteFile(installTempPath + filename);
                            }
                        }
                        core.tempFiles.deleteFile(installTempPath + "ContensiveDbModels.xml");
                        core.tempFiles.deleteFile(installTempPath + "CPBase.xml");
                        core.tempFiles.deleteFile(installTempPath + "Processor.xml");
                        core.tempFiles.deleteFile(installTempPath + "nlog.config");
                        core.tempFiles.deleteFile(installTempPath + "ClearScriptV8.ICU.dat");
                        core.tempFiles.deleteFile(installTempPath + "DefaultAspxSite.zip");
                        //
                        core.tempFiles.zipPath(baseCollectionZipFilename, installTempPath);
                        //
                        core.tempFiles.deleteFolder(installTempPath);
                        //
                        string baseCollectionZipPathFilename = installTempPath + baseCollectionZipFilename;
                        core.tempFiles.copyFile(baseCollectionZipFilename, baseCollectionZipPathFilename);
                        core.tempFiles.deleteFile(baseCollectionZipFilename);
                        //
                        //core.programFiles.copyFile(baseCollectionFilename, installTempPath + baseCollectionFilename, core.tempFiles);
                        string installErrorMessage = "";
                        string installedCollectionGuid = "";
                        bool isDependency = false;
                        if (!installCollectionFromTempFile(core, isDependency, contextLog, baseCollectionZipPathFilename, ref installErrorMessage, ref installedCollectionGuid, isNewBuild, reinstallDependencies, ref nonCriticalErrorList, logPrefix, ref collectionsInstalledList)) {
                            throw new GenericException("installBaseCollection, call to installCollectionFromPrivateFile failed, message returned [" + installErrorMessage + "]");
                        }
                    } catch (Exception ex) {
                        LogController.logError(core, ex);
                        throw;
                    } finally {
                        //
                        // -- remove temp folder
                        core.tempFiles.deleteFolder(installTempPath);
                        //
                        // -- invalidate cache
                        core.cache.invalidateAll();
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex, "installBaseCollection, unexpected exception");
                throw;
            } finally {
                contextLog.Pop();
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Primary collection installation method. 
        /// If collection not already installed during this install, mark it installed and install
        /// </summary>
        /// <param name="core"></param>
        /// <param name="contextLog"></param>
        /// <param name="collectionGuid"></param>
        /// <param name="return_ErrorMessage"></param>
        /// <param name="IsNewBuild"></param>
        /// <param name="reinstallDependencies"></param>
        /// <param name="nonCriticalErrorList"></param>
        /// <param name="logPrefix"></param>
        /// <param name="collectionsInstalledList"></param>
        /// <param name="includeBaseMetaDataInstall"></param>
        /// <param name="collectionsDownloaded">Collections downloaded but not installed yet. Do not need to download them again.</param>
        /// <param name="isDependency"></param>
        /// <returns></returns>
        public static bool installCollectionFromCollectionFolder(CoreController core, bool isDependency, Stack<string> contextLog, string collectionGuid, ref string return_ErrorMessage, bool IsNewBuild, bool reinstallDependencies, ref List<string> nonCriticalErrorList, string logPrefix, ref List<string> collectionsInstalledList, bool includeBaseMetaDataInstall, ref List<string> collectionsDownloaded) {
            bool result = false;
            try {
                //
                contextLog.Push(MethodInfo.GetCurrentMethod().Name + ", [" + collectionGuid + "]");
                traceContextLog(core, contextLog);
                //
                if (collectionsInstalledList.Contains(collectionGuid.ToLower(CultureInfo.InvariantCulture))) {
                    //
                    // -- EXIT, this collection has already been installed during this installation process. Skip and return success
                    LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", [" + collectionGuid + "] was not installed because it was previously installed during this installation.");
                    return true;
                }
                // -- collection needs to be installed
                if (!collectionsInstalledList.Contains(collectionGuid.ToLower(CultureInfo.InvariantCulture))) {
                    collectionsInstalledList.Add(collectionGuid.ToLower(CultureInfo.InvariantCulture));
                }
                //
                var collectionFolderConfig = CollectionFolderModel.getCollectionFolderConfig(core, collectionGuid);
                if ((collectionFolderConfig == null) || string.IsNullOrEmpty(collectionFolderConfig.path)) {
                    //
                    // -- ERROR, collection folder not found
                    LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", installCollectionFromAddonCollectionFolder [" + collectionGuid + "], collection folder not found.");
                    return_ErrorMessage += "The collection was not installed from the local collections because the folder containing the Add-on's resources could not be found. It may not be installed locally.";
                    return false;
                }
                //
                // Search Local Collection Folder for collection config file (xml file)
                //
                string CollectionVersionFolder = AddonController.getPrivateFilesAddonPath() + collectionFolderConfig.path + "\\";
                List<FileDetail> srcFileInfoArray = core.privateFiles.getFileList(CollectionVersionFolder);
                if (srcFileInfoArray.Count == 0) {
                    //
                    // -- EXIT, ERROR, collection folder was empty
                    LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", installCollectionFromAddonCollectionFolder [" + collectionGuid + "], collection folder is empty.");
                    return_ErrorMessage += "The collection was not installed because the folder containing the Add-on's resources was empty.";
                    return false;
                }
                //
                // collect list of DLL files and add them to the exec files if they were missed
                List<string> assembliesInZip = new List<string>();
                foreach (FileDetail file in srcFileInfoArray) {
                    if (file.Extension.ToLowerInvariant() == ".dll") {
                        if (!assembliesInZip.Contains(file.Name.ToLowerInvariant())) {
                            LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", installCollectionFromAddonCollectionFolder [" + collectionGuid + "], adding DLL from folder[" + file.Name.ToLowerInvariant() + "].");
                            assembliesInZip.Add(file.Name.ToLowerInvariant());
                        }
                    }
                }
                //
                // -- Process the other files
                LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", installCollectionFromAddonCollectionFolder [" + collectionGuid + "], process xml files.");
                foreach (FileDetail file in srcFileInfoArray) {
                    if (file.Extension == ".xml") {
                        //
                        // -- XML file -- open it to figure out if it is one we can use
                        XmlDocument Doc = new XmlDocument();
                        string CollectionFilename = file.Name;
                        bool loadOK = true;
                        string collectionFileContent = core.privateFiles.readFileText(CollectionVersionFolder + file.Name);
                        try {
                            Doc.LoadXml(collectionFileContent);
                        } catch (Exception ex) {
                            //
                            // error - Need a way to reach the user that submitted the file
                            //
                            string errMsg = "skipping xml file, not valid collection metadata, [" + core.privateFiles.localAbsRootPath + CollectionVersionFolder + file.Name + "]";
                            logger.Error(ex, LogController.processLogMessage(core, errMsg, true));
                            loadOK = false;
                        }
                        if (loadOK) {
                            if ((Doc.DocumentElement.Name.ToLowerInvariant() == GenericController.toLCase(CollectionFileRootNode)) || (Doc.DocumentElement.Name.ToLowerInvariant() == GenericController.toLCase(CollectionFileRootNodeOld))) {
                                //
                                //------------------------------------------------------------------------------------------------------
                                // Collection File - import from sub so it can be re-entrant
                                //------------------------------------------------------------------------------------------------------
                                //
                                string CollectionName = XmlController.getXMLAttribute(core, Doc.DocumentElement, "name", "");
                                if (string.IsNullOrEmpty(CollectionName)) {
                                    //
                                    // ----- Error condition -- it must have a collection name
                                    //
                                    LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", installCollectionFromAddonCollectionFolder [" + CollectionName + "], collection has no name");
                                    return_ErrorMessage += "The collection was not installed because the collection name in the xml collection file is blank";
                                    return false;
                                }
                                bool CollectionSystem = GenericController.encodeBoolean(XmlController.getXMLAttribute(core, Doc.DocumentElement, "system", "false"));
                                string collectionOninstalladdonGuid = XmlController.getXMLAttribute(core, Doc.DocumentElement, "onInstallAddonGuid", "");
                                string dataRecordList = XmlController.getXMLAttribute(core, Doc.DocumentElement, "DataRecordList", "");
                                int Parent_NavId = BuildController.verifyNavigatorEntry(core, new MetadataMiniCollectionModel.MiniCollectionMenuModel {
                                    guid = addonGuidManageAddon,
                                    name = "Manage Add-ons",
                                    adminOnly = false,
                                    developerOnly = false,
                                    newWindow = false,
                                    active = true,
                                }, 0);
                                bool CollectionUpdatable = GenericController.encodeBoolean(XmlController.getXMLAttribute(core, Doc.DocumentElement, "updatable", "true"));
                                bool CollectionblockNavigatorNode = GenericController.encodeBoolean(XmlController.getXMLAttribute(core, Doc.DocumentElement, "blockNavigatorNode", "false"));
                                string FileGuid = XmlController.getXMLAttribute(core, Doc.DocumentElement, "guid", CollectionName);
                                if (string.IsNullOrEmpty(FileGuid)) {
                                    FileGuid = CollectionName;
                                }
                                if (collectionGuid.ToLowerInvariant() != GenericController.toLCase(FileGuid)) {
                                    //
                                    //
                                    //
                                    LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", installCollectionFromAddonCollectionFolder [" + CollectionName + "], Collection file contains incorrect GUID, correct GUID [" + collectionGuid.ToLowerInvariant() + "], incorrect GUID in file [" + GenericController.toLCase(FileGuid) + "]");
                                    return_ErrorMessage += " The collection was not installed because the unique number identifying the collection, called the guid, does not match the collection requested.";
                                    return false;
                                }
                                if (string.IsNullOrEmpty(collectionGuid)) {
                                    //
                                    // I hope I do not regret this
                                    //
                                    collectionGuid = CollectionName;
                                }
                                AddonCollectionModel collection = AddonCollectionModel.create<AddonCollectionModel>(core.cpParent, collectionGuid);
                                if ((collection != null) && !CollectionUpdatable) {
                                    //
                                    // -- New collection Not Updateable and collection already exists
                                    string message = "The collection [" + CollectionName + "] was not installed because the new collection is marked not-updateable and the current collection is already installed";
                                    LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", installCollectionFromAddonCollectionFolder [" + CollectionName + "], " + message);
                                    return_ErrorMessage += message;
                                    return true;
                                } else if ((collection != null) && !collection.updatable) {
                                    //
                                    // -- Current collection is not updateable
                                    string message = "The collection [" + CollectionName + "] was not installed because the current collection is marked not-updateable";
                                    LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", installCollectionFromAddonCollectionFolder [" + CollectionName + "], " + message);
                                    return_ErrorMessage += message;
                                    return true;
                                }
                                //
                                //-------------------------------------------------------------------------------
                                LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", installCollectionFromAddonCollectionFolder [" + CollectionName + "], stage-1, save resourses and process collection dependencies");
                                // Go through all collection nodes
                                // Process ImportCollection Nodes - so includeaddon nodes will work
                                // these must be processes regardless of the state of this collection in this app
                                // Get Resource file list
                                //-------------------------------------------------------------------------------
                                //
                                string wwwFileList = "";
                                string ContentFileList = "";
                                string ExecFileList = "";
                                bool collectionIncludesDiagnosticAddon = false;
                                foreach (XmlNode MetaDataSection in Doc.DocumentElement.ChildNodes) {
                                    switch (MetaDataSection.Name.ToLowerInvariant()) {
                                        case "resource": {
                                                //
                                                // set wwwfilelist, contentfilelist, execfilelist
                                                //
                                                string resourceType = XmlController.getXMLAttribute(core, MetaDataSection, "type", "");
                                                string resourcePath = XmlController.getXMLAttribute(core, MetaDataSection, "path", "");
                                                string filename = XmlController.getXMLAttribute(core, MetaDataSection, "name", "");
                                                //
                                                LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", installCollectionFromAddonCollectionFolder [" + CollectionName + "], resource found, name [" + filename + "], type [" + resourceType + "], path [" + resourcePath + "]");
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
                                                filename = core.privateFiles.correctFilenameCase(CollectionVersionFolder + SrcPath + filename);
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
                                                            LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", CollectionName [" + CollectionName + "], GUID [" + collectionGuid + "], pass 1, copying file to wwwFiles, src [" + CollectionVersionFolder + SrcPath + "], dst [" + core.appConfig.localWwwPath + dstDosPath + "].");
                                                            core.privateFiles.copyFile(CollectionVersionFolder + SrcPath + filename, dstDosPath + filename, core.wwwFiles);
                                                            if (GenericController.toLCase(filename.Substring(filename.Length - 4)) == ".zip") {
                                                                LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", installCollectionFromAddonCollectionFolder [" + CollectionName + "], GUID [" + collectionGuid + "], pass 1, unzipping www file [" + core.appConfig.localWwwPath + dstDosPath + filename + "].");
                                                                core.wwwFiles.unzipFile(dstDosPath + filename);
                                                                core.wwwFiles.deleteFile(dstDosPath + filename);
                                                            }
                                                            break;
                                                        }
                                                    case "privatefiles":
                                                    case "private": {
                                                            ContentFileList += Environment.NewLine + dstDosPath + filename;
                                                            LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", CollectionName [" + CollectionName + "], GUID [" + collectionGuid + "], pass 1, copying file to privateFiles, src [" + CollectionVersionFolder + SrcPath + "], dst [" + dstDosPath + "].");
                                                            core.privateFiles.copyFile(CollectionVersionFolder + SrcPath + filename, dstDosPath + filename);
                                                            if (GenericController.toLCase(filename.Substring(filename.Length - 4)) == ".zip") {
                                                                LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", CollectionName [" + CollectionName + "], GUID [" + collectionGuid + "], pass 1, unzipping privateFiles file [" + dstDosPath + filename + "].");
                                                                core.privateFiles.unzipFile(dstDosPath + filename);
                                                                core.privateFiles.deleteFile(dstDosPath + filename);
                                                            }
                                                            break;
                                                        }
                                                    case "file":
                                                    case "files":
                                                    case "cdnfiles":
                                                    case "content": {
                                                            ContentFileList += Environment.NewLine + dstDosPath + filename;
                                                            LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", CollectionName [" + CollectionName + "], GUID [" + collectionGuid + "], pass 1, copying file to cdnFiles, src [" + CollectionVersionFolder + SrcPath + "], dst [" + dstDosPath + "].");
                                                            core.privateFiles.copyFile(CollectionVersionFolder + SrcPath + filename, dstDosPath + filename, core.cdnFiles);
                                                            if (GenericController.toLCase(filename.Substring(filename.Length - 4)) == ".zip") {
                                                                LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", CollectionName [" + CollectionName + "], GUID [" + collectionGuid + "], pass 1, unzipping cdnFiles [" + dstDosPath + filename + "].");
                                                                core.cdnFiles.unzipFile(dstDosPath + filename);
                                                                core.cdnFiles.deleteFile(dstDosPath + filename);
                                                            }
                                                            break;
                                                        }
                                                    default: {
                                                            if (assembliesInZip.Contains(filename.ToLowerInvariant())) {
                                                                assembliesInZip.Remove(filename.ToLowerInvariant());
                                                            }
                                                            ExecFileList = ExecFileList + Environment.NewLine + filename;
                                                            break;
                                                        }
                                                }
                                                break;
                                            }
                                        case "getcollection":
                                        case "importcollection": {
                                                //
                                                // Get path to this collection and call into it
                                                //
                                                string ChildCollectionName = XmlController.getXMLAttribute(core, MetaDataSection, "name", "");
                                                string ChildCollectionGUId = XmlController.getXMLAttribute(core, MetaDataSection, "guid", MetaDataSection.InnerText);
                                                if (string.IsNullOrEmpty(ChildCollectionGUId)) {
                                                    ChildCollectionGUId = MetaDataSection.InnerText;
                                                }
                                                if (collectionsInstalledList.Contains(ChildCollectionGUId.ToLower(CultureInfo.InvariantCulture))) {
                                                    //
                                                    // circular import detected, this collection is already imported
                                                    //
                                                    LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", installCollectionFromAddonCollectionFolder [" + CollectionName + "], Circular import detected. This collection attempts to import a collection that had previously been imported. A collection can not import itself. The collection is [" + CollectionName + "], GUID [" + collectionGuid + "], pass 1. The collection to be imported is [" + ChildCollectionName + "], GUID [" + ChildCollectionGUId + "]");
                                                } else {
                                                    //
                                                    // -- all included collections should already be installed, because buildfolder is called before call
                                                    installCollectionFromCollectionFolder(core, true, contextLog, ChildCollectionGUId, ref return_ErrorMessage, IsNewBuild, reinstallDependencies, ref nonCriticalErrorList, logPrefix, ref collectionsInstalledList, false, ref collectionsDownloaded);
                                                }
                                                break;
                                            }
                                        default: {
                                                // do nothing
                                                break;
                                            }
                                    }
                                }
                                //
                                // -- any assemblies found in the zip that were not part of the resources section need to be added
                                foreach (string filename in assembliesInZip) {
                                    ExecFileList = ExecFileList + Environment.NewLine + filename;
                                }
                                //
                                //-------------------------------------------------------------------------------
                                LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", installCollectionFromAddonCollectionFolder [" + CollectionName + "], stage-2, determine if this collection is already installed");
                                //-------------------------------------------------------------------------------
                                //
                                bool OKToInstall = false;
                                if (collection != null) {
                                    //
                                    // Upgrade addon
                                    //
                                    if (!isDependency) {
                                        LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", installCollectionFromAddonCollectionFolder [" + CollectionName + "], GUID [" + collectionGuid + "], Install non-dependency collection.");
                                        OKToInstall = true;
                                    } else if (reinstallDependencies) {
                                        LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", installCollectionFromAddonCollectionFolder [" + CollectionName + "], GUID [" + collectionGuid + "], dependent collection is up-to-date but installation set to reinstall all collections.");
                                        OKToInstall = true;
                                    } else if (collectionFolderConfig.lastChangeDate == DateTime.MinValue) {
                                        LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", installCollectionFromAddonCollectionFolder [" + CollectionName + "], GUID [" + collectionGuid + "], dependent collection installedDate could not be determined so it will upgrade.");
                                        OKToInstall = true;
                                    } else if (collectionFolderConfig.lastChangeDate > collection.modifiedDate) {
                                        LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", installCollectionFromAddonCollectionFolder [" + CollectionName + "], GUID [" + collectionGuid + "], dependent collection is out-of-date and will be upgraded.");
                                        OKToInstall = true;
                                    } else {
                                        LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", installCollectionFromAddonCollectionFolder [" + CollectionName + "], GUID [" + collectionGuid + "], dependent collection is up-to-date and will not be upgraded, but all imports in the new version will be checked.");
                                        OKToInstall = false;
                                    }
                                } else {
                                    //
                                    // Install new on this application
                                    //
                                    collection = AddonCollectionModel.addEmpty<AddonCollectionModel>(core.cpParent);
                                    LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", installCollectionFromAddonCollectionFolder [" + CollectionName + "], GUID [" + collectionGuid + "], App does not have this collection so it will be installed.");
                                    OKToInstall = true;
                                }
                                if (!OKToInstall) {
                                    //
                                    // Do not install, but still check all imported collections to see if they need to be installed
                                    // imported collections moved in front this check
                                    //
                                } else {
                                    //
                                    //-------------------------------------------------------------------------------
                                    LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", installCollectionFromAddonCollectionFolder [" + CollectionName + "], stage-3, prepare to import full collection");
                                    //-------------------------------------------------------------------------------
                                    //
                                    {
                                        string CollectionHelpLink = "";
                                        foreach (XmlNode metaDataSection in Doc.DocumentElement.ChildNodes) {
                                            if (metaDataSection.Name.ToLowerInvariant() == "helplink") {
                                                //
                                                // only save the first
                                                CollectionHelpLink = metaDataSection.InnerText;
                                                break;
                                            }
                                        }
                                        //
                                        // ----- set or clear all fields
                                        collection.name = CollectionName;
                                        collection.help = "";
                                        collection.ccguid = collectionGuid;
                                        collection.lastChangeDate = collectionFolderConfig.lastChangeDate;
                                        collection.system = CollectionSystem;
                                        collection.updatable = CollectionUpdatable;
                                        collection.blockNavigatorNode = CollectionblockNavigatorNode;
                                        collection.helpLink = CollectionHelpLink;
                                        //
                                        MetadataController.deleteContentRecords(core, "Add-on Collection CDef Rules", "CollectionID=" + collection.id);
                                        MetadataController.deleteContentRecords(core, "Add-on Collection Parent Rules", "ParentID=" + collection.id);
                                        //
                                        // Store all resource found, new way and compatibility way
                                        //
                                        collection.contentFileList = ContentFileList;
                                        collection.execFileList = ExecFileList;
                                        collection.wwwFileList = wwwFileList;
                                        //
                                        // ----- remove any current navigator nodes installed by the collection previously
                                        //
                                        if (collection.id != 0) {
                                            MetadataController.deleteContentRecords(core, NavigatorEntryModel.tableMetadata.contentName, "installedbycollectionid=" + collection.id);
                                        }
                                        collection.save(core.cpParent);
                                    }
                                    //
                                    //-------------------------------------------------------------------------------
                                    LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", installCollectionFromAddonCollectionFolder [" + CollectionName + "], stage-4, isolate and process schema-relatednodes (metadata,index,etc)");
                                    //-------------------------------------------------------------------------------
                                    //
                                    bool isBaseCollection = (baseCollectionGuid.ToLowerInvariant() == collectionGuid.ToLowerInvariant());
                                    if (!isBaseCollection || includeBaseMetaDataInstall) {
                                        string metaDataMiniCollection = "";
                                        foreach (XmlNode metaDataSection in Doc.DocumentElement.ChildNodes) {
                                            switch (metaDataSection.Name.ToLowerInvariant()) {
                                                case "contensivecdef": {
                                                        //
                                                        // old metadata section -- take the inner
                                                        //
                                                        foreach (XmlNode ChildNode in metaDataSection.ChildNodes) {
                                                            metaDataMiniCollection += Environment.NewLine + ChildNode.OuterXml;
                                                        }
                                                        break;
                                                    }
                                                case "cdef":
                                                case "sqlindex":
                                                case "style":
                                                case "styles":
                                                case "stylesheet":
                                                case "adminmenu":
                                                case "menuentry":
                                                case "navigatorentry": {
                                                        //
                                                        // handled by Upgrade class
                                                        metaDataMiniCollection += metaDataSection.OuterXml;
                                                        break;
                                                    }
                                                default: {
                                                        // do nothing
                                                        break;
                                                    }
                                            }
                                        }
                                        //
                                        // -- install metadataMiniCollection
                                        if (!string.IsNullOrEmpty(metaDataMiniCollection)) {
                                            //
                                            // -- Use the upgrade code to import this part
                                            metaDataMiniCollection = "<" + CollectionFileRootNode + " name=\"" + CollectionName + "\" guid=\"" + collectionGuid + "\">" + metaDataMiniCollection + "</" + CollectionFileRootNode + ">";
                                            CollectionInstallMetadataController.installMetaDataMiniCollectionFromXml(core, metaDataMiniCollection, IsNewBuild, reinstallDependencies, isBaseCollection, logPrefix);
                                            //
                                            // -- Process nodes to save Collection data
                                            XmlDocument NavDoc = new XmlDocument();
                                            loadOK = true;
                                            try {
                                                NavDoc.LoadXml(metaDataMiniCollection);
                                            } catch (Exception ex) {
                                                //
                                                // error - Need a way to reach the user that submitted the file
                                                //
                                                string errMsg = "[" + CollectionName + "], creating navigator entries, there was an error parsing the portion of the collection that contains metadata. Navigator entry creation was aborted. [There was an error reading the Meta data file.]";
                                                logger.Error(ex, LogController.processLogMessage(core, errMsg, true));
                                                return_ErrorMessage += " The collection was not installed because the xml collection file has an error.";
                                                return false;
                                            }
                                            if (loadOK) {
                                                foreach (XmlNode metaDataNode in NavDoc.DocumentElement.ChildNodes) {
                                                    switch (GenericController.toLCase(metaDataNode.Name)) {
                                                        case "cdef": {
                                                                string ContentName = XmlController.getXMLAttribute(core, metaDataNode, "name", "");
                                                                //
                                                                // setup metadata rule
                                                                //
                                                                int ContentId = ContentMetadataModel.getContentId(core, ContentName);
                                                                if (ContentId > 0) {
                                                                    using (var csData = new CsModel(core)) {
                                                                        csData.insert("Add-on Collection CDef Rules");
                                                                        if (csData.ok()) {
                                                                            csData.set("Contentid", ContentId);
                                                                            csData.set("CollectionID", collection.id);
                                                                        }
                                                                    }
                                                                }
                                                                break;
                                                            }
                                                        default: {
                                                                // do nothing
                                                                break;
                                                            }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    //
                                    //-------------------------------------------------------------------------------
                                    LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", installCollectionFromAddonCollectionFolder [" + CollectionName + "], stage-5, create data records from data nodes, ignore fields");
                                    //-------------------------------------------------------------------------------
                                    //
                                    {
                                        foreach (XmlNode metaDataSection in Doc.DocumentElement.ChildNodes) {
                                            switch (GenericController.toLCase(metaDataSection.Name)) {
                                                case "data": {
                                                        //
                                                        // import content
                                                        //   This can only be done with matching guid
                                                        //
                                                        foreach (XmlNode ContentNode in metaDataSection.ChildNodes) {
                                                            if (GenericController.toLCase(ContentNode.Name) == "record") {
                                                                //
                                                                // Data.Record node
                                                                //
                                                                string ContentName = XmlController.getXMLAttribute(core, ContentNode, "content", "");
                                                                if (string.IsNullOrEmpty(ContentName)) {
                                                                    LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", installCollectionFromAddonCollectionFolder [" + CollectionName + "], install collection file contains a data.record node with a blank content attribute.");
                                                                    result = false;
                                                                    return_ErrorMessage += " Collection file [" + CollectionName + "] contains a data.record node with a blank content attribute.";
                                                                    return false;
                                                                } else {
                                                                    string ContentRecordGuid = XmlController.getXMLAttribute(core, ContentNode, "guid", "");
                                                                    string ContentRecordName = XmlController.getXMLAttribute(core, ContentNode, "name", "");
                                                                    if ((string.IsNullOrEmpty(ContentRecordGuid)) && (string.IsNullOrEmpty(ContentRecordName))) {
                                                                        LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", installCollectionFromAddonCollectionFolder [" + CollectionName + "], install collection file contains a data record node with neither guid nor name. It must have either a name or a guid attribute. The content is [" + ContentName + "]");
                                                                        result = false;
                                                                        return_ErrorMessage += " The collection [" + CollectionName + "] was not installed because the Collection file contains a data record node with neither name nor guid. This is not allowed. The content is [" + ContentName + "].";
                                                                        return false;
                                                                    } else {
                                                                        //
                                                                        // create or update the record
                                                                        //
                                                                        ContentMetadataModel metaData = ContentMetadataModel.createByUniqueName(core, ContentName);
                                                                        using (var csData = new CsModel(core)) {
                                                                            if (!string.IsNullOrEmpty(ContentRecordGuid)) {
                                                                                csData.open(ContentName, "ccguid=" + DbController.encodeSQLText(ContentRecordGuid));
                                                                            } else {
                                                                                csData.open(ContentName, "name=" + DbController.encodeSQLText(ContentRecordName));
                                                                            }
                                                                            bool recordfound = true;
                                                                            if (!csData.ok()) {
                                                                                //
                                                                                // Insert the new record
                                                                                //
                                                                                recordfound = false;
                                                                                csData.close();
                                                                                csData.insert(ContentName);
                                                                            }
                                                                            if (csData.ok()) {
                                                                                //
                                                                                // Update the record
                                                                                //
                                                                                if (recordfound && (!string.IsNullOrEmpty(ContentRecordGuid))) {
                                                                                    //
                                                                                    // found by guid, use guid in list and save name
                                                                                    //
                                                                                    csData.set("name", ContentRecordName);
                                                                                } else if (recordfound) {
                                                                                    //
                                                                                    // record found by name, use name is list but do not add guid
                                                                                    //
                                                                                } else {
                                                                                    //
                                                                                    // record was created
                                                                                    //
                                                                                    csData.set("ccguid", ContentRecordGuid);
                                                                                    csData.set("name", ContentRecordName);
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        break;
                                                    }
                                                default: {
                                                        // do nothing
                                                        break;
                                                    }
                                            }
                                        }
                                    }
                                    //
                                    //-------------------------------------------------------------------------------
                                    LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", installCollectionFromAddonCollectionFolder [" + CollectionName + "], stage-6, install addon nodes, set importcollection relationships");
                                    //-------------------------------------------------------------------------------
                                    //
                                    foreach (XmlNode metaDataSection in Doc.DocumentElement.ChildNodes) {
                                        switch (GenericController.toLCase(metaDataSection.Name)) {
                                            case "cdef":
                                            case "data":
                                            case "help":
                                            case "resource":
                                            case "helplink": {
                                                    //
                                                    // ignore - processed in previous passes
                                                    break;
                                                }
                                            case "getcollection":
                                            case "importcollection": {
                                                    //
                                                    // processed, but add rule for collection record
                                                    string ChildCollectionGuid = XmlController.getXMLAttribute(core, metaDataSection, "guid", metaDataSection.InnerText);
                                                    if (string.IsNullOrEmpty(ChildCollectionGuid)) {
                                                        ChildCollectionGuid = metaDataSection.InnerText;
                                                    }
                                                    if (!string.IsNullOrEmpty(ChildCollectionGuid)) {
                                                        using (var csData = new CsModel(core)) {
                                                            csData.open("Add-on Collections", "ccguid=" + DbController.encodeSQLText(ChildCollectionGuid));
                                                            int ChildCollectionId = 0;
                                                            if (csData.ok()) {
                                                                ChildCollectionId = csData.getInteger("id");
                                                            }
                                                            csData.close();
                                                            if (ChildCollectionId != 0) {
                                                                csData.insert("Add-on Collection Parent Rules");
                                                                if (csData.ok()) {
                                                                    csData.set("ParentID", collection.id);
                                                                    csData.set("ChildID", ChildCollectionId);
                                                                }
                                                                csData.close();
                                                            }
                                                        }
                                                    }
                                                    break;
                                                }
                                            case "scriptingmodule":
                                            case "scriptingmodules": {
                                                    result = false;
                                                    return_ErrorMessage += " Collection [" + CollectionName + "] includes a scripting module which is no longer supported. Move scripts to the code tab.";
                                                    return false;
                                                }
                                            case "sharedstyle": {
                                                    result = false;
                                                    return_ErrorMessage += " Collection [" + CollectionName + "] includes a shared style which is no longer supported. Move styles to the default styles tab.";
                                                    return false;
                                                }
                                            case "addon":
                                            case "add-on": {
                                                    //
                                                    // Add-on Node, do part 1 of 2
                                                    //   (include add-on node must be done after all add-ons are installed)
                                                    //
                                                    CollectionInstallAddonController.installNode(core, metaDataSection, "ccguid", collection.id, ref result, ref return_ErrorMessage, ref collectionIncludesDiagnosticAddon);
                                                    if (!result) { return result; }
                                                    break;
                                                }
                                            case "interfaces": {
                                                    //
                                                    // Legacy Interface Node
                                                    //
                                                    foreach (XmlNode metaDataInterfaces in metaDataSection.ChildNodes) {
                                                        CollectionInstallAddonController.installNode(core, metaDataInterfaces, "ccguid", collection.id, ref result, ref return_ErrorMessage, ref collectionIncludesDiagnosticAddon);
                                                        if (!result) { return result; }
                                                    }
                                                    break;
                                                }
                                            case "layout": {
                                                    //
                                                    // -- layouts
                                                    CollectionInstallLayoutController.installNode(core, metaDataSection, collection.id, ref result, ref return_ErrorMessage, ref collectionIncludesDiagnosticAddon);
                                                    if (!result) { return result; }
                                                    break;
                                                }
                                            case "template": {
                                                    //
                                                    // -- template
                                                    CollectionInstallTemplateController.installNode(core, metaDataSection, collection.id, ref result, ref return_ErrorMessage, ref collectionIncludesDiagnosticAddon);
                                                    if (!result) { return result; }
                                                    break;
                                                }
                                            default: {
                                                    // do nothing
                                                    break;
                                                }
                                        }
                                    }
                                    //
                                    //-------------------------------------------------------------------------------
                                    LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", installCollectionFromAddonCollectionFolder [" + CollectionName + "], stage-7, set addon dependency relationships");
                                    //-------------------------------------------------------------------------------
                                    //
                                    foreach (XmlNode collectionNode in Doc.DocumentElement.ChildNodes) {
                                        switch (collectionNode.Name.ToLowerInvariant()) {
                                            case "addon":
                                            case "add-on": {
                                                    //
                                                    // Add-on Node, do part 1, verify the addon in the table with name and guid
                                                    setAddonDependencies(core, CollectionName, collectionNode, "ccguid", core.siteProperties.dataBuildVersion, collection.id, ref result, ref return_ErrorMessage);
                                                    if (!result) { return result; }
                                                    break;
                                                }
                                            case "interfaces": {
                                                    //
                                                    // Legacy Interface Node
                                                    //
                                                    foreach (XmlNode metaDataInterfaces in collectionNode.ChildNodes) {
                                                        setAddonDependencies(core, CollectionName, metaDataInterfaces, "ccguid", core.siteProperties.dataBuildVersion, collection.id, ref result, ref return_ErrorMessage);
                                                        if (!result) { return result; }
                                                    }
                                                    break;
                                                }
                                            default: {
                                                    // do nothing
                                                    break;
                                                }
                                        }
                                    }
                                    //
                                    //-------------------------------------------------------------------------------
                                    LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", installCollectionFromAddonCollectionFolder [" + CollectionName + "], stage-8, process data nodes, set record fields");
                                    //-------------------------------------------------------------------------------
                                    //
                                    foreach (XmlNode metaDataSection in Doc.DocumentElement.ChildNodes) {
                                        if (metaDataSection.Name.ToLower().Equals("data")) {
                                            installDataNode(core, metaDataSection, ref return_ErrorMessage);
                                        }
                                    }
                                    //
                                    //----------------------------------------------------------------------------------------------------------------------
                                    LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", verify all navigator menu entries for updated addons");
                                    //----------------------------------------------------------------------------------------------------------------------
                                    //
                                    MetadataMiniCollectionModel Collection = CollectionInstallMetadataController.loadXML(core, collectionFileContent, isBaseCollection, false, IsNewBuild, "");
                                    foreach (var kvp in Collection.menus) {
                                        BuildController.verifyNavigatorEntry(core, kvp.Value, 0);
                                    }
                                    //
                                    // --- end of pass
                                }
                                //
                                // -- setup onInstall if included
                                int collectionOninstalladdonid = 0;
                                if (!string.IsNullOrWhiteSpace(collectionOninstalladdonGuid)) {
                                    var addon = DbBaseModel.create<AddonModel>(core.cpParent, collectionOninstalladdonGuid);
                                    if (addon != null) {
                                        collection.oninstalladdonid = collectionOninstalladdonid;
                                        collection.save(core.cpParent);
                                    }
                                }
                                collection.dataRecordList = dataRecordList;
                                collection.save(core.cpParent);
                                //
                                // -- test for diagnostic addon, warn if missing
                                if (!collectionIncludesDiagnosticAddon) {
                                    //
                                    // -- log warning. This collection does not have an install addon
                                    logger.Debug(LogController.processLogMessage(core, "Collection does not include a Diagnostic addon, [" + collection.name + "]", false));
                                }
                                //
                                // -- execute onInstall addon if found
                                if (string.IsNullOrEmpty(collectionOninstalladdonGuid)) {
                                    //
                                    // -- log warning. This collection does not have an install addon
                                    logger.Debug(LogController.processLogMessage(core, "Collection does not include an install addon, [" + collection.name + "]", false));
                                } else {
                                    //
                                    // -- install the install addon
                                    var addon = DbBaseModel.create<AddonModel>(core.cpParent, collectionOninstalladdonGuid);
                                    if (addon != null) {
                                        var executeContext = new BaseClasses.CPUtilsBaseClass.addonExecuteContext {
                                            addonType = BaseClasses.CPUtilsBaseClass.addonContext.ContextSimple,
                                            errorContextMessage = "calling onInstall Addon [" + addon.name + "] for collection [" + collection.name + "]"
                                        };
                                        core.addon.execute(addon, executeContext);
                                    }
                                }
                                //
                                LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", installCollectionFromAddonCollectionFolder [" + CollectionName + "], upgrade complete, flush cache");
                                //
                                // -- import complete, flush caches
                                core.cache.invalidateAll();
                                result = true;
                            }
                            //
                            // -- invalidate cache
                            core.cache.invalidateAll();
                        }
                    }
                }
            } catch (Exception ex) {
                //
                // Log error and exit with failure. This way any other upgrading will still continue
                LogController.logError(core, ex);
                throw;
            } finally {
                contextLog.Pop();
            }
            return result;
        }
        //
        //======================================================================================================
        /// <summary>
        /// install data node from install file
        /// </summary>
        /// <param name="core"></param>
        /// <param name="dataNode"></param>
        /// <param name="return_ErrorMessage"></param>
        public static void installDataNode(CoreController core, XmlNode dataNode, ref string return_ErrorMessage) {
            foreach (XmlNode ContentNode in dataNode.ChildNodes) {
                if (ContentNode.Name.ToLowerInvariant() == "record") {
                    string ContentName = XmlController.getXMLAttribute(core, ContentNode, "content", "");
                    if (string.IsNullOrEmpty(ContentName)) {
                        LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", installCollectionFromAddonCollectionFolder, install collection file contains a data.record node with a blank content attribute.");
                        return_ErrorMessage += "<P>Collection file contains a data.record node with a blank content attribute.</P>";
                        break;
                    } else {
                        string ContentRecordGuid = XmlController.getXMLAttribute(core, ContentNode, "guid", "");
                        string ContentRecordName = XmlController.getXMLAttribute(core, ContentNode, "name", "");
                        if ((!string.IsNullOrEmpty(ContentRecordGuid)) || (!string.IsNullOrEmpty(ContentRecordName))) {
                            ContentMetadataModel metaData = Models.Domain.ContentMetadataModel.createByUniqueName(core, ContentName);
                            bool isPageContent = metaData.name.ToLower().Equals("page content");
                            bool pageCopyFilenameNotNull = false;
                            bool pageAddonListNotNull = false;
                            int recordId = 0;
                            using (var csData = new CsModel(core)) {
                                if (!string.IsNullOrEmpty(ContentRecordGuid)) {
                                    csData.open(ContentName, "ccguid=" + DbController.encodeSQLText(ContentRecordGuid));
                                } else {
                                    csData.open(ContentName, "name=" + DbController.encodeSQLText(ContentRecordName));
                                }
                                if (csData.ok()) {
                                    //
                                    // Update the record
                                    recordId = csData.getInteger("id");
                                    foreach (XmlNode FieldNode in ContentNode.ChildNodes) {
                                        if (FieldNode.Name.ToLowerInvariant() == "field") {
                                            //
                                            // todo optimize 
                                            bool IsFieldFound = false;
                                            string FieldNameLc = XmlController.getXMLAttribute(core, FieldNode, "name", "").ToLowerInvariant();
                                            CPContentBaseClass.FieldTypeIdEnum fieldTypeId = 0;
                                            int FieldLookupContentId = -1;
                                            ContentFieldMetadataModel fieldMetadata = null;
                                            foreach (var keyValuePair in metaData.fields) {
                                                fieldMetadata = keyValuePair.Value;
                                                if (fieldMetadata.nameLc == FieldNameLc) {
                                                    fieldTypeId = fieldMetadata.fieldTypeId;
                                                    FieldLookupContentId = fieldMetadata.lookupContentId;
                                                    IsFieldFound = true;
                                                    break;
                                                }
                                            }
                                            if (IsFieldFound) {
                                                string fieldValue = FieldNode.InnerText;
                                                pageCopyFilenameNotNull |= isPageContent && FieldNameLc.Equals("copyfilename") && !string.IsNullOrWhiteSpace(fieldValue);
                                                pageAddonListNotNull |= isPageContent && FieldNameLc.Equals("addonlist") && !string.IsNullOrWhiteSpace(fieldValue);
                                                switch (fieldTypeId) {
                                                    case CPContentBaseClass.FieldTypeIdEnum.AutoIdIncrement:
                                                    case CPContentBaseClass.FieldTypeIdEnum.Redirect: {
                                                            //
                                                            // not supported
                                                            break;
                                                        }
                                                    case CPContentBaseClass.FieldTypeIdEnum.Lookup: {
                                                            //
                                                            // lookup
                                                            if ((FieldLookupContentId != 0)) {
                                                                //
                                                                // content lookup
                                                                var lookupContentMetadata = ContentMetadataModel.create(core, FieldLookupContentId);
                                                                if (lookupContentMetadata == null) {
                                                                    //
                                                                    // lookup not configured
                                                                    csData.set(FieldNameLc, 0);
                                                                    break;
                                                                }
                                                                csData.set(FieldNameLc, lookupContentMetadata.getRecordId(core, fieldValue));
                                                                break;
                                                            }
                                                            if (!string.IsNullOrEmpty(fieldMetadata.lookupList)) {
                                                                //
                                                                // Lookup list
                                                                csData.set(FieldNameLc, fieldValue);
                                                                break;
                                                            }
                                                            csData.set(FieldNameLc, 0);
                                                            break;
                                                        }
                                                    case CPContentBaseClass.FieldTypeIdEnum.ManyToMany: {
                                                            //
                                                            // -- many-to-many
                                                            if (string.IsNullOrEmpty(fieldValue)) {
                                                                //
                                                                // -- no value,no record
                                                                continue;
                                                            }
                                                            //
                                                            // -- find secondary content (record in secondary content with matching guid
                                                            using (var cs = core.cpParent.CSNew()) {
                                                                string secondaryContentName = core.cpParent.Content.GetName(fieldMetadata.manyToManyContentId);
                                                                if (cs.Open(secondaryContentName, "ccGuid=" + core.cpParent.Db.EncodeSQLText(fieldValue))) {
                                                                    //
                                                                    // -- find rule record
                                                                    using (var csRule = core.cpParent.CSNew()) {
                                                                        string ruleContent = core.cpParent.Content.GetName(fieldMetadata.manyToManyRuleContentId);
                                                                        int secondaryRecordId = cs.GetInteger("id");
                                                                        if (!csRule.Open(ruleContent, "(" + fieldMetadata.manyToManyRulePrimaryField + "=" + recordId + ")and(" + fieldMetadata.manyToManyRuleSecondaryField + "=" + secondaryRecordId + ")")) {
                                                                            //
                                                                            // -- rule record is missing, check the box
                                                                            csRule.Close();
                                                                            csRule.Insert(ruleContent);
                                                                            csRule.SetField(fieldMetadata.manyToManyRulePrimaryField, recordId);
                                                                            csRule.SetField(fieldMetadata.manyToManyRuleSecondaryField, secondaryRecordId);
                                                                            csRule.Save();
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                            break;
                                                        }
                                                    default: {
                                                            csData.set(FieldNameLc, fieldValue);
                                                            break;
                                                        }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            if (isPageContent && pageCopyFilenameNotNull && !pageAddonListNotNull) {
                                PageContentModel page = DbBaseModel.create<PageContentModel>(core.cpParent, recordId);
                                BuildDataMigrationController.convertPageContentToAddonList(core, page);
                            }
                        }
                    }
                }
            }
        }
        //
        //======================================================================================================
        /// <summary>
        /// Installs all collections found in a source folder. Builds the Collection Folder. Calls installCollectionFromCollectionFolder.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="isDependency"></param>
        /// <param name="contextLog"></param>
        /// <param name="installTempPath"></param>
        /// <param name="return_ErrorMessage"></param>
        /// <param name="collectionsInstalledList">a list of the collections installed to the database during this installation (dependencies etc.). The collections installed are added to this list</param>
        /// <param name="IsNewBuild"></param>
        /// <param name="reinstallDependencies"></param>
        /// <param name="nonCriticalErrorList"></param>
        /// <param name="logPrefix"></param>
        /// <param name="includeBaseMetaDataInstall"></param>
        /// <param name="collectionsDownloaded">List of collections that have been downloaded during this istall pass but have not been installed yet. Do no need to download them again.</param>
        /// <returns></returns>
        public static bool installCollectionsFromTempFolder(CoreController core, bool isDependency, Stack<string> contextLog, string installTempPath, ref string return_ErrorMessage, ref List<string> collectionsInstalledList, bool IsNewBuild, bool reinstallDependencies, ref List<string> nonCriticalErrorList, string logPrefix, bool includeBaseMetaDataInstall, ref List<string> collectionsDownloaded) {
            bool returnSuccess = false;
            try {
                contextLog.Push(MethodInfo.GetCurrentMethod().Name + ", [" + installTempPath + "]");
                traceContextLog(core, contextLog);
                DateTime CollectionLastChangeDate = core.dateTimeNowMockable;
                //
                // -- collectionsToInstall = collections stored in the collection folder that need to be stored in the Db
                var collectionsToInstall = new List<string>();
                var collectionsBuildingFolder = new List<string>();
                returnSuccess = CollectionFolderController.buildCollectionFoldersFromCollectionZips(core, contextLog, installTempPath, CollectionLastChangeDate, ref collectionsToInstall, ref return_ErrorMessage, ref collectionsInstalledList, ref collectionsBuildingFolder);
                if (!returnSuccess) {
                    //
                    // BuildLocal failed, log it and do not upgrade
                    //
                    LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", BuildLocalCollectionFolder returned false with Error Message [" + return_ErrorMessage + "], exiting without calling UpgradeAllAppsFromLocalCollection");
                } else {
                    foreach (string collectionGuid in collectionsToInstall) {
                        if (!installCollectionFromCollectionFolder(core, isDependency, contextLog, collectionGuid, ref return_ErrorMessage, IsNewBuild, reinstallDependencies, ref nonCriticalErrorList, logPrefix, ref collectionsInstalledList, includeBaseMetaDataInstall, ref collectionsDownloaded)) {
                            LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", UpgradeAllAppsFromLocalCollection returned false with Error Message [" + return_ErrorMessage + "].");
                            break;
                        }
                        //
                        // -- invalidate cache
                        core.cache.invalidateAll();
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                returnSuccess = false;
                if (string.IsNullOrEmpty(return_ErrorMessage)) {
                    return_ErrorMessage = "There was an unexpected error installing the collection, details [" + ex.Message + "]";
                }
            } finally {
                contextLog.Pop();
            }
            return returnSuccess;
        }
        //
        //======================================================================================================
        /// <summary>
        /// Installs a collectionZip from a file in tempFiles.
        /// Builds the Collection Folder. 
        /// Calls installCollectionFromCollectionFolder.
        /// </summary>
        public static bool installCollectionFromTempFile(CoreController core, bool isDependency, Stack<string> contextLog, string tempPathFilename, ref string return_ErrorMessage, ref string return_CollectionGUID, bool IsNewBuild, bool reinstallDependencies, ref List<string> nonCriticalErrorList, string logPrefix, ref List<string> collectionsInstalledList) {
            bool returnSuccess = true;
            try {
                contextLog.Push(MethodInfo.GetCurrentMethod().Name + ", [" + tempPathFilename + "]");
                traceContextLog(core, contextLog);
                DateTime CollectionLastChangeDate;
                //
                // -- build the collection folder and download/install all collection dependencies, return list collectionsDownloaded
                CollectionLastChangeDate = core.dateTimeNowMockable;
                var collectionsDownloaded = new List<string>();
                var collectionsBuildingFolder = new List<string>();
                if (!CollectionFolderController.buildCollectionFolderFromCollectionZip(core, contextLog, tempPathFilename, CollectionLastChangeDate, ref return_ErrorMessage, ref collectionsDownloaded, ref collectionsInstalledList, ref collectionsBuildingFolder)) {
                    //
                    // BuildLocal failed, log it and do not upgrade
                    //
                    returnSuccess = false;
                    LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", BuildLocalCollectionFolder returned false with Error Message [" + return_ErrorMessage + "], exiting without calling UpgradeAllAppsFromLocalCollection");
                } else if (collectionsDownloaded.Count > 0) {
                    return_CollectionGUID = collectionsDownloaded.First();
                    foreach (var collection in collectionsDownloaded) {
                        if (!installCollectionFromCollectionFolder(core, isDependency, contextLog, collection, ref return_ErrorMessage, IsNewBuild, reinstallDependencies, ref nonCriticalErrorList, logPrefix, ref collectionsInstalledList, true, ref collectionsDownloaded)) {
                            //
                            // Upgrade all apps failed
                            //
                            returnSuccess = false;
                            LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", Error, Collection(s) install failed, UpgradeAllAppsFromLocalCollection returned false with Error Message [" + return_ErrorMessage + "].");
                            break;
                        }
                    }
                }
                if (returnSuccess) {
                    LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", Collection(s) installed successfully.");
                }
                //
                // -- invalidate cache
                core.cache.invalidateAll();
            } catch (Exception ex) {
                LogController.logError(core, ex);
                returnSuccess = false;
                if (string.IsNullOrEmpty(return_ErrorMessage)) {
                    return_ErrorMessage = "There was an unexpected error installing the collection, details [" + ex.Message + "]";
                }
            } finally {
                contextLog.Pop();
            }
            return returnSuccess;
        }
        //
        //======================================================================================================
        /// <summary>
        /// process the include add-on node of the add-on nodes. 
        /// this is the second pass, so all add-ons should be added
        /// no errors for missing addones, except the include add-on case
        /// </summary>
        private static void setAddonDependencies(CoreController core, string parentCollectionName, XmlNode AddonNode, string AddonGuidFieldName, string ignore_BuildVersion, int CollectionID, ref bool ReturnUpgradeOK, ref string ReturnErrorMessage) {
            try {
                string Basename = GenericController.toLCase(AddonNode.Name);
                if ((Basename == "page") || (Basename == "process") || (Basename == "addon") || (Basename == "add-on")) {
                    string parentAddonName = "?";
                    string addonName = XmlController.getXMLAttribute(core, AddonNode, "name", "No Name");
                    if (string.IsNullOrEmpty(addonName)) { addonName = "No Name"; }
                    string addonGuid = XmlController.getXMLAttribute(core, AddonNode, "guid", addonName);
                    if (string.IsNullOrEmpty(addonGuid)) { addonGuid = addonName; }
                    string Criteria = "(" + AddonGuidFieldName + "=" + DbController.encodeSQLText(addonGuid) + ")";
                    using (var csData = new CsModel(core)) {
                        //
                        //????? dont understand why this code aborted if the addon was found. It should add the dependencies found, remove the ones not found
                        // 
                        if (!csData.open(AddonModel.tableMetadata.contentName, Criteria, "", false)) {
                            //
                            // -- not found by GUID - search name to update legacy Add-ons
                            csData.close();
                            Criteria = "(name=" + DbController.encodeSQLText(addonName) + ")and(" + AddonGuidFieldName + " is null)";
                            if (!csData.open(AddonModel.tableMetadata.contentName, Criteria, "", false)) {
                                //
                                // Could not find add-on, this is an error, but do not abort
                                LogController.logError(core, new ApplicationException( MethodInfo.GetCurrentMethod().Name + ", installing collection [" + parentCollectionName + "], could not find the addon in which the dependency is added, by name [" + addonName + "], Guid [" + addonGuid + "],  skipping dependent add-on"));
                                return;
                            }
                        }
                        foreach (XmlNode PageInterface in AddonNode.ChildNodes) {
                            switch (GenericController.toLCase(PageInterface.Name)) {
                                case "includeaddon":
                                case "includeadd-on":
                                case "include addon":
                                case "include add-on": {
                                        //
                                        // -- include add-ons - NOTE - import collections must be run before interfaces, when importing a collectin that will be used for an include
                                        string IncludeAddonName = XmlController.getXMLAttribute(core, PageInterface, "name", "");
                                        string IncludeAddonGuid = XmlController.getXMLAttribute(core, PageInterface, "guid", IncludeAddonName);
                                        int IncludeAddonId = 0;
                                        Criteria = "";
                                        if (!string.IsNullOrEmpty(IncludeAddonGuid)) {
                                            Criteria = AddonGuidFieldName + "=" + DbController.encodeSQLText(IncludeAddonGuid);
                                            if (string.IsNullOrEmpty(IncludeAddonName)) {
                                                IncludeAddonName = "Add-on " + IncludeAddonGuid;
                                            }
                                        } else if (!string.IsNullOrEmpty(IncludeAddonName)) {
                                            Criteria = "(name=" + DbController.encodeSQLText(IncludeAddonName) + ")";
                                        }
                                        if (!string.IsNullOrEmpty(Criteria)) {
                                            using (var CS2 = new CsModel(core)) {
                                                CS2.open(AddonModel.tableMetadata.contentName, Criteria);
                                                if (CS2.ok()) {
                                                    IncludeAddonId = CS2.getInteger("ID");
                                                }
                                            }
                                            bool AddRule = false;
                                            if (IncludeAddonId == 0) {
                                                string UserError = "While installng collection/addon [" + parentCollectionName + "/" + parentAddonName + "], the include add-on [" + IncludeAddonName + "] could not be added because it was not found. If it is in the collection being installed, it must appear before any add-ons that include it.";
                                                LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", UpgradeAddFromLocalCollection_InstallAddonNode, UserError [" + UserError + "]");
                                                ReturnUpgradeOK = false;
                                                ReturnErrorMessage = ReturnErrorMessage + "<P>The collection was not installed because the add-on [" + addonName + "] requires an included add-on [" + IncludeAddonName + "] which could not be found. If it is in the collection being installed, it must appear before any add-ons that include it.</P>";
                                            } else {
                                                using (var cs3 = new CsModel(core)) {
                                                    AddRule = !cs3.openSql("select ID from ccAddonIncludeRules where Addonid=" + csData.getInteger("id") + " and IncludedAddonID=" + IncludeAddonId);
                                                }
                                            }
                                            if (AddRule) {
                                                using (var cs3 = new CsModel(core)) {
                                                    cs3.insert("Add-on Include Rules");
                                                    if (cs3.ok()) {
                                                        cs3.set("Addonid", csData.getInteger("id"));
                                                        cs3.set("IncludedAddonID", IncludeAddonId);
                                                    }
                                                }
                                            }
                                        }
                                        break;
                                    }
                                default: {
                                        // do nothing
                                        break;
                                    }
                            }
                        }
                    }
                }
                return;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //======================================================================================================
        /// <summary>
        /// process the include add-on node of the add-on nodes. 
        /// this is the second pass, so all add-ons should be added
        /// no errors for missing addones, except the include add-on case
        /// </summary>
        private static void setAddonDependencies_Refactored_Error(CoreController core, string parentCollectionName, XmlNode AddonNode, string AddonGuidFieldName, string ignore_BuildVersion, int CollectionID, ref bool ReturnUpgradeOK, ref string ReturnErrorMessage) {
            try {
                string Basename = GenericController.toLCase(AddonNode.Name);
                if ((Basename == "page") || (Basename == "process") || (Basename == "addon") || (Basename == "add-on")) {
                    string parentAddonName = "?";
                    string addonName = XmlController.getXMLAttribute(core, AddonNode, "name", "No Name");
                    if (string.IsNullOrEmpty(addonName)) { addonName = "No Name"; }
                    string addonGuid = XmlController.getXMLAttribute(core, AddonNode, "guid", addonName);
                    if (string.IsNullOrEmpty(addonGuid)) { addonGuid = addonName; }
                    string Criteria = "(" + AddonGuidFieldName + "=" + DbController.encodeSQLText(addonGuid) + ")";
                    foreach (XmlNode PageInterface in AddonNode.ChildNodes) {
                        switch (GenericController.toLCase(PageInterface.Name)) {
                            case "includeaddon":
                            case "includeadd-on":
                            case "include addon":
                            case "include add-on": {
                                    //
                                    // -- open the addon 
                                    using var addonCs = new CsModel(core);
                                    if (!addonCs.open(AddonModel.tableMetadata.contentName, Criteria, "", false)) {
                                        //
                                        // -- not found by GUID - search name to update legacy Add-ons
                                        addonCs.close();
                                        Criteria = "(name=" + DbController.encodeSQLText(addonName) + ")and(" + AddonGuidFieldName + " is null)";
                                        if (!addonCs.open(AddonModel.tableMetadata.contentName, Criteria, "", false)) {
                                            //
                                            // Could not find add-on, this is an error, but do not abort
                                            LogController.logError(core, new ApplicationException(MethodInfo.GetCurrentMethod().Name + ", installing collection [" + parentCollectionName + "], could not find the addon in which the dependency is added, by name [" + addonName + "], Guid [" + addonGuid + "],  skipping dependent add-on"));
                                            return;
                                        }
                                    }
                                    //
                                    // -- include add-ons - NOTE - import collections must be run before interfaces, when importing a collectin that will be used for an include
                                    string IncludeAddonName = XmlController.getXMLAttribute(core, PageInterface, "name", "");
                                    string IncludeAddonGuid = XmlController.getXMLAttribute(core, PageInterface, "guid", IncludeAddonName);
                                    int IncludeAddonId = 0;
                                    Criteria = "";
                                    if (!string.IsNullOrEmpty(IncludeAddonGuid)) {
                                        Criteria = AddonGuidFieldName + "=" + DbController.encodeSQLText(IncludeAddonGuid);
                                        if (string.IsNullOrEmpty(IncludeAddonName)) {
                                            IncludeAddonName = "Add-on " + IncludeAddonGuid;
                                        }
                                    } else if (!string.IsNullOrEmpty(IncludeAddonName)) {
                                        Criteria = "(name=" + DbController.encodeSQLText(IncludeAddonName) + ")";
                                    }
                                    if (!string.IsNullOrEmpty(Criteria)) {
                                        using (var CS2 = new CsModel(core)) {
                                            CS2.open(AddonModel.tableMetadata.contentName, Criteria);
                                            if (CS2.ok()) {
                                                IncludeAddonId = CS2.getInteger("ID");
                                            }
                                        }
                                        bool AddRule = false;
                                        if (IncludeAddonId == 0) {
                                            string UserError = "While installng collection/addon [" + parentCollectionName + "/" + parentAddonName + "], the include add-on [" + IncludeAddonName + "] could not be added because it was not found. If it is in the collection being installed, it must appear before any add-ons that include it.";
                                            LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", UpgradeAddFromLocalCollection_InstallAddonNode, UserError [" + UserError + "]");
                                            ReturnUpgradeOK = false;
                                            ReturnErrorMessage = ReturnErrorMessage + "<P>The collection was not installed because the add-on [" + addonName + "] requires an included add-on [" + IncludeAddonName + "] which could not be found. If it is in the collection being installed, it must appear before any add-ons that include it.</P>";
                                        } else {
                                            using (var cs3 = new CsModel(core)) {
                                                AddRule = !cs3.openSql("select ID from ccAddonIncludeRules where Addonid=" + addonCs.getInteger("id") + " and IncludedAddonID=" + IncludeAddonId);
                                            }
                                        }
                                        if (AddRule) {
                                            using (var ruleCs = new CsModel(core)) {
                                                ruleCs.insert("Add-on Include Rules");
                                                if (ruleCs.ok()) {
                                                    ruleCs.set("Addonid", addonCs.getInteger("id"));
                                                    ruleCs.set("IncludedAddonID", IncludeAddonId);
                                                }
                                            }
                                        }
                                    }
                                    break;
                                }
                            default: {
                                    // do nothing
                                    break;
                                }

                        }
                    }
                }
                return;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
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
        private static void copyTempSubfoldersToRoot( CoreController core, string srcPath, string rootPath) {
            //
            // -- copy files
            foreach( var file in core.tempFiles.getFileList(srcPath)) {
                core.tempFiles.copyFile(srcPath + file.Name, rootPath + file.Name);
            }
            //
            // -- copy subfolders
            foreach (FolderDetail subFolder in core.tempFiles.getFolderList(srcPath)) {
                copyTempSubfoldersToRoot(core, srcPath + subFolder.Name + "\\", rootPath);
            }

        }
    }
}