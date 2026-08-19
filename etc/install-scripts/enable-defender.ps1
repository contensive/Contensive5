#Requires -RunAsAdministrator
# enable-defender.ps1
# Re-enables Microsoft Defender real-time protection and associated services
# on Windows Server 2025. Run as Administrator.

Write-Host "Enabling Microsoft Defender...`n"

# -- Remove registry policy overrides first (must happen before services start) --
$regPath = "HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender"
try {
    if (Test-Path $regPath) {
        Remove-ItemProperty -Path $regPath -Name "DisableAntiSpyware" -Force -ErrorAction SilentlyContinue
        Remove-ItemProperty -Path $regPath -Name "DisableAntiVirus" -Force -ErrorAction SilentlyContinue
    }
    $realtimePath = "$regPath\Real-Time Protection"
    if (Test-Path $realtimePath) {
        Remove-ItemProperty -Path $realtimePath -Name "DisableRealtimeMonitoring" -Force -ErrorAction SilentlyContinue
    }
    Write-Host "  OK    Registry policies removed" -ForegroundColor Green
} catch {
    Write-Host "  FAIL  Registry - $($_.Exception.Message)" -ForegroundColor Red
}

# -- Re-enable Defender services --
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
        Set-Service -Name $name -StartupType Automatic -ErrorAction Stop
        Start-Service -Name $name -ErrorAction Stop
        Write-Host "  OK    $name ($($svc.DisplayName)) enabled and started" -ForegroundColor Green
    } catch {
        Write-Host "  FAIL  $name - $($_.Exception.Message)" -ForegroundColor Red
    }
}

# -- Re-enable real-time monitoring --
try {
    Set-MpPreference -DisableRealtimeMonitoring $false
    Write-Host "  OK    Real-time monitoring enabled" -ForegroundColor Green
} catch {
    Write-Host "  FAIL  Real-time monitoring - $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nDone. Reboot the server to ensure Defender is fully operational."
Write-Host "Run 'Get-MpComputerStatus | Select-Object RealTimeProtectionEnabled' to verify."
