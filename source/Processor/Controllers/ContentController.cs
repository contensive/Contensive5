
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Models.Domain;
using System;
using System.Collections.Generic;
using System.Globalization;

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
                // todo - if a collection is deleted, consider deleting the collection folder (or saving as archive)
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
                        if (recordId == GenericController.encodeInteger(core.siteProperties.getText("PageNotFoundPageID", "0"))) {
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