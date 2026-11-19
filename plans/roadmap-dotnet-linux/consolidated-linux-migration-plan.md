# Consolidated Plan: Contensive 5 Cross-Platform Linux Migration

> **Replaces:** `dev-plan.md`, `migration-to-linux-core.md`, `migration-to-linux-core-windows-service.md`, `remove-framework-references.md`

## Goal

Run Contensive 5 on Linux to reduce hosting costs and improve performance on a minimal platform. The platform will target **.NET 10 LTS** with **Nginx** as the reverse proxy on both Windows and Linux, and **SQL Server** as the database on all platforms.

## Key Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| .NET version | **.NET 10 LTS** | Long-term support; avoids an extra upgrade cycle from .NET 9 STS |
| Processor target framework | **`net48;net10.0`** | `net10.0` gives access to modern APIs; `net48` retained for legacy Framework hosts |
| Application targets | **`net10.0`** | WebApi, CLI, TaskService all target plain `net10.0` (no `-windows` suffix) |
| VBScript/JScript addons | **Convert to C# DLLs** | Rewrite each scripting addon as an `AddonBaseClass` assembly. Preserves all functionality |
| Reverse proxy | **Nginx everywhere** | Replace IIS with Nginx on both Windows and Linux for uniform configuration |
| Database | **SQL Server everywhere** | SQL Server on Windows natively; SQL Server in Docker on Linux. No SQL dialect porting needed |
| Legacy ASPX site | **Keep as-is** | Maintain `iisDefaultSite` for legacy Windows/IIS Framework deployments; do not port |

---

## Current State (What's Already Done)

These items from the earlier plans have been completed:

- [x] `UseWPF` removed from Processor.csproj and WebApi.csproj
- [x] `Microsoft.Win32.Primitives` package removed from Processor
- [x] `System.ServiceProcess.ServiceController` package removed from Processor
- [x] `Microsoft.Web.Administration` package removed from Processor (IIS management migrated to iisutil.exe)
- [x] Kernel32 `GetSystemTimePreciseAsFileTime` P/Invoke replaced with cross-platform alternative
- [x] IIS management code in `WebServerController.cs` gated with `RuntimeInformation.IsOSPlatform()` checks
- [x] `IISReset` removed; `CPUtilsBaseClass.IISReset()` marked `[Obsolete]`
- [x] `%comspec%` shell execution replaced with cross-platform shell invocation in `GenericController.cs`
- [x] Stubble.Core integrated alongside Nustache with a feature toggle in `MustacheController.cs`

### What Still Exists (Windows Blockers)

| Blocker | Location | Why It Blocks Linux |
|---------|----------|-------------------|
| `Microsoft.ClearScript` 7.5.0 | Processor.csproj | Wraps Windows-only `vbscript.dll` / `jscript.dll` COM objects |
| `Nustache.Core.dll` (local assembly) | Processor `Libs/` folder | Pre-built .NET Framework DLL, not cross-platform |
| `-windows` TFM suffix | All .NET 9 projects | Restricts deployment to Windows |
| `AspNetCoreHostingModel InProcess` | WebApi.csproj | IIS-specific hosting model |
| `Microsoft.Extensions.Hosting.WindowsServices` only | TaskService.csproj | No Linux systemd support |
| `System.ServiceProcess` reference | ProcessorTests.csproj | Windows-only test dependency |
| Dead `using` imports | Several Processor files | Reference removed Windows namespaces (build errors on Linux) |

---

## ~~Phase 1: Remove Windows-Only Dependencies from Processor~~ **DONE**

### Completed

- **1.1 Dead imports** — All Windows-only `using` statements removed (System.Windows.Input, Microsoft.Web.Administration, Microsoft.ClearScript.JavaScript)
- **1.2 Nustache → Stubble** — Stubble is now the sole Mustache renderer. Nustache code path, site-property toggle, and `Libs/Nustache.Core.dll` removed. `SetIgnoreCaseOnKeyLookup(true)` preserves backward compatibility.
- **1.3 VBScript/JScript addons converted** — All scripting addons across base and external collection repos converted to C# `AddonBaseClass` assemblies
- **1.4 ClearScript removed** — `Microsoft.ClearScript` package removed from Processor.csproj. `AddonScriptController.cs` and `MainCsvScriptCompatibilityClass` deleted. `AddonController.cs` now logs an error for any remaining scripting addons instead of executing them.
- **1.5 Nustache DLL removed** — `<Reference>` and `<None>` pack items removed from Processor.csproj

