
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Addons.Tools;
using Contensive.Processor.Controllers;
using Contensive.Exceptions;
using Contensive.Processor.Models.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Contensive.Processor.Constants;
using static Contensive.Processor.Controllers.GenericController;
using Contensive.Processor.Addons.AdminSite.Controllers;
using Contensive.Processor.Addons.AdminSite.Models;

namespace Contensive.Processor.Addons.AdminSite {
    /// <summary>
    /// create admin header
    /// </summary>
    public static class AdminContentController {
        //
        //====================================================================================================
        /// <summary>
        /// return the html body for the admin site
        /// </summary>
        /// <param name="cp">interface wrapper for coreController</param>
        /// <returns></returns>
        public static string getAdminContent(CPClass cp) {
            try {
                if (!cp.core.doc.continueProcessing) { return ""; }
                //
                // -- consider
                // -- use route segments to run addons in admin content, ex /admin/invoicemanager should run addon /invoicemanager in the admin content
                // -- /admin = [0] blank, [1] admin
                // -- /admin/ = [0] blank, [1] admin, [2] blank
                // -- /admin/backoffice = [0] blank, [1] admin, [2] backoffice
                string[] pathSegments = cp.Request.PathPage.Split('/');
                if (pathSegments.Length > 2 && !string.IsNullOrEmpty(pathSegments[2])) {
                    if (pathSegments[1].ToLower().Equals(cp.GetAppConfig().adminRoute.ToLower())) {
                        string subroute = cp.Request.Path.Substring(pathSegments[0].Length + 1 + pathSegments[1].Length);
                        return cp.executeRoute(subroute);
                    }
                }
                //
                // turn off chrome protection against submitting html content
                cp.core.webServer.addResponseHeader("X-XSS-Protection", "0");
                //
                // check for member login, if logged in and no admin, lock out, Do CheckMember here because we need to know who is there to create proper blocked menu
                //
                // -- read wl+wr values into wherePair dictionary
                // -- wherepairs are used to:
                // ---- prepopulate inserted records
                // ---- create filters for gridList
                // -- wherepair wr0=id, wl=1 (means id=1)
                Dictionary<string, string> wherePairs = new();
                for (int wpCnt = 0; wpCnt <= 99; wpCnt++) {
                    string key = cp.Doc.GetText("wl" + wpCnt);
                    if (string.IsNullOrEmpty(key)) { break; }
                    wherePairs.Add(key.ToLowerInvariant(), cp.Doc.GetText("wr" + wpCnt));
                }
                //
                // -- read wc (whereclause) into wherepair dictionary also
                // -- whereclause wc=id%3D1 (means id=1)
                string WhereClauseContent = GenericController.encodeText(cp.Doc.GetText("wc"));
                if (!string.IsNullOrEmpty(WhereClauseContent)) {
                    string[] QSSplit = WhereClauseContent.Split(',');
                    for (int QSPointer = 0; QSPointer <= QSSplit.GetUpperBound(0); QSPointer++) {
                        string NameValue = QSSplit[QSPointer];
                        if (!string.IsNullOrEmpty(NameValue)) {
                            if ((NameValue.left(1) == "(") && (NameValue.Substring(NameValue.Length - 1) == ")") && (NameValue.Length > 2)) {
                                NameValue = NameValue.Substring(1, NameValue.Length - 2);
                            }
                            string[] NVSplit = NameValue.Split('=');
                            if (NVSplit.GetUpperBound(0) > 0) {
                                wherePairs.Add(NVSplit[0].ToLowerInvariant(), NVSplit[1]);
                            }
                        }
                    }
                }

                int adminForm = cp.Doc.GetInteger(rnAdminForm);
                var adminData = new AdminDataModel(cp.core, new AdminDataRequest {
                    contentId = cp.Doc.GetInteger("cid"),
                    id = cp.Doc.GetInteger("id"),
                    guid = cp.Doc.GetText("guid"),
                    titleExtension = cp.Doc.GetText(RequestNameTitleExtension),
                    recordTop = cp.Doc.GetInteger("RT"),
                    recordsPerPage = cp.Doc.GetInteger("RS"),
                    wherePairDict = wherePairs,
                    adminAction = cp.Doc.GetInteger(rnAdminAction),
                    adminSourceForm = cp.Doc.GetInteger(rnAdminSourceForm),
                    adminForm = adminForm,
                    adminButton = cp.Doc.GetText(RequestNameButton),
                    fieldEditorPreference = cp.Doc.GetText("fieldEditorPreference")
                });
                cp.core.db.sqlCommandTimeout = 300;
                adminData.buttonObjectCount = 0;
                adminData.contentWatchLoaded = false;
                //
                string buildVersion = cp.core.siteProperties.dataBuildVersion;
                if (versionIsOlder(buildVersion, cp.Version)) {
                    LogController.logWarn(cp.core, new GenericException("Application code (v" + cp.Version + ") is newer than database (v" + buildVersion + "). Upgrade the database with the command line 'cc.exe -a " + cp.core.appConfig.name + " -u'."));
                }
                //
                if (versionIsOlder(cp.Version, buildVersion)) {
                    LogController.logWarn(cp.core, new GenericException("Database upgrade (v" + buildVersion + ") is newer than the Application code (v" + cp.Version + "). Upgrade the website code."));
                }
                //
                // Process SourceForm/Button into Action/Form, and process
                bool CheckUserErrors = true;
                if (adminData.srcFormButton == ButtonCancelAll) {
                    adminData.dstFormId = AdminFormRoot;
                } else {
                    ProcessFormController.processForms(cp, adminData);
                    ProcessActionController.processActions(cp, adminData, cp.core.siteProperties.useContentWatchLink, CheckUserErrors);
                }
                //
                // Normalize values to be needed
                if (adminData.editRecord.id != 0) {
                    var table = DbBaseModel.createByUniqueName<TableModel>(cp, adminData.adminContent.tableName);
                    if (table != null) {
                        WorkflowController.clearEditLock(cp.core, table.id, adminData.editRecord.id);
                    }
                }
                if (adminData.dstFormId < 1) {
                    //
                    // No form was set, use default form
                    if (adminData.adminContent.id <= 0) {
                        adminData.dstFormId = AdminFormRoot;
                    } else {
                        adminData.dstFormId = AdminFormIndex;
                    }
                }
                int addonId = cp.core.docProperties.getInteger("addonid");
                string AddonGuid = cp.core.docProperties.getText("addonguid");
                if (adminData.dstFormId == AdminFormLegacyAddonManager) {
                    //
                    // patch out any old links to the legacy addon manager
                    adminData.dstFormId = 0;
                    AddonGuid = addonGuidAddonManager;
                }
                //
                //-------------------------------------------------------------------------------
                // Edit form but not valid record case
                // Put this here so we can display the error without being stuck displaying the edit form
                // Putting the error on the edit form is confusing because there are fields to fill in
                //-------------------------------------------------------------------------------
                //
                if (adminData.srcFormId == AdminFormEdit) {
                    if (cp.core.doc.userErrorList.Count.Equals(0) && adminData.allowRedirectToRefer ) {
                        string EditReferer = cp.core.docProperties.getText("EditReferer");
                        string CurrentLink = GenericController.modifyLinkQuery(cp.core.webServer.requestUrl, "editreferer", "", false);
                        CurrentLink = GenericController.toLCase(CurrentLink);
                        //
                        // check if this editreferer includes cid=thisone and id=thisone -- if so, go to index form for this cid
                        //
                        if ((!string.IsNullOrEmpty(EditReferer)) && (EditReferer.ToLowerInvariant() != CurrentLink)) {
                            //
                            // return to the page it came from
                            //
                            return cp.core.webServer.redirect(EditReferer, "Admin Edit page returning to the EditReferer setting");
                        } else {
                            //
                            // return to the index page for this content
                            //
                            adminData.dstFormId = AdminFormIndex;
                        }
                    }
                    if (adminData.blockEditForm) {
                        adminData.dstFormId = AdminFormIndex;
                    }
                }
                int HelpAddonId = cp.core.docProperties.getInteger("helpaddonid");
                int HelpCollectionId = cp.core.docProperties.getInteger("helpcollectionid");
                if (HelpCollectionId == 0) {
                    HelpCollectionId = cp.core.visitProperty.getInteger("RunOnce HelpCollectionID");
                    if (HelpCollectionId != 0) {
                        cp.core.visitProperty.setProperty("RunOnce HelpCollectionID", "");
                    }
                }
                //
                //-------------------------------------------------------------------------------
                // build refresh string
                //-------------------------------------------------------------------------------
                //
                if (adminData.adminContent.id != 0) {
                    cp.core.doc.addRefreshQueryString("cid", GenericController.encodeText(adminData.adminContent.id));
                }
                if (adminData.editRecord.id != 0) {
                    cp.core.doc.addRefreshQueryString("id", GenericController.encodeText(adminData.editRecord.id));
                }
                if (!string.IsNullOrEmpty(adminData.editViewTitleSuffix)) {
                    cp.core.doc.addRefreshQueryString(RequestNameTitleExtension, GenericController.encodeRequestVariable(adminData.editViewTitleSuffix));
                }
                if (adminData.listViewRecordTop != 0) {
                    cp.core.doc.addRefreshQueryString("rt", GenericController.encodeText(adminData.listViewRecordTop));
                }
                if (adminData.listViewRecordsPerPage != Constants.RecordsPerPageDefault) {
                    cp.core.doc.addRefreshQueryString("rs", GenericController.encodeText(adminData.listViewRecordsPerPage));
                }
                if (adminData.dstFormId != 0) {
                    cp.core.doc.addRefreshQueryString(rnAdminForm, GenericController.encodeText(adminData.dstFormId));
                }
                //
                // normalize guid
                //
                if (!string.IsNullOrEmpty(AddonGuid)) {
                    if ((AddonGuid.Length == 38) && (AddonGuid.left(1) == "{") && (AddonGuid.Substring(AddonGuid.Length - 1) == "}")) {
                        //
                        // Good to go
                        //
                    } else if (AddonGuid.Length == 36) {
                        //
                        // might be valid with the brackets, add them
                        //
                        AddonGuid = "{" + AddonGuid + "}";
                    } else if (AddonGuid.Length == 32) {
                        //
                        // might be valid with the brackets and the dashes, add them
                        //
                        AddonGuid = "{" + AddonGuid.left(8) + "-" + AddonGuid.Substring(8, 4) + "-" + AddonGuid.Substring(12, 4) + "-" + AddonGuid.Substring(16, 4) + "-" + AddonGuid.Substring(20) + "}";
                    } else {
                        //
                        // not valid
                        //
                        AddonGuid = "";
                    }
                }
                //
                //-------------------------------------------------------------------------------
                // Create the content
                //-------------------------------------------------------------------------------
                //
                string content = "";
                string AddonName = "";
                if (HelpAddonId != 0) {
                    //
                    // display Addon Help
                    //
                    cp.core.doc.addRefreshQueryString("helpaddonid", HelpAddonId.ToString());
                    content = GetAddonHelp(cp, HelpAddonId, "");
                } else if (HelpCollectionId != 0) {
                    //
                    // display Collection Help
                    //
                    cp.core.doc.addRefreshQueryString("helpcollectionid", HelpCollectionId.ToString());
                    content = GetCollectionHelp(cp, HelpCollectionId, "");
                } else if (adminData.dstFormId != 0) {
                    //
                    // -- formindex requires valid content
                    if ((adminData.adminContent.tableName == null) && (adminData.dstFormId == AdminFormIndex)) {
                        adminData.dstFormId = AdminFormRoot;
                    }
                    //
                    // No content so far, try the forms
                    // todo - convert this to switch
                    switch (adminData.dstFormId) {
                        //
                        case AdminFormIndex: {
                                content = ListView.get(cp, cp.core, adminData);
                                break;
                            }
                        case AdminFormEdit: {
                                content = EditView.get(cp.core, adminData);
                                break;
                            }
                        case AdminFormToolSyncTables: {
                                content = SyncTablesClass.get(cp.core);
                                break;
                            }
                        case AdminFormToolSchema: {
                                content = DbSchemaToolClass.get(cp.core);
                                break;
                            }
                        case AdminFormToolDbIndex: {
                                content = DbIndexToolClass.get(cp.core);
                                break;
                            }
                        case AdminformToolFindAndReplace: {
                                content = FindAndReplaceToolClass.get(cp.core);
                                break;
                            }
                        case AdminformToolCreateGUID: {
                                content = CreateGUIDToolClass.get(cp.core);
                                break;
                            }
                        case AdminformToolIISReset: {
                                content = IISResetToolClass.get(cp.core);
                                break;
                            }
                        case AdminFormToolContentSchema: {
                                content = ContentSchemaToolClass.get(cp.core);
                                break;
                            }
                        case AdminFormToolManualQuery: {
                                content = ManualQueryClass.get(cp);
                                break;
                            }
                        case AdminFormToolDefineContentFieldsFromTable: {
                                content = DefineContentFieldsFromTableClass.get(cp.core);
                                break;
                            }
                        case AdminFormToolCreateContentDefinition: {
                                content = CreateContentDefinitionClass.get(cp.core);
                                break;
                            }
                        case AdminFormToolConfigureEdit: {
                                content = ConfigureEditClass.get(cp);
                                break;
                            }
                        case AdminFormToolConfigureListing: {
                                content = ConfigureListClass.get(cp.core);
                                break;
                            }
                        case AdminFormClearCache: {
                                content = cp.core.addon.execute("{7B5B8150-62BE-40F4-A66A-7CC74D99BA76}", new CPUtilsBaseClass.addonExecuteContext {
                                    addonType = CPUtilsBaseClass.addonContext.ContextAdmin
                                });
                                break;
                            }
                        case AdminFormResourceLibrary: {
                                content = cp.core.html.getResourceLibrary("", false, "", "", true);
                                break;
                            }
                        case AdminFormQuickStats: {
                                content = QuickStatsView.get(cp.core);
                                break;
                            }
                        case AdminFormClose: {
                                content = "<Script Language=\"JavaScript\" type=\"text/javascript\"> window.close(); </Script>";
                                break;
                            }
                        case AdminFormContentChildTool: {
                                content = ContentChildToolClass.get(cp);
                                break;
                            }
                        case AdminformHousekeepingControl: {
                                content = HouseKeepingControlClass.get(cp);
                                break;
                            }
                        case AdminFormDownloads: {
                                content = (ToolDownloads.get(cp.core));
                                break;
                            }
                        case AdminFormImportWizard: {
                                content = cp.core.addon.execute(addonGuidImportWizard, new BaseClasses.CPUtilsBaseClass.addonExecuteContext {
                                    addonType = BaseClasses.CPUtilsBaseClass.addonContext.ContextAdmin,
                                    errorContextMessage = "get Import Wizard for Admin"
                                });
                                break;
                            }
                        case AdminFormCustomReports: {
                                content = ToolCustomReports.get(cp.core);
                                break;
                            }
                        case AdminFormFormWizard: {
                                content = cp.core.addon.execute(addonGuidFormWizard, new BaseClasses.CPUtilsBaseClass.addonExecuteContext {
                                    addonType = BaseClasses.CPUtilsBaseClass.addonContext.ContextAdmin,
                                    errorContextMessage = "get Form Wizard for Admin"
                                });
                                break;
                            }
                        case AdminFormLegacyAddonManager: {
                                content = AddonController.getAddonManager(cp.core);
                                break;
                            }
                        case AdminFormEditorConfig: {
                                content = EditorConfigView.get(cp.core);
                                break;
                            }
                        default: {
                                content = "<p>The form requested is not supported</p>";
                                break;
                            }
                    }
                } else if ((addonId != 0) || (!string.IsNullOrEmpty(AddonGuid)) || (!string.IsNullOrEmpty(AddonName))) {
                    //
                    // execute an addon
                    //
                    if ((AddonGuid == addonGuidAddonManager) || (AddonName.ToLowerInvariant() == "add-on manager") || (AddonName.ToLowerInvariant() == "addon manager")) {
                        //
                        // Special case, call the routine that provides a backup
                        //
                        cp.core.doc.addRefreshQueryString("addonguid", addonGuidAddonManager);
                        content = AddonController.getAddonManager(cp.core);
                    } else {
                        AddonModel addon = null;
                        string executeContextErrorCaption = "unknown";
                        if (addonId != 0) {
                            executeContextErrorCaption = " addon id:" + addonId + " for Admin";
                            cp.core.doc.addRefreshQueryString("addonid", addonId.ToString());
                            addon = DbBaseModel.create<AddonModel>(cp, addonId);
                        } else if (!string.IsNullOrEmpty(AddonGuid)) {
                            executeContextErrorCaption = "addon guid:" + AddonGuid + " for Admin";
                            cp.core.doc.addRefreshQueryString("addonguid", AddonGuid);
                            addon = DbBaseModel.create<AddonModel>(cp, AddonGuid);
                        } else if (!string.IsNullOrEmpty(AddonName)) {
                            executeContextErrorCaption = "addon name:" + AddonName + " for Admin";
                            cp.core.doc.addRefreshQueryString("addonname", AddonName);
                            addon = AddonModel.createByUniqueName(cp, AddonName);
                        }
                        if (addon != null) {
                            addonId = addon.id;
                            AddonName = addon.name;
                            cp.core.doc.addRefreshQueryString(RequestNameRunAddon, addonId.ToString());
                        }
                        //
                        // -- add to admin recents
                        AdminRecentModel.insertAdminRecentAddon(cp, cp.User.Id, addon.name, "/" + cp.GetAppConfig().adminRoute + "?addonid=" + addon.id);
                        //
                        // -- execute
                        string InstanceOptionString = cp.core.userProperty.getText("Addon [" + AddonName + "] Options", "");
                        int DefaultWrapperId = -1;
                        content = cp.core.addon.execute(addon, new BaseClasses.CPUtilsBaseClass.addonExecuteContext {
                            addonType = Contensive.BaseClasses.CPUtilsBaseClass.addonContext.ContextAdmin,
                            instanceGuid = adminSiteInstanceId,
                            argumentKeyValuePairs = GenericController.convertQSNVAArgumentstoDocPropertiesList(cp.core, InstanceOptionString),
                            wrapperID = DefaultWrapperId,
                            errorContextMessage = executeContextErrorCaption
                        });
                        if (string.IsNullOrEmpty(content)) {
                            //
                            // empty returned, display desktop
                            content = RootView.getForm_Root(cp.core);
                        }

                    }
                } else {
                    //
                    // nothing so far, display desktop
                    content = RootView.getForm_Root(cp.core);
                }
                //
                // -- add user errors
                if (!cp.core.doc.userErrorList.Count.Equals(0)) {
                    content = ErrorController.getUserError(cp.core) + content;
                }
                //
                // -- add button count to js
                // todo should be added to hidden input
                cp.core.html.addScriptCode("ButtonObjectCount = " + adminData.buttonObjectCount + ";", "Admin Site");
                //
                // -- create the body html
                return content;
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        //
        private static string GetAddonHelp(CPClass cp, int HelpAddonID, string UsedIDString) {
            string addonHelp = "";
            try {
                if (GenericController.strInstr(1, "," + UsedIDString + ",", "," + HelpAddonID + ",") == 0) {
                    string AddonName = "";
                    string AddonHelpCopy = "";
                    DateTime AddonDateAdded = default;
                    DateTime AddonLastUpdated = default;
                    bool FoundAddon = false;
                    string helpLink = "";
                    string IconImg = "";
                    using (var csData = new CsModel(cp.core)) {
                        csData.openRecord(AddonModel.tableMetadata.contentName, HelpAddonID);
                        if (csData.ok()) {
                            FoundAddon = true;
                            AddonName = csData.getText("Name");
                            AddonHelpCopy = csData.getText("help");
                            AddonDateAdded = csData.getDate("dateadded");
                            AddonLastUpdated = csData.getDate("lastupdated");
                            if (AddonLastUpdated == DateTime.MinValue) {
                                AddonLastUpdated = AddonDateAdded;
                            }
                            string IconFilename = csData.getText("Iconfilename");
                            int IconWidth = csData.getInteger("IconWidth");
                            int IconHeight = csData.getInteger("IconHeight");
                            int IconSprites = csData.getInteger("IconSprites");
                            bool IconIsInline = csData.getBoolean("IsInline");
                            IconImg = AddonController.getAddonIconImg("/" + cp.core.appConfig.adminRoute, IconWidth, IconHeight, IconSprites, IconIsInline, "", IconFilename, cp.core.appConfig.cdnFileUrl, AddonName, AddonName, "", 0);
                            helpLink = csData.getText("helpLink");
                        }
                    }
                    //
                    if (FoundAddon) {
                        //
                        // Included Addons
                        //
                        var IncludeHelp = new StringBuilder();
                        foreach (var addonon in cp.core.addonCache.getDependsOnList(HelpAddonID)) {
                            IncludeHelp.Append(GetAddonHelp(cp, addonon.id, HelpAddonID + "," + addonon.id.ToString()));
                        }
                        if (!string.IsNullOrEmpty(helpLink)) {
                            if (!string.IsNullOrEmpty(AddonHelpCopy)) {
                                AddonHelpCopy = AddonHelpCopy + "<p>For additional help with this add-on, please visit <a href=\"" + helpLink + "\">" + helpLink + "</a>.</p>";
                            } else {
                                AddonHelpCopy = AddonHelpCopy + "<p>For help with this add-on, please visit <a href=\"" + helpLink + "\">" + helpLink + "</a>.</p>";
                            }
                        }
                        if (string.IsNullOrEmpty(AddonHelpCopy)) {
                            AddonHelpCopy += "<p>Please refer to the help resources available for this collection. More information may also be available in the Contensive online Learning Center <a href=\"http://support.contensive.com/Learning-Center\">http://support.contensive.com/Learning-Center</a> or contact Contensive Support support@contensive.com for more information.</p>";
                        }
                        addonHelp = ""
                            + "<div class=\"ccHelpCon\">"
                            + "<div class=\"title\"><div style=\"float:right;\"><a href=\"?addonid=" + HelpAddonID + "\">" + IconImg + "</a></div>" + AddonName + " Add-on</div>"
                            + "<div class=\"byline\">"
                                + "<div>Installed " + AddonDateAdded + "</div>"
                                + "<div>Last Updated " + AddonLastUpdated + "</div>"
                            + "</div>"
                            + "<div class=\"body\" style=\"clear:both;\">" + AddonHelpCopy + "</div>"
                            + "</div>";
                        addonHelp += IncludeHelp;
                    }
                }
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
                throw;
            }
            return addonHelp;
        }
        //
        //====================================================================================================
        //
        private static string GetCollectionHelp(CPClass cp, int helpCollectionID, string usedIDString) {
            try {
                string Collectionname = "";
                string collectionHelpCopy = "";
                string CollectionHelpLink = "";
                DateTime CollectionDateAdded = default;
                DateTime CollectionLastUpdated = default;
                var includeHelp = new StringBuilder();
                //
                if (GenericController.strInstr(1, "," + usedIDString + ",", "," + helpCollectionID + ",") == 0) {
                    return "";
                }
                using (var csData = new CsModel(cp.core)) {
                    csData.openRecord("Add-on Collections", helpCollectionID);
                    if (csData.ok()) {
                        Collectionname = csData.getText("Name");
                        collectionHelpCopy = csData.getText("help");
                        CollectionDateAdded = csData.getDate("dateadded");
                        CollectionLastUpdated = csData.getDate("lastupdated");
                        CollectionHelpLink = csData.getText("helplink");
                        if (CollectionLastUpdated == DateTime.MinValue) {
                            CollectionLastUpdated = CollectionDateAdded;
                        }
                    }
                }
                //
                // Add-ons
                //
                using (var csData = new CsModel(cp.core)) {
                    csData.open(AddonModel.tableMetadata.contentName, "CollectionID=" + helpCollectionID, "name");
                    while (csData.ok()) {
                        includeHelp.Append("<div style=\"clear:both;\">" + GetAddonHelp(cp, csData.getInteger("ID"), "") + "</div>");
                        csData.goNext();
                    }
                }
                //
                if ((string.IsNullOrEmpty(CollectionHelpLink)) && (string.IsNullOrEmpty(collectionHelpCopy))) {
                    collectionHelpCopy = "<p>No help information could be found for this collection. Please use the online resources at <a href=\"http://support.contensive.com/Learning-Center\">http://support.contensive.com/Learning-Center</a> or contact Contensive Support support@contensive.com by email.</p>";
                } else if (!string.IsNullOrEmpty(CollectionHelpLink)) {
                    collectionHelpCopy = ""
                        + "<p>For information about this collection please visit <a href=\"" + CollectionHelpLink + "\">" + CollectionHelpLink + "</a>.</p>"
                        + collectionHelpCopy;
                }
                //
                string returnHelp = ""
                    + "<div class=\"ccHelpCon\">"
                    + "<div class=\"title\">" + Collectionname + " Collection</div>"
                    + "<div class=\"byline\">"
                        + "<div>Installed " + CollectionDateAdded + "</div>"
                        + "<div>Last Updated " + CollectionLastUpdated + "</div>"
                    + "</div>"
                    + "<div class=\"body\">" + collectionHelpCopy + "</div>"
                    + ((includeHelp.Length > 0) ? includeHelp.ToString() : "")
                    + "</div>";
                return returnHelp;
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
                throw;
            }
        }
        //
        //==============================================================================================
        /// <summary>
        /// If this field has no help message, check the field with the same name from it's inherited parent
        /// </summary>
        /// <param name="ContentID"></param>
        /// <param name="FieldName"></param>
        /// <param name="return_Default"></param>
        /// <param name="return_Custom"></param>
        private static void getFieldHelpMsgs(CPClass cp, int ContentID, string FieldName, ref string return_Default, ref string return_Custom) {
            try {
                bool Found = false;
                using (var csData = new CsModel(cp.core)) {
                    string SQL = "select h.HelpDefault,h.HelpCustom from ccfieldhelp h left join ccfields f on f.id=h.fieldid where f.contentid=" + ContentID + " and f.name=" + DbController.encodeSQLText(FieldName);
                    csData.openSql(SQL);
                    if (csData.ok()) {
                        Found = true;
                        return_Default = csData.getText("helpDefault");
                        return_Custom = csData.getText("helpCustom");
                    }
                }
                //
                if (!Found) {
                    int ParentId = 0;
                    using (var csData = new CsModel(cp.core)) {
                        string SQL = "select parentid from cccontent where id=" + ContentID;
                        csData.openSql(SQL);
                        if (csData.ok()) {
                            ParentId = csData.getInteger("parentid");
                        }
                    }
                    if (ParentId != 0) {
                        getFieldHelpMsgs(cp, ParentId, FieldName, ref return_Default, ref return_Custom);
                    }
                }
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
                throw;
            }
        }
        //
        //=================================================================================
        /// <summary>
        /// Save the index config
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="core"></param>
        /// <param name="IndexConfig"></param>
        public static void setIndexSQL_SaveIndexConfig(CPClass cp, CoreController core, IndexConfigClass IndexConfig) {
            //
            // --Find words
            string SubList = "";
            foreach (var kvp in IndexConfig.findWords) {
                IndexConfigFindWordClass findWord = kvp.Value;
                if ((!string.IsNullOrEmpty(findWord.Name)) && (findWord.MatchOption != FindWordMatchEnum.MatchIgnore)) {
                    SubList += Environment.NewLine + findWord.Name + "\t" + findWord.Value + "\t" + (int)findWord.MatchOption;
                }
            }
            string FilterText = "";
            if (!string.IsNullOrEmpty(SubList)) {
                FilterText += Environment.NewLine + "FindWordList" + SubList + Environment.NewLine;
            }
            //
            // --CDef List
            if (IndexConfig.subCDefID > 0) {
                FilterText += Environment.NewLine + "CDefList\r\n" + IndexConfig.subCDefID + Environment.NewLine;
            }
            //
            // -- Group List
            SubList = "";
            if (IndexConfig.groupListCnt > 0) {
                //
                for (int ptr = 0; ptr < IndexConfig.groupListCnt; ptr++) {
                    if (!string.IsNullOrEmpty(IndexConfig.groupList[ptr])) {
                        SubList += Environment.NewLine + IndexConfig.groupList[ptr];
                    }
                }
            }
            if (!string.IsNullOrEmpty(SubList)) {
                FilterText += Environment.NewLine + "GroupList" + SubList + Environment.NewLine;
            }
            //
            // PageNumber and Records Per Page
            FilterText += Environment.NewLine
                + Environment.NewLine + "pagenumber"
                + Environment.NewLine + IndexConfig.pageNumber;
            FilterText += Environment.NewLine
                + Environment.NewLine + "recordsperpage"
                + Environment.NewLine + IndexConfig.recordsPerPage;
            //
            // misc filters
            if (IndexConfig.activeOnly) {
                FilterText += Environment.NewLine
                    + Environment.NewLine + "IndexFilterActiveOnly";
            }
            if (IndexConfig.lastEditedByMe) {
                FilterText += Environment.NewLine
                    + Environment.NewLine + "IndexFilterLastEditedByMe";
            }
            if (IndexConfig.lastEditedToday) {
                FilterText += Environment.NewLine
                    + Environment.NewLine + "IndexFilterLastEditedToday";
            }
            if (IndexConfig.lastEditedPast7Days) {
                FilterText += Environment.NewLine
                    + Environment.NewLine + "IndexFilterLastEditedPast7Days";
            }
            if (IndexConfig.lastEditedPast30Days) {
                FilterText += Environment.NewLine
                    + Environment.NewLine + "IndexFilterLastEditedPast30Days";
            }
            if (IndexConfig.open) {
                FilterText += Environment.NewLine
                    + Environment.NewLine + "IndexFilterOpen";
            }
            //
            cp.core.visitProperty.setProperty(AdminDataModel.IndexConfigPrefix + encodeText(IndexConfig.contentID), FilterText);
            //
            //   Member Properties (persistant)
            //
            // Save Admin Column
            SubList = "";
            foreach (var column in IndexConfig.columns) {
                if (!string.IsNullOrEmpty(column.Name)) {
                    SubList += Environment.NewLine + column.Name + "\t" + column.Width;
                }
            }
            FilterText = "";
            if (!string.IsNullOrEmpty(SubList)) {
                FilterText += Environment.NewLine + "Columns" + SubList + Environment.NewLine;
            }
            //
            // Sorts
            //
            SubList = "";
            foreach (var kvp in IndexConfig.sorts) {
                IndexConfigSortClass sort = kvp.Value;
                if (!string.IsNullOrEmpty(sort.fieldName)) {
                    SubList += Environment.NewLine + sort.fieldName + "\t" + sort.direction;
                }
            }
            if (!string.IsNullOrEmpty(SubList)) {
                FilterText += Environment.NewLine + "Sorts" + SubList + Environment.NewLine;
            }
            cp.core.userProperty.setProperty(AdminDataModel.IndexConfigPrefix + encodeText(IndexConfig.contentID), FilterText);
        }
    }
}
