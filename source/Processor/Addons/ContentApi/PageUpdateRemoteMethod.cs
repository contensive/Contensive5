
using Contensive.BaseClasses;
using System;
//
namespace Contensive.Processor.Addons.ContentApi {
    //
    public class PageUpdateRemoteMethod : AddonBaseClass {
        //
        private class PageUpdateRequest {
            public string url { get; set; }
            public string headline { get; set; }
            public string navHeadline { get; set; }
            public string metaDescription { get; set; }
            public string metaKeywordList { get; set; }
            public string structuredData { get; set; }
            public string pageTitle { get; set; }
            public int templateId { get; set; }
            public int parentId { get; set; }
            public bool? allowInMenus { get; set; }
            public bool? blockPage { get; set; }
        }
        //
        public override object Execute(CPBaseClass cp) {
            try {
                if (!ContentApiAuth.requireAdmin(cp)) {
                    return ContentApiHelper.errorResponse(cp, "Admin access required.");
                }
                //
                string requestBody = cp.Request.Body;
                if (string.IsNullOrEmpty(requestBody)) {
                    return ContentApiHelper.errorResponse(cp, "Request body is required.");
                }
                var request = cp.JSON.Deserialize<PageUpdateRequest>(requestBody);
                if (request == null || string.IsNullOrEmpty(request.url)) {
                    return ContentApiHelper.errorResponse(cp, "url is required.");
                }
                //
                var context = UrlResolverHelper.resolve(cp, request.url);
                if (context == null) {
                    return ContentApiHelper.errorResponse(cp, $"No page found for url '{request.url}'.");
                }
                var page = context.page;
                //
                if (request.headline != null) { page.headline = request.headline; }
                if (request.navHeadline != null) { page.menuHeadline = request.navHeadline; }
                if (request.metaDescription != null) { page.metaDescription = request.metaDescription; }
                if (request.metaKeywordList != null) { page.metaKeywordList = request.metaKeywordList; }
                if (request.structuredData != null) { page.structuredData = request.structuredData; }
                if (request.pageTitle != null) { page.pageTitle = request.pageTitle; }
                if (request.templateId > 0) { page.templateId = request.templateId; }
                if (request.parentId > 0) { page.parentId = request.parentId; }
                if (request.allowInMenus.HasValue) { page.allowInMenus = request.allowInMenus.Value; }
                if (request.blockPage.HasValue) { page.blockPage = request.blockPage.Value; }
                //
                page.save(cp);
                //
                return ContentApiHelper.successResponse(cp, new { pageId = page.id, url = context.linkAliasName }, "Page updated.");
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return ContentApiHelper.errorResponse(cp, $"Error: {ex.Message}");
            }
        }
    }
}
