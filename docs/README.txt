================================================================================
CONTENSIVE 5 - POST-INSTALLATION SETUP
================================================================================

Thank you for installing Contensive 5. Complete these steps to finish setup:

--------------------------------------------------------------------------------
1. INSTALL TASK SERVICE (Windows Service)
--------------------------------------------------------------------------------

The Contensive Task Service runs scheduled tasks and background processes.

INSTALLATION STEPS:
  1. Open Command Prompt as Administrator
  2. Navigate to installation folder:
     cd "C:\Program Files\Contensive"

  3. Install the service using .NET Framework InstallUtil:
     C:\Windows\Microsoft.NET\Framework64\v4.0.30319\installutil.exe TaskService.exe

  4. Start the service:
     net start TaskService

     OR use Services console (services.msc):
     - Find "Contensive Task Service"
     - Right-click -> Start

VERIFICATION:
  • Open Services (services.msc)
  • Verify "Contensive Task Service" is Running
  • Set Startup Type to "Automatic" for auto-start on boot

UNINSTALLATION:
  1. Stop the service:
     net stop TaskService

  2. Uninstall the service:
     C:\Windows\Microsoft.NET\Framework64\v4.0.30319\installutil.exe /u TaskService.exe

TROUBLESHOOTING:
  • Service runs as LocalService account (limited permissions)
  • Logs are stored in: C:\ProgramData\Contensive\[AppName]\Logs\
  • If InstallUtil is not found, verify .NET Framework 4.x is installed

--------------------------------------------------------------------------------
2. SETUP SCHEDULED TASK (Server Diagnostics)
--------------------------------------------------------------------------------

Run daily server diagnostics (Windows updates, domain bindings) for ALL
applications on the server with elevated permissions via Windows Task Scheduler.

This single task checks Windows updates once and validates domain bindings for
each application, saving results to each application's site properties.

POWERSHELL INSTALLATION (Recommended):
  1. Open PowerShell as Administrator
  2. Run the following:

  $ccExePath = "C:\Program Files\Contensive\cc.exe"
  $taskName = "Contensive Server Diagnostics"

  $action = New-ScheduledTaskAction `
      -Execute $ccExePath `
      -Argument "--serverdiagnostic"

  $trigger = New-ScheduledTaskTrigger -Daily -At 2:00AM

  $principal = New-ScheduledTaskPrincipal `
      -UserId "SYSTEM" `
      -LogonType ServiceAccount `
      -RunLevel Highest

  $settings = New-ScheduledTaskSettingsSet `
      -AllowStartIfOnBatteries `
      -DontStopIfGoingOnBatteries `
      -StartWhenAvailable `
      -ExecutionTimeLimit (New-TimeSpan -Minutes 10)

  Register-ScheduledTask `
      -TaskName $taskName `
      -Action $action `
      -Trigger $trigger `
      -Principal $principal `
      -Settings $settings `
      -Description "Daily server diagnostics for all Contensive applications"

MANUAL TEST:
  1. Open Command Prompt as Administrator
  2. Run manually to verify:
     cc.exe --serverdiagnostic

  3. Check output shows results for each application:
     - Windows updates status (server-wide)
     - Domain bindings validation (per application)
     - Summary of successful/failed checks

VERIFICATION:
  • Open Task Scheduler (taskschd.msc)
  • Verify "Contensive Server Diagnostics" task exists
  • Right-click task -> Run to test manually
  • Check "Last Run Result" should be 0x0 (success)
  • View diagnostics for each app at: https://yoursite.com/status

NOTE: One scheduled task handles ALL applications on the server.

--------------------------------------------------------------------------------
3. ADDITIONAL CONFIGURATION
--------------------------------------------------------------------------------

CREATE CONTENSIVE APPLICATION:
  • Run: cc.exe --new "YourAppName"
  • Follow prompts to configure database and settings

VERIFY INSTALLATION:
  • Run: cc.exe --version
  • Run: cc.exe -a "YourAppName" --status

CHECK IIS BINDINGS:
  • Ensure application domains are registered in IIS
  • Configure SSL certificates as needed

--------------------------------------------------------------------------------
DOCUMENTATION
--------------------------------------------------------------------------------

Detailed documentation available at:
  • Server Diagnostics Setup: docs\server-diagnostics-monitoring-setup.md
  • Contensive Documentation: https://contensive.com/docs

SUPPORT:
  • GitHub: https://github.com/contensive/contensive5
  • Email: support@contensive.com

================================================================================
