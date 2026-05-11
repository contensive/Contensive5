
using Contensive.BaseClasses;
using System;
using System.Collections.Generic;
using System.Data;
//
namespace Contensive.Processor.Addons.ContentApi {
    //
    public class ResourceListRemoteMethod : AddonBaseClass {
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
                string fileTypeFilter = cp.Doc.GetText("fileType").Trim().ToLowerInvariant();
                if (pageSize <= 0) { pageSize = 50; }
                if (pageNumber <= 0) { pageNumber = 1; }
                int offset = (pageNumber - 1) * pageSize;
                //
                // -- build WHERE clause
                var conditions = new List<string> { "(f.active<>0 OR f.active IS NULL)" };
                if (folderId > 0) { conditions.Add($"f.folderId={folderId}"); }
                switch (fileTypeFilter) {
                    case "image": conditions.Add("t.isImage=1"); break;
                    case "download": conditions.Add("t.isDownload=1"); break;
                    case "video": conditions.Add("t.isVideo=1"); break;
                }
                string where = string.Join(" AND ", conditions);
                //
                string sql = $@"
                    SELECT f.id, f.name, f.filename, f.altText, f.width, f.height, f.fileSize, f.folderId,
                           f.fileTypeId, f.description,
                           t.name AS fileTypeName,
                           ISNULL(t.isImage,0) AS isImage,
                           ISNULL(t.isDownload,0) AS isDownload,
                           ISNULL(t.isVideo,0) AS isVideo
                    FROM cclibraryfiles f
                    LEFT JOIN ccLibraryFileTypes t ON t.id=f.fileTypeId
                    WHERE {where}
                    ORDER BY f.name
                    OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";
                //
                var result = new List<object>();
                using (DataTable dt = cp.Db.ExecuteQuery(sql)) {
                    if (dt?.Rows != null) {
                        foreach (DataRow row in dt.Rows) {
                            result.Add(new {
                                id = cp.Utils.EncodeInteger(row["id"]),
                                name = row["name"]?.ToString() ?? "",
                                filename = row["filename"]?.ToString() ?? "",
                                altText = row["altText"]?.ToString() ?? "",
                                description = row["description"]?.ToString() ?? "",
                                width = cp.Utils.EncodeInteger(row["width"]),
                                height = cp.Utils.EncodeInteger(row["height"]),
                                fileSize = cp.Utils.EncodeInteger(row["fileSize"]),
                                folderId = cp.Utils.EncodeInteger(row["folderId"]),
                                fileTypeId = cp.Utils.EncodeInteger(row["fileTypeId"]),
                                fileTypeName = row["fileTypeName"]?.ToString() ?? "",
                                isImage = row["isImage"]?.ToString() == "1" || row["isImage"]?.ToString()?.ToLower() == "true",
                                isDownload = row["isDownload"]?.ToString() == "1" || row["isDownload"]?.ToString()?.ToLower() == "true",
                                isVideo = row["isVideo"]?.ToString() == "1" || row["isVideo"]?.ToString()?.ToLower() == "true"
                            });
                        }
                    }
                }
                //
                return ContentApiHelper.successResponse(cp, result);
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return ContentApiHelper.errorResponse(cp, $"Error: {ex.Message}");
            }
        }
    }
}
