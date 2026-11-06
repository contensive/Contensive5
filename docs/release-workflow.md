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
- For the e2e step (repos with an `e2e_path` in `repo-catalog.yml`, today aoEcommerce):
  - Node.js LTS (18 or later) with `npm` and `npx` on the PATH
  - A secrets file `%LOCALAPPDATA%\contensive\e2e.env` (never in a repo, see "Step 4b")
  - A dedicated e2e admin account on https://test.sitefpo.com

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
.\release-runner.ps1 -Step e2e     -Tier 0
```

The **staging** step auto-skips for Tier 0 (the local machine is already upgraded).
The **e2e** step auto-skips for Tier 0 (core e2e is a later phase; core cannot be
deployed with `/installCollection`).

Verify the local site, then mark smoke and publish:

```powershell
.\release-runner.ps1 -Mark done -Step smoke -Repo Contensive5
.\release-runner.ps1 -Step publish -Tier 0
```

---

## Step 4b: E2E on the test site

The `e2e` step runs after `staging` and before `smoke`. "Has a suite" is decided by `repo-catalog.yml` only:
a repo with an `e2e_path` there (today: aoEcommerce, `tests/e2e`) always runs the step. For every repo
without an `e2e_path` it records `skipped` ("no e2e suite"). Tier 0 also skips.

If the catalog declares an `e2e_path` but `playwright.config.ts` is not in that folder of the checked-out
branch, the step **fails** ("suite missing on branch X"). It is never skipped: check out the branch that has
the suite, or override with `-Mark done -Step e2e -Reason`. A manifest that is created or upgraded while the
suite is not checked out gets `e2e: pending`, not `skipped`.

```powershell
.\release-runner.ps1 -Step e2e -Repo aoEcommerce
```

**First time, or weeks later and you have forgotten how the secrets file works?** Two commands, no reading needed:

```powershell
.\release-runner.ps1 -SetupE2E   # creates %LOCALAPPDATA%\contensive\e2e.env from a template if missing, opens Notepad, prints where each value comes from
.\release-runner.ps1 -CheckE2E   # checks every key (OK / MISSING / PLACEHOLDER / INVALID) and node/npm/npx; prints no values; exit 0 or 1
```

If the file is missing, a key is missing, a value is still a `<placeholder>` or `changeme`, or `E2E_BASE_URL` is invalid, the step prints an
"E2E SETUP INCOMPLETE" block (what is wrong, the file path and whether it exists, the two commands above, the doc), marks the step `failed`
with the note "e2e setup incomplete: <names>; run -CheckE2E", and changes nothing on the site.
Full instructions: [docs/e2e-setup.md](https://github.com/contensive/Contensive5/blob/main/docs/e2e-setup.md) (local: `C:\Git\Contensive5\docs\e2e-setup.md`).

What it does, in order:
1. Loads `%LOCALAPPDATA%\contensive\e2e.env` (real environment variables win) and checks that
   `E2E_BASE_URL` is `https` on an allowed host (`test.sitefpo.com` unless `E2E_ALLOWED_HOSTS` says otherwise).
   A missing setting fails here, before anything is changed. Secret values are never printed or written to the manifest.
   `CONTENSIVE_TEST_TOKEN` is read into a variable and is **not** put in the process environment (an existing
   process variable of that name is hidden for the run and restored afterwards), so `npm ci`, Playwright and the
   test workers never inherit it. It is passed only to the deploy call.
2. Takes a machine lock (`%LOCALAPPDATA%\contensive\e2e-run.lock`) so two runs cannot overlap on this machine.
3. Runs `npm ci` and `npx playwright install chromium` in the suite folder. It calls `npm.cmd`, `npx.cmd` and
   `node.exe` explicitly, so Windows PowerShell 5.1 never picks the `.ps1` shims (blocked by a restricted execution policy).
4. Takes the test-site lock (site property `E2E Lock`). If a manual job's build is installed on the test site
   (`E2E Manual Job`), it prints a warning: the e2e deploy replaces that collection.
5. Deploys the **same zip that publish would upload** to the test site with `CONTENSIVE_TEST_TOKEN`
   (no prompts) and records its path and SHA-256 in the manifest. `/installCollection` installs synchronously and
   answers `{"success":true|false,"message":...}`: HTTP 200 with `success` not `true` fails the step.
6. Sets `E2E_EXPECTED_VERSION` for the suite, waits for the site to answer 200 twice in a row (up to
   `E2E_READY_TIMEOUT_SEC`, default 180 s), then runs `npx playwright test`.

   **Installed build = tested build.** The expected version is the name of the folder the build wrote the zip into
   (`<DeploymentRoot>\<version>\<Collection>.zip`, for example `26.11.6.30150` or `26.11.6.30150-branch-name`).
   The suite's first setup project must read the installed collection version from the site and fail if it differs
   (compare the 4-part number; the `-branch` suffix may or may not be part of the installed value). With several zips
   the variable holds the versions joined by `;`. If the version cannot be derived the step still runs, but its notes
   carry `WARNING: installed version not verified`. Until a suite asserts it, a green run proves only that the install
   reported success, not which build the site is serving.
7. Archives the HTML report and results under `C:\Deployments\e2e-reports\<repo>\<runId>\`
   (override the root with `E2E_REPORT_ROOT`), and records the passed/failed/flaky/skipped counts.
8. Always (even on failure, unless the run was killed; the next seed restores a leftover snapshot) restores any
   leftover site-property snapshot, then releases the test-site lock and the machine lock. The restore and lock
   release also run when `npm` fails after the lock was taken.

The step is `failed` when any test fails, when the suite produced no readable `results.json`, when **0 tests passed**,
when the `smoke`, `seed` or `regression` project is missing from the results, or when the suite reports that
site properties were **not** restored (restore them from the archived `run-info.json` before anyone tests).
Old result files are deleted before the main run; the lock phases (which write their own `results.json`) are never read as results.

The archive folder holds Playwright traces, screenshots and videos. Treat it as sensitive: a trace can record typed
values (logins, settings). Keep it on the release machine and do not share it outside the team.

Reading a failure: `-Status` prints `e2e report: <path>` under the repo. Open that `index.html`. The per-test
traces, screenshots and videos are inside the archived folder. `-Report` lists every e2e report under "E2E Reports".

Settings file, placeholders only (the maintained copy is `scripts/release/e2e.env.template`; `-SetupE2E` creates the file from it):

```ini
# Contensive e2e secrets - never commit. KEY=VALUE, one per line, # comments, no quotes needed.
E2E_BASE_URL=https://test.sitefpo.com
E2E_ADMIN_USER=<e2e admin user>
E2E_ADMIN_PASSWORD=<password>
E2E_CUSTOMER_PASSWORD=<password>
CONTENSIVE_TEST_TOKEN=<bearer token for test.sitefpo.com>
# optional
E2E_TEST_EMAIL_TO=<mailbox for redirected test email>
E2E_STRIPE_SECRET_KEY=<sk_test_... sandbox key>
E2E_STRIPE_WEBHOOK_SECRET=<whsec_...>
E2E_AUTHNET_LOGIN_ID=<sandbox login id>
E2E_AUTHNET_TRANSACTION_KEY=<sandbox transaction key>
```

Policy and background: [e2e-testing-strategy.md](e2e-testing-strategy.md).

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
.\release-runner.ps1 -Step e2e     -Repo aoMCP
.\release-runner.ps1 -Mark done -Step smoke -Repo aoMCP
.\release-runner.ps1 -Step publish -Repo aoMCP

# aoCrm (depends on aoMCP)
.\release-runner.ps1 -Step nuget   -Repo aoCrm
.\release-runner.ps1 -Step build   -Repo aoCrm
.\release-runner.ps1 -Step test    -Repo aoCrm
.\release-runner.ps1 -Step staging -Repo aoCrm
.\release-runner.ps1 -Step e2e     -Repo aoCrm
.\release-runner.ps1 -Mark done -Step smoke -Repo aoCrm
.\release-runner.ps1 -Step publish -Repo aoCrm

# aoDesignBlocks (depends on aoCrm, aoMCP)
.\release-runner.ps1 -Step nuget   -Repo aoDesignBlocks
.\release-runner.ps1 -Step build   -Repo aoDesignBlocks
.\release-runner.ps1 -Step test    -Repo aoDesignBlocks
.\release-runner.ps1 -Step staging -Repo aoDesignBlocks
.\release-runner.ps1 -Step e2e     -Repo aoDesignBlocks
.\release-runner.ps1 -Mark done -Step smoke -Repo aoDesignBlocks
.\release-runner.ps1 -Step publish -Repo aoDesignBlocks

# aoEcommerce (depends on aoDesignBlocks). Its e2e step deploys to test.sitefpo.com and runs Playwright.
# The other repos here have no e2e suite, so their e2e step auto-skips.
.\release-runner.ps1 -Step nuget   -Repo aoEcommerce
.\release-runner.ps1 -Step build   -Repo aoEcommerce
.\release-runner.ps1 -Step test    -Repo aoEcommerce
.\release-runner.ps1 -Step staging -Repo aoEcommerce
.\release-runner.ps1 -Step e2e     -Repo aoEcommerce
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
.\release-runner.ps1 -Step e2e     -Tier 2
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
.\release-runner.ps1 -Step e2e     -Tier 3
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

This resets the step from `failed` to `pending` and re-executes it. It works the same for `-Step e2e`.

### Publish is blocked by e2e

`publish` refuses until the repo's `e2e` step is `done`, or `skipped` because the repo has no e2e suite in the catalog.
A skip without an override for a repo the catalog gives an `e2e_path` is refused, and so is `done` with no recorded zip hash and no override.
It also re-computes the SHA-256 of the zip it is about to upload. If that differs from the zip e2e tested, it stops with
`Collection zip changed since e2e tested it`. Re-test:

```powershell
.\release-runner.ps1 -Mark pending -Step e2e -Repo aoEcommerce
.\release-runner.ps1 -Step e2e -Repo aoEcommerce
```

### Override the e2e gate (with a recorded reason)

When e2e cannot pass for a reason that is not a product defect (for example a payment sandbox outage), mark it by hand.
`-Reason` is required, and is stored in the manifest as `override_reason` with your user name and the time:

```powershell
.\release-runner.ps1 -Mark done -Step e2e -Repo aoEcommerce -Reason "Stripe sandbox outage"
```

The dashboard shows `OVRD` instead of `DONE`, `-Report` lists it under "E2E Overrides", and publish skips the zip check
and prints the reason. Marking e2e `pending` or `failed` clears the override and the recorded zip, so the next run re-tests.

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
.\release-runner.ps1 -Step e2e     -Repo aoDesignBlocks
.\release-runner.ps1 -Mark done    -Step smoke -Repo aoDesignBlocks
.\release-runner.ps1 -Step publish -Repo aoDesignBlocks
```

