using Contensive.BaseClasses;

namespace Contensive.BaseClasses.LayoutBuilder {
    public abstract class LayoutBuilderContentWithTabsBaseClass {
        //
        // ====================================================================================================
        /// <summary>
        /// Optional. If set, this value will populate the title in the subnav of the portalbuilder
        /// </summary>
        public abstract string portalSubNavTitle { get; set; }
        //
        //-------------------------------------------------
        //
        //-------------------------------------------------
        //
        public abstract bool isOuterContainer { get; set; }
        //
        //-------------------------------------------------
        //
        //-------------------------------------------------
        //
        public abstract string styleSheet { get; }
        //
        //-------------------------------------------------
        //
        //-------------------------------------------------
        //
        public abstract string javascript { get; }
        //
        //-------------------------------------------------
        // body
        //-------------------------------------------------
        //
        public abstract string body { get; set; }
        //
        //-------------------------------------------------
        // Title
        //-------------------------------------------------
        //
        public abstract string title { get; set; }
        //
        //-------------------------------------------------
        // Warning
        //-------------------------------------------------
        //
        public abstract string warning { get; set; }
        //
        //-------------------------------------------------
        // Description
        //-------------------------------------------------
        //
        public abstract string description { get; set; }
        //
        //-------------------------------------------------
        // 
        //-------------------------------------------------
        //
        public abstract string tabCaption { get; set; }
        //
        //-------------------------------------------------
        // 
        //-------------------------------------------------
        //
        public abstract string tabStyleClass { get; set; }
        //
        //-------------------------------------------------
        // 
        //-------------------------------------------------
        //
        public abstract string tabLink { get; set; }
        //
        //-------------------------------------------------
        // 
        //-------------------------------------------------
        //
        public abstract void setActiveTab(string caption);
        //
        //-------------------------------------------------
        // add a column
        //-------------------------------------------------
        //
        /// <summary>
        /// Add a navigation entry. The navCaption and navLink should be set after creating a new entry. The first nav entry does not need to be added.
        /// </summary>
        public abstract void addTab();
        //
        //-------------------------------------------------
        // get
        //-------------------------------------------------
        //
        public abstract string getHtml(CPBaseClass cp);
    }
}
