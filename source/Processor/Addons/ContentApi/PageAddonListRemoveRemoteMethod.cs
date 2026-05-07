
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Models;
using System;
using System.Collections.Generic;
//
namespace Contensive.Processor.Addons.ContentApi {
    //
    public class PageAddonListRemoveRemoteMethod : AddonBaseClass {
        //
        private class RemoveRequest {
            public int pageId { get; set; }
            public string instanceGuid { get; set; }
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
                var request = cp.JSON.Deserialize<RemoveRequest>(requestBody);
                if (request == null || request.pageId <= 0 || string.IsNullOrEmpty(request.instanceGuid)) {
                    return ContentApiHelper.errorResponse(cp, "pageId and instanceGuid are required.");
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
                bool removed = AddonListItemModel.deleteInstance(cp, addonList, request.instanceGuid);
                if (!removed) {
                    return ContentApiHelper.errorResponse(cp, "Widget instance not found on this page.");
                }
                //
                page.addonList = cp.JSON.Serialize(addonList);
                page.save(cp);
                //
                return ContentApiHelper.successResponse(cp, null, "Widget removed.");
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return ContentApiHelper.errorResponse(cp, $"Error: {ex.Message}");
            }
        }
    }
}
