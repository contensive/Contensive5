# Migration to Cross-Platform .NET (Remove Windows Dependency)

This document describes the changes required to migrate Contensive 5 to cross-platform targets, enabling deployment on Linux (and macOS).

## Architecture

The solution follows a library/application split:

- **Libraries** target `netstandard2.0` (with optional `net48` dual-target for legacy). This makes them consumable by any .NET Standard 2.0-compatible host — .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+, etc.
- **Applications** (executable entry points) target `net9.0`. These are the runnable hosts that reference the libraries.

### Reverse proxy

**Current**: IIS serves as both the web server and application host. On .NET Framework, the application runs in-process inside the IIS worker (`w3wp.exe`). On .NET 9, the `AspNetCoreHostingModel InProcess` setting keeps this same pattern.

**Target**: Kestrel as the application server with Nginx as the reverse proxy, on both Windows and Linux. Kestrel runs the .NET application as its own process. Nginx handles external traffic on ports 80/443 and proxies to Kestrel on a local port. Using the same reverse proxy on both platforms provides a consistent configuration for:
- TLS termination and certificate management (Let's Encrypt via certbot)
- URL rewriting (`rewrite` and `location` directives)
- Static and dynamic response compression (`gzip` / `gzip_proxied`)
- IP address blocking (`allow` / `deny` directives, `geo` module)
- Multi-site domain routing with SNI for per-domain TLS

## Current State

The codebase currently dual-targets `net48` (legacy .NET Framework) and `net9.0-windows` (.NET 9, Windows-only). Several projects also reference Windows-only APIs: IIS management, Windows services, and WPF.

Libraries already cross-platform (no changes needed):
- `CPBase.csproj` — `netstandard2.0`
- `Models.csproj` — `netstandard2.0`
- `DashboardWidgets.csproj` — `netstandard2.0`
- `aoMcp.csproj` — `netstandard2.0`

---

## Step 1: Change Target Frameworks in Project Files

### Libraries — convert to `netstandard2.0`

Libraries should target `netstandard2.0` (dual-targeted with `net48` for legacy support). This makes them portable across all .NET runtimes without being tied to a specific version.

| Project File | Type | Current | Change To |
|---|---|---|---|
| `source/Processor/Processor.csproj` | Library | `net48;net9.0-windows` | `net48;netstandard2.0` |

### Applications — convert to `net9.0`

Applications are executable entry points and must target a concrete runtime. Remove the `-windows` suffix to enable cross-platform deployment.

| Project File | Type | Current | Change To |
|---|---|---|---|
| `source/Cli/Cli.csproj` | Application | `net48;net9.0-windows` | `net48;net9.0` |
| `source/TaskService/TaskService.csproj` | Application | `net9.0-windows` | `net9.0` |
| `source/WebApi/WebApi.csproj` | Application | `net9.0-windows` | `net9.0` |

### Test projects

| Project File | Current | Change To |
|---|---|---|
| `source/ProcessorTests/ProcessorTests.csproj` | `net48;net9.0-windows` | `net48;net9.0` |
| `source/ModelsTests/ModelsTests.csproj` | `net48;net9.0-windows` | `net48;net9.0` |

Test projects target `net9.0` (not `netstandard2.0`) because test runners need a concrete runtime to execute.

---

## ~~Step 2: Remove `<UseWPF>true</UseWPF>`~~ **DONE**

- ~~`source/Processor/Processor.csproj`~~ **DONE**
- ~~`source/WebApi/WebApi.csproj`~~ **DONE**

---

## Step 3: Remove or Replace Windows-Only NuGet Packages

### 3a. ~~Remove `Microsoft.Win32.Primitives`~~ **DONE**

~~In `source/Processor/Processor.csproj`, remove:~~
```xml
<PackageReference Include="Microsoft.Win32.Primitives" Version="4.3.0" />
```
This package reference has been removed. These APIs are included in the .NET 9 runtime already.

### 3b. ~~Remove `System.ServiceProcess.ServiceController`~~ **DONE**

~~In `source/Processor/Processor.csproj`, remove:~~
```xml
<PackageReference Include="System.ServiceProcess.ServiceController" Version="9.0.9" />
```
~~This is Windows-only.~~ This package reference has been removed.

### 3c. Add `Microsoft.Extensions.Hosting.Systemd` alongside WindowsServices

In `source/TaskService/TaskService.csproj`, add the systemd package alongside the existing WindowsServices package. Both packages are no-ops when not running on their respective platform, so they can coexist safely:
```xml
<PackageReference Include="Microsoft.Extensions.Hosting.WindowsServices" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Hosting.Systemd" Version="9.0.0" />
```
In `Program.cs`, call both:
```csharp
builder.Host.UseWindowsService();
builder.Host.UseSystemd();
```

### 3d. ~~Handle `Microsoft.Web.Administration`~~ **DONE**

~~In `source/Processor/Processor.csproj`, remove:~~
```xml
<PackageReference Include="Microsoft.Web.Administration" Version="11.1.0" />
```
This package reference has been removed. IIS management is now done by shelling out to `iisutil.exe`. See Step 5 for remaining code changes.

### 3e. Remove `Nustache.Core` DLL Reference

In `source/Processor/Processor.csproj`, remove the direct DLL reference and the pack item:
```xml
<Reference Include="Nustache.Core">
    <HintPath>Libs\Nustache.Core.dll</HintPath>
    <Private>True</Private>
</Reference>
```
```xml
<None Include="Libs\Nustache.Core.dll" Pack="true" PackagePath="lib\net48\" />
```

Nustache is a pre-built .NET Framework DLL (not a NuGet package) used in `source/Processor/Controllers/MustacheController.cs`. The cross-platform replacement `Stubble.Core` is already referenced and integrated — the controller has a Stubble code path toggled by a site property. Make Stubble the default and remove the Nustache code path.

### 3f. Remove `Microsoft.ClearScript`

In `source/Processor/Processor.csproj`, remove:
```xml
<PackageReference Include="Microsoft.ClearScript" Version="7.5.0" />
```

This package provides VBScript and JScript execution via Windows Script Host COM objects (`Microsoft.ClearScript.Windows.VBScriptEngine` and `JScriptEngine`). Used in `source/Processor/Controllers/Addon/AddonScriptController.cs`.

These engines are inherently Windows-only — they wrap `vbscript.dll` and `jscript.dll` COM objects. Options:
- **JScript**: Can be replaced with ClearScript's cross-platform `V8ScriptEngine` for JavaScript execution
- **VBScript**: Has no cross-platform alternative. Legacy VBScript addons can only run on Windows deployments. Guard with `OperatingSystem.IsWindows()` or drop VBScript support entirely

---

## ~~Step 4: Replace the Kernel32 P/Invoke~~ **DONE**

~~In `source/Processor/Controllers/CoreController.cs` (lines 130–135), there is a Windows-only P/Invoke.~~ The `GetSystemTimePreciseAsFileTime` P/Invoke and `DllImport("Kernel32.dll")` have been removed.

---

## ~~Step 5: Abstract or Remove IIS Management Code~~ **DONE**

The `Microsoft.Web.Administration` package reference has been removed. IIS management now shells out to `iisutil.exe` via `Process.Start()`.

### Completed

- **iisutil.exe calls gated** — `runIisUtil()`, `isValidBinding()`, and `syncIpBlocksToIIS()` in `WebServerController.cs` now check `RuntimeInformation.IsOSPlatform(OSPlatform.Windows)` and no-op on Linux.
- **IISReset removed** — The `reset()` method in `WebServerController.cs` is now a no-op. `CPUtilsBaseClass.IISReset()` and `CPUtilsClass.IISReset()` are marked `[Obsolete]` (cannot delete from CPBase due to binary compatibility). The `--iisreset` CLI command prints a message directing users to `--iisrecycle`. The commented-out `IISResetToolClass` reference in `AdminContentController.cs` has been removed.

### Dashboard models — no IIS config reads found

The following Dashboard files were reviewed and **none read IIS configuration**. They manage user widget layout stored in JSON config files under `cp.PrivateFiles`:
- `source/Processor/Models/Domain/DashboardUserConfigModel.cs`
- `source/Processor/Models/View/DashboardViewModel.cs`
- `source/Processor/Models/Domain/DashboardWidgetUserConfigModel.cs`
- `source/Processor/Models/View/DashboardWidgetViewModel.cs`
- `source/Processor/Controllers/DashboardWidgetRenderController.cs`

---

## ~~Step 5b: Replace `%comspec%` Shell Execution~~ **DONE**

In `source/Processor/Controllers/GenericController.cs`, the `executeCommandSync()` method now uses `RuntimeInformation.IsOSPlatform()` to select `cmd.exe /c` on Windows or `/bin/sh -c` on Linux.

---

## Step 6: Remove `AspNetCoreHostingModel` and Migrate to Nginx

### 6a. Remove `AspNetCoreHostingModel` from WebApi.csproj

In `source/WebApi/WebApi.csproj`, remove:
```xml
<AspNetCoreHostingModel>InProcess</AspNetCoreHostingModel>
```
`InProcess` is IIS-specific — it runs the .NET process inside the IIS worker (`w3wp.exe`). With Nginx as the reverse proxy, Kestrel runs as its own process and this setting is no longer relevant.

### 6b. Windows — Convert from IIS to Nginx

**Install Nginx on Windows:**
1. Download the Windows build from https://nginx.org/en/download.html (mainline or stable)
2. Extract to `C:\nginx\`
3. Configure as a Windows service using [WinSW](https://github.com/winsw/winsw) or [NSSM](https://nssm.cc/) so it starts automatically

**Configure Nginx** (`C:\nginx\conf\nginx.conf`):
```nginx
worker_processes auto;

events {
    worker_connections 1024;
}

http {
    include       mime.types;
    default_type  application/octet-stream;
    sendfile      on;
    keepalive_timeout 65;

    # -- compression
    gzip on;
    gzip_proxied any;
    gzip_types text/plain text/css application/json application/javascript text/xml application/xml application/xml+rss text/javascript;
    gzip_min_length 256;

    # -- per-site config files
    include C:/nginx/conf/sites-enabled/*.conf;
}
```

**Per-site config** (`C:\nginx\conf\sites-enabled\example.com.conf`):
```nginx
server {
    listen 80;
    server_name example.com www.example.com;

    # -- redirect HTTP to HTTPS
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl;
    server_name example.com www.example.com;

    ssl_certificate     C:/nginx/certs/example.com/fullchain.pem;
    ssl_certificate_key C:/nginx/certs/example.com/privkey.pem;

    # -- IP blocking
    # deny 192.168.1.100;
    # allow all;

    # -- URL rewrites
    # rewrite ^/old-path$ /new-path permanent;

    location / {
        proxy_pass http://127.0.0.1:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }
}
```

**Decommission IIS:**
1. Stop the IIS site and application pool
2. Verify Nginx is serving traffic correctly
3. Remove the IIS site/app pool (keep IIS installed if other apps need it)

### 6c. Linux — Deploy with Nginx

**Install Nginx:**
```bash
# Ubuntu/Debian
sudo apt update && sudo apt install nginx

# Amazon Linux / RHEL
sudo yum install nginx
```

**Configure Nginx** (`/etc/nginx/sites-available/example.com.conf`):
```nginx
server {
    listen 80;
    server_name example.com www.example.com;
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl;
    server_name example.com www.example.com;

    ssl_certificate     /etc/letsencrypt/live/example.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/example.com/privkey.pem;

    # -- compression
    gzip on;
    gzip_proxied any;
    gzip_types text/plain text/css application/json application/javascript text/xml application/xml application/xml+rss text/javascript;
    gzip_min_length 256;

    # -- IP blocking
    # deny 192.168.1.100;
    # allow all;

    location / {
        proxy_pass http://127.0.0.1:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }
}
```

**Enable the site and TLS:**
```bash
sudo ln -s /etc/nginx/sites-available/example.com.conf /etc/nginx/sites-enabled/
sudo certbot --nginx -d example.com -d www.example.com
sudo nginx -t && sudo systemctl reload nginx
```

---

## Step 7: Update Service Hosting (TaskService)

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

## ~~Step 10: Reverse Proxy Configuration~~ Merged into Step 6

Nginx reverse proxy configuration for both Windows and Linux is documented in Step 6.

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

**Already completed:**
- Step 2: `UseWPF` removed from Processor.csproj and WebApi.csproj
- Step 3a: `Microsoft.Win32.Primitives` removed
- Step 3b: `System.ServiceProcess.ServiceController` removed
- Step 3d: `Microsoft.Web.Administration` package removed (code migrated to iisutil.exe)
- Step 4: Kernel32 P/Invoke replaced
- Step 5: IIS management code gated with OS checks, IISReset removed
- Step 5b: `%comspec%` replaced with cross-platform shell invocation

**Remaining work, in recommended order:**
1. Steps 3e, 3f: Remove Nustache and ClearScript (prerequisite for Processor `netstandard2.0`)
2. Step 1: TFM changes — Processor to `net48;netstandard2.0`, applications to `net9.0`
3. Step 3c: Add systemd hosting package alongside WindowsServices in TaskService
4. Step 6: Remove `AspNetCoreHostingModel`, deploy Nginx on Windows and Linux
5. Step 7: TaskService systemd support
6. Step 8: File paths and deployment scripts
7. Step 9: Decision on legacy ASPX site
8. Steps 11–12: Scheduled tasks and build/test
