using System;

namespace Contensive.BaseClasses.LayoutBuilder {
    public abstract class LayoutBuilderNameValueBaseClass : LayoutBuilderBaseClass {
        //
        //-------------------------------------------------
        /// <summary>
        /// deprecated. Use htmlAfterTable instead
        /// </summary>
        public abstract void addRow();
        //
        public abstract void openFieldSet(string caption);
        //
        public abstract void closeFieldSet();
        //
        public abstract string formAction { get; set; }
        //
        public abstract string rowHtmlId { get; set; }
        public abstract string rowName { get; set; }
        public abstract string rowValue { get; set; }
        public abstract  string rowHelp { get; set; }
        public  abstract string formId { get; set; }

    }
}
