disable-server-services.ps1
============================

WHAT IT DOES
------------
Disables and stops non-essential Windows services on a dedicated Windows
Server 2025 application/web server. These are services that consume CPU,
memory, and disk I/O but are not needed on a headless server (printing,
audio, Bluetooth, Xbox, Wi-Fi, telemetry, search indexing, etc.).

Before making any changes, the script exports the current state of every
targeted service to a timestamped CSV backup file at:

    c:\tmp\service-state-backup-YYYYMMDD-HHMMSS.csv

Services that do not exist on the server are silently skipped.

WHAT IS NOT INCLUDED
--------------------
- Windows Defender (WinDefend, WdNisSvc) -- left enabled due to security
  implications. Disable separately only if you have alternative protection.
- Remote Desktop services -- assumed needed for server management.

HOW TO RUN
----------
1. Log in to the server as a local Administrator or domain admin.

2. Open PowerShell as Administrator:
   - Right-click the Start menu > "Windows Terminal (Admin)"
   - Or: search for PowerShell, right-click, "Run as administrator"

3. If script execution is restricted, allow it for this session:

       Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass

4. Run the script:

       c:\tmp\disable-server-services.ps1

5. Review the output. Each service will show:
   - OK    -- successfully disabled and stopped
   - SKIP  -- service not found on this server (normal)
   - FAIL  -- could not disable (check the error message)

6. Reboot the server to confirm everything starts cleanly without
   the disabled services.

HOW TO RESTORE SERVICES
------------------------
The script creates a backup CSV before making changes. To restore all
services to their previous state:

1. Open PowerShell as Administrator.

2. Find your backup file:

       dir c:\tmp\service-state-backup-*.csv

3. Run the restore commands (replace the filename with your actual backup):

       $backup = Import-Csv "c:\tmp\service-state-backup-20260819-143022.csv"
       foreach ($row in $backup) {
           Set-Service -Name $row.Name -StartupType $row.StartType -ErrorAction SilentlyContinue
           if ($row.Status -eq "Running") {
               Start-Service -Name $row.Name -ErrorAction SilentlyContinue
           }
       }

4. Reboot the server to ensure restored services start correctly.
