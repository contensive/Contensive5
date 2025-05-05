
using System;

namespace Contensive.BaseClasses.LayoutBuilder {
    /// <summary>
    /// A tabular list of data rows with filters on the left.
    /// </summary>
    public abstract class LayoutBuilderListBaseClass(CPBaseClass cp) {
        private CPBaseClass cp { get; set; } = cp;
        //
        /// <summary>
        /// This property is setup by the constructor to the base url
        /// (Previously this was the refreshQueryString.)
        /// Set the url to the url of the current view. This is used to refresh the view when a user clicks on a link, like a column header to sort the data.
        /// This url is saved as a hidden in submitted forms and should be read and used to create links that refresh the view.
        /// The area where the hiddens are saved is not repainted duting ajax refreshes, so this value is set one time when the page is first drawn.
        /// When a user clicks on a feature on the view, like pagination, the view is redrawn with an ajax call. That call has a different baseUrlf so links created during that call need the baseUrl of the view.
        /// </summary>
        public abstract string baseUrl { get; }
        /// <summary>
        /// deprecated. Instead set callbackMethodGuid to the guid of the addon method that returns the current view.
        /// When a search or pagination is performed, the view is refreshed by calling this method.
        /// </summary>
        [Obsolete("Deprecated. Instead set callbackMethodGuid to the guid of the addon that creates the updated view on  pagination or search update. Typically this is the same addon tha calls the layout builder.",false)] 
        public abstract string baseAjaxUrl { get; set; }
        /// <summary>
        ///  The guid of the addon method that returns the current view.
        ///  Typically this is the method that calls the layout builder.
        /// When a search or pagination is performed, the view is refreshed by calling this method.
        /// </summary>
        public abstract string callbackMethodGuid { get; set; }
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
        //
        /// <summary>
        /// if true, the data set is paginated. If false, all records are displayed.
        /// 
        /// </summary>
        public abstract bool allowPagination { get; }
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
        /// adds this name/url as an ellipse menu item to the current column
        /// </summary>
        /// <param name="name"></param>
        /// <param name="url"></param>
        public abstract void addRowEllipseMenuItem(string name, string url);
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Set true if this tool is requested directly and not embedded in another AdminUI form
        /// </summary>
        public abstract bool isOuterContainer { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// If true, the resulting html is wrapped in a form element whose action returns execution back to this addon where is it processed here in the same code.
        /// </summary>
        public abstract bool includeForm { get; set; }
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
        /// Add padding around the body
        /// </summary>
        public abstract bool includeBodyPadding { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Add background color to the body
        /// </summary>
        public abstract bool includeBodyColor { get; set; }
        // 
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Method retrieves the rendered html. Call this method after populating all object elements
        /// </summary>
        /// <returns></returns>
        public abstract string getHtml();
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// use this area for optional filters
        /// </summary>
        public abstract string htmlLeftOfBody { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// optional html before the Body
        /// </summary>
        public abstract string htmlBeforeBody { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// optional html after the Body
        /// </summary>
        public abstract string htmlAfterBody { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Add hidden form input
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Value"></param>
        public abstract void addFormHidden(string Name, string Value);
        /// <summary>
        /// Add hidden form input
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public abstract void addFormHidden(string name, int value);
        /// <summary>
        /// Add hidden form input
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public abstract void addFormHidden(string name, double value);
        /// <summary>
        /// Add hidden form input
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public abstract void addFormHidden(string name, DateTime value);
        /// <summary>
        /// Add hidden form input
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public abstract void addFormHidden(string name, bool value);
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Add form button
        /// </summary>
        /// <param name="buttonValue"></param>
        public abstract void addFormButton(string buttonValue);
        /// <summary>
        /// Add form button
        /// </summary>
        /// <param name="buttonValue"></param>
        /// <param name="buttonName"></param>
        public abstract void addFormButton(string buttonValue, string buttonName);
        /// <summary>
        /// Add form button
        /// </summary>
        /// <param name="buttonValue"></param>
        /// <param name="buttonName"></param>
        /// <param name="buttonId"></param>
        public abstract void addFormButton(string buttonValue, string buttonName, string buttonId);
        /// <summary>
        /// Add form button
        /// </summary>
        /// <param name="buttonValue"></param>
        /// <param name="buttonName"></param>
        /// <param name="buttonId"></param>
        /// <param name="buttonClass"></param>
        public abstract void addFormButton(string buttonValue, string buttonName, string buttonId, string buttonClass);
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// This report will be wrapped in a form tag and the action should send traffic back to the same page. If empty, the form uses cp.Doc.RefreshQueryString
        /// </summary>
        [Obsolete("Deprecated. No longer needed.", false)] public abstract string formActionQueryString { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        public abstract string formId { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public abstract string guid { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public abstract string name { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public abstract string title { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public abstract string warning { get; }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public abstract string description { get; set; }
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
        //
        public abstract void setCell(string content);
        public abstract void setCell(string reportContent, string downloadContent);
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
        //
        public abstract bool addCsvDownloadCurrentPage { get; set; }


        //
        /// <summary>
        /// message displayed as a warning message. Not an error, but an issue of some type
        /// </summary>
        public abstract string warningMessage { get; set; }
        //
        /// <summary>
        /// message displayed as a fail message. Data is wrong
        /// </summary>
        public abstract string failMessage { get; set; }
        //
        /// <summary>
        /// message displayed as an informational message. Nothing is wrong, but the user should know
        /// </summary>
        public abstract string infoMessage { get; set; }
        //
        /// <summary>
        /// message displayed as a success message.
        /// </summary>
        public abstract string successMessage { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// The default Layoutbuilder styles. Override to customize.
        /// </summary>
        [Obsolete("move javascript and styles to layouts", false)] public abstract string styleSheet { get; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// The default Layoutbuilder script. Override to customize.
        /// </summary>
        [Obsolete("move javascript and styles to layouts", false)] public abstract string javascript { get; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Optional. If set, this value will populate the title in the subnav of the portalbuilder
        /// </summary>
        public abstract string portalSubNavTitle { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// A virtual filename to a download of the report data. Leave blank to prevent download file
        /// </summary>
        public abstract string csvDownloadFilename { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// add a form hidden input to the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Value"></param>
        /// <param name="htmlId"></param>
        public abstract void addFormHidden(string Name, string Value, string htmlId);
        //
        /// <summary>
        /// add a form hidden input to the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="htmlId"></param>
        public abstract void addFormHidden(string name, int value, string htmlId);
        //
        /// <summary>
        /// add a form hidden input to the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="htmlId"></param>
        public abstract void addFormHidden(string name, double value, string htmlId);
        //
        /// <summary>
        /// add a form hidden input to the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="htmlId"></param>
        public abstract void addFormHidden(string name, DateTime value, string htmlId);
        //
        /// <summary>
        /// add a form hidden input to the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="htmlId"></param>
        public abstract void addFormHidden(string name, bool value, string htmlId);
        //
        /// <summary>
        /// Create a button with a link that does not submit a form. When clicked it anchors to the link
        /// </summary>
        /// <param name="buttonCaption"></param>
        /// <param name="link"></param>
        public abstract void addLinkButton(string buttonCaption, string link);
        //
        /// <summary>
        /// Create a button with a link that does not submit a form. When clicked it anchors to the link
        /// </summary>
        /// <param name="buttonCaption"></param>
        /// <param name="link"></param>
        /// <param name="htmlId"></param>
        public abstract void addLinkButton(string buttonCaption, string link, string htmlId);
        //
        /// <summary>
        /// Create a button with a link that does not submit a form. When clicked it anchors to the link
        /// </summary>
        /// <param name="buttonCaption"></param>
        /// <param name="link"></param>
        /// <param name="htmlId"></param>
        /// <param name="htmlClass"></param>
        public abstract void addLinkButton(string buttonCaption, string link, string htmlId, string htmlClass);
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// if true, the optional form tag will be blocked. The form tag is added automaatically if buttons, hiddens or a form-action is added
        /// </summary>
        [Obsolete("Deprecated. To prevent the form tag set includeForm=false.", false)] public abstract bool blockFormTag { get; set; }
        //
        /// <summary>
        /// Include all nameValue pairs required to refresh the page if someone clicks on a header. For example, if there is a filter dateTo that is not empty, add dateTo=1/1/2000 to the RQS
        /// </summary>
        [Obsolete("Deprecated. Instead use BaseUrl.", false)] public abstract string refreshQueryString { get; set; }
        // 
        /// <summary>
        /// Method retrieves the rendered html. Call this method after populating all object elements
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        [Obsolete("Deprecated. Use getHtml().", false)] public abstract string getHtml(CPBaseClass cp);
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
