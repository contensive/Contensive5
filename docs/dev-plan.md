# Development Plan: WebApi Core 9 Applications

## Goal

Enable Contensive sites to run on ASP.NET Core 9 via the WebApi project, supporting both new deployments and upgrades from legacy ASPX Framework sites.

## Current State

The WebApi project (`source/WebApi/`) is a minimal ASP.NET Core host that:
- Resolves the Contensive app name from config, IIS App Pool, or environment
- Reads Contensive config at startup to configure CDN file serving
- Routes all dynamic requests through the Contensive Processor
- Serves static files (baseassets, CDN files) via ASP.NET Core middleware

### Completed Work

1. **Target framework**: `net9.0-windows` with `UseWPF` (required for ClearScript VBScript support via WindowsBase)
2. **App name resolution**: Priority order — appsettings.json → APP_POOL_ID → SERVER_NAME → env var
3. **CDN file serving**: Reads `cdnFileUrl` and `localFilesPath` from Contensive config at startup, configures `UseStaticFiles` middleware to replace IIS virtual directories
4. **WebRootPath**: Defaults to `.` (current directory) for converted ASPX sites where binaries and static files share the same folder
5. **Build script**: `scripts/build-core.cmd` publishes WebApi with `--self-contained false` and generates a deployment web.config

### Server Prerequisites

- .NET 9.0 Hosting Bundle (ASP.NET Core runtime + IIS module)
- .NET 9.0 Desktop Runtime (WindowsBase for ClearScript VBScript)
- IIS App Pool: "No Managed Code", 32-bit disabled
- SQL Server for application databases

## Remaining Work

### P1 — Required for production use

- [ ] **Verify static file serving for baseassets** — `WebRootPath = "."` should serve `/baseassets/` from the www folder. Needs end-to-end testing on a deployed site.
- [ ] **Verify CDN file serving** — `/appname/files/...` requests should be served by the CDN static file middleware. Confirm with network tab showing 200 responses.
- [ ] **Remove diagnostic Console.WriteLine** — Remove startup logging lines from Program.cs once static file serving is confirmed working. Or convert to proper structured logging.
- [ ] **Test conversion workflow** — Document and validate the full ASPX-to-WebApi upgrade path: stop site, copy published files, iisreset, verify.
- [ ] **Database upgrade warning** — The app logs "Application code is newer than database. Upgrade with cc.exe -a appName -u". This must be run after every WebApi upgrade.

### P2 — Improvements

- [ ] **Separate app root from www root** — For new (non-converted) deployments, support `localAppPath` so binaries live in a separate folder from static content. This avoids `WebRootPath = "."` exposing the binary folder.
- [ ] **Authentication middleware** — Add ASP.NET Core authentication/authorization middleware for admin routes if needed beyond Contensive's internal auth.
- [ ] **Response headers** — Uncomment and implement response headers transfer (cache-control, status code, etc.) in `executeManagedRoute`.
- [ ] **Binary/stream responses** — Handle cases where the Processor writes binary content (images, downloads) directly to the response stream instead of returning HTML string.
- [ ] **Health check endpoint** — Add `/healthz` or similar for load balancer probes.

### P3 — Linux Support

**Goal**: Run Contensive WebApi on Linux (Docker, AWS ECS, Azure App Service for Linux).

**Blocker**: The VBScript/ClearScript dependency requires `WindowsBase.dll` and the Windows Desktop Runtime. This forces `net9.0-windows` targeting and Windows-only deployment.

**Strategy**: Replace VBScript addon scripts with .NET assemblies (DLLs). Use Claude Code to analyze each VBScript addon, generate an equivalent C# assembly, and install it as a replacement.

#### Migration Steps

