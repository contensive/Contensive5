#Requires -Version 5.1
<#
.SYNOPSIS
    Contensive release runner -status dashboard, step execution, and release management.

.DESCRIPTION
    Reads release-manifest.yml and either displays the release dashboard or executes
    a release step for one or more repos. Enforces dependency ordering so repos cannot
    build before their dependencies are published.

.PARAMETER Status
    Display the release status dashboard.

.PARAMETER Step
    The release step to execute: merge, nuget, build, test, staging, smoke, publish.

.PARAMETER Mark
    Manually mark a step with a status (done, failed, skipped, pending, blocked).

.PARAMETER Repo
    Target a specific repo by name.

.PARAMETER Tier
    Target all repos in a specific tier (0, 1, 2, 3).

.PARAMETER All
    Target all repos that are ready (dependencies met).

.PARAMETER Report
    Generate a summary report of the current release.

.PARAMETER Retry
    Retry a previously failed step.

.PARAMETER ManifestPath
    Path to release-manifest.yml. Defaults to the file alongside this script.

.EXAMPLE
    .\release-runner.ps1 -Status
    .\release-runner.ps1 -Step merge -Tier 0
    .\release-runner.ps1 -Step merge -Repo aoDesignBlocks
    .\release-runner.ps1 -Step nuget -All
    .\release-runner.ps1 -Mark done -Step merge -Repo Contensive5
    .\release-runner.ps1 -Report
    .\release-runner.ps1 -Step build -Repo aoBlog -Retry
#>
[CmdletBinding(DefaultParameterSetName = 'Status')]
param(
    [Parameter(ParameterSetName = 'Status')]
    [switch]$Status,

    [Parameter(ParameterSetName = 'Execute', Mandatory)]
    [Parameter(ParameterSetName = 'Mark', Mandatory)]
    [ValidateSet('merge', 'nuget', 'build', 'test', 'staging', 'smoke', 'publish')]
    [string]$Step,

    [Parameter(ParameterSetName = 'Mark', Mandatory)]
    [ValidateSet('done', 'failed', 'skipped', 'pending', 'blocked')]
    [string]$Mark,

    [Parameter(ParameterSetName = 'Execute')]
    [Parameter(ParameterSetName = 'Mark')]
    [string]$Repo,

    [Parameter(ParameterSetName = 'Execute')]
    [Parameter(ParameterSetName = 'Mark')]
    [ValidateSet(0, 1, 2, 3)]
    [int]$Tier = -1,

    [Parameter(ParameterSetName = 'Execute')]
    [Parameter(ParameterSetName = 'Mark')]
    [switch]$All,

    [Parameter(ParameterSetName = 'Report')]
    [switch]$Report,

    [Parameter(ParameterSetName = 'Execute')]
    [switch]$Retry,

    [string]$ManifestPath = ''
)

$ErrorActionPreference = 'Stop'

$script:scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
if (-not $ManifestPath) { $ManifestPath = Join-Path $script:scriptDir 'release-manifest.yml' }

# ===========================================================================
# YAML parser for release-manifest.yml
# ===========================================================================
function Read-ReleaseManifest {
    param([string]$Path)

    if (-not (Test-Path $Path)) {
        throw "Manifest not found: $Path`nRun new-release.ps1 first to generate one."
    }

    $lines = Get-Content $Path
    $manifest = @{
        release = ''
        created = ''
        status  = ''
        repos   = @()
    }
    $current = $null
    $inSteps = $false
    $lastArray = ''

    foreach ($line in $lines) {
        if ($line -match '^\s*#' -or $line -match '^\s*$') { continue }

        # Top-level scalars
        if ($line -match '^release:\s*"?([^"]+)"?\s*$') { $manifest.release = $Matches[1].Trim(); continue }
        if ($line -match '^created:\s*"?([^"]+)"?\s*$') { $manifest.created = $Matches[1].Trim(); continue }
        if ($line -match '^status:\s*(.+)$') { $manifest.status = $Matches[1].Trim(); continue }
        if ($line -match '^repos:\s*$') { continue }

        # New repo
        if ($line -match '^\s{2}- name:\s*(.+)$') {
            if ($current) { $manifest.repos += $current }
            $current = @{
                name             = $Matches[1].Trim()
                path             = ''
                tier             = 0
                produces         = @()
                depends_on       = @()
                has_tests        = $false
                has_build_script = $false
                steps            = @{}
            }
            $inSteps = $false
            $lastArray = ''
            continue
        }

        if (-not $current) { continue }

        # Repo scalars
        if ($line -match '^\s{4}path:\s*(.+)$') { $current.path = $Matches[1].Trim(); continue }
        if ($line -match '^\s{4}tier:\s*(\d+)') { $current.tier = [int]$Matches[1]; continue }
        if ($line -match '^\s{4}has_tests:\s*(true|false)') { $current.has_tests = $Matches[1] -eq 'true'; continue }
        if ($line -match '^\s{4}has_build_script:\s*(true|false)') { $current.has_build_script = $Matches[1] -eq 'true'; continue }

        # Inline empty arrays
        if ($line -match '^\s{4}(produces|depends_on):\s*\[\]\s*$') {
            $current[$Matches[1]] = @()
            $lastArray = ''
            continue
        }

        # Array headers
        if ($line -match '^\s{4}(produces|depends_on):\s*$') {
            $lastArray = $Matches[1]
            $inSteps = $false
            continue
        }

        # Array items
        if ($line -match '^\s{6}-\s+(.+)$' -and $lastArray -and -not $inSteps) {
            $current[$lastArray] += $Matches[1].Trim()
            continue
        }

        # Steps header
        if ($line -match '^\s{4}steps:\s*$') {
            $inSteps = $true
            $lastArray = ''
            continue
        }

        # Step line: merge: { status: pending, timestamp: null, notes: "" }
        if ($inSteps -and $line -match '^\s{6}(\w+):\s*\{\s*status:\s*(\w+),\s*timestamp:\s*(null|"[^"]*"|[\d\-T:]+),\s*notes:\s*"([^"]*)"\s*\}') {
            $stepName = $Matches[1]
            $stepStatus = $Matches[2]
            $stepTimestamp = $Matches[3]
            $stepNotes = $Matches[4]
            if ($stepTimestamp -eq 'null') { $stepTimestamp = $null }
            else { $stepTimestamp = $stepTimestamp.Trim('"') }

            $current.steps[$stepName] = @{
                status    = $stepStatus
                timestamp = $stepTimestamp
                notes     = $stepNotes
            }
        }
    }
    if ($current) { $manifest.repos += $current }

    return $manifest
}

