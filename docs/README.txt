================================================================================
CONTENSIVE 5 - POST-INSTALLATION SETUP
================================================================================

Thank you for installing Contensive 5. Complete these steps to finish setup.

The install script (install.cmd) has already:
  - Copied CLI files to C:\Program Files\Contensive\Cli\
  - Added cc.exe to the system PATH
  - Copied TaskService files to C:\Program Files\Contensive\TaskService\
  - Registered and started the "Contensive Task Service" Windows service
  - Copied WebApi package to C:\Program Files\Contensive\WebApi\

--------------------------------------------------------------------------------
1. CONFIGURE THE SERVER
--------------------------------------------------------------------------------

Open a NEW command prompt as Administrator (so PATH updates take effect):

  cc --configure

Follow the prompts to set up database connection, AWS credentials, and
other server-level settings.

--------------------------------------------------------------------------------
2. CREATE AN APPLICATION
--------------------------------------------------------------------------------

For a new .NET Core WebApi site (recommended):

  cc -n appName domainName

For a legacy .NET Framework ASPX site:

  cc -nf appName domainName

This creates:
  - A SQL Server database for the application
  - Local file storage folders (www, files, private, temp)
  - An IIS site with the appropriate binaries deployed

--------------------------------------------------------------------------------
3. SETUP SCHEDULED TASK (Server Diagnostics)
--------------------------------------------------------------------------------

Run daily server diagnostics (Windows updates, domain bindings) for ALL
applications on the server with elevated permissions via Windows Task Scheduler.

POWERSHELL INSTALLATION (Recommended):
  1. Open PowerShell as Administrator
  2. Run the following:

  $ccExePath = "C:\Program Files\Contensive\Cli\cc.exe"
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
  2. Run: cc --serverdiagnostic
  3. Check output shows results for each application

VERIFICATION:
  - Open Task Scheduler (taskschd.msc)
  - Verify "Contensive Server Diagnostics" task exists
  - Right-click task -> Run to test manually
  - Check "Last Run Result" should be 0x0 (success)

NOTE: One scheduled task handles ALL applications on the server.

--------------------------------------------------------------------------------
4. SETUP SCHEDULED TASK (Task Service Daily Restart)
--------------------------------------------------------------------------------

The Contensive Task Service may accumulate memory over time. A daily restart
releases resources and keeps the service running reliably.

FIND YOUR SERVICE NAME:
  The registered service name may vary by installation. Run this to find it:

  Get-Service *contensive*, *taskservice* | Format-Table Name, DisplayName, Status

  Use the value from the "Name" column below. Common names are
  "Contensive Task Service" or "TaskService".

POWERSHELL INSTALLATION (Recommended):
  1. Open PowerShell as Administrator
  2. Set the service name variable to match your installation:

  $serviceName = "Contensive Task Service"

  3. Run the following to create the stop task:

  $stopAction = New-ScheduledTaskAction `
      -Execute "sc.exe" `
      -Argument "stop `"$serviceName`""

  $stopTrigger = New-ScheduledTaskTrigger -Daily -At 3:00AM

  $stopPrincipal = New-ScheduledTaskPrincipal `
      -UserId "SYSTEM" `
      -LogonType ServiceAccount `
      -RunLevel Highest

  Register-ScheduledTask `
      -TaskName "Contensive Task Service - Daily Stop" `
      -Action $stopAction `
      -Trigger $stopTrigger `
      -Principal $stopPrincipal `
      -Description "Stops the Contensive Task Service for daily restart"

  4. Run the following to create the start task (1-minute delay for graceful
     shutdown):

  $startAction = New-ScheduledTaskAction `
      -Execute "sc.exe" `
      -Argument "start `"$serviceName`""

  $startTrigger = New-ScheduledTaskTrigger -Daily -At 3:01AM

  $startPrincipal = New-ScheduledTaskPrincipal `
      -UserId "SYSTEM" `
      -LogonType ServiceAccount `
      -RunLevel Highest

  Register-ScheduledTask `
      -TaskName "Contensive Task Service - Daily Start" `
      -Action $startAction `
      -Trigger $startTrigger `
      -Principal $startPrincipal `
      -Description "Starts the Contensive Task Service after daily restart"

  5. Configure the service to auto-restart on unexpected failures:

  sc.exe failure $serviceName reset=86400 actions=restart/60000/restart/60000/restart/60000

  This restarts the service after 60 seconds on failure (up to 3 times),
  then resets the failure count after 24 hours.

VERIFICATION:
  - Open Task Scheduler (taskschd.msc)
  - Verify both "Contensive Task Service - Daily Stop" and
    "Contensive Task Service - Daily Start" tasks exist
  - Right-click each task -> Run to test manually
  - Confirm the service restarts: sc query $serviceName

REMOVAL:
  To remove the scheduled tasks later, run in PowerShell as Administrator:

  Unregister-ScheduledTask -TaskName "Contensive Task Service - Daily Stop" -Confirm:$false
  Unregister-ScheduledTask -TaskName "Contensive Task Service - Daily Start" -Confirm:$false

NOTE: Adjust the 3:00 AM time to a low-traffic window appropriate for your
environment. The server diagnostics task (section 3) runs at 2:00 AM by
default, so stagger accordingly.

--------------------------------------------------------------------------------
5. VERIFY INSTALLATION
--------------------------------------------------------------------------------

  cc --version                       Show version
  cc --status                        Display server and app status
  cc -a appName --status             Check a specific application

--------------------------------------------------------------------------------
6. UPGRADE EXISTING INSTALLATION
--------------------------------------------------------------------------------

To upgrade an existing server to a new build:

  1. Run uninstall.cmd as Administrator
  2. Run install.cmd as Administrator
  3. Open a new command prompt and upgrade all apps:
       cc -u
     Wait for this to complete before proceeding.
  4. Upgrade each IIS site:
     - Core WebApi apps: copy new WebApi files from
         C:\Program Files\Contensive\WebApi\
       into the site's physical path, then recycle the app pool.
     - Framework ASPX apps: in IIS Manager, click the site,
       click "Import Application", and select:
         C:\Program Files\Contensive\defaultaspxsite.zip
  5. Verify the Task Service is running:
       sc query "Contensive Task Service"

--------------------------------------------------------------------------------
7. UNINSTALL
--------------------------------------------------------------------------------

Run uninstall.cmd as Administrator. This will:
  - Stop and remove the "Contensive Task Service"
  - Remove cc.exe from the system PATH
  - Delete all files from C:\Program Files\Contensive\

Individual app IIS sites, databases, and file folders are NOT removed.

--------------------------------------------------------------------------------
DOCUMENTATION
--------------------------------------------------------------------------------

  - Setup & Deployment:   docs\setup-and-deployment.md
  - Migration to Core:    docs\migration-to-core.md
  - Contensive Docs:      https://contensive.com/docs

SUPPORT:
  - GitHub: https://github.com/contensive/contensive5
  - Email:  support@contensive.com

================================================================================
