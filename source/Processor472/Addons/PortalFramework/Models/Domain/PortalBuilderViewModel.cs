using System.Collections.Generic;

namespace Contensive.Processor.Addons.PortalFramework.Models.Domain {
    //
    /// <summary>
    /// mustache view model for page
    /// This model is built in the PortalBuilderClass and passed to the view
    /// </summary>
    public class PortalBuilderViewModel {
        public string title { get; set; }
        public string warning { get; set; }
        public string description { get; set; }
        public string body { get; set; }
        public bool isOuterContainer { get; set; }
        /// <summary>
        /// this portals main features - features with no parent feature
        /// </summary>
        public List<PortalBuilderNavItemModel> navItemList { get; set; }
        /// <summary>
        /// true ifthe main nav has no items
        /// </summary>
        public bool navItemListEmpty {
            get {
                return navItemList.Count == 0;
            }
        }
        /// <summary>
        /// if for example the user clicks on the Account entry in the main nav, this Account section will display. The accoutn section has several subsections. Those subsections are listed here.
        /// </summary>
        public List<PortalBuilderSubNavItemModel> subNavItemList { get; set; }
        /// <summary>
        /// The bootstrap brand element of the subnav
        /// </summary>
        public List<string> subNavTitleList { get; set; } = [];
        public bool subNavItemListEmpty {
            get {
                return subNavItemList.Count == 0;
            }
        }
        /// <summary>
        /// This is a list of nav elements that appear when the selected element in the subNavList has child navs.
        /// For example, if the navList has an element "Meetings". When it is clicked, a list of meetings is displayed with no subnavs. When when a user clicks a meeting, there may be several views for that meeting. these are the subNavList.
        /// One of those elements in the subnavList might be "Sessions". When a user clicks Sessions, they see a view with a list of sessions. If they click on one other those sessions, they see the session details, but there might be several other views of that session. Those are in childSubNavLists[0]
        /// And this may repeat for childSubNavLists[1], etc.
        /// 
        /// In this example, the Meetings feature has no parent so it appear in the navList.
        /// MeetingDetails has a parent of Meetings, so when it is clicked, the subNavList appears with all other child pages of Meetings.
        /// one of those child pages of Meetings is Sessions. When sessions is clicked, the subNavList still displays, but below it are all the child features of Sessions.
        /// 
        /// When created
        /// navList always has the features with no parent
        /// subNavList has the child features of the selected navList item
        /// childSubNavLists has an array of subNavLists that appear below subNavList. childSubNavLists[0] are all the features that are children of the current subNavList item. etc.
        /// 
        /// </summary>
        public List<ChildSubNavItemListModel> childSubNavItemLists { get; set; }

    }
    public class ChildSubNavItemListModel {
        public List<PortalBuilderSubNavItemModel> childSubNavItemList { get; set; }
    }
}
