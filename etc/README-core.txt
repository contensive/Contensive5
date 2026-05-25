============================================================
Contensive .NET 9.0 Core - Deployment Guide
============================================================

This package contains the Contensive .NET 9.0 server components:
  - CLI (cc.exe) - command-line tool for server and app management
  - TaskService  - Windows service for background task processing
  - WebApi       - ASP.NET Core website package deployed per-app

============================================================
PREREQUISITES
============================================================

1. Windows Server 2019 or later (or Windows 10/11 for development)

2. .NET 9.0 Hosting Bundle
   Download from: https://dotnet.microsoft.com/download/dotnet/9.0
   Get the "Hosting Bundle" under the Windows section.
   After installing, restart IIS: iisreset

3. IIS with ASP.NET Core Module
   Verify with: %windir%\system32\inetsrv\appcmd list modules | findstr AspNetCoreModuleV2

4. SQL Server (local or remote) for application databases

============================================================
NEW MACHINE INSTALL
============================================================

1. Extract this package to a temporary folder on the server.

2. Run the install script (requires Administrator):

   Double-click install.cmd
     -- or --
   Open an elevated command prompt and run:
     install.cmd

   This is equivalent to running:
     powershell -ExecutionPolicy Bypass -File install.ps1

3. The installer will:
   - Copy CLI files to C:\Program Files\Contensive\Cli\
   - Add cc.exe to the system PATH
   - Copy TaskService files to C:\Program Files\Contensive\TaskService\
   - Register and start the "Contensive Task Service" Windows service
   - Copy WebApi package to C:\Program Files\Contensive\WebApi\

4. Open a NEW command prompt (so PATH updates take effect) and configure:
     cc --configure

5. Create your first application:
     cc -n appName domainName

   This creates the SQL database, local file storage, and an IIS site
   with the WebApi deployed into it.

Install options (PowerShell):
  .\install.ps1 -InstallPath "E:\Contensive"    Custom install path
  .\install.ps1 -SkipWebApi -SkipTaskService     Install only the CLI
  .\install.ps1 -SkipWebApi -SkipCli             Install only TaskService

============================================================
UPGRADE AN EXISTING INSTALLATION
============================================================

1. Extract the new package to a temporary folder on the server.

2. Run the uninstall script first:

   Double-click uninstall.cmd
     -- or --
   Open an elevated command prompt and run:
     uninstall.cmd

   This stops the Task Service, removes it, removes the CLI from PATH,
   and deletes the installed files. Individual app IIS sites, databases,
   and file storage are NOT affected.

3. Run the install script:

   Double-click install.cmd

4. Update each application's WebApi files. For each app, either:
   - Use the CLI: cc -a appName --upgrade
   - Or manually copy the WebApi files from C:\Program Files\Contensive\WebApi\
     to the app's IIS site physical path

5. Start the Task Service if not auto-started:
     sc start "Contensive Task Service"

============================================================
UNINSTALL
============================================================

1. Run the uninstall script (requires Administrator):

   Double-click uninstall.cmd
     -- or --
   Open an elevated command prompt and run:
     uninstall.cmd

   This is equivalent to running:
     powershell -ExecutionPolicy Bypass -File uninstall.ps1

2. The uninstaller will:
   - Stop and remove the "Contensive Task Service" Windows service
   - Remove cc.exe from the system PATH
   - Delete installed files from C:\Program Files\Contensive\

3. Individual app IIS sites, databases, and file folders are NOT removed.
   To remove an application completely:
   - Delete the IIS site from IIS Manager
   - Delete the SQL Server database
   - Delete the app's file storage folder

Uninstall options (PowerShell):
  .\uninstall.ps1 -KeepFiles              Remove services but keep files
  .\uninstall.ps1 -SkipTaskService         Keep TaskService, remove the rest
  .\uninstall.ps1 -InstallPath "E:\Cont"   Custom install path

============================================================
CLI QUICK REFERENCE
============================================================

  cc --help                          Show all commands
  cc --version                       Show version
  cc --configure                     Server configuration wizard
  cc --status                        Display server and app status
  cc -n appName domainName           Create a new application
  cc -a appName --upgrade            Upgrade a specific application
  cc -a appName --install Collection Install a collection from the library
  cc -a appName --tasks run          Run task scheduler in console mode
  cc --serverdiagnostic              Run server diagnostics

============================================================
TROUBLESHOOTING
============================================================

- "ASP.NET Core Module not found": Install the .NET 9.0 Hosting Bundle
  and run iisreset.

- "Access denied": Run install.cmd or uninstall.cmd as Administrator.
  Right-click and select "Run as administrator".

- Task Service won't start: Check the Windows Event Log under
  Application for errors. Verify cc --configure has been run.

- IIS site returns 500: Check the stdout log at the app's IIS site
  folder under logs\stdout. Verify the app pool is set to
  "No Managed Code" for the CLR version.

- cc.exe not found after install: Open a NEW command prompt so the
  updated PATH takes effect.

============================================================
