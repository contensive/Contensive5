
using Contensive.BaseClasses;
using Contensive.Models.Db;
using System;
using System.Linq;
//
namespace Contensive.Processor.Addons.ContentApi {
    //
    public class WidgetTypesRemoteMethod : AddonBaseClass {
        //
        public override object Execute(CPBaseClass cp) {
            try {
                if (!ContentApiAuth.requireAdmin(cp)) {
                    return ContentApiHelper.errorResponse(cp, "Admin access required.");
                }
                //
                var addons = DbBaseModel.createList<AddonModel>(cp, "(content<>0)and(active<>0)", "name");
                var result = addons.Select(a => new {
                    guid = a.ccguid,
                    name = a.name,
                    description = a.help ?? "",
                    hasInstanceContent = (a.instanceSettingPrimaryContentId ?? 0) > 0
                }).ToList();
                //
                return ContentApiHelper.successResponse(cp, result);
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return ContentApiHelper.errorResponse(cp, $"Error: {ex.Message}");
            }
        }
    }
}
