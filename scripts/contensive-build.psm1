<#
.SYNOPSIS
    Contensive shared build library.
    Host: C:\Git\Contensive5\scripts\contensive-build.psm1

.DESCRIPTION
    Provides Invoke-ContensiveBuild — the single entry point that runs every
    step of a standard Contensive collection build.  Individual helper
    functions are also exported for repos that need to extend the standard flow.

    Typical caller (scripts/build.ps1 in an addon repo):

        $projectRoot = (Resolve-Path "$PSScriptRoot\..").Path
        Import-Module "C:\Git\Contensive5\scripts\contensive-build.psm1" -Force

        Invoke-ContensiveBuild `
            -CollectionName    'MyAddon' `
            -CollectionPath    "$projectRoot\Collections\MyAddon" `
            -SolutionPath      "$projectRoot\server\MyAddon.sln" `
            -BinPath           "$projectRoot\server\MyAddon\bin\Release" `
            -DeploymentRoot    'C:\deployments\myaddon' `
            -CollectionDlls    @('MyAddon.dll', 'MyAddon.dll.config') `
            -CleanFolders      @("$projectRoot\server\MyAddon\bin",
                                  "$projectRoot\server\MyAddon\obj") `
            -UiPath            "$projectRoot\ui" `
            -PackagesDirectory "$projectRoot\server\packages"
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ===========================================================================
# Private helpers
# ===========================================================================

function Get-ContensiveVersion {
<#
.SYNOPSIS
    Returns a date-based version string (YY.M.D.R) that does not collide
    with any existing folder under DeploymentRoot.
.PARAMETER DeploymentRoot
    Root folder under which versioned deployment sub-folders are created.
.OUTPUTS
    Version string, e.g. "26.4.4.1"
#>
    param(
        [Parameter(Mandatory)][string]$DeploymentRoot
    )
    $now   = Get-Date
    $yy    = $now.ToString('yy')
    $month = $now.Month.ToString()
    $day   = $now.Day.ToString()
    $rev   = 1
    while ($true) {
        $version = "$yy.$month.$day.$rev"
        if (-not (Test-Path (Join-Path $DeploymentRoot $version))) { break }
        $rev++
    }
    return $version
}

function Find-MSBuild {
<#
.SYNOPSIS
    Locates msbuild.exe via vswhere. Throws if not found.
.OUTPUTS
    Absolute path to msbuild.exe.
#>
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (-not (Test-Path $vswhere)) {
        throw "vswhere.exe not found at: $vswhere - install Visual Studio."
    }
    $msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild `
                          -find 'MSBuild\**\Bin\MSBuild.exe' 2>$null |
               Select-Object -First 1
    if (-not $msbuild) {
        throw "MSBuild not found. Install Visual Studio with the .NET desktop development workload."
    }
    return $msbuild
}

function New-DeploymentFolder {
<#
.SYNOPSIS
    Creates a versioned deployment folder and returns its path.
#>
    param(
        [Parameter(Mandatory)][string]$DeploymentRoot,
        [Parameter(Mandatory)][string]$Version
    )
    $folder = Join-Path $DeploymentRoot $Version
    if (-not (Test-Path $folder)) {
        New-Item -ItemType Directory -Path $folder | Out-Null
    }
    return $folder
}

function Invoke-ZipFolder {
<#
.SYNOPSIS
    Zips the contents of SourceFolder into DestZip using 7-Zip.
    Equivalent to: pushd $SourceFolder && 7z a -tzip -r $DestZip *
.PARAMETER SourceFolder
    Folder whose contents are zipped (not the folder itself).
.PARAMETER DestZip
    Output zip file path (absolute).
.PARAMETER Zip7Path
    Path to 7z.exe. Defaults to 'C:\Program Files\7-Zip\7z.exe'.
#>
    param(
        [Parameter(Mandatory)][string]$SourceFolder,
        [Parameter(Mandatory)][string]$DestZip,
        [string]$Zip7Path = 'C:\Program Files\7-Zip\7z.exe'
    )
    if (-not (Test-Path $Zip7Path))     { throw "7-Zip not found at: $Zip7Path" }
    if (-not (Test-Path $SourceFolder)) { throw "Source folder not found: $SourceFolder" }

    Push-Location $SourceFolder
    try {
        & $Zip7Path a -tzip -r $DestZip '*'
        if ($LASTEXITCODE -ne 0) { throw "7-Zip failed: '$SourceFolder' -> '$DestZip'" }
    } finally {
        Pop-Location
    }
}

