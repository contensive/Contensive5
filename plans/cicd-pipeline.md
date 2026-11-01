# CI/CD Pipeline Plan

Phase 3 of a three-phase plan to automate the monthly Contensive release process.
This phase moves repos from the local PowerShell release runner to GitHub Actions,
one repo at a time.

Related plans:
- [deployment-pipeline.md](deployment-pipeline.md) — Phase 1: manifest, runner, NuGet update
- [test-coverage-pipeline.md](test-coverage-pipeline.md) — Phase 2: test projects for all repos

---

## Problem Statement

The release pipeline from Phase 1 automates the mechanical steps but still requires
someone to sit at a Windows workstation and run `release-runner.ps1`. The build
depends on local tools (7-Zip, IIS, `cc` CLI, `C:\NuGetLocalPackages`,
`C:\Deployments`), and there's no way for agents or remote developers to trigger
builds.

Moving to CI/CD means:
- Builds trigger automatically on merge to main
- Tests run on every PR (catch failures before release day)
- Deployment to staging can be automatic
- Publishing to the addon library is a manual approval in the GitHub UI
- The release runner becomes a status dashboard only — execution moves to GitHub

---

## Prerequisites

Phase 3 requires:
1. **Phase 1 complete** — manifest and repo catalog exist, steps are well-defined
2. **Phase 2 in progress** — at minimum, Tier 0 and Tier 1 repos have tests
3. **A GitHub self-hosted runner** — the build requires Windows, .NET SDK, 7-Zip,
   IIS, and access to `C:\NuGetLocalPackages`. A cloud runner cannot satisfy this
   today. The self-hosted runner runs on the same workstation used for manual builds.
4. **NuGet package feed** — either keep using `C:\NuGetLocalPackages` (self-hosted)
   or migrate to a hosted feed (GitHub Packages, Azure Artifacts, or a private
   NuGet server). The hosted feed is required before cloud runners become viable.

---

## Architecture

### Two workflow types

Every repo gets one of two workflow templates:

**1. Addon Collection Workflow** (for Tier 1, 2, and 3 repos)
```
PR to main → build + test → status check
Merge to main → build + test + deploy to staging → wait for approval → publish
```

**2. Core Platform Workflow** (for Contensive5 only)
```
PR to main → build + test → status check
Merge to main → build + pack NuGet + test + deploy to staging
  → wait for approval → publish NuGet + publish core
```

### NuGet package flow

The dependency chain means packages must flow from upstream to downstream:

```
Contensive5 builds → NuGet packages published to feed
  ↓
aoMCP builds (consumes new packages) → Mcp.Shared published
  ↓
aoCrm builds → CRM published
  ↓
aoDesignBlocks builds → DesignBlockBase published
  ↓
aoEcommerce builds → EcommerceApi published
  ↓
All Tier 2 repos can build in parallel
```

In CI/CD, this chain is triggered by:
- **Option A: Repository dispatch** — Contensive5's workflow sends a
  `repository_dispatch` event to aoMCP when its packages are published. aoMCP
  does the same for aoCrm, etc. This creates an automatic cascade.
- **Option B: Manual trigger with the release runner** — the release runner calls
  `gh workflow run` for each repo in order. Simpler to implement, keeps the
  runner as the orchestrator.

**Recommendation: Start with Option B.** It's the smallest change from the current
process — the release runner just calls `gh workflow run` instead of running local
PowerShell. Move to Option A later when the process is stable.

---

## Workflow Templates

### Addon Collection Workflow

```yaml
# .github/workflows/release.yml
name: Build & Deploy

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]
  workflow_dispatch:
    inputs:
      deploy_staging:
        description: 'Deploy to staging after build'
        type: boolean
        default: true
      publish:
        description: 'Publish to addon library'
        type: boolean
        default: false

jobs:
  build:
    runs-on: self-hosted
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Build collection
        shell: pwsh
        run: |
          Import-Module "$env:CONTENSIVE_SCRIPTS\build-addon-collection.psm1" -Force
          # ... repo-specific build params

      - name: Run tests
        if: hashFiles('**/Tests.csproj', '**/*Tests.csproj') != ''
        shell: pwsh
        run: |
          # ... repo-specific test command

  deploy-staging:
    needs: build
    if: github.event_name == 'push' || (github.event_name == 'workflow_dispatch' && inputs.deploy_staging)
    runs-on: self-hosted
    steps:
      - name: Deploy to staging
        shell: pwsh
        run: |
          # ... deploy to addontest.sitefpo.com

  publish:
    needs: deploy-staging
    if: github.event_name == 'workflow_dispatch' && inputs.publish
    runs-on: self-hosted
    environment: production    # requires manual approval in GitHub
    steps:
      - name: Publish to addon library
        shell: pwsh
        run: |
          # ... deploy-to-addon-library.ps1
```

### Key design decisions

1. **`environment: production`** on the publish job creates a manual approval gate
   in GitHub. This replaces the smoke test step — you smoke test on staging, then
   approve the publish in the GitHub UI.

2. **`workflow_dispatch`** allows the release runner to trigger builds remotely via
   `gh workflow run <repo> -f publish=true`.

3. **`self-hosted` runner** is required initially. The runner label should be
   specific (e.g., `contensive-build`) so it doesn't conflict with other self-hosted
   runners.

4. **`CONTENSIVE_SCRIPTS`** environment variable points to the shared build module
   path. Set this on the runner machine.

