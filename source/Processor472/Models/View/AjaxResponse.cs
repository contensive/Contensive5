//

using Contensive.BaseClasses;

namespace Contensive.Processor.Models.View {
    /// <summary>
    /// 
    /// </summary>
    public class AjaxResponse {
        public string errorMessage { get; set; }
        public object data { get; set; }
        //
        /// <summary>
        /// General server error
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public static AjaxResponse getResponseServerError(CPBaseClass cp) {
            cp.Response.SetStatus("500 Server Error");
            return new AjaxResponse {
                errorMessage = "Server Error"
            };
        }
        //
        /// <summary>
        /// Invalid argument
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public static AjaxResponse getResponseArgumentInvalid(CPBaseClass cp) {
            return getResponseArgumentInvalid(cp, "The request was not valid.");
        }
        //
        public static AjaxResponse getResponseArgumentInvalid(CPBaseClass cp, string errorMessage) {
            cp.Response.SetStatus("400 Bad Request");
            return new AjaxResponse {
                errorMessage = errorMessage
            };
        }
        //
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static AjaxResponse getResponse(CPBaseClass cp, object data) {
            cp.Response.SetStatus("200 OK");
            return new AjaxResponse {
                data = data
            };
        }
        //
        /// <summary>
        /// not authenticated
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public static AjaxResponse getResponseNotAuthenticated(CPBaseClass cp) {
            return getResponseNotAuthenticated(cp, "You must login to perform this action.");
        }
        //
        /// <summary>
        /// not authenticated
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public static AjaxResponse getResponseNotAuthenticated(CPBaseClass cp, string errorMessage) {
            cp.Response.SetStatus("401 Unauthorized");
            return new AjaxResponse {
                errorMessage = errorMessage
            };
        }
        //
        /// <summary>
        /// not authorized, standard message
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public static AjaxResponse getResponseNotAuthorized(CPBaseClass cp) {
            return getResponseNotAuthorized(cp, "You do not have permission to perform this action.");
        }
        //
        /// <summary>
        /// not authorized, set the message
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        public static AjaxResponse getResponseNotAuthorized(CPBaseClass cp, string errorMessage) {
            cp.Response.SetStatus("403 Forbidden");
            return new AjaxResponse {
                errorMessage = errorMessage
            };
        }
    }
}
