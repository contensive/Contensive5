# Plan: Move /status endpoint to aoStatus addon

## Goal

Extract the `/status` remote method from the Processor (where it's hardcoded in `RouteController` and implemented in `StatusClass`) into a standalone addon at `addons/aoStatus/`, following the same folder structure as `addons/aoMcp/`.

## What's Moving

The `/status` endpoint currently lives inside the Processor project as an internal class (`Contensive.Processor.Addons.Diagnostics.StatusClass`) with a hardcoded route in `RouteController.cs`. It also depends on three diagnostics controllers and the `PerformanceMetricsController` — all internal to Processor.

### Files to move (source → new location in aoStatus):

| Current Location (Processor) | New Location (aoStatus) | Notes |
|---|---|---|
| `Addons/Diagnostics/StatusClass.cs` | `server/Status/StatusRemoteMethod.cs` | Main endpoint — must be rewritten to remove internal Processor dependencies |
| `Controllers/SecurityDiagnosticsController.cs` | `server/Status/SecurityDiagnosticsController.cs` | Already uses only `CPBaseClass` — moves cleanly |
| `Controllers/PerformanceDiagnosticsController.cs` | `server/Status/PerformanceDiagnosticsController.cs` | Already uses only `CPBaseClass` — moves cleanly |
| `Controllers/ReliabilityDiagnosticsController.cs` | `server/Status/ReliabilityDiagnosticsController.cs` | Already uses only `CPBaseClass` — moves cleanly |
| `Addons/Diagnostics/SecurityAcknowledgeAdminsClass.cs` | `server/Status/SecurityAcknowledgeAdminsRemoteMethod.cs` | Calls `SecurityDiagnosticsController.AcknowledgeAdmins()` — must move with it |
| `Models/Domain/ServerDiagnosticsStatusModel.cs` | `server/Status/ServerDiagnosticsStatusModel.cs` | Simple POCO, no dependencies |

### File NOT moving (stays in CPBase, already shared):
- `CPBase/BaseModels/StatusResponseModel.cs` — this is the public response model in the CPBaseClass NuGet package, already accessible to external addons

### Internal dependency that CANNOT move as-is:
- `Controllers/PerformanceMetricsController.cs` — this is a static, in-process metrics collector that records per-request timings from inside the Processor pipeline. It has no meaning outside the Processor process. The `metrics` section of the JSON response will need to be sourced differently (see approach below).

## Approach for Internal Dependencies

### 1. `CoreController.codeVersion()` → `cp.Version`
`CPBaseClass` exposes a public `Version` property that returns the same Contensive version string. Direct replacement.

### 2. `core.dateTimeNowMockable` → `DateTime.Now`
The mockable datetime is used for testing inside Processor. In an external addon, `DateTime.Now` is the standard approach.

### 3. `core.appConfig.name` (for PerformanceMetricsController) → `cp.Site.Name`
`CPBaseClass.Site.Name` returns the app/site name.

### 4. `((CPClass)(cp)).core` cast — ELIMINATE
StatusClass currently casts `cp` to the internal `CPClass` to access `core` for `dateTimeNowMockable`, `appConfig.name`, and `addon.execute()`. All of these have public `CPBaseClass` equivalents except the metrics controller.

### 5. `core.addon.execute(addon, ...)` for diagnostic addons → `cp.Utils.ExecuteAddon(addonGuid)`
The diagnostic addon loop queries for addons with `diagnostic>0` and executes them. `CPBaseClass.Utils.ExecuteAddon()` provides the same capability.

### 6. PerformanceMetrics — requires a new approach
`PerformanceMetricsController` is a static in-process collector. Its data (avg response time, hit count, uptime) is only meaningful inside the running Processor. **Options:**
- **Option A (Recommended)**: Expose metrics as a site property (JSON blob) written periodically by the Processor, read by aoStatus (same pattern as `ServerDiagnosticsStatus`). This requires a small change to Processor to persist metrics.
- **Option B**: Drop the `metrics` section from the external addon initially. The data is already available through other monitoring tools, and moving it requires Processor changes anyway.
- **Option C**: Add a public method to `CPBaseClass` to expose metrics. This would require changes to CPBase, Processor, and a NuGet version bump.

### 7. `.left()` extension method
This is a simple `string.Substring(0, n)` equivalent used once. Replace inline with standard string operations.

## New aoStatus Folder Structure

```
addons/aoStatus/
├── collections/
│   └── aoStatus.xml          # Collection XML with addon definitions
├── server/
│   ├── aoStatus.csproj       # netstandard2.0, same pattern as aoMcp
│   ├── aoStatus.sln
│   ├── signingKey.snk        # Generate new signing key
│   └── Status/
│       ├── StatusRemoteMethod.cs                     # Main /status endpoint
│       ├── SecurityDiagnosticsController.cs          # Security checks
│       ├── SecurityAcknowledgeAdminsRemoteMethod.cs  # Acknowledge admins endpoint
│       ├── PerformanceDiagnosticsController.cs       # Performance checks
│       ├── ReliabilityDiagnosticsController.cs       # Backup/reliability checks
│       └── ServerDiagnosticsStatusModel.cs           # Server diagnostics POCO
├── scripts/
│   ├── build.cmd
│   ├── build.ps1
│   ├── build-deploy-sprint11.cmd
│   └── build-deploy-veronica.cmd
└── ui/
    ├── cdnFiles/
    ├── layoutFiles/
    ├── privateFiles/
    └── wwwFiles/
```

## Collection XML (aoStatus.xml)

Will define two addon entries (matching what's currently in `aoBase51.xml`):

1. **"status"** addon — `RemoteMethod=Yes`, `DotNetClass=Contensive.Addons.Status.StatusRemoteMethod`
2. **"securityAcknowledgeAdmins"** addon — `RemoteMethod=Yes`, `DotNetClass=Contensive.Addons.Status.SecurityAcknowledgeAdminsRemoteMethod`

No CDefs needed (no new database tables).

## Changes to Processor (removal / cleanup)

### Remove from Processor:
1. Delete `Addons/Diagnostics/StatusClass.cs`
2. Delete `Addons/Diagnostics/SecurityAcknowledgeAdminsClass.cs`
3. Delete `Controllers/SecurityDiagnosticsController.cs`
4. Delete `Controllers/PerformanceDiagnosticsController.cs`
5. Delete `Controllers/ReliabilityDiagnosticsController.cs`
6. Delete `Models/Domain/ServerDiagnosticsStatusModel.cs`

### Modify in Processor:
7. **`Controllers/RouteController.cs`** (~line 476): Remove the hardcoded `case HardCodedPageStatus:` block. The `/status` route will now be handled by the addon's `RemoteMethod=Yes` registration (same as `/mcp`), no hardcoded route needed.
8. **`Constants.cs`** (~line 466): Remove `HardCodedPageStatus` constant (verify no other references first).
9. **`aoBase51.xml`**: Remove the "Status" addon definition (guid `{6444B5C9-36DD-43FF-978C-26650EB2333F}`) and the "SecurityAcknowledgeAdmins" addon definition — these move to `aoStatus.xml`.
10. **`Addons/OnInstallBase51.cs`** (~lines 19-24): Remove the legacy status method blocking code (this cleanup logic moves to aoStatus's own OnInstall, or is no longer needed since the addon GUID will change).

### Add to Processor (for metrics):
11. **`PerformanceMetricsController`**: Add a method or hook to periodically serialize the current metrics snapshot to a site property (e.g., `"PerformanceMetricsStatus"`), so aoStatus can read it. A natural place is after each `Record()` call (throttled to avoid excessive writes), or in the existing housekeeping/timer path.

## Implementation Steps

1. Create the aoStatus folder structure modeled after aoMcp
2. Generate GUIDs for the new collection and addon definitions
3. Create `aoStatus.xml` collection XML with the two addon definitions
4. Create `aoStatus.csproj` targeting netstandard2.0, referencing CPBaseClass, DbModels, Newtonsoft.Json, and AWSSDK.RDS
5. Create `aoStatus.sln`
6. Move and adapt `StatusRemoteMethod.cs` — rewrite to use `CPBaseClass` APIs only, read metrics from site property
7. Move diagnostics controllers — update namespaces, minimal changes needed
8. Move `ServerDiagnosticsStatusModel.cs` — update namespace only
9. Move `SecurityAcknowledgeAdminsRemoteMethod.cs` — update namespace and class reference
10. Add metrics site property write to Processor's `PerformanceMetricsController`
11. Remove files from Processor (StatusClass, diagnostics controllers, model)
12. Update `RouteController.cs` — remove hardcoded status route
13. Update `Constants.cs` — remove `HardCodedPageStatus`
14. Update `aoBase51.xml` — remove Status and SecurityAcknowledgeAdmins addon definitions
15. Update `OnInstallBase51.cs` — remove legacy status blocking code
16. Create build scripts modeled after aoMcp
17. Generate signing key for the new assembly
18. Build and verify the new aoStatus addon compiles
