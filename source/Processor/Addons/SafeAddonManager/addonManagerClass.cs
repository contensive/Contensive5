
using System;
using System.Xml;
using System.Collections.Generic;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Controllers.GenericController;
using static Contensive.Processor.Constants;
using Contensive.Processor.Exceptions;
using Contensive.Processor.Models.Domain;
using Contensive.Models.Db;
//
namespace Contensive.Processor.Addons.SafeAddonManager {
    public class AddonManagerClass {
        //
        // constructor sets cp from argument for use in calls to other objects, then core because cp cannot be uses since that would be a circular depenancy
        //
        private readonly CoreController core;
        //
        //==================================================================================================================================
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="cp"></param>
        /// <remarks></remarks>
        public AddonManagerClass(CoreController core) {
            this.core = core;
        }
        //
        //==========================================================================================================================================
        //   Addon Manager
        //       This is a form that lets you upload an addon
        //       Eventually, this should be substituted with a "Addon Manager Addon" - so the interface can be improved with Contensive recompile
        //==========================================================================================================================================
        //
        public string getForm_SafeModeAddonManager()  {
            string addonManager = "";
            try {
                var collectionsInstalledList = new List<string>();
                string LocalCollectionXML = null;
                bool DisplaySystem = false;
                bool DbUpToDate = false;
                string GuidFieldName = null;
                List<int> collectionsInstalledIDList = new List<int>();
                DateTime DateValue = default;
                string ErrorMessage = "";
                string OnServerGuidList = "";
                bool UpgradeOK = false;
                XmlDocument LocalCollections = null;
                XmlDocument LibCollections = null;
                string InstallFolder = null;
                int AddonNavigatorId = 0;
                string TargetCollectionName = null;
                string Collectionname = "";
                string CollectionGuid = "";
                string CollectionVersion = null;
                string CollectionDescription = "";
                string CollectionContensiveVersion = "";
                string CollectionLastChangeDate = "";
                string[,] Cells3 = null;
                string FormInput = null;
                int Cnt = 0;
                int Ptr = 0;
                StringBuilderLegacyController UploadTab = new StringBuilderLegacyController();
                StringBuilderLegacyController ModifyTab = new StringBuilderLegacyController();
                int RowPtr = 0;
                StringBuilderLegacyController Body = null;
                string[,] Cells = null;
                int ColumnCnt = 0;
                string[] ColCaption = null;
                string[] ColAlign = null;
                string[] ColWidth = null;
                bool[] ColSortable = null;
                string PostTableCopy = "";
                string BodyHTML = null;
                string UserError = null;
                StringBuilderLegacyController Content = new StringBuilderLegacyController();
                string Button = null;
                string Caption = null;
                string Description = null;
                string ButtonList = "";
                string CollectionFilename = null;
                string status = "";
                bool AllowInstallFromFolder = false;
                string InstallLibCollectionList = "";
                int TargetCollectionId = 0;
                string privateFilesInstallPath = null;
                List<string> nonCriticalErrorList = new List<string>();
                string dataBuildVersion = core.siteProperties.dataBuildVersion;
                string coreVersion = CoreController.codeVersion();
                var adminMenu = new EditTabModel();
                DbUpToDate = (dataBuildVersion == coreVersion);
                //
                Button = core.docProperties.getText(Constants.RequestNameButton);
                AllowInstallFromFolder = false;
                GuidFieldName = "ccguid";
                if (Button == Constants.ButtonCancel) {
                    //
                    // ----- redirect back to the root
                    //
                    addonManager = core.webServer.redirect("/" + core.appConfig.adminRoute, "Addon Manager, Cancel Button Pressed");
                } else {
                    if (!core.session.isAuthenticatedAdmin()) {
                        //
                        // ----- Put up error message
                        //
                        ButtonList = Constants.ButtonCancel;
                        Content.add(AdminUIController.getFormBodyAdminOnly());
                    } else {
                        //
                        InstallFolder = "temp\\CollectionUpload" + encodeText(GenericController.getRandomInteger(core));
                        privateFilesInstallPath = InstallFolder + "\\";
                        if (Button == Constants.ButtonOK) {
                            //
                            //---------------------------------------------------------------------------------------------
                            // Download and install Collections from the Collection Library
                            //---------------------------------------------------------------------------------------------
                            //
                            if (core.docProperties.getText("LibraryRow") != "") {
                                Ptr = core.docProperties.getInteger("LibraryRow");
                                CollectionGuid = core.docProperties.getText("LibraryRowguid" + Ptr);
                                InstallLibCollectionList = InstallLibCollectionList + "," + CollectionGuid;
                            }
                            //
                            //---------------------------------------------------------------------------------------------
                            // Delete collections
                            //   Before deleting each addon, make sure it is not in another collection
                            //---------------------------------------------------------------------------------------------
                            //
                            Cnt = core.docProperties.getInteger("accnt");
                            if (Cnt > 0) {
                                for (Ptr = 0; Ptr < Cnt; Ptr++) {
                                    if (core.docProperties.getBoolean("ac" + Ptr)) {
                                        TargetCollectionId = core.docProperties.getInteger("acID" + Ptr);
                                        TargetCollectionName = MetadataController.getRecordName(core, "Add-on Collections", TargetCollectionId);
                                        //
                                        // Delete any addons from this collection
                                        //
                                        MetadataController.deleteContentRecords(core, AddonModel.tableMetadata.contentName, "collectionid=" + TargetCollectionId);
                                        //
                                        // Delete the navigator entry for the collection under 'Add-ons'
                                        //
                                        if (TargetCollectionId > 0) {
                                            AddonNavigatorId = 0;
                                            using (var csData = new CsModel(core)) {
                                                csData.open(NavigatorEntryModel.tableMetadata.contentName, "name='Manage Add-ons' and ((parentid=0)or(parentid is null))");
                                                if (csData.ok()) {
                                                    AddonNavigatorId = csData.getInteger("ID");
                                                }
                                            }
                                            if (AddonNavigatorId > 0) {
                                                GetForm_SafeModeAddonManager_DeleteNavigatorBranch(TargetCollectionName, AddonNavigatorId);
                                            }
                                            //
                                            // Now delete the Collection record
                                            //
                                            MetadataController.deleteContentRecord(core, "Add-on Collections", TargetCollectionId);
                                            //
                                            // Delete Navigator Entries set as installed by the collection (this may be all that is needed)
                                            //
                                            MetadataController.deleteContentRecords(core, NavigatorEntryModel.tableMetadata.contentName, "installedbycollectionid=" + TargetCollectionId);
                                        }
                                    }
                                }
                            }
                            //
                            //---------------------------------------------------------------------------------------------
                            // Delete Add-ons
                            //---------------------------------------------------------------------------------------------
                            //
                            Cnt = core.docProperties.getInteger("aocnt");
                            if (Cnt > 0) {
                                for (Ptr = 0; Ptr < Cnt; Ptr++) {
                                    if (core.docProperties.getBoolean("ao" + Ptr)) {
                                        MetadataController.deleteContentRecord(core, AddonModel.tableMetadata.contentName, core.docProperties.getInteger("aoID" + Ptr));
                                    }
                                }
                            }
                            //
                            //---------------------------------------------------------------------------------------------
                            // Upload new collection files
                            //---------------------------------------------------------------------------------------------
                            //
                            List<string> uploadedCollectionPathFilenames = new List<string>();
                            CollectionFilename = "";
                            if (core.privateFiles.upload("MetaFile", InstallFolder, ref CollectionFilename)) {
                                status += "<br>Uploaded collection file [" + CollectionFilename + "]";
                                uploadedCollectionPathFilenames.Add(InstallFolder + CollectionFilename);
                                AllowInstallFromFolder = true;
                            }
                            //
                            int tempVar = core.docProperties.getInteger("UploadCount");
                            for (Ptr = 0; Ptr < tempVar; Ptr++) {
                                if (core.privateFiles.upload("Upload" + Ptr, InstallFolder, ref CollectionFilename)) {
                                    status += "<br>Uploaded collection file [" + CollectionFilename + "]";
                                    uploadedCollectionPathFilenames.Add(InstallFolder + CollectionFilename);
                                    AllowInstallFromFolder = true;
                                }
                            }
                        }
                        //
                        // --------------------------------------------------------------------------------
                        //   Install Library Collections
                        // --------------------------------------------------------------------------------
                        //
                        if (!string.IsNullOrEmpty(InstallLibCollectionList)) {
                            InstallLibCollectionList = InstallLibCollectionList.Substring(1);
                            string[] LibGuids = InstallLibCollectionList.Split(',');
                            Cnt = LibGuids.GetUpperBound(0) + 1;
                            for (Ptr = 0; Ptr < Cnt; Ptr++) {
                                var context = new Stack<string>();
                                context.Push("AddonManager Install Library Collection [" + LibGuids[Ptr] + "]");
                                bool isDependency = false;
                                UpgradeOK = CollectionLibraryController.installCollectionFromLibrary(core, isDependency, context, LibGuids[Ptr], ref ErrorMessage, false, true, ref nonCriticalErrorList, "AddonManagerClass.GetForm_SaveModeAddonManager", ref collectionsInstalledList);
                                if (!UpgradeOK) {
                                    //
                                    // block the reset because we will loose the error message
                                    //
                                    ErrorController.addUserError(core, "This Add-on Collection did not install correctly, " + ErrorMessage);
                                }
                            }
                        }
                        //
                        // --------------------------------------------------------------------------------
                        //   Install Manual Collections
                        // --------------------------------------------------------------------------------
                        //
                        if (AllowInstallFromFolder) {
                            if (core.privateFiles.pathExists(privateFilesInstallPath)) {
                                string logPrefix = "SafeModeAddonManager";
                                var context = new Stack<string>();
                                context.Push("AddonManager install from path [" + privateFilesInstallPath + "]");
                                var collectionsDownloaded = new List<string>();
                                UpgradeOK = CollectionInstallController.installCollectionsFromTempFolder(core, false, context, privateFilesInstallPath, ref ErrorMessage, ref collectionsInstalledList, false, true, ref nonCriticalErrorList, logPrefix, true, ref collectionsDownloaded);
                                if (!UpgradeOK) {
                                    if (string.IsNullOrEmpty(ErrorMessage)) {
                                        ErrorController.addUserError(core, "The Add-on Collection did not install correctly, but no detailed error message was given.");
                                    } else {
                                        ErrorController.addUserError(core, "The Add-on Collection did not install correctly, " + ErrorMessage);
                                    }
                                } else {
                                    foreach (string installedCollectionGuid in collectionsInstalledList) {
                                        using var csData = new CsModel(core);
                                        csData.open("Add-on Collections", GuidFieldName + "=" + DbController.encodeSQLText(installedCollectionGuid));
                                        if (csData.ok()) {
                                            collectionsInstalledIDList.Add(csData.getInteger("ID"));
                                        }
                                    }
                                }
                            }
                        }
                        //
                        //   Forward to help page
                        if ((collectionsInstalledIDList.Count > 0) && (core.doc.userErrorList.Count.Equals(0))) {
                            return core.webServer.redirect("/" + core.appConfig.adminRoute + "?helpcollectionid=" + collectionsInstalledIDList[0].ToString(), "Redirecting to help page after collection installation");
                        }
                        //
                        // Get the Collection Library tab
                        //
                        ColumnCnt = 4;
                        ColCaption = new string[4];
                        ColAlign = new string[4];
                        ColWidth = new string[4];
                        ColSortable = new bool[4];
                        Cells3 = new string[1001, 5];
                        //
                        ColCaption[0] = "Install";
                        ColAlign[0] = "center";
                        ColWidth[0] = "50";
                        ColSortable[0] = false;
                        //
                        ColCaption[1] = "Name";
                        ColAlign[1] = "left";
                        ColWidth[1] = "200";
                        ColSortable[1] = false;
                        //
                        ColCaption[2] = "Last&nbsp;Updated";
                        ColAlign[2] = "right";
                        ColWidth[2] = "200";
                        ColSortable[2] = false;
                        //
                        ColCaption[3] = "Description";
                        ColAlign[3] = "left";
                        ColWidth[3] = "99%";
                        ColSortable[3] = false;
                        //
                        LocalCollections = new XmlDocument() { XmlResolver = null };
                        LocalCollectionXML = CollectionFolderModel.getCollectionFolderConfigXml(core);
                        LocalCollections.LoadXml(LocalCollectionXML);
                        foreach (XmlNode CDef_Node in LocalCollections.DocumentElement.ChildNodes) {
                            if (GenericController.toLCase(CDef_Node.Name) == "collection") {
                                foreach (XmlNode CollectionNode in CDef_Node.ChildNodes) {
                                    if (GenericController.toLCase(CollectionNode.Name) == "guid") {
                                        OnServerGuidList += "," + CollectionNode.InnerText;
                                        break;
                                    }
                                }
                            }
                        }
                        //
                        LibCollections = new XmlDocument() { XmlResolver = null };
                        bool parseError = false;
                        try {
                            LibCollections.Load("http://support.contensive.com/GetCollectionList?iv=" + CoreController.codeVersion() + "&includeSystem=1&includeNonPublic=1");
                        } catch (Exception ex) {
                            LogController.logError(core, ex);
                            UserError = "There was an error reading the Collection Library. The site may be unavailable.";
                            HandleClassAppendLog("AddonManager", UserError);
                            status += "<br>" + UserError;
                            ErrorController.addUserError(core, UserError);
                            parseError = true;
                        }
                        Ptr = 0;
                        if (!parseError) {
                            if (GenericController.toLCase(LibCollections.DocumentElement.Name) != GenericController.toLCase(CollectionListRootNode)) {
                                UserError = "There was an error reading the Collection Library file. The '" + CollectionListRootNode + "' element was not found.";
                                HandleClassAppendLog("AddonManager", UserError);
                                status += "<br>" + UserError;
                                ErrorController.addUserError(core, UserError);
                            } else {
                                //
                                // Go through file to validate the XML, and build status message -- since service process can not communicate to user
                                //
                                RowPtr = 0;
                                foreach (XmlNode CDef_Node in LibCollections.DocumentElement.ChildNodes) {
                                    switch (GenericController.toLCase(CDef_Node.Name)) {
                                        case "collection": {
                                                //
                                                // Read the collection
                                                //
                                                foreach (XmlNode CollectionNode in CDef_Node.ChildNodes) {
                                                    switch (GenericController.toLCase(CollectionNode.Name)) {
                                                        case "name": {
                                                                //
                                                                // Name
                                                                //
                                                                Collectionname = CollectionNode.InnerText;
                                                                break;
                                                            }
                                                        case "guid": {
                                                                //
                                                                // Guid
                                                                //
                                                                CollectionGuid = CollectionNode.InnerText;
                                                                break;
                                                            }
                                                        case "version": {
                                                                //
                                                                // Version
                                                                //
                                                                CollectionVersion = CollectionNode.InnerText;
                                                                break;
                                                            }
                                                        case "description": {
                                                                //
                                                                // Version
                                                                //
                                                                CollectionDescription = CollectionNode.InnerText;
                                                                break;
                                                            }
                                                        case "contensiveversion": {
                                                                //
                                                                // Version
                                                                //
                                                                CollectionContensiveVersion = CollectionNode.InnerText;
                                                                break;
                                                            }
                                                        case "lastchangedate": {
                                                                //
                                                                // Version
                                                                //
                                                                CollectionLastChangeDate = CollectionNode.InnerText;
                                                                if (GenericController.isDate(CollectionLastChangeDate)) {
                                                                    DateValue = DateTime.Parse(CollectionLastChangeDate);
                                                                    CollectionLastChangeDate = DateValue.ToShortDateString();
                                                                }
                                                                if (string.IsNullOrEmpty(CollectionLastChangeDate)) {
                                                                    CollectionLastChangeDate = "unknown";
                                                                }
                                                                break;
                                                            }
                                                        default: {
                                                                // do nothing
                                                                break;
                                                            }
                                                    }
                                                }
                                                bool IsOnServer = false;
                                                bool IsOnSite = false;
                                                if (RowPtr >= Cells3.GetUpperBound(0)) {
                                                    string[,] tempVar2 = new string[RowPtr + 101, ColumnCnt + 1];
                                                    if (Cells3 != null) {
                                                        for (int Dimension0 = 0; Dimension0 < Cells3.GetLength(0); Dimension0++) {
                                                            int CopyLength = Math.Min(Cells3.GetLength(1), tempVar2.GetLength(1));
                                                            for (int Dimension1 = 0; Dimension1 < CopyLength; Dimension1++) {
                                                                tempVar2[Dimension0, Dimension1] = Cells3[Dimension0, Dimension1];
                                                            }
                                                        }
                                                    }
                                                    Cells3 = tempVar2;
                                                }
                                                if (string.IsNullOrEmpty(Collectionname)) {
                                                    Cells3[RowPtr, 0] = "<input TYPE=\"CheckBox\" NAME=\"LibraryRow\" VALUE=\"" + RowPtr + "\" disabled>";
                                                    Cells3[RowPtr, 1] = "no name";
                                                    Cells3[RowPtr, 2] = CollectionLastChangeDate + "&nbsp;";
                                                    Cells3[RowPtr, 3] = CollectionDescription + "&nbsp;";
                                                } else {
                                                    if (string.IsNullOrEmpty(CollectionGuid)) {
                                                        Cells3[RowPtr, 0] = "<input TYPE=\"CheckBox\" NAME=\"LibraryRow\" VALUE=\"" + RowPtr + "\" disabled>";
                                                        Cells3[RowPtr, 1] = Collectionname + " (no guid)";
                                                        Cells3[RowPtr, 2] = CollectionLastChangeDate + "&nbsp;";
                                                        Cells3[RowPtr, 3] = CollectionDescription + "&nbsp;";
                                                    } else {
                                                        IsOnServer = GenericController.encodeBoolean(OnServerGuidList.IndexOf(CollectionGuid, System.StringComparison.OrdinalIgnoreCase) + 1);
                                                        using (var csData = new CsModel(core)) {
                                                            IsOnSite = csData.open("Add-on Collections", GuidFieldName + "=" + DbController.encodeSQLText(CollectionGuid));
                                                        }
                                                        if (IsOnSite) {
                                                            //
                                                            // Already installed
                                                            //
                                                            Cells3[RowPtr, 0] = "<input TYPE=\"CheckBox\" NAME=\"LibraryRow" + RowPtr + "\" VALUE=\"1\" disabled>";
                                                            Cells3[RowPtr, 1] = Collectionname + "&nbsp;(installed already)";
                                                            Cells3[RowPtr, 2] = CollectionLastChangeDate + "&nbsp;";
                                                            Cells3[RowPtr, 3] = CollectionDescription + "&nbsp;";
                                                        } else if ((!string.IsNullOrEmpty(CollectionContensiveVersion)) && (string.CompareOrdinal(CollectionContensiveVersion, CoreController.codeVersion()) > 0)) {
                                                            //
                                                            // wrong version
                                                            //
                                                            Cells3[RowPtr, 0] = "<input TYPE=\"CheckBox\" NAME=\"LibraryRow\" VALUE=\"" + RowPtr + "\" disabled>";
                                                            Cells3[RowPtr, 1] = Collectionname + "&nbsp;(Contensive v" + CollectionContensiveVersion + " needed)";
                                                            Cells3[RowPtr, 2] = CollectionLastChangeDate + "&nbsp;";
                                                            Cells3[RowPtr, 3] = CollectionDescription + "&nbsp;";
                                                        } else if (!DbUpToDate) {
                                                            //
                                                            // Site needs to by upgraded
                                                            //
                                                            Cells3[RowPtr, 0] = "<input TYPE=\"CheckBox\" NAME=\"LibraryRow\" VALUE=\"" + RowPtr + "\" disabled>";
                                                            Cells3[RowPtr, 1] = Collectionname + "&nbsp;(install disabled)";
                                                            Cells3[RowPtr, 2] = CollectionLastChangeDate + "&nbsp;";
                                                            Cells3[RowPtr, 3] = CollectionDescription + "&nbsp;";
                                                        } else {
                                                            //
                                                            // Not installed yet
                                                            //
                                                            Cells3[RowPtr, 0] = "<input TYPE=\"CheckBox\" NAME=\"LibraryRow\" VALUE=\"" + RowPtr + "\" onClick=\"clearLibraryRows('" + RowPtr + "');\">" + HtmlController.inputHidden("LibraryRowGuid" + RowPtr, CollectionGuid) + HtmlController.inputHidden("LibraryRowName" + RowPtr, Collectionname);
                                                            Cells3[RowPtr, 1] = Collectionname + "&nbsp;";
                                                            Cells3[RowPtr, 2] = CollectionLastChangeDate + "&nbsp;";
                                                            Cells3[RowPtr, 3] = CollectionDescription + "&nbsp;";
                                                        }
                                                    }
                                                }
                                                RowPtr = RowPtr + 1;
                                                break;
                                            }
                                        default: {
                                                // do nothing
                                                break;
                                            }
                                    }
                                }
                            }
                            BodyHTML = ""
                            + "<input type=hidden name=LibraryCnt value=\"" + RowPtr + "\">"
                            + "<script language=\"JavaScript\">"
                            + "function clearLibraryRows(r) {"
                            + "var c,p;"
                            + "c=document.getElementsByName('LibraryRow');"
                                + "for (p=0;p<c.length;p++){"
                                    + "if(c[p].value!=r)c[p].checked=false;"
                                + "}"
                            + ""
                            + "}"
                            + "</script>"
                            + "<div style=\"width:100%\">" + AdminUIController.getReport2(core, RowPtr, ColCaption, ColAlign, ColWidth, Cells3, RowPtr, 1, "", PostTableCopy, RowPtr, "ccAdmin", ColSortable, 0) + "</div>"
                            + "";
                            BodyHTML = AdminUIController.getEditPanel(core, true, "Add-on Collection Library", "Select an Add-on to install from the Contensive Add-on Library. Please select only one at a time. Click OK to install the selected Add-on. The site may need to be stopped during the installation, but will be available again in approximately one minute.", BodyHTML);
                            BodyHTML = BodyHTML + HtmlController.inputHidden("AOCnt", RowPtr);
                            adminMenu.addEntry("<nobr>Collection&nbsp;Library</nobr>", BodyHTML, "ccAdminTab");
                        }
                        //
                        // --------------------------------------------------------------------------------
                        // Current Collections Tab
                        // --------------------------------------------------------------------------------
                        //
                        ColumnCnt = 2;
                        ColCaption = new string[3];
                        ColAlign = new string[3];
                        ColWidth = new string[3];
                        ColSortable = new bool[3];
                        //
                        ColCaption[0] = "Del";
                        ColAlign[0] = "center";
                        ColWidth[0] = "50";
                        ColSortable[0] = false;
                        //
                        ColCaption[1] = "Name";
                        ColAlign[1] = "left";
                        ColWidth[1] = "";
                        ColSortable[1] = false;
                        //
                        DisplaySystem = false;
                        using (var csData = new CsModel(core)) {
                            if (!core.session.isAuthenticatedDeveloper()) {
                                //
                                // non-developers
                                //
                                csData.open("Add-on Collections", "((system is null)or(system=0))", "Name");
                            } else {
                                //
                                // developers
                                //
                                DisplaySystem = true;
                                csData.open("Add-on Collections", "", "Name");
                            }
                            string[,] tempVar3 = new string[csData.getRowCount() + 1, ColumnCnt + 1];
                            if (Cells != null) {
                                for (int Dimension0 = 0; Dimension0 < Cells.GetLength(0); Dimension0++) {
                                    int CopyLength = Math.Min(Cells.GetLength(1), tempVar3.GetLength(1));
                                    for (int Dimension1 = 0; Dimension1 < CopyLength; Dimension1++) {
                                        tempVar3[Dimension0, Dimension1] = Cells[Dimension0, Dimension1];
                                    }
                                }
                            }
                            Cells = tempVar3;
                            RowPtr = 0;
                            while (csData.ok()) {
                                Cells[RowPtr, 0] = HtmlController.checkbox("AC" + RowPtr) + HtmlController.inputHidden("ACID" + RowPtr, csData.getInteger("ID"));
                                Cells[RowPtr, 1] = csData.getText("name");
                                if (DisplaySystem) {
                                    if (csData.getBoolean("system")) {
                                        Cells[RowPtr, 1] = Cells[RowPtr, 1] + " (system)";
                                    }
                                }
                                csData.goNext();
                                RowPtr = RowPtr + 1;
                            }
                            csData.close();
                        }
                        BodyHTML = "<div style=\"width:100%\">" + AdminUIController.getReport2(core, RowPtr, ColCaption, ColAlign, ColWidth, Cells, RowPtr, 1, "", PostTableCopy, RowPtr, "ccAdmin", ColSortable, 0) + "</div>";
                        BodyHTML = AdminUIController.getEditPanel(core, true, "Add-on Collections", "Use this form to review and delete current add-on collections.", BodyHTML);
                        BodyHTML = BodyHTML + HtmlController.inputHidden("accnt", RowPtr);
                        adminMenu.addEntry("Installed&nbsp;Collections", BodyHTML, "ccAdminTab");
                        //
                        // --------------------------------------------------------------------------------
                        // Get the Upload Add-ons tab
                        // --------------------------------------------------------------------------------
                        //
                        Body = new StringBuilderLegacyController();
                        if (!DbUpToDate) {
                            Body.add("<p>Add-on upload is disabled because your site database needs to be updated.</p>");
                        } else {
                            FormInput = ""
                                + "<table id=\"UploadInsert\" border=\"0\" cellpadding=\"0\" cellspacing=\"1\" width=\"100%\">"
                                + "</table>"
                                + "<table border=\"0\" cellpadding=\"0\" cellspacing=\"1\" width=\"100%\">"
                                + "<tr><td align=\"left\"><a href=\"#\" onClick=\"InsertUpload(); return false;\">+ Add more files</a></td></tr>"
                                + "</table>"
                                + HtmlController.inputHidden("UploadCount", 1, "", "UploadCount") + "";
                            Body.add(AdminUIController.editTable(""
                                + AdminUIController.getEditRowLegacy(core, HtmlController.inputFile("MetaFile"), "Add-on Collection File(s)", "", true, false, "")
                                + AdminUIController.getEditRowLegacy(core, FormInput, "&nbsp;", "", true, false, "")
                                ));
                        }
                        adminMenu.addEntry("Add&nbsp;Manually", AdminUIController.getEditPanel(core, true, "Install or Update an Add-on Collection.", "Use this form to upload a new or updated Add-on Collection to your site. A collection file can be a single xml configuration file, a single zip file containing the configuration file and other resource files, or a configuration with other resource files uploaded separately. Use the 'Add more files' link to add as many files as you need. When you hit OK, the Collection will be checked, and only submitted if all files are uploaded.", Body.text), "ccAdminTab");
                        //
                        // --------------------------------------------------------------------------------
                        // Build Page from tabs
                        // --------------------------------------------------------------------------------
                        //
                        Content.add(adminMenu.getTabs(core));
                        //
                        ButtonList = ButtonCancel + "," + ButtonOK;
                        Content.add(HtmlController.inputHidden(RequestNameAdminSourceForm, AdminFormLegacyAddonManager));
                    }
                    //
                    // Output the Add-on
                    //
                    Caption = "Add-on Manager (Safe Mode)";
                    Description = "<div>Use the add-on manager to add and remove Add-ons from your Contensive installation.</div>";
                    if (!DbUpToDate) {
                        Description = Description + "<div style=\"Margin-left:50px\">The Add-on Manager is disabled because this site's Database needs to be upgraded.</div>";
                    }
                    if (nonCriticalErrorList.Count > 0) {
                        status += "<ul>";
                        foreach (string item in nonCriticalErrorList) {
                            status += "<li>" + item + "</li>";
                        }
                        status += "</ul>";
                    }
                    if (!string.IsNullOrEmpty(status)) {
                        Description = Description + "<div style=\"Margin-left:50px\">" + status + "</div>";
                    }
                    addonManager = AdminUIController.getToolBody(core, Caption, ButtonList, "", false, false, Description, "", 0, Content.text);
                    core.html.addTitle("Add-on Manager");
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
            return addonManager;
        }
        //
        //
        //
        private void GetForm_SafeModeAddonManager_DeleteNavigatorBranch(string EntryName, int EntryParentID) {
            try {
                int EntryID = 0;
                using (var csData = new CsModel(core)) {
                    if (EntryParentID == 0) {
                        csData.open(NavigatorEntryModel.tableMetadata.contentName, "(name=" + DbController.encodeSQLText(EntryName) + ")and((parentID is null)or(parentid=0))");
                    } else {
                        csData.open(NavigatorEntryModel.tableMetadata.contentName, "(name=" + DbController.encodeSQLText(EntryName) + ")and(parentID=" + DbController.encodeSQLNumber(EntryParentID) + ")");
                    }
                    if (csData.ok()) {
                        EntryID = csData.getInteger("ID");
                    }
                    csData.close();
                }
                //
                if (EntryID != 0) {
                    using (var csData = new CsModel(core)) {
                        csData.open(NavigatorEntryModel.tableMetadata.contentName, "(parentID=" + DbController.encodeSQLNumber(EntryID) + ")");
                        while (csData.ok()) {
                            GetForm_SafeModeAddonManager_DeleteNavigatorBranch(csData.getText("name"), EntryID);
                            csData.goNext();
                        }
                        csData.close();
                    }
                    MetadataController.deleteContentRecord(core, NavigatorEntryModel.tableMetadata.contentName, EntryID);
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
        }
        //
        //========================================================================
        // ----- Get an XML nodes attribute based on its name
        //========================================================================
        //
        private string GetXMLAttribute(bool Found, XmlNode Node, string Name, string DefaultIfNotFound) {
            string tempGetXMLAttribute = null;
            try {
                //
                // todo  NOTE: Commented this declaration since looping variables in 'foreach' loops are declared in the 'foreach' header in C#:
                XmlNode ResultNode = null;
                string UcaseName = null;
                //
                Found = false;
                ResultNode = Node.Attributes.GetNamedItem(Name);
                if (ResultNode == null) {
                    UcaseName = GenericController.toUCase(Name);
                    foreach (XmlAttribute NodeAttribute in Node.Attributes) {
                        if (GenericController.toUCase(NodeAttribute.Name) == UcaseName) {
                            tempGetXMLAttribute = NodeAttribute.Value;
                            Found = true;
                            break;
                        }
                    }
                } else {
                    tempGetXMLAttribute = ResultNode.Value;
                    Found = true;
                }
                if (!Found) {
                    tempGetXMLAttribute = DefaultIfNotFound;
                }
                return tempGetXMLAttribute;
                //
                // ----- Error Trap
                //
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
            HandleClassTrapError("GetXMLAttribute");
            return tempGetXMLAttribute;
        }
        //
        //
        //
        private void HandleClassAppendLog(string MethodName, string Context) {
            LogController.logTrace(core, "addonManager: " + Context);
        }
        //
        //===========================================================================
        //
        //===========================================================================
        //
        private void HandleClassTrapError(string MethodName, string Context = "context unknown") {
            throw new GenericException("Unexpected exception in method [" + MethodName + "], cause [" + Context + "]");
        }
        //
        //
        //
        public int getParentIDFromNameSpace(string ContentName, string menuNameSpace) {
            int tempGetParentIDFromNameSpace = 0;
            try {
                string ParentNameSpace = null;
                string ParentName = null;
                int ParentId = 0;
                int Pos = 0;
                //
                tempGetParentIDFromNameSpace = 0;
                if (!string.IsNullOrEmpty(menuNameSpace)) {
                    Pos = GenericController.strInstr(1, menuNameSpace, ".");
                    if (Pos == 0) {
                        ParentName = menuNameSpace;
                        ParentNameSpace = "";
                    } else {
                        ParentName = menuNameSpace.Substring(Pos);
                        ParentNameSpace = menuNameSpace.left(Pos - 1);
                    }
                    if (string.IsNullOrEmpty(ParentNameSpace)) {
                        using var csData = new CsModel(core);
                        csData.open(ContentName, "(name=" + DbController.encodeSQLText(ParentName) + ")and((parentid is null)or(parentid=0))", "ID", false, 0, "ID");
                        if (csData.ok()) {
                            tempGetParentIDFromNameSpace = csData.getInteger("ID");
                        }
                        csData.close();
                    } else {
                        ParentId = getParentIDFromNameSpace(ContentName, ParentNameSpace);
                        using var csData = new CsModel(core);
                        csData.open(ContentName, "(name=" + DbController.encodeSQLText(ParentName) + ")and(parentid=" + ParentId + ")", "ID", false, 0, "ID");
                        if (csData.ok()) {
                            tempGetParentIDFromNameSpace = csData.getInteger("ID");
                        }
                    }
                }
                //
                return tempGetParentIDFromNameSpace;
                //
                // ----- Error Trap
                //
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
            HandleClassTrapError("GetParentIDFromNameSpace");
            return tempGetParentIDFromNameSpace;
        }
    }
}
