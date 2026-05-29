# Migration to .NET Core

There are two migration paths for moving a Contensive application to .NET Core.

## Path 1: Full System Migration

Migrate the entire Contensive installation from .NET Framework to .NET Core. This replaces the IIS-hosted framework application with the Core-based WebApi, CLI, and TaskService components. The application logic, addon collections, and database remain unchanged — only the hosting infrastructure changes.

### Prerequisites

- .NET 9.0 Hosting Bundle installed on the server (see [setup-and-deployment.md](setup-and-deployment.md))
- A new build from `build-core.cmd`, extracted to a temporary folder on the server

### Steps

1. **Stop the existing framework Task Service:**
   ```
   sc stop "Contensive Task Service"
   ```

2. **Uninstall the framework components:**
   - Uninstall the Contensive Console MSI (Add/Remove Programs)
   - Delete the framework Task Service:
     ```
     sc delete "Contensive Task Service"
     ```

3. **Install the core components:**
   Run `install.cmd` as Administrator from the extracted build folder. This installs the CLI, TaskService, and WebApi package to `C:\Program Files\Contensive\`.

4. **Configure the server** (if first time with core CLI):
   ```
   cc --configure
   ```

5. **Upgrade all applications:**
   ```
   cc -u
   ```
   Wait for this to complete. This upgrades the database schema and collections for every application on the server.

6. **Upgrade each IIS site:**
   - For sites migrating to core WebApi: copy the files from `C:\Program Files\Contensive\WebApi\` into the site's IIS physical path, replacing all files. Then in IIS Manager, change the app pool CLR version to "No Managed Code" and recycle the app pool.
   - For sites remaining on framework ASPX: in IIS Manager, click the site, click "Import Application", and select `C:\Program Files\Contensive\defaultaspxsite.zip`. The app pool stays at CLR v4.0.

7. **Verify the Task Service is running:**
   ```
   sc query "Contensive Task Service"
   ```

### Notes

- You do not need to migrate all sites at once. Core WebApi and framework ASPX sites can run side-by-side on the same server.
- If you need to create additional framework ASPX sites during the transition, use `cc -nf appName domainName`.
- All new sites should be created with `cc -n appName domainName` (core WebApi).

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
