

using Contensive.BaseClasses;
using Contensive.BaseClasses.LayoutBuilder;
using Contensive.Processor.Controllers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.CompilerServices;


namespace Contensive.Processor.LayoutBuilder {
    /// <summary>
    /// 
    /// </summary>
    public class LayoutBuilderClass : Contensive.BaseClasses.LayoutBuilder.LayoutBuilderBaseClass {
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// construtor
        /// </summary>
        /// <param name="cp"></param>
        public LayoutBuilderClass(CPBaseClass cp) : base(cp) { }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// set to true to display the download button.
        /// If the user clicks the download button, an ajax request is made that calls the client addon (must be set in .callbackAddonGuid).
        /// For the LayoutBuilderList, pagination will be disabled and rows/columns should be set as they do in non-download cases.
        /// Rows as .downloadable will be included in the resulting csv, which is returned in getHtml instead of the html form, which the ajax method then returns and is handled by the calling javscript.
        /// </summary>
        public override bool allowDownloadButton { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// For this LayoutBuilderList implementation, requestDownload can be ignored. The creation of the download is handled by the getHtml() method.
        /// </summary>
        public override bool requestDownload {
            get {
                return cp.Request.GetBoolean("downloadRequest");
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// if true, the layout will include a filter group. 
        /// This is used to determine if the layout should include a filter group or not.
        /// </summary>
        public bool includeFilter { get; set; } = false;
        //
        public List<LayoutBuilder_ActiveFilter> activeFilters { get; set; } = [];
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public bool includeResetFilterButton { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// The viewModel for layout mustache
        /// a list of filter groups. 
        /// populated with the addFilter() method
        /// Each group has a caption and a list of filters. 
        /// Each filter can be one of several types, checkbox, text, etc.
        /// </summary>
        public List<LayoutBuilderClass_FilterGroup> filterGroups { get; set; }
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
        /// The headline at the top of the form.
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
        /// Optional. If set, this value will populate the title in the subnav of the portalbuilder
        /// </summary>
        public override List<string> portalSubNavTitleList { get; set; } = [];
        [Obsolete("Use portalSubNavTitleList instead. Deprecated.", false)] public override string portalSubNavTitle { 
            get {
                if (portalSubNavTitleList != null && portalSubNavTitleList.Count > 0) {
                    return string.Join(Environment.NewLine + " ", portalSubNavTitleList);
                }
                return "";
            }
            set {
                if (string.IsNullOrEmpty(value)) {
                    portalSubNavTitleList = [];
                } else {
                    portalSubNavTitleList = [.. value.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries)];
                }
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// pathFilename of a file in cdnFiles.
        /// If set, the [Request Download] 
        /// A virtual filename to a download of the report data. Leave blank to prevent download file
        /// </summary>
        [Obsolete("Deprecated. To implement a download see allowDownloadButton.", false)] public override string csvDownloadFilename { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// a list of buttons added with addButton().
        /// </summary>
        public string buttonList { get; set; } = "";
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// true if either title or buttonList is set.
        /// </summary>
        public bool hasTitleOrButtonList { get { return !string.IsNullOrEmpty(title) || !string.IsNullOrEmpty(buttonList); } }
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
        //
        /// <summary>
        /// The default body. Typically you would create a layout by adding content to the individual elements and calling this method. Oveerride this method and consider using AdminUIHtmlController.getReportDoc()
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override string getHtml() {
            //
            // -- if download requested, return the csv download
            if (allowDownloadButton && requestDownload) {
                //cp.Doc.SetProperty("contentDisposition", "attachment; filename=\"" + csvDownloadFilename + "\"");
                //cp.Doc.SetProperty("contentType", "text/csv");
                //cp.Doc.SetProperty("contentEncoding", "utf-8");
                return "Download error, this form supports download (allowDownloadButton is true), but requestDownload is set and the form did not return the data.";
            }
            if (allowDownloadButton) {
                addFormButton(Constants.ButtonRequestDownload, Constants.RequestNameButton,"js-downloadButton");
            }
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
                result = cp.Html.Form(result, "", "", $"afwForm");
            }
            //
            // -- set the optional title of the portal subnav
            if (portalSubNavTitleList != null && portalSubNavTitleList.Count > 0) {
                cp.Doc.SetProperty("portalSubNavTitleList", string.Join("|", portalSubNavTitleList));
            }
            //
            // -- add dependency on jQuery BlockUI 
            cp.Addon.ExecuteDependency(Constants.addonGuidJQueryBlockUI);
            //
            cp.Log.Debug("LayoutBuilderClass.getHtml(), exit");
            //
            return result;
        }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public override void addActiveFilter(string caption, string name, string value) {
            if (string.IsNullOrEmpty(caption)) { return; }
            activeFilters ??= [];
            activeFilters.Add(new LayoutBuilder_ActiveFilter() {
                activeFilterCaption = caption,
                activeFilterName = name,
                activeFilterValue = value
            });
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <param name="caption"></param>
        public override void addFilterGroup(string caption) {
            filterGroups ??= [];
            filterGroups.Add(new LayoutBuilderClass_FilterGroup() {
                filterGroupCaption = caption,
                filterInputs = []
            });
            includeFilter = true;
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Read a checkbox filter back
        /// </summary>
        /// <param name="filterHtmlName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override bool getFilterBoolean(string filterHtmlName, string viewName) {
            string propertyName = $"{viewName}-{filterHtmlName}";
            if (cp.Doc.GetText("removeFilter") == filterHtmlName) {
                cp.Visit.SetProperty(propertyName, false);
                return false;
            }
            bool visitResult = cp.Visit.GetBoolean(propertyName);
            if (!cp.Doc.IsProperty(filterHtmlName) && !cp.Doc.IsProperty($"{filterHtmlName}-checkbox")) { 
                return visitResult; 
            }
            bool result = cp.Doc.GetBoolean(filterHtmlName);
            cp.Visit.SetProperty(propertyName, result);
            return result;
        }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public override string getFilterText(string filterHtmlName, string viewName) {
            string propertyName = $"{viewName}-{filterHtmlName}";
            if (cp.Doc.GetText("removeFilter") == filterHtmlName) {
                cp.Visit.SetProperty(propertyName, "");
                return "";
            }
            string visitResult = cp.Visit.GetText(propertyName);
            if (!cp.Doc.IsProperty(filterHtmlName) && !cp.Doc.IsProperty($"{filterHtmlName}-checkbox")) { 
                return visitResult; 
            }
            string result = cp.Doc.GetText(filterHtmlName);
            cp.Visit.SetProperty(propertyName, result);
            return result;
        }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public override int getFilterInteger(string filterHtmlName, string viewName) {
            string propertyName = $"{viewName}-{filterHtmlName}";
            if (cp.Doc.GetText("removeFilter") == filterHtmlName) {
                cp.Visit.SetProperty(propertyName, 0);
                return 0;
            }
            int visitResult = cp.Visit.GetInteger(propertyName);
            if (!cp.Doc.IsProperty(filterHtmlName) && !cp.Doc.IsProperty($"{filterHtmlName}-checkbox")) { 
                return visitResult; 
            }
            int result = cp.Doc.GetInteger(filterHtmlName);
            cp.Visit.SetProperty(propertyName, result);
            return result;
        }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public override DateTime? getFilterDate(string filterHtmlName, string viewName) {
            string propertyName = $"{viewName}-{filterHtmlName}";
            if (cp.Doc.GetText("removeFilter") == filterHtmlName) {
                cp.Visit.SetProperty(propertyName, null);
                return null;
            }
            DateTime? visitResult = cp.Visit.GetDate(propertyName);
            if (!cp.Doc.IsProperty(filterHtmlName) && !cp.Doc.IsProperty($"{filterHtmlName}-checkbox")) { 
                return visitResult == DateTime.MinValue ? null : visitResult; 
            }
            DateTime? result = cp.Doc.GetDate(filterHtmlName);
            cp.Visit.SetProperty(propertyName, result ?? DateTime.MinValue);
            return result == DateTime.MinValue ? null : result;
        }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public override void addFilterCheckbox(string caption, string htmlName, string htmlValue, bool selected) {
            filterGroups ??= [];
            if (filterGroups.Count == 0) { addFilterGroup(""); }
            includeFilter = true;
            filterGroups[filterGroups.Count - 1].filterInputs.Add(new LayoutBuilderClass_FilterGroup_Input() {
                filterIsCheckbox = true,
                filterCaption = caption,
                filterHtmlId = $"filter{cp.Utils.GetRandomString(4)}",
                selected = selected,
                name = htmlName,
                value = htmlValue
            });
            if (selected) {
                //
                // -- filter is selected, add the remove-filter button
                addActiveFilter(caption, "removeFilter", htmlName);
            }
            //
            // -- add hidden that can be detected in submitted form if checkbox is not selected
            // -- when an unchecked checkbox is submitted, it is not detected by isProperty(). 
            addFormHidden($"{htmlName}-checkbox", "1");
        }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public override void addFilterRadio(string caption, string htmlName, string htmlValue, bool selected) {
            filterGroups ??= [];
            if (filterGroups.Count == 0) { addFilterGroup(""); }
            includeFilter = true;
            filterGroups[filterGroups.Count - 1].filterInputs.Add(new LayoutBuilderClass_FilterGroup_Input() {
                filterIsRadio = true,
                filterCaption = caption,
                filterHtmlId = $"filter{cp.Utils.GetRandomString(4)}",
                selected = selected,
                name = htmlName,
                value = htmlValue
            });
            if (selected) {
                //
                // -- filter is selected, add the remove-filter button
                addActiveFilter(caption, "removeFilter", htmlName);
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public override void addFilterTextInput(string caption, string htmlName, string htmlValue) {
            filterGroups ??= [];
            if (filterGroups.Count == 0) { addFilterGroup(""); }
            includeFilter = true;
            filterGroups[filterGroups.Count - 1].filterInputs.Add(new LayoutBuilderClass_FilterGroup_Input() {
                filterIsText = true,
                filterCaption = caption,
                filterHtmlId = $"filter{cp.Utils.GetRandomString(4)}",
                name = htmlName,
                value = htmlValue
            });
            if (!string.IsNullOrEmpty(htmlValue)) {
                //
                // -- filter is selected, add the remove-filter button
                addActiveFilter(caption, "removeFilter", htmlName);
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// add a filter if the input is not null and not DateTime.MinValue.
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="htmlName"></param>
        /// <param name="htmlDateValue"></param>
        public override void addFilterDateInput(string caption, string htmlName, DateTime? htmlDateValue) {
            filterGroups ??= [];
            if (filterGroups.Count == 0) { addFilterGroup(""); }
            includeFilter = true;
            filterGroups[filterGroups.Count - 1].filterInputs.Add(new LayoutBuilderClass_FilterGroup_Input() {
                filterIsDate = true,
                filterCaption = caption,
                filterHtmlId = $"filter{cp.Utils.GetRandomString(4)}",
                name = htmlName,
                value = (htmlDateValue ?? DateTime.MinValue).Equals(DateTime.MinValue) ? "" : htmlDateValue.Value.ToString("yyyy-MM-dd")
            });
            if (htmlDateValue != null && !htmlDateValue.Equals(DateTime.MinValue)) {
                //
                // -- filter is selected, add the remove-filter button
                addActiveFilter($"{caption}:{((DateTime)htmlDateValue).ToShortDateString()}", "removeFilter", htmlName);
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public override void addFilterSelect(string caption, string htmlName, List<NameValueSelected> options) {
            filterGroups ??= [];
            if (filterGroups.Count == 0) { addFilterGroup(""); }
            filterGroups[filterGroups.Count - 1].filterInputs.Add(new LayoutBuilderClass_FilterGroup_Input() {
                filterIsSelect = true,
                filterCaption = caption,
                filterHtmlId = $"filter{cp.Utils.GetRandomString(4)}",
                name = htmlName,
                filterSelectOptions = options
            });
            NameValueSelected selectedOption = options.Find((x) => x.selected);
            if(selectedOption!=null && !string.IsNullOrEmpty(selectedOption.value)) {
                int maxLength = 20;
                string selectedOptionName = selectedOption.name.Length <= maxLength ? selectedOption.name : selectedOption.name.Substring(0, maxLength);
                addActiveFilter($"{caption}:{selectedOptionName}", "removeFilter", htmlName);
            }
        }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public override void addFilterSelectContent(string caption, string htmlName, int htmlValue, string content, string sqlCriteria, string nonCaption) {
            string table = cp.Content.GetTable(content);
            if (string.IsNullOrEmpty(table)) {
                throw new Exception("LayoutBuilderClass.addFilterSelectContent: content not found: " + content);
            }
            List<NameValueSelected> options = [];
            if (!string.IsNullOrEmpty(nonCaption)) {
                options.Add(new NameValueSelected(cp.Utils.EncodeText(nonCaption), "", false));
            }
            sqlCriteria = string.IsNullOrEmpty(sqlCriteria) ? "" : $"and({sqlCriteria})";
            using DataTable dt = cp.Db.ExecuteQuery($"select id,name from {table} where (active>0){sqlCriteria} order by name;");
            bool anyRowSelected = false;
            foreach (DataRow row in dt.Rows) {
                bool rowSelected = row["id"].ToString() == htmlValue.ToString();
                anyRowSelected = anyRowSelected || rowSelected;
                options.Add(new NameValueSelected(cp.Utils.EncodeText(row["name"].ToString()), row["id"].ToString(), rowSelected));
            }
            if (options.Count == 0) {
                options.Add(new NameValueSelected("No Options Available", "", false));
            }
            addFilterSelect(caption, htmlName, options);
        }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public override void addFilterSelectContent(string caption, string htmlName, int htmlValue, string content, string sqlCriteria) {
            addFilterSelectContent(caption, htmlName, htmlValue, content, sqlCriteria, "");
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
        //
        //
        //
        //====================================================================================================
        // -- Deprecated properties and methods
        //
        //
        /// <summary>
        /// The default Layoutbuilder styles. Override to customize.
        /// </summary>
        [Obsolete("move javascript and styles to layouts", false)] public override string styleSheet => Properties.Resources.layoutBuilderStyles;
        //
        /// <summary>
        /// The default Layoutbuilder script. Override to customize.
        /// </summary>
        [Obsolete("move javascript and styles to layouts", false)] public override string javascript => Properties.Resources.layoutBuilderJavaScript;
        //
        /// <summary>
        /// The action attribute of the form element that wraps the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        [Obsolete("Deprecated. No longer needed.", false)] public override string formActionQueryString { get; set; }
        //
        /// <summary>
        /// if true, the optional form tag will be blocked. The form tag is added automatically if buttons, hiddens or a form-action is added
        /// </summary>
        [Obsolete("Deprecated. Use includeForm to prevent the form tag.", false)]
        public override bool blockFormTag { get; set; }
        //
        [Obsolete("Deprecated. Use getHtml()", false)]
        //
        public override string getHtml(CPBaseClass cp) {
            return getHtml();
        }

        //
        [Obsolete("Deprecated. Instead use cp.adminUI.getPortalFeatureLink()", false)]
        public override string refreshQueryString { get; set; }
        //
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
        [Obsolete("Deprecated. Instead use cp.adminUI.getPortalFeatureLink()", false)] public override string baseUrl { get; }
        //
        [Obsolete("Deprecated. Use callbackAddonGuid", false)] public override string baseAjaxUrl { get; set; }
    }
    //
    public class LayoutBuilder_ActiveFilter {
        /// <summary>
        /// when a filter is added, an activeFilter is added.
        /// The caption is the name of the filter that appears
        /// </summary>
        public string activeFilterCaption { get; set; } = "";
        /// <summary>
        /// the name of the filter is the button html name
        /// </summary>
        public string activeFilterName { get; set; } = "";
        /// <summary>
        /// the name of the filter is the button html value
        /// </summary>
        public string activeFilterValue { get; set; } = "";
    }
    //
    // =====================================================================================================
    /// <summary>
    /// classes used in viewmodel to build layout mustache
    /// </summary>
    public class LayoutBuilderClass_FilterGroup {
        /// <summary>
        /// for this option group, this is the caption that will be displayed in the filter options
        /// </summary>
        public string filterGroupCaption { get; set; }
        /// <summary>
        /// a list of inputs for this option group
        /// </summary>
        public List<LayoutBuilderClass_FilterGroup_Input> filterInputs { get; set; }
    }
    //
    // =====================================================================================================
    /// <summary>
    /// classes used in viewmodel to build layout mustache
    /// </summary>
    public class LayoutBuilderClass_FilterGroup_Input {
        public bool filterIsCheckbox { get; set; }
        public bool filterIsRadio { get; set; }
        public bool filterIsSelect { get; set; }
        public bool filterIsText { get; set; }
        public bool filterIsDate { get; set; }
        public string filterCaption { get; set; }
        /// <summary>
        /// a unique string for this filter input
        /// </summary>
        public string filterHtmlId { get; set; }
        /// <summary>
        /// the html name of the filter input.
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// the html value for this input. for checkbox, always 1. For other types the value of the filter.
        /// </summary>
        public string value { get; set; }
        /// <summary>
        /// for checkbox and radio, true if selected.
        /// </summary>
        public bool selected { get; set; }
        /// <summary>
        /// if the filter option is a select, this is the list of options for the select.
        /// NameValueSelected is definied in the BaseClasses
        /// </summary>
        public List<NameValueSelected> filterSelectOptions { get; set; }
    }
}
