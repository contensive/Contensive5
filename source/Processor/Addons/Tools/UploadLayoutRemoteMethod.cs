using Contensive.BaseClasses;
using Contensive.Models.Db;
using System;
using System.Collections.Generic;
using System.IO;
using static Contensive.BaseClasses.CPLayoutBaseClass;

namespace Contensive.Processor.Addons.Tools {
    /// <summary>
    /// Remote method addon for uploading HTML files to be processed and saved as layouts
    /// </summary>
    public class UploadLayoutRemoteMethod : AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// Execute method - handles file upload and processing
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(CPBaseClass cp) {
            try {
                //
                // -- not valid for production site
                if(cp.ServerConfig.productionEnvironment) {
                    return createJsonResponse(false, prodMessage, null);
                }
                //
                // -- authentication
                if (!cp.User.IsAuthenticated) {
                    string userGuid = cp.Doc.GetText("userGuid");
                    if (string.IsNullOrEmpty(userGuid)) { return createJsonResponse(false, authMessage, null); }
                    PersonModel user = DbBaseModel.create<PersonModel>(cp, userGuid);
                    if (user == null || !cp.User.LoginByID(user.id)) {
                        return createJsonResponse(false, authMessage, null);
                    }
                }
                // -- authorization
                if (!cp.User.IsAdmin) {
                    return createJsonResponse(false, adminMessage, null);
                }
                //
                // -- check for uploaded file
                string uploadedFilename = "";
                string uploadPath = $"uploadLayoutRemoteTmp{cp.Utils.GetRandomString(10)}";
                if (!cp.TempFiles.SaveUpload("htmlFile", uploadPath, ref uploadedFilename)) {
                    return createJsonResponse(false, noFileMessage, null);
                }
                //
                // Check if user has uploaded too many files recently
                int recentUploads = cp.Db.ExecuteScalar($"SELECT COUNT(*) FROM ccActivityLog WHERE MemberId={cp.User.Id} AND TypeId=11 AND DateAdded > DATEADD(minute, -5, GETDATE())");
                if (recentUploads > 10) {
                    return createJsonResponse(false, tooManyUploadsMessage, null);
                }
                //
                // After successful upload
                cp.Db.ExecuteNonQuery($@"
                    INSERT INTO ccActivityLog 
                        (Name, MemberId, TypeId, Message, DateAdded) 
                    VALUES 
                        ('Layout Upload', {cp.User.Id}, 11, 'Uploaded: {uploadedFilename}', GETDATE())
                    ");
                //
                // -- verify file extension
                string extension = Path.GetExtension(uploadedFilename).ToLowerInvariant();
                if (extension != ".html" && extension != ".htm" && extension != ".zip") {
                    cp.TempFiles.DeleteFile(uploadedFilename);
                    return createJsonResponse(false, onlyHtmlFilesMessage, null);
                }
                //
                // After SaveUpload, before processing
                var fileDetails = cp.TempFiles.FileDetails(uploadedFilename);
                if (fileDetails != null && fileDetails.Size > 104857600) { // 10MB limit
                    cp.TempFiles.DeleteFile(uploadedFilename);
                    return createJsonResponse(false, fileSizeLimitMessage, null);
                }
                //
                // -- process the uploaded file
                var userMessageList = new List<string>();
                bool success = cp.Layout.processImportFile(
                    uploadedFilename,
                    ImporttypeEnum.SetInMetadata,
                    0, // layoutid
                    0, // pageTemplateId
                    0, // emailTemplateId
                    0, // emailId
                    ref userMessageList
                );
                //
                // -- cleanup temp file
                cp.TempFiles.DeleteFile(uploadedFilename);
                //
                // -- prepare response
                if (success) {
                    return createJsonResponse(true, "File processed successfully", new {
                        messages = userMessageList
                    });
                } else {
                    return createJsonResponse(false, "Error processing file", new {
                        errors = userMessageList
                    });
                }
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return createJsonResponse(false, "Error: " + ex.Message, null);
            }
        }
        public const string curlExample = @" example: curl -X POST http://domain/uploadLayout  -F ""htmlFile=@layoutSample\UploadSampleFormLayout.html""  -F ""userGuid={1234-1234-1234-1234}""";
        public const string authMessage = "Authentication required. To authenticate, go to your user record on the site and copy the guid. Then add this guid to the post request with with the request name 'userGuid'" + curlExample;
        public const string adminMessage = "Admin access required. To use this tool your user account must have Admin access.";
        public const string noFileMessage = "No file uploaded. To include a file use an HTTP Post and attach the file to the request name 'htmlFile'." + curlExample;
        public const string tooManyUploadsMessage = "Too many uploads. Please wait before trying again.";
        public const string onlyHtmlFilesMessage = "Only HTML files are allowed.";
        public const string fileSizeLimitMessage = "File size exceeds 100MB limit.";
        public const string prodMessage = "This remote method is not available on production sites.";

        //
        //====================================================================================================
        /// <summary>
        /// Create JSON response
        /// </summary>
        /// <param name="success"></param>
        /// <param name="message"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private string createJsonResponse(bool success, string message, object data) {
            string dataJson = (data != null) ? Newtonsoft.Json.JsonConvert.SerializeObject(data) : "null";
            return $"{{\"success\":{success.ToString().ToLower()},\"message\":\"{escapeJson(message)}\",\"data\":{dataJson}}}";
        }
        //
        //====================================================================================================
        /// <summary>
        /// Escape JSON string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private string escapeJson(string value) {
            if (string.IsNullOrEmpty(value)) return "";
            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }
    }
}