---

## ~~Phase 2: Change Target Frameworks~~ **DONE**

### Completed

- **2.1 Libraries** — Processor changed from `net48;net9.0-windows` to `net48;net10.0`. IisUtil ProjectReference decoupled (now built separately; DLL copied via conditional Content item that skips on Linux).
- **2.2 Applications** — WebApi, Cli, TaskService all changed to `net10.0` (no `-windows` suffix). IisUtil stays `net10.0-windows` (Windows-only by design).
- **2.3 Test projects** — ProcessorTests and ModelsTests changed to `net10.0`. `System.ServiceProcess` reference removed from ProcessorTests.
- **2.4 Build script** — `scripts/build-core.cmd` updated: all `net9.0-windows` references changed to `net10.0` / `net10.0-windows` as appropriate.
- **2.5 Build verification** — All projects build with 0 errors on `net10.0`. CLI shows expected CA1416 warnings for Windows-only APIs (`WindowsIdentity`, `SecurityIdentifier`) that will need platform guards in a future pass.

---

## Phase 3: Add Linux Service Support (TaskService)

### 3.1 Add Systemd NuGet Package

In `source/TaskService/TaskService.csproj`, add alongside the existing Windows package:
```xml
<PackageReference Include="Microsoft.Extensions.Hosting.WindowsServices" Version="10.0.0" />
<PackageReference Include="Microsoft.Extensions.Hosting.Systemd" Version="10.0.0" />
```

### 3.2 Update Program.cs

```csharp
Host.CreateDefaultBuilder(args)
    .UseWindowsService(options => {
        options.ServiceName = "Contensive Task Service";
    })
    .UseSystemd()  // no-op on Windows, active under systemd on Linux
    .ConfigureServices(services => {
        services.AddHostedService<ContensiveWorker>();
    });
```

Both calls are safe to have simultaneously — each is a no-op when not running on its respective platform.

### 3.3 Platform-Aware Process Spawning

In `TaskRunnerController.cs`, the out-of-process task execution spawns `cc.exe`. Make it platform-aware:

```csharp
string exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cc.exe" : "cc";
```

### 3.4 CLI Task Control on Linux

The `--taskrunner on/off` and `--taskscheduler on/off` commands currently use `ServiceController` (Windows-only). On Linux, shell out to `systemctl`:

```csharp
if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
    Process.Start("systemctl", "start contensive-task");
}
```

### 3.5 Create Systemd Unit File

Create `deploy/contensive-task.service`:

```ini
[Unit]
Description=Contensive Task Service
After=network.target
Wants=network-online.target

[Service]
Type=notify
ExecStart=/opt/contensive/taskservice/TaskService
WorkingDirectory=/opt/contensive/taskservice
Restart=always
RestartSec=10
WatchdogSec=60
User=contensive
Group=contensive
Environment=DOTNET_ENVIRONMENT=Production

# Hardening
NoNewPrivileges=true
ProtectSystem=strict
ProtectHome=true
ReadWritePaths=/opt/contensive /var/log/contensive

[Install]
WantedBy=multi-user.target
```

### 3.6 Create Diagnostics Timer

Replace Windows Task Scheduler with a systemd timer for `cc --serverdiagnostic`:

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
User=contensive
Group=contensive
```

---

## Phase 4: Migrate Web Server to Nginx

### 4.1 Remove IIS-Specific Settings from WebApi

In `source/WebApi/WebApi.csproj`, remove:
```xml
<AspNetCoreHostingModel>InProcess</AspNetCoreHostingModel>
```

### 4.2 Configure Kestrel

In `Program.cs`, configure Kestrel to listen on a local port (e.g., `http://127.0.0.1:5000`). External traffic goes through Nginx.

### 4.3 Nginx Configuration (Both Platforms)

