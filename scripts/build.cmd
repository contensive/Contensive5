rem 
rem Must be run from the projects git\project\scripts folder - everything is relative
rem run >build [versionNumber]
rem versionNumber is 5.YYMM.DD.build-number, like 5.1908.24.5
rem

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

rem pause

rem ==============================================================
rem
rem build and pack Contensive common solution (CPBase +Models + Processor)
rem The assembly version represents the architecture, 4.1.2.0 was the version we locked this down.
rem Assemblies are signed so if we use cpbase or models as a dependency in a dll that as a dependency, if a dll uses it and one of these bases, the assembly must match
rem

cd ..\source

dotnet clean contensivecommon.sln

dotnet build CPBase/CPBase.csproj --no-dependencies /property:AssemblyVersion=4.1.2.0 /property:FileVersion=%versionNumber%

dotnet build Models/Models.csproj --no-dependencies /property:AssemblyVersion=4.1.2.0 /property:FileVersion=%versionNumber%

dotnet build Processor/Processor.csproj --no-dependencies /property:Version=%versionNumber%

dotnet build taskservice/taskservice.csproj --no-dependencies /property:Version=%versionNumber%

dotnet build cli/cli.csproj --no-dependencies /property:Version=%versionNumber%

dotnet pack CPBase/CPBase.csproj --no-build --no-restore /property:PackageVersion=%versionNumber%

dotnet pack Models/Models.csproj --no-build --no-restore /property:PackageVersion=%versionNumber%

dotnet pack Processor/Processor.csproj --no-build --no-restore /property:PackageVersion=%versionNumber%

if errorlevel 1 (
   echo failure building common solution
   pause
   exit /b %errorlevel%
)

cd ..\scripts

rem pause

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

rem pause

rem ==============================================================
rem
rem update cli, taskservice nuget packages 
rem
rem 
rem cd ..\source\cli
rem dotnet add package Contensive.CPBaseClass -s %NuGetLocalPackagesFolder%
rem dotnet add package Contensive.DbModels -s %NuGetLocalPackagesFolder%
rem dotnet add package Contensive.Processor -s %NuGetLocalPackagesFolder%
rem 
rem cd ..\taskservice
rem dotnet add package Contensive.CPBaseClass -s %NuGetLocalPackagesFolder%
rem dotnet add package Contensive.DbModels -s %NuGetLocalPackagesFolder%
rem dotnet add package Contensive.Processor -s %NuGetLocalPackagesFolder%
rem cd ..\..\scripts

rem ==============================================================
rem
rem build cli and task server
rem
rem cd ..\source
rem 
rem dotnet clean ContensiveCli.sln
rem 
rem dotnet build cli/cli.csproj --no-dependencies /property:FileVersion=%versionNumber%
rem 
rem pause
rem 
rem dotnet build taskservice/taskservice.csproj --no-dependencies /property:FileVersion=%versionNumber%
rem 
rem pause
rem 
rem cd ..\scripts
rem 

rem ==============================================================
rem
rem build cli installer
rem

cd ..\source
rem "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\msbuild.exe" ContensiveCLIInstaller\ContensiveCLIInstaller.wixproj
"%msbuildLocation%msbuild.exe" cli.installer\cli.installer.wixproj
if errorlevel 1 (
echo failure building cli installer
   pause
   exit /b %errorlevel%
)

xcopy "Cli.Installer\bin\Debug\en-us\*.msi" "%deploymentFolderRoot%%versionNumber%\"

cd ..\scripts

rem pause


rem ==============================================================
rem
rem web.config must be manually updated from processor.dll.config
rem
@echo off
cls

@echo +++++++++++++++++++++++++++++
@echo Manually update web.config if there were nuget package changes
@echo 
@echo nuget packages automatically update Processor project appconfig, but not the iisDefault web.config file. It must be manually updated if there were any nuget changes.
@echo 
@echo edit C:\Git\Contensive5\source\Processor\bin\Debug\net472\Processor.dll.config
@echo copy the assembly redirect section
@echo 
@echo edit C:\Git\Contensive5\source\iisDefaultSite\Web.config
@echo replace the assembly redirect section
@echo 
@echo +++++++++++++++++++++++++++++
pause



rem ==============================================================
rem
rem update aspx site nuget packages 
rem
cd ..\source\iisdefaultsite
nuget update iisdefaultsite.vbproj -noninteractive -source nuget.org -source %NuGetLocalPackagesFolder% -Id Contensive.CPBaseClass
nuget update iisdefaultsite.vbproj -noninteractive -source nuget.org -source %NuGetLocalPackagesFolder% -Id Contensive.DbModels
nuget update iisdefaultsite.vbproj -noninteractive -source nuget.org -source %NuGetLocalPackagesFolder% -Id Contensive.Processor
cd ..\..\scripts

rem pause

rem ==============================================================
rem
rem build aspx and publish 
rem
cd ..\source

"%msbuildLocation%msbuild.exe" contensiveAspx.sln /p:DeployOnBuild=true /p:PublishProfile=defaultSite
if errorlevel 1 (
   echo failure building contensiveAspx
   pause
   exit /b %errorlevel%
)

xcopy "..\WebDeploymentPackage\*.zip" "%deploymentFolderRoot%%versionNumber%" /Y

cd ..\scripts

rem pause

rem ==============================================================
rem
rem update nuget for all test projects
rem

cd ..\source\Models
cd ..\ModelTests
nuget update ModelTests.csproj -noninteractive -source nuget.org -source %NuGetLocalPackagesFolder% -Id Contensive.CPBaseClass
nuget update ModelTests.csproj -noninteractive -source nuget.org -source %NuGetLocalPackagesFolder% -Id Contensive.DbModels
nuget update ModelTests.csproj -noninteractive -source nuget.org -source %NuGetLocalPackagesFolder% -Id Contensive.Processor
cd ..\..\scripts

cd ..\source\Processor
cd ..\ProcessorTests
nuget update ProcessorTests.csproj -noninteractive -source nuget.org -source %NuGetLocalPackagesFolder% -Id Contensive.CPBaseClass
nuget update ProcessorTests.csproj -noninteractive -source nuget.org -source %NuGetLocalPackagesFolder% -Id Contensive.DbModels
nuget update ProcessorTests.csproj -noninteractive -source nuget.org -source %NuGetLocalPackagesFolder% -Id Contensive.Processor
cd ..\..\scripts

rem pause

rem ==============================================================
rem
rem done
rem

rem pause
