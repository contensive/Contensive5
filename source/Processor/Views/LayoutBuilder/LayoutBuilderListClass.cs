using Amazon.SimpleEmail.Model;
using Contensive.BaseClasses;
using Contensive.Processor.Addons.AdminSite;
using Contensive.Processor.Controllers;
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
        private CPClass cp { get; set; }
        //
        /// <summary>
        /// the base layout builder used to assemble the components of this builder.
        /// initialized in constructor so it can read the request for baseUrl
        /// </summary>
        private LayoutBuilderClass layoutBuilder { get; set; }
        //
        // ====================================================================================================
        // constructors
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// prefered constructor
        /// </summary>
        /// <param name="cp"></param>
        public LayoutBuilderListClass(CPBaseClass cp) :base(cp) {
            this.cp = (CPClass)cp;
            layoutBuilder = new(cp);
            //
            // -- if an ajax callback, get the baseUrl comes the request, else it is the url of the current page
            baseUrl = cp.Request.GetText("LayoutBuilderBaseUrl");
            if (string.IsNullOrEmpty(baseUrl)) {
                baseUrl = $"{cp.Request.Protocol}{cp.Request.Host}{cp.Request.PathPage}?{cp.Request.QueryString}";
            }
            addFormHidden("layoutBuilderBaseUrl", baseUrl);
        }
        //
        // ====================================================================================================
        // privates
        //
        // ----------------------------------------------------------------------------------------------------
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
        private string[,] localReportCells { get; } = new string[rowSize, columnSize];
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// the report download data
        /// </summary>
        private string[,] localDownloadData { get; } = new string[rowSize, columnSize];
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// add indent to the source
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        private string indent(string src) {
            return src.Replace(Constants.cr, Constants.cr2);
        }
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
        /// todo deprecate - use ReportListColumnClass
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
        private bool ReportTooLong { get; set; } = false;
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
        /// The url of the page. Needed because the page is rendered with ajax, and the original page's url is used as the base for calls
        /// </summary>
        public override string baseUrl { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// a url to refresh the grid provided by the client application.
        /// Required for pagination and search. If empty, pagination and search are disabled.
        /// This url is passed in an input-hidden and used by the layoutbuilder javascript to refresh the grid for search and sort
        /// </summary>
        public override string baseAjaxUrl { get; set; }

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
        private string? _sqlSearchTerm;
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
        private string? _sqlOrderBy;
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// if set true, the pageSize and pageNumber will control pagination
        /// The grid will include pagination controls, and the client application should read pageSize and pageNumber when setting up the query
        /// </summary>
        public override bool allowPagination {
            get {
                return cp.Site.GetBoolean("allow afw pagination beta", false);
            }
        }
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
                if (!allowPagination) {
                    _paginationPageSize = 9999999;
                    return (int)_paginationPageSize;
                }
                _paginationPageSize = paginationPageSizeDefault;
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
                if (!allowPagination || cp == null) {
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
        /// layoutBase was created in the constructor so it can read the request for baseUrl. 
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
                    baseAjaxUrl = baseAjaxUrl,
                    allowSearch = !string.IsNullOrEmpty(baseAjaxUrl),
                    allowPagination = !string.IsNullOrEmpty(baseAjaxUrl) && (recordCount > paginationPageSize)
                };
                //
                // -- prepend navigation to before-table
                if (renderData.allowPagination) {
                    _ = AdminUIController.getPageNavigation(cp.core, renderData, paginationPageNumber, paginationPageSize, recordCount);
                }
                //
                // -- render the body of the list view
                string layout = cp.Layout.GetLayout(Constants.layoutAdminUILayoutBuilderListBodyGuid, Constants.layoutAdminUILayoutBuilderListBodyName, Constants.layoutAdminUILayoutBuilderListBodyCdnPathFilename);
                string body = cp.Mustache.Render(layout, renderData);
                //
                // add user errors
                string userErrors = cp.Utils.ConvertHTML2Text(cp.UserError.GetList());
                if (!string.IsNullOrEmpty(userErrors)) {
                    warningMessage += userErrors;
                }
                //
                // -- add hidden for ajax function in javascript
                // -- this might need a serial number to prevent collisions if this layout is used 2+ times
                addFormHidden("baseAjaxUrl", baseAjaxUrl);
                ////
                //// -- construct page
                //LayoutBuilderClass layoutBase = new(cp) {
                //    body = body,
                //    includeBodyPadding = includeBodyPadding,
                //    includeBodyColor = includeBodyColor,
                //    buttonList = buttonList,
                //    csvDownloadFilename = csvDownloadFilename,
                //    description = description,
                //    formActionQueryString = formActionQueryString,
                //    refreshQueryString = refreshQueryString,
                //    hiddenList = hiddenList,
                //    includeForm = includeForm,
                //    isOuterContainer = isOuterContainer,
                //    title = title,
                //    warningMessage = warningMessage,
                //    failMessage = failMessage,
                //    infoMessage = infoMessage,
                //    successMessage = successMessage,
                //    htmlAfterBody = htmlAfterBody,
                //    htmlBeforeBody = htmlBeforeBody,
                //    htmlLeftOfBody = htmlLeftOfBody,
                //    blockFormTag = blockFormTag,
                //    baseAjaxUrl = baseAjaxUrl
                //};

                layoutBuilder.body = body;
                layoutBuilder.includeBodyPadding = includeBodyPadding;
                layoutBuilder.includeBodyColor = includeBodyColor;
                layoutBuilder.buttonList = buttonList;
                layoutBuilder.csvDownloadFilename = csvDownloadFilename;
                layoutBuilder.description = description;
                layoutBuilder.formActionQueryString = formActionQueryString;
                layoutBuilder.refreshQueryString = refreshQueryString;
                layoutBuilder.hiddenList = hiddenList;
                layoutBuilder.includeForm = includeForm;
                layoutBuilder.isOuterContainer = isOuterContainer;
                layoutBuilder.title = title;
                layoutBuilder.warningMessage = warningMessage;
                layoutBuilder.failMessage = failMessage;
                layoutBuilder.infoMessage = infoMessage;
                layoutBuilder.successMessage = successMessage;
                layoutBuilder.htmlAfterBody = htmlAfterBody;
                layoutBuilder.htmlBeforeBody = htmlBeforeBody;
                layoutBuilder.htmlLeftOfBody = htmlLeftOfBody;
                layoutBuilder.blockFormTag = blockFormTag;
                layoutBuilder.baseAjaxUrl = baseAjaxUrl;




                string listReport = layoutBuilder.getHtml(cp);
                return listReport;
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
                    string xrefreshQueryString = (!string.IsNullOrEmpty(refreshQueryString) ? refreshQueryString : cp.Doc.RefreshQueryString);
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
                } else if (ReportTooLong) {
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
                        tableBodyRows.Append( $"<tr {classAttribute}>{row}</tr>");
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
                addFormHidden("baseUrl", baseAjaxUrl);
                addFormHidden("baseAjaxUrl", baseAjaxUrl);

                string dataGrid  = ""
                    + "<div id=\"afwListReportDataGrid\">"
                    + "<table class=\"afwListReportTable\">"
                    + tableHeader.ToString()
                    + "<tbody>"
                    + tableBodyRows.ToString()
                    + "</tbody>"
                    + "</table>"
                    + "</div>"
                    + $"<input type=hidden name=columnSort value=\"{cp.Utils.EncodeHTML(cp.Doc.GetText("columnSort"))}\">"
                    + $"<input type=hidden name=baseAjaxUrl value=\"{cp.Utils.EncodeHTML( baseAjaxUrl)}\">"
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
        /// If true, the resulting html is wrapped in a form element whose action returns execution back to this addon where is it processed here in the same code.
        /// consider a pattern that blocks the include form if this layout is called form the portal system, where the portal methods create the entire strucuture
        /// </summary>
        private bool includeForm { get; set; } = false;
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// deprecated. Had previously been the guid of the saved report record.
        /// </summary>
        [Obsolete("deprecated. Had previously been the guid of the saved report record.", false)]
        public override string guid { get; set; } = "";
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// deprecated. Had previously been the name of the saved report record.
        /// </summary>
        [Obsolete("deprecated. Had previously been the name of the saved report record.", false)]
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
                ReportTooLong = true;
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
        private readonly bool[] localExcludeRowFromDownload = new bool[rowSize];
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
        private readonly string[] localRowClasses = new string[rowSize];
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
            if (!ReportTooLong) {
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
        [Obsolete("deprecated. use addHidden() to add a formId hidden tag", false)]
        public override string formId {
            get {
                return localFormId_Local;
            }
            set {
                localFormId_Local = value;
                includeForm = !string.IsNullOrEmpty(value);
            }
        }
        private string localFormId_Local = "";
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// if true, the optional form tag will be blocked. The form tag is added automaatically if buttons, hiddens or a form-action is added
        /// </summary>
        public override bool blockFormTag { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// if true, the container between the button rows will include default padding
        /// </summary>
        public override bool includeBodyPadding { get; set; } = true;
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// if true, the container between the button rows will include the default background color. Else it is transparent.
        /// </summary>
        public override bool includeBodyColor { get; set; } = true;
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// if true, this layoutBuilder will not be contained in other layoutBuilder content. 
        /// This is used by the default getHtml() to include an outer div with the htmlId "afw", and the styles and javascript
        /// </summary>
        public override bool isOuterContainer { get; set; } = false;
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// The headline at the top of the form
        /// </summary>
        public override string title { get; set; } = "";
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// message displayed as a warning message. Not an error, but an issue of some type
        /// </summary>
        public override string warningMessage {
            get {
                return _warningMessage;
            }
            set {
                _warningMessage = value;
            }
        }
        private string _warningMessage = "";
        //
        // ----------------------------------------------------------------------------------------------------
        public override string warning {
            get {
                return _warningMessage;
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// message displayed as a fail message. Data is wrong
        /// </summary>
        public override string failMessage { get; set; } = "";
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// message displayed as an informational message. Nothing is wrong, but the user should know
        /// </summary>
        public override string infoMessage { get; set; } = "";
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// message displayed as a success message.
        /// </summary>
        public override string successMessage { get; set; } = "";
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// simple description text. Will be wrapped in an html paragraph tag.
        /// </summary>
        public override string description { get; set; } = "";
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// The default Layoutbuilder styles. Override to customize.
        /// </summary>
        public override string styleSheet => Processor.Properties.Resources.layoutBuilderStyles;
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// The default Layoutbuilder script. Override to customize.
        /// </summary>
        public override string javascript => Processor.Properties.Resources.layoutBuilderJavaScript;
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
        public override string csvDownloadFilename { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// add a form hidden input to the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Value"></param>
        /// <param name="htmlId"></param>
        public override void addFormHidden(string Name, string Value, string htmlId) {
            hiddenList += "<input type=\"hidden\" name=\"" + Name + "\" value=\"" + Value + "\" id=\"" + htmlId + "\">";
            includeForm = true;
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
            buttonList += LayoutBuilderController.a(buttonCaption, link);
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
            buttonList += LayoutBuilderController.a(buttonCaption, link, htmlId);
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
            buttonList += LayoutBuilderController.a(buttonCaption, link, htmlId, htmlClass);
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
            buttonList += LayoutBuilderController.getButton(buttonName, buttonValue, buttonId, buttonClass);
            includeForm = true;
        }
        private string buttonList = "";
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// The action attribute of the form element that wraps the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        public override string formActionQueryString {
            get {
                return formActionQueryString_local;
            }
            set {
                formActionQueryString_local = value;
                includeForm |= !string.IsNullOrEmpty(value);
            }
        }
        private string formActionQueryString_local;
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// An html block added to the left of the table. Typically used for filters.
        /// </summary>
        public override string htmlLeftOfBody { get; set; } = "";
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// An html block added above the table. Typically used for filters.
        /// </summary>
        public override string htmlBeforeBody { get; set; } = "";
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// An html block added below the table. Typically used for filters.
        /// </summary>
        public override string htmlAfterBody { get; set; } = "";
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Include all nameValue pairs required to refresh the page if someone clicks on a header. For example, if there is a filter dateTo that is not empty, add dateTo=1/1/2000 to the RQS
        /// </summary>
        public override string refreshQueryString {
            get {
                return refreshQueryString_Local;
            }
            set {
                refreshQueryString_Local = value;
                //refreshQueryStringSet_Local = true;
            }
        }


        private string refreshQueryString_Local = "";
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
        public string baseAjaxUrl { set; get; }
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