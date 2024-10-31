
using Contensive.BaseClasses;
using Contensive.Exceptions;
using Contensive.Models.Db;
using Contensive.Processor.Models.Domain;
using System;
using System.Collections.Generic;
using static Contensive.Processor.Constants;
using static Contensive.Processor.Controllers.GenericController;

namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// static class controller
    /// </summary>
    public static class RouteController {
        //
        //=============================================================================
        /// <summary>
        /// Executes the current route. To determine the route:
        /// route can be from URL, or from routeOverride
        /// how to process route
        /// -- urlParameters - /urlParameter(0)/urlParameter(1)/etc.
        /// -- first try full url, then remove them from the left and test until last, try just urlParameter(0)
        /// ---- so url /a/b/c, with addon /a and addon /a/b -> would run addon /a/b
        /// 
        /// </summary>
        /// <returns>The doc created by the default addon. (html, json, etc)</returns>
        public static string executeRoute(CoreController core, string routeOverride) {
            try {
                //
                // -- check appConfig
                if (core?.appConfig?.enabled == null || !core.appConfig.enabled) {
                    if (core == null) { throw new GenericException("executeRoute failed because coreController null"); }
                    if (core?.appConfig == null) { throw new GenericException("executeRoute failed because core.appConfig null"); }
                    logger.Debug($"{core.logCommonMessage},executeRoute returned empty because application [" + core.appConfig.name + "] is marked inactive in config.json");
                    return string.Empty;
                }
                LogController.log(core, "CoreController executeRoute, enter", BaseClasses.CPLogBaseClass.LogLevel.Trace);
                //
                // -- debug defaults on, so if not on, set it off and clear what was collected
                core.doc.visitPropertyAllowDebugging = core.visitProperty.getBoolean("AllowDebugging");
                if (!core.doc.visitPropertyAllowDebugging) { core.doc.testPointMessage = ""; }
                //
                // -- test fix for 404 response during routing - could it be a response left over from processing before we are called
                core.webServer.setResponseStatus(WebServerController.httpResponseStatus200_Success);
                //
                // -- execute intercept methods first, like login, that run before the route that returns the page
                // -- intercept routes should be addons alos
                //
                // -- determine the route: try routeOverride
                string normalizedRoute = GenericController.normalizeRoute(routeOverride);
                if (string.IsNullOrEmpty(normalizedRoute)) {
                    //
                    // -- no override, try argument route (remoteMethodAddon=)
                    normalizedRoute = GenericController.normalizeRoute(core.docProperties.getText(RequestNameRemoteMethodAddon));
                    if (string.IsNullOrEmpty(normalizedRoute)) {
                        //
                        // -- no override or argument, use the url as the route
                        normalizedRoute = GenericController.normalizeRoute(core.webServer.requestPathPage.ToLowerInvariant());
                    }
                }
                //
                // -- legacy form process methods 
                // todo -- move legacy form process methods to process within their own code
                string ajaxfnRouteResult = "";
                if (tryExecuteAjaxfnRoute(core, normalizedRoute, ref ajaxfnRouteResult)) {
                    return ajaxfnRouteResult;
                }
                //
                // -- legacy email intercept methods
                // todo -- convert email intercept methods to remote methods
                if (core.docProperties.getInteger(rnEmailOpenFlag) > 0) {
                    //
                    // -- Process Email Open
                    return (new Contensive.Processor.Addons.Primitives.OpenEmailClass()).Execute(core.cpParent).ToString();
                }
                if (core.docProperties.getInteger(rnEmailClickFlag) > 0) {
                    //
                    // -- Process Email click, execute and continue
                    (new Contensive.Processor.Addons.Primitives.ClickEmailClass()).Execute(core.cpParent).ToString();
                }
                if (!string.IsNullOrWhiteSpace(core.docProperties.getText(rnEmailBlockRecipientEmail))) {
                    //
                    // -- Process Email block
                    return (new Contensive.Processor.Addons.Primitives.BlockEmailClass()).Execute(core.cpParent).ToString();
                }
                //
                // -- legacy form process methods
                // -- returned userError is a problem. 
                string userErrorMessage = "";
                processBuiltInForms(core, userErrorMessage);
                if(!string.IsNullOrEmpty(userErrorMessage)) { core.cpParent.UserError.Add(userErrorMessage); }
                //
                // -- try legacy methods (?method=login)
                string methodRouteResult = "";
                if (tryExecuteMethodRoute(core, normalizedRoute, ref methodRouteResult)) {
                    return methodRouteResult;
                }
                //
                // -- try route Dictionary (addons, admin, link forwards, link alias), from full route to first segment one at a time
                // -- addons and admin are addon execution, link forward and link alias are page manager execution
                string routeDictionaryResult = "";
                if (tryExecuteRouteDictionary(core, normalizedRoute, ref routeDictionaryResult)) {
                    return routeDictionaryResult;
                }
                //
                // -- default route, try domain default route first, then site default route, then just use page manager
                AddonModel routeAddon = null;
                if (core.domain.defaultRouteId > 0) {
                    routeAddon = core.cacheRuntime.addonCache.create(core.domain.defaultRouteId);
                }
                if (routeAddon == null && core.siteProperties.defaultRouteId > 0) {
                    routeAddon = core.cacheRuntime.addonCache.create(core.siteProperties.defaultRouteId);
                }
                if (routeAddon == null) {
                    routeAddon = core.cacheRuntime.addonCache.create(addonGuidPageManager);
                }
                if (routeAddon != null) {
                    //
                    // -- default route is run if no other route is found, which includes the route=defaultPage (default.aspx)
                    CPUtilsBaseClass.addonExecuteContext executeContext = new CPUtilsBaseClass.addonExecuteContext {
                        addonType = CPUtilsBaseClass.addonContext.ContextPage,
                        cssContainerClass = "",
                        cssContainerId = "",
                        hostRecord = new CPUtilsBaseClass.addonExecuteHostRecordContext {
                            contentName = "",
                            fieldName = "",
                            recordId = 0
                        },
                        errorContextMessage = "calling default route addon [" + core.domain.defaultRouteId + "] during execute route method"
                    };
                    return core.addon.execute(routeAddon, executeContext);
                }
                //
                // -- unrecognized route and no default route
                logger.Warn($"{core.logCommonMessage}, executeRoute called with an unknown route [" + normalizedRoute + "], and no default route is set to handle it. Go to the admin site, open preferences and set a detault route. Typically this is Page Manager for websites or an authorization error for remote applications.");
                return "Unknown command";
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                return "";
            } finally {
                LogController.log(core, "CoreController executeRoute, exit", BaseClasses.CPLogBaseClass.LogLevel.Trace);
            }
        }
        //
        /// <summary>
        /// Execute a route from the dictionary. return true if route was found, false if not
        /// route Dictionary (addons, admin, link forwards, link alias), from full route to first segment one at a time
        /// route /this/and/that would first test /this/and/that, then test /this/and, then test /this
        /// </summary>
        /// <param name="core"></param>
        /// <param name="normalizedRoute"></param>
        /// <param name="returnResult"></param>
        /// <returns></returns>
        public static bool tryExecuteRouteDictionary(CoreController core, string normalizedRoute, ref string returnResult) {
            try {
                string routeTest = normalizedRoute;
                bool routeFound = false;
                int routeCnt = 100;
                do {
                    routeFound = core.routeMap.routeDictionary.ContainsKey(routeTest);
                    if (routeFound) {
                        break;
                    }
                    if (routeTest.IndexOf("/") < 0) {
                        break;
                    }
                    routeTest = routeTest.left(routeTest.LastIndexOf("/", StringComparison.InvariantCulture));
                    routeCnt -= 1;
                } while ((routeCnt > 0) && (!routeFound));
                if (!routeFound) { return false; }
                //
                // -- execute route
                RouteMapModel.RouteClass route = core.routeMap.routeDictionary[routeTest];
                switch (route.routeType) {
                    case RouteMapModel.RouteTypeEnum.admin: {
                            //
                            // -- admin site, force platform to 4 until layouts upgraded
                            core.siteProperties.htmlPlatformOverride = 5;
                            //
                            AddonModel addon = core.cacheRuntime.addonCache.create(addonGuidAdminSite);
                            if (addon == null) {
                                logger.Error($"{core.logCommonMessage}", new GenericException("The admin site addon could not be found by guid [" + addonGuidAdminSite + "]."));
                                returnResult = "The default admin site addon could not be found. Please run an upgrade on this application to restore default services (command line> cc -a appName -r )";
                                return true;
                            } else {
                                returnResult = core.addon.execute(addon, new CPUtilsBaseClass.addonExecuteContext {
                                    addonType = CPUtilsBaseClass.addonContext.ContextAdmin,
                                    errorContextMessage = "calling admin route [" + addonGuidAdminSite + "] during execute route method"
                                });
                                return true;
                            }
                        }
                    case RouteMapModel.RouteTypeEnum.remoteMethod: {
                            //
                            // -- remote method
                            AddonModel addon = core.cacheRuntime.addonCache.create(route.remoteMethodAddonId);
                            if (addon == null) {
                                logger.Error($"{core.logCommonMessage}", new GenericException("The addon for remoteMethodAddonId [" + route.remoteMethodAddonId + "] could not be opened."));
                                return true;
                            }
                            CPUtilsBaseClass.addonExecuteContext executeContext = new CPUtilsBaseClass.addonExecuteContext {
                                addonType = CPUtilsBaseClass.addonContext.ContextRemoteMethodJson,
                                cssContainerClass = "",
                                cssContainerId = "",
                                hostRecord = new CPUtilsBaseClass.addonExecuteHostRecordContext {
                                    contentName = core.docProperties.getText("hostcontentname"),
                                    fieldName = "",
                                    recordId = core.docProperties.getInteger("HostRecordID")
                                },
                                errorContextMessage = "calling remote method addon [" + route.remoteMethodAddonId + "] during execute route method"
                            };
                            returnResult = core.addon.execute(addon, executeContext);
                            return true;
                        }
                    case RouteMapModel.RouteTypeEnum.linkAlias: {
                            //
                            // - link alias
                            // -- either redirect to the most current route for an entry, or setup doc properties for the default route
                            // -- all the query string values have already been added to doc properties, so do not over write them.
                            // -- consensus is that since the link alias (permalink, long-tail url, etc) comes first on the left, that the querystring should override
                            // -- so http://www.mySite.com/My-Blog-Post?bid=9 means use the bid not the bid from the link-alias
                            if (!string.IsNullOrEmpty(route.linkAliasRedirect)) {
                                core.webServer.redirect(route.linkAliasRedirect, "Page URL, older link forward to primary link.");
                                return true;
                            }
                            core.docProperties.setProperty("bid", route.linkAliasPageId);
                            foreach (NameValueModel nameValue in route.linkAliasQSList) {
                                core.docProperties.setProperty(nameValue.name, nameValue.value);
                            }
                            return false;
                        }
                    case RouteMapModel.RouteTypeEnum.linkForward: {
                            //
                            // -- link forward
                            LinkForwardModel linkForward = DbBaseModel.create<LinkForwardModel>(core.cpParent, route.linkForwardId);
                            returnResult = core.webServer.redirect(linkForward.destinationLink, "Link Forward #" + linkForward.id + ", " + linkForward.name);
                            return true;
                        }
                    default: {
                            return false;
                        }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //
        /// <summary>
        /// legacy method=login
        /// </summary>
        /// <param name="core"></param>
        /// <param name="route"></param>
        /// <param name="returnResult"></param>
        /// <returns></returns>
        public static bool tryExecuteMethodRoute(CoreController core, string route, ref string returnResult) {
            try {
                //
                // -- legacy methods=
                string HardCodedPage = core.docProperties.getText(RequestNameHardCodedPage);
                if (string.IsNullOrEmpty(HardCodedPage)) { return false; }
                switch (GenericController.toLCase(HardCodedPage)) {
                    case HardCodedPageLogout: {
                            //
                            // -- logout intercept
                            (new Contensive.Processor.Addons.Primitives.ProcessLogoutMethodClass()).Execute(core.cpParent);
                            //
                            // -- redirect to the route without the method
                            string routeWithoutQuery = modifyLinkQuery(core.webServer.requestUrlSource, "method", "", false);
                            core.webServer.redirect(routeWithoutQuery, "Redirect to route without 'method' because login was successful.");
                            returnResult = string.Empty;
                            return true;
                        }
                    case HardCodedPageResourceLibrary: {
                            //
                            returnResult = (new Contensive.Processor.Addons.Primitives.ProcessResourceLibraryMethodClass()).Execute(core.cpParent).ToString();
                            return true;
                        }
                    case HardCodedPageLoginDefault: {
                            //
                            if (core.session.isAuthenticated) {
                                //
                                // -- if authenticated, redirect to the route without the method
                                string routeWithoutQuery = modifyLinkQuery(core.webServer.requestUrlSource, "method", "", false);
                                core.webServer.redirect(routeWithoutQuery, "Redirect to route without 'method' because login was successful.");
                                returnResult = string.Empty;
                            }
                            //
                            // -- default login page
                            returnResult = core.addon.execute(addonGuidLoginPage, new CPUtilsBaseClass.addonExecuteContext {
                                addonType = CPUtilsBaseClass.addonContext.ContextPage,
                                argumentKeyValuePairs = new Dictionary<string, string> {
                                            { "Force Default Login", "true" }
                                        },
                                forceHtmlDocument = true,
                                errorContextMessage = "executing method=loginDefault"
                            });
                            return true;
                        }
                    case HardCodedPageLogin: {
                            //
                            if (core.session.isAuthenticated) {
                                //
                                // -- if authenticated, redirect to the route without the method
                                string routeWithoutQuery = modifyLinkQuery(core.webServer.requestUrlSource, "method", "", false);
                                core.webServer.redirect(routeWithoutQuery, "Redirect to route without 'method' because login was successful.");
                                returnResult = string.Empty;
                            }
                            //
                            // -- process the login method, or return the login form
                            core.doc.continueProcessing = false;
                            returnResult = core.addon.execute(core.cacheRuntime.addonCache.create(addonGuidLoginPage), new CPUtilsBaseClass.addonExecuteContext {
                                addonType = CPUtilsBaseClass.addonContext.ContextPage,
                                argumentKeyValuePairs = new Dictionary<string, string> {
                                            { "Force Default Login", "false" }
                                        },
                                forceHtmlDocument = true,
                                errorContextMessage = "executing method=login"
                            });
                            return true;
                        }
                    case HardCodedPageLogoutLogin: {
                            //
                            returnResult = (new Contensive.Processor.Addons.Primitives.ProcessLogoutLoginMethodClass()).Execute(core.cpParent).ToString();
                            return true;
                        }
                    case HardCodedPageSiteExplorer: {
                            //
                            returnResult = (new Contensive.Processor.Addons.Primitives.ProcessSiteExplorerMethodClass()).Execute(core.cpParent).ToString();
                            return true;
                        }
                    case HardCodedPageStatus: {
                            //
                            returnResult = (new Contensive.Processor.Addons.Diagnostics.StatusClass()).Execute(core.cpParent).ToString();
                            return true;
                        }
                    case HardCodedPageRedirect: {
                            //
                            returnResult = (new Contensive.Processor.Addons.Primitives.ProcessRedirectMethodClass()).Execute(core.cpParent).ToString();
                            return true;
                        }
                    case HardCodedPageExportAscii: {
                            //
                            returnResult = (new Contensive.Processor.Addons.Primitives.ProcessExportAsciiMethodClass()).Execute(core.cpParent).ToString();
                            return true;
                        }
                    default: {
                            return false;
                        }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //
        /// <summary>
        /// try built in form process. 
        /// Here, the output will be added to the result for true or false return
        /// true return means page is complete 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="normalizedRoute"></param>
        /// <param name="returnResult"></param>
        /// <returns></returns>
        public static void processBuiltInForms(CoreController core, string userErrorMessage) {
            try {
                string formType = core.docProperties.getText(core.docProperties.getText("ccformsn") + "type");
                if (string.IsNullOrEmpty(formType)) { return; }
                //
                // set the meta content flag to show it is not needed for the head tag
                switch (formType) {
                    case FormTypeLogin:
                    case "l09H58a195": {
                            //
                            string requestUsername = core.cpParent.Doc.GetText("username");
                            string requestPassword = core.cpParent.Doc.GetText("password");
                            bool requestIncludesPassword = core.cpParent.Doc.IsProperty("password");
                            LoginWorkflowController.processLogin(core, requestUsername, requestPassword, requestIncludesPassword, ref userErrorMessage);
                            return;
                        }
                    case FormTypeToolsPanel: {
                            //
                            (new Contensive.Processor.Addons.Primitives.processFormToolsPanelClass()).Execute(core.cpParent);
                            return;
                        }
                    case FormTypeActiveEditor: {
                            //
                            (new Contensive.Processor.Addons.Primitives.ProcessActiveEditorClass()).Execute(core.cpParent);
                            return;
                        }
                    case FormTypeSiteStyleEditor: {
                            //
                            (new Contensive.Processor.Addons.Primitives.ProcessSiteStyleEditorClass()).Execute(core.cpParent);
                            return;
                        }
                    case FormTypeHelpBubbleEditor: {
                            //
                            (new Contensive.Processor.Addons.Primitives.processHelpBubbleEditorClass()).Execute(core.cpParent);
                            return;
                        }
                    case FormTypeRegister: {
                            //
                            RegisterController.processRegisterForm(core);
                            return;
                        }
                    default: {
                            return;
                        }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //
        /// <summary>
        /// legacy built in ajax functions.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="normalizedRoute"></param>
        /// <param name="returnResult"></param>
        /// <returns></returns>
        public static bool tryExecuteAjaxfnRoute(CoreController core, string normalizedRoute, ref string returnResult) {
            try {
                string AjaxFunction = core.docProperties.getText(RequestNameAjaxFunction);
                if (string.IsNullOrEmpty(AjaxFunction)) { return false; }
                //
                // -- Need to be converted to Url parameter addons
                switch ((AjaxFunction)) {
                    case AjaxSetVisitProperty: {
                            //
                            // moved to Addons.AdminSite
                            returnResult = (new Contensive.Processor.Addons.AdminSite.SetAjaxVisitPropertyClass()).Execute(core.cpParent).ToString();
                            return true;
                        }
                    case AjaxGetVisitProperty: {
                            //
                            // moved to Addons.AdminSite
                            returnResult = (new Contensive.Processor.Addons.AdminSite.GetAjaxVisitPropertyClass()).Execute(core.cpParent).ToString();
                            return true;
                        }
                    case AjaxData: {
                            //
                            // moved to Addons.AdminSite
                            returnResult = (new Contensive.Processor.Addons.AdminSite.ProcessAjaxDataClass()).Execute(core.cpParent).ToString();
                            return true;
                        }
                    case AjaxOpenIndexFilter: {
                            //
                            // moved to Addons.AdminSite
                            returnResult = (new Contensive.Processor.Addons.AdminSite.OpenAjaxIndexFilterClass()).Execute(core.cpParent).ToString();
                            return true;
                        }
                    case AjaxOpenIndexFilterGetContent: {
                            //
                            // moved to Addons.AdminSite
                            returnResult = (new Contensive.Processor.Addons.AdminSite.OpenAjaxIndexFilterGetContentClass()).Execute(core.cpParent).ToString();
                            return true;
                        }
                    case AjaxCloseIndexFilter: {
                            //
                            // moved to Addons.AdminSite
                            returnResult = (new Contensive.Processor.Addons.AdminSite.CloseAjaxIndexFilterClass()).Execute(core.cpParent).ToString();
                            return true;
                        }
                    default: {
                            //
                            // -- unknown method, log warning
                            returnResult = string.Empty;
                            return true;
                        }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}