@echo off
setlocal enabledelayedexpansion

set "nopause=0"
set "configuration=Debug"
set "scriptDir=%~dp0"

:parse_args
if "%~1"=="" goto end_parse
if /i "%~1"=="/nopause" (
    set "nopause=1"
    shift
    goto parse_args
)
if /i "%~1"=="/release" (
    set "configuration=Release"
    shift
    goto parse_args
)
shift
goto parse_args

:end_parse

set "buildScript=%scriptDir%build.ps1"

if not exist "%buildScript%" (
    echo ERROR: Build script not found: %buildScript%
    echo Script directory: %scriptDir%
    if "%nopause%"=="0" pause
    exit /b 1
)

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%buildScript%" -Configuration %configuration%
set "exitcode=%errorlevel%"

if not "%exitcode%"=="0" (
    echo.
    echo ========================================
    echo BUILD FAILED
    echo ========================================
)

if "%nopause%"=="0" pause
exit /b %exitcode%
