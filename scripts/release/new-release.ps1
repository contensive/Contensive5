#Requires -Version 5.1
<#
.SYNOPSIS
    Generates a fresh release-manifest.yml for a new monthly release cycle.

.DESCRIPTION
    Reads repo-catalog.yml for tier assignments and package metadata, then
    scans each repo on disk to detect has_tests and has_build_script. Outputs
    a release-manifest.yml with all steps set to 'pending'.

.PARAMETER Month
    Release month label in YYYY-MM format. Defaults to the current month.

.PARAMETER CatalogPath
    Path to repo-catalog.yml. Defaults to the file alongside this script.

.PARAMETER OutputPath
    Path to write the generated release-manifest.yml. Defaults to
    release-manifest.yml alongside this script.

.EXAMPLE
    .\new-release.ps1
    .\new-release.ps1 -Month 2026-12
#>
[CmdletBinding()]
param(
    [string]$Month = (Get-Date -Format 'yyyy-MM'),
    [string]$CatalogPath = '',
    [string]$OutputPath = ''
)

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
if (-not $CatalogPath) { $CatalogPath = Join-Path $scriptDir 'repo-catalog.yml' }
if (-not $OutputPath) { $OutputPath = Join-Path $scriptDir 'release-manifest.yml' }

# ---------------------------------------------------------------------------
# Minimal YAML parser -- repo-catalog.yml uses a simple flat structure
# ---------------------------------------------------------------------------
function Read-RepoCatalog {
    param([string]$Path)

    if (-not (Test-Path $Path)) {
        throw "Catalog file not found: $Path"
    }

    $lines = Get-Content $Path
    $repos = @()
    $current = $null

    foreach ($line in $lines) {
        # Skip comments and blank lines
        if ($line -match '^\s*#' -or $line -match '^\s*$') { continue }

        # New repo entry
        if ($line -match '^\s{2}- name:\s*(.+)$') {
            if ($current) { $repos += $current }
            $current = @{
                name       = $Matches[1].Trim()
                path       = ''
                tier       = 0
                produces   = @()
                depends_on = @()
                e2e_path   = ''
            }
            continue
        }

        if (-not $current) { continue }

        # Scalar fields
        if ($line -match '^\s{4}path:\s*(.+)$') {
            $current.path = $Matches[1].Trim()
        }
        elseif ($line -match '^\s{4}tier:\s*(\d+)') {
            $current.tier = [int]$Matches[1]
        }
        elseif ($line -match '^\s{4}e2e_path:\s*(.+)$') {
            $current.e2e_path = $Matches[1].Trim()
        }
        # Inline empty array
        elseif ($line -match '^\s{4}(produces|depends_on):\s*\[\]\s*$') {
            $current[$Matches[1]] = @()
        }
        # Array items
        elseif ($line -match '^\s{6}- (.+)$') {
            # Figure out which array we're appending to by looking at context
            # The last array-header line determines the target
            $current['_lastArray'] = $current['_lastArray']  # keep existing
        }
        # Array header followed by items
        elseif ($line -match '^\s{4}(produces|depends_on):\s*$') {
            $current['_lastArray'] = $Matches[1]
        }

        # Array item (re-check after setting _lastArray)
        if ($line -match '^\s{6}-\s+(.+)$' -and $current['_lastArray']) {
            $arrayName = $current['_lastArray']
            $current[$arrayName] += $Matches[1].Trim()
        }
    }
    if ($current) { $repos += $current }

    # Clean up temp keys
    foreach ($r in $repos) {
        $r.Remove('_lastArray')
    }

    return $repos
}

# ---------------------------------------------------------------------------
# Scan a repo for has_tests and has_build_script
# ---------------------------------------------------------------------------
function Get-RepoFeatures {
    param([string]$RepoPath)

    $result = @{
        has_tests        = $false
        has_build_script = $false
    }

    if (-not (Test-Path $RepoPath)) {
        return $result
    }

    # Check for test projects -- look for *Test*.csproj or *Tests*.csproj
    $testProjects = Get-ChildItem -Path $RepoPath -Filter '*Test*.csproj' -Recurse -ErrorAction SilentlyContinue
    if ($testProjects.Count -gt 0) {
        $result.has_tests = $true
    }

    # Check for build script (includes build-core.cmd for Tier 0)
    $buildScript = Join-Path $RepoPath 'scripts\build.ps1'
    $buildCmd = Join-Path $RepoPath 'scripts\build.cmd'
    $buildCoreCmd = Join-Path $RepoPath 'scripts\build-core.cmd'
    if ((Test-Path $buildScript) -or (Test-Path $buildCmd) -or (Test-Path $buildCoreCmd)) {
        $result.has_build_script = $true
    }

    return $result
}

