
using System;
using System.Collections.Generic;
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using Contensive.Processor.Exceptions;
//
namespace Contensive.Processor.Models.Domain {
    /// <summary>
    /// Dictionary of Routes
    /// </summary>
    public class RouteMapModel {
        /// <summary>
        /// cache object name
        /// </summary>
        private const string cacheNameRouteMap = "RouteMapModel";
        //
        //====================================================================================================
        /// <summary>
        /// model for stored route
        /// </summary>
        public class RouteClass {
            public string virtualRoute;
            public string physicalRoute;
            public RouteTypeEnum routeType;
            public int remoteMethodAddonId;
            public int linkAliasId;
            public int linkForwardId;
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
            RouteMapModel result = new RouteMapModel();
            try {
                result = getCache(core);
                if (result == null) {
                    result = new RouteMapModel {
                        dateCreated = core.dateTimeNowMockable,
                        routeDictionary = new Dictionary<string, RouteClass>()
                    };
                    string physicalFile = "~/" + core.siteProperties.serverPageDefault;
                    {
                        //
                        // -- admin route
                        string localRoute = GenericController.normalizeRoute(core.appConfig.adminRoute);
                        if (!string.IsNullOrWhiteSpace(localRoute)) {
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
                                routeType = RouteTypeEnum.admin
                            });
                        }
                    }
                    {
                        //
                        // -- remote methods
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
                    }
                    {
                        //
                        // -- link forwards
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
                    }
                    {
                        //
                        // -- link aliases
                        foreach (var linkAlias in DbBaseModel.createList<LinkAliasModel>(core.cpParent, "name Is Not null")) {
                            string localRoute = GenericController.normalizeRoute(linkAlias.name);
                            if (!string.IsNullOrEmpty(localRoute)) {
                                if (result.routeDictionary.ContainsKey(localRoute)) {
                                    LogController.logError(core, new GenericException("Link Alias route [" + localRoute + "] cannot be added because it matches the Admin Route, a Remote Method, a Link Forward o another Link Alias."));
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
                                        routeType = RouteTypeEnum.linkAlias,
                                        linkAliasId = linkAlias.id
                                    });
                                }
                            }
                        }
                    }
                    setCache(core, result);
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
            return result;
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
        /// invalidate cache if anything is modified
        /// </summary>
        /// <param name="core"></param>
        public static void invalidateCache(CoreController core) {
            core.cache.invalidate(cacheNameRouteMap);
        }
    }
}

