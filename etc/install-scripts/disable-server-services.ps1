#Requires -RunAsAdministrator
# disable-server-services.ps1
# Disables non-essential services on a dedicated Windows Server 2025 application/web server.
# Run as Administrator. Review the list before running — comment out any services you need.

$services = @(
    # -- Print & Fax --
    "Spooler",              # Print Spooler
    "Fax",                  # Fax

    # -- Audio & Multimedia --
    "Audiosrv",             # Windows Audio
    "AudioEndpointBuilder", # Windows Audio Endpoint Builder

    # -- User Experience --
    "TabletInputService",   # Touch Keyboard and Handwriting
    "WSearch",              # Windows Search (high disk I/O)
    "MapsBroker",           # Downloaded Maps Manager
    "lfsvc",                # Geolocation Service
    "RetailDemo",           # Retail Demo Service
    "WerSvc",               # Windows Error Reporting

    # -- Xbox / Consumer --
    "XblAuthManager",       # Xbox Live Auth Manager
    "XblGameSave",          # Xbox Live Game Save
    "XboxGipSvc",           # Xbox Accessory Management
    "XboxNetApiSvc",        # Xbox Live Networking

    # -- Network (unused on wired servers) --
    "WlanSvc",              # WLAN AutoConfig (Wi-Fi)
    "WwanSvc",              # WWAN AutoConfig (mobile broadband)
    "icssvc",               # Windows Mobile Hotspot
    "lmhosts",             # TCP/IP NetBIOS Helper (legacy)
    "NetTcpPortSharing",    # Net.Tcp Port Sharing

    # -- Remote Access (if unused) --
    "RemoteRegistry",       # Remote Registry
    "RemoteAccess",         # Routing and Remote Access

    # -- Bluetooth & Hardware --
    "bthserv",              # Bluetooth Support
    "SensrSvc",             # Sensor Monitoring
    "SensorService",        # Sensor Service

    # -- Telemetry & Diagnostics --
    "DiagTrack",            # Connected User Experiences and Telemetry
    "dmwappushservice",     # WAP Push Message Routing
    "SysMain",              # SysMain / Superfetch (high disk I/O)
    "DPS",                  # Diagnostic Policy Service
    "WdiServiceHost",       # Diagnostic Service Host
    "TrkWks",               # Distributed Link Tracking Client

    # -- Misc --
    "PhoneSvc",             # Phone Service
    "SEMgrSvc"              # Payments and NFC/SE Manager
)

# -- Export current state for rollback --
$backupFile = "c:\tmp\service-state-backup-$(Get-Date -Format 'yyyyMMdd-HHmmss').csv"
Write-Host "Backing up current service states to $backupFile"
$services | ForEach-Object {
    $svc = Get-Service -Name $_ -ErrorAction SilentlyContinue
    if ($svc) {
        [PSCustomObject]@{
            Name        = $svc.Name
            DisplayName = $svc.DisplayName
            StartType   = $svc.StartType
            Status      = $svc.Status
        }
    }
} | Export-Csv -Path $backupFile -NoTypeInformation
Write-Host "Backup saved.`n"

# -- Disable and stop each service --
$stopped = 0
$skipped = 0
foreach ($name in $services) {
    $svc = Get-Service -Name $name -ErrorAction SilentlyContinue
    if (-not $svc) {
        Write-Host "  SKIP  $name (not found)" -ForegroundColor DarkGray
        $skipped++
        continue
    }
    try {
        Set-Service -Name $name -StartupType Disabled -ErrorAction Stop
        if ($svc.Status -eq "Running") {
            Stop-Service -Name $name -Force -ErrorAction Stop
        }
        Write-Host "  OK    $name ($($svc.DisplayName))" -ForegroundColor Green
        $stopped++
    } catch {
        Write-Host "  FAIL  $name - $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`nDone. Disabled: $stopped, Skipped: $skipped"
Write-Host "To restore, use the backup CSV at: $backupFile"
