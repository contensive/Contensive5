
using Contensive.BaseClasses;
using Contensive.Models.Db;
using System;
//
namespace Contensive.Processor.Addons.ContentApi {
    //
    public class PageCreateRemoteMethod : AddonBaseClass {
        //
        private class PageCreateRequest {
            public string headline { get; set; }
            public string name { get; set; }
            public int parentId { get; set; }
            public int templateId { get; set; }
            public string linkAlias { get; set; }
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
                if (request == null) {
                    return ContentApiHelper.errorResponse(cp, "Invalid request body.");
                }
                //
                var page = DbBaseModel.addDefault<PageContentModel>(cp);
                if (page == null) {
                    return ContentApiHelper.errorResponse(cp, "Failed to create page record.");
                }
                //
                if (!string.IsNullOrEmpty(request.headline)) { page.headline = request.headline; }
                if (!string.IsNullOrEmpty(request.name)) { page.name = request.name; }
                if (request.parentId > 0) { page.parentId = request.parentId; }
                if (request.templateId > 0) { page.templateId = request.templateId; }
                if (!string.IsNullOrEmpty(request.linkAlias)) { page.linkAlias = request.linkAlias; }
                //
                page.save(cp);
                //
                var result = new {
                    id = page.id,
                    guid = page.ccguid
                };
                return ContentApiHelper.successResponse(cp, result, "Page created.");
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return ContentApiHelper.errorResponse(cp, $"Error: {ex.Message}");
            }
        }
    }
}
