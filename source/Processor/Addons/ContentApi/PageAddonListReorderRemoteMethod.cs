
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Models;
using System;
using System.Collections.Generic;
using System.Linq;
//
namespace Contensive.Processor.Addons.ContentApi {
    //
    public class PageAddonListReorderRemoteMethod : AddonBaseClass {
        //
        private class ReorderRequest {
            public string url { get; set; }
            public List<string> instanceGuids { get; set; }
        }
        //
        public override object Execute(CPBaseClass cp) {
            try {
                if (!ContentApiAuth.requireAdmin(cp)) {
                    return ContentApiHelper.errorResponse(cp, "Admin access required.");
                }
                //
                string requestBody = cp.Request.Body;
                if (string.IsNullOrEmpty(requestBody)) {
                    return ContentApiHelper.errorResponse(cp, "Request body is required.");
                }
                var request = cp.JSON.Deserialize<ReorderRequest>(requestBody);
                if (request == null || string.IsNullOrEmpty(request.url) || request.instanceGuids == null || request.instanceGuids.Count == 0) {
                    return ContentApiHelper.errorResponse(cp, "url and instanceGuids are required.");
                }
                //
                var context = UrlResolverHelper.resolve(cp, request.url);
                if (context == null) {
                    return ContentApiHelper.errorResponse(cp, $"No page found for url '{request.url}'.");
                }
                var page = context.page;
                //
                var addonList = string.IsNullOrEmpty(page.addonList)
                    ? new List<AddonListItemModel>()
                    : cp.JSON.Deserialize<List<AddonListItemModel>>(page.addonList);
                //
                // -- reorder to match provided guid sequence; append any unlisted items at the end
                var reordered = new List<AddonListItemModel>();
                foreach (string guid in request.instanceGuids) {
                    var item = addonList.FirstOrDefault(a => string.Equals(a.instanceGuid, guid, StringComparison.OrdinalIgnoreCase));
                    if (item != null) { reordered.Add(item); }
                }
                foreach (var item in addonList) {
                    if (!reordered.Contains(item)) { reordered.Add(item); }
                }
                //
                AddonListItemModel.normalizeAddonList(cp, reordered);
                page.addonList = cp.JSON.Serialize(reordered);
                page.save(cp);
                //
                return ContentApiHelper.successResponse(cp, null, "Widgets reordered.");
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return ContentApiHelper.errorResponse(cp, $"Error: {ex.Message}");
            }
        }
    }
}
