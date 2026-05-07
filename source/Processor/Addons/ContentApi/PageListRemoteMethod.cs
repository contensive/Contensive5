
using Contensive.BaseClasses;
using Contensive.Models.Db;
using System;
using System.Collections.Generic;
using System.Linq;
//
namespace Contensive.Processor.Addons.ContentApi {
    //
    public class PageListRemoteMethod : AddonBaseClass {
        //
        public override object Execute(CPBaseClass cp) {
            try {
                if (!ContentApiAuth.requireAdmin(cp)) {
                    return ContentApiHelper.errorResponse(cp, "Admin access required.");
                }
                //
                int parentId = cp.Doc.GetInteger("parentId");
                int pageSize = cp.Doc.GetInteger("pageSize");
                int pageNumber = cp.Doc.GetInteger("pageNumber");
                if (pageSize <= 0) { pageSize = 100; }
                if (pageNumber <= 0) { pageNumber = 1; }
                //
                string criteria = "(active<>0)";
                if (parentId > 0) {
                    criteria += $"and(parentId={parentId})";
                }
                //
                var pages = DbBaseModel.createList<PageContentModel>(cp, criteria, "name", pageSize, pageNumber);
                var result = pages.Select(p => new {
                    id = p.id,
                    name = p.name,
                    guid = p.ccguid,
                    parentId = p.parentId,
                    linkAlias = p.linkAlias,
                    headline = p.headline,
                    active = p.active,
                    templateId = p.templateId
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
