
using Contensive.BaseClasses;
using System;
using System.Data;

namespace Contensive.Models.Db {
    /// <summary>
    /// Logs events related to user actvity
    /// </summary>
    public class AuthenticationTokensModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Authentication Tokens", "ccAuthenticationTokens", "default", false);
        // 
        // ====================================================================================================
        // -- instance properties
        // 
    }
}
