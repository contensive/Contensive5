
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using System;
using System.Collections.Generic;
using System.Data;
//
namespace Contensive.Processor.Addons.ContentApi {
    //
    public class WidgetInstanceGetRemoteMethod : AddonBaseClass {
        //
        public override object Execute(CPBaseClass cp) {
            try {
                if (!ContentApiAuth.requireAdmin(cp)) {
                    return ContentApiHelper.errorResponse(cp, "Admin access required.");
                }
                //
                string designBlockTypeGuid = cp.Doc.GetText("designBlockTypeGuid");
                string instanceGuid = cp.Doc.GetText("instanceGuid");
                //
                if (string.IsNullOrEmpty(designBlockTypeGuid) || string.IsNullOrEmpty(instanceGuid)) {
                    return ContentApiHelper.errorResponse(cp, "designBlockTypeGuid and instanceGuid are required.");
                }
                //
                // -- look up the addon to find its instance content type
                var addon = DbBaseModel.create<AddonModel>(cp, designBlockTypeGuid);
                if (addon == null) {
                    return ContentApiHelper.errorResponse(cp, "Widget type not found.");
                }
                int contentId = addon.instanceSettingPrimaryContentId ?? 0;
                if (contentId == 0) {
                    return ContentApiHelper.errorResponse(cp, "This widget type has no instance content.");
                }
                //
                // -- resolve content name from content ID
                string contentName = cp.Content.GetRecordName("content", contentId);
                if (string.IsNullOrEmpty(contentName)) {
                    return ContentApiHelper.errorResponse(cp, "Content definition not found.");
                }
                //
                // -- open the instance record by ccguid
                var fields = new Dictionary<string, string>();
                using (CPCSBaseClass cs = cp.CSNew()) {
                    if (cs.Open(contentName, $"ccguid={cp.Db.EncodeSQLText(instanceGuid)}", "id", true, "", 1)) {
                        // -- read all fields from the record using a SQL query to get column names
                        string tableName = cp.Content.GetTable(contentName);
                        using (DataTable dt = cp.Db.ExecuteQuery($"select top 1 * from {tableName} where ccguid={cp.Db.EncodeSQLText(instanceGuid)}")) {
                            if (dt?.Rows != null && dt.Rows.Count > 0) {
                                foreach (DataColumn col in dt.Columns) {
                                    string fieldName = col.ColumnName.ToLowerInvariant();
                                    // -- skip system fields
                                    if (fieldName == "id" || fieldName == "ccguid" || fieldName == "contentcontrolid" || fieldName == "createdby" || fieldName == "modifiedby" || fieldName == "dateadded" || fieldName == "modifieddate") { continue; }
                                    fields[col.ColumnName] = dt.Rows[0][col.ColumnName]?.ToString() ?? "";
                                }
                            }
                        }
                    } else {
                        return ContentApiHelper.successResponse(cp, new { exists = false, contentName = contentName, fields = new Dictionary<string, string>() }, "Instance record not yet created. It will be created on first render.");
                    }
                }
                //
                return ContentApiHelper.successResponse(cp, new { exists = true, contentName = contentName, fields = fields });
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return ContentApiHelper.errorResponse(cp, $"Error: {ex.Message}");
            }
        }
    }
}
