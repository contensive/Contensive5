
using Contensive.Processor.Controllers;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using static Contensive.BaseClasses.CPFileSystemBaseClass;
using static Contensive.Processor.Constants;
//
namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// Housekeep addon folder
    /// </summary>
    public static class AddonFolderClass {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        /// <summary>
        /// execute hourly tasks
        /// </summary>
        /// <param name="core"></param>
        public static void executeHourlyTasks(HouseKeepEnvironmentModel env) {
            try {
                //
                env.log("Housekeep, executeHourlyTasks, AddonFolder");
                //
            } catch (Exception ex) {
                logger.Error(ex, $"{env.core.logCommonMessage}");
                LogController.logAlarm(env.core, "Housekeep, exception, ex [" + ex + "]");
                throw;

            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// execute Daily Tasks
        /// </summary>
        /// <param name="core"></param>
        public static void executeDailyTasks(HouseKeepEnvironmentModel env) {
            try {
                //
                env.log("HousekeepDaily, addon folder");
                //
                bool loadOK = true;
                XmlDocument Doc = new XmlDocument();
                string hint = "";
                try {
                    string collectionFileFilename = AddonController.getPrivateFilesAddonPath() + "Collections.xml";
                    string collectionFileContent = env.core.privateFiles.readFileText(collectionFileFilename);
                    Doc.LoadXml(collectionFileContent);
                } catch (Exception ex) {
                    logger.Error(ex, $"{env.core.logCommonMessage}");
                    LogController.logAlarm(env.core, "Housekeep, exception, ex [" + ex + "]");
                    throw;
                }
                if (loadOK) {
                    //
                    env.log("Collection.xml loaded ok");
                    //
                    if (GenericController.toLCase(Doc.DocumentElement.Name) != GenericController.toLCase(CollectionListRootNode)) {
                        env.log("RegisterAddonFolder, Hint=[" + hint + "], The Collections.xml file has an invalid root node, [" + Doc.DocumentElement.Name + "] was received and [" + CollectionListRootNode + "] was expected.");
                    } else {
                        //
                        env.log("Collection.xml root name ok");
                        //
                        {
                            int NodeCnt = 0;
                            foreach (XmlNode LocalListNode in Doc.DocumentElement.ChildNodes) {
                                //
                                // Get the collection path
                                //
                                string collectionPath = "";
                                string localGuid = "";
                                string localName = "no name found";
                                DateTime lastChangeDate = default;
                                if (LocalListNode.Name.ToLower(CultureInfo.InvariantCulture).Equals("collection")) {
                                    localGuid = "";
                                    foreach (XmlNode CollectionNode in LocalListNode.ChildNodes) {
                                        switch (CollectionNode.Name.ToLower(CultureInfo.InvariantCulture)) {
                                            case "name":
                                                //
                                                localName = CollectionNode.InnerText.ToLower(CultureInfo.InvariantCulture);
                                                break;
                                            case "guid":
                                                //
                                                localGuid = CollectionNode.InnerText.ToLower(CultureInfo.InvariantCulture);
                                                break;
                                            case "path":
                                                //
                                                collectionPath = CollectionNode.InnerText.ToLower(CultureInfo.InvariantCulture);
                                                break;
                                            case "lastchangedate":
                                                lastChangeDate = GenericController.encodeDate(CollectionNode.InnerText);
                                                break;
                                            default:
                                                logger.Warn($"{env.core.logCommonMessage}, Collection node contains unrecognized child [" + CollectionNode.Name.ToLower(CultureInfo.InvariantCulture) + "]");
                                                break;
                                        }
                                    }
                                }
                                //
                                env.log("Node[" + NodeCnt + "], LocalName=[" + localName + "], LastChangeDate=[" + lastChangeDate + "], CollectionPath=[" + collectionPath + "], LocalGuid=[" + localGuid + "]");
                                //
                                // Go through all subpaths of the collection path, register the version match, unregister all others
                                //
                                if (string.IsNullOrEmpty(collectionPath)) {
                                    //
                                    env.log("no collection path, skipping");
                                    //
                                } else {
                                    collectionPath = GenericController.toLCase(collectionPath);
                                    string CollectionRootPath = collectionPath;
                                    int Pos = CollectionRootPath.LastIndexOf("\\", StringComparison.InvariantCulture) + 1;
                                    if (Pos <= 0) {
                                        //
                                        env.log("CollectionPath has no '\\', skipping");
                                        //
                                    } else {
                                        CollectionRootPath = CollectionRootPath.left(Pos - 1);
                                        string Path = AddonController.getPrivateFilesAddonPath() + CollectionRootPath + "\\";
                                        List<FolderDetail> folderList = new List<FolderDetail>();
                                        if (env.core.privateFiles.pathExists(Path)) {
                                            folderList = env.core.privateFiles.getFolderList(Path);
                                        }
                                        if (folderList.Count == 0) {
                                            //
                                            env.log("no subfolders found in physical path [" + Path + "], skipping");
                                            //
                                        } else {
                                            int folderPtr = -1;
                                            foreach (FolderDetail dir in folderList) {
                                                folderPtr += 1;
                                                //
                                                // -- check for empty foler name
                                                if (string.IsNullOrEmpty(dir.Name)) {
                                                    //
                                                    env.log("....empty folder skipped [" + dir.Name + "]");
                                                    continue;
                                                }
                                                //
                                                // -- preserve folder in use
                                                if (CollectionRootPath + "\\" + dir.Name == collectionPath) {
                                                    env.log("....active folder preserved [" + dir.Name + "]");
                                                    continue;
                                                }
                                                //
                                                // preserve last three folders
                                                if (folderPtr >= (folderList.Count - 5)) {
                                                    env.log("....last 5 folders reserved [" + dir.Name + "]");
                                                    continue;
                                                }
                                                //
                                                env.log("....Deleting unused folder [" + Path + dir.Name + "]");
                                                env.core.privateFiles.deleteFolder(Path + dir.Name);
                                            }
                                        }
                                    }
                                }
                                
                                NodeCnt += 1;
                            }
                        }
                    }
                }
                //
                env.log("Exiting RegisterAddonFolder");
            } catch (Exception ex) {
                logger.Error(ex, $"{env.core.logCommonMessage}");
                LogController.logAlarm(env.core, "Housekeep, exception, ex [" + ex + "]");
                throw;

            }
        }
        //
    }
}