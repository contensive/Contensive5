
using System;
using System.Collections.Generic;
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using Contensive.Processor.Exceptions;
//
namespace Contensive.Processor.Models.Domain {
    /// <summary>
    /// Dictionary of Routes, addons marked remote method, link forwards and link aliases
    /// </summary>
    public class RouteMapModel {
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
                string adminRoute = GenericController.normalizeRoute(core.appConfig.adminRoute);
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
                foreach (var remoteMethod in core.addonCache.getRemoteMethodAddonList()) {
                    string localRoute = GenericController.normalizeRoute(remoteMethod.name);
                    if (!string.IsNullOrWhiteSpace(localRoute)) {
                        if (result.routeDictionary.ContainsKey(localRoute)) {
                            LogController.logWarn(core, new GenericException("Route [" + localRoute + "] cannot be added because it matches the Admin Route or another Remote Method."));
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
                    string localRoute = GenericController.normalizeRoute(linkForward.sourceLink);
                    if (!string.IsNullOrEmpty(localRoute)) {
                        if (result.routeDictionary.ContainsKey(localRoute)) {
                            LogController.logError(core, new GenericException("Link Forward Route [" + localRoute + "] cannot be added because it matches the Admin Route, a Remote Method or another Link Forward."));
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
                }
                //
                // -- add link aliases
                // 221119 - each destination (pageid+qsSuffix) may have mulitple urls (name). if not primary (highest id) link alias for this page/qs, forward to the first
                Dictionary<string, string> usedPages = new();
                foreach (var linkAlias in DbBaseModel.createList<LinkAliasModel>(core.cpParent, "name Is Not null", "pageid,queryStringSuffix,id desc")) {
                    string localRoute = GenericController.normalizeRoute(linkAlias.name);
                    if (string.IsNullOrEmpty(localRoute)) { continue; }
                    //
                    if (result.routeDictionary.ContainsKey(localRoute)) {
                        //
                        // -- duplicate route. map must be unique. fail route
                        LogController.logError(core, new GenericException("Link Alias route [" + localRoute + "] cannot be added because it matches the Admin Route, a Remote Method, a Link Forward o another Link Alias."));
                        continue;
                    }
                    //
                    // -- add routeSuffix wildcard to all remote methods that do not have a wildcard so /a/b/c will match addons a, or a/b, or a/b/c
                    string mapRoute = localRoute;
                    if (!localRoute.Contains("{*")) {
                        mapRoute += (localRoute.Substring(localRoute.Length - 1, 1).Equals("/") ? "" : "/") + "{*routeSuffix}";
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
                            linkAliasRedirect = usedPages[pageKey]
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
                LogController.logError(core, ex);
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
        ////
        ////====================================================================================================
        ///// <summary>
        ///// invalidate cache if anything is modified
        ///// </summary>
        ///// <param name="core"></param>
        //public static void invalidateCache(CoreController core) {
        //    core.cache.invalidate(cacheNameRouteMap);
        //}
    }
}