---

## Hotfix releases

A hotfix ships one repo from a branch, outside the monthly cycle, and still has to pass tests and e2e.

```powershell
.\release-runner.ps1 -Hotfix -Repo aoEcommerce -Branch hotfix/20261106-fix-refund
```

It creates `hotfix-manifest-<yyyyMMdd>-<repo>.yml` (kept as an audit trail, commit it like `release-manifest.yml`), requires a clean
working tree in the repo, checks out and fast-forwards the branch (warns if it does not contain `origin/main`), records the commit,
then runs `build`, `test`, `e2e` and `publish`, stopping at the first step that does not finish. `merge`, `nuget`, `staging` and
`smoke` are marked skipped. The build uses the package references already on the branch. Staging is skipped on purpose, so the zip
that the build step made is the newest zip, and it is the one e2e tests and publish uploads.

- Run one step: add `-Step build` (or `test`, `e2e`, `publish`). Re-run a failed step: add `-Retry`.
- Check or override that manifest: add `-ManifestPath <the hotfix file>` to `-Status`, `-Report` or `-Mark`.
- After `publish`, open PRs to merge the hotfix branch into `main` and `dev`.
- The e2e override with `-Reason` works the same as in a monthly release.
- The branch name must be a safe git name, must exist on `origin`, and cannot be `main`, `master` or `dev`.
- The repo is returned to the branch it was on when the hotfix run ends (success or failure).
- If the branch tip differs from the manifest's `commit` on a re-run, `build`, `test`, `e2e` and `publish` are reset to
  pending, so an old zip is never tested or published under a new commit.

