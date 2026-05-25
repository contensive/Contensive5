# Migration to .NET Core

There are two migration paths for moving a Contensive application to .NET Core.

## Path 1: Full System Migration

Migrate the entire Contensive installation from .NET Framework to .NET Core by uninstalling the Contensive Framework version and installing the Core version.

This replaces the IIS-hosted Framework application with the Core-based WebApi, CLI, and TaskService components. The application logic, addon collections, and database remain unchanged — only the hosting infrastructure changes.

## Path 2: SPA Remote Methods to .NET Core Without Contensive Routing

For applications that are primarily SPA-based with remote method addons, you can migrate to a standalone .NET Core 9 application that bypasses Contensive routing entirely while still executing existing addon code.

### Approach

For each remote method:

1. Call a helper that populates the Contensive `HttpContextModel` from the .NET Core `HttpContext`
2. Create an instance of `CPClass` from the new Contensive request and a hardcoded app name
3. Call `cp.executeAddon()` to invoke the current execute method
4. After return, populate the Core response object from the Contensive response object

### Why This Works

- `CPClass` has a constructor that takes `appName` + `HttpContextModel` — this is the web constructor used in the current WebApi.
- `HttpContextModel` is a plain DTO holding `Request` (query string, form, headers, cookies, body, server variables) and `Response` (redirect URL, headers, content type, cookies). It can be populated from an ASP.NET Core `HttpContext`.
- `cp.executeAddon()` bypasses routing entirely — it loads the addon from cache by name or GUID and executes it directly. No Contensive route matching is involved.
- There is already a reference implementation for populating `HttpContextModel` from ASP.NET Core in `ConfigurationClass.buildContext`, which maps `HttpContext.Request` to the Contensive model.

### Example Pattern Per Endpoint

```csharp
app.MapGet("/some-remote-method", async (HttpContext context) => {
    var httpContextModel = MapFromCoreRequest(context);
    using var cp = new CPClass("myAppName", httpContextModel);
    string result = cp.executeAddon("remote-method-guid-or-name",
        addonContext.ContextRemoteMethodJson);
    MapToCoreResponse(context, cp);
    await context.Response.WriteAsync(result);
});
```

### Things to Watch For

- **Sessions**: The `CPClass(appName, httpContextModel)` constructor enables sessions. If the remote methods don't need user/visit tracking, `CPClass(appName)` is lighter. If they do, you'll need to forward cookies both ways.
- **addonContext parameter**: Use `ContextRemoteMethodJson` or `ContextRemoteMethodHtml` to match what Contensive routing would have set — some addons may check this.
- **Response mapping**: After `executeAddon`, check `httpContextModel.Response` for redirects, cookies, headers, and content type. These need to be copied back to the Core response.
- **Disposable**: `CPClass` implements `IDisposable` — wrap in `using` to clean up.
- **Scaffold generation**: With many remote methods, you can generate the endpoint registrations from the addon collection XML or the database's `ccaddons` table rather than hand-coding each one.

### What This Avoids

All of Contensive's `RouteController` — URL parsing, page/template resolution, addon stacking, and HTML document wrapping. Your .NET Core 9 app owns routing completely, and Contensive is just the addon execution engine.
