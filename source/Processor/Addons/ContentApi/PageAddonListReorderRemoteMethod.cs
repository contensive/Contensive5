
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
            public int pageId { get; set; }
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
                if (request == null || request.pageId <= 0 || request.instanceGuids == null || request.instanceGuids.Count == 0) {
                    return ContentApiHelper.errorResponse(cp, "pageId and instanceGuids are required.");
                }
                //
                var page = DbBaseModel.create<PageContentModel>(cp, request.pageId);
                if (page == null) {
                    return ContentApiHelper.errorResponse(cp, "Page not found.");
                }
                //
                var addonList = string.IsNullOrEmpty(page.addonList)
                    ? new List<AddonListItemModel>()
                    : cp.JSON.Deserialize<List<AddonListItemModel>>(page.addonList);
                //
                // -- reorder based on provided guid sequence
                var reordered = new List<AddonListItemModel>();
                foreach (string guid in request.instanceGuids) {
                    var item = addonList.FirstOrDefault(a => string.Equals(a.instanceGuid, guid, StringComparison.OrdinalIgnoreCase));
                    if (item != null) {
                        reordered.Add(item);
                    }
                }
                // -- append any items not in the provided list (safety)
                foreach (var item in addonList) {
                    if (!reordered.Contains(item)) {
                        reordered.Add(item);
                    }
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
