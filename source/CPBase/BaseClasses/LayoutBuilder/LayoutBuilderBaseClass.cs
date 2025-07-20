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
        /// add a clickable button to the UI that will remove a filter from the active filters list.
        /// </summary>
        /// <param name="caption">The caption that appears on the button. This would typically be a caption that tells the user about the filter, like if the filter selects a meeting, this might be name of the meeting.</param>
        /// <param name="removeFilterRequestName">The request name that will be submitted when this button is pressed. This would typically by 'removeFilter'.</param>
        /// <param name="requestNameToClear">When this button is pressed, the code should read the removeFilterRequestName and if not empty, clear or ignore the requestName matching this string. For example if the filter selects a meeting, this might be the meeting requestName, like meetingId.</param>
        public abstract void addActiveFilter(string caption, string removeFilterRequestName, string requestNameToClear);
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Creates a new filter group. Filter groups have a caption followed by a list of filter inputs
        /// </summary>
        public abstract void addFilterGroup(string caption);
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Get a boolean filter value from the request and/or visit.
        /// Use this method to read the value of a filter create with addFilterCheckbox().
        /// This method incorporates both the request object and visit object, so a filter stays set through the visit.
        /// </summary>
        /// <param name="filterHtmlName"></param>
        /// <param name="viewName">
        /// The name of the view, or form that uses the filter. For example 'attendee list'. 
        /// This name is used when the filter value is saved so a user returning the page will retain thier fields, and if a filter with the same
        /// name on another page is cleared, it will only clear the filter on that page.
        /// </param>
        /// <returns></returns>
        public abstract bool getFilterBoolean(string filterHtmlName, string viewName);
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Get a string filter value from the request and/or visit.
        /// Use this method to read the value of a filter create with addFilterTextInput(), addFilterRadio()
        /// This method incorporates both the request object and visit object, so a filter stays set through the visit.
        /// </summary>
        /// <param name="filterHtmlName"></param>
        /// <param name="viewName">
        /// The name of the view, or form that uses the filter. For example 'attendee list'. 
        /// This name is used when the filter value is saved so a user returning the page will retain thier fields, and if a filter with the same
        /// name on another page is cleared, it will only clear the filter on that page.
        /// </param>
        /// <returns></returns>
        public abstract string getFilterText(string filterHtmlName, string viewName);
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Get a string filter value from the request and/or visit.
        /// Use this method to read the value of a filter create with addFilterInteger(), addFilterSelect(), addFilterSelectContent().
        /// This method incorporates both the request object and visit object, so a filter stays set through the visit.
        /// </summary>
        /// <param name="filterHtmlName">The name attribute of the html input element for this filter.</param>
        /// <param name="viewName">
        /// The name of the view, or form that uses the filter. For example 'attendee list'. 
        /// This name is used when the filter value is saved so a user returning the page will retain thier fields, and if a filter with the same
        /// name on another page is cleared, it will only clear the filter on that page.
        /// </param>
        /// <returns></returns>
        public abstract int getFilterInteger(string filterHtmlName, string viewName);
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Get a string filter value from the request and/or visit.
        /// Use this method to read the value of a filter create with addFilterDate().
        /// This method incorporates both the request object and visit object, so a filter stays set through the visit.
        /// </summary>
        /// <param name="filterHtmlName"></param>
        /// <param name="viewName">
        /// The name of the view, or form that uses the filter. For example 'attendee list'. 
        /// This name is used when the filter value is saved so a user returning the page will retain thier fields, and if a filter with the same
        /// name on another page is cleared, it will only clear the filter on that page.
        /// </param>
        /// <returns></returns>
        public abstract DateTime? getFilterDate(string filterHtmlName, string viewName);
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Add a filter to the current filter group
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="filterHtmlName"></param>
        /// <param name="htmlValue"></param>
        /// <param name="selected"></param>
        public abstract void addFilterCheckbox(string caption, string filterHtmlName, string htmlValue, bool selected);
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Add a filter to the current filter group.
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="filterHtmlName">The name attribute of the html radio element.</param>
        /// <param name="filterValue"></param>
        /// <param name="filterSelected"></param>
        public abstract void addFilterRadio(string caption, string filterHtmlName, string filterValue, bool filterSelected);
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Add a filter to the current filter group
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="filterHtmlName"></param>
        /// <param name="filterValue"></param>
        public abstract void addFilterTextInput(string caption, string filterHtmlName, string filterValue);
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Add a filter to the current filter group
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="filterHtmlName"></param>
        /// <param name="filterValue"></param>
        public abstract void addFilterDateInput(string caption, string filterHtmlName, DateTime? filterValue);
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Add a filter to the current filter group. This is a select input with a list of options.
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="filterHtmlName"></param>
        /// <param name="options"></param>
        public abstract void addFilterSelect(string caption, string filterHtmlName, List<NameValueSelected> options);
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Add a filter to the current filter group. This is a select input built from the data in a table.
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="filterHtmlName"></param>
        /// <param name="filterValue"></param>
        /// <param name="content"></param>
        /// <param name="sqlCriteria"></param>
        public abstract void addFilterSelectContent(string caption, string filterHtmlName, int filterValue, string content, string sqlCriteria);
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Add a filter to the current filter group. This is a select input built from the data in a table. 
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="filterHtmlName"></param>
        /// <param name="filterValue"></param>
        /// <param name="content">The name of the content (table metadata)</param>
        /// <param name="sqlCriteria"></param>
        /// <param name="nonCaption">If provided, an entry will be added allowing the user to select no option</param>
        public abstract void addFilterSelectContent(string caption, string filterHtmlName, int filterValue, string content, string sqlCriteria, string nonCaption);
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
        [Obsolete("move javascript and styles to layouts", false)] public abstract string styleSheet { get; }
        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// The default Layoutbuilder script. Override to customize.
        /// </summary>
        [Obsolete("move javascript and styles to layouts", false)] public abstract string javascript { get; }
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
    public class NameValueSelected(string name, string value, bool selected) {
        public string name { get; set; } = name;
        public string value { get; set; } = value;
        public bool selected { get; set; } = selected;
    }
}