# Deployment Pipeline Plan

Phase 1 of a three-phase plan to automate the monthly Contensive release process.
This phase replaces the paper checklist with a machine-readable manifest and PowerShell
scripts that automate the mechanical steps while preserving manual gates for smoke testing.

Related plans:
- [test-coverage-pipeline.md](test-coverage-pipeline.md) — Phase 2: add test projects to all repos
- [cicd-pipeline.md](cicd-pipeline.md) — Phase 3: move repos to GitHub Actions CI/CD

---

## Problem Statement

The monthly release touches ~52 addon repos plus the Contensive5 core. Each repo
goes through 7 steps in dependency order. Today this is tracked on paper, making it
easy to lose track, skip a repo, or build out of order. The NuGet dependency chain
means building in the wrong order produces broken packages.

---

## Phase 0: Discover the Full Addon List

Before building any tooling, we need a verified inventory of every repo that
participates in the release. The initial candidate list is every `ao*` directory
under `C:\Git`, plus `Contensive5` itself.

### Step 0.1 — Scan and classify repos

Scan all `ao*` repos under `C:\Git` for:
- Presence of a `.csproj` file (has compilable code vs. data/UI only)
- NuGet packages produced (`GeneratePackageOnBuild`, `PackageId`)
- NuGet packages consumed (`PackageReference` with `Contensive.*` prefix)
- Presence of a test project
- Presence of `scripts/build.ps1` (uses shared build infrastructure)
- Git branch state (`main`, `dev`, feature branches)

### Step 0.2 — Assign tiers

Every repo falls into one of four tiers based on what it produces and consumes:

| Tier | Description | Build order |
|------|-------------|-------------|
| 0 | Foundation — produces core packages (CPBaseClass, DbModels, Processor) | First |
| 1 | Shared libraries — produces packages consumed by other addons | After Tier 0 |
| 2 | Leaf addons with C# code — consumes packages but produces none others depend on | After their dependencies |
| 3 | Data/UI-only collections — no C# build, just collection XML and assets | Any time |

Based on the current repo scan, the tiers are:

**Tier 0 — Foundation**
| Repo | Produces |
|------|----------|
| Contensive5 | Contensive.CPBaseClass, Contensive.DbModels, Contensive.Processor |

**Tier 1 — Shared Libraries (build in this order)**
| Repo | Produces | Depends on |
|------|----------|-----------|
| aoMCP | Contensive.Addons.Mcp.Shared | CPBaseClass, DbModels |
| aoCrm | Contensive.CRM | CPBaseClass, DbModels, Processor, Mcp.Shared |
| aoDesignBlocks | Contensive.DesignBlockBase | CPBaseClass, DbModels, CRM, Mcp.Shared |
| aoEcommerce | Contensive.EcommerceApi | CPBaseClass, DbModels, DesignBlockBase |

**Tier 2 — Leaf Addons with C# Code (~26 repos)**
These all consume Contensive packages but don't produce packages that others depend on:
aoAddonManager, aoAnalytics, aoBlog, aoCalender2, aoCodeMirrorEditor,
aoContactManager, aoContentPortal, aoDashboard, aoDistanceLearning,
aoFilemanager, aoFormWizard, aoHelpCenter, aoHtmlImport, aoImportWizard,
aoLibrary, aoMeetingManager, aoMemberBoss, aoMembershipApplication,
aoMembershipManager, aoMenuCrm, aoMenuing, aoNewsletter2, aoPageBuilder,
aoPersonalization, aoPortalFramework, aoReCAPTCHA, aoRedactorEditor,
aoReporting, aoRSS, aoSamples, aoSecurity, aoShare, aoSiteMonitor,
aoTextSearch2, aoThemes, aoToolPanel, aoTools, aoWhosOnline

