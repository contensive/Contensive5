
using Contensive.BaseClasses;
using Contensive.Processor.Controllers;
//
namespace Contensive.Processor.Addons.ContentApi {
    //
    public static class ContentApiAuth {
        //
        /// <summary>
        /// Require admin authentication. Returns false and sets 401 if not admin.
        /// </summary>
        public static bool requireAdmin(CPBaseClass cp) {
            if (!cp.User.IsAdmin) {
                cp.Response.SetStatus(WebServerController.httpResponseStatus.Unauthorized);
                return false;
            }
            return true;
        }
    }
}
