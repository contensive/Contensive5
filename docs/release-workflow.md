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

```powershell
.\release-runner.ps1 -Step build   -Tier 0
.\release-runner.ps1 -Step test    -Tier 0
.\release-runner.ps1 -Step staging -Tier 0
```

Smoke test on staging, then mark and publish:

```powershell
.\release-runner.ps1 -Mark done -Step smoke -Repo Contensive5
.\release-runner.ps1 -Step publish -Tier 0
```

---

## Step 5: Update NuGet Refs and Build Tier 1

Once Tier 0 is published, Tier 1 repos can pick up the new core packages:

```powershell
.\release-runner.ps1 -Step nuget   -Tier 1
.\release-runner.ps1 -Step build   -Tier 1
.\release-runner.ps1 -Step test    -Tier 1
.\release-runner.ps1 -Step staging -Tier 1
```

Smoke test each Tier 1 repo, then mark and publish:

```powershell
.\release-runner.ps1 -Mark done -Step smoke -Repo aoMCP
.\release-runner.ps1 -Mark done -Step smoke -Repo aoCrm
.\release-runner.ps1 -Mark done -Step smoke -Repo aoDesignBlocks
.\release-runner.ps1 -Mark done -Step smoke -Repo aoEcommerce
.\release-runner.ps1 -Step publish -Tier 1
```

---

## Step 6: Update NuGet Refs and Build Tier 2

```powershell
.\release-runner.ps1 -Step nuget   -All
.\release-runner.ps1 -Step build   -Tier 2
.\release-runner.ps1 -Step test    -Tier 2
.\release-runner.ps1 -Step staging -Tier 2
```

Smoke test and publish. Mark each repo individually or in bulk:

```powershell
.\release-runner.ps1 -Mark done -Step smoke -Repo aoBlog
.\release-runner.ps1 -Mark done -Step smoke -Repo aoCalender2
# ... repeat for each Tier 2 repo ...
.\release-runner.ps1 -Step publish -Tier 2
```

---

## Step 7: Deploy Tier 3 (Data/UI Only)

Tier 3 repos have no C# build or tests (auto-skipped). Just merge, deploy to
staging, smoke test, and publish:

```powershell
.\release-runner.ps1 -Step staging -Tier 3
```

Mark each repo after verifying on staging:

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
| `nuget` | Updates `Contensive.*` PackageReference versions from `C:\NuGetLocalPackages` | Yes |
| `build` | Runs the repo's `scripts/build.ps1` (falls back to `dotnet build` if no script) | Yes |
| `test` | Runs `dotnet test` on any `*Test*.csproj` projects | Yes |
| `staging` | Runs the repo's `scripts/build-deploy-remote.cmd` | Yes |
| `smoke` | You verify on staging, then mark done or failed | Manual |
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
