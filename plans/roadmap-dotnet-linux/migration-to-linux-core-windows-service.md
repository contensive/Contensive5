# Migration Plan: Task Service from Windows Services to Systemd

This document describes how the Contensive Task Service (Windows Service with internal timers) maps to Linux systemd, and the code changes required.

## Current Windows Architecture

The Windows service runs three internal services on timers inside a single `ContensiveWorker` BackgroundService:

1. **TaskSchedulerController** (5-second timer) — queries `ccAggregateFunctions` for addons due to run, inserts records into `cctasks`
2. **TaskRunnerController** (5-second timer) — claims tasks from `cctasks`, executes them either in-process or by spawning `cc.exe -a "appName" --runtask "guid"`
3. **MQTTSubscriptionService** (30-second timer) — maintains MQTT connections, routes incoming messages into `cctasks` via the scheduler

---

## What Changes and What Doesn't

The timer-based architecture lives inside the .NET `BackgroundService` / `IHostedService` layer, not in Windows Services itself. `UseWindowsService()` and `UseSystemd()` only control how the .NET Generic Host integrates with the OS process lifecycle (start, stop, health notifications). The internal timers, database polling, and task execution logic are OS-agnostic.

### What stays the same

- `ContensiveWorker.cs` — unchanged. It starts the three controllers and waits for cancellation.
- `TaskSchedulerController` — unchanged. Its `System.Timers.Timer` works identically on Linux.
- `TaskRunnerController` — unchanged internally, but the out-of-process execution path needs the CLI binary name updated (see below).
- `MQTTSubscriptionService` — unchanged.
- `TaskModel`, `AddonModel`, all SQL queries — unchanged.
- The `--runtask`, `--taskscheduler`, `--taskrunner`, `--tasks` CLI commands — unchanged.

### What changes

#### 1. Program.cs — Host configuration

Replace or augment the host builder per Option B from the migration doc:

```csharp
Host.CreateDefaultBuilder(args)
    .UseWindowsService(options => {
        options.ServiceName = "Contensive Task Service";
    })
    .UseSystemd()  // no-op on Windows, active on Linux
    .ConfigureServices(services => {
        services.AddHostedService<ContensiveWorker>();
    });
```

`UseSystemd()` does three things:
- Sets the service lifetime to `SystemdLifetime` (sends `READY=1` via sd_notify, handles `SIGTERM` for graceful shutdown)
- Configures console logging to use systemd's journal format (no timestamps, severity prefixes)
- Is a no-op when not running under systemd (so safe to call always)

#### 2. TaskRunnerController — Process spawning

The out-of-process path currently spawns `cc.exe`. On Linux the binary will be `cc` (no `.exe`). The `File.Exists()` check and process start need a platform-aware name:

```csharp
string exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cc.exe" : "cc";
```

#### 3. Systemd unit file

Create `deploy/contensive-task.service`:

```ini
[Unit]
Description=Contensive Task Service
After=network.target postgresql.service
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

Key directives:

| Directive | Why |
|---|---|
| `Type=notify` | .NET's `UseSystemd()` sends `READY=1` via sd_notify when the host is fully started. Systemd won't consider the service "up" until it receives this. |
| `Restart=always` + `RestartSec=10` | Mirrors the Windows service auto-restart behavior. If the process crashes, systemd restarts it after 10 seconds. |
| `WatchdogSec=60` | Optional. If `UseSystemd()` is configured with watchdog support, .NET sends periodic keepalive pings. If systemd doesn't receive one within 60s, it restarts the service. This catches hung processes that the internal timers wouldn't detect. |
| `User=contensive` | Runs as a non-root service account. The Windows service runs as LocalSystem; on Linux a dedicated user should be created. |

#### 4. CLI `--taskrunner on/off` and `--taskscheduler on/off` commands

These currently use `ServiceController` (Windows-only) to start/stop the Windows service. On Linux, the equivalent is `systemctl start/stop contensive-task`.

**Recommended approach:** Keep `--tasks run` for console/debug mode on both platforms, but let systemd handle service lifecycle on Linux. Admins expect to use `systemctl` directly. If CLI control is desired, shell out to `systemctl`:

```csharp
if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
    Process.Start("systemctl", "start contensive-task");
}
```

---

## Service Management Command Mapping

| Windows | Linux (systemd) |
|---|---|
| `sc start "Contensive Task Service"` | `systemctl start contensive-task` |
| `sc stop "Contensive Task Service"` | `systemctl stop contensive-task` |
| `sc query "Contensive Task Service"` | `systemctl status contensive-task` |
| Install as service | `systemctl enable contensive-task` |
| Event Viewer logs | `journalctl -u contensive-task` |

---

## Execution Flow Under Systemd

```
[systemctl start contensive-task]
    |
systemd forks TaskService process
    |
.NET Host starts, UseSystemd() sends READY=1
    |
ContensiveWorker.ExecuteAsync() runs
    |
+-- TaskSchedulerController timer (5s) --> polls DB, inserts into cctasks
|
+-- TaskRunnerController timer (5s) --> claims from cctasks
|       +-- in-process: execute addon directly
|       +-- out-of-process: spawn `cc -a "app" --runtask "guid"`
|
+-- MQTTSubscriptionService timer (30s) --> MQTT -> cctasks
    |
[systemctl stop contensive-task]
    |
systemd sends SIGTERM -> .NET Host triggers cancellation
    |
ContensiveWorker.StopAsync() -> stops all three controllers
    |
Process exits, systemd records clean shutdown
```

This is functionally identical to the Windows flow. The timers, database polling, task claiming, and addon execution all work the same way. The only difference is the process lifecycle wrapper.

---

## Summary of Code Changes Required

| File | Change |
|---|---|
| `source/TaskService/TaskService.csproj` | Add `Microsoft.Extensions.Hosting.Systemd` NuGet package |
| `source/TaskService/Program.cs` | Add `.UseSystemd()` call |
| `source/Processor/Controllers/TaskRunnerController.cs` | Platform-aware binary name (`cc` vs `cc.exe`) for out-of-process execution |
| CLI on/off commands | Either add `systemctl` fallback or document as systemd-only on Linux |
| New file: `deploy/contensive-task.service` | Systemd unit file |
