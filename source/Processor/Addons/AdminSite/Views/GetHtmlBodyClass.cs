﻿
using System;
using System.Collections.Generic;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Controllers.GenericController;
using static Contensive.Processor.Constants;
using Contensive.Processor.Models.Domain;
using static Contensive.Processor.Addons.AdminSite.Controllers.AdminUIController;
using Contensive.Processor.Exceptions;
using Contensive.Processor.Addons.AdminSite.Controllers;
using Contensive.BaseClasses;
using Contensive.Models.Db;
using System.Globalization;
using Contensive.Processor.Addons.AdminSite.Models;
using Contensive.Processor.Addons.Tools;

namespace Contensive.Processor.Addons.AdminSite {
    public class GetHtmlBodyClass : Contensive.BaseClasses.AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// addon method, deliver complete Html admin site
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cpBase) {
            string result = "";
            CPClass cp = (CPClass)cpBase;
            try {
                //
                // todo - convert admin addon to use cpbase to help understand cp api requirements
                //
                if (!cp.core.session.isAuthenticated) {
                    //
                    // --- must be authenticated to continue. Force a local login
                    result = cp.core.addon.execute(addonGuidLoginPage, new BaseClasses.CPUtilsBaseClass.addonExecuteContext {
                        errorContextMessage = "get Login Page for Html Body",
                        addonType = BaseClasses.CPUtilsBaseClass.addonContext.ContextPage
                    });
                } else if (!cp.core.session.isAuthenticatedContentManager()) {
                    //
                    // --- member must have proper access to continue
                    result = ""
                        + "<p>You are attempting to enter an area which your account does not have access.</p>"
                        + "<ul class=\"ccList\">"
                        + "<li class=\"ccListItem\">To return to the public web site, use your back button, or <a href=\"" + "/" + "\">Click Here</A>."
                        + "<li class=\"ccListItem\">To login under a different account, <a href=\"/" + cp.core.appConfig.adminRoute + "?method=logout\" rel=\"nofollow\">Click Here</A>"
                        + "<li class=\"ccListItem\">To have your account access changed to include this area, please contact the <a href=\"mailto:" + cp.core.siteProperties.getText("EmailAdmin") + "\">system administrator</A>. "
                        + "\r</ul>"
                        + "";
                    result = ""
                        + "<div style=\"display:table;padding:100px 0 0 0;margin:0 auto;\">"
                        + cp.core.html.getPanelHeader("Unauthorized Access")
                        + cp.core.html.getPanel(result, "ccPanel", "ccPanelHilite", "ccPanelShadow", "400", 15)
                        + "</div>";
                    cp.core.doc.setMetaContent(0, 0);
                    cp.core.html.addTitle("Unauthorized Access", "adminSite");
                    result = HtmlController.div(result, "container-fluid ccBodyAdmin ccCon");
                } else {
                    //
                    // get admin content
                    result = getHtmlBody(cp);
                    result = HtmlController.div(result, "container-fluid ccBodyAdmin ccCon");
                }
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// return the html body for the admin site
        /// </summary>
        /// <param name="forceAdminContent"></param>
        /// <returns></returns>
        private string getHtmlBody(CPClass cp, string forceAdminContent = "") {
            string result = "";
            try {
                // todo convert to jquery bind
                cp.core.doc.setMetaContent(0, 0);
                //
                // turn off chrome protection against submitting html content
                cp.core.webServer.addResponseHeader("X-XSS-Protection", "0");
                //
                // check for member login, if logged in and no admin, lock out, Do CheckMember here because we need to know who is there to create proper blocked menu
                if (cp.core.doc.continueProcessing) {
                    var adminData = new AdminDataModel(cp.core);
                    cp.core.db.sqlCommandTimeout = 300;
                    adminData.buttonObjectCount = 0;
                    adminData.javaScriptString = "";
                    adminData.contentWatchLoaded = false;
                    //
                    if (string.Compare(cp.core.siteProperties.dataBuildVersion, cp.Version) < 0) {
                        LogController.logWarn(cp.core, new GenericException("Application code (v" + cp.Version + ") is newer than database (v" + cp.core.siteProperties.dataBuildVersion + "). Upgrade the database with the command line 'cc.exe -a " + cp.core.appConfig.name + " -u'."));
                    }
                    //
                    // Process SourceForm/Button into Action/Form, and process
                    if (adminData.requestButton == ButtonCancelAll) {
                        adminData.adminForm = AdminFormRoot;
                    } else {
                        ProcessForms(cp, adminData);
                        ProcessActions(cp, adminData, cp.core.siteProperties.useContentWatchLink);
                    }
                    //
                    // Normalize values to be needed
                    if (adminData.editRecord.id != 0) {
                        var table = DbBaseModel.createByUniqueName<TableModel>(cp, adminData.adminContent.tableName);
                        if (table != null) {
                            WorkflowController.clearEditLock(cp.core, table.id, adminData.editRecord.id);
                        }
                    }
                    if (adminData.adminForm < 1) {
                        //
                        // No form was set, use default form
                        if (adminData.adminContent.id <= 0) {
                            adminData.adminForm = AdminFormRoot;
                        } else {
                            adminData.adminForm = AdminFormIndex;
                        }
                    }
                    int addonId = cp.core.docProperties.getInteger("addonid");
                    string AddonGuid = cp.core.docProperties.getText("addonguid");
                    if (adminData.adminForm == AdminFormLegacyAddonManager) {
                        //
                        // patch out any old links to the legacy addon manager
                        adminData.adminForm = 0;
                        AddonGuid = addonGuidAddonManager;
                    }
                    //
                    //-------------------------------------------------------------------------------
                    // Edit form but not valid record case
                    // Put this here so we can display the error without being stuck displaying the edit form
                    // Putting the error on the edit form is confusing because there are fields to fill in
                    //-------------------------------------------------------------------------------
                    //
                    if (adminData.adminSourceForm == AdminFormEdit) {
                        if (cp.core.doc.userErrorList.Count.Equals(0) && (adminData.requestButton.Equals(ButtonOK) || adminData.requestButton.Equals(ButtonCancel) || adminData.requestButton.Equals(ButtonDelete))) {
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
                                adminData.adminForm = AdminFormIndex;
                            }
                        }
                        if (adminData.blockEditForm) {
                            adminData.adminForm = AdminFormIndex;
                        }
                    }
                    int HelpLevel = cp.core.docProperties.getInteger("helplevel");
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
                    if (adminData.titleExtension != "") {
                        cp.core.doc.addRefreshQueryString(RequestNameTitleExtension, GenericController.encodeRequestVariable(adminData.titleExtension));
                    }
                    if (adminData.recordTop != 0) {
                        cp.core.doc.addRefreshQueryString("rt", GenericController.encodeText(adminData.recordTop));
                    }
                    if (adminData.recordsPerPage != Constants.RecordsPerPageDefault) {
                        cp.core.doc.addRefreshQueryString("rs", GenericController.encodeText(adminData.recordsPerPage));
                    }
                    if (adminData.adminForm != 0) {
                        cp.core.doc.addRefreshQueryString(rnAdminForm, GenericController.encodeText(adminData.adminForm));
                    }
                    if (adminData.ignore_legacyMenuDepth != 0) {
                        cp.core.doc.addRefreshQueryString(RequestNameAdminDepth, GenericController.encodeText(adminData.ignore_legacyMenuDepth));
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
                    string adminBody = "";
                    StringBuilderLegacyController Stream = new StringBuilderLegacyController();
                    string AddonName = "";
                    if (!string.IsNullOrEmpty(forceAdminContent)) {
                        //
                        // Use content passed in as an argument
                        //
                        adminBody = forceAdminContent;
                    } else if (HelpAddonId != 0) {
                        //
                        // display Addon Help
                        //
                        cp.core.doc.addRefreshQueryString("helpaddonid", HelpAddonId.ToString());
                        adminBody = GetAddonHelp(cp, HelpAddonId, "");
                    } else if (HelpCollectionId != 0) {
                        //
                        // display Collection Help
                        //
                        cp.core.doc.addRefreshQueryString("helpcollectionid", HelpCollectionId.ToString());
                        adminBody = GetCollectionHelp(cp, HelpCollectionId, "");
                    } else if (adminData.adminForm != 0) {
                        //
                        // -- formindex requires valkid content
                        if ((adminData.adminContent.tableName == null) && ((adminData.adminForm == AdminFormIndex) || (adminData.adminForm == AdminFormIndex))) { adminData.adminForm = AdminFormRoot; }
                        //
                        // No content so far, try the forms
                        // todo - convert this to switch
                        switch (adminData.adminForm) {
                            //
                            case AdminFormIndex: {
                                    adminBody = FormIndex.get(cp, cp.core, adminData, (adminData.adminContent.tableName.ToLowerInvariant() == "ccemail"));
                                    break;
                                }
                            case AdminFormEdit: {
                                    adminBody = FormEdit.get(cp.core, adminData);
                                    break;
                                }
                            case AdminFormToolSyncTables: {
                                    adminBody = SyncTablesClass.get(cp.core);
                                    break;
                                }
                            case AdminFormToolSchema: {
                                    adminBody = DbSchemaToolClass.get(cp.core);
                                    break;
                                }
                            case AdminFormToolDbIndex: {
                                    adminBody = DbIndexToolClass.get( cp.core );
                                    break;
                                }
                            case AdminformToolFindAndReplace: {
                                    adminBody = FindAndReplaceToolClass.get( cp.core) ;
                                    break;
                                }
                            case AdminformToolCreateGUID: {
                                    adminBody = CreateGUIDToolClass.get( cp.core );
                                    break;
                                }
                            case AdminformToolIISReset: {
                                    adminBody = IISResetToolClass.get(cp.core);
                                    break;
                                }
                            case AdminFormToolContentSchema: {
                                    adminBody = ContentSchemaToolClass.get(cp.core);
                                    break;
                                }
                            case AdminFormToolManualQuery: {
                                    adminBody = ManualQueryClass.get(cp);
                                    break;
                                }
                            case AdminFormToolDefineContentFieldsFromTable: {
                                    adminBody = DefineContentFieldsFromTableClass.get(cp.core);
                                    break;
                                }
                            case AdminFormToolCreateContentDefinition: {
                                    adminBody = CreateContentDefinitionClass.get(cp.core);
                                    break;
                                }
                            case AdminFormToolConfigureEdit: {
                                    adminBody = ConfigureEditClass.get(cp);
                                    break;
                                }
                            case AdminFormToolConfigureListing: {
                                    adminBody = ConfigureListClass.get(cp.core);
                                    break;
                                }
                            case AdminFormClearCache: {
                                    adminBody = cp.core.addon.execute("{7B5B8150-62BE-40F4-A66A-7CC74D99BA76}",new CPUtilsBaseClass.addonExecuteContext() { 
                                         addonType = CPUtilsBaseClass.addonContext.ContextAdmin
                                    });
                                    break;
                                }
                            case AdminFormResourceLibrary: {
                                    adminBody = cp.core.html.getResourceLibrary("", false, "", "", true);
                                    break;
                                }
                            case AdminFormQuickStats: {
                                    adminBody = FormQuickStats.get(cp.core);
                                    break;
                                }
                            case AdminFormClose: {
                                    Stream.add("<Script Language=\"JavaScript\" type=\"text/javascript\"> window.close(); </Script>");
                                    break;
                                }
                            case AdminFormContentChildTool: {
                                    adminBody = ContentChildToolClass.get(cp);
                                    break;
                                }
                            case AdminformHousekeepingControl: {
                                    adminBody = HouseKeepingControlClass.get(cp);
                                    break;
                                }
                            case AdminFormDownloads: {
                                    adminBody = (ToolDownloads.get(cp.core));
                                    break;
                                }
                            case AdminFormImportWizard: {
                                    adminBody = cp.core.addon.execute(addonGuidImportWizard, new BaseClasses.CPUtilsBaseClass.addonExecuteContext {
                                        addonType = BaseClasses.CPUtilsBaseClass.addonContext.ContextAdmin,
                                        errorContextMessage = "get Import Wizard for Admin"
                                    });
                                    break;
                                }
                            case AdminFormCustomReports: {
                                    adminBody = ToolCustomReports.get(cp.core);
                                    break;
                                }
                            case AdminFormFormWizard: {
                                    adminBody = cp.core.addon.execute(addonGuidFormWizard, new BaseClasses.CPUtilsBaseClass.addonExecuteContext {
                                        addonType = BaseClasses.CPUtilsBaseClass.addonContext.ContextAdmin,
                                        errorContextMessage = "get Form Wizard for Admin"
                                    });
                                    break;
                                }
                            case AdminFormLegacyAddonManager: {
                                    adminBody = AddonController.getAddonManager(cp.core);
                                    break;
                                }
                            case AdminFormEditorConfig: {
                                    adminBody = FormEditConfig.get(cp.core);
                                    break;
                                }
                            default: {
                                    adminBody = "<p>The form requested is not supported</p>";
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
                            adminBody = AddonController.getAddonManager(cp.core);
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
                                string AddonHelpCopy = addon.help;
                                cp.core.doc.addRefreshQueryString(RequestNameRunAddon, addonId.ToString());
                            }
                            string InstanceOptionString = cp.core.userProperty.getText("Addon [" + AddonName + "] Options", "");
                            int DefaultWrapperId = -1;
                            adminBody = cp.core.addon.execute(addon, new BaseClasses.CPUtilsBaseClass.addonExecuteContext {
                                addonType = Contensive.BaseClasses.CPUtilsBaseClass.addonContext.ContextAdmin,
                                instanceGuid = adminSiteInstanceId,
                                argumentKeyValuePairs = GenericController.convertQSNVAArgumentstoDocPropertiesList(cp.core, InstanceOptionString),
                                wrapperID = DefaultWrapperId,
                                errorContextMessage = executeContextErrorCaption
                            });
                            if (string.IsNullOrEmpty(adminBody)) {
                                //
                                // empty returned, display desktop
                                adminBody = FormRoot.getForm_Root(cp.core);
                            }

                        }
                    } else {
                        //
                        // nothing so far, display desktop
                        adminBody = FormRoot.getForm_Root(cp.core);
                    }
                    //
                    // include fancybox if it was needed
                    if (adminData.includeFancyBox) {
                        cp.core.addon.executeDependency(DbBaseModel.create<AddonModel>(cp, addonGuidjQueryFancyBox), new BaseClasses.CPUtilsBaseClass.addonExecuteContext {
                            addonType = BaseClasses.CPUtilsBaseClass.addonContext.ContextAdmin,
                            errorContextMessage = "adding fancybox dependency in Admin"
                        });
                        cp.core.html.addScriptCode_onLoad(adminData.fancyBoxHeadJS, "");
                    }
                    //
                    // add user errors
                    if (!cp.core.doc.userErrorList.Count.Equals(0)) {
                        adminBody = HtmlController.div(Processor.Controllers.ErrorController.getUserError(cp.core), "ccAdminMsg") + adminBody;
                    }
                    Stream.add(getAdminHeader(cp, adminData));
                    Stream.add(adminBody);
                    Stream.add(adminData.adminFooter);
                    adminData.javaScriptString += "ButtonObjectCount = " + adminData.buttonObjectCount + ";";
                    cp.core.html.addScriptCode(adminData.javaScriptString, "Admin Site");
                    result = Stream.text;
                }
                if (cp.core.session.user.developer) {
                    result = Processor.Controllers.ErrorController.getDocExceptionHtmlList(cp.core) + result;
                }
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
            }
            return result;
        }
        //
        //====================================================================================================
        //
        private string GetAddonHelp(CPClass cp, int HelpAddonID, string UsedIDString) {
            string addonHelp = "";
            try {
                string IconFilename = null;
                int IconWidth = 0;
                int IconHeight = 0;
                int IconSprites = 0;
                bool IconIsInline = false;
                string AddonName = "";
                string AddonHelpCopy = "";
                DateTime AddonDateAdded = default(DateTime);
                DateTime AddonLastUpdated = default(DateTime);
                string IncludeHelp = "";
                string IconImg = "";
                string helpLink = "";
                bool FoundAddon = false;
                //
                if (GenericController.strInstr(1, "," + UsedIDString + ",", "," + HelpAddonID + ",") == 0) {
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
                            IconFilename = csData.getText("Iconfilename");
                            IconWidth = csData.getInteger("IconWidth");
                            IconHeight = csData.getInteger("IconHeight");
                            IconSprites = csData.getInteger("IconSprites");
                            IconIsInline = csData.getBoolean("IsInline");
                            IconImg = AddonController.getAddonIconImg("/" + cp.core.appConfig.adminRoute, IconWidth, IconHeight, IconSprites, IconIsInline, "", IconFilename, cp.core.appConfig.cdnFileUrl, AddonName, AddonName, "", 0);
                            helpLink = csData.getText("helpLink");
                        }
                    }
                    //
                    if (FoundAddon) {
                        //
                        // Included Addons
                        //
                        foreach (var addonon in cp.core.addonCache.getDependsOnList(HelpAddonID)) {
                            IncludeHelp += GetAddonHelp(cp, addonon.id, HelpAddonID + "," + addonon.id.ToString());
                        }
                        if (!string.IsNullOrEmpty(helpLink)) {
                            if (!string.IsNullOrEmpty(AddonHelpCopy)) {
                                AddonHelpCopy = AddonHelpCopy + "<p>For additional help with this add-on, please visit <a href=\"" + helpLink + "\">" + helpLink + "</a>.</p>";
                            } else {
                                AddonHelpCopy = AddonHelpCopy + "<p>For help with this add-on, please visit <a href=\"" + helpLink + "\">" + helpLink + "</a>.</p>";
                            }
                        }
                        if (string.IsNullOrEmpty(AddonHelpCopy)) {
                            AddonHelpCopy = AddonHelpCopy + "<p>Please refer to the help resources available for this collection. More information may also be available in the Contensive online Learning Center <a href=\"http://support.contensive.com/Learning-Center\">http://support.contensive.com/Learning-Center</a> or contact Contensive Support support@contensive.com for more information.</p>";
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
                        addonHelp = addonHelp + IncludeHelp;
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
        private string GetCollectionHelp(CPClass cp, int HelpCollectionID, string UsedIDString) {
            string returnHelp = "";
            try {
                string Collectionname = "";
                string CollectionHelpCopy = "";
                string CollectionHelpLink = "";
                DateTime CollectionDateAdded = default(DateTime);
                DateTime CollectionLastUpdated = default(DateTime);
                string IncludeHelp = "";
                //
                if (GenericController.strInstr(1, "," + UsedIDString + ",", "," + HelpCollectionID + ",") == 0) {
                    using (var csData = new CsModel(cp.core)) {
                        csData.openRecord("Add-on Collections", HelpCollectionID);
                        if (csData.ok()) {
                            Collectionname = csData.getText("Name");
                            CollectionHelpCopy = csData.getText("help");
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
                        csData.open(AddonModel.tableMetadata.contentName, "CollectionID=" + HelpCollectionID, "name");
                        while (csData.ok()) {
                            IncludeHelp = IncludeHelp + "<div style=\"clear:both;\">" + GetAddonHelp(cp, csData.getInteger("ID"), "") + "</div>";
                            csData.goNext();
                        }
                    }
                    //
                    if ((string.IsNullOrEmpty(CollectionHelpLink)) && (string.IsNullOrEmpty(CollectionHelpCopy))) {
                        CollectionHelpCopy = "<p>No help information could be found for this collection. Please use the online resources at <a href=\"http://support.contensive.com/Learning-Center\">http://support.contensive.com/Learning-Center</a> or contact Contensive Support support@contensive.com by email.</p>";
                    } else if (!string.IsNullOrEmpty(CollectionHelpLink)) {
                        CollectionHelpCopy = ""
                            + "<p>For information about this collection please visit <a href=\"" + CollectionHelpLink + "\">" + CollectionHelpLink + "</a>.</p>"
                            + CollectionHelpCopy;
                    }
                    //
                    returnHelp = ""
                        + "<div class=\"ccHelpCon\">"
                        + "<div class=\"title\">" + Collectionname + " Collection</div>"
                        + "<div class=\"byline\">"
                            + "<div>Installed " + CollectionDateAdded + "</div>"
                            + "<div>Last Updated " + CollectionLastUpdated + "</div>"
                        + "</div>"
                        + "<div class=\"body\">" + CollectionHelpCopy + "</div>";
                    if (!string.IsNullOrEmpty(IncludeHelp)) {
                        returnHelp = returnHelp + IncludeHelp;
                    }
                    returnHelp = returnHelp + "</div>";
                }
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
                throw;
            }
            return returnHelp;
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
        private void getFieldHelpMsgs(CPClass cp, int ContentID, string FieldName, ref string return_Default, ref string return_Custom) {
            try {
                //
                string SQL = null;
                bool Found = false;
                int ParentId = 0;
                //
                Found = false;
                using (var csData = new CsModel(cp.core)) {
                    SQL = "select h.HelpDefault,h.HelpCustom from ccfieldhelp h left join ccfields f on f.id=h.fieldid where f.contentid=" + ContentID + " and f.name=" + DbController.encodeSQLText(FieldName);
                    csData.openSql(SQL);
                    if (csData.ok()) {
                        Found = true;
                        return_Default = csData.getText("helpDefault");
                        return_Custom = csData.getText("helpCustom");
                    }
                }
                //
                if (!Found) {
                    ParentId = 0;
                    using (var csData = new CsModel(cp.core)) {
                        SQL = "select parentid from cccontent where id=" + ContentID;
                        csData.openSql(SQL);
                        if (csData.ok()) {
                            ParentId = csData.getInteger("parentid");
                        }
                    }
                    if (ParentId != 0) {
                        getFieldHelpMsgs(cp, ParentId, FieldName, ref return_Default, ref return_Custom);
                    }
                }
                //
                return;
                //
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
            }
        }
        //     
        //========================================================================
        // ProcessActions
        //   perform the action called from the previous form
        //   when action is complete, replace the action code with one that will refresh
        //
        //   Request Variables
        //       Id = ID of record to edit
        //       adminContextClass.AdminAction = action to be performed, defined below, required except for very first call to edit
        //   adminContextClass.AdminAction Definitions
        //       edit - edit the record defined by ID, If ID="", edit a new record
        //       Save - saves an edit record and returns to the index
        //       Delete - hmmm.
        //       Cancel - returns to index
        //       Change Filex - uploads a file to a FieldTypeFile, x is a number 0...adminContext.content.FieldMax
        //       Delete Filex - clears a file name for a FieldTypeFile, x is a number 0...adminContext.content.FieldMax
        //       Upload - The action that actually uploads the file
        //       Email - (not done) Sends "body" field to "email" field in adminContext.content.id
        //========================================================================
        //
        private void ProcessActions(CPClass cp, AdminDataModel adminData, bool UseContentWatchLink) {
            try {
                int RecordId = 0;
                string ContentName = null;
                int EmailToConfirmationMemberId = 0;
                int RowCnt = 0;
                int RowPtr = 0;
                //
                if (adminData.admin_Action != Constants.AdminActionNop) {
                    if (!adminData.userAllowContentEdit) {
                        //
                        // Action blocked by BlockCurrentRecord
                        //
                    } else {
                        //
                        // Process actions
                        //
                        using (var db = new DbController(cp.core, adminData.adminContent.dataSourceName)) {
                            switch (adminData.admin_Action) {
                                case Constants.AdminActionEditRefresh:
                                //
                                // Load the record as if it will be saved, but skip the save
                                //
                                adminData.loadEditRecord(cp.core);
                                adminData.loadEditRecord_Request(cp.core);
                                break;
                                case Constants.AdminActionMarkReviewed:
                                //
                                // Mark the record reviewed without making any changes
                                //
                                PageContentModel.markReviewed(cp, adminData.editRecord.id);
                                break;
                                case Constants.AdminActionDelete:
                                if (adminData.editRecord.userReadOnly) {
                                    Processor.Controllers.ErrorController.addUserError(cp.core, "Your request was blocked because the record you specified is now locked by another authcontext.user.");
                                } else {
                                    adminData.loadEditRecord(cp.core);
                                    db.delete(adminData.editRecord.id, adminData.adminContent.tableName);
                                    ContentController.processAfterSave(cp.core, true, adminData.editRecord.contentControlId_Name, adminData.editRecord.id, adminData.editRecord.nameLc, adminData.editRecord.parentId, UseContentWatchLink);
                                }
                                adminData.admin_Action = Constants.AdminActionNop;
                                break;
                                case Constants.AdminActionSave:
                                //
                                // ----- Save Record
                                //
                                if (adminData.editRecord.userReadOnly) {
                                    Processor.Controllers.ErrorController.addUserError(cp.core, "Your request was blocked because the record you specified is now locked by another authcontext.user.");
                                } else {
                                    adminData.loadEditRecord(cp.core);
                                    adminData.loadEditRecord_Request(cp.core);
                                    ProcessActionSave(cp, adminData, UseContentWatchLink);
                                    ContentController.processAfterSave(cp.core, false, adminData.adminContent.name, adminData.editRecord.id, adminData.editRecord.nameLc, adminData.editRecord.parentId, UseContentWatchLink);
                                }
                                adminData.admin_Action = Constants.AdminActionNop; // convert so action can be used in as a refresh
                                                                                   //
                                break;
                                case Constants.AdminActionSaveAddNew:
                                //
                                // ----- Save and add a new record
                                //
                                if (adminData.editRecord.userReadOnly) {
                                    Processor.Controllers.ErrorController.addUserError(cp.core, "Your request was blocked because the record you specified is now locked by another authcontext.user.");
                                } else {
                                    adminData.loadEditRecord(cp.core);
                                    adminData.loadEditRecord_Request(cp.core);
                                    ProcessActionSave(cp, adminData, UseContentWatchLink);
                                    ContentController.processAfterSave(cp.core, false, adminData.adminContent.name, adminData.editRecord.id, adminData.editRecord.nameLc, adminData.editRecord.parentId, UseContentWatchLink);
                                    adminData.editRecord.id = 0;
                                    adminData.editRecord.loaded = false;
                                }
                                adminData.admin_Action = Constants.AdminActionNop; // convert so action can be used in as a refresh
                                                                                   //
                                break;
                                case Constants.AdminActionDuplicate:
                                //
                                // ----- Save Record
                                //
                                ProcessActionDuplicate(cp, adminData);
                                adminData.admin_Action = Constants.AdminActionNop;
                                break;
                                case Constants.AdminActionSendEmail:
                                //
                                // ----- Send (Group Email Only)
                                //
                                if (adminData.editRecord.userReadOnly) {
                                    Processor.Controllers.ErrorController.addUserError(cp.core, "Your request was blocked because the record you specified is now locked by another authcontext.user.");
                                } else {
                                    adminData.loadEditRecord(cp.core);
                                    adminData.loadEditRecord_Request(cp.core);
                                    ProcessActionSave(cp, adminData, UseContentWatchLink);
                                    ContentController.processAfterSave(cp.core, false, adminData.adminContent.name, adminData.editRecord.id, adminData.editRecord.nameLc, adminData.editRecord.parentId, UseContentWatchLink);
                                    if (cp.core.doc.userErrorList.Count.Equals(0)) {
                                        using (var csData = new CsModel(cp.core)) {
                                            csData.openRecord("Group Email", adminData.editRecord.id);
                                            if (!csData.ok()) {
                                            } else if (csData.getText("FromAddress") == "") {
                                                Processor.Controllers.ErrorController.addUserError(cp.core, "A 'From Address' is required before sending an email.");
                                            } else if (csData.getText("Subject") == "") {
                                                Processor.Controllers.ErrorController.addUserError(cp.core, "A 'Subject' is required before sending an email.");
                                            } else {
                                                csData.set("submitted", true);
                                                csData.set("ConditionID", 0);
                                                if (csData.getDate("ScheduleDate") == DateTime.MinValue) {
                                                    csData.set("ScheduleDate", cp.core.doc.profileStartTime);
                                                }
                                            }
                                        }
                                    }
                                }
                                adminData.admin_Action = Constants.AdminActionNop; // convert so action can be used in as a refresh
                                                                                   //
                                break;
                                case Constants.AdminActionDeactivateEmail:
                                //
                                // ----- Deactivate (Conditional Email Only)
                                //
                                if (adminData.editRecord.userReadOnly) {
                                    Processor.Controllers.ErrorController.addUserError(cp.core, "Your request was blocked because the record you specified is now locked by another authcontext.user.");
                                } else {
                                    // no save, page was read only - Call ProcessActionSave
                                    adminData.loadEditRecord(cp.core);
                                    if (cp.core.doc.userErrorList.Count.Equals(0)) {
                                        using (var csData = new CsModel(cp.core)) {
                                            if (csData.openRecord("Conditional Email", adminData.editRecord.id)) { csData.set("submitted", false); }
                                            csData.close();
                                        }
                                        adminData.loadEditRecord(cp.core);
                                        adminData.loadEditRecord_Request(cp.core);
                                    }
                                }
                                adminData.admin_Action = Constants.AdminActionNop; // convert so action can be used in as a refresh
                                break;
                                case Constants.AdminActionActivateEmail:
                                //
                                // ----- Activate (Conditional Email Only)
                                //
                                if (adminData.editRecord.userReadOnly) {
                                    Processor.Controllers.ErrorController.addUserError(cp.core, "Your request was blocked because the record you specified is now locked by another authcontext.user.");
                                } else {
                                    adminData.loadEditRecord(cp.core);
                                    adminData.loadEditRecord_Request(cp.core);
                                    ProcessActionSave(cp, adminData, UseContentWatchLink);
                                    ContentController.processAfterSave(cp.core, false, adminData.adminContent.name, adminData.editRecord.id, adminData.editRecord.nameLc, adminData.editRecord.parentId, UseContentWatchLink);
                                    if (cp.core.doc.userErrorList.Count.Equals(0)) {
                                        using (var csData = new CsModel(cp.core)) {
                                            csData.openRecord("Conditional Email", adminData.editRecord.id);
                                            if (!csData.ok()) {
                                            } else if (csData.getInteger("ConditionID") == 0) {
                                                Processor.Controllers.ErrorController.addUserError(cp.core, "A condition must be set.");
                                            } else {
                                                csData.set("submitted", true);
                                                if (csData.getDate("ScheduleDate") == DateTime.MinValue) {
                                                    csData.set("ScheduleDate", cp.core.doc.profileStartTime);
                                                }
                                            }
                                        }
                                        adminData.loadEditRecord(cp.core);
                                        adminData.loadEditRecord_Request(cp.core);
                                    }
                                }
                                adminData.admin_Action = Constants.AdminActionNop; // convert so action can be used in as a refresh
                                break;
                                case Constants.AdminActionSendEmailTest:
                                if (adminData.editRecord.userReadOnly) {
                                    Processor.Controllers.ErrorController.addUserError(cp.core, "Your request was blocked because the record you specified is now locked by another authcontext.user.");
                                } else {
                                    //
                                    adminData.loadEditRecord(cp.core);
                                    adminData.loadEditRecord_Request(cp.core);
                                    ProcessActionSave(cp, adminData, UseContentWatchLink);
                                    ContentController.processAfterSave(cp.core, false, adminData.adminContent.name, adminData.editRecord.id, adminData.editRecord.nameLc, adminData.editRecord.parentId, UseContentWatchLink);
                                    //
                                    if (cp.core.doc.userErrorList.Count.Equals(0)) {
                                        //
                                        EmailToConfirmationMemberId = 0;
                                        if (adminData.editRecord.fieldsLc.ContainsKey("testmemberid")) {
                                            EmailToConfirmationMemberId = GenericController.encodeInteger(adminData.editRecord.fieldsLc["testmemberid"].value);
                                            EmailController.queueConfirmationTestEmail(cp.core, adminData.editRecord.id, EmailToConfirmationMemberId);
                                            //
                                            if (adminData.editRecord.fieldsLc.ContainsKey("lastsendtestdate")) {
                                                //
                                                // -- if there were no errors, and the table supports lastsendtestdate, update it
                                                adminData.editRecord.fieldsLc["lastsendtestdate"].value = cp.core.doc.profileStartTime;
                                                db.executeQuery("update ccemail Set lastsendtestdate=" + DbController.encodeSQLDate(cp.core.doc.profileStartTime) + " where id=" + adminData.editRecord.id);
                                            }
                                        }
                                    }
                                }
                                adminData.admin_Action = Constants.AdminActionNop; // convert so action can be used in as a refresh
                                                                                   // end case
                                break;
                                case Constants.AdminActionDeleteRows:
                                //
                                // Delete Multiple Rows
                                //
                                RowCnt = cp.core.docProperties.getInteger("rowcnt");
                                if (RowCnt > 0) {
                                    for (RowPtr = 0; RowPtr < RowCnt; RowPtr++) {
                                        if (cp.core.docProperties.getBoolean("row" + RowPtr)) {
                                            using (var csData = new CsModel(cp.core)) {
                                                csData.openRecord(adminData.adminContent.name, cp.core.docProperties.getInteger("rowid" + RowPtr));
                                                if (csData.ok()) {
                                                    RecordId = csData.getInteger("ID");
                                                    csData.deleteRecord();
                                                    //
                                                    // non-Workflow Delete
                                                    //
                                                    ContentName = MetadataController.getContentNameByID(cp.core, csData.getInteger("contentControlId"));
                                                    cp.core.cache.invalidateDbRecord(RecordId, adminData.adminContent.tableName);
                                                    ContentController.processAfterSave(cp.core, true, ContentName, RecordId, "", 0, UseContentWatchLink);
                                                    //
                                                    // Page Content special cases
                                                    //
                                                    if (GenericController.toLCase(adminData.adminContent.tableName) == "ccpagecontent") {
                                                        if (RecordId == (cp.core.siteProperties.getInteger("PageNotFoundPageID", 0))) {
                                                            cp.core.siteProperties.getText("PageNotFoundPageID", "0");
                                                        }
                                                        if (RecordId == (cp.core.siteProperties.getInteger("LandingPageID", 0))) {
                                                            cp.core.siteProperties.getText("LandingPageID", "0");
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                                case Constants.AdminActionReloadCDef:
                                //
                                // ccContent - save changes and reload content definitions
                                //
                                if (adminData.editRecord.userReadOnly) {
                                    Processor.Controllers.ErrorController.addUserError(cp.core, "Your request was blocked because the record you specified Is now locked by another authcontext.user.");
                                } else {
                                    adminData.loadEditRecord(cp.core);
                                    adminData.loadEditRecord_Request(cp.core);
                                    ProcessActionSave(cp, adminData, UseContentWatchLink);
                                    cp.core.cache.invalidateAll();
                                    cp.core.clearMetaData();
                                }
                                adminData.admin_Action = Constants.AdminActionNop; // convert so action can be used in as a refresh
                                break;
                                default:
                                //
                                // do nothing action or anything unrecognized - read in database
                                //
                                break;
                            }
                        }
                    }
                }
                //
                return;
                //
                // ----- Error Trap
                //
            } catch (Exception ex) {
                Processor.Controllers.ErrorController.addUserError(cp.core, "There was an unknown error processing this page at " + cp.core.doc.profileStartTime + ". Please try again, Or report this error To the site administrator.");
                LogController.logError(cp.core, ex);
            }
        }
        //
        //========================================================================
        // LoadAndSaveContentGroupRules
        //
        //   For a particular content, remove previous GroupRules, and Create new ones
        //========================================================================
        //
        private void LoadAndSaveContentGroupRules(CPClass cp, int GroupID) {
            try {
                //
                int ContentCount = 0;
                int ContentPointer = 0;
                int ContentId = 0;
                bool AllowAdd = false;
                bool AllowDelete = false;
                bool RecordChanged = false;
                bool RuleNeeded = false;
                bool RuleFound = false;
                string SQL = null;
                string DeleteIdList = "";
                int RuleId = 0;
                //
                // ----- Delete duplicate Group Rules
                //
                SQL = "Select distinct DuplicateRules.ID"
                    + " from ccgrouprules"
                    + " Left join ccgrouprules As DuplicateRules On DuplicateRules.ContentID=ccGroupRules.ContentID"
                    + " where ccGroupRules.ID < DuplicateRules.ID"
                    + " And ccGroupRules.GroupID=DuplicateRules.GroupID";
                SQL = "Delete from ccGroupRules where ID In (" + SQL + ")";
                cp.core.db.executeQuery(SQL);
                //
                // --- create GroupRule records for all selected
                //
                using (var csData = new CsModel(cp.core)) {
                    csData.open("Group Rules", "GroupID=" + GroupID, "ContentID, ID", true);
                    ContentCount = cp.core.docProperties.getInteger("ContentCount");
                    if (ContentCount > 0) {
                        for (ContentPointer = 0; ContentPointer < ContentCount; ContentPointer++) {
                            RuleNeeded = cp.core.docProperties.getBoolean("Content" + ContentPointer);
                            ContentId = cp.core.docProperties.getInteger("ContentID" + ContentPointer);
                            AllowAdd = cp.core.docProperties.getBoolean("ContentGroupRuleAllowAdd" + ContentPointer);
                            AllowDelete = cp.core.docProperties.getBoolean("ContentGroupRuleAllowDelete" + ContentPointer);
                            //
                            RuleFound = false;
                            csData.goFirst();
                            if (csData.ok()) {
                                while (csData.ok()) {
                                    if (csData.getInteger("ContentID") == ContentId) {
                                        RuleId = csData.getInteger("id");
                                        RuleFound = true;
                                        break;
                                    }
                                    csData.goNext();
                                }
                            }
                            if (RuleNeeded && !RuleFound) {
                                using (var CSNew = new CsModel(cp.core)) {
                                    CSNew.insert("Group Rules");
                                    if (CSNew.ok()) {
                                        CSNew.set("GroupID", GroupID);
                                        CSNew.set("ContentID", ContentId);
                                        CSNew.set("AllowAdd", AllowAdd);
                                        CSNew.set("AllowDelete", AllowDelete);
                                    }
                                }
                                RecordChanged = true;
                            } else if (RuleFound && !RuleNeeded) {
                                DeleteIdList += ", " + RuleId;
                                RecordChanged = true;
                            } else if (RuleFound && RuleNeeded) {
                                if (AllowAdd != csData.getBoolean("AllowAdd")) {
                                    csData.set("AllowAdd", AllowAdd);
                                    RecordChanged = true;
                                }
                                if (AllowDelete != csData.getBoolean("AllowDelete")) {
                                    csData.set("AllowDelete", AllowDelete);
                                    RecordChanged = true;
                                }
                            }
                        }
                    }
                }
                if (!string.IsNullOrEmpty(DeleteIdList)) {
                    SQL = "delete from ccgrouprules where id In (" + DeleteIdList.Substring(1) + ")";
                    cp.core.db.executeQuery(SQL);
                }
                if (RecordChanged) {
                    GroupRuleModel.invalidateCacheOfTable<GroupRuleModel>(cp);
                }
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
            }
        }
        //
        //========================================================================
        // LoadAndSaveGroupRules
        //   read groups from the edit form and modify Group Rules to match
        //========================================================================
        //
        private void LoadAndSaveGroupRules(CPClass cp, EditRecordModel editRecord) {
            try {
                //
                if (editRecord.id != 0) {
                    LoadAndSaveGroupRules_ForContentAndChildren(cp, editRecord.id, "");
                }
                //
                return;
                //
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
            }
        }
        //
        //========================================================================
        // LoadAndSaveGroupRules_ForContentAndChildren
        //   read groups from the edit form and modify Group Rules to match
        //========================================================================
        //
        private void LoadAndSaveGroupRules_ForContentAndChildren(CPClass cp, int ContentID, string ParentIDString) {
            try {
                string MyParentIDString = null;
                //
                // --- Create Group Rules for this content
                //
                if (encodeBoolean(ParentIDString.IndexOf("," + ContentID + ",") + 1)) {
                    throw (new Exception("Child ContentID [" + ContentID + "] Is its own parent"));
                } else {
                    MyParentIDString = ParentIDString + "," + ContentID + ",";
                    LoadAndSaveGroupRules_ForContent(cp, ContentID);
                    //
                    // --- Create Group Rules for all child content
                    //
                    using (var csData = new CsModel(cp.core)) {
                        csData.open("Content", "ParentID=" + ContentID);
                        while (csData.ok()) {
                            LoadAndSaveGroupRules_ForContentAndChildren(cp, csData.getInteger("id"), MyParentIDString);
                            csData.goNext();
                        }
                    }
                }
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
            }
        }
        //
        //========================================================================
        // LoadAndSaveGroupRules_ForContent
        //
        //   For a particular content, remove previous GroupRules, and Create new ones
        //========================================================================
        //
        private void LoadAndSaveGroupRules_ForContent(CPClass cp, int ContentID) {
            try {
                //
                int GroupCount = 0;
                int GroupPointer = 0;
                int GroupID = 0;
                bool AllowAdd = false;
                bool AllowDelete = false;
                bool RecordChanged = false;
                bool RuleNeeded = false;
                bool RuleFound = false;
                string SQL;
                //
                // ----- Delete duplicate Group Rules
                //

                SQL = "Delete from ccGroupRules where ID In ("
                    + "Select distinct DuplicateRules.ID from ccgrouprules Left join ccgrouprules As DuplicateRules On DuplicateRules.GroupID=ccGroupRules.GroupID where ccGroupRules.ID < DuplicateRules.ID  And ccGroupRules.ContentID=DuplicateRules.ContentID"
                    + ")";
                cp.core.db.executeQuery(SQL);
                //
                // --- create GroupRule records for all selected
                //
                using (var csData = new CsModel(cp.core)) {
                    csData.open("Group Rules", "ContentID=" + ContentID, "GroupID,ID", true);
                    GroupCount = cp.core.docProperties.getInteger("GroupCount");
                    if (GroupCount > 0) {
                        for (GroupPointer = 0; GroupPointer < GroupCount; GroupPointer++) {
                            RuleNeeded = cp.core.docProperties.getBoolean("Group" + GroupPointer);
                            GroupID = cp.core.docProperties.getInteger("GroupID" + GroupPointer);
                            AllowAdd = cp.core.docProperties.getBoolean("GroupRuleAllowAdd" + GroupPointer);
                            AllowDelete = cp.core.docProperties.getBoolean("GroupRuleAllowDelete" + GroupPointer);
                            //
                            RuleFound = false;
                            csData.goFirst();
                            if (csData.ok()) {
                                while (csData.ok()) {
                                    if (csData.getInteger("GroupID") == GroupID) {
                                        RuleFound = true;
                                        break;
                                    }
                                    csData.goNext();
                                }
                            }
                            if (RuleNeeded && !RuleFound) {
                                using (var CSNew = new CsModel(cp.core)) {
                                    CSNew.insert("Group Rules");
                                    if (CSNew.ok()) {
                                        CSNew.set("ContentID", ContentID);
                                        CSNew.set("GroupID", GroupID);
                                        CSNew.set("AllowAdd", AllowAdd);
                                        CSNew.set("AllowDelete", AllowDelete);
                                    }
                                }
                                RecordChanged = true;
                            } else if (RuleFound && !RuleNeeded) {
                                csData.deleteRecord();
                                RecordChanged = true;
                            } else if (RuleFound && RuleNeeded) {
                                if (AllowAdd != csData.getBoolean("AllowAdd")) {
                                    csData.set("AllowAdd", AllowAdd);
                                    RecordChanged = true;
                                }
                                if (AllowDelete != csData.getBoolean("AllowDelete")) {
                                    csData.set("AllowDelete", AllowDelete);
                                    RecordChanged = true;
                                }
                            }
                        }
                    }
                }
                if (RecordChanged) {
                    GroupRuleModel.invalidateCacheOfTable<GroupRuleModel>(cp);
                }
                return;
                //
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
            }
        }
        //========================================================================
        //   Save Whats New values if present
        //
        //   does NOT check AuthoringLocked -- you must check before calling
        //========================================================================
        //
        private void SaveContentTracking(CPClass cp, AdminDataModel adminData) {
            try {
                // todo

                Contensive.Processor.Addons.AdminSite.Models.EditRecordModel editRecord = adminData.editRecord;
                //
                int ContentId = 0;
                int ContentWatchId = 0;
                //
                if (adminData.adminContent.allowContentTracking && (!editRecord.userReadOnly)) {
                    //
                    // ----- Set default content watch link label
                    //
                    if ((adminData.contentWatchListIDCount > 0) && (adminData.contentWatchLinkLabel == "")) {
                        if (editRecord.menuHeadline != "") {
                            adminData.contentWatchLinkLabel = editRecord.menuHeadline;
                        } else if (editRecord.nameLc != "") {
                            adminData.contentWatchLinkLabel = editRecord.nameLc;
                        } else {
                            adminData.contentWatchLinkLabel = "Click Here";
                        }
                    }
                    // ----- update/create the content watch record for this content record
                    //
                    ContentId = (editRecord.contentControlId.Equals(0)) ? adminData.adminContent.id : editRecord.contentControlId;
                    using (var csData = new CsModel(cp.core)) {
                        csData.open("Content Watch", "(ContentID=" + DbController.encodeSQLNumber(ContentId) + ")And(RecordID=" + DbController.encodeSQLNumber(editRecord.id) + ")");
                        if (!csData.ok()) {
                            csData.insert("Content Watch");
                            csData.set("contentid", ContentId);
                            csData.set("recordid", editRecord.id);
                            csData.set("ContentRecordKey", ContentId + "." + editRecord.id);
                            csData.set("clicks", 0);
                        }
                        if (!csData.ok()) {
                            LogController.logError(cp.core, new GenericException("SaveContentTracking, can Not create New record"));
                        } else {
                            ContentWatchId = csData.getInteger("ID");
                            csData.set("LinkLabel", adminData.contentWatchLinkLabel);
                            csData.set("WhatsNewDateExpires", adminData.contentWatchExpires);
                            csData.set("Link", adminData.contentWatchLink);
                            //
                            // ----- delete all rules for this ContentWatch record
                            //
                            using (var CSPointer = new CsModel(cp.core)) {
                                CSPointer.open("Content Watch List Rules", "(ContentWatchID=" + ContentWatchId + ")");
                                while (CSPointer.ok()) {
                                    CSPointer.deleteRecord();
                                    CSPointer.goNext();
                                }
                                CSPointer.close();
                            }
                            //
                            // ----- Update ContentWatchListRules for all entries in ContentWatchListID( ContentWatchListIDCount )
                            //
                            int ListPointer = 0;
                            if (adminData.contentWatchListIDCount > 0) {
                                for (ListPointer = 0; ListPointer < adminData.contentWatchListIDCount; ListPointer++) {
                                    using (var CSRules = new CsModel(cp.core)) {
                                        CSRules.insert("Content Watch List Rules");
                                        if (CSRules.ok()) {
                                            CSRules.set("ContentWatchID", ContentWatchId);
                                            CSRules.set("ContentWatchListID", adminData.contentWatchListID[ListPointer]);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
            }
        }
        //
        //========================================================================
        //   Save Link Alias field if it supported, and is non-authoring
        //   if it is authoring, it will be saved by the userfield routines
        //   if not, it appears in the LinkAlias tab, and must be saved here
        //========================================================================
        //
        private void SaveLinkAlias(CPClass cp, AdminDataModel adminData) {
            try {
                // todo
                Contensive.Processor.Addons.AdminSite.Models.EditRecordModel editRecord = adminData.editRecord;
                //
                // --use field ptr to test if the field is supported yet
                if (cp.core.siteProperties.allowLinkAlias) {
                    bool isDupError = false;
                    string linkAlias = cp.core.docProperties.getText("linkalias");
                    bool OverRideDuplicate = cp.core.docProperties.getBoolean("OverRideDuplicate");
                    bool DupCausesWarning = false;
                    if (string.IsNullOrEmpty(linkAlias)) {
                        //
                        // Link Alias is blank, use the record name
                        //
                        linkAlias = editRecord.nameLc;
                        DupCausesWarning = true;
                    }
                    if (!string.IsNullOrEmpty(linkAlias)) {
                        if (OverRideDuplicate) {
                            cp.core.db.executeQuery("update " + adminData.adminContent.tableName + " set linkalias=null where ( linkalias=" + DbController.encodeSQLText(linkAlias) + ") and (id<>" + editRecord.id + ")");
                        } else {
                            using (var csData = new CsModel(cp.core)) {
                                csData.open(adminData.adminContent.name, "( linkalias=" + DbController.encodeSQLText(linkAlias) + ")and(id<>" + editRecord.id + ")");
                                if (csData.ok()) {
                                    isDupError = true;
                                    ErrorController.addUserError(cp.core, "The Link Alias you entered can not be used because another record uses this value [" + linkAlias + "]. Enter a different Link Alias, or check the Override Duplicates checkbox in the Link Alias tab.");
                                }
                                csData.close();
                            }
                        }
                        if (!isDupError) {
                            DupCausesWarning = true;
                            using (var csData = new CsModel(cp.core)) {
                                csData.openRecord(adminData.adminContent.name, editRecord.id);
                                if (csData.ok()) {
                                    csData.set("linkalias", linkAlias);
                                }
                            }
                            //
                            // Update the Link Aliases
                            //
                            LinkAliasController.addLinkAlias(cp.core, linkAlias, editRecord.id, "", OverRideDuplicate, DupCausesWarning);
                        }
                    }
                }
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
            }
        }
        //
        //========================================================================
        //
        private void SaveEditRecord(CPClass cp, AdminDataModel adminData) {
            try {
                // todo
                Contensive.Processor.Addons.AdminSite.Models.EditRecordModel editRecord = adminData.editRecord;
                //
                int SaveCCIDValue = 0;
                int ActivityLogOrganizationId = -1;
                if (!cp.core.doc.userErrorList.Count.Equals(0)) {
                    //
                    // -- If There is an error, block the save
                    adminData.admin_Action = Constants.AdminActionNop;
                } else if (!cp.core.session.isAuthenticatedContentManager(adminData.adminContent.name)) {
                    //
                    // -- must be content manager
                } else if (editRecord.userReadOnly) {
                    //
                    // -- read only block
                } else {
                    //
                    // -- Record will be saved, create a new one if this is an add
                    bool NewRecord = false;
                    bool recordChanged = false;
                    using (var csData = new CsModel(cp.core)) {
                        if (editRecord.id == 0) {
                            NewRecord = true;
                            recordChanged = true;
                            csData.insert(adminData.adminContent.name);
                        } else {
                            NewRecord = false;
                            csData.openRecord(adminData.adminContent.name, editRecord.id);
                        }
                        if (!csData.ok()) {
                            //
                            // ----- Error: new record could not be created
                            //
                            if (NewRecord) {
                                //
                                // Could not insert record
                                //
                                LogController.logError(cp.core, new GenericException("A new record could not be inserted for content [" + adminData.adminContent.name + "]. Verify the Database table and field DateAdded, CreateKey, and ID."));
                            } else {
                                //
                                // Could not locate record you requested
                                //
                                LogController.logError(cp.core, new GenericException("The record you requested (ID=" + editRecord.id + ") could not be found for content [" + adminData.adminContent.name + "]"));
                            }
                        } else {
                            //
                            // ----- Get the ID of the current record
                            //
                            editRecord.id = csData.getInteger("ID");
                            //
                            // ----- Create the update sql
                            //
                            bool fieldChanged = false;
                            foreach (var keyValuePair in adminData.adminContent.fields) {
                                ContentFieldMetadataModel field = keyValuePair.Value;
                                EditRecordFieldModel editRecordField = editRecord.fieldsLc[field.nameLc];
                                object fieldValueObject = editRecordField.value;
                                string FieldValueText = GenericController.encodeText(fieldValueObject);
                                string fieldName = field.nameLc;
                                string UcaseFieldName = GenericController.toUCase(fieldName);
                                //
                                // ----- Handle special case fields
                                //
                                switch (UcaseFieldName) {
                                    case "NAME": {
                                            //
                                            editRecord.nameLc = GenericController.encodeText(fieldValueObject);
                                            break;
                                        }
                                    case "CCGUID": {
                                            if (NewRecord && string.IsNullOrEmpty(FieldValueText)) {
                                                //
                                                // if new record and edit form returns empty, preserve the guid used to create the record.
                                            } else {
                                                //
                                                // save the value in the request
                                                if (csData.getText(fieldName) != FieldValueText) {
                                                    fieldChanged = true;
                                                    recordChanged = true;
                                                    csData.set(fieldName, FieldValueText);
                                                }
                                            }
                                            break;
                                        }
                                    case "CONTENTCONTROLID": {
                                            //
                                            // run this after the save, so it will be blocked if the save fails
                                            // block the change from this save
                                            // Update the content control ID here, for all the children, and all the edit and archive records of both
                                            //
                                            int saveValue = GenericController.encodeInteger(fieldValueObject);
                                            if (editRecord.contentControlId != saveValue) {
                                                SaveCCIDValue = saveValue;
                                                recordChanged = true;
                                            }
                                            break;
                                        }
                                    case "ACTIVE": {
                                            bool saveValue = GenericController.encodeBoolean(fieldValueObject);
                                            if (csData.getBoolean(fieldName) != saveValue) {
                                                fieldChanged = true;
                                                recordChanged = true;
                                                csData.set(fieldName, saveValue);
                                            }
                                            break;
                                        }
                                    case "DATEEXPIRES": {
                                            //
                                            // ----- make sure content watch expires before content expires
                                            //
                                            if (!GenericController.isNull(fieldValueObject)) {
                                                if (GenericController.isDate(fieldValueObject)) {
                                                    DateTime saveValue = GenericController.encodeDate(fieldValueObject);
                                                    if (adminData.contentWatchExpires <= DateTime.MinValue) {
                                                        adminData.contentWatchExpires = saveValue;
                                                    } else if (adminData.contentWatchExpires > saveValue) {
                                                        adminData.contentWatchExpires = saveValue;
                                                    }
                                                }
                                            }
                                            //
                                            break;
                                        }
                                    case "DATEARCHIVE": {
                                            //
                                            // ----- make sure content watch expires before content archives
                                            //
                                            if (!GenericController.isNull(fieldValueObject)) {
                                                if (GenericController.isDate(fieldValueObject)) {
                                                    DateTime saveValue = GenericController.encodeDate(fieldValueObject);
                                                    if ((adminData.contentWatchExpires) <= DateTime.MinValue) {
                                                        adminData.contentWatchExpires = saveValue;
                                                    } else if (adminData.contentWatchExpires > saveValue) {
                                                        adminData.contentWatchExpires = saveValue;
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
                                //
                                // ----- Put the field in the SQL to be saved
                                //
                                if (AdminDataModel.isVisibleUserField(cp.core, field.adminOnly, field.developerOnly, field.active, field.authorable, field.nameLc, adminData.adminContent.tableName) && (NewRecord || (!field.readOnly)) && (NewRecord || (!field.notEditable))) {
                                    //
                                    // ----- save the value by field type
                                    //
                                    switch (field.fieldTypeId) {
                                        case CPContentBaseClass.FieldTypeIdEnum.AutoIdIncrement:
                                        case CPContentBaseClass.FieldTypeIdEnum.Redirect: {
                                                //
                                                // do nothing with these
                                                //
                                                break;
                                            }
                                        case CPContentBaseClass.FieldTypeIdEnum.File:
                                        case CPContentBaseClass.FieldTypeIdEnum.FileImage: {
                                                //
                                                // filenames, upload to cdnFiles
                                                //
                                                if (cp.core.docProperties.getBoolean(fieldName + ".DeleteFlag")) {
                                                    recordChanged = true;
                                                    fieldChanged = true;
                                                    csData.set(fieldName, "");
                                                }
                                                string filename = GenericController.encodeText(fieldValueObject);
                                                if (!string.IsNullOrWhiteSpace(filename)) {
                                                    filename = FileController.encodeDosFilename(filename);
                                                    string unixPathFilename = csData.getFieldFilename(fieldName, filename);
                                                    string dosPathFilename = FileController.convertToDosSlash(unixPathFilename);
                                                    string dosPath = FileController.getPath(dosPathFilename);
                                                    cp.core.cdnFiles.upload(fieldName, dosPath, ref filename);
                                                    csData.set(fieldName, unixPathFilename);
                                                    recordChanged = true;
                                                    fieldChanged = true;
                                                }
                                                break;
                                            }
                                        case CPContentBaseClass.FieldTypeIdEnum.Boolean: {
                                                //
                                                // boolean
                                                //
                                                bool saveValue = GenericController.encodeBoolean(fieldValueObject);
                                                if (csData.getBoolean(fieldName) != saveValue) {
                                                    recordChanged = true;
                                                    fieldChanged = true;
                                                    csData.set(fieldName, saveValue);
                                                }
                                                break;
                                            }
                                        case CPContentBaseClass.FieldTypeIdEnum.Currency:
                                        case CPContentBaseClass.FieldTypeIdEnum.Float: {
                                                //
                                                // Floating pointer numbers, allow nullable
                                                if (string.IsNullOrWhiteSpace(encodeText(fieldValueObject))) {
                                                    fieldChanged = true;
                                                    recordChanged = true;
                                                    csData.set(fieldName, null);
                                                } else if (encodeNumber(fieldValueObject) != csData.getNumber(fieldName)) {
                                                    fieldChanged = true;
                                                    recordChanged = true;
                                                    csData.set(fieldName, encodeNumber(fieldValueObject));
                                                }
                                                break;
                                            }
                                        case CPContentBaseClass.FieldTypeIdEnum.Date: {
                                                //
                                                // Date
                                                //
                                                if (string.IsNullOrWhiteSpace(encodeText(fieldValueObject))) {
                                                    fieldChanged = true;
                                                    recordChanged = true;
                                                    csData.set(fieldName, null);
                                                } else if (encodeDate(fieldValueObject) != csData.getDate(fieldName)) {
                                                    fieldChanged = true;
                                                    recordChanged = true;
                                                    csData.set(fieldName, encodeDate(fieldValueObject));
                                                }
                                                break;
                                            }
                                        case CPContentBaseClass.FieldTypeIdEnum.Integer:
                                        case CPContentBaseClass.FieldTypeIdEnum.Lookup: {
                                                //
                                                // Integers, allow nullable
                                                if (string.IsNullOrWhiteSpace(encodeText(fieldValueObject))) {
                                                    fieldChanged = true;
                                                    recordChanged = true;
                                                    csData.set(fieldName, null);
                                                } else if (encodeInteger(fieldValueObject) != csData.getInteger(fieldName)) {
                                                    fieldChanged = true;
                                                    recordChanged = true;
                                                    csData.set(fieldName, encodeInteger(fieldValueObject));
                                                }
                                                break;
                                            }
                                        case CPContentBaseClass.FieldTypeIdEnum.LongText:
                                        case CPContentBaseClass.FieldTypeIdEnum.Text:
                                        case CPContentBaseClass.FieldTypeIdEnum.FileText:
                                        case CPContentBaseClass.FieldTypeIdEnum.FileCSS:
                                        case CPContentBaseClass.FieldTypeIdEnum.FileXML:
                                        case CPContentBaseClass.FieldTypeIdEnum.FileJavascript:
                                        case CPContentBaseClass.FieldTypeIdEnum.HTML:
                                        case CPContentBaseClass.FieldTypeIdEnum.HTMLCode:
                                        case CPContentBaseClass.FieldTypeIdEnum.FileHTML:
                                        case CPContentBaseClass.FieldTypeIdEnum.FileHTMLCode: {
                                                //
                                                // Text
                                                //
                                                string saveValue = GenericController.encodeText(fieldValueObject);
                                                if (csData.getText(fieldName) != saveValue) {
                                                    fieldChanged = true;
                                                    recordChanged = true;
                                                    csData.set(fieldName, saveValue);
                                                }
                                                break;
                                            }
                                        case CPContentBaseClass.FieldTypeIdEnum.ManyToMany: {
                                                //
                                                // Many to Many checklist
                                                cp.core.html.processCheckList("field" + field.id, MetadataController.getContentNameByID(cp.core, field.contentId), encodeText(editRecord.id), MetadataController.getContentNameByID(cp.core, field.manyToManyContentId), MetadataController.getContentNameByID(cp.core, field.manyToManyRuleContentId), field.manyToManyRulePrimaryField, field.manyToManyRuleSecondaryField);
                                                break;
                                            }
                                        default: {
                                                //
                                                // Unknown other types
                                                string saveValue = GenericController.encodeText(fieldValueObject);
                                                fieldChanged = true;
                                                recordChanged = true;
                                                csData.set(UcaseFieldName, saveValue);
                                                break;
                                            }
                                    }
                                }
                                //
                                // -- put any changes back in array for the next page to display
                                editRecordField.value = fieldValueObject;
                                //
                                // -- Log Activity for changes to people and organizattions
                                if (fieldChanged) {
                                    if (adminData.adminContent.tableName.Equals("cclibraryfiles")) {
                                        if (cp.core.docProperties.getText("filename") != "") {
                                            csData.set("altsizelist", "");
                                        }
                                    }
                                    if (!NewRecord) {
                                        switch (GenericController.toLCase(adminData.adminContent.tableName)) {
                                            case "ccmembers": {
                                                    //
                                                    if (ActivityLogOrganizationId < 0) {
                                                        PersonModel person = DbBaseModel.create<PersonModel>(cp, editRecord.id);
                                                        if (person != null) {
                                                            ActivityLogOrganizationId = person.organizationId;
                                                        }
                                                    }
                                                    LogController.addSiteActivity(cp.core, "modifying field " + fieldName, editRecord.id, ActivityLogOrganizationId);
                                                    break;
                                                }
                                            case "organizations": {
                                                    //
                                                    LogController.addSiteActivity(cp.core, "modifying field " + fieldName, 0, editRecord.id);
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
                            if (recordChanged) {
                                //
                                // -- clear cache
                                string tableName;
                                if (editRecord.contentControlId == 0) {
                                    tableName = MetadataController.getContentTablename(cp.core, adminData.adminContent.name).ToLowerInvariant();
                                } else {
                                    tableName = MetadataController.getContentTablename(cp.core, editRecord.contentControlId_Name).ToLowerInvariant();
                                }
                                if (tableName == LinkAliasModel.tableMetadata.tableNameLower) {
                                    LinkAliasModel.invalidateCacheOfRecord<LinkAliasModel>(cp, editRecord.id);
                                } else if (tableName == AddonModel.tableMetadata.tableNameLower) {
                                    AddonModel.invalidateCacheOfRecord<AddonModel>(cp, editRecord.id);
                                } else {
                                    LinkAliasModel.invalidateCacheOfRecord<LinkAliasModel>(cp, editRecord.id);
                                }
                            }
                            //
                            // ----- clear/set authoring controls
                            var contentTable = DbBaseModel.createByUniqueName<TableModel>(cp, adminData.adminContent.tableName);
                            if (contentTable != null) WorkflowController.clearEditLock(cp.core, contentTable.id, editRecord.id);
                            //
                            // ----- if admin content is changed, reload the adminContext.content data in case this is a save, and not an OK
                            if (recordChanged && SaveCCIDValue != 0) {
                                adminData.adminContent.setContentControlId(cp.core, editRecord.id, SaveCCIDValue);
                                editRecord.contentControlId_Name = MetadataController.getContentNameByID(cp.core, SaveCCIDValue);
                                adminData.adminContent = ContentMetadataModel.createByUniqueName(cp.core, editRecord.contentControlId_Name);
                                adminData.adminContent.id = adminData.adminContent.id;
                                adminData.adminContent.name = adminData.adminContent.name;
                            }
                        }
                    }
                    editRecord.saved = true;
                }
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
            }
        }
        //
        ////
        //========================================================================
        // GetForm_Top
        //   Prints the admin page before the content form window.
        //   After this, print the content window, then PrintFormBottom()
        //========================================================================
        //
        private string getAdminHeader(CPClass cp, AdminDataModel adminData, string BackgroundColor = "") {
            string result = "";
            try {
                string leftSide = cp.core.siteProperties.getText("AdminHeaderHTML", "Administration Site");
                string rightSide = HtmlController.a(cp.User.Name, "?af=4&cid=" + cp.Content.GetID("people") + "&id=" + cp.User.Id);
                string rightSideNavHtml = ""
                    + "<form class=\"form-inline\" method=post action=\"?method=logout\">"
                    + "<button class=\"btn btn-warning btn-sm ml-2\" type=\"submit\">Logout</button>"
                    + "</form>";
                //
                // Assemble header
                //
                StringBuilderLegacyController Stream = new StringBuilderLegacyController();
                Stream.add(AdminUIController.getHeader(cp.core, leftSide, rightSide, rightSideNavHtml));
                //
                // --- Content Definition
                adminData.adminFooter = "";
                //
                // -- Admin Navigator
                string AdminNavFull = cp.core.addon.execute(DbBaseModel.create<AddonModel>(cp, AdminNavigatorGuid), new BaseClasses.CPUtilsBaseClass.addonExecuteContext {
                    addonType = BaseClasses.CPUtilsBaseClass.addonContext.ContextAdmin,
                    errorContextMessage = "executing Admin Navigator in Admin"
                });
                //
                Stream.add("<table border=0 cellpadding=0 cellspacing=0><tr>\r<td class=\"ccToolsCon\" valign=top>" + AdminNavFull + "</td>\r<td id=\"desktop\" class=\"ccContentCon\" valign=top>");
                adminData.adminFooter = adminData.adminFooter + "</td></tr></table>";
                //
                result = Stream.text;
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
                throw;
            }
            return result;
        }
        //
        //========================================================================
        // Get Menu Link
        //========================================================================
        //
        private string GetMenuLink(CPClass cp, string LinkPage, int LinkCID) {
            string tempGetMenuLink = null;
            try {
                //
                int ContentId = 0;
                //
                if (!string.IsNullOrEmpty(LinkPage) || (LinkCID != 0)) {
                    tempGetMenuLink = LinkPage;
                    if (!string.IsNullOrEmpty(tempGetMenuLink)) {
                        if (tempGetMenuLink.left(1) == "?" || tempGetMenuLink.left(1) == "#") {
                            tempGetMenuLink = "/" + cp.core.appConfig.adminRoute + tempGetMenuLink;
                        }
                    } else {
                        tempGetMenuLink = "/" + cp.core.appConfig.adminRoute;
                    }
                    ContentId = GenericController.encodeInteger(LinkCID);
                    if (ContentId != 0) {
                        tempGetMenuLink = GenericController.modifyLinkQuery(tempGetMenuLink, "cid", ContentId.ToString(), true);
                    }
                }
                return tempGetMenuLink;
                //
                // ----- Error Trap
                //
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
            }
            return tempGetMenuLink;
        }
        //
        /// <summary>
        /// 
        /// </summary>
        /// <param name="adminData.content"></param>
        /// <param name="editRecord"></param>
        //

        private void ProcessForms(CPClass cp, AdminDataModel adminData) {
            try {
                // todo
                Contensive.Processor.Addons.AdminSite.Models.EditRecordModel editRecord = adminData.editRecord;
                //
                //
                if (adminData.adminSourceForm != 0) {
                    string EditorStyleRulesFilename = null;
                    switch (adminData.adminSourceForm) {
                        case AdminFormReports: {
                                //
                                // Reports form cancel button
                                //
                                switch (adminData.requestButton) {
                                    case ButtonCancel: {
                                            adminData.admin_Action = Constants.AdminActionNop;
                                            adminData.adminForm = AdminFormRoot;
                                            break;
                                        }
                                    default: {
                                            // do nothing
                                            break;
                                        }
                                }
                                break;
                            }
                        case AdminFormQuickStats: {
                                switch (adminData.requestButton) {
                                    case ButtonCancel: {
                                            adminData.admin_Action = Constants.AdminActionNop;
                                            adminData.adminForm = AdminFormRoot;
                                            break;
                                        }
                                    default: {
                                            // do nothing
                                            break;
                                        }
                                }
                                break;
                            }
                        case AdminFormPublishing: {
                                //
                                // Publish Form
                                //
                                switch (adminData.requestButton) {
                                    case ButtonCancel: {
                                            adminData.admin_Action = Constants.AdminActionNop;
                                            adminData.adminForm = AdminFormRoot;
                                            break;
                                        }
                                    default: {
                                            // do nothing
                                            break;
                                        }
                                }
                                break;
                            }
                        case AdminFormIndex: {

                                switch (adminData.requestButton) {
                                    case ButtonCancel: {
                                            adminData.admin_Action = Constants.AdminActionNop;
                                            adminData.adminForm = AdminFormRoot;
                                            adminData.adminContent = new ContentMetadataModel();
                                            break;
                                        }
                                    case ButtonClose: {
                                            adminData.admin_Action = Constants.AdminActionNop;
                                            adminData.adminForm = AdminFormRoot;
                                            adminData.adminContent = new ContentMetadataModel();
                                            break;
                                        }
                                    case ButtonAdd: {
                                            adminData.admin_Action = Constants.AdminActionNop;
                                            adminData.adminForm = AdminFormEdit;
                                            break;
                                        }

                                    case ButtonFind: {
                                            adminData.admin_Action = Constants.AdminActionFind;
                                            adminData.adminForm = adminData.adminSourceForm;
                                            break;
                                        }

                                    case ButtonFirst: {
                                            adminData.recordTop = 0;
                                            adminData.adminForm = adminData.adminSourceForm;
                                            break;
                                        }
                                    case ButtonPrevious: {
                                            adminData.recordTop = adminData.recordTop - adminData.recordsPerPage;
                                            if (adminData.recordTop < 0) {
                                                adminData.recordTop = 0;
                                            }
                                            adminData.admin_Action = Constants.AdminActionNop;
                                            adminData.adminForm = adminData.adminSourceForm;
                                            break;
                                        }

                                    case ButtonNext: {
                                            adminData.admin_Action = Constants.AdminActionNext;
                                            adminData.adminForm = adminData.adminSourceForm;
                                            break;
                                        }

                                    case ButtonDelete: {
                                            adminData.admin_Action = Constants.AdminActionDeleteRows;
                                            adminData.adminForm = adminData.adminSourceForm;
                                            break;
                                        }
                                    default: {
                                            // do nothing
                                            break;
                                        }
                                }
                                // end case
                                break;

                            }
                        case AdminFormEdit: {
                                //
                                // Edit Form
                                //
                                switch (adminData.requestButton) {
                                    case ButtonRefresh: {
                                            //
                                            // this is a test operation. need this so the user can set editor preferences without saving the record
                                            //   during refresh, the edit page is redrawn just was it was, but no save
                                            //
                                            adminData.admin_Action = Constants.AdminActionEditRefresh;
                                            adminData.adminForm = AdminFormEdit;
                                            break;

                                        }

                                    case ButtonMarkReviewed: {
                                            adminData.admin_Action = Constants.AdminActionMarkReviewed;
                                            adminData.adminForm = GetForm_Close(cp, adminData.ignore_legacyMenuDepth, adminData.adminContent.name, editRecord.id);
                                            break;

                                        }

                                    case ButtonSaveandInvalidateCache: {
                                            adminData.admin_Action = Constants.AdminActionReloadCDef;
                                            adminData.adminForm = AdminFormEdit;
                                            break;

                                        }

                                    case ButtonDelete: {
                                            adminData.admin_Action = Constants.AdminActionDelete;
                                            adminData.adminForm = GetForm_Close(cp, adminData.ignore_legacyMenuDepth, adminData.adminContent.name, editRecord.id);
                                            break;

                                        }

                                    case ButtonSave: {
                                            adminData.admin_Action = Constants.AdminActionSave;
                                            adminData.adminForm = AdminFormEdit;
                                            break;

                                        }

                                    case ButtonSaveAddNew: {
                                            adminData.admin_Action = Constants.AdminActionSaveAddNew;
                                            adminData.adminForm = AdminFormEdit;
                                            break;

                                        }

                                    case ButtonOK: {
                                            adminData.admin_Action = Constants.AdminActionSave;
                                            adminData.adminForm = GetForm_Close(cp, adminData.ignore_legacyMenuDepth, adminData.adminContent.name, editRecord.id);
                                            break;

                                        }

                                    case ButtonCancel: {
                                            adminData.admin_Action = Constants.AdminActionNop;
                                            adminData.adminForm = GetForm_Close(cp, adminData.ignore_legacyMenuDepth, adminData.adminContent.name, editRecord.id);
                                            break;

                                        }

                                    case ButtonSend: {
                                            //
                                            // Send a Group Email
                                            //
                                            adminData.admin_Action = Constants.AdminActionSendEmail;
                                            adminData.adminForm = AdminFormEdit;
                                            break;

                                        }

                                    case ButtonActivate: {
                                            //
                                            // Activate (submit) a conditional Email
                                            //
                                            adminData.admin_Action = Constants.AdminActionActivateEmail;
                                            adminData.adminForm = AdminFormEdit;
                                            break;

                                        }
                                    case ButtonDeactivate: {
                                            //
                                            // Deactivate (clear submit) a conditional Email
                                            //
                                            adminData.admin_Action = Constants.AdminActionDeactivateEmail;
                                            adminData.adminForm = AdminFormEdit;
                                            break;

                                        }
                                    case ButtonSendTest: {
                                            //
                                            // Test an Email (Group, System, or Conditional)
                                            //
                                            adminData.admin_Action = Constants.AdminActionSendEmailTest;
                                            adminData.adminForm = AdminFormEdit;
                                            break;
                                        }
                                    case ButtonCreateDuplicate: {
                                            //
                                            // Create a Duplicate record (for email)
                                            //
                                            adminData.admin_Action = Constants.AdminActionDuplicate;
                                            adminData.adminForm = AdminFormEdit;
                                            break;

                                        }
                                    default: {
                                            // do nothing
                                            break;
                                        }
                                }
                                break;
                            }
                        case AdminFormStyleEditor: {
                                //
                                // Process actions
                                //
                                switch (adminData.requestButton) {
                                    case ButtonSave:
                                    case ButtonOK: {
                                            //
                                            cp.core.siteProperties.setProperty("Allow CSS Reset", cp.core.docProperties.getBoolean(RequestNameAllowCSSReset));
                                            cp.core.cdnFiles.saveFile(DynamicStylesFilename, cp.core.docProperties.getText("StyleEditor"));
                                            if (cp.core.docProperties.getBoolean(RequestNameInlineStyles)) {
                                                //
                                                // Inline Styles
                                                //
                                                cp.core.siteProperties.setProperty("StylesheetSerialNumber", "0");
                                            } else {
                                                // mark to rebuild next fetch
                                                cp.core.siteProperties.setProperty("StylesheetSerialNumber", "-1");
                                            }
                                            //
                                            // delete all templateid based editorstylerule files, build on-demand
                                            //
                                            EditorStyleRulesFilename = GenericController.strReplace(EditorStyleRulesFilenamePattern, "$templateid$", "0", 1, 99, 1);
                                            cp.core.cdnFiles.deleteFile(EditorStyleRulesFilename);
                                            //
                                            using (var csData = new CsModel(cp.core)) {
                                                csData.openSql("select id from cctemplates");
                                                while (csData.ok()) {
                                                    EditorStyleRulesFilename = GenericController.strReplace(EditorStyleRulesFilenamePattern, "$templateid$", csData.getText("ID"), 1, 99, 1);
                                                    cp.core.cdnFiles.deleteFile(EditorStyleRulesFilename);
                                                    csData.goNext();
                                                }
                                                csData.close();
                                            }
                                            break;
                                        }
                                    default: {
                                            // do nothing
                                            break;
                                        }
                                }
                                switch (adminData.requestButton) {
                                    case ButtonCancel:
                                    case ButtonOK: {
                                            //
                                            // Process redirects
                                            //
                                            adminData.adminForm = AdminFormRoot;
                                            break;
                                        }
                                    default: {
                                            // do nothing
                                            break;
                                        }
                                }
                                break;
                            }
                        default: {
                                // end case
                                break;
                            }
                    }
                }
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
            }
        }
        //
        //       
        //=============================================================================================
        //   Get
        //=============================================================================================
        //
        private int GetForm_Close(CPClass cp, int MenuDepth, string ContentName, int RecordID) {
            int tempGetForm_Close = 0;
            try {
                //
                if (MenuDepth > 0) {
                    tempGetForm_Close = AdminFormClose;
                } else {
                    tempGetForm_Close = AdminFormIndex;
                }
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
            }
            return tempGetForm_Close;
        }
        //
        //=============================================================================================
        //
        //=============================================================================================
        //
        private void ProcessActionSave(CPClass cp, AdminDataModel adminData, bool UseContentWatchLink) {
            try {
                // todo
                Contensive.Processor.Addons.AdminSite.Models.EditRecordModel editRecord = adminData.editRecord;
                //
                string EditorStyleRulesFilename = null;
                //
                {
                    //
                    //
                    //
                    if (cp.core.doc.userErrorList.Count.Equals(0)) {
                        if (GenericController.toUCase(adminData.adminContent.tableName) == GenericController.toUCase("ccMembers")) {
                            //
                            //
                            SaveEditRecord(cp, adminData);
                            SaveMemberRules(cp, editRecord.id);
                        } else if (GenericController.toUCase(adminData.adminContent.tableName) == "CCEMAIL") {
                            //
                            //
                            SaveEditRecord(cp, adminData);
                        } else if (GenericController.toUCase(adminData.adminContent.tableName) == "CCCONTENT") {
                            //
                            //
                            SaveEditRecord(cp, adminData);
                            LoadAndSaveGroupRules(cp, editRecord);
                        } else if (GenericController.toUCase(adminData.adminContent.tableName) == "CCPAGECONTENT") {
                            //
                            //
                            SaveEditRecord(cp, adminData);
                            adminData.loadContentTrackingDataBase(cp.core);
                            adminData.loadContentTrackingResponse(cp.core);
                            SaveLinkAlias(cp, adminData);
                            SaveContentTracking(cp, adminData);
                        } else if (GenericController.toUCase(adminData.adminContent.tableName) == "CCLIBRARYFOLDERS") {
                            //
                            //
                            SaveEditRecord(cp, adminData);
                            adminData.loadContentTrackingDataBase(cp.core);
                            adminData.loadContentTrackingResponse(cp.core);
                            cp.core.html.processCheckList("LibraryFolderRules", adminData.adminContent.name, GenericController.encodeText(editRecord.id), "Groups", "Library Folder Rules", "FolderID", "GroupID");
                            SaveContentTracking(cp, adminData);
                        } else if (GenericController.toUCase(adminData.adminContent.tableName) == "CCSETUP") {
                            //
                            // Site Properties
                            SaveEditRecord(cp, adminData);
                            if (editRecord.nameLc.ToLowerInvariant() == "allowlinkalias") {
                                if (cp.core.siteProperties.getBoolean("AllowLinkAlias", true)) {
                                    TurnOnLinkAlias(cp, UseContentWatchLink);
                                }
                            }
                        } else if (GenericController.toUCase(adminData.adminContent.tableName) == GenericController.toUCase("ccGroups")) {
                            //
                            //
                            SaveEditRecord(cp, adminData);
                            adminData.loadContentTrackingDataBase(cp.core);
                            adminData.loadContentTrackingResponse(cp.core);
                            LoadAndSaveContentGroupRules(cp, editRecord.id);
                            SaveContentTracking(cp, adminData);
                        } else if (GenericController.toUCase(adminData.adminContent.tableName) == "CCTEMPLATES") {
                            //
                            // save and clear editorstylerules for this template
                            SaveEditRecord(cp, adminData);
                            adminData.loadContentTrackingDataBase(cp.core);
                            adminData.loadContentTrackingResponse(cp.core);
                            SaveContentTracking(cp, adminData);
                            EditorStyleRulesFilename = GenericController.strReplace(EditorStyleRulesFilenamePattern, "$templateid$", editRecord.id.ToString(), 1, 99, 1);
                            cp.core.privateFiles.deleteFile(EditorStyleRulesFilename);
                        } else {
                            //
                            //
                            SaveEditRecord(cp, adminData);
                            adminData.loadContentTrackingDataBase(cp.core);
                            adminData.loadContentTrackingResponse(cp.core);
                            SaveContentTracking(cp, adminData);
                        }
                    }
                }
                //
                // If the content supports datereviewed, mark it
                //
                if (!cp.core.doc.userErrorList.Count.Equals(0)) {
                    adminData.adminForm = adminData.adminSourceForm;
                }
                adminData.admin_Action = Constants.AdminActionNop; // convert so action can be used in as a refresh
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
            }
        }
        //
        //=============================================================================================
        //
        private void ProcessActionDuplicate(CPClass cp, AdminDataModel adminData) {
            try {
                if (cp.core.doc.userErrorList.Count.Equals(0)) {
                    switch (adminData.adminContent.tableName.ToLower(CultureInfo.InvariantCulture)) {
                        case "ccemail":
                        //
                        // --- preload array with values that may not come back in response
                        //
                        adminData.loadEditRecord(cp.core);
                        adminData.loadEditRecord_Request(cp.core);
                        //
                        if (cp.core.doc.userErrorList.Count.Equals(0)) {
                            //
                            // ----- Convert this to the Duplicate
                            //
                            if (adminData.adminContent.fields.ContainsKey("submitted")) {
                                adminData.editRecord.fieldsLc["submitted"].value = false;
                            }
                            if (adminData.adminContent.fields.ContainsKey("sent")) {
                                adminData.editRecord.fieldsLc["sent"].value = false;
                            }
                            if (adminData.adminContent.fields.ContainsKey("lastsendtestdate")) {
                                adminData.editRecord.fieldsLc["lastsendtestdate"].value = "";
                            }
                            //
                            adminData.editRecord.id = 0;
                            cp.core.doc.addRefreshQueryString("id", GenericController.encodeText(adminData.editRecord.id));
                        }
                        break;
                        default:
                        //
                        // --- preload array with values that may not come back in response
                        adminData.loadEditRecord(cp.core);
                        adminData.loadEditRecord_Request(cp.core);
                        //
                        if (cp.core.doc.userErrorList.Count.Equals(0)) {
                            //
                            // ----- Convert this to the Duplicate
                            adminData.editRecord.id = 0;
                            //
                            // block fields that should not duplicate
                            if (adminData.editRecord.fieldsLc.ContainsKey("ccguid")) {
                                adminData.editRecord.fieldsLc["ccguid"].value = "";
                            }
                            //
                            if (adminData.editRecord.fieldsLc.ContainsKey("dateadded")) {
                                adminData.editRecord.fieldsLc["dateadded"].value = DateTime.MinValue;
                            }
                            //
                            if (adminData.editRecord.fieldsLc.ContainsKey("modifieddate")) {
                                adminData.editRecord.fieldsLc["modifieddate"].value = DateTime.MinValue;
                            }
                            //
                            if (adminData.editRecord.fieldsLc.ContainsKey("modifiedby")) {
                                adminData.editRecord.fieldsLc["modifiedby"].value = 0;
                            }
                            //
                            // block fields that must be unique
                            foreach (KeyValuePair<string, Contensive.Processor.Models.Domain.ContentFieldMetadataModel> keyValuePair in adminData.adminContent.fields) {
                                ContentFieldMetadataModel field = keyValuePair.Value;
                                if (GenericController.toLCase(field.nameLc) == "email") {
                                    if ((adminData.adminContent.tableName.ToLowerInvariant() == "ccmembers") && (GenericController.encodeBoolean(cp.core.siteProperties.getBoolean("allowemaillogin", false)))) {
                                        adminData.editRecord.fieldsLc[field.nameLc].value = "";
                                    }
                                }
                                if (field.uniqueName) {
                                    adminData.editRecord.fieldsLc[field.nameLc].value = "";
                                }
                            }
                            //
                            cp.core.doc.addRefreshQueryString("id", GenericController.encodeText(adminData.editRecord.id));
                        }
                        break;
                    }
                    adminData.adminForm = adminData.adminSourceForm;
                    //
                    // convert so action can be used in as a refresh
                    adminData.admin_Action = Constants.AdminActionNop;
                }
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
            }
        }
        //
        //========================================================================
        // Read and save a GetForm_InputCheckList
        //   see GetForm_InputCheckList for an explaination of the input
        //========================================================================
        //
        private void SaveMemberRules(CPClass cp, int PeopleID) {
            try {
                //
                int GroupCount = 0;
                int GroupPointer = 0;
                int GroupId = 0;
                bool RuleNeeded = false;
                DateTime DateExpires = default(DateTime);
                object DateExpiresVariant = null;
                bool RuleActive = false;
                DateTime RuleDateExpires = default(DateTime);
                int MemberRuleId = 0;
                //
                // --- create MemberRule records for all selected
                //
                GroupCount = cp.core.docProperties.getInteger("MemberRules.RowCount");
                if (GroupCount > 0) {
                    for (GroupPointer = 0; GroupPointer < GroupCount; GroupPointer++) {
                        //
                        // ----- Read Response
                        //
                        GroupId = cp.core.docProperties.getInteger("MemberRules." + GroupPointer + ".ID");
                        RuleNeeded = cp.core.docProperties.getBoolean("MemberRules." + GroupPointer);
                        DateExpires = cp.core.docProperties.getDate("MemberRules." + GroupPointer + ".DateExpires");
                        if (DateExpires == DateTime.MinValue) {
                            DateExpiresVariant = DBNull.Value;
                        } else {
                            DateExpiresVariant = DateExpires;
                        }
                        //
                        // ----- Update Record
                        //
                        using (var csData = new CsModel(cp.core)) {
                            csData.open("Member Rules", "(MemberID=" + PeopleID + ")and(GroupID=" + GroupId + ")", "", false, 0, "Active,MemberID,GroupID,DateExpires");
                            if (!csData.ok()) {
                                //
                                // No record exists
                                //
                                if (RuleNeeded) {
                                    //
                                    // No record, Rule needed, add it
                                    //
                                    csData.insert("Member Rules");
                                    if (csData.ok()) {
                                        csData.set("Active", true);
                                        csData.set("MemberID", PeopleID);
                                        csData.set("GroupID", GroupId);
                                        csData.set("DateExpires", DateExpires);
                                    }
                                }
                            } else {
                                //
                                // Record exists
                                //
                                if (RuleNeeded) {
                                    //
                                    // record exists, and it is needed, update the DateExpires if changed
                                    //
                                    RuleActive = csData.getBoolean("active");
                                    RuleDateExpires = csData.getDate("DateExpires");
                                    if ((!RuleActive) || (RuleDateExpires != DateExpires)) {
                                        csData.set("Active", true);
                                        csData.set("DateExpires", DateExpires);
                                    }
                                } else {
                                    //
                                    // record exists and it is not needed, delete it
                                    //
                                    MemberRuleId = csData.getInteger("ID");
                                    cp.core.db.delete(MemberRuleId, "ccMemberRules");
                                }
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
            }
        }
        ////
        //========================================================================
        //
        //========================================================================
        //
        private void TurnOnLinkAlias(CPClass cp, bool UseContentWatchLink) {
            try {
                //
                string ErrorList = null;
                string linkAlias = null;
                //
                if (!cp.core.doc.userErrorList.Count.Equals(0)) {
                    Processor.Controllers.ErrorController.addUserError(cp.core, "Existing pages could not be checked for Link Alias names because there was another error on this page. Correct this error, and turn Link Alias on again to rerun the verification.");
                } else {
                    using (var csData = new CsModel(cp.core)) {
                        csData.open("Page Content");
                        while (csData.ok()) {
                            //
                            // Add the link alias
                            //
                            linkAlias = csData.getText("LinkAlias");
                            if (!string.IsNullOrEmpty(linkAlias)) {
                                //
                                // Add the link alias
                                //
                                LinkAliasController.addLinkAlias(cp.core, linkAlias, csData.getInteger("ID"), "", true, true);
                            } else {
                                //
                                // Add the name
                                //
                                linkAlias = csData.getText("name");
                                if (!string.IsNullOrEmpty(linkAlias)) {
                                    LinkAliasController.addLinkAlias(cp.core, linkAlias, csData.getInteger("ID"), "", true, false);
                                }
                            }
                            //
                            csData.goNext();
                        }
                    }
                    if (!cp.core.doc.userErrorList.Count.Equals(0)) {
                        //
                        // Throw out all the details of what happened, and add one simple error
                        //
                        ErrorList = Processor.Controllers.ErrorController.getUserError(cp.core);
                        ErrorList = GenericController.strReplace(ErrorList, UserErrorHeadline, "", 1, 99, 1);
                        Processor.Controllers.ErrorController.addUserError(cp.core, "The following errors occurred while verifying Link Alias entries for your existing pages." + ErrorList);
                    }
                }
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
            }
        }
        //
        //=================================================================================
        //
        //=================================================================================
        //
        public static void setIndexSQL_SaveIndexConfig(CPClass cp, CoreController core, IndexConfigClass IndexConfig) {
            //
            // --Find words
            string SubList = "";
            foreach (var kvp in IndexConfig.findWords) {
                IndexConfigClass.IndexConfigFindWordClass findWord = kvp.Value;
                if ((!string.IsNullOrEmpty(findWord.Name)) && (findWord.MatchOption != FindWordMatchEnum.MatchIgnore)) {
                    SubList = SubList + Environment.NewLine + findWord.Name + "\t" + findWord.Value + "\t" + (int)findWord.MatchOption;
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
                        SubList = SubList + Environment.NewLine + IndexConfig.groupList[ptr];
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
                    SubList = SubList + Environment.NewLine + column.Name + "\t" + column.Width;
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
                IndexConfigClass.IndexConfigSortClass sort = kvp.Value;
                if (!string.IsNullOrEmpty(sort.fieldName)) {
                    SubList = SubList + Environment.NewLine + sort.fieldName + "\t" + sort.direction;
                }
            }
            if (!string.IsNullOrEmpty(SubList)) {
                FilterText += Environment.NewLine + "Sorts" + SubList + Environment.NewLine;
            }
            cp.core.userProperty.setProperty(AdminDataModel.IndexConfigPrefix + encodeText(IndexConfig.contentID), FilterText);
            //

        }
    }
}