**Per-site config template:**
```nginx
server {
    listen 80;
    server_name example.com www.example.com;
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl;
    server_name example.com www.example.com;

    ssl_certificate     /path/to/fullchain.pem;
    ssl_certificate_key /path/to/privkey.pem;

    gzip on;
    gzip_proxied any;
    gzip_types text/plain text/css application/json application/javascript text/xml application/xml application/xml+rss text/javascript;
    gzip_min_length 256;

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

### 4.4 Windows: Install Nginx

1. Download Windows build from nginx.org
2. Install as a service using WinSW or NSSM
3. Place per-site configs in `C:\nginx\conf\sites-enabled\`
4. TLS certificates managed manually or via win-acme

### 4.5 Linux: Install Nginx

```bash
sudo apt update && sudo apt install nginx    # Ubuntu/Debian
sudo certbot --nginx -d example.com          # Let's Encrypt TLS
```

Place per-site configs in `/etc/nginx/sites-available/`, symlink to `sites-enabled/`.

### 4.6 Decommission IIS (Windows Migration)

For existing Windows sites migrating from IIS:
1. Install Nginx and configure the site
2. Stop the IIS site and application pool
3. Verify Nginx serves traffic correctly
4. Remove the IIS site/app pool

---

## Phase 5: Update File Paths and Deployment

### 5.1 Linux Installation Paths

| Component | Windows Path | Linux Path |
|-----------|-------------|------------|
| CLI | `C:\Program Files\Contensive\Cli\` | `/opt/contensive/cli/` |
| TaskService | `C:\Program Files\Contensive\TaskService\` | `/opt/contensive/taskservice/` |
| WebApi | `C:\Program Files\Contensive\WebApi\` | `/opt/contensive/webapi/` |
| App data | `{drive}:\Contensive\` | `/opt/contensive/data/` |

### 5.2 Update Hardcoded Paths

Review and update any hardcoded Windows paths in:
- `source/Cli/Views/ConfigureCmd.cs`
- `source/Cli/Views/UpgradeCmd.cs`
- Any code using `Environment.GetFolderPath()` or hardcoded `C:\` paths

Use `RuntimeInformation.IsOSPlatform()` to select platform-appropriate paths.

### 5.3 Linux Build and Publish Script

Create `scripts/build-linux.sh`:
```bash
dotnet publish source/WebApi/WebApi.csproj -c Release -r linux-x64 --self-contained false
dotnet publish source/Cli/Cli.csproj -c Release -r linux-x64 --self-contained false
dotnet publish source/TaskService/TaskService.csproj -c Release -r linux-x64 --self-contained false
```

### 5.4 Docker Support

Create a `Dockerfile` for containerized deployment:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /opt/contensive/webapi
COPY publish/ .
ENTRYPOINT ["dotnet", "WebApi.dll"]
```

SQL Server runs as a sibling container (via docker-compose or the `cc --configure` Docker integration from the docker-auto-install plan).

---

## Phase 6: Verification and Testing

### 6.1 Build Verification

```bash
# Cross-platform build
dotnet build -f net10.0

# Linux-specific publish
dotnet publish -c Release -r linux-x64 --self-contained false

# Run tests
dotnet test -f net10.0
```

### 6.2 Linux End-to-End Test

1. Deploy to a Linux VM or Docker container
2. Start TaskService via systemd
3. Start WebApi behind Nginx
4. Verify: page loads, static file serving (baseassets, CDN), admin login, addon execution, task scheduling
5. Run `cc -a appName -u` to verify database upgrade

### 6.3 Windows Regression Test

Verify that the same binaries still work on Windows:
1. WebApi behind Nginx (new path)
2. TaskService as a Windows Service
3. CLI operations (`cc --configure`, `cc -n`, `cc -a appName -u`)

---

## Phase Summary and Dependencies

