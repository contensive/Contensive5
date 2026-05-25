# Setup and Deployment Guide (.NET 9.0)

This guide covers building and deploying the three .NET 9.0 server components: the CLI tool (`cc.exe`), the Task Service (Windows service), and the WebApi website package. These can run side-by-side with existing .NET Framework 4.8 deployments on the same server.

## Architecture

Contensive has two levels of installation:

1. **Server install** — installs the CLI, Task Service, and WebApi package files onto the server
2. **App creation** — uses `cc.exe -n appName domainName` to create individual applications, each with its own IIS site, database, and file storage

Each Contensive application is a separate IIS site. The WebApi resolves its app name from the IIS site name at runtime, so no per-app configuration file is needed.

## Prerequisites

1. **Install the .NET 9.0 Hosting Bundle** on the target server. This provides the .NET 9.0 runtime, the ASP.NET Core runtime, and the IIS ASP.NET Core Module (ANCM):
   - Download from https://dotnet.microsoft.com/download/dotnet/9.0 -- get the "Hosting Bundle" under the Windows section
   - Run the installer
   - Restart IIS: `iisreset` from an admin command prompt

2. **Verify the ASP.NET Core Module is installed** (needed for WebApi IIS hosting):
   ```
   %windir%\system32\inetsrv\appcmd list modules | findstr AspNetCoreModuleV2
   ```

## Build

### Local build

On your dev machine, run the build script from the scripts folder:

```
cd C:\Git\Contensive5\scripts
build-core.cmd
```

This produces:
- A deployment folder at `C:\Deployments\Contensive5\Dev\{version}\` containing:

  | Folder | Contents |
  |---|---|
  | `Cli\` | `cc.exe` command-line tool |
  | `TaskService\` | Windows service executable |
  | `WebApi\` | ASP.NET Core website package (deployed per-app by `cc.exe -n`) |
  | `install.ps1` | PowerShell install script |
  | `uninstall.ps1` | PowerShell uninstall script |

- A distribution zip at `C:\Deployments\Contensive5\Dev\Contensive-{version}.zip`

The build generates a date-based version number (YY.M.D.R), packages help files and base assets, runs `dotnet publish` for all three projects, and creates a single zip for distribution.

### CI/CD build (GitHub Actions)

The workflow at `.github/workflows/build-core.yml` runs automatically on pushes to `master` that change source files. It:

1. Builds and publishes all three projects
2. Packages everything into a zip with install scripts
3. Uploads the zip as a build artifact
4. On pushes to master, creates a GitHub Release with the zip attached

To trigger a build manually, go to Actions > "Build .NET 9.0 Core" > "Run workflow".

Download the distribution zip from the GitHub Releases page or from the workflow artifacts.

## Quick install (scripted)

The distribution zip includes PowerShell install/uninstall scripts that automate the setup of all server components.

### Install

1. Copy the zip to the target server and extract it
2. Open an **elevated PowerShell prompt** and run:
   ```powershell
   .\install.ps1
   ```

This will:
- Install the CLI and add it to the system PATH
- Install and start the Task Service as a Windows service
- Place the WebApi package files in the install folder

### Create an application

After the server install, create individual applications using the CLI:

```
cc -n appName domainName
```

This creates:
- A SQL Server database for the application
- Local file storage folders (www, files, private, temp)
- An IIS site named after the app, with the WebApi files deployed into it
- The initial database schema

### Install options

```powershell
# Install to a custom path
.\install.ps1 -InstallPath "E:\Contensive"

# Install only the CLI
.\install.ps1 -SkipWebApi -SkipTaskService

