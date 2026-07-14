

echo ***** problem - must be run elevated, but elevating runs in system32 and the location of this install has the version number in it.

rem upgrade from current path
rem this script uninstalls the existing version and installs the new version
rem

echo.
echo ===== Contensive Upgrade =====
echo.
echo This will uninstall the current version and install the new version.
echo.
pause

echo.
echo [1/2] Uninstalling existing version...
echo.

msiexec.exe /x ContensiveConsole-Debug-x64.msi /qn

echo.
echo [2/2] Installing new version...
echo.

msiexec.exe /i ContensiveConsole-Debug-x64.msi /qn

xcopy DefaultAspxSite.zip "c:\program files\contensive\"

echo.
echo ===== Upgrade Complete =====
echo.

pause
