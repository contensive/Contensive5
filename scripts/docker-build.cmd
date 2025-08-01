:: @echo off
::
:: ----------------------------------------------------------------------------------------------------
:: -- test if docker is runnin
:: 
docker info >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo Docker is not running. Please start Docker and try again.
    pause
    exit /b
)
::
:: ----------------------------------------------------------------------------------------------------
:: -- set variables
:: 
:: Get the date in YYYY-MM-DD format
for /f "tokens=2 delims==." %%A in ('"wmic os get localdatetime /value"') do set dt=%%A
:: Extract year, month, and day
set yyyy=%dt:~0,4%
set mm=%dt:~4,2%
set dd=%dt:~6,2%
set yymmdd=%yyyy:~2,2%%mm%%dd%
::
:: Get the current date and time using WMIC
for /f "tokens=2 delims==." %%A in ('wmic os get localdatetime /value') do set dt=%%A
:: Extract hour, minute, and second
set hh=%dt:~8,2%
set mm=%dt:~10,2%
set ss=%dt:~12,2%
set hhmmss=%hh%%mm%%ss%
::
:: setup and Display variables
set imageName=contensive-site
echo imageName %imageName%
::
set imageVersion=%yymmdd%-%hhmmss%
echo imageVersion %imageVersion%
::
set repoName=contensive-site-repo
echo repoName %repoName%

set containerDevName=ontensive-site-container
echo containerDevName %containerDevName%
::
set containerProName=contensive-site-container
echo containerProName %containerProName%
::
:: ----------------------------------------------------------------------------------------------------
echo .
echo any-key to     build image
pause::
:: 
cd C:\Git\Contensive5\source\iisDefaultSite
docker build -t %imageName%:%imageVersion% . 

:: pause
