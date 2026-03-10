
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Models.Domain;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;

namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// static class controller
    /// </summary>
    public class ContentController : IDisposable {
        //
        //====================================================================================================
        /// <summary>
        /// return the 1-based value from comma delimited string.
        /// Use for LookupList decode
        /// </summary>
        /// <param name="key"></param>
        /// <param name="lookupList"></param>
        /// <returns></returns>
        public static string getLookupListText(int key, string lookupList) {
            int FieldValueInteger = key - 1;
            string result = "";
            if (FieldValueInteger < 0) { return result; }
            string[] lookups = lookupList.Split(',');
            if (lookups.GetUpperBound(0) < FieldValueInteger) { return result; }
            return lookups[FieldValueInteger];
        }
        //
        //====================================================================================================
        //
        public static void processAfterSave_AddonCollection(CoreController core, bool isDelete, string contentName, int recordID, string recordName, int recordParentID) {
            //
            // -- if this is an add or delete, manage the collection folders
            if (isDelete) {
                //
                // -- collection deleted, use resource manifest to clean up installed files
                try {
                    //
                    // -- the db record is already deleted, so find the collection folder config by name from Collections.xml
                    CollectionFolderModel folderConfig = findCollectionFolderConfigByName(core, recordName);
                    if (folderConfig != null && !string.IsNullOrEmpty(folderConfig.path)) {
                        string collectionVersionFolder = AddonController.getPrivateFilesAddonPath() + folderConfig.path + "\\";
                        var manifest = ResourceManifestModel.load(core, collectionVersionFolder);
                        if (manifest != null && manifest.resources != null) {
                            //
                            // -- delete all tracked resource files
                            foreach (var entry in manifest.resources) {
                                switch (entry.type.ToLowerInvariant()) {
                                    case "www":
                                        core.wwwFiles.deleteFile(entry.destinationPath);
                                        break;
                                    case "private":
                                        core.privateFiles.deleteFile(entry.destinationPath);
                                        break;
                                    case "cdn":
                                        core.cdnFiles.deleteFile(entry.destinationPath);
                                        break;
                                    case "helpfiles":
                                        core.privateFiles.deleteFile(entry.destinationPath);
                                        break;
                                }
                            }
                            //
                            // -- delete tracked folders if empty (deepest first so subfolders are removed before parents)
                            if (manifest.folders != null) {
                                for (int i = manifest.folders.Count - 1; i >= 0; i--) {
                                    var folder = manifest.folders[i];
                                    FileController fileSystem = folder.type.ToLowerInvariant() switch {
                                        "www" => core.wwwFiles,
                                        "private" => core.privateFiles,
                                        "cdn" => core.cdnFiles,
                                        "helpfiles" => core.privateFiles,
                                        _ => null
                                    };
                                    if (fileSystem != null && fileSystem.getFileList(folder.folderPath).Count == 0 && fileSystem.getFolderList(folder.folderPath).Count == 0) {
                                        fileSystem.deleteFolder(folder.folderPath);
                                    }
                                }
                            }
                            logger.Info($"{core.logCommonMessage}, collection [{recordName}] deleted, removed {manifest.resources.Count} resource files and checked {manifest.folders?.Count ?? 0} folders via manifest");
                        }
                    }
                } catch (Exception ex) {
                    logger.Error(ex, $"{core.logCommonMessage}, error cleaning up resource files for deleted collection [{recordName}]");
                }
            } else {
                //
                // -- add or modify collection, verify collection /addon folder
                var addonCollection = AddonCollectionModel.create<AddonCollectionModel>(core.cpParent, recordID);
                if (addonCollection != null) {
                    string CollectionVersionFolderName = CollectionFolderController.verifyCollectionVersionFolderName(core, addonCollection.ccguid, addonCollection.name);
                    if (string.IsNullOrEmpty(CollectionVersionFolderName)) {
                        //
                        // -- new collection
                        string CollectionVersionFolder = AddonController.getPrivateFilesAddonPath() + CollectionVersionFolderName;
                        core.privateFiles.createPath(CollectionVersionFolder);
                        CollectionFolderController.updateCollectionFolderConfig(core, addonCollection.name, addonCollection.ccguid, core.dateTimeNowMockable, CollectionVersionFolderName);
                    }
                }
            }
        }
        //
        //====================================================================================================
        //
        public static void processAfterSave_LibraryFiles(CoreController core, bool isDelete, string contentName, int recordID, string recordName, int recordParentID) {
            //
            // if a AltSizeList is blank, make large,medium,small and thumbnails
            //
        }
        //
        //====================================================================================================
        /// <summary>
        /// Process manual changes needed for special cases
        /// </summary>
        /// <param name="isDelete"></param>
        /// <param name="contentName"></param>
        /// <param name="recordId"></param>
        /// <param name="recordName"></param>
        /// <param name="recordParentID"></param>
        /// <param name="useContentWatchLink"></param>
        public static void processAfterSave(CoreController core, bool isDelete, string contentName, int recordId, string recordName, int recordParentID) {
            try {
                string tableName = MetadataController.getContentTablename(core, contentName);
                //
                // -- invalidate the specific cache for this record
                core.cache.invalidateRecordKey(recordId, tableName);
                //
                string tableNameLower = tableName.ToLower(CultureInfo.InvariantCulture);
                if (tableNameLower == AddonCollectionModel.tableMetadata.tableNameLower) {
                    //
                    // -- addon collection
                    processAfterSave_AddonCollection(core, isDelete, contentName, recordId, recordName, recordParentID);
                } else if (tableNameLower == LinkForwardModel.tableMetadata.tableNameLower) {
                    //
                    // -- link forward
                    core.routeMapRebuild();
                } else if (tableNameLower == LinkAliasModel.tableMetadata.tableNameLower) {
                    //
                    // -- link alias
                    core.routeMapRebuild();
                } else if (tableNameLower == AddonModel.tableMetadata.tableNameLower) {
                    //
                    // -- addon
                    core.routeMapRebuild();
                } else if (tableNameLower == PersonModel.tableMetadata.tableNameLower) {
                    //
                    // -- PersonModel
                    if (!isDelete) {
                        LogController.addActivityCompletedEdit(core, "Edit", core.session.user.name + " saved changes to user #" + recordId + " (" + recordName + ")", recordId);
                        bool allowPlainTextPassword = core.siteProperties.getBoolean(Constants.sitePropertyName_AllowPlainTextPassword, true);
                        if (!allowPlainTextPassword) {
                            //
                            // -- handle auto password hash
                            PersonModel user = DbBaseModel.create<PersonModel>(core.cpParent, recordId);
                            if (user != null && !string.IsNullOrEmpty(user.password)) {
                                //
                                // -- password field set, hash and save to passwordHash, used passwords
                                string userErrorMessage = "";
                                if (!AuthController.tryIsValidPassword(core, user, user.password, ref userErrorMessage)) {
                                    ErrorController.addUserError(core, $"Could not set password. {userErrorMessage}");
                                } else if (!AuthController.trySetPassword(core.cpParent, user.password, user, ref userErrorMessage)) {
                                    ErrorController.addUserError(core, $"Could not set password. {userErrorMessage}");
                                }
                            }
                        }
                    }
                } else if (tableNameLower == SitePropertyModel.tableMetadata.tableNameLower) {
                    //
                    // -- Site Properties
                    //
                    switch (GenericController.toLCase(recordName)) {
                        case "allowlinkalias":
                            PageContentModel.invalidateCacheOfTable<PageContentModel>(core.cpParent);
                            break;
                        case "sectionlandinglink":
                            PageContentModel.invalidateCacheOfTable<PageContentModel>(core.cpParent);
                            break;
                        case Constants.sitePropertyName_ServerPageDefault:
                            PageContentModel.invalidateCacheOfTable<PageContentModel>(core.cpParent);
                            break;
                    }
                } else if (tableNameLower == PageContentModel.tableMetadata.tableNameLower) {
                    //
                    // -- Page Content
                    //
                    if (isDelete) {
                        //
                        // Clear the Landing page and page not found site properties
                        if (recordId == GenericController.getInteger(core.siteProperties.getText("PageNotFoundPageID", "0"))) {
                            core.siteProperties.setProperty("PageNotFoundPageID", "0");
                        }
                        if (recordId == core.siteProperties.landingPageID) {
                            core.siteProperties.setProperty("landingPageId", "0");
                        }
                        //
                        // Delete Link Alias entries with this PageID
                        core.db.executeNonQuery("delete from cclinkAliases where PageID=" + recordId);
                        DbBaseModel.invalidateCacheOfTable<LinkAliasModel>(core.cpParent);
                    } else {
                        //
                        // -- not delete
                        if (recordParentID > 0) {
                            //
                            // -- set ChildPagesFound true for parent page
                            core.db.executeNonQuery("update ccpagecontent set ChildPagesfound=1 where ID=" + recordParentID);
                        }
                        //
                        // -- if new page and no linkAlias set, use page name.
                        PageContentModel page = DbBaseModel.create<PageContentModel>(core.cpParent, recordId);
                        if (page is not null) {
                            List<LinkAliasModel> linkAliasList = DbBaseModel.createList<LinkAliasModel>(core.cpParent, $"pageid={page.id}");
                            if (linkAliasList.Count == 0) {
                                LinkAliasController.addLinkAlias(core, page.name, page.id, "", true, false);
                            }
                        }
                    }
                    DbBaseModel.invalidateCacheOfRecord<PageContentModel>(core.cpParent, recordId);
                    core.routeMapRebuild();
                } else if (tableNameLower == LibraryFilesModel.tableMetadata.tableNameLower) {
                    //
                    // -- 
                    processAfterSave_LibraryFiles(core, isDelete, contentName, recordId, recordName, recordParentID);
                }
                //
                // Process Addons marked to trigger a process call on content change
                //
                Dictionary<string, string> instanceArguments;
                bool onChangeAddonsAsync = core.siteProperties.getBoolean("execute oncontentchange addons async", false);
                using (var csData = new CsModel(core)) {
                    int contentId = ContentMetadataModel.getContentId(core, contentName);
                    csData.open("Add-on Content Trigger Rules", "ContentID=" + contentId, "", false, 0, "addonid");
                    string Option_String = null;
                    if (isDelete) {
                        instanceArguments = new Dictionary<string, string> {
                            {"action","contentdelete"},
                            {"contentid",contentId.ToString()},
                            {"recordid",recordId.ToString()}
                        };
                        Option_String = ""
                            + Environment.NewLine + "action=contentdelete"
                            + Environment.NewLine + "contentid=" + contentId
                            + Environment.NewLine + "recordid=" + recordId + "";
                    } else {
                        instanceArguments = new Dictionary<string, string> {
                            {"action","contentchange"},
                            {"contentid",contentId.ToString()},
                            {"recordid",recordId.ToString()}
                        };
                        Option_String = ""
                            + Environment.NewLine + "action=contentchange"
                            + Environment.NewLine + "contentid=" + contentId
                            + Environment.NewLine + "recordid=" + recordId + "";
                    }
                    while (csData.ok()) {
                        var addon = core.cacheRuntime.addonCache.create(csData.getInteger("Addonid"));
                        if (addon != null) {
                            if (onChangeAddonsAsync) {
                                //
                                // -- execute addon async
                                core.addon.executeAsProcess(addon, instanceArguments);
                            } else {
                                //
                                // -- execute addon
                                core.addon.execute(addon, new CPUtilsBaseClass.addonExecuteContext {
                                    addonType = CPUtilsBaseClass.addonContext.ContextOnContentChange,
                                    backgroundProcess = false,
                                    errorContextMessage = "",
                                    argumentKeyValuePairs = instanceArguments
                                });
                            }
                        }
                        csData.goNext();
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Find a collection folder config by name from Collections.xml.
        /// Used during delete when the db record is already gone and only the record name is available.
        /// </summary>
        private static CollectionFolderModel findCollectionFolderConfigByName(CoreController core, string collectionName) {
            try {
                string configXml = CollectionFolderModel.getCollectionFolderConfigXml(core);
                if (string.IsNullOrWhiteSpace(configXml)) { return null; }
                var doc = new XmlDocument();
                doc.LoadXml(configXml);
                foreach (XmlNode configNode in doc.DocumentElement.ChildNodes) {
                    if (!configNode.Name.ToLowerInvariant().Equals("collection")) { continue; }
                    var result = new CollectionFolderModel();
                    foreach (XmlNode childNode in configNode.ChildNodes) {
                        switch (childNode.Name.ToLowerInvariant()) {
                            case "name":
                                result.name = childNode.InnerText;
                                break;
                            case "guid":
                                result.guid = childNode.InnerText;
                                break;
                            case "path":
                                result.path = childNode.InnerText;
                                break;
                            case "lastchangedate":
                                result.lastChangeDate = GenericController.getDate(childNode.InnerText);
                                break;
                        }
                    }
                    if (result.name.Equals(collectionName, StringComparison.OrdinalIgnoreCase)) { return result; }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}, error finding collection folder config by name [{collectionName}]");
            }
            return null;
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        //
        #region  IDisposable Support 
        //
        // this class must implement System.IDisposable
        // never throw an exception in dispose
        // Do not change or add Overridable to these methods.
        // Put cleanup code in Dispose(ByVal disposing As Boolean).
        //====================================================================================================
        //
        protected bool disposed;
        //
        public void Dispose() {
            // do not add code here. Use the Dispose(disposing) overload
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        //
        ~ContentController() {
            // do not add code here. Use the Dispose(disposing) overload
            Dispose(false);
        }
        //
        //====================================================================================================
        /// <summary>
        /// dispose.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing) {
            if (!this.disposed) {
                this.disposed = true;
                if (disposing) {
                }
                //
                // cleanup non-managed objects
                //
            }
        }
        #endregion
    }
}