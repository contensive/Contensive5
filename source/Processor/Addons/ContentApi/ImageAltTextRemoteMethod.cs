
using Contensive.BaseClasses;
using Contensive.Models.Db;
using System;
//
namespace Contensive.Processor.Addons.ContentApi {
    //
    public class ImageAltTextRemoteMethod : AddonBaseClass {
        //
        private class AltTextRequest {
            public int imageId { get; set; }
            public string altText { get; set; }
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
                var request = cp.JSON.Deserialize<AltTextRequest>(requestBody);
                if (request == null || request.imageId <= 0) {
                    return ContentApiHelper.errorResponse(cp, "A valid imageId is required.");
                }
                //
                var file = DbBaseModel.create<LibraryFilesModel>(cp, request.imageId);
                if (file == null) {
                    return ContentApiHelper.errorResponse(cp, $"Image #{request.imageId} not found.");
                }
                //
                file.altText = request.altText ?? "";
                file.save(cp);
                //
                return ContentApiHelper.successResponse(cp, new { id = file.id, altText = file.altText }, "Alt text updated.");
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return ContentApiHelper.errorResponse(cp, $"Error: {ex.Message}");
            }
        }
    }
}
