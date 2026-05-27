rem ==============================================================
rem
rem Must be run from the projects git\project\scripts folder - everything is relative
rem run >build [versionNumber]
rem run >build /nopause (for automated builds, no pause on errors)
rem
@echo off
cls

rem Check for /nopause flag
set PAUSE_ON_ERROR=1
if "%1"=="/nopause" set PAUSE_ON_ERROR=0

@echo +++++++++++++++++++++++++++++
@echo Build Process
@echo .
@echo When ready, hit any key to continue
@echo .
@echo +++++++++++++++++++++++++++++
rem
rem echo on

c:
cd \Git\Contensive5\scripts

rem @echo off
rem Setup deployment folder
set msbuildLocation=C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\
set deploymentFolderRoot=C:\Deployments\Contensive5\Dev\
set NuGetLocalPackagesFolder=C:\NuGetLocalPackages\
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
rem
rem if deployment folder exists, delete it and make directory
rem
:tryagain
set versionNumber=%versionMajor%.%versionMinor%.%versionBuild%.%versionRevision%
if not exist "%deploymentFolderRoot%%versionNumber%" goto :makefolder
set /a versionRevision=%versionRevision%+1
goto tryagain
:makefolder
md "%deploymentFolderRoot%%versionNumber%"
rem ==============================================================
rem
rem clean build folders
rem

@echo on

del /s /q  "..\source\cli\bin"
rd /s /q  "..\source\cli\bin"

del /s /q  "..\source\cli\obj"
rd /s /q  "..\source\cli\obj"

del /s /q  "..\source\clisetup\debug"
rd /s /q  "..\source\clisetup\debug"

del /s /q  "..\source\clisetup\Debug-DevApp"
rd /s /q  "..\source\clisetup\Debug-DevApp"

del /s /q  "..\source\clisetup\Debug-StagingDefaultApp"
rd /s /q  "..\source\clisetup\Debug-StagingDefaultApp"

del /s /q  "..\source\clisetup\DevDefaultApp"
rd /s /q  "..\source\clisetup\DevDefaultApp"

del /s /q  "..\source\clisetup\Release"
rd /s /q  "..\source\clisetup\Release"

del /s /q  "..\source\clisetup\StagingDefaultApp"
rd /s /q  "..\source\clisetup\StagingDefaultApp"

del /s /q  "..\source\CPBase\obj"
rd /s /q  "..\source\CPBase\obj"

del /s /q  "..\source\iisDefaultSite\bin"
rd /s /q  "..\source\iisDefaultSite\bin"

del /s /q  "..\source\iisDefaultSite\obj"
rd /s /q  "..\source\iisDefaultSite\obj"

del /s /q  "..\source\models\bin"
rd /s /q  "..\source\models\bin"

del /s /q  "..\source\models\obj"
rd /s /q  "..\source\models\obj"

del /s /q  "..\source\processor\bin"
rd /s /q  "..\source\processor\bin"

del /s /q  "..\source\processor\obj"
rd /s /q  "..\source\processor\obj"

del /s /q  "..\source\processortests\bin"
rd /s /q  "..\source\processortests\bin"

del /s /q  "..\source\processortests\obj"
rd /s /q  "..\source\processortests\obj"

del /s /q  "..\source\taskservice\bin"
rd /s /q  "..\source\taskservice\bin"

del /s /q  "..\source\taskservice\obj"
rd /s /q  "..\source\taskservice\obj"

del /q "..\WebDeploymentPackage\*.*"

rem del /s /q "C:\Git\Contensive5\source\Cli.Installer\bin"
rem rd /s /q  "C:\Git\Contensive5\source\Cli.Installer\bin"

rem del /s /q "C:\Git\Contensive5\source\Cli.Installer\obj"
rem rd /s /q  "C:\Git\Contensive5\source\Cli.Installer\obj"

rem

rem ==============================================================
rem
rem package helpfiles
rem
rem The /helpfiles folder contains markdown help files organized by role (admin, dev, member).
rem The build zips this folder into HelpFiles.zip and saves it to the collection folder.
rem The collection xml includes: <Resource Name="HelpFiles.zip" Type="helpfiles" Path="" />
rem

del "%deploymentFolderRoot%%versionNumber%\HelpFiles.zip"
del "C:\Git\Contensive5\source\Processor\HelpFiles.zip"

