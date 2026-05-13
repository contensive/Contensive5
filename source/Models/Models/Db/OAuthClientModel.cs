
namespace Contensive.Models.Db {
    /// <summary>
    /// Stores OAuth 2.0 dynamic client registrations (RFC 7591).
    /// Each record represents a client that has self-registered via POST /oauth/register.
    /// </summary>
    public class OAuthClientModel : DbBaseModel {
        //
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("OAuth Clients", "ccOAuthClients", "default", false);
        //
        /// <summary>The unique client identifier returned to the registering client.</summary>
        public string clientId { get; set; }
        /// <summary>Human-readable name of the client application.</summary>
        public string clientName { get; set; }
        /// <summary>JSON array of allowed redirect URIs.</summary>
        public string redirectUris { get; set; }
        /// <summary>Supported grant types (e.g. "authorization_code").</summary>
        public string grantTypes { get; set; }
        /// <summary>Supported response types (e.g. "code").</summary>
        public string responseTypes { get; set; }
        /// <summary>Token endpoint authentication method (e.g. "none" for public clients).</summary>
        public string tokenEndpointAuthMethod { get; set; }
    }
}
