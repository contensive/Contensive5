
using Contensive.BaseClasses;
using System;
using System.Collections.Generic;
using System.Data;

namespace Contensive.Models.Db {
    //
    public class DomainModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("domains", "ccdomains", "default", true);
        //
        //====================================================================================================
        /// <summary>
        /// the template used for this domain. Can be overridden by page
        /// </summary>
        public int defaultTemplateId { get; set; }
        /// <summary>
        /// forward traffic to this domain to another domain
        /// </summary>
        public int forwardDomainId { get; set; }
        /// <summary>
        /// forward traffic to this url
        /// </summary>
        public string forwardUrl { get; set; }
        /// <summary>
        /// set response header to noFollow for this domain
        /// </summary>
        public bool noFollow { get; set; }
        /// <summary>
        /// for this domain, display this page not found
        /// </summary>
        public int pageNotFoundPageId { get; set; }
        /// <summary>
        /// for this domain, the home/landing page
        /// </summary>
        public int rootPageId { get; set; }
        /// <summary>
        /// determines the type of response. 0/1=normal, 2=forward to url, 3=forward to replacement domain
        /// </summary>
        public int typeId { get; set; }
        /// <summary>
        /// true if this domain has received traffic
        /// </summary>
        public bool visited { get; set; }
        /// <summary>
        /// the default code to execute for this domain
        /// </summary>
        public int defaultRouteId { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// Cache key for the domain-name-to-DomainModel dictionary
        /// </summary>
        private const string domainDictCacheKey = "domainDictByName";
        //
        //====================================================================================================
        /// <summary>
        /// Look up a domain by request domain name using a cached dictionary of all domains.
        /// The cache is invalidated when any record in ccdomains changes.
        /// If the requested domain is not found, the wildcard "*" domain is returned.
        /// If no wildcard exists, it is created.
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="requestDomainName">The domain name from the incoming request</param>
        /// <returns></returns>
        public static DomainModel getDomainForRequest(CPBaseClass cp, string requestDomainName) {
            //
            // -- try cache first
            var domainDict = cp.Cache.GetObject<Dictionary<string, DomainModel>>(domainDictCacheKey);
            if (domainDict == null) {
                //
                // -- cache miss, load all domains and build dictionary keyed by lowercase name
                domainDict = new Dictionary<string, DomainModel>(StringComparer.OrdinalIgnoreCase);
                List<DomainModel> allDomains = createList<DomainModel>(cp);
                foreach (var domain in allDomains) {
                    if (!string.IsNullOrWhiteSpace(domain.name)) {
                        domainDict[domain.name.ToLowerInvariant()] = domain;
                    }
                }
                //
                // -- store with dependency on ccdomains table so it invalidates on any domain record change
                string tableDependencyKey = cp.Cache.CreateTableDependencyKey(tableMetadata.tableNameLower);
                cp.Cache.Store(domainDictCacheKey, domainDict, new List<string> { tableDependencyKey });
            }
            //
            // -- look up the requested domain
            string lookupKey = (requestDomainName ?? "").ToLowerInvariant();
            if (domainDict.TryGetValue(lookupKey, out DomainModel result)) {
                return result;
            }
            //
            // -- not found, use wildcard
            if (domainDict.TryGetValue("*", out DomainModel wildcard)) {
                return wildcard;
            }
            //
            // -- no wildcard in cache, create one (this also adds it to the db which will invalidate the cache for next request)
            return getWildcardDomain(cp);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Return (do not save) a domain record with name = '*' that can be used when no domain is available.
        /// This domain will be used when no other matching domain is found
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="requestDomain"></param>
        /// <returns></returns>
        public static DomainModel getWildcardDomain(CPBaseClass cp) {
            //
            // -- attempt to get existing wildcard domain
            DomainModel domain = createByUniqueName<DomainModel>(cp, "*");
            if (domain != null) { return domain; }
            //
            // -- create new wildcard domain
            domain = DbBaseModel.addDefault<DomainModel>(cp);
            domain.name = "*";
            domain.rootPageId = 0;
            domain.noFollow = false;
            domain.typeId = 1;
            domain.visited = false;
            domain.forwardUrl = "";
            domain.defaultTemplateId = 0;
            domain.pageNotFoundPageId = 0;
            domain.forwardDomainId = 0;
            domain.defaultRouteId = cp.Site.GetInteger("DEFAULT ROUTE ADDONID");
            domain.save(cp);
            return domain;
        }
        //
        //====================================================================================================
        /// <summary>
        /// get a list of domains that a user can click on
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public static List<string> getPublicDomains(CPBaseClass cp) {
            List<string> result = [];
            using (DataTable dt = cp.Db.ExecuteQuery("select name from ccdomains where (name<>'*')and((typeId is null)or(typeId<=1))")) {
                if(dt?.Rows == null || dt.Rows.Count == 0) { return result; }
                foreach (DataRow row in dt.Rows) {
                    result.Add(row[0].ToString());
                }
            }
            return result;
        }
        //
        public enum DomainTypeEnum {
            Normal = 1,
            ForwardToUrl = 2,
            ForwardToReplacementDomain = 3
        }
    }
}