# ===========================================================================
# Write manifest back to YAML
# ===========================================================================
function Write-ReleaseManifest {
    param(
        [hashtable]$Manifest,
        [string]$Path
    )

    $sb = [System.Text.StringBuilder]::new()
    [void]$sb.AppendLine("release: `"$($Manifest.release)`"")
    [void]$sb.AppendLine("created: `"$($Manifest.created)`"")
    [void]$sb.AppendLine("status: $($Manifest.status)")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("repos:")

    $sorted = $Manifest.repos | Sort-Object { $_.tier }, { $_.name }
    $stepOrder = @('merge', 'nuget', 'build', 'staging', 'test', 'smoke', 'publish')

    foreach ($repo in $sorted) {
        [void]$sb.AppendLine("  - name: $($repo.name)")
        [void]$sb.AppendLine("    path: $($repo.path)")
        [void]$sb.AppendLine("    tier: $($repo.tier)")

        if ($repo.produces.Count -eq 0) {
            [void]$sb.AppendLine("    produces: []")
        } else {
            [void]$sb.AppendLine("    produces:")
            foreach ($p in $repo.produces) {
                [void]$sb.AppendLine("      - $p")
            }
        }

        if ($repo.depends_on.Count -eq 0) {
            [void]$sb.AppendLine("    depends_on: []")
        } else {
            [void]$sb.AppendLine("    depends_on:")
            foreach ($d in $repo.depends_on) {
                [void]$sb.AppendLine("      - $d")
            }
        }

        [void]$sb.AppendLine("    has_tests: $($repo.has_tests.ToString().ToLower())")
        [void]$sb.AppendLine("    has_build_script: $($repo.has_build_script.ToString().ToLower())")
        [void]$sb.AppendLine("    steps:")

        foreach ($s in $stepOrder) {
            $stepData = $repo.steps[$s]
            if (-not $stepData) {
                $stepData = @{ status = 'pending'; timestamp = $null; notes = '' }
            }
            $ts = if ($stepData.timestamp) { "`"$($stepData.timestamp)`"" } else { 'null' }
            [void]$sb.AppendLine("      $s`: { status: $($stepData.status), timestamp: $ts, notes: `"$($stepData.notes)`" }")
        }

        [void]$sb.AppendLine("")
    }

    $content = $sb.ToString().TrimEnd() + "`n"
    Set-Content -Path $Path -Value $content -NoNewline -Encoding UTF8
}

# ===========================================================================
# Dashboard display
# ===========================================================================
function Show-Dashboard {
    param([hashtable]$Manifest)

    $stepOrder = @('merge', 'nuget', 'build', 'staging', 'test', 'smoke', 'publish')
    $sorted = $Manifest.repos | Sort-Object { $_.tier }, { $_.name }

    Write-Host ""
    Write-Host "Release $($Manifest.release) - Status Dashboard" -ForegroundColor Cyan
    Write-Host ("=" * 100) -ForegroundColor Cyan
    Write-Host ""

    $tierNames = @{
        0 = 'Foundation'
        1 = 'Shared Libraries'
        2 = 'Leaf Addons'
        3 = 'Data/UI Only'
    }

    $totalPublished = 0
    $totalRepos = $sorted.Count

    foreach ($tierNum in 0..3) {
        $tierRepos = @($sorted | Where-Object { $_.tier -eq $tierNum })
        if ($tierRepos.Count -eq 0) { continue }

        $tierPublished = @($tierRepos | Where-Object { $_.steps['publish'].status -eq 'done' }).Count
        $totalPublished += $tierPublished

        $tierLabel = "TIER $tierNum - $($tierNames[$tierNum])"
        if ($tierNum -ge 2) {
            $tierLabel += " ($tierPublished/$($tierRepos.Count) published)"
        }
        Write-Host $tierLabel -ForegroundColor Yellow
        Write-Host ""

        foreach ($repo in $tierRepos) {
            $nameCol = "  $($repo.name)".PadRight(28)
            Write-Host $nameCol -NoNewline

            foreach ($s in $stepOrder) {
                $stepData = $repo.steps[$s]
                $stepStatus = if ($stepData) { $stepData.status } else { 'pending' }
                $label = "$s`:"

                switch ($stepStatus) {
                    'done'    { $display = 'DONE'; $color = 'Green' }
                    'failed'  { $display = 'FAIL'; $color = 'Red' }
                    'skipped' { $display = 'SKIP'; $color = 'DarkGray' }
                    'blocked' { $display = 'BLKD'; $color = 'Magenta' }
                    default   { $display = '----'; $color = 'DarkGray' }
                }

                Write-Host "$label" -NoNewline
                Write-Host "$display  " -NoNewline -ForegroundColor $color
            }
            Write-Host ""
        }
        Write-Host ""
    }

    # Summary line
    $tier0Done = (@($sorted | Where-Object { $_.tier -eq 0 -and $_.steps['publish'].status -eq 'done' }).Count -eq @($sorted | Where-Object { $_.tier -eq 0 }).Count)
    $tier1Count = @($sorted | Where-Object { $_.tier -eq 1 }).Count
    $tier1Published = @($sorted | Where-Object { $_.tier -eq 1 -and $_.steps['publish'].status -eq 'done' }).Count
    $tier2Count = @($sorted | Where-Object { $_.tier -eq 2 }).Count
    $tier2Published = @($sorted | Where-Object { $_.tier -eq 2 -and $_.steps['publish'].status -eq 'done' }).Count
    $tier3Count = @($sorted | Where-Object { $_.tier -eq 3 }).Count
    $tier3Published = @($sorted | Where-Object { $_.tier -eq 3 -and $_.steps['publish'].status -eq 'done' }).Count

    $tier0Label = if ($tier0Done) { "DONE" } else { "in progress" }

    Write-Host ("=" * 100) -ForegroundColor Cyan
    Write-Host "Summary: $totalPublished/$totalRepos repos published | Tier 0: $tier0Label | Tier 1: $tier1Published/$tier1Count | Tier 2: $tier2Published/$tier2Count | Tier 3: $tier3Published/$tier3Count" -ForegroundColor Cyan
    Write-Host ""
}

# ===========================================================================
# Dependency checking
# ===========================================================================
function Test-StepReady {
    <#
    .SYNOPSIS
        Returns $true if a repo is ready to execute the given step, or a string
        describing what's blocking it.
    #>
    param(
        [hashtable]$Repo,
        [string]$StepName,
        [array]$AllRepos
    )

    $stepOrder = @('merge', 'nuget', 'build', 'staging', 'test', 'smoke', 'publish')
    $stepIndex = [Array]::IndexOf($stepOrder, $StepName)

    # 1. Check step ordering -prior steps must be done or skipped
    for ($i = 0; $i -lt $stepIndex; $i++) {
        $priorStep = $stepOrder[$i]
        $priorStatus = $Repo.steps[$priorStep].status
        if ($priorStatus -ne 'done' -and $priorStatus -ne 'skipped') {
            return "step '$priorStep' is $priorStatus"
        }
    }

    # 2. Check current step isn't already done/skipped
    $currentStatus = $Repo.steps[$StepName].status
    if ($currentStatus -eq 'done') { return "already done" }
    if ($currentStatus -eq 'skipped') { return "already skipped" }

    # 3. Check dependency ordering -repos that produce packages this repo depends on
    #    must have publish:done before this repo can start 'nuget' or later
    if ($stepIndex -ge 1 -and $Repo.depends_on.Count -gt 0) {
        $blockers = @()
        foreach ($dep in $Repo.depends_on) {
            # Find the repo that produces this package
            $producer = $AllRepos | Where-Object { $_.produces -contains $dep } | Select-Object -First 1
            if ($producer) {
                $pubStatus = $producer.steps['publish'].status
                if ($pubStatus -ne 'done') {
                    $blockers += "$($producer.name) (publish: $pubStatus)"
                }
            }
        }
        if ($blockers.Count -gt 0) {
            return "waiting on: $($blockers -join ', ')"
        }
    }

    return $true
}

# ===========================================================================
# Step handlers
# ===========================================================================
function Invoke-GitCommand {
    <#
    .SYNOPSIS
        Runs a git command with stderr suppressed so PowerShell 5.1 doesn't
        treat informational messages (e.g., "Already on 'main'") as errors.
        Returns stdout. Sets $LASTEXITCODE.
    #>
    param([string[]]$Arguments)
    $prevPref = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = & git @Arguments 2>$null
        return $output
    } finally {
        $ErrorActionPreference = $prevPref
    }
}

function Invoke-StepMerge {
    param([hashtable]$Repo)
    $repoPath = $Repo.path
    if (-not (Test-Path $repoPath)) {
        throw "Repo path not found: $repoPath"
    }
    Write-Host "  Merging dev into main for $($Repo.name)..."

    # Check if dev branch exists (local or remote)
    Invoke-GitCommand -Arguments @('-C', $repoPath, 'rev-parse', '--verify', 'dev') | Out-Null
    $hasLocalDev = ($LASTEXITCODE -eq 0)
    Invoke-GitCommand -Arguments @('-C', $repoPath, 'rev-parse', '--verify', 'origin/dev') | Out-Null
    $hasRemoteDev = ($LASTEXITCODE -eq 0)
    if (-not $hasLocalDev -and -not $hasRemoteDev) {
        Write-Host "  No dev branch found for $($Repo.name) - marking done" -ForegroundColor Yellow
        return
    }

    # Checkout main
    Invoke-GitCommand -Arguments @('-C', $repoPath, 'checkout', 'main') | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "git checkout main failed in $repoPath" }

    Invoke-GitCommand -Arguments @('-C', $repoPath, 'pull', 'origin', 'main') | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "git pull main failed in $repoPath" }

    # Fetch latest so origin/dev is current
    Invoke-GitCommand -Arguments @('-C', $repoPath, 'fetch', 'origin') | Out-Null

    # Check if there are any differences between main and origin/dev
    $devRef = if ($hasRemoteDev) { 'origin/dev' } else { 'dev' }
    $diffOutput = Invoke-GitCommand -Arguments @('-C', $repoPath, 'diff', "main..$devRef", '--stat')
    if (-not $diffOutput) {
        Write-Host "  No changes between main and dev for $($Repo.name) - marking done" -ForegroundColor Yellow
        return
    }

    Invoke-GitCommand -Arguments @('-C', $repoPath, 'merge', $devRef) | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "git merge dev failed in $repoPath - resolve conflicts manually" }
    Write-Host "  Merge complete for $($Repo.name)" -ForegroundColor Green
}

