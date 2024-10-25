

With the website running

- stop the windows service -- Contensive Task Service
- uninstall the Contensive Console app
- install the new Contensive build - ContensiveConsole-Debug-x64.msi
- if the previous version was in "program files (x86)", it was 32-bit. You have to remove the old service.
	- sc stop "contensive task service"
	- sc delete "contensive task service"
	- SC CREATE "Contensive Task Service" binpath="c:\program files\contensive\taskservice.exe"
	- sc config "Contensive task service" start=auto
	- update environemnt variatible path
		- search windows for environment variables, click open
		- System Properties opends -- click Environment Variables
		- System Variables - click Path, Edit
		- change 'C:\Program Files (x86)\Contensive' to C:\Program Files\Contensive
		- OK, OK, OK
		- start a new command prompt
- upgrade the application(s) with
	- cc -u
- start task service
	- from GUI or 
	- sc start "contensive task service"
- when that is complete, go to iis and install the new contensive application
	- open iis
	- click on the site(s)
	- on the right, click Import Application
	- select the package -- C:\...\deployments\c5\...version...\DefaultAspxSite.zip
- upgrade manual changes
	- if this application was created before the introduction of WebAppSettings.config and WebRewrite.config
		- rename WebAppSettings-Sample.config -> WebAppSettings.config
		- rename WebRewrite-Sample.config -> WebRewrite.config
	- doctype
		Settings -> Website -> set doctype to "<!DOCTYPE html>"
	- domains list is full of noise
		- settings -> domains
	- older buildds saved credentials in the db, or sent email with smtp, newer builds send through aws
		- check d:\contensive\config.json
			- valid awsAccessKey
			- valid awsSecretAccessKey
			- aws user (role later) has access to SES and SQS, SNS
	- bootstrap 4 vs 5.
		- older sites will default to boostrap 5. if the html is bootstrap 4, go to settings -> website and change to 4, then have it upgraded
	
	
