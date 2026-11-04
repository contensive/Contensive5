#Requires -Version 5.1
<#
.SYNOPSIS
    One-time script to generate placeholder deploy script stubs for addon repos
    that are missing them.

.DESCRIPTION
    Scans repos listed in repo-catalog.yml and creates missing deploy script
    stubs following the established Contensive stub pattern.
    This script is idempotent -- it will not overwrite existing files.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

# ---------------------------------------------------------------------------
# Read repo catalog
# ---------------------------------------------------------------------------
$catalogPath = Join-Path $PSScriptRoot 'repo-catalog.yml'
$lines = Get-Content $catalogPath
$repos = @()
$current = $null

foreach ($line in $lines) {
    if ($line -match '^\s*#' -or $line -match '^\s*$') { continue }
    if ($line -match '^\s{2}- name:\s*(.+)$') {
        if ($current) { $repos += $current }
        $current = @{ name = $Matches[1].Trim(); path = ''; tier = 0 }
        continue
    }
    if (-not $current) { continue }
    if ($line -match '^\s{4}path:\s*(.+)$') { $current.path = $Matches[1].Trim() }
    if ($line -match '^\s{4}tier:\s*(\d+)') { $current.tier = [int]$Matches[1] }
}
if ($current) { $repos += $current }

# ---------------------------------------------------------------------------
# Extract collection info from existing build.ps1
# ---------------------------------------------------------------------------
function Get-CollectionInfo {
    param([string]$RepoPath, [string]$RepoName)

    $result = @{
        CollectionName = ''
        CollectionPath = ''
        DeploymentRoot = ''
        HasUi          = $false
    }

    $buildPs1 = Join-Path $RepoPath 'scripts\build.ps1'
    if (Test-Path $buildPs1) {
        $content = Get-Content $buildPs1 -Raw
        if ($content -match "-CollectionName\s+'([^']+)'") {
            $result.CollectionName = $Matches[1]
        } elseif ($content -match '-CollectionName\s+"([^"]+)"') {
            $result.CollectionName = $Matches[1]
        }
        if ($content -match "-DeploymentRoot\s+'([^']+)'") {
            $result.DeploymentRoot = $Matches[1]
        }
    }

    $result.HasUi = Test-Path (Join-Path $RepoPath 'ui')

    if (-not $result.CollectionName) { $result.CollectionName = $RepoName }
    if (-not $result.CollectionPath) { $result.CollectionPath = $result.CollectionName }
    if (-not $result.DeploymentRoot) { $result.DeploymentRoot = "C:\Deployments\$RepoName" }

    return $result
}

# ---------------------------------------------------------------------------
# Stub creation functions -- use arrays of lines to avoid here-string issues
# ---------------------------------------------------------------------------
function New-BuildDeployLocalCmd {
    param([string]$Path)
    $lines = @(
        '@echo off'
        'powershell -NoProfile -ExecutionPolicy Bypass -Command ^'
        "    `"& '%%~dp0build.ps1' -Configuration 'Debug' -DeployType 'local'`""
        'pause'
        'exit /b %errorlevel%'
    )
    # Fix the %% back to % -- we doubled them to avoid PS interpretation
    $text = ($lines -join "`r`n") -replace '%%', '%'
    Set-Content -Path $Path -Value $text -NoNewline -Encoding ASCII
}

function New-BuildDeployRemoteCmd {
    param([string]$Path)
    $lines = @(
        '@echo off'
        'powershell -NoProfile -ExecutionPolicy Bypass -Command ^'
        "    `"& '%%~dp0build.ps1' -Configuration 'Debug' -DeployType 'remote'`""
        'pause'
        'exit /b %errorlevel%'
    )
    $text = ($lines -join "`r`n") -replace '%%', '%'
    Set-Content -Path $Path -Value $text -NoNewline -Encoding ASCII
}