rem copy default article and articles for the  Help Pages collection
"c:\program files\7-zip\7z.exe" a "C:\Git\Contensive5\source\Processor\HelpFiles.zip" "C:\Git\Contensive5\helpfiles\*"

rem

rem ==============================================================
rem
rem zip ui files, copy to the placeholders in the folder, move to the bin folder
rem baseassets need img files copied to root of the zip file.
rem

@echo on

c:
cd "C:\Git\Contensive5\ui\baseassets\"
del baseassets.zip /Q
"c:\program files\7-zip\7z.exe" a "baseassets.zip"
"c:\program files\7-zip\7z.exe" d baseassets.zip baseassets\
"c:\program files\7-zip\7z.exe" d baseassets.zip ".DS_Store"
cd baseassets
"c:\program files\7-zip\7z.exe" a "..\baseassets.zip"
move /y "..\baseassets.zip"  "C:\Git\Contensive5\source\Processor\"

cd "C:\Git\Contensive5\scripts"

rem ==============================================================
rem
rem copy all Collection.dtd file from /xml folder to deployment
rem then the deployment script will copy to aws contensive folder
rem

c:
copy "C:\Git\Contensive5\source\Processor\Collection.xsd" "%deploymentFolderRoot%%versionNumber%\"

rem

rem ==============================================================
rem
rem copy the install readme file
rem then the deployment script will copy to aws contensive folder
rem

c:
copy "C:\Git\Contensive5\etc\install-readme.txt" "%deploymentFolderRoot%%versionNumber%\"
copy "C:\Git\Contensive5\etc\install.cmd" "%deploymentFolderRoot%%versionNumber%\"
copy "C:\Git\Contensive5\etc\uninstall.cmd" "%deploymentFolderRoot%%versionNumber%\"

rem

rem ==============================================================
rem
rem build and pack Contensive common solution (CPBase +Models + Processor)
rem The assembly version represents the architecture, 4.1.2.0 was the version we locked this down.
rem Assemblies are signed so if we use cpbase or models as a dependency in a dll that as a dependency, if a dll uses it and one of these bases, the assembly must match
rem

cd ..\source

dotnet clean contensivecommon.sln
if errorlevel 1 (
   echo failure cleaning common solution
   if %PAUSE_ON_ERROR%==1 pause
   exit /b %errorlevel%
)

dotnet build CPBase/CPBase.csproj --no-incremental --no-dependencies /property:AssemblyVersion=4.1.2.0 /property:FileVersion=%versionNumber% -p:TargetFramework=netstandard2.0
if errorlevel 1 (
   echo failure building CPBase
   if %PAUSE_ON_ERROR%==1 pause
   exit /b %errorlevel%
)

rem asssembly product version was set 20.0.0.0, properties, package, packageid was
dotnet build Models/Models.csproj --no-incremental --no-dependencies /property:AssemblyVersion=20.0.0.0 /property:FileVersion=%versionNumber% -p:TargetFramework=netstandard2.0
if errorlevel 1 (
   echo failure building Models
   if %PAUSE_ON_ERROR%==1 pause
   exit /b %errorlevel%
)

dotnet build Processor/Processor.csproj --no-incremental --no-dependencies /property:Version=%versionNumber% -p:TargetFramework=net48
if errorlevel 1 (
   echo failure building Processor net48
   if %PAUSE_ON_ERROR%==1 pause
   exit /b %errorlevel%
)

dotnet build Processor/Processor.csproj --no-incremental --no-dependencies /property:Version=%versionNumber% -p:TargetFramework=net9.0-windows
if errorlevel 1 (
   echo failure building Processor net9.0-windows
   if %PAUSE_ON_ERROR%==1 pause
   exit /b %errorlevel%
)

dotnet build taskservice/taskservice.csproj --no-incremental --no-dependencies /property:Version=%versionNumber% -p:TargetFramework=net9.0-windows
if errorlevel 1 (
   echo failure building taskservice
   if %PAUSE_ON_ERROR%==1 pause
   exit /b %errorlevel%
)

dotnet build cli/cli.csproj --no-incremental --no-dependencies /property:Version=%versionNumber% -p:TargetFramework=net48
if errorlevel 1 (
   echo failure building cli net48
   if %PAUSE_ON_ERROR%==1 pause
   exit /b %errorlevel%
)

dotnet build cli/cli.csproj --no-incremental --no-dependencies /property:Version=%versionNumber% -p:TargetFramework=net9.0-windows
if errorlevel 1 (
   echo failure building cli net9.0-windows
   if %PAUSE_ON_ERROR%==1 pause
   exit /b %errorlevel%
)

