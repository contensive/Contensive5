<%@ Page Language="C#" %>
<script runat="server">
    void Page_Load() {
        //
        // -- DO NOT edit. This file is replaced during upgrade
        //
        Contensive.Processor.Controllers.LogController.logShortLine("Page_Load", Contensive.BaseClasses.CPLogBaseClass.LogLevel.Trace);
        try {
            if (HttpContext.Current.Request.HttpMethod != "OPTIONS") {
                //
                // -- not preflight options call, get content
                //
                // -- initialize with contensive d:\contensive\serverConfig.json (use same settings as cli and services)
                string appName = ConfigurationClass.getAppName();
                var context = ConfigurationClass.buildContext(appName, HttpContext.Current);
                using (var cp = new Contensive.Processor.CPClass(appName, context)) {
                    //
                    // -- if disabled, exit
                    if (!cp.appOk) {
                        Response.Write("The application [" + appName + "] is currently disabled.");
                        return;
                    }
                    //
                    // -- execute code ------------------------------------------------
                    string content = cp.executeRoute();
                    // -- /execute code ------------------------------------------------
                    //
                    // -- exit now if response headers sent. This technique is used to write binary
                    if (Response.HeadersWritten) { return; }
                    //
                    // -- delete uploaded temp files in request
                    foreach (var file in context.Request.Files) {
                        DefaultSite.WindowsTempFileController.deleteTmpFile(file.windowsTempfilename);
                    }
                    //
                    foreach (Contensive.Processor.Models.Domain.HttpContextResponseHeader header in context.Response.headers) {
                        Response.Headers.Add(header.name, header.value);
                    }
                    //
                    // -- write Set-Cookie headers directly instead of using HttpCookie/AppendCookie.
                    //    ASP.NET's HttpCookie serializer emits a legacy hyphenated Expires format
                    //    (dd-MMM-yyyy) that some browsers will not persist reliably. Writing the
                    //    header directly lets us emit RFC 1123 dates and Max-Age, which RFC 6265
                    //    says takes precedence over Expires.
                    foreach (KeyValuePair<string, Contensive.Processor.Models.Domain.HttpContextResponseCookie> cookie in context.Response.cookies) {
                        var sb = new System.Text.StringBuilder();
                        sb.Append(cookie.Key);
                        sb.Append("=");
                        sb.Append(cookie.Value.value ?? "");
                        if (!string.IsNullOrEmpty(cookie.Value.path)) {
                            sb.Append("; Path=");
                            sb.Append(cookie.Value.path);
                        } else {
                            sb.Append("; Path=/");
                        }
                        if (!string.IsNullOrEmpty(cookie.Value.domain)) {
                            sb.Append("; Domain=");
                            sb.Append(cookie.Value.domain);
                        }
                        if (cookie.Value.expires != DateTime.MinValue) {
                            //
                            // -- Max-Age (seconds) takes precedence per RFC 6265 and cannot be misparsed
                            long maxAgeSeconds = (long)(cookie.Value.expires.ToUniversalTime() - DateTime.UtcNow).TotalSeconds;
                            if (maxAgeSeconds < 0) {
                                maxAgeSeconds = 0;
                            }
                            sb.Append("; Max-Age=");
                            sb.Append(maxAgeSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture));
                            //
                            // -- RFC 1123 Expires as a fallback for clients that do not honor Max-Age
                            sb.Append("; Expires=");
                            sb.Append(cookie.Value.expires.ToUniversalTime().ToString("R", System.Globalization.CultureInfo.InvariantCulture));
                        }
                        if (cookie.Value.httpOnly) {
                            sb.Append("; HttpOnly");
                        }
                        if (cookie.Value.secure) {
                            sb.Append("; Secure");
                        }
                        //
                        // -- HttpContextResponseCookieSameSiteMode currently only defines Lax
                        sb.Append("; SameSite=Lax");
                        Response.Headers.Add("Set-Cookie", sb.ToString());
                    }
                    //
                    Response.ContentType = context.Response.contentType;
                    Response.CacheControl = context.Response.cacheControl;
                    Response.Status = context.Response.status;
                    Response.Expires = context.Response.expires;
                    Response.Buffer = context.Response.buffer;
                    //
                    // -- transfer response to webserver
                    if (!string.IsNullOrEmpty(context.Response.redirectUrl)) {
                        //
                        // -- perform redirect. Do not exit because reload required if route change
                        if (context.Response.status.Contains("301")) {
                            Response.RedirectPermanent(context.Response.redirectUrl, false);
                        } else {
                            Response.Redirect(context.Response.redirectUrl, false);
                        }
                    } else {
                        //
                        // -- write content body to webserver
                        Response.Write(content);
                    }
                    //
                    // -- if routeMap changed, unload app domain
                    if (ConfigurationClass.routeMapDateInvalid() || (cp.routeMap.dateCreated != (DateTime)HttpContext.Current.Application["RouteMapDateCreated"])) {
                        HttpRuntime.UnloadAppDomain();
                    }
                    //
                    // -- if a collection install (or similar) requested recycle, unload app domain
                    if (context.Response.requestRecycle) {
                        HttpRuntime.UnloadAppDomain();
                    }
                }
            }
            //
            // -- setup CORS if not present
            // -- option 1
            //    - set CORS programmatically and allow this to handle option verb when code is not executed (preflight options for ex)
            //    - to set origin, use webconfig AppSettings section. Set DefaultCORSAllowOrigin (see web.config for details)
            // -- option 2
            //    - add all CORS response to customweb.config.  (see web.config for details)
            if (!HttpContext.Current.Response.Headers.AllKeys.Contains("Access-Control-Allow-Origin")) {
                string allowOrigin = "";
                string allowOriginList = ConfigurationManager.AppSettings["DefaultCORSAllowOrigins"];
                if (!string.IsNullOrEmpty(allowOriginList)) {
                    string[] allowOrigins = allowOriginList.Split(',');
                    string origin = Request.Headers["Origin"];
                    //
                    allowOrigin = allowOrigins.Contains(origin) ? origin : ConfigurationManager.AppSettings["DefaultCORSAllowOrigin"];
                }
                allowOrigin = string.IsNullOrEmpty(allowOrigin) ? "*" : allowOrigin;
                HttpContext.Current.Response.Headers.Set("Access-Control-Allow-Origin", allowOrigin);

                Trace.Write("PageLoad, allowOrigin [" + allowOrigin + "]");
                // System.Diagnostics.Trace.WriteLine("Error in Widget 42");
                // Trace.Flush();
                //
                if (allowOrigin != "*") {
                    HttpContext.Current.Response.Headers.Set("Access-Control-Allow-Credentials", "true");
                }
                //
                string allowMethods = ConfigurationManager.AppSettings["DefaultCORSAllowMethods"];
                HttpContext.Current.Response.Headers.Set("Access-Control-Allow-Methods", string.IsNullOrEmpty(allowMethods) ? "GET,PUT,POST,DELETE,PATCH,OPTIONS" : allowMethods);
                //
                string allowHeaders = ConfigurationManager.AppSettings["DefaultCORSAllowHeaders"];
                HttpContext.Current.Response.Headers.Set("Access-Control-Allow-Headers", string.IsNullOrEmpty(allowHeaders) ? "Origin,X-Requested-With,Content-Type,soapaction,Accept,Authorization" : allowHeaders);
            }
        } catch (Exception) {
        } finally {
        }
    }
</script>
