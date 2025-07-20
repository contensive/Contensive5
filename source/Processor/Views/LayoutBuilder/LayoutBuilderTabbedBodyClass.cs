using Contensive.BaseClasses;
using Contensive.BaseClasses.LayoutBuilder;
using Contensive.Processor.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;

namespace Contensive.Processor.LayoutBuilder {
    public class LayoutBuilderTabbedBodyClass : LayoutBuilderTabbedBodyBaseClass {
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// used for pagination and export. Setter included to support older legacy code that used cp parameter in getHtml(cp).
        /// </summary>
        private CPClass cp { get; set; }
        //
        public LayoutBuilderTabbedBodyClass(CPBaseClass cp) : base(cp) {
            this.cp = (CPClass)cp;
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// tmp. convert to match -list pattern
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public override void addActiveFilter(string caption, string name, string value) {
            if (string.IsNullOrEmpty(caption)) { return; }
            activeFilters ??= [];
            activeFilters.Add(new LayoutBuilder_ActiveFilter() {
                activeFilterCaption = caption,
                activeFilterName = name,
                activeFilterValue = value
            });
        }
        private List<LayoutBuilder_ActiveFilter> activeFilters = null;

        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// tmp. If the layoutBuilderBase filter works, convert this to match -list pattern, create a LayoutBase during constructor and pass the filter calls through. Then the getHtml does not need to pass local values to the base, they are there
        /// </summary>
        /// <param name="caption"></param>
        /// <exception cref="NotImplementedException"></exception>
        public override void addFilterGroup(string caption) {
            throw new NotImplementedException();
        }
        public override bool getFilterBoolean(string filterHtmlName, string viewName) {
            throw new NotImplementedException();
        }
        public override int getFilterInteger(string filterHtmlName, string viewName) {
            throw new NotImplementedException();
        }
        public override string getFilterText(string filterHtmlName, string viewName) {
            throw new NotImplementedException();
        }
        public override DateTime? getFilterDate(string filterHtmlName, string viewName) {
            throw new NotImplementedException();
        }
        public override void addFilterCheckbox(string caption, string htmlName, string htmlValue, bool selected) {
            throw new NotImplementedException();
        }
        public override void addFilterRadio(string caption, string htmlName, string htmlValue, bool selected) {
            throw new NotImplementedException();
        }
        public override void addFilterTextInput(string caption, string htmlName, string htmlValue) {
            throw new NotImplementedException();
        }
        public override void addFilterDateInput(string caption, string htmlName, DateTime? htmlDateValue) {
            throw new NotImplementedException();
        }
        public override void addFilterSelect(string caption, string htmlName, List<NameValueSelected> options) {
            throw new NotImplementedException();
        }
        public override void addFilterSelectContent(string caption, string htmlName, int htmlValue, string content, string sqlCriteria) {
            throw new NotImplementedException();
        }
        public override void addFilterSelectContent(string caption, string htmlName, int htmlValue, string content, string sqlCriteria, string nonCaption) {
            throw new NotImplementedException();
        }
        //
        /// <summary>
        /// The guid of the addon that refreshes the view for search or pagination update.
        /// Typically the addon that created the layout.
        /// </summary>
        public override string callbackAddonGuid { get; set; }
        //
        /// <summary>
        /// add a form hidden input to the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        public override bool includeForm { get; set; } = true;
        //
        public override bool includeBodyPadding { get; set; } = true;
        //
        public override bool includeBodyColor { get; set; } = true;
        //
        public override bool isOuterContainer { get; set; }
        //
        public override string title { get; set; }
        //
        public override string description { get; set; }
        //
        /// <summary>
        /// Optional. If set, this value will populate the title in the subnav of the portalbuilder
        /// </summary>
        public override string portalSubNavTitle { get; set; }
        //
        /// <summary>
        /// Add hidden field. Also creates a form element wrapping the layout.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public override void addFormHidden(string name, string value) {
            hiddenList += Constants.cr + "<input type=\"hidden\" name=\"" + name + "\" value=\"" + value + "\">";
        }
        private string hiddenList { get; set; }
        /// <summary>
        /// Add hidden field. Also creates a form element wrapping the layout.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public override void addFormHidden(string name, int value) => addFormHidden(name, value.ToString());
        /// <summary>
        /// Add hidden field. Also creates a form element wrapping the layout.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public override void addFormHidden(string name, double value) => addFormHidden(name, value.ToString());
        /// <summary>
        /// Add hidden field. Also creates a form element wrapping the layout.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public override void addFormHidden(string name, DateTime value) => addFormHidden(name, value.ToString());
        /// <summary>
        /// Add hidden field. Also creates a form element wrapping the layout.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public override void addFormHidden(string name, bool value) => addFormHidden(name, value.ToString());
        //
        // ----------------------------------------------------------------------------------------------------
        // add a form button
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Adds a button to the button panel. Also creates a form element wrapping the layout.
        /// </summary>
        /// <param name="buttonValue"></param>
        public override void addFormButton(string buttonValue) {
            addFormButton(buttonValue, "button", "", "");
        }
        /// <summary>
        /// Adds a button to the button panel. Also creates a form element wrapping the layout.
        /// </summary>
        /// <param name="buttonValue"></param>
        /// <param name="buttonName"></param>
        public override void addFormButton(string buttonValue, string buttonName) {
            addFormButton(buttonValue, buttonName, "", "");
        }
        /// <summary>
        /// Adds a button to the button panel. Also creates a form element wrapping the layout.
        /// </summary>
        /// <param name="buttonValue"></param>
        /// <param name="buttonName"></param>
        /// <param name="buttonId"></param>
        public override void addFormButton(string buttonValue, string buttonName, string buttonId) {
            addFormButton(buttonValue, buttonName, buttonId, "");
        }
        /// <summary>
        /// Adds a button to the button panel. Also creates a form element wrapping the layout.
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
        //
        // ----------------------------------------------------------------------------------------------------
        // body
        // ----------------------------------------------------------------------------------------------------
        //
        public override string body { get; set; }

