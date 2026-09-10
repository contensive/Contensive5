@echo off
echo Stopping Contensive Task Service...
sc stop "contensive task service"

:waitstop
sc query "contensive task service" | find "STOPPED" >nul
if errorlevel 1 (
    timeout /t 1 /nobreak >nul
    goto waitstop
)

echo Service stopped. Starting...
sc start "contensive task service"


