
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Models;
using System;
using System.Collections.Generic;
//
namespace Contensive.Processor.Addons.ContentApi {
    //
    public class WidgetInstanceUpdateRemoteMethod : AddonBaseClass {
        //
        private class UpdateRequest {
            public string url { get; set; }
            public string instanceGuid { get; set; }
            public Dictionary<string, string> fields { get; set; }
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
                var request = cp.JSON.Deserialize<UpdateRequest>(requestBody);
                if (request == null || string.IsNullOrEmpty(request.url) || string.IsNullOrEmpty(request.instanceGuid)) {
                    return ContentApiHelper.errorResponse(cp, "url and instanceGuid are required.");
                }
                if (request.fields == null || request.fields.Count == 0) {
                    return ContentApiHelper.errorResponse(cp, "fields dictionary is required.");
                }
                //
                // -- resolve url to get page context; queryStringSuffix is available here for
                // -- future dynamic-widget support (Phase 5 of the MCP plan)
                var context = UrlResolverHelper.resolve(cp, request.url);
                if (context == null) {
                    return ContentApiHelper.errorResponse(cp, $"No page found for url '{request.url}'.");
                }
                //
                // -- find the widget in the page's addonList by instanceGuid to get its designBlockTypeGuid
                if (string.IsNullOrEmpty(context.page.addonList)) {
                    return ContentApiHelper.errorResponse(cp, "This page has no widgets.");
                }
                var addonList = cp.JSON.Deserialize<List<AddonListItemModel>>(context.page.addonList);
                var widget = findWidget(addonList, request.instanceGuid);
                if (widget == null) {
                    return ContentApiHelper.errorResponse(cp, $"Widget '{request.instanceGuid}' not found on page '{request.url}'.");
                }
                //
                // -- look up the addon to find its instance content type
                var addon = DbBaseModel.create<AddonModel>(cp, widget.designBlockTypeGuid);
                if (addon == null) {
                    return ContentApiHelper.errorResponse(cp, "Widget type not found.");
                }
                int contentId = addon.instanceSettingPrimaryContentId ?? 0;
                if (contentId == 0) {
                    return ContentApiHelper.errorResponse(cp, "This widget type has no editable instance content.");
                }
                //
                string contentName = cp.Content.GetRecordName("content", contentId);
                if (string.IsNullOrEmpty(contentName)) {
                    return ContentApiHelper.errorResponse(cp, "Content definition not found.");
                }
                //
                // -- open the instance record, creating it if it doesn't exist yet
                using (CPCSBaseClass cs = cp.CSNew()) {
                    if (!cs.Open(contentName, $"ccguid={cp.Db.EncodeSQLText(request.instanceGuid)}", "id", true, "", 1)) {
                        if (!cs.Insert(contentName)) {
                            return ContentApiHelper.errorResponse(cp, "Failed to create instance record.");
                        }
                        cs.SetField("ccguid", request.instanceGuid);
                        cs.SetField("name", $"Instance {request.instanceGuid}");
                    }
                    //
                    foreach (var kvp in request.fields) {
                        cs.SetField(kvp.Key, kvp.Value);
                    }
                    cs.Save();
                }
                //
                return ContentApiHelper.successResponse(cp, new { updated = true }, "Widget instance updated.");
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return ContentApiHelper.errorResponse(cp, $"Error: {ex.Message}");
            }
        }
        //
        /// <summary>
        /// Recursively searches an addonList and its column children for a widget by instanceGuid.
        /// </summary>
        private static AddonListItemModel findWidget(List<AddonListItemModel> addonList, string instanceGuid) {
            if (addonList == null) { return null; }
            foreach (var item in addonList) {
                if (string.Equals(item.instanceGuid, instanceGuid, StringComparison.OrdinalIgnoreCase)) { return item; }
                if (item.columns != null) {
                    foreach (var col in item.columns) {
                        var found = findWidget(col.addonList, instanceGuid);
                        if (found != null) { return found; }
                    }
                }
            }
            return null;
        }
    }
}
