
using Contensive.Processor.Controllers;
using Contensive.Exceptions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Xml;
using static Contensive.BaseClasses.CPFileSystemBaseClass;
using NLog;
//
namespace Contensive.Processor.Models.Domain {
    /// <summary>
    /// This model represents the folder where collections are stored, and dotnet assemblies are run for this application
    /// </summary>
    //
    public class CollectionFolderModel {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public string name { get; set; }
        public string guid { get; set; }
        public string path { get; set; }
        public DateTime lastChangeDate { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// Return the collection path, lastChangeDate, and collectionName given the guid.
        /// if the collection is not found, returns null
        /// </summary>
        public static CollectionFolderModel getCollectionFolderConfig(CoreController core, string collectionGuid) {
            try {
                XmlDocument doc = new XmlDocument();
                try {
                    doc.LoadXml(getCollectionFolderConfigXml(core));
                } catch (Exception) {
                    logger.Info($"{core.logCommonMessage}, Error loading CollectionFolderConfig file.");
                    return null;
                }
                if (doc.DocumentElement.Name.ToLower(CultureInfo.InvariantCulture) != GenericController.toLCase(Constants.CollectionListRootNode)) {
                    logger.Info($"{core.logCommonMessage}, The Collections.xml file has an invalid root node");
                    return null;
                }
                foreach (XmlNode configNode in doc.DocumentElement.ChildNodes) {
                    if (configNode.Name.ToLowerInvariant().Equals("collection")) {
                        //
                        // -- set defaults
                        var result = new CollectionFolderModel {
                            name = string.Empty,
                            guid = string.Empty,
                            path = string.Empty,
                            lastChangeDate = core.dateTimeNowMockable
                        };
                        foreach (XmlNode collectionNode in configNode.ChildNodes) {
                            switch (GenericController.toLCase(collectionNode.Name)) {
                                case "name":
                                    result.name = collectionNode.InnerText;
                                    break;
                                case "guid":
                                    result.guid = collectionNode.InnerText;
                                    break;
                                case "path":
                                    result.path = collectionNode.InnerText;
                                    break;
                                case "lastchangedate":
                                    result.lastChangeDate = GenericController.getDate(collectionNode.InnerText);
                                    break;
                            }
                        }
                        //
                        // -- if this is the collection requested, exit now
                        if (collectionGuid.ToLowerInvariant().Equals(result.guid.ToLowerInvariant())) { return result; }
                    }
                }
                return null;
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Return the collectionList file stored in the root of the addon folder.
        /// </summary>
        /// <returns></returns>
        public static string getCollectionFolderConfigXml(CoreController core) {
            string returnXml = "";
            try {
                string LastChangeDate = "";
                string FolderName = null;
                string collectionFilePathFilename = null;
                string CollectionGuid = null;
                string Collectionname = null;
                //
                collectionFilePathFilename = AddonController.getPrivateFilesAddonPath() + "Collections.xml";
                returnXml = core.privateFiles.readFileText(collectionFilePathFilename);
                if (string.IsNullOrWhiteSpace(returnXml)) {
                    //
                    logger.Info($"{core.logCommonMessage},Collection Folder XML is blank, rebuild start");
                    //                     
                    List<FolderDetail> FolderList = core.privateFiles.getFolderList(AddonController.getPrivateFilesAddonPath());
                    //
                    logger.Info($"{core.logCommonMessage},Collection Folder XML rebuild, FolderList.count [" + FolderList.Count + "]");
                    //                     
                    if (FolderList.Count > 0) {
                        var collectionsFound = new List<string>();
                        foreach (FolderDetail folder in FolderList) {
                            FolderName = folder.Name;
                            if (FolderName.Length > 34) {
                                if (GenericController.toLCase(FolderName.left(4)) != "temp") {
                                    CollectionGuid = FolderName.Substring(FolderName.Length - 32);
                                    Collectionname = FolderName.left(FolderName.Length - CollectionGuid.Length - 1);
                                    CollectionGuid = CollectionGuid.left(8) + "-" + CollectionGuid.Substring(8, 4) + "-" + CollectionGuid.Substring(12, 4) + "-" + CollectionGuid.Substring(16, 4) + "-" + CollectionGuid.Substring(20);
                                    CollectionGuid = "{" + CollectionGuid + "}";
                                    if (collectionsFound.Contains(CollectionGuid)) {
                                        //
                                        // -- folder with duplicate Guid not allowed. throw;ception and block the folder
                                        logger.Error($"{core.logCommonMessage}", new GenericException("Add-on Collection Folder contains a mulitple collection folders with the same guid, [" + CollectionGuid + "], duplicate folder ignored [" + folder.Name + "]. Remove or Combine the mulitple instances. Then delete the collections.xml file and it will regenerate without the duplicate."));
                                    } else {
                                        collectionsFound.Add(CollectionGuid);
                                        List<FolderDetail> SubFolderList = core.privateFiles.getFolderList(AddonController.getPrivateFilesAddonPath() + FolderName + "\\");
                                        if (SubFolderList.Count > 0) {
                                            FolderDetail lastSubFolder = SubFolderList.Last<FolderDetail>();
                                            FolderName = FolderName + "\\" + lastSubFolder.Name;
                                            LastChangeDate = lastSubFolder.Name.Substring(4, 2) + "/" + lastSubFolder.Name.Substring(6, 2) + "/" + lastSubFolder.Name.left(4);
                                            if (!GenericController.isDate(LastChangeDate)) {
                                                LastChangeDate = "";
                                            }
                                        }
                                        returnXml += Environment.NewLine + "\t<Collection>";
                                        returnXml += Environment.NewLine + "\t\t<name>" + Collectionname + "</name>";
                                        returnXml += Environment.NewLine + "\t\t<guid>" + CollectionGuid + "</guid>";
                                        returnXml += Environment.NewLine + "\t\t<lastchangedate>" + LastChangeDate + "</lastchangedate>";
                                        returnXml += Environment.NewLine + "\t\t<path>" + FolderName + "</path>";
                                        returnXml += Environment.NewLine + "\t</Collection>";
                                    }
                                }
                            }
                        }
                    }
                    returnXml = "<CollectionList>" + returnXml + Environment.NewLine + "</CollectionList>";
                    core.privateFiles.saveFile(collectionFilePathFilename, returnXml);
                    //
                    logger.Info($"{core.logCommonMessage},Collection Folder XML is blank, rebuild finished and saved");
                    //                     
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
            return returnXml;
        }

        //
        //====================================================================================================
        /// <summary>
        /// get a list of collections available on the server
        /// </summary>
        public static bool getCollectionFolderConfigCollectionList(CoreController core, ref List<CollectionLibraryModel> localCollectionStoreList, ref string return_ErrorMessage) {
            bool returnOk = true;
            try {
                //
                // --------------------------------------------------------------------------------------------------------------------------------------------------
                //   Load LocalCollections from the Collections.xml file
                // --------------------------------------------------------------------------------------------------------------------------------------------------
                //
                string localCollectionStoreListXml = getCollectionFolderConfigXml(core);
                if (!string.IsNullOrEmpty(localCollectionStoreListXml)) {
                    XmlDocument LocalCollections = new XmlDocument();
                    try {
                        LocalCollections.LoadXml(localCollectionStoreListXml);
                    } catch (Exception) {
                        string Copy = "Error loading privateFiles\\addons\\Collections.xml";
                        logger.Info($"{core.logCommonMessage},{Copy}");
                        return_ErrorMessage += "<P>" + Copy + "</P>";
                        returnOk = false;
                    }
                    if (returnOk) {
                        if (GenericController.toLCase(LocalCollections.DocumentElement.Name) != GenericController.toLCase(Constants.CollectionListRootNode)) {
                            string Copy = "The addons\\Collections.xml has an invalid root node, [" + LocalCollections.DocumentElement.Name + "] was received and [" + Constants.CollectionListRootNode + "] was expected.";
                            logger.Info($"{core.logCommonMessage},{Copy}");
                            return_ErrorMessage += "<P>" + Copy + "</P>";
                            returnOk = false;
                        } else {
                            //
                            // Get a list of the collection guids on this server
                            //
                            if (GenericController.toLCase(LocalCollections.DocumentElement.Name) == "collectionlist") {
                                foreach (XmlNode LocalListNode in LocalCollections.DocumentElement.ChildNodes) {
                                    switch (GenericController.toLCase(LocalListNode.Name)) {
                                        case "collection":
                                            var collection = new CollectionLibraryModel();
                                            localCollectionStoreList.Add(collection);
                                            foreach (XmlNode CollectionNode in LocalListNode.ChildNodes) {
                                                if (CollectionNode.Name.ToLowerInvariant() == "name") {
                                                    collection.name = CollectionNode.InnerText;
                                                } else if (CollectionNode.Name.ToLowerInvariant() == "guid") {
                                                    collection.guid = CollectionNode.InnerText;
                                                } else if (CollectionNode.Name.ToLowerInvariant() == "path") {
                                                    collection.path = CollectionNode.InnerText;
                                                } else if (CollectionNode.Name.ToLowerInvariant() == "lastchangedate") {
                                                    collection.lastChangeDate = GenericController.getDate(CollectionNode.InnerText);
                                                }
                                            }
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
            return returnOk;
        }
    }
}