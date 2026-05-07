
using Contensive.BaseClasses;
using System;
//
namespace Contensive.Processor.Addons.OAuth {
    //
    /// <summary>
    /// Serves OAuth 2.0 Authorization Server Metadata (RFC 8414) at /.well-known/oauth-authorization-server.
    /// Used by MCP clients to discover authorization and token endpoints.
    /// </summary>
    public class OAuthMetadataRemoteMethod : AddonBaseClass {
        //
        public override object Execute(CPBaseClass cp) {
            try {
                string baseUrl = $"{cp.Request.Protocol}{cp.Request.Host}";
                cp.Response.SetType("application/json");
                cp.Response.AddHeader("Cache-Control", "no-store");
                cp.Response.AddHeader("Access-Control-Allow-Origin", "*");
                return $@"{{
  ""issuer"": ""{baseUrl}"",
  ""authorization_endpoint"": ""{baseUrl}/oauth/authorize"",
  ""token_endpoint"": ""{baseUrl}/oauth/token"",
  ""registration_endpoint"": ""{baseUrl}/oauth/register"",
  ""response_types_supported"": [""code""],
  ""code_challenge_methods_supported"": [""S256""],
  ""grant_types_supported"": [""authorization_code""],
  ""token_endpoint_auth_methods_supported"": [""none""]
}}";
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return "{}";
            }
        }
    }
}
