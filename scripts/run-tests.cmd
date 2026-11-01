@echo off
rem ==============================================================
rem
rem Run all ProcessorTests
rem Must be run from the project's git\project\scripts folder
rem
rem Before running, ensure the latest build has been installed
rem on the c5test application by running upgrade-full.cmd from
rem the latest contensive.zip deployment folder.
rem
rem ==============================================================

cls

echo.
echo ==============================================================
echo  Contensive5 ProcessorTests
echo ==============================================================
echo.
echo  Before running tests, the c5test database must be upgraded
echo  to match the current code. If you have not run the upgrade
echo  since your last schema change, press Ctrl+C now and run
echo  upgrade-full.cmd from the latest contensive.zip deployment.
echo.
echo ==============================================================
echo.

pause

cd /d C:\Git\Contensive5

dotnet test source\ProcessorTests\ProcessorTests.csproj -v n

echo.
if errorlevel 1 (
   echo Tests FAILED
) else (
   echo Tests PASSED
)
echo.

pause
