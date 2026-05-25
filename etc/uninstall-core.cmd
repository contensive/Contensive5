@echo off
rem ============================================================
rem Uninstall Contensive .NET 9.0 Core components
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

powershell -ExecutionPolicy Bypass -File "%~dp0uninstall.ps1"

pause