dotnet pack CPBase/CPBase.csproj --configuration Debug --no-build --no-restore /property:PackageVersion=%versionNumber%  -p:TargetFrameworks=netstandard2.0
if errorlevel 1 (
   echo failure pack CPBase
   if %PAUSE_ON_ERROR%==1 pause
   exit /b %errorlevel%
)

dotnet pack Models/Models.csproj --configuration Debug --no-build --no-restore /property:PackageVersion=%versionNumber%  -p:TargetFrameworks=netstandard2.0
if errorlevel 1 (
   echo failure pack Models
   if %PAUSE_ON_ERROR%==1 pause
   exit /b %errorlevel%
)

dotnet pack Processor/Processor.csproj --configuration Debug --no-build --no-restore /property:PackageVersion=%versionNumber%  -p:TargetFrameworks=net48
if errorlevel 1 (
   echo failure pack Processor
   if %PAUSE_ON_ERROR%==1 pause
   exit /b %errorlevel%
)

cd ..\scripts

rem ==============================================================
rem
rem move packages to deplyment, and to local package folder

cd ..\source

move /y "CPBase\bin\debug\Contensive.CPBaseClass.%versionNumber%.nupkg" "%deploymentFolderRoot%%versionNumber%\"
rem copy this package to the local package source so the next project builds all upgrade the assembly
xcopy "%deploymentFolderRoot%%versionNumber%\Contensive.CPBaseClass.%versionNumber%.nupkg" "%NuGetLocalPackagesFolder%" /Y

move /y "Models\Bin\Debug\Contensive.DBModels.%versionNumber%.nupkg" "%deploymentFolderRoot%%versionNumber%\"
rem copy this package to the local package source so the next project builds all upgrade the assembly
xcopy "%deploymentFolderRoot%%versionNumber%\Contensive.DBModels.%versionNumber%.nupkg" "%NuGetLocalPackagesFolder%" /Y

move /y "Processor\bin\debug\Contensive.Processor.%versionNumber%.nupkg" "%deploymentFolderRoot%%versionNumber%\"
rem copy this package to the local package source so the next project builds all upgrade the assembly
xcopy "%deploymentFolderRoot%%versionNumber%\Contensive.Processor.%versionNumber%.nupkg" "%NuGetLocalPackagesFolder%" /Y

cd ..\scripts
rem ==============================================================
rem
rem update aspx site nuget packages 
rem

cd ..\source\iisdefaultsite
dotnet remove package Contensive.CPBaseClass
dotnet add package Contensive.CPBaseClass --source c:\NuGetLocalPackages

dotnet remove package Contensive.DbModels
dotnet add package Contensive.DbModels --source c:\NuGetLocalPackages

dotnet remove package Contensive.Processor
dotnet add package Contensive.Processor --source c:\NuGetLocalPackages
cd ..\..\scripts

rem 

rem ==============================================================
rem
rem build aspx and publish 
rem
cd ..\source

"%msbuildLocation%msbuild.exe" contensiveAspx.sln /p:DeployOnBuild=true /p:PublishProfile=defaultSite
if errorlevel 1 (
   echo failure building contensiveAspx
   if %PAUSE_ON_ERROR%==1 pause
   exit /b %errorlevel%
)

xcopy "..\WebDeploymentPackage\*.zip" "%deploymentFolderRoot%%versionNumber%" /Y

cd ..\scripts

rem

rem ==============================================================
rem
rem build cli installer
rem

cd ..\source
rem reset integrity label on existing msi so WiX linker can overwrite it from a non-elevated process
icacls "Cli.Installer\bin\Debug\en-us\ContensiveConsole-Debug-x64.msi" /setintegritylevel Medium 2>nul
rem "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\msbuild.exe" ContensiveCLIInstaller\ContensiveCLIInstaller.wixproj
"%msbuildLocation%msbuild.exe" cli.installer\cli.installer.wixproj
if errorlevel 1 (
echo failure building cli installer
   if %PAUSE_ON_ERROR%==1 pause
   exit /b %errorlevel%
)

xcopy "Cli.Installer\bin\Debug\en-us\*.msi" "%deploymentFolderRoot%%versionNumber%\"

cd ..\scripts

rem

rem ==============================================================
rem
rem done
rem