function Invoke-StepNuget {
    param([hashtable]$Repo)
    # Skip repos with no Contensive package dependencies
    if ($Repo.depends_on.Count -eq 0) {
        Write-Host "  No Contensive package dependencies for $($Repo.name) - skipping" -ForegroundColor Yellow
        return 'skipped'
    }
    $updateScript = Join-Path $script:scriptDir 'update-contensive-packages.ps1'
    if (-not (Test-Path $updateScript)) {
        throw "update-contensive-packages.ps1 not found at: $updateScript"
    }
    Write-Host "  Updating Contensive NuGet packages for $($Repo.name)..."
    & $updateScript -RepoPath $Repo.path
    Write-Host "  NuGet update complete for $($Repo.name)" -ForegroundColor Green
}

function Invoke-StepBuild {
    param([hashtable]$Repo)
    $repoPath = $Repo.path

    # --- Tier 0 (Contensive5) uses build-core.cmd ---
    if ($Repo.tier -eq 0) {
        $buildCoreCmd = Join-Path $repoPath 'scripts\build-core.cmd'
        if (-not (Test-Path $buildCoreCmd)) {
            throw "build-core.cmd not found at $buildCoreCmd"
        }
        Write-Host "  Building $($Repo.name) via scripts\build-core.cmd (Tier 0 core build)..."
        Write-Host "  This builds NuGet packages, WebApi, CLI, TaskService, and creates deployment package." -ForegroundColor DarkGray
        & cmd /c "$buildCoreCmd /nopause"
        if ($LASTEXITCODE -ne 0) { throw "build-core.cmd failed for $($Repo.name)" }
        Write-Host "  Core build complete for $($Repo.name)" -ForegroundColor Green
        return
    }

    # --- All other tiers use the standard build path ---
    $buildPs1 = Join-Path $repoPath 'scripts\build.ps1'
    $buildCmd = Join-Path $repoPath 'scripts\build.cmd'

    if (Test-Path $buildPs1) {
        Write-Host "  Building $($Repo.name) via scripts\build.ps1..."
        # Check if the build script accepts -NoPause; if not, pass it via env var
        $env:CONTENSIVE_NOPAUSE = '1'
        & powershell -NoProfile -ExecutionPolicy Bypass -File $buildPs1
        Remove-Item Env:\CONTENSIVE_NOPAUSE -ErrorAction SilentlyContinue
        if ($LASTEXITCODE -ne 0) { throw "Build failed for $($Repo.name)" }
    } elseif (Test-Path $buildCmd) {
        Write-Host "  Building $($Repo.name) via scripts\build.cmd..."
        & cmd /c "$buildCmd /nopause"
        if ($LASTEXITCODE -ne 0) { throw "Build failed for $($Repo.name)" }
    } else {
        # Fallback: try dotnet build if a .sln exists
        $slnFiles = Get-ChildItem -Path $repoPath -Filter '*.sln' -Recurse -Depth 2 -ErrorAction SilentlyContinue
        if ($slnFiles.Count -gt 0) {
            $sln = $slnFiles[0].FullName
            Write-Host "  Building $($Repo.name) via dotnet build ($sln)..."
            & dotnet build $sln --configuration Release
            if ($LASTEXITCODE -ne 0) { throw "dotnet build failed for $($Repo.name)" }
        } else {
            Write-Host "  No build script or solution found for $($Repo.name) - skipping" -ForegroundColor Yellow
            return 'skipped'
        }
    }
    Write-Host "  Build complete for $($Repo.name)" -ForegroundColor Green
}

