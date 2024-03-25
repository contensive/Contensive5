using System.Diagnostics;

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
        app.MapGet("/admin", () => {
            string appName = "c5test";
            Contensive.Processor.Models.Domain.HttpContextModel context = new();
            string content = "";
            using (Contensive.Processor.CPClass cp = new(appName, context)) {
                content = cp.executeRoute("/admin");
            }
            return content;
        });

        app.Run("http://localhost:5099");
    }

    public static string someRoute() {
        return "method test";
    }
}