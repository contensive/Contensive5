
using Contensive.Models.Db;
using Contensive.Processor.Addons.AdminSite.Models;
using Contensive.Processor.Exceptions;
using Contensive.Processor.Models.Domain;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using static Contensive.Processor.Constants;
using static Contensive.Processor.Controllers.GenericController;

namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// UI rendering for Admin
    /// REFACTOR - add  try-catch
    /// not IDisposable - not contained classes that need to be disposed
    /// </summary>
    public static class AdminUIController {
        //
        //====================================================================================================
        //
        public enum SortingStateEnum {
            NotSortable = 0,
            SortableSetAZ = 1,
            SortableSetza = 2,
            SortableNotSet = 3
        }
        //
        //====================================================================================================
        /// <summary>
        /// The title and description section at the top of a tool
        /// </summary>
        /// <param name="core"></param>
        /// <param name="Title"></param>
        /// <param name="Description"></param>
        /// <returns></returns>
        public static string getSectionHeader(CoreController core, string Title, string Description) {
            try {
                string result = getHeaderTitleDescription(Title, Description);
                if (!core.doc.userErrorList.Count.Equals(0)) { result += HtmlController.div(ErrorController.getUserError(core)); }
                result = HtmlController.div(result, "p-2");
                return HtmlController.section(result);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// The button bar section at the top and bottom of the edit page
        /// </summary>
        public static string getSectionButtonBarForEdit(CoreController core, EditButtonBarInfoClass info) {
            try {
                //
                LogController.logTrace(core, "getSectionButtonBarForEdit, enter");
                //
                string buttonsLeft = "";
                string buttonsRight = "";
                if (info.allowCancel) { buttonsLeft += getButtonPrimary(ButtonCancel, "return processSubmit(this);"); }
                if (info.allowRefresh) { buttonsLeft += getButtonPrimary(ButtonRefresh); }
                if (info.allowSave) {
                    buttonsLeft += getButtonPrimary(ButtonOK, "return processSubmit(this);");
                    buttonsLeft += getButtonPrimary(ButtonSave, "return processSubmit(this);");
                    if (info.allowAdd) buttonsLeft += getButtonPrimary(ButtonSaveAddNew, "return processSubmit(this);");
                }
                if (info.allowSendTest) { buttonsLeft += getButtonPrimary(ButtonSendTest, "Return processSubmit(this)"); }
                if (info.allowSend) { buttonsLeft += getButtonPrimary(ButtonSend, "Return processSubmit(this)"); }
                if (info.allowMarkReviewed) { buttonsLeft += getButtonPrimary(ButtonMarkReviewed); }
                if (info.allowCreateDuplicate) { buttonsLeft += getButtonPrimary(ButtonCreateDuplicate, "return processSubmit(this)"); }
                if (info.allowActivate) { buttonsLeft += getButtonPrimary(ButtonActivate, "return processSubmit(this)"); }
                if (info.allowDeactivate) { buttonsLeft += getButtonPrimary(ButtonDeactivate, "return processSubmit(this)"); }
                string JSOnClick = "if(!DeleteCheck())return false;";
                if (info.isPageContent) {
                    JSOnClick = "if(!DeletePageCheck())return false;";
                } else if (info.hasChildRecords) {
                    JSOnClick = "if(!DeleteCheckWithChildren())return false;";
                }
                buttonsRight += getButtonDanger(ButtonDelete, JSOnClick, !info.allowDelete);
                if (core.session.isAuthenticatedAdmin()) {
                    buttonsRight += getButtonDanger(ButtonModifyEditForm, "window.location='?af=105&button=select&contentid=" + info.contentId + "';return false;", info.contentId.Equals(0));
                }
                //
                LogController.logTrace(core, "getButtonBarForEdit, exit");
                //
                return getSectionButtonBar(core, buttonsLeft, buttonsRight);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Return a panel header with the header message reversed out of the left
        /// </summary>
        /// <param name="core"></param>
        /// <param name="leftSideMessage"></param>
        /// <param name="rightSideMessage"></param>
        /// <param name="rightSideNavHtml"></param>
        /// <returns></returns>
        public static string getHeader(CoreController core, string leftSideMessage, string rightSideMessage, string rightSideNavHtml) {
            string result = Processor.Properties.Resources.adminNavBarHtml;
            result = result.Replace("{navBrand}", core.appConfig.name);
            result = result.Replace("{leftSideMessage}", leftSideMessage);
            result = result.Replace("{rightSideMessage}", rightSideMessage);
            result = result.Replace("{rightSideNavHtml}", rightSideNavHtml);
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// get list of buttons html
        /// </summary>
        /// <param name="core"></param>
        /// <param name="ButtonList"></param>
        /// <param name="AllowDelete"></param>
        /// <param name="AllowAdd"></param>
        /// <returns></returns>
        public static string getButtonHtmlFromList(CoreController core, List<ButtonMetadata> ButtonList, bool AllowDelete, bool AllowAdd) {
            try {
                var result = new StringBuilder();
                foreach (ButtonMetadata button in ButtonList) {
                    if (button.isDelete) {
                        result.Append(getButtonDanger(button.value, "if(!DeleteCheck()) { return false; }", !AllowDelete));
                    } else if (button.isAdd) {
                        result.Append(getButtonPrimary(button.value, "return processSubmit(this);", !AllowAdd));
                    } else if (button.isClose) {
                        result.Append(getButtonPrimary(button.value, "window.close();"));
                    } else {
                        result.Append(getButtonPrimary(button.value));
                    }

                }
                return result.ToString();
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// get list of buttons html
        /// </summary>
        /// <param name="core"></param>
        /// <param name="ButtonList"></param>
        /// <param name="AllowDelete"></param>
        /// <param name="AllowAdd"></param>
        /// <param name="ButtonName"></param>
        /// <returns></returns>
        public static string getButtonHtmlFromCommaList(CoreController core, string ButtonList, bool AllowDelete, bool AllowAdd, string ButtonName) {
            return getButtonHtmlFromList(core, buttonStringToButtonList(ButtonList), AllowDelete, AllowAdd);
        }
        //
        //====================================================================================================
        /// <summary>
        /// The button bar section at the top and bottom of a tool
        /// </summary>
        /// <param name="leftButtonHtml"></param>
        /// <param name="rightButtonHtml"></param>
        /// <returns></returns>
        public static string getSectionButtonBar(CoreController core, string leftButtonHtml, string rightButtonHtml) {
            if (string.IsNullOrWhiteSpace(leftButtonHtml + rightButtonHtml)) {
                return "";
            } else if (string.IsNullOrWhiteSpace(rightButtonHtml)) {
                return HtmlController.section(HtmlController.div(leftButtonHtml, "border bg-white p-2"));
            } else {
                return HtmlController.section(HtmlController.div(leftButtonHtml + HtmlController.div(rightButtonHtml, "float-right"), "border bg-white p-2"));
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// get button bar for the index form
        /// </summary>
        /// <param name="core"></param>
        /// <param name="AllowAdd"></param>
        /// <param name="AllowDelete"></param>
        /// <param name="pageNumber"></param>
        /// <param name="recordsPerPage"></param>
        /// <param name="recordCnt"></param>
        /// <param name="contentName"></param>
        /// <returns></returns>
        public static string getForm_Index_ButtonBar(CoreController core, bool AllowAdd, bool AllowDelete, int pageNumber, int recordsPerPage, int recordCnt, string contentName) {
            string LeftButtons = "";
            string RightButtons = "";
            LeftButtons += AdminUIController.getButtonPrimary(ButtonCancel);
            LeftButtons += AdminUIController.getButtonPrimary(ButtonRefresh);
            if (AllowAdd) {
                LeftButtons += AdminUIController.getButtonPrimary(ButtonAdd, "");
            } else {
                LeftButtons += AdminUIController.getButtonPrimary(ButtonAdd, "", true);
            }
            LeftButtons += "<span class=\"custom-divider-vertical\">&nbsp&nbsp&nbsp</span>";
            if (pageNumber == 1) {
                LeftButtons += AdminUIController.getButtonPrimary(ButtonFirst, "", true);
                LeftButtons += AdminUIController.getButtonPrimary(ButtonPrevious, "", true);
            } else {
                LeftButtons += AdminUIController.getButtonPrimary(ButtonFirst);
                LeftButtons += AdminUIController.getButtonPrimary(ButtonPrevious);
            }
            if (recordCnt > (pageNumber * recordsPerPage)) {
                LeftButtons += AdminUIController.getButtonPrimary(ButtonNext);
            } else {
                LeftButtons += AdminUIController.getButtonPrimary(ButtonNext, "", true);
            }
            if (AllowDelete) {
                RightButtons += AdminUIController.getButtonDanger(ButtonDelete, "if(!DeleteCheck())return false;");
            } else {
                RightButtons += AdminUIController.getButtonDanger(ButtonDelete, "", true);
            }
            return getSectionButtonBar(core, LeftButtons, RightButtons);
        }
        //
        //====================================================================================================
        public static string getForm_index_pageNavigation(CoreController core, int PageNumber, int recordsPerPage, int recordCnt, string contentName) {
            try {
                int PageCount = 1;
                if (recordCnt > 1) {
                    PageCount = encodeInteger(1 + encodeInteger(Math.Floor(encodeNumber((recordCnt - 1) / recordsPerPage))));
                }

                int NavStart = PageNumber - 9;
                if (NavStart < 1) {
                    NavStart = 1;
                }
                int NavEnd = NavStart + 20;
                if (NavEnd > PageCount) {
                    NavEnd = PageCount;
                    NavStart = NavEnd - 20;
                    if (NavStart < 1) {
                        NavStart = 1;
                    }
                }
                var Nav = new StringBuilder();
                if (NavStart > 1) {
                    Nav.Append(cr3 + "<li onclick=\"bbj(this);\">1</li><li class=\"delim\">&#171;</li>");
                }
                for (int Ptr = NavStart; Ptr <= NavEnd; Ptr++) {
                    if (Ptr.Equals(PageNumber)) {
                        Nav.Append(cr3 + "<li onclick=\"bbj(this);\" class=\"hit\">" + Ptr + "</li>");
                        continue;
                    }
                    Nav.Append(cr3 + "<li onclick=\"bbj(this);\">" + Ptr + "</li>");
                }
                if (NavEnd < PageCount) {
                    Nav.Append(cr3 + "<li class=\"delim\">&#187;</li><li onclick=\"bbj(this);\">" + PageCount + "</li>");
                }
                string recordDetails = "";
                switch (recordCnt) {
                    case 0: {
                            recordDetails = "no records found";
                            break;
                        }
                    case 1: {
                            recordDetails = "1 record found";
                            break;
                        }
                    default: {
                            recordDetails = recordCnt + " records found";
                            break;
                        }
                }
                return ""
                    + cr + "<script language=\"javascript\">function bbj(p){document.getElementsByName('indexGoToPage')[0].value=p.innerHTML;document.adminForm.submit();}</script>"
                    + cr + "<div class=\"ccJumpCon\">"
                    + cr2 + "<ul>"
                    + cr3 + "<li class=\"caption\">" + recordDetails + ", page</li>"
                    + cr3 + Nav
                    + cr2 + "</ul>"
                    + cr + "</div>";
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        public static string getToolBody(CoreController core, string title, string ButtonCommaListLeft, string ButtonCommaListRight, bool AllowAdd, bool AllowDelete, string description, string ContentSummary, int ContentPadding, string Content) {
            try {
                //
                // Build inner content
                string CellContentSummary = "";
                if (!string.IsNullOrEmpty(ContentSummary)) {
                    CellContentSummary = ""
                        + "\r<div class=\"ccPanelBackground\" style=\"padding:10px;\">"
                        + core.html.getPanel(ContentSummary, "ccPanel", "ccPanelShadow", "ccPanelHilite", "100%", 5)
                        + "\r</div>";
                }
                string body = ""
                    + getSectionHeader(core, title, description)
                    + CellContentSummary
                    + "<div style=\"padding:" + ContentPadding + "px;\">" + Content + "\r</div>"
                    + "";
                //
                // -- assemble form
                return getToolForm(core, body, ButtonCommaListLeft, ButtonCommaListRight);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                return toolExceptionMessage;
            }
        }
        //
        //====================================================================================================
        public static string getEditSubheadRow(CoreController core, string Caption) {
            return "<tr><td colspan=2 class=\"ccAdminEditSubHeader\">" + Caption + "</td></tr>";
        }
        //
        //====================================================================================================
        // 
        /// <summary>
        /// GetEditPanel, An edit panel is a section of an admin page, under a subhead. When in tab mode, the subhead is blocked, and the panel is assumed to go in its own tab windows
        /// </summary>
        /// <param name="core"></param>
        /// <param name="AllowHeading"></param>
        /// <param name="PanelHeading"></param>
        /// <param name="PanelDescription"></param>
        /// <param name="PanelBody"></param>
        /// <returns></returns>
        public static string getEditPanel(CoreController core, bool AllowHeading, string PanelHeading, string PanelDescription, string PanelBody) {
            var result = new StringBuilder();
            try {
                result.Append("<div class=\"ccPanel3DReverse ccAdminEditBody\">");
                result.Append((AllowHeading && (!string.IsNullOrEmpty(PanelHeading))) ? "<h3 class=\"p-2 ccAdminEditHeading\">" + PanelHeading + "</h3>" : "");
                result.Append((!string.IsNullOrEmpty(PanelDescription)) ? "<p class=\"p-2 ccAdminEditDescription\">" + PanelDescription + "</p>" : "");
                result.Append(PanelBody + "</div>");
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
            return result.ToString();
        }
        //
        //====================================================================================================
        // Edit Table
        public static string editTable(string innerHtml) {
            return ""
                + "<table border=0 cellpadding=3 cellspacing=0 width=\"100%\">"
                    + innerHtml
                    + "<tr>"
                        + "<td width=20%><img alt=\"space\" src=\"" + cdnPrefix + "images/spacer.gif\" width=\"100%\" height=1 ></td>"
                        + "<td width=80%><img alt=\"space\" src=\"" + cdnPrefix + "images/spacer.gif\" width=\"100%\" height=1 ></td>"
                    + "</tr>"
                + "</table>";
        }
        //
        //====================================================================================================
        //
        private static string getReport_Cell(string Copy, string Align, int Columns, int RowPointer) {
            string iAlign = encodeEmpty(Align, "left");
            string Style = "ccAdminListRowEven";
            if ((RowPointer % 2) > 0) {
                Style = "ccAdminListRowOdd";
            }
            //
            string cell = Environment.NewLine + "<td valign=\"middle\" align=\"" + iAlign + "\" class=\"" + Style + "\"";
            if (Columns > 1) {
                cell = cell + " colspan=\"" + Columns + "\"";
            }
            //
            string CellCopy = Copy;
            if (string.IsNullOrEmpty(CellCopy)) {
                CellCopy = "&nbsp;";
            }
            return cell + "><span class=\"ccSmall\">" + CellCopy + "</span></td>";
        }
        //
        //====================================================================================================
        //   Report Cell Header
        //       ColumnPtr       :   0 based column number
        //       Title
        //       Width           :   if just a number, assumed to be px in style and an image is added
        //                       :   if 00px, image added with the numberic part
        //                       :   if not number, added to style as is
        //       align           :   style value
        //       ClassStyle      :   class
        //       RQS
        //       SortingState
        //
        private static string getReport_CellHeader(CoreController core, int ColumnPtr, string Title, string Width, string Align, string ClassStyle, string RefreshQueryString, SortingStateEnum SortingState) {
            string result = "";
            try {
                string Copy = "&nbsp;";
                if (!string.IsNullOrEmpty(Title)) { Copy = GenericController.strReplace(Title, " ", "&nbsp;"); }
                string Style = "VERTICAL-ALIGN:bottom;";
                if (!string.IsNullOrEmpty(Align)) { Style += "TEXT-ALIGN:" + Align + ";"; }
                switch (SortingState) {
                    case SortingStateEnum.SortableNotSet: {
                            string QS = GenericController.modifyQueryString(RefreshQueryString, "ColSort", ((int)SortingStateEnum.SortableSetAZ).ToString(), true);
                            QS = GenericController.modifyQueryString(QS, "ColPtr", ColumnPtr.ToString(), true);
                            Copy = "<a href=\"?" + QS + "\" title=\"Sort A-Z\" class=\"ccAdminListCaption\">" + Copy + "</a>";
                            break;
                        }
                    case SortingStateEnum.SortableSetza: {
                            string QS = GenericController.modifyQueryString(RefreshQueryString, "ColSort", ((int)SortingStateEnum.SortableSetAZ).ToString(), true);
                            QS = GenericController.modifyQueryString(QS, "ColPtr", ColumnPtr.ToString(), true);
                            Copy = "<a href=\"?" + QS + "\" title=\"Sort A-Z\" class=\"ccAdminListCaption\">" + Copy + "<img src=\"" + cdnPrefix + "images/arrowup.gif\" width=8 height=8 border=0></a>";
                            break;
                        }
                    case SortingStateEnum.SortableSetAZ: {
                            string QS = GenericController.modifyQueryString(RefreshQueryString, "ColSort", ((int)SortingStateEnum.SortableSetza).ToString(), true);
                            QS = GenericController.modifyQueryString(QS, "ColPtr", ColumnPtr.ToString(), true);
                            Copy = "<a href=\"?" + QS + "\" title=\"Sort Z-A\" class=\"ccAdminListCaption\">" + Copy + "<img src=\"" + cdnPrefix + "images/arrowdown.gif\" width=8 height=8 border=0></a>";
                            break;
                        }
                    default: {
                            // nothing to do
                            break;
                        }
                }
                //
                if (!string.IsNullOrEmpty(Width)) {
                    Style = Style + "width:" + Width + ";";
                }
                result = Environment.NewLine + "<td style=\"" + Style + "\" class=\"" + ClassStyle + "\">" + Copy + "</td>";
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// returns the integer column ptr of the column last selected
        /// </summary>
        /// <param name="core"></param>
        /// <param name="DefaultSortColumnPtr"></param>
        /// <returns></returns>
        public static int getReportSortColumnPtr(CoreController core, int DefaultSortColumnPtr) {
            string VarText = core.docProperties.getText("ColPtr");
            int tempGetReportSortColumnPtr = GenericController.encodeInteger(VarText);
            if ((tempGetReportSortColumnPtr == 0) && (VarText != "0")) {
                tempGetReportSortColumnPtr = DefaultSortColumnPtr;
            }
            return tempGetReportSortColumnPtr;
        }
        //
        //====================================================================================================
        //   Get Sort Column Type
        //
        //   returns the integer for the type of sorting last requested
        //       0 = nothing selected
        //       1 = sort A-Z
        //       2 = sort Z-A
        //
        //   Orderby is generated by the selection of headers captions by the user
        //   It is up to the calling program to call GetReportOrderBy to get the orderby and use it in the query to generate the cells
        //   This call returns a comma delimited list of integers representing the columns to sort
        //
        public static int getReportSortType(CoreController core) {
            string VarText = core.docProperties.getText("ColPtr");
            if ((encodeInteger(VarText) != 0) || (VarText == "0")) {
                //
                // A valid ColPtr was found
                //
                return core.docProperties.getInteger("ColSort");
            } else {
                return (int)SortingStateEnum.SortableSetAZ;
            }
        }
        //
        //====================================================================================================
        public static string getReport(CoreController core, int RowCount, string[] ColCaption, string[] ColAlign, string[] ColWidth, string[,] Cells, int PageSize, int PageNumber, string PreTableCopy, string PostTableCopy, int DataRowCount, string ClassStyle) {
            string result = "";
            try {
                int ColCnt = Cells.GetUpperBound(1);
                bool[] ColSortable = new bool[ColCnt + 1];
                for (int Ptr = 0; Ptr < ColCnt; Ptr++) {
                    ColSortable[Ptr] = false;
                }
                //
                result = getReport2(core, RowCount, ColCaption, ColAlign, ColWidth, Cells, PageSize, PageNumber, PreTableCopy, PostTableCopy, DataRowCount, ClassStyle, ColSortable, 0);
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
            return result;
        }
        //
        //====================================================================================================
        public static string getReport2(CoreController core, int RowCount, string[] ColCaption, string[] ColAlign, string[] ColWidth, string[,] Cells, int PageSize, int PageNumber, string PreTableCopy, string PostTableCopy, int DataRowCount, string ClassStyle, bool[] ColSortable, int DefaultSortColumnPtr) {
            var result = new StringBuilder();
            try {
                string RQS = null;
                int RowBase = 0;
                StringBuilderLegacyController Content = new StringBuilderLegacyController();
                int ColumnCount = 0;
                int ColumnPtr = 0;
                string ColumnWidth = null;
                int RowPointer = 0;
                string WorkingQS = null;
                //
                int PageCount = 0;
                int PagePointer = 0;
                int LinkCount = 0;
                int reportPageNumber = 0;
                int reportPageSize = 0;
                int SortColPtr = 0;
                int SortColType = 0;
                //
                reportPageNumber = PageNumber;
                if (reportPageNumber == 0) {
                    reportPageNumber = 1;
                }
                reportPageSize = PageSize;
                if (reportPageSize < 1) {
                    reportPageSize = 50;
                }
                ColumnCount = Cells.GetUpperBound(1);
                RQS = core.doc.refreshQueryString;
                //
                SortColPtr = getReportSortColumnPtr(core, DefaultSortColumnPtr);
                SortColType = getReportSortType(core);
                //
                // ----- Start the table
                Content.add(HtmlController.tableStart(3, 1, 0));
                //
                // ----- Header
                Content.add(Environment.NewLine + "<tr>");
                Content.add(getReport_CellHeader(core, 0, "&nbsp", "50px", "Right", "ccAdminListCaption", RQS, SortingStateEnum.NotSortable));
                for (ColumnPtr = 0; ColumnPtr < ColumnCount; ColumnPtr++) {
                    ColumnWidth = ColWidth[ColumnPtr];
                    if (!ColSortable[ColumnPtr]) {
                        //
                        // not sortable column
                        //
                        Content.add(getReport_CellHeader(core, ColumnPtr, ColCaption[ColumnPtr], ColumnWidth, ColAlign[ColumnPtr], "ccAdminListCaption", RQS, SortingStateEnum.NotSortable));
                    } else if (ColumnPtr == SortColPtr) {
                        //
                        // This is the current sort column
                        //
                        Content.add(getReport_CellHeader(core, ColumnPtr, ColCaption[ColumnPtr], ColumnWidth, ColAlign[ColumnPtr], "ccAdminListCaption", RQS, (SortingStateEnum)SortColType));
                    } else {
                        //
                        // Column is sortable, but not selected
                        //
                        Content.add(getReport_CellHeader(core, ColumnPtr, ColCaption[ColumnPtr], ColumnWidth, ColAlign[ColumnPtr], "ccAdminListCaption", RQS, SortingStateEnum.SortableNotSet));
                    }
                }
                Content.add(Environment.NewLine + "</tr>");
                //
                // ----- Data
                //
                if (RowCount == 0) {
                    Content.add(Environment.NewLine + "<tr>");
                    Content.add(getReport_Cell((RowBase + RowPointer).ToString(), "right", 1, RowPointer));
                    Content.add(getReport_Cell("-- End --", "left", ColumnCount, 0));
                    Content.add(Environment.NewLine + "</tr>");
                } else {
                    RowBase = DbController.getStartRecord(reportPageSize, reportPageNumber) + 1;
                    for (RowPointer = 0; RowPointer < RowCount; RowPointer++) {
                        Content.add(Environment.NewLine + "<tr>");
                        Content.add(getReport_Cell((RowBase + RowPointer).ToString(), "right", 1, RowPointer));
                        for (ColumnPtr = 0; ColumnPtr < ColumnCount; ColumnPtr++) {
                            Content.add(getReport_Cell(Cells[RowPointer, ColumnPtr], ColAlign[ColumnPtr], 1, RowPointer));
                        }
                        Content.add(Environment.NewLine + "</tr>");
                    }
                }
                //
                // ----- End Table
                //
                Content.add(kmaEndTable);
                result.Append(Content.text);
                //
                // ----- Post Table copy
                //
                if ((DataRowCount / (double)reportPageSize) != Math.Floor((DataRowCount / (double)reportPageSize))) {
                    PageCount = encodeInteger((DataRowCount / (double)reportPageSize) + 0.5);
                } else {
                    PageCount = encodeInteger(DataRowCount / (double)reportPageSize);
                }
                if (PageCount > 1) {
                    result.Append("<br>Page " + reportPageNumber + " (Row " + (RowBase) + " of " + DataRowCount + ")");
                    if (PageCount > 20) {
                        PagePointer = reportPageNumber - 10;
                    }
                    if (PagePointer < 1) {
                        PagePointer = 1;
                    }
                    if (PageCount > 1) {
                        result.Append("<br>Go to Page ");
                        if (PagePointer != 1) {
                            WorkingQS = core.doc.refreshQueryString;
                            WorkingQS = GenericController.modifyQueryString(WorkingQS, "GotoPage", "1", true);
                            result.Append("<a href=\"" + core.webServer.requestPage + "?" + WorkingQS + "\">1</A>...&nbsp;");
                        }
                        WorkingQS = core.doc.refreshQueryString;
                        WorkingQS = GenericController.modifyQueryString(WorkingQS, RequestNamePageSize, reportPageSize.ToString(), true);
                        while ((PagePointer <= PageCount) && (LinkCount < 20)) {
                            if (PagePointer == reportPageNumber) {
                                result.Append(PagePointer + "&nbsp;");
                            } else {
                                WorkingQS = GenericController.modifyQueryString(WorkingQS, RequestNamePageNumber, PagePointer.ToString(), true);
                                result.Append("<a href=\"" + core.webServer.requestPage + "?" + WorkingQS + "\">" + PagePointer + "</A>&nbsp;");
                            }
                            PagePointer += 1;
                            LinkCount += 1;
                        }
                        if (PagePointer < PageCount) {
                            WorkingQS = GenericController.modifyQueryString(WorkingQS, RequestNamePageNumber, PageCount.ToString(), true);
                            result.Append("...<a href=\"" + core.webServer.requestPage + "?" + WorkingQS + "\">" + PageCount + "</A>&nbsp;");
                        }
                        if (reportPageNumber < PageCount) {
                            WorkingQS = GenericController.modifyQueryString(WorkingQS, RequestNamePageNumber, (reportPageNumber + 1).ToString(), true);
                            result.Append("...<a href=\"" + core.webServer.requestPage + "?" + WorkingQS + "\">next</A>&nbsp;");
                        }
                        result.Append("<br>&nbsp;");
                    }
                }
                //
                return ""
                + PreTableCopy + "<table border=0 cellpadding=0 cellspacing=0 width=\"100%\"><tr><td style=\"padding:10px;\">"
                + result + "</td></tr></table>"
                + PostTableCopy + "";
            } catch (Exception ex) {
                LogController.logError(core, ex);
                return string.Empty;
            }
        }
        //
        // ====================================================================================================
        //
        public static string getFormBodyAdminOnly() => HtmlController.div("This page requires administrator permissions.", "ccError").Replace(">", " style=\"margin:10px;padding:10px;background-color:white;\">");
        //
        // ====================================================================================================
        //
        public static string getButtonPrimary(string buttonValue, string onclick, bool disabled, string htmlId, string htmlName) => HtmlController.inputSubmit(buttonValue, htmlName, htmlId, onclick, disabled, "btn btn-primary mr-1 btn-sm");
        //
        public static string getButtonPrimary(string buttonValue, string onclick, bool disabled, string htmlId) => HtmlController.inputSubmit(buttonValue, "button", htmlId, onclick, disabled, "btn btn-primary mr-1 btn-sm");
        //
        public static string getButtonPrimary(string buttonValue, string onclick, bool disabled) => HtmlController.inputSubmit(buttonValue, "button", "", onclick, disabled, "btn btn-primary mr-1 btn-sm");
        //
        public static string getButtonPrimary(string buttonValue, string onclick) => HtmlController.inputSubmit(buttonValue, "button", "", onclick, false, "btn btn-primary mr-1 btn-sm");
        //
        public static string getButtonPrimary(string buttonValue) => HtmlController.inputSubmit(buttonValue, "button", "", "", false, "btn btn-primary mr-1 btn-sm");
        //
        // ====================================================================================================
        //
        public static string getButtonDanger(string buttonValue, string onclick, bool disabled, string htmlId) => HtmlController.inputSubmit(buttonValue, "button", htmlId, onclick, disabled, "btn btn-danger mr-1 btn-sm");
        //
        public static string getButtonDanger(string buttonValue, string onclick, bool disabled) => HtmlController.inputSubmit(buttonValue, "button", "", onclick, disabled, "btn btn-danger mr-1 btn-sm");
        //
        public static string getButtonDanger(string buttonValue, string onclick) => HtmlController.inputSubmit(buttonValue, "button", "", onclick, false, "btn btn-danger mr-1 btn-sm");
        //
        public static string getButtonDanger(string buttonValue) => HtmlController.inputSubmit(buttonValue, "button", "", "", false, "btn btn-danger mr-1 btn-sm");
        //
        // ====================================================================================================
        //
        public static string getButtonPrimaryAnchor(string buttonCaption, string href) => HtmlController.a(buttonCaption, href, "btn btn-primary mr-1 btn-sm");
        //
        // ====================================================================================================
        //
        public static string getButtonDangerAnchor(string buttonCaption, string href) => HtmlController.a(buttonCaption, href, "btn btn-danger mr-1 btn-sm");
        //
        // ====================================================================================================
        //
        public static List<ButtonMetadata> buttonStringToButtonList(string ButtonList) {
            var result = new List<ButtonMetadata>();
            if (!string.IsNullOrEmpty(ButtonList.Trim(' '))) {
                foreach (string buttonValue in ButtonList.Split(',')) {
                    string buttonValueTrim = buttonValue.Trim();
                    result.Add(new ButtonMetadata {
                        name = "button",
                        value = buttonValue,
                        isAdd = buttonValueTrim.Equals(ButtonAdd),
                        isClose = buttonValueTrim.Equals(ButtonClose),
                        isDelete = buttonValueTrim.Equals(ButtonDelete)
                    });
                }
            }
            return result;
        }
        //
        // ====================================================================================================
        //
        public static string getEditForm_TitleBarDetails_EditorString(DateTime editDate, PersonModel editor, string notEditedMessage) {
            if (editDate < new DateTime(1990, 1, 1)) {
                return "unknown date";
            }
            string result = editDate.ToString(CultureInfo.InvariantCulture) + ", by ";
            if (editor == null) {
                result += "unknown user";
            } else {
                result += editor.getDisplayName();
            }
            return result;
        }
        //
        // ====================================================================================================
        /// <summary>
        /// The panel at the top of the edit page that describes the content being edited
        /// </summary>
        /// <param name="core"></param>
        /// <param name="headerInfo"></param>
        /// <returns></returns>
        public static string getEditForm_TitleBarDetails(CoreController core, RecordEditHeaderInfoClass headerInfo, EditRecordModel editRecord) {
            string result = "";
            if (editRecord.id == 0) {
                result += HtmlController.div(HtmlController.strong(editRecord.contentControlId_Name) + ":&nbsp;New record", "col-sm-12");
            } else {
                result += HtmlController.div(HtmlController.strong(editRecord.contentControlId_Name + ":&nbsp;#") + headerInfo.recordId + ", " + editRecord.nameLc, "col-sm-4");
                result += HtmlController.div(HtmlController.strong("Created:&nbsp;") + getEditForm_TitleBarDetails_EditorString(editRecord.dateAdded, editRecord.createdBy, "unknown"), "col-sm-4");
                result += HtmlController.div(HtmlController.strong("Modified:&nbsp;") + getEditForm_TitleBarDetails_EditorString(editRecord.modifiedDate, editRecord.modifiedBy, "not modified"), "col-sm-4");
            }
            return HtmlController.div(result, "row");
        }
        //
        // ====================================================================================================

        //
        // ====================================================================================================
        /// <summary>
        /// return an admin edit page row for one field in a list of fields within a tab
        /// </summary>
        /// <param name="core"></param>
        /// <param name="Caption"></param>
        /// <param name="fieldHtmlId"></param>
        /// <param name="EditorString"></param>
        /// <param name="editorHelpRow"></param>
        /// <returns></returns>
        public static string getEditRow(CoreController core, string EditorString, string Caption, string editorHelpRow, bool fieldRequired = false, bool ignore = false, string fieldHtmlId = "", string editorWrapperStyle = "", bool blockBottomRule = false) {
            return HtmlController.div(HtmlController.label(Caption, fieldHtmlId) + HtmlController.div(EditorString, "ml-5", "", editorWrapperStyle) + HtmlController.div(HtmlController.small(editorHelpRow, "form-text text-muted"), "ml-5"), "p-2" + ((blockBottomRule) ? "" : " border-bottom"));
        }
        //
        // ====================================================================================================
        //
        public static string getEditRowLegacy(CoreController core, string HTMLFieldString, string Caption, string HelpMessage = "", bool FieldRequired = false, bool AllowActiveEdit = false, string ignore0 = "") {
            return getEditRow(core, HTMLFieldString, Caption, HelpMessage, FieldRequired, AllowActiveEdit, ignore0);
        }
        //
        //====================================================================================================
        //
        public static string getHeaderTitleDescription(string Title, string Description) {
            return ""
                + ((string.IsNullOrWhiteSpace(Title)) ? "" : HtmlController.h2(Title))
                + ((string.IsNullOrWhiteSpace(Description)) ? "" : HtmlController.div(Description));
        }
        //
        //====================================================================================================
        //
        public static string getToolForm(CoreController core, string innerHtml, string leftButtonCommaList, string rightButtonCommaList) {
            string buttonBar = core.html.getPanelButtons(leftButtonCommaList, rightButtonCommaList);
            string result = ""
                + buttonBar
                + HtmlController.div(innerHtml, "p-4 bg-light")
                + buttonBar;
            return HtmlController.formMultipart(core, result);
        }
        //
        public static string getToolForm(CoreController core, string innerHtml, string leftButtonCommaList)
            => getToolForm(core, innerHtml, leftButtonCommaList, "");
        //
        //====================================================================================================
        //
        public static string getToolFormRow(CoreController core, string asdf) {
            return HtmlController.div(asdf, "p-1");
        }
        //
        //====================================================================================================
        //
        public static string getToolFormInputRow(CoreController core, string label, string input) {
            return getToolFormRow(core, HtmlController.label(label) + "<br>" + input);
        }
        //
        //===================================================================================================
        /// <summary>
        /// Wrap an edit region in a dotted border
        /// </summary>
        /// <param name="core"></param>
        /// <param name="caption"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public static string getEditWrapper(CoreController core, string caption, string content) {
            if (!core.session.isEditing()) { return content; }
            string result = HtmlController.div(content, "ccEditWrapperContent");
            if (!string.IsNullOrEmpty(caption)) { result = HtmlController.div(caption, "ccEditWrapperCaption") + result; }
            return HtmlController.div(result, "ccEditWrapper", "editWrapper" + core.doc.editWrapperCnt++);
        }
        //
        //===================================================================================================
        /// <summary>
        /// Wrap an edit region in a dotted border
        /// </summary>
        /// <param name="core"></param>
        /// <param name="innerHtml"></param>
        /// <returns></returns>
        public static string getEditWrapper(CoreController core, string innerHtml)
            => getEditWrapper(core, "", innerHtml);
        //
        //===================================================================================================
        /// <summary>
        /// Wrap an edit region in a dotted border
        /// </summary>
        /// <param name="core"></param>
        /// <param name="innerHtml"></param>
        /// <param name="contentName"></param>
        /// <param name="recordId"></param>
        /// <returns></returns>
        public static string getEditWrapper(CoreController core, string innerHtml, string contentName, int recordId)
            => getEditWrapper(core, "", getRecordEditAndCutAnchorTag(core, contentName, recordId, false) + innerHtml);
        //
        public static string getEditWrapper(CoreController core, string innerHtml, string contentName, int recordId, string customCaption)
            => getEditWrapper(core, "", getRecordEditAndCutAnchorTag(core, contentName, recordId, false, "", customCaption) + innerHtml);
        //
        //===================================================================================================
        /// <summary>
        /// Wrap an edit region in a dotted border
        /// </summary>
        /// <param name="core"></param>
        /// <param name="innerHtml"></param>
        /// <param name="contentId"></param>
        /// <param name="recordId"></param>
        /// <returns></returns>
        public static string getEditWrapper(CoreController core, string innerHtml, int contentId, int recordId) {
            var metadata = ContentMetadataModel.create(core, contentId);
            return getEditWrapper(core, "", getRecordEditAndCutAnchorTag(core, metadata, recordId, false, "", "") + innerHtml);
        }
        public static string getEditWrapper(CoreController core, string innerHtml, int contentId, int recordId, string customCaption) {
            var metadata = ContentMetadataModel.create(core, contentId);
            return getEditWrapper(core, "", getRecordEditAndCutAnchorTag(core, metadata, recordId, false, "", customCaption) + innerHtml);
        }
        //
        //===================================================================================================
        //
        public static string getEditWrapper(CoreController core, string innerHtml, string contentName, string recordGuid) {
            var metadata = ContentMetadataModel.createByUniqueName(core, contentName);
            return getEditWrapper(core, "", getRecordEditAndCutAnchorTag(core, metadata, recordGuid) + innerHtml);
        }
        //
        public static string getEditWrapper(CoreController core, string innerHtml, string contentName, string recordGuid, string customCaption) {
            var metadata = ContentMetadataModel.createByUniqueName(core, contentName);
            return getEditWrapper(core, "", getRecordEditAndCutAnchorTag(core, metadata, recordGuid, customCaption) + innerHtml);
        }
        //
        //===================================================================================================
        //
        public static string getEditWrapper(CoreController core, string innerHtml, int contentId, string recordGuid) {
            var metadata = ContentMetadataModel.create(core, contentId);
            return getEditWrapper(core, "", getRecordEditAndCutAnchorTag(core, metadata, recordGuid) + innerHtml);
        }
        //
        public static string getEditWrapper(CoreController core, string innerHtml, int contentId, string recordGuid, string customCaption) {
            var metadata = ContentMetadataModel.create(core, contentId);
            return getEditWrapper(core, "", getRecordEditAndCutAnchorTag(core, metadata, recordGuid, customCaption) + innerHtml);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// get linked icon for remove (red x)
        /// </summary>
        /// <param name="link"></param>
        /// <returns></returns>
        public static string getDeleteLink(string link) { return HtmlController.a(iconDelete_Red, link); }
        public static string getArrowRightLink(string link) { return HtmlController.a(iconArrowRight, link); }
        public static string getArrowLeftLink(string link) { return HtmlController.a(iconArrowLeft, link); }
        public static string getPlusLink(string link, string caption = "") { return HtmlController.a(iconAdd_Green + caption, link); }
        public static string getExpandLink(string link) { return HtmlController.a(iconExpand, link); }
        public static string getContractLink(string link) { return HtmlController.a(iconContract, link); }
        public static string getRefreshLink(string link) { return HtmlController.a(iconRefresh, link); }
        //
        //====================================================================================================
        //
        public static string getRecordEditAndCutAnchorTag(CoreController core, ContentMetadataModel contentMetadata, int recordId, bool allowCut, string recordName, string customCaption) {
            try {
                if (!core.session.isEditing()) { return string.Empty; }
                if (contentMetadata == null) { throw new GenericException("contentMetadata null."); }
                var editSegmentList = new List<string> {
                    getRecordEditSegment(core, contentMetadata, recordId, recordName, customCaption)
                };
                if (allowCut) {
                    string WorkingLink = GenericController.modifyLinkQuery(core.webServer.requestPage + "?" + core.doc.refreshQueryString, rnPageCut, GenericController.encodeText(contentMetadata.id) + "." + GenericController.encodeText(recordId), true);
                    editSegmentList.Add("<a class=\"ccRecordCutLink\" TabIndex=\"-1\" href=\"" + HtmlController.encodeHtml(WorkingLink) + "\">&nbsp;" + iconContentCut.Replace("content cut", getEditSegmentRecordCaption(core, "Cut", contentMetadata.name, customCaption)) + "</a>");
                }
                return getRecordEditAnchorTag(core, editSegmentList);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                return string.Empty;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Create edit and cut anchor tag
        /// </summary>
        /// <param name="core"></param>
        /// <param name="contentName"></param>
        /// <param name="recordId"></param>
        /// <param name="allowCut"></param>
        /// <param name="recordName"></param>
        /// <param name="customCaption"></param>
        /// <returns></returns>
        public static string getRecordEditAndCutAnchorTag(CoreController core, string contentName, int recordId, bool allowCut, string recordName, string customCaption) {
            try {
                if (!core.session.isEditing()) { return string.Empty; }
                if (string.IsNullOrWhiteSpace(contentName)) { throw (new GenericException("ContentName [" + contentName + "] is invalid")); }
                var contentMetadata = ContentMetadataModel.createByUniqueName(core, contentName);
                if (contentMetadata == null) { throw new GenericException("ContentName [" + contentName + "], but no content metadata found with this name."); }
                return getRecordEditAndCutAnchorTag(core, contentMetadata, recordId, allowCut, recordName, customCaption);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                return string.Empty;
            }
        }
        //
        public static string getRecordEditAndCutAnchorTag(CoreController core, string contentName, int recordId, bool allowCut, string recordName)
            => getRecordEditAndCutAnchorTag(core, contentName, recordId, allowCut, recordName, "");
        //
        //====================================================================================================
        /// <summary>
        /// create UI edit record link
        /// </summary>
        /// <param name="ContentName"></param>
        /// <param name="RecordID"></param>
        /// <param name="AllowCut"></param>
        /// <returns></returns>
        public static string getRecordEditAndCutAnchorTag(CoreController core, string ContentName, int RecordID, bool AllowCut) {
            return getRecordEditAndCutAnchorTag(core, ContentName, RecordID, AllowCut, "");
        }
        //
        public static string getRecordEditAnchorTag(CoreController core, string ContentName, int RecordID) {
            return getRecordEditAndCutAnchorTag(core, ContentName, RecordID, false, "");
        }
        //
        public static string getRecordEditAnchorTag(CoreController core, string ContentName, int RecordID, string recordName) {
            return getRecordEditAndCutAnchorTag(core, ContentName, RecordID, false, recordName);
        }
        //
        //====================================================================================================
        //
        public static string getRecordEditAndCutAnchorTag(CoreController core, ContentMetadataModel contentMetadata, string recordGuid) {
            try {
                if (!core.session.isEditing()) { return string.Empty; }
                if (contentMetadata == null) { throw new GenericException("contentMetadata null."); }
                var editSegmentList = new List<string> {
                    getRecordEditSegment(core, contentMetadata, recordGuid)
                };
                return getRecordEditAnchorTag(core, editSegmentList);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                return string.Empty;
            }
        }
        //
        public static string getRecordEditAndCutAnchorTag(CoreController core, ContentMetadataModel contentMetadata, string recordGuid, string customCaption) {
            try {
                if (!core.session.isEditing()) { return string.Empty; }
                if (contentMetadata == null) { throw new GenericException("contentMetadata null."); }
                var editSegmentList = new List<string> {
                    getRecordEditSegment(core, contentMetadata, recordGuid)
                };
                return getRecordEditAnchorTag(core, editSegmentList);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                return string.Empty;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// return caption for edit, cut, paste links
        /// </summary>
        /// <param name="verb">edit, cut, paste, etc</param>
        /// <param name="contentName"></param>
        /// <param name="recordId"></param>
        /// <param name="recordName"></param>
        /// <param name="customCaption">if blank, create a new caption.</param>
        /// <returns></returns>
        public static string getEditSegmentRecordCaption(CoreController core, string verb, string contentName, string customCaption) {
            if (!string.IsNullOrEmpty(customCaption)) { return customCaption; }
            string result = verb + "&nbsp;";
            switch (contentName.ToLower(CultureInfo.InvariantCulture)) {
                case "page content": {
                        result += "Page";
                        break;
                    }
                case "page templates": {
                        result += "Template";
                        break;
                    }
                default: {
                        result += core.pluralizationService.Singularize(contentName);
                        break;
                    }
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// return an addon edit link
        /// </summary>
        /// <param name="core"></param>
        /// <param name="cdef"></param>
        /// <param name="addonId"></param>
        /// <param name="caption"></param>
        /// <returns></returns>
        public static string getAddonEditAnchorTag(CoreController core, int addonId, string caption) {
            var cdef = ContentMetadataModel.createByUniqueName(core, "add-ons");
            if (cdef == null) { return string.Empty; }
            string link = "/" + core.appConfig.adminRoute + "?af=4&aa=2&ad=1&cid=" + cdef.id + "&id=" + addonId;
            return HtmlController.a(iconAddon_Green + ((string.IsNullOrWhiteSpace(caption)) ? "" : "&nbsp;" + caption), link, "ccAddonEditLink");
        }
        //
        //====================================================================================================
        /// <summary>
        /// returns an addon edit link to be included getEditLink wrapper
        /// </summary>
        /// <param name="core"></param>
        /// <param name="addonId"></param>
        /// <param name="addonName"></param>
        /// <returns></returns>
        public static string getAddonEditSegment(CoreController core, int addonId, string addonName) {
            try {
                if (!core.session.isEditing()) { return string.Empty; }
                if (addonId < 1) { throw (new GenericException("RecordID [" + addonId + "] is invalid")); }
                return AdminUIController.getAddonEditAnchorTag(core, addonId, "Edit Add-on" + ((string.IsNullOrEmpty(addonName)) ? "" : " '" + addonName + "'"));
            } catch (Exception ex) {
                LogController.logError(core, ex);
                return string.Empty;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// returns a record edit link to be included getEditLink wrapper
        /// </summary>
        /// <param name="core"></param>
        /// <param name="contentName"></param>
        /// <param name="recordId"></param>
        /// <param name="recordName"></param>
        /// <returns></returns>
        public static string getRecordEditSegment(CoreController core, string contentName, int recordId, string recordName, string customCaption) {
            try {
                if (!core.session.isEditing()) { return string.Empty; }
                if (string.IsNullOrWhiteSpace(contentName)) { throw (new GenericException("ContentName [" + contentName + "] is invalid")); }
                var contentMetadata = ContentMetadataModel.createByUniqueName(core, contentName);
                if (contentMetadata == null) { throw new GenericException("getRecordEditLink called with contentName [" + contentName + "], but no content metadata found with this name."); }
                return getRecordEditSegment(core, contentMetadata, recordId, recordName, customCaption);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                return string.Empty;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// returns a record edit link to be included getEditLink wrapper
        /// </summary>
        public static string getRecordEditSegment(CoreController core, ContentMetadataModel contentMetadata, int recordId, string recordName, string customCaption)
            => getRecordEditAnchorTag(core, contentMetadata, recordId, getEditSegmentRecordCaption(core, "Edit", contentMetadata.name, customCaption));
        //
        //====================================================================================================
        /// <summary>
        /// returns a record edit link to be included getEditLink wrapper
        /// </summary>
        public static string getRecordEditSegment(CoreController core, ContentMetadataModel contentMetadata, string recordGuid)
            => getRecordEditAnchorTag(core, contentMetadata, recordGuid);
        //
        public static string getRecordEditSegment(CoreController core, string contentName, int recordID)
            => getRecordEditSegment(core, contentName, recordID, "", "");
        //
        //====================================================================================================
        /// <summary>
        /// An edit link (or left justified pill) is composed of a sequence of edit links (content edit links, cut, paste, addon edit) plus an endcap
        /// </summary>
        /// <param name="core"></param>
        /// <param name="editSegmentList"></param>
        /// <returns></returns>
        public static string getRecordEditAnchorTag(CoreController core, List<string> editSegmentList) {
            if (!core.session.isEditing()) { return string.Empty; }
            string result = string.Join("", editSegmentList) + HtmlController.div("&nbsp;", "ccEditLinkEndCap");
            return HtmlController.div(result, "ccRecordLinkCon");
        }
        //
        //====================================================================================================
        /// <summary>
        /// An edit link (or left justified pill) is composed of a sequence of edit links (content edit links, cut, paste, addon edit) plus an endcap
        /// </summary>
        /// <param name="core"></param>
        /// <param name="editSegmentList"></param>
        /// <returns></returns>
        public static string getAddonEditAnchorTag(CoreController core, List<string> editSegmentList) {
            if (!core.session.isAdvancedEditing()) { return string.Empty; }
            string result = string.Join("", editSegmentList) + HtmlController.div("&nbsp;", "ccEditLinkEndCap");
            return HtmlController.div(result, "ccAddonEditLinkCon");
        }
        //
        //====================================================================================================
        /// <summary>
        /// Return a list of add links for the content plus all valid subbordinate content
        /// </summary>
        /// <param name="core"></param>
        /// <param name="contentName"></param>
        /// <param name="presetNameValueList"></param>
        /// <param name="allowPaste"></param>
        /// <param name="allowUserAdd"></param>
        /// <returns></returns>
        public static List<string> getRecordAddAnchorTag(CoreController core, string contentName, string presetNameValueList, bool allowPaste, bool allowUserAdd, bool includeChildContent) {
            try {
                List<string> result = new List<string>();
                if (!allowUserAdd) { return result; }
                if (string.IsNullOrWhiteSpace(contentName)) { throw (new GenericException("ContentName [" + contentName + "] is invalid")); }
                //
                // -- convert older QS format to command delimited format
                presetNameValueList = presetNameValueList.Replace("&", ",");
                var content = DbBaseModel.createByUniqueName<ContentModel>(core.cpParent, contentName);
                result.AddRange(getRecordAddAnchorTag_GetChildContentLinks(core, content, presetNameValueList, includeChildContent, new List<int>()));
                //
                // -- Add in the paste entry, if needed
                if (!allowPaste) { return result; }
                string ClipBoard = core.visitProperty.getText("Clipboard", "");
                if (string.IsNullOrEmpty(ClipBoard)) { return result; }
                int Position = GenericController.strInstr(1, ClipBoard, ".");
                if (Position == 0) { return result; }
                string[] ClipBoardArray = ClipBoard.Split('.');
                if (ClipBoardArray.GetUpperBound(0) == 0) { return result; }
                int ClipboardContentId = GenericController.encodeInteger(ClipBoardArray[0]);
                int ClipChildRecordId = GenericController.encodeInteger(ClipBoardArray[1]);
                if (content.isParentOf<ContentModel>(core.cpParent, ClipboardContentId)) {
                    int ParentId = 0;
                    if (GenericController.strInstr(1, presetNameValueList, "PARENTID=", 1) != 0) {
                        //
                        // must test for main_IsChildRecord
                        //
                        string BufferString = presetNameValueList;
                        BufferString = BufferString.Replace("(", "");
                        BufferString = BufferString.Replace(")", "");
                        BufferString = BufferString.Replace(",", "&");
                        ParentId = encodeInteger(GenericController.main_GetNameValue_Internal(core, BufferString, "Parentid"));
                    }
                    if ((ParentId != 0) && (!DbBaseModel.isChildOf<PageContentModel>(core.cpParent, ParentId, 0, new List<int>()))) {
                        //
                        // Can not paste as child of itself
                        string PasteLink = core.webServer.requestPage + "?" + core.doc.refreshQueryString;
                        PasteLink = GenericController.modifyLinkQuery(PasteLink, RequestNamePaste, "1", true);
                        PasteLink = GenericController.modifyLinkQuery(PasteLink, rnPasteParentContentId, content.id.ToString(), true);
                        PasteLink = GenericController.modifyLinkQuery(PasteLink, rnPasteParentRecordId, ParentId.ToString(), true);
                        PasteLink = GenericController.modifyLinkQuery(PasteLink, RequestNamePasteFieldList, presetNameValueList, true);
                        string pasteLinkAnchor = HtmlController.a(iconContentPaste_Green + "&nbsp;Paste Record", PasteLink, "ccRecordPasteLink", "", "-1");
                        result.Add(HtmlController.div(pasteLinkAnchor + HtmlController.div("&nbsp;", "ccEditLinkEndCap"), "ccRecordLinkCon"));
                    }
                }
                return result;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                return new List<string>();
            }
        }
        //
        public static List<string> getRecordAddAnchorTag(CoreController core, string ContentName, string PresetNameValueList, bool AllowPaste) => getRecordAddAnchorTag(core, ContentName, PresetNameValueList, AllowPaste, core.session.isEditing(ContentName), false);
        //
        public static List<string> getRecordAddAnchorTag(CoreController core, string ContentName, string PresetNameValueList) => getRecordAddAnchorTag(core, ContentName, PresetNameValueList, false, core.session.isEditing(ContentName), false);
        //
        //====================================================================================================
        /// <summary>
        /// return a list of record add links. If not includechildContent, the list has 1 entry, else it also includes links for all the child
        /// </summary>
        /// <param name="core"></param>
        /// <param name="content"></param>
        /// <param name="PresetNameValueList"></param>
        /// <param name="usedContentIdList"></param>
        /// <param name="MenuName"></param>
        /// <param name="ParentMenuName"></param>
        /// <returns></returns>
        private static List<string> getRecordAddAnchorTag_GetChildContentLinks(CoreController core, ContentModel content, string PresetNameValueList, bool includeChildContent, List<int> usedContentIdList) {
            var result = new List<string>();
            string Link = "";
            if (content != null) {
                if (usedContentIdList.Contains(content.id)) {
                    throw (new ArgumentException("result , Content Child [" + content.name + "] is one of its own parents", nameof(usedContentIdList)));
                } else {
                    usedContentIdList.Add(content.id);
                    //
                    // -- Determine if use has access
                    bool userHasAccess = false;
                    bool contentAllowAdd = false;
                    bool groupRulesAllowAdd = false;
                    DateTime memberRulesDateExpires;
                    bool memberRulesAllow = false;
                    if (core.session.isAuthenticatedAdmin()) {
                        //
                        // Entry was found
                        userHasAccess = true;
                        contentAllowAdd = true;
                        groupRulesAllowAdd = true;
                        memberRulesDateExpires = DateTime.MinValue;
                        memberRulesAllow = true;
                    } else {
                        //
                        // non-admin member, first check if they have access and main_Get true markers
                        //
                        using var csData = new CsModel(core);
                        string sql = "SELECT ccContent.ID as ContentID, ccContent.AllowAdd as ContentAllowAdd, ccGroupRules.AllowAdd as GroupRulesAllowAdd, ccMemberRules.DateExpires as MemberRulesDateExpires"
                            + " FROM (((ccContent"
                                + " LEFT JOIN ccGroupRules ON ccGroupRules.ContentID=ccContent.ID)"
                                + " LEFT JOIN ccgroups ON ccGroupRules.GroupID=ccgroups.ID)"
                                + " LEFT JOIN ccMemberRules ON ccgroups.ID=ccMemberRules.GroupID)"
                                + " LEFT JOIN ccMembers ON ccMemberRules.memberId=ccMembers.ID"
                            + " WHERE ("
                            + " (ccContent.id=" + content.id + ")"
                            + " AND(ccContent.active<>0)"
                            + " AND(ccGroupRules.active<>0)"
                            + " AND(ccMemberRules.active<>0)"
                            + " AND((ccMemberRules.DateExpires is Null)or(ccMemberRules.DateExpires>" + DbController.encodeSQLDate(core.doc.profileStartTime) + "))"
                            + " AND(ccgroups.active<>0)"
                            + " AND(ccMembers.active<>0)"
                            + " AND(ccMembers.ID=" + core.session.user.id + ")"
                            + " );";
                        csData.openSql(sql);
                        if (csData.ok()) {
                            //
                            // ----- Entry was found, member has some kind of access
                            //
                            userHasAccess = true;
                            contentAllowAdd = content.allowAdd;
                            groupRulesAllowAdd = csData.getBoolean("GroupRulesAllowAdd");
                            memberRulesDateExpires = csData.getDate("MemberRulesDateExpires");
                            memberRulesAllow = false;
                            if (memberRulesDateExpires == DateTime.MinValue) {
                                memberRulesAllow = true;
                            } else if (memberRulesDateExpires > core.doc.profileStartTime) {
                                memberRulesAllow = true;
                            }
                        } else {
                            //
                            // ----- No entry found, this member does not have access, just main_Get ContentID
                            //
                            userHasAccess = true;
                            contentAllowAdd = false;
                            groupRulesAllowAdd = false;
                            memberRulesAllow = false;
                        }
                    }
                    if (userHasAccess) {
                        //
                        // Add the Menu Entry* to the current menu (MenuName)
                        //
                        Link = "";
                        if (contentAllowAdd && groupRulesAllowAdd && memberRulesAllow) {
                            Link = "/" + core.appConfig.adminRoute + "?cid=" + content.id + "&af=4&aa=2&ad=1";
                            if (!string.IsNullOrEmpty(PresetNameValueList)) {
                                Link = Link + "&wc=" + GenericController.encodeRequestVariable(PresetNameValueList);
                            }
                        }
                        string shortName;
                        switch (content.name.ToLower()) {
                            case "page content": {
                                    shortName = "Page";
                                    break;
                                }
                            default: {
                                    shortName = core.pluralizationService.Singularize(content.name);
                                    break;
                                }
                        }
                        result.Add(HtmlController.div(HtmlController.a(iconAdd_Green + "&nbsp;Add " + shortName, Link, "ccRecordAddLink", "", "-1") + HtmlController.div("&nbsp;", "ccEditLinkEndCap"), "ccRecordLinkCon"));
                        //
                        // -- exit now if no child content
                        if (!includeChildContent) { return result; }
                        //
                        // Create child submenu if Child Entries found
                        var childList = DbBaseModel.createList<ContentModel>(core.cpParent, "ParentID=" + content.id);
                        if (childList.Count > 0) {
                            //
                            // ----- Create the ChildPanel with all Children found
                            foreach (var child in childList) {
                                result.AddRange(getRecordAddAnchorTag_GetChildContentLinks(core, child, PresetNameValueList, includeChildContent, usedContentIdList));
                            }
                        }
                    }
                }
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// get record add tag
        /// </summary>
        /// <param name="core"></param>
        /// <param name="cdef"></param>
        /// <returns></returns>
        public static string getRecordEditAnchorTag(CoreController core, ContentMetadataModel cdef)
            => getRecordEditAnchorTag(core, cdef, 0, "");
        //
        //====================================================================================================
        /// <summary>
        /// get record edit tag based on recordId
        /// </summary>
        /// <param name="core"></param>
        /// <param name="cdef"></param>
        /// <param name="recordId"></param>
        /// <returns></returns>
        public static string getRecordEditAnchorTag(CoreController core, ContentMetadataModel cdef, int recordId)
            => getRecordEditAnchorTag(core, cdef, recordId, "");
        //
        public static string getRecordEditAnchorTag(CoreController core, ContentMetadataModel cdef, int recordId, string caption)
            => getRecordEditAnchorTag("/" + core.appConfig.adminRoute + "?af=4&aa=2&ad=1&cid=" + cdef.id + "&id=" + recordId, caption, "ccRecordEditLink");
        //
        public static string getRecordEditAnchorTag(CoreController core, ContentMetadataModel cdef, int recordId, string caption, string htmlClass)
            => getRecordEditAnchorTag("/" + core.appConfig.adminRoute + "?af=4&aa=2&ad=1&cid=" + cdef.id + "&id=" + recordId, caption, htmlClass);
        //
        //====================================================================================================
        /// <summary>
        /// get record edit tag based on guid
        /// </summary>
        /// <param name="core"></param>
        /// <param name="cdef"></param>
        /// <param name="recordId"></param>
        /// <returns></returns>
        public static string getRecordEditAnchorTag(CoreController core, ContentMetadataModel cdef, string recordGuid)
            => getRecordEditAnchorTag(core, cdef, recordGuid, "");
        //
        public static string getRecordEditAnchorTag(CoreController core, ContentMetadataModel cdef, string recordGuid, string caption)
            => getRecordEditAnchorTag("/" + core.appConfig.adminRoute + "?af=4&aa=2&ad=1&cid=" + cdef.id + "&guid=" + recordGuid, caption, "ccRecordEditLink");
        //
        //====================================================================================================
        /// <summary>
        /// UI Edit Record, with url
        /// </summary>
        /// <param name="url"></param>
        /// <param name="caption"></param>
        /// <param name="htmlClass"></param>
        /// <returns></returns>
        public static string getRecordEditAnchorTag(string url, string caption, string htmlClass)
            => HtmlController.a(iconEdit_Green + ((string.IsNullOrWhiteSpace(caption)) ? "" : "&nbsp;" + caption), url, htmlClass);
        //
        public static string getRecordEditAnchorTag(string url, string caption)
            => getRecordEditAnchorTag(url, caption, "");
        //
        public static string getRecordEditAnchorTag(string url)
            => getRecordEditAnchorTag(url, "", "");
        //
        //====================================================================================================
        /// <summary>
        /// Wrap the content in a common wrapper if authoring is enabled
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static string getAdminHintWrapper(CoreController core, string content) {
            string msg = "<div class=\"ccHintWrapperContent\"><h4>Administrator</h4>" + content + "</div>";
            return ((core.session.isEditing("") || core.session.isAuthenticatedAdmin())) ? msg : string.Empty;
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