```
Phase 1: Remove Windows Dependencies
  ├── 1.1 Dead imports (no dependencies)
  ├── 1.2 Nustache → Stubble (no dependencies)
  ├── 1.3 Convert VBScript addons to C# (no dependencies, can parallel with 1.2)
  └── 1.4 Remove ClearScript (depends on 1.3)

Phase 2: Change Target Frameworks (depends on all of Phase 1)
  ├── 2.1–2.3 Update .csproj files
  └── 2.4 Build and fix

Phase 3: Linux Service Support (depends on Phase 2)
  ├── 3.1–3.4 TaskService changes
  └── 3.5–3.6 Systemd unit files

Phase 4: Nginx Migration (can start in parallel with Phase 3)
  ├── 4.1–4.2 WebApi changes
  ├── 4.3 Nginx config templates
  └── 4.4–4.6 Platform-specific deployment

Phase 5: Paths and Deployment (depends on Phases 2–4)
  ├── 5.1–5.2 Path updates
  ├── 5.3 Build scripts
  └── 5.4 Docker support

Phase 6: Verification (depends on all prior phases)
  ├── 6.1 Build verification
  ├── 6.2 Linux E2E test
  └── 6.3 Windows regression test
```

---

## Linux Deployment Architecture (Target State)

```
Linux Host (Docker / systemd / Cloud VM)
  ├── Nginx (reverse proxy, TLS, compression, IP blocking)
  │     └── proxy_pass http://127.0.0.1:5000
  │
  ├── Kestrel (ASP.NET Core application server)
  │     └── WebApi.dll (net10.0)
  │           ├── UseStaticFiles() — baseassets
  │           ├── UseStaticFiles(CDN) — /appname/files/
  │           └── MapFallback → executeManagedRoute()
  │                 └── Processor.dll (Contensive engine, net10.0)
  │
  ├── TaskService (systemd managed)
  │     └── ContensiveWorker
  │           ├── TaskSchedulerController (5s timer)
  │           ├── TaskRunnerController (5s timer)
  │           └── MQTTSubscriptionService (30s timer)
  │
  ├── CLI (/opt/contensive/cli/cc)
  │
  └── SQL Server (Docker container or remote instance)
```

---

## Out of Scope (Future Work)

- **PostgreSQL support** — Not included in this plan. SQL Server is used on all platforms.
- **Legacy ASPX site (iisDefaultSite)** — Kept as-is for legacy Windows/IIS Framework deployments. Not ported.
- **WebApi P2 improvements** — Response headers, binary/stream responses, health check endpoint, authentication middleware. These are independent of the Linux migration.
- **.NET 10 → future version upgrades** — .NET 10 is LTS; next upgrade deferred.
- **Non-interactive `cc --configure`** — Phase 2 of the docker-auto-install plan. Independent of Linux migration.

---

## Service Management Quick Reference

| Operation | Windows (Nginx) | Linux (systemd) |
|-----------|----------------|-----------------|
| Start web server | `nssm start nginx` | `systemctl start nginx` |
| Start task service | `sc start "Contensive Task Service"` | `systemctl start contensive-task` |
| Stop task service | `sc stop "Contensive Task Service"` | `systemctl stop contensive-task` |
| View task service logs | Event Viewer | `journalctl -u contensive-task` |
| Run diagnostics | Windows Task Scheduler | `systemctl start contensive-diagnostics` |
| Upgrade database | `cc -a appName -u` | `cc -a appName -u` |
| Server status | `cc --status` | `cc --status` |

---

## Key Files Reference

| File | Purpose |
|------|---------|
| `source/Processor/Processor.csproj` | Processor library — main migration target |
| `source/WebApi/WebApi.csproj` | Web application host |
| `source/WebApi/Program.cs` | Startup, middleware, Kestrel config |
| `source/TaskService/TaskService.csproj` | Background task service |
| `source/TaskService/Program.cs` | Service host config (Windows + systemd) |
| `source/Cli/Cli.csproj` | Command-line tool |
| `source/Processor/Controllers/MustacheController.cs` | Mustache rendering (Nustache → Stubble) |
| `source/Processor/Controllers/Addon/AddonScriptController.cs` | VBScript/JScript execution (to be removed) |
| `source/Processor/Controllers/WebServerController.cs` | Web server management (IIS gated) |
| `deploy/contensive-task.service` | Systemd unit file (new) |
| `deploy/contensive-diagnostics.timer` | Systemd diagnostics timer (new) |
| `scripts/build-linux.sh` | Linux publish script (new) |
