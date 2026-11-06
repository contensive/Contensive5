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
    The release step to execute: merge, nuget, build, test, staging, e2e, smoke, publish.
    The e2e step deploys the staged collection zip to the shared test site, runs the
    repo's Playwright suite (catalog e2e_path) and records the result. Publish is
    blocked until e2e is done (or skipped for repos without an e2e suite).

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

.PARAMETER Reason
    With -Mark: why the step is being marked by hand. Required for -Mark done or
    skipped on -Step e2e (an override); stored in the manifest as override_reason
    and shown in -Status and -Report. For other steps it is written to the notes.

.PARAMETER Hotfix
    Run a single-repo hotfix: build, test, e2e, publish on a branch, with its own
    hotfix-manifest-<yyyyMMdd>-<repo>.yml. Requires -Repo and -Branch.

.PARAMETER Branch
    With -Hotfix: the hotfix branch to check out and build.

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
    .\release-runner.ps1 -Step e2e -Repo aoEcommerce
    .\release-runner.ps1 -Mark done -Step e2e -Repo aoEcommerce -Reason "Stripe sandbox outage"
    .\release-runner.ps1 -Hotfix -Repo aoEcommerce -Branch hotfix/20261106-fix-refund
#>
[CmdletBinding(DefaultParameterSetName = 'Status')]
param(
    [Parameter(ParameterSetName = 'Status')]
    [switch]$Status,

    [Parameter(ParameterSetName = 'Execute', Mandatory)]
    [Parameter(ParameterSetName = 'Mark', Mandatory)]
    [Parameter(ParameterSetName = 'Hotfix')]
    [ValidateSet('merge', 'nuget', 'build', 'test', 'staging', 'e2e', 'smoke', 'publish')]
    [string]$Step,

    [Parameter(ParameterSetName = 'Mark', Mandatory)]
    [ValidateSet('done', 'failed', 'skipped', 'pending', 'blocked')]
    [string]$Mark,

    [Parameter(ParameterSetName = 'Execute')]
    [Parameter(ParameterSetName = 'Mark')]
    [Parameter(ParameterSetName = 'Hotfix', Mandatory)]
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
    [Parameter(ParameterSetName = 'Hotfix')]
    [switch]$Retry,

    [Parameter(ParameterSetName = 'Mark')]
    [string]$Reason = '',

    [Parameter(ParameterSetName = 'Hotfix', Mandatory)]
    [switch]$Hotfix,

    [Parameter(ParameterSetName = 'Hotfix', Mandatory)]
    [string]$Branch,

    [string]$ManifestPath = ''
)

$ErrorActionPreference = 'Stop'

# One canonical step order. keep in sync with new-release.ps1 $steps and the -Step ValidateSet above.
$script:stepOrder = @('merge', 'nuget', 'build', 'test', 'staging', 'e2e', 'smoke', 'publish')

$script:scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
if (-not $ManifestPath) {
    if ($Hotfix) {
        $ManifestPath = Join-Path $script:scriptDir "hotfix-manifest-$(Get-Date -Format 'yyyyMMdd')-$Repo.yml"
    } else {
        $ManifestPath = Join-Path $script:scriptDir 'release-manifest.yml'
    }
}

# ===========================================================================
# YAML parser for release-manifest.yml
# ===========================================================================
function Format-ManifestText {
    <#
    .SYNOPSIS
        Makes a value safe to write inside double quotes on one manifest line.
    #>
    param([string]$Text)
    if ($null -eq $Text) { return '' }
    $clean = $Text -replace '"', "'"
    $clean = $clean -replace '[\r\n]+', ' '
    return $clean
}

function Read-CatalogRepo {
    <#
    .SYNOPSIS
        Reads one repo's entry from repo-catalog.yml (next to this script).
        Returns a hashtable (name, path, tier, e2e_path, produces, depends_on) or $null.
    #>
    param([string]$RepoName)

    $catalogPath = Join-Path $script:scriptDir 'repo-catalog.yml'
    if (-not (Test-Path $catalogPath)) { return $null }

    $found = $null
    $inBlock = $false
    $lastArray = ''
    foreach ($line in (Get-Content $catalogPath)) {
        if ($line -match '^\s*#' -or $line -match '^\s*$') { continue }

        if ($line -match '^\s{2}- name:\s*(.+)$') {
            $thisName = $Matches[1].Trim()
            if ($found) { break }
            if ($thisName -eq $RepoName) {
                $inBlock = $true
                $lastArray = ''
                $found = @{
                    name       = $thisName
                    path       = ''
                    tier       = 0
                    e2e_path   = ''
                    produces   = @()
                    depends_on = @()
                }
            } else {
                $inBlock = $false
            }
            continue
        }
        if (-not $inBlock) { continue }

        if ($line -match '^\s{4}path:\s*(.+)$') { $found.path = $Matches[1].Trim(); continue }
        if ($line -match '^\s{4}tier:\s*(\d+)') { $found.tier = [int]$Matches[1]; continue }
        if ($line -match '^\s{4}e2e_path:\s*(.+)$') { $found.e2e_path = $Matches[1].Trim(); continue }
        if ($line -match '^\s{4}(produces|depends_on):\s*\[\]\s*$') { $lastArray = ''; continue }
        if ($line -match '^\s{4}(produces|depends_on):\s*$') { $lastArray = $Matches[1]; continue }
        if ($line -match '^\s{6}-\s+(.+)$' -and $lastArray) {
            $found[$lastArray] += $Matches[1].Trim()
            continue
        }
    }
    return $found
}

