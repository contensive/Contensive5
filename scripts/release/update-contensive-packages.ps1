#Requires -Version 5.1
<#
.SYNOPSIS
    Updates all Contensive.* NuGet package references in a repo to the latest
    versions available in the local NuGet feed (C:\NuGetLocalPackages).

.DESCRIPTION
    Scans all .csproj files in the target repo for PackageReference elements
    where Include starts with "Contensive.". For each, looks up the latest
    .nupkg file in the local feed folder and updates the Version attribute.

    Handles edge cases:
    - Test projects are updated too
    - Pinned versions (marked with <!-- pinned --> comment) generate a warning
    - Non-Contensive packages are left untouched
    - Prefers release versions over pre-release when both exist

.PARAMETER RepoPath
    Path to the repo root to update.

.PARAMETER LocalFeed
    Path to the local NuGet package folder. Defaults to C:\NuGetLocalPackages.

.PARAMETER WhatIf
    Dry run -- show what would change without modifying files.

.EXAMPLE
    .\update-contensive-packages.ps1 -RepoPath C:\Git\aoDesignBlocks
    .\update-contensive-packages.ps1 -RepoPath C:\Git\aoDesignBlocks -WhatIf
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)]
    [string]$RepoPath,

    [string]$LocalFeed = 'C:\NuGetLocalPackages'
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $RepoPath)) {
    throw "Repo path not found: $RepoPath"
}

if (-not (Test-Path $LocalFeed)) {
    throw "Local NuGet feed not found: $LocalFeed"
}

# ---------------------------------------------------------------------------
# Build a lookup of latest package versions from the local feed
# ---------------------------------------------------------------------------
function Get-LatestLocalPackages {
    param([string]$FeedPath)

    $packages = @{}
    $nupkgFiles = Get-ChildItem -Path $FeedPath -Filter '*.nupkg' -ErrorAction SilentlyContinue

    foreach ($file in $nupkgFiles) {
        # .nupkg filename format: PackageId.Version.nupkg
        # e.g. Contensive.CPBaseClass.26.11.1.30150.nupkg
        # or   Contensive.CPBaseClass.26.11.1.30150-dev.nupkg
        $baseName = $file.BaseName  # strip .nupkg

        # Find where the version starts -- look for the pattern where a segment
        # transitions from letters/dots to digits
        # Strategy: split on '.', find the first segment that's all digits
        $parts = $baseName -split '\.'
        $versionStart = -1
        for ($i = 0; $i -lt $parts.Count; $i++) {
            if ($parts[$i] -match '^\d+') {
                $versionStart = $i
                break
            }
        }

        if ($versionStart -lt 1) { continue }

        $packageId = ($parts[0..($versionStart - 1)]) -join '.'
        $versionStr = ($parts[$versionStart..($parts.Count - 1)]) -join '.'

        # Check if this is a pre-release version (contains a hyphen)
        $isPreRelease = $versionStr -match '-'

        if (-not $packages.ContainsKey($packageId)) {
            $packages[$packageId] = @{
                Version      = $versionStr
                IsPreRelease = $isPreRelease
                File         = $file.Name
            }
        } else {
            $existing = $packages[$packageId]
            # Prefer release over pre-release
            if ($existing.IsPreRelease -and -not $isPreRelease) {
                $packages[$packageId] = @{
                    Version      = $versionStr
                    IsPreRelease = $isPreRelease
                    File         = $file.Name
                }
            }
            # If both are same type (both release or both pre-release),
            # prefer the newer one by file write time
            elseif ($existing.IsPreRelease -eq $isPreRelease) {
                $existingFile = Get-Item (Join-Path $LocalFeed $existing.File) -ErrorAction SilentlyContinue
                if ($existingFile -and $file.LastWriteTime -gt $existingFile.LastWriteTime) {
                    $packages[$packageId] = @{
                        Version      = $versionStr
                        IsPreRelease = $isPreRelease
                        File         = $file.Name
                    }
                }
            }
        }
    }

    return $packages
}

