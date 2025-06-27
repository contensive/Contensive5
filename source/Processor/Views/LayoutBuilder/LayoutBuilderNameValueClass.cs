
using Contensive.BaseClasses;
using Contensive.BaseClasses.LayoutBuilder;
using Contensive.Processor.Addons.AdminSite;
using Contensive.Processor.Controllers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;

namespace Contensive.Processor.LayoutBuilder {
    public class LayoutBuilderNameValueClass : LayoutBuilderNameValueBaseClass {
        //
        /// <summary>
        /// used for pagination and export. Setter included to support older legacy code that used cp parameter in getHtml(cp).
        /// </summary>
        private CPBaseClass cp { get; set; }
        //
        // constructors
        //
        /// <summary>
        /// prefered constructor
        /// </summary>
        /// <param name="cp"></param>
        public LayoutBuilderNameValueClass(CPBaseClass cp) : base(cp) {
            this.cp = cp;
        }
        //
        /// <summary>
        /// data structure for mustache rendering a row
        /// </summary>
        public List<LayoutBuilderNameValueClass_RowClass> rowList { get; set; } = new List<LayoutBuilderNameValueClass_RowClass>();
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
        /// start a new html fieldset
        /// </summary>
        /// <param name="caption"></param>
        public override void openFieldSet(string caption) {
            fieldSetPtrStack.Push(fieldSetPtr);
            if (fieldSetMax < fieldSetSize) {
                fieldSetMax += 1;
            }
            fieldSetPtr = fieldSetMax;
            fieldSets[fieldSetPtr].caption = caption;
            fieldSets[fieldSetPtr].rowOpen = rowList.Count + 1;
        }
        //
        // ====================================================================================================
        /// <summary>
        /// close  fieldset. creates an html fieldset around the field elements
        /// </summary>
        public override void closeFieldSet() {
            if (fieldSetPtr >= 0) {
                fieldSets[fieldSetPtr].rowClose = rowList.Count;
            }
            if (fieldSetPtrStack.Count > 0) {
                fieldSetPtr = (int)fieldSetPtrStack.Pop();
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Optional. If set, this value will populate the title in the subnav of the portalbuilder
        /// </summary>
        public override string portalSubNavTitle { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        // add a form hidden
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Add hidden field. Also creates a form element wrapping the layout.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public override void addFormHidden(string name, string value) {
            hiddenList += Constants.cr + "<input type=\"hidden\" name=\"" + name + "\" value=\"" + value + "\">";
        }
        private string hiddenList = "";
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
        // ----------------------------------------------------------------------------------------------------
        // setForm
        // ----------------------------------------------------------------------------------------------------
        //
        /// <summary>
        /// Sets the action attribute to the layout's form.
        /// </summary>
        [Obsolete("Deprecated. Not needed.", false)]
        public override string formAction { get; set; }
        //
        //
        // ----------------------------------------------------------------------------------------------------
        // body
        // ----------------------------------------------------------------------------------------------------
        //
        public override string body { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        // add a row
        // ----------------------------------------------------------------------------------------------------
        //
        public override void addRow() {
            rowList.Add(new LayoutBuilderNameValueClass_RowClass() {
                rowColumnList = [],
                rowHas1Column = true,
                rowHas2Columns = false
            });
            rowList.Last().rowColumnList.Add(new LayoutBuilderNameValueClass_RowClass_ColumnClass());
        }
        //
        // ----------------------------------------------------------------------------------------------------
        // add a column to the row
        // ----------------------------------------------------------------------------------------------------
        //
        public override void addColumn() {
            rowList.Last().rowColumnList.Add(new LayoutBuilderNameValueClass_RowClass_ColumnClass());
        }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public override string rowHtmlId {
            get {
                checkRowCnt();
                return rowList.Last().rowColumnList.Last().columnHtmlId;
            }
            set {
                rowList.Last().rowColumnList.Last().columnHtmlId = value;
                //checkRowCnt();
                //rows[rowCnt].htmlId = value;
                //rowList.Last().rowColumnList.Last().columnHtmlId = value;
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public override string rowName {
            get {
                checkRowCnt();
                return rowList.Last().rowColumnList.Last().columnName;
                //return rows[rowCnt].name;
            }
            set {
                rowList.Last().rowColumnList.Last().columnName = value;
                //checkRowCnt();
                //rows[rowCnt].name = value;
                //rowList.Last().rowColumnList.Last().columnName = value;
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public override string rowValue {
            get {
                //checkRowCnt();
                return rowList.Last().rowColumnList.Last().columnValue;
                //return rows[rowCnt].value;
            }
            set {
                rowList.Last().rowColumnList.Last().columnValue = value;
                //checkRowCnt();
                //rows[rowCnt].value = value;
                //rowList.Last().rowColumnList.Last().columnValue = value;
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public override string rowHelp {
            get {
                return rowList.Last().rowColumnList.Last().columnHelp;
                //checkRowCnt();
                //return rows[rowCnt].help;
            }
            set {
                rowList.Last().rowColumnList.Last().columnHelp = value;
                //checkRowCnt();
                //rows[rowCnt].help = value;
                //rowList.Last().rowColumnList.Last().columnHelp = value;
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        // ----------------------------------------------------------------------------------------------------
        //
        private void checkRowCnt() {
            //if (rowCnt < 0) {
            //    addRow();
            //}
        }

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
        // ====================================================================================================
        /// <summary>
        /// Render the stored structure to an html form
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override string getHtml() {
            if (!string.IsNullOrEmpty(portalSubNavTitle)) { cp.Doc.SetProperty("portalSubNavTitle", portalSubNavTitle); }
            //
            //string result = "";
            //string rowName;
            //string rowValue;
            //
            // -- add user errors
            string userErrors = cp.Utils.ConvertHTML2Text(cp.UserError.GetList());
            if (!string.IsNullOrEmpty(userErrors)) {
                warningMessage += userErrors;
            }
            //
            // -- create body from layout
            string layout = cp.Layout.GetLayout(Constants.layoutAdminUILayoutBuilderNameValueBodyGuid, Constants.layoutAdminUILayoutBuilderNameValueBodyName, Constants.layoutAdminUILayoutBuilderNameValueBodyCdnPathFilename);
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
                portalSubNavTitle = portalSubNavTitle
            };
            return layoutBase.getHtml();
        }




        //
        // ====================================================================================================
        //
        /// <summary>
        /// the maximum number of fields allowed
        /// </summary>
        const int fieldSetSize = 999;
        //
        /// <summary>
        /// the number of fields used in this form
        /// </summary>
        private int fieldSetMax = -1;
        //
        /// <summary>
        /// the current field being updated
        /// </summary>
        private int fieldSetPtr = -1;
        //
        /// <summary>
        /// the structure of data saved to each field
        /// </summary>
        struct FieldSetStruct {
            public string caption;
            public int rowOpen;
            public int rowClose;
        }
        //
        /// <summary>
        /// fieldsets are used to group fields visually with ah html fieldset element
        /// </summary>
        private readonly FieldSetStruct[] fieldSets = new FieldSetStruct[fieldSetSize];
        //
        /// <summary>
        /// fieldsets are used to group fields visually with ah html fieldset element
        /// </summary>
        private readonly Stack fieldSetPtrStack = new Stack();

        public bool tableFilter { /* todo */ get { return false; } set { } }
        public bool removeFilters { /* todo */ get { return false; } set { } }

        public override void addFilterGroup(LayoutBuilderFilterGroupRequest filterRequest) {
            throw new NotImplementedException();
        }
        //
        //
        // ====================================================================================================
        // -- deprecated
        //

        [Obsolete("Deprecated. Use getHtml()", false)]
        public override string getHtml(CPBaseClass cp) {
            return getHtml();
        }

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
        //
        [Obsolete("Deprecated. Use warningMessage", false)] public override string warning { get; set; }
        //
        /// <summary>
        /// if true, the optional form tag will be blocked. The form tag is added automatically if buttons, hiddens or a form-action is added
        /// </summary>
        [Obsolete("Deprecated. Use includeForm.", false)] public override bool blockFormTag { get; set; }
        //
        /// <summary>
        /// The action attribute of the form element that wraps the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        [Obsolete("Deprecated. No longer needed.", false)] public override string formActionQueryString { get; set; }
        //
        /// <summary>
        /// Include all nameValue pairs required to refresh the page if someone clicks on a header. For example, if there is a filter dateTo that is not empty, add dateTo=1/1/2000 to the RQS
        /// </summary>
        [Obsolete("Deprecated. No longer needed.", false)] public override string refreshQueryString { get; set; }
        //
        /// <summary>
        /// The html to add after the table. This is typically used for pagination controls, but can be used for anything.
        /// </summary>
        [Obsolete("Depricated. Use htmlAfterTable", false)]
        public override string footer {
            get {
                return htmlAfterBody;
            }
            set {
                htmlAfterBody = value;
            }
        }
        //
        /// <summary>
        /// The base url to use when creating links. Set internally to the url of the current page. If this is an ajax callback, this will be the url of the page that called the ajax
        /// </summary>
        [Obsolete("Deprecated. Instead use cp.adminUI.getPortalFeatureLink()", false)] public override string baseUrl { get; }
        //
        /// <summary>
        /// The url to the ajax method that will be called to refresh the page. This is used by the default getHtml() to include in the hidden fields. This is the url of the current page
        /// </summary>
        [Obsolete("Deprecated. use callbackAddonGuid", false)] public override string baseAjaxUrl { get; set; }
        //
        /// <summary>
        /// The default Layoutbuilder styles. Override to customize.
        /// </summary>
        [Obsolete("move javascript and styles to layouts", false)] public override string styleSheet => Processor.Properties.Resources.layoutBuilderStyles;
        //
        /// <summary>
        /// The default Layoutbuilder script. Override to customize.
        /// </summary>
        [Obsolete("move javascript and styles to layouts", false)] public override string javascript => Processor.Properties.Resources.layoutBuilderJavaScript;
    }
    //
    public class LayoutBuilderNameValueClass_RowClass {

        //public string rowName { get; set; } = "";
        //public string rowValue { get; set; } = "";
        //public string rowHelp { get; set; } = "";
        //public string rowHtmlId { get; set; } = "";
        public bool rowHas1Column { get; set; } = true;
        public bool rowHas2Columns { get; set; } = false;
        public bool rowHas4Columns { get; set; } = false;
        public List<LayoutBuilderNameValueClass_RowClass_ColumnClass> rowColumnList { get; set; } = [];

    }
    //
    public class LayoutBuilderNameValueClass_RowClass_ColumnClass {

        public string columnName { get; set; } = "";
        public string columnValue { get; set; } = "";
        public string columnHelp { get; set; } = "";
        public string columnHtmlId { get; set; } = "";

    }
}
