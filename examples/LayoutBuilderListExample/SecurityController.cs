using Contensive.BaseClasses;

namespace Contensive.AccountBilling.Controllers {
    /// <summary>
    /// Controller for handling security and authorization
    /// </summary>
    public static class SecurityController {
        /// <summary>
        /// Returns an HTML response indicating the user is not authorized
        /// </summary>
        /// <param name="cp">The CPBaseClass instance</param>
        /// <returns>HTML string with not authorized message</returns>
        public static string getNotAuthorizedHtmlResponse(CPBaseClass cp) {
            return "<h1>Not Authorized</h1><p>You do not have permission to access this resource.</p>";
        }
    }
}
