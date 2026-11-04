# Monthly Release Workflow

Step-by-step instructions for running the Contensive monthly release using the
deployment pipeline scripts in `scripts/release/`.

All commands run from an **Administrator PowerShell** prompt.

```
cd C:\Git\Contensive5\scripts\release
```

---

## Prerequisites

- All repos cloned under `C:\Git\` (each repo should already have `main` and `dev` branches from normal development)
- Visual Studio installed (MSBuild required for some builds)
- 7-Zip installed at `C:\Program Files\7-Zip\7z.exe`
- `C:\NuGetLocalPackages` folder exists (local NuGet feed)
- Feature branches merged into `dev` before starting the release

---

## Step 1: Generate the Release Manifest

Run this once at the start of each release cycle:

```powershell
.\new-release.ps1
```

This scans all repos and creates `release-manifest.yml` for the current month.
To target a specific month:

```powershell
.\new-release.ps1 -Month 2027-01
```

---

## Step 2: Check the Dashboard

Use this at any time to see where things stand:

```powershell
.\release-runner.ps1 -Status
```

---

## Step 3: Merge dev into main

Work through each tier in order. A tier cannot start `nuget` until the previous
tier is fully published.

```powershell
.\release-runner.ps1 -Step merge -Tier 0
.\release-runner.ps1 -Step merge -Tier 1
.\release-runner.ps1 -Step merge -Tier 2
.\release-runner.ps1 -Step merge -Tier 3
```

If a merge fails due to conflicts, resolve them manually in that repo, then
mark the step done:

```powershell
.\release-runner.ps1 -Mark done -Step merge -Repo <repoName>
```

---

## Step 4: Build and Publish Tier 0 (Contensive5)

Tier 0 must be fully published before any other tier can update NuGet packages.

The **build** step calls `scripts/build-core.cmd`, which:
- Builds and packs CPBase, DbModels, and Processor NuGet packages
- Copies packages to `C:\NuGetLocalPackages` (the local feed)
- Builds WebApi, CLI, TaskService, iisDefaultSite
- Creates a deployment package at `C:\Deployments\Contensive5-Core\{version}\contensive.zip`

The **test** step automatically:
1. Extracts the deployment zip
2. Runs `upgrade-full.ps1` to upgrade the local server and database (requires Administrator)
3. Runs the test projects

**Important:** The test step requires an **Administrator PowerShell** because
`upgrade-full.ps1` installs components, upgrades databases (`cc -u`), and
deploys to IIS sites. If the runner is not elevated, it will print manual
instructions and leave the step as `pending`.

```powershell
.\release-runner.ps1 -Step build   -Tier 0
.\release-runner.ps1 -Step test    -Tier 0
.\release-runner.ps1 -Step staging -Tier 0
```

The **staging** step auto-skips for Tier 0 (the local machine is already upgraded).

Verify the local site, then mark smoke and publish:

```powershell
.\release-runner.ps1 -Mark done -Step smoke -Repo Contensive5
.\release-runner.ps1 -Step publish -Tier 0
```

---

## Step 5: Build and Publish Tier 1

Tier 1 repos depend on each other (aoMCP → aoCrm → aoDesignBlocks → aoEcommerce),
so each must be fully published before the next can start. Process them one at a time
in this order:

```powershell
# aoMCP (no Tier 1 dependencies)
.\release-runner.ps1 -Step nuget   -Repo aoMCP
.\release-runner.ps1 -Step build   -Repo aoMCP
.\release-runner.ps1 -Step test    -Repo aoMCP
.\release-runner.ps1 -Step staging -Repo aoMCP
.\release-runner.ps1 -Mark done -Step smoke -Repo aoMCP
.\release-runner.ps1 -Step publish -Repo aoMCP

# aoCrm (depends on aoMCP)
.\release-runner.ps1 -Step nuget   -Repo aoCrm
.\release-runner.ps1 -Step build   -Repo aoCrm
.\release-runner.ps1 -Step test    -Repo aoCrm
.\release-runner.ps1 -Step staging -Repo aoCrm
.\release-runner.ps1 -Mark done -Step smoke -Repo aoCrm
.\release-runner.ps1 -Step publish -Repo aoCrm

# aoDesignBlocks (depends on aoCrm, aoMCP)
.\release-runner.ps1 -Step nuget   -Repo aoDesignBlocks
.\release-runner.ps1 -Step build   -Repo aoDesignBlocks
.\release-runner.ps1 -Step test    -Repo aoDesignBlocks
.\release-runner.ps1 -Step staging -Repo aoDesignBlocks
.\release-runner.ps1 -Mark done -Step smoke -Repo aoDesignBlocks
.\release-runner.ps1 -Step publish -Repo aoDesignBlocks

