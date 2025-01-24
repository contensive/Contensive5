

using Contensive.BaseClasses;
using Contensive.Processor.Controllers;
using System;

namespace Contensive.Processor.LayoutBuilder {
    public class LayoutBuilderClass : Contensive.BaseClasses.LayoutBuilder.LayoutBuilderBaseClass {
        //
        public LayoutBuilderClass(CPBaseClass cp):base(cp) {
            //
            // -- if an ajax callback, get the baseUrl comes the request, else it is the url of the current page
            baseUrl = cp.Request.GetText("LayoutBuilderBaseUrl");
            if (string.IsNullOrEmpty(baseUrl)) {
                //
                // -- if request is not present then this is the original page render. Populate with the current page
                // -- if however it is present, then this is the ajax callback, and the value in the request was the original page url
                baseUrl = $"{cp.Request.Protocol}{cp.Request.Host}{cp.Request.PathPage}?{cp.Request.QueryString}";
            }
            addFormHidden("layoutBuilderBaseUrl", baseUrl);
        }
        //
        //-------------------------------------------------
        /// <summary>
        /// The base url to use when creating links. Set internally to the url of the current page. If this is an ajax callback, this will be the url of the page that called the ajax
        /// </summary>
        public override string baseUrl { get; }
        //
        //-------------------------------------------------
        /// <summary>
        /// The url to the ajax method that will be called to refresh the page. This is used by the default getHtml() to include in the hidden fields. This is the url of the current page
        /// </summary>
        public override  string baseAjaxUrl { get; set; }
        //
        //-------------------------------------------------
        /// <summary>
        /// if true, the optional form tag will be blocked. The form tag is added automaatically if buttons, hiddens or a form-action is added
        /// </summary>
        public override bool blockFormTag { get; set; }
        //
        //-------------------------------------------------
        /// <summary>
        /// if true, the container between the button rows will include default padding
        /// </summary>
        public override bool includeBodyPadding { get; set; } = true;
        //
        //-------------------------------------------------
        /// <summary>
        /// if true, the container between the button rows will include the default background color. Else it is transparent.
        /// </summary>
        public override bool includeBodyColor { get; set; } = true;
        //
        //-------------------------------------------------
        /// <summary>
        /// if true, this layoutBuilder will not be contained in other layoutBuilder content. This is used by the default getHtml() to include an outer div with the htmlId "afw", and the styles and javascript
        /// </summary>
        public override bool isOuterContainer { get; set; } = false;
        //
        //-------------------------------------------------
        /// <summary>
        /// The headline at the top of the form
        /// </summary>
        public override string title { get; set; } = "";
        //
        /// <summary>
        /// message displayed as a warning message. Not an error, but an issue of some type
        /// </summary>
        public override string warningMessage { get; set; } = "";
        //
        /// <summary>
        /// message displayed as a fail message. Data is wrong
        /// </summary>
        public override string failMessage { get; set; } = "";
        //
        /// <summary>
        /// message displayed as an informational message. Nothing is wrong, but the user should know
        /// </summary>
        public override string infoMessage { get; set; } = "";
        //
        /// <summary>
        /// message displayed as a success message.
        /// </summary>
        public override string successMessage { get; set; } = "";
        //
        //-------------------------------------------------
        /// <summary>
        /// simple description text. Will be wrapped in an html paragraph tag.
        /// </summary>
        public override string description { get; set; } = "";
        //
        //-------------------------------------------------
        /// <summary>
        /// The default Layoutbuilder styles. Override to customize.
        /// </summary>
        public override string styleSheet => Properties.Resources.layoutBuilderStyles;
        //
        //-------------------------------------------------
        /// <summary>
        /// The default Layoutbuilder script. Override to customize.
        /// </summary>
        public override string javascript => Properties.Resources.layoutBuilderJavaScript;
        //
        //-------------------------------------------------
        /// <summary>
        /// Optional. If set, this value will populate the title in the subnav of the portalbuilder
        /// </summary>
        public override string portalSubNavTitle { get; set; }
        //
        //-------------------------------------------------
        /// <summary>
        /// A virtual filename to a download of the report data. Leave blank to prevent download file
        /// </summary>
        public override string csvDownloadFilename { get; set; }
        // 
        //-------------------------------------------------
        /// <summary>
        /// The default body. Typically you would create a layout by adding content to the individual elements and calling this method. Oveerride this method and consider using AdminUIHtmlController.getReportDoc()
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override string getHtml(CPBaseClass cp) {
            //
            // -- base report layout
            string layout = cp.Layout.GetLayout(Constants.layoutAdminUILayoutBuilderBaseGuid, Constants.layoutAdminUILayoutBuilderBaseName, Constants.layoutAdminUILayoutBuilderBaseCdnPathFilename);
            string result = cp.Mustache.Render(layout, this);
            //
            // -- add the baseAjaxUrl to the form
            addFormHidden("baseAjaxUrl", baseAjaxUrl);
            //
            // -- add all hiddens to the html
            result += hiddenList;
            //
            // -- wrap with form
            if (includeForm && !blockFormTag) {
                string action = !string.IsNullOrEmpty(formActionQueryString) ? formActionQueryString : !string.IsNullOrEmpty(refreshQueryString) ? refreshQueryString : cp.Doc.RefreshQueryString;
                result = cp.Html.Form(result, "", "", "afwForm", action, "");
            }
            //
            // -- set the optional title of the portal subnav
            if (!string.IsNullOrEmpty(portalSubNavTitle)) { cp.Doc.SetProperty("portalSubNavTitle", portalSubNavTitle); }
            return result;
        }

