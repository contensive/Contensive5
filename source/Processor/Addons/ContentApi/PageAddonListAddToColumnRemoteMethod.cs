
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Models;
using System;
using System.Collections.Generic;
//
namespace Contensive.Processor.Addons.ContentApi {
    //
    public class PageAddonListAddToColumnRemoteMethod : AddonBaseClass {
        //
        private class AddToColumnRequest {
            public int pageId { get; set; }
            public string parentInstanceGuid { get; set; }
            public int columnIndex { get; set; }
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
                var request = cp.JSON.Deserialize<AddToColumnRequest>(requestBody);
                if (request == null || request.pageId <= 0 || string.IsNullOrEmpty(request.parentInstanceGuid) || string.IsNullOrEmpty(request.designBlockTypeGuid)) {
                    return ContentApiHelper.errorResponse(cp, "pageId, parentInstanceGuid, and designBlockTypeGuid are required.");
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
                // -- find the parent column block
                var parent = findByInstanceGuid(addonList, request.parentInstanceGuid);
                if (parent == null) {
                    return ContentApiHelper.errorResponse(cp, "Parent column block not found.");
                }
                if (parent.columns == null || request.columnIndex < 0 || request.columnIndex >= parent.columns.Count) {
                    return ContentApiHelper.errorResponse(cp, "Invalid columnIndex.");
                }
                //
                var column = parent.columns[request.columnIndex];
                if (column.addonList == null) {
                    column.addonList = new List<AddonListItemModel>();
                }
                //
                var newItem = new AddonListItemModel {
                    designBlockTypeGuid = request.designBlockTypeGuid,
                    instanceGuid = cp.Utils.CreateGuid()
                };
                //
                if (request.position > 0 && request.position <= column.addonList.Count) {
                    column.addonList.Insert(request.position - 1, newItem);
                } else {
                    column.addonList.Add(newItem);
                }
                //
                AddonListItemModel.normalizeAddonList(cp, addonList);
                page.addonList = cp.JSON.Serialize(addonList);
                page.save(cp);
                //
                return ContentApiHelper.successResponse(cp, new { instanceGuid = newItem.instanceGuid }, "Widget added to column.");
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return ContentApiHelper.errorResponse(cp, $"Error: {ex.Message}");
            }
        }
        //
        private static AddonListItemModel findByInstanceGuid(List<AddonListItemModel> addonList, string instanceGuid) {
            if (addonList == null) { return null; }
            foreach (var item in addonList) {
                if (string.Equals(item.instanceGuid, instanceGuid, StringComparison.OrdinalIgnoreCase)) {
                    return item;
                }
                if (item.columns != null) {
                    foreach (var column in item.columns) {
                        var found = findByInstanceGuid(column.addonList, instanceGuid);
                        if (found != null) { return found; }
                    }
                }
            }
            return null;
        }
    }
}
