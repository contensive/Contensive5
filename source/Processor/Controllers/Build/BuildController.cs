
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Exceptions;
using Contensive.Processor.Models.Domain;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using static Contensive.Processor.Constants;
using static Contensive.Processor.Controllers.GenericController;
using System.Xml.Linq;

namespace Contensive.Processor.Controllers.Build {
    //
    //====================================================================================================
    /// <summary>
    /// code to built and upgrade apps
    /// not IDisposable - not contained classes that need to be disposed
    /// </summary>
    public static class BuildController {
        //
        //====================================================================================================
        /// <summary>
        /// Reinstall the core collectin. 
        /// if repair false, only install the base collection without any dependencies
        /// If repair true, include all dependencies from base install, plus reinstall all collections installed
        /// </summary>
        /// <param name="core"></param>
        /// <param name="isNewBuild"></param>
        /// <param name="repair"></param>
        public static void upgrade(CoreController core, bool isNewBuild, bool repair) {
            try {
                //
                logger.Info($"{core.logCommonMessage},AppBuilderController.upgrade, app [" + core.appConfig.name + "], repair (reinstall base with dependencies, update critical site properties, and reinstall all Library collections) [" + repair + "]");
                string logPrefix = "upgrade[" + core.appConfig.name + "]";
                //
                {
                    string DataBuildVersion = core.siteProperties.dataBuildVersion;
                    if (versionIsOlder(DataBuildVersion, "4.1.636")) {
                        // this is a test
                    }
                    //
                    // -- determine primary domain
                    string primaryDomain = core.appConfig.name;
                    if (core.appConfig.domainList.Count > 0) {
                        primaryDomain = core.appConfig.domainList[0];
                    }
                    //
                    // -- Verify core table fields (DataSources, Content Tables, Content, Content Fields, Setup, Sort Methods), then other basic system ops work, like site properties
                    verifyBasicTables(core, logPrefix);
                    //
                    // 20180217 - move this before base collection because during install it runs addons (like _oninstall)
                    // if anything is needed that is not there yet, I need to build a list of adds to run after the app goes to app status ok
                    // -- Update server config file
                    logger.Info($"{core.logCommonMessage},{logPrefix}, update configuration file");
                    if (!core.appConfig.appStatus.Equals(BaseModels.AppConfigBaseModel.AppStatusEnum.ok)) {
                        core.appConfig.appStatus = BaseModels.AppConfigBaseModel.AppStatusEnum.ok;
                        core.serverConfig.save(core);
                    }
                    //
                    // verify current database meets minimum field requirements (before installing base collection)
                    logger.Info($"{core.logCommonMessage},{logPrefix}, verify existing database fields meet requirements");
                    verifySqlfieldCompatibility(core, logPrefix);
                    //
                    // -- verify base collection
                    logger.Info($"{core.logCommonMessage},{logPrefix}, install base collection");
                    var context = new Stack<string>();
                    context.Push("NewAppController.upgrade call installbasecollection, repair [" + repair + "]");
                    var collectionsInstalledList = new List<string>();
                    List<string> nonCriticalErrorList = new List<string>();
                    bool skipCdefInstall = false;
                    CollectionInstallController.installBaseCollection(core, context, isNewBuild, repair, ref nonCriticalErrorList, logPrefix, collectionsInstalledList, skipCdefInstall);
                    foreach (string nonCriticalError in nonCriticalErrorList) {
                        //
                        // -- error messages, already reported?
                    }
                    //
                    // -- verify staff group
                    GroupModel StaffGroup = verifyGroup(core, logPrefix, defaultStaffGroupGuid, defaultStaffGroupName);
                    //
                    // -- verify site managers group
                    GroupModel SiteManagerGroup = verifyGroup(core, logPrefix, defaultSiteManagerGuid, defaultSiteManagerName);
                    //
                    // -- upgrade work only for the first build, not upgrades of version 5+
                    if (isNewBuild) {
                        //
                        // -- verify iis configuration
                        logger.Info($"{core.logCommonMessage},{logPrefix}, verify iis configuration");
                        core.webServer.verifySite(core.appConfig.name, primaryDomain, core.appConfig.localWwwPath);
                        //
                        // -- verify root developer
                        logger.Info($"{core.logCommonMessage},{logPrefix}, verify developer user");
                        var root = DbBaseModel.create<PersonModel>(core.cpParent, defaultRootUserGuid);
                        if (root == null) {
                            logger.Info($"{core.logCommonMessage},{logPrefix}, root user guid not found, test for root username");
                            var rootList = DbBaseModel.createList<PersonModel>(core.cpParent, "(username='root')");
                            if (rootList.Count > 0) {
                                logger.Info($"{core.logCommonMessage},{logPrefix}, root username found");
                                root = rootList.First();
                            }
                        }
                        if (root == null) {
                            logger.Info($"{core.logCommonMessage},{logPrefix}, root user not found, adding root/contensive");
                            root = DbBaseModel.addEmpty<PersonModel>(core.cpParent);
                            root.name = defaultRootUserName;
                            root.firstName = defaultRootUserName;
                            root.username = defaultRootUserUsername;
                            root.password = defaultRootUserPassword;
                            root.developer = true;
                            root.contentControlId = ContentMetadataModel.getContentId(core, "people");
                            try {
                                root.save(core.cpParent);
                            } catch (Exception ex) {
                                string errMsg = "error prevented root user update";
                                logger.Error(ex, $"{core.logCommonMessage},{errMsg}");
                            }
                        }
                        //
                        // -- verify root user
                        if (root != null && SiteManagerGroup != null) {
                            //
                            // -- verify root is in site managers
                            var memberRuleList = DbBaseModel.createList<MemberRuleModel>(core.cpParent, "(groupid=" + SiteManagerGroup.id + ")and(MemberID=" + root.id + ")");
                            if (memberRuleList.Count() == 0) {
                                var memberRule = DbBaseModel.addEmpty<MemberRuleModel>(core.cpParent);
                                memberRule.groupId = SiteManagerGroup.id;
                                memberRule.memberId = root.id;
                                memberRule.save(core.cpParent);
                            }
                        }
                        //
                        // -- set build version so a scratch build will not go through data conversion
                        DataBuildVersion = CoreController.codeVersion();
                        core.siteProperties.dataBuildVersion = CoreController.codeVersion();
                    }
                    //
                    // -- data updates
                    logger.Info($"{core.logCommonMessage},{logPrefix}, run database conversions, DataBuildVersion [" + DataBuildVersion + "], software version [" + CoreController.codeVersion() + "]");
                    BuildDataMigrationController.migrateData(core, DataBuildVersion, logPrefix);
                    //
                    //  verify data
                    logger.Info($"{core.logCommonMessage},{logPrefix}, verify records required");
                    verifyAdminMenus(core, DataBuildVersion);
                    verifyLanguageRecords(core);
                    verifyCountries(core);
                    verifyStates(core);
                    verifyLibraryFolders(core);
                    verifyLibraryFileTypes(core);
                    verifyDefaultGroups(core);
                    verifyLayouts(core);
                    //
                    // -- verify many to many triggers for all many-to-many fields
                    verifyManyManyDeleteTriggers(core);
                    //
                    logger.Info($"{core.logCommonMessage},{logPrefix}, verify Site Properties");
                    if (repair) {
                        //
                        // -- repair, set values to what the default system uses
                        core.siteProperties.setProperty(sitePropertyName_ServerPageDefault, sitePropertyDefaultValue_ServerPageDefault);
                        core.siteProperties.setProperty("AdminURL", "/" + core.appConfig.adminRoute);
                    }
                    //
                    // todo remove site properties not used, put all in preferences
                    core.siteProperties.getText("AllowAutoLogin", "False");
                    core.siteProperties.getText("AllowChildMenuHeadline", "True");
                    core.siteProperties.getText("AllowContentAutoLoad", "True");
                    core.siteProperties.getText("AllowContentSpider", "False");
                    core.siteProperties.getText("AllowContentWatchLinkUpdate", "True");
                    core.siteProperties.getText("ConvertContentText2HTML", "False");
                    core.siteProperties.getText("AllowMemberJoin", "False");
                    core.siteProperties.getText("AllowPasswordEmail", "True");
                    core.siteProperties.getText("AllowWorkflowAuthoring", "False");
                    core.siteProperties.getText("ArchiveAllowFileClean", "False");
                    core.siteProperties.getText("ArchiveRecordAgeDays", "2");
                    core.siteProperties.getText("ArchiveTimeOfDay", "2:00:00 AM");
                    core.siteProperties.getText("BreadCrumbDelimiter", "&nbsp;&gt;&nbsp;");
                    core.siteProperties.getText("CalendarYearLimit", "1");
                    core.siteProperties.getText("ContentPageCompatibility21", "false");
                    core.siteProperties.getText("DefaultFormInputHTMLHeight", "500");
                    core.siteProperties.getText("DefaultFormInputTextHeight", "1");
                    core.siteProperties.getText("DefaultFormInputWidth", "60");
                    core.siteProperties.getText("EditLockTimeout", "5");
                    core.siteProperties.getText("EmailAdmin", core.cpParent.ServerConfig.defaultEmailContact);
                    core.siteProperties.getText("EmailFromAddress", core.cpParent.ServerConfig.defaultEmailContact);
                    core.siteProperties.getText("EmailPublishSubmitFrom", core.cpParent.ServerConfig.defaultEmailContact);
                    core.siteProperties.getText("Language", "English");
                    core.siteProperties.getText("PageContentMessageFooter", "Copyright " + core.appConfig.domainList[0]);
                    core.siteProperties.getText("SelectFieldLimit", "4000");
                    core.siteProperties.getText("SelectFieldWidthLimit", "100");
                    core.siteProperties.getText("SMTPServer", "127.0.0.1");
                    core.siteProperties.getText("TextSearchEndTag", "<!-- TextSearchEnd -->");
                    core.siteProperties.getText("TextSearchStartTag", "<!-- TextSearchStart -->");
                    core.siteProperties.getBoolean("AllowLinkAlias", true);
                    core.siteProperties.getBoolean("allow plain text password", false);
                    core.siteProperties.getBoolean("ALLOW ADDONLIST EDITOR FOR QUICK EDITOR", true);
                    //
                    AddonModel defaultRouteAddon = core.cacheRuntime.addonCache.create(core.siteProperties.defaultRouteId);
                    if (defaultRouteAddon == null) {
                        defaultRouteAddon = core.cacheRuntime.addonCache.create(addonGuidPageManager);
                        if (defaultRouteAddon != null) {
                            core.siteProperties.defaultRouteId = defaultRouteAddon.id;
                        }
                    }
                    //
                    // - if repair, reinstall all upgradable collections not already re-installed
                    if (repair) {
                        foreach (var collection in DbBaseModel.createList<AddonCollectionModel>(core.cpParent, "(updatable>0)")) {
                            if (!collectionsInstalledList.Contains(collection.ccguid)) {
                                //
                                // -- install all of them, ignore install errors
                                ErrorReturnModel installErrorMessage = new();
                                nonCriticalErrorList = new List<string>();
                                CollectionLibraryController.installCollectionFromLibrary(core, false, new Stack<string>(), collection.ccguid, ref installErrorMessage, isNewBuild, repair, ref nonCriticalErrorList, "", ref collectionsInstalledList, skipCdefInstall);
                            }
                        }
                    }
                    //
                    int StyleSN = core.siteProperties.getInteger("StylesheetSerialNumber");
                    if (StyleSN > 0) {
                        StyleSN += 1;
                        core.siteProperties.setProperty("StylesheetSerialNumber", StyleSN.ToString());
                    }
                    //
                    // clear all cache
                    core.cache.invalidateAll();
                    if (isNewBuild) {
                        //
                        // -- setup default site
                        verifyBasicWebSiteData(core);
                    }
                    //
                    // ----- internal upgrade complete
                    {
                        logger.Info($"{core.logCommonMessage},{logPrefix}, internal upgrade complete, set Buildversion to " + CoreController.codeVersion());
                        core.siteProperties.setProperty("BuildVersion", CoreController.codeVersion());
                    }
                    //
                    // ----- Explain, put up a link and exit without continuing
                    core.cache.invalidateAll();
                    logger.Info($"{core.logCommonMessage},{logPrefix}, Upgrade Complete");
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// verify there is only one group with this name and guid. merge dups to the first.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="logPrefix"></param>
        /// <param name="groupGuid"></param>
        /// <param name="groupName"></param>
        /// <returns></returns>
        private static GroupModel verifyGroup(CoreController core, string logPrefix, string groupGuid, string groupName) {
            //
            // -- site managers group is created in the base collection install, with the wrong guid. fix the guid before verifying the group fix new builds.
            core.db.executeNonQuery($"update ccGroups set ccguid={DbController.encodeSQLText(groupGuid)} where name={DbController.encodeSQLText(groupName)};");

            logger.Info($"{core.logCommonMessage},{logPrefix}, verify group [{groupName}]");
            GroupModel group = null;
            List<GroupModel> groups = DbBaseModel.createList<GroupModel>(core.cpParent, $"ccguid={DbController.encodeSQLText(groupGuid)}");
            if (groups.Count == 0) {
                logger.Info($"{core.logCommonMessage},{logPrefix}, create group [{groupName}]");
                group = DbBaseModel.addEmpty<GroupModel>(core.cpParent);
                group.name = groupName;
                group.caption = groupName;
                group.allowBulkEmail = true;
                group.ccguid = groupGuid;
                try {
                    group.save(core.cpParent);
                } catch (Exception ex) {
                    logger.Info($"{core.logCommonMessage},{logPrefix}, error creating site managers group. " + ex);
                }
            } else if (groups.Count == 1) {
                group = groups.First();
            } else {
                //
                // -- merge 2+ to the first
                group = groups.First();
                foreach (GroupModel groupDup in groups.Skip(1)) {
                    core.db.executeNonQuery("update ccMemberRules set groupid=" + group.id + " where groupid=" + groupDup.id + ";");
                    DbBaseModel.delete<GroupModel>(core.cpParent, groupDup.id);
                }
            }
            return group;
        }

        //
        //====================================================================================================
        //
        private static void verifyAdminMenus(CoreController core, string DataBuildVersion) {
            try {
                DataTable dt = core.db.executeQuery("Select ID,Name,ParentID from ccMenuEntries where (active<>0) Order By ParentID,Name");
                if (dt.Rows.Count > 0) {
                    string FieldLast = "";
                    for (var rowptr = 0; rowptr < dt.Rows.Count; rowptr++) {
                        string FieldNew = encodeText(dt.Rows[rowptr]["name"]) + "." + encodeText(dt.Rows[rowptr]["parentid"]);
                        if (FieldNew == FieldLast) {
                            int FieldRecordId = encodeInteger(dt.Rows[rowptr]["ID"]);
                            core.db.executeNonQuery("Update ccMenuEntries set active=0 where ID=" + FieldRecordId + ";");
                        }
                        FieldLast = FieldNew;
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// verify a simple record exists
        /// </summary>
        /// <param name="core"></param>
        /// <param name="contentName"></param>
        /// <param name="name"></param>
        /// <param name="sqlName"></param>
        /// <param name="sqlValue"></param>
        private static void verifyRecord(CoreController core, string contentName, string name, string sqlName, string sqlValue) {
            try {
                var metaData = ContentMetadataModel.createByUniqueName(core, contentName);
                DataTable dt = core.db.executeQuery("SELECT ID FROM " + metaData.tableName + " WHERE NAME=" + DbController.encodeSQLText(name) + ";");
                if (dt.Rows.Count == 0) {
                    string sql1 = "insert into " + metaData.tableName + " (active,name";
                    string sql2 = ") values (1," + DbController.encodeSQLText(name);
                    string sql3 = ")";
                    if (!string.IsNullOrEmpty(sqlName)) {
                        sql1 += "," + sqlName;
                        sql2 += "," + sqlValue;
                    }
                    core.db.executeNonQuery(sql1 + sql2 + sql3);
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        /// <summary>
        /// verify a simple record exists
        /// </summary>
        /// <param name="core"></param>
        /// <param name="contentName"></param>
        /// <param name="name"></param>
        /// <param name="sqlName"></param>
        private static void verifyRecord(CoreController core, string contentName, string name, string sqlName)
            => verifyRecord(core, contentName, name, sqlName, "");
        /// <summary>
        /// verify a simple record exists
        /// </summary>
        /// <param name="core"></param>
        /// <param name="contentName"></param>
        /// <param name="name"></param>
        private static void verifyRecord(CoreController core, string contentName, string name)
            => verifyRecord(core, contentName, name, "", "");
        //
        //====================================================================================================
        /// <summary>
        /// gaurantee db fields meet minimum requirements. Like dateTime precision
        /// </summary>
        /// <param name="core"></param>
        /// <param name="logPrefix"></param>
        private static void verifySqlfieldCompatibility(CoreController core, string logPrefix) {
            string hint = "0";
            try {
                //
                // verify Db field schema for fields handled internally (fix datatime2(0) problem -- need at least 3 digits for precision)
                var tableList = DbBaseModel.createList<TableModel>(core.cpParent, "(1=1)", "dataSourceId");
                foreach (TableModel table in tableList) {
                    hint = "1";
                    var tableSchema = TableSchemaModel.getTableSchema(core, table.name, "default");
                    hint = "2";
                    if (tableSchema != null) {
                        hint = "3";
                        foreach (TableSchemaModel.ColumnSchemaModel column in tableSchema.columns) {
                            hint = "4";
                            if (column.DATA_TYPE.ToLowerInvariant() == "datetime2" && column.DATETIME_PRECISION < 3) {
                                //
                                logger.Info($"{core.logCommonMessage},{logPrefix}, verifySqlFieldCompatibility, conversion required, table [" + table.name + "], field [" + column.COLUMN_NAME + "], reason [datetime precision too low (" + column.DATETIME_PRECISION.ToString() + ")]");
                                //
                                // these can be very long queries for big tables 
                                int sqlTimeout = core.cpParent.Db.SQLTimeout;
                                core.cpParent.Db.SQLTimeout = 1800;
                                //
                                // drop any indexes that use this field
                                hint = "5";
                                bool indexDropped = false;
                                foreach (TableSchemaModel.IndexSchemaModel index in tableSchema.indexes) {
                                    if (index.indexKeyList.Contains(column.COLUMN_NAME)) {
                                        //
                                        logger.Info($"{core.logCommonMessage},{logPrefix}, verifySqlFieldCompatibility, index [" + index.index_name + "] must be dropped");
                                        core.db.deleteIndex(table.name, index.index_name);
                                        indexDropped = true;
                                        //
                                    }
                                }
                                hint = "6";
                                //
                                // -- datetime2(0)...datetime2(2) need to be converted to datetime2(7)
                                // -- rename column to tempName
                                string tempName = "tempDateTime" + getRandomInteger().ToString();
                                core.db.executeNonQuery("sp_rename '" + table.name + "." + column.COLUMN_NAME + "', '" + tempName + "', 'COLUMN';");
                                core.db.executeNonQuery("ALTER TABLE " + table.name + " ADD " + column.COLUMN_NAME + " DateTime2(7) NULL;");
                                core.db.executeNonQuery("update " + table.name + " set " + column.COLUMN_NAME + "=" + tempName + " ");
                                core.db.executeNonQuery("ALTER TABLE " + table.name + " DROP COLUMN " + tempName + ";");
                                //
                                hint = "7";
                                // recreate dropped indexes
                                if (indexDropped) {
                                    foreach (TableSchemaModel.IndexSchemaModel index in tableSchema.indexes) {
                                        if (index.indexKeyList.Contains(column.COLUMN_NAME)) {
                                            //
                                            logger.Info($"{core.logCommonMessage},{logPrefix}, verifySqlFieldCompatibility, recreating index [" + index.index_name + "]");
                                            core.db.createSQLIndex(table.name, index.index_name, index.index_keys);
                                            //
                                        }
                                    }
                                }
                                core.cpParent.Db.SQLTimeout = sqlTimeout;
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"hint [{hint}], {core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// verify the basic languages are populated
        /// </summary>
        /// <param name="core"></param>
        public static void verifyLanguageRecords(CoreController core) {
            try {
                //
                appendUpgradeLogAddStep(core, core.appConfig.name, "VerifyLanguageRecords", "Verify Language Records.");
                //
                verifyRecord(core, "Languages", "English", "HTTP_Accept_Language", "'en'");
                verifyRecord(core, "Languages", "Spanish", "HTTP_Accept_Language", "'es'");
                verifyRecord(core, "Languages", "French", "HTTP_Accept_Language", "'fr'");
                verifyRecord(core, "Languages", "Any", "HTTP_Accept_Language", "'any'");
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// verify the basic library folders
        /// </summary>
        /// <param name="core"></param>
        private static void verifyLibraryFolders(CoreController core) {
            try {
                //
                appendUpgradeLogAddStep(core, core.appConfig.name, "VerifyLibraryFolders", "Verify Library Folders: Images and Downloads");
                DataTable dt = core.db.executeQuery("select id from cclibraryfiles");
                if (dt.Rows.Count == 0) {
                    verifyRecord(core, "Library Folders", "Images");
                    verifyRecord(core, "Library Folders", "Downloads");
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// verify library folder types
        /// </summary>
        /// <param name="core"></param>
        private static void verifyLibraryFileTypes(CoreController core) {
            try {
                //
                // Load basic records -- default images are handled in the REsource Library through the " + cdnPrefix + "config/DefaultValues.txt GetDefaultValue(key) mechanism
                if (MetadataController.getRecordIdByUniqueName(core, "Library File Types", "Image") == 0) {
                    verifyRecord(core, "Library File Types", "Image", "ExtensionList", "'GIF,JPG,JPE,JPEG,BMP,PNG'");
                    verifyRecord(core, "Library File Types", "Image", "IsImage", "1");
                    verifyRecord(core, "Library File Types", "Image", "IsVideo", "0");
                    verifyRecord(core, "Library File Types", "Image", "IsDownload", "1");
                    verifyRecord(core, "Library File Types", "Image", "IsFlash", "0");
                }
                if (MetadataController.getRecordIdByUniqueName(core, "Library File Types", "Video") == 0) {
                    verifyRecord(core, "Library File Types", "Video", "ExtensionList", "'ASX,AVI,WMV,MOV,MPG,MPEG,MP4,QT,RM'");
                    verifyRecord(core, "Library File Types", "Video", "IsImage", "0");
                    verifyRecord(core, "Library File Types", "Video", "IsVideo", "1");
                    verifyRecord(core, "Library File Types", "Video", "IsDownload", "1");
                    verifyRecord(core, "Library File Types", "Video", "IsFlash", "0");
                }
                //
                if (MetadataController.getRecordIdByUniqueName(core, "Library File Types", "Audio") == 0) {
                    verifyRecord(core, "Library File Types", "Audio", "ExtensionList", "'AIF,AIFF,ASF,CDA,M4A,M4P,MP2,MP3,MPA,WAV,WMA'");
                    verifyRecord(core, "Library File Types", "Audio", "IsImage", "0");
                    verifyRecord(core, "Library File Types", "Audio", "IsVideo", "0");
                    verifyRecord(core, "Library File Types", "Audio", "IsDownload", "1");
                    verifyRecord(core, "Library File Types", "Audio", "IsFlash", "0");
                }
                //
                if (MetadataController.getRecordIdByUniqueName(core, "Library File Types", "Word") == 0) {
                    verifyRecord(core, "Library File Types", "Word", "ExtensionList", "'DOC'");
                    verifyRecord(core, "Library File Types", "Word", "IsImage", "0");
                    verifyRecord(core, "Library File Types", "Word", "IsVideo", "0");
                    verifyRecord(core, "Library File Types", "Word", "IsDownload", "1");
                    verifyRecord(core, "Library File Types", "Word", "IsFlash", "0");
                }
                //
                if (MetadataController.getRecordIdByUniqueName(core, "Library File Types", "Flash") == 0) {
                    verifyRecord(core, "Library File Types", "Flash", "ExtensionList", "'SWF'");
                    verifyRecord(core, "Library File Types", "Flash", "IsImage", "0");
                    verifyRecord(core, "Library File Types", "Flash", "IsVideo", "0");
                    verifyRecord(core, "Library File Types", "Flash", "IsDownload", "1");
                    verifyRecord(core, "Library File Types", "Flash", "IsFlash", "1");
                }
                //
                if (MetadataController.getRecordIdByUniqueName(core, "Library File Types", "PDF") == 0) {
                    verifyRecord(core, "Library File Types", "PDF", "ExtensionList", "'PDF'");
                    verifyRecord(core, "Library File Types", "PDF", "IsImage", "0");
                    verifyRecord(core, "Library File Types", "PDF", "IsVideo", "0");
                    verifyRecord(core, "Library File Types", "PDF", "IsDownload", "1");
                    verifyRecord(core, "Library File Types", "PDF", "IsFlash", "0");
                }
                //
                if (MetadataController.getRecordIdByUniqueName(core, "Library File Types", "XLS") == 0) {
                    verifyRecord(core, "Library File Types", "Excel", "ExtensionList", "'XLS'");
                    verifyRecord(core, "Library File Types", "Excel", "IsImage", "0");
                    verifyRecord(core, "Library File Types", "Excel", "IsVideo", "0");
                    verifyRecord(core, "Library File Types", "Excel", "IsDownload", "1");
                    verifyRecord(core, "Library File Types", "Excel", "IsFlash", "0");
                }
                //
                if (MetadataController.getRecordIdByUniqueName(core, "Library File Types", "PPT") == 0) {
                    verifyRecord(core, "Library File Types", "Power Point", "ExtensionList", "'PPT,PPS'");
                    verifyRecord(core, "Library File Types", "Power Point", "IsImage", "0");
                    verifyRecord(core, "Library File Types", "Power Point", "IsVideo", "0");
                    verifyRecord(core, "Library File Types", "Power Point", "IsDownload", "1");
                    verifyRecord(core, "Library File Types", "Power Point", "IsFlash", "0");
                }
                //
                if (MetadataController.getRecordIdByUniqueName(core, "Library File Types", "Default") == 0) {
                    verifyRecord(core, "Library File Types", "Default", "ExtensionList", "''");
                    verifyRecord(core, "Library File Types", "Default", "IsImage", "0");
                    verifyRecord(core, "Library File Types", "Default", "IsVideo", "0");
                    verifyRecord(core, "Library File Types", "Default", "IsDownload", "1");
                    verifyRecord(core, "Library File Types", "Default", "IsFlash", "0");
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// verify a state record
        /// </summary>
        /// <param name="core"></param>
        /// <param name="Name"></param>
        /// <param name="Abbreviation"></param>
        /// <param name="SaleTax"></param>
        /// <param name="CountryID"></param>
        private static void verifyState(CoreController core, string Name, string Abbreviation, double SaleTax, int CountryID) {
            try {
                var state = DbBaseModel.createByUniqueName<StateModel>(core.cpParent, Name);
                if (state == null) state = DbBaseModel.addEmpty<StateModel>(core.cpParent);
                state.abbreviation = Abbreviation;
                state.name = Name;
                state.salesTax = SaleTax;
                state.countryId = CountryID;
                state.save(core.cpParent, 0, true);
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// verify all default states
        /// </summary>
        /// <param name="core"></param>
        public static void verifyStates(CoreController core) {
            try {
                //
                appendUpgradeLogAddStep(core, core.appConfig.name, "VerifyStates", "Verify States");
                //
                verifyCountry(core, "United States", "US");
                int CountryID = MetadataController.getRecordIdByUniqueName(core, "Countries", "United States");
                //
                verifyState(core, "Alaska", "AK", 0.0D, CountryID);
                verifyState(core, "Alabama", "AL", 0.0D, CountryID);
                verifyState(core, "Arizona", "AZ", 0.0D, CountryID);
                verifyState(core, "Arkansas", "AR", 0.0D, CountryID);
                verifyState(core, "California", "CA", 0.0D, CountryID);
                verifyState(core, "Connecticut", "CT", 0.0D, CountryID);
                verifyState(core, "Colorado", "CO", 0.0D, CountryID);
                verifyState(core, "Delaware", "DE", 0.0D, CountryID);
                verifyState(core, "District of Columbia", "DC", 0.0D, CountryID);
                verifyState(core, "Florida", "FL", 0.0D, CountryID);
                verifyState(core, "Georgia", "GA", 0.0D, CountryID);

                verifyState(core, "Hawaii", "HI", 0.0D, CountryID);
                verifyState(core, "Idaho", "ID", 0.0D, CountryID);
                verifyState(core, "Illinois", "IL", 0.0D, CountryID);
                verifyState(core, "Indiana", "IN", 0.0D, CountryID);
                verifyState(core, "Iowa", "IA", 0.0D, CountryID);
                verifyState(core, "Kansas", "KS", 0.0D, CountryID);
                verifyState(core, "Kentucky", "KY", 0.0D, CountryID);
                verifyState(core, "Louisiana", "LA", 0.0D, CountryID);
                verifyState(core, "Massachusetts", "MA", 0.0D, CountryID);
                verifyState(core, "Maine", "ME", 0.0D, CountryID);

                verifyState(core, "Maryland", "MD", 0.0D, CountryID);
                verifyState(core, "Michigan", "MI", 0.0D, CountryID);
                verifyState(core, "Minnesota", "MN", 0.0D, CountryID);
                verifyState(core, "Missouri", "MO", 0.0D, CountryID);
                verifyState(core, "Mississippi", "MS", 0.0D, CountryID);
                verifyState(core, "Montana", "MT", 0.0D, CountryID);
                verifyState(core, "North Carolina", "NC", 0.0D, CountryID);
                verifyState(core, "Nebraska", "NE", 0.0D, CountryID);
                verifyState(core, "New Hampshire", "NH", 0.0D, CountryID);
                verifyState(core, "New Mexico", "NM", 0.0D, CountryID);

                verifyState(core, "New Jersey", "NJ", 0.0D, CountryID);
                verifyState(core, "New York", "NY", 0.0D, CountryID);
                verifyState(core, "Nevada", "NV", 0.0D, CountryID);
                verifyState(core, "North Dakota", "ND", 0.0D, CountryID);
                verifyState(core, "Ohio", "OH", 0.0D, CountryID);
                verifyState(core, "Oklahoma", "OK", 0.0D, CountryID);
                verifyState(core, "Oregon", "OR", 0.0D, CountryID);
                verifyState(core, "Pennsylvania", "PA", 0.0D, CountryID);
                verifyState(core, "Rhode Island", "RI", 0.0D, CountryID);
                verifyState(core, "South Carolina", "SC", 0.0D, CountryID);

                verifyState(core, "South Dakota", "SD", 0.0D, CountryID);
                verifyState(core, "Tennessee", "TN", 0.0D, CountryID);
                verifyState(core, "Texas", "TX", 0.0D, CountryID);
                verifyState(core, "Utah", "UT", 0.0D, CountryID);
                verifyState(core, "Vermont", "VT", 0.0D, CountryID);
                verifyState(core, "Virginia", "VA", 0.045, CountryID);
                verifyState(core, "Washington", "WA", 0.0D, CountryID);
                verifyState(core, "Wisconsin", "WI", 0.0D, CountryID);
                verifyState(core, "West Virginia", "WV", 0.0D, CountryID);
                verifyState(core, "Wyoming", "WY", 0.0D, CountryID);
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// verify a country
        /// </summary>
        /// <param name="core"></param>
        /// <param name="name"></param>
        /// <param name="abbreviation"></param>
        private static void verifyCountry(CoreController core, string name, string abbreviation) {
            try {
                using (var csData = new CsModel(core)) {
                    csData.open("Countries", "name=" + DbController.encodeSQLText(name));
                    if (!csData.ok()) {
                        csData.close();
                        csData.insert("Countries");
                        if (csData.ok()) {
                            csData.set("ACTIVE", true);
                        }
                    }
                    if (csData.ok()) {
                        csData.set("NAME", encodeInitialCaps(name));
                        csData.set("Abbreviation", abbreviation);
                        if (name.ToLowerInvariant() == "united states") {
                            csData.set("DomesticShipping", 1);
                        } else {
                            csData.set("DomesticShipping", 0);
                        }
                    }
                    csData.close();
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Verify all base countries
        /// </summary>
        /// <param name="core"></param>
        public static void verifyCountries(CoreController core) {
            try {
                //
                appendUpgradeLogAddStep(core, core.appConfig.name, "VerifyCountries", "Verify Countries");
                //
                string list = core.programFiles.readFileText("DefaultCountryList.txt");
                string[] rows = stringSplit(list, Environment.NewLine);
                foreach (var row in rows) {
                    if (string.IsNullOrEmpty(row)) { continue; }
                    string[] attrs = row.Split(';');
                    if (attrs.Length < 2) { continue; }
                    verifyCountry(core, attrs[0], attrs[1]);

                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// verify default groups
        /// </summary>
        /// <param name="core"></param>
        public static void verifyDefaultGroups(CoreController core) {
            try {
                appendUpgradeLogAddStep(core, core.appConfig.name, "VerifyDefaultGroups", "Verify Default Groups");
                //
                int groupId = GroupController.add(core, "Site Managers");
                string sql = "Update ccContent Set EditorGroupID=" + DbController.encodeSQLNumber(groupId) + " where EditorGroupID is null;";
                core.db.executeNonQuery(sql);
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Verify all the core tables
        /// </summary>
        /// <param name="core"></param>
        /// <param name="logPrefix"></param>
        /// <param name="isNewBuild">if new build, basic indexes are created</param>
        internal static void verifyBasicTables(CoreController core, string logPrefix) {
            try {
                {
                    logPrefix += "-verifyBasicTables";
                    logger.Info($"{core.logCommonMessage},{logPrefix}, enter");
                    //
                    core.db.createSQLTable("ccDataSources");
                    core.db.createSQLTableField("ccDataSources", "username", CPContentBaseClass.FieldTypeIdEnum.Text);
                    core.db.createSQLTableField("ccDataSources", "password", CPContentBaseClass.FieldTypeIdEnum.Text);
                    core.db.createSQLTableField("ccDataSources", "connString", CPContentBaseClass.FieldTypeIdEnum.Text);
                    core.db.createSQLTableField("ccDataSources", "endpoint", CPContentBaseClass.FieldTypeIdEnum.Text);
                    core.db.createSQLTableField("ccDataSources", "dbTypeId", CPContentBaseClass.FieldTypeIdEnum.Lookup);
                    core.db.createSQLTableField("ccDataSources", "secure", CPContentBaseClass.FieldTypeIdEnum.Boolean);
                    //
                    core.db.createSQLTable("ccTables");
                    core.db.createSQLTableField("ccTables", "DataSourceID", CPContentBaseClass.FieldTypeIdEnum.Lookup);
                    //
                    core.db.createSQLTable("ccContent");
                    core.db.createSQLTableField("ccContent", "ContentTableID", CPContentBaseClass.FieldTypeIdEnum.Integer);
                    core.db.createSQLTableField("ccContent", "AuthoringTableId", CPContentBaseClass.FieldTypeIdEnum.Integer);
                    core.db.createSQLTableField("ccContent", "AllowAdd", CPContentBaseClass.FieldTypeIdEnum.Boolean);
                    core.db.createSQLTableField("ccContent", "AllowDelete", CPContentBaseClass.FieldTypeIdEnum.Boolean);
                    core.db.createSQLTableField("ccContent", "AllowWorkflowAuthoring", CPContentBaseClass.FieldTypeIdEnum.Boolean);
                    core.db.createSQLTableField("ccContent", "DeveloperOnly", CPContentBaseClass.FieldTypeIdEnum.Boolean);
                    core.db.createSQLTableField("ccContent", "AdminOnly", CPContentBaseClass.FieldTypeIdEnum.Boolean);
                    core.db.createSQLTableField("ccContent", "ParentId", CPContentBaseClass.FieldTypeIdEnum.Integer);
                    core.db.createSQLTableField("ccContent", "DefaultSortMethodId", CPContentBaseClass.FieldTypeIdEnum.Integer);
                    core.db.createSQLTableField("ccContent", "DropDownFieldList", CPContentBaseClass.FieldTypeIdEnum.Text);
                    core.db.createSQLTableField("ccContent", "EditorGroupId", CPContentBaseClass.FieldTypeIdEnum.Integer);
                    core.db.createSQLTableField("ccContent", "AllowCalendarEvents", CPContentBaseClass.FieldTypeIdEnum.Boolean);
                    core.db.createSQLTableField("ccContent", "AllowContentTracking", CPContentBaseClass.FieldTypeIdEnum.Boolean);
                    core.db.createSQLTableField("ccContent", "AllowTopicRules", CPContentBaseClass.FieldTypeIdEnum.Boolean);
                    core.db.createSQLTableField("ccContent", "AllowContentChildTool", CPContentBaseClass.FieldTypeIdEnum.Boolean);
                    core.db.createSQLTableField("ccContent", "IconLink", CPContentBaseClass.FieldTypeIdEnum.Link);
                    core.db.createSQLTableField("ccContent", "IconHeight", CPContentBaseClass.FieldTypeIdEnum.Integer);
                    core.db.createSQLTableField("ccContent", "IconWidth", CPContentBaseClass.FieldTypeIdEnum.Integer);
                    core.db.createSQLTableField("ccContent", "IconSprites", CPContentBaseClass.FieldTypeIdEnum.Integer);
                    core.db.createSQLTableField("ccContent", "installedByCollectionId", CPContentBaseClass.FieldTypeIdEnum.Integer);
                    core.db.createSQLTableField("ccContent", "IsBaseContent", CPContentBaseClass.FieldTypeIdEnum.Boolean);
                    //
                    core.db.createSQLTable("ccFields");
                    core.db.createSQLTableField("ccFields", "ContentId", CPContentBaseClass.FieldTypeIdEnum.Integer);
                    core.db.createSQLTableField("ccFields", "Type", CPContentBaseClass.FieldTypeIdEnum.Integer);
                    core.db.createSQLTableField("ccFields", "Caption", CPContentBaseClass.FieldTypeIdEnum.Text);
                    core.db.createSQLTableField("ccFields", "ReadOnly", CPContentBaseClass.FieldTypeIdEnum.Boolean);
                    core.db.createSQLTableField("ccFields", "NotEditable", CPContentBaseClass.FieldTypeIdEnum.Boolean);
                    core.db.createSQLTableField("ccFields", "LookupContentId", CPContentBaseClass.FieldTypeIdEnum.Integer);
                    core.db.createSQLTableField("ccFields", "LookupContentSqlFilter", CPContentBaseClass.FieldTypeIdEnum.Text);
                    core.db.createSQLTableField("ccFields", "RedirectContentId", CPContentBaseClass.FieldTypeIdEnum.Integer);
                    core.db.createSQLTableField("ccFields", "RedirectPath", CPContentBaseClass.FieldTypeIdEnum.Text);
                    core.db.createSQLTableField("ccFields", "RedirectId", CPContentBaseClass.FieldTypeIdEnum.Text);
                    core.db.createSQLTableField("ccFields", "HelpMessage", CPContentBaseClass.FieldTypeIdEnum.LongText); // deprecated but Im chicken to remove this
                    core.db.createSQLTableField("ccFields", "UniqueName", CPContentBaseClass.FieldTypeIdEnum.Boolean);
                    core.db.createSQLTableField("ccFields", "TextBuffered", CPContentBaseClass.FieldTypeIdEnum.Boolean);
                    core.db.createSQLTableField("ccFields", "Password", CPContentBaseClass.FieldTypeIdEnum.Boolean);
                    core.db.createSQLTableField("ccFields", "IndexColumn", CPContentBaseClass.FieldTypeIdEnum.Integer);
                    core.db.createSQLTableField("ccFields", "IndexWidth", CPContentBaseClass.FieldTypeIdEnum.Text);
                    core.db.createSQLTableField("ccFields", "IndexSortPriority", CPContentBaseClass.FieldTypeIdEnum.Integer);
                    core.db.createSQLTableField("ccFields", "IndexSortDirection", CPContentBaseClass.FieldTypeIdEnum.Integer);
                    core.db.createSQLTableField("ccFields", "EditSortPriority", CPContentBaseClass.FieldTypeIdEnum.Integer);
                    core.db.createSQLTableField("ccFields", "AdminOnly", CPContentBaseClass.FieldTypeIdEnum.Boolean);
                    core.db.createSQLTableField("ccFields", "DeveloperOnly", CPContentBaseClass.FieldTypeIdEnum.Boolean);
                    core.db.createSQLTableField("ccFields", "DefaultValue", CPContentBaseClass.FieldTypeIdEnum.Text);
                    core.db.createSQLTableField("ccFields", "Required", CPContentBaseClass.FieldTypeIdEnum.Boolean);
                    core.db.createSQLTableField("ccFields", "HTMLContent", CPContentBaseClass.FieldTypeIdEnum.Boolean);
                    core.db.createSQLTableField("ccFields", "Authorable", CPContentBaseClass.FieldTypeIdEnum.Boolean);
                    core.db.createSQLTableField("ccFields", "ManyToManyContentId", CPContentBaseClass.FieldTypeIdEnum.Integer);
                    core.db.createSQLTableField("ccFields", "ManyToManyRuleContentId", CPContentBaseClass.FieldTypeIdEnum.Integer);
                    core.db.createSQLTableField("ccFields", "ManyToManyRulePrimaryField", CPContentBaseClass.FieldTypeIdEnum.Text);
                    core.db.createSQLTableField("ccFields", "ManyToManyRuleSecondaryField", CPContentBaseClass.FieldTypeIdEnum.Text);
                    core.db.createSQLTableField("ccFields", "RSSTitleField", CPContentBaseClass.FieldTypeIdEnum.Boolean);
                    core.db.createSQLTableField("ccFields", "RSSDescriptionField", CPContentBaseClass.FieldTypeIdEnum.Boolean);
                    core.db.createSQLTableField("ccFields", "MemberSelectGroupId", CPContentBaseClass.FieldTypeIdEnum.Integer);
                    core.db.createSQLTableField("ccFields", "EditTab", CPContentBaseClass.FieldTypeIdEnum.Text);
                    core.db.createSQLTableField("ccFields", "Scramble", CPContentBaseClass.FieldTypeIdEnum.Boolean);
                    core.db.createSQLTableField("ccFields", "LookupList", CPContentBaseClass.FieldTypeIdEnum.Text);
                    core.db.createSQLTableField("ccFields", "IsBaseField", CPContentBaseClass.FieldTypeIdEnum.Boolean);
                    core.db.createSQLTableField("ccFields", "installedByCollectionId", CPContentBaseClass.FieldTypeIdEnum.Integer);
                    //
                    core.db.createSQLTable("ccFieldHelp");
                    core.db.createSQLTableField("ccFieldHelp", "FieldID", CPContentBaseClass.FieldTypeIdEnum.Lookup);
                    core.db.createSQLTableField("ccFieldHelp", "HelpDefault", CPContentBaseClass.FieldTypeIdEnum.LongText);
                    core.db.createSQLTableField("ccFieldHelp", "HelpCustom", CPContentBaseClass.FieldTypeIdEnum.LongText);
                    //
                    core.db.createSQLTable("ccSetup");
                    core.db.createSQLTableField("ccSetup", "FieldValue", CPContentBaseClass.FieldTypeIdEnum.Text);
                    core.db.createSQLTableField("ccSetup", "DeveloperOnly", CPContentBaseClass.FieldTypeIdEnum.Boolean);
                    //
                    core.db.createSQLTable("ccSortMethods");
                    core.db.createSQLTableField("ccSortMethods", "OrderByClause", CPContentBaseClass.FieldTypeIdEnum.Text);
                    //
                    core.db.createSQLTable("ccFieldTypes");
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        //
        private static void verifyManyManyDeleteTriggers(CoreController core) {
            logger.Debug($"{core.logCommonMessage},verifyManyManyDeleteTriggers not implemented");
        }
        //
        //====================================================================================================
        //  todo deprecate 
        private static void appendUpgradeLog(CoreController core, string appName, string Method, string Message) {
            logger.Info($"{core.logCommonMessage},app [" + appName + "], Method [" + Method + "], Message [" + Message + "]");
        }
        //
        //====================================================================================================
        // todo deprecate
        private static void appendUpgradeLogAddStep(CoreController core, string appName, string Method, string Message) {
            appendUpgradeLog(core, appName, Method, Message);
        }
        //
        //====================================================================================================
        /// <summary>
        /// verify a nanigator entry
        /// </summary>
        /// <param name="core"></param>
        /// <param name="menu"></param>
        /// <param name="InstalledByCollectionID"></param>
        /// <returns></returns>
        public static int verifyNavigatorEntry(CoreController core, MetadataMiniCollectionModel.MiniCollectionMenuModel menu, int InstalledByCollectionID) {
            int returnEntry = 0;
            try {
                if (!string.IsNullOrEmpty(menu.name.Trim())) {
                    if (!string.IsNullOrWhiteSpace(menu.addonGuid)) {
                        returnEntry = 0;
                    }
                    AddonModel addon = !string.IsNullOrWhiteSpace(menu.addonGuid) ? core.cacheRuntime.addonCache.create(menu.addonGuid) : null;
                    addon ??= (!string.IsNullOrWhiteSpace(menu.addonName) ? core.cacheRuntime.addonCache.createByUniqueName(menu.addonName) : null);
                    int parentId = verifyNavigatorEntry_getParentIdFromNameSpace(core, menu.menuNameSpace);
                    int contentId = ContentMetadataModel.getContentId(core, menu.contentName);
                    string listCriteria = "(name=" + DbController.encodeSQLText(menu.name) + ")and(Parentid=" + parentId + ")";
                    List<NavigatorEntryModel> entryList = DbBaseModel.createList<NavigatorEntryModel>(core.cpParent, listCriteria, "id");
                    NavigatorEntryModel entry = null;
                    if (entryList.Count == 0) {
                        entry = DbBaseModel.addEmpty<NavigatorEntryModel>(core.cpParent);
                        entry.name = menu.name.Trim();
                        entry.parentId = parentId;
                    } else {
                        entry = entryList.First();
                    }
                    if (contentId <= 0) {
                        entry.contentId = 0;
                    } else {
                        entry.contentId = contentId;
                    }
                    entry.linkPage = menu.linkPage;
                    entry.sortOrder = menu.sortOrder;
                    entry.adminOnly = menu.adminOnly;
                    entry.developerOnly = menu.developerOnly;
                    entry.newWindow = menu.newWindow;
                    entry.active = menu.active;
                    entry.addonId = addon == null ? 0 : addon.id;
                    entry.ccguid = menu.guid;
                    entry.navIconTitle = menu.navIconTitle;
                    entry.navIconType = getListIndex(menu.navIconType, NavIconTypeList);
                    entry.installedByCollectionId = InstalledByCollectionID;
                    entry.save(core.cpParent);
                    returnEntry = entry.id;
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
            return returnEntry;
        }
        //====================================================================================================
        /// <summary>
        /// get navigator id from namespace
        /// </summary>
        /// <param name="core"></param>
        /// <param name="menuNameSpace"></param>
        /// <returns></returns>
        public static int verifyNavigatorEntry_getParentIdFromNameSpace(CoreController core, string menuNameSpace) {
            int parentRecordId = 0;
            try {
                if (!string.IsNullOrEmpty(menuNameSpace)) {
                    string[] parents = menuNameSpace.Trim().Split('.');
                    foreach (var parent in parents) {
                        string recordName = parent.Trim();
                        if (!string.IsNullOrEmpty(recordName)) {
                            string Criteria = "(name=" + DbController.encodeSQLText(recordName) + ")";
                            if (parentRecordId == 0) {
                                Criteria += "and((Parentid is null)or(Parentid=0))";
                            } else {
                                Criteria += "and(Parentid=" + parentRecordId + ")";
                            }
                            int RecordId = 0;
                            using (var csData = new CsModel(core)) {
                                csData.open(NavigatorEntryModel.tableMetadata.contentName, Criteria, "ID", true, 0, "ID", 1);
                                if (csData.ok()) {
                                    RecordId = csData.getInteger("ID");
                                }
                                csData.close();
                                if (RecordId == 0) {
                                    csData.insert(NavigatorEntryModel.tableMetadata.contentName);
                                    if (csData.ok()) {
                                        RecordId = csData.getInteger("ID");
                                        csData.set("name", recordName);
                                        csData.set("parentID", parentRecordId);
                                    }
                                }
                            }
                            parentRecordId = RecordId;
                        }
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
            return parentRecordId;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Create an entry in the Sort Methods Table
        /// </summary>
        internal static void verifySortMethod(CoreController core, string name, string orderByCriteria) {
            try {
                //
                NameValueCollection sqlList = new() {
                    { "name", DbController.encodeSQLText(name) },
                    { "orderbyclause", DbController.encodeSQLText(orderByCriteria) }
                };
                //
                using (DataTable dt = core.db.openTable("ccSortMethods", "name=" + DbController.encodeSQLText(name), "id", "id", 1, 1)) {
                    if (dt?.Rows is not null && dt.Rows.Count > 0) {
                        //
                        // update sort method
                        int recordId = encodeInteger(dt.Rows[0]["id"]);
                        core.db.update("ccsortmethods", "id=" + recordId.ToString(), sqlList);
                        DbBaseModel.invalidateCacheOfRecord<SortMethodModelx>(core.cpParent, recordId);
                        return;
                    }
                }
                //
                // Create the new sort method
                core.db.insert("ccSortMethods", sqlList, 0);
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        //
        public static void verifySortMethods(CoreController core) {
            try {
                //
                logger.Info($"{core.logCommonMessage},Verify Sort Records");
                //
                verifySortMethod(core, "By Name", "Name");
                verifySortMethod(core, "By Alpha Sort Order Field", "SortOrder");
                verifySortMethod(core, "By Date", "DateAdded");
                verifySortMethod(core, "By Date Reverse", "DateAdded Desc");
                verifySortMethod(core, "By Alpha Sort Order Then Oldest First", "SortOrder,ID");
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Get a ContentID from the ContentName using just the tables
        /// </summary>
        internal static void verifyContentFieldTypes(CoreController core) {
            try {
                //
                int RowsFound = 0;
                bool TableBad = false;
                using (DataTable rs = core.db.executeQuery("Select ID from ccFieldTypes order by id")) {
                    if (!DbController.isDataTableOk(rs)) {
                        //
                        // problem
                        //
                        TableBad = true;
                    } else {
                        //
                        // Verify the records that are there
                        //
                        RowsFound = 0;
                        foreach (DataRow dr in rs.Rows) {
                            RowsFound = RowsFound + 1;
                            if (RowsFound != encodeInteger(dr["ID"])) {
                                //
                                // Bad Table
                                //
                                TableBad = true;
                                break;
                            }
                        }
                    }

                }
                //
                // ----- Replace table if needed
                //
                if (TableBad) {
                    core.db.deleteTable("ccFieldTypes");
                    core.db.createSQLTable("ccFieldTypes");
                    RowsFound = 0;
                }
                //
                // ----- Add the number of rows needed
                //
                int RowsNeeded = Enum.GetNames(typeof(CPContentBaseClass.FieldTypeIdEnum)).Length - RowsFound;
                if (RowsNeeded > 0) {
                    int CId = ContentMetadataModel.getContentId(core, "Content Field Types");
                    if (CId <= 0) {
                        //
                        // Problem
                        //
                        logger.Error($"{core.logCommonMessage}", new GenericException("Content Field Types content definition was not found"));
                    } else {
                        while (RowsNeeded > 0) {
                            core.db.executeNonQuery("Insert into ccFieldTypes (active,contentcontrolid)values(1," + CId + ")");
                            RowsNeeded = RowsNeeded - 1;
                        }
                    }
                }
                //
                // ----- Update the Names of each row
                //
                core.db.executeNonQuery("Update ccFieldTypes Set active=1,Name='Integer' where ID=1;");
                core.db.executeNonQuery("Update ccFieldTypes Set active=1,Name='Text' where ID=2;");
                core.db.executeNonQuery("Update ccFieldTypes Set active=1,Name='LongText' where ID=3;");
                core.db.executeNonQuery("Update ccFieldTypes Set active=1,Name='Boolean' where ID=4;");
                core.db.executeNonQuery("Update ccFieldTypes Set active=1,Name='Date' where ID=5;");
                core.db.executeNonQuery("Update ccFieldTypes Set active=1,Name='File' where ID=6;");
                core.db.executeNonQuery("Update ccFieldTypes Set active=1,Name='Lookup' where ID=7;");
                core.db.executeNonQuery("Update ccFieldTypes Set active=1,Name='Redirect' where ID=8;");
                core.db.executeNonQuery("Update ccFieldTypes Set active=1,Name='Currency' where ID=9;");
                core.db.executeNonQuery("Update ccFieldTypes Set active=1,Name='TextFile' where ID=10;");
                core.db.executeNonQuery("Update ccFieldTypes Set active=1,Name='Image' where ID=11;");
                core.db.executeNonQuery("Update ccFieldTypes Set active=1,Name='Float' where ID=12;");
                core.db.executeNonQuery("Update ccFieldTypes Set active=1,Name='AutoIncrement' where ID=13;");
                core.db.executeNonQuery("Update ccFieldTypes Set active=1,Name='ManyToMany' where ID=14;");
                core.db.executeNonQuery("Update ccFieldTypes Set active=1,Name='Member Select' where ID=15;");
                core.db.executeNonQuery("Update ccFieldTypes Set active=1,Name='CSS File' where ID=16;");
                core.db.executeNonQuery("Update ccFieldTypes Set active=1,Name='XML File' where ID=17;");
                core.db.executeNonQuery("Update ccFieldTypes Set active=1,Name='Javascript File' where ID=18;");
                core.db.executeNonQuery("Update ccFieldTypes Set active=1,Name='Link' where ID=19;");
                core.db.executeNonQuery("Update ccFieldTypes Set active=1,Name='Resource Link' where ID=20;");
                core.db.executeNonQuery("Update ccFieldTypes Set active=1,Name='HTML' where ID=21;");
                core.db.executeNonQuery("Update ccFieldTypes Set active=1,Name='HTML File' where ID=22;");
                core.db.executeNonQuery("Update ccFieldTypes Set active=1,Name='HTML Code' where ID=23;");
                core.db.executeNonQuery("Update ccFieldTypes Set active=1,Name='HTML Code File' where ID=24;");
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        //
        public static void verifyBasicWebSiteData(CoreController core) {
            //
            // -- determine primary domain
            string primaryDomain = core.appConfig.name;
            var domain = DbBaseModel.createByUniqueName<DomainModel>(core.cpParent, primaryDomain);
            if (DbBaseModel.createByUniqueName<DomainModel>(core.cpParent, primaryDomain) == null) {
                domain = DbBaseModel.addDefault<DomainModel>(core.cpParent, ContentMetadataModel.getDefaultValueDict(core, "domains"));
                domain.name = primaryDomain;
            }
            //
            // -- Landing Page
            PageContentModel landingPage = DbBaseModel.create<PageContentModel>(core.cpParent, defaultLandingPageGuid);
            if (landingPage == null) {
                landingPage = DbBaseModel.addDefault<PageContentModel>(core.cpParent, ContentMetadataModel.getDefaultValueDict(core, "page content"));
                landingPage.name = "Home";
                landingPage.ccguid = defaultLandingPageGuid;
            }
            //
            // -- default template
            PageTemplateModel defaultTemplate = DbBaseModel.create<PageTemplateModel>(core.cpParent, singleColumnTemplateGuid);
            if (defaultTemplate == null) {
                // -- did not install correctly, build a placeholder
                // -- create content, never update content
                core.doc.pageController.template = DbBaseModel.addDefault<PageTemplateModel>(core.cpParent);
                core.doc.pageController.template.bodyHTML = Properties.Resources.DefaultTemplateHtml;
                core.doc.pageController.template.name = singleColumnTemplateName;
                core.doc.pageController.template.ccguid = singleColumnTemplateGuid;
                core.doc.pageController.template.save(core.cpParent);
            }
            //
            // -- verify menu record
            var menu = DbBaseModel.create<MenuModel>(core.cpParent, "Header Nav Menu");
            if (menu == null) {
                menu = DbBaseModel.addDefault<MenuModel>(core.cpParent, ContentMetadataModel.getDefaultValueDict(core, "Menus"));
                menu.ccguid = "Header Nav Menu";
                menu.name = "Header Nav Menu";
                menu.save(core.cpParent);
            }
            //
            // -- create menu record
            var menuPageRule = DbBaseModel.createFirstOfList<MenuPageRuleModel>(core.cpParent, "(menuid=" + menu.id + ")and(pageid=" + landingPage.id + ")", "id");
            if (menuPageRule == null) {
                menuPageRule = DbBaseModel.addDefault<MenuPageRuleModel>(core.cpParent, ContentMetadataModel.getDefaultValueDict(core, "Menu Page Rules"));
                menuPageRule.menuId = menu.id;
                menuPageRule.pageId = landingPage.id;
                menuPageRule.save(core.cpParent);
            }
            //
            // -- update domain
            domain.defaultTemplateId = defaultTemplate.id;
            domain.name = primaryDomain;
            domain.pageNotFoundPageId = landingPage.id;
            domain.rootPageId = landingPage.id;
            domain.typeId = (int)DomainModel.DomainTypeEnum.Normal;
            domain.visited = false;
            domain.save(core.cpParent);
            //
            landingPage.templateId = defaultTemplate.id;
            landingPage.copyfilename.content = defaultLandingPageHtml;
            landingPage.save(core.cpParent);
            //
            if (core.siteProperties.getInteger("LandingPageID", landingPage.id) == 0) {
                core.siteProperties.setProperty("LandingPageID", landingPage.id);
            }
            //
            // -- convert the data to textblock and addonlist
            BuildDataMigrationController.convertPageContentToAddonList(core, landingPage);
        }
        //
        //====================================================================================================
        /// <summary>
        /// add and upgrade layouts used in base platform
        /// </summary>
        /// <param name="core"></param>
        public static void verifyLayouts(CoreController core) {
            try {
                appendUpgradeLogAddStep(core, core.appConfig.name, "verifyLayouts", "add and upgrade layouts used in base platform");
                //
                core.cpParent.Layout.updateLayout(layoutAdminSiteGuid, layoutAdminSiteName, layoutAdminSiteCdnPathFilename);
                core.cpParent.Layout.updateLayout(layoutLinkAliasPreviewEditorGuid, layoutLinkAliasPreviewEditorName, layoutLinkAliasPreviewEditorCdnPathFilename);
                core.cpParent.Layout.updateLayout(layoutAdminEditIconGuid, layoutAdminEditIconName, layoutAdminEditIconCdnPathFilename);
                core.cpParent.Layout.updateLayout(layoutEditAddModalGuid, layoutEditAddModalName, layoutEditAddModalCdnPathFilename);
                core.cpParent.Layout.updateLayout(guidLayoutAdminUITwoColumnLeft, nameLayoutAdminUITwoColumnLeft, cdnPathFilenameLayoutAdminUITwoColumnLeft);
                core.cpParent.Layout.updateLayout(guidLayoutAdminUITwoColumnRight, layoutAdminUITwoColumnRightName, layoutAdminUITwoColumnRightCdnPathFilename);
                core.cpParent.Layout.updateLayout(layoutEditControlAutocompleteGuid, layoutEditControlAutocompleteName, layoutEditControlAutocompleteCdnPathFilename);
                _ = LayoutController.updateLayout(core.cpParent, 0, layoutAdminSidebarGuid, layoutAdminSidebarName, layoutAdminSidebarCdnPathFilename, layoutAdminSidebarCdnPathFilename);
                //
                _ = LayoutController.updateLayout(core.cpParent, 0, Constants.guidLayoutPageWithNav, Constants.nameLayoutPageWithNav, Constants.pathFilenameLayoutAdminUIPageWithNav, Constants.pathFilenameLayoutAdminUIPageWithNav);
                _ = LayoutController.updateLayout(core.cpParent, 0, Constants.guidLayoutAdminUITwoColumnLeft, Constants.nameLayoutPageWithNav, Constants.pathFilenameLayoutAdminUIPageWithNav, Constants.pathFilenameLayoutAdminUIPageWithNav);
                //
                core.cpParent.Layout.updateLayout(layoutEmailVerificationGuid, layoutEmailVerificationName, layoutEmailVerificationCdnPathFilename);
                core.cpParent.Layout.updateLayout(layoutCustomBlockingRegistrationGuid, layoutCustomBlockingRegistrationName, layoutCustomBlockingRegistrationCdnPathFilename);
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
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
