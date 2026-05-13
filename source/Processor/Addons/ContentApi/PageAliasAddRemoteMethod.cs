
using Contensive.BaseClasses;
using Contensive.Models.Db;
using System;
using System.Data;
//
namespace Contensive.Processor.Addons.ContentApi {
    //
    public class PageAliasAddRemoteMethod : AddonBaseClass {
        //
        private class PageAliasAddRequest {
            public string url { get; set; }
            public string newAlias { get; set; }
            /// <summary>
            /// If true (default), the new alias becomes the canonical URL.
            /// If false, the existing canonical URL is preserved as canonical and the new alias is added as a redirect.
            /// </summary>
            public bool makeCanonical { get; set; } = true;
        }
        //
        public override object Execute(CPBaseClass cp) {
            try {
                if (!ContentApiAuth.requireAdmin(cp)) {
                    return ContentApiHelper.errorResponse(cp, "Admin access required.");
                }
                //
                string requestBody = cp.Request.Body;
                if (string.IsNullOrEmpty(requestBody)) {
                    return ContentApiHelper.errorResponse(cp, "Request body is required.");
                }
                var request = cp.JSON.Deserialize<PageAliasAddRequest>(requestBody);
                if (request == null || string.IsNullOrEmpty(request.url)) {
                    return ContentApiHelper.errorResponse(cp, "url is required.");
                }
                if (string.IsNullOrEmpty(request.newAlias)) {
                    return ContentApiHelper.errorResponse(cp, "newAlias is required.");
                }
                //
                // -- normalize new alias
                string normalizedAlias = request.newAlias.Trim();
                if (!normalizedAlias.StartsWith("/")) { normalizedAlias = $"/{normalizedAlias}"; }
                if (normalizedAlias.Length > 1 && normalizedAlias.EndsWith("/")) {
                    normalizedAlias = normalizedAlias.TrimEnd('/');
                }
                //
                // -- resolve the target page
                var context = UrlResolverHelper.resolve(cp, request.url);
                if (context == null) {
                    return ContentApiHelper.errorResponse(cp, $"No page found for url '{request.url}'.");
                }
                //
                // -- check if newAlias already exists in cclinkaliases
                using (DataTable dtCheck = cp.Db.ExecuteQuery(
                    $"SELECT TOP 1 id, pageId FROM cclinkaliases WHERE name={cp.Db.EncodeSQLText(normalizedAlias)} AND (active<>0 OR active IS NULL)")) {
                    if (dtCheck?.Rows != null && dtCheck.Rows.Count > 0) {
                        return ContentApiHelper.errorResponse(cp, $"The URL '{normalizedAlias}' is already in use.");
                    }
                }
                //
                string qsSuffix = context.queryStringSuffix ?? "";
                //
                if (request.makeCanonical) {
                    //
                    // -- add new alias as canonical: insert it — it gets the highest ID and becomes canonical
                    var alias = DbBaseModel.addDefault<LinkAliasModel>(cp);
                    alias.name = normalizedAlias;
                    alias.pageId = context.pageId;
                    alias.queryStringSuffix = qsSuffix;
                    alias.save(cp);
                    //
                    return ContentApiHelper.successResponse(cp, new {
                        pageId = context.pageId,
                        newAlias = normalizedAlias,
                        isCanonical = true
                    }, $"Alias '{normalizedAlias}' added as the canonical URL.");
                    //
                } else {
                    //
                    // -- add new alias as redirect: insert the new alias, then re-insert the current
                    //    canonical to give it a higher ID so it remains canonical, then delete the old
                    //    canonical record.
                    //
                    // -- find current canonical record (highest id for this page + queryStringSuffix)
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
                    // -- insert the new alias (redirect)
                    var newAliasRecord = DbBaseModel.addDefault<LinkAliasModel>(cp);
                    newAliasRecord.name = normalizedAlias;
                    newAliasRecord.pageId = context.pageId;
                    newAliasRecord.queryStringSuffix = qsSuffix;
                    newAliasRecord.save(cp);
                    //
                    // -- if a canonical existed, re-insert it as a new record and delete the old one
                    if (oldCanonicalId > 0 && !string.IsNullOrEmpty(canonicalUrl)) {
                        var reinserted = DbBaseModel.addDefault<LinkAliasModel>(cp);
                        reinserted.name = canonicalUrl;
                        reinserted.pageId = context.pageId;
                        reinserted.queryStringSuffix = qsSuffix;
                        reinserted.save(cp);
                        //
                        // -- delete old canonical record so no duplicate
                        DbBaseModel.delete<LinkAliasModel>(cp, oldCanonicalId);
                    }
                    //
                    return ContentApiHelper.successResponse(cp, new {
                        pageId = context.pageId,
                        newAlias = normalizedAlias,
                        canonicalUrl,
                        isCanonical = false
                    }, $"Alias '{normalizedAlias}' added as a redirect. '{canonicalUrl}' remains canonical.");
                }
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return ContentApiHelper.errorResponse(cp, $"Error: {ex.Message}");
            }
        }
    }
}
