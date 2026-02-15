using Contensive.BaseClasses;

namespace Views.AccountManager {
    /// <summary>
    /// Addon for editing account details
    /// </summary>
    public class AccountEditAddon : AddonBaseClass {
        /// <summary>
        /// GUID for the account edit portal feature
        /// </summary>
        public const string guidPortalFeature = "{AAAABBBB-CCCC-DDDD-EEEE-FFFFFFFFFFFF}";

        /// <summary>
        /// Execute method for the account edit addon
        /// </summary>
        /// <param name="cp">The CPBaseClass instance</param>
        /// <returns>HTML content for editing an account</returns>
        public override object Execute(CPBaseClass cp) {
            return "";
        }
    }
}