function Invoke-StepTest {
    param([hashtable]$Repo)
    if (-not $Repo.has_tests) {
        Write-Host "  No test project for $($Repo.name) - skipping" -ForegroundColor Yellow
        return 'skipped'
    }

    $repoPath = $Repo.path

    # --- Tier 0: upgrade local database before running tests ---
    if ($Repo.tier -eq 0) {
        Write-Host "  Tier 0 pre-test: upgrading local server from latest core build..." -ForegroundColor Cyan

        # Find the latest deployment folder (build-core.cmd creates versioned folders)
        $deployRoot = 'C:\Deployments\Contensive5-Core'
        if (-not (Test-Path $deployRoot)) {
            throw "Deployment root not found: $deployRoot - run the build step first"
        }

        # Find the most recently created version folder containing contensive.zip
        $versionFolders = Get-ChildItem -Path $deployRoot -Directory -ErrorAction SilentlyContinue |
            Where-Object { Test-Path (Join-Path $_.FullName 'contensive.zip') } |
            Sort-Object -Property LastWriteTime -Descending
        if ($versionFolders.Count -eq 0) {
            throw "No deployment folders with contensive.zip found in $deployRoot - run the build step first"
        }
        $latestFolder = $versionFolders[0].FullName
        $zipPath = Join-Path $latestFolder 'contensive.zip'
        Write-Host "  Using deployment: $latestFolder" -ForegroundColor DarkGray

        # Extract the zip to a temp subfolder for upgrade
        $extractPath = Join-Path $latestFolder '_extracted'
        if (Test-Path $extractPath) {
            Remove-Item -Path $extractPath -Recurse -Force
        }
        Write-Host "  Extracting contensive.zip..."
        Expand-Archive -Path $zipPath -DestinationPath $extractPath -Force

        # Run upgrade-full.ps1 (requires Administrator)
        $upgradePsPath = Join-Path $extractPath 'upgrade-full.ps1'
        if (-not (Test-Path $upgradePsPath)) {
            throw "upgrade-full.ps1 not found in extracted deployment at $extractPath"
        }

        # Check if running as admin
        $isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
        if (-not $isAdmin) {
            Write-Host ""
            Write-Host "  WARNING: upgrade-full.ps1 requires Administrator privileges." -ForegroundColor Yellow
            Write-Host "  The release runner is not elevated. Run upgrade manually:" -ForegroundColor Yellow
            Write-Host "    cd $extractPath" -ForegroundColor DarkGray
            Write-Host "    .\upgrade-full.cmd" -ForegroundColor DarkGray
            Write-Host "  Then re-run: .\release-runner.ps1 -Step test -Repo $($Repo.name)" -ForegroundColor DarkGray
            Write-Host ""
            return 'pending'
        }

        Write-Host "  Running upgrade-full.ps1 (server upgrade + database upgrade)..."
        & powershell -NoProfile -ExecutionPolicy Bypass -File $upgradePsPath -SourcePath $extractPath
        if ($LASTEXITCODE -ne 0) { throw "upgrade-full.ps1 failed for $($Repo.name)" }
        Write-Host "  Local server upgrade complete" -ForegroundColor Green

        # Clean up extracted folder
        Remove-Item -Path $extractPath -Recurse -Force -ErrorAction SilentlyContinue
    }

    # --- Run tests ---
    $testProjects = Get-ChildItem -Path $repoPath -Filter '*Test*.csproj' -Recurse -ErrorAction SilentlyContinue
    if ($testProjects.Count -eq 0) {
        Write-Host "  No test projects found for $($Repo.name) - skipping" -ForegroundColor Yellow
        return 'skipped'
    }
    foreach ($tp in $testProjects) {
        Write-Host "  Running tests: $($tp.Name)..."
        & dotnet test $tp.FullName --configuration Release
        if ($LASTEXITCODE -ne 0) { throw "Tests failed for $($tp.Name) in $($Repo.name)" }
    }
    Write-Host "  Tests passed for $($Repo.name)" -ForegroundColor Green
}

