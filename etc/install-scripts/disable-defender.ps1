#Requires -RunAsAdministrator
# disable-defender.ps1
# Disables Microsoft Defender real-time protection and associated services
# on Windows Server 2025. Run as Administrator.

Write-Host "Disabling Microsoft Defender...`n"

# -- Disable real-time monitoring via PowerShell cmdlet --
try {
    Set-MpPreference -DisableRealtimeMonitoring $true
    Write-Host "  OK    Real-time monitoring disabled" -ForegroundColor Green
} catch {
    Write-Host "  FAIL  Real-time monitoring - $($_.Exception.Message)" -ForegroundColor Red
}

# -- Disable Defender services --
$services = @(
    "WinDefend",  # Microsoft Defender Antivirus Service
    "WdNisSvc"    # Microsoft Defender Network Inspection Service
)

foreach ($name in $services) {
    $svc = Get-Service -Name $name -ErrorAction SilentlyContinue
    if (-not $svc) {
        Write-Host "  SKIP  $name (not found)" -ForegroundColor DarkGray
        continue
    }
    try {
        Set-Service -Name $name -StartupType Disabled -ErrorAction Stop
        if ($svc.Status -eq "Running") {
            Stop-Service -Name $name -Force -ErrorAction Stop
        }
        Write-Host "  OK    $name ($($svc.DisplayName)) disabled" -ForegroundColor Green
    } catch {
        Write-Host "  FAIL  $name - $($_.Exception.Message)" -ForegroundColor Red
    }
}

# -- Disable via registry (survives Group Policy refresh) --
$regPath = "HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender"
try {
    if (-not (Test-Path $regPath)) {
        New-Item -Path $regPath -Force | Out-Null
    }
    Set-ItemProperty -Path $regPath -Name "DisableAntiSpyware" -Value 1 -Type DWord -Force
    Set-ItemProperty -Path $regPath -Name "DisableAntiVirus" -Value 1 -Type DWord -Force

    $realtimePath = "$regPath\Real-Time Protection"
    if (-not (Test-Path $realtimePath)) {
        New-Item -Path $realtimePath -Force | Out-Null
    }
    Set-ItemProperty -Path $realtimePath -Name "DisableRealtimeMonitoring" -Value 1 -Type DWord -Force

    Write-Host "  OK    Registry policies set" -ForegroundColor Green
} catch {
    Write-Host "  FAIL  Registry - $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nDone. Reboot the server for all changes to take full effect."
Write-Host "Use enable-defender.ps1 to reverse these changes."