1. **Inventory VBScript addons** — Query the database for all addons with `scriptingCode` populated. The three known addons with VBScript are:
   - Open Graph (addon #41, collection Base5)
   - AddonListEndOfBody (addon #117, collection Page Builder)
   - Tool Panel (addon #154, collection Tool Panel)

2. **Convert scripts to assemblies** — For each VBScript addon:
   - Use Claude to analyze the VBScript code and the `cp` object model it uses
   - Generate an equivalent C# class implementing `AddonBaseClass`
   - Build as a netstandard2.0 DLL (compatible with both Framework and Core)
   - Test output matches the original VBScript behavior

3. **Replace addons in collections** — Update the addon collection XML:
   - Remove `<ScriptingCode>` and `<ScriptingLanguage>` elements
   - Add `<DotNetClass>` and include the assembly in the collection resources

4. **Remove ClearScript dependency** — Once no addons use VBScript:
   - Remove `Microsoft.ClearScript` NuGet package from Processor
   - Remove `AddonScriptController.execute_Script_VBScript()` (or leave as a stub that logs a deprecation warning)
   - Remove `UseWPF` from both WebApi and Processor projects
   - Change WebApi target to `net9.0` (plain, no `-windows`)
   - Change Processor targets to `net48;net9.0` (drop `-windows`)

5. **Remove Windows-only P/Invoke** — Address the `Kernel32.dll` call in `CoreController.cs`:
   - Replace `GetSystemTimePreciseAsFileTime` with `DateTime.UtcNow` or `Stopwatch.GetTimestamp()` (cross-platform)
   - Guard with `RuntimeInformation.IsOSPlatform(OSPlatform.Windows)` if precision is critical on Windows

6. **Remove IIS management dependency** — `WebServerController.cs` uses `Microsoft.Web.Administration`:
   - This is only used by CLI and TaskService for site provisioning, not by WebApi at runtime
   - Conditionally compile or move to a separate Windows-only package
   - For Linux deployments, site provisioning would use nginx/reverse proxy config generation instead

7. **Target `net9.0`** — Once all Windows dependencies are removed:
   - WebApi can target plain `net9.0`
   - Server only needs ASP.NET Core runtime (no Desktop Runtime)
   - Docker images can use `mcr.microsoft.com/dotnet/aspnet:9.0` base
   - Linux deployments become possible

#### Linux Deployment Architecture

```
Linux Host (Docker / systemd / App Service)
  └── Kestrel (direct, or behind nginx/caddy reverse proxy)
        └── WebApi.dll (ASP.NET Core host)
              ├── UseStaticFiles() — baseassets
              ├── UseStaticFiles(CDN) — /appname/files/
              └── MapFallback → executeManagedRoute()
                    └── Processor.dll (Contensive engine, net9.0)
```

#### Other P3 Items

- [ ] **Upgrade to .NET 10** — Requires updating target frameworks across Processor, CLI, TaskService, and all NuGet packages.
- [ ] **Out-of-process hosting** — Evaluate `hostingModel="OutOfProcess"` for better process isolation and restart behavior on Windows/IIS.

## Architecture

```
IIS (w3wp.exe)
  └── AspNetCoreModuleV2 (in-process)
        └── WebApi.dll (ASP.NET Core host)
              ├── UseStaticFiles() — serves baseassets from WebRootPath
              ├── UseStaticFiles(CDN) — serves /appname/files/ from localFilesPath
              └── MapFallback → executeManagedRoute()
                    └── CPClass(appName, httpContext)
                          └── Processor.dll (Contensive engine)
                                ├── Route resolution
                                ├── Addon execution
                                ├── Template rendering
                                └── Database access
```

## Key Files

| File | Purpose |
|------|---------|
| `source/WebApi/WebApi.csproj` | Project file, target framework, dependencies |
| `source/WebApi/Program.cs` | App startup, middleware config, route handler |
| `source/WebApi/ConfigurationClass.cs` | Builds HttpContextModel from ASP.NET Core HttpContext |
| `source/WebApi/WindowsTempFileController.cs` | Temp file management for uploads |
| `source/Processor/Processor.csproj` | Processor library (net48 + net9.0-windows) |
| `scripts/build-core.cmd` | Build and publish script |
| `etc/README-core.txt` | Deployment guide |

## Conversion from ASPX to WebApi

1. Install prerequisites on server (Hosting Bundle, Desktop Runtime)
2. Set App Pool to "No Managed Code" and disable 32-bit
3. Stop the IIS site
4. Copy published WebApi files into the site's www folder (replacing ASPX binaries)
5. Start the site, `iisreset`
6. Run `cc.exe -a appName -u` to upgrade the database schema
7. Verify page loads, static files, and CDN files in browser network tab
