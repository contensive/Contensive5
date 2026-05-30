# Migration to Cross-Platform .NET 9 (Remove Windows Dependency)

This document describes the changes required to migrate Contensive 5 from `net9.0-windows` to cross-platform `net9.0`, enabling deployment on Linux (and macOS).

## Current State

The codebase currently dual-targets `net48` (legacy .NET Framework) and `net9.0-windows` (.NET 9, Windows-only). Several projects also reference Windows-only APIs: IIS management, Windows services, Kernel32 P/Invoke, and WPF.

Projects already cross-platform (no changes needed):
- `Models.csproj` — `netstandard2.0`
- `CPBase.csproj` — `netstandard2.0`
- `DashboardWidgets.csproj` — `netstandard2.0`
- `aoMcp.csproj` — `netstandard2.0`

---

## Step 1: Change Target Frameworks in Project Files

Update `TargetFramework` / `TargetFrameworks` from `net9.0-windows` to `net9.0` in the following `.csproj` files:

| Project File | Current | Change To |
|---|---|---|
| `source/Processor/Processor.csproj` | `net48;net9.0-windows` | `net48;net9.0` |
| `source/Cli/Cli.csproj` | `net48;net9.0-windows` | `net48;net9.0` |
| `source/TaskService/TaskService.csproj` | `net9.0-windows` | `net9.0` |
| `source/WebApi/WebApi.csproj` | `net9.0-windows7.0` | `net9.0` |
| `source/ProcessorTests/ProcessorTests.csproj` | `net48;net9.0-windows` | `net48;net9.0` |
| `source/ModelsTests/ModelsTests.csproj` | `net48;net9.0-windows` | `net48;net9.0` |

`ContensiveMcpServer.csproj` is already `net9.0` — no change needed.

---

## Step 2: Remove `<UseWPF>true</UseWPF>`

Remove all `<UseWPF>true</UseWPF>` lines from:
- `source/Processor/Processor.csproj` (line 6)
- `source/WebApi/WebApi.csproj` (lines 4 and 9 — appears twice)

WPF is a Windows-only desktop UI framework and cannot be used on Linux. If any code actually depends on WPF types (unlikely in a web/server project), those references will surface as build errors and must be replaced.

---

## Step 3: Remove or Replace Windows-Only NuGet Packages

### 3a. Remove `Microsoft.Win32.Primitives`

In `source/Processor/Processor.csproj`, remove:
```xml
<PackageReference Include="Microsoft.Win32.Primitives" Version="4.3.0" />
```
These APIs are included in the .NET 9 runtime already.

### 3b. Remove `System.ServiceProcess.ServiceController`

In `source/Processor/Processor.csproj`, remove:
```xml
<PackageReference Include="System.ServiceProcess.ServiceController" Version="9.0.9" />
```
This is Windows-only. Any code using `ServiceController` must be conditionalized behind `#if NET48` or replaced with cross-platform process management.

### 3c. Replace `Microsoft.Extensions.Hosting.WindowsServices`

This package is used in two projects for Windows service hosting:
- `source/TaskService/TaskService.csproj`
- `source/ContensiveMcpServer/ContensiveMcpServer.csproj`

**Option A — Linux-only (systemd):** Replace with `Microsoft.Extensions.Hosting.Systemd`:
```xml
<PackageReference Include="Microsoft.Extensions.Hosting.Systemd" Version="9.0.0" />
```
And in `Program.cs`, replace `UseWindowsService()` with `UseSystemd()`.

**Option B — Support both platforms:** Include both packages and call both methods (each is a no-op when not running on its platform):
```xml
<PackageReference Include="Microsoft.Extensions.Hosting.WindowsServices" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Hosting.Systemd" Version="9.0.0" />
```
```csharp
builder.Host.UseWindowsService();
builder.Host.UseSystemd();
```

### 3d. Handle `Microsoft.Web.Administration`

In `source/Processor/Processor.csproj`, remove:
```xml
<PackageReference Include="Microsoft.Web.Administration" Version="11.1.0" />
```
This is the IIS management API and is Windows/IIS-only. See Step 5 for the code changes required.

---

## Step 4: Replace the Kernel32 P/Invoke

In `source/Processor/Controllers/CoreController.cs` (lines 130–135), there is a Windows-only P/Invoke:
```csharp
GetSystemTimePreciseAsFileTime(out long fileTime);
return DateTimeOffset.FromFileTime(fileTime).DateTime;
...
[DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi)]
static extern void GetSystemTimePreciseAsFileTime(out long filetime);
```

Replace with cross-platform .NET:
```csharp
return DateTime.UtcNow;
```
`DateTime.UtcNow` in .NET 9 provides sufficient precision for server-side timestamp generation. Remove the `DllImport` declaration and the `using System.Runtime.InteropServices` if no longer needed.

---

## Step 5: Abstract or Remove IIS Management Code

This is the largest area of work. The following files use `Microsoft.Web.Administration` to manage IIS application pools and sites:

- `source/Processor/Controllers/WebServerController.cs` — `verifyAppPool()` method for creating/managing IIS application pools
- `source/Processor/Models/Domain/DashboardUserConfigModel.cs` — reads IIS configuration for dashboard display
- `source/Processor/Models/View/DashboardViewModel.cs` — reads IIS configuration for dashboard display
- `source/Processor/Addons/Tools/IISResetToolClass.cs` — admin tool to restart IIS

### Recommended Approach

Create an `IWebServerManager` interface to abstract web server operations, with an IIS implementation (for legacy Windows deployments) and a no-op or Kestrel/Nginx implementation for Linux:

```csharp
public interface IWebServerManager {
    void VerifyAppPool(string appName);
    void RecycleAppPool(string appName);
    List<WebServerSiteInfo> GetSites();
}
```