# ---------------------------------------------------------------------------
# Generate the manifest YAML
# ---------------------------------------------------------------------------
function Write-ReleaseManifest {
    param(
        [string]$Month,
        [array]$Repos,
        [string]$OutputPath
    )

    $today = Get-Date -Format 'yyyy-MM-dd'
    $sb = [System.Text.StringBuilder]::new()

    [void]$sb.AppendLine("release: `"$Month`"")
    [void]$sb.AppendLine("created: `"$today`"")
    [void]$sb.AppendLine("status: in-progress")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("repos:")

    # Sort by tier, then alphabetically within tier
    $sorted = $Repos | Sort-Object { $_.tier }, { $_.name }

    foreach ($repo in $sorted) {
        $features = Get-RepoFeatures -RepoPath $repo.path

        [void]$sb.AppendLine("  - name: $($repo.name)")
        [void]$sb.AppendLine("    path: $($repo.path)")
        [void]$sb.AppendLine("    tier: $($repo.tier)")

        # produces
        if ($repo.produces.Count -eq 0) {
            [void]$sb.AppendLine("    produces: []")
        } else {
            [void]$sb.AppendLine("    produces:")
            foreach ($p in $repo.produces) {
                [void]$sb.AppendLine("      - $p")
            }
        }

        # depends_on
        if ($repo.depends_on.Count -eq 0) {
            [void]$sb.AppendLine("    depends_on: []")
        } else {
            [void]$sb.AppendLine("    depends_on:")
            foreach ($d in $repo.depends_on) {
                [void]$sb.AppendLine("      - $d")
            }
        }

        [void]$sb.AppendLine("    has_tests: $($features.has_tests.ToString().ToLower())")
        [void]$sb.AppendLine("    has_build_script: $($features.has_build_script.ToString().ToLower())")
        if ($repo.e2e_path) { [void]$sb.AppendLine("    e2e_path: $($repo.e2e_path)") }

        # Steps
        [void]$sb.AppendLine("    steps:")
        $steps = @('merge', 'nuget', 'build', 'test', 'staging', 'e2e', 'smoke', 'publish')  # keep in sync with release-runner.ps1 $script:stepOrder
        $hasE2ESuite = $false
        if ($repo.e2e_path) {
            $hasE2ESuite = Test-Path (Join-Path (Join-Path $repo.path $repo.e2e_path) 'playwright.config.ts')
        }
        foreach ($step in $steps) {
            # Tier 3 repos skip build and test
            if ($repo.tier -eq 3 -and ($step -eq 'build' -or $step -eq 'test')) {
                [void]$sb.AppendLine("      $step`: { status: skipped, timestamp: null, notes: `"`" }")
            }
            # Repos without an e2e suite (no e2e_path, or no playwright.config.ts yet) skip the e2e step
            elseif ($step -eq 'e2e' -and -not $hasE2ESuite) {
                [void]$sb.AppendLine("      $step`: { status: skipped, timestamp: null, notes: `"no e2e suite`" }")
            }
            # Repos without tests skip the test step
            elseif ($step -eq 'test' -and -not $features.has_tests) {
                [void]$sb.AppendLine("      $step`: { status: skipped, timestamp: null, notes: `"no test project`" }")
            }
            else {
                [void]$sb.AppendLine("      $step`: { status: pending, timestamp: null, notes: `"`" }")
            }
        }

        [void]$sb.AppendLine("")
    }

    $content = $sb.ToString().TrimEnd() + "`n"
    Set-Content -Path $OutputPath -Value $content -NoNewline -Encoding UTF8
    Write-Host "Release manifest generated: $OutputPath"
    Write-Host "  Month:  $Month"
    Write-Host "  Repos:  $($sorted.Count)"
    Write-Host "  Tier 0: $(@($sorted | Where-Object { $_.tier -eq 0 }).Count)"
    Write-Host "  Tier 1: $(@($sorted | Where-Object { $_.tier -eq 1 }).Count)"
    Write-Host "  Tier 2: $(@($sorted | Where-Object { $_.tier -eq 2 }).Count)"
    Write-Host "  Tier 3: $(@($sorted | Where-Object { $_.tier -eq 3 }).Count)"
}

# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "========================================"
Write-Host "  New Release: $Month"
Write-Host "========================================"
Write-Host ""

$repos = Read-RepoCatalog -Path $CatalogPath
Write-ReleaseManifest -Month $Month -Repos $repos -OutputPath $OutputPath
