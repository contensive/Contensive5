
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Models;
using System;
using System.Collections.Generic;
//
namespace Contensive.Processor.Addons.ContentApi {
    //
    public class PageAddonListAddRemoteMethod : AddonBaseClass {
        //
        private class AddRequest {
            public string url { get; set; }
            public string designBlockTypeGuid { get; set; }
            public int position { get; set; }
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
                var request = cp.JSON.Deserialize<AddRequest>(requestBody);
                if (request == null || string.IsNullOrEmpty(request.url) || string.IsNullOrEmpty(request.designBlockTypeGuid)) {
                    return ContentApiHelper.errorResponse(cp, "url and designBlockTypeGuid are required.");
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
                var newItem = new AddonListItemModel {
                    designBlockTypeGuid = request.designBlockTypeGuid,
                    instanceGuid = cp.Utils.CreateGuid()
                };
                //
                if (request.position > 0 && request.position <= addonList.Count) {
                    addonList.Insert(request.position - 1, newItem);
                } else {
                    addonList.Add(newItem);
                }
                //
                AddonListItemModel.normalizeAddonList(cp, addonList);
                page.addonList = cp.JSON.Serialize(addonList);
                page.save(cp);
                //
                return ContentApiHelper.successResponse(cp, new { instanceGuid = newItem.instanceGuid }, "Widget added.");
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return ContentApiHelper.errorResponse(cp, $"Error: {ex.Message}");
            }
        }
    }
}
