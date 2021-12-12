
using Contensive.BaseClasses;
using System;

namespace Contensive.Models.Db {
    //
    public class PageContentModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("page content", "ccpagecontent", "default", true);
        //
        //====================================================================================================
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
        public int archiveParentId { get; set; }
        public bool blockContent { get; set; }
        public bool blockPage { get; set; }
        public int blockSourceId { get; set; }
        public string briefFilename { get; set; }
        public string childListInstanceOptions { get; set; }
        public int childListSortMethodId { get; set; }
        public bool childPagesFound { get; set; }
        public int clicks { get; set; }
        public int contactMemberId { get; set; }
        public int contentPadding { get; set; }
        public FieldTypeHTMLFile copyfilename { get; set; } = new FieldTypeHTMLFile();
        public string customBlockMessage { get; set; }
        public DateTime? dateArchive { get; set; }
        public DateTime? dateExpires { get; set; }
        public DateTime? dateReviewed { get; set; }
        public string headline { get; set; }
        public string imageFilename { get; set; }
        /// <summary>
        /// deprecated. https vs http should be controlled by the webserver software.
        /// </summary>
        [Obsolete("Deprecated. https vs http should be controlled by the webserver software.", false)]
        public bool isSecure { get; set; }
        public string jSEndBody { get; set; }
        public string jSFilename { get; set; }
        public string jSHead { get; set; }
        public string jSOnLoad { get; set; }
        public string linkAlias { get; set; }
        /// <summary>
        /// html class added to the LI element of dynamic bootstrap menus
        /// </summary>
        public string menuClass { get; set; }
        public string menuHeadline { get; set; }
        public string metaDescription { get; set; }
        public string metaKeywordList { get; set; }
        public string otherHeadTags { get; set; }
        public string pageTitle { get; set; }
        public int parentId { get; set; }
        public string parentListName { get; set; }
        public DateTime? pubDate { get; set; }
        public int registrationGroupId { get; set; }
        public int reviewedBy { get; set; }
        public int templateId { get; set; }
        public int triggerAddGroupId { get; set; }
        public int triggerConditionGroupId { get; set; }
        public int triggerConditionId { get; set; }
        public int triggerRemoveGroupId { get; set; }
        public int triggerSendSystemEmailId { get; set; }
        public int viewings { get; set; }
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
