
using Contensive.BaseClasses;
using Contensive.Models.Db;
using System;
//
namespace Contensive.Processor.Addons.ContentApi {
    //
    public class ImageUploadRemoteMethod : AddonBaseClass {
        //
        private class UploadRequest {
            public string filename { get; set; }
            public string base64Content { get; set; }
            public string altText { get; set; }
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
                var request = cp.JSON.Deserialize<UploadRequest>(requestBody);
                if (request == null || string.IsNullOrEmpty(request.filename) || string.IsNullOrEmpty(request.base64Content)) {
                    return ContentApiHelper.errorResponse(cp, "filename and base64Content are required.");
                }
                //
                // -- decode base64 content
                byte[] fileBytes;
                try {
                    fileBytes = Convert.FromBase64String(request.base64Content);
                } catch {
                    return ContentApiHelper.errorResponse(cp, "Invalid base64Content.");
                }
                //
                // -- save to CDN
                string cdnPath = $"content/images/{request.filename}";
                cp.CdnFiles.SaveBinary(cdnPath, fileBytes);
                //
                // -- create library file record
                var libraryFile = DbBaseModel.addDefault<LibraryFilesModel>(cp);
                if (libraryFile == null) {
                    return ContentApiHelper.errorResponse(cp, "Failed to create library file record.");
                }
                libraryFile.name = request.filename;
                libraryFile.filename = cdnPath;
                libraryFile.altText = request.altText ?? "";
                libraryFile.fileSize = fileBytes.Length;
                if (request.folderId > 0) { libraryFile.folderId = request.folderId; }
                libraryFile.save(cp);
                //
                string fileUrl = $"{cp.Http.CdnFilePathPrefix}{cdnPath}";
                //
                var result = new {
                    id = libraryFile.id,
                    url = fileUrl,
                    filename = request.filename,
                    altText = libraryFile.altText
                };
                return ContentApiHelper.successResponse(cp, result, "Image uploaded.");
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return ContentApiHelper.errorResponse(cp, $"Error: {ex.Message}");
            }
        }
    }
}
