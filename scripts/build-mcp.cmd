@echo off
rem ==============================================================
rem
rem Builds and publishes the ContensiveMcpServer as self-contained .NET 9.
rem
rem Standalone: publishes to McpDeployment\ for manual deployment.
rem MSI build:  build.cmd publishes to source\Cli\bin\Debug\McpServer\
rem             which WiX harvests into the installer.
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