---

## Step Pipeline Reference

| Step | What happens | Automated? |
|------|-------------|-----------|
| `merge` | `git checkout main && git merge origin/dev` | Yes |
| `nuget` | Updates `Contensive.*` PackageReference versions from `C:\NuGetLocalPackages` (skipped for repos with no dependencies) | Yes |
| `build` | Tier 0: runs `build-core.cmd` (NuGet packages + full platform build). Others: runs `scripts/build.ps1` (falls back to `dotnet build`) | Yes |
| `test` | Tier 0: extracts deployment zip, runs `upgrade-full.ps1` to upgrade local server/database, then runs tests (requires Admin). Others: runs `dotnet test` on `*Test*.csproj` projects | Yes |
| `staging` | Tier 0: auto-skipped (local machine already upgraded in test step). Others: runs `scripts/build.ps1 -Configuration Debug -DeployType local` to build and install the addon on the local Contensive site | Yes |
| `e2e` | Repos with `e2e_path` in `repo-catalog.yml`: deploys the newest collection zip to test.sitefpo.com and runs the Playwright suite (see Step 4b). Skipped for repos without a suite and for Tier 0 | Yes |
| `smoke` | You verify the addon on the local site, then mark done or failed | Manual |
| `publish` | Checks the e2e gate and the zip SHA-256, then runs the repo's `scripts/deploy-to-addon-library.cmd` | Yes |

Step order (one list, `$script:stepOrder` in `release-runner.ps1`): `merge, nuget, build, test, staging, e2e, smoke, publish`.
A manifest written before the `e2e` step existed is upgraded when it is read: `e2e` becomes `pending` for a repo whose catalog entry has an `e2e_path`, otherwise `skipped`.
A repo that already finished `staging` but not `test` (the old order) is blocked at `e2e` until `test` is done.
An `e2e: skipped` (no override) on a repo that the catalog now gives an `e2e_path` is reset to `pending` when the manifest is read.

## Step Statuses

| Status | Meaning |
|--------|---------|
| `pending` | Not started |
| `done` | Completed successfully (`e2e` done by hand shows as `OVRD` and carries an `override_reason`) |
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
  hotfix-manifest-*.yml           # one per hotfix run (audit trail, committed)
  generate-stubs.ps1              # one-time stub generator (already run)
```

See also: [e2e-testing-strategy.md](e2e-testing-strategy.md) for the e2e policy and the aoEcommerce suite.
