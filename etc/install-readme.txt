

With the website running

- stop the windows service -- Contensive Task Service
- uninstall the Contensive Console app
- install the new Contensive build - ContensiveConsole-Debug-x64.msi
- if the previous version was in "program files (x86)", it was 32-bit. You have to remove the old service.
	- sc stop "contensive task service"
	- sc delete "contensive task service"
	- SC CREATE "Contensive Task Service" binpath="c:\program files\contensive\taskservice.exe"
	- sc config "Contensive task service" start=auto
	- sc start "contensive task service"
- upgrade the application(s) with
	- cc -u
- when that is complete, go to iis and install the new contensive application
	- open iis
	- click on the site(s)
	- on the right, click Import Application
	- select the package -- C:\...\deployments\c5\...version...\DefaultAspxSite.zip
