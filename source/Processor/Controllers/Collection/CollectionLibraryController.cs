
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Xml;
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
    public class CollectionLibraryController {
        //
        //====================================================================================================
        /// <summary>
        /// download a collectionZip from the collection library to a privateFilesPath
        /// </summary>
        /// <param name="core"></param>
        /// <param name="tempFilesDownloadPath"></param>
        /// <param name="collectionGuid"></param>
        /// <param name="return_CollectionLastModifiedDate"></param>
        /// <param name="return_ErrorMessage"></param>
        /// <returns></returns>
        internal static bool downloadCollectionFromLibrary(CoreController core, string tempFilesDownloadPath, string collectionGuid, ref DateTime return_CollectionLastModifiedDate, ref string return_ErrorMessage) {
            bool result = false;
            try {
                //
                LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", downloading collection [" + collectionGuid + "]");
                //
                // Request the Download file for this collection
                XmlDocument Doc = new XmlDocument();
                string URL = "http://support.contensive.com/GetCollection?iv=" + CoreController.codeVersion() + "&guid=" + collectionGuid;
                string errorPrefix = "DownloadCollectionFiles, Error reading the collection library status file from the server for Collection [" + collectionGuid + "], download URL [" + URL + "]. ";
                int downloadRetry = 0;
                int downloadDelay = 2000;
                const int downloadRetryMax = 3;
                do {
                    try {
                        result = true;
                        return_ErrorMessage = "";
                        //
                        // -- pause for a second between fetches to pace the server (<10 hits in 10 seconds)
                        Thread.Sleep(downloadDelay);
                        //
                        // -- download file
                        System.Net.WebRequest rq = System.Net.WebRequest.Create(URL);
                        rq.Timeout = 60000;
                        System.Net.WebResponse response = rq.GetResponse();
                        Stream responseStream = response.GetResponseStream();
                        XmlTextReader reader = new XmlTextReader(responseStream);
                        Doc.Load(reader);
                        break;
                    } catch (Exception ex) {
                        //
                        // this error could be data related, and may not be critical. log issue and continue
                        downloadDelay += 2000;
                        return_ErrorMessage = "There was an error while requesting the download details for collection [" + collectionGuid + "]";
                        result = false;
                        LogController.logInfo(core, errorPrefix + "There was a parse error reading the response [" + ex + "]");
                    }
                    downloadRetry += 1;
                } while (downloadRetry < downloadRetryMax);
                if (string.IsNullOrEmpty(return_ErrorMessage)) {
                    //
                    // continue if no errors
                    if (Doc.DocumentElement.Name.ToLowerInvariant() != GenericController.toLCase(DownloadFileRootNode)) {
                        return_ErrorMessage = "The collection file from the server was not valid for collection [" + collectionGuid + "]";
                        result = false;
                        LogController.logInfo(core, errorPrefix + "The response has a basename [" + Doc.DocumentElement.Name + "] but [" + DownloadFileRootNode + "] was expected.");
                    } else {
                        //
                        // Parse the Download File and download each file into the working folder
                        if (Doc.DocumentElement.ChildNodes.Count == 0) {
                            return_ErrorMessage = "The collection library status file from the server has a valid basename, but no childnodes.";
                            LogController.logInfo(core, errorPrefix + "The collection library status file from the server has a valid basename, but no childnodes. The collection was probably Not found");
                            result = false;
                        } else {
                            //
                            int CollectionFileCnt = 0;
                            foreach (XmlNode metaDataSection in Doc.DocumentElement.ChildNodes) {
                                string ResourceFilename = null;
                                string ResourceLink = null;
                                string CollectionVersion = null;
                                string CollectionFileLink = null;
                                string Collectionname = null;
                                switch (GenericController.toLCase(metaDataSection.Name)) {
                                    case "collection":
                                        //
                                        // Read in the interfaces and save to Add-ons
                                        ResourceFilename = "";
                                        ResourceLink = "";
                                        Collectionname = "";
                                        collectionGuid = "";
                                        CollectionVersion = "";
                                        CollectionFileLink = "";
                                        foreach (XmlNode metaDataInterfaces in metaDataSection.ChildNodes) {
                                            int Pos = 0;
                                            string UserError = null;
                                            switch (GenericController.toLCase(metaDataInterfaces.Name)) {
                                                case "name":
                                                    Collectionname = metaDataInterfaces.InnerText;
                                                    break;
                                                case "help":
                                                    if (!string.IsNullOrWhiteSpace(metaDataInterfaces.InnerText)) {
                                                        core.tempFiles.saveFile(tempFilesDownloadPath + "Collection.hlp", metaDataInterfaces.InnerText);
                                                    }
                                                    break;
                                                case "guid":
                                                    collectionGuid = metaDataInterfaces.InnerText;
                                                    break;
                                                case "lastmodifieddate":
                                                    return_CollectionLastModifiedDate = GenericController.encodeDate(metaDataInterfaces.InnerText);
                                                    break;
                                                case "version":
                                                    CollectionVersion = metaDataInterfaces.InnerText;
                                                    break;
                                                case "collectionfilelink":
                                                    CollectionFileLink = metaDataInterfaces.InnerText;
                                                    CollectionFileCnt = CollectionFileCnt + 1;
                                                    if (!string.IsNullOrEmpty(CollectionFileLink)) {
                                                        Pos = CollectionFileLink.LastIndexOf("/", StringComparison.InvariantCulture) + 1;
                                                        if ((Pos <= 0) && (Pos < CollectionFileLink.Length)) {
                                                            //
                                                            // Skip this file because the collecion file link has no slash (no file)
                                                            LogController.logInfo(core, errorPrefix + "Collection [" + Collectionname + "] was not installed because the Collection File Link does not point to a valid file [" + CollectionFileLink + "]");
                                                        } else {
                                                            string CollectionFilePath = tempFilesDownloadPath + CollectionFileLink.Substring(Pos);
                                                            core.tempFiles.saveHttpRequestToFile(CollectionFileLink, CollectionFilePath);
                                                        }
                                                    }
                                                    break;
                                                case "activexdll":
                                                case "resourcelink":
                                                    //
                                                    // save the filenames and download them only if OKtoinstall
                                                    ResourceFilename = "";
                                                    ResourceLink = "";
                                                    foreach (XmlNode ActiveXNode in metaDataInterfaces.ChildNodes) {
                                                        switch (GenericController.toLCase(ActiveXNode.Name)) {
                                                            case "filename":
                                                                ResourceFilename = ActiveXNode.InnerText;
                                                                break;
                                                            case "link":
                                                                ResourceLink = ActiveXNode.InnerText;
                                                                break;
                                                        }
                                                    }
                                                    if (string.IsNullOrEmpty(ResourceLink)) {
                                                        UserError = "There was an error processing a collection in the download file [" + Collectionname + "]. An ActiveXDll node with filename [" + ResourceFilename + "] contained no 'Link' attribute.";
                                                        LogController.logInfo(core, errorPrefix + UserError);
                                                    } else {
                                                        if (string.IsNullOrEmpty(ResourceFilename)) {
                                                            //
                                                            // Take Filename from Link
                                                            Pos = ResourceLink.LastIndexOf("/", StringComparison.InvariantCulture) + 1;
                                                            if (Pos != 0) {
                                                                ResourceFilename = ResourceLink.Substring(Pos);
                                                            }
                                                        }
                                                        if (string.IsNullOrEmpty(ResourceFilename)) {
                                                            UserError = "There was an error processing a collection in the download file [" + Collectionname + "]. The ActiveX filename attribute was empty, and the filename could not be read from the link [" + ResourceLink + "].";
                                                            LogController.logInfo(core, errorPrefix + UserError);
                                                        } else {
                                                            core.tempFiles.saveHttpRequestToFile(ResourceLink, tempFilesDownloadPath + ResourceFilename);
                                                        }
                                                    }
                                                    break;
                                            }
                                        }
                                        break;
                                }
                            }
                            if (CollectionFileCnt == 0) {
                                LogController.logInfo(core, errorPrefix + "The collection was requested and downloaded, but was not installed because the download file did not have a collection root node.");
                            }
                        }
                    }
                    //
                    // -- invalidate cache
                    core.cache.invalidateAll();
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Install a collectionZip from the Collection Library (registry, distribution, etc.)
        /// downloads the collection to a private folder
        /// Calls installCollectionFromCollectionFolder.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="isDependency"></param>
        /// <param name="contextLog"></param>
        /// <param name="collectionGuid"></param>
        /// <param name="return_ErrorMessage"></param>
        /// <param name="IsNewBuild"></param>
        /// <param name="repair"></param>
        /// <param name="nonCriticalErrorList"></param>
        /// <param name="logPrefix"></param>
        /// <param name="collectionsInstalledList"></param>
        /// <returns></returns>
        public static bool installCollectionFromLibrary(CoreController core, bool isDependency, Stack<string> contextLog, string collectionGuid, ref string return_ErrorMessage, bool IsNewBuild, bool repair, ref List<string> nonCriticalErrorList, string logPrefix, ref List<string> collectionsInstalledList, bool skipCdefInstall) {
            bool UpgradeOK = true;
            try {
                //
                contextLog.Push(MethodInfo.GetCurrentMethod().Name + ", [" + collectionGuid + "]");
                //
                collectionGuid = GenericController.normalizeGuid(collectionGuid);
                if (string.IsNullOrWhiteSpace(collectionGuid)) {
                    LogController.logWarn(core, "installCollectionFromRemoteRepo, collectionGuid is null");
                } else if (!collectionsInstalledList.Contains(collectionGuid.ToLower(CultureInfo.InvariantCulture))) {
                    //
                    // Download all files for this collection and build the collection folder(s)
                    string tempFilesDownloadPath = AddonController.getPrivateFilesAddonPath() +  Contensive.Processor.Controllers.GenericController.getGUIDNaked() + "\\";
                    core.tempFiles.createPath(tempFilesDownloadPath);
                    //
                    // -- download the collection file into the download path from the collectionGuid provided
                    DateTime CollectionLastModifiedDate = default;
                    if (CollectionLibraryController.downloadCollectionFromLibrary(core, tempFilesDownloadPath, collectionGuid, ref CollectionLastModifiedDate, ref return_ErrorMessage)) {
                        //
                        // -- build the collection folders for all collection files in the download path and created a list of collection Guids that need to be installed
                        var collectionsDownloaded = new List<string>();
                        CollectionInstallController.installCollectionsFromTempFolder(core, isDependency, contextLog, tempFilesDownloadPath, ref return_ErrorMessage, ref collectionsInstalledList, IsNewBuild, repair, ref nonCriticalErrorList, logPrefix, true, ref collectionsDownloaded, skipCdefInstall);
                    }
                    //
                    // -- delete the temporary install folder
                    core.tempFiles.deleteFolder(tempFilesDownloadPath);
                    //
                    // -- invalidate cache
                    core.cache.invalidateAll();
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            } finally {
                contextLog.Pop();
            }
            return UpgradeOK;
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
