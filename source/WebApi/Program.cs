

namespace Contensive.WebApi {
    internal class Program {
        private static void Main(string[] args) {

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions {
                Args = args,
                // Look for static files in webroot
                WebRootPath = "webroot"
            });
            //var builder = WebApplication.CreateBuilder(args);
            var app = builder.Build();
            app.UseStaticFiles();

            app.MapGet("/test", () => {
                return "Hello World 3";
            });
            //
            // -- get root route w
            app.MapGet("/", () => $"Hello World, no qs");
            //
            // -- post root route w
            app.MapPost("/", () => $"Hello World, no qs");
            //
            // -- root route with  querystring parameter test=
            //app.MapGet("/", (string test) => $"Hello World {test}");
            //
            // -- 
            app.MapGet("/customers", () => $"customer route");
            app.MapGet("/customers/{id}", (int id) => $"customer route that is an integer segment, {id}");
            //app.MapGet("/customers/{id}", (int id, string option) => $"customer route that is an integer segment, {id}, and option querystring {option}");
            app.MapGet("/customers/{id}/{thing}", (int id, string thing) => $"customer route with an integer then string segment,  {id}, {thing}");
            //
            app.MapGet("/admin", (HttpRequest request, HttpResponse response, HttpContext iisContext) => {
                return adminRoute(request, response, iisContext);
            });
            app.MapPost("/admin", (HttpRequest request, HttpResponse response, HttpContext iisContext) => {
                return adminRoute(request, response, iisContext);
            });

            // -- query all dynamic routes


            app.Run("http://localhost:5099");
        }
        public static string someRoute() {
            return "method test";
        }
        public static IResult adminRoute(HttpRequest request, HttpResponse response, HttpContext iisContext) {

            string appName = "c5test";
            HttpContextModel context = ConfigurationClass.buildContext(appName, iisContext);
            //HttpContextModel context = new();
            string content = "";
            using (Contensive.Processor.CPClass cp = new(appName, context)) {
                //
                // need to add request and set response -- ?middleware
                //
                // -- execute code ------------------------------------------------
                content = cp.executeRoute("/admin");
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