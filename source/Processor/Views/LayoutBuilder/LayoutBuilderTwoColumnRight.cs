
using Contensive.BaseClasses;
using Contensive.Processor.Controllers;
using System;

namespace Contensive.Processor.LayoutBuilder {
    public class LayoutBuilderTwoColumnRight : BaseClasses.LayoutBuilder.LayoutBuilderTwoColumnRightBaseClass {
        //
        public LayoutBuilderTwoColumnRight(CPBaseClass cp) : base(cp) {
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Deprecated. Instead use cp.adminUI.getPortalFeatureLink()
        /// </summary>
        [Obsolete("Deprecated. Instead use cp.adminUI.getPortalFeatureLink()", false)] public override string baseUrl { get; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// The guid of the addon that refreshes the view for search or pagination update.
        /// Typically the addon that created the layout.
        /// </summary>
        public override string callbackAddonGuid { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        [Obsolete("Deprecated. use callbackAddonGuid", false)] public override string baseAjaxUrl { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public override string contentRight { get; set; } = "";
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public override string contentLeft { get; set; } = "";
        //
        // ----------------------------------------------------------------------------------------------------
        //
        [Obsolete("deprecated, Use title", false)]
        public override string headline {
            get {
                return title;
            }
            set {
                title = value;
            }
        }
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
        /// add a form hidden input to the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        public override bool includeForm { get; set; } = false;
        //
        // ----------------------------------------------------------------------------------------------------
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
        /// The default body. Typically you would create a layout by adding content to the individual elements and calling this method. Oveerride this method and consider using AdminUIHtmlController.getReportDoc()
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override string getHtml() {
            //
            // -- construct body
            //
            // -- render layout
            string layout = cp.Layout.GetLayout(Constants.guidLayoutAdminUITwoColumnRight, Constants.layoutAdminUITwoColumnRightName, Constants.layoutAdminUITwoColumnRightCdnPathFilename);
            //
            LayoutBuilderClass layoutBase = new(cp) {
                body = cp.Mustache.Render(layout, this),
                includeBodyPadding = includeBodyPadding,
                includeBodyColor = includeBodyColor,
                buttonList = buttonList,
                csvDownloadFilename = csvDownloadFilename,
                description = description,
                hiddenList = hiddenList,
                includeForm = includeForm,
                isOuterContainer = isOuterContainer,
                title = title,
                warningMessage = warningMessage,
                failMessage = failMessage,
                infoMessage = infoMessage,
                successMessage = successMessage,
                htmlAfterBody = htmlAfterBody,
                htmlBeforeBody = htmlBeforeBody,
                htmlLeftOfBody = htmlLeftOfBody
            };
            string result = layoutBase.getHtml();
            //
            // -- set the optional title of the portal subnav
            if (!string.IsNullOrEmpty(portalSubNavTitle)) { cp.Doc.SetProperty("portalSubNavTitle", portalSubNavTitle); }
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
        private string hiddenList = "";
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
        private string buttonList = "";
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// The action attribute of the form element that wraps the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        [Obsolete("Deprecated. No longer needed.", false)] public override string formActionQueryString { get; set; }
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
        /// An html block added to the left of the table. Typically used for filters.
        /// </summary>
        public override string htmlLeftOfBody { get; set; } = "";
        //
        // ----------------------------------------------------------------------------------------------------
        //
        /// <summary>
        /// An html block added above the table. Typically used for filters.
        /// </summary>
        public override string htmlBeforeBody { get; set; } = "";
        //
        // ----------------------------------------------------------------------------------------------------
        //
        /// <summary>
        /// An html block added below the table. Typically used for filters.
        /// </summary>
        public override string htmlAfterBody { get; set; } = "";
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Include all nameValue pairs required to refresh the page if someone clicks on a header. For example, if there is a filter dateTo that is not empty, add dateTo=1/1/2000 to the RQS
        /// </summary>
        [Obsolete("Deprecated. No longer needed.", false)] public override string refreshQueryString { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// deprecated. Use warningMessage instead
        /// </summary>
        [Obsolete("Deprecated. Use warningMessage instead", false)]
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
        /// deprecated. Use htmlAfterTable instead
        /// </summary>
        [Obsolete("Deprecated. Use htmlAfterTable instead", false)]
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
        [Obsolete("Deprecated. Use addFormHidden instead", false)]
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

        [Obsolete("Deprecated. Use getHtml()", false)]
        public override string getHtml(CPBaseClass cp) {
            return getHtml();
        }

        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        [Obsolete("Deprecated. Use includeForm.", false)] public override bool blockFormTag { get; set; }
    }
}
