

using Contensive.BaseClasses;
using Contensive.BaseClasses.LayoutBuilder;
using Contensive.Processor.Controllers;
using System;
using System.Collections.Generic;

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
        /// if true, the layout will include a filter group. 
        /// This is used to determine if the layout should include a filter group or not.
        /// </summary>
        public bool includeFilter { get; set; } = false;
        //
        // ----------------------------------------------------------------------------------------------------
        //
        public bool includeResetFilterButton { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// a list of filter groups. 
        /// populated with the addFilter() method
        /// Each group has a caption and a list of filters. 
        /// Each filter can be one of several types, checkbox, text, etc.
        /// </summary>
        public List<LayoutBuilderClass_FilterGroup> filterGroups { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// concrete method shared with all derived classes.
        /// Add a filter group to the layout. A filter group is a list of similar options under a single caption. You can have as many filter groups as you want, and each filter group can have as many filter inputs as you want.
        /// </summary>
        /// <param name="filterRequest"></param>
        public override void addFilterGroup(LayoutBuilderBaseFilterGroupRequest filterRequest) {
            //
            // -- add a filter group to the ViewData used render the layout in getHtml()
            includeFilter = true;
            List<LayoutBuilderClass_FilterGroup_Input> filterInputs = [];
            foreach (LayoutBuilderBaseFilterGroupRequest_FilterInput filterInput in filterRequest.filterInput) {
                //
                // -- add the filter input to the list
                LayoutBuilderClass_FilterGroup_Input input = new LayoutBuilderClass_FilterGroup_Input() {
                    filterCaption = filterInput.filterCaption,
                    filterInputId = cp.Utils.GetRandomString(8),
                    filterIsCheckbox = filterInput.filterIsCheckbox,
                    filterIsSelect = filterInput.filterIsSelect,
                    filterIsText = filterInput.filterIsText,
                    filterIsDate = filterInput.filterIsDate,
                    filterSelected = filterInput.filterSelected,
                    filterValue = filterInput.filterValue,
                    filterSelectOptions = []
                };
                //
                // -- add the select options if this is a select
                if (filterInput.filterIsSelect) {
                    foreach (var option in filterInput.selectOptions) {
                        input.filterSelectOptions.Add(new LayoutBuilderClass_FilterGroup_Input_SelectOptions() {
                            filterSelectOptionName = option.filterSelectOptionName,
                            filterSelectOptionValue = option.filterSelectOptionValue
                        });
                    }
                }
                //
                // -- add the input to the list
                filterInputs.Add(input);
            }
            filterGroups.Add(new LayoutBuilderClass_FilterGroup() {
                filterGroupCaption = filterRequest.caption,
                filterInputs = filterInputs
            });
        }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// create the object argument required for addFilterGroup().
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override LayoutBuilderBaseFilterGroupRequest createFilterGroupRequest() {
            throw new NotImplementedException("createFilterGroupRequest() not implemented in LayoutBuilderTwoColumnRight");
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
    // =====================================================================================================
    /// <summary>
    /// the concrete implementation of the ILayoutBuilderFilterGroupRequest interface needed for the createFilterGroupRequest() method.
    /// </summary>
    public class LayoutBuilderFilterGroupRequest : LayoutBuilderBaseFilterGroupRequest {
        /// <summary>
        /// The caption for this filter group. An option group has a caption and a list of filter inputs
        /// </summary>
        public override string caption { get; set; }
        /// <summary>
        /// list of inputs for this filter group. An option group has a caption and a list of filter inputs
        /// </summary>
        public override List<LayoutBuilderBaseFilterGroupRequest_FilterInput> filterInput { get; set; }
    }
    public class LayoutBuilderFilterGroupRequest_FilterInput : LayoutBuilderBaseFilterGroupRequest_FilterInput {
        public override bool filterIsCheckbox { get; set; }
        public override bool filterIsSelect { get; set; }
        public override bool filterIsText { get; set; }
        public override bool filterIsDate { get; set; }
        public override string filterCaption { get; set; }
        ///// <summary>
        ///// a unique string for this filter input
        ///// </summary>
        //public string filterInputId { get; set; }
        /// <summary>
        /// for checkbox and radio, true if selected.
        /// </summary>
        public override bool filterSelected { get; set; }
        /// <summary>
        /// for checkbox, always 1. For other types the value of the filter.
        /// </summary>
        public override bool filterValue { get; set; }
        /// <summary>
        /// if the filter option is a select, this is the list of options for the select
        /// </summary>
        //public List<LayoutBuilderClass_RequestFilterOptionGroup_Option_SelectOption> selectOptions { get; set; }
        public override  List<LayoutBuilderBaseClass_RequestFilterOptionGroup_Option_SelectOption> selectOptions { 
            get => throw new NotImplementedException(); 
            set => throw new NotImplementedException(); 
        }
    }
    public class LayoutBuilderClass_RequestFilterOptionGroup_Option_SelectOption : LayoutBuilderBaseClass_RequestFilterOptionGroup_Option_SelectOption {
        /// <summary>
        /// the select optons value
        /// </summary>
        public override string filterSelectOptionValue { get; set; }
        /// <summary>
        /// the select options caption
        /// </summary>
        public override string filterSelectOptionName { get; set; }
    }
    //
    // =====================================================================================================
    //
    public class LayoutBuilderClass_FilterGroup{
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
    //
    public class LayoutBuilderClass_FilterGroup_Input {
        public bool filterIsCheckbox { get; set; }
        public bool filterIsSelect { get; set; }
        public bool filterIsText { get; set; }
        public bool filterIsDate { get; set; }
        public string filterCaption { get; set; }
        /// <summary>
        /// a unique string for this filter input
        /// </summary>
        public string filterInputId { get; set; }
        /// <summary>
        /// for checkbox and radio, true if selected.
        /// </summary>
        public bool filterSelected { get; set; }
        /// <summary>
        /// for checkbox, always 1. For other types the value of the filter.
        /// </summary>
        public bool filterValue { get; set; }
        /// <summary>
        /// if the filter option is a select, this is the list of options for the select
        /// </summary>
        public List<LayoutBuilderClass_FilterGroup_Input_SelectOptions> filterSelectOptions { get; set; }
    }
    //
    // =====================================================================================================
    /// <summary>
    /// if the filter option is a select, this is the value of the selected option.
    /// </summary>
    public class LayoutBuilderClass_FilterGroup_Input_SelectOptions {
        /// <summary>
        /// the select optons value
        /// </summary>
        public string filterSelectOptionValue { get; set; }
        /// <summary>
        /// the select options caption
        /// </summary>
        public string filterSelectOptionName { get; set; }
    }
    //
    // ====================================================================================================
    // classes derived from addFilter request
    //

    ////
    //// ----------------------------------------------------------------------------------------------------
    ///// <summary>
    ///// A filter group is a list of similar options under a single caption.
    ///// </summary>
    //public abstract class LayoutBuilderFilterGroupRequest : LayoutBuilderBaseFilterGroupRequest {
    //    /// <summary>
    //    /// The caption for this filter group. An option group has a caption and a list of filter inputs
    //    /// </summary>
    //    public override string caption { get; set; }
    //    /// <summary>
    //    /// list of inputs for this filter group. An option group has a caption and a list of filter inputs
    //    /// </summary>
    //    public override List<LayoutBuilderBaseFilterGroupRequest_FilterInput> filterInput { get; set; }
    //}
    ////
    //// ----------------------------------------------------------------------------------------------------

    //public abstract class LayoutBuilderFilterGroupRequest_FilterInput : LayoutBuilderBaseFilterGroupRequest_FilterInput {
    //    public override bool filterIsCheckbox { get; set; }
    //    public override bool filterIsSelect { get; set; }
    //    public override bool filterIsText { get; set; }
    //    public override bool filterIsDate { get; set; }
    //    public override string filterCaption { get; set; }
    //    /// <summary>
    //    /// for checkbox and radio, true if selected.
    //    /// </summary>
    //    public override bool filterSelected { get; set; }
    //    /// <summary>
    //    /// for checkbox, always 1. For other types the value of the filter.
    //    /// </summary>
    //    public override bool filterValue { get; set; }
    //    /// <summary>
    //    /// if the filter option is a select, this is the list of options for the select
    //    /// </summary>
    //    public override List<LayoutBuilderBaseClass_RequestFilterOptionGroup_Option_SelectOption> selectOptions { get; set; }
    //}
    ////
    //// ----------------------------------------------------------------------------------------------------
    ///// <summary>
    ///// if the filter option is a select, this is the value of the selected option.
    ///// </summary>
    //public abstract class LayoutBuilderClass_RequestFilterOptionGroup_Option_SelectOption : LayoutBuilderBaseClass_RequestFilterOptionGroup_Option_SelectOption {
    //    /// <summary>
    //    /// the select optons value
    //    /// </summary>
    //    public override string filterSelectOptionValue { get; set; }
    //    /// <summary>
    //    /// the select options caption
    //    /// </summary>
    //    public override string filterSelectOptionName { get; set; }
    //}
}
