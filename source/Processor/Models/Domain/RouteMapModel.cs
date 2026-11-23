
using System;
using System.Collections.Generic;
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using Contensive.Exceptions;
using NLog;
//
namespace Contensive.Processor.Models.Domain {
    /// <summary>
    /// Dictionary of Routes, addons marked remote method, link forwards and link aliases
    /// </summary>
    public class RouteMapModel {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// cache object name
        /// </summary>
        public const string cacheNameRouteMap = "RouteMapModel";
        //
        //====================================================================================================
        /// <summary>
        /// model for stored route
        /// </summary>
        public class RouteClass {
            /// <summary>
            /// the url that this route applies to
            /// </summary>
            public string virtualRoute { get; set; }
            /// <summary>
            /// the actual physical page this should route. This is the script page.
            /// </summary>
            public string physicalRoute { get; set; }
            public RouteTypeEnum routeType { get; set; }
            public int remoteMethodAddonId { get; set; }
            public int linkAliasId { get; set; }
            public int linkForwardId { get; set; }
            /// <summary>
            /// if the link alias is not the first for this page, this is populated with the primary route for this page for a redirect
            /// </summary>
            public string linkAliasRedirect { get; set; }
            /// <summary>
            /// if this entry is a link alias, this is the pageid
            /// </summary>
            public int linkAliasPageId { get; set; }
            /// <summary>
            /// if this entry is a link alis, this is a list of request properties to add 
            /// </summary>
            public List<NameValueModel> linkAliasQSList { get; set; }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Types of routes stored
        /// </summary>
        public enum RouteTypeEnum {
            admin,
            remoteMethod,
            linkAlias,
            linkForward
        }
        //
        //====================================================================================================
        /// <summary>
        /// The date and time when this route dictionary was created. Used by iis app to detect if the route table needs to be updated.
        /// </summary>
        public DateTime dateCreated { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// public dictionary of routes in the model
        /// routes DO NOT include the leading slash, so if used in normalizeUrl etc, you have to prepend a slash
        /// </summary>
        public Dictionary<string, RouteClass> routeDictionary;
        //
        //===================================================================================================
        /// <summary>
        /// Create a list of routes
        /// </summary>
        /// <param name="core"></param>
        /// <returns></returns>
        public static RouteMapModel create(CoreController core) {
            RouteMapModel result = null;
            try {
                result = getCache(core);
                if (result != null) { return result; }
                //
                // -- create route map and add records from each contributor
                result = new RouteMapModel {
                    dateCreated = core.dateTimeNowMockable,
                    routeDictionary = new Dictionary<string, RouteClass>()
                };
                string physicalFile = "~/" + core.siteProperties.serverPageDefault;
                //
                // -- add admin route
                string adminRoute = RouteController.normalizeRoute(core.appConfig.adminRoute);
                if (!string.IsNullOrWhiteSpace(adminRoute)) {
                    //
                    // -- add routeSuffix wildcard to all remote methods that do not have a wildcard so /a/b/c will match addons a, or a/b, or a/b/c
                    string adminMapRoute = adminRoute;
                    if (!adminRoute.Contains("{*")) {
                        adminMapRoute += (adminRoute.Substring(adminRoute.Length - 1, 1).Equals("/") ? "" : "/") + "{*routeSuffix}";
                    }
                    //
                    result.routeDictionary.Add(adminRoute, new RouteClass {
                        physicalRoute = physicalFile,
                        virtualRoute = adminMapRoute,
                        routeType = RouteTypeEnum.admin
                    });
                }
                //
                // -- add remote methods
                foreach (var remoteMethod in core.cacheRuntime.addonCache.getRemoteMethodAddonList()) {
                    string localRoute = RouteController.normalizeRoute(remoteMethod.name);
                    if (!string.IsNullOrWhiteSpace(localRoute)) {
                        if (result.routeDictionary.ContainsKey(localRoute)) {
                            logger.Warn($"{core.logCommonMessage}, Route [{localRoute}] cannot be added because it matches the Admin Route or another Remote Method.");
                        } else {
                            //
                            // -- add routeSuffix wildcard to all remote methods that do not have a wildcard so /a/b/c will match addons a, or a/b, or a/b/c
                            string mapRoute = localRoute;
                            if (!localRoute.Contains("{*")) {
                                mapRoute += ((localRoute.Substring(localRoute.Length - 1, 1).Equals("/")) ? "" : "/") + "{*routeSuffix}";
                            }
                            //
                            result.routeDictionary.Add(localRoute, new RouteClass {
                                physicalRoute = physicalFile,
                                virtualRoute = mapRoute,
                                routeType = RouteTypeEnum.remoteMethod,
                                remoteMethodAddonId = remoteMethod.id
                            });
                        }
                    }
                }
                //
                // -- add link forwards
                foreach (var linkForward in DbBaseModel.createList<LinkForwardModel>(core.cpParent, "name Is Not null")) {
                    string localRoute = RouteController.normalizeRoute(linkForward.sourceLink);
                    if (string.IsNullOrEmpty(localRoute)) {
                        continue;
                    }
                    if (result.routeDictionary.ContainsKey(localRoute)) {
                        string warnName = $"Link Forward Route [{localRoute}] cannot be added";
                        string warnDesc = $"Link Forward Route [{localRoute}] cannot be added because it matches the Admin Route [{core.appConfig.adminRoute}], a Remote Method or another Link Forward.";
                        LogController.setSiteWarning(core, warnName, warnDesc, true);
                    } else {
                        //
                        // -- link alias does not modify the route 
                        result.routeDictionary.Add(localRoute, new RouteClass {
                            physicalRoute = physicalFile,
                            virtualRoute = localRoute,
                            routeType = RouteTypeEnum.linkForward,
                            linkForwardId = linkForward.id
                        });
                    }
                }
                //
                // -- build landing page dictionary (maps pageId to canonical domain root, or empty string if config error)
                Dictionary<int, string> landingPageDict = buildLandingPageDictionary(core);
                //
                // -- add link aliases
                // 221119 - each destination (pageid+qsSuffix) may have mulitple urls (name). if not primary (highest id) link alias for this page/qs, forward to the first
                Dictionary<string, string> usedPages = [];
                foreach (KeyValuePair<string,LinkAliasModel> kvp in core.cacheRuntime.linkAliasNameDict ) {
                    LinkAliasModel linkAlias = kvp.Value;   
                    string localRoute = RouteController.normalizeRoute(linkAlias.name);
                    if (string.IsNullOrEmpty(localRoute)) {
                        continue;
                    }
                    //
                    if (result.routeDictionary.ContainsKey(localRoute)) {
                        //
                        // -- duplicate route. map must be unique. fail route
                        string warnName = $"Page URL route [{localRoute}] cannot be added";
                        string warnDesc = $"Page URL route [{localRoute}] cannot be added because it matches the Admin Route, a Remote Method, a Link Forward o another Page URL.";
                        LogController.setSiteWarning(core, warnName, warnDesc, true);
                        continue;
                    }
                    //
                    // -- add routeSuffix wildcard to all remote methods that do not have a wildcard so /a/b/c will match addons a, or a/b, or a/b/c
                    string mapRoute = localRoute;
                    if (!localRoute.Contains("{*")) {
                        mapRoute += (localRoute.Substring(localRoute.Length - 1, 1).Equals("/") ? "" : "/") + "{*routeSuffix}";
                    }
                    //
                    // -- check if this page is a domain landing page (and has no query string)
                    // -- landing pages should always redirect to domain root, not to their link alias
                    bool isLandingPage = landingPageDict.ContainsKey(linkAlias.pageId) && string.IsNullOrEmpty(linkAlias.queryStringSuffix);
                    if (isLandingPage) {
                        // Landing page link alias: always redirect to domain root
                        result.routeDictionary.Add(localRoute, new RouteClass {
                            physicalRoute = physicalFile,
                            virtualRoute = mapRoute,
                            routeType = RouteTypeEnum.linkAlias,
                            linkAliasId = linkAlias.id,
                            linkAliasPageId = linkAlias.pageId,
                            linkAliasRedirect = "/"
                        });
                        logger.Debug($"{core.logCommonMessage}, Link alias [{localRoute}] for landing page {linkAlias.pageId} redirects to domain root");
                        continue;
                    }
                    //
                    // -- if this is not the first linkalias for a destination, save it as a redirect to the first
                    string pageKey = linkAlias.pageId + "/" + linkAlias.queryStringSuffix;
                    if (usedPages.ContainsKey(pageKey)) {
                        //
                        // -- this page/qs is a duplicate (with a different url). Forward to this url
                        result.routeDictionary.Add(localRoute, new RouteClass {
                            physicalRoute = physicalFile,
                            virtualRoute = mapRoute,
                            routeType = RouteTypeEnum.linkAlias,
                            linkAliasId = linkAlias.id,
                            linkAliasRedirect = "/" + usedPages[pageKey]
                        });
                        continue;
                    }
                    usedPages.Add(pageKey, localRoute);
                    //
                    // -- set the linkAliasPageId and linkAliasQSList
                    int linkAliasPageId = linkAlias.pageId;
                    List<NameValueModel> linkAliasQSList = new();
                    if (!string.IsNullOrWhiteSpace(linkAlias.queryStringSuffix)) {
                        string[] keyValuePairs = linkAlias.queryStringSuffix.Split('&');
                        // -- iterate through all the key=value pairs
                        foreach (var keyEqualsValue in keyValuePairs) {
                            string[] keyValue = keyEqualsValue.Split('=');
                            if (!string.IsNullOrEmpty(keyValue[0])) {
                                if (!core.docProperties.containsKey(keyValue[0])) {
                                    if (keyValue.Length > 1) {
                                        linkAliasQSList.Add(new NameValueModel {
                                            name = keyValue[0],
                                            value = keyValue[1]
                                        });
                                    } else {
                                        linkAliasQSList.Add(new NameValueModel {
                                            name = keyValue[0],
                                            value = string.Empty
                                        });
                                    }
                                }
                            }
                        }
                    }
                    //
                    result.routeDictionary.Add(localRoute, new RouteClass {
                        physicalRoute = physicalFile,
                        virtualRoute = mapRoute,
                        routeType = RouteTypeEnum.linkAlias,
                        linkAliasId = linkAlias.id,
                        linkAliasPageId = linkAliasPageId,
                        linkAliasQSList = linkAliasQSList
                    });
                }
                setCache(core, result);
                return result;
            } catch (Exception ex) {
                // -- log error but return what you can. Without routes the application fails hard.
                logger.Error(ex, $"{core.logCommonMessage}");
                if (result == null) { return new RouteMapModel(); }
                return result;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// save the model afer it is created. Depends on addons, linkAlias and LinkForwards
        /// </summary>
        /// <param name="core"></param>
        /// <param name="routeDictionary"></param>
        private static void setCache(CoreController core, RouteMapModel routeDictionary) {
            var dependentKeyList = new List<CacheKeyHashClass> {
                core.cache.createTableDependencyKeyHash(AddonModel.tableMetadata.tableNameLower),
                core.cache.createTableDependencyKeyHash(LinkAliasModel.tableMetadata.tableNameLower),
                core.cache.createTableDependencyKeyHash(LinkForwardModel.tableMetadata.tableNameLower)
            };
            core.cache.storeObject(cacheNameRouteMap, routeDictionary, dependentKeyList);
        }
        //
        //====================================================================================================
        /// <summary>
        /// load the model from cache. returns null if cache not valid
        /// </summary>
        /// <param name="core"></param>
        /// <returns></returns>
        private static RouteMapModel getCache(CoreController core) {
            return core.cache.getObject<RouteMapModel>(cacheNameRouteMap);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Build a dictionary of pages that are domain landing pages.
        /// Key: pageId
        /// Value: empty string (meaning this is the canonical domain for this page)
        /// If multiple domains use the same landing page (configuration error), use the highest domain ID.
        /// Returns empty dictionary if query fails (fallback to current behavior).
        /// </summary>
        /// <param name="core"></param>
        /// <returns></returns>
        private static Dictionary<int, string> buildLandingPageDictionary(CoreController core) {
            var result = new Dictionary<int, string>();
            try {
                // Query all active normal domains with landing pages (typeId 0 or 1, exclude forwarding domains 2 and 3)
                using (var csData = new CsModel(core)) {
                    csData.open("Domains", "(rootPageId>0)and(active>0)and((typeId is null)or(typeId<=1))", "rootPageId,id desc", false, 0, "rootPageId,id,name");
                    while (csData.ok()) {
                        int pageId = csData.getInteger("rootPageId");
                        int domainId = csData.getInteger("id");
                        string domainName = csData.getText("name");
                        if (pageId > 0) {
                            if (!result.ContainsKey(pageId)) {
                                // First occurrence (highest ID due to sort order) - this is the canonical domain
                                result.Add(pageId, "");
                                logger.Debug($"{core.logCommonMessage}, Landing page {pageId} canonical domain: {domainName} (id:{domainId})");
                            } else {
                                // Duplicate landing page - configuration error, but already logged by status endpoint
                                logger.Debug($"{core.logCommonMessage}, Landing page {pageId} duplicate domain: {domainName} (id:{domainId})");
                            }
                        }
                        csData.goNext();
                    }
                }
                logger.Debug($"{core.logCommonMessage}, Built landing page dictionary with {result.Count} entries");
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}, Error building landing page dictionary, returning empty dictionary");
                return new Dictionary<int, string>();
            }
            return result;
        }
    }
}