# Install only the TaskService
.\install.ps1 -SkipWebApi -SkipCli
```

| Parameter | Default | Description |
|---|---|---|
| `-SourcePath` | Script folder | Path to extracted deployment files |
| `-InstallPath` | `C:\Program Files\Contensive` | Base installation directory for program binaries |
| `-ServiceName` | `Contensive Task Service` | Windows service name |
| `-SkipWebApi` | | Skip WebApi package installation |
| `-SkipTaskService` | | Skip TaskService installation |
| `-SkipCli` | | Skip CLI installation |

### Upgrade

To upgrade, first uninstall the previous version, then run `install.ps1` with the new build:

```powershell
.\uninstall.ps1
.\install.ps1
```

The install script checks for an existing installation and will stop with an error if one is found. This prevents accidental partial upgrades. Individual app IIS sites need their WebApi files updated separately — use `cc.exe` upgrade commands or manually copy the WebApi files from the install folder to each app's IIS site physical path.

### Uninstall

```powershell
.\uninstall.ps1
```

This removes the CLI from PATH, stops and removes the Task Service, and deletes the WebApi package files. Individual app IIS sites, databases, and file folders are not removed.

| Parameter | Default | Description |
|---|---|---|
| `-InstallPath` | `C:\Program Files\Contensive` | Base installation directory for program binaries |
| `-ServiceName` | `Contensive Task Service` | Windows service to remove |
| `-KeepFiles` | | Remove services but keep installed files |
| `-SkipWebApi` | | Skip WebApi package removal |
| `-SkipTaskService` | | Skip TaskService removal |
| `-SkipCli` | | Skip CLI removal |

---

## Manual install

If you prefer to set up each component individually rather than using the install script:

### 1. CLI Tool

The CLI (`cc.exe`) is a command-line tool for server setup, application management, collection installation, and diagnostics.

1. Copy the `Cli\` folder to the target server:
   ```
   C:\Program Files\Contensive\Cli\
   ```

2. Add the folder to the system PATH:
   ```
   setx /M PATH "%PATH%;C:\Program Files\Contensive\Cli"
   ```

#### Usage

Run from an elevated command prompt (many commands require administrator privileges):

```
cc --help                          Show all commands
cc --version                       Show version
cc --configure                     Server configuration wizard
cc --status                        Display server and app status
cc -n appName domainName           Create a new application
cc -a myApp --upgrade              Upgrade a specific application
cc -a myApp --install MyCollection Install a collection from the library
cc -a myApp --tasks run            Run task scheduler and runner in console
cc --serverdiagnostic              Run server diagnostics (requires admin)
```

See `cc --help` for the full command reference.

### 2. Task Service (Windows Service)

The Task Service runs the Contensive task scheduler, task runner, and MQTT subscription service as background processes. It is a .NET 9.0 Windows Service using `Microsoft.Extensions.Hosting.WindowsServices`.

1. Copy the `TaskService\` folder to the target server:
   ```
   C:\Program Files\Contensive\TaskService\
   ```

2. If replacing an existing .NET Framework Task Service, stop and remove it first:
   ```
   sc.exe stop "Contensive Task Service"
   sc.exe delete "Contensive Task Service"
   ```

3. Install the new service using `sc.exe` from an elevated command prompt:
   ```
   sc.exe create "Contensive Task Service" binPath="C:\Program Files\Contensive\TaskService\TaskService.exe" start=auto
   ```

4. Start the service:
   ```
   sc.exe start "Contensive Task Service"
   ```

#### Running from the command line

For testing and debugging, you can run `TaskService.exe` directly from a command prompt. When not installed as a Windows service, it runs as a console application and can be stopped with Ctrl+C.

### 3. WebApi Package

The WebApi package is a set of ASP.NET Core files that get deployed into each application's IIS site folder. The server install places these files in a central location; `cc.exe -n` copies them when creating new applications.

1. Copy the `WebApi\` folder to the install path:
   ```
   C:\Program Files\Contensive\WebApi\
   ```

This folder serves as the template. When `cc.exe -n appName` creates a new app, it copies these files into the app's IIS site physical path.

#### How app name resolution works

The WebApi resolves its Contensive app name at runtime using this priority:
1. `Contensive:AppName` in appsettings.json (optional override)
2. The IIS site name (via the `IIS_SITE_NAME` server variable)
3. The `CONTENSIVE_APPNAME` environment variable

In normal operation, the IIS site name matches the Contensive app name, so no configuration is needed. The optional overrides exist for testing or non-standard setups.

#### Differences from iisDefaultSite

| | iisDefaultSite (Web Forms) | WebApi (ASP.NET Core) |
|---|---|---|
| Runtime | .NET Framework 4.8 | .NET 9.0 |
| IIS hosting | Direct managed pipeline | ASP.NET Core Module / Kestrel (in-process) |
| App pool CLR | v4.0 | No Managed Code |
| Config | WebAppSettings.config, web.config | appsettings.json |
| App name | `ContensiveAppName` app setting or IIS site name | IIS site name (or config override) |

---

## Upgrading an existing server

To add the .NET 9.0 components alongside existing .NET Framework 4.8 deployments:

1. Install the .NET 9.0 Hosting Bundle (see Prerequisites)
2. Run `build-core.cmd` on your dev machine, or download a release from GitHub
3. Copy the zip to the server, extract, and run `install.ps1`
4. Existing Web Forms IIS sites continue to run unchanged
5. New apps created with `cc.exe -n` will use the WebApi
6. The Task Service installs under a different service name if you want to run both temporarily
7. The .NET 9.0 CLI can coexist with the .NET Framework CLI in different folders

Both the old and new components use the same Contensive server configuration, database, and file storage. They can run simultaneously for testing before cutting over.