function Get-CatalogE2EPath {
    param([string]$RepoName)
    $entry = Read-CatalogRepo -RepoName $RepoName
    if ($entry) { return $entry.e2e_path }
    return ''
}

function New-StepData {
    param([string]$Status = 'pending', [string]$Notes = '')
    return @{
        status          = $Status
        timestamp       = $null
        notes           = $Notes
        report          = ''
        results         = ''
        artifact        = ''
        artifact_sha256 = ''
        override_reason = ''
    }
}

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
        kind    = ''
        branch  = ''
        commit  = ''
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
        if ($line -match '^kind:\s*(.+)$') { $manifest.kind = $Matches[1].Trim(); continue }
        if ($line -match '^branch:\s*(.+)$') { $manifest.branch = $Matches[1].Trim(); continue }
        if ($line -match '^commit:\s*(.+)$') { $manifest.commit = $Matches[1].Trim(); continue }
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
                e2e_path         = ''
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
        if ($line -match '^\s{4}e2e_path:\s*(.+)$') { $current.e2e_path = $Matches[1].Trim(); continue }

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
        if ($inSteps -and $line -match '^\s{6}(\w+):\s*\{\s*status:\s*(\w+),\s*timestamp:\s*(null|"[^"]*"|[\d\-T:]+),\s*notes:\s*"([^"]*)"(.*)\}\s*$') {
            # Copy $Matches to locals first: the extras loop below runs more regexes
            $stepName = $Matches[1]
            $stepStatus = $Matches[2]
            $stepTimestamp = $Matches[3]
            $stepNotes = $Matches[4]
            $extraText = $Matches[5]
            if ($stepTimestamp -eq 'null') { $stepTimestamp = $null }
            else { $stepTimestamp = $stepTimestamp.Trim('"') }

            $stepData = New-StepData -Status $stepStatus -Notes $stepNotes
            $stepData.timestamp = $stepTimestamp
            # Optional trailing keys: report, results, artifact, artifact_sha256, override_reason
            foreach ($m in [regex]::Matches($extraText, ',\s*(\w+):\s*"([^"]*)"')) {
                $stepData[$m.Groups[1].Value] = $m.Groups[2].Value
            }
            $current.steps[$stepName] = $stepData
        }
    }
    if ($current) { $manifest.repos += $current }

    # Migration: manifests written before the e2e step existed have no e2e key.
    # Without it, Test-StepReady would block publish ("step 'e2e' is ") for every repo.
    foreach ($r in $manifest.repos) {
        if (-not $r.steps.ContainsKey('e2e')) {
            $catalogE2E = Get-CatalogE2EPath -RepoName $r.name
            $hasSuite = $false
            if ($catalogE2E) {
                $configPath = Join-Path (Join-Path $r.path $catalogE2E) 'playwright.config.ts'
                $hasSuite = Test-Path $configPath
            }
            if ($hasSuite) {
                $r.e2e_path = $catalogE2E
                $r.steps['e2e'] = New-StepData -Status 'pending' -Notes 'e2e step added after manifest creation'
            } else {
                $r.steps['e2e'] = New-StepData -Status 'skipped' -Notes 'no e2e suite'
            }
        }
        foreach ($sn in $script:stepOrder) {
            if (-not $r.steps.ContainsKey($sn)) {
                $r.steps[$sn] = New-StepData -Status 'pending'
            }
        }
    }

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
    if ($Manifest.kind) { [void]$sb.AppendLine("kind: $($Manifest.kind)") }
    if ($Manifest.branch) { [void]$sb.AppendLine("branch: $($Manifest.branch)") }
    if ($Manifest.commit) { [void]$sb.AppendLine("commit: $($Manifest.commit)") }
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("repos:")

    $sorted = $Manifest.repos | Sort-Object { $_.tier }, { $_.name }

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
        if ($repo.e2e_path) { [void]$sb.AppendLine("    e2e_path: $($repo.e2e_path)") }
        [void]$sb.AppendLine("    steps:")

        foreach ($s in $script:stepOrder) {
            $stepData = $repo.steps[$s]
            if (-not $stepData) {
                $stepData = New-StepData -Status 'pending'
            }
            $ts = if ($stepData.timestamp) { "`"$($stepData.timestamp)`"" } else { 'null' }
            $stepNotesText = Format-ManifestText -Text $stepData.notes
            $line = "      $s`: { status: $($stepData.status), timestamp: $ts, notes: `"$stepNotesText`""
            # Optional trailing keys, written only when non-empty (other steps stay byte-identical)
            foreach ($extraKey in @('report', 'results', 'artifact', 'artifact_sha256', 'override_reason')) {
                $extraValue = $stepData[$extraKey]
                if ($extraValue) {
                    $line += ", $extraKey`: `"$(Format-ManifestText -Text $extraValue)`""
                }
            }
            [void]$sb.AppendLine("$line }")
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

    $sorted = $Manifest.repos | Sort-Object { $_.tier }, { $_.name }

    Write-Host ""
    if ($Manifest.kind -eq 'hotfix') {
        Write-Host "Hotfix $($Manifest.branch) ($($Manifest.release)) - Status Dashboard" -ForegroundColor Cyan
    } else {
        Write-Host "Release $($Manifest.release) - Status Dashboard" -ForegroundColor Cyan
    }
    Write-Host ("=" * 120) -ForegroundColor Cyan
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

            foreach ($s in $script:stepOrder) {
                $stepData = $repo.steps[$s]
                $stepStatus = if ($stepData) { $stepData.status } else { 'pending' }
                $label = "$s`:"

                switch ($stepStatus) {
                    'done' {
                        if ($s -eq 'e2e' -and $stepData.override_reason) { $display = 'OVRD'; $color = 'Yellow' }
                        else { $display = 'DONE'; $color = 'Green' }
                    }
                    'failed'  { $display = 'FAIL'; $color = 'Red' }
                    'skipped' { $display = 'SKIP'; $color = 'DarkGray' }
                    'blocked' { $display = 'BLKD'; $color = 'Magenta' }
                    default   { $display = '----'; $color = 'DarkGray' }
                }

                Write-Host "$label" -NoNewline
                Write-Host "$display  " -NoNewline -ForegroundColor $color
            }
            Write-Host ""
            $e2eData = $repo.steps['e2e']
            if ($e2eData -and $e2eData.status -eq 'failed' -and $e2eData.report) {
                Write-Host "    e2e report: $($e2eData.report)" -ForegroundColor DarkGray
            }
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

    Write-Host ("=" * 120) -ForegroundColor Cyan
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

    $stepIndex = [Array]::IndexOf($script:stepOrder, $StepName)

    # 1. Check step ordering -prior steps must be done or skipped
    for ($i = 0; $i -lt $stepIndex; $i++) {
        $priorStep = $script:stepOrder[$i]
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
# E2E helpers
# ===========================================================================
function Import-E2EEnvironment {
    <#
    .SYNOPSIS
        Loads %LOCALAPPDATA%\contensive\e2e.env (KEY=VALUE lines) into the process
        environment. Real environment variables win: a key is only set when the
        process variable is empty. Returns the keys it set so the caller can remove
        them again. Never prints a value.
    #>
    $setKeys = @()
    $envFile = Join-Path $env:LOCALAPPDATA 'contensive\e2e.env'
    if (Test-Path $envFile) {
        foreach ($rawLine in (Get-Content $envFile)) {
            $entry = $rawLine.Trim()
            if (-not $entry -or $entry.StartsWith('#')) { continue }
            $eqIndex = $entry.IndexOf('=')
            if ($eqIndex -lt 1) { continue }
            $key = $entry.Substring(0, $eqIndex).Trim()
            $value = $entry.Substring($eqIndex + 1).Trim()
            if ($value.Length -ge 2) {
                $first = $value.Substring(0, 1)
                $last = $value.Substring($value.Length - 1, 1)
                if (($first -eq '"' -and $last -eq '"') -or ($first -eq "'" -and $last -eq "'")) {
                    $value = $value.Substring(1, $value.Length - 2)
                }
            }
            if (-not $value) { continue }
            if ([string]::IsNullOrEmpty([Environment]::GetEnvironmentVariable($key, 'Process'))) {
                [Environment]::SetEnvironmentVariable($key, $value, 'Process')
                $setKeys += $key
            }
        }
        Write-Host "  Loaded $($setKeys.Count) e2e settings from $envFile" -ForegroundColor DarkGray
    }
    return $setKeys
}

function Test-E2EEnvironment {
    <#
    .SYNOPSIS
        Throws if a required e2e variable is missing or E2E_BASE_URL is not an
        allowed target. Messages carry variable names only, never values.
    #>
    $required = @('E2E_BASE_URL', 'E2E_ADMIN_USER', 'E2E_ADMIN_PASSWORD', 'E2E_CUSTOMER_PASSWORD', 'CONTENSIVE_TEST_TOKEN')
    $missing = @()
    foreach ($name in $required) {
        if ([string]::IsNullOrEmpty([Environment]::GetEnvironmentVariable($name, 'Process'))) { $missing += $name }
    }
    if ($missing.Count -gt 0) {
        $envFile = Join-Path $env:LOCALAPPDATA 'contensive\e2e.env'
        throw "Missing e2e setting(s): $($missing -join ', '). Set them as environment variables or in $envFile"
    }

    $uri = $null
    if (-not [Uri]::TryCreate($env:E2E_BASE_URL, [UriKind]::Absolute, [ref]$uri)) {
        throw "E2E_BASE_URL is not a valid absolute URL"
    }
    $allowed = @('test.sitefpo.com')
    if ($env:E2E_ALLOWED_HOSTS) {
        $allowed = @($env:E2E_ALLOWED_HOSTS.Split(',') | ForEach-Object { $_.Trim() } | Where-Object { $_ })
    }
    $hostAllowed = $false
    foreach ($h in $allowed) {
        if ($uri.Host -ieq $h) { $hostAllowed = $true }
    }
    if ($uri.Scheme -ne 'https' -or -not $hostAllowed) {
        throw "E2E_BASE_URL host '$($uri.Host)' is not an allowed e2e target"
    }
}

function Remove-E2EEnvironment {
    param([string[]]$Keys)
    $names = @('E2E_LOCK_ACTION', 'E2E_RUN_ID', 'E2E_RUN_OWNER', 'CI')
    if ($Keys) { $names += $Keys }
    foreach ($n in $names) {
        [Environment]::SetEnvironmentVariable($n, $null, 'Process')
    }
}

function Read-JsonFileOrNull {
    param([string]$Path)
    if (-not (Test-Path $Path)) { return $null }
    try {
        return (Get-Content $Path -Raw | ConvertFrom-Json)
    } catch {
        return $null
    }
}

function Get-RepoCollectionZips {
    <#
    .SYNOPSIS
        Returns the collection zips (FileInfo) that publish would upload for a repo:
        one per valid deploy-to-addon-library.cmd stub, found with the shared
        Find-CollectionZip, so e2e and publish always pick the same files.
    #>
    param([hashtable]$Repo)

    Import-Module (Join-Path $script:scriptDir '..\deploy-to-addon-library.psm1') -Force

    $scriptsDir = Join-Path $Repo.path 'scripts'
    $deployCmds = @(Get-ChildItem -Path $scriptsDir -Filter 'deploy-to-addon-library.cmd' -Recurse -ErrorAction SilentlyContinue)
    $zips = @()
    foreach ($cmdFile in $deployCmds) {
        $cmdParams = Read-DeployCmdParameters -CmdPath $cmdFile.FullName
        if (-not $cmdParams) {
            Write-Host "  SKIP: Could not parse deploy parameters from $($cmdFile.FullName)" -ForegroundColor Yellow
            continue
        }
        if (-not (Test-Path $cmdParams.CollectionPath)) {
            Write-Host "  SKIP: Collection path not found for '$($cmdParams.CollectionName)' -- $($cmdParams.CollectionPath)" -ForegroundColor Yellow
            continue
        }
        $zips += (Find-CollectionZip -DeploymentPath $cmdParams.DeploymentPath)
    }
    return $zips
}

function Test-PublishGate {
    <#
    .SYNOPSIS
        Publish precondition. Test-StepReady already requires e2e done or skipped.
        This adds the artifact check: the zip publish would upload must be the
        zip e2e tested (SHA-256), unless the e2e step was overridden.
    #>
    param([hashtable]$Repo)

    $e2e = $Repo.steps['e2e']
    if (-not $e2e) { return }
    if ($e2e.status -eq 'skipped') {
        if ($e2e.override_reason) {
            Write-Host "  Publishing $($Repo.name) with e2e skipped by OVERRIDE: $($e2e.override_reason)" -ForegroundColor Yellow
        }
        return
    }
    if ($e2e.override_reason) {
        Write-Host "  Publishing $($Repo.name) with e2e OVERRIDE: $($e2e.override_reason)" -ForegroundColor Yellow
        return
    }
    # e2e marked done by hand before the hash was recorded
    if (-not $e2e.artifact_sha256) { return }

    $hashes = @()
    foreach ($zip in @(Get-RepoCollectionZips -Repo $Repo)) {
        $hashes += (Get-FileHash -Path $zip.FullName -Algorithm SHA256).Hash
    }
    $nowText = $hashes -join ';'
    if ($nowText -ne $e2e.artifact_sha256) {
        throw "Collection zip changed since e2e tested it (tested $($e2e.artifact_sha256), now $nowText). Re-test with: .\release-runner.ps1 -Mark pending -Step e2e -Repo $($Repo.name); .\release-runner.ps1 -Step e2e -Repo $($Repo.name)"
    }
}

# ===========================================================================
# Hotfix manifest
# ===========================================================================
function New-HotfixManifest {
    <#
    .SYNOPSIS
        Builds a one-repo manifest for a hotfix. merge, nuget, staging and smoke are
        skipped; build, test, e2e and publish run on the hotfix branch.
    #>
    param(
        [hashtable]$CatalogEntry,
        [string]$Branch,
        [string]$RepoName
    )

    $today = Get-Date -Format 'yyyyMMdd'

    # Same checks as new-release.ps1 Get-RepoFeatures
    $hasTests = $false
    $hasBuildScript = $false
    if (Test-Path $CatalogEntry.path) {
        $testProjects = @(Get-ChildItem -Path $CatalogEntry.path -Filter '*Test*.csproj' -Recurse -ErrorAction SilentlyContinue)
        if ($testProjects.Count -gt 0) { $hasTests = $true }
        $buildScript = Join-Path $CatalogEntry.path 'scripts\build.ps1'
        $buildCmd = Join-Path $CatalogEntry.path 'scripts\build.cmd'
        $buildCoreCmd = Join-Path $CatalogEntry.path 'scripts\build-core.cmd'
        if ((Test-Path $buildScript) -or (Test-Path $buildCmd) -or (Test-Path $buildCoreCmd)) { $hasBuildScript = $true }
    }

    $e2ePath = ''
    if ($CatalogEntry.e2e_path) {
        $configPath = Join-Path (Join-Path $CatalogEntry.path $CatalogEntry.e2e_path) 'playwright.config.ts'
        if (Test-Path $configPath) { $e2ePath = $CatalogEntry.e2e_path }
    }

    $repoEntry = @{
        name             = $RepoName
        path             = $CatalogEntry.path
        tier             = $CatalogEntry.tier
        produces         = @($CatalogEntry.produces)
        depends_on       = @($CatalogEntry.depends_on)
        has_tests        = $hasTests
        has_build_script = $hasBuildScript
        e2e_path         = $e2ePath
        steps            = @{}
    }
    $repoEntry.steps['merge']   = New-StepData -Status 'skipped' -Notes 'hotfix: branch built directly'
    $repoEntry.steps['nuget']   = New-StepData -Status 'skipped' -Notes "hotfix: uses the branch's package references"
    $repoEntry.steps['build']   = New-StepData -Status 'pending'
    if ($hasTests) {
        $repoEntry.steps['test'] = New-StepData -Status 'pending'
    } else {
        $repoEntry.steps['test'] = New-StepData -Status 'skipped' -Notes 'no test project'
    }
    $repoEntry.steps['staging'] = New-StepData -Status 'skipped' -Notes 'hotfix'
    if ($e2ePath) {
        $repoEntry.steps['e2e'] = New-StepData -Status 'pending'
    } else {
        $repoEntry.steps['e2e'] = New-StepData -Status 'skipped' -Notes 'no e2e suite'
    }
    $repoEntry.steps['smoke']   = New-StepData -Status 'skipped' -Notes 'hotfix: e2e is the check'
    $repoEntry.steps['publish'] = New-StepData -Status 'pending'

    return @{
        release = "hotfix-$today-$RepoName"
        created = (Get-Date -Format 'yyyy-MM-dd')
        status  = 'in-progress'
        kind    = 'hotfix'
        branch  = $Branch
        commit  = ''
        repos   = @($repoEntry)
    }
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

function Invoke-StepE2E {
    param([hashtable]$Repo)

    $stepData = $Repo.steps['e2e']

    # --- Auto-skip: no suite, or Tier 0 ---
    if (-not $Repo.e2e_path) {
        $stepData.notes = 'no e2e suite'
        Write-Host "  No e2e suite for $($Repo.name) - skipping" -ForegroundColor Yellow
        return 'skipped'
    }
    $e2eDir = Join-Path $Repo.path $Repo.e2e_path
    if (-not (Test-Path (Join-Path $e2eDir 'playwright.config.ts'))) {
        $stepData.notes = 'no e2e suite'
        Write-Host "  No e2e suite for $($Repo.name) - skipping" -ForegroundColor Yellow
        return 'skipped'
    }
    if ($Repo.tier -eq 0) {
        $stepData.notes = 'core e2e is phase 3'
        Write-Host "  Core e2e is phase 3 - skipping" -ForegroundColor Yellow
        return 'skipped'
    }

    # --- Settings: nothing has changed yet, so a failure here is clean ---
    $setKeys = @(Import-E2EEnvironment)
    $lockDir = Join-Path $env:LOCALAPPDATA 'contensive'
    $lockFile = Join-Path $lockDir 'e2e-run.lock'
    $lockCreated = $false
    $lockAcquired = $false
    $pushed = $false

    try {
        Test-E2EEnvironment
        $baseUrl = $env:E2E_BASE_URL.TrimEnd('/')
        $siteName = ([Uri]$baseUrl).Host.Split('.')[0]

        if (-not (Get-Command npm -ErrorAction SilentlyContinue) -or -not (Get-Command node -ErrorAction SilentlyContinue)) {
            throw "Node.js (npm) not found on PATH. Install Node.js LTS (18 or later) on the release machine."
        }
        $nodeText = (& node --version | Out-String).Trim()
        if ($nodeText -match '^v(\d+)\.' -and [int]$Matches[1] -lt 18) {
            throw "Node.js $nodeText is too old. Install Node.js 18 or later."
        }

        # --- Machine lock: one e2e run at a time on this machine ---
        if (-not (Test-Path $lockDir)) { New-Item -ItemType Directory -Path $lockDir -Force | Out-Null }
        if (Test-Path $lockFile) {
            $lockInfo = Read-JsonFileOrNull -Path $lockFile
            $isLive = $false
            if ($lockInfo) {
                $lockProc = Get-Process -Id ([int]$lockInfo.pid) -ErrorAction SilentlyContinue
                $startedAt = $null
                try { $startedAt = [datetime]$lockInfo.started } catch { $startedAt = $null }
                if ($lockProc -and $startedAt -and ((Get-Date) - $startedAt).TotalHours -lt 3) { $isLive = $true }
            }
            if ($isLive) {
                throw "Another e2e run is in progress ($($lockInfo.repo), $($lockInfo.runId))"
            }
            Write-Host "  Removing stale e2e lock" -ForegroundColor Yellow
            Remove-Item $lockFile -Force
        }
        $runId = "$(Get-Date -Format 'yyyyMMdd-HHmmss')-$($Repo.name)"
        $lockJson = (@{ pid = $PID; runId = $runId; repo = $Repo.name; started = (Get-Date).ToString('yyyy-MM-ddTHH:mm:ss') } | ConvertTo-Json -Compress)
        $lockStream = [System.IO.File]::Open($lockFile, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::Write)
        try {
            $lockWriter = New-Object System.IO.StreamWriter($lockStream)
            $lockWriter.Write($lockJson)
            $lockWriter.Flush()
        } finally {
            $lockStream.Close()
        }
        $lockCreated = $true

        $env:E2E_RUN_ID = $runId
        $env:E2E_RUN_OWNER = 'release-runner'
        $env:CI = '1'

        Push-Location $e2eDir
        $pushed = $true
        $resultsDir = Join-Path $e2eDir 'e2e-results'
        if (-not (Test-Path $resultsDir)) { New-Item -ItemType Directory -Path $resultsDir -Force | Out-Null }
        # Remove results from earlier runs so stale files are never read as this run's
        foreach ($stale in @('results.json', 'run-info.json', 'site-lock.json')) {
            Remove-Item (Join-Path $resultsDir $stale) -Force -ErrorAction SilentlyContinue
        }

        # --- Install suite dependencies ---
        Write-Host "  Installing e2e dependencies (npm ci)..."
        & npm ci --no-audit --no-fund | Out-Host
        if ($LASTEXITCODE -ne 0) { throw "npm ci failed in $e2eDir" }
        & npx playwright install chromium | Out-Host
        if ($LASTEXITCODE -ne 0) { throw "playwright install chromium failed" }

        # --- Site lock on the test site ---
        Write-Host "  Acquiring the e2e lock on $baseUrl..."
        $env:E2E_LOCK_ACTION = 'acquire'
        & npx playwright test --project=site-lock --reporter=list | Out-Host
        $lockExit = $LASTEXITCODE
        Remove-Item Env:\E2E_LOCK_ACTION -ErrorAction SilentlyContinue
        $lockResult = Read-JsonFileOrNull -Path (Join-Path $resultsDir 'site-lock.json')
        if ($lockExit -eq 0 -and $lockResult -and $lockResult.ok) {
            $lockAcquired = $true
        } else {
            $heldBy = 'unknown'
            if ($lockResult -and $lockResult.heldBy) { $heldBy = "$($lockResult.heldBy)" }
            throw "Test site is locked: $heldBy"
        }
        $manualJobNote = ''
        if ($lockResult.manualJob) {
            Write-Host "  WARNING: manual job build on test site: $($lockResult.manualJob)." -ForegroundColor Yellow
            Write-Host "  The e2e deploy replaces the collection on the test site." -ForegroundColor Yellow
            $manualJobNote = "manual job on site: $($lockResult.manualJob); "
        }

        # --- Deploy the exact zip(s) publish would upload ---
        Import-Module (Join-Path $script:scriptDir '..\build-addon-collection.psm1') -Force
        $zips = @(Get-RepoCollectionZips -Repo $Repo)
        if ($zips.Count -eq 0) { throw "No collection zip to deploy for $($Repo.name)" }
        $paths = @()
        $hashes = @()
        foreach ($zip in $zips) {
            $hash = (Get-FileHash -Path $zip.FullName -Algorithm SHA256).Hash
            Write-Host "  Deploying $($zip.FullName) to $siteName..."
            Invoke-ContensiveDeploy -SiteUrl "$baseUrl/installCollection" -CollectionZip $zip.FullName `
                -SiteName $siteName -BearerToken $env:CONTENSIVE_TEST_TOKEN -NonInteractive | Out-Host
            $paths += $zip.FullName
            $hashes += $hash
        }
        $stepData.artifact = ($paths -join ';')
        $stepData.artifact_sha256 = ($hashes -join ';')

        # --- Wait for the site to come back after the install ---
        $timeoutSec = 180
        if ($env:E2E_READY_TIMEOUT_SEC -and $env:E2E_READY_TIMEOUT_SEC -match '^\d+$') { $timeoutSec = [int]$env:E2E_READY_TIMEOUT_SEC }
        $deadline = (Get-Date).AddSeconds($timeoutSec)
        $siteReady = $false
        while ((Get-Date) -lt $deadline) {
            try {
                $resp = Invoke-WebRequest -Uri "$baseUrl/" -UseBasicParsing -TimeoutSec 30
                if ($resp.StatusCode -eq 200) { $siteReady = $true; break }
            } catch {
                # not up yet
            }
            Start-Sleep -Seconds 5
        }
        if (-not $siteReady) { throw "Test site did not respond after deploy" }

        # --- Main run (no --grep on the CLI: it would filter out the setup projects) ---
        Write-Host "  Running Playwright suite..."
        & npx playwright test | Out-Host
        $pwExit = $LASTEXITCODE

        # --- Results ---
        $results = Read-JsonFileOrNull -Path (Join-Path $resultsDir 'results.json')
        $runInfo = Read-JsonFileOrNull -Path (Join-Path $resultsDir 'run-info.json')

        # --- Archive the report ---
        $reportRoot = 'C:\Deployments\e2e-reports'
        if ($env:E2E_REPORT_ROOT) { $reportRoot = $env:E2E_REPORT_ROOT }
        $dest = Join-Path (Join-Path $reportRoot $Repo.name) $runId
        New-Item -ItemType Directory -Path $dest -Force | Out-Null
        foreach ($folder in @('playwright-report', 'e2e-results')) {
            $src = Join-Path $e2eDir $folder
            if (Test-Path $src) { Copy-Item -Path $src -Destination (Join-Path $dest $folder) -Recurse -Force }
        }
        $stepData.report = Join-Path $dest 'playwright-report\index.html'

        if (-not $results -or -not $results.stats) {
            throw "Playwright produced no results (exit $pwExit) - see $dest"
        }
        $passed = [int]$results.stats.expected
        $failedCount = [int]$results.stats.unexpected
        $flaky = [int]$results.stats.flaky
        $skippedTests = [int]$results.stats.skipped
        $stepData.results = "passed=$passed failed=$failedCount flaky=$flaky skipped=$skippedTests"

        # --- Decide ---
        if ($runInfo -and $null -ne $runInfo.propertiesRestored -and $runInfo.propertiesRestored -eq $false) {
            throw "Site properties were NOT restored on the test site. Restore from $dest\e2e-results\run-info.json (snapshot section) before anyone tests."
        }
        if ($pwExit -ne 0 -or $failedCount -gt 0) {
            throw "$failedCount e2e test(s) failed - report: $($stepData.report)"
        }
        $stepData.notes = "$manualJobNote$passed passed, $flaky flaky, $skippedTests skipped"
        Write-Host "  E2E passed for $($Repo.name)" -ForegroundColor Green
        if ($flaky -gt 0) { Write-Host "  $flaky flaky test(s) - see report" -ForegroundColor Yellow }
    } finally {
        # Must not throw: every cleanup action is guarded
        if ($lockAcquired) {
            try {
                $env:E2E_LOCK_ACTION = 'release'
                & npx playwright test --project=site-lock --reporter=list | Out-Host
                if ($LASTEXITCODE -ne 0) {
                    Write-Host "  WARNING: releasing the e2e lock on the test site failed. Clear 'E2E Lock' in Site Properties." -ForegroundColor Red
                }
            } catch {
                Write-Host "  WARNING: releasing the e2e lock on the test site failed. Clear 'E2E Lock' in Site Properties." -ForegroundColor Red
            }
        }
        if ($pushed) { try { Pop-Location } catch { } }
        Remove-E2EEnvironment -Keys $setKeys
        if ($lockCreated) { Remove-Item $lockFile -Force -ErrorAction SilentlyContinue }
    }
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

    # Publish gate: e2e passed (or was overridden/skipped) and the zip is the one e2e tested
    Test-PublishGate -Repo $Repo

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
            'e2e'     { Invoke-StepE2E     -Repo $Repo }
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
    foreach ($s in $script:stepOrder) {
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
        foreach ($s in $script:stepOrder) {
            if ($_.steps[$s].status -eq 'failed') { $failed = $true; break }
        }
        $failed
    })
    if ($failedRepos.Count -gt 0) {
        Write-Host "Failed Steps:" -ForegroundColor Red
        Write-Host ("-" * 50)
        foreach ($r in $failedRepos) {
            foreach ($s in $script:stepOrder) {
                if ($r.steps[$s].status -eq 'failed') {
                    $notes = if ($r.steps[$s].notes) { " - $($r.steps[$s].notes)" } else { '' }
                    Write-Host "  $($r.name) / $s$notes" -ForegroundColor Red
                }
            }
        }
        Write-Host ""
    }

    # E2E overrides and reports
    $overrideRepos = @($repos | Where-Object { $_.steps['e2e'].override_reason })
    if ($overrideRepos.Count -gt 0) {
        Write-Host "E2E Overrides:" -ForegroundColor Yellow
        Write-Host ("-" * 50)
        foreach ($r in $overrideRepos) {
            Write-Host "  $($r.name): $($r.steps['e2e'].override_reason)" -ForegroundColor Yellow
        }
        Write-Host ""
    }
    $reportRepos = @($repos | Where-Object { $_.steps['e2e'].report })
    if ($reportRepos.Count -gt 0) {
        Write-Host "E2E Reports:" -ForegroundColor Yellow
        Write-Host ("-" * 50)
        foreach ($r in $reportRepos) {
            $e2eStep = $r.steps['e2e']
            Write-Host "  $($r.name): $($e2eStep.status) $($e2eStep.results) $($e2eStep.report)"
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
if (-not $Step -and -not $Mark -and -not $Report -and -not $Status -and -not $Hotfix) {
    $Status = $true
}

# --- Hotfix: build -> test -> e2e -> publish for one repo on a branch ---
if ($Hotfix) {
    $catalogEntry = Read-CatalogRepo -RepoName $Repo
    if (-not $catalogEntry) { throw "Repo '$Repo' not found in repo-catalog.yml" }

    $manifest = $null
    if (Test-Path $ManifestPath) {
        $manifest = Read-ReleaseManifest -Path $ManifestPath
        if ($manifest.branch -ne $Branch) {
            throw "Today's hotfix manifest for $Repo is for branch '$($manifest.branch)'. Finish it or pass -ManifestPath."
        }
    }
    $repoPath = $catalogEntry.path
    if (-not (Test-Path $repoPath)) { throw "Repo path not found: $repoPath" }

    Write-Host ""
    Write-Host "Hotfix $Branch for $Repo" -ForegroundColor Cyan

    # Git: clean tree, check out the branch, fast-forward it
    $dirty = Invoke-GitCommand -Arguments @('-C', $repoPath, 'status', '--porcelain')
    if ($dirty) { throw "Working tree not clean in $repoPath" }
    Invoke-GitCommand -Arguments @('-C', $repoPath, 'fetch', 'origin') | Out-Null
    Invoke-GitCommand -Arguments @('-C', $repoPath, 'checkout', $Branch) | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "git checkout $Branch failed in $repoPath" }
    Invoke-GitCommand -Arguments @('-C', $repoPath, 'rev-parse', '--verify', "origin/$Branch") | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Invoke-GitCommand -Arguments @('-C', $repoPath, 'pull', '--ff-only', 'origin', $Branch) | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "git pull --ff-only origin $Branch failed in $repoPath" }
    }
    Invoke-GitCommand -Arguments @('-C', $repoPath, 'merge-base', '--is-ancestor', 'origin/main', 'HEAD') | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  WARNING: $Branch does not contain origin/main" -ForegroundColor Yellow
    }
    $headSha = Invoke-GitCommand -Arguments @('-C', $repoPath, 'rev-parse', '--short', 'HEAD')

    # Create the manifest after checkout so has_tests / e2e detection sees the hotfix branch
    if (-not $manifest) {
        $manifest = New-HotfixManifest -CatalogEntry $catalogEntry -Branch $Branch -RepoName $Repo
        Write-Host "Created hotfix manifest: $ManifestPath" -ForegroundColor Green
    }
    $manifest.commit = "$headSha".Trim()
    $hotfixRepo = $manifest.repos[0]
    Write-ReleaseManifest -Manifest $manifest -Path $ManifestPath

    if ($Step) { $hotfixSteps = @($Step) } else { $hotfixSteps = @('build', 'test', 'e2e', 'publish') }

    foreach ($hs in $hotfixSteps) {
        Write-Host ""
        Write-Host "--- $Repo / $hs ---" -ForegroundColor White
        $hsStatus = $hotfixRepo.steps[$hs].status
        if ($Retry -and $hsStatus -eq 'failed') {
            $hotfixRepo.steps[$hs] = New-StepData -Status 'pending'
            Write-Host "  Reset $Repo / $hs from failed to pending" -ForegroundColor Yellow
            $hsStatus = 'pending'
        }
        if ($hsStatus -eq 'done' -or $hsStatus -eq 'skipped') {
            Write-Host "  Already $hsStatus" -ForegroundColor DarkGray
            continue
        }
        Invoke-ReleaseStep -Repo $hotfixRepo -StepName $hs -AllRepos $manifest.repos | Out-Null
        Write-ReleaseManifest -Manifest $manifest -Path $ManifestPath
        $hsStatus = $hotfixRepo.steps[$hs].status
        if ($hsStatus -ne 'done' -and $hsStatus -ne 'skipped') {
            Write-Host "  Hotfix stopped at '$hs' ($hsStatus). Fix it, then re-run with -Retry." -ForegroundColor Red
            break
        }
    }

    if ($hotfixRepo.steps['publish'].status -eq 'done') {
        $manifest.status = 'complete'
        Write-Host ""
        Write-Host "Hotfix published. Now merge $Branch into main and dev (PRs)." -ForegroundColor Green
    }
    Write-ReleaseManifest -Manifest $manifest -Path $ManifestPath
    Show-Dashboard -Manifest $manifest
    return
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
    $isE2EOverride = ($Step -eq 'e2e' -and ($Mark -eq 'done' -or $Mark -eq 'skipped'))

    # Marking e2e done/skipped by hand is an override and must say why
    if ($isE2EOverride -and -not $Reason) {
        foreach ($t in $targets) {
            if ($t.e2e_path -or $t.steps['e2e'].status -ne 'skipped') {
                throw "-Reason is required to mark e2e $Mark (it is recorded in the manifest)."
            }
        }
    }

    $count = 0
    foreach ($t in $targets) {
        if (-not $t.steps.ContainsKey($Step)) { continue }
        $t.steps[$Step].status = $Mark
        $t.steps[$Step].timestamp = $timestamp
        if ($isE2EOverride -and $Reason) {
            $t.steps[$Step].override_reason = Format-ManifestText -Text "$Reason (by $env:USERNAME $timestamp)"
            $t.steps[$Step].notes = 'OVERRIDE'
        } elseif ($Step -eq 'e2e' -and ($Mark -eq 'pending' -or $Mark -eq 'failed')) {
            # A re-run must re-test: forget the old override and artifact
            $t.steps[$Step].override_reason = ''
            $t.steps[$Step].artifact = ''
            $t.steps[$Step].artifact_sha256 = ''
            if ($Reason) { $t.steps[$Step].notes = Format-ManifestText -Text $Reason }
        } elseif ($Reason) {
            $t.steps[$Step].notes = Format-ManifestText -Text $Reason
        }
        $count++
    }

    Write-ReleaseManifest -Manifest $manifest -Path $ManifestPath
    if ($Repo) {
        if ($isE2EOverride -and $Reason) {
            Write-Host "Marked $Repo / $Step as $Mark (override: $Reason)" -ForegroundColor Green
        } else {
            Write-Host "Marked $Repo / $Step as $Mark" -ForegroundColor Green
        }
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
