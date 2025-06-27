using System;

namespace Contensive.BaseClasses.LayoutBuilder {
    public abstract class LayoutBuilderNameValueBaseClass : LayoutBuilderBaseClass {
        //
        public LayoutBuilderNameValueBaseClass(CPBaseClass cp) : base(cp) { }
        /// <summary>
        /// After populating the current rowName, rowValue, rowHelp, and rowHtmlId properties, addRow to create a new row, and populate its rowName, rowValue, rowHelp, rowHtmlId
        /// </summary>
        public abstract void addRow();
        /// <summary>
        /// After populating a row, optionally add a second column and populate its rowName, rowValue, rowHelp, rowHtmlId
        /// </summary>
        public abstract void addColumn();
        /// <summary>
        /// A fieldset creates a border around all the rows added after the openFieldSet call, until the closeFieldSet call.
        /// Open a fieldset with a caption. The caption is used as the legend of the fieldset.
        /// </summary>
        /// <param name="caption"></param>
        public abstract void openFieldSet(string caption);
        /// <summary>
        /// ends a fieldset started with openFieldSet.
        /// </summary>
        public abstract void closeFieldSet();
        /// <summary>
        /// When adding a name/value set, this is the HtmlId of the input element you supplied in the rowValue. used to link the caption
        /// </summary>
        public abstract string rowHtmlId { get; set; }
        /// <summary>
        /// This is the caption for the row.
        /// </summary>
        public abstract string rowName { get; set; }
        /// <summary>
        /// This is the value for the row. It can be a text input, a checkbox, a select, or any other HTML element.
        /// </summary>
        public abstract string rowValue { get; set; }
        /// <summary>
        /// This is optional help text related to the row
        /// </summary>
        public abstract string rowHelp { get; set; }
        //
        [Obsolete("Deprecated. Not needed.", false)] public abstract string formAction { get; set; }
    }
}
