
using Contensive.BaseClasses;
//
namespace Contensive.Processor.Addons.ContentApi {
    //
    public static class ContentApiHelper {
        //
        public static string successResponse(CPBaseClass cp, object data, string message = "OK") {
            return cp.JSON.Serialize(new ContentApiResponse {
                success = true,
                message = message,
                data = data
            });
        }
        //
        public static string errorResponse(CPBaseClass cp, string message) {
            return cp.JSON.Serialize(new ContentApiResponse {
                success = false,
                message = message,
                data = null
            });
        }
    }
}
