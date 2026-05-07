@echo off
rem ==============================================================
rem
rem Builds and publishes the ContensiveMcpServer to c:\git\contensive5\McpDeployment
rem
rem ==============================================================
cls

set PROJECT=%~dp0..\source\ContensiveMcpServer\ContensiveMcpServer.csproj
set OUTPUT=c:\git\contensive5\McpDeployment

@echo +++++++++++++++++++++++++++++
@echo Building ContensiveMcpServer
@echo Output: %OUTPUT%
@echo +++++++++++++++++++++++++++++

dotnet publish "%PROJECT%" -c Release -o "%OUTPUT%" --self-contained -r win-x64
if errorlevel 1 (
    @echo.
    @echo ERROR: Build failed.
    pause
    exit /b 1
)

@echo.
@echo Build succeeded. Published to %OUTPUT%
pause
