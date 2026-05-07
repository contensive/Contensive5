
using System;
//
namespace Contensive.Models.Db {
    /// <summary>
    /// Stores short-lived OAuth 2.0 authorization codes used in the PKCE authorization code flow.
    /// Each record is single-use and expires after a few minutes.
    /// </summary>
    public class OAuthCodeModel : DbBaseModel {
        //
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("OAuth Authorization Codes", "ccOAuthCodes", "default", false);
        //
        /// <summary>The authorization code value returned to the client.</summary>
        public string code { get; set; }
        /// <summary>The authenticated user who authorized the request.</summary>
        public int userId { get; set; }
        /// <summary>The PKCE code challenge (BASE64URL(SHA256(code_verifier))).</summary>
        public string codeChallenge { get; set; }
        /// <summary>Always "S256" — plain is not supported.</summary>
        public string codeChallengeMethod { get; set; }
        /// <summary>The redirect URI provided in the authorization request. Must match exactly on token exchange.</summary>
        public string redirectUri { get; set; }
        /// <summary>The client_id provided in the authorization request.</summary>
        public string clientId { get; set; }
        /// <summary>When this code expires (5 minutes after creation).</summary>
        public DateTime expiresAt { get; set; }
    }
}