**Tier 3 — Data/UI-Only Collections (~25 repos)**
No C# code, just collection XML and UI assets:
aoAngularJs, aoArticleLibrary, aoBootstrap, aoCallToAction, aoCatalog,
aoDonations, aoEmailBlockedReport, aoEmailBounceProcess, aoEvents,
aoGoogleAnalytics, aoGridly, aoGridstack, aoJQuery, aoLists,
aoLoginLightbox, aoMeetingManagerMobileApp, aoNewsStories, aoOwlCarousel,
aoPageExtension, aoPageNavigation2, aoPeopleMergeTool, aoPrefs,
aoProcessEmail, aoSEOSiteMap, aoSocialMedia, aoSpider, aoTemplateManager,
aoVisitTracking

**Excluded** (legacy/retired — do not include in release):
aoCrm-old, aoBannerCampaigns, aoForum, aoIntercomm, aoMessageBoard,
aoRealEstateFeatures, aoTooltips

### Step 0.3 — Verify exclusions

Manually review the excluded list. Some repos may appear inactive but still be
deployed to production sites. Check the `addons/index.md` in agency-ops for
status (active, legacy, retired).

---

## Phase 1: Release Manifest (Recommendation 1)

### Deliverable

A YAML file `scripts/release/release-manifest.yml` that is the single source of
truth for the current release cycle. Both humans and scripts read/write it.

### Schema

```yaml
release: "2026-11"           # month label
created: "2026-11-01"
status: in-progress          # in-progress | complete | abandoned

repos:
  - name: Contensive5
    path: C:\Git\Contensive5
    tier: 0
    produces:
      - Contensive.CPBaseClass
      - Contensive.DbModels
      - Contensive.Processor
    depends_on: []
    has_tests: true
    has_build_script: true
    steps:
      merge:   { status: pending, timestamp: null, notes: "" }
      nuget:   { status: pending, timestamp: null, notes: "" }
      build:   { status: pending, timestamp: null, notes: "" }
      test:    { status: pending, timestamp: null, notes: "" }
      staging: { status: pending, timestamp: null, notes: "" }
      smoke:   { status: pending, timestamp: null, notes: "" }
      publish: { status: pending, timestamp: null, notes: "" }

  - name: aoMCP
    path: C:\Git\aoMCP
    tier: 1
    produces:
      - Contensive.Addons.Mcp.Shared
    depends_on:
      - Contensive.CPBaseClass
      - Contensive.DbModels
    has_tests: false
    has_build_script: true
    steps:
      merge:   { status: pending, timestamp: null, notes: "" }
      nuget:   { status: pending, timestamp: null, notes: "" }
      build:   { status: pending, timestamp: null, notes: "" }
      test:    { status: pending, timestamp: null, notes: "" }
      staging: { status: pending, timestamp: null, notes: "" }
      smoke:   { status: pending, timestamp: null, notes: "" }
      publish: { status: pending, timestamp: null, notes: "" }

  # ... one entry per repo, sorted by tier then alphabetically
```

### Step statuses

- `pending` — not started
- `done` — completed successfully
- `failed` — completed with errors (see notes)
- `skipped` — intentionally skipped (e.g., no tests to run)
- `blocked` — waiting on a dependency

### Implementation tasks

1. Create `scripts/release/` directory in Contensive5
2. Write a PowerShell script `scripts/release/new-release.ps1` that:
   - Scans `C:\Git\ao*` repos and classifies them (Step 0.1 logic)
   - Generates a fresh `release-manifest.yml` for the given month
   - Populates tier, produces, depends_on, has_tests, has_build_script from live repo data
   - All steps default to `pending`
3. Write a `scripts/release/repo-catalog.yml` that holds the stable metadata (tier
   assignments, exclusions, manual overrides) so `new-release.ps1` doesn't re-scan
   everything from scratch every month

---

## Phase 2: Release Runner with Status Dashboard (Recommendation 2)

### Deliverable

A PowerShell script `scripts/release/release-runner.ps1` that reads the manifest
and either shows status or executes a step.

### Usage

