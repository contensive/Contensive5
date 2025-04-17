
using Contensive.BaseClasses;
using System;
using System.Data;

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
        /// <summary>
        /// JSON for List<AddonListItemModel>
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
        /// <summary>
        /// deprecated
        /// </summary>
        public bool allowReturnLinkDisplay { get; set; }
        public bool allowReviewedFooter { get; set; }
        public bool allowSeeAlso { get; set; }
        public int archiveParentId { get; set; }
        public bool blockContent { get; set; }
        public bool blockPage { get; set; }
        public int blockSourceId { get; set; }
        public FieldTypeHTMLFile briefFilename { get; set; }
        public string childListInstanceOptions { get; set; }
        public int childListSortMethodId { get; set; }
        public bool childPagesFound { get; set; }
        public int clicks { get; set; }
        public int contactMemberId { get; set; }
        public int contentPadding { get; set; }
        public FieldTypeHTMLFile copyfilename { get; set; } = new FieldTypeHTMLFile();
        public FieldTypeHTMLFile customBlockMessage { get; set; }
        public DateTime? dateArchive { get; set; }
        public DateTime? dateExpires { get; set; }
        public DateTime? dateReviewed { get; set; }
        public string headline { get; set; }
        public FieldTypeFile imageFilename { get; set; }
        /// <summary>
        /// deprecated. https vs http should be controlled by the webserver software.
        /// </summary>
        [Obsolete("Deprecated. https vs http should be controlled by the webserver software.", false)]
        public bool isSecure { get; set; }
        public string jSEndBody { get; set; }
        public FieldTypeJavascriptFile jSFilename { get; set; }
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
        [Obsolete("deprecated, cached, track externally", false)] public int viewings { get; set; }
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
        //
        //====================================================================================================
        /// <summary>
        /// get the number of pages that have not been reviewed in the last 90 days
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public static int getPagesToReviewCount(CPBaseClass cp) {
            try {
                using DataTable dt = cp.Db.ExecuteQuery(@$"
                    select 
                        count(*) as cnt 
                    from 
                        ccpagecontent 
                    where 
                        ((dateReviewed is null) or (dateReviewed<{cp.Db.EncodeSQLDate(DateTime.UtcNow.AddDays(-90))}))
                        and (dateExpires is null) 
                        and (active>0)
                ");
                if (dt?.Rows != null) { return cp.Utils.EncodeInteger(dt.Rows[0]["cnt"]); }
                return 0;
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// verify all page content has Page URL.
        /// Page URL runs out of a cache. If a Page URL is requested by pageId that is no in linkalis cache, it reloads the addoncache which is expensive.
        /// </summary>
        /// <param name="cp"></param>
        public static void verifyPageUrl(CPBaseClass cp) {
            string sql = "" +
                "select " +
                    "p.id,p.name,p.linkalias " +
                "from " +
                    "ccPageContent p " +
                    "left join ccLinkAliases a on a.pageid=p.id " +
                "where 1=1 " +
                    "and(a.id is null) " +
                    "and(p.active>0) " +
                "";
            DataTable dt = cp.Db.ExecuteQuery(sql);
            if (dt == null || dt.Rows.Count == 0) { return; }
            foreach (DataRow dr in dt.Rows) {
                // -- normalize new linkalis
                string pageLinkAlias = cp.Utils.EncodeText(dr[2]);
                string pageName = cp.Utils.EncodeText(dr[1]);
                int pageId = cp.Utils.EncodeInteger(dr[0]);
                if (string.IsNullOrEmpty(pageLinkAlias)) { pageLinkAlias = pageName.ToLowerInvariant(); }
                if (string.IsNullOrEmpty(pageLinkAlias)) { pageLinkAlias = $"page{pageId}"; }
                string linkAliasNormalized = LinkAliasModel.normalizeLinkAlias(cp, pageLinkAlias);
                // -- save back to page record
                int recordsAffected = 0;
                cp.Db.ExecuteNonQuery($"update ccpagecontent set linkalias={cp.Db.EncodeSQLText(pageLinkAlias)} where id={pageId}", ref recordsAffected);
                // -- create Page URL record
                LinkAliasModel asdf = addDefault<LinkAliasModel>(cp);
                if (asdf == null) { continue; }
                asdf.name = linkAliasNormalized;
                asdf.pageId = pageId;
                asdf.save(cp);
            }
        }
        //
        //========================================================================
        /// <summary>
        /// Save Page URL field, managing duplicates and creating the linkalias field.
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="adminData"></param>
        /// <returns>The normalized Page URL saved</returns>
        public static string savePageContentPageUrl(CPBaseClass cp, string linkAlias, int recordId, string recordName, bool overrideDuplicates) {
            try {
                if (string.IsNullOrEmpty(linkAlias)) { linkAlias = recordName.ToLowerInvariant(); }
                if (string.IsNullOrEmpty(linkAlias)) { linkAlias = $"page{recordId}"; }
                //
                string linkAliasNormalized = LinkAliasModel.normalizeLinkAlias(cp, linkAlias);
                if (getCount<PageContentModel>(cp, $"(linkalias={cp.Db.EncodeSQLText(linkAlias)})and(id<>{recordId})") > 0) {
                    //
                    // -- there is another page record with the same alias
                    if (overrideDuplicates) {
                        cp.Db.ExecuteQuery($"update ccpagecontent set linkalias=null where ( linkalias={cp.Db.EncodeSQLText(linkAlias)}) and (id<>{recordId})");
                    } else {
                        cp.Site.ErrorReport("The Page URL you entered can not be used because another record uses this value [" + linkAlias + "]. Enter a different Page URL, or check the Override Duplicates checkbox in the Page URL tab.");
                        return linkAliasNormalized;
                    }
                }
                int recordsAffected = 0;
                cp.Db.ExecuteNonQuery($"update ccpagecontent set linkalias={cp.Db.EncodeSQLText(linkAlias)} where id={recordId}", ref recordsAffected);
                if (recordsAffected == 0) {
                    var link = DbBaseModel.addDefault<LinkAliasModel>(cp);
                }
                //
                return linkAliasNormalized;
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }

    }
}
