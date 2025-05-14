

using Contensive.BaseClasses;
using Contensive.Processor.Controllers;
using System;

namespace Contensive.Processor.LayoutBuilder {
    public class LayoutBuilderClass : Contensive.BaseClasses.LayoutBuilder.LayoutBuilderBaseClass {
        //
        //
        public LayoutBuilderClass(CPBaseClass cp) : base(cp) {
            ////
            //// -- if an ajax callback, get the baseUrl comes the request, else it is the url of the current page
            //baseUrl = cp.Request.GetText("LayoutBuilderBaseUrl");
            //if (string.IsNullOrEmpty(baseUrl)) {
            //    //
            //    // -- if request is not present then this is the original page render. Populate with the current page
            //    // -- if however it is present, then this is the ajax callback, and the value in the request was the original page url
            //    baseUrl = $"{cp.Request.Protocol}{cp.Request.Host}{cp.Request.PathPage}?{cp.Request.QueryString}";
            //}
            //addFormHidden("layoutBuilderBaseUrl", baseUrl);
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// The addon of the Addon to be called to refresh the view on search or pagination. 
        /// Typically the same method that calls the layoutbuilder
        /// </summary>
        public override string callbackAddonGuid { get; set; }
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
        /// if true, this layoutBuilder will not be contained in other layoutBuilder content. This is used by the default getHtml() to include an outer div with the htmlId "afw", and the styles and javascript
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
        public override string warningMessage { get; set; } = "";
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
        [Obsolete("move javascript and styles to layouts", false)] public override string styleSheet => Properties.Resources.layoutBuilderStyles;
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// The default Layoutbuilder script. Override to customize.
        /// </summary>
        [Obsolete("move javascript and styles to layouts", false)] public override string javascript => Properties.Resources.layoutBuilderJavaScript;
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Optional. If set, this value will populate the title in the subnav of the portalbuilder
        /// </summary>
        public override string portalSubNavTitle { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// A virtual filename to a download of the report data. Leave blank to prevent download file
        /// </summary>
        public override string csvDownloadFilename { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// a list of buttons added with addButton().
        /// </summary>
        public string buttonList { get; set; } = "";
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// a list of hidden inputs added with addFormHidden()
        /// </summary>
        public string hiddenList { get; set; } = "";
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Set true to wrap this layout in a form. Default true
        /// </summary>
        public override bool includeForm { get; set; } = true;
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// The body of the layout.
        /// </summary>
        public override string body { get; set; } = "";
        //
        // ----------------------------------------------------------------------------------------------------
        //
        /// <summary>
        /// An html block added to the left of the Body. Typically used for filters.
        /// </summary>
        public override string htmlLeftOfBody { get; set; } = "";
        //
        // ----------------------------------------------------------------------------------------------------
        //
        /// <summary>
        /// An html block added above the Body. Typically used for filters.
        /// </summary>
        public override string htmlBeforeBody { get; set; } = "";
        //
        // ----------------------------------------------------------------------------------------------------
        //
        /// <summary>
        /// An html block added below the Body. Typically used for filters.
        /// </summary>
        public override string htmlAfterBody { get; set; } = "";
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// The action attribute of the form element that wraps the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        [Obsolete("Deprecated. No longer needed.", false)] public override string formActionQueryString { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// if true, the optional form tag will be blocked. The form tag is added automaatically if buttons, hiddens or a form-action is added
        /// </summary>
        [Obsolete("Deprecated. Use includeForm to prevent the form tag.", false)] public override bool blockFormTag { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        /// <summary>
        /// The default body. Typically you would create a layout by adding content to the individual elements and calling this method. Oveerride this method and consider using AdminUIHtmlController.getReportDoc()
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override string getHtml() {
            //
            // add user errors
            string userErrors = cp.Utils.ConvertHTML2Text(cp.UserError.GetList());
            if (!string.IsNullOrEmpty(userErrors)) {
                warningMessage += userErrors;
            }
            //
            // -- base report layout
            string layout = cp.Layout.GetLayout(Constants.layoutAdminUILayoutBuilderBaseGuid, Constants.layoutAdminUILayoutBuilderBaseName, Constants.layoutAdminUILayoutBuilderBaseCdnPathFilename);
            string result = cp.Mustache.Render(layout, this);
            //
            // -- add the baseAjaxUrl to the form
            addFormHidden("baseAjaxUrl", baseAjaxUrl);
            addFormHidden("callbackAddonGuid", callbackAddonGuid);
            //
            // -- add all hiddens to the html
            result += hiddenList;
            //
            // -- wrap with form
            if (includeForm) {
                result = cp.Html.Form(result, "", "", "afwForm");
            }
            //
            // -- set the optional title of the portal subnav
            if (!string.IsNullOrEmpty(portalSubNavTitle)) { cp.Doc.SetProperty("portalSubNavTitle", portalSubNavTitle); }
            //
            // -- add dependency on jQuery BlockUI 
            cp.Addon.ExecuteDependency(Constants.addonGuidJQueryBlockUI);
            return result;
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
            hiddenList += "<input type=\"hidden\" name=\"" + Name + "\" value=\"" + Value + "\" id=\"" + htmlId + "\">";
        }
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
        // ----------------------------------------------------------------------------------------------------
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
        }

        [Obsolete("Deprecated. Use getHtml()", false)]
         public override string getHtml(CPBaseClass cp) {
            return getHtml();
        }

        //
        //====================================================================================================
        //
        /// <summary>
        /// Include all nameValue pairs required to refresh the page if someone clicks on a header. For example, if there is a filter dateTo that is not empty, add dateTo=1/1/2000 to the RQS
        /// </summary>
        [Obsolete("Deprecated. Use baseUrl().", false)]
        public override string refreshQueryString { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
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
        // ----------------------------------------------------------------------------------------------------
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
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// deprecated. Use warningMessage instead
        /// </summary>
        [Obsolete("Deprecated. Use addFormHidden.", false)]
        public override string formid {
            get {
                return _formid;
            }
            set {
                addFormHidden("formId", value);
                _formid = value;
            }
        }
        private string _formid;
        //
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// The base url to use when creating links. Set internally to the url of the current page. If this is an ajax callback, this will be the url of the page that called the ajax
        /// </summary>
        [Obsolete("Deprecated. use callbackAddonGuid.", false)] public override string baseUrl { get; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// The url to the ajax method that will be called to refresh the page. This is used by the default getHtml() to include in the hidden fields. This is the url of the current page
        /// </summary>
        [Obsolete("Deprecated. Use callbackAddonGuid", false)] public override string baseAjaxUrl { get; set; }

    }
}
