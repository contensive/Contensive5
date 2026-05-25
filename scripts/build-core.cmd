rem ==============================================================
rem
rem Build and publish all .NET 9.0 projects (WebApi, CLI, TaskService)
rem Must be run from the project's git\project\scripts folder
rem run >build-core [/nopause]
rem
@echo off
cls

rem Check for /nopause flag
set PAUSE_ON_ERROR=1
if "%1"=="/nopause" set PAUSE_ON_ERROR=0

@echo +++++++++++++++++++++++++++++
@echo .NET 9.0 Core Build and Publish
@echo +++++++++++++++++++++++++++++

c:
cd \Git\Contensive5\scripts

rem Setup deployment folder with date-based version
set deploymentFolderRoot=C:\Deployments\Contensive5\Dev\
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

@echo.
@echo Version: %versionNumber%
@echo Deploy to: %deploymentFolderRoot%%versionNumber%
@echo.

rem ==============================================================
rem
rem clean build folders
rem

@echo on

del /s /q "..\source\WebApi\bin" 2>nul
rd /s /q "..\source\WebApi\bin" 2>nul

del /s /q "..\source\WebApi\obj" 2>nul
rd /s /q "..\source\WebApi\obj" 2>nul

del /s /q "..\source\Cli\bin" 2>nul
rd /s /q "..\source\Cli\bin" 2>nul

del /s /q "..\source\Cli\obj" 2>nul
rd /s /q "..\source\Cli\obj" 2>nul

del /s /q "..\source\TaskService\bin" 2>nul
rd /s /q "..\source\TaskService\bin" 2>nul

del /s /q "..\source\TaskService\obj" 2>nul
rd /s /q "..\source\TaskService\obj" 2>nul

del /s /q "..\source\CPBase\obj" 2>nul
rd /s /q "..\source\CPBase\obj" 2>nul

del /s /q "..\source\Models\bin" 2>nul
rd /s /q "..\source\Models\bin" 2>nul

del /s /q "..\source\Models\obj" 2>nul
rd /s /q "..\source\Models\obj" 2>nul

del /s /q "..\source\Processor\bin" 2>nul
rd /s /q "..\source\Processor\bin" 2>nul

del /s /q "..\source\Processor\obj" 2>nul
rd /s /q "..\source\Processor\obj" 2>nul

rem ==============================================================
rem
rem package helpfiles and baseassets (same as main build)
rem

del "C:\Git\Contensive5\source\Processor\HelpFiles.zip" 2>nul
"c:\program files\7-zip\7z.exe" a "C:\Git\Contensive5\source\Processor\HelpFiles.zip" "C:\Git\Contensive5\helpfiles\*"

c:
cd "C:\Git\Contensive5\ui\baseassets\"
del baseassets.zip /Q 2>nul
"c:\program files\7-zip\7z.exe" a "baseassets.zip"
"c:\program files\7-zip\7z.exe" d baseassets.zip baseassets\
"c:\program files\7-zip\7z.exe" d baseassets.zip ".DS_Store"
cd baseassets
"c:\program files\7-zip\7z.exe" a "..\baseassets.zip"
move /y "..\baseassets.zip" "C:\Git\Contensive5\source\Processor\"

cd "C:\Git\Contensive5\scripts"

rem ==============================================================
rem
rem publish WebApi as a self-contained deployment to a folder
rem This produces a complete deployment folder with all dependencies
rem

@echo.
@echo Publishing WebApi...
@echo.

cd ..\source

dotnet publish WebApi/WebApi.csproj --configuration Release --output "%deploymentFolderRoot%%versionNumber%\WebApi" --runtime win-x64 --self-contained false /property:Version=%versionNumber%
if errorlevel 1 (
   echo.
   echo FAILURE publishing WebApi
   if %PAUSE_ON_ERROR%==1 pause
   exit /b %errorlevel%
)

cd ..\scripts

rem ==============================================================
rem
rem publish CLI for .NET 9.0
rem

@echo.
@echo Publishing CLI (net9.0-windows)...
@echo.

cd ..\source

dotnet publish Cli/Cli.csproj --configuration Release --output "%deploymentFolderRoot%%versionNumber%\Cli" --runtime win-x64 --self-contained false /property:Version=%versionNumber% -p:TargetFramework=net9.0-windows
if errorlevel 1 (
   echo.
   echo FAILURE publishing CLI
   if %PAUSE_ON_ERROR%==1 pause
   exit /b %errorlevel%
)

