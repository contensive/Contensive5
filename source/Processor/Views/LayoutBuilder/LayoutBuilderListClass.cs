using Amazon.SimpleEmail.Model;
using Contensive.BaseClasses;
using Contensive.BaseClasses.LayoutBuilder;
using Contensive.Processor.Addons.AdminSite;
using Contensive.Processor.Controllers;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace Contensive.Processor.LayoutBuilder {
    /// <summary>
    /// Create a layout with a data grid
    /// </summary>
    public class LayoutBuilderListClass : BaseClasses.LayoutBuilder.LayoutBuilderListBaseClass {
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// used for pagination and export. Setter included to support older legacy code that used cp parameter in getHtml(cp).
        /// </summary>
        private new CPClass cp { get; set; }
        //
        /// <summary>
        /// the base layout builder used to assemble the components of this builder.
        /// </summary>
        private LayoutBuilderClass layoutBuilderBase { get; set; }
        //
        // ====================================================================================================
        // constructors
        //
        /// <summary>
        /// prefered constructor
        /// </summary>
        /// <param name="cp"></param>
        public LayoutBuilderListClass(CPBaseClass cp) : base(cp) {
            this.cp = (CPClass)cp;
            layoutBuilderBase = new(cp);
            //
            // -- set includeForm default true
            includeForm = true;
        }
        //
        // ====================================================================================================
        // privates
        //
        //
        private GridConfigClass gridConfig {
            get {
                var request = new GridConfigRequest {
                    defaultRecordsPerPage = paginationPageSizeDefault,
                    gridPropertiesSaveName = "",
                    sortableFields = []
                };
                return new GridConfigClass(cp.core, request);
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// The report grid data
        /// </summary>
        private string[,] localReportCells { get; } = new string[rowSize + 1, columnSize];
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// the report download data
        /// </summary>
        private string[,] localDownloadData { get; } = new string[rowSize + 1, columnSize];
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// check if first column has been added. If not add the first column.
        /// </summary>
        private void checkColumnPtr() {
            if (columnPtr < 0) {
                addColumn();
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// check if the first row has been added. if not, add it
        /// </summary>
        private void checkRowCnt() {
            if (rowCnt < 0) {
                addRow();
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// list of elipse menu items added to the rightmost column
        /// </summary>
        private Dictionary<int, List<EllipseMenuItem>> rowEllipseMenuDict { get; set; } = [];
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// maximum columns allowed
        /// </summary>
        private const int columnSize = 99;
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// maximum rows allowed
        /// </summary>
        private const int rowSize = 19999;
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// internal storage for grid column
        /// </summary>
        private struct ColumnStruct {
            public string name { get; set; }
            public string caption { get; set; }
            public string captionClass { get; set; }
            public string cellClass { get; set; }
            public bool sortable { get; set; }
            public bool visible { get; set; }
            public bool downloadable { get; set; }
            public int columnWidthPercent { get; set; }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// when true, the report has exceeded the rowSize and future columns will populate on top of each other
        /// </summary>
        private bool reportTooLong { get; set; } = false;
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// storage for the current column
        /// </summary>
        private ColumnStruct[] columns { get; set; } = new ColumnStruct[columnSize];
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// true if the caption or captionclass has been initialized
        /// </summary>
        private bool captionIncluded { get; set; } = false;
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// the highest column count of any row
        /// </summary>
        private int columnMax { get; set; } = -1;
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// pointer to the current column
        /// </summary>
        private int columnPtr { get; set; } = -1;
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// the number of rows in the report
        /// </summary>
        private int rowCnt { get; set; } = -1;
        //
        // ====================================================================================================
        // publics 
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// pass addFilter methods through the the hidden layoutbuilderbase
        /// </summary>
        /// <param name="caption"></param>
        /// <exception cref="NotImplementedException"></exception>
        public override void addFilterGroup(string caption) {
            layoutBuilderBase.addFilterGroup(caption);
        }
        public override void addFilterCheckbox(string caption, string htmlName, string htmlValue, bool selected) {
            layoutBuilderBase.addFilterCheckbox(caption, htmlName, htmlValue, selected);
        }
        public override void addFilterRadio(string caption, string htmlName, string htmlValue, bool selected) {
            layoutBuilderBase.addFilterRadio(caption, htmlName, htmlValue, selected);
        }
        public override void addFilterTextInput(string caption, string htmlName, string htmlValue) {
            layoutBuilderBase.addFilterTextInput(caption, htmlName, htmlValue);
        }
        public override void addFilterDateInput(string caption, string htmlName, DateTime? htmlDateValue) {
            layoutBuilderBase.addFilterDateInput(caption, htmlName, htmlDateValue);
        }
        public override void addFilterSelect(string caption, string htmlName, List<NameValueSelected> options) {
            layoutBuilderBase.addFilterSelect(caption, htmlName, options);
        }
        public override void addFilterSelectContent(string caption, string htmlName, int htmlValue, string content, string sqlCriteria) {
            layoutBuilderBase.addFilterSelectContent(caption, htmlName, htmlValue, content, sqlCriteria);
        }
        public override void addFilterSelectContent(string caption, string htmlName, int htmlValue, string content, string sqlCriteria, string nonCaption) {
            layoutBuilderBase.addFilterSelectContent(caption, htmlName, htmlValue, content, sqlCriteria, nonCaption);
        }

        //
        /// <summary>
        ///  The guid of the addon method that returns the current view.
        ///  Typically this is the method that calls the layout builder.
        /// When a search or pagination is performed, the view is refreshed by calling this method.
        /// To enable pagination, set callbackAddonGuid and recordCount.
        /// </summary>
        public override string callbackAddonGuid {
            get {
                return layoutBuilderBase.callbackAddonGuid;
            }
            set {
                layoutBuilderBase.callbackAddonGuid = value;
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public override string sqlSearchTerm {
            get {
                if (_sqlSearchTerm != null) { return _sqlSearchTerm; }
                _sqlSearchTerm = cp.Request.GetText("searchTerm");
                return _sqlSearchTerm;
            }
        }
        private string _sqlSearchTerm = null;
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public override string sqlOrderBy {
            get {
                if (_sqlOrderBy != null) { return _sqlOrderBy; }
                _sqlOrderBy = "";
                string orderByDelim = " ";
                foreach (var kvp in gridConfig.sorts) {
                    _sqlOrderBy += orderByDelim + kvp.Value.fieldName;
                    if (kvp.Value.direction > 1) { _sqlOrderBy += " Desc"; }
                    orderByDelim = ",";
                }
                return _sqlOrderBy;
            }
        }
        private string _sqlOrderBy = null;
        //
        // ----------------------------------------------------------------------------------------------------
        public override void paginationReset() {
            if (_paginationPageSize != null) {
                //
                // -- cannot reset after the pagination has been used
                cp.Log.Warn($"LayoutBuilderListClass.pageinationReset cannot be called after pagination has been used. Call reset before accessing pagination or query elements.");
            }
            _paginationPageNumber = 1;
        }
        ////
        //// ----------------------------------------------------------------------------------------------------
        ///// <summary></summary> 
        //[Obsolete("Deprecated. Pagination is enabled if recordCount is set > pageSize and the callbackAddonGuid is not empty.", false)]
        //public override bool allowPagination { get { return true; } }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Only valid if allowPagination is set to true.
        /// Set the pageSize used by default.
        /// The user may select a different page size.
        /// </summary>
        public override int paginationPageSizeDefault {
            get {
                if (_paginationPageSizeDefault != null) { return (int)_paginationPageSizeDefault; }
                _paginationPageSizeDefault = 50;
                return (int)_paginationPageSizeDefault;
            }
            set {
                _paginationPageSizeDefault = value;
            }
        }
        private int? _paginationPageSizeDefault;
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// if allowPagination false, this will will be 9999999. 
        /// If allowPagination true, this is the number of rows in the display, and should be used as the pageSize in the query
        /// </summary>
        public override int paginationPageSize {
            get {
                if (_paginationPageSize != null) { return (int)_paginationPageSize; }
                _paginationPageSize = paginationPageSizeDefault;
                if (_paginationPageSize==0) { _paginationPageSize = 9999999; }
                return (int)_paginationPageSize;
            }
        }
        private int? _paginationPageSize;
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// The 1-based page number being displayed
        /// if allowPagination false, this will will be 1 (the first page). 
        /// If allowPagination true, this is the page shown in the display, and should be used as the pageNumber in the query
        /// </summary>
        public override int paginationPageNumber {
            get {
                if (_paginationPageNumber != null) { return (int)_paginationPageNumber; }
                if (cp == null) {
                    _paginationPageNumber = 1;
                    return (int)_paginationPageNumber;
                }
                //
                // -- if request includes setPaginationPageNumber, use it. Else use paginationPageNumber or 1
                _paginationPageNumber = cp.Request.GetInteger("setPaginationPageNumber");
                if ((int)_paginationPageNumber >= 1) { return (int)_paginationPageNumber; }
                //
                _paginationPageNumber = cp.Request.GetInteger("paginationPageNumber");
                if ((int)_paginationPageNumber >= 1) { return (int)_paginationPageNumber; }
                //
                _paginationPageNumber = 1;
                return (int)_paginationPageNumber;
            }
        }
        private int? _paginationPageNumber;
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Add an ellipse menu entry for the current row
        /// </summary>
        /// <param name="name"></param>
        /// <param name="url"></param>
        public override void addRowEllipseMenuItem(string name, string url) {
            if (rowEllipseMenuDict == null) { rowEllipseMenuDict = new Dictionary<int, List<EllipseMenuItem>>(); }
            if (!rowEllipseMenuDict.ContainsKey(rowCnt)) { rowEllipseMenuDict[rowCnt] = new List<EllipseMenuItem>(); }
            rowEllipseMenuDict[rowCnt].Add(new EllipseMenuItem {
                name = name,
                url = url
            });
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// create csv download as form is build
        /// </summary>
        public override bool addCsvDownloadCurrentPage { get; set; } = false;
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// The maximum number of rows allowed
        /// </summary>
        public override int reportRowLimit {
            get {
                return rowSize;
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// The maximum number of rows allowed
        /// </summary>
        public override int recordCount { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// render the entire report.
        /// Populate it and getHtml()
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override string getHtml() {
            int hint = 0;
            try {
                //
                // -- page navigation
                RenderData renderData = new() {
                    grid = getGridHtml(),
                    allowSearch = !string.IsNullOrEmpty(layoutBuilderBase.callbackAddonGuid),
                    allowPagination = !string.IsNullOrEmpty(layoutBuilderBase.callbackAddonGuid) && (recordCount > paginationPageSize)
                };
                //
                // -- prepend navigation to before-table
                if (renderData.allowPagination) {
                    _ = AdminUIController.getPageNavigation(cp.core, renderData, paginationPageNumber, paginationPageSize, recordCount);
                }
                //
                // -- render the body of the list view
                string layout = cp.Layout.GetLayout(Constants.layoutAdminUILayoutBuilderListBodyGuid, Constants.layoutAdminUILayoutBuilderListBodyName, Constants.layoutAdminUILayoutBuilderListBodyCdnPathFilename);
                layoutBuilderBase.body = cp.Mustache.Render(layout, renderData);
                //
                return layoutBuilderBase.getHtml();
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex, "hint [" + hint + "]");
                throw;
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Build the data grid part of the layout (the Body)
        /// Built 
        /// </summary>
        /// <param name="renderData"></param>
        /// <param name="bodyPagination"></param>
        private string getGridHtml() {
            int hint = 0;
            try {
                int colPtrDownload;
                StringBuilder tableHeader = new();
                string csvDownloadContent = "";
                if (captionIncluded) {
                    //
                    // -- build grid headers
                    tableHeader.Append("<thead><tr>");
                    for (int colPtr = 0; colPtr <= columnMax; colPtr++) {
                        if (columns[colPtr].visible) {
                            string classAttribute = columns[colPtr].captionClass;
                            if (classAttribute != "") {
                                classAttribute = " class=\"" + classAttribute + "\"";
                            }
                            string content = columns[colPtr].caption;
                            string sortField = columns[colPtr].name;
                            if (content == "") {
                                content = "&nbsp;";
                            } else if (columns[colPtr].sortable) {
                                content = $"<a class=\"columnSort\" data-columnSort=\"{sortField}\" href=\"#\">" + content + "</a>";
                            }
                            string styleAttribute = "";
                            if (columns[colPtr].columnWidthPercent > 0) {
                                styleAttribute = " style=\"width:" + columns[colPtr].columnWidthPercent.ToString() + "%;\"";
                            }
                            tableHeader.Append(Constants.cr + "<th" + classAttribute + styleAttribute + ">" + content + "</th>");
                        }
                    }
                    tableHeader.Append("</tr></thead>");
                    //
                    // -- build download headers (might be different from display)
                    if (addCsvDownloadCurrentPage) {
                        colPtrDownload = 0;
                        for (int colPtr = 0; colPtr <= columnMax; colPtr++) {
                            if (columns[colPtr].downloadable) {
                                if (colPtrDownload == 0) {
                                    csvDownloadContent += "\"" + columns[colPtr].caption.Replace("\"", "\"\"") + "\"";
                                } else {
                                    csvDownloadContent += ",\"" + columns[colPtr].caption.Replace("\"", "\"\"") + "\"";
                                }
                                colPtrDownload += 1;
                            }
                        }
                    }
                }
                hint = 30;
                //
                // body
                //
                StringBuilder tableBodyRows = new();
                if (localIsEmptyReport) {
                    hint = 40;
                    tableBodyRows.Append(""
                        + "<tr>"
                        + "<td style=\"text-align:left\" colspan=\"" + (columnMax + 1) + "\">[empty]</td>"
                        + "</tr>");
                } else if (reportTooLong) {
                    //
                    // -- report is too long
                    string classAttribute = columns[0].cellClass;
                    if (classAttribute != "") {
                        classAttribute = " class=\"" + classAttribute + "\"";
                    }
                    tableBodyRows.Append(""
                        + "<tr>"
                        + "<td style=\"text-align:left\" " + classAttribute + " colspan=\"" + (columnMax + 1) + "\">There are too many rows in this report. Please consider filtering the data.</td>"
                        + "</tr>");
                } else {
                    hint = 50;
                    //
                    // -- if ellipse needed, determine last visible column
                    int colPtrLastVisible = -1;
                    for (int colPtr = 0; colPtr <= columnMax; colPtr++) {
                        if (columns[colPtr].visible) {
                            colPtrLastVisible = colPtr;
                        }
                    }
                    //
                    // -- output the grid
                    for (int rowPtr = 0; rowPtr <= rowCnt; rowPtr++) {
                        string row = "";
                        colPtrDownload = 0;
                        int colVisibleCnt = 0;
                        for (int colPtr = 0; colPtr <= columnMax; colPtr++) {
                            if (columns[colPtr].visible) {
                                colVisibleCnt++;
                                string classAttribute2 = columns[colPtr].cellClass;
                                if (!string.IsNullOrEmpty(classAttribute2)) {
                                    classAttribute2 = " class=\"" + classAttribute2 + "\"";
                                }
                                string rowContent = localReportCells[rowPtr, colPtr];
                                if ((colPtrLastVisible == colPtr) && rowEllipseMenuDict.ContainsKey(rowPtr)) {
                                    //
                                    // -- add ellipse menu
                                    Contensive.BaseClasses.LayoutBuilder.EllipseMenuDataModel ellipseMenu = new Contensive.BaseClasses.LayoutBuilder.EllipseMenuDataModel {
                                        menuId = rowPtr,
                                        content = rowContent,
                                        hasMenu = true,
                                        menuList = new List<Contensive.BaseClasses.LayoutBuilder.EllipseMenuDataItemModel>()
                                    };
                                    foreach (var menuItem in rowEllipseMenuDict[rowPtr]) {
                                        ellipseMenu.menuList.Add(new Contensive.BaseClasses.LayoutBuilder.EllipseMenuDataItemModel {
                                            menuName = menuItem.name,
                                            menuHref = menuItem.url
                                        });
                                    }
                                    rowContent = cp.Mustache.Render(Processor.Properties.Resources.ellipseMenu, ellipseMenu);
                                }
                                row += Constants.cr + "<td" + classAttribute2 + ">" + rowContent + "</td>";
                            }
                            if (addCsvDownloadCurrentPage && !localExcludeRowFromDownload[rowPtr]) {
                                if (columns[colPtr].downloadable) {
                                    if (colPtrDownload == 0) {
                                        csvDownloadContent += Environment.NewLine;
                                    } else {
                                        csvDownloadContent += ",";
                                    }
                                    if (!string.IsNullOrEmpty(localDownloadData[rowPtr, colPtr])) {
                                        csvDownloadContent += "\"" + localDownloadData[rowPtr, colPtr].Replace("\"", "\"\"") + "\"";
                                    }
                                    colPtrDownload += 1;
                                }
                            }
                        }
                        string classAttribute = localRowClasses[rowPtr];
                        if (rowPtr % 2 != 0) {
                            classAttribute += " afwOdd";
                        }
                        if (classAttribute != "") {
                            classAttribute = " class=\"" + classAttribute + "\"";
                        }
                        tableBodyRows.Append($"<tr {classAttribute}>{row}</tr>");
                    }
                }
                hint = 60;
                if (addCsvDownloadCurrentPage) {
                    //
                    // todo implement cp.db.CreateCsv()
                    // 5.1 -- download
                    CPCSBaseClass csDownloads = cp.CSNew();
                    if (csDownloads.Insert("downloads")) {
                        csvDownloadFilename = csDownloads.GetFilename("filename", "export.csv");
                        cp.CdnFiles.Save(csvDownloadFilename, csvDownloadContent);
                        csDownloads.SetField("name", "Download for [" + title + "], requested by [" + cp.User.Name + "]");
                        csDownloads.SetField("requestedBy", cp.User.Id.ToString());
                        csDownloads.SetField("filename", csvDownloadFilename);
                        csDownloads.SetField("dateRequested", DateTime.Now.ToString());
                        csDownloads.SetField("datecompleted", DateTime.Now.ToString());
                        csDownloads.SetField("resultmessage", "Completed");
                        csDownloads.Save();
                    }
                    csDownloads.Close();
                }
                hint = 70;
                string dataGrid = ""
                    + "<div id=\"afwListReportDataGrid\">"
                    + "<table class=\"afwListReportTable\">"
                    + tableHeader.ToString()
                    + "<tbody>"
                    + tableBodyRows.ToString()
                    + "</tbody>"
                    + "</table>"
                    + "</div>"
                    + $"<input type=hidden name=columnSort value=\"{cp.Utils.EncodeHTML(cp.Doc.GetText("columnSort"))}\">"
                    + "";
                return dataGrid;
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex, $"hint {hint}");
                throw;
            }
        }

        [Obsolete("Deprecated. Use getHtml() with construction LayoutBuilderListClass(cp, gridConfigRequest)", false)]
        public override string getHtml(CPBaseClass cp) {
            this.cp = (CPClass)cp;
            return getHtml();
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// deprecated. Had previously been the guid of the saved report record.
        /// </summary>
        [Obsolete("Deprecated. Saved reports are no longer supported.", false)]
        public override string guid { get; set; } = "";
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// deprecated. Had previously been the name of the saved report record.
        /// </summary>
        [Obsolete("Deprecated. Saved reports are no longer supported.", false)]
        public override string name { get; set; } = "";
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// The name of the current column.
        /// To define a column, first call addColumn(), then set its name, caption, captionclass, cellclass, visible, sortable, width, downloadable. 
        /// When columns are defined, use addRow() to create a row, then addCell() repeately to create a cell for each column.
        /// </summary>
        public override string columnName {
            get {
                checkColumnPtr();
                return columns[columnPtr].name;
            }
            set {
                if (value != "") {
                    checkColumnPtr();
                    //formIncludeHeader = true;
                    columns[columnPtr].name = value;
                }
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// The caption displayed in the top row of the table.
        /// To define a column, first call addColumn(), then set its name, caption, captionclass, cellclass, visible, sortable, width, downloadable. When columns are defined, use addRow() to create a row, then addCell() repeately to create a cell for each column.
        /// </summary>
        public override string columnCaption {
            get {
                checkColumnPtr();
                return columns[columnPtr].caption;
            }
            set {
                if (value != "") {
                    checkColumnPtr();
                    captionIncluded = true;
                    columns[columnPtr].caption = value;
                }
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Optional class to be added to the caption for the current column.
        /// To define a column, first call addColumn(), then set its name, caption, captionclass, cellclass, visible, sortable, width, downloadable. When columns are defined, use addRow() to create a row, then addCell() repeately to create a cell for each column.
        /// </summary>
        public override string columnCaptionClass {
            get {
                checkColumnPtr();
                return columns[columnPtr].captionClass;
            }
            set {
                if (value != "") {
                    captionIncluded = true;
                    checkColumnPtr();
                    columns[columnPtr].captionClass = value;
                }
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Optional class to be added to each cell in this column.
        /// To define a column, first call addColumn(), then set its name, caption, captionclass, cellclass, visible, sortable, width, downloadable. When columns are defined, use addRow() to create a row, then addCell() repeately to create a cell for each column.
        /// </summary>
        public override string columnCellClass {
            get {
                checkColumnPtr();
                return columns[columnPtr].cellClass;
            }
            set {
                checkColumnPtr();
                columns[columnPtr].cellClass = value;
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// If true, the column caption will be linked and if the user clicks it, the form will redraw giving the calling code the opportunity to sort data accordingly.
        /// To define a column, first call addColumn(), then set its name, caption, captionclass, cellclass, visible, sortable, width, downloadable. When columns are defined, use addRow() to create a row, then addCell() repeately to create a cell for each column.
        /// </summary>
        public override bool columnSortable {
            get {
                checkColumnPtr();
                return columns[columnPtr].sortable;
            }
            set {
                checkColumnPtr();
                columns[columnPtr].sortable = value;
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Optional integer percentagle added to the caption for this column. 1 to 100.
        /// To define a column, first call addColumn(), then set its name, caption, captionclass, cellclass, visible, sortable, width, downloadable. When columns are defined, use addRow() to create a row, then addCell() repeately to create a cell for each column.
        /// </summary>
        public override int columnWidthPercent {
            get {
                checkColumnPtr();
                return columns[columnPtr].columnWidthPercent;
            }
            set {
                checkColumnPtr();
                columns[columnPtr].columnWidthPercent = value;
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Default true. If set false, the column will not display but will be exported in the csv.
        /// To define a column, first call addColumn(), then set its name, caption, captionclass, cellclass, visible, sortable, width, downloadable. When columns are defined, use addRow() to create a row, then addCell() repeately to create a cell for each column.
        /// </summary>
        public override bool columnVisible {
            get {
                checkColumnPtr();
                return columns[columnPtr].visible;
            }
            set {
                checkColumnPtr();
                columns[columnPtr].visible = value;
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// set the column downloadable
        /// To define a column, first call addColumn(), then set its name, caption, captionclass, cellclass, visible, sortable, width, downloadable. When columns are defined, use addRow() to create a row, then addCell() repeately to create a cell for each column.
        /// </summary>
        public override bool columnDownloadable {
            get {
                checkColumnPtr();
                return columns[columnPtr].downloadable;
            }
            set {
                checkColumnPtr();
                columns[columnPtr].downloadable = value;
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Add a new blank column. After adding the column, use properties like .columnName to populate it.
        /// To define a column, first call addColumn(), then set its name, caption, captionclass, cellclass, visible, sortable, width, downloadable. When columns are defined, use addRow() to create a row, then addCell() repeately to create a cell for each column.
        /// </summary>
        public override void addColumn() {
            if (columnPtr < columnSize) {
                columnPtr += 1;
                columns[columnPtr].name = "";
                columns[columnPtr].caption = "";
                columns[columnPtr].captionClass = "";
                columns[columnPtr].cellClass = "";
                columns[columnPtr].sortable = false;
                columns[columnPtr].visible = true;
                columns[columnPtr].downloadable = true;
                columns[columnPtr].columnWidthPercent = 0;
                if (columnPtr > columnMax) {
                    columnMax = columnPtr;
                }
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Add a new column populated with the values provided.
        /// To define a column, first call addColumn(), then set its name, caption, captionclass, cellclass, visible, sortable, width, downloadable. When columns are defined, use addRow() to create a row, then addCell() repeately to create a cell for each column.
        /// </summary>
        public override void addColumn(BaseClasses.LayoutBuilder.ReportListColumnBaseClass column) {
            addColumn();
            columnCaption = column.caption;
            columnCaptionClass = column.captionClass;
            columnCellClass = column.cellClass;
            columnDownloadable = column.downloadable;
            columnName = column.name;
            columnSortable = column.sortable;
            columnVisible = column.visible;
            columnWidthPercent = column.columnWidthPercent;
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// add a new row. After adding a row, add columns and populate them.
        /// To define a column, first call addColumn(), then set its name, caption, captionclass, cellclass, visible, sortable, width, downloadable. When columns are defined, use addRow() to create a row, then addCell() repeately to create a cell for each column.
        /// </summary>
        public override void addRow() {
            localIsEmptyReport = false;
            if (rowCnt < rowSize) {
                rowCnt += 1;
                localExcludeRowFromDownload[rowCnt] = false;
                localRowClasses[rowCnt] = "";
            } else {
                reportTooLong = true;
            }
            checkColumnPtr();
            columnPtr = 0;
        }
        private bool localIsEmptyReport = true;
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// mark this row to exclude from data download.
        /// To define a column, first call addColumn(), then set its name, caption, captionclass, cellclass, visible, sortable, width, downloadable. When columns are defined, use addRow() to create a row, then addCell() repeately to create a cell for each column.
        /// </summary>
        public override bool excludeRowFromDownload {
            get {
                checkColumnPtr();
                checkRowCnt();
                return localExcludeRowFromDownload[rowCnt];
            }
            set {
                checkColumnPtr();
                checkRowCnt();
                localExcludeRowFromDownload[rowCnt] = value;
            }
        }
        private readonly bool[] localExcludeRowFromDownload = new bool[rowSize + 1];
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Set the html class for the current row.
        /// To define a column, first call addColumn(), then set its name, caption, captionclass, cellclass, visible, sortable, width, downloadable. When columns are defined, use addRow() to create a row, then addCell() repeately to create a cell for each column.
        /// </summary>
        /// <param name="styleClass"></param>
        public override void addRowClass(string styleClass) {
            localIsEmptyReport = false;
            checkColumnPtr();
            checkRowCnt();
            localRowClasses[rowCnt] += " " + styleClass;
        }
        private readonly string[] localRowClasses = new string[rowSize + 1];
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// populate a cell.
        /// To define a column, first call addColumn(), then set its name, caption, captionclass, cellclass, visible, sortable, width, downloadable. When columns are defined, use addRow() to create a row, then addCell() repeately to create a cell for each column.
        /// </summary>
        /// <param name="content"></param>
        public override void setCell(string content) {
            setCell(content, content);
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// populate a cell.
        /// To define a column, first call addColumn(), then set its name, caption, captionclass, cellclass, visible, sortable, width, downloadable. When columns are defined, use addRow() to create a row, then addCell() repeately to create a cell for each column.
        /// </summary>
        public override void setCell(string reportContent, string downloadContent) {
            if (!reportTooLong) {
                localIsEmptyReport = false;
                checkColumnPtr();
                checkRowCnt();
                localReportCells[rowCnt, columnPtr] = reportContent;
                localDownloadData[rowCnt, columnPtr] = downloadContent;
            }
            if (columnPtr < columnMax) {
                columnPtr += 1;
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// populate a cell.
        /// To define a column, first call addColumn(), then set its name, caption, captionclass, cellclass, visible, sortable, width, downloadable. When columns are defined, use addRow() to create a row, then addCell() repeately to create a cell for each column.
        /// </summary>
        public override void setCell(int content) => setCell(content.ToString(), content.ToString());
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// populate a cell.
        /// To define a column, first call addColumn(), then set its name, caption, captionclass, cellclass, visible, sortable, width, downloadable. When columns are defined, use addRow() to create a row, then addCell() repeately to create a cell for each column.
        /// </summary>
        public override void setCell(int content, int downloadContent) => setCell(content.ToString(), downloadContent.ToString());
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// populate a cell.
        /// To define a column, first call addColumn(), then set its name, caption, captionclass, cellclass, visible, sortable, width, downloadable. When columns are defined, use addRow() to create a row, then addCell() repeately to create a cell for each column.
        /// </summary>
        public override void setCell(double content) => setCell(content.ToString(), content.ToString());
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// populate a cell.
        /// To define a column, first call addColumn(), then set its name, caption, captionclass, cellclass, visible, sortable, width, downloadable. When columns are defined, use addRow() to create a row, then addCell() repeately to create a cell for each column.
        /// </summary>
        public override void setCell(double content, double downloadContent) => setCell(content.ToString(), downloadContent.ToString());
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// populate a cell.
        /// To define a column, first call addColumn(), then set its name, caption, captionclass, cellclass, visible, sortable, width, downloadable. When columns are defined, use addRow() to create a row, then addCell() repeately to create a cell for each column.
        /// </summary>
        public override void setCell(bool content) => setCell(content.ToString(), content.ToString());
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// populate a cell.
        /// To define a column, first call addColumn(), then set its name, caption, captionclass, cellclass, visible, sortable, width, downloadable. When columns are defined, use addRow() to create a row, then addCell() repeately to create a cell for each column.
        /// </summary>
        public override void setCell(bool content, bool downloadContent) => setCell(content.ToString(), downloadContent.ToString());
        // 
        /// <summary>
        /// populate a cell.
        /// To define a column, first call addColumn(), then set its name, caption, captionclass, cellclass, visible, sortable, width, downloadable. When columns are defined, use addRow() to create a row, then addCell() repeately to create a cell for each column.
        /// </summary>
        public override void setCell(DateTime? content) => setCell(content.ToString(), content.ToString());
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// populate a cell.
        /// To define a column, first call addColumn(), then set its name, caption, captionclass, cellclass, visible, sortable, width, downloadable. When columns are defined, use addRow() to create a row, then addCell() repeately to create a cell for each column.
        /// </summary>
        public override void setCell(DateTime? content, DateTime? downloadContent) => setCell((content == null) ? "" : content.ToString(), downloadContent == null ? "" : downloadContent.ToString());
        //
        //====================================================================================================
        // -- deprecated
        //
        [Obsolete("Deprecated. use addHidden() to add a formId hidden tag", false)]
        public override string formId {
            get {
                return localFormId_Local;
            }
            set {
                localFormId_Local = value;
                layoutBuilderBase.includeForm = !string.IsNullOrEmpty(value);
            }
        }
        private string localFormId_Local = "";

        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// if true, the container between the button rows will include default padding
        /// </summary>
        public override bool includeBodyPadding {
            get {
                return layoutBuilderBase.includeBodyPadding;
            }
            set {
                layoutBuilderBase.includeBodyPadding = value;
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// if true, the container between the button rows will include the default background color. Else it is transparent.
        /// </summary>
        public override bool includeBodyColor {
            get {
                return layoutBuilderBase.includeBodyColor;
            }
            set {
                layoutBuilderBase.includeBodyColor = value;
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// if true, this layoutBuilder will not be contained in other layoutBuilder content. 
        /// This is used by the default getHtml() to include an outer div with the htmlId "afw", and the styles and javascript
        /// </summary>
        public override bool isOuterContainer {
            get {
                return layoutBuilderBase.isOuterContainer;
            }
            set {
                layoutBuilderBase.isOuterContainer = value;
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// add a form hidden input to the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        public override bool includeForm {
            get {
                return layoutBuilderBase.includeForm;
            }
            set {
                layoutBuilderBase.includeForm = value;
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// The headline at the top of the form
        /// </summary>
        public override string title {
            get {
                return layoutBuilderBase.title;
            }
            set {
                layoutBuilderBase.title = value;
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// message displayed as a warning message. Not an error, but an issue of some type
        /// </summary>
        public override string warningMessage {
            get {
                return layoutBuilderBase.warningMessage;
            }
            set {
                layoutBuilderBase.warningMessage = value;
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        [Obsolete("Deprecated. use warningMessage.",false)] public override string warning {
            get {
                return layoutBuilderBase.warningMessage;
            }
            set {
                layoutBuilderBase.warningMessage = value;
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// message displayed as a fail message. Data is wrong
        /// </summary>
        public override string failMessage {
            get {
                return layoutBuilderBase.failMessage;
            }
            set {
                layoutBuilderBase.failMessage = value;
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// message displayed as an informational message. Nothing is wrong, but the user should know
        /// </summary>
        public override string infoMessage {
            get {
                return layoutBuilderBase.infoMessage;
            }
            set {
                layoutBuilderBase.infoMessage = value;
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// message displayed as a success message.
        /// </summary>
        public override string successMessage {
            get {
                return layoutBuilderBase.successMessage;
            }
            set {
                layoutBuilderBase.successMessage = value;
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// simple description text. Will be wrapped in an html paragraph tag.
        /// </summary>
        public override string description {
            get {
                return layoutBuilderBase.description;
            }
            set {
                layoutBuilderBase.description = value;
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// The default Layoutbuilder styles. Override to customize.
        /// </summary>
        [Obsolete("move javascript and styles to layouts", false)] public override string styleSheet => Processor.Properties.Resources.layoutBuilderStyles;
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// The default Layoutbuilder script. Override to customize.
        /// </summary>
        [Obsolete("move javascript and styles to layouts", false)] public override string javascript => Processor.Properties.Resources.layoutBuilderJavaScript;
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Optional. If set, this value will populate the title in the subnav of the portalbuilder
        /// </summary>
        public override string portalSubNavTitle {
            get {
                return _portalSubNavTitle;
            }
            set {
                _portalSubNavTitle = value;
                cp.Doc.SetProperty("portalSubNavTitle", value);
            }
        }
        private string _portalSubNavTitle = "";
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// A virtual filename to a download of the report data. Leave blank to prevent download file
        /// </summary>
        public override string csvDownloadFilename {
            get {
                return layoutBuilderBase.csvDownloadFilename;
            }
            set {
                layoutBuilderBase.csvDownloadFilename = value;
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// add a form hidden input to the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Value"></param>
        /// <param name="htmlId"></param>
        public override void addFormHidden(string Name, string Value, string htmlId) {
            layoutBuilderBase.addFormHidden(Name, Value, htmlId);
        }
        private string hiddenList = "";
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// add a form hidden input to the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Value"></param>
        public override void addFormHidden(string Name, string Value) => addFormHidden(Name, Value, "");
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// add a form hidden input to the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="htmlId"></param>
        public override void addFormHidden(string name, int value, string htmlId) => addFormHidden(name, value.ToString(), htmlId);
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// add a form hidden input to the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public override void addFormHidden(string name, int value) => addFormHidden(name, value.ToString(), "");
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// add a form hidden input to the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="htmlId"></param>
        public override void addFormHidden(string name, double value, string htmlId) => addFormHidden(name, value.ToString(), htmlId);
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// add a form hidden input to the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public override void addFormHidden(string name, double value) => addFormHidden(name, value.ToString());
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// add a form hidden input to the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="htmlId"></param>
        public override void addFormHidden(string name, DateTime value, string htmlId) => addFormHidden(name, value.ToString(), htmlId);
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// add a form hidden input to the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public override void addFormHidden(string name, DateTime value) => addFormHidden(name, value.ToString());
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// add a form hidden input to the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="htmlId"></param>
        public override void addFormHidden(string name, bool value, string htmlId) => addFormHidden(name, value.ToString(), htmlId);
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// add a form hidden input to the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public override void addFormHidden(string name, bool value) => addFormHidden(name, value.ToString());
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Create a button with a link that does not submit a form. When clicked it anchors to the link
        /// </summary>
        /// <param name="buttonCaption"></param>
        /// <param name="link"></param>
        public override void addLinkButton(string buttonCaption, string link) {
            layoutBuilderBase.addLinkButton(buttonCaption, link);
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Create a button with a link that does not submit a form. When clicked it anchors to the link
        /// </summary>
        /// <param name="buttonCaption"></param>
        /// <param name="link"></param>
        /// <param name="htmlId"></param>
        public override void addLinkButton(string buttonCaption, string link, string htmlId) {
            layoutBuilderBase.addLinkButton(buttonCaption, link, htmlId);
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Create a button with a link that does not submit a form. When clicked it anchors to the link
        /// </summary>
        /// <param name="buttonCaption"></param>
        /// <param name="link"></param>
        /// <param name="htmlId"></param>
        /// <param name="htmlClass"></param>
        public override void addLinkButton(string buttonCaption, string link, string htmlId, string htmlClass) {
            layoutBuilderBase.addLinkButton(buttonCaption, link, htmlId, htmlClass);
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// add a form button to the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        /// <param name="buttonValue"></param>
        public override void addFormButton(string buttonValue) {
            addFormButton(buttonValue, "button", "", "");
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// add a form button to the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        /// <param name="buttonValue"></param>
        /// <param name="buttonName"></param>
        public override void addFormButton(string buttonValue, string buttonName) {
            addFormButton(buttonValue, buttonName, "", "");
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// add a form button to the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        /// <param name="buttonValue"></param>
        /// <param name="buttonName"></param>
        /// <param name="buttonId"></param>
        public override void addFormButton(string buttonValue, string buttonName, string buttonId) {
            addFormButton(buttonValue, buttonName, buttonId, "");
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// add a form button to the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        /// <param name="buttonValue"></param>
        /// <param name="buttonName"></param>
        /// <param name="buttonId"></param>
        /// <param name="buttonClass"></param>
        public override void addFormButton(string buttonValue, string buttonName, string buttonId, string buttonClass) {
            layoutBuilderBase.addFormButton(buttonValue, buttonName, buttonId, buttonClass);
        }
        /// <summary>  
        /// You should avoid setting the body directly. Instead, use the addRow() and setCell() methods to populate the body.  
        /// Automatically calls the body property of the inherited class.  
        /// </summary>  
        public override string body {
            get => layoutBuilderBase.body;
            set => layoutBuilderBase.body = value;
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// An html block added to the left of the table. Typically used for filters.
        /// </summary>
        public override string htmlLeftOfBody {
            get {
                return layoutBuilderBase.htmlLeftOfBody;
            }
            set {
                layoutBuilderBase.htmlLeftOfBody = value;
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// An html block added above the table. Typically used for filters.
        /// </summary>
        public override string htmlBeforeBody {
            get {
                return layoutBuilderBase.htmlBeforeBody;
            }
            set {
                layoutBuilderBase.htmlBeforeBody = value;
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// An html block added below the table. Typically used for filters.
        /// </summary>
        public override string htmlAfterBody {
            get {
                return layoutBuilderBase.htmlAfterBody;
            }
            set {
                layoutBuilderBase.htmlAfterBody = value;
            }
        }
        [Obsolete("Deprecated. Instead use cp.adminUI.getPortalFeatureLink()", false)] public override string formActionQueryString { get; set; }
        //
        [Obsolete("Deprecated. Use includeform to block the form tag.", false)] public override bool blockFormTag {
            get {
                return !layoutBuilderBase.includeForm;
            }
            set {
                layoutBuilderBase.includeForm = !value;
            }
        }
        //
        [Obsolete("Deprecated. No longer needed.", false)] 
        public override string refreshQueryString { get; set; }
        //
        [Obsolete("Deprecated. Use CP.AdminUI.getPortalFeatureLink()", false)]
        public override string baseUrl {
            get {
                return layoutBuilderBase.baseUrl;
            }
        }
        [Obsolete("Deprecated. To enable pagination, set callbackAddonGuid and recordCount.", false)]
        public override string baseAjaxUrl {
            get {
                return layoutBuilderBase.baseAjaxUrl;
            }
            set {
                layoutBuilderBase.baseAjaxUrl = value;
            }
        }
        [Obsolete("Depricated. Use htmlAfterTable", false)]
        public override string footer {
            get {
                return htmlAfterBody;
            }
            set {
                htmlAfterBody = value;
            }
        }
    }
    //
    // ====================================================================================================
    /// <summary>
    /// data model used for mustache rendering
    /// </summary>
    public class RenderData {
        public List<RenderData_Link> links { get; set; } = [];
        public int paginationPageNumber { get; set; }
        public string searchTerm { get; set; }
        public string grid { get; set; }
        public bool allowSearch { set; get; }
        public bool allowPagination { set; get; }

    }
    public class RenderData_Link {
        public string css { get; set; }
        public string content { get; set; }

    }
    //
    public class EllipseMenuItem {
        public string name { get; set; }
        public string url { get; set; }
    }

}