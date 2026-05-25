#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Install Contensive .NET 9.0 server components (CLI, TaskService, WebApi package).

.DESCRIPTION
    This script installs the server-level Contensive components to Program Files:
    - CLI (cc.exe) - command line tool added to system PATH
    - TaskService - Windows service for background task processing
    - WebApi package - template files deployed per-app by cc.exe -n

    Individual Contensive applications (IIS sites, databases, file folders) are
    created using the CLI: cc.exe -n appName domainName

    Run from the extracted deployment package folder, or specify -SourcePath.

.PARAMETER SourcePath
    Path to the deployment folder containing WebApi, Cli, and TaskService subfolders.
    Defaults to the folder containing this script.

.PARAMETER InstallPath
    Base installation path for program binaries. Defaults to C:\Program Files\Contensive.

.PARAMETER ServiceName
    Windows service name. Defaults to "Contensive Task Service".

.PARAMETER SkipWebApi
    Skip WebApi package installation.

.PARAMETER SkipTaskService
    Skip TaskService installation.

.PARAMETER SkipCli
    Skip CLI installation.

.EXAMPLE
    .\install.ps1

.EXAMPLE
    .\install.ps1 -InstallPath "D:\Contensive"
#>

param(
    [string]$SourcePath = $PSScriptRoot,
    [string]$InstallPath = "$env:ProgramFiles\Contensive",
    [string]$ServiceName = "Contensive Task Service",
    [switch]$SkipWebApi,
    [switch]$SkipTaskService,
    [switch]$SkipCli
)

$ErrorActionPreference = "Stop"

function Write-Step($message) {
    Write-Host ""
    Write-Host "== $message" -ForegroundColor Cyan
}

function Test-DotNetRuntime {
    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $dotnet) {
        Write-Host "ERROR: .NET runtime not found. Install the .NET 9.0 Hosting Bundle first." -ForegroundColor Red
        Write-Host "  https://dotnet.microsoft.com/download/dotnet/9.0" -ForegroundColor Yellow
        exit 1
    }
    $runtimes = dotnet --list-runtimes 2>&1
    if (-not ($runtimes | Where-Object { $_ -match "Microsoft\.AspNetCore\.App 9\." })) {
        Write-Host "WARNING: ASP.NET Core 9.0 runtime not found. WebApi IIS hosting requires the .NET 9.0 Hosting Bundle." -ForegroundColor Yellow
        Write-Host "  https://dotnet.microsoft.com/download/dotnet/9.0" -ForegroundColor Yellow
    }
}

