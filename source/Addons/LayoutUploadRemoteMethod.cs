using Contensive.BaseClasses;
using Contensive.Models.Db;
using System;
using System.Collections.Generic;
using System.IO;
using static Contensive.BaseClasses.CPLayoutBaseClass;

namespace YourNamespace.Addons {
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
                // -- check authentication
                if (!cp.User.IsAuthenticated) {
                    return createJsonResponse(false, "Authentication required", null);
                }
                //
                // -- check admin access
                if (!cp.User.IsAdmin) {
                    return createJsonResponse(false, "Admin access required", null);
                }
                //
                // -- get parameters
                string layoutName = cp.Doc.GetText("layoutName");
                int layoutId = cp.Doc.GetInteger("layoutId");
                //
                // -- check for uploaded file
                string uploadedFilename = "";
                if (!cp.TempFiles.SaveUpload("htmlFile", ref uploadedFilename)) {
                    return createJsonResponse(false, "No file uploaded", null);
                }
                //
                // -- verify file extension
                string extension = Path.GetExtension(uploadedFilename).ToLowerInvariant();
                if (extension != ".html" && extension != ".htm" && extension != ".zip") {
                    cp.TempFiles.DeleteFile(uploadedFilename);
                    return createJsonResponse(false, "Only HTML and ZIP files are allowed", null);
                }
                //
                // -- process the uploaded file
                var userMessageList = new List<string>();
                bool success = cp.Layout.processImportFile(
                    uploadedFilename,
                    ImporttypeEnum.SetInMetadata,
                    layoutId,
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