#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Full Contensive server upgrade: components, databases, and IIS sites.

.DESCRIPTION
    This script performs a complete server upgrade in one step:
      1. Upgrade server components (CLI, TaskService, WebApi package) via upgrade.ps1
      2. Upgrade all application databases and collections via cc -u
      3. Deploy binaries to every IIS site:
         - Core WebApi apps: copy WebApi files from the install package
         - Framework ASPX apps: import defaultaspxsite.zip via Web Deploy (msdeploy)

    After this script completes, all applications should be running
    the new version with no additional manual steps required.

    Run from the extracted deployment package folder, or specify -SourcePath.

.PARAMETER SourcePath
    Path to the deployment folder containing WebApi, Cli, and TaskService subfolders.
    Defaults to the folder containing this script.

.PARAMETER InstallPath
    Base installation path for program binaries. Defaults to C:\Program Files\Contensive.

.PARAMETER ServiceName
    Windows service name. Defaults to "Contensive Task Service".

.EXAMPLE
    .\upgrade-full.ps1

.EXAMPLE
    .\upgrade-full.ps1 -InstallPath "D:\Contensive"
#>

param(
    [string]$SourcePath = $PSScriptRoot,
    [string]$InstallPath = "$env:ProgramFiles\Contensive",
    [string]$ServiceName = "Contensive Task Service"
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host "Contensive Full Upgrade" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host "Source:  $SourcePath"
Write-Host "Target:  $InstallPath"
Write-Host ""

# ============================================================
# Step 1: Upgrade server components (uninstall + install)
# ============================================================

Write-Host ""
Write-Host "[1/3] Upgrading server components..." -ForegroundColor Cyan
Write-Host ""

$upgradeScript = Join-Path $SourcePath "upgrade.ps1"
if (-not (Test-Path $upgradeScript)) {
    Write-Host "ERROR: upgrade.ps1 not found at $upgradeScript" -ForegroundColor Red
    exit 1
}
& $upgradeScript -SourcePath $SourcePath -InstallPath $InstallPath -ServiceName $ServiceName

# ============================================================
# Step 2: Upgrade all databases (cc -u)
# ============================================================

Write-Host ""
Write-Host "[2/3] Upgrading all application databases (cc -u)..." -ForegroundColor Cyan
Write-Host ""

# Refresh PATH in this session so cc.exe is available after install
$machinePath = [Environment]::GetEnvironmentVariable("PATH", "Machine")
$userPath = [Environment]::GetEnvironmentVariable("PATH", "User")
$env:PATH = "$machinePath;$userPath"

$ccExe = Join-Path $InstallPath "Cli\cc.exe"
if (-not (Test-Path $ccExe)) {
    Write-Host "ERROR: cc.exe not found at $ccExe" -ForegroundColor Red
    Write-Host "The server component upgrade may have failed." -ForegroundColor Red
    exit 1
}

Write-Host "Running: $ccExe -u"
Write-Host ""
& $ccExe -u
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "ERROR: cc -u failed with exit code $LASTEXITCODE" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Database upgrade complete." -ForegroundColor Green

# ============================================================
# Step 3: Deploy binaries to all IIS sites
# ============================================================

Write-Host ""
Write-Host "[3/3] Deploying binaries to IIS sites..." -ForegroundColor Cyan
Write-Host ""

# Locate source packages
$webApiSource = Join-Path $InstallPath "WebApi"
$frameworkZipPath = Join-Path $InstallPath "FrameworkSite\defaultaspxsite.zip"

$hasWebApi = Test-Path $webApiSource
$hasFrameworkZip = Test-Path $frameworkZipPath

if (-not $hasWebApi) {
    Write-Host "NOTE: WebApi package not found at $webApiSource" -ForegroundColor Yellow
}
if (-not $hasFrameworkZip) {
    Write-Host "NOTE: Framework package not found at $frameworkZipPath" -ForegroundColor Yellow
}

# Locate msdeploy.exe for Framework site deployments
$msdeployExe = "C:\Program Files\IIS\Microsoft Web Deploy V3\msdeploy.exe"
$hasMsdeploy = Test-Path $msdeployExe
if ($hasFrameworkZip -and -not $hasMsdeploy) {
    Write-Host "WARNING: Web Deploy not found at $msdeployExe" -ForegroundColor Yellow
    Write-Host "Framework ASPX sites cannot be upgraded without Web Deploy." -ForegroundColor Yellow
    Write-Host "Install from: https://www.iis.net/downloads/microsoft/web-deploy" -ForegroundColor Yellow
    Write-Host ""
}

# Locate config.json - same search order as Contensive Processor
$configPath = $null
$searchPaths = @()

# Prefer D:\Contensive
$searchPaths += "D:\Contensive\config.json"

# Check each fixed drive for {drive}\Contensive\config.json
foreach ($drive in [System.IO.DriveInfo]::GetDrives()) {
    if ($drive.IsReady -and $drive.DriveType -eq [System.IO.DriveType]::Fixed) {
        $candidate = Join-Path $drive.Name "Contensive\config.json"
        if ($searchPaths -notcontains $candidate) {
            $searchPaths += $candidate
        }
    }
}

# Legacy location
$searchPaths += Join-Path $env:ProgramData "Contensive\config.json"

foreach ($candidate in $searchPaths) {
    if (Test-Path $candidate) {
        $configPath = $candidate
        break
    }
}

if (-not $configPath) {
    Write-Host "WARNING: Contensive config.json not found. Skipping IIS site deployment." -ForegroundColor Yellow
    Write-Host "Searched:" -ForegroundColor Yellow
    foreach ($p in $searchPaths) {
        Write-Host "  $p" -ForegroundColor Yellow
    }
    Write-Host "You will need to manually deploy to each IIS site." -ForegroundColor Yellow
}
else {
    Write-Host "Config: $configPath"
    Write-Host ""

    $config = Get-Content $configPath -Raw | ConvertFrom-Json
    $apps = $config.apps

    if (-not $apps -or ($apps.PSObject.Properties | Measure-Object).Count -eq 0) {
        Write-Host "No applications found in config.json." -ForegroundColor Yellow
    }
    else {
        # Import IIS module if available (for app pool recycling on Core sites)
        $iisModuleAvailable = $false
        try {
            Import-Module WebAdministration -ErrorAction Stop
            $iisModuleAvailable = $true
        }
        catch {
            Write-Host "NOTE: WebAdministration module not available. Core app pools will not be recycled automatically." -ForegroundColor Yellow
            Write-Host ""
        }

        $successCount = 0
        $skipCount = 0
        $failCount = 0

        foreach ($prop in $apps.PSObject.Properties) {
            $appName = $prop.Name
            $app = $prop.Value

            # Skip disabled apps
            if ($app.enabled -eq $false) {
                Write-Host "  [$appName] Skipped (disabled)" -ForegroundColor DarkGray
                $skipCount++
                continue
            }

            # Determine the target path (effectiveAppPath logic)
            $targetPath = $app.localAppPath
            if ([string]::IsNullOrEmpty($targetPath)) {
                $targetPath = $app.localWwwPath
            }

            if ([string]::IsNullOrEmpty($targetPath)) {
                Write-Host "  [$appName] Skipped (no app path configured)" -ForegroundColor Yellow
                $skipCount++
                continue
            }

            if (-not (Test-Path $targetPath)) {
                Write-Host "  [$appName] Skipped (path does not exist: $targetPath)" -ForegroundColor Yellow
                $skipCount++
                continue
            }

            # Detect Framework vs Core: Core sites have WebApi.dll or WebApi.exe in the app path
            # Default to Framework since that's the current standard deployment
            $isCore = (Test-Path (Join-Path $targetPath "WebApi.dll")) -or (Test-Path (Join-Path $targetPath "WebApi.exe"))
            $isFramework = -not $isCore

            try {
                if ($isFramework) {
                    # ---- Framework ASPX site: deploy via Web Deploy (msdeploy) ----
                    if (-not $hasFrameworkZip) {
                        Write-Host "  [$appName] Skipped (Framework site, no defaultaspxsite.zip available)" -ForegroundColor Yellow
                        $skipCount++
                        continue
                    }
                    if (-not $hasMsdeploy) {
                        Write-Host "  [$appName] Skipped (Framework site, Web Deploy not installed)" -ForegroundColor Yellow
                        $skipCount++
                        continue
                    }

                    # msdeploy imports the Web Deploy package to the IIS site.
                    # -verb:sync updates files in the package without deleting extra files
                    # on the destination (like WebAppSettings.config and WebRewrite.config).
                    # -skip rules preserve user-modified config files that are also in the package.
                    Write-Host "  [$appName] Framework site - importing via Web Deploy"
                    $msdeployArgs = @(
                        "-verb:sync",
                        "-source:package=$frameworkZipPath",
                        "-dest:iisApp=$appName",
                        "-skip:objectName=filePath,absolutePath=WebAppSettings\.config$",
                        "-skip:objectName=filePath,absolutePath=WebRewrite\.config$"
                    )
                    $msdeployOutput = & $msdeployExe @msdeployArgs 2>&1
                    if ($LASTEXITCODE -ne 0) {
                        Write-Host "  [$appName] Web Deploy output:" -ForegroundColor Red
                        $msdeployOutput | ForEach-Object { Write-Host "    $_" -ForegroundColor Red }
                        throw "msdeploy exited with code $LASTEXITCODE"
                    }
                }
                else {
                    # ---- Core WebApi site: copy WebApi files ----
                    if (-not $hasWebApi) {
                        Write-Host "  [$appName] Skipped (Core site, no WebApi package available)" -ForegroundColor Yellow
                        $skipCount++
                        continue
                    }

                    # Stop the app pool to release file locks
                    if ($iisModuleAvailable) {
                        try {
                            $pool = Get-Item "IIS:\AppPools\$appName" -ErrorAction SilentlyContinue
                            if ($pool -and $pool.State -eq "Started") {
                                Write-Host "  [$appName] Stopping app pool..."
                                Stop-WebAppPool -Name $appName
                                $retries = 0
                                while ((Get-WebAppPoolState -Name $appName).Value -ne "Stopped" -and $retries -lt 15) {
                                    Start-Sleep -Seconds 1
                                    $retries++
                                }
                            }
                        }
                        catch {
                            Write-Host "  [$appName] Could not stop app pool: $_" -ForegroundColor Yellow
                        }
                    }

                    Write-Host "  [$appName] Core site - deploying WebApi to $targetPath"
                    robocopy $webApiSource $targetPath /E /NJH /NJS /NDL /NP | Out-Null

                    # Ensure logs folder exists
                    $logsFolder = Join-Path $targetPath "logs"
                    if (-not (Test-Path $logsFolder)) {
                        New-Item -Path $logsFolder -ItemType Directory -Force | Out-Null
                    }

                    # Start the app pool back up
                    if ($iisModuleAvailable) {
                        try {
                            $pool = Get-Item "IIS:\AppPools\$appName" -ErrorAction SilentlyContinue
                            if ($pool -and $pool.State -eq "Stopped") {
                                Start-WebAppPool -Name $appName
                                Write-Host "  [$appName] App pool started" -ForegroundColor Green
                            }
                        }
                        catch {
                            Write-Host "  [$appName] Could not start app pool: $_" -ForegroundColor Yellow
                        }
                    }
                }

                Write-Host "  [$appName] Done" -ForegroundColor Green
                $successCount++
            }
            catch {
                Write-Host "  [$appName] FAILED: $_" -ForegroundColor Red
                $failCount++

                # Try to restart the app pool if we stopped it for a Core site
                if (-not $isFramework -and $iisModuleAvailable) {
                    try {
                        $pool = Get-Item "IIS:\AppPools\$appName" -ErrorAction SilentlyContinue
                        if ($pool -and $pool.State -eq "Stopped") {
                            Start-WebAppPool -Name $appName
                        }
                    }
                    catch { }
                }
            }
        }

        Write-Host ""
        Write-Host "IIS deployment summary: $successCount deployed, $skipCount skipped, $failCount failed" -ForegroundColor $(if ($failCount -gt 0) { "Yellow" } else { "Green" })
    }
}

# ============================================================
# Done
# ============================================================

Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host "Full upgrade complete" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host ""