function Test-ExistingInstallation {
    # Check if any Contensive components already exist at the install path
    $cliExists = Test-Path (Join-Path $InstallPath "Cli\cc.exe")
    $taskExists = Test-Path (Join-Path $InstallPath "TaskService\TaskService.exe")
    $webApiExists = Test-Path (Join-Path $InstallPath "WebApi\WebApi.dll")

    if ($cliExists -or $taskExists -or $webApiExists) {
        Write-Host ""
        Write-Host "An existing Contensive installation was found at $InstallPath" -ForegroundColor Yellow
        Write-Host ""
        if ($cliExists)  { Write-Host "  - CLI:         $(Join-Path $InstallPath 'Cli\cc.exe')" }
        if ($taskExists) { Write-Host "  - TaskService: $(Join-Path $InstallPath 'TaskService\TaskService.exe')" }
        if ($webApiExists) { Write-Host "  - WebApi:      $(Join-Path $InstallPath 'WebApi\WebApi.dll')" }
        Write-Host ""
        Write-Host "Please run uninstall.ps1 to remove the previous installation before installing." -ForegroundColor Yellow
        Write-Host "  Example: .\uninstall.ps1 -InstallPath `"$InstallPath`"" -ForegroundColor Yellow
        Write-Host ""
        exit 1
    }
}

function Install-WebApiPackage {
    Write-Step "Installing WebApi package"

    $webApiSource = Join-Path $SourcePath "WebApi"
    $webApiDest = Join-Path $InstallPath "WebApi"

    if (-not (Test-Path $webApiSource)) {
        Write-Host "  WebApi source folder not found at $webApiSource, skipping." -ForegroundColor Yellow
        return
    }

    # Copy WebApi files to install folder as the deployment package
    # cc.exe -n will copy these files into each app's IIS site folder
    Write-Host "  Copying WebApi package to $webApiDest"
    if (-not (Test-Path $webApiDest)) { New-Item -Path $webApiDest -ItemType Directory -Force | Out-Null }
    robocopy $webApiSource $webApiDest /MIR /NJH /NJS /NDL /NP | Out-Null

    Write-Host "  WebApi package installed at $webApiDest" -ForegroundColor Green
}

function Install-TaskService {
    Write-Step "Installing TaskService"

    $taskSource = Join-Path $SourcePath "TaskService"
    $taskDest = Join-Path $InstallPath "TaskService"

    if (-not (Test-Path $taskSource)) {
        Write-Host "  TaskService source folder not found at $taskSource, skipping." -ForegroundColor Yellow
        return
    }

    # Copy files
    Write-Host "  Copying files to $taskDest"
    if (-not (Test-Path $taskDest)) { New-Item -Path $taskDest -ItemType Directory -Force | Out-Null }
    robocopy $taskSource $taskDest /MIR /NJH /NJS /NDL /NP | Out-Null

    # Create and start service
    $svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if (-not $svc) {
        Write-Host "  Creating Windows service: $ServiceName"
        $exePath = Join-Path $taskDest "TaskService.exe"
        New-Service -Name $ServiceName -BinaryPathName $exePath -DisplayName $ServiceName -Description "Manages Contensive Task Scheduler, Task Runner, and MQTT services" -StartupType Automatic | Out-Null
    }

    Write-Host "  Starting service: $ServiceName"
    Start-Service -Name $ServiceName

    Write-Host "  TaskService installed at $taskDest" -ForegroundColor Green
}

function Install-Cli {
    Write-Step "Installing CLI"

    $cliSource = Join-Path $SourcePath "Cli"
    $cliDest = Join-Path $InstallPath "Cli"

    if (-not (Test-Path $cliSource)) {
        Write-Host "  CLI source folder not found at $cliSource, skipping." -ForegroundColor Yellow
        return
    }

    # Copy files
    Write-Host "  Copying files to $cliDest"
    if (-not (Test-Path $cliDest)) { New-Item -Path $cliDest -ItemType Directory -Force | Out-Null }
    robocopy $cliSource $cliDest /MIR /NJH /NJS /NDL /NP | Out-Null

    # Add to PATH if not already there
    $currentPath = [Environment]::GetEnvironmentVariable("PATH", "Machine")
    if ($currentPath -notlike "*$cliDest*") {
        Write-Host "  Adding CLI to system PATH"
        [Environment]::SetEnvironmentVariable("PATH", "$currentPath;$cliDest", "Machine")
    }

    Write-Host "  CLI installed at $cliDest" -ForegroundColor Green
    Write-Host "  Run 'cc --help' from a new command prompt" -ForegroundColor Green
}

# ============================================================
# Main
# ============================================================

Write-Host "============================================================"
Write-Host "Contensive .NET 9.0 Installer"
Write-Host "============================================================"
Write-Host "Source:  $SourcePath"
Write-Host "Target:  $InstallPath"
Write-Host ""

Test-DotNetRuntime
Test-ExistingInstallation

if (-not $SkipCli)         { Install-Cli }
if (-not $SkipTaskService) { Install-TaskService }
if (-not $SkipWebApi)      { Install-WebApiPackage }

Write-Step "Installation complete"

Write-Host ""
Write-Host "  To create a new Contensive application:" -ForegroundColor Green
Write-Host "    cc -n appName domainName" -ForegroundColor Green
Write-Host ""
