using System;

namespace Contensive.BaseClasses.LayoutBuilder {
    public abstract class LayoutBuilderNameValueBaseClass : LayoutBuilderBaseClass {
        //
        public LayoutBuilderNameValueBaseClass(CPBaseClass cp) : base(cp) { }

        //
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// deprecated. Use htmlAfterTable instead
        /// </summary>
        public abstract void addRow();
        //
        public abstract void openFieldSet(string caption);
        //
        public abstract void closeFieldSet();
        //
        [Obsolete("Deprecated. Not needed.",false)] public abstract string formAction { get; set; }
        //
        public abstract string rowHtmlId { get; set; }
        public abstract string rowName { get; set; }
        public abstract string rowValue { get; set; }
        public abstract string rowHelp { get; set; }
        public abstract string formId { get; set; }

    }
}