```powershell
# Show the release dashboard
.\release-runner.ps1 -Status

# Execute a step for all repos in a tier
.\release-runner.ps1 -Step merge -Tier 0
.\release-runner.ps1 -Step merge -Tier 1

# Execute a step for one specific repo
.\release-runner.ps1 -Step build -Repo aoDesignBlocks

# Execute a step for all ready repos (dependencies met)
.\release-runner.ps1 -Step nuget -All
```

### Dashboard output

```
Release 2026-11 — Status Dashboard
=======================================

TIER 0 — Foundation
  Contensive5          merge:DONE  nuget:DONE  build:DONE  test:DONE  staging:----  smoke:----  publish:----

TIER 1 — Shared Libraries
  aoMCP                merge:DONE  nuget:DONE  build:DONE  test:SKIP  staging:----  smoke:----  publish:----
  aoCrm                merge:DONE  nuget:----  build:----  test:----  staging:----  smoke:----  publish:----
  aoDesignBlocks       merge:DONE  nuget:DONE  build:DONE  test:FAIL  staging:----  smoke:----  publish:----
  aoEcommerce          merge:DONE  nuget:----  build:----  test:----  staging:----  smoke:----  publish:----

TIER 2 — Leaf Addons (0/26 published)
  aoAddonManager       merge:----  nuget:----  build:----  test:----  staging:----  smoke:----  publish:----
  aoBlog               merge:----  nuget:----  build:----  test:----  staging:----  smoke:----  publish:----
  ...

TIER 3 — Data/UI Only (0/25 published)
  aoAngularJs          merge:----  nuget:----  build:SKIP  test:SKIP  staging:----  smoke:----  publish:----
  ...

Summary: 2/52 repos published | Tier 0: DONE | Tier 1: 1/4 | Tier 2: 0/26 | Tier 3: 0/25
```

### Step implementations

Each step maps to specific commands:

| Step | What it does | Automated? |
|------|-------------|-----------|
| `merge` | `git checkout main && git merge dev` (or `git merge origin/dev`) | Yes |
| `nuget` | Run `update-contensive-packages.ps1` for the repo (see Phase 3) | Yes |
| `build` | Run the repo's `scripts/build-deploy-local.cmd` or equivalent | Yes |
| `test` | Run `dotnet test` or `dotnet run` for the test project | Yes |
| `staging` | Run the repo's `scripts/build-deploy-remote.cmd` | Yes |
| `smoke` | Prompt user: "Smoke test [repo] on staging. Pass? (y/n)" | Manual |
| `publish` | Run the repo's `scripts/deploy-to-addon-library.cmd` | Yes |

### Dependency enforcement

Before executing a step, the runner checks:

1. **Step ordering** — `nuget` requires `merge:done`, `build` requires `nuget:done`, etc.
2. **Tier ordering** — Tier 1 repos cannot start `build` until all Tier 0 repos have
   `publish:done` (their NuGet packages are available)
3. **Specific dependencies** — If a repo's `depends_on` includes `DesignBlockBase`,
   `aoDesignBlocks` must have `publish:done` before this repo can `nuget`

If a dependency is not met, the runner prints a clear message:
```
BLOCKED: aoEcommerce cannot run 'nuget' — waiting on:
  - aoDesignBlocks (publish: pending)
```

### Implementation tasks

1. Write `scripts/release/release-runner.ps1` with:
   - `-Status` flag for dashboard display
   - `-Step` parameter for executing steps
   - `-Repo` parameter for targeting one repo
   - `-Tier` parameter for targeting one tier
   - `-All` flag for all ready repos
   - Manifest read/write (update status + timestamp after each step)
   - Dependency checking before execution
2. Implement each step handler as a function in the script
3. Add color coding to the dashboard (green=done, red=fail, yellow=in-progress)

---

## Phase 3: NuGet Update Script (Recommendation 3 + 4)

### Deliverable

A PowerShell script `scripts/release/update-contensive-packages.ps1` that
updates all `Contensive.*` package references in a repo to the versions
available in `C:\NuGetLocalPackages`.