function Invoke-StepStaging {
    param([hashtable]$Repo)

    # Tier 0: local dev machine is the staging environment (upgrade-full runs in test step)
    if ($Repo.tier -eq 0) {
        Write-Host "  Tier 0 staging is the local dev machine (already upgraded in test step) - skipping" -ForegroundColor Yellow
        return 'skipped'
    }

    # Deploy to local Contensive site for verification before publishing.
    # Call build.ps1 directly with -DeployType 'local' to avoid the unconditional
    # pause in build-deploy-local.cmd (designed for interactive double-click use).
    $repoPath = $Repo.path
    $buildPs1 = Join-Path $repoPath 'scripts\build.ps1'

    if (Test-Path $buildPs1) {
        # Read saved local site name so we can pass it non-interactively
        $defaultsFile = Join-Path $env:LOCALAPPDATA 'contensive\local-deploy-appname.txt'
        $localSiteName = ''
        if (Test-Path $defaultsFile) {
            $localSiteName = (Get-Content $defaultsFile -First 1).Trim()
        }
        if (-not $localSiteName) {
            throw "No saved local site name found at $defaultsFile. Run a manual local deploy first to set it."
        }
        Write-Host "  Deploying $($Repo.name) to local site '$localSiteName'..."
        # Override Read-Host to return empty string (accepts saved default) and
        # set CONTENSIVE_NOPAUSE to bypass ReadKey pauses in sub-collection scripts.
        $env:CONTENSIVE_NOPAUSE = '1'
        $cmd = "function Read-Host { param([string]`$Prompt) return '' }; " +
               "`$ErrorActionPreference = 'Continue'; " +
               "& '$buildPs1' -Configuration Debug -DeployType local; " +
               "exit `$LASTEXITCODE"
        & powershell -NoProfile -ExecutionPolicy Bypass -Command $cmd
        Remove-Item Env:\CONTENSIVE_NOPAUSE -ErrorAction SilentlyContinue
        if ($LASTEXITCODE -ne 0) { throw "Local staging deploy failed for $($Repo.name)" }
    } else {
        Write-Host "  No build.ps1 found for $($Repo.name) - marking pending" -ForegroundColor Yellow
        return 'pending'
    }
    Write-Host "  Local staging deploy complete for $($Repo.name)" -ForegroundColor Green
}

