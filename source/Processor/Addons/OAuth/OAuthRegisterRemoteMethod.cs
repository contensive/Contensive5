
using Contensive.BaseClasses;
using Contensive.Models.Db;
using System;
//
namespace Contensive.Processor.Addons.OAuth {
    //
    /// <summary>
    /// Handles OAuth 2.0 Dynamic Client Registration (RFC 7591) at POST /oauth/register.
    /// Allows MCP clients (e.g. Claude.ai) to self-register and obtain a client_id
    /// before starting the authorization code flow.
    /// </summary>
    public class OAuthRegisterRemoteMethod : AddonBaseClass {
        //
        private class RegistrationRequest {
            public string[] redirect_uris { get; set; }
            public string client_name { get; set; }
            public string token_endpoint_auth_method { get; set; }
            public string[] grant_types { get; set; }
            public string[] response_types { get; set; }
        }
        //
        public override object Execute(CPBaseClass cp) {
            try {
                cp.Response.SetType("application/json");
                cp.Response.AddHeader("Cache-Control", "no-store");
                //
                string requestBody = cp.Request.Body;
                if (string.IsNullOrEmpty(requestBody)) {
                    cp.Response.SetStatus(Controllers.WebServerController.httpResponseStatus.BadRequest);
                    return errorResponse("invalid_client_metadata", "Request body is required.");
                }
                //
                var request = cp.JSON.Deserialize<RegistrationRequest>(requestBody);
                if (request == null) {
                    cp.Response.SetStatus(Controllers.WebServerController.httpResponseStatus.BadRequest);
                    return errorResponse("invalid_client_metadata", "Invalid JSON in request body.");
                }
                //
                // -- redirect_uris is required per RFC 7591
                if (request.redirect_uris == null || request.redirect_uris.Length == 0) {
                    cp.Response.SetStatus(Controllers.WebServerController.httpResponseStatus.BadRequest);
                    return errorResponse("invalid_client_metadata", "redirect_uris is required.");
                }
                //
                // -- generate a unique client_id
                string generatedClientId = Guid.NewGuid().ToString("N");
                //
                // -- apply defaults per RFC 7591
                string clientName = request.client_name ?? "";
                string tokenEndpointAuthMethod = request.token_endpoint_auth_method ?? "none";
                string[] grantTypes = (request.grant_types != null && request.grant_types.Length > 0)
                    ? request.grant_types
                    : new[] { "authorization_code" };
                string[] responseTypes = (request.response_types != null && request.response_types.Length > 0)
                    ? request.response_types
                    : new[] { "code" };
                //
                // -- store the registration
                var record = DbBaseModel.addEmpty<OAuthClientModel>(cp);
                record.name = $"OAuth client {generatedClientId}";
                record.clientId = generatedClientId;
                record.clientName = clientName;
                record.redirectUris = cp.JSON.Serialize(request.redirect_uris);
                record.grantTypes = string.Join(",", grantTypes);
                record.responseTypes = string.Join(",", responseTypes);
                record.tokenEndpointAuthMethod = tokenEndpointAuthMethod;
                record.save(cp);
                //
                // -- build RFC 7591 response
                string redirectUrisJson = cp.JSON.Serialize(request.redirect_uris);
                string grantTypesJson = cp.JSON.Serialize(grantTypes);
                string responseTypesJson = cp.JSON.Serialize(responseTypes);
                //
                cp.Response.SetStatus(Controllers.WebServerController.httpResponseStatus.Created);
                return $@"{{
  ""client_id"": ""{escapeJson(generatedClientId)}"",
  ""client_name"": ""{escapeJson(clientName)}"",
  ""redirect_uris"": {redirectUrisJson},
  ""grant_types"": {grantTypesJson},
  ""response_types"": {responseTypesJson},
  ""token_endpoint_auth_method"": ""{escapeJson(tokenEndpointAuthMethod)}""
}}";
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                cp.Response.SetStatus(Controllers.WebServerController.httpResponseStatus.InternalServerError);
                return errorResponse("server_error", ex.Message);
            }
        }
        //
        private static string errorResponse(string error, string description) {
            return $@"{{""error"":""{escapeJson(error)}"",""error_description"":""{escapeJson(description)}""}}";
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
