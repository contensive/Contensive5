
using System;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Controllers.GenericController;
using static Contensive.Processor.Constants;
using System.Collections.Generic;
using System.Data;
using Contensive.BaseClasses;
using System.Linq;
using Contensive.Exceptions;
//
namespace Contensive.Processor.Addons.Tools {
    //
    public class ConfigureListClass : Contensive.BaseClasses.AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// addon method, deliver complete Html admin site
        /// blank return on OK or cancel button
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cpBase) {
            return get(((CPClass)cpBase).core);
        }
        //
        //====================================================================================================
        //
        public static  string get(CoreController core) {
            string result = "";
            try {
                string Button = core.docProperties.getText("Button");
                if (Button == ButtonCancelAll) {
                    //
                    // Cancel to the admin site
                    return core.webServer.redirect(core.appConfig.adminRoute, "Tools-List, cancel button");
                }
                //
                const string RequestNameAddField = "addfield";
                const string RequestNameAddFieldId = "addfieldID";
                StringBuilderLegacyController Stream = new StringBuilderLegacyController();
                Stream.add(AdminUIController.getHeaderTitleDescription("Configure Admin Listing", "Configure the Administration Content Listing Page."));
                //
                //   Load Request
                int ToolsAction = core.docProperties.getInteger("dta");
                int TargetFieldID = core.docProperties.getInteger("fi");
                int ContentId = core.docProperties.getInteger(RequestNameToolContentId);
                string FieldNameToAdd = GenericController.toUCase(core.docProperties.getText(RequestNameAddField));
                int FieldIDToAdd = core.docProperties.getInteger(RequestNameAddFieldId);
                string ButtonList = ButtonCancel + "," + ButtonSelect;
                bool ReloadCDef = core.docProperties.getBoolean("ReloadCDef");
                bool AllowContentAutoLoad = false;
                //
                //--------------------------------------------------------------------------------
                // Process actions
                //--------------------------------------------------------------------------------
                //
                if (ContentId != 0) {
                    ButtonList = ButtonCancel + "," + ButtonSaveandInvalidateCache;
                    string ContentName = Local_GetContentNameByID(core, ContentId);
                    Processor.Models.Domain.ContentMetadataModel CDef = Processor.Models.Domain.ContentMetadataModel.create(core, ContentId, false, true);
                    string FieldName = null;
                    int ColumnWidthTotal = 0;
                    int fieldId = 0;
                    if (ToolsAction != 0) {
                        //
                        // Block contentautoload, then force a load at the end
                        //
                        AllowContentAutoLoad = (core.siteProperties.getBoolean("AllowContentAutoLoad", true));
                        core.siteProperties.setProperty("AllowContentAutoLoad", false);
                        int SourceContentId = 0;
                        string SourceName = null;
                        //
                        // Make sure the FieldNameToAdd is not-inherited, if not, create new field
                        //
                        if (FieldIDToAdd != 0) {
                            foreach (var keyValuePair in CDef.fields) {
                                Processor.Models.Domain.ContentFieldMetadataModel field = keyValuePair.Value;
                                if (field.id == FieldIDToAdd) {
                                    if (field.inherited) {
                                        SourceContentId = field.contentId;
                                        SourceName = field.nameLc;
                                        using (var CSSource = new CsModel(core)) {
                                            CSSource.open("Content Fields", "(ContentID=" + SourceContentId + ")and(Name=" + DbController.encodeSQLText(SourceName) + ")");
                                            if (CSSource.ok()) {
                                                using (var CSTarget = new CsModel(core)) {
                                                    CSTarget.insert("Content Fields");
                                                    if (CSTarget.ok()) {
                                                        CSSource.copyRecord(CSTarget);
                                                        CSTarget.set("ContentID", ContentId);
                                                        ReloadCDef = true;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                        //
                        // Make sure all fields are not-inherited, if not, create new fields
                        //
                        int ColumnNumberMax = 0;
                        foreach (var keyValuePair in CDef.adminColumns) {
                            Processor.Models.Domain.ContentMetadataModel.MetaAdminColumnClass adminColumn = keyValuePair.Value;
                            Processor.Models.Domain.ContentFieldMetadataModel field = CDef.fields[adminColumn.Name];
                            if (field.inherited) {
                                SourceContentId = field.contentId;
                                SourceName = field.nameLc;
                                using (var CSSource = new CsModel(core)) {
                                    if (CSSource.open("Content Fields", "(ContentID=" + SourceContentId + ")and(Name=" + DbController.encodeSQLText(SourceName) + ")")) {
                                        using (var CSTarget = new CsModel(core)) {
                                            if (CSTarget.insert("Content Fields")) {
                                                CSSource.copyRecord(CSTarget);
                                                CSTarget.set("ContentID", ContentId);
                                                ReloadCDef = true;
                                            }
                                        }
                                    }
                                }
                            }
                            if (ColumnNumberMax < field.indexColumn) {
                                ColumnNumberMax = field.indexColumn;
                            }
                            ColumnWidthTotal += adminColumn.Width;
                        }
                        //
                        // ----- Perform any actions first
                        //
                        int columnPtr = 0;
                        bool MoveNextColumn = false;
                        switch (ToolsAction) {
                            case ToolsActionAddField: {
                                    //
                                    // Add a field to the Listing Page
                                    //
                                    if (FieldIDToAdd != 0) {
                                        columnPtr = 0;
                                        if (CDef.adminColumns.Count > 1) {
                                            foreach (var keyValuePair in CDef.adminColumns) {
                                                Processor.Models.Domain.ContentMetadataModel.MetaAdminColumnClass adminColumn = keyValuePair.Value;
                                                Processor.Models.Domain.ContentFieldMetadataModel field = CDef.fields[adminColumn.Name];
                                                using (var csData = new CsModel(core)) {
                                                    csData.openRecord("Content Fields", field.id);
                                                    csData.set("IndexColumn", (columnPtr) * 10);
                                                    csData.set("IndexWidth", Math.Floor((adminColumn.Width * 80) / (double)ColumnWidthTotal));
                                                }
                                                columnPtr += 1;
                                            }
                                        }
                                        using (var csData = new CsModel(core)) {
                                            if (csData.openRecord("Content Fields", FieldIDToAdd)) {
                                                csData.set("IndexColumn", columnPtr * 10);
                                                csData.set("IndexWidth", 20);
                                                csData.set("IndexSortPriority", 99);
                                                csData.set("IndexSortDirection", 1);
                                            }
                                        }
                                        ReloadCDef = true;
                                    }
                                    //
                                    break;
                                }
                            case ToolsActionRemoveField: {
                                    //
                                    // Remove a field to the Listing Page
                                    //
                                    if (CDef.adminColumns.Count > 1) {
                                        columnPtr = 0;
                                        foreach (var keyValuePair in CDef.adminColumns) {
                                            Processor.Models.Domain.ContentMetadataModel.MetaAdminColumnClass adminColumn = keyValuePair.Value;
                                            Processor.Models.Domain.ContentFieldMetadataModel field = CDef.fields[adminColumn.Name];
                                            using (var csData = new CsModel(core)) {
                                                csData.openRecord("Content Fields", field.id);
                                                if (fieldId == TargetFieldID) {
                                                    csData.set("IndexColumn", 0);
                                                    csData.set("IndexWidth", 0);
                                                    csData.set("IndexSortPriority", 0);
                                                    csData.set("IndexSortDirection", 0);
                                                } else {
                                                    csData.set("IndexColumn", (columnPtr) * 10);
                                                    csData.set("IndexWidth", Math.Floor((adminColumn.Width * 100) / (double)ColumnWidthTotal));
                                                }
                                            }
                                            columnPtr += 1;
                                        }
                                        ReloadCDef = true;
                                    }
                                    break;
                                }
                            case ToolsActionMoveFieldRight: {
                                    //
                                    // Move column field right
                                    //
                                    if (CDef.adminColumns.Count > 1) {
                                        MoveNextColumn = false;
                                        columnPtr = 0;
                                        foreach (var keyValuePair in CDef.adminColumns) {
                                            Processor.Models.Domain.ContentMetadataModel.MetaAdminColumnClass adminColumn = keyValuePair.Value;
                                            Processor.Models.Domain.ContentFieldMetadataModel field = CDef.fields[adminColumn.Name];
                                            FieldName = adminColumn.Name;
                                            using (var csData = new CsModel(core)) {
                                                csData.openRecord("Content Fields", field.id);
                                                if ((CDef.fields[FieldName.ToLowerInvariant()].id == TargetFieldID) && (columnPtr < CDef.adminColumns.Count)) {
                                                    csData.set("IndexColumn", (columnPtr + 1) * 10);
                                                    //
                                                    MoveNextColumn = true;
                                                } else if (MoveNextColumn) {
                                                    //
                                                    // This is one past target
                                                    //
                                                    csData.set("IndexColumn", (columnPtr - 1) * 10);
                                                    MoveNextColumn = false;
                                                } else {
                                                    //
                                                    // not target or one past target
                                                    //
                                                    csData.set("IndexColumn", (columnPtr) * 10);
                                                    MoveNextColumn = false;
                                                }
                                                csData.set("IndexWidth", Math.Floor((adminColumn.Width * 100) / (double)ColumnWidthTotal));
                                            }
                                            columnPtr += 1;
                                        }
                                        ReloadCDef = true;
                                    }
                                    // end case
                                    break;
                                }
                            case ToolsActionMoveFieldLeft: {
                                    //
                                    // Move Index column field left
                                    //
                                    if (CDef.adminColumns.Count > 1) {
                                        MoveNextColumn = false;
                                        columnPtr = 0;
                                        foreach (var keyValuePair in CDef.adminColumns.Reverse()) {
                                            Processor.Models.Domain.ContentMetadataModel.MetaAdminColumnClass adminColumn = keyValuePair.Value;
                                            Processor.Models.Domain.ContentFieldMetadataModel field = CDef.fields[adminColumn.Name];
                                            FieldName = adminColumn.Name;
                                            using (var csData = new CsModel(core)) {
                                                csData.openRecord("Content Fields", field.id);
                                                if ((field.id == TargetFieldID) && (columnPtr < CDef.adminColumns.Count)) {
                                                    csData.set("IndexColumn", (columnPtr - 1) * 10);
                                                    //
                                                    MoveNextColumn = true;
                                                } else if (MoveNextColumn) {
                                                    //
                                                    // This is one past target
                                                    //
                                                    csData.set("IndexColumn", (columnPtr + 1) * 10);
                                                    MoveNextColumn = false;
                                                } else {
                                                    //
                                                    // not target or one past target
                                                    //
                                                    csData.set("IndexColumn", (columnPtr) * 10);
                                                    MoveNextColumn = false;
                                                }
                                                csData.set("IndexWidth", Math.Floor((adminColumn.Width * 100) / (double)ColumnWidthTotal));
                                            }
                                            columnPtr += 1;
                                        }
                                        ReloadCDef = true;
                                    }
                                    break;
                                }
                            default: {
                                    // do nothing
                                    break;
                                }
                        }
                        //
                        // Get a new copy of the content definition
                        //
                        CDef = Processor.Models.Domain.ContentMetadataModel.create(core, ContentId, false, true);
                    }
                    if (Button == ButtonSaveandInvalidateCache) {
                        core.cache.invalidateAll();
                        core.cacheRuntime.clear();
                        return core.webServer.redirect("?af=" + AdminFormToolConfigureListing + "&ContentID=" + ContentId, "Tools-ConfigureListing, Save and Invalidate Cache, Go to back ConfigureListing tools");
                    }
                    //
                    //--------------------------------------------------------------------------------
                    //   Display the form
                    //--------------------------------------------------------------------------------
                    //
                    if (!string.IsNullOrEmpty(ContentName)) {
                        Stream.add("<br><br><B>" + ContentName + "</b><br>");
                    }
                    Stream.add("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" width=\"99%\"><tr>");
                    Stream.add("<td width=\"5%\">&nbsp;</td>");
                    Stream.add("<td width=\"9%\" align=\"center\" class=\"ccAdminSmall\"><nobr>10%</nobr></td>");
                    Stream.add("<td width=\"9%\" align=\"center\" class=\"ccAdminSmall\"><nobr>20%</nobr></td>");
                    Stream.add("<td width=\"9%\" align=\"center\" class=\"ccAdminSmall\"><nobr>30%</nobr></td>");
                    Stream.add("<td width=\"9%\" align=\"center\" class=\"ccAdminSmall\"><nobr>40%</nobr></td>");
                    Stream.add("<td width=\"9%\" align=\"center\" class=\"ccAdminSmall\"><nobr>50%</nobr></td>");
                    Stream.add("<td width=\"9%\" align=\"center\" class=\"ccAdminSmall\"><nobr>60%</nobr></td>");
                    Stream.add("<td width=\"9%\" align=\"center\" class=\"ccAdminSmall\"><nobr>70%</nobr></td>");
                    Stream.add("<td width=\"9%\" align=\"center\" class=\"ccAdminSmall\"><nobr>80%</nobr></td>");
                    Stream.add("<td width=\"9%\" align=\"center\" class=\"ccAdminSmall\"><nobr>90%</nobr></td>");
                    Stream.add("<td width=\"9%\" align=\"center\" class=\"ccAdminSmall\"><nobr>100%</nobr></td>");
                    Stream.add("<td width=\"4%\" align=\"center\">&nbsp;</td>");
                    Stream.add("</tr></TABLE>");
                    //
                    Stream.add("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" width=\"99%\"><tr>");
                    Stream.add("<td width=\"9%\"><nobr><IMG src=\"" + cdnPrefix + "images/black.gif\" width=\"1\" height=\"10\"><IMG alt=\"\" src=\"" + cdnPrefix + "Images/spacer.gif\" width=\"100%\" height=\"10\"></nobr></td>");
                    Stream.add("<td width=\"9%\"><nobr><IMG src=\"" + cdnPrefix + "images/black.gif\" width=\"1\" height=\"10\"><IMG alt=\"\" src=\"" + cdnPrefix + "Images/spacer.gif\" width=\"100%\" height=\"10\"></nobr></td>");
                    Stream.add("<td width=\"9%\"><nobr><IMG src=\"" + cdnPrefix + "images/black.gif\" width=\"1\" height=\"10\"><IMG alt=\"\" src=\"" + cdnPrefix + "Images/spacer.gif\" width=\"100%\" height=\"10\"></nobr></td>");
                    Stream.add("<td width=\"9%\"><nobr><IMG src=\"" + cdnPrefix + "images/black.gif\" width=\"1\" height=\"10\"><IMG alt=\"\" src=\"" + cdnPrefix + "Images/spacer.gif\" width=\"100%\" height=\"10\"></nobr></td>");
                    Stream.add("<td width=\"9%\"><nobr><IMG src=\"" + cdnPrefix + "images/black.gif\" width=\"1\" height=\"10\"><IMG alt=\"\" src=\"" + cdnPrefix + "Images/spacer.gif\" width=\"100%\" height=\"10\"></nobr></td>");
                    Stream.add("<td width=\"9%\"><nobr><IMG src=\"" + cdnPrefix + "images/black.gif\" width=\"1\" height=\"10\"><IMG alt=\"\" src=\"" + cdnPrefix + "Images/spacer.gif\" width=\"100%\" height=\"10\"></nobr></td>");
                    Stream.add("<td width=\"9%\"><nobr><IMG src=\"" + cdnPrefix + "images/black.gif\" width=\"1\" height=\"10\"><IMG alt=\"\" src=\"" + cdnPrefix + "Images/spacer.gif\" width=\"100%\" height=\"10\"></nobr></td>");
                    Stream.add("<td width=\"9%\"><nobr><IMG src=\"" + cdnPrefix + "images/black.gif\" width=\"1\" height=\"10\"><IMG alt=\"\" src=\"" + cdnPrefix + "Images/spacer.gif\" width=\"100%\" height=\"10\"></nobr></td>");
                    Stream.add("<td width=\"9%\"><nobr><IMG src=\"" + cdnPrefix + "images/black.gif\" width=\"1\" height=\"10\"><IMG alt=\"\" src=\"" + cdnPrefix + "Images/spacer.gif\" width=\"100%\" height=\"10\"></nobr></td>");
                    Stream.add("<td width=\"9%\"><nobr><IMG src=\"" + cdnPrefix + "images/black.gif\" width=\"1\" height=\"10\"><IMG alt=\"\" src=\"" + cdnPrefix + "Images/spacer.gif\" width=\"100%\" height=\"10\"></nobr></td>");
                    Stream.add("<td width=\"9%\"><nobr><IMG src=\"" + cdnPrefix + "images/black.gif\" width=\"1\" height=\"10\"><IMG alt=\"\" src=\"" + cdnPrefix + "Images/spacer.gif\" width=\"100%\" height=\"10\"></nobr></td>");
                    Stream.add("</tr></TABLE>");
                    //
                    // print the column headers
                    //
                    ColumnWidthTotal = 0;
                    int InheritedFieldCount = 0;
                    if (CDef.adminColumns.Count > 0) {
                        //
                        // Calc total width
                        //
                        foreach (KeyValuePair<string, Processor.Models.Domain.ContentMetadataModel.MetaAdminColumnClass> kvp in CDef.adminColumns) {
                            ColumnWidthTotal += kvp.Value.Width;
                        }
                        if (ColumnWidthTotal > 0) {
                            Stream.add("<table border=\"0\" cellpadding=\"5\" cellspacing=\"0\" width=\"90%\">");
                            int ColumnCount = 0;
                            foreach (KeyValuePair<string, Processor.Models.Domain.ContentMetadataModel.MetaAdminColumnClass> kvp in CDef.adminColumns) {
                                //
                                // print column headers - anchored so they sort columns
                                //
                                int ColumnWidth = encodeInteger(100 * (kvp.Value.Width / (double)ColumnWidthTotal));
                                FieldName = kvp.Value.Name;
                                var tempVar = CDef.fields[FieldName.ToLowerInvariant()];
                                fieldId = tempVar.id;
                                string Caption = tempVar.caption;
                                if (tempVar.inherited) {
                                    Caption += "*";
                                    InheritedFieldCount += 1;
                                }
                                string AStart = "<A href=\"" + core.webServer.requestPage + "?" + RequestNameToolContentId + "=" + ContentId + "&af=" + AdminFormToolConfigureListing + "&fi=" + fieldId + "&dtcn=" + ColumnCount;
                                Stream.add("<td width=\"" + ColumnWidth + "%\" valign=\"top\" align=\"left\">" + SpanClassAdminNormal + Caption + "<br>");
                                Stream.add("<IMG src=\"" + cdnPrefix + "images/black.GIF\" width=\"100%\" height=\"1\">");
                                Stream.add(AStart + "&dta=" + ToolsActionRemoveField + "\"><IMG src=\"" + cdnPrefix + "images/LibButtonDeleteUp.gif\" width=\"50\" height=\"15\" border=\"0\"></A><br>");
                                Stream.add(AStart + "&dta=" + ToolsActionMoveFieldRight + "\"><IMG src=\"" + cdnPrefix + "images/LibButtonMoveRightUp.gif\" width=\"50\" height=\"15\" border=\"0\"></A><br>");
                                Stream.add(AStart + "&dta=" + ToolsActionMoveFieldLeft + "\"><IMG src=\"" + cdnPrefix + "images/LibButtonMoveLeftUp.gif\" width=\"50\" height=\"15\" border=\"0\"></A><br>");
                                Stream.add(AStart + "&dta=" + ToolsActionSetAZ + "\"><IMG src=\"" + cdnPrefix + "images/LibButtonSortazUp.gif\" width=\"50\" height=\"15\" border=\"0\"></A><br>");
                                Stream.add(AStart + "&dta=" + ToolsActionSetZA + "\"><IMG src=\"" + cdnPrefix + "images/LibButtonSortzaUp.gif\" width=\"50\" height=\"15\" border=\"0\"></A><br>");
                                Stream.add(AStart + "&dta=" + ToolsActionExpand + "\"><IMG src=\"" + cdnPrefix + "images/LibButtonOpenUp.gif\" width=\"50\" height=\"15\" border=\"0\"></A><br>");
                                Stream.add(AStart + "&dta=" + ToolsActionContract + "\"><IMG src=\"" + cdnPrefix + "images/LibButtonCloseUp.gif\" width=\"50\" height=\"15\" border=\"0\"></A>");
                                Stream.add("</SPAN></td>");
                                ColumnCount += 1;
                            }
                            Stream.add("</tr>");
                            Stream.add("</TABLE>");
                        }
                    }
                    //
                    // ----- If anything was inherited, put up the message
                    //
                    if (InheritedFieldCount > 0) {
                        Stream.add("<P class=\"ccNormal\">* This field was inherited from the Content Definition's Parent. Inherited fields will automatically change when the field in the parent is changed. If you alter these settings, this connection will be broken, and the field will no longer inherit it's properties.</P class=\"ccNormal\">");
                    }
                    //
                    // ----- now output a list of fields to add
                    //
                    if (CDef.fields.Count == 0) {
                        Stream.add(SpanClassAdminNormal + "This Content Definition has no fields</SPAN><br>");
                    } else {
                        Stream.add(SpanClassAdminNormal + "<br>");
                        bool skipField = false;
                        foreach (KeyValuePair<string, Processor.Models.Domain.ContentFieldMetadataModel> keyValuePair in CDef.fields) {
                            Processor.Models.Domain.ContentFieldMetadataModel field = keyValuePair.Value;
                            //
                            // test if this column is in use
                            //
                            skipField = false;
                            if (CDef.adminColumns.Count > 0) {
                                foreach (KeyValuePair<string, Processor.Models.Domain.ContentMetadataModel.MetaAdminColumnClass> kvp in CDef.adminColumns) {
                                    if (field.nameLc == kvp.Value.Name) {
                                        skipField = true;
                                        break;
                                    }
                                }
                            }
                            //
                            // display the column if it is not in use
                            //
                            if (skipField) {
                                if (field.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.FileText) {
                                    //
                                    // text filename can not be search
                                    //
                                    Stream.add("<IMG src=\"" + cdnPrefix + "images/Spacer.gif\" width=\"50\" height=\"15\" border=\"0\"> " + field.caption + " (text file field)<br>");
                                } else if (field.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.FileCSS) {
                                    //
                                    // text filename can not be search
                                    //
                                    Stream.add("<IMG src=\"" + cdnPrefix + "images/Spacer.gif\" width=\"50\" height=\"15\" border=\"0\"> " + field.caption + " (css file field)<br>");
                                } else if (field.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.FileXML) {
                                    //
                                    // text filename can not be search
                                    //
                                    Stream.add("<IMG src=\"" + cdnPrefix + "images/Spacer.gif\" width=\"50\" height=\"15\" border=\"0\"> " + field.caption + " (xml file field)<br>");
                                } else if (field.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.FileJavascript) {
                                    //
                                    // text filename can not be search
                                    //
                                    Stream.add("<IMG src=\"" + cdnPrefix + "images/Spacer.gif\" width=\"50\" height=\"15\" border=\"0\"> " + field.caption + " (javascript file field)<br>");
                                } else if (field.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.LongText) {
                                    //
                                    // long text can not be search
                                    //
                                    Stream.add("<IMG src=\"" + cdnPrefix + "images/Spacer.gif\" width=\"50\" height=\"15\" border=\"0\"> " + field.caption + " (long text field)<br>");
                                } else if (field.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.FileImage) {
                                    //
                                    // long text can not be search
                                    //
                                    Stream.add("<IMG src=\"" + cdnPrefix + "images/Spacer.gif\" width=\"50\" height=\"15\" border=\"0\"> " + field.caption + " (image field)<br>");
                                } else if (field.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.Redirect) {
                                    //
                                    // long text can not be search
                                    //
                                    Stream.add("<IMG src=\"" + cdnPrefix + "images/Spacer.gif\" width=\"50\" height=\"15\" border=\"0\"> " + field.caption + " (redirect field)<br>");
                                } else {
                                    //
                                    // can be used as column header
                                    //
                                    Stream.add("<A href=\"" + core.webServer.requestPage + "?" + RequestNameToolContentId + "=" + ContentId + "&af=" + AdminFormToolConfigureListing + "&fi=" + field.id + "&dta=" + ToolsActionAddField + "&" + RequestNameAddFieldId + "=" + field.id + "\"><IMG src=\"" + cdnPrefix + "images/LibButtonAddUp.gif\" width=\"50\" height=\"15\" border=\"0\"></A> " + field.caption + "<br>");
                                }
                            }
                        }
                    }
                }
                //
                //--------------------------------------------------------------------------------
                // print the content tables that have Listing Pages to Configure
                //--------------------------------------------------------------------------------
                //
                string FormPanel = SpanClassAdminNormal + "Select a Content Definition to Configure its Listing Page<br>";
                FormPanel += core.html.selectFromContent("ContentID", ContentId, "Content");
                Stream.add(core.html.getPanel(FormPanel));
                core.siteProperties.setProperty("AllowContentAutoLoad", AllowContentAutoLoad);
                Stream.add(Controllers.HtmlController.inputHidden("ReloadCDef", ReloadCDef));
                result = AdminUIController.getToolForm(core, Stream.text, ButtonList);
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
            return result;
        }
        //
        //=============================================================================
        //   Get a ContentID from the ContentName using just the tables
        //=============================================================================
        //
        private static string Local_GetContentNameByID(CoreController core, int ContentID) {
            string tempLocal_GetContentNameById = null;
            try {
                //
                DataTable dt = null;
                //
                tempLocal_GetContentNameById = "";
                dt = core.db.executeQuery("Select name from ccContent where id=" + ContentID);
                if (dt.Rows.Count > 0) {
                    tempLocal_GetContentNameById = GenericController.encodeText(dt.Rows[0][0]);
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
            return tempLocal_GetContentNameById;
        }
        //
        //=============================================================================
        // Normalize the Index page Columns, setting proper values for IndexColumn, etc.
        //=============================================================================
        //
        private static void NormalizeIndexColumns(CoreController core, int ContentID) {
            try {
                //
                int ColumnWidth = 0;
                int ColumnWidthTotal = 0;
                int ColumnCounter = 0;
                int IndexColumn = 0;
                using (var csData = new CsModel(core)) {
                    csData.open("Content Fields", "(ContentID=" + ContentID + ")", "IndexColumn");
                    if (!csData.ok()) {
                        throw (new GenericException("Unexpected exception")); // Call handleLegacyClassErrors2("NormalizeIndexColumns", "Could not read Content Field Definitions")
                    } else {
                        //
                        // Adjust IndexSortOrder to be 0 based, count by 1
                        //
                        ColumnCounter = 0;
                        while (csData.ok()) {
                            IndexColumn = csData.getInteger("IndexColumn");
                            ColumnWidth = csData.getInteger("IndexWidth");
                            if ((IndexColumn == 0) || (ColumnWidth == 0)) {
                                csData.set("IndexColumn", 0);
                                csData.set("IndexWidth", 0);
                                csData.set("IndexSortPriority", 0);
                            } else {
                                //
                                // Column appears in Index, clean it up
                                //
                                csData.set("IndexColumn", ColumnCounter);
                                ColumnCounter += 1;
                                ColumnWidthTotal += ColumnWidth;
                            }
                            csData.goNext();
                        }
                        if (ColumnCounter == 0) {
                            //
                            // No columns found, set name as Column 0, active as column 1
                            //
                            csData.goFirst();
                            while (csData.ok()) {
                                switch (GenericController.toUCase(csData.getText("name"))) {
                                    case "ACTIVE": {
                                            csData.set("IndexColumn", 0);
                                            csData.set("IndexWidth", 20);
                                            ColumnWidthTotal += 20;
                                            break;
                                        }
                                    case "NAME": {
                                            csData.set("IndexColumn", 1);
                                            csData.set("IndexWidth", 80);
                                            ColumnWidthTotal += 80;
                                            break;
                                        }
                                    default: {
                                            // do nothing
                                            break;
                                        }
                                }
                                csData.goNext();
                            }
                        }
                        //
                        // ----- Now go back and set a normalized Width value
                        //
                        if (ColumnWidthTotal > 0) {
                            csData.goFirst();
                            while (csData.ok()) {
                                ColumnWidth = csData.getInteger("IndexWidth");
                                ColumnWidth = encodeInteger((ColumnWidth * 100) / (double)ColumnWidthTotal);
                                csData.set("IndexWidth", ColumnWidth);
                                csData.goNext();
                            }
                        }
                    }
                }
                //
                // ----- now fixup Sort Priority so only visible fields are sorted.
                //
                using (var csData = new CsModel(core)) {
                    csData.open("Content Fields", "(ContentID=" + ContentID + ")", "IndexSortPriority, IndexColumn");
                    if (!csData.ok()) {
                        throw (new GenericException("Unexpected exception")); // Call handleLegacyClassErrors2("NormalizeIndexColumns", "Error reading Content Field Definitions")
                    } else {
                        //
                        // Go through all fields, clear Sort Priority if it does not appear
                        //
                        int SortValue = 0;
                        int SortDirection = 0;
                        SortValue = 0;
                        while (csData.ok()) {
                            SortDirection = 0;
                            if (csData.getInteger("IndexColumn") == 0) {
                                csData.set("IndexSortPriority", 0);
                            } else {
                                csData.set("IndexSortPriority", SortValue);
                                SortDirection = csData.getInteger("IndexSortDirection");
                                if (SortDirection == 0) {
                                    SortDirection = 1;
                                } else {
                                    if (SortDirection > 0) {
                                        SortDirection = 1;
                                    } else {
                                        SortDirection = -1;
                                    }
                                }
                                SortValue += 1;
                            }
                            csData.set("IndexSortDirection", SortDirection);
                            csData.goNext();
                        }
                    }
                }
                return;
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
        }
        //

    }
}