function Invoke-StepSmoke {
    param([hashtable]$Repo)
    Write-Host ""
    Write-Host "  SMOKE TEST: $($Repo.name)" -ForegroundColor Yellow
    Write-Host "  Verify $($Repo.name) on the local site, then mark manually:" -ForegroundColor Yellow
    Write-Host "    .\release-runner.ps1 -Mark done -Step smoke -Repo $($Repo.name)" -ForegroundColor DarkGray
    Write-Host "    .\release-runner.ps1 -Mark failed -Step smoke -Repo $($Repo.name)" -ForegroundColor DarkGray
    Write-Host ""
    return 'pending'
}

function Invoke-StepPublish {
    param([hashtable]$Repo)
    $repoPath = $Repo.path
    $scriptsDir = Join-Path $repoPath 'scripts'

    # Find all deploy-to-addon-library.cmd stubs (root and sub-collection directories)
    $deployCmds = @(Get-ChildItem -Path $scriptsDir -Filter 'deploy-to-addon-library.cmd' -Recurse -ErrorAction SilentlyContinue)
    if ($deployCmds.Count -eq 0) {
        Write-Host "  No deploy-to-addon-library.cmd found for $($Repo.name) -- marking pending" -ForegroundColor Yellow
        return 'pending'
    }

    # Import the shared deploy module once
    $modulePath = Join-Path $script:scriptDir '..\deploy-to-addon-library.psm1'
    Import-Module $modulePath -Force

    $publishedCount = 0
    $skippedCount = 0

    foreach ($cmdFile in $deployCmds) {
        $cmdParams = Read-DeployCmdParameters -CmdPath $cmdFile.FullName
        if (-not $cmdParams) {
            Write-Host "  SKIP: Could not parse deploy parameters from $($cmdFile.FullName)" -ForegroundColor Yellow
            $skippedCount++
            continue
        }

        # Validate the collection path exists (skip stubs that reference nonexistent collections)
        if (-not (Test-Path $cmdParams.CollectionPath)) {
            Write-Host "  SKIP: Collection path not found for '$($cmdParams.CollectionName)' -- $($cmdParams.CollectionPath)" -ForegroundColor Yellow
            $skippedCount++
            continue
        }

        Write-Host "  Publishing '$($cmdParams.CollectionName)' via shared deploy module..."
        $deployArgs = @{
            CollectionName = $cmdParams.CollectionName
            CollectionPath = $cmdParams.CollectionPath
            DeploymentPath = $cmdParams.DeploymentPath
        }
        if ($cmdParams.UiPath) { $deployArgs['UiPath'] = $cmdParams.UiPath }

        Invoke-AddonLibraryDeploy @deployArgs
        $publishedCount++
    }

    if ($publishedCount -eq 0) {
        Write-Host "  No valid collections to publish for $($Repo.name) -- marking pending" -ForegroundColor Yellow
        return 'pending'
    }

    Write-Host "  Publish complete for $($Repo.name) ($publishedCount collection(s), $skippedCount skipped)" -ForegroundColor Green
}

function Read-DeployCmdParameters {
    <#
    .SYNOPSIS
        Parses a deploy-to-addon-library.cmd stub and returns the parameters.
    .DESCRIPTION
        Extracts CollectionName, CollectionPath, DeploymentPath, and UiPath from
        the .cmd file. Resolves %~dp0-relative paths against the .cmd file's directory.
    #>
    param([string]$CmdPath)

    $content = Get-Content $CmdPath -Raw
    $cmdDir = Split-Path $CmdPath -Parent

    $params = @{
        CollectionName = ''
        CollectionPath = ''
        DeploymentPath = ''
        UiPath         = ''
    }

    # Match -CollectionName "value"
    if ($content -match '-CollectionName\s+"([^"]+)"') {
        $params.CollectionName = $Matches[1]
    }

    # Match -CollectionPath "value" -- resolve %~dp0 to the .cmd file's directory
    if ($content -match '-CollectionPath\s+"([^"]+)"') {
        $raw = $Matches[1] -replace '%~dp0', "$cmdDir\"
        $params.CollectionPath = [IO.Path]::GetFullPath($raw)
    }

    # Match -DeploymentPath "value"
    if ($content -match '-DeploymentPath\s+"([^"]+)"') {
        $raw = $Matches[1] -replace '%~dp0', "$cmdDir\"
        $params.DeploymentPath = [IO.Path]::GetFullPath($raw)
    }

    # Match -UiPath "value"
    if ($content -match '-UiPath\s+"([^"]+)"') {
        $raw = $Matches[1] -replace '%~dp0', "$cmdDir\"
        $params.UiPath = [IO.Path]::GetFullPath($raw)
    }

    if (-not $params.CollectionName -or -not $params.CollectionPath -or -not $params.DeploymentPath) {
        return $null
    }

    return $params
}