# ---------------------------------------------------------------------------
# Update .csproj files
# ---------------------------------------------------------------------------
$localPackages = Get-LatestLocalPackages -FeedPath $LocalFeed
$csprojFiles = Get-ChildItem -Path $RepoPath -Filter '*.csproj' -Recurse -ErrorAction SilentlyContinue

if ($csprojFiles.Count -eq 0) {
    Write-Host "No .csproj files found in $RepoPath" -ForegroundColor Yellow
    return
}

$totalUpdated = 0
$totalSkipped = 0
$totalPinned = 0

foreach ($csproj in $csprojFiles) {
    $content = Get-Content $csproj.FullName -Raw
    $originalContent = $content
    $relativePath = $csproj.FullName.Substring($RepoPath.Length).TrimStart('\', '/')
    $fileUpdated = $false

    # Find all PackageReference lines with Contensive.* packages
    $pattern = '(<PackageReference\s+Include="(Contensive\.[^"]+)"\s+Version=")([^"]+)(")'
    $matches = [regex]::Matches($content, $pattern)

    foreach ($m in $matches) {
        $fullMatch = $m.Value
        $packageId = $m.Groups[2].Value
        $currentVersion = $m.Groups[3].Value

        # Check for pinned comment -- look at the line before or same line
        $lineIndex = $content.Substring(0, $m.Index).Split("`n").Count - 1
        $lines = $content.Split("`n")
        $isPinned = $false
        if ($lineIndex -gt 0 -and $lines[$lineIndex - 1] -match '<!--\s*pinned\s*-->') {
            $isPinned = $true
        }
        if ($lines[$lineIndex] -match '<!--\s*pinned\s*-->') {
            $isPinned = $true
        }

        if ($isPinned) {
            Write-Host "  PINNED: $relativePath - $packageId @ $currentVersion" -ForegroundColor Yellow
            $totalPinned++
            continue
        }

        if (-not $localPackages.ContainsKey($packageId)) {
            Write-Host "  NOT FOUND: $packageId not in local feed -- keeping $currentVersion" -ForegroundColor DarkGray
            $totalSkipped++
            continue
        }

        $latestVersion = $localPackages[$packageId].Version
        if ($currentVersion -eq $latestVersion) {
            $totalSkipped++
            continue
        }

        $newRef = "$($m.Groups[1].Value)$latestVersion$($m.Groups[4].Value)"

        if ($WhatIf -or $PSCmdlet.ShouldProcess("$relativePath", "Update $packageId $currentVersion -> $latestVersion")) {
            Write-Host "  UPDATE: $relativePath - $packageId $currentVersion -> $latestVersion" -ForegroundColor Green
            $content = $content.Replace($fullMatch, $newRef)
            $fileUpdated = $true
            $totalUpdated++
        }
    }

    if ($fileUpdated -and -not $WhatIf) {
        Set-Content -Path $csproj.FullName -Value $content -NoNewline -Encoding UTF8
    }
}

Write-Host ""
Write-Host "NuGet update summary for $($RepoPath | Split-Path -Leaf):"
Write-Host "  Updated: $totalUpdated"
Write-Host "  Skipped: $totalSkipped (already current or not in feed)"
Write-Host "  Pinned:  $totalPinned"

# Run dotnet restore if we made changes
if ($totalUpdated -gt 0 -and -not $WhatIf) {
    Write-Host ""
    Write-Host "Running dotnet restore..."
    $slnFiles = Get-ChildItem -Path $RepoPath -Filter '*.sln' -Recurse -Depth 2 -ErrorAction SilentlyContinue
    if ($slnFiles.Count -gt 0) {
        & dotnet restore $slnFiles[0].FullName
        if ($LASTEXITCODE -ne 0) {
            Write-Host "WARNING: dotnet restore failed -- check package versions" -ForegroundColor Red
        } else {
            Write-Host "Restore complete" -ForegroundColor Green
        }
    }
}
