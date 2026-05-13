
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
//
namespace Contensive.Processor.Addons.ContentApi {
    //
    public class PageGetRemoteMethod : AddonBaseClass {
        //
        public override object Execute(CPBaseClass cp) {
            try {
                if (!ContentApiAuth.requireAdmin(cp)) {
                    return ContentApiHelper.errorResponse(cp, "Admin access required.");
                }
                //
                string url = cp.Doc.GetText("url");
                if (string.IsNullOrEmpty(url)) {
                    return ContentApiHelper.errorResponse(cp, "url parameter is required.");
                }
                //
                var context = UrlResolverHelper.resolve(cp, url);
                if (context == null) {
                    return ContentApiHelper.errorResponse(cp, $"No page found for url '{url}'.");
                }
                //
                var page = context.page;
                //
                // -- resolve template name (use page's templateId; walk parent chain if not set)
                string templateName = "";
                int resolvedTemplateId = resolveTemplateId(cp, page);
                if (resolvedTemplateId > 0) {
                    var template = DbBaseModel.create<PageTemplateModel>(cp, resolvedTemplateId);
                    templateName = template?.name ?? "";
                }
                //
                // -- deserialize addon list and load widget content for each widget
                var addonList = string.IsNullOrEmpty(page.addonList)
                    ? new List<AddonListItemModel>()
                    : cp.JSON.Deserialize<List<AddonListItemModel>>(page.addonList);
                //
                // -- load all link aliases for this page + queryStringSuffix combination
                string qsSql = string.IsNullOrEmpty(context.queryStringSuffix)
                    ? "(queryStringSuffix IS NULL OR queryStringSuffix='')"
                    : $"queryStringSuffix={cp.Db.EncodeSQLText(context.queryStringSuffix)}";
                var linkAliases = new List<object>();
                int canonicalAliasId = 0;
                using (DataTable dtAliases = cp.Db.ExecuteQuery(
                    $"SELECT id, name FROM cclinkaliases WHERE pageId={context.pageId} AND {qsSql} AND (active<>0 OR active IS NULL) ORDER BY id DESC")) {
                    if (dtAliases?.Rows != null) {
                        bool first = true;
                        foreach (DataRow aliasRow in dtAliases.Rows) {
                            int aliasId = cp.Utils.EncodeInteger(aliasRow["id"]);
                            if (first) { canonicalAliasId = aliasId; first = false; }
                            linkAliases.Add(new {
                                id = aliasId,
                                url = aliasRow["name"]?.ToString() ?? "",
                                isCanonical = (aliasId == canonicalAliasId)
                            });
                        }
                    }
                }
                //
                var result = new {
                    url = context.linkAliasName,
                    pageId = context.pageId,
                    queryStringSuffix = context.queryStringSuffix,
                    page = new {
                        name = page.name ?? "",
                        headline = page.headline ?? "",
                        navHeadline = page.menuHeadline ?? page.name ?? "",
                        metaDescription = page.metaDescription ?? "",
                        metaKeywordList = page.metaKeywordList ?? "",
                        structuredData = page.structuredData ?? "",
                        pageTitle = page.pageTitle ?? "",
                        parentId = page.parentId,
                        templateId = resolvedTemplateId,
                        templateName = templateName
                    },
                    linkAliases,
                    widgets = loadWidgets(cp, addonList)
                };
                //
                return ContentApiHelper.successResponse(cp, result);
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return ContentApiHelper.errorResponse(cp, $"Error: {ex.Message}");
            }
        }
        //
        /// <summary>
        /// Resolves the effective templateId for a page by walking the parentId chain.
        /// Returns the first non-zero templateId found.
        /// </summary>
        private static int resolveTemplateId(CPBaseClass cp, PageContentModel page) {
            if (page == null) { return 0; }
            if (page.templateId > 0) { return page.templateId; }
            if (page.parentId <= 0) { return 0; }
            // -- walk up to parent (limit depth to avoid infinite loops)
            int depth = 0;
            int parentId = page.parentId;
            while (parentId > 0 && depth < 10) {
                var parent = DbBaseModel.create<PageContentModel>(cp, parentId);
                if (parent == null) { break; }
                if (parent.templateId > 0) { return parent.templateId; }
                parentId = parent.parentId;
                depth++;
            }
            return 0;
        }
        //
        /// <summary>
        /// Loads widget content for a list of addon list items, recursing into columns.
        /// </summary>
        private static List<object> loadWidgets(CPBaseClass cp, List<AddonListItemModel> addonList) {
            if (addonList == null) { return new List<object>(); }
            return addonList.Select((item, position) => loadWidgetData(cp, item, position)).ToList();
        }
        //
        /// <summary>
        /// Loads content fields for a single widget. Recurses into columns for structural blocks.
        /// </summary>
        private static object loadWidgetData(CPBaseClass cp, AddonListItemModel item, int position) {
            bool hasInstanceContent = false;
            bool contentExists = false;
            string contentName = "";
            var fields = new Dictionary<string, string>();
            //
            // -- recurse into column blocks
            var columns = new List<object>();
            if (item.columns != null && item.columns.Count > 0) {
                foreach (var col in item.columns) {
                    columns.Add(new {
                        col = col.col,
                        widgets = loadWidgets(cp, col.addonList)
                    });
                }
            }
            //
            // -- load the addon definition to check for instance content
            if (!string.IsNullOrEmpty(item.designBlockTypeGuid)) {
                var addon = DbBaseModel.create<AddonModel>(cp, item.designBlockTypeGuid);
                if (addon != null) {
                    int contentId = addon.instanceSettingPrimaryContentId ?? 0;
                    if (contentId > 0) {
                        hasInstanceContent = true;
                        contentName = cp.Content.GetRecordName("content", contentId);
                        if (!string.IsNullOrEmpty(contentName) && !string.IsNullOrEmpty(item.instanceGuid)) {
                            string tableName = cp.Content.GetTable(contentName);
                            using (DataTable dt = cp.Db.ExecuteQuery(
                                $"SELECT TOP 1 * FROM {tableName} WHERE ccguid={cp.Db.EncodeSQLText(item.instanceGuid)}")) {
                                if (dt?.Rows != null && dt.Rows.Count > 0) {
                                    contentExists = true;
                                    foreach (DataColumn col in dt.Columns) {
                                        string fieldName = col.ColumnName.ToLowerInvariant();
                                        // -- skip internal system fields
                                        if (fieldName == "id" || fieldName == "ccguid"
                                            || fieldName == "contentcontrolid" || fieldName == "createdby"
                                            || fieldName == "modifiedby" || fieldName == "dateadded"
                                            || fieldName == "modifieddate" || fieldName == "active") {
                                            continue;
                                        }
                                        fields[col.ColumnName] = dt.Rows[0][col.ColumnName]?.ToString() ?? "";
                                    }
                                }
                            }
                        }
                    }
                }
            }
            //
            return new {
                instanceGuid = item.instanceGuid ?? "",
                designBlockTypeGuid = item.designBlockTypeGuid ?? "",
                designBlockTypeName = item.designBlockTypeName ?? "",
                position = position,
                hasInstanceContent = hasInstanceContent,
                contentExists = contentExists,
                contentName = contentName,
                fields = fields,
                columns = columns
            };
        }
    }
}