function Invoke-MSBuildSolution {
<#
.SYNOPSIS
    Runs NuGet restore then MSBuild /t:Rebuild /p:Configuration=Release.
    Intended for legacy .NET Framework solutions using packages.config.
.PARAMETER SolutionPath
    Absolute path to the .sln file.
.PARAMETER MSBuild
    Path to msbuild.exe (from Find-MSBuild).
.PARAMETER PackagesDirectory
    NuGet packages restore directory. Optional.
#>
    param(
        [Parameter(Mandatory)][string]$SolutionPath,
        [Parameter(Mandatory)][string]$MSBuild,
        [string]$PackagesDirectory = ''
    )
    if (-not (Test-Path $SolutionPath)) { throw "Solution not found: $SolutionPath" }

    Write-Host "Restoring NuGet packages..."
    $nugetArgs = @('restore', $SolutionPath)
    if ($PackagesDirectory) { $nugetArgs += @('-PackagesDirectory', $PackagesDirectory) }
    & nuget @nugetArgs
    if ($LASTEXITCODE -ne 0) { throw "NuGet restore failed for: $SolutionPath" }

    Write-Host "Building solution..."
    & $MSBuild $SolutionPath /p:Configuration=Release /p:Platform="Any CPU" /t:Rebuild /verbosity:minimal
    if ($LASTEXITCODE -ne 0) { throw "MSBuild failed for: $SolutionPath" }
}

