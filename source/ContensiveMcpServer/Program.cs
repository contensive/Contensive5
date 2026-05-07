using ContensiveMcpServer;
using ContensiveMcpServer.Tools;
using System.Text.Json;
//
var builder = WebApplication.CreateBuilder(args);
builder.Host.UseWindowsService();
//
builder.Configuration.AddJsonFile("appsettings.json", optional: true);
builder.Configuration.AddEnvironmentVariables(prefix: "CONTENSIVE_");
//
// -- configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddEventLog(settings => {
    settings.SourceName = "ContensiveMcpServer";
});
builder.Logging.SetMinimumLevel(LogLevel.Debug);
//
// -- register the ContensiveClient factory as a singleton
// -- the base URL is configured once; the bearer token comes from each user's MCP request
builder.Services.AddSingleton(sp => {
    var config = sp.GetRequiredService<IConfiguration>();
    string baseUrl = config["Contensive:BaseUrl"] ?? config["BASEURL"] ?? "";
    return new ContensiveClientFactory(baseUrl);
});
//
// -- register ContensiveClient as scoped (per-request) via HttpContext
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped(sp => {
    var factory = sp.GetRequiredService<ContensiveClientFactory>();
    var httpContext = sp.GetRequiredService<IHttpContextAccessor>().HttpContext;
    string bearerToken = "";
    if (httpContext != null) {
        string? authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) {
            bearerToken = authHeader.Substring(7).Trim();
        }
    }
    return factory.Create(bearerToken);
});
//
builder.Services.AddMcpServer()
    .WithHttpTransport(options => {
        options.Stateless = true;
    })
    .WithTools<PageTools>()
    .WithTools<WidgetTools>()
    .WithTools<WidgetInstanceTools>()
    .WithTools<ImageTools>();
//
var app = builder.Build();
var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("McpServer");
//
// -- log every request: method, path, headers, and response status
app.Use(async (context, next) => {
    var request = context.Request;
    logger.LogInformation("REQUEST {Method} {Path}{QueryString} from {RemoteIp}",
        request.Method, request.Path, request.QueryString, context.Connection.RemoteIpAddress);
    foreach (var header in request.Headers) {
        string value = header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase)
            ? $"Bearer ***{header.Value.ToString().Substring(Math.Max(0, header.Value.ToString().Length - 8))}"
            : header.Value.ToString();
        logger.LogDebug("  Header: {Key}: {Value}", header.Key, value);
    }
    await next(context);
    logger.LogInformation("RESPONSE {StatusCode} for {Method} {Path}",
        context.Response.StatusCode, request.Method, request.Path);
});
//
// -- Require a bearer token on all MCP requests.
// -- Unauthenticated requests return HTTP 401, which tells the MCP client
// -- to start the OAuth flow using the discovery documents below.
// -- Requests to /.well-known/* are always allowed so discovery works before auth.
app.Use(async (context, next) => {
    bool isWellKnown = context.Request.Path.StartsWithSegments("/.well-known");
    if (!isWellKnown) {
        string? authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        bool hasToken = !string.IsNullOrEmpty(authHeader)
            && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            && authHeader.Length > 7;
        if (!hasToken) {
            string mcpBase = GetMcpBaseUrl(context.RequestServices.GetRequiredService<IConfiguration>());
            context.Response.StatusCode = 401;
            context.Response.Headers["WWW-Authenticate"] =
                $"Bearer realm=\"Contensive\", resource_metadata=\"{mcpBase}/.well-known/oauth-protected-resource\"";
            await context.Response.WriteAsync("Authentication required.");
            return;
        }
    }
    await next(context);
});
//
// -- OAuth 2.0 Protected Resource Metadata (RFC 9728)
// -- tells MCP clients where to find the Authorization Server
app.MapGet("/.well-known/oauth-protected-resource", (IConfiguration config) => {
    string contensiveBaseUrl = config["Contensive:BaseUrl"] ?? config["BASEURL"] ?? "";
    var metadata = new {
        resource = GetMcpBaseUrl(config),
        authorization_servers = new[] { contensiveBaseUrl }
    };
    return Results.Json(metadata, new JsonSerializerOptions { WriteIndented = true }, contentType: "application/json");
});
//
// -- OAuth 2.0 Authorization Server Metadata (RFC 8414) — proxied from Contensive
// -- some clients look here instead of the protected resource metadata
app.MapGet("/.well-known/oauth-authorization-server", (IConfiguration config) => {
    string contensiveBaseUrl = (config["Contensive:BaseUrl"] ?? config["BASEURL"] ?? "").TrimEnd('/');
    var metadata = new {
        issuer = contensiveBaseUrl,
        authorization_endpoint = $"{contensiveBaseUrl}/oauth/authorize",
        token_endpoint = $"{contensiveBaseUrl}/oauth/token",
        registration_endpoint = $"{contensiveBaseUrl}/oauth/register",
        response_types_supported = new[] { "code" },
        code_challenge_methods_supported = new[] { "S256" },
        grant_types_supported = new[] { "authorization_code" },
        token_endpoint_auth_methods_supported = new[] { "none" }
    };
    return Results.Json(metadata, new JsonSerializerOptions { WriteIndented = true }, contentType: "application/json");
});
//
app.MapMcp();
app.Run();
//
// -- returns the public base URL of this MCP server (used in resource metadata)
static string GetMcpBaseUrl(IConfiguration config) {
    string urls = config["Urls"] ?? config["ASPNETCORE_URLS"] ?? "";
    string firstUrl = urls.Split(';')[0].Trim();
    // prefer the configured public URL; fall back to the listen address
    return config["Contensive:McpPublicUrl"] ?? firstUrl;
}