# ===========================================================================
# Step dispatcher
# ===========================================================================
function Invoke-ReleaseStep {
    param(
        [hashtable]$Repo,
        [string]$StepName,
        [array]$AllRepos
    )

    # Check readiness
    $ready = Test-StepReady -Repo $Repo -StepName $StepName -AllRepos $AllRepos
    if ($ready -ne $true) {
        Write-Host "  BLOCKED: $($Repo.name) cannot run '$StepName' - $ready" -ForegroundColor Magenta
        return $false
    }

    $timestamp = Get-Date -Format 'yyyy-MM-ddTHH:mm:ss'
    $result = $null

    try {
        $result = switch ($StepName) {
            'merge'   { Invoke-StepMerge   -Repo $Repo }
            'nuget'   { Invoke-StepNuget   -Repo $Repo }
            'build'   { Invoke-StepBuild   -Repo $Repo }
            'test'    { Invoke-StepTest    -Repo $Repo }
            'staging' { Invoke-StepStaging -Repo $Repo }
            'smoke'   { Invoke-StepSmoke   -Repo $Repo }
            'publish' { Invoke-StepPublish -Repo $Repo }
        }

        # If the handler returned 'skipped' or 'pending', use that status
        if ($result -eq 'skipped') {
            $Repo.steps[$StepName].status = 'skipped'
            $Repo.steps[$StepName].timestamp = $timestamp
        } elseif ($result -eq 'pending') {
            # Leave as pending (smoke test or missing script)
        } else {
            $Repo.steps[$StepName].status = 'done'
            $Repo.steps[$StepName].timestamp = $timestamp
        }
        return $true
    }
    catch {
        Write-Host "  ERROR: $($_.Exception.Message)" -ForegroundColor Red
        $Repo.steps[$StepName].status = 'failed'
        $Repo.steps[$StepName].timestamp = $timestamp
        $Repo.steps[$StepName].notes = $_.Exception.Message
        return $false
    }
}

# ===========================================================================
# Report generator
# ===========================================================================
function Show-Report {
    param([hashtable]$Manifest)

    $stepOrder = @('merge', 'nuget', 'build', 'staging', 'test', 'smoke', 'publish')
    $repos = $Manifest.repos

    Write-Host ""
    Write-Host "Release $($Manifest.release) - Summary Report" -ForegroundColor Cyan
    Write-Host ("=" * 60) -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Created:  $($Manifest.created)"
    Write-Host "Status:   $($Manifest.status)"
    Write-Host "Repos:    $($repos.Count)"
    Write-Host ""

    # Per-step summary
    Write-Host "Step Summary:" -ForegroundColor Yellow
    Write-Host ("-" * 50)
    foreach ($s in $stepOrder) {
        $done    = @($repos | Where-Object { $_.steps[$s].status -eq 'done' }).Count
        $failed  = @($repos | Where-Object { $_.steps[$s].status -eq 'failed' }).Count
        $skipped = @($repos | Where-Object { $_.steps[$s].status -eq 'skipped' }).Count
        $pending = @($repos | Where-Object { $_.steps[$s].status -eq 'pending' }).Count
        $blocked = @($repos | Where-Object { $_.steps[$s].status -eq 'blocked' }).Count

        $stepLabel = $s.PadRight(10)
        Write-Host "  $stepLabel" -NoNewline
        Write-Host "done:$done " -NoNewline -ForegroundColor Green
        Write-Host "fail:$failed " -NoNewline -ForegroundColor Red
        Write-Host "skip:$skipped " -NoNewline -ForegroundColor DarkGray
        Write-Host "wait:$pending " -NoNewline -ForegroundColor Gray
        Write-Host "blkd:$blocked" -ForegroundColor Magenta
    }
    Write-Host ""

    # Per-tier summary
    Write-Host "Tier Summary:" -ForegroundColor Yellow
    Write-Host ("-" * 50)
    $tierNames = @{ 0 = 'Foundation'; 1 = 'Shared Libraries'; 2 = 'Leaf Addons'; 3 = 'Data/UI Only' }
    foreach ($t in 0..3) {
        $tierRepos = @($repos | Where-Object { $_.tier -eq $t })
        if ($tierRepos.Count -eq 0) { continue }
        $published = @($tierRepos | Where-Object { $_.steps['publish'].status -eq 'done' }).Count
        Write-Host "  Tier $t ($($tierNames[$t])): $published/$($tierRepos.Count) published"
    }
    Write-Host ""

    # Failed repos
    $failedRepos = @($repos | Where-Object {
        $failed = $false
        foreach ($s in $stepOrder) {
            if ($_.steps[$s].status -eq 'failed') { $failed = $true; break }
        }
        $failed
    })
    if ($failedRepos.Count -gt 0) {
        Write-Host "Failed Steps:" -ForegroundColor Red
        Write-Host ("-" * 50)
        foreach ($r in $failedRepos) {
            foreach ($s in $stepOrder) {
                if ($r.steps[$s].status -eq 'failed') {
                    $notes = if ($r.steps[$s].notes) { " - $($r.steps[$s].notes)" } else { '' }
                    Write-Host "  $($r.name) / $s$notes" -ForegroundColor Red
                }
            }
        }
        Write-Host ""
    }

    Write-Host ("=" * 60) -ForegroundColor Cyan
    Write-Host ""
}

