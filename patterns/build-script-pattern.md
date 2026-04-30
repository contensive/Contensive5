
# Build Script Pattern

> All patterns and API reference: [Patterns Index](https://raw.githubusercontent.com/contensive/Contensive5/refs/heads/master/patterns/index.md)

## Overview

A build script automates compiling, packaging, and deploying a Contensive addon collection. It produces a versioned deployment folder containing the collection zip.

Each addon repo contains two files in its `scripts/` folder:

- **`build.cmd`** — a thin entry point that calls `build.ps1` via PowerShell. Handles the `/nopause` flag and prints a `BUILD FAILED` banner on error. Identical across all repos; copy verbatim.
- **`build.ps1`** — repo-specific configuration only. Imports the shared build library from Contensive5 and calls `Invoke-ContensiveBuild` with the repo's parameters.

All build step logic lives in the shared PowerShell module:

```
C:\Git\Contensive5\scripts\contensive-build.psm1
```

To change the build process for every repo, edit that module. No per-repo changes are needed.

---

## Folder Convention

Build scripts assume the following project folder structure:

```
Git/
  projectName/
    scripts/
      build.cmd           <-- entry point, calls build.ps1
      build.ps1           <-- repo config, calls Invoke-ContensiveBuild
    collections/
      collectionName/     <-- collection XML and staged build artifacts
    server/               <-- Visual Studio solution and projects
      projectName/
    ui/
      wwwFiles/           <-- copied to the www root on install
      cdnFiles/           <-- accessible via a CDN URL the code generates
      privateFiles/       <-- accessible only by server-side code
      layoutFiles/        <-- HTML layout files (optional)
    helpFiles/            <-- markdown help/documentation files (optional)
```

- All folders that install on servers must be lowercase.
- The `design/` folder stores working design files that are not deployed.
- UI sub-folder contents map directly to the corresponding server file system. For example, `ui/wwwFiles/img/logo.png` installs to `/img/logo.png` on the server www root.
- `ui/layoutFiles/` contains HTML layout fragments managed by the Contensive layout system.

---

## The Shared Build Library

**Location:** `C:\Git\Contensive5\scripts\contensive-build.psm1`

### `Invoke-ContensiveBuild` — main entry point

Runs all build steps in order. Addon repos call this with their configuration.

| Parameter | Required | Description |
|---|---|---|
| `CollectionName` | Yes | Short name for the collection zip and banner label (e.g. `'FMA'`) |
| `CollectionPath` | Yes | Absolute path to the collection folder containing the collection XML |
| `SolutionPath` | Yes | Absolute path to the `.sln` file |
| `BinPath` | Yes | Absolute path to the Release bin folder the build produces |
| `DeploymentRoot` | Yes | Root folder where versioned deployment sub-folders are created |
| `CollectionDlls` | Yes | String array of DLL (and `.dll.config`) file names to include in the collection |
| `CleanFolders` | No | String array of folders to delete before building (typically `bin\` and `obj\` for each project). Defaults to empty. |
| `UiPath` | No | Root folder containing UI asset sub-folders. If omitted, UI packaging is skipped. |
| `UiAssetFolders` | No | Sub-folder names under `UiPath` to zip into the collection. Defaults to `@('wwwFiles','cdnFiles','privateFiles','layoutFiles')`. |
| `HelpFilesPath` | No | Path to the `helpFiles/` folder at the repo root. If omitted, help file packaging is skipped. |
| `PackagesDirectory` | No | NuGet packages restore target directory. |
| `Zip7Path` | No | Path to `7z.exe`. Defaults to `'C:\Program Files\7-Zip\7z.exe'`. |

### Exported helper functions

These are also available for repos that extend the standard flow:

| Function | Description |
|---|---|
| `Get-ContensiveVersion` | Returns a date-based version string `YY.M.D.R` that doesn't collide with existing deployment folders |
| `Find-MSBuild` | Locates `msbuild.exe` via vswhere; throws if not found |
| `New-DeploymentFolder` | Creates a versioned deployment folder and returns its path |
| `Invoke-ZipFolder` | Zips the *contents* of a folder into a zip file using 7-Zip |
| `Invoke-MSBuildSolution` | Runs NuGet restore then MSBuild `/t:Rebuild /p:Configuration=Release` (legacy .NET Framework / packages.config pattern) |

---

## Build Steps (executed by `Invoke-ContensiveBuild`)

### Step 1 — Resolve version, deployment folder, and tools

- Generates a version string in `YY.M.D.R` format from the current date. The revision increments if a folder for that version already exists under `DeploymentRoot`.
- Creates the versioned deployment folder.
- Locates `msbuild.exe` via vswhere.

### Step 2 — Clean build output

- Deletes every folder listed in `CleanFolders`.
- Removes any stale DLLs, asset zips, and the collection zip from the collection staging folder.

### Step 3 — Package UI assets

Skipped if `UiPath` is not provided.

For each folder in `UiAssetFolders`, zips the folder contents into `{asset}.zip` in the collection folder. Each zip is named after its source folder (`wwwFiles.zip`, `cdnFiles.zip`, etc.). Missing source folders are created automatically so the zip is valid even if empty.

The collection XML must include a resource entry for each asset zip that should be installed:

```xml
<Resource Name="wwwFiles.zip"    Type="wwwFiles"    Path="" />
<Resource Name="cdnFiles.zip"    Type="cdnFiles"    Path="" />
<Resource Name="privateFiles.zip" Type="privateFiles" Path="" />
<Resource Name="layoutFiles.zip" Type="layoutFiles"  Path="" />
<Resource Name="helpFiles.zip"   Type="helpFiles"    Path="" />
```

Only include resource entries for the asset types the repo actually uses.

### Step 4 — Build the solution

Runs NuGet restore then MSBuild in Release configuration (`/t:Rebuild /p:Configuration=Release /p:Platform="Any CPU"`). This uses the legacy packages.config NuGet pattern. Throws on failure.

### Step 5 — Assemble the collection zip and deploy

- Copies each file in `CollectionDlls` from `BinPath` into the collection staging folder.
- Zips the entire collection staging folder (XML, DLLs, and asset zips) into `{CollectionName}.zip`.
- Copies the collection zip to the versioned deployment folder.

### Step 6 — Clean staging files

Removes DLLs and asset zips from the collection folder, leaving only the collection XML and the final collection zip. The collection zip stays so it can be referenced or re-deployed without a rebuild.

---

## Version Number Format

Versions follow `YY.M.D.R`:

- `YY` — two-digit year (e.g. `26`)
- `M` — month without leading zero (e.g. `4`)
- `D` — day without leading zero (e.g. `4`)
- `R` — revision starting at `1`, incrementing if the deployment folder already exists

Example: `26.4.4.1`

---

## Interactive vs Automation Mode (`/nopause`)

`build.cmd` accepts an optional `/nopause` flag:

- **Manual** (`build.cmd`) — pauses after completion so the window stays open.
- **Automation** (`build.cmd /nopause`) — exits immediately with code `0` (success) or `1` (failure). Use this when invoking from Claude Code or CI.

```bash
# From bash (Claude Code)
/c/Windows/System32/cmd.exe //c "cd /d C:\Git\projectName\scripts & build.cmd /nopause" 2>&1
```

---

## Implementing the Pattern in an Addon Repo

### `scripts/build.cmd` — copy verbatim

```cmd
@echo off
setlocal

set nopause=0
if /i "%~1"=="/nopause" set nopause=1

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1"
set exitcode=%errorlevel%

if not "%exitcode%"=="0" (
    echo.
    echo ========================================
    echo BUILD FAILED
    echo ========================================
)

if "%nopause%"=="0" pause
exit /b %exitcode%
```

### `scripts/build.ps1` — repo-specific config

```powershell
#Requires -Version 5.1
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot '..\..\Contensive5\scripts\contensive-build.psm1') -Force

$projectRoot = (Resolve-Path "$PSScriptRoot\..").Path

Invoke-ContensiveBuild `
    -CollectionName    'MyAddon' `
    -CollectionPath    "$projectRoot\Collections\MyAddon" `
    -SolutionPath      "$projectRoot\server\MyAddon.sln" `
    -BinPath           "$projectRoot\server\MyAddon\bin\Release" `
    -DeploymentRoot    'C:\deployments\myaddon' `
    -CollectionDlls    @(
                           'MyAddon.dll'
                           'MyAddon.dll.config'
                           'CPBase.dll'
                       ) `
    -CleanFolders      @(
                           "$projectRoot\server\MyAddon\bin"
                           "$projectRoot\server\MyAddon\obj"
                       ) `
    -UiPath            "$projectRoot\ui" `
    -PackagesDirectory "$projectRoot\server\packages"
```

Only supply the parameters relevant to the repo. `UiPath` and `PackagesDirectory` can be omitted if the repo has no UI assets or uses default package restore paths.

---

## Migrating from the Previous CMD Pattern

The previous pattern used a standalone `build.cmd` that contained all build logic inline. To migrate:

1. **Delete** the existing `scripts/build.cmd` content.
2. **Create** `scripts/build.cmd` using the verbatim template above.
3. **Create** `scripts/build.ps1` with the repo's configuration block (see template above).
   - Map the old `collectionName`, `collectionPath`, `binPath`, and `deploymentFolderRoot` variables to the corresponding `Invoke-ContensiveBuild` parameters.
   - List the specific DLL filenames the old script copied in `CollectionDlls`.
   - List the `bin\` and `obj\` folders the old script deleted in `CleanFolders`.
   - If the old script zipped UI assets, set `UiPath` to the `ui\` folder path.
4. **Remove** the old version number generation block, the 7-Zip calls, the MSBuild/dotnet calls, and the `:builddone` / `:builderror` labels — all of these are now handled by the module.

The build is invoked the same way after migration: run `build.cmd` or `build.cmd /nopause`.
