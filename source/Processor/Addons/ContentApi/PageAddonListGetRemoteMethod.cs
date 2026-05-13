
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Models;
using System;
using System.Collections.Generic;
using System.Linq;
//
namespace Contensive.Processor.Addons.ContentApi {
    //
    public class PageAddonListGetRemoteMethod : AddonBaseClass {
        //
        public override object Execute(CPBaseClass cp) {
            try {
                if (!ContentApiAuth.requireAdmin(cp)) {
                    return ContentApiHelper.errorResponse(cp, "Admin access required.");
                }
                //
                int pageId = cp.Doc.GetInteger("pageId");
                if (pageId <= 0) {
                    return ContentApiHelper.errorResponse(cp, "A valid pageId is required.");
                }
                //
                var page = DbBaseModel.create<PageContentModel>(cp, pageId);
                if (page == null) {
                    return ContentApiHelper.errorResponse(cp, "Page not found.");
                }
                //
                var addonList = string.IsNullOrEmpty(page.addonList)
                    ? new List<AddonListItemModel>()
                    : cp.JSON.Deserialize<List<AddonListItemModel>>(page.addonList);
                //
                var result = stripRenderFields(addonList);
                return ContentApiHelper.successResponse(cp, result);
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return ContentApiHelper.errorResponse(cp, $"Error: {ex.Message}");
            }
        }
        //
        private static List<object> stripRenderFields(List<AddonListItemModel> addonList) {
            if (addonList == null) { return new List<object>(); }
            return addonList.Select(a => (object)new {
                designBlockTypeGuid = a.designBlockTypeGuid,
                designBlockTypeName = a.designBlockTypeName,
                instanceGuid = a.instanceGuid,
                columns = a.columns?.Select(c => new {
                    col = c.col,
                    addonList = stripRenderFields(c.addonList)
                }).ToList()
            }).ToList();
        }
    }
}
