//using System;
//using System.Collections.Generic;
//using System.Collections.Specialized;
//using System.Configuration;
//using System.Web;
////using System.Web.Routing;
//using Contensive.Processor;
//using Contensive.Processor.Controllers;
//using Contensive.Processor.Models.Domain;
//using Microsoft.VisualBasic.CompilerServices;

using Amazon.Runtime.Internal.Transform;
using Amazon.SimpleEmail;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.ClearScript;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Input;

namespace Contensive.WebApi {
    public class ConfigurationClass {
        //    // 
        //    // ====================================================================================================
        //    /// <summary>
        //    /// if true, the route map is not loaded or invalid and needs to be loaded
        //    /// </summary>
        //    /// <returns></returns>
        //    public static bool routeMapDateInvalid() {
        //        if (HttpContext.Current.Application["RouteMapDateCreated"] is null)
        //            return true;
        //        DateTime dateResult;
        //        return !DateTime.TryParse(HttpContext.Current.Application["RouteMapDateCreated"].ToString(), out dateResult);
        //    }
        //    // 
        //    // ====================================================================================================
        //    /// <summary>
        //    /// determine the Contensive application name from the webconfig or iis sitename
        //    /// </summary>
        //    /// <returns></returns>
        //    public static string getAppName() {
        //        // 
        //        // -- app name matches iis site name unless overridden by aspx app setting "ContensiveAppName"
        //        string appName = System.Configuration.ConfigurationManager.AppSettings["ContensiveAppName"] ?? "c5test";
        //        //if (string.IsNullOrEmpty(appName)) {
        //        //    appName = System.Web.Hosting.HostingEnvironment.ApplicationHost.GetSiteName();
        //        //}
        //        return appName;
        //    }
        //    // 
        //    // ====================================================================================================
        //    /// <summary>
        //    /// verify the routemap is not stale. This was the legacy reload process that reloads without an application load.
        //    /// </summary>
        //    /// <param name="cp"></param>
        //    public static void verifyRouteMap(CPClass cp) {
        //        // 
        //        // -- if application var does not equal routemap.datecreated rebuild
        //        if (routeMapDateInvalid() || cp.routeMap.dateCreated != Conversions.ToDate(HttpContext.Current.Application["RouteMapDateCreated"])) {
        //            if (routeMapDateInvalid()) {
        //                LogController.logShortLine("configurationClass, loadRouteMap, [" + cp.Site.Name + "], rebuild because HttpContext.Current.Application(RouteMapDateCreated) is not valid", Contensive.BaseClasses.CPLogBaseClass.LogLevel.Info);
        //            } else {
        //                LogController.logShortLine("configurationClass, loadRouteMap, [" + cp.Site.Name + "], rebuild because not equal, cp.routeMap.dateCreated [" + cp.routeMap.dateCreated.ToString() + "], HttpContext.Current.Application(RouteMapDateCreated) [" + HttpContext.Current.Application["RouteMapDateCreated"].ToString() + "]", Contensive.BaseClasses.CPLogBaseClass.LogLevel.Info);
        //            }
        //            loadRouteMap(cp);
        //        }
        //    }
        //    // 
        //    // ====================================================================================================
        //    /// <summary>
        //    /// load the routemap
        //    /// </summary>
        //    /// <param name="cp"></param>
        //    public static void loadRouteMap(CPClass cp) {
        //        lock (RouteTable.Routes) {
        //            // 
        //            LogController.logShortLine("configurationClass, loadRouteMap enter, [" + cp.Site.Name + "]", Contensive.BaseClasses.CPLogBaseClass.LogLevel.Trace);
        //            // 
        //            HttpContext.Current.Application["routeMapDateCreated"] = cp.routeMap.dateCreated;
        //            // 
        //            RouteTable.Routes.Clear();
        //            foreach (var newRouteKeyValuePair in cp.routeMap.routeConcurrentDictionary) {
        //                try {
        //                    RouteTable.Routes.Remove(RouteTable.Routes[newRouteKeyValuePair.Key]);
        //                    RouteTable.Routes.MapPageRoute(newRouteKeyValuePair.Value.virtualRoute, newRouteKeyValuePair.Value.virtualRoute, newRouteKeyValuePair.Value.physicalRoute);
        //                } catch (Exception ex) {
        //                    cp.Site.ErrorReport(ex, "Unexpected exception adding virtualRoute, key [" + newRouteKeyValuePair.Key + "], route [" + newRouteKeyValuePair.Value.virtualRoute + "]");
        //                }
        //            }
        //        }
        //        // 
        //        LogController.logShortLine("configurationClass, loadRouteMap exit, [" + cp.Site.Name + "]", Contensive.BaseClasses.CPLogBaseClass.LogLevel.Info);
        //        // 
        //    }
        // 
        // ====================================================================================================

