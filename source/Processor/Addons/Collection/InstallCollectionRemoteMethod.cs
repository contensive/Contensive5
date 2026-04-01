
using Contensive.BaseClasses;
using Contensive.Processor.Controllers;
using System;
using System.IO;
//
namespace Contensive.Processor.Addons.Collection {
    //
    //====================================================================================================
    /// <summary>
    /// Remote method to upload and install a collection ZIP file via HTTP POST.
    /// Authentication is via a bearer token created from the admin user edit page.
    ///
    /// Example curl call:
    ///   curl -X POST https://mysite.com/installCollection \
    ///     -H "Authorization: Bearer &lt;encrypted-token&gt;" \
    ///     -F "collectionFile=@myCollection.zip"
    ///
    /// The bearer token is created on the user edit page in the admin site using the
    /// "Create Bearer Token" button. The token encodes the user identity and an expiry date.
    /// </summary>
    public class InstallCollectionRemoteMethod : AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// Accept a collection ZIP file upload and install it.
        /// </summary>
        public override object Execute(CPBaseClass cp) {
            try {
                CoreController core = ((CPClass)cp).core;
                //
                // -- not valid for production site
                if (cp.ServerConfig.productionEnvironment) {
                    return createJsonResponse(false, prodMessage, null);
                }
                //
                // -- authenticate via bearer token
                string authorizationHeader = core.docProperties.getText("authorization");
                if (!BearerTokenAuthController.tryAuthenticateByBearerToken(core, authorizationHeader, out int authenticatedUserId)) {
                    cp.Response.SetStatus(WebServerController.httpResponseStatus.Unauthorized);
                    return createJsonResponse(false, authMessage, null);
                }
                //
                // -- require admin
                if (!core.session.isAuthenticatedAdmin()) {
                    cp.Response.SetStatus(WebServerController.httpResponseStatus.Forbidden);
                    return createJsonResponse(false, adminMessage, null);
                }
                //
                // -- save the uploaded file to a temp location
                string uploadedFilename = "";
                string uploadPath = $"installCollectionTmp{cp.Utils.GetRandomString(10)}\\";
                if (!cp.TempFiles.SaveUpload("collectionFile", uploadPath, ref uploadedFilename)) {
                    return createJsonResponse(false, noFileMessage, null);
                }
                string uploadPathFilename = uploadPath + uploadedFilename;
                //
                // -- validate extension
                string extension = Path.GetExtension(uploadedFilename).ToLowerInvariant();
                if (extension != ".zip") {
                    cp.TempFiles.DeleteFolder(uploadPath);
                    return createJsonResponse(false, onlyZipMessage, null);
                }
                //
                // -- install the collection
                string userError = "";
                bool success = cp.Addon.InstallCollectionFile(uploadPathFilename, ref userError);
                //
                // -- cleanup
                cp.TempFiles.DeleteFolder(uploadPath);
                //
                if (success) {
                    return createJsonResponse(true, $"Collection installed successfully from {uploadedFilename}.", null);
                } else {
                    return createJsonResponse(false, string.IsNullOrWhiteSpace(userError) ? "Collection install failed." : userError, null);
                }
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return createJsonResponse(false, $"Error: {ex.Message}", null);
            }
        }
        //
        public const string prodMessage = "This method is not available on production sites. Set productionEnvironment to false in config.json to enable this method.";
        public const string authMessage = "Authentication required. Provide a bearer token in the Authorization header: 'Authorization: Bearer <token>'. "
            + "Generate a token from the user edit page in the admin site using the 'Create Bearer Token' button. "
            + "Example: curl -X POST https://mysite.com/installCollection -H \"Authorization: Bearer <token>\" -F \"collectionFile=@myCollection.zip\"";
        public const string adminMessage = "Admin access required. The bearer token must belong to a user with admin access.";
        public const string noFileMessage = "No file uploaded. Attach a .zip collection file to the request using the field name 'collectionFile'. "
            + "Example: curl -X POST https://mysite.com/installCollection -H \"Authorization: Bearer <token>\" -F \"collectionFile=@myCollection.zip\"";
        public const string onlyZipMessage = "Only .zip collection files are accepted.";
        //
        private static string createJsonResponse(bool success, string message, object data) {
            string dataJson = data != null ? Newtonsoft.Json.JsonConvert.SerializeObject(data) : "null";
            return $"{{\"success\":{success.ToString().ToLower()},\"message\":\"{escapeJson(message)}\",\"data\":{dataJson}}}";
        }
        //
        private static string escapeJson(string value) {
            if (string.IsNullOrEmpty(value)) { return ""; }
            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }
    }
}
