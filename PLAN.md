# Plan: Make Core and Framework Builds Coexist

## Goal
Merge sprint12 into master so that `build-core.cmd` produces the new .NET 9 deployment and `build-framework.cmd` continues to produce the existing .NET Framework ASPX deployment — both from the same codebase.

## Key Insight
The `iisDefaultSite` project (VB.NET WebForms, net4.8) consumes CPBase, Models, and Processor as **NuGet packages**, not ProjectReferences. Since those libraries already multi-target (`netstandard2.0` for CPBase/Models, `net48;net9.0-windows` for Processor), the framework build can produce net48 NuGet packages for iisDefaultSite while the core build uses the net9.0 targets. No changes needed to iisDefaultSite itself.

The CLI will always be built as a core (.NET 9) application. The IIS app pool configuration is a **runtime decision** — not a compile-time one — since the same CLI binary must create both core and framework apps.

## Changes Required

### 1. WebServerController.cs — Add `bool isFramework` Parameter
**File:** `source/Processor/Controllers/WebServerController.cs`

The sprint12 change hardcodes core-style app pool settings (`ManagedRuntimeVersion = ""`, `Enable32BitAppOnWin64 = false`). Framework deployments need `"v4.0"` and `true`. Since the CLI always runs on net9.0, the Processor DLL it loads is always the net9.0 build, so `#if NETFRAMEWORK` would never be true — both `-n` and `-nf` would get the core path.

**Change:** Add a `bool isFramework` parameter (defaulting to `false`) to both `verifySite` and `verifyAppPool`:

```csharp
public void verifySite(string appName, string DomainName, string wwwRootPath, bool isFramework = false) {
    try {
        verifyAppPool(appName, isFramework);
        verifyWebsite(appName, DomainName, wwwRootPath, appName);
    } catch (Exception ex) {
        logger.Error($"{core.logCommonMessage}", ex, "verifySite");
        throw;
    }
}

public void verifyAppPool(string poolName, bool isFramework = false) {
    try {
        using ServerManager serverManager = new();
        // ... existing pool find/create logic unchanged ...
        if (isFramework) {
            appPool.ManagedRuntimeVersion = "v4.0";
            appPool.Enable32BitAppOnWin64 = true;
        } else {
            appPool.ManagedRuntimeVersion = "";
            appPool.Enable32BitAppOnWin64 = false;
        }
        appPool.ManagedPipelineMode = ManagedPipelineMode.Integrated;
        serverManager.CommitChanges();
    } catch (Exception ex) {
        logger.Error($"{core.logCommonMessage}", ex, "verifyAppPool");
        throw;
    }
}
```

### 2. BuildController.upgrade — Auto-detect Framework vs Core
**File:** `source/Processor/Controllers/Build/BuildController.cs` (~line 97)

`BuildController.upgrade` calls `verifySite` on the `isNewBuild` path (line 97). This is called after the CLI has already deployed site files to the www root. We can detect the site type by checking whether `default.aspx` exists:

- If `default.aspx` exists in `core.appConfig.localWwwPath` → framework site → pass `isFramework: true`
- Otherwise → core site → pass `isFramework: false` (default)

```csharp
// -- verify iis configuration
logger.Info($"{core.logCommonMessage},{logPrefix}, verify iis configuration");
bool isFrameworkSite = System.IO.File.Exists(
    System.IO.Path.Combine(core.appConfig.localWwwPath, "default.aspx"));
core.webServer.verifySite(core.appConfig.name, primaryDomain, core.appConfig.localWwwPath, isFrameworkSite);
```

**Why this works:** During new app creation, the CLI deploys files *before* calling `upgrade`:
- `NewAppCmd` deploys WebApi files (no `default.aspx`), then calls `upgrade` → detection returns `false` → correct
- `NewAppFrameworkCmd` deploys ASPX zip (includes `default.aspx`), then calls `upgrade` → detection returns `true` → correct

For `--upgrade` on existing apps, `isNewBuild` is `false`, so `verifySite` is never called and the app pool is not touched.

### 3. Remove `NewAppClass` Addon
**Files:**
- `source/Processor/Addons/NewApp/NewAppClass.cs` — **delete**
- `source/Processor/aoBase51.xml` — remove the `<Addon>` element with GUID `{DFE6FD4B-B361-4F9C-BA52-18F6DCFABF56}` and its contents

The `NewAppClass` addon is not referenced by any code in the codebase. It is registered in the collection XML by GUID and could theoretically be invoked dynamically, but app creation is always done through the CLI. The addon contains stale framework-only logic (`iisDefaultDoc = "default.aspx"`, no `isFramework` parameter support). Remove it to avoid confusion.

### 4. CLI — Add `--newappframework` Command for ASPX Deployment
**Files:**
- `source/Cli/Views/NewAppCmd.cs` — keep as-is (core WebApi deployment for `-n` / `--newapp`)
- `source/Cli/Views/NewAppFrameworkCmd.cs` — **new file** containing the framework ASPX deployment logic
- `source/Cli/mainClass.cs` — add new case for `-nf` / `--newappframework`
- `source/Cli/Views/HelpCmd.cs` — add help text reference for the new command

The user chooses the deployment style at runtime:

- `cc -n appName domainName` → creates a **core** app (WebApi deployment, `ManagedRuntimeVersion = ""`, no default doc)
- `cc -nf appName domainName` → creates a **framework** app (ASPX deployment, `ManagedRuntimeVersion = "v4.0"`, `default.aspx`)

