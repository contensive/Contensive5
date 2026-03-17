
# Build Script Pattern

> All patterns and API reference: [Patterns Index](https://raw.githubusercontent.com/contensive/Contensive5/refs/heads/master/patterns/index.md)

## Overview

A build script automates compiling, packaging, and deploying a Contensive addon collection. It is a Windows batch file (`.cmd`) that lives in the project's `scripts/` folder and uses relative paths from that location. The script produces a versioned deployment folder containing the collection zip and optionally NuGet packages or installers.

## Folder Convention

Build scripts assume the following project folder structure:

```
Git/
  projectName/
    scripts/          <-- build script lives here, all paths relative to this folder
      build.cmd
    collections/
      collectionName/ <-- unzipped collection files including the collection XML
    server/ or source/
      projectName/    <-- Visual Studio project folder
    ui/
      wwwfiles/      <-- UI assets that will be copied to the www folder (HTML, CSS, JS, images)
      cdnfiles/      <-- assets that can be access by the http interace but a different url than the www
      privatefiles/  <-- assets used by the code but cannot be accessed from the http interface
    help/             <-- help/documentation files (optional)
```

- all folders that install on the servers should be lowercase letters.
- the "design" folder is where we store working design files that do no install with the deployment
- the "ui" folder (lowercase) has only three subfolders where we will put files that are going to be installed on the server in the addons collection file every time the project installs.
- /ui/wwwfiles/ folder and all its subfolders will be copied to the root www folder of the server when it is installed. So if you want images in an /img folder on the server, jsut put them in /ui/wwwfiles/img
- /ui/cdnfiles folder will be copied to cdnFiles. These files can be accessed by the website, but only by a url the code creates
- /ui/privatefiles folder is copies to privateFiles on the server. These files can only be accessed by the code.

## Script Sections

Every build script follows the same section order. Each section is separated by a `rem ======` comment line.

### 1. Header and Configuration Variables

Declare all configurable values at the top of the script. The script must first set the working directory to the project's `scripts/` folder since all paths are relative to it.

```cmd
rem Must be run from the projects git\project\scripts folder - everything is relative

c:
cd \Git\projectName\scripts

set DebugRelease=Debug
set solutionName=projectName.sln
set collectionName=collectionName
set collectionPath=..\collections\collectionName\
set binPath=..\server\projectName\bin\Debug\net48\
set deploymentFolderRoot=C:\Deployments\projectName\Dev\
set NuGetLocalPackagesFolder=C:\NuGetLocalPackages\
```

| Variable | Description |
|----------|-------------|
| `DebugRelease` | Build configuration, typically `Debug` or `Release` |
| `solutionName` | Name of the Visual Studio solution file |
| `collectionName` | Name of the addon collection (used for the output zip) |
| `collectionPath` | Relative path to the collection folder containing the collection XML and resource files |
| `binPath` | Relative path to the compiled output folder |
| `deploymentFolderRoot` | Root folder where versioned deployment folders are created |
| `NuGetLocalPackagesFolder` | Local folder for NuGet package copies (optional, only for projects that produce NuGet packages) |

### 2. Version Number Generation

The version number is auto-generated from the current date in `YYYY.M.D.revision` format. If a deployment folder already exists for that version, the revision number increments until a unique folder name is found.

```cmd
set year=%date:~12,4%
set month=%date:~4,2%
if %month% GEQ 10 goto monthOk
set month=%date:~5,1%
:monthOk
set day=%date:~7,2%
if %day% GEQ 10 goto dayOk
set day=%date:~8,1%
:dayOk
set versionMajor=%year%
set versionMinor=%month%
set versionBuild=%day%
set versionRevision=1

:tryagain
set versionNumber=%versionMajor%.%versionMinor%.%versionBuild%.%versionRevision%
if not exist "%deploymentFolderRoot%%versionNumber%" goto :makefolder
set /a versionRevision=%versionRevision%+1
goto tryagain
:makefolder
md "%deploymentFolderRoot%%versionNumber%"
```

This block should be copied as-is into every build script. Only `deploymentFolderRoot` changes between projects.

### 3. Clean Build and Collection Folders

Remove previous build artifacts to ensure a clean build. This includes the compiled `bin/` and `obj/` folders and any leftover files in the collection folder.

```cmd
rem ==============================================================
rem
rem clean build folders
rem
rd /S /Q "..\server\projectName\bin"
rd /S /Q "..\server\projectName\obj"

rem ==============================================================
rem
rem clean collection folder
rem
cd %collectionPath%
del *.zip
cd ..\..\scripts
```

### 4. Package UI Assets (optional)

If the project has UI files (layouts, CSS, JS, images), zip them into a `ui.zip` in the collection folder using 7-Zip.

```cmd
rem ==============================================================
rem
rem copy UI files
rem

cd ..\ui
"c:\program files\7-zip\7z.exe" a "%collectionPath%ui.zip"
cd ..\scripts
```

The `ui.zip` is included as a resource in the collection XML and installed to the site's content files.

### 5. Package Help Files (optional)

If the project has documentation, zip the help files into a `HelpFiles.zip` in the collection folder. Help files use a naming convention where commas in the filename represent topic hierarchy (e.g., `Topic,Article.md`).

```cmd
rem ==============================================================
rem
rem create helpfiles.zip
rem

cd ..\help
"c:\program files\7-zip\7z.exe" a "%collectionPath%HelpFiles.zip"
cd ..\scripts
```

The collection XML must include a corresponding resource node:
```xml
<Resource name="HelpFiles.zip" type="helpfiles" path="" />
```

### 6. Build the Solution

Clean and build the .NET project using `dotnet build`. The version number is passed as a build property so it is embedded in the compiled assemblies.

```cmd
rem ==============================================================
rem
rem build
rem

cd ..\server

dotnet clean %solutionName%

dotnet build projectName/projectName.csproj --configuration %DebugRelease% --no-dependencies /property:Version=%versionNumber% /property:AssemblyVersion=%versionNumber% /property:FileVersion=%versionNumber%
if errorlevel 1 (
   echo failure building
   pause
   exit /b %errorlevel%
)

cd ..\scripts
```

Key points:
- Use `dotnet clean` before building to ensure a fresh compile
- Always check `errorlevel` after the build and exit on failure
- Pass `/property:Version`, `/property:AssemblyVersion`, and `/property:FileVersion` to stamp the version
- If the project produces NuGet packages, delete old `.nupkg` files before building

### 7. Copy NuGet Packages to Deployment (optional)

If the project produces NuGet packages, copy them to both the deployment folder and the local NuGet packages folder.

```cmd
rem ==============================================================
rem
rem copy nuget to deployment
rem

xcopy projectName\bin\Debug\*.nupkg "%deploymentFolderRoot%%versionNumber%" /Y
xcopy projectName\bin\Debug\*.nupkg "%NuGetLocalPackagesFolder%" /Y
```

### 8. Build the Addon Collection Zip

This is the core deployment step. It assembles the collection zip by copying compiled DLLs into the collection folder alongside the collection XML and resource files, then zips everything together.

```cmd
rem ==============================================================
rem
rem Build addon collection
rem

rem delete old collection zip and DLLs
del "%collectionPath%%collectionName%.zip" /Q
del "%collectionPath%*.dll" /Q

rem copy compiled assemblies to collection folder
copy "%binPath%*.dll" "%collectionPath%"

rem create the collection zip
cd %collectionPath%
"c:\program files\7-zip\7z.exe" a "%collectionName%.zip"

rem copy to deployment folder
xcopy "%collectionName%.zip" "%deploymentFolderRoot%%versionNumber%" /Y

rem optionally copy to a shared sprint folder
xcopy "%collectionName%.zip" "c:\deployments\_current_sprint" /Y

cd ..\..\scripts
```

### 9. Clean Up Collection Folder

After the collection zip is built, remove the DLLs and the temporary resource zip files (like `HelpFiles.zip` and UI zips) from the collection folder. These resource zips were created during the build from source folders and will be recreated on the next build. The collection zip itself stays in the collection folder.

```cmd
rem ==============================================================
rem
rem cleanup collection folder
rem

cd %collectionPath%

del *.dll
del HelpFiles.zip
del ui.zip

cd ..\..\scripts
```

## Primary Example: aoEcommerce

Source: `C:\Git\aoEcommerce\scripts\build-c#.cmd`

This project demonstrates:
- Two UI folders (`catalog` and `ecommerce`) each zipped separately
- Two .NET projects built in sequence (`ApiV1` and `accountBilling`) with error checking after each
- NuGet package output from `ApiV1` copied to deployment and local package folders
- Additional binary file types copied to the collection (`.pdb`, `.dep`, `.dll.config`)
- Two deployment folders (`Dev` and `Dev-c#`) for parallel deployment targets
- Thorough cleanup removing all file types that may have been generated

## Second Example: aoMeetingManager

Source: `C:\Git\aoMeetingManager\scripts\build.cmd`

This project demonstrates:
- A simpler single-project build
- Help file packaging with the `HelpFiles.zip` pattern
- UI assets zipped from a single `ui/` folder
- Clean build by removing `bin/` and `obj/` folders before compiling
- Standard collection assembly with only `.dll` files copied

## Checklist for Creating a New Build Script

1. Create a `scripts/` folder in the project root
2. Set all configuration variables (solution name, collection name, paths)
3. Copy the version number generation block as-is
4. Add clean steps for build and collection folders
5. Add UI packaging if the project has UI assets
6. Add help file packaging if the project has documentation
7. Add the `dotnet build` step with version properties and error checking
8. Add NuGet copy steps if the project produces packages
9. Add the collection zip assembly step
10. Add cleanup to remove temporary files from the collection folder
11. Verify all `cd` commands return to the `scripts/` folder before the next section