        public override void addFormHidden(string name, string value, string htmlId) {
            hiddenList += Constants.cr + $"{Constants.cr}<input type=\"hidden\" name=\"{name}\" value=\"{value}\" id=\"{htmlId}\">";
        }

        public override void addFormHidden(string name, int value, string htmlId) {
            hiddenList += Constants.cr + $"{Constants.cr}<input type=\"hidden\" name=\"{name}\" value=\"{value}\" id=\"{htmlId}\">";
        }

        public override void addFormHidden(string name, double value, string htmlId) {
            hiddenList += Constants.cr + $"{Constants.cr}<input type=\"hidden\" name=\"{name}\" value=\"{value}\" id=\"{htmlId}\">";
        }

        public override void addFormHidden(string name, DateTime value, string htmlId) {
            hiddenList += Constants.cr + $"{Constants.cr}<input type=\"hidden\" name=\"{name}\" value=\"{value}\" id=\"{htmlId}\">";
        }

        public override void addFormHidden(string name, bool value, string htmlId) {
            hiddenList += Constants.cr + $"{Constants.cr}<input type=\"hidden\" name=\"{name}\" value=\"{value}\" id=\"{htmlId}\">";
        }

        public override void addLinkButton(string buttonCaption, string link) {
            buttonList += LayoutBuilderController.a(buttonCaption, link);
        }

        public override void addLinkButton(string buttonCaption, string link, string htmlId) {
            buttonList += LayoutBuilderController.a(buttonCaption, link, htmlId);
        }

        public override void addLinkButton(string buttonCaption, string link, string htmlId, string htmlClass) {
            buttonList += LayoutBuilderController.a(buttonCaption, link, htmlId, htmlClass);
        }

