
### download the current version

from Contensive.io/downloads

### Uninstall the curren version CLI/Task Service

- Stop the Contensive Task Service
- Uninstall the application Contensive Console
- Install ContensiveConsole-Debug-x64.msi
- copy the DefaultAspxSite.zip into c:\Program Files\Contensive
- Create or Verify the environment varialbe path to c:\Program Files\Contensive
- Upgrade the current applications. Open a command line and execute cc.exe -u
- When the upgrade is complete, upgrade each IIS site using iis web deploy, and the DefaultAspxSite.zip
- If migrating from the x86 to the x64 version, uninstall the windows service and reinstall the new service
-- sc delete "contensive task service"
-- from the c:\programFiles\Contensive folder, run c:\iwndows\MicrosoftDotnet\framework64\v4.0]installutil "TaskService.exe"
- Start the windows task service