
using System;
using System.Linq;
using System.Collections.Generic;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Controllers.GenericController;
using static Contensive.Processor.Constants;
using Contensive.Processor.Models.Domain;
using Contensive.BaseClasses;
using System.Text;
using NLog;

namespace Contensive.Processor.Addons.AdminSite {
    public class ListViewSetColumns {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        //=============================================================================
        //   Print the Configure Index Form
        //=============================================================================
        //
        public static string get(CPClass cp, CoreController core, AdminDataModel adminData) {
            string result = "";
            try {
                // todo refactor out
                ContentMetadataModel adminContent = adminData.adminContent;
                string Button = core.docProperties.getText(RequestNameButton);
                if (Button == ButtonOK) {
                    //
                    // -- Process OK, remove subform from querystring and return empty
                    cp.Doc.AddRefreshQueryString(RequestNameAdminSubForm, "");
                    return result;
                }
                //
                //   Load Request
                if (Button == ButtonReset) {
                    //
                    // -- Process reset
                    core.userProperty.setProperty(AdminDataModel.IndexConfigPrefix + adminContent.id.ToString(), "");
                }
                GridConfigClass gridConfig = GridConfigClass.get(core, adminData);
                int ToolsAction = core.docProperties.getInteger("dta");
                int TargetFieldId = core.docProperties.getInteger("fi");
                string TargetFieldName = core.docProperties.getText("FieldName");
                int ColumnPointer = core.docProperties.getInteger("dtcn");
                const string RequestNameAddField = "addfield";
                string FieldNameToAdd = GenericController.toUCase(core.docProperties.getText(RequestNameAddField));
                const string RequestNameAddFieldId = "addfieldID";
                int FieldIDToAdd = core.docProperties.getInteger(RequestNameAddFieldId);
                bool normalizeSaveLoad = core.docProperties.getBoolean("NeedToReloadConfig");
                bool AllowContentAutoLoad = false;
                StringBuilderLegacyController Stream = new StringBuilderLegacyController();
                string Title = "Set Columns: " + adminContent.name;
                string Description = "Use the icons to add, remove and modify your personal column prefernces for this content (" + adminContent.name + "). Hit OK when complete. Hit Reset to restore your column preferences for this content to the site's default column preferences.";
                Stream.add(AdminUIController.getHeaderTitleDescription(Title, Description));
                //
                //--------------------------------------------------------------------------------
                // Process actions
                //--------------------------------------------------------------------------------
                //
                if (adminContent.id != 0) {
                    var CDef = ContentMetadataModel.create(core, adminContent.id);
                    int ColumnWidthTotal = 0;
                    if (ToolsAction != 0) {
                        //
                        // Block contentautoload, then force a load at the end
                        //
                        AllowContentAutoLoad = (core.siteProperties.getBoolean("AllowContentAutoLoad", true));
                        core.siteProperties.setProperty("AllowContentAutoLoad", false);
                        bool reloadMetadata = false;
                        int SourceContentId = 0;
                        string SourceName = null;
                        //
                        // Make sure the FieldNameToAdd is not-inherited, if not, create new field
                        //
                        if (FieldIDToAdd != 0) {
                            foreach (KeyValuePair<string, ContentFieldMetadataModel> keyValuePair in adminContent.fields) {
                                ContentFieldMetadataModel field = keyValuePair.Value;
                                if (field.id == FieldIDToAdd) {
                                    if (field.inherited) {
                                        SourceContentId = field.contentId;
                                        SourceName = field.nameLc;
                                        //
                                        // -- copy the field
                                        using var CSSource = new CsModel(core);
                                        if (CSSource.open("Content Fields", "(ContentID=" + SourceContentId + ")and(Name=" + DbController.encodeSQLText(SourceName) + ")")) {
                                            using var CSTarget = new CsModel(core);
                                            if (CSTarget.insert("Content Fields")) {
                                                CSSource.copyRecord(CSTarget);
                                                CSTarget.set("ContentID", adminContent.id);
                                                reloadMetadata = true;
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
                        foreach (var column in gridConfig.columns) {
                            ContentFieldMetadataModel field = adminContent.fields[column.Name.ToLowerInvariant()];
                            if (field.inherited) {
                                SourceContentId = field.contentId;
                                SourceName = field.nameLc;
                                using var CSSource = new CsModel(core);
                                if (CSSource.open("Content Fields", "(ContentID=" + SourceContentId + ")and(Name=" + DbController.encodeSQLText(SourceName) + ")")) {
                                    using var CSTarget = new CsModel(core);
                                    if (CSTarget.insert("Content Fields")) {
                                        CSSource.copyRecord(CSTarget);
                                        CSTarget.set("ContentID", adminContent.id);
                                        reloadMetadata = true;
                                    }
                                }
                            }
                        }
                        //
                        // get current values for Processing
                        //
                        foreach (var column in gridConfig.columns) {
                            ColumnWidthTotal += column.Width;
                        }
                        //
                        // ----- Perform any actions first
                        //
                        switch (ToolsAction) {
                            case ToolsActionAddField: {
                                    //
                                    // Add a field to the index form
                                    //
                                    if (FieldIDToAdd != 0) {
                                        //
                                        // -- add new column to be 20% of width (reduce all by 20%)
                                        foreach (var columnx in gridConfig.columns) {
                                            columnx.Width = encodeInteger(columnx.Width * 80.0 / 100.0);
                                        }
                                        IndexConfigColumnClass column = new() {
                                            Name = cp.Content.GetRecordName("Content Fields", FieldIDToAdd),
                                            Width = encodeInteger(ColumnWidthTotal * 0.20)
                                        };
                                        if (gridConfig.columns.Find((x) => x.Name.ToLower() == column.Name.ToLower()) == null) {
                                            //
                                            // -- is the column already added (double click or refresh could add it again)
                                            gridConfig.columns.Add(column);
                                            normalizeSaveLoad = true;
                                        }
                                    }
                                    //
                                    break;
                                }
                            case ToolsActionRemoveField: {
                                    //
                                    // Remove a field to the index form
                                    int columnWidthTotal = 0;
                                    var dstColumns = new List<IndexConfigColumnClass>();
                                    foreach (var column in gridConfig.columns) {
                                        if (column.Name != TargetFieldName.ToLowerInvariant()) {
                                            dstColumns.Add(column);
                                            columnWidthTotal += column.Width;
                                        }
                                    }
                                    gridConfig.columns = dstColumns;
                                    normalizeSaveLoad = true;
                                    break;
                                }
                            case ToolsActionMoveFieldLeft: {
                                    if (gridConfig.columns.First().Name != TargetFieldName.ToLowerInvariant()) {
                                        int listIndex = 0;
                                        foreach (var column in gridConfig.columns) {
                                            if (column.Name == TargetFieldName.ToLowerInvariant()) {
                                                break;
                                            }
                                            listIndex += 1;
                                        }
                                        gridConfig.columns.swap(listIndex, listIndex - 1);
                                        normalizeSaveLoad = true;
                                    }
                                    break;
                                }
                            case ToolsActionMoveFieldRight: {
                                    if (gridConfig.columns.Last().Name != TargetFieldName.ToLowerInvariant()) {
                                        int listIndex = 0;
                                        foreach (var column in gridConfig.columns) {
                                            if (column.Name == TargetFieldName.ToLowerInvariant()) {
                                                break;
                                            }
                                            listIndex += 1;
                                        }
                                        gridConfig.columns.swap(listIndex, listIndex + 1);
                                        normalizeSaveLoad = true;
                                    }
                                    break;
                                }
                            case ToolsActionExpand: {
                                    foreach (var column in gridConfig.columns) {
                                        if (column.Name == TargetFieldName.ToLowerInvariant()) {
                                            column.Width = Convert.ToInt32(Convert.ToDouble(column.Width) * 1.1);
                                        } else {
                                            column.Width = Convert.ToInt32(Convert.ToDouble(column.Width) * 0.9);
                                        }
                                    }
                                    normalizeSaveLoad = true;
                                    break;
                                }
                            case ToolsActionContract: {
                                    foreach (var column in gridConfig.columns) {
                                        if (column.Name != TargetFieldName.ToLowerInvariant()) {
                                            column.Width = Convert.ToInt32(Convert.ToDouble(column.Width) * 1.1);
                                        } else {
                                            column.Width = Convert.ToInt32(Convert.ToDouble(column.Width) * 0.9);
                                        }
                                    }
                                    normalizeSaveLoad = true;
                                    break;
                                }
                        }
                        //
                        // Reload CDef if it changed
                        //
                        if (reloadMetadata) {
                            core.cacheRuntime.clear();
                            core.cache.invalidateAll();
                            CDef = ContentMetadataModel.createByUniqueName(core, adminContent.name);
                        }
                        //
                        // save indexconfig
                        //
                        if (normalizeSaveLoad) {
                            //
                            // Normalize the widths of the remaining columns
                            ColumnWidthTotal = 0;
                            foreach (var column in gridConfig.columns) {
                                ColumnWidthTotal += column.Width;
                            }
                            foreach (var column in gridConfig.columns) {
                                column.Width = encodeInteger((1000 * column.Width) / (double)ColumnWidthTotal);
                            }
                            AdminContentController.setIndexSQL_SaveIndexConfig(cp, core, gridConfig);
                            gridConfig = GridConfigClass.get(core, adminData);
                        }
                    }
                    //
                    //--------------------------------------------------------------------------------
                    //   Display the form
                    //--------------------------------------------------------------------------------
                    //
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
                    Stream.add("</tr></table>");
                    //
                    Stream.add("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" width=\"99%\"><tr>");
                    Stream.add("<td width=\"9%\"><nobr><img src=\"" + cdnPrefix + "images/black.gif\" width=\"1\" height=\"10\" ><img alt=\"space\" src=\"" + cdnPrefix + "images/spacer.gif\" width=\"100%\" height=\"10\" ></nobr></td>");
                    Stream.add("<td width=\"9%\"><nobr><img src=\"" + cdnPrefix + "images/black.gif\" width=\"1\" height=\"10\" ><img alt=\"space\" src=\"" + cdnPrefix + "images/spacer.gif\" width=\"100%\" height=\"10\" ></nobr></td>");
                    Stream.add("<td width=\"9%\"><nobr><img src=\"" + cdnPrefix + "images/black.gif\" width=\"1\" height=\"10\" ><img alt=\"space\" src=\"" + cdnPrefix + "images/spacer.gif\" width=\"100%\" height=\"10\" ></nobr></td>");
                    Stream.add("<td width=\"9%\"><nobr><img src=\"" + cdnPrefix + "images/black.gif\" width=\"1\" height=\"10\" ><img alt=\"space\" src=\"" + cdnPrefix + "images/spacer.gif\" width=\"100%\" height=\"10\" ></nobr></td>");
                    Stream.add("<td width=\"9%\"><nobr><img src=\"" + cdnPrefix + "images/black.gif\" width=\"1\" height=\"10\" ><img alt=\"space\" src=\"" + cdnPrefix + "images/spacer.gif\" width=\"100%\" height=\"10\" ></nobr></td>");
                    Stream.add("<td width=\"9%\"><nobr><img src=\"" + cdnPrefix + "images/black.gif\" width=\"1\" height=\"10\" ><img alt=\"space\" src=\"" + cdnPrefix + "images/spacer.gif\" width=\"100%\" height=\"10\" ></nobr></td>");
                    Stream.add("<td width=\"9%\"><nobr><img src=\"" + cdnPrefix + "images/black.gif\" width=\"1\" height=\"10\" ><img alt=\"space\" src=\"" + cdnPrefix + "images/spacer.gif\" width=\"100%\" height=\"10\" ></nobr></td>");
                    Stream.add("<td width=\"9%\"><nobr><img src=\"" + cdnPrefix + "images/black.gif\" width=\"1\" height=\"10\" ><img alt=\"space\" src=\"" + cdnPrefix + "images/spacer.gif\" width=\"100%\" height=\"10\" ></nobr></td>");
                    Stream.add("<td width=\"9%\"><nobr><img src=\"" + cdnPrefix + "images/black.gif\" width=\"1\" height=\"10\" ><img alt=\"space\" src=\"" + cdnPrefix + "images/spacer.gif\" width=\"100%\" height=\"10\" ></nobr></td>");
                    Stream.add("<td width=\"9%\"><nobr><img src=\"" + cdnPrefix + "images/black.gif\" width=\"1\" height=\"10\" ><img alt=\"space\" src=\"" + cdnPrefix + "images/spacer.gif\" width=\"100%\" height=\"10\" ></nobr></td>");
                    Stream.add("<td width=\"9%\"><nobr><img src=\"" + cdnPrefix + "images/black.gif\" width=\"1\" height=\"10\" ><img alt=\"space\" src=\"" + cdnPrefix + "images/spacer.gif\" width=\"100%\" height=\"10\" ></nobr></td>");
                    Stream.add("</tr></table>");
                    //
                    // print the column headers
                    //
                    ColumnWidthTotal = 0;
                    int InheritedFieldCount = 0;
                    if (gridConfig.columns.Count > 0) {
                        //
                        // Calc total width
                        //
                        foreach (var column in gridConfig.columns) {
                            ColumnWidthTotal += column.Width;
                        }
                        if (ColumnWidthTotal > 0) {
                            Stream.add("<table border=\"0\" cellpadding=\"5\" cellspacing=\"0\" width=\"90%\">");
                            //
                            // -- header
                            Stream.add("<tr>");
                            int ColumnWidth = 0;
                            int fieldId = 0;
                            string Caption = null;
                            foreach (var column in gridConfig.columns) {
                                //
                                // print column headers - anchored so they sort columns
                                //
                                ColumnWidth = encodeInteger(100 * (column.Width / (double)ColumnWidthTotal));
                                ContentFieldMetadataModel field = adminContent.fields[column.Name.ToLowerInvariant()];
                                fieldId = field.id;
                                Caption = field.caption;
                                if (field.inherited) {
                                    Caption = Caption + "*";
                                    InheritedFieldCount = InheritedFieldCount + 1;
                                }
                                Stream.add("<td class=\"small\" width=\"" + ColumnWidth + "%\" valign=\"top\" align=\"left\" style=\"background-color:white;border: 1px solid #555;\">" + Caption + "</td>");
                            }
                            Stream.add("</tr>");
                            //
                            // -- body
                            Stream.add("<tr>");
                            foreach (var column in gridConfig.columns) {
                                //
                                // print column headers - anchored so they sort columns
                                //
                                ColumnWidth = encodeInteger(100 * (column.Width / (double)ColumnWidthTotal));
                                ContentFieldMetadataModel field = adminContent.fields[column.Name.ToLowerInvariant()];
                                fieldId = field.id;
                                Caption = field.caption;
                                if (field.inherited) {
                                    Caption = Caption + "*";
                                    InheritedFieldCount = InheritedFieldCount + 1;
                                }
                                int ColumnPtr = 0;
                                string link = "?" + core.doc.refreshQueryString + "&FieldName=" + HtmlController.encodeHtml(field.nameLc) + "&fi=" + fieldId + "&dtcn=" + ColumnPtr + "&" + RequestNameAdminSubForm + "=" + AdminFormIndex_SubFormSetColumns;
                                Stream.add("<td width=\"" + ColumnWidth + "%\" valign=\"top\" align=\"left\">");
                                Stream.add(HtmlController.div(AdminUIController.getDeleteLink(link + "&dta=" + ToolsActionRemoveField), "text-center"));
                                Stream.add(HtmlController.div(AdminUIController.getArrowRightLink(link + "&dta=" + ToolsActionMoveFieldRight), "text-center"));
                                Stream.add(HtmlController.div(AdminUIController.getArrowLeftLink(link + "&dta=" + ToolsActionMoveFieldLeft), "text-center"));
                                Stream.add(HtmlController.div(AdminUIController.getExpandLink(link + "&dta=" + ToolsActionExpand), "text-center"));
                                Stream.add(HtmlController.div(AdminUIController.getContractLink(link + "&dta=" + ToolsActionContract), "text-center"));
                                Stream.add("</td>");
                            }
                            Stream.add("</tr>");
                            Stream.add("</table>");
                        }
                    }
                    //
                    // ----- If anything was inherited, put up the message
                    //
                    if (InheritedFieldCount > 0) {
                        Stream.add("<p class=\"ccNormal\">* This field was inherited from the Content Definition's Parent. Inherited fields will automatically change when the field in the parent is changed. If you alter these settings, this connection will be broken, and the field will no longer inherit it's properties.</P class=\"ccNormal\">");
                    }
                    //
                    // ----- now output a list of fields to add
                    //
                    if (CDef.fields.Count == 0) {
                        //
                        // -- no fields to list
                        Stream.add(SpanClassAdminNormal + "This Content Definition has no fields</span><br>");
                    } else {
                        //
                        // -- create list to sort
                        var sortList = new List<ContentFieldMetadataModel>();
                        foreach (KeyValuePair<string, ContentFieldMetadataModel> keyValuePair in adminContent.fields) {
                            sortList.Add(keyValuePair.Value);
                        }
                        sortList.Sort((a, b) => a.caption.Trim().CompareTo(b.caption.Trim()));
                        StringBuilder validFields = new();
                        StringBuilder inactiveFields = new();
                        StringBuilder notCompatible = new();
                        StringBuilder notAuthorable = new();
                        var fieldsThatAllowNotAuthorable = new List<string> { "id", "dateadded", "createdby", "modifieddate", "modifiedby", "ccguid", "contentcontrolid", "sortorder", "active" };
                        foreach (ContentFieldMetadataModel field in sortList) {
                            //
                            // display the column if it is not in use
                            if (gridConfig.columns.Find(x => x.Name == field.nameLc) == null) {
                                if (field.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.File) {
                                    //
                                    // file can not be search
                                    notCompatible.Append(HtmlController.div(iconNotAvailable + "&nbsp;" + field.caption + " (file field)"));
                                } else if (field.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.FileText) {
                                    //
                                    // filename can not be search
                                    notCompatible.Append(HtmlController.div(iconNotAvailable + "&nbsp;" + field.caption + " (text file field)"));
                                } else if (field.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.FileHTML) {
                                    //
                                    // filename can not be search
                                    notCompatible.Append(HtmlController.div(iconNotAvailable + "&nbsp;" + field.caption + " (html file field)"));
                                } else if (field.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.FileHTMLCode) {
                                    //
                                    // filename can not be search
                                    notCompatible.Append(HtmlController.div(iconNotAvailable + "&nbsp;" + field.caption + " (html code file field)"));
                                } else if (field.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.FileCSS) {
                                    //
                                    // css filename can not be search
                                    notCompatible.Append(HtmlController.div(iconNotAvailable + "&nbsp;" + field.caption + " (css file field)"));
                                } else if (field.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.FileXML) {
                                    //
                                    // xml filename can not be search
                                    notCompatible.Append(HtmlController.div(iconNotAvailable + "&nbsp;" + field.caption + " (xml file field)"));
                                } else if (field.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.FileJavaScript) {
                                    //
                                    // javascript filename can not be search
                                    notCompatible.Append(HtmlController.div(iconNotAvailable + "&nbsp;" + field.caption + " (javascript file field)"));
                                } else if (field.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.LongText) {
                                    //
                                    // can not be search
                                    notCompatible.Append(HtmlController.div(iconNotAvailable + "&nbsp;" + field.caption + " (long text field)"));
                                } else if (field.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.HTML) {
                                    //
                                    // can not be search
                                    notCompatible.Append(HtmlController.div(iconNotAvailable + "&nbsp;" + field.caption + " (html field)"));
                                } else if (field.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.FileImage) {
                                    //
                                    // can not be search
                                    notCompatible.Append(HtmlController.div(iconNotAvailable + "&nbsp;" + field.caption + " (image field)"));
                                } else if (field.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.Redirect) {
                                    //
                                    // can not be search
                                    notCompatible.Append(HtmlController.div(iconNotAvailable + "&nbsp;" + field.caption + " (redirect field)"));
                                } else if (field.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.ManyToMany) {
                                    //
                                    // many to many can not be search
                                    notCompatible.Append(HtmlController.div(iconNotAvailable + "&nbsp;" + field.caption + " (many-to-many field)"));
                                } else if (!field.authorable && !fieldsThatAllowNotAuthorable.Contains(field.nameLc, StringComparer.OrdinalIgnoreCase)) {
                                    //
                                    // many to many can not be search
                                    notAuthorable.Append(HtmlController.div(iconNotAvailable + "&nbsp;" + field.caption));
                                } else {
                                    //
                                    // can be used as column header
                                    string link = "?" + core.doc.refreshQueryString + "&fi=" + field.id + "&dta=" + ToolsActionAddField + "&" + RequestNameAddFieldId + "=" + field.id + "&" + RequestNameAdminSubForm + "=" + AdminFormIndex_SubFormSetColumns;
                                    validFields.Append(HtmlController.div(AdminUIController.getPlusLink(link, "&nbsp;" + field.caption)));
                                }
                            }
                        }
                        //
                        // valid
                        if (notAuthorable.Length > 0) {
                            Stream.add("<h4>Fields to Add</h4>");
                            Stream.add(validFields.ToString());
                        }
                        //
                        // add other lists
                        if (notAuthorable.Length > 0) {
                            Stream.add("<h4>Fields Marked to Exclude (not authorable)</h4>");
                            Stream.add(notAuthorable.ToString());
                        }
                        if (notCompatible.Length > 0) {
                            Stream.add("<h4>Fields Not Compatible</h4>");
                            Stream.add(notCompatible.ToString());
                        }
                    }
                }
                //
                //--------------------------------------------------------------------------------
                // print the content tables that have index forms to Configure
                //--------------------------------------------------------------------------------
                //
                core.siteProperties.setProperty("AllowContentAutoLoad", GenericController.encodeText(AllowContentAutoLoad));
                string Content = ""
                    + Stream.text
                    + HtmlController.inputHidden("cid", adminContent.id.ToString())
                    + HtmlController.inputHidden(rnAdminForm, "1")
                    + HtmlController.inputHidden(RequestNameAdminSubForm, AdminFormIndex_SubFormSetColumns)
                    + "";
                //
                // -- assemble form
                result = AdminUIController.getToolForm(core, Content, ButtonOK + "," + ButtonReset);
                core.html.addTitle(Title, "admin list view set columns");
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
            return result;
        }
    }
}
