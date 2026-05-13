
using Contensive.BaseClasses;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
//
namespace Contensive.Processor.Addons.ContentApi {
    //
    public class PageListRemoteMethod : AddonBaseClass {
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
        public override object Execute(CPBaseClass cp) {
            try {
                if (!ContentApiAuth.requireAdmin(cp)) {
                    return ContentApiHelper.errorResponse(cp, "Admin access required.");
                }
                //
                // -- query the most-recent link alias per (pageId, queryStringSuffix) pair,
                // -- joined with page content for nav headline and parent hierarchy.
                // -- "most-recent" means highest id, which reflects the current/canonical URL
                // -- when a page has been renamed (e.g. /B1 renamed to /BB).
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
                // -- separate base pages (empty queryStringSuffix) from dynamic variants
                var baseNodesByPageId = allNodes
                    .Where(n => !n.isDynamicVariant)
                    .ToDictionary(n => n.pageId);
                var dynamicNodes = allNodes.Where(n => n.isDynamicVariant).ToList();
                //
                // -- attach dynamic variants under their base page node
                foreach (var variant in dynamicNodes) {
                    if (baseNodesByPageId.TryGetValue(variant.pageId, out var baseNode)) {
                        baseNode.children.Add(variant);
                    }
                }
                //
                // -- build tree from parentId relationships among base pages.
                // -- base pages whose parent is not in the list become roots.
                var roots = new List<PageNode>();
                foreach (var node in baseNodesByPageId.Values) {
                    if (node.parentPageId > 0 && baseNodesByPageId.TryGetValue(node.parentPageId, out var parentNode)) {
                        // -- insert before any dynamic variants so child base pages appear first
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
                return ContentApiHelper.successResponse(cp, serializeTree(roots));
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return ContentApiHelper.errorResponse(cp, $"Error: {ex.Message}");
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
    }
}
