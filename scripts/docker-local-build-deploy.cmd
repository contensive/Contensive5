@echo off
::
:: ----------------------------------------------------------------------------------------------------
:: -- set variables
::
set awsAccountId=037688919340
echo awsAccountId %awsAccountId%
::
:: ----------------------------------------------------------------------------------------------------
:: -- build
:: 
call docker-build.cmd
::
:: ----------------------------------------------------------------------------------------------------
echo any-key to     run image
pause
:: 
docker stop %containerDevName%
docker rm  %containerDevName%
docker run -d -v d:/weave:/var/learninglink --name %containerDevName% -p 8080:8080 %imageName%:%imageVersion%

echo any-key to exit
pause
