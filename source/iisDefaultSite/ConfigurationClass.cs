using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Web;
using System.Web.Routing;
using Contensive.Processor;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using Microsoft.VisualBasic.CompilerServices;

public class ConfigurationClass {
    // 
    // ====================================================================================================
    /// <summary>
    /// if true, the route map is not loaded or invalid and needs to be loaded
    /// </summary>
    /// <returns></returns>
    public static bool routeMapDateInvalid() {
        if (HttpContext.Current.Application["RouteMapDateCreated"] is null)
            return true;
        DateTime dateResult;
        return !DateTime.TryParse(HttpContext.Current.Application["RouteMapDateCreated"].ToString(), out dateResult);
    }
    // 
    // ====================================================================================================
    /// <summary>
    /// determine the Contensive application name from the webconfig or iis sitename
    /// </summary>
    /// <returns></returns>
    public static string getAppName() {
        // 
        // -- app name matches iis site name unless overridden by aspx app setting "ContensiveAppName"
        string appName = ConfigurationManager.AppSettings["ContensiveAppName"];
        if (string.IsNullOrEmpty(appName)) {
            appName = System.Web.Hosting.HostingEnvironment.ApplicationHost.GetSiteName();
        }
        return appName;
    }
    // 
    // ====================================================================================================
    /// <summary>
    /// verify the routemap is not stale. This was the legacy reload process that reloads without an application load.
    /// </summary>
    /// <param name="cp"></param>
    public static void verifyRouteMap(CPClass cp) {
        // 
        // -- if application var does not equal routemap.datecreated rebuild
        if (routeMapDateInvalid() || cp.routeMap.dateCreated != Conversions.ToDate(HttpContext.Current.Application["RouteMapDateCreated"])) {
            if (routeMapDateInvalid()) {
                LogController.logShortLine("configurationClass, loadRouteMap, [" + cp.Site.Name + "], rebuild because HttpContext.Current.Application(RouteMapDateCreated) is not valid", Contensive.BaseClasses.CPLogBaseClass.LogLevel.Info);
            } else {
                LogController.logShortLine("configurationClass, loadRouteMap, [" + cp.Site.Name + "], rebuild because not equal, cp.routeMap.dateCreated [" + cp.routeMap.dateCreated.ToString() + "], HttpContext.Current.Application(RouteMapDateCreated) [" + HttpContext.Current.Application["RouteMapDateCreated"].ToString() + "]", Contensive.BaseClasses.CPLogBaseClass.LogLevel.Info);
            }
            loadRouteMap(cp);
        }
    }
    // 
    // ====================================================================================================
    /// <summary>
    /// load the routemap
    /// </summary>
    /// <param name="cp"></param>
    public static void loadRouteMap(CPClass cp) {
        lock (RouteTable.Routes) {
            // 
            LogController.logShortLine("configurationClass, loadRouteMap enter, [" + cp.Site.Name + "]", Contensive.BaseClasses.CPLogBaseClass.LogLevel.Trace);
            // 
            HttpContext.Current.Application["routeMapDateCreated"] = cp.routeMap.dateCreated;
            // 
            RouteTable.Routes.Clear();
            foreach (var newRouteKeyValuePair in cp.routeMap.routeDictionary) {
                try {
                    RouteTable.Routes.Remove(RouteTable.Routes[newRouteKeyValuePair.Key]);
                    RouteTable.Routes.MapPageRoute(newRouteKeyValuePair.Value.virtualRoute, newRouteKeyValuePair.Value.virtualRoute, newRouteKeyValuePair.Value.physicalRoute);
                } catch (Exception ex) {
                    cp.Site.ErrorReport(ex, "Unexpected exception adding virtualRoute, key [" + newRouteKeyValuePair.Key + "], route [" + newRouteKeyValuePair.Value.virtualRoute + "]");
                }
            }
        }
        // 
        LogController.logShortLine("configurationClass, loadRouteMap exit, [" + cp.Site.Name + "]", Contensive.BaseClasses.CPLogBaseClass.LogLevel.Info);
        // 
    }
    // 
    // ====================================================================================================
    /// <summary>
    /// build the http context from the iis httpContext object
    /// </summary>
    /// <param name="appName"></param>
    /// <param name="iisContext"></param>
    /// <returns></returns>
    public static HttpContextModel buildContext(string appName, HttpContext iisContext) {
        try {
            var context = new HttpContextModel();

            if (iisContext is null || iisContext.Request is null || iisContext.Response is null) {
                LogController.logShortLine("ConfigurationClass.buildContext - Attempt to initialize webContext but iisContext or one of its objects is null., [" + appName + "]", Contensive.BaseClasses.CPLogBaseClass.LogLevel.Fatal);
                throw new ApplicationException("ConfigurationClass.buildContext - Attempt to initialize webContext but iisContext or one of its objects is null., [" + appName + "]");
            }
            // 
            // -- set default response
            context.Response.cacheControl = "no-cache";
            context.Response.expires = -1;
            context.Response.buffer = true;
            // 
            // -- setup request
            iisContext.Request.InputStream.Position = 0L;
            context.Request.requestBody = new System.IO.StreamReader(iisContext.Request.InputStream).ReadToEnd();
            context.Request.ContentType = iisContext.Request.ContentType;
            context.Request.Url = new HttpContentRequestUrl() {
                AbsoluteUri = iisContext.Request.Url.Scheme + "://" + iisContext.Request.Url.Host + iisContext.Request.RawUrl,
                Port = iisContext.Request.Url.Port
            };
            context.Request.UrlReferrer = iisContext.Request.UrlReferrer;
            // 
            context.Request.RawUrl = iisContext.Request.RawUrl;
            // 
            // -- server variables
            storeNameValues(iisContext.Request.ServerVariables, context.Request.ServerVariables, true);
            // 
            // -- request headers
            storeNameValues(iisContext.Request.Headers, context.Request.Headers, true);
            // 
            // -- request querystring
            storeNameValues(iisContext.Request.QueryString, context.Request.QueryString, false);
            // 
            // -- request form
            storeNameValues(iisContext.Request.Form, context.Request.Form, false);
            // 
            // -- transfer upload files
            foreach (string key in iisContext.Request.Files.AllKeys) {
                if (string.IsNullOrWhiteSpace(key))
                    continue;
                var @file = iisContext.Request.Files[key];
                if (@file is null)
                    continue;
                if (@file.ContentLength == 0)
                    continue;
                string normalizedFilename = FileController.normalizeDosFilename(@file.FileName);
                if (string.IsNullOrWhiteSpace(normalizedFilename))
                    continue;
                string windowsTempFile = global::DefaultSite.WindowsTempFileController.createTmpFile();
                @file.SaveAs(windowsTempFile);
                context.Request.Files.Add(new DocPropertyModel() {
                    name = key,
                    value = normalizedFilename,
                    nameValue = Uri.EscapeDataString(key) + "=" + Uri.EscapeDataString(normalizedFilename),
                    windowsTempfilename = windowsTempFile,
                    propertyType = DocPropertyModel.DocPropertyTypesEnum.@file
                });
            }
            // 
            // -- transfer cookies
            foreach (string cookieKey in iisContext.Request.Cookies.Keys) {
                if (string.IsNullOrWhiteSpace(cookieKey))
                    continue;
                if (context.Request.Cookies.ContainsKey(cookieKey))
                    context.Request.Cookies.Remove(cookieKey);
                context.Request.Cookies.Add(cookieKey, new HttpContextRequestCookie() {
                    Name = cookieKey,
                    Value = iisContext.Request.Cookies[cookieKey].Value
                });
            }
            // 
            return context;
        } catch (Exception ex) {
            LogController.logShortLine("ConfigurationClass.buildContext exception, [" + ex.ToString() + "]", Contensive.BaseClasses.CPLogBaseClass.LogLevel.Fatal);
            throw;
        }
    }
    // 
    // ====================================================================================================
    /// <summary>
    /// Store NameValueCollection to a dictionary of string,string
    /// </summary>
    /// <param name="nameValues"></param>
    /// <param name="store"></param>
    public static void storeNameValues(NameValueCollection nameValues, Dictionary<string, string> store, bool skipEmptyValues) {
        for (int i = 0, loopTo = nameValues.Count - 1; i <= loopTo; i++) {
            string value = nameValues.Get(i);
            if (skipEmptyValues && string.IsNullOrWhiteSpace(value))
                continue;
            string key = nameValues.GetKey(i);
            if (string.IsNullOrWhiteSpace(key))
                continue;
            if (store.ContainsKey(key))
                store.Remove(key);
            store.Add(key, value);
        }
    }
}