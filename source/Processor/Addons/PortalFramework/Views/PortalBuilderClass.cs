using Contensive.BaseClasses;
using Contensive.Processor.Addons.PortalFramework.Models.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Contensive.Processor.Addons.PortalFramework.Views {
    public class PortalBuilderClass {
        /// <summary>
        /// The nav items at the top -- features in this portal with no parent
        /// </summary>
        private List<PortalBuilderNavItemModel> navItemList { get; set; } = new List<PortalBuilderNavItemModel>();
        private List<PortalBuilderSubNavItemModel> subNavItemList { get; set; } = new List<PortalBuilderSubNavItemModel>();
        private List<PortalBuilderChildSubNavListModel> childSubNavList { get; set; } = new List<PortalBuilderChildSubNavListModel>();
        /// <summary>
        /// if true, background padding added
        /// </summary>
        public bool includeBodyPadding { get; set; }
        /// <summary>
        /// if true, background color added
        /// </summary>
        public bool includeBodyColor { get; set; }
        /// <summary>
        /// if true, styles and js are added on return
        /// </summary>
        public bool isOuterContainer { get; set; }
        //
        //====================================================================================================
        //
        private void checkNavPtr() {
            navItemList ??= [];
            if (navItemList.Count == 0) { addNav(); }
        }
        //
        //====================================================================================================
        //
        public string styleSheet {
            get {
                return "";
            }
        }
        //
        //====================================================================================================
        //
        public string javascript {
            get {
                return "";
            }
        }
        //
        public string body { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// title added to the Nav
        /// </summary>
        public string title { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// optional title added to the subnav. Example, if the main nav item is a list of accounts. Click on an account takes user to a child feature. The subnav appears for the child features and the subNavTitle could be Account 92
        /// </summary>
        public string subNavTitle { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// warning message added below nav
        /// </summary>
        public string warning { get; set; }
        //
        //====================================================================================================
        //
        public string description { get; set; }
        //
        //====================================================================================================
        //
        public string navCaption {
            get {
                checkNavPtr();
                return navItemList.Last().caption;
            }
            set {
                checkNavPtr();
                navItemList.Last().caption = value;
            }
        }
        //
        //====================================================================================================
        //
        public string navLink {
            get {
                checkNavPtr();
                return navItemList.Last().link;
            }
            set {
                checkNavPtr();
                navItemList.Last().link = value;
            }
        }
        //
        //====================================================================================================
        //
        public void setActiveNav(string caption) {
            checkNavPtr();
            foreach ( var nav in navItemList) {
                if (nav.caption.ToLower() == caption.ToLower()) { 
                    nav.active = true;
                    return;
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Add a navigation entry. The navCaption and navLink should be set after creating a new entry. The first nav entry does not need to be added.
        /// </summary>
        public void addNav() {
            navItemList ??= [];
            navItemList.Add(new PortalBuilderNavItemModel() {
                caption = "",
                link = "",
                active = false,
                isPortalLink = false,
                navFlyoutList = new List<PortalBuilderSubNavItemModel>()
            });
        }
        //
        //====================================================================================================
        //
        public void addNav(PortalBuilderNavItemModel navItem) {
            navItemList ??= [];
            navItemList.Add(navItem);
        }
        //
        //====================================================================================================
        //
        public void addSubNav(PortalBuilderSubNavItemModel subNavItem) {
            subNavItemList.Add(subNavItem);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Add a navigation entry. The navCaption and navLink should be set after creating a new entry. The first nav entry does not need to be added.
        /// </summary>
        [Obsolete("Use addNav(). Deprecated.", false)] public void addPortalNav() => addNav();
        //
        //====================================================================================================
        //
        public string getHtml(CPBaseClass cp) {
            try {
                //
                // todo, a second model is not needed
                PortalBuilderViewModel viewModel = new PortalBuilderViewModel {
                    navItemList = [],
                    subNavItemList = [],
                    childSubNavLists = [[]],
                    warning = cp.Utils.ConvertHTML2Text(cp.UserError.GetList()),
                    title = title,
                    description = description,
                    body = body,
                    subNavTitle = subNavTitle
                };
                //
                // -- build nav
                foreach (var nav in navItemList) {
                    if (!string.IsNullOrEmpty(nav.caption)) {
                        viewModel.navItemList.Add(nav);
                    }
                }
                //
                // -- build subnav
                foreach (var subNavItem in subNavItemList) {
                    if (!string.IsNullOrEmpty(subNavItem.subCaption)) {
                        viewModel.subNavItemList.Add(subNavItem);
                    }
                }
                //
                // -- build all the childSubNav lists
                foreach (var childSubNav in childSubNavList) {
                    // -- build this childSubNav
                    foreach (var subNavItem in childSubNav.childSubNavItemList) {
                        if (!string.IsNullOrEmpty(subNavItem.subCaption)) {
                            viewModel.subNavItemList.Add(subNavItem);
                        }
                    }
                }
                //
                // -- if outer container, add styles and javascript
                if (isOuterContainer) {
                    //cp.Doc.AddHeadJavascript(Properties.Resources.javascript);
                    //cp.Doc.AddHeadStyle(Properties.Resources.styles);
                }
                //
                // -- render layout
                string layout = cp.Layout.GetLayout(Contensive.Processor.Constants.guidLayoutPageWithNav, Contensive.Processor.Constants.nameLayoutPageWithNav, Contensive.Processor.Constants.pathFilenameLayoutAdminUIPageWithNav);
                return cp.Mustache.Render(layout, viewModel);
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
        //
        /// <summary>
        /// This class is one row in the child subnav.
        /// </summary>
        public class PortalBuilderChildSubNavListModel {
            public List<PortalBuilderSubNavItemModel> childSubNavItemList;
        }
    }
}
