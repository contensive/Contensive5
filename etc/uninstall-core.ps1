#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Uninstall Contensive .NET 9.0 server components (CLI, TaskService, WebApi package).

.DESCRIPTION
    Removes server-level Contensive components. Individual application IIS sites,
    databases, and file folders are NOT removed by this script.

.PARAMETER InstallPath
    Base installation path. Defaults to D:\Contensive.

.PARAMETER ServiceName
    Windows service name. Defaults to "Contensive Task Service".

.PARAMETER SkipWebApi
    Skip WebApi package removal.

.PARAMETER SkipTaskService
    Skip TaskService removal.

.PARAMETER SkipCli
    Skip CLI removal.

.PARAMETER KeepFiles
    Remove services but keep the installed files.

.EXAMPLE
    .\uninstall.ps1

.EXAMPLE
    .\uninstall.ps1 -SkipCli -KeepFiles
#>

param(
    [string]$InstallPath = "$env:ProgramFiles\Contensive",
    [string]$ServiceName = "Contensive Task Service",
    [switch]$SkipWebApi,
    [switch]$SkipTaskService,
    [switch]$SkipCli,
    [switch]$KeepFiles
)

$ErrorActionPreference = "Stop"

function Write-Step($message) {
    Write-Host ""
    Write-Host "== $message" -ForegroundColor Cyan
}

function Uninstall-WebApiPackage {
    Write-Step "Removing WebApi package"

    if (-not $KeepFiles) {
        $webApiDest = Join-Path $InstallPath "WebApi"
        if (Test-Path $webApiDest) {
            Write-Host "  Deleting files: $webApiDest"
            Remove-Item -Path $webApiDest -Recurse -Force
        }
    }

    Write-Host "  WebApi package removed." -ForegroundColor Green
    Write-Host "  NOTE: Individual app IIS sites and their deployed WebApi files are not removed." -ForegroundColor Yellow
    Write-Host "  Remove app IIS sites manually if needed." -ForegroundColor Yellow
}

function Uninstall-TaskService {
    Write-Step "Removing TaskService"

    $svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($svc) {
        if ($svc.Status -eq "Running") {
            Write-Host "  Stopping service: $ServiceName"
            Stop-Service -Name $ServiceName -Force
            Start-Sleep -Seconds 2
        }
        Write-Host "  Removing service: $ServiceName"
        sc.exe delete $ServiceName | Out-Null
    }
    else {
        Write-Host "  Service '$ServiceName' not found, skipping."
    }

    if (-not $KeepFiles) {
        $taskDest = Join-Path $InstallPath "TaskService"
        if (Test-Path $taskDest) {
            Write-Host "  Deleting files: $taskDest"
            Remove-Item -Path $taskDest -Recurse -Force
        }
    }

    Write-Host "  TaskService removed." -ForegroundColor Green
}

function Uninstall-Cli {
    Write-Step "Removing CLI"

    $cliDest = Join-Path $InstallPath "Cli"

    # Remove from PATH
    $currentPath = [Environment]::GetEnvironmentVariable("PATH", "Machine")
    if ($currentPath -like "*$cliDest*") {
        Write-Host "  Removing CLI from system PATH"
        $newPath = ($currentPath.Split(";") | Where-Object { $_ -ne $cliDest }) -join ";"
        [Environment]::SetEnvironmentVariable("PATH", $newPath, "Machine")
    }

    if (-not $KeepFiles) {
        if (Test-Path $cliDest) {
            Write-Host "  Deleting files: $cliDest"
            Remove-Item -Path $cliDest -Recurse -Force
        }
    }

    Write-Host "  CLI removed." -ForegroundColor Green
}

function Uninstall-ScheduledTask {
    Write-Step "Removing scheduled task: Contensive Server Diagnostics"

    $taskName = "Contensive Server Diagnostics"
    try {
        $existing = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
        if ($existing) {
            Write-Host "  Removing scheduled task: $taskName"
            Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
            Write-Host "  Scheduled task removed." -ForegroundColor Green
        }
        else {
            Write-Host "  Scheduled task '$taskName' not found, skipping."
        }
    }
    catch {
        Write-Host "  Could not query scheduled tasks (CIM unavailable), skipping: $_" -ForegroundColor Yellow
    }
}

# ============================================================
# Main
# ============================================================

Write-Host "============================================================"
Write-Host "Contensive .NET 9.0 Uninstaller"
Write-Host "============================================================"
Write-Host "Install path: $InstallPath"
Write-Host ""

if (-not $SkipCli)         { Uninstall-ScheduledTask }
if (-not $SkipTaskService) { Uninstall-TaskService }
if (-not $SkipCli)         { Uninstall-Cli }
if (-not $SkipWebApi)      { Uninstall-WebApiPackage }

# Remove legacy ASPX deployment package if present
if (-not $KeepFiles) {
    $FrameworkSitePath = Join-Path $InstallPath "FrameworkSite"
    if (Test-Path $FrameworkSitePath) {
        Write-Host "  Removing FrameworkSite folder"
        Remove-Item -Path $FrameworkSitePath -Recurse -Force
    }
    # Remove legacy location (prior installs put it directly in InstallPath)
    $aspxZip = Join-Path $InstallPath "defaultaspxsite.zip"
    if (Test-Path $aspxZip) {
        Write-Host "  Removing defaultaspxsite.zip (legacy location)"
        Remove-Item -Path $aspxZip -Force
    }
}

Write-Step "Uninstall complete"
Write-Host ""
