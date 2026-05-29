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
4. VERIFY INSTALLATION
--------------------------------------------------------------------------------

  cc --version                       Show version
  cc --status                        Display server and app status
  cc -a appName --status             Check a specific application

--------------------------------------------------------------------------------
5. UPGRADE EXISTING INSTALLATION
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
6. UNINSTALL
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