        //
        //-------------------------------------------------
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
        //
        public string hiddenList = "";
        /// <summary>
        /// add a form hidden input to the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        public bool includeForm { get; set; } = false;
        //
        /// <summary>
        /// add a form hidden input to the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Value"></param>
        public override void addFormHidden(string Name, string Value) => addFormHidden(Name, Value, "");
        //
        /// <summary>
        /// add a form hidden input to the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="htmlId"></param>
        public override void addFormHidden(string name, int value, string htmlId) => addFormHidden(name, value.ToString(), htmlId);
        //
        /// <summary>
        /// add a form hidden input to the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public override void addFormHidden(string name, int value) => addFormHidden(name, value.ToString(), "");
        //
        /// <summary>
        /// add a form hidden input to the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="htmlId"></param>
        public override void addFormHidden(string name, double value, string htmlId) => addFormHidden(name, value.ToString(), htmlId);
        //
        /// <summary>
        /// add a form hidden input to the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public override void addFormHidden(string name, double value) => addFormHidden(name, value.ToString());
        //
        /// <summary>
        /// add a form hidden input to the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="htmlId"></param>
        public override void addFormHidden(string name, DateTime value, string htmlId) => addFormHidden(name, value.ToString(), htmlId);
        //
        /// <summary>
        /// add a form hidden input to the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public override void addFormHidden(string name, DateTime value) => addFormHidden(name, value.ToString());
        //
        /// <summary>
        /// add a form hidden input to the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="htmlId"></param>
        public override void addFormHidden(string name, bool value, string htmlId) => addFormHidden(name, value.ToString(), htmlId);
        //
        /// <summary>
        /// add a form hidden input to the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public override void addFormHidden(string name, bool value) => addFormHidden(name, value.ToString());
        //
        /// <summary>
        /// Create a button with a link that does not submit a form. When clicked it anchors to the link
        /// </summary>
        /// <param name="buttonCaption"></param>
        /// <param name="link"></param>
        public override void addLinkButton(string buttonCaption, string link) {
            buttonList += LayoutBuilderController.a(buttonCaption, link);
        }
        //
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
        //-------------------------------------------------
        /// <summary>
        /// add a form button to the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        /// <param name="buttonValue"></param>
        public override void addFormButton(string buttonValue) {
            addFormButton(buttonValue, "button", "", "");
        }
        //
        /// <summary>
        /// add a form button to the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        /// <param name="buttonValue"></param>
        /// <param name="buttonName"></param>
        public override void addFormButton(string buttonValue, string buttonName) {
            addFormButton(buttonValue, buttonName, "", "");
        }
        //
        /// <summary>
        /// add a form button to the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        /// <param name="buttonValue"></param>
        /// <param name="buttonName"></param>
        /// <param name="buttonId"></param>
        public override void addFormButton(string buttonValue, string buttonName, string buttonId) {
            addFormButton(buttonValue, buttonName, buttonId, "");
        }
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
        public string buttonList = "";
        //
        //-------------------------------------------------
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
        //-------------------------------------------------
        /// <summary>
        /// The body of the layout.
        /// </summary>
        public override string body { get; set; } = "";
        //
        //-------------------------------------------------
        //
        /// <summary>
        /// An html block added to the left of the Body. Typically used for filters.
        /// </summary>
        public override string htmlLeftOfBody { get; set; } = "";
        //
        //-------------------------------------------------
        //
        /// <summary>
        /// An html block added above the Body. Typically used for filters.
        /// </summary>
        public override string htmlBeforeBody { get; set; } = "";
        //
        //-------------------------------------------------
        //
        /// <summary>
        /// An html block added below the Body. Typically used for filters.
        /// </summary>
        public override string htmlAfterBody { get; set; } = "";
        //
        //====================================================================================================
        /// <summary>
        /// Include all nameValue pairs required to refresh the page if someone clicks on a header. For example, if there is a filter dateTo that is not empty, add dateTo=1/1/2000 to the RQS
        /// </summary>
        [Obsolete("Instead use baseUrl.",false)] public override string refreshQueryString {
            get {
                return refreshQueryString_Local;
            }
            set {
                refreshQueryString_Local = value;
                //refreshQueryStringSet_Local = true;
            }
        }
        private string refreshQueryString_Local = "";
        //
        //-------------------------------------------------
        /// <summary>
        /// deprecated. Use warningMessage instead
        /// </summary>
        [Obsolete("Deprecated. Use warningMessage.", false)]
        public override string warning {
            get {
                return warningMessage;
            }
            set {
                warningMessage = value;
            }
        }
        //
        //-------------------------------------------------
        /// <summary>
        /// deprecated. Use htmlAfterBody instead
        /// </summary>
        [Obsolete("Deprecated. Use htmlAfterBody", false)]
        public override string footer {
            get {
                return htmlAfterBody;
            }
            set {
                htmlAfterBody = value;
            }
        }
        //
        //-------------------------------------------------
        /// <summary>
        /// deprecated. Use warningMessage instead
        /// </summary>
        [Obsolete("Deprecated. Use addFormHidden.", false)]
        public override string formid {
            get {
                return formid_local;
            }
            set {
                addFormHidden("formId", value);
                formid_local = value;
            }
        }