function New-DeployToAddonLibraryCmd {
    param([string]$Path, [string]$CollectionName, [string]$CollectionPath, [string]$DeploymentPath, [bool]$HasUi)
    $lines = @(
        '@echo off'
        'setlocal'
        ''
        'rem Uploads the built collection to the Addon Collection Library on contensive.com.'
        'rem TODO: verify CollectionName, CollectionPath, and DeploymentPath are correct for this repo.'
        ''
    )
    if ($HasUi) {
        $lines += @(
            "powershell -NoProfile -ExecutionPolicy Bypass -File `"%%~dp0deploy-to-addon-library.ps1`" ^"
            "    -CollectionName `"$CollectionName`" ^"
            "    -CollectionPath `"%%~dp0..\collections\$CollectionPath`" ^"
            "    -DeploymentPath `"$DeploymentPath`" ^"
            "    -UiPath `"%%~dp0..\ui`""
        )
    } else {
        $lines += @(
            "powershell -NoProfile -ExecutionPolicy Bypass -File `"%%~dp0deploy-to-addon-library.ps1`" ^"
            "    -CollectionName `"$CollectionName`" ^"
            "    -CollectionPath `"%%~dp0..\collections\$CollectionPath`" ^"
            "    -DeploymentPath `"$DeploymentPath`""
        )
    }
    $lines += @(
        ''
        'set exitCode=%errorlevel%'
        'pause'
        'exit /b %exitCode%'
    )
    $text = ($lines -join "`r`n") -replace '%%', '%'
    Set-Content -Path $Path -Value $text -NoNewline -Encoding ASCII
}

function New-DeployToAddonLibraryPs1 {
    param([string]$Path)
    $lines = @(
        '#Requires -Version 5.1'
        '<#'
        '.SYNOPSIS'
        '    Uploads a built collection to the Addon Collection Library on contensive.com.'
        ''
        '.DESCRIPTION'
        '    Thin wrapper that imports the shared deploy-to-addon-library module from'
        '    Contensive5\scripts\ and calls Invoke-AddonLibraryDeploy with the'
        '    parameters for this repo.'
        ''
        '    TODO: verify this placeholder works correctly for this repo.'
        '#>'
        '[CmdletBinding()]'
        'param('
        '    [Parameter(Mandatory)][string]$CollectionName,'
        '    [Parameter(Mandatory)][string]$CollectionPath,'
        '    [Parameter(Mandatory)][string]$DeploymentPath,'
        "    [string]`$UiPath = '',"
        "    [string]`$TargetDomain = 'www.contensive.com'"
        ')'
        ''
        "`$ErrorActionPreference = 'Stop'"
        ''
        "Import-Module (Join-Path `$PSScriptRoot '..\..\Contensive5\scripts\deploy-to-addon-library.psm1') -Force"
        ''
        'Invoke-AddonLibraryDeploy `'
        '    -CollectionName $CollectionName `'
        '    -CollectionPath $CollectionPath `'
        '    -DeploymentPath $DeploymentPath `'
        '    -UiPath         $UiPath `'
        '    -TargetDomain   $TargetDomain'
    )
    $text = $lines -join "`r`n"
    Set-Content -Path $Path -Value $text -NoNewline -Encoding UTF8
}

function New-BuildCmd {
    param([string]$Path, [string]$RepoName)
    $lines = @(
        '@echo off'
        'setlocal'
        ''
        "@echo Build $RepoName"
        'rem TODO: configure build.ps1 for this repo before using.'
        ''
        'powershell -NoProfile -ExecutionPolicy Bypass -File "%%~dp0build.ps1"'
        ''
        'if not "%%1"=="/nopause" pause'
    )
    $text = ($lines -join "`r`n") -replace '%%', '%'
    Set-Content -Path $Path -Value $text -NoNewline -Encoding ASCII
}

function New-BuildPs1Placeholder {
    param([string]$Path, [string]$RepoName, [string]$CollectionName, [string]$CollectionPath, [string]$DeploymentRoot, [bool]$HasUi)

    # Build a plain text file content -- no here-strings
    $sb = [System.Text.StringBuilder]::new()
    [void]$sb.AppendLine('#Requires -Version 5.1')
    [void]$sb.AppendLine("# TODO: Configure this build script for $RepoName.")
    [void]$sb.AppendLine('# This is a placeholder - verify CollectionName, CollectionPath, SolutionPath, and BinPath.')
    [void]$sb.AppendLine('[CmdletBinding()]')
    [void]$sb.AppendLine('param(')
    [void]$sb.AppendLine("    [ValidateSet('Debug','Release')]")
    [void]$sb.AppendLine("    [string]   `$Configuration      = 'Release',")
    [void]$sb.AppendLine("    [ValidateSet('','local','remote')]")
    [void]$sb.AppendLine("    [string]   `$DeployType         = ''")
    [void]$sb.AppendLine(')')
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine("`$ErrorActionPreference = 'Stop'")
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine("Import-Module (Join-Path `$PSScriptRoot '..\..\Contensive5\scripts\build-addon-collection.psm1') -Force")
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine("`$projectRoot = (Resolve-Path `"`$PSScriptRoot\..`").Path")
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine('# Display build info')
    [void]$sb.AppendLine("`$version = Get-ContensiveVersion -DeploymentRoot '$DeploymentRoot' -RepoPath `$projectRoot")
    [void]$sb.AppendLine('Write-Host ""')
    [void]$sb.AppendLine('Write-Host "========================================"')
    [void]$sb.AppendLine("Write-Host `"  $RepoName Build`"")
    [void]$sb.AppendLine('Write-Host "  Version:       $version"')
    [void]$sb.AppendLine('Write-Host "  Configuration: $Configuration"')
    [void]$sb.AppendLine('Write-Host "========================================"')
    [void]$sb.AppendLine('Write-Host ""')
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine('# After displaying info, either prompt for deploy target or pause')
    [void]$sb.AppendLine("`$LocalDeployTarget  = ''")
    [void]$sb.AppendLine("`$RemoteDeployTarget = `$null")
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine('switch ($DeployType) {')
    [void]$sb.AppendLine("    'local' {")
    [void]$sb.AppendLine("        `$defaultsFile = Join-Path `$env:LOCALAPPDATA 'contensive\local-deploy-appname.txt'")
    [void]$sb.AppendLine("        `$savedApp = ''")
    [void]$sb.AppendLine('        if (Test-Path $defaultsFile) {')
    [void]$sb.AppendLine('            $savedApp = (Get-Content $defaultsFile -First 1).Trim()')
    [void]$sb.AppendLine('        }')
    [void]$sb.AppendLine('        $prompt = if ($savedApp) { "Enter local site name [$savedApp]: " } else { "Enter local site name: " }')
    [void]$sb.AppendLine('        $appName = Read-Host $prompt')
    [void]$sb.AppendLine('        if (-not $appName) { $appName = $savedApp }')
    [void]$sb.AppendLine('        if (-not $appName) {')
    [void]$sb.AppendLine('            Write-Host "No site name entered."')
    [void]$sb.AppendLine('            exit 1')
    [void]$sb.AppendLine('        }')
    [void]$sb.AppendLine("        `$defaultsDir = Join-Path `$env:LOCALAPPDATA 'contensive'")
    [void]$sb.AppendLine('        if (-not (Test-Path $defaultsDir)) { New-Item -ItemType Directory -Path $defaultsDir | Out-Null }')
    [void]$sb.AppendLine('        $appName | Set-Content $defaultsFile')
    [void]$sb.AppendLine('        $LocalDeployTarget = $appName')
    [void]$sb.AppendLine('        Write-Host "Deploy to: $appName (local)"')
    [void]$sb.AppendLine('        Write-Host ""')
    [void]$sb.AppendLine('    }')
    [void]$sb.AppendLine("    'remote' {")
    [void]$sb.AppendLine("        `$defaultsFile = Join-Path `$env:LOCALAPPDATA 'contensive\remote-deploy-domain.txt'")
    [void]$sb.AppendLine("        `$savedDomain = ''")
    [void]$sb.AppendLine('        if (Test-Path $defaultsFile) {')
    [void]$sb.AppendLine('            $savedDomain = (Get-Content $defaultsFile -First 1).Trim()')
    [void]$sb.AppendLine('        }')
    [void]$sb.AppendLine('        $prompt = if ($savedDomain) { "Enter remote domain [$savedDomain]: " } else { "Enter remote domain: " }')
    [void]$sb.AppendLine('        $domain = Read-Host $prompt')
    [void]$sb.AppendLine('        if (-not $domain) { $domain = $savedDomain }')
    [void]$sb.AppendLine('        if (-not $domain) {')
    [void]$sb.AppendLine('            Write-Host "No domain entered."')
    [void]$sb.AppendLine('            exit 1')
    [void]$sb.AppendLine('        }')
    [void]$sb.AppendLine("        `$defaultsDir = Join-Path `$env:LOCALAPPDATA 'contensive'")
    [void]$sb.AppendLine('        if (-not (Test-Path $defaultsDir)) { New-Item -ItemType Directory -Path $defaultsDir | Out-Null }')
    [void]$sb.AppendLine('        $domain | Set-Content $defaultsFile')
    [void]$sb.AppendLine("        `$siteName = (`$domain -split '\.')[0]")
    [void]$sb.AppendLine('        $RemoteDeployTarget = @{ Url = "https://$domain/installCollection"; SiteName = $siteName }')
    [void]$sb.AppendLine('        Write-Host "Deploy to: $domain (remote)"')
    [void]$sb.AppendLine('        Write-Host ""')
    [void]$sb.AppendLine('    }')
    [void]$sb.AppendLine('    default {')
    [void]$sb.AppendLine('        Write-Host "Press any key to continue or Ctrl+C to cancel..."')
    [void]$sb.AppendLine("        `$null = `$Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')")
    [void]$sb.AppendLine('        Write-Host ""')
    [void]$sb.AppendLine('    }')
    [void]$sb.AppendLine('}')
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine('# TODO: Update SolutionPath, BinPath, CleanFolders, and NuGetProjects for this repo.')
    [void]$sb.AppendLine('Invoke-ContensiveBuild `')
    [void]$sb.AppendLine("    -CollectionName    '$CollectionName' ``")
    [void]$sb.AppendLine("    -CollectionPath    `"`$projectRoot\collections\$CollectionPath`" ``")
    [void]$sb.AppendLine("    -DeploymentRoot    '$DeploymentRoot' ``")
    [void]$sb.AppendLine("    -Configuration     `$Configuration ``")
    if ($HasUi) {
        [void]$sb.AppendLine("    -UiPath            `"`$projectRoot\ui`" ``")
    }
    [void]$sb.AppendLine('    -LocalDeployTarget  $LocalDeployTarget `')
    [void]$sb.AppendLine('    -RemoteDeployTarget $RemoteDeployTarget')

    $text = $sb.ToString().TrimEnd()
    Set-Content -Path $Path -Value $text -NoNewline -Encoding UTF8
}

# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------
$created = 0
$skippedExisting = 0

foreach ($repo in $repos) {
    $repoPath = $repo.path
    $repoName = $repo.name

    if ($repoName -eq 'Contensive5') { continue }

    if (-not (Test-Path $repoPath)) {
        Write-Host "SKIP: $repoName - repo not found at $repoPath" -ForegroundColor Yellow
        continue
    }

    $info = Get-CollectionInfo -RepoPath $repoPath -RepoName $repoName
    $scriptsDir = Join-Path $repoPath 'scripts'

    if (-not (Test-Path $scriptsDir)) {
        New-Item -ItemType Directory -Path $scriptsDir | Out-Null
        Write-Host "  Created scripts/ directory for $repoName"
    }

    # --- build.cmd ---
    $buildCmd = Join-Path $scriptsDir 'build.cmd'
    if (-not (Test-Path $buildCmd)) {
        New-BuildCmd -Path $buildCmd -RepoName $repoName
        Write-Host "  Created: $repoName/scripts/build.cmd" -ForegroundColor Green
        $created++
    } else { $skippedExisting++ }

    # --- build.ps1 ---
    $buildPs1 = Join-Path $scriptsDir 'build.ps1'
    if (-not (Test-Path $buildPs1)) {
        New-BuildPs1Placeholder -Path $buildPs1 -RepoName $repoName `
            -CollectionName $info.CollectionName `
            -CollectionPath $info.CollectionPath `
            -DeploymentRoot $info.DeploymentRoot `
            -HasUi $info.HasUi
        Write-Host "  Created: $repoName/scripts/build.ps1 (placeholder)" -ForegroundColor Green
        $created++
    } else { $skippedExisting++ }

    # --- build-deploy-local.cmd ---
    $bdlCmd = Join-Path $scriptsDir 'build-deploy-local.cmd'
    if (-not (Test-Path $bdlCmd)) {
        New-BuildDeployLocalCmd -Path $bdlCmd
        Write-Host "  Created: $repoName/scripts/build-deploy-local.cmd" -ForegroundColor Green
        $created++
    } else { $skippedExisting++ }

    # --- build-deploy-remote.cmd ---
    $bdrCmd = Join-Path $scriptsDir 'build-deploy-remote.cmd'
    if (-not (Test-Path $bdrCmd)) {
        New-BuildDeployRemoteCmd -Path $bdrCmd
        Write-Host "  Created: $repoName/scripts/build-deploy-remote.cmd" -ForegroundColor Green
        $created++
    } else { $skippedExisting++ }

    # --- deploy-to-addon-library.ps1 ---
    $dalPs1 = Join-Path $scriptsDir 'deploy-to-addon-library.ps1'
    if (-not (Test-Path $dalPs1)) {
        New-DeployToAddonLibraryPs1 -Path $dalPs1
        Write-Host "  Created: $repoName/scripts/deploy-to-addon-library.ps1" -ForegroundColor Green
        $created++
    } else { $skippedExisting++ }

    # --- deploy-to-addon-library.cmd ---
    $dalCmd = Join-Path $scriptsDir 'deploy-to-addon-library.cmd'
    if (-not (Test-Path $dalCmd)) {
        New-DeployToAddonLibraryCmd -Path $dalCmd `
            -CollectionName $info.CollectionName `
            -CollectionPath $info.CollectionPath `
            -DeploymentPath $info.DeploymentRoot `
            -HasUi $info.HasUi
        Write-Host "  Created: $repoName/scripts/deploy-to-addon-library.cmd" -ForegroundColor Green
        $created++
    } else { $skippedExisting++ }
}

Write-Host ""
Write-Host "========================================"
Write-Host "Stub generation complete"
Write-Host "  Created: $created files"
Write-Host "  Skipped: $skippedExisting (already exist)"
Write-Host "========================================"
