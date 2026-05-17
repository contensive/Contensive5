
using Contensive.BaseClasses;
using Contensive.Models.Db;
using System.Data;
//
namespace Contensive.Addons.Mcp {
    //
    /// <summary>
    /// Result of resolving a URL to its page context via the link alias table.
    /// </summary>
    public class UrlResolvedContext {
        public int pageId { get; set; }
        public string queryStringSuffix { get; set; }
        public PageContentModel page { get; set; }
        public string linkAliasName { get; set; }
    }
    //
    /// <summary>
    /// Shared URL resolution logic used by MCP tool methods.
    /// Resolves a URL string to its (pageId, queryStringSuffix, page) context
    /// via the cclinkaliases table.
    /// </summary>
    public static class UrlResolverHelper {
        //
        /// <summary>
        /// Resolves a URL to its page context. Returns null if not found.
        /// Normalizes the URL: ensures leading slash, trims trailing slash (except root "/").
        /// Selects the most recently created link alias record when multiple exist for the same URL.
        /// </summary>
        public static UrlResolvedContext resolve(CPBaseClass cp, string url) {
            if (string.IsNullOrEmpty(url)) { return null; }
            //
            // -- normalize: ensure leading slash, trim trailing slash (unless root)
            string normalizedUrl = url.Trim();
            if (!normalizedUrl.StartsWith("/")) { normalizedUrl = $"/{normalizedUrl}"; }
            if (normalizedUrl.Length > 1 && normalizedUrl.EndsWith("/")) {
                normalizedUrl = normalizedUrl.TrimEnd('/');
            }
            //
            // -- special case: "/" means the site root landing page, resolved via the domain record
            if (normalizedUrl == "/") {
                var domain = DomainModel.createByUniqueName<DomainModel>(cp, cp.Request.Host);
                if (domain == null) {
                    domain = DomainModel.getWildcardDomain(cp);
                }
                if (domain == null || domain.rootPageId <= 0) { return null; }
                var rootPage = DbBaseModel.create<PageContentModel>(cp, domain.rootPageId);
                if (rootPage == null) { return null; }
                // -- find the canonical link alias for this page (no queryStringSuffix)
                string rootAlias = "/";
                using (DataTable dtAlias = cp.Db.ExecuteQuery(
                    $"SELECT TOP 1 name FROM cclinkaliases WHERE pageId={domain.rootPageId} AND (queryStringSuffix IS NULL OR queryStringSuffix='') AND (active<>0 OR active IS NULL) ORDER BY id DESC")) {
                    if (dtAlias?.Rows != null && dtAlias.Rows.Count > 0) {
                        rootAlias = dtAlias.Rows[0]["name"]?.ToString() ?? "/";
                    }
                }
                return new UrlResolvedContext {
                    pageId = domain.rootPageId,
                    queryStringSuffix = "",
                    page = rootPage,
                    linkAliasName = rootAlias
                };
            }
            //
            // -- look up in link alias table; most recently created record wins when multiple match
            using (DataTable dt = cp.Db.ExecuteQuery(
                $"SELECT TOP 1 id, name, pageId, queryStringSuffix FROM cclinkaliases WHERE name={cp.Db.EncodeSQLText(normalizedUrl)} AND (active<>0 OR active IS NULL) ORDER BY id DESC")) {
                if (dt?.Rows == null || dt.Rows.Count == 0) { return null; }
                var row = dt.Rows[0];
                if (!int.TryParse(row["pageId"]?.ToString(), out int pageId) || pageId <= 0) { return null; }
                var page = DbBaseModel.create<PageContentModel>(cp, pageId);
                if (page == null) { return null; }
                return new UrlResolvedContext {
                    pageId = pageId,
                    queryStringSuffix = row["queryStringSuffix"]?.ToString() ?? "",
                    page = page,
                    linkAliasName = normalizedUrl
                };
            }
        }
    }
}