        private string formid_local;

    }

    //
    //public class LayoutBuilderBaseHtmlRequest {
    //    /// <summary>
    //    /// The body of the document
    //    /// </summary>
    //    public string body { get; set; }
    //    /// <summary>
    //    /// if not empty, this download will be included below the description
    //    /// </summary>
    //    public string csvDownloadFilename { get; set; }
    //    /// <summary>
    //    /// if text description
    //    /// </summary>
    //    public string description { get; set; }
    //    // 
    //    /// <summary>
    //    /// message displayed as a success message
    //    /// </summary>
    //    public string successMessage { get; set; } = "";
    //    // 
    //    /// <summary>
    //    /// message displayed as an informational message. Not a warning and not success, but something happened that the user needs to know about the results.
    //    /// </summary>
    //    public string infoMessage { get; set; } = "";
    //    // 
    //    /// <summary>
    //    /// message displayed as a warning message. Not an error, but an issue of some type
    //    /// </summary>
    //    public string warningMessage { get; set; } = "";
    //    // 
    //    /// <summary>
    //    /// message displayed as a fail message. The data is not correct
    //    /// </summary>
    //    public string failMessage { get; set; } = "";

    //    /// <summary>
    //    /// the document headline
    //    /// </summary>
    //    public string title { get; set; }
    //    /// <summary>
    //    /// if true, include padding around the doc (but not the buttons)
    //    /// </summary>
    //    public bool includeBodyPadding { get; set; }
    //    /// <summary>
    //    /// A list of htm tags that will be placed in the button sections
    //    /// </summary>
    //    public string buttonList { get; set; }
    //    /// <summary>
    //    /// a list of html tags that will be placed at the end of the form
    //    /// </summary>
    //    public string hiddenList { get; set; }
    //    /// <summary>
    //    /// if true a form will be added
    //    /// </summary>
    //    public bool includeForm { get; set; }
    //    /// <summary>
    //    /// if true, background color will be added
    //    /// </summary>
    //    public bool includeBodyColor { get; set; }
    //    /// <summary>
    //    /// the querystring to be added to the optional form
    //    /// </summary>
    //    public string formActionQueryString { get; set; }
    //    /// <summary>
    //    /// the querystring that will be used as the basis for links to the view
    //    /// </summary>
    //    public string refreshQueryString { get; set; }
    //    /// <summary>
    //    /// if true, the outer htmlid, styles and javascript will be added
    //    /// </summary>
    //    public bool isOuterContainer { get; set; }
    //    /// <summary>
    //    /// html elements that will be displayed to the left of the body
    //    /// </summary>
    //    public string htmlLeftOfBody { get; set; }
    //    /// <summary>
    //    /// html elements that will be displayed before the body
    //    /// </summary>
    //    public string htmlBeforeBody { get; set; }
    //    /// <summary>
    //    /// html elements that will be displayed after the body
    //    /// </summary>
    //    public string htmlAfterBody { get; set; }
    //    /// <summary>
    //    /// if true, the form tag will not be added
    //    /// </summary>
    //    public bool blockFormTag { get; set; }
    //    //
    //    /// <summary>
    //    /// This html layout includes functions like search, pagination and sort. 
    //    /// It includes javascript that calls the server back whent these features are clicked.
    //    /// This url is stored in a hidden field in the form so the server knows where to call back to.
    //    /// This url calls back to the client software to request a refresh of the page
    //    /// AdminUI adds parameters to the querystring that it reads and updates pageNumber, etc.
    //    /// </summary>
    //    public string baseAjaxUrl { get; set; }
    //    //
    //    [Obsolete("deprecated. Use warningMessage", false)]
    //    public string warning { get; set; }
    //}
}
