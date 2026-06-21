

using System.IO;
using Microsoft.Extensions.FileProviders;
using Contensive.Processor.Models.Domain;

namespace Contensive.WebApi {
    internal class Program {
        /// <summary>
        /// Resolve the Contensive app name from available sources (no HttpContext required).
        /// Priority: appsettings.json → APP_POOL_ID env var → CONTENSIVE_APPNAME env var
        /// </summary>
        private static string resolveAppNameAtStartup(IConfiguration configuration) {
            string appName = configuration["Contensive:AppName"] ?? "";
            if (string.IsNullOrEmpty(appName)) {
                appName = Environment.GetEnvironmentVariable("APP_POOL_ID") ?? "";
            }
            if (string.IsNullOrEmpty(appName)) {
                appName = Environment.GetEnvironmentVariable("CONTENSIVE_APPNAME") ?? "";
            }
            return appName;
        }
        /// <summary>
        /// Resolve the Contensive app name from available sources (with HttpContext).
        /// Priority: appsettings.json → APP_POOL_ID server var → SERVER_NAME → CONTENSIVE_APPNAME env var
        /// </summary>
        private static string resolveAppName(IConfiguration configuration, HttpContext iisContext) {
            string appName = configuration["Contensive:AppName"] ?? "";
            if (string.IsNullOrEmpty(appName)) {
                appName = iisContext.GetServerVariable("APP_POOL_ID") ?? "";
            }
            if (string.IsNullOrEmpty(appName)) {
                appName = iisContext.GetServerVariable("SERVER_NAME") ?? "";
            }
            if (string.IsNullOrEmpty(appName)) {
                appName = Environment.GetEnvironmentVariable("CONTENSIVE_APPNAME") ?? "";
            }
            return appName;
        }
        private static void Main(string[] args) {
            //
            // -- read appsettings early to resolve app name and Contensive config
            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: true).Build();
            string startupAppName = resolveAppNameAtStartup(config);
            Console.WriteLine($"[Contensive] startup appName resolved to: [{startupAppName}]");
            //
            // -- resolve WebRootPath from Contensive config (localWwwPath).
            // -- For converted ASPX sites, this is the www folder (e.g., D:\inetpub\sprint10\www\).
            // -- appsettings.json Contensive:WebRootPath overrides if set explicitly.
            string webRootPath = config["Contensive:WebRootPath"] ?? "";
            string cdnRequestPath = "";
            string cdnPhysicalPath = "";
            bool configureCdn = false;
            if (!string.IsNullOrEmpty(startupAppName)) {
                try {
                    using (var cp = new Contensive.Processor.CPClass(startupAppName)) {
                        var serverConfig = cp.ServerConfig;
                        var appConfig = cp.GetAppConfig();
                        if (serverConfig != null && appConfig != null) {
                            //
                            // -- use localWwwPath as WebRootPath if not overridden in appsettings
                            if (string.IsNullOrEmpty(webRootPath)) {
                                webRootPath = appConfig.localWwwPath ?? "";
                                Console.WriteLine($"[Contensive] WebRootPath from config: [{webRootPath}]");
                            }
                            //
                            // -- prepare CDN file serving config
                            if (serverConfig.isLocalFileSystem) {
                                cdnRequestPath = appConfig.cdnFileUrl ?? "";
                                cdnPhysicalPath = appConfig.localFilesPath ?? "";
                                Console.WriteLine($"[Contensive] CDN config: requestPath=[{cdnRequestPath}], physicalPath=[{cdnPhysicalPath}], exists=[{Directory.Exists(cdnPhysicalPath)}]");
                                if (!string.IsNullOrEmpty(cdnRequestPath) && !string.IsNullOrEmpty(cdnPhysicalPath) && Directory.Exists(cdnPhysicalPath)) {
                                    if (!cdnRequestPath.StartsWith("/")) { cdnRequestPath = $"/{cdnRequestPath}"; }
                                    cdnRequestPath = cdnRequestPath.TrimEnd('/');
                                    configureCdn = true;
                                }
                            } else {
                                Console.WriteLine($"[Contensive] CDN static files NOT configured: isLocalFileSystem=[{serverConfig.isLocalFileSystem}]");
                            }
                        }
                    }
                } catch (Exception ex) {
                    Console.Error.WriteLine($"Warning: could not read Contensive config for app [{startupAppName}]: {ex.Message}");
                }
            }
            //
            // -- fall back to "." if no WebRootPath resolved (binary folder = www folder)
            if (string.IsNullOrEmpty(webRootPath)) { webRootPath = "."; }
            Console.WriteLine($"[Contensive] WebRootPath: [{webRootPath}]");
            //
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions {
                Args = args,
                WebRootPath = webRootPath
            });
            //
            // -- when running standalone (dotnet run, Kestrel), set the listen url from config.
            // -- when hosted by IIS in-process, UseUrls is ignored because IIS controls the binding.
            string urls = builder.Configuration["Contensive:Urls"] ?? "";
            if (!string.IsNullOrEmpty(urls)) {
                builder.WebHost.UseUrls(urls);
            }
            var app = builder.Build();
            //
            // -- serve static files from WebRootPath (baseassets, designblocks, toolpanel, etc.)
            app.UseStaticFiles();
            //
            // -- serve CDN files from localFilesPath, replacing the IIS virtual directory
            if (configureCdn) {
                Console.WriteLine($"[Contensive] CDN static files configured: [{cdnRequestPath}] -> [{cdnPhysicalPath}]");
                var cdnFileProvider = new PhysicalFileProvider(cdnPhysicalPath);
                app.UseStaticFiles(new StaticFileOptions {
                    FileProvider = cdnFileProvider,
                    RequestPath = cdnRequestPath,
                    ServeUnknownFileTypes = true,
                    OnPrepareResponse = ctx => {
                        Console.WriteLine($"[Contensive] CDN serving: [{ctx.Context.Request.Path}]");
                    }
                });
            }
            //
            // -- all dynamic routes handled by Contensive processor
            app.MapFallback((HttpRequest request, HttpResponse response, HttpContext iisContext) => {
                return executeManagedRoute(app.Configuration, request, response, iisContext);
            });
            app.Run();
        }
        public static IResult executeManagedRoute(IConfiguration configuration, HttpRequest request, HttpResponse response, HttpContext iisContext) {
            //
            // -- diagnostic: log requests that should have been served as static files
            string requestPath = request.Path.Value ?? "";
            if (requestPath.Contains("/files/") || requestPath.Contains("/baseassets/")) {
                Console.WriteLine($"[Contensive] WARNING: static file request fell through to route handler: [{requestPath}]");
            }
            //
            // -- resolve appName using request context (includes SERVER_NAME fallback)
            string appName = resolveAppName(configuration, iisContext);
            if (string.IsNullOrEmpty(appName)) {
                string errorMessage = "appName is not valid. Set Contensive:AppName in appsettings.json, configure the IIS site name, or set the CONTENSIVE_APPNAME environment variable.";
                Console.Error.WriteLine(errorMessage);
                response.StatusCode = 500;
                return Results.Text(errorMessage, "text/plain", statusCode: 500);
            }
            HttpContextModel context = ConfigurationClass.buildContext(appName, iisContext);
            //HttpContextModel context = new();
            string content = "";
            using (Contensive.Processor.CPClass cp = new(appName, context)) {
                //
                // need to add request and set response -- ?middleware
                //
                // -- execute code ------------------------------------------------
                content = cp.executeRoute();
                // -- /execute code ------------------------------------------------
                // 
                // -- exit now if response headers sent. This technique is used to write binary
                //if (response.HeadersWritten)
                //    return;
                // 
                // -- delete uploaded temp files in request
                foreach (var file in context.Request.Files)
                    WindowsTempFileController.deleteTmpFile(file.windowsTempfilename);
                // 
                //foreach (Contensive.Processor.Models.Domain.HttpContextResponseHeader header in context.Response.headers)
                //    Response.Headers.Add(header.name, header.value);
                // 
                foreach (KeyValuePair<string, HttpContextResponseCookie> cookie in context.Response.cookies) {
                    CookieOptions responseCookie = new Microsoft.AspNetCore.Http.CookieOptions() {
                        Path = cookie.Value.path,
                        Domain = cookie.Value.domain,
                        Expires = cookie.Value.expires.Equals(new DateTime(1, 1, 1, 0, 0, 0)) ? null : cookie.Value.expires,
                        HttpOnly = cookie.Value.httpOnly,
                        SameSite = SameSiteMode.Strict,
                        Secure = cookie.Value.secure
                    };
                    response.Cookies.Append(cookie.Key, cookie.Value.value, responseCookie);
                }
                // 
                response.ContentType = context.Response.contentType;
                //response.CacheControl = context.Response.cacheControl;
                //response.StatusCode =  context.Response.status;
                //response.Expires = context.Response.expires;
                //response.Buffer = context.Response.buffer;
                // 
                // -- transfer response to webserver
                if ((!string.IsNullOrEmpty(context.Response.redirectUrl))) {
                    // 
                    // -- perform redirect. Do not exit because reload required if route change
                    response.Redirect(context.Response.redirectUrl, false);
                    return Results.Content("", "text/html");
                }
                // 
                // -- if routeMap changed, unload app domain
                //if ((ConfigurationClass.routeMapDateInvalid() || (cp.routeMap.dateCreated != (DateTime)HttpContext.Current.Application("RouteMapDateCreated"))))
                //    HttpRuntime.UnloadAppDomain();
            }
            return Results.Content(content, "text/html");
        }
        ////
        // // middleware

        //public async Task InvokeAsync(HttpContext httpContext) {
        //    try {
        //        httpContext.Request.EnableBuffering();
        //        string requestBody = await new StreamReader(httpContext.Request.Body, Encoding.UTF8).ReadToEndAsync();
        //        httpContext.Request.Body.Position = 0;
        //        Console.WriteLine($"Request body: {requestBody}");
        //    } catch (Exception ex) {
        //        Console.WriteLine($"Exception reading request: {ex.Message}");
        //    }
        //}
    }
}