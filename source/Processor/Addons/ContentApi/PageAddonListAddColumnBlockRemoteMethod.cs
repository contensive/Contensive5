
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Models;
using System;
using System.Collections.Generic;
using System.Linq;
//
namespace Contensive.Processor.Addons.ContentApi {
    //
    public class PageAddonListAddColumnBlockRemoteMethod : AddonBaseClass {
        //
        private class AddColumnBlockRequest {
            public int pageId { get; set; }
            public string designBlockTypeGuid { get; set; }
            public List<int> columnWidths { get; set; }
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
                var request = cp.JSON.Deserialize<AddColumnBlockRequest>(requestBody);
                if (request == null || request.pageId <= 0 || string.IsNullOrEmpty(request.designBlockTypeGuid)) {
                    return ContentApiHelper.errorResponse(cp, "pageId and designBlockTypeGuid are required.");
                }
                if (request.columnWidths == null || request.columnWidths.Count == 0) {
                    return ContentApiHelper.errorResponse(cp, "columnWidths are required.");
                }
                if (request.columnWidths.Sum() != 12) {
                    return ContentApiHelper.errorResponse(cp, "columnWidths must total 12.");
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
                var newItem = new AddonListItemModel {
                    designBlockTypeGuid = request.designBlockTypeGuid,
                    instanceGuid = cp.Utils.CreateGuid(),
                    columns = request.columnWidths.Select(w => new AddonListColumnItemModel {
                        col = w,
                        addonList = new List<AddonListItemModel>()
                    }).ToList()
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
                return ContentApiHelper.successResponse(cp, new { instanceGuid = newItem.instanceGuid }, "Column block added.");
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return ContentApiHelper.errorResponse(cp, $"Error: {ex.Message}");
            }
        }
    }
}
