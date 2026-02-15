namespace Contensive.AccountBilling {
    /// <summary>
    /// Application constants used throughout the Account Billing addon
    /// </summary>
    public static class Constants {
        // ====================================================================================================
        // -- Request name constants (form field names)
        // ====================================================================================================

        /// <summary>Request name for button field</summary>
        public const string rnButton = "button";

        /// <summary>Request name for account ID</summary>
        public const string rnAccountId = "accountId";

        /// <summary>Request name for source form ID</summary>
        public const string rnSrcFormId = "srcFormId";

        // ====================================================================================================
        // -- Form ID constants
        // ====================================================================================================

        /// <summary>Form ID for the user list form</summary>
        public const int formIdUserList = 1001;

        // ====================================================================================================
        // -- Button constants
        // ====================================================================================================

        /// <summary>Cancel button value</summary>
        public const string buttonCancel = "Cancel";

        // ====================================================================================================
        // -- GUID constants
        // ====================================================================================================

        /// <summary>GUID for the user list addon</summary>
        public const string guidAddonUserList = "{12345678-1234-1234-1234-123456789ABC}";

        /// <summary>GUID for the portal account manager</summary>
        public const string guidPortalAccountManager = "{87654321-4321-4321-4321-CBA987654321}";
    }
}