#### 4a. New file: `source/Cli/Views/NewAppFrameworkCmd.cs`

Create this file based on the original master `NewAppCmd.cs` logic (before sprint12 changes), with these specifics:
- Class name: `NewAppFrameworkCmd`
- `helpText`: documents `-nf` / `--newappframework` with description noting this creates a .NET Framework ASPX site (transition support)
- `iisDefaultDoc = "default.aspx"`
- Program files verification checks for `defaultaspxsite.zip`: `cp.core.programFiles.fileExists("defaultaspxsite.zip")`
- Calls `cp.core.webServer.verifySite(appName, domainName, cp.core.appConfig.localWwwPath, isFramework: true)` — passing `true` to get `"v4.0"` app pool config
- Site deployment uses the original ASPX zip extraction logic:
  1. Copy `defaultaspxsite.zip` from programFiles to tempFiles
  2. Unzip it
  3. Find `Content/Web.config` via `getZipSrcTempPath`
  4. Copy extracted content to www root
  5. Cleanup temp files
  6. Copy `WebAppSettings-Sample.config` → `WebAppSettings.config` and `WebRewrite-Sample.config` → `WebRewrite.config` if they don't exist
- Include the `getZipSrcTempPath` helper method (copy from existing `NewAppCmd.cs:423-443`)
- Then calls `BuildController.upgrade(cp.core, true, true)` — `BuildController` will auto-detect `default.aspx` in the www root and pass `isFramework: true` to `verifySite`

#### 4b. Modify `source/Cli/mainClass.cs`

Add a new case in the switch statement (after the existing `--newapp` case, ~line 227):

```csharp
case "--newappframework":
case "-nf":
    //
    // -- require elevated permissions
    if (!WindowsIdentity.GetCurrent().Owner.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid)) {
        Console.WriteLine("The --newappframework (-nf) command requires elevated permissions (run as administrator).");
        return;
    }
    //
    // -- start the new framework app wizard
    appName = getNextCmdArg(args, ref argPtr);
    string fwDomainName = getNextCmdArg(args, ref argPtr);
    await NewAppFrameworkCmd.executeAsync(appName, fwDomainName);
    break;
```

#### 4c. Modify `source/Cli/Views/HelpCmd.cs`

Add after line 18 (`Console.Write(NewAppCmd.helpText);`):
```csharp
Console.Write(NewAppFrameworkCmd.helpText);
```

### 5. TaskService — Keep Sprint12 As-Is (No Changes Needed)
The framework build at `build-framework.cmd:234` already builds TaskService with `-p:TargetFramework=net9.0-windows`, which matches the sprint12 SDK-style csproj.

**No changes needed.**

### 6. build-framework.cmd — No Changes Needed
The framework build script builds each project with explicit `-p:TargetFramework` flags. The sprint12 csproj changes are compatible.

**No changes needed.**

### 7. Cli.csproj — No Changes Needed
Sprint12 removed the TaskService ProjectReference. The CLI doesn't directly call TaskService types.

**No changes needed.**

### 8. WebApi Changes — Keep Sprint12 As-Is
The framework build does not build or use WebApi.

**No changes needed.**

### 9. Processor Library Changes — Keep Sprint12 As-Is
All Processor changes are additive and backward-compatible.

**No changes needed.**

## Summary of Files to Modify

| File | Change |
|------|--------|
| `source/Processor/Controllers/WebServerController.cs` | Add `bool isFramework = false` parameter to `verifySite` and `verifyAppPool`; use runtime flag for app pool settings |
| `source/Processor/Controllers/Build/BuildController.cs` | Auto-detect framework vs core via `default.aspx` check; pass `isFramework` to `verifySite` |
| `source/Processor/Addons/NewApp/NewAppClass.cs` | **Delete file** |
| `source/Processor/aoBase51.xml` | Remove `NewApp` addon entry (GUID `{DFE6FD4B-B361-4F9C-BA52-18F6DCFABF56}`) |
| `source/Cli/Views/NewAppFrameworkCmd.cs` | **New file** — framework ASPX deployment command (passes `isFramework: true`) |
| `source/Cli/mainClass.cs` | Add `--newappframework` / `-nf` case in command switch |
| `source/Cli/Views/HelpCmd.cs` | Add `NewAppFrameworkCmd.helpText` to help output |

**Total: 2 files modified + 1 file deleted + 1 XML updated in Processor, 1 new file + 2 files modified in CLI.** Everything else from sprint12 works as-is for both builds.

## Verification Steps

1. Run `build-framework.cmd /nopause` — should produce:
   - NuGet packages (CPBase, Models, Processor for net48)
   - iisDefaultSite ASPX deployment zip (via MSBuild ContensiveAspx.sln)
   - CLI installer MSI (net48 target)
   - TaskService (net9.0-windows)

2. Run `build-core.cmd /nopause` — should produce:
   - WebApi published output
   - CLI published output (net9.0)
   - TaskService published output
   - Distribution zip with install scripts

3. Test CLI commands:
   - `cc -n testCoreApp testcore.local` → deploys WebApi files, app pool = "No Managed Code" (`ManagedRuntimeVersion = ""`)
   - `cc -nf testFrameworkApp testfw.local` → deploys defaultaspxsite.zip, app pool = v4.0 (`ManagedRuntimeVersion = "v4.0"`)
   - `cc -u` → upgrades existing apps, does not touch app pool settings (isNewBuild=false skips verifySite)
