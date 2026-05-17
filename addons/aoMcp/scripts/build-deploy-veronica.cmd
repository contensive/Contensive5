@echo off
setlocal

@echo Build project and deploy to veronica

rem run the build
call "%~dp0build.cmd" /nopause
if not "%errorlevel%"=="0" (
    echo Build failed, skipping deploy.
    pause
    exit /b 1
)

rem deploy using cc command-line tool
set "collectionPath=%~dp0..\collections"
c:
cd "%collectionPath%"
cc -a veronica --installFile "aoMcp.zip"
cd "%~dp0"

pause
