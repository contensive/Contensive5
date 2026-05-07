
using Contensive.BaseClasses;
using Contensive.Models.Db;
using System;
//
namespace Contensive.Processor.Addons.ContentApi {
    //
    public class PageGetRemoteMethod : AddonBaseClass {
        //
        public override object Execute(CPBaseClass cp) {
            try {
                if (!ContentApiAuth.requireAdmin(cp)) {
                    return ContentApiHelper.errorResponse(cp, "Admin access required.");
                }
                //
                int pageId = cp.Doc.GetInteger("pageId");
                string guid = cp.Doc.GetText("guid");
                //
                PageContentModel page = null;
                if (pageId > 0) {
                    page = DbBaseModel.create<PageContentModel>(cp, pageId);
                } else if (!string.IsNullOrEmpty(guid)) {
                    page = DbBaseModel.create<PageContentModel>(cp, guid);
                }
                //
                if (page == null) {
                    return ContentApiHelper.errorResponse(cp, "Page not found.");
                }
                //
                string bodyHtml = page.copyfilename?.content ?? "";
                //
                var result = new {
                    id = page.id,
                    name = page.name,
                    guid = page.ccguid,
                    parentId = page.parentId,
                    linkAlias = page.linkAlias,
                    headline = page.headline,
                    metaDescription = page.metaDescription,
                    metaKeywordList = page.metaKeywordList,
                    pageTitle = page.pageTitle,
                    templateId = page.templateId,
                    addonListJson = page.addonList ?? "",
                    active = page.active,
                    bodyHtml = bodyHtml
                };
                //
                return ContentApiHelper.successResponse(cp, result);
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return ContentApiHelper.errorResponse(cp, $"Error: {ex.Message}");
            }
        }
    }
}