# ===========================================================================
# Invoke-ContensiveBuild  — the single orchestrated entry point
# ===========================================================================
function Invoke-ContensiveBuild {
<#
.SYNOPSIS
    Runs a full Contensive collection build: clean, zip UI assets, build
    solution, assemble collection zip, deploy, and clean up.

.DESCRIPTION
    All build steps are defined here. Addon repos supply only configuration —
    they never duplicate step logic. To change the build process for every
    repo, edit this function.

.PARAMETER CollectionName
    Short name used for the collection zip and deployment folder label
    (e.g. 'FMA').

.PARAMETER CollectionPath
    Absolute path to the collection source folder that contains the
    collection XML and will stage the build artifacts.

.PARAMETER SolutionPath
    Absolute path to the .sln file to build.

.PARAMETER BinPath
    Absolute path to the Release bin folder produced by the build
    (e.g. '...\server\FMA\bin\Release').

.PARAMETER DeploymentRoot
    Root folder under which a new versioned sub-folder is created for
    each build (e.g. 'C:\deployments\fma').

.PARAMETER CollectionDlls
    Array of DLL (and config) file names to copy from BinPath into the
    collection folder before zipping.

.PARAMETER CleanFolders
    Array of folders to delete before building (typically bin\ and obj\
    for each project in the solution). Defaults to empty — nothing cleaned.

.PARAMETER UiPath
    Root folder containing UI asset sub-folders (wwwFiles, cdnFiles, etc.).
    If omitted, the UI packaging step is skipped.

.PARAMETER UiAssetFolders
    Sub-folder names under UiPath to zip into the collection.
    Defaults to the standard five: wwwFiles, cdnFiles, privateFiles,
    layoutFiles, helpFiles.

.PARAMETER PackagesDirectory
    NuGet packages restore target directory. Optional.

.PARAMETER Zip7Path
    Path to 7z.exe. Defaults to 'C:\Program Files\7-Zip\7z.exe'.
#>
    param(
        [Parameter(Mandatory)][string]   $CollectionName,
        [Parameter(Mandatory)][string]   $CollectionPath,
        [Parameter(Mandatory)][string]   $SolutionPath,
        [Parameter(Mandatory)][string]   $BinPath,
        [Parameter(Mandatory)][string]   $DeploymentRoot,
        [Parameter(Mandatory)][string[]] $CollectionDlls,
        [string[]] $CleanFolders    = @(),
        [string]   $UiPath          = '',
        [string[]] $UiAssetFolders  = @('wwwFiles', 'cdnFiles', 'privateFiles', 'layoutFiles', 'helpFiles'),
        [string]   $PackagesDirectory = '',
        [string]   $Zip7Path        = 'C:\Program Files\7-Zip\7z.exe'
    )

    # -----------------------------------------------------------------------
    # Step 1 — Resolve version, deployment folder, and tools
    # -----------------------------------------------------------------------
    $version          = Get-ContensiveVersion -DeploymentRoot $DeploymentRoot
    $deploymentFolder = New-DeploymentFolder  -DeploymentRoot $DeploymentRoot -Version $version
    $msbuild          = Find-MSBuild

    Write-Host ""
    Write-Host "========================================"
    Write-Host "Building $CollectionName v$version"
    Write-Host "========================================"
    Write-Host ""

    # -----------------------------------------------------------------------
    # Step 2 — Clean build output folders
    # -----------------------------------------------------------------------
    Write-Host "Cleaning build folders..."
    foreach ($folder in $CleanFolders) {
        if (Test-Path $folder) { Remove-Item $folder -Recurse -Force }
    }

    # Remove stale artifacts from the collection staging folder
    foreach ($dll in $CollectionDlls) {
        $f = Join-Path $CollectionPath $dll
        if (Test-Path $f) { Remove-Item $f -Force }
    }
    foreach ($asset in ($UiAssetFolders)) {
        $f = Join-Path $CollectionPath "$asset.zip"
        if (Test-Path $f) { Remove-Item $f -Force }
    }
    $collectionZip = Join-Path $CollectionPath "$CollectionName.zip"
    if (Test-Path $collectionZip) { Remove-Item $collectionZip -Force }

    # -----------------------------------------------------------------------
    # Step 3 — Package UI assets
    # -----------------------------------------------------------------------
    if ($UiPath) {
        Write-Host "Packaging UI assets..."
        foreach ($asset in $UiAssetFolders) {
            $srcFolder = Join-Path $UiPath $asset
            if (-not (Test-Path $srcFolder)) { New-Item -ItemType Directory -Path $srcFolder | Out-Null }
            Invoke-ZipFolder -SourceFolder $srcFolder `
                             -DestZip      (Join-Path $CollectionPath "$asset.zip") `
                             -Zip7Path     $Zip7Path
        }
    }

    # -----------------------------------------------------------------------
    # Step 4 — Build the solution
    # -----------------------------------------------------------------------
    Invoke-MSBuildSolution -SolutionPath      $SolutionPath `
                           -MSBuild           $msbuild `
                           -PackagesDirectory $PackagesDirectory

    # -----------------------------------------------------------------------
    # Step 5 — Assemble the collection zip and copy to deployment folder
    # -----------------------------------------------------------------------
    Write-Host "Building collection zip..."
    foreach ($dll in $CollectionDlls) {
        Copy-Item (Join-Path $BinPath $dll) -Destination $CollectionPath -Force
    }

    Push-Location $CollectionPath
    try {
        & $Zip7Path a -tzip $collectionZip '*'
        if ($LASTEXITCODE -ne 0) { throw "Failed to create collection zip" }
    } finally {
        Pop-Location
    }

    Copy-Item $collectionZip -Destination $deploymentFolder -Force

    Write-Host ""
    Write-Host "Collection deployed to: $deploymentFolder"

    # -----------------------------------------------------------------------
    # Step 6 — Clean staging files from collection folder
    # -----------------------------------------------------------------------
    Write-Host "Cleaning collection staging folder..."
    foreach ($dll in $CollectionDlls) {
        $f = Join-Path $CollectionPath $dll
        if (Test-Path $f) { Remove-Item $f -Force }
    }
    foreach ($asset in $UiAssetFolders) {
        $f = Join-Path $CollectionPath "$asset.zip"
        if (Test-Path $f) { Remove-Item $f -Force }
    }

    Write-Host ""
    Write-Host "========================================"
    Write-Host "Build complete: $CollectionName v$version"
    Write-Host "========================================"
}

Export-ModuleMember -Function Invoke-ContensiveBuild, `
                               Get-ContensiveVersion, Find-MSBuild, `
                               New-DeploymentFolder,  Invoke-ZipFolder, `
                               Invoke-MSBuildSolution