# ===========================================================================
# Main
# ===========================================================================

# Default to -Status if no parameters specified
if (-not $Step -and -not $Mark -and -not $Report -and -not $Status) {
    $Status = $true
}

# --- Status dashboard ---
if ($Status) {
    $manifest = Read-ReleaseManifest -Path $ManifestPath
    Show-Dashboard -Manifest $manifest
    return
}

# --- Report ---
if ($Report) {
    $manifest = Read-ReleaseManifest -Path $ManifestPath
    Show-Report -Manifest $manifest
    return
}

# --- Mark a step manually ---
if ($Mark) {
    if (-not $Repo -and $Tier -lt 0 -and -not $All) {
        throw "Specify -Repo, -Tier, or -All to select target repos for -Mark."
    }
    $manifest = Read-ReleaseManifest -Path $ManifestPath

    # Determine target repos
    $targets = @()
    if ($Repo) {
        $found = $manifest.repos | Where-Object { $_.name -eq $Repo } | Select-Object -First 1
        if (-not $found) { throw "Repo '$Repo' not found in manifest." }
        $targets = @($found)
    } elseif ($Tier -ge 0) {
        $targets = @($manifest.repos | Where-Object { $_.tier -eq $Tier } | Sort-Object { $_.name })
    } elseif ($All) {
        $targets = @($manifest.repos | Sort-Object { $_.tier }, { $_.name })
    }

    $timestamp = Get-Date -Format 'yyyy-MM-ddTHH:mm:ss'
    $count = 0
    foreach ($t in $targets) {
        if (-not $t.steps.ContainsKey($Step)) { continue }
        $t.steps[$Step].status = $Mark
        $t.steps[$Step].timestamp = $timestamp
        $count++
    }

    Write-ReleaseManifest -Manifest $manifest -Path $ManifestPath
    if ($Repo) {
        Write-Host "Marked $Repo / $Step as $Mark" -ForegroundColor Green
    } else {
        Write-Host "Marked $count repo(s) / $Step as $Mark" -ForegroundColor Green
    }
    return
}

# --- Execute a step ---
if ($Step) {
    $manifest = Read-ReleaseManifest -Path $ManifestPath

    # Determine target repos
    $targets = @()
    if ($Repo) {
        $found = $manifest.repos | Where-Object { $_.name -eq $Repo } | Select-Object -First 1
        if (-not $found) { throw "Repo '$Repo' not found in manifest." }
        $targets = @($found)
    } elseif ($Tier -ge 0) {
        $targets = @($manifest.repos | Where-Object { $_.tier -eq $Tier } | Sort-Object { $_.name })
    } elseif ($All) {
        # All repos sorted by tier then name
        $targets = @($manifest.repos | Sort-Object { $_.tier }, { $_.name })
    } else {
        throw "Specify -Repo, -Tier, or -All to select target repos."
    }

    if ($targets.Count -eq 0) {
        Write-Host "No repos matched the selection." -ForegroundColor Yellow
        return
    }

    # If -Retry, reset failed steps to pending first
    if ($Retry) {
        foreach ($t in $targets) {
            if ($t.steps[$Step].status -eq 'failed') {
                $t.steps[$Step].status = 'pending'
                $t.steps[$Step].notes = ''
                Write-Host "  Reset $($t.name) / $Step from failed to pending" -ForegroundColor Yellow
            }
        }
    }

    Write-Host ""
    Write-Host "Executing step '$Step' for $($targets.Count) repo(s)..." -ForegroundColor Cyan
    Write-Host ""

    $succeeded = 0
    $failed = 0
    $skippedCount = 0
    $blockedCount = 0

    foreach ($t in $targets) {
        Write-Host "--- $($t.name) ---" -ForegroundColor White
        $currentStatus = $t.steps[$Step].status
        if ($currentStatus -eq 'done') {
            Write-Host "  Already done -skipping" -ForegroundColor DarkGray
            $skippedCount++
            continue
        }
        if ($currentStatus -eq 'skipped') {
            Write-Host "  Marked as skipped -skipping" -ForegroundColor DarkGray
            $skippedCount++
            continue
        }

        $result = Invoke-ReleaseStep -Repo $t -StepName $Step -AllRepos $manifest.repos
        $finalStatus = $t.steps[$Step].status
        if ($finalStatus -eq 'done' -or $finalStatus -eq 'skipped') {
            $succeeded++
        } elseif ($finalStatus -eq 'failed') {
            $failed++
        } else {
            # pending (smoke test) or blocked
            $blockedCount++
        }
    }

    # Save the manifest
    Write-ReleaseManifest -Manifest $manifest -Path $ManifestPath

    Write-Host ""
    Write-Host "Step '$Step' complete: $succeeded succeeded, $failed failed, $skippedCount skipped, $blockedCount blocked/pending" -ForegroundColor Cyan
    Write-Host ""
}