        /// <summary>
        /// A virtual filename to a download of the report data. Leave blank to prevent download file
        /// </summary>
        public override string csvDownloadFilename { get; set; }
        //
        /// <summary>
        /// message displayed as a warning message. Not an error, but an issue of some type
        /// </summary>
        public override string warningMessage { get; set; }
        //
        /// <summary>
        /// message displayed as a fail message. Data is wrong
        /// </summary>
        public override string failMessage { get; set; }
        //
        /// <summary>
        /// message displayed as an informational message. Nothing is wrong, but the user should know
        /// </summary>
        public override string infoMessage { get; set; }
        //
        public override string successMessage { get; set; }
        //
        /// <summary>
        /// An html block added to the left of the table. Typically used for filters.
        /// </summary>
        public override string htmlLeftOfBody { get; set; } = "";
        /// <summary>
        /// An html block added above the table. Typically used for filters.
        /// </summary>
        public override string htmlBeforeBody { get; set; } = "";
        /// <summary>
        /// An html block added below the table. Typically used for filters.
        /// </summary>
        public override string htmlAfterBody { get; set; } = "";
        //
        // ----------------------------------------------------------------------------------------------------
        // tab processing
        // ----------------------------------------------------------------------------------------------------
        //
        //
        public class TabClass {
            public string tabClass { get; set; }
            public string tabLink { get; set; }
            public string tabCaption { get; set; }
            public bool tabActive { get; set; }
        }
        public List<TabClass> tabList { get; set; } = [];
        //
        public override string tabCaption {
            get {
                return tabList.Last().tabCaption;
            }
            set {
                tabList.Last().tabCaption = value;
            }
        }
        //
        public override string tabStyleClass {
            get {
                return tabList.Last().tabClass;
            }
            set {
                tabList.Last().tabClass = value;
            }
        }
        //
        public override string tabLink {
            get {
                return tabList.Last().tabLink;
            }
            set {
                tabList.Last().tabLink = value;
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        // 
        // ----------------------------------------------------------------------------------------------------
        //
        public override void setActiveTab(string caption) {
            if (string.IsNullOrEmpty(caption)) { return; }
            foreach (var tab in tabList) {
                if (tab.tabCaption.ToLower() == caption.ToLower()) {
                    tab.tabActive = true;
                    return;
                }
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        // add a column
        // ----------------------------------------------------------------------------------------------------
        //
        /// <summary>
        /// Add a navigation entry. The navCaption and navLink should be set after creating a new entry. The first nav entry does not need to be added.
        /// </summary>
        public override void addTab() {
            tabList.Add(new TabClass());
        }
        //
        // ----------------------------------------------------------------------------------------------------
        // get
        // ----------------------------------------------------------------------------------------------------
        //
        public override string getHtml() {
            if (!string.IsNullOrEmpty(portalSubNavTitle)) { cp.Doc.SetProperty("portalSubNavTitle", portalSubNavTitle); }
            //
            // -- add user errors
            string userErrors = cp.Utils.ConvertHTML2Text(cp.UserError.GetList());
            if (!string.IsNullOrEmpty(userErrors)) {
                warningMessage += userErrors;
            }
            //
            // -- create body from layout
            string layout = cp.Layout.GetLayout(Constants.layoutAdminUILayoutBuilderTabbedBodyGuid, Constants.layoutAdminUILayoutBuilderTabbedBodyName, Constants.layoutAdminUILayoutBuilderTabbedBodyCdnPathFilename);
            string bodyHtml = cp.Mustache.Render(layout, this);
            //
            // -- construct report
            LayoutBuilderClass layoutBase = new(cp) {
                body = bodyHtml,
                includeBodyPadding = includeBodyPadding,
                includeBodyColor = includeBodyColor,
                buttonList = buttonList,
                csvDownloadFilename = "",
                description = description,
                hiddenList = hiddenList,
                includeForm = includeForm,
                isOuterContainer = isOuterContainer,
                title = title,
                warningMessage = warningMessage,
                failMessage = failMessage,
                infoMessage = infoMessage,
                successMessage = successMessage,
                baseAjaxUrl = baseAjaxUrl,
                callbackAddonGuid = callbackAddonGuid,
                htmlLeftOfBody = htmlLeftOfBody,
                htmlBeforeBody = htmlBeforeBody,
                htmlAfterBody = htmlAfterBody,
                portalSubNavTitle = portalSubNavTitle,
                activeFilters = activeFilters,
            };
            return layoutBase.getHtml();
            //-------------copied from namevalue
        }
        //navStruct[] navs = new navStruct[tabSize];
        //int tabMax = -1;
        //int tabPtr = -1;
        //
        //string localBody = "";
        //string localTitle = "";
        //string localWarning = "";
        //bool localIsOuterContainer = false;
        ////
        //private void checkTabPtr() {

        //    if (tabPtr < 0) {
        //        addTab();
        //    }
        //}
        ////
        //private string indent(string src) {
        //    return src.Replace(Constants.cr, Constants.cr2);
        //}

        //
        [Obsolete("move javascript and styles to layouts", false)]
        public override string styleSheet {
            get {
                return Processor.Properties.Resources.layoutBuilderStyles;
            }
        }
        //
        [Obsolete("move javascript and styles to layouts", false)]
        public override string javascript {
            get {
                return Processor.Properties.Resources.layoutBuilderJavaScript;
            }
        }

        [Obsolete("Deprecated. Use getHtml()", false)]
        public override string getHtml(CPBaseClass cp) {
            return getHtml();
        }
        public bool tableFilter { /* todo */ get { return false; } set { } }
        public bool removeFilters { /* todo */ get { return false; } set { } }

        [Obsolete("Deprecated. Use includeForm.", false)] public override bool blockFormTag { get; set; }
        [Obsolete("Deprecated. No longer needed.", false)] public override string formActionQueryString { get; set; }
        [Obsolete("Depricated. Use htmlAfterTable", false)]
        public override string footer {
            get {
                return htmlAfterBody;
            }
            set {
                htmlAfterBody = value;
            }
        }
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

        [Obsolete("Deprecated. Instead use cp.adminUI.getPortalFeatureLink()", false)] public override string baseUrl { get; }

        [Obsolete("Deprecated. use callbackAddonGuid", false)] public override string baseAjaxUrl { get; set; }
        [Obsolete("Deprecated. No longer needed.", false)] public override string refreshQueryString { get; set; }
        [Obsolete("Deprecated. Use warningMessage", false)] public override string warning { get; set; }
    }
}
