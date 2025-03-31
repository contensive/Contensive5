using Contensive.BaseClasses;
using Contensive.BaseClasses.LayoutBuilder;
using System;

namespace Contensive.Processor.LayoutBuilder {
    public class LayoutBuilderContentWithTabsClass : LayoutBuilderContentWithTabsBaseClass {
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// used for pagination and export. Setter included to support older legacy code that used cp parameter in getHtml(cp).
        /// </summary>
        private CPClass cp { get; set; }
        //
        public LayoutBuilderContentWithTabsClass(CPBaseClass cp) : base(cp) {
            this.cp = (CPClass)cp;
        }
        //
        const int tabSize = 99;
        struct navStruct {
            public string caption;
            public string link;
            public bool active;
            public string styleClass;
        }
        navStruct[] navs = new navStruct[tabSize];
        int tabMax = -1;
        int tabPtr = -1;
        //
        string localBody = "";
        string localTitle = "";
        string localWarning = "";
        bool localIsOuterContainer = false;
        //
        // ====================================================================================================
        /// <summary>
        /// Optional. If set, this value will populate the title in the subnav of the portalbuilder
        /// </summary>
        public override string portalSubNavTitle { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public override bool isOuterContainer {
            get {
                return localIsOuterContainer;
            }
            set {
                localIsOuterContainer = value;
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// add a form hidden input to the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        public override bool includeForm { get; set; } = false;
        //
        // ----------------------------------------------------------------------------------------------------
        //
        // ----------------------------------------------------------------------------------------------------
        //
        private void checkTabPtr() {

            if (tabPtr < 0) {
                addTab();
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public override string styleSheet {
            get {
                return Processor.Properties.Resources.layoutBuilderStyles;
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public override string javascript {
            get {
                return Processor.Properties.Resources.layoutBuilderJavaScript;
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        // body
        // ----------------------------------------------------------------------------------------------------
        //
        public override string body {
            get {
                return localBody;
            }
            set {
                localBody = value;
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        // Title
        // ----------------------------------------------------------------------------------------------------
        //
        public override string title {
            get {
                return localTitle;
            }
            set {
                localTitle = value;
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        // Warning
        // ----------------------------------------------------------------------------------------------------
        //
        public override string warning {
            get {
                return localWarning;
            }
            set {
                localWarning = value;
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        // Description
        // ----------------------------------------------------------------------------------------------------
        //
        public override string description {
            get {
                return localDescription;
            }
            set {
                localDescription = value;
            }
        }
        private string localDescription = "";
        //
        // ----------------------------------------------------------------------------------------------------
        // 
        // ----------------------------------------------------------------------------------------------------
        //
        public override string tabCaption {
            get {
                checkTabPtr();
                return navs[tabPtr].caption;
            }
            set {
                checkTabPtr();
                navs[tabPtr].caption = value;
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        // 
        // ----------------------------------------------------------------------------------------------------
        //
        public override string tabStyleClass {
            get {
                checkTabPtr();
                return navs[tabPtr].styleClass;
            }
            set {
                checkTabPtr();
                navs[tabPtr].styleClass = value;
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        // 
        // ----------------------------------------------------------------------------------------------------
        //
        public override string tabLink {
            get {
                checkTabPtr();
                return navs[tabPtr].link;
            }
            set {
                checkTabPtr();
                navs[tabPtr].link = value;
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        // 
        // ----------------------------------------------------------------------------------------------------
        //
        public override void setActiveTab(string caption) {
            int ptr = 0;
            for (ptr = 0; ptr <= tabMax; ptr++) {
                if (navs[ptr].caption.ToLower() == caption.ToLower()) {
                    navs[ptr].active = true;
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
            if (tabPtr < tabSize) {
                tabPtr += 1;
                navs[tabPtr].caption = "";
                navs[tabPtr].link = "";
                navs[tabPtr].active = false;
                if (tabPtr > tabMax) {
                    tabMax = tabPtr;
                }
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        // get
        // ----------------------------------------------------------------------------------------------------
        //
        public override string getHtml() {
            string result = "";
            string list = "";
            string item = "";
            string tabStyleClass;
            //
            // if outer container, add styles and javascript
            //
            if (localIsOuterContainer) {
                cp.Doc.AddHeadJavascript(javascript);
                cp.Doc.AddHeadStyle(styleSheet);
            }
            //
            for (tabPtr = 0; tabPtr <= tabMax; tabPtr++) {
                item = navs[tabPtr].caption;
                if (item != "") {
                    tabStyleClass = navs[tabPtr].styleClass;
                    if (navs[tabPtr].link != "") {
                        item = "<a href=\"" + navs[tabPtr].link + "\">" + item + "</a>";
                    }
                    if (navs[tabPtr].active) {
                        tabStyleClass += " afwTabActive";
                    }
                    if (tabStyleClass != "") {
                        tabStyleClass = " class=\"" + tabStyleClass + "\"";
                    }
                    item = Constants.cr + "<li" + tabStyleClass + ">" + item + "</li>";
                    list += item;
                }
            }
            if (list != "") {
                result += ""
                    + Constants.cr + "<ul class=\"afwBodyTabs\">"
                    + indent(list)
                    + Constants.cr + "</ul>";
            }
            //
            // headers
            //
            if (description != "") {
                result = Constants.cr + "<p id=\"afwDescription\">" + description + "</p>" + result;
            }
            if (string.IsNullOrEmpty(warning)) {
                string userErrors = cp.Utils.ConvertHTML2Text(cp.UserError.GetList());
                if (userErrors != "") { warning = userErrors; }
            }
            if (!string.IsNullOrEmpty(warning)) {
                result = Constants.cr + "<div id=\"afwWarning\">" + warning + "</div>" + result;
            }
            if (localTitle != "") {
                result = Constants.cr + "<h2 id=\"afwTitle\">" + localTitle + "</h2>" + result;
            }
            if (localBody != "") {
                result += localBody;
            }
            result = ""
                + Constants.cr + "<div class=\"afwTabbedBody\">"
                + indent(result)
                + Constants.cr + "</div>";
            //
            // if outer container, add styles and javascript
            //
            if (localIsOuterContainer) {
                cp.Doc.AddHeadJavascript(javascript);
                cp.Doc.AddHeadStyle(styleSheet);
                result = ""
                    + Constants.cr + "<div id=\"afw\">"
                    + indent(result)
                    + Constants.cr + "</div>";
            }
            //
            // -- set the optional title of the portal subnav
            if (!string.IsNullOrEmpty(portalSubNavTitle)) { cp.Doc.SetProperty("portalSubNavTitle", portalSubNavTitle); }
            return result;
        }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        private string indent(string src) {
            return src.Replace(Constants.cr, Constants.cr2);
        }
        //
        // ----------------------------------------------------------------------------------------------------
        //

        [Obsolete("Deprecated. Use getHtml().",false)] public override string getHtml(CPBaseClass cp) {
            throw new System.NotImplementedException();
        }
    }
}
