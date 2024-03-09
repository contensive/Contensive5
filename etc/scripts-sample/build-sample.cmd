
 echo off

set debugRelease=debug

rem
rem repo and collection name are the vs-solution (client or larger cagtegory)
rem collection and project name are the individual installable vs-project
rem

set gitRepoName=aoRss
set solutionName=aoRss
set projectName=aoRss
set collectionName=aoRss

set gitRepoPath=c:\git\%gitRepoName%\
set scriptsPath=%gitRepoPath%\scripts\
set collectionPath=%gitRepoPath%collections\%collectionName%\
set serverSolutionPath=%gitRepoPath%server\
set serverProjectPath=%serverSolutionPath%%projectName%\
set serverBinPath=%serverProjectPath%bin\%debugRelease%\net472\
set uiPath=%gitRepoPath%ui\
set deploymentPath=c:\Deployments\%collectionName%\Dev\
set msbuildLocation=c:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\
set NuGetLocalPackagesFolder=c:\NuGetLocalPackages\
set year=%date:~12,4%
set month=%date:~4,2%
if %month% GEQ 10 goto monthOk
set month=%date:~5,1%
:monthOk
set day=%date:~7,2%
if %day% GEQ 10 goto dayOk
set day=%date:~8,1%
:dayOk
set versionRevision=1
rem
rem if deployment folder exists, delete it and make directory
rem
:tryagain
set versionNumber=%year%.%month%.%day%.%versionRevision%
if not exist "%deploymentPath%%versionNumber%" goto :makefolder
set /a versionRevision=%versionRevision%+1
goto tryagain
:makefolder
md "%deploymentPath%%versionNumber%"

rem ==============================================================
rem
rem echo and test paths
rem

@echo debugRelease=%debugRelease%
@echo gitRepoName=%gitRepoName%
@echo solutionName=%solutionName%
@echo collectionName=%collectionName%
@echo versionNumber=%versionNumber%

@echo scriptsPath=%scriptsPath%
if exist "%scriptsPath%" goto :scriptsPathOK
   echo missing folder
   pause
:scriptsPathOK


@echo gitRepoPath=%gitRepoPath%
if exist "%gitRepoPath%" goto :gitRepoPathOK
   echo missing folder
   pause
:gitRepoPathOK

@echo collectionPath=%collectionPath%
if exist "%collectionPath%" goto :collectionPathOK
   echo missing folder
   pause
:collectionPathOK

@echo serverBinPath=%serverBinPath%
if exist "%serverBinPath%" goto :serverBinPathOK
   echo missing folder
   pause
:serverBinPathOK

@echo uiPath=%uiPath%
if exist "%uiPath%" goto :uiPathOK
   echo missing folder
   pause
:uiPathOK

@echo deploymentPath=%deploymentPath%
if exist "%deploymentPath%" goto :deploymentPathOK
   echo missing folder
   pause
:deploymentPathOK

@echo msbuildLocation=%msbuildLocation%
if exist "%msbuildLocation%" goto :msbuildLocationOK
   echo missing folder
   pause
:msbuildLocationOK

@echo NuGetLocalPackagesFolder=%NuGetLocalPackagesFolder%
if exist "%NuGetLocalPackagesFolder%" goto :NuGetLocalPackagesFolderOK
   echo missing folder
   pause
:NuGetLocalPackagesFolderOK

rem pause

rem ==============================================================
rem
echo clean collection folder
cd %collectionPath%

del *.zip

rem pause


rem ==============================================================
rem
echo copy UI files
cd %uiPath%

copy *.* %collectionPath%

rem pause

rem ==============================================================
rem
echo build solution 
@echo on
cd %serverSolutionPath%

dotnet clean %solutionName%.sln

rem pause

cd %serverProjectPath%

dotnet build %projectName% --configuration %debugRelease% --property WarningLevel=0 --property:Version=%versionNumber% --property:AssemblyVersion=%versionNumber% --property:FileVersion=%versionNumber%
if errorlevel 1 (
   echo failure building project
   pause
)

rem pause

rem ==============================================================
rem
echo Build ecommerce collection
cd %collectionPath%

rem remove old DLL files from the collection folder
del "%collectionPath%*.dll"
del "%collectionPath%*.pdb"

rem copy accountbilling bin folder assemblies to collection folder
copy "%serverBinPath%*.dll" "%collectionPath%"

rem create new collection zip file
c:
del "%collectionName%.zip" /Q
"c:\program files\7-zip\7z.exe" a "%collectionName%.zip"

rem copy the finished collection zip file to deployment folder
xcopy "%collectionName%.zip" "%deploymentPath%%versionNumber%" /Y

rem pause

rem ==============================================================
rem
echo clean ecommerce collection folder
rem

cd %collectionPath%

del *.dll
del *.dll.config
del *.html
del *.gif
del *.jpg
del *.svg
del *.png
del *.txt
del *.js
del *.css
del *.pdf
del *.jfif

cd %scriptsPath%

rem pause

