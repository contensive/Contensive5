
using Contensive.BaseClasses;
using Contensive.Models.Db;
using System;
using System.Data;
using System.IO;
//
namespace Contensive.Processor.Addons.ContentApi {
    //
    public class ResourceUploadRemoteMethod : AddonBaseClass {
        //
        private class ResourceUploadRequest {
            public string filename { get; set; }
            public string base64Content { get; set; }
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
                var request = cp.JSON.Deserialize<ResourceUploadRequest>(requestBody);
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
                // -- detect file type from extension
                string ext = Path.GetExtension(request.filename).TrimStart('.').ToLowerInvariant();
                int fileTypeId = 0;
                string fileTypeName = "";
                bool isImage = false;
                bool isDownload = false;
                bool isVideo = false;
                using (DataTable dtType = cp.Db.ExecuteQuery(
                    $"SELECT TOP 1 id, name, ISNULL(isImage,0) AS isImage, ISNULL(isDownload,0) AS isDownload, ISNULL(isVideo,0) AS isVideo FROM ccLibraryFileTypes WHERE extensionList LIKE {cp.Db.EncodeSQLText($"%{ext}%")} AND (active<>0 OR active IS NULL)")) {
                    if (dtType?.Rows != null && dtType.Rows.Count > 0) {
                        fileTypeId = cp.Utils.EncodeInteger(dtType.Rows[0]["id"]);
                        fileTypeName = dtType.Rows[0]["name"]?.ToString() ?? "";
                        isImage = dtType.Rows[0]["isImage"]?.ToString() == "1";
                        isDownload = dtType.Rows[0]["isDownload"]?.ToString() == "1";
                        isVideo = dtType.Rows[0]["isVideo"]?.ToString() == "1";
                    }
                }
                //
                // -- save to CDN
                string cdnPath = $"cclibraryfiles/filename/{request.filename}";
                cp.CdnFiles.SaveBinary(cdnPath, fileBytes);
                //
                // -- create library file record
                var libraryFile = DbBaseModel.addDefault<LibraryFilesModel>(cp);
                if (libraryFile == null) {
                    return ContentApiHelper.errorResponse(cp, "Failed to create library file record.");
                }
                libraryFile.name = !string.IsNullOrEmpty(request.name) ? request.name : request.filename;
                libraryFile.filename = cdnPath;
                libraryFile.altText = request.altText ?? "";
                libraryFile.description = request.description ?? "";
                libraryFile.fileSize = fileBytes.Length;
                libraryFile.fileTypeId = fileTypeId;
                if (request.folderId > 0) { libraryFile.folderId = request.folderId; }
                libraryFile.save(cp);
                //
                var result = new {
                    id = libraryFile.id,
                    name = libraryFile.name,
                    filename = cdnPath,
                    altText = libraryFile.altText,
                    fileSize = fileBytes.Length,
                    fileTypeId,
                    fileTypeName,
                    isImage,
                    isDownload,
                    isVideo
                };
                return ContentApiHelper.successResponse(cp, result, "Resource uploaded.");
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return ContentApiHelper.errorResponse(cp, $"Error: {ex.Message}");
            }
        }
    }
}
