#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Upgrade Contensive .NET 9.0 server components (CLI, TaskService, WebApi package).

.DESCRIPTION
    This script upgrades the server-level Contensive components by running the
    uninstall and install scripts in sequence. Individual application IIS sites,
    databases, and file folders are NOT affected.

    After the upgrade completes, open a new command prompt and run:
      cc -u
    to upgrade all application databases and collections.

    Run from the extracted deployment package folder, or specify -SourcePath.

.PARAMETER SourcePath
    Path to the deployment folder containing WebApi, Cli, and TaskService subfolders.
    Defaults to the folder containing this script.

.PARAMETER InstallPath
    Base installation path for program binaries. Defaults to C:\Program Files\Contensive.

.PARAMETER ServiceName
    Windows service name. Defaults to "Contensive Task Service".

.EXAMPLE
    .\upgrade.ps1

.EXAMPLE
    .\upgrade.ps1 -InstallPath "D:\Contensive"
#>

param(
    [string]$SourcePath = $PSScriptRoot,
    [string]$InstallPath = "$env:ProgramFiles\Contensive",
    [string]$ServiceName = "Contensive Task Service"
)

$ErrorActionPreference = "Stop"

Write-Host "============================================================"
Write-Host "Contensive .NET 9.0 Upgrade"
Write-Host "============================================================"
Write-Host "Source:  $SourcePath"
Write-Host "Target:  $InstallPath"
Write-Host ""

# -- Step 1: Uninstall existing version
Write-Host ""
Write-Host "[1/2] Uninstalling existing version..." -ForegroundColor Cyan
Write-Host ""

$uninstallScript = Join-Path $SourcePath "uninstall.ps1"
if (-not (Test-Path $uninstallScript)) {
    Write-Host "ERROR: uninstall.ps1 not found at $uninstallScript" -ForegroundColor Red
    exit 1
}
& $uninstallScript -InstallPath $InstallPath -ServiceName $ServiceName

# -- Step 2: Install new version
Write-Host ""
Write-Host "[2/2] Installing new version..." -ForegroundColor Cyan
Write-Host ""

$installScript = Join-Path $SourcePath "install.ps1"
if (-not (Test-Path $installScript)) {
    Write-Host "ERROR: install.ps1 not found at $installScript" -ForegroundColor Red
    exit 1
}
& $installScript -SourcePath $SourcePath -InstallPath $InstallPath -ServiceName $ServiceName

Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host "Upgrade complete" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Open a NEW command prompt (so PATH updates take effect)" -ForegroundColor Yellow
Write-Host "  2. Run:  cc -u" -ForegroundColor Yellow
Write-Host "     This upgrades all application databases and collections." -ForegroundColor Yellow
Write-Host ""