        /// <summary>
        /// build the http context from the iis httpContext object
        /// </summary>
        /// <param name="appName"></param>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        public static HttpContextModel buildContext(string appName, HttpContext httpContext) {
            try {
                var context = new HttpContextModel();

                if (httpContext is null || httpContext.Request is null || httpContext.Response is null) {
                    //LogController.logShortLine("ConfigurationClass.buildContext - Attempt to initialize webContext but iisContext or one of its objects is null., [" + appName + "]", Contensive.BaseClasses.CPLogBaseClass.LogLevel.Fatal);
                    throw new ApplicationException("ConfigurationClass.buildContext - Attempt to initialize webContext but iisContext or one of its objects is null., [" + appName + "]");
                }
                // 
                // -- set default response
                context.Response.cacheControl = "no-cache";
                context.Response.expires = -1;
                context.Response.buffer = true;
                // 
                // -- setup request
                httpContext.Request.EnableBuffering();
                using (StreamReader reader = new StreamReader(httpContext.Request.Body, Encoding.UTF8, true, 1024, true)) {
                    context.Request.requestBody = reader.ReadToEndAsync().Result;
                    //context.Request.requestBody = reader.ReadToEnd();
                }
                httpContext.Request.Body.Position = 0;

                context.Request.ContentType = httpContext.Request.ContentType;
                Uri requestUri = new(httpContext.Request.GetEncodedUrl());
                context.Request.Url = new HttpContentRequestUrl() {
                    AbsoluteUri = requestUri.Scheme + "://" + requestUri.Host + requestUri.PathAndQuery,
                    Port = requestUri.Port
                };
                string uriString = httpContext.Request.Headers["Referer"].ToString();
                context.Request.UrlReferrer = string.IsNullOrEmpty(uriString) ? null : new Uri(uriString);
                // 
                context.Request.RawUrl = httpContext.Request.GetEncodedUrl();
                //
                // todo - map these to their own request arguments, remove servervariables
                //
                int? requestPort = httpContext.Request.Host.Port;
                Uri? referrerUri = httpContext.Request.GetTypedHeaders().Referer;
                string requestReferrer = referrerUri == null ? "" : "";
                var requestBrowserLang = httpContext.Request.Headers["Accept-Language"].ToString().Split(";").FirstOrDefault()?.Split(",").FirstOrDefault();
                IPAddress? remoteIPAddressObj = httpContext.Connection.RemoteIpAddress;
                string remoteIPAddress = remoteIPAddressObj == null ? "" : remoteIPAddressObj.ToString();
                //
                context.Request.ServerVariables.Add("SCRIPT_NAME", httpContext.Request.Path);
                context.Request.ServerVariables.Add("SERVER_NAME", httpContext.Request.Host.Host);
                context.Request.ServerVariables.Add("HTTP_REFERER", requestReferrer);
                context.Request.ServerVariables.Add("SERVER_PORT_SECURE", (requestPort ?? 0) == 443 ? "true" : "false");
                context.Request.ServerVariables.Add("HTTP_X_FORWARDED_FOR", httpContext.GetServerVariable("HTTP_X_FORWARDED_FOR"));
                context.Request.ServerVariables.Add("REMOTE_ADDR", remoteIPAddress);
                context.Request.ServerVariables.Add("HTTP_USER_AGENT", httpContext.Request.Headers["User-Agent"].ToString());
                context.Request.ServerVariables.Add("HTTP_ACCEPT_LANGUAGE", requestBrowserLang);
                // 
                // -- request headers
                foreach (var header in httpContext.Request.Headers) {
                    context.Request.Headers.Add(header.Key, header.Value);
                }
                // 
                // -- request querystring
                if (!string.IsNullOrEmpty(httpContext.Request.QueryString.Value)) {
                    string qs = httpContext.Request.QueryString.Value;
                    if (qs.Substring(0, 1).Equals("?")) { qs = qs.Substring(1); }
                    if (!string.IsNullOrEmpty(qs)) {
                        foreach (var nvPair in qs.Split('&')) {
                            string[] keyValue = nvPair.Split('=');
                            if (string.IsNullOrEmpty(keyValue[0])) { continue; }
                            if (keyValue.Length > 1) {
                                string qsValue = WebUtility.UrlDecode(keyValue[1]);
                                context.Request.QueryString.Add(keyValue[0], qsValue);
                            } else {
                                context.Request.QueryString.Add(keyValue[0], "");
                            }
                        }
                    }
                }
                // 
                // -- request form
                if (context.Request.Headers.ContainsKey("content-type")) {
                    if (httpContext.Request.Form is not null) {
                        foreach (var fc in httpContext.Request.Form) {
                            if (!string.IsNullOrEmpty(fc.Key)) {
                                if (!context.Request.Form.ContainsKey(fc.Key)) { context.Request.Form.Add(fc.Key, fc.Value); }
                            }
                        }
                        // 
                        // -- transfer upload files
                        foreach (var formFile in httpContext.Request.Form.Files) {
                            if (formFile.Length > 0) {
                                string tmpFullPathFilename = WindowsTempFileController.createTmpFile();
                                using (var inputStream = new FileStream(tmpFullPathFilename, FileMode.Create)) {
                                    // read file to stream
                                    formFile.CopyTo(inputStream);
                                    // stream to byte array
                                    byte[] array = new byte[inputStream.Length];
                                    inputStream.Seek(0, SeekOrigin.Begin);
                                    inputStream.Read(array, 0, array.Length);
                                    string normalizedFilename = Processor.Controllers.FileController.normalizeDosFilename(formFile.FileName);
                                    if (string.IsNullOrWhiteSpace(normalizedFilename)) { continue; }
                                    context.Request.Files.Add(new DocPropertyModel() {
                                        name = formFile.Name,
                                        value = normalizedFilename,
                                        nameValue = Uri.EscapeDataString(formFile.Name) + "=" + Uri.EscapeDataString(normalizedFilename),
                                        windowsTempfilename = tmpFullPathFilename,
                                        propertyType = DocPropertyModel.DocPropertyTypesEnum.@file
                                    });
                                }
                            }
                        }
                    }
                }
                //foreach (string key in httpContext.Request.Form .AllKeys) {
                //    if (string.IsNullOrWhiteSpace(key))
                //        continue;
                //    var @file = httpContext.Request.Files[key];
                //    if (@file is null)
                //        continue;
                //    if (@file.ContentLength == 0)
                //        continue;
                //    string normalizedFilename = Processor.Controllers.FileController.normalizeDosFilename(@file.FileName);
                //    if (string.IsNullOrWhiteSpace(normalizedFilename))
                //        continue;
                //    string windowsTempFile = WindowsTempFileController.createTmpFile();
                //    @file.SaveAs(windowsTempFile);
                //    context.Request.Files.Add(new DocPropertyModel() {
                //        name = key,
                //        value = normalizedFilename,
                //        nameValue = Uri.EscapeDataString(key) + "=" + Uri.EscapeDataString(normalizedFilename),
                //        windowsTempfilename = windowsTempFile,
                //        propertyType = DocPropertyModel.DocPropertyTypesEnum.@file
                //    });
                //}
                // 
                // -- transfer cookies
                foreach (string cookieKey in httpContext.Request.Cookies.Keys) {
                    if (string.IsNullOrWhiteSpace(cookieKey))
                        continue;
                    if (context.Request.Cookies.ContainsKey(cookieKey))
                        context.Request.Cookies.Remove(cookieKey);
                    context.Request.Cookies.Add(cookieKey, new HttpContextRequestCookie() {
                        Name = cookieKey,
                        Value = httpContext.Request.Cookies[cookieKey]
                    });
                }
                // 
                return context;
            } catch (Exception) {
                //LogController.logShortLine("ConfigurationClass.buildContext exception, [" + ex.ToString() + "]", Contensive.BaseClasses.CPLogBaseClass.LogLevel.Fatal);
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
                string value = nameValues.Get(i) ?? "";
                if (skipEmptyValues && string.IsNullOrWhiteSpace(value))
                    continue;
                string key = nameValues.GetKey(i) ?? "";
                if (string.IsNullOrWhiteSpace(key))
                    continue;
                if (store.ContainsKey(key))
                    store.Remove(key);
                store.Add(key, value);
            }
        }
    }
}