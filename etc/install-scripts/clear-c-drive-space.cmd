@echo off
rem ============================================================
rem Reclaim disk space on the C: drive
rem This script must be run as Administrator
rem ============================================================

net session >nul 2>&1
if errorlevel 1 (
    echo.
    echo ERROR: This script must be run as Administrator.
    echo Right-click and select "Run as administrator".
    echo.
    pause
    exit /b 1
)

echo.
echo ============================================================
echo Contensive - Clear C: Drive Space
echo ============================================================
echo.

rem ------------------------------------------------------------
rem Windows Component Store cleanup (removes superseded updates)
rem ------------------------------------------------------------
echo [1/7] Cleaning up Windows component store (DISM)...
Dism.exe /Online /Cleanup-Image /StartComponentCleanup /ResetBase
echo.

rem ------------------------------------------------------------
rem Windows Update cleanup
rem ------------------------------------------------------------
echo [2/7] Clearing Windows Update cache...
net stop wuauserv >nul 2>&1
if exist "%SystemRoot%\SoftwareDistribution\Download" (
    del /q /s "%SystemRoot%\SoftwareDistribution\Download\*" >nul 2>&1
)
net start wuauserv >nul 2>&1
echo   Done.
echo.

rem ------------------------------------------------------------
rem Temporary files
rem ------------------------------------------------------------
echo [3/7] Clearing temporary files...
if exist "%TEMP%" (
    del /q /s "%TEMP%\*" >nul 2>&1
    for /d %%D in ("%TEMP%\*") do rd /s /q "%%D" >nul 2>&1
)
if exist "%SystemRoot%\Temp" (
    del /q /s "%SystemRoot%\Temp\*" >nul 2>&1
    for /d %%D in ("%SystemRoot%\Temp\*") do rd /s /q "%%D" >nul 2>&1
)
echo   Done.
echo.

rem ------------------------------------------------------------
rem IIS log files older than 30 days
rem ------------------------------------------------------------
echo [4/7] Removing IIS log files older than 30 days...
if exist "%SystemDrive%\inetpub\logs\LogFiles" (
    forfiles /p "%SystemDrive%\inetpub\logs\LogFiles" /s /m *.log /d -30 /c "cmd /c del @path" >nul 2>&1
)
echo   Done.
echo.

rem ------------------------------------------------------------
rem Windows Event Logs - clear all
rem ------------------------------------------------------------
echo [5/7] Clearing Windows event logs...
for /f "tokens=*" %%L in ('wevtutil el') do wevtutil cl "%%L" >nul 2>&1
echo   Done.
echo.

rem ------------------------------------------------------------
rem Empty the Recycle Bin
rem ------------------------------------------------------------
echo [6/7] Emptying Recycle Bin...
rd /s /q "%SystemDrive%\$Recycle.Bin" >nul 2>&1
echo   Done.
echo.

rem ------------------------------------------------------------
rem Windows Disk Cleanup (silent, all categories)
rem ------------------------------------------------------------
echo [7/7] Running Windows Disk Cleanup...
rem Set all cleanup categories in the registry for sageset profile 99
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Active Setup Temp Folders" /v StateFlags0099 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Downloaded Program Files" /v StateFlags0099 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Internet Cache Files" /v StateFlags0099 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Old ChkDsk Files" /v StateFlags0099 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Previous Installations" /v StateFlags0099 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Recycle Bin" /v StateFlags0099 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Setup Log Files" /v StateFlags0099 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\System error memory dump files" /v StateFlags0099 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\System error minidump files" /v StateFlags0099 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Temporary Files" /v StateFlags0099 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Temporary Setup Files" /v StateFlags0099 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Thumbnail Cache" /v StateFlags0099 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Update Cleanup" /v StateFlags0099 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Windows Error Reporting Files" /v StateFlags0099 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Windows Upgrade Log Files" /v StateFlags0099 /t REG_DWORD /d 2 /f >nul 2>&1
cleanmgr /sagerun:99 >nul 2>&1
echo   Done.
echo.

echo ============================================================
echo Disk cleanup complete.
echo ============================================================
echo.

pause
