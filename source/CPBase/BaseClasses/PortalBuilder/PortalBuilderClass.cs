
//namespace Contensive.BaseClasses.PortalBuilder {
//    public abstract class PortalBuilderClass {
//        /// <summary>
//        /// if true, background padding added
//        /// </summary>
//        public abstract bool includeBodyPadding { get; set; }
//        /// <summary>
//        /// if true, background color added
//        /// </summary>
//        public abstract bool includeBodyColor { get; set; }
//        /// <summary>
//        /// if true, styles and js are added on return
//        /// </summary>
//        public abstract bool isOuterContainer { get; set; }
//        //
//        //====================================================================================================
//        //
//        public abstract string styleSheet { get; }
//        //
//        //====================================================================================================
//        //
//        public abstract string javascript { get;  }
//        //
//        public abstract string body { get; set; }
//        //
//        //====================================================================================================
//        /// <summary>
//        /// title added to the Nav
//        /// </summary>
//        public abstract string title { get; set; }
//        //
//        //====================================================================================================
//        /// <summary>
//        /// optional title added to the subnav. Example, if the main nav item is a list of accounts. Click on an account takes user to a child feature. The subnav appears for the child features and the subNavTitle could be Account 92
//        /// </summary>
//        public abstract string subNavTitle { get; set; }
//        //
//        //====================================================================================================
//        /// <summary>
//        /// warning message added below nav
//        /// </summary>
//        public abstract string warning { get; set; }
//        //
//        //====================================================================================================
//        //
//        public abstract string description { get; set; }
//        //
//        //====================================================================================================
//        //
//        public abstract string navCaption { get; set; }
//        //
//        //====================================================================================================
//        //
//        public abstract string navLink { get; set; }
//        //
//        //====================================================================================================
//        //
//        public abstract void setActiveNav(string caption);
//        //
//        //====================================================================================================
//        /// <summary>
//        /// Add a navigation entry. The navCaption and navLink should be set after creating a new entry. The first nav entry does not need to be added.
//        /// </summary>
//        public abstract void addNav();
//        //
//        //====================================================================================================
//        //
//        public abstract void addNav(PortalBuilderNavItemViewModel navItem);
//        //
//        //====================================================================================================
//        //
//        public abstract void addSubNav(PortalBuilderSubNavItemViewModel subNavItem);
//        //
//        //====================================================================================================
//        /// <summary>
//        /// Add a navigation entry. The navCaption and navLink should be set after creating a new entry. The first nav entry does not need to be added.
//        /// </summary>
//        public abstract void addPortalNav();
//        //
//        //====================================================================================================
//        //
//        public abstract string getHtml(CPBaseClass cp);
//    }
//}
