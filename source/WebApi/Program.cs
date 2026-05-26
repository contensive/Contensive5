

using Contensive.Processor.Models.Domain;

namespace Contensive.WebApi {
    internal class Program {
        private static void Main(string[] args) {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions {
                Args = args,
                WebRootPath = "webroot"
            });
            //
            // -- when running standalone (dotnet run, Kestrel), set the listen url from config.
            // -- when hosted by IIS in-process, UseUrls is ignored because IIS controls the binding.
            string urls = builder.Configuration["Contensive:Urls"] ?? "";
            if (!string.IsNullOrEmpty(urls)) {
                builder.WebHost.UseUrls(urls);
            }
            var app = builder.Build();
            app.UseStaticFiles();
            //
            // -- all dynamic routes handled by Contensive processor
            app.MapFallback((HttpRequest request, HttpResponse response, HttpContext iisContext) => {
                return executeManagedRoute(app.Configuration, request, response, iisContext);
            });
            app.Run();
        }
        public static IResult executeManagedRoute(IConfiguration configuration, HttpRequest request, HttpResponse response, HttpContext iisContext) {
            //
            // -- resolve appName: config override, then IIS site name, then env var
            string appName = configuration["Contensive:AppName"] ?? "";
            if (string.IsNullOrEmpty(appName)) {
                appName = iisContext.GetServerVariable("IIS_SITE_NAME") ?? "";
            }
            if (string.IsNullOrEmpty(appName)) {
                appName = Environment.GetEnvironmentVariable("CONTENSIVE_APPNAME") ?? "";
            }
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
                        SameSite = SameSiteMode.Lax,
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