
using System;

namespace Contensive.BaseClasses.LayoutBuilder {
    /// <summary>
    /// A tabular list of data rows with filters on the left.
    /// </summary>
    public abstract class LayoutBuilderListBaseClass(CPBaseClass cp) : LayoutBuilderBaseClass(cp) {
        //
        /// <summary>
        /// The sql search term used to filter the data set when a user types the term into the search box.
        /// </summary>
        public abstract string sqlSearchTerm { get; }
        //
        /// <summary>
        /// The sql orderby clause used to order the data set. Created from the user clicking on a column header.
        /// </summary>
        public abstract string sqlOrderBy { get; }
        /// <summary>
        /// reset pagination back to page 1. Use to force the view to page when when the query filter changes.
        /// Mus tbe called before accessing pagination fields or query elements.
        /// </summary>
        public abstract void paginationReset();
        /// <summary>
        /// The default records per page to be displayed. The user may make changes, reflected in paginationPageSize
        /// </summary>
        public abstract int paginationPageSizeDefault { get; set; }
        /// <summary>
        /// records per page to be displayed. If the user has not changed it, this is paginationPageSizeDefault
        /// </summary>
        public abstract int paginationPageSize { get; }
        /// <summary>
        /// 1-based page number
        /// </summary>
        public abstract int paginationPageNumber { get; }
        /// <summary>
        /// In the pagination section, this phrase replaces the word 'records' in '# records found'
        /// </summary>
        public abstract string  paginationRecordAlias { get; set; }
        /// <summary>
        /// adds this name/url as an ellipse menu item to the current column
        /// </summary>
        /// <param name="name"></param>
        /// <param name="url"></param>
        public abstract void addRowEllipseMenuItem(string name, string url);
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Add padding around the body
        /// </summary>
        public abstract int reportRowLimit { get; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// The total records in the dataset, not just what is displayed on the page
        /// </summary>
        public abstract int recordCount { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        [Obsolete("Deprecated. use addHidden() to add a formId hidden tag", false)] public abstract string formId { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        [Obsolete("Deprecated. Saved reports are no longer supported.", false)] public abstract string guid { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        [Obsolete("Deprecated. Saved reports are no longer supported.", false)] public abstract string name { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public abstract string columnName { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public abstract string columnCaption { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public abstract string columnCaptionClass { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public abstract string columnCellClass { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public abstract bool columnSortable { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public abstract bool columnVisible { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public abstract bool columnDownloadable { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public abstract int columnWidthPercent { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public abstract void addColumn();
        //
        //====================================================================================================
        /// 
        public abstract void addColumn(ReportListColumnBaseClass column);
        // ----------------------------------------------------------------------------------------------------
        //
        public abstract void addRow();
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public abstract bool excludeRowFromDownload { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public abstract void addRowClass(string styleClass);
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// set the cell content for the current row.
        /// </summary>
        /// <param name="content"></param>
        public abstract void setCell(string content);
        /// <summary>
        /// set the cell content for the current row.
        /// </summary>
        /// <param name="reportContent"></param>
        /// <param name="downloadContent"></param>
        public abstract void setCell(string reportContent, string downloadContent);
        /// <summary>
        /// set the cell content for the current row.
        /// </summary>
        /// <param name="reportContent"></param>
        /// <param name="downloadContent"></param>
        public abstract void setCell(string reportContent, int downloadContent);
        /// <summary>
        /// set the cell content for the current row.
        /// </summary>
        /// <param name="reportContent"></param>
        /// <param name="downloadContent"></param>
        public abstract void setCell(string reportContent, double downloadContent);
        /// <summary>
        /// set the cell content for the current row.
        /// </summary>
        /// <param name="reportContent"></param>
        /// <param name="downloadContent"></param>
        public abstract void setCell(string reportContent, DateTime downloadContent);
        /// <summary>
        /// set the cell content for the current row.
        /// </summary>
        /// <param name="reportContent"></param>
        /// <param name="downloadContent"></param>
        public abstract void setCell(string reportContent, bool downloadContent);
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public abstract void setCell(int content);
        public abstract void setCell(int content, int downloadContent);
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public abstract void setCell(double content);
        public abstract void setCell(double content, double downloadContent);
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public abstract void setCell(bool content);
        public abstract void setCell(bool content, bool downloadContent);
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public abstract void setCell(DateTime? content);
        public abstract void setCell(DateTime? content, DateTime? downloadContent);
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        ///  See allowDownloadButton for download details.  
        /// </summary>
        [Obsolete("Deprecated. See allowDownloadButton for download details.", false)] public abstract bool addCsvDownloadCurrentPage { get; set; }

        //
        [Obsolete("Deprecated. Use addFormHidden()", false)]
        public override string formid {
            get {
                return formId_local;
            }
            set {
                addFormHidden("formid", value);
                formId_local = value;
            }
        }
        private string formId_local = "";
    }
    //
    //====================================================================================================
    /// <summary>
    /// The data used to build a column
    /// </summary>
    public class ReportListColumnBaseClass {
        public string name { get; set; }
        public string caption { get; set; }
        public string captionClass { get; set; }
        public string cellClass { get; set; }
        public bool sortable { get; set; } = false;
        public bool visible { get; set; } = true;
        public bool downloadable { get; set; } = false;
        /// <summary>
        /// set as an integer between 1 and 100. This value will be added as the width of the column in a style tag
        /// </summary>
        public int columnWidthPercent { get; set; } = 10;
    }
    //
    public class EllipseMenuItem {
        public string name { get; set; }
        public string url { get; set; }
    }
}
