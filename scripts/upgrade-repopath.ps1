#Requires -Version 5.1
<#
.SYNOPSIS
    Upgrades all Contensive addon build.ps1 files under C:\Git to pass
    -RepoPath $projectRoot to Get-ContensiveVersion.

.DESCRIPTION
    Scans every build.ps1 in C:\Git\*\scripts\ and C:\Git\*\scripts\*\ for
    calls to Get-ContensiveVersion that are missing -RepoPath. Performs the
    fix in-place and reports what was changed.

    Run from any directory — no parameters required.
#>

$ErrorActionPreference = 'Stop'

$gitRoot = 'C:\Git'
$fixed   = @()
$skipped = @()
$already = @()

# Find all build.ps1 files (single-collection and multi-collection patterns)
$buildFiles = @()
$buildFiles += Get-ChildItem -Path $gitRoot -Directory | ForEach-Object {
    $singleCol = Join-Path $_.FullName 'scripts\build.ps1'
    if (Test-Path $singleCol) { Get-Item $singleCol }
    # Multi-collection: scripts/<subfolder>/build.ps1
    $multiDir = Join-Path $_.FullName 'scripts'
    if (Test-Path $multiDir) {
        Get-ChildItem -Path $multiDir -Directory -ErrorAction SilentlyContinue | ForEach-Object {
            $multiCol = Join-Path $_.FullName 'build.ps1'
            if (Test-Path $multiCol) { Get-Item $multiCol }
        }
    }
}

Write-Host ""
Write-Host "========================================"
Write-Host "  Upgrade Get-ContensiveVersion calls"
Write-Host "  Adding -RepoPath `$projectRoot"
Write-Host "========================================"
Write-Host ""
Write-Host "Found $($buildFiles.Count) build.ps1 files to check"
Write-Host ""

foreach ($file in $buildFiles) {
    $content = Get-Content $file.FullName -Raw
    $relativePath = $file.FullName.Substring($gitRoot.Length + 1)

    # Skip if already has -RepoPath
    if ($content -match 'Get-ContensiveVersion[^;]*-RepoPath') {
        $already += $relativePath
        continue
    }

    # Check if it has a Get-ContensiveVersion call without -RepoPath
    if ($content -match "Get-ContensiveVersion\s+-DeploymentRoot\s+'[^']+'") {
        # Replace: append -RepoPath $projectRoot after the -DeploymentRoot parameter
        $updated = $content -replace "(Get-ContensiveVersion\s+-DeploymentRoot\s+'[^']+')", '$1 -RepoPath $projectRoot'
        Set-Content -Path $file.FullName -Value $updated -NoNewline
        $fixed += $relativePath
        Write-Host "  FIXED: $relativePath"
    } else {
        $skipped += $relativePath
        Write-Host "  SKIP:  $relativePath (no matching Get-ContensiveVersion call found)"
    }
}

Write-Host ""
Write-Host "========================================"
Write-Host "  Results"
Write-Host "========================================"
Write-Host ""
Write-Host "  Fixed:   $($fixed.Count)"
Write-Host "  Already: $($already.Count)"
Write-Host "  Skipped: $($skipped.Count)"
Write-Host ""

if ($already.Count -gt 0) {
    Write-Host "Already had -RepoPath:"
    $already | ForEach-Object { Write-Host "  $_" }
    Write-Host ""
}

if ($fixed.Count -gt 0) {
    Write-Host "Fixed (added -RepoPath `$projectRoot):"
    $fixed | ForEach-Object { Write-Host "  $_" }
    Write-Host ""
}

if ($skipped.Count -gt 0) {
    Write-Host "Skipped (manual review needed):"
    $skipped | ForEach-Object { Write-Host "  $_" }
    Write-Host ""
}
