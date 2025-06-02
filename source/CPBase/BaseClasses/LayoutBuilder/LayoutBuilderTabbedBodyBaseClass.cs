using Contensive.BaseClasses;
using System;

namespace Contensive.BaseClasses.LayoutBuilder {
    public abstract class LayoutBuilderTabbedBodyBaseClass : LayoutBuilderBaseClass {
        //
        public LayoutBuilderTabbedBodyBaseClass(CPBaseClass cp) : base(cp) { }
        //
        /// <summary>
        /// After adding a tab with addTab, set the caption for the tab. This is used to display the tab in the tab bar.
        /// </summary>
        public abstract string tabCaption { get; set; }
        //
        /// <summary>
        /// After adding a table with addTab, set the style class for the tab. This is used to apply custom styles to the tab.
        /// </summary>
        public abstract string tabStyleClass { get; set; }
        //
        /// <summary>
        /// After adding a tab with addTab, set the link for the tab. This is used to navigate to the tab when clicked.
        /// </summary>
        public abstract string tabLink { get; set; }
        //
        /// <summary>
        /// Set the active tab by caption. This is used to highlight the active tab in the tab bar.
        /// </summary>
        /// <param name="caption"></param>
        public abstract void setActiveTab(string caption);
        //
        /// <summary>
        /// Add a navigation entry. The navCaption and navLink should be set after creating a new entry. The first nav entry does not need to be added.
        /// </summary>
        public abstract void addTab();
    }
}
