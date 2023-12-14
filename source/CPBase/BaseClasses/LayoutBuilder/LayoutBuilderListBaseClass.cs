
using System;
using Contensive.BaseClasses;

namespace Contensive.BaseClasses.LayoutBuilder {
    /// <summary>
    /// A tabular list of data rows with filters on the left.
    /// </summary>
    public abstract class LayoutBuilderListBaseClass {
        //
        //-------------------------------------------------
        /// <summary>
        /// Set true if this tool is requested directly and not embedded in another AdminUI form
        /// </summary>
        public abstract bool isOuterContainer { get; set; }
        //
        //-------------------------------------------------
        /// <summary>
        /// Add padding around the body
        /// </summary>
        public abstract int reportRowLimit { get; }
        //
        //-------------------------------------------------
        /// <summary>
        /// Add padding around the body
        /// </summary>
        public abstract bool includeBodyPadding { get; set; }
        //
        //-------------------------------------------------
        /// <summary>
        /// Add background color to the body
        /// </summary>
        public abstract bool includeBodyColor { get; set; }

        // 
        //-------------------------------------------------
        /// <summary>
        /// Method retrieves the rendered html. Call this method after populating all object elements
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public abstract string getHtml(CPBaseClass cp);
        //
        //-------------------------------------------------
        /// <summary>
        /// use this area for optional filters
        /// </summary>
        public abstract string htmlLeftOfTable { get; set; }
        //
        //-------------------------------------------------
        /// <summary>
        /// optional html before the table
        /// </summary>
        public abstract string htmlBeforeTable { get; set; }
        //
        //-------------------------------------------------
        /// <summary>
        /// optional html after the table
        /// </summary>
        public abstract string htmlAfterTable { get; set; }
        //
        //-------------------------------------------------
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
        //-------------------------------------------------
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
        //-------------------------------------------------
        /// <summary>
        /// This report will be wrapped in a form tag and the action should send traffic back to the same page. If empty, the form uses cp.Doc.RefreshQueryString
        /// </summary>
        public abstract string formActionQueryString { get; set; }
        //
        //-------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        public abstract string formId { get; set; }
        //
        //-------------------------------------------------
        //
        public abstract string guid { get; set; }
        //
        //-------------------------------------------------
        //
        public abstract string name { get; set; }
        //
        //-------------------------------------------------
        //
        public abstract string title { get; set; }
        //
        //-------------------------------------------------
        //
        public abstract string warning { get;  }
        //
        //-------------------------------------------------
        //
        public abstract string description { get; set; }
        //
        //-------------------------------------------------
        //
        public abstract string columnName { get; set; }
        //
        //-------------------------------------------------
        //
        public abstract string columnCaption { get; set; }
        //
        //-------------------------------------------------
        //
        public abstract string columnCaptionClass { get; set; }
        //
        //-------------------------------------------------
        //
        public abstract string columnCellClass { get; set; }
        //
        //-------------------------------------------------
        //
        public abstract bool columnSortable { get; set; }
        //
        //-------------------------------------------------
        //
        public abstract bool columnVisible { get; set; }
        //
        //-------------------------------------------------
        //
        public abstract bool columnDownloadable { get; set; }
        //
        //-------------------------------------------------
        //
        public abstract int columnWidthPercent { get; set; }
        //
        //-------------------------------------------------
        //
        public abstract void addColumn();
        //
        //====================================================================================================
        /// 
        public abstract void addColumn(ReportListColumnBaseClass column);
        //-------------------------------------------------
        //
        public abstract void addRow();
        //
        //-------------------------------------------------
        //
        public abstract bool excludeRowFromDownload { get; set; }
        //
        //-------------------------------------------------
        //
        public abstract void addRowClass(string styleClass);
        //
        //-------------------------------------------------
        //
        public abstract void setCell(string content);
        public abstract void setCell(string reportContent, string downloadContent);
        //
        //-------------------------------------------------
        //
        public abstract void setCell(int content);
        public abstract void setCell(int content, int downloadContent);
        //
        //-------------------------------------------------
        //
        public abstract void setCell(double content);
        public abstract void setCell(double content, double downloadContent);
        //
        //-------------------------------------------------
        //
        public abstract void setCell(bool content);
        public abstract void setCell(bool content, bool downloadContent);
        //
        //-------------------------------------------------
        //
        public abstract void setCell(DateTime? content);
        public abstract void setCell(DateTime? content, DateTime? downloadContent);
        //
        //-------------------------------------------------
        //
        public abstract bool addCsvDownloadCurrentPage { get; set; }


        //
        //-------------------------------------------------
        /// <summary>
        /// if true, the optional form tag will be blocked. The form tag is added automaatically if buttons, hiddens or a form-action is added
        /// </summary>
        public abstract bool blockFormTag { get; set; }
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
        //-------------------------------------------------
        /// <summary>
        /// The default Layoutbuilder styles. Override to customize.
        /// </summary>
        public abstract string styleSheet { get; }
        //
        //-------------------------------------------------
        /// <summary>
        /// The default Layoutbuilder script. Override to customize.
        /// </summary>
        public abstract string javascript { get; }
        //
        //-------------------------------------------------
        /// <summary>
        /// Optional. If set, this value will populate the title in the subnav of the portalbuilder
        /// </summary>
        public abstract string portalSubNavTitle { get; set; }
        //
        //-------------------------------------------------
        /// <summary>
        /// A virtual filename to a download of the report data. Leave blank to prevent download file
        /// </summary>
        public abstract string csvDownloadFilename { get; set; }
        //
        //-------------------------------------------------
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
        /// <summary>
        /// The body of the layout.
        /// </summary>
        public abstract string body { get; set; }
        //
        /// <summary>
        /// Include all nameValue pairs required to refresh the page if someone clicks on a header. For example, if there is a filter dateTo that is not empty, add dateTo=1/1/2000 to the RQS
        /// </summary>
        public abstract string refreshQueryString { get; set; }
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
