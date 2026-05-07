
using Contensive.BaseClasses;
using Contensive.Models.Db;
using System;
//
namespace Contensive.Processor.Addons.ContentApi {
    //
    public class PageUpdateRemoteMethod : AddonBaseClass {
        //
        private class PageUpdateRequest {
            public int pageId { get; set; }
            public string headline { get; set; }
            public string name { get; set; }
            public string bodyHtml { get; set; }
            public string metaDescription { get; set; }
            public string metaKeywordList { get; set; }
            public string pageTitle { get; set; }
            public string linkAlias { get; set; }
            public int templateId { get; set; }
            public bool? active { get; set; }
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
                if (request == null || request.pageId <= 0) {
                    return ContentApiHelper.errorResponse(cp, "A valid pageId is required.");
                }
                //
                var page = DbBaseModel.create<PageContentModel>(cp, request.pageId);
                if (page == null) {
                    return ContentApiHelper.errorResponse(cp, $"Page #{request.pageId} not found.");
                }
                //
                if (request.headline != null) { page.headline = request.headline; }
                if (request.name != null) { page.name = request.name; }
                if (request.metaDescription != null) { page.metaDescription = request.metaDescription; }
                if (request.metaKeywordList != null) { page.metaKeywordList = request.metaKeywordList; }
                if (request.pageTitle != null) { page.pageTitle = request.pageTitle; }
                if (request.linkAlias != null) { page.linkAlias = request.linkAlias; }
                if (request.templateId > 0) { page.templateId = request.templateId; }
                if (request.active.HasValue) { page.active = request.active.Value; }
                if (request.bodyHtml != null) { page.copyfilename.content = request.bodyHtml; }
                //
                page.save(cp);
                //
                return ContentApiHelper.successResponse(cp, new { id = page.id }, "Page updated.");
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return ContentApiHelper.errorResponse(cp, $"Error: {ex.Message}");
            }
        }
    }
}
