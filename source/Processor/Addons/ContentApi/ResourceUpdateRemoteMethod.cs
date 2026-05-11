
using Contensive.BaseClasses;
using Contensive.Models.Db;
using System;
//
namespace Contensive.Processor.Addons.ContentApi {
    //
    public class ResourceUpdateRemoteMethod : AddonBaseClass {
        //
        private class ResourceUpdateRequest {
            public int id { get; set; }
            public string name { get; set; }
            public string altText { get; set; }
            public string description { get; set; }
            public int folderId { get; set; }
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
                var request = cp.JSON.Deserialize<ResourceUpdateRequest>(requestBody);
                if (request == null || request.id <= 0) {
                    return ContentApiHelper.errorResponse(cp, "A valid id is required.");
                }
                //
                var file = DbBaseModel.create<LibraryFilesModel>(cp, request.id);
                if (file == null) {
                    return ContentApiHelper.errorResponse(cp, $"Resource #{request.id} not found.");
                }
                //
                if (request.name != null) { file.name = request.name; }
                if (request.altText != null) { file.altText = request.altText; }
                if (request.description != null) { file.description = request.description; }
                if (request.folderId > 0) { file.folderId = request.folderId; }
                file.save(cp);
                //
                return ContentApiHelper.successResponse(cp, new {
                    id = file.id,
                    name = file.name,
                    altText = file.altText,
                    description = file.description,
                    folderId = file.folderId,
                    filename = file.filename
                }, "Resource updated.");
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return ContentApiHelper.errorResponse(cp, $"Error: {ex.Message}");
            }
        }
    }
}