- **IIS implementation**: wraps current `Microsoft.Web.Administration` calls, only compiled/loaded on Windows
- **Linux implementation**: no-op for AppPool operations (Kestrel doesn't use them), or shell calls to manage Nginx/reverse proxy configuration

### Files That Reference IIS Indirectly

These files reference IIS reset functionality and need review:
- `source/Cli/Views/iisresetCmd.cs`
- `source/Cli/Views/HelpCmd.cs`
- `source/Cli/mainClass.cs`
- `source/Processor/Views/CPUtilsClass.cs`
- `source/CPBase/BaseClasses/CPUtilsBaseClass.cs`
- `source/Processor/Addons/AdminSite/Controllers/AdminContentController.cs`

---

## Step 6: Conditionalize or Remove `AspNetCoreHostingModel`

In `source/WebApi/WebApi.csproj`, remove:
```xml
<AspNetCoreHostingModel>InProcess</AspNetCoreHostingModel>
```
`InProcess` hosting is IIS-specific. On Linux with Kestrel, this setting is ignored but should be removed for clarity. If you still need IIS support on Windows, conditionalize it:
```xml
<AspNetCoreHostingModel Condition="'$(OS)' == 'Windows_NT'">InProcess</AspNetCoreHostingModel>
```

---

## Step 7: Update Service Hosting (TaskService and MCP Server)

### TaskService (`source/TaskService/Program.cs`)

Current code:
```csharp
Host.CreateDefaultBuilder(args)
    .UseWindowsService(options => {
        options.ServiceName = "Contensive Task Service";
    })
```

For Linux, replace with systemd or use both:
```csharp
Host.CreateDefaultBuilder(args)
    .UseWindowsService(options => {
        options.ServiceName = "Contensive Task Service";
    })
    .UseSystemd()
```

### MCP Server (`source/ContensiveMcpServer/Program.cs`)

Current code:
```csharp
builder.Host.UseWindowsService();
```

Same approach — add `.UseSystemd()` alongside or replace.

### Systemd Unit File

For Linux deployment, create a systemd service unit file (e.g., `deploy/contensive-task.service`):
```ini
[Unit]
Description=Contensive Task Service
After=network.target

[Service]
Type=notify
ExecStart=/opt/contensive/taskservice/TaskService
WorkingDirectory=/opt/contensive/taskservice
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
```

---

## Step 8: Update File Paths and Deployment Scripts

The current deployment assumes Windows paths and tooling:
- `C:\Program Files\Contensive\Cli\` → `/opt/contensive/cli/`
- `C:\Program Files\Contensive\TaskService\` → `/opt/contensive/taskservice/`
- `C:\Program Files\Contensive\WebApi\` → `/opt/contensive/webapi/`
- Windows Task Scheduler → cron or systemd timer
- `install.cmd` / `uninstall.cmd` → shell scripts (`install.sh` / `uninstall.sh`)

Review and update any hardcoded Windows paths in CLI code:
- `source/Cli/Views/ConfigureCmd.cs`
- `source/Cli/Views/UpgradeCmd.cs`
- Any code that uses `Environment.GetFolderPath()` or hardcoded `C:\` paths

---

## Step 9: Handle the Legacy `iisDefaultSite` Project

`source/iisDefaultSite/iisDefaultSite.vbproj` is a .NET Framework 4.8 ASPX site targeting `win-x86`. It references `System.Web`, `System.Drawing`, and other Windows-only namespaces.

This project **cannot** be ported to cross-platform .NET 9. Options:
1. **Keep as-is** — maintain for legacy Windows/IIS Framework deployments only
2. **Remove from solution** — if Framework site support is being dropped
3. **Rewrite** — create a new .NET 9 Kestrel-hosted equivalent

---

## Step 10: Reverse Proxy Configuration for Linux

On Windows, IIS acts as the web server. On Linux, the standard approach is:
- **Kestrel** as the application server (built into ASP.NET Core)
- **Nginx** or **Caddy** as the reverse proxy in front of Kestrel

Create a sample Nginx configuration (e.g., `deploy/nginx-contensive.conf`):
```nginx
server {
    listen 80;
    server_name example.com;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }
}
```

---

## Step 11: Update Scheduled Tasks

Replace Windows Task Scheduler with cron or a systemd timer for server diagnostics:

**Cron (simple):**
```
0 2 * * * /opt/contensive/cli/cc --serverdiagnostic
```

**Systemd timer (recommended):**

`deploy/contensive-diagnostics.timer`:
```ini
[Unit]
Description=Contensive Server Diagnostics Timer

[Timer]
OnCalendar=*-*-* 02:00:00
Persistent=true

[Install]
WantedBy=timers.target
```

`deploy/contensive-diagnostics.service`:
```ini
[Unit]
Description=Contensive Server Diagnostics

[Service]
Type=oneshot
ExecStart=/opt/contensive/cli/cc --serverdiagnostic
```

---

## Step 12: Build and Test

After making all changes:

1. Build the .NET 9 targets:
   ```bash
   dotnet build -f net9.0
   ```

2. Fix any remaining build errors — most will be missing type references from removed Windows packages

3. Run tests:
   ```bash
   dotnet test -f net9.0
   ```

4. Test on Linux:
   ```bash
   dotnet publish -c Release -r linux-x64 --self-contained
   ```

---

## Migration Order (Recommended)

1. Steps 1–3: Project file changes (mechanical, low risk)
2. Step 4: P/Invoke replacement (small, isolated change)
3. Step 6: Remove `AspNetCoreHostingModel` setting
4. Step 5: IIS abstraction (largest effort, most design work)
5. Steps 7–8: Service hosting and path updates
6. Step 9: Decision on legacy ASPX site
7. Steps 10–12: Linux deployment infrastructure and testing
