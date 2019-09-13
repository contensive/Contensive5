﻿
using Contensive.BaseClasses;
using System;

namespace Contensive.Models.Db {
    [System.Serializable]
    public class PageContentModel : DbBaseModel {
        //
        //====================================================================================================
        //-- const
        public const string contentName = "page content";
        public const string contentTableNameLowerCase = "ccpagecontent";
        public const string contentDataSource = "default";
        public const bool nameFieldIsUnique = true;
        //
        //====================================================================================================
        // -- instance properties
        /// <summary>
        /// A structured json that holds the list of addons to run on this page. Each element in this list includes
        /// - addonGuid
        /// - instanceId (guid of content, etc)
        /// - childList
        /// </summary>
        public string addonList { get; set; }
        public bool allowBrief { get; set; }
        public bool allowChildListDisplay { get; set; }
        public bool allowFeedback { get; set; }
        public bool allowHitNotification { get; set; }
        public bool allowInChildLists { get; set; }
        public bool allowInMenus { get; set; }
        public bool allowLastModifiedFooter { get; set; }
        public bool allowMessageFooter { get; set; }
        public bool allowMetaContentNoFollow { get; set; }
        public bool allowMoreInfo { get; set; }
        public bool allowReturnLinkDisplay { get; set; }
        public bool allowReviewedFooter { get; set; }
        public bool allowSeeAlso { get; set; }
        public int archiveParentID { get; set; }
        public bool blockContent { get; set; }
        public bool blockPage { get; set; }
        public int blockSourceID { get; set; }
        public string briefFilename { get; set; }
        public string childListInstanceOptions { get; set; }
        public int childListSortMethodID { get; set; }
        public bool childPagesFound { get; set; }
        public int clicks { get; set; }
        public int contactMemberID { get; set; }
        public int contentPadding { get; set; }
        public FieldTypeHTMLFile copyfilename { get; set; } = new FieldTypeHTMLFile();
        public string customBlockMessage { get; set; }
        public DateTime dateArchive { get; set; }
        public DateTime dateExpires { get; set; }
        public DateTime dateReviewed { get; set; }
        public string headline { get; set; }
        public string imageFilename { get; set; }
        public bool isSecure { get; set; }
        public string jSEndBody { get; set; }
        public string jSFilename { get; set; }
        public string jSHead { get; set; }
        public string jSOnLoad { get; set; }
        public string linkAlias { get; set; }
        public string menuHeadline { get; set; }
        public string metaDescription { get; set; }
        public string metaKeywordList { get; set; }
        public string otherHeadTags { get; set; }
        public string pageLink { get; set; }
        public string pageTitle { get; set; }
        public int parentID { get; set; }
        public string parentListName { get; set; }
        public DateTime pubDate { get; set; }
        public int RegistrationGroupID { get; set; }
        public int reviewedBy { get; set; }
        public int TemplateID { get; set; }
        public int TriggerAddGroupID { get; set; }
        public int TriggerConditionGroupID { get; set; }
        public int TriggerConditionID { get; set; }
        public int TriggerRemoveGroupID { get; set; }
        public int TriggerSendSystemEmailID { get; set; }
        public int Viewings { get; set; }
        public string link { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// mark record reviewed
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="pageId"></param>
        public static void markReviewed(CPBaseClass cp, int pageId) {
            try {
                var page = create<PageContentModel>(cp, pageId);
                if (page != null) {
                    page.dateReviewed = DateTime.Now;
                    page.reviewedBy = cp.User.Id;
                    page.save(cp, cp.User.Id, true);
                }
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
            }
        }
    }
}