---

## Migration Strategy: One Repo at a Time

### Step 1: Set up the self-hosted runner

1. Install the GitHub Actions runner on the build workstation
2. Configure it with the label `contensive-build`
3. Set environment variables: `CONTENSIVE_SCRIPTS=C:\Git\Contensive5\scripts`
4. Register it with the `contensive` GitHub organization (so all repos can use it)

### Step 2: Migrate a Tier 2 leaf addon first

Pick a simple Tier 2 repo that already has tests (e.g., aoAddonManager or
aoCalender2). This is low-risk because no other repos depend on it.

1. Add `.github/workflows/release.yml` using the addon template
2. Trigger a manual build via `gh workflow run`
3. Verify: build succeeds, tests pass, staging deploy works
4. Verify: manual approval gate works for publish
5. Update `repo-catalog.yml` to mark `cicd: true`

### Step 3: Migrate aoDesignBlocks (Tier 1 with tests)

This is the first repo that produces a NuGet package other repos consume. The
workflow must:
1. Build and test
2. Pack the NuGet package
3. Copy to `C:\NuGetLocalPackages` on the runner machine
4. Deploy to staging
5. Publish (with approval gate)

After this succeeds, downstream repos (aoBlog, aoEcommerce, etc.) can be updated
to reference the package version built by CI.

### Step 4: Migrate Contensive5 (Tier 0)

This is the most complex workflow:
1. Build CPBase, Models, Processor
2. Pack three NuGet packages
3. Copy to local feed
4. Build the legacy iisDefaultSite (requires MSBuild, not dotnet CLI)
5. Publish WebApi, Cli, TaskService
6. Run ProcessorTests and ModelsTests
7. Create the contensive.zip distribution package
8. Deploy to staging
9. Publish (with approval gate)

The existing `scripts/build-core.cmd` already does all of this. The GitHub Actions
workflow wraps it.

### Step 5: Migrate remaining Tier 1 repos

aoMCP, aoCrm, aoEcommerce — in dependency order.

### Step 6: Batch-migrate Tier 2 repos

Once the template is proven, add the workflow file to all remaining Tier 2 repos.
This can be done with a script that copies the template and commits it.

### Step 7: Migrate Tier 3 repos

Tier 3 repos have no C# build, so their workflow is simpler: just package the
collection zip and deploy. Some may not need CI/CD at all if they change rarely.

---

## Release Runner Integration

Once repos have CI/CD, the release runner changes behavior:

**Before CI/CD:**
```powershell
# release-runner.ps1 runs the build locally
.\release-runner.ps1 -Step build -Repo aoDesignBlocks
# → runs scripts/build.ps1 directly
```

**After CI/CD:**
```powershell
# release-runner.ps1 triggers the GitHub workflow
.\release-runner.ps1 -Step build -Repo aoDesignBlocks
# → runs: gh workflow run release.yml -R contensive/aoDesignBlocks
# → polls: gh run list -R contensive/aoDesignBlocks --limit 1
# → updates manifest when the run completes
```

Add a `cicd` flag to `repo-catalog.yml`:

```yaml
- name: aoDesignBlocks
  cicd: true          # use GitHub Actions
  workflow: release   # workflow filename (without .yml)
```

When `cicd: true`, the release runner uses `gh workflow run` instead of local
execution. When `cicd: false`, it falls back to local PowerShell. This allows
migrating one repo at a time without changing the release process.

---

## Future: Cloud Runners

Moving to cloud runners (GitHub-hosted or a dedicated build VM) requires:

1. **NuGet package feed** — replace `C:\NuGetLocalPackages` with a hosted feed
   (GitHub Packages is free for public repos, Azure Artifacts for private)
2. **Collection deploy via API** — the `installCollection` endpoint already exists
   and uses bearer token auth, so remote deploy works from anywhere
3. **Remove IIS dependency** — the legacy `iisDefaultSite` build requires MSBuild
   and IIS. Once the net48 ASPX site is retired, this dependency goes away
4. **7-Zip** — replace with `Compress-Archive` or a NuGet tool package

This is not urgent. The self-hosted runner works today and the migration to cloud
runners can happen gradually as dependencies are removed.

---

## Success Criteria

- Every Tier 0 and Tier 1 repo has a GitHub Actions workflow
- PRs to main trigger build + test (status checks required for merge)
- Merge to main triggers automatic staging deploy
- Publish requires manual approval in the GitHub UI
- The release runner can orchestrate the full release by calling `gh workflow run`
  for each repo in dependency order
- A repo can be migrated from local-only to CI/CD by adding one workflow file and
  setting `cicd: true` in the catalog — no other changes needed

---

## Risk Mitigation

| Risk | Mitigation |
|------|-----------|
| Self-hosted runner goes offline | Runner is on the same machine used today; same availability. Add monitoring (runner status in GitHub UI). |
| NuGet version conflicts during cascade | The release runner enforces build order — same as today. CI doesn't auto-cascade until Option A is implemented. |
| Workflow template drift across repos | Keep templates in Contensive5 `scripts/release/templates/`. Use a script to sync them across repos. |
| Bearer token expiry for staging deploy | Token caching already exists in `Invoke-ContensiveDeploy`. Store as a GitHub Actions secret per repo. |
| Legacy MSBuild for iisDefaultSite | Keep this step in Contensive5's workflow only. It doesn't affect addon repos. |
