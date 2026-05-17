
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
//
namespace Contensive.Addons.Mcp {
    //
    /// <summary>
    /// MCP tool method implementations. Each static method corresponds to one MCP tool.
    /// Methods return a serialized JSON result string.
    /// </summary>
    public static class McpToolMethods {
        //
        // ====================================================================================================
        // -- helper to safely extract a string argument
        // ====================================================================================================
        //
        private static string getStringArg(Dictionary<string, object> args, string key, string defaultValue = "") {
            if (args == null || !args.ContainsKey(key)) { return defaultValue; }
            return args[key]?.ToString() ?? defaultValue;
        }
        //
        private static int getIntArg(Dictionary<string, object> args, string key, int defaultValue = 0) {
            if (args == null || !args.ContainsKey(key)) { return defaultValue; }
            if (int.TryParse(args[key]?.ToString(), out int result)) { return result; }
            return defaultValue;
        }
        //
        private static bool getBoolArg(Dictionary<string, object> args, string key, bool defaultValue = false) {
            if (args == null || !args.ContainsKey(key)) { return defaultValue; }
            string val = args[key]?.ToString()?.ToLowerInvariant() ?? "";
            if (val == "true" || val == "1") { return true; }
            if (val == "false" || val == "0") { return false; }
            return defaultValue;
        }
        //
        // ====================================================================================================
        // -- response helpers
        // ====================================================================================================
        //
        private static string successResponse(CPBaseClass cp, object data, string message = "OK") {
            return cp.JSON.Serialize(new { success = true, message, data });
        }
        //
        private static string errorResponse(CPBaseClass cp, string message) {
            return cp.JSON.Serialize(new { success = false, message, data = (object)null });
        }
        //
        // ====================================================================================================
        // -- page_list
        // ====================================================================================================
        //
        private class PageNode {
            public string url { get; set; }
            public int pageId { get; set; }
            public string queryStringSuffix { get; set; }
            public string navHeadline { get; set; }
            public bool isDynamicVariant { get; set; }
            public int parentPageId { get; set; }
            public List<PageNode> children { get; set; } = new List<PageNode>();
        }
        //
        public static string pageList(CPBaseClass cp, Dictionary<string, object> args) {
            try {
                string sql = @"
                    SELECT la.name AS url, la.pageId,
                           ISNULL(la.queryStringSuffix, '') AS queryStringSuffix,
                           ISNULL(p.menuHeadline, ISNULL(p.name, '')) AS navHeadline,
                           ISNULL(p.parentId, 0) AS parentId
                    FROM cclinkaliases la
                    JOIN ccpagecontent p ON la.pageId = p.id
                    WHERE la.id IN (
                        SELECT MAX(id) FROM cclinkaliases
                        WHERE (active<>0 OR active IS NULL)
                        GROUP BY pageId, ISNULL(queryStringSuffix, '')
                    )
                    AND (p.active<>0 OR p.active IS NULL)
                    ORDER BY la.name";
                //
                var allNodes = new List<PageNode>();
                using (DataTable dt = cp.Db.ExecuteQuery(sql)) {
                    if (dt?.Rows != null) {
                        foreach (DataRow row in dt.Rows) {
                            if (!int.TryParse(row["pageId"]?.ToString(), out int pageId) || pageId <= 0) { continue; }
                            string qss = row["queryStringSuffix"]?.ToString() ?? "";
                            allNodes.Add(new PageNode {
                                url = row["url"]?.ToString() ?? "",
                                pageId = pageId,
                                queryStringSuffix = qss,
                                navHeadline = row["navHeadline"]?.ToString() ?? "",
                                isDynamicVariant = !string.IsNullOrEmpty(qss),
                                parentPageId = int.TryParse(row["parentId"]?.ToString(), out int pid) ? pid : 0
                            });
                        }
                    }
                }
                //
                var baseNodesByPageId = allNodes
                    .Where(n => !n.isDynamicVariant)
                    .ToDictionary(n => n.pageId);
                var dynamicNodes = allNodes.Where(n => n.isDynamicVariant).ToList();
                //
                foreach (var variant in dynamicNodes) {
                    if (baseNodesByPageId.TryGetValue(variant.pageId, out var baseNode)) {
                        baseNode.children.Add(variant);
                    }
                }
                //
                var roots = new List<PageNode>();
                foreach (var node in baseNodesByPageId.Values) {
                    if (node.parentPageId > 0 && baseNodesByPageId.TryGetValue(node.parentPageId, out var parentNode)) {
                        int insertAt = parentNode.children.FindIndex(c => c.isDynamicVariant);
                        if (insertAt < 0) {
                            parentNode.children.Add(node);
                        } else {
                            parentNode.children.Insert(insertAt, node);
                        }
                    } else {
                        roots.Add(node);
                    }
                }
                //
                return successResponse(cp, serializeTree(roots));
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return errorResponse(cp, $"Error: {ex.Message}");
            }
        }
        //
        private static List<object> serializeTree(List<PageNode> nodes) {
            return nodes.Select(n => (object)new {
                url = n.url,
                pageId = n.pageId,
                queryStringSuffix = n.queryStringSuffix,
                navHeadline = n.navHeadline,
                isDynamicVariant = n.isDynamicVariant,
                children = serializeTree(n.children)
            }).ToList();
        }
        //
        // ====================================================================================================
        // -- page_get
        // ====================================================================================================
        //
        public static string pageGet(CPBaseClass cp, Dictionary<string, object> args) {
            try {
                string url = getStringArg(args, "url");
                if (string.IsNullOrEmpty(url)) {
                    return errorResponse(cp, "url parameter is required.");
                }
                //
                var context = UrlResolverHelper.resolve(cp, url);
                if (context == null) {
                    return errorResponse(cp, $"No page found for url '{url}'.");
                }
                var page = context.page;
                //
                string templateName = "";
                int resolvedTemplateId = resolveTemplateId(cp, page);
                if (resolvedTemplateId > 0) {
                    var template = DbBaseModel.create<PageTemplateModel>(cp, resolvedTemplateId);
                    templateName = template?.name ?? "";
                }
                //
                var addonList = string.IsNullOrEmpty(page.addonList)
                    ? new List<AddonListItemModel>()
                    : cp.JSON.Deserialize<List<AddonListItemModel>>(page.addonList);
                //
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
                return successResponse(cp, result);
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return errorResponse(cp, $"Error: {ex.Message}");
            }
        }
        //
        private static int resolveTemplateId(CPBaseClass cp, PageContentModel page) {
            if (page == null) { return 0; }
            if (page.templateId > 0) { return page.templateId; }
            if (page.parentId <= 0) { return 0; }
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
        private static List<object> loadWidgets(CPBaseClass cp, List<AddonListItemModel> addonList) {
            if (addonList == null) { return new List<object>(); }
            return addonList.Select((item, position) => loadWidgetData(cp, item, position)).ToList();
        }
        //
        private static object loadWidgetData(CPBaseClass cp, AddonListItemModel item, int position) {
            bool hasInstanceContent = false;
            bool contentExists = false;
            string contentName = "";
            var fields = new Dictionary<string, string>();
            //
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
                position,
                hasInstanceContent,
                contentExists,
                contentName,
                fields,
                columns
            };
        }
        //
        // ====================================================================================================
        // -- page_create
        // ====================================================================================================
        //
        public static string pageCreate(CPBaseClass cp, Dictionary<string, object> args) {
            try {
                string parentUrl = getStringArg(args, "parentUrl");
                string navHeadline = getStringArg(args, "navHeadline");
                string headline = getStringArg(args, "headline");
                int templateId = getIntArg(args, "templateId");
                //
                if (string.IsNullOrEmpty(navHeadline)) {
                    return errorResponse(cp, "navHeadline is required.");
                }
                //
                int parentId = 0;
                string parentUrlBase = "";
                if (!string.IsNullOrEmpty(parentUrl)) {
                    var parentContext = UrlResolverHelper.resolve(cp, parentUrl);
                    if (parentContext == null) {
                        return errorResponse(cp, $"Parent page not found for url '{parentUrl}'.");
                    }
                    parentId = parentContext.pageId;
                    parentUrlBase = parentContext.linkAliasName;
                }
                //
                string slug = LinkAliasModel.normalizeLinkAlias(cp, navHeadline);
                string newUrl = parentUrlBase.TrimEnd('/') + slug;
                //
                var page = DbBaseModel.addDefault<PageContentModel>(cp);
                if (page == null) {
                    return errorResponse(cp, "Failed to create page record.");
                }
                //
                page.name = navHeadline;
                page.menuHeadline = navHeadline;
                page.headline = !string.IsNullOrEmpty(headline) ? headline : navHeadline;
                if (parentId > 0) { page.parentId = parentId; }
                if (templateId > 0) { page.templateId = templateId; }
                page.save(cp);
                //
                var linkAlias = DbBaseModel.addDefault<LinkAliasModel>(cp);
                if (linkAlias != null) {
                    linkAlias.name = newUrl;
                    linkAlias.pageId = page.id;
                    linkAlias.queryStringSuffix = "";
                    linkAlias.save(cp);
                }
                //
                // -- capture undo (record was created, undo will delete it)
                captureUndo(cp, "Page Content", page.id, page.ccguid ?? "", "page_create", new Dictionary<string, string>(), true);
                //
                return successResponse(cp, new { pageId = page.id, url = newUrl }, "Page created.");
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return errorResponse(cp, $"Error: {ex.Message}");
            }
        }
        //
        // ====================================================================================================
        // -- page_update
        // ====================================================================================================
        //
        public static string pageUpdate(CPBaseClass cp, Dictionary<string, object> args) {
            try {
                string url = getStringArg(args, "url");
                if (string.IsNullOrEmpty(url)) {
                    return errorResponse(cp, "url is required.");
                }
                //
                var context = UrlResolverHelper.resolve(cp, url);
                if (context == null) {
                    return errorResponse(cp, $"No page found for url '{url}'.");
                }
                var page = context.page;
                //
                // -- capture undo before modifying
                var beforeFields = new Dictionary<string, string> {
                    ["headline"] = page.headline ?? "",
                    ["menuHeadline"] = page.menuHeadline ?? "",
                    ["metaDescription"] = page.metaDescription ?? "",
                    ["metaKeywordList"] = page.metaKeywordList ?? "",
                    ["structuredData"] = page.structuredData ?? "",
                    ["pageTitle"] = page.pageTitle ?? "",
                    ["templateId"] = page.templateId.ToString(),
                    ["parentId"] = page.parentId.ToString()
                };
                captureUndo(cp, "Page Content", page.id, page.ccguid ?? "", "page_update", beforeFields);
                //
                if (args.ContainsKey("headline")) { page.headline = getStringArg(args, "headline"); }
                if (args.ContainsKey("navHeadline")) { page.menuHeadline = getStringArg(args, "navHeadline"); }
                if (args.ContainsKey("metaDescription")) { page.metaDescription = getStringArg(args, "metaDescription"); }
                if (args.ContainsKey("metaKeywordList")) { page.metaKeywordList = getStringArg(args, "metaKeywordList"); }
                if (args.ContainsKey("structuredData")) { page.structuredData = getStringArg(args, "structuredData"); }
                if (args.ContainsKey("pageTitle")) { page.pageTitle = getStringArg(args, "pageTitle"); }
                int templateId = getIntArg(args, "templateId");
                if (templateId > 0) { page.templateId = templateId; }
                int parentId = getIntArg(args, "parentId");
                if (parentId > 0) { page.parentId = parentId; }
                //
                page.save(cp);
                //
                return successResponse(cp, new { pageId = page.id, url = context.linkAliasName }, "Page updated.");
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return errorResponse(cp, $"Error: {ex.Message}");
            }
        }
        //
        // ====================================================================================================
        // -- page_alias_add
        // ====================================================================================================
        //
        public static string pageAliasAdd(CPBaseClass cp, Dictionary<string, object> args) {
            try {
                string url = getStringArg(args, "url");
                string newAlias = getStringArg(args, "newAlias");
                bool makeCanonical = getBoolArg(args, "makeCanonical", true);
                //
                if (string.IsNullOrEmpty(url)) {
                    return errorResponse(cp, "url is required.");
                }
                if (string.IsNullOrEmpty(newAlias)) {
                    return errorResponse(cp, "newAlias is required.");
                }
                //
                string normalizedAlias = newAlias.Trim();
                if (!normalizedAlias.StartsWith("/")) { normalizedAlias = $"/{normalizedAlias}"; }
                if (normalizedAlias.Length > 1 && normalizedAlias.EndsWith("/")) {
                    normalizedAlias = normalizedAlias.TrimEnd('/');
                }
                //
                var context = UrlResolverHelper.resolve(cp, url);
                if (context == null) {
                    return errorResponse(cp, $"No page found for url '{url}'.");
                }
                //
                using (DataTable dtCheck = cp.Db.ExecuteQuery(
                    $"SELECT TOP 1 id, pageId FROM cclinkaliases WHERE name={cp.Db.EncodeSQLText(normalizedAlias)} AND (active<>0 OR active IS NULL)")) {
                    if (dtCheck?.Rows != null && dtCheck.Rows.Count > 0) {
                        return errorResponse(cp, $"The URL '{normalizedAlias}' is already in use.");
                    }
                }
                //
                string qsSuffix = context.queryStringSuffix ?? "";
                //
                if (makeCanonical) {
                    var alias = DbBaseModel.addDefault<LinkAliasModel>(cp);
                    alias.name = normalizedAlias;
                    alias.pageId = context.pageId;
                    alias.queryStringSuffix = qsSuffix;
                    alias.save(cp);
                    //
                    return successResponse(cp, new {
                        pageId = context.pageId,
                        newAlias = normalizedAlias,
                        isCanonical = true
                    }, $"Alias '{normalizedAlias}' added as the canonical URL.");
                } else {
                    string qsSql = string.IsNullOrEmpty(qsSuffix)
                        ? "(queryStringSuffix IS NULL OR queryStringSuffix='')"
                        : $"queryStringSuffix={cp.Db.EncodeSQLText(qsSuffix)}";
                    int oldCanonicalId = 0;
                    string canonicalUrl = "";
                    using (DataTable dtCanon = cp.Db.ExecuteQuery(
                        $"SELECT TOP 1 id, name FROM cclinkaliases WHERE pageId={context.pageId} AND {qsSql} AND (active<>0 OR active IS NULL) ORDER BY id DESC")) {
                        if (dtCanon?.Rows != null && dtCanon.Rows.Count > 0) {
                            oldCanonicalId = cp.Utils.EncodeInteger(dtCanon.Rows[0]["id"]);
                            canonicalUrl = dtCanon.Rows[0]["name"]?.ToString() ?? "";
                        }
                    }
                    //
                    var newAliasRecord = DbBaseModel.addDefault<LinkAliasModel>(cp);
                    newAliasRecord.name = normalizedAlias;
                    newAliasRecord.pageId = context.pageId;
                    newAliasRecord.queryStringSuffix = qsSuffix;
                    newAliasRecord.save(cp);
                    //
                    if (oldCanonicalId > 0 && !string.IsNullOrEmpty(canonicalUrl)) {
                        var reinserted = DbBaseModel.addDefault<LinkAliasModel>(cp);
                        reinserted.name = canonicalUrl;
                        reinserted.pageId = context.pageId;
                        reinserted.queryStringSuffix = qsSuffix;
                        reinserted.save(cp);
                        //
                        DbBaseModel.delete<LinkAliasModel>(cp, oldCanonicalId);
                    }
                    //
                    return successResponse(cp, new {
                        pageId = context.pageId,
                        newAlias = normalizedAlias,
                        canonicalUrl,
                        isCanonical = false
                    }, $"Alias '{normalizedAlias}' added as a redirect. '{canonicalUrl}' remains canonical.");
                }
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return errorResponse(cp, $"Error: {ex.Message}");
            }
        }
        //
        // ====================================================================================================
        // -- widget_types
        // ====================================================================================================
        //
        public static string widgetTypes(CPBaseClass cp, Dictionary<string, object> args) {
            try {
                var addons = DbBaseModel.createList<AddonModel>(cp, "(content<>0)and(active<>0)", "name");
                var result = addons.Select(a => new {
                    guid = a.ccguid,
                    name = a.name,
                    description = a.help ?? "",
                    hasInstanceContent = (a.instanceSettingPrimaryContentId ?? 0) > 0
                }).ToList();
                //
                return successResponse(cp, result);
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return errorResponse(cp, $"Error: {ex.Message}");
            }
        }
        //
        // ====================================================================================================
        // -- page_widget_add
        // ====================================================================================================
        //
        public static string pageWidgetAdd(CPBaseClass cp, Dictionary<string, object> args) {
            try {
                string url = getStringArg(args, "url");
                string designBlockTypeGuid = getStringArg(args, "designBlockTypeGuid");
                int position = getIntArg(args, "position");
                //
                if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(designBlockTypeGuid)) {
                    return errorResponse(cp, "url and designBlockTypeGuid are required.");
                }
                //
                var context = UrlResolverHelper.resolve(cp, url);
                if (context == null) {
                    return errorResponse(cp, $"No page found for url '{url}'.");
                }
                var page = context.page;
                //
                var addonList = string.IsNullOrEmpty(page.addonList)
                    ? new List<AddonListItemModel>()
                    : cp.JSON.Deserialize<List<AddonListItemModel>>(page.addonList);
                //
                // -- capture undo before modifying addonList
                var beforeFields = new Dictionary<string, string> { ["addonList"] = page.addonList ?? "" };
                captureUndo(cp, "Page Content", page.id, page.ccguid ?? "", "page_widget_add", beforeFields);
                //
                var newItem = new AddonListItemModel {
                    designBlockTypeGuid = designBlockTypeGuid,
                    instanceGuid = cp.Utils.CreateGuid()
                };
                //
                if (position > 0 && position <= addonList.Count) {
                    addonList.Insert(position - 1, newItem);
                } else {
                    addonList.Add(newItem);
                }
                //
                AddonListItemModel.normalizeAddonList(cp, addonList);
                page.addonList = cp.JSON.Serialize(addonList);
                page.save(cp);
                //
                return successResponse(cp, new { instanceGuid = newItem.instanceGuid }, "Widget added.");
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return errorResponse(cp, $"Error: {ex.Message}");
            }
        }
        //
        // ====================================================================================================
        // -- page_widget_remove
        // ====================================================================================================
        //
        public static string pageWidgetRemove(CPBaseClass cp, Dictionary<string, object> args) {
            try {
                string url = getStringArg(args, "url");
                string instanceGuid = getStringArg(args, "instanceGuid");
                //
                if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(instanceGuid)) {
                    return errorResponse(cp, "url and instanceGuid are required.");
                }
                //
                var context = UrlResolverHelper.resolve(cp, url);
                if (context == null) {
                    return errorResponse(cp, $"No page found for url '{url}'.");
                }
                var page = context.page;
                //
                var addonList = string.IsNullOrEmpty(page.addonList)
                    ? new List<AddonListItemModel>()
                    : cp.JSON.Deserialize<List<AddonListItemModel>>(page.addonList);
                //
                // -- capture undo before modifying addonList
                var beforeFields = new Dictionary<string, string> { ["addonList"] = page.addonList ?? "" };
                captureUndo(cp, "Page Content", page.id, page.ccguid ?? "", "page_widget_remove", beforeFields);
                //
                bool removed = AddonListItemModel.deleteInstance(cp, addonList, instanceGuid);
                if (!removed) {
                    return errorResponse(cp, "Widget instance not found on this page.");
                }
                //
                page.addonList = cp.JSON.Serialize(addonList);
                page.save(cp);
                //
                return successResponse(cp, null, "Widget removed.");
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return errorResponse(cp, $"Error: {ex.Message}");
            }
        }
        //
        // ====================================================================================================
        // -- page_widget_reorder
        // ====================================================================================================
        //
        public static string pageWidgetReorder(CPBaseClass cp, Dictionary<string, object> args) {
            try {
                string url = getStringArg(args, "url");
                string instanceGuidsCommaSeparated = getStringArg(args, "instanceGuidsCommaSeparated");
                //
                if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(instanceGuidsCommaSeparated)) {
                    return errorResponse(cp, "url and instanceGuidsCommaSeparated are required.");
                }
                //
                var instanceGuids = instanceGuidsCommaSeparated.Split(',').Select(g => g.Trim()).Where(g => !string.IsNullOrEmpty(g)).ToList();
                if (instanceGuids.Count == 0) {
                    return errorResponse(cp, "instanceGuidsCommaSeparated must contain at least one GUID.");
                }
                //
                var context = UrlResolverHelper.resolve(cp, url);
                if (context == null) {
                    return errorResponse(cp, $"No page found for url '{url}'.");
                }
                var page = context.page;
                //
                var addonList = string.IsNullOrEmpty(page.addonList)
                    ? new List<AddonListItemModel>()
                    : cp.JSON.Deserialize<List<AddonListItemModel>>(page.addonList);
                //
                // -- capture undo before modifying addonList
                var beforeFields = new Dictionary<string, string> { ["addonList"] = page.addonList ?? "" };
                captureUndo(cp, "Page Content", page.id, page.ccguid ?? "", "page_widget_reorder", beforeFields);
                //
                var reordered = new List<AddonListItemModel>();
                foreach (string guid in instanceGuids) {
                    var item = addonList.FirstOrDefault(a => string.Equals(a.instanceGuid, guid, StringComparison.OrdinalIgnoreCase));
                    if (item != null) { reordered.Add(item); }
                }
                foreach (var item in addonList) {
                    if (!reordered.Contains(item)) { reordered.Add(item); }
                }
                //
                AddonListItemModel.normalizeAddonList(cp, reordered);
                page.addonList = cp.JSON.Serialize(reordered);
                page.save(cp);
                //
                return successResponse(cp, null, "Widgets reordered.");
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return errorResponse(cp, $"Error: {ex.Message}");
            }
        }
        //
        // ====================================================================================================
        // -- widget_instance_update
        // ====================================================================================================
        //
        public static string widgetInstanceUpdate(CPBaseClass cp, Dictionary<string, object> args) {
            try {
                string url = getStringArg(args, "url");
                string instanceGuid = getStringArg(args, "instanceGuid");
                string fieldsJson = getStringArg(args, "fieldsJson");
                //
                if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(instanceGuid)) {
                    return errorResponse(cp, "url and instanceGuid are required.");
                }
                //
                Dictionary<string, string> fields;
                try {
                    fields = cp.JSON.Deserialize<Dictionary<string, string>>(fieldsJson);
                } catch {
                    return errorResponse(cp, "Invalid fieldsJson — must be a JSON object mapping field names to string values.");
                }
                if (fields == null || fields.Count == 0) {
                    return errorResponse(cp, "fieldsJson must contain at least one field.");
                }
                //
                var context = UrlResolverHelper.resolve(cp, url);
                if (context == null) {
                    return errorResponse(cp, $"No page found for url '{url}'.");
                }
                //
                if (string.IsNullOrEmpty(context.page.addonList)) {
                    return errorResponse(cp, "This page has no widgets.");
                }
                var addonList = cp.JSON.Deserialize<List<AddonListItemModel>>(context.page.addonList);
                var widget = findWidget(addonList, instanceGuid);
                if (widget == null) {
                    return errorResponse(cp, $"Widget '{instanceGuid}' not found on page '{url}'.");
                }
                //
                var addon = DbBaseModel.create<AddonModel>(cp, widget.designBlockTypeGuid);
                if (addon == null) {
                    return errorResponse(cp, "Widget type not found.");
                }
                int contentId = addon.instanceSettingPrimaryContentId ?? 0;
                if (contentId == 0) {
                    return errorResponse(cp, "This widget type has no editable instance content.");
                }
                //
                string contentName = cp.Content.GetRecordName("content", contentId);
                if (string.IsNullOrEmpty(contentName)) {
                    return errorResponse(cp, "Content definition not found.");
                }
                //
                using (CPCSBaseClass cs = cp.CSNew()) {
                    bool isNew = false;
                    if (!cs.Open(contentName, $"ccguid={cp.Db.EncodeSQLText(instanceGuid)}", "id", true, "", 1)) {
                        isNew = true;
                        if (!cs.Insert(contentName)) {
                            return errorResponse(cp, "Failed to create instance record.");
                        }
                        cs.SetField("ccguid", instanceGuid);
                        cs.SetField("name", $"Instance {instanceGuid}");
                    }
                    //
                    if (isNew) {
                        // -- new record created, undo will delete it
                        captureUndo(cp, contentName, cs.GetInteger("id"), instanceGuid, "widget_instance_update", new Dictionary<string, string>(), true);
                    } else {
                        // -- capture current field values before update
                        var beforeFields = new Dictionary<string, string>();
                        foreach (var kvp in fields) {
                            beforeFields[kvp.Key] = cs.GetText(kvp.Key);
                        }
                        captureUndo(cp, contentName, cs.GetInteger("id"), instanceGuid, "widget_instance_update", beforeFields);
                    }
                    //
                    foreach (var kvp in fields) {
                        cs.SetField(kvp.Key, kvp.Value);
                    }
                    cs.Save();
                }
                //
                return successResponse(cp, new { updated = true }, "Widget instance updated.");
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return errorResponse(cp, $"Error: {ex.Message}");
            }
        }
        //
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
        //
        // ====================================================================================================
        // -- resource_list
        // ====================================================================================================
        //
        public static string resourceList(CPBaseClass cp, Dictionary<string, object> args) {
            try {
                string fileTypeFilter = getStringArg(args, "fileType").Trim().ToLowerInvariant();
                int folderId = getIntArg(args, "folderId");
                int pageSize = getIntArg(args, "pageSize", 50);
                int pageNumber = getIntArg(args, "pageNumber", 1);
                if (pageSize <= 0) { pageSize = 50; }
                if (pageNumber <= 0) { pageNumber = 1; }
                int offset = (pageNumber - 1) * pageSize;
                //
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
                return successResponse(cp, result);
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return errorResponse(cp, $"Error: {ex.Message}");
            }
        }
        //
        // ====================================================================================================
        // -- resource_upload
        // ====================================================================================================
        //
        public static string resourceUpload(CPBaseClass cp, Dictionary<string, object> args) {
            try {
                string filename = getStringArg(args, "filename");
                string base64Content = getStringArg(args, "base64Content");
                string name = getStringArg(args, "name");
                string altText = getStringArg(args, "altText");
                string description = getStringArg(args, "description");
                int folderId = getIntArg(args, "folderId");
                //
                if (string.IsNullOrEmpty(filename) || string.IsNullOrEmpty(base64Content)) {
                    return errorResponse(cp, "filename and base64Content are required.");
                }
                //
                byte[] fileBytes;
                try {
                    fileBytes = Convert.FromBase64String(base64Content);
                } catch {
                    return errorResponse(cp, "Invalid base64Content.");
                }
                //
                string ext = Path.GetExtension(filename).TrimStart('.').ToLowerInvariant();
                int fileTypeId = 0;
                string fileTypeName = "";
                bool isImage = false;
                bool isDownload = false;
                bool isVideo = false;
                using (DataTable dtType = cp.Db.ExecuteQuery(
                    $"SELECT TOP 1 id, name, ISNULL(isImage,0) AS isImage, ISNULL(isDownload,0) AS isDownload, ISNULL(isVideo,0) AS isVideo FROM ccLibraryFileTypes WHERE extensionList LIKE {cp.Db.EncodeSQLText($"%{ext}%")} AND (active<>0 OR active IS NULL)")) {
                    if (dtType?.Rows != null && dtType.Rows.Count > 0) {
                        fileTypeId = cp.Utils.EncodeInteger(dtType.Rows[0]["id"]);
                        fileTypeName = dtType.Rows[0]["name"]?.ToString() ?? "";
                        isImage = dtType.Rows[0]["isImage"]?.ToString() == "1";
                        isDownload = dtType.Rows[0]["isDownload"]?.ToString() == "1";
                        isVideo = dtType.Rows[0]["isVideo"]?.ToString() == "1";
                    }
                }
                //
                string cdnPath = $"cclibraryfiles/filename/{filename}";
                cp.CdnFiles.SaveBinary(cdnPath, fileBytes);
                //
                var libraryFile = DbBaseModel.addDefault<LibraryFilesModel>(cp);
                if (libraryFile == null) {
                    return errorResponse(cp, "Failed to create library file record.");
                }
                libraryFile.name = !string.IsNullOrEmpty(name) ? name : filename;
                libraryFile.filename = cdnPath;
                libraryFile.altText = altText ?? "";
                libraryFile.description = description ?? "";
                libraryFile.fileSize = fileBytes.Length;
                libraryFile.fileTypeId = fileTypeId;
                if (folderId > 0) { libraryFile.folderId = folderId; }
                libraryFile.save(cp);
                //
                // -- capture undo (record was created, undo will delete it)
                captureUndo(cp, "Library Files", libraryFile.id, libraryFile.ccguid ?? "", "resource_upload", new Dictionary<string, string>(), true);
                //
                var result = new {
                    id = libraryFile.id,
                    name = libraryFile.name,
                    filename = cdnPath,
                    altText = libraryFile.altText,
                    fileSize = fileBytes.Length,
                    fileTypeId,
                    fileTypeName,
                    isImage,
                    isDownload,
                    isVideo
                };
                return successResponse(cp, result, "Resource uploaded.");
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return errorResponse(cp, $"Error: {ex.Message}");
            }
        }
        //
        // ====================================================================================================
        // -- resource_update
        // ====================================================================================================
        //
        public static string resourceUpdate(CPBaseClass cp, Dictionary<string, object> args) {
            try {
                int id = getIntArg(args, "id");
                if (id <= 0) {
                    return errorResponse(cp, "A valid id is required.");
                }
                //
                var file = DbBaseModel.create<LibraryFilesModel>(cp, id);
                if (file == null) {
                    return errorResponse(cp, $"Resource #{id} not found.");
                }
                //
                // -- capture undo before modifying
                var beforeFields = new Dictionary<string, string> {
                    ["name"] = file.name ?? "",
                    ["altText"] = file.altText ?? "",
                    ["description"] = file.description ?? "",
                    ["folderId"] = file.folderId.ToString()
                };
                captureUndo(cp, "Library Files", file.id, file.ccguid ?? "", "resource_update", beforeFields);
                //
                if (args.ContainsKey("name")) { file.name = getStringArg(args, "name"); }
                if (args.ContainsKey("altText")) { file.altText = getStringArg(args, "altText"); }
                if (args.ContainsKey("description")) { file.description = getStringArg(args, "description"); }
                int folderId = getIntArg(args, "folderId");
                if (folderId > 0) { file.folderId = folderId; }
                file.save(cp);
                //
                return successResponse(cp, new {
                    id = file.id,
                    name = file.name,
                    altText = file.altText,
                    description = file.description,
                    folderId = file.folderId,
                    filename = file.filename
                }, "Resource updated.");
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return errorResponse(cp, $"Error: {ex.Message}");
            }
        }
        //
        // ====================================================================================================
        // -- undo helpers
        // ====================================================================================================
        //
        private static void captureUndo(CPBaseClass cp, string contentName, int recordId, string recordGuid, string toolName, Dictionary<string, string> fieldsBefore, bool recordDeleted = false) {
            try {
                using (CPCSBaseClass cs = cp.CSNew()) {
                    if (cs.Insert("MCP Undo")) {
                        cs.SetField("name", $"{toolName}: {contentName} id:{recordId} {DateTime.Now:yyyy-MM-dd HH:mm}");
                        cs.SetField("contentName", contentName);
                        cs.SetField("recordId", recordId.ToString());
                        cs.SetField("recordGuid", recordGuid ?? "");
                        cs.SetField("mcpToolName", toolName);
                        cs.SetField("fieldData", cp.JSON.Serialize(fieldsBefore));
                        cs.SetField("recordDeleted", recordDeleted ? "1" : "0");
                        cs.Save();
                    }
                }
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
            }
        }
        //
        // ====================================================================================================
        // -- undo_list
        // ====================================================================================================
        //
        public static string undoList(CPBaseClass cp, Dictionary<string, object> args) {
            try {
                string toolFilter = getStringArg(args, "mcpToolName");
                int pageSize = getIntArg(args, "pageSize", 50);
                int pageNumber = getIntArg(args, "pageNumber", 1);
                if (pageSize <= 0) { pageSize = 50; }
                if (pageNumber <= 0) { pageNumber = 1; }
                int offset = (pageNumber - 1) * pageSize;
                //
                var conditions = new List<string> { "(active<>0 OR active IS NULL)" };
                if (!string.IsNullOrEmpty(toolFilter)) {
                    conditions.Add($"mcpToolName={cp.Db.EncodeSQLText(toolFilter)}");
                }
                string where = string.Join(" AND ", conditions);
                //
                string sql = $@"
                    SELECT id, name, contentName, recordId, recordGuid, mcpToolName, dateAdded, recordDeleted
                    FROM mcpUndo
                    WHERE {where}
                    ORDER BY id DESC
                    OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";
                //
                var result = new List<object>();
                using (DataTable dt = cp.Db.ExecuteQuery(sql)) {
                    if (dt?.Rows != null) {
                        foreach (DataRow row in dt.Rows) {
                            result.Add(new {
                                id = cp.Utils.EncodeInteger(row["id"]),
                                name = row["name"]?.ToString() ?? "",
                                contentName = row["contentName"]?.ToString() ?? "",
                                recordId = cp.Utils.EncodeInteger(row["recordId"]),
                                recordGuid = row["recordGuid"]?.ToString() ?? "",
                                mcpToolName = row["mcpToolName"]?.ToString() ?? "",
                                dateAdded = row["dateAdded"]?.ToString() ?? "",
                                recordDeleted = row["recordDeleted"]?.ToString() == "1" || row["recordDeleted"]?.ToString()?.ToLower() == "true"
                            });
                        }
                    }
                }
                //
                return successResponse(cp, result);
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return errorResponse(cp, $"Error: {ex.Message}");
            }
        }
        //
        // ====================================================================================================
        // -- undo_apply
        // ====================================================================================================
        //
        public static string undoApply(CPBaseClass cp, Dictionary<string, object> args) {
            try {
                int undoId = getIntArg(args, "undoId");
                if (undoId <= 0) {
                    return errorResponse(cp, "undoId is required.");
                }
                //
                // -- load the undo record
                string undoSql = $"SELECT id, contentName, recordId, recordGuid, mcpToolName, fieldData, recordDeleted FROM mcpUndo WHERE id={undoId} AND (active<>0 OR active IS NULL)";
                string contentName = "";
                int recordId = 0;
                string recordGuid = "";
                string mcpToolName = "";
                string fieldDataJson = "";
                bool recordDeleted = false;
                //
                using (DataTable dt = cp.Db.ExecuteQuery(undoSql)) {
                    if (dt?.Rows == null || dt.Rows.Count == 0) {
                        return errorResponse(cp, $"Undo record #{undoId} not found or already applied.");
                    }
                    DataRow row = dt.Rows[0];
                    contentName = row["contentName"]?.ToString() ?? "";
                    recordId = cp.Utils.EncodeInteger(row["recordId"]);
                    recordGuid = row["recordGuid"]?.ToString() ?? "";
                    mcpToolName = row["mcpToolName"]?.ToString() ?? "";
                    fieldDataJson = row["fieldData"]?.ToString() ?? "";
                    recordDeleted = row["recordDeleted"]?.ToString() == "1" || row["recordDeleted"]?.ToString()?.ToLower() == "true";
                }
                //
                if (string.IsNullOrEmpty(contentName)) {
                    return errorResponse(cp, "Undo record has no content name.");
                }
                //
                if (recordDeleted) {
                    //
                    // -- this record was created by MCP, undo means deactivate it
                    // -- first capture current state so undo of the undo can restore it
                    using (CPCSBaseClass cs = cp.CSNew()) {
                        bool opened = false;
                        if (!string.IsNullOrEmpty(recordGuid)) {
                            opened = cs.Open(contentName, $"ccguid={cp.Db.EncodeSQLText(recordGuid)}", "id", true, "", 1);
                        } else if (recordId > 0) {
                            opened = cs.Open(contentName, $"id={recordId}", "id", true, "", 1);
                        }
                        if (opened) {
                            //
                            // -- capture current state as a new undo record (enables redo)
                            var currentFields = new Dictionary<string, string> { ["active"] = "1" };
                            captureUndo(cp, contentName, recordId, recordGuid, "undo_apply", currentFields, false);
                            //
                            cs.SetField("active", "0");
                            cs.Save();
                        }
                    }
                } else {
                    //
                    // -- restore field values from the undo record
                    Dictionary<string, string> fields;
                    try {
                        fields = cp.JSON.Deserialize<Dictionary<string, string>>(fieldDataJson);
                    } catch {
                        return errorResponse(cp, "Invalid fieldData in undo record.");
                    }
                    if (fields == null || fields.Count == 0) {
                        return errorResponse(cp, "Undo record has no field data to restore.");
                    }
                    //
                    using (CPCSBaseClass cs = cp.CSNew()) {
                        bool opened = false;
                        if (!string.IsNullOrEmpty(recordGuid)) {
                            opened = cs.Open(contentName, $"ccguid={cp.Db.EncodeSQLText(recordGuid)}", "id", true, "", 1);
                        } else if (recordId > 0) {
                            opened = cs.Open(contentName, $"id={recordId}", "id", true, "", 1);
                        }
                        //
                        if (!opened) {
                            //
                            // -- record was deleted/deactivated, try to find and reactivate it
                            string reactivateSql = "";
                            if (!string.IsNullOrEmpty(recordGuid)) {
                                reactivateSql = $"SELECT id FROM {cp.Content.GetTable(contentName)} WHERE ccguid={cp.Db.EncodeSQLText(recordGuid)}";
                            } else if (recordId > 0) {
                                reactivateSql = $"SELECT id FROM {cp.Content.GetTable(contentName)} WHERE id={recordId}";
                            }
                            if (!string.IsNullOrEmpty(reactivateSql)) {
                                using (DataTable dtReactivate = cp.Db.ExecuteQuery(reactivateSql)) {
                                    if (dtReactivate?.Rows != null && dtReactivate.Rows.Count > 0) {
                                        int foundId = cp.Utils.EncodeInteger(dtReactivate.Rows[0]["id"]);
                                        if (foundId > 0) {
                                            opened = cs.Open(contentName, $"id={foundId}", "id", false, "", 1);
                                        }
                                    }
                                }
                            }
                            if (!opened) {
                                return errorResponse(cp, $"Target record not found in '{contentName}'.");
                            }
                        }
                        //
                        // -- capture current state before applying undo (enables redo)
                        var currentFields = new Dictionary<string, string>();
                        foreach (var kvp in fields) {
                            currentFields[kvp.Key] = cs.GetText(kvp.Key);
                        }
                        captureUndo(cp, contentName, recordId, recordGuid, "undo_apply", currentFields, false);
                        //
                        // -- apply the undo field data
                        foreach (var kvp in fields) {
                            cs.SetField(kvp.Key, kvp.Value);
                        }
                        //
                        // -- ensure record is active
                        cs.SetField("active", "1");
                        cs.Save();
                    }
                }
                //
                // -- deactivate the consumed undo record
                cp.Db.ExecuteNonQuery($"UPDATE mcpUndo SET active=0 WHERE id={undoId}");
                //
                return successResponse(cp, new { undoId, contentName, recordId, recordGuid }, "Undo applied successfully.");
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return errorResponse(cp, $"Error: {ex.Message}");
            }
        }
    }
}