cd ..\scripts

rem ==============================================================
rem
rem publish TaskService for .NET 9.0
rem

@echo.
@echo Publishing TaskService (net9.0-windows)...
@echo.

cd ..\source

dotnet publish TaskService/TaskService.csproj --configuration Release --output "%deploymentFolderRoot%%versionNumber%\TaskService" --runtime win-x64 --self-contained false /property:Version=%versionNumber%
if errorlevel 1 (
   echo.
   echo FAILURE publishing TaskService
   if %PAUSE_ON_ERROR%==1 pause
   exit /b %errorlevel%
)

cd ..\scripts

rem ==============================================================
rem
rem create a web.config for IIS reverse proxy hosting
rem This tells IIS to hand off requests to the Kestrel process
rem

@echo.
@echo Creating IIS web.config for reverse proxy...
@echo.

(
echo ^<?xml version="1.0" encoding="utf-8"?^>
echo ^<configuration^>
echo   ^<system.webServer^>
echo     ^<handlers^>
echo       ^<add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" /^>
echo     ^</handlers^>
echo     ^<aspNetCore processPath="dotnet"
echo                  arguments=".\WebApi.dll"
echo                  stdoutLogEnabled="true"
echo                  stdoutLogFile=".\logs\stdout"
echo                  hostingModel="InProcess"^>
echo       ^<environmentVariables^>
echo         ^<environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" /^>
echo       ^</environmentVariables^>
echo     ^</aspNetCore^>
echo     ^<security^>
echo       ^<requestFiltering^>
echo         ^<requestLimits maxAllowedContentLength="1073741824" /^>
echo       ^</requestFiltering^>
echo     ^</security^>
echo   ^</system.webServer^>
echo ^</configuration^>
) > "%deploymentFolderRoot%%versionNumber%\WebApi\web.config"

rem ==============================================================
rem
rem create the logs folder for stdout logging
rem

md "%deploymentFolderRoot%%versionNumber%\WebApi\logs" 2>nul

rem ==============================================================
rem
rem copy install/uninstall scripts into the deployment folder
rem

copy "C:\Git\Contensive5\scripts\install.ps1" "%deploymentFolderRoot%%versionNumber%\"
copy "C:\Git\Contensive5\scripts\uninstall.ps1" "%deploymentFolderRoot%%versionNumber%\"

rem ==============================================================
rem
rem create a single zip distribution package
rem

@echo.
@echo Creating distribution package...
@echo.

"c:\program files\7-zip\7z.exe" a "%deploymentFolderRoot%%versionNumber%\deploy.zip" "%deploymentFolderRoot%%versionNumber%\*"
if errorlevel 1 (
   echo.
   echo FAILURE creating zip package
   if %PAUSE_ON_ERROR%==1 pause
   exit /b %errorlevel%
)

rem ==============================================================
rem
rem remove source files, leaving only deploy.zip in the version folder
rem

@echo.
@echo Cleaning up build artifacts...
@echo.

rd /s /q "%deploymentFolderRoot%%versionNumber%\WebApi" 2>nul
rd /s /q "%deploymentFolderRoot%%versionNumber%\Cli" 2>nul
rd /s /q "%deploymentFolderRoot%%versionNumber%\TaskService" 2>nul
del /q "%deploymentFolderRoot%%versionNumber%\install.ps1" 2>nul
del /q "%deploymentFolderRoot%%versionNumber%\uninstall.ps1" 2>nul

rem ==============================================================
rem
rem done
rem

@echo.
@echo ==============================================================
@echo Build complete: %versionNumber%
@echo.
@echo Deployment folder:
@echo   %deploymentFolderRoot%%versionNumber%\
@echo.
@echo Distribution package:
@echo   %deploymentFolderRoot%%versionNumber%\deploy.zip
@echo.
@echo To install on a server:
@echo   1. Copy the zip to the server and extract
@echo   2. Run: powershell -ExecutionPolicy Bypass -File install.ps1
@echo   3. Create apps: cc -n appName domainName
@echo   4. See docs\setup-and-deployment.md for details
@echo ==============================================================
@echo.

if %PAUSE_ON_ERROR%==1 pause
