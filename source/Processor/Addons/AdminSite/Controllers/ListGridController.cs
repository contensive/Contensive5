﻿
using System;
using System.Text;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Controllers.AdminUIController;
using static Contensive.Processor.Controllers.GenericController;
using static Contensive.Processor.Constants;
using Contensive.Models.Db;
using System.Collections.Generic;
using Contensive.BaseClasses;
using System.Linq;
using NLog;
//
namespace Contensive.Processor.Addons.AdminSite {
    /// <summary>
    /// Create the List Grid in the List View
    /// </summary>
    public static class ListGridController {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        //===========================================================================
        /// <summary>
        /// Get list view html
        /// </summary>
        /// <param name="core"></param>
        /// <param name="adminData"></param>
        /// <param name="gridConfig"></param>
        /// <param name="userContentPermissions"></param>
        /// <param name="sql"></param>
        /// <param name="dataSource"></param>
        /// <param name="FieldUsedInColumns"></param>
        /// <param name="IsLookupFieldValid"></param>
        /// <returns></returns>
        public static string get(CoreController core, AdminDataModel adminData, GridConfigClass gridConfig, PermissionController.UserContentPermissions userContentPermissions, string sql, DataSourceModel dataSource, Dictionary<string, bool> FieldUsedInColumns, Dictionary<string, bool> IsLookupFieldValid) {
            try {
                bool allowDelete = (adminData.adminContent.allowDelete) && (userContentPermissions.allowDelete) && (gridConfig.allowDelete);
                StringBuilder headerRow = getHeaderRow(core, adminData, gridConfig, allowDelete);
                //
                // -- generic admin url for edit and add links
                string adminEditPresetArgQsList = "";
                string adminUrlBase = "\\" + core.appConfig.adminRoute + "?" + rnAdminAction + "=" + Constants.AdminActionNop + "&cid=" + adminData.adminContent.id + "&" + RequestNameTitleExtension + "=" + GenericController.encodeRequestVariable(adminData.editViewTitleSuffix) + "&" + rnAdminSourceForm + "=" + adminData.dstFormId + "&" + rnAdminForm + "=" + AdminFormEdit;
                if (!adminData.wherePair.Count.Equals(0)) {
                    int WhereCount = 0;
                    foreach (var kvp in adminData.wherePair) {
                        adminEditPresetArgQsList += "&" + encodeRequestVariable(kvp.Key) + "=" + GenericController.encodeRequestVariable(kvp.Value);
                        WhereCount++;
                    }
                    adminUrlBase += adminEditPresetArgQsList;
                }
                //
                // Assemble DataTable
                //
                string grid = "" +
                    "<table ID=\"DataTable\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" width=\"100%\" style=\"Background-Color:white;\">" +
                        headerRow +
                        getDataRows(core, adminData, gridConfig, userContentPermissions, sql, dataSource, FieldUsedInColumns, IsLookupFieldValid, allowDelete, adminEditPresetArgQsList) +
                        (gridConfig.allowHeaderAtBottom ? headerRow : "") +
                        (gridConfig.allowFind ? getFindRow(gridConfig) : "") +
                    "</table>";
                return grid;
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }

        private static StringBuilder getDataRows(CoreController core, AdminDataModel adminData, GridConfigClass gridConfig, PermissionController.UserContentPermissions userContentPermissions, string sql, DataSourceModel dataSource, Dictionary<string, bool> FieldUsedInColumns, Dictionary<string, bool> IsLookupFieldValid, bool allowDelete, string adminEditPresetArgQsList) {
            var dataTableRows = new StringBuilder();
            string rowColor = "";
            int rowNumber = 0;
            int rowNumberLast = 0;
            using (var csData = new CsModel(core)) {
                if (csData.openSql(sql, dataSource.name, gridConfig.recordsPerPage, gridConfig.pageNumber)) {
                    rowNumber = gridConfig.recordTop;
                    rowNumberLast = gridConfig.recordTop + gridConfig.recordsPerPage;
                    //
                    // --- Print out the records
                    while ((csData.ok()) && (rowNumber < rowNumberLast)) {
                        int recordId = csData.getInteger("ID");
                        if (rowColor == "class=\"ccGridRowOdd\"") {
                            rowColor = "class=\"ccGridRowEven\"";
                        } else {
                            rowColor = "class=\"ccGridRowOdd\"";
                        }
                        //
                        // -- new row
                        dataTableRows.Append(Environment.NewLine + "<tr>");
                        //
                        // --- row number column
                        dataTableRows.Append("<td align=right " + rowColor + ">" + (rowNumber + 1) + "</td>");
                        //
                        // --- edit column
                        string editUrl = EditUIController.getEditUrl(core, adminData.adminContent.id, recordId);
                        dataTableRows.Append("<td align=center " + rowColor + ">" + EditUIController.getEditIcon(core, editUrl, "", "") + "</td>");
                        // dataTableRows.Append("<td align=center " + rowColor + ">" + AdminUIEditButtonController.getLegacyRecordEditAnchorTag(adminUrlBase + "&id=" + recordId) + "</td>");
                        //
                        // --- Delete Checkbox Columns
                        if (allowDelete) {
                            dataTableRows.Append("<td align=center " + rowColor + "><input TYPE=CheckBox NAME=row" + rowNumber + " VALUE=1 ID=\"DelCheck\"><input type=hidden name=rowid" + rowNumber + " VALUE=" + recordId + "></span></td>");
                        } else {
                            dataTableRows.Append("<td align=center " + rowColor + "><input TYPE=CheckBox disabled=\"disabled\" NAME=row" + rowNumber + " VALUE=1><input type=hidden name=rowid" + rowNumber + " VALUE=" + recordId + "></span></td>");
                        }
                        //
                        // --- field columns
                        foreach (var column in gridConfig.columns) {
                            string columnNameLc = column.Name.ToLowerInvariant();
                            dataTableRows.Append((Environment.NewLine + "<td valign=\"middle\" " + rowColor + " align=\"left\">" + SpanClassAdminNormal));
                            if ((FieldUsedInColumns.ContainsKey(columnNameLc)) && (FieldUsedInColumns[columnNameLc])) {
                                //
                                // -- field can be displayed
                                dataTableRows.Append(getGridCell(core, adminData, column.Name, csData, IsLookupFieldValid[columnNameLc]));
                            } else {
                                //
                                // -- field cannot be displayed
                                dataTableRows.Append("(invalid column)");
                            }
                            dataTableRows.Append(("&nbsp;</span></td>"));
                        }
                        dataTableRows.Append(("\n    </tr>"));
                        csData.goNext();
                        rowNumber++;
                    }
                    dataTableRows.Append("<input type=hidden name=rowcnt value=" + rowNumber + ">");
                    //
                    // --- print out the stuff at the bottom
                    //
                    int RecordTop_NextPage = gridConfig.recordTop;
                    if (csData.ok()) {
                        RecordTop_NextPage = rowNumber;
                    }
                    int RecordTop_PreviousPage = gridConfig.recordTop - gridConfig.recordsPerPage;
                    if (RecordTop_PreviousPage < 0) {
                        RecordTop_PreviousPage = 0;
                    }
                }
            }
            //
            // Header at bottom
            //
            if (rowColor == "class=\"ccGridRowOdd\"") {
                rowColor = "class=\"ccGridRowEven\"";
            } else {
                rowColor = "class=\"ccGridRowOdd\"";
            }
            string blankRow = "<tr><td colspan=" + (gridConfig.columns.Count + 3) + " " + rowColor + " style=\"text-align:left ! important;\">{msg}</td></tr>";
            if (rowNumber == 0) {
                //
                // -- No records found
                dataTableRows.Append(blankRow.Replace("{msg}", "----- no records were found -----"));
            } else {
                if (rowNumber < rowNumberLast) {
                    //
                    // --End of list
                    dataTableRows.Append(blankRow.Replace("{msg}", "----- end of list -----"));
                }
            }
            if (gridConfig.allowAddRow) {
                //
                // optional AddRow
                foreach (var addTag in EditUIController.getAddTabList(core, adminData.adminContent.name, adminEditPresetArgQsList, false, userContentPermissions.allowAdd, true)) {
                    dataTableRows.Append(blankRow.Replace("{msg}", addTag));
                }
            }

            return dataTableRows;
        }

        private static StringBuilder getHeaderRow(CoreController core, AdminDataModel adminData, GridConfigClass gridConfig, bool allowDelete) {
            var DataTable_HdrRow = new StringBuilder("<tr>");
            //
            // Row Number Column
            DataTable_HdrRow.Append("<td width=20 align=center valign=bottom class=\"small ccGridCaption\">Row</td>");
            //
            // Edit Column
            DataTable_HdrRow.Append("<td width=20 align=center valign=bottom class=\"small ccGridCaption\">Edit</td>");
            //
            // Delete Select Box Columns
            if (!allowDelete) {
                DataTable_HdrRow.Append("<td width=20 align=center valign=bottom class=\"small ccGridCaption\"><input TYPE=CheckBox disabled=\"disabled\"></td>");
            } else {
                DataTable_HdrRow.Append("<td width=20 align=center valign=bottom class=\"small ccGridCaption\"><input TYPE=CheckBox OnClick=\"CheckInputs('DelCheck',this.checked);\"></td>");
            }
            //
            // -- create header
            int ColumnWidthTotal = 0;
            foreach (var column in gridConfig.columns) {
                if (column.Width < 1) {
                    column.Width = 1;
                }
                ColumnWidthTotal += column.Width;
            }
            foreach (var column in gridConfig.columns) {
                //
                // ----- print column headers - anchored so they sort columns
                //
                int ColumnWidth = encodeInteger((100 * column.Width) / (double)ColumnWidthTotal);
                //
                // if this is a current sort ,add the reverse flag
                //
                StringBuilder buttonHref = new();
                buttonHref.Append("/" + core.appConfig.adminRoute + "?" + rnAdminForm + "=" + AdminFormIndex + "&SetSortField=" + column.Name + "&RT=0&" + RequestNameTitleExtension + "=" + GenericController.encodeRequestVariable(adminData.editViewTitleSuffix) + "&cid=" + adminData.adminContent.id);
                if (!gridConfig.sorts.ContainsKey(column.Name)) {
                    buttonHref.Append("&SetSortDirection=1");
                } else {
                    switch (gridConfig.sorts[column.Name].direction) {
                        case 1: {
                                buttonHref.Append("&SetSortDirection=2");
                                break;
                            }
                        case 2: {
                                buttonHref.Append("&SetSortDirection=0");
                                break;
                            }
                        default: {
                                // nothing
                                break;
                            }
                    }
                }
                //
                // -- column header includes WherePairCount
                if (!adminData.wherePair.Count.Equals(0)) {
                    int ptr = 0;
                    foreach (var kvp in adminData.wherePair) {
                        if (!string.IsNullOrWhiteSpace(kvp.Key)) {
                            buttonHref.Append("&wl" + ptr + "=" + GenericController.encodeRequestVariable(kvp.Value));
                            buttonHref.Append("&wr" + ptr + "=" + GenericController.encodeRequestVariable(kvp.Value));
                            ptr++;
                        }
                    }
                }
                string buttonFace = adminData.adminContent.fields[column.Name.ToLowerInvariant()].caption;
                buttonFace = GenericController.strReplace(buttonFace, " ", "&nbsp;");
                string SortTitle = "Sort A-Z";
                //
                if (gridConfig.sorts.ContainsKey(column.Name)) {
                    string sortSuffix = ((gridConfig.sorts.Count < 2) ? "" : gridConfig.sorts[column.Name].order.ToString());
                    switch (gridConfig.sorts[column.Name].direction) {
                        case 1: {
                                buttonFace = iconArrowDown + sortSuffix + "&nbsp;" + buttonFace;
                                SortTitle = "Sort Z-A";
                                break;
                            }
                        case 2: {
                                buttonFace = iconArrowUp + sortSuffix + "&nbsp;" + buttonFace;
                                SortTitle = "Remove Sort";
                                break;
                            }
                        default: {
                                // nothing
                                break;
                            }
                    }
                }
                if (gridConfig.allowColumnSort) {
                    buttonFace = HtmlController.a(buttonFace, new CPBase.BaseModels.HtmlAttributesA {
                        title = SortTitle,
                        href = buttonHref.ToString(),
                        @class = "ccGridCaption"
                    });
                }
                adminData.buttonObjectCount += 1;
                DataTable_HdrRow.Append("<td width=\"" + ColumnWidth + "%\" valign=bottom align=left class=\"small ccGridCaption\">");
                DataTable_HdrRow.Append(buttonFace);
                DataTable_HdrRow.Append("</td>");
            }
            DataTable_HdrRow.Append("</tr>");
            return DataTable_HdrRow;
        }

        private static StringBuilder getFindRow(GridConfigClass gridConfig) {
            //
            var DataTable_FindRow = new StringBuilder();
            if (gridConfig.allowFind) {
                //
                // ----- DataTable_FindRow
                //
                DataTable_FindRow.Append("<tr><td colspan=" + (3 + gridConfig.columns.Count) + " style=\"background-color:black;height:1;\"></td></tr>");
                DataTable_FindRow.Append("<tr>");
                DataTable_FindRow.Append("<td valign=\"middle\" colspan=3 width=\"60\" class=\"ccPanel\" align=center style=\"vertical-align:middle;padding:8px;text-align:center ! important;\">");
                DataTable_FindRow.Append(AdminUIController.getButtonPrimary(ButtonFind, "", false, "FindButton") + "</td>");
                int ColumnPointer = 0;
                var listOfMatches = new List<FindWordMatchEnum> { FindWordMatchEnum.matchincludes, FindWordMatchEnum.MatchEquals, FindWordMatchEnum.MatchTrue, FindWordMatchEnum.MatchFalse };
                foreach (var column in gridConfig.columns) {
                    string FieldName = GenericController.toLCase(column.Name);
                    string FindWordValue = "";
                    if (gridConfig.findWords.ContainsKey(FieldName)) {
                        var findWord = gridConfig.findWords[FieldName];
                        if (listOfMatches.Any(s => findWord.MatchOption.Equals(s))) {
                            FindWordValue = findWord.Value;
                        }
                    }
                    DataTable_FindRow.Append(Environment.NewLine + "<td valign=\"middle\" align=\"center\" class=\"ccPanel3DReverse\" style=\"padding:8px;\">"
                        + "<input type=hidden name=\"FindName" + ColumnPointer + "\" value=\"" + FieldName + "\">"
                        + "<input class=\"form-control findInput\"  onkeypress=\"KeyCheck(event);\"  type=text id=\"F" + ColumnPointer + "\" name=\"FindValue" + ColumnPointer + "\" value=\"" + FindWordValue + "\" style=\"padding-right:.2rem;padding-left:.2rem;\">"
                        + "</td>");
                    ColumnPointer += 1;
                }
                DataTable_FindRow.Append("</tr>");
            }

            return DataTable_FindRow;
        }

        //   
        //========================================================================
        /// <summary>
        /// Display a field in the admin index form
        /// </summary>
        /// <param name="core"></param>
        /// <param name="adminData"></param>
        /// <param name="fieldName"></param>
        /// <param name="CS"></param>
        /// <param name="IsLookupFieldValid"></param>
        /// <returns></returns>
        public static string getGridCell(CoreController core, AdminDataModel adminData, string fieldName, CsModel csData, bool IsLookupFieldValid) {
            try {
                var Stream = new StringBuilderLegacyController();
                var field = adminData.adminContent.fields[fieldName.ToLowerInvariant()];
                if (field.password) {
                    //
                    // -- do not list password fields
                    Stream.add("****");
                } else {
                    int Pos = 0;
                    switch (field.fieldTypeId) {
                        case CPContentBaseClass.FieldTypeIdEnum.File:
                        case CPContentBaseClass.FieldTypeIdEnum.FileImage: {
                                string filename = csData.getText(field.nameLc);
                                filename = GenericController.strReplace(filename, "\\", "/");
                                Pos = filename.LastIndexOf("/", StringComparison.InvariantCulture) + 1;
                                if (Pos != 0) {
                                    filename = filename.Substring(Pos);
                                }
                                Stream.add(filename);
                                break;
                            }
                        case CPContentBaseClass.FieldTypeIdEnum.Lookup: {
                                if (IsLookupFieldValid) {
                                    Stream.add(csData.getText("LookupTable" + field.id + "Name"));
                                } else if (!string.IsNullOrEmpty(field.lookupList)) {
                                    string[] lookups = field.lookupList.Split(',');
                                    int LookupPtr = csData.getInteger(field.nameLc) - 1;
                                    if (LookupPtr <= lookups.GetUpperBound(0)) {
                                        if (LookupPtr >= 0) {
                                            Stream.add(lookups[LookupPtr]);
                                        }
                                    }
                                } else {
                                    Stream.add(" ");
                                }
                                break;
                            }
                        case CPContentBaseClass.FieldTypeIdEnum.MemberSelect: {
                                if (IsLookupFieldValid) {
                                    Stream.add(csData.getText("LookupTable" + field.id + "Name"));
                                } else {
                                    Stream.add(csData.getText(field.nameLc));
                                }
                                break;
                            }
                        case CPContentBaseClass.FieldTypeIdEnum.Boolean: {
                                if (csData.getBoolean(field.nameLc)) {
                                    Stream.add("yes");
                                } else {
                                    Stream.add("no");
                                }
                                break;
                            }
                        case CPContentBaseClass.FieldTypeIdEnum.Currency: {
                                string fieldValueText = csData.getText(field.nameLc);
                                if (string.IsNullOrWhiteSpace(fieldValueText)) {
                                    Stream.add(fieldValueText);
                                    break;
                                }
                                Stream.add(string.Format("{0:C}", csData.getNumber(field.nameLc)));
                                break;
                            }
                        case CPContentBaseClass.FieldTypeIdEnum.LongText:
                        case CPContentBaseClass.FieldTypeIdEnum.HTML:
                        case CPContentBaseClass.FieldTypeIdEnum.HTMLCode: {
                                string fieldValueText = csData.getText(field.nameLc);
                                if (fieldValueText.Length > 50) {
                                    fieldValueText = fieldValueText.left(50) + "[more]";
                                }
                                Stream.add(fieldValueText);
                                break;
                            }
                        case CPContentBaseClass.FieldTypeIdEnum.FileText:
                        case CPContentBaseClass.FieldTypeIdEnum.FileCSS:
                        case CPContentBaseClass.FieldTypeIdEnum.FileXML:
                        case CPContentBaseClass.FieldTypeIdEnum.FileJavaScript:
                        case CPContentBaseClass.FieldTypeIdEnum.FileHTML:
                        case CPContentBaseClass.FieldTypeIdEnum.FileHTMLCode: {
                                string filename = csData.getText(field.nameLc);
                                if (!string.IsNullOrEmpty(filename)) {
                                    string Copy = core.cdnFiles.readFileText(filename);
                                    Stream.add(Copy);
                                }
                                break;
                            }
                        case CPContentBaseClass.FieldTypeIdEnum.Redirect:
                        case CPContentBaseClass.FieldTypeIdEnum.ManyToMany: {
                                Stream.add("n/a");
                                break;
                            }
                        case CPContentBaseClass.FieldTypeIdEnum.Date: {
                                //
                                // -- if minvalue, use blank, if no time-part, do short-date
                                DateTime cellValueDate = csData.getDate(field.nameLc);
                                if (cellValueDate.Equals(DateTime.MinValue)) {
                                    Stream.add("");
                                } else if (cellValueDate.Equals(cellValueDate.Date)) {
                                    Stream.add(cellValueDate.ToShortDateString());
                                } else {
                                    Stream.add(cellValueDate.ToString());
                                }
                                break;
                            }
                        default: {
                                string valueString = csData.getText(field.nameLc);
                                if (string.IsNullOrWhiteSpace(valueString)) {
                                    Stream.add(valueString);
                                    break;
                                }
                                Stream.add(csData.getText(field.nameLc));
                                break;
                            }
                    }
                }
                return HtmlController.encodeHtml(Stream.text);
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
    }
}