# aoEcommerce (depends on aoDesignBlocks)
.\release-runner.ps1 -Step nuget   -Repo aoEcommerce
.\release-runner.ps1 -Step build   -Repo aoEcommerce
.\release-runner.ps1 -Step test    -Repo aoEcommerce
.\release-runner.ps1 -Step staging -Repo aoEcommerce
.\release-runner.ps1 -Mark done -Step smoke -Repo aoEcommerce
.\release-runner.ps1 -Step publish -Repo aoEcommerce
```

---

## Step 6: Update NuGet Refs and Build Tier 2

```powershell
.\release-runner.ps1 -Step nuget   -All
.\release-runner.ps1 -Step build   -Tier 2
.\release-runner.ps1 -Step test    -Tier 2
.\release-runner.ps1 -Step staging -Tier 2
```

Verify on the local site and publish. Mark each repo individually or in bulk:

```powershell
.\release-runner.ps1 -Mark done -Step smoke -Repo aoBlog
.\release-runner.ps1 -Mark done -Step smoke -Repo aoCalender2
# ... repeat for each Tier 2 repo ...
.\release-runner.ps1 -Step publish -Tier 2
```

---

## Step 7: Deploy Tier 3 (Data/UI Only)

Tier 3 repos have no C# build or tests (auto-skipped). Just merge, deploy to
the local site, verify, and publish:

```powershell
.\release-runner.ps1 -Step staging -Tier 3
```

Mark each repo after verifying on the local site:

```powershell
.\release-runner.ps1 -Mark done -Step smoke -Repo aoBootstrap
.\release-runner.ps1 -Mark done -Step smoke -Repo aoJQuery
# ... repeat for each Tier 3 repo ...
.\release-runner.ps1 -Step publish -Tier 3
```

---

## Step 8: Final Report

Generate a summary of the completed release:

```powershell
.\release-runner.ps1 -Report
```

Commit the final manifest to preserve the audit trail:

```powershell
cd C:\Git\Contensive5
git add scripts/release/release-manifest.yml
git commit -m "Release 2026-11 complete"
```

---

## Handling Failures

### Retry a failed step

```powershell
.\release-runner.ps1 -Step build -Repo aoBlog -Retry
```

This resets the step from `failed` to `pending` and re-executes it.

### Manually override a status

```powershell
.\release-runner.ps1 -Mark done    -Step build -Repo aoBlog
.\release-runner.ps1 -Mark skipped -Step test  -Repo aoBlog
.\release-runner.ps1 -Mark failed  -Step smoke -Repo aoBlog
```

### Dry-run a NuGet update

Preview what package versions would change without modifying files:

```powershell
.\update-contensive-packages.ps1 -RepoPath C:\Git\aoDesignBlocks -WhatIf
```

### Run a single repo through the full pipeline

```powershell
.\release-runner.ps1 -Step merge   -Repo aoDesignBlocks
.\release-runner.ps1 -Step nuget   -Repo aoDesignBlocks
.\release-runner.ps1 -Step build   -Repo aoDesignBlocks
.\release-runner.ps1 -Step test    -Repo aoDesignBlocks
.\release-runner.ps1 -Step staging -Repo aoDesignBlocks
.\release-runner.ps1 -Mark done    -Step smoke -Repo aoDesignBlocks
.\release-runner.ps1 -Step publish -Repo aoDesignBlocks
```

---

## Step Pipeline Reference

| Step | What happens | Automated? |
|------|-------------|-----------|
| `merge` | `git checkout main && git merge origin/dev` | Yes |
| `nuget` | Updates `Contensive.*` PackageReference versions from `C:\NuGetLocalPackages` (skipped for repos with no dependencies) | Yes |
| `build` | Tier 0: runs `build-core.cmd` (NuGet packages + full platform build). Others: runs `scripts/build.ps1` (falls back to `dotnet build`) | Yes |
| `test` | Tier 0: extracts deployment zip, runs `upgrade-full.ps1` to upgrade local server/database, then runs tests (requires Admin). Others: runs `dotnet test` on `*Test*.csproj` projects | Yes |
| `staging` | Tier 0: auto-skipped (local machine already upgraded in test step). Others: runs `scripts/build-deploy-local.cmd` to install the addon on the local Contensive site | Yes |
| `smoke` | You verify the addon on the local site, then mark done or failed | Manual |
| `publish` | Runs the repo's `scripts/deploy-to-addon-library.cmd` | Yes |

## Step Statuses

| Status | Meaning |
|--------|---------|
| `pending` | Not started |
| `done` | Completed successfully |
| `failed` | Completed with errors (see notes in manifest) |
| `skipped` | Intentionally skipped (e.g., no tests to run, Tier 3 build) |
| `blocked` | Waiting on a dependency |

## Tier Definitions

| Tier | Description | Examples |
|------|-------------|---------|
| 0 | Foundation - produces core NuGet packages | Contensive5 |
| 1 | Shared libraries - produces packages consumed by other addons | aoMCP, aoCrm, aoDesignBlocks, aoEcommerce |
| 2 | Leaf addons with C# code - consumes packages but doesn't produce any | aoBlog, aoHtmlImport, aoNewsletter2, etc. |
| 3 | Data/UI-only collections - no C# build | aoBootstrap, aoJQuery, aoSpider, etc. |

---

## Files

```
scripts/release/
  new-release.ps1                 # generates a fresh manifest for a new month
  release-runner.ps1              # main entry point: status, execute steps, mark
  update-contensive-packages.ps1  # updates Contensive.* NuGet refs in a repo
  repo-catalog.yml                # stable metadata: tiers, package relationships
  release-manifest.yml            # current release state (committed monthly)
  generate-stubs.ps1              # one-time stub generator (already run)
```