### How it works

1. Find all `.csproj` files in the target repo
2. For each `PackageReference` where `Include` starts with `Contensive.`:
   - Look up the latest `.nupkg` file in `C:\NuGetLocalPackages` for that package ID
   - Extract the version from the filename
   - Update the `Version` attribute in the `.csproj`
3. Run `dotnet restore` to verify the update

### Usage

```powershell
# Update one repo
.\update-contensive-packages.ps1 -RepoPath C:\Git\aoDesignBlocks

# Dry run — show what would change without modifying files
.\update-contensive-packages.ps1 -RepoPath C:\Git\aoDesignBlocks -WhatIf
```

### Edge cases

- **Test projects**: Update test .csproj files too (they reference Contensive.Processor)
- **Pinned versions**: Some repos may intentionally pin to an older version. The script
  should print a warning if a .csproj has a comment like `<!-- pinned -->` near the
  package reference
- **Non-Contensive packages**: Leave all non-Contensive packages untouched
- **Pre-release versions**: When building from `main`, packages won't have a pre-release
  suffix. The script should prefer release versions over pre-release when both exist

### Implementation tasks

1. Write `scripts/release/update-contensive-packages.ps1`
2. Add a `-WhatIf` dry-run mode
3. Integrate as the `nuget` step handler in `release-runner.ps1`

---

## Implementation Order

Build these in dependency order — each phase depends on the previous:

### Sprint 1: Manifest + Dashboard (read-only tracking)
1. Create `scripts/release/` directory
2. Write `repo-catalog.yml` with the tier/exclusion data from Phase 0
3. Write `new-release.ps1` to generate `release-manifest.yml`
4. Write the `-Status` dashboard in `release-runner.ps1`
5. Add manual status update commands:
   `.\release-runner.ps1 -Mark done -Step merge -Repo Contensive5`
   This alone replaces the paper checklist.

### Sprint 2: NuGet Update Script
1. Write `update-contensive-packages.ps1`
2. Test on aoDesignBlocks and aoEcommerce (Tier 1 repos with known dependencies)
3. Verify it handles test projects and pre-release versions correctly

### Sprint 3: Automated Step Execution
1. Wire up each step handler in `release-runner.ps1`
2. Add dependency enforcement logic
3. Test the full flow: `merge` -> `nuget` -> `build` -> `test` -> `staging` for
   one Tier 0 + one Tier 1 + one Tier 2 repo
4. Add the `smoke` step as a manual prompt
5. Add the `publish` step calling `deploy-to-addon-library.ps1`

### Sprint 4: Polish
1. Add color coding to the dashboard
2. Add `-All` flag for batch operations
3. Add error recovery (retry a failed step, resume after a crash)
4. Add a `release-runner.ps1 -Report` that generates a summary of the release
   (how many repos, total time, any failures)

---

## File Layout When Complete

```
scripts/release/
  new-release.ps1                 # generates a fresh manifest for a new month
  release-runner.ps1              # main entry point: status, execute steps, mark
  update-contensive-packages.ps1  # updates Contensive.* NuGet refs in a repo
  repo-catalog.yml                # stable metadata: tiers, exclusions, overrides
  release-manifest.yml            # current release state (gitignored or committed per preference)
```

---

## Open Questions

1. **Should `release-manifest.yml` be committed to git?** Pro: audit trail, shareable.
   Con: noisy diffs every release. Recommendation: commit it — the audit trail is
   valuable and it's one file changed once a month.

2. **Should Tier 3 (data/UI-only) repos go through the full pipeline?** They have no
   C# build or tests. Recommendation: include them with `build:skipped` and
   `test:skipped` so they still get merged, deployed to staging, smoke-tested,
   and published.

3. **What about repos that don't have `scripts/build.ps1`?** Some Tier 3 repos may
   need a minimal build script added. The `new-release.ps1` scanner should flag
   these so they can be addressed before release day.
