using System;

namespace Contensive.BaseClasses.LayoutBuilder {
    /// <summary>
    /// LayoutBuilders create the html for common Admin UI cases.
    /// They must be implemented 2 ways, first called by the page during the initial page load, and second called by the ajax refresh methods.
    /// Both methods return the exact same html, but the javascript that calls the ajax methods selectively replaces the important content.
    /// Set the constructor arguments baseUrl and baseAjaxUrl to manage these calls.
    /// baseUrl is the url of the initial page load and is used as the base url for links included on the page.
    /// It is set during the constructor, and should be read from the proproperty when used because when for example, the initial page might be /admin?srcViewId=10.
    /// baseAjaxUrl is the url to an ajax method that just returns the html for this form.
    /// </summary>
    public abstract class LayoutBuilderBaseClass(CPBaseClass cp) {
        private CPBaseClass cp { get; set; } = cp;
        //
        //-------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        public abstract string baseUrl { get; set; }
        //
        //-------------------------------------------------
        /// <summary>
        /// LayoutBuilder layouts require html page redraw with an ajax method. 
        /// Set this to the url of the ajax method that returns the html for the form.
        /// </summary>
        public abstract string baseAjaxUrl { get; set; }
        //
        //-------------------------------------------------
        /// <summary>
        /// if true, the optional form tag will be blocked. The form tag is added automaatically if buttons, hiddens or a form-action is added
        /// </summary>
        public abstract bool blockFormTag { get; set; }
        //
        //-------------------------------------------------
        /// <summary>
        /// if true, the container between the button rows will include default padding
        /// </summary>
        public abstract bool includeBodyPadding { get; set; }
        //
        //-------------------------------------------------
        /// <summary>
        /// if true, the container between the button rows will include the default background color. Else it is transparent.
        /// </summary>
        public abstract bool includeBodyColor { get; set; }
        //
        //-------------------------------------------------
        /// <summary>
        /// if true, this layoutBuilder will not be contained in other layoutBuilder content. This is used by the default getHtml() to include an outer div with the htmlId "afw", and the styles and javascript
        /// </summary>
        public abstract bool isOuterContainer { get; set; }
        //
        //-------------------------------------------------
        /// <summary>
        /// The headline at the top of the form
        /// </summary>
        public abstract string title { get; set; }
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
        /// simple description text. Will be wrapped in an html paragraph tag.
        /// </summary>
        public abstract string description { get; set; }
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
        /// The default body. Typically you would create a layout by adding content to the individual elements and calling this method. Oveerride this method and consider using HtmlController.getReportDoc()
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public abstract string getHtml(CPBaseClass cp);
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
        /// <param name="Name"></param>
        /// <param name="Value"></param>
        public abstract void addFormHidden(string Name, string Value);
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
        public abstract void addFormHidden(string name, int value);
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
        public abstract void addFormHidden(string name, double value);
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
        public abstract void addFormHidden(string name, DateTime value);
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
        /// add a form hidden input to the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public abstract void addFormHidden(string name, bool value);
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
        //-------------------------------------------------
        /// <summary>
        /// add a form button to the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        /// <param name="buttonValue"></param>
        public abstract void addFormButton(string buttonValue);
        //
        /// <summary>
        /// add a form button to the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        /// <param name="buttonValue"></param>
        /// <param name="buttonName"></param>
        public abstract void addFormButton(string buttonValue, string buttonName);
        //
        /// <summary>
        /// add a form button to the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        /// <param name="buttonValue"></param>
        /// <param name="buttonName"></param>
        /// <param name="buttonId"></param>
        public abstract void addFormButton(string buttonValue, string buttonName, string buttonId);
        /// <summary>
        /// add a form button to the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        /// <param name="buttonValue"></param>
        /// <param name="buttonName"></param>
        /// <param name="buttonId"></param>
        /// <param name="buttonClass"></param>
        public abstract void addFormButton(string buttonValue, string buttonName, string buttonId, string buttonClass);
        //
        //-------------------------------------------------
        /// <summary>
        /// The action attribute of the form element that wraps the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        public abstract string formActionQueryString { get; set; }
        //
        //-------------------------------------------------
        /// <summary>
        /// The body of the layout.
        /// </summary>
        public abstract string body { get; set; }
        //
        //-------------------------------------------------
        //
        /// <summary>
        /// An html block added to the left of the Body. Typically used for filters.
        /// </summary>
        public abstract string htmlLeftOfBody { get; set; }
        //
        //-------------------------------------------------
        //
        /// <summary>
        /// An html block added above the Body. Typically used for filters.
        /// </summary>
        public abstract string htmlBeforeBody { get; set; }
        //
        //-------------------------------------------------
        //
        /// <summary>
        /// An html block added below the Body. Typically used for filters.
        /// </summary>
        public abstract string htmlAfterBody { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// Include all nameValue pairs required to refresh the page if someone clicks on a header. For example, if there is a filter dateTo that is not empty, add dateTo=1/1/2000 to the RQS
        /// </summary>
        public abstract string refreshQueryString { get; set; }
        //
        //-------------------------------------------------
        /// <summary>
        /// deprecated. Use warningMessage instead
        /// </summary>
        [Obsolete("deprecated. Use warningMessage instead", false)]
        public abstract string warning { get; set; }
        //
        //-------------------------------------------------
        /// <summary>
        /// deprecated. Use htmlAfterBody instead
        /// </summary>
        [Obsolete("deprecated. Use htmlAfterBody instead", false)]
        public abstract string footer { get; set; }
        //
        //-------------------------------------------------
        /// <summary>
        /// deprecated. Use warningMessage instead
        /// </summary>
        [Obsolete("deprecated. Use addFormHidden instead", false)]
        public abstract string formid { get; set; }

    }
}
