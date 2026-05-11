
using Contensive.BaseClasses;
using Contensive.Models.Db;
using System;
//
namespace Contensive.Processor.Addons.ContentApi {
    //
    public class PageCreateRemoteMethod : AddonBaseClass {
        //
        private class PageCreateRequest {
            public string parentUrl { get; set; }
            public string navHeadline { get; set; }
            public string headline { get; set; }
            public int templateId { get; set; }
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
                var request = cp.JSON.Deserialize<PageCreateRequest>(requestBody);
                if (request == null || string.IsNullOrEmpty(request.navHeadline)) {
                    return ContentApiHelper.errorResponse(cp, "navHeadline is required.");
                }
                //
                // -- resolve parent page from parentUrl
                int parentId = 0;
                string parentUrlBase = "";
                if (!string.IsNullOrEmpty(request.parentUrl)) {
                    var parentContext = UrlResolverHelper.resolve(cp, request.parentUrl);
                    if (parentContext == null) {
                        return ContentApiHelper.errorResponse(cp, $"Parent page not found for url '{request.parentUrl}'.");
                    }
                    parentId = parentContext.pageId;
                    parentUrlBase = parentContext.linkAliasName;
                }
                //
                // -- build new URL: parent path + normalized navHeadline slug
                string slug = LinkAliasModel.normalizeLinkAlias(cp, request.navHeadline);
                string newUrl = parentUrlBase.TrimEnd('/') + slug;
                //
                // -- create page record
                var page = DbBaseModel.addDefault<PageContentModel>(cp);
                if (page == null) {
                    return ContentApiHelper.errorResponse(cp, "Failed to create page record.");
                }
                //
                page.name = request.navHeadline;
                page.menuHeadline = request.navHeadline;
                page.headline = !string.IsNullOrEmpty(request.headline) ? request.headline : request.navHeadline;
                if (parentId > 0) { page.parentId = parentId; }
                if (request.templateId > 0) { page.templateId = request.templateId; }
                page.save(cp);
                //
                // -- create link alias record for the new URL
                var linkAlias = DbBaseModel.addDefault<LinkAliasModel>(cp);
                if (linkAlias != null) {
                    linkAlias.name = newUrl;
                    linkAlias.pageId = page.id;
                    linkAlias.queryStringSuffix = "";
                    linkAlias.save(cp);
                }
                //
                return ContentApiHelper.successResponse(cp, new { pageId = page.id, url = newUrl }, "Page created.");
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return ContentApiHelper.errorResponse(cp, $"Error: {ex.Message}");
            }
        }
    }
}
