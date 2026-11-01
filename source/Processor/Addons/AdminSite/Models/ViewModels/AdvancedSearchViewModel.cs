
using System.Collections.Generic;

namespace Contensive.Processor.Addons.AdminSite {
    //
    //====================================================================================================
    /// <summary>
    /// View model for the advanced search Mustache layout
    /// </summary>
    public class AdvancedSearchViewModel {
        public List<AdvancedSearchFieldViewModel> fields { get; set; } = new();
    }
    //
    //====================================================================================================
    /// <summary>
    /// A single field row in the advanced search form
    /// </summary>
    public class AdvancedSearchFieldViewModel {
        /// <summary>
        /// Field display name
        /// </summary>
        public string caption { get; set; }
        /// <summary>
        /// Radio button options for match type (ignore, empty, not empty, equals, etc.)
        /// </summary>
        public List<AdvancedSearchRadioOption> radioOptions { get; set; } = new();
        /// <summary>
        /// True if this field type has a value input (text box or date picker)
        /// </summary>
        public bool hasValueInput { get; set; }
        /// <summary>
        /// Pre-rendered HTML for the value input element (triple-mustache, unescaped)
        /// </summary>
        public string valueInputHtml { get; set; }
    }
    //
    //====================================================================================================
    /// <summary>
    /// A single radio button option within a search field row
    /// </summary>
    public class AdvancedSearchRadioOption {
        /// <summary>
        /// Form field name for the radio group (e.g. "FieldMatch0")
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// Radio button value (the FindWordMatchEnum integer as string)
        /// </summary>
        public string value { get; set; }
        /// <summary>
        /// True if this radio option is currently selected
        /// </summary>
        public bool isChecked { get; set; }
        /// <summary>
        /// Display label for the radio option (e.g. "ignore", "empty", "=", ">")
        /// </summary>
        public string label { get; set; }
        /// <summary>
        /// Optional HTML id for the radio input (used by onFocus handlers)
        /// </summary>
        public string radioId { get; set; }
    }
}
