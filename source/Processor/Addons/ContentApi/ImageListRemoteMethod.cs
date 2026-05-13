
using Contensive.BaseClasses;
using Contensive.Models.Db;
using System;
using System.Linq;
//
namespace Contensive.Processor.Addons.ContentApi {
    //
    public class ImageListRemoteMethod : AddonBaseClass {
        //
        public override object Execute(CPBaseClass cp) {
            try {
                if (!ContentApiAuth.requireAdmin(cp)) {
                    return ContentApiHelper.errorResponse(cp, "Admin access required.");
                }
                //
                int folderId = cp.Doc.GetInteger("folderId");
                int pageSize = cp.Doc.GetInteger("pageSize");
                int pageNumber = cp.Doc.GetInteger("pageNumber");
                if (pageSize <= 0) { pageSize = 50; }
                if (pageNumber <= 0) { pageNumber = 1; }
                //
                string criteria = "(active<>0)";
                if (folderId > 0) {
                    criteria += $"and(folderId={folderId})";
                }
                //
                var files = DbBaseModel.createList<LibraryFilesModel>(cp, criteria, "name", pageSize, pageNumber);
                var result = files.Select(f => new {
                    id = f.id,
                    name = f.name,
                    filename = f.filename,
                    altText = f.altText,
                    width = f.width,
                    height = f.height,
                    fileSize = f.fileSize,
                    folderId = f.folderId
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
