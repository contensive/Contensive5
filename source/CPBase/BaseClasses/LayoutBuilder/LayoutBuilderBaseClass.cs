using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Contensive.BaseClasses.LayoutBuilder {
    /// <summary>
    /// LayoutBuilders create the html for common Admin UI cases.
    /// This class has 2 uses. 
    /// - the base class provides an api that client apps use to create admin layouts
    /// - child classes implement these api calls, and also provide non-override properties used in the mustache viewModels that populate layouts.
    ///
    /// They must be implemented 2 ways, first called by the page during the initial page load, and second called by the ajax refresh methods.
    /// Both methods return the exact same html, but the javascript that calls the ajax methods selectively replaces the important content.
    /// </summary>
    public abstract class LayoutBuilderBaseClass(CPBaseClass cp) {
        public CPBaseClass cp { get; set; } = cp;
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Creates a new filter group. Filter groups have a caption followed by a list of filter inputs
        /// </summary>
        public abstract void addFilterGroup(string caption);
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Add a filter to the current filter group
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="htmlName"></param>
        /// <param name="htmlValue"></param>
        /// <param name="selected"></param>
        public abstract void addFilterCheckbox(string caption, string htmlName, string htmlValue, bool selected);
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Add a filter to the current filter group
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="htmlName"></param>
        /// <param name="htmlValue"></param>
        /// <param name="selected"></param>
        public abstract void addFilterRadio(string caption, string htmlName, string htmlValue, bool selected);
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Add a filter to the current filter group
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="htmlName"></param>
        /// <param name="htmlValue"></param>
        public abstract void addFilterTextInput(string caption, string htmlName, string htmlValue);
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Add a filter to the current filter group
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="htmlName"></param>
        /// <param name="htmlDateValue"></param>
        public abstract void addFilterDateInput(string caption, string htmlName, DateTime? htmlDateValue);
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Add a filter to the current filter group. This is a select input with a list of options.
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="htmlName"></param>
        /// <param name="options"></param>
        public abstract void addFilterSelect(string caption, string htmlName, List<NameValueSelected> options);
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Add a filter to the current filter group. This is a select input built from the data in a table.
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="htmlName"></param>
        /// <param name="htmlValue"></param>
        /// <param name="content"></param>
        /// <param name="sqlCriteria"></param>
        public abstract void addFilterSelectContent(string caption, string htmlName, int htmlValue, string content, string sqlCriteria);
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Add a filter to the current filter group. This is a select input built from the data in a table. 
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="htmlName"></param>
        /// <param name="htmlValue"></param>
        /// <param name="content">The name of the content (table metadata)</param>
        /// <param name="sqlCriteria"></param>
        /// <param name="nonCaption">If provided, an entry will be added allowing the user to select no option</param>
        public abstract void addFilterSelectContent(string caption, string htmlName, int htmlValue, string content, string sqlCriteria, string nonCaption);
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// The guid of the addon to be called to update pagination or search.
        /// Typically the same remote that calls the layout builder.
        /// </summary>
        public abstract string callbackAddonGuid { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// if true, the container between the button rows will include default padding
        /// </summary>
        public abstract bool includeBodyPadding { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// if true, the container between the button rows will include the default background color. Else it is transparent.
        /// </summary>
        public abstract bool includeBodyColor { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// if true, this layoutBuilder will not be contained in other layoutBuilder content. This is used by the default getHtml() to include an outer div with the htmlId "afw", and the styles and javascript
        /// </summary>
        public abstract bool isOuterContainer { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Include a form tag around the layout. Default is true. Set false to block the form tag.
        /// </summary>
        public abstract bool includeForm { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
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
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// simple description text. Will be wrapped in an html paragraph tag.
        /// </summary>
        public abstract string description { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// The default Layoutbuilder styles. Override to customize.
        /// </summary>
        public abstract string styleSheet { get; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// The default Layoutbuilder script. Override to customize.
        /// </summary>
        public abstract string javascript { get; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Optional. If set, this value will populate the title in the subnav of the portalbuilder
        /// </summary>
        public abstract string portalSubNavTitle { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// A virtual filename to a download of the report data. Leave blank to prevent download file
        /// </summary>
        public abstract string csvDownloadFilename { get; set; }
        // 
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// The default body. Typically you would create a layout by adding content to the individual elements and calling this method. Oveerride this method and consider using HtmlController.getReportDoc()
        /// </summary>
        /// <returns></returns>
        public abstract string getHtml();
        //
        // ----------------------------------------------------------------------------------------------------
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
        // ----------------------------------------------------------------------------------------------------
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
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// The action attribute of the form element that wraps the layout. This will also create a form around the layout. Set blockForm to true to block the automatic form.
        /// </summary>
        [Obsolete("Deprecated. No longer needed.", false)] public abstract string formActionQueryString { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// The body of the layout.
        /// </summary>
        public abstract string body { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        /// <summary>
        /// An html block added to the left of the Body. Typically used for filters.
        /// </summary>
        public abstract string htmlLeftOfBody { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        /// <summary>
        /// An html block added above the Body. Typically used for filters.
        /// </summary>
        public abstract string htmlBeforeBody { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        //

        public enum AfwStyles {
            afwWidth10,
            afwWidth20,
            afwWidth30,
            afwWidth40,
            afwWidth50,
            afwWidth60,
            afwWidth70,
            afwWidth80,
            afwWidth90,
            afwWidth100,
            //
            afwWidth10px,
            afwWidth20px,
            afwWidth30px,
            afwWidth40px,
            afwWidth50px,
            afwWidth60px,
            afwWidth70px,
            afwWidth80px,
            afwWidth90px,
            //
            afwWidth100px,
            afwWidth200px,
            afwWidth300px,
            afwWidth400px,
            afwWidth500px,
            //
            afwMarginLeft100px,
            afwMarginLeft200px,
            afwMarginLeft300px,
            afwMarginLeft400px,
            afwMarginLeft500px,
            //
            afwTextAlignRight,
            afwTextAlignLeft,
            afwTextAlignCenter
        }
        //
        // ----------------------------------------------------------------------------------------------------
        //
        /// <summary>
        /// An html block added below the Body. Typically used for filters.
        /// </summary>
        public abstract string htmlAfterBody { get; set; }
        //
        //====================================================================================================
        //
        /// <summary>
        /// if true, the optional form tag will be blocked. The form tag is added automaatically if buttons, hiddens or a form-action is added
        /// </summary>
        [Obsolete("Deprecated. To prevent the form tag set includeForm=false.", false)] public abstract bool blockFormTag { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Include all nameValue pairs required to refresh the page if someone clicks on a header. For example, if there is a filter dateTo that is not empty, add dateTo=1/1/2000 to the RQS
        /// </summary>
        [Obsolete("Deprecated. No longer needed.", false)] public abstract string refreshQueryString { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// deprecated. Use warningMessage instead
        /// </summary>
        [Obsolete("Deprecated. Use warningMessage instead", false)]
        public abstract string warning { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// deprecated. Use htmlAfterBody instead
        /// </summary>
        [Obsolete("Deprecated. Use htmlAfterBody instead", false)]
        public abstract string footer { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// deprecated. Use warningMessage instead
        /// </summary>
        [Obsolete("Deprecated. Use addFormHidden instead", false)]
        public abstract string formid { get; set; }
        // 
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        [Obsolete("Deprecated. use getHtml().", false)] public abstract string getHtml(CPBaseClass cp);
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// deprecated. Instead use cp.adminUI.getPortalFeatureLink()
        /// </summary>
        [Obsolete("deprecated. Instead use cp.adminUI.getPortalFeatureLink()", false)] public abstract string baseUrl { get; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// LayoutBuilder layouts require html page redraw with an ajax method. 
        /// Set this to the url of the ajax method that returns the html for the form.
        /// </summary>
        [Obsolete("Deprecated. use callbackAddonGuid", false)] public abstract string baseAjaxUrl { get; set; }

    }
    //
    // ----------------------------------------------------------------------------------------------------
    /// <summary>
    /// A simple class to hold a name value pair with a selected flag. Used to create a list of options for a select input in the filter group.
    /// </summary>
    public class NameValueSelected {
        public string name { get; set; }
        public string value { get; set; }
        public bool selected { get; set; }
        public NameValueSelected(string name, string value, bool selected) {
            this.name = name;
            this.value = value;
            this.selected = selected;
        }
    }
    ////
    //// ----------------------------------------------------------------------------------------------------
    ///// <summary>
    ///// Use an Interface to define the request for a filter group. This is used to create a filter group in the layout builder.
    ///// Then use intereface versioning to incrementally update the filter group request.
    ///// </summary>
    //public abstract class LayoutBuilderBaseFilterGroupRequest {
    //    /// <summary>
    //    /// The caption for this filter group. An option group has a caption and a list of filter inputs
    //    /// </summary>
    //    public abstract string caption { get; set; }
    //    /// <summary>
    //    /// list of inputs for this filter group. An option group has a caption and a list of filter inputs
    //    /// </summary>
    //    public abstract List<LayoutBuilderBaseFilterGroupRequest_FilterInput> filterInput { get; set; }
    //}
    ////
    //// ----------------------------------------------------------------------------------------------------

    //public abstract class LayoutBuilderBaseFilterGroupRequest_FilterInput {
    //    public abstract bool filterIsCheckbox { get; set; }
    //    public abstract bool filterIsRadio { get; set; }
    //    public abstract bool filterIsSelect { get; set; }
    //    public abstract bool filterIsText { get; set; }
    //    public abstract bool filterIsDate { get; set; }
    //    public abstract string filterCaption { get; set; }
    //    /// <summary>
    //    /// for checkbox and radio, true if selected.
    //    /// </summary>
    //    public abstract bool filterSelected { get; set; }
    //    /// <summary>
    //    /// for checkbox, always 1. For other types the value of the filter.
    //    /// </summary>
    //    public abstract bool filterValue { get; set; }
    //    /// <summary>
    //    /// if the filter option is a select, this is the list of options for the select
    //    /// </summary>
    //    public abstract List<LayoutBuilderBaseClass_RequestFilterOptionGroup_Option_SelectOption> selectOptions { get; set; }
    //}

    ////
    //// ----------------------------------------------------------------------------------------------------
    ///// <summary>
    ///// if the filter option is a select, this is the value of the selected option.
    ///// </summary>
    //public abstract class LayoutBuilderBaseClass_RequestFilterOptionGroup_Option_SelectOption {
    //    /// <summary>
    //    /// the select optons value
    //    /// </summary>
    //    public abstract string filterSelectOptionValue { get; set; }
    //    /// <summary>
    //    /// the select options caption
    //    /// </summary>
    //    public abstract string filterSelectOptionName { get; set; }
    //}


    //internal class sample {
    //    //
    //    // ======================================================================================================
    //    //
    //    // -- pattern from Claude, passing an object argument into a cp method, with versioning for interfaces
    //    // -- after recommending an interface, that forced a factorypattern to create the request argument, which then recommended an abstract base class
    //    //
    //    // -- define the request class interface
    //    //
    //    private interface IRequestClass {
    //        string Name { get; }
    //        int Version { get; }
    //    }
    //    //
    //    // -- possible future revision
    //    //
    //    private interface IRequestClassV2 : IRequestClass {
    //        string NewProperty { get; }
    //        DateTime CreatedDate { get; }
    //    }
    //    //
    //    // -- a concrete class in the main application that implements the interface, this is returned by the factory method
    //    //
    //    private class RequestClass : IRequestClass {
    //        public string Name { get; set; } = "DefaultName";
    //        public int Version { get; set; } = 1;
    //        // Optional: New properties for newer versions
    //        public string FilterType { get; set; } = "DefaultType";
    //        public string Value { get; set; } = "DefaultValue";
    //    }
    //    //
    //    // addon base defining an addFilter method that passes the interface
    //    //
    //    private abstract class AddonBase {
    //        public abstract void AddFilter(IRequestClass request);

    //        // Helper method for addons that want to use newer features
    //        protected IRequestClassV2 AsV2(IRequestClass request) => request as IRequestClassV2;
    //    }
    //    //
    //    // -- then addons implement versioning
    //    //
    //    private class MyAddon : AddonBase {
    //        public override void AddFilter(IRequestClass request) {
    //            // Works with any version
    //            Console.WriteLine($"Processing: {request.Name}");

    //            // Optional: Use newer features if available
    //            if (AsV2(request) is IRequestClassV2 v2) {
    //                Console.WriteLine($"Created: {v2.CreatedDate}");
    //            }
    //        }
    //    }
    //    //
    //    // -- but the addon cant create an instance of the request class, co implement a factory pattern
    //    // -- that creates the request object
    //    //
    //    //
    //    // -- but the addon cant create an instance of the request class, co implement a factory pattern
    //    // -- that creates the request object
    //    // Your CP class in the main application
    //    private class CP : AddonBase {
    //        public override void AddFilter(IRequestClass request) {
    //            throw new NotImplementedException();
    //        }

    //        // Factory method for addons to create request objects
    //        private IRequestClass CreateRequest() {
    //            return new RequestClass(); // Your concrete implementation
    //        }

    //        // Or with initial values
    //        private IRequestClass CreateRequest(string filterType, string value) {
    //            return new RequestClass {
    //                FilterType = filterType,
    //                Value = value
    //            };
    //        }
    //    }

    //}
}