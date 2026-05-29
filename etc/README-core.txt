============================================================
Contensive Deployment Guide
============================================================

This package contains the Contensive server components:
  - CLI (cc.exe)    - command-line tool for server and app management
  - TaskService     - Windows service for background task processing
  - WebApi          - ASP.NET Core website package (for core apps)
  - defaultaspxsite - Web Deploy package (for framework ASPX apps)

All components are deployed to:
  C:\Program Files\Contensive\

============================================================
PREREQUISITES
============================================================

1. Windows Server 2019 or later (or Windows 10/11 for development)

2. .NET 9.0 Hosting Bundle
   Download: https://dotnet.microsoft.com/download/dotnet/9.0
   Get the "Hosting Bundle" under the Windows section.
   After installing, restart IIS: iisreset

3. IIS with ASP.NET Core Module (for core WebApi apps)
   Verify: %windir%\system32\inetsrv\appcmd list modules | findstr AspNetCoreModuleV2

4. SQL Server (local or remote) for application databases

============================================================
NEW INSTALL
============================================================

1. Extract contensive.zip to a temporary folder on the server.

2. Run the install script as Administrator:

     Right-click install.cmd -> "Run as administrator"
       -- or --
     From an elevated command prompt:
       install.cmd

3. The installer will:
   - Copy CLI files to C:\Program Files\Contensive\Cli\
   - Add cc.exe to the system PATH
   - Copy TaskService files to C:\Program Files\Contensive\TaskService\
   - Register and start the "Contensive Task Service" Windows service
   - Copy WebApi package to C:\Program Files\Contensive\WebApi\

4. Open a NEW command prompt (so PATH updates take effect) and run:
     cc --configure

5. Create your first application:

   For a .NET Core WebApi site:
     cc -n appName domainName

   For a .NET Framework ASPX site (legacy):
     cc -nf appName domainName

============================================================
UPGRADE
============================================================

1. Extract the new contensive.zip to a temporary folder.

2. Run the uninstall script as Administrator:

     Right-click uninstall.cmd -> "Run as administrator"

   This stops the Task Service, removes it, removes the CLI
   from PATH, and deletes installed files from:
     C:\Program Files\Contensive\

   Individual app IIS sites, databases, and file storage
   are NOT affected.

3. Run the install script as Administrator:

     Right-click install.cmd -> "Run as administrator"

4. Open a NEW command prompt and upgrade all applications:

     cc -u

   This upgrades the database schema and collections for
   every application on the server. Wait for this to complete
   before proceeding.

5. Upgrade each IIS site to use the new binaries:

   For .NET Core WebApi apps:
     Open IIS Manager and for each core site, copy the new 
     WebApi files from:
       C:\Program Files\Contensive\WebApi\
     into the site's existing physical path, replacing
     all files. Then recycle the app pool.

   For .NET Framework ASPX apps:
     Open IIS Manager, click on the site, then on the
     right click "Import Application" and select:
       C:\Program Files\Contensive\defaultaspxsite.zip
     This replaces the site binaries while preserving
     app-specific config files (WebAppSettings.config,
     WebRewrite.config).

6. Verify the Task Service is running:
     sc query "Contensive Task Service"
   If not started:
     sc start "Contensive Task Service"

============================================================
UNINSTALL
============================================================

1. Run the uninstall script as Administrator:

     Right-click uninstall.cmd -> "Run as administrator"

2. The uninstaller will:
   - Stop and remove the "Contensive Task Service"
   - Remove cc.exe from the system PATH
   - Delete all files from C:\Program Files\Contensive\

3. Individual app IIS sites, databases, and file folders
   are NOT removed. To fully remove an application:
   - Delete the IIS site from IIS Manager
   - Delete the app pool from IIS Manager
   - Delete the SQL Server database
   - Delete the app's file storage folder

============================================================
CLI QUICK REFERENCE
============================================================

  cc --help                          Show all commands
  cc --version                       Show version
  cc --configure                     Server configuration wizard
  cc --status                        Display server and app status
  cc -n appName domainName           Create a new core WebApi app
  cc -nf appName domainName          Create a new framework ASPX app
  cc -u                              Upgrade all applications
  cc -a appName -u                   Upgrade a specific application
  cc -a appName --install Collection Install a collection
  cc --serverdiagnostic              Run server diagnostics

============================================================
TROUBLESHOOTING
============================================================

- "ASP.NET Core Module not found":
    Install the .NET 9.0 Hosting Bundle and run iisreset.

- "Access denied":
    Run install/uninstall scripts as Administrator.
    Right-click -> "Run as administrator".

- Task Service won't start:
    Check the Windows Event Log (Application) for errors.
    Verify cc --configure has been run.

- Core IIS site returns 500:
    Check the stdout log in the site's logs\stdout folder.
    Verify the app pool CLR version is set to "No Managed Code".

- Framework ASPX site returns 500:
    Verify the app pool CLR version is set to "v4.0".
    Verify the app pool has "Enable 32-Bit Applications" = True.

- cc.exe not found after install:
    Open a NEW command prompt so the updated PATH takes effect.

============================================================
