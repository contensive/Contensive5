:: ==============================================================
::
:: Must be run from the projects git\project\scripts folder - everything is relative
:: run >build [versionNumber]
::
@echo off
cls
 
@echo +++++++++++++++++++++++++++++
@echo Build Sprint-8
@echo .
@echo IF NEEDED -- run contensive build and install now.
@echo .
@echo - cd C:\Git\Contensive5\scripts
@echo - call "jay-build.cmd"
@echo .
@echo - stop Contensive Task Service
@echo - uninstall Contensive CLI
@echo - run Contensive CLI installer
@echo - start Contensive Task Service
@echo - run cc -a veronica -u
@echo - iis install aspx site
@echo .
@echo when ready, hit any key to continue the install
@echo +++++++++++++++++++++++++++++
@echo on
pause
::
@echo off
@echo +++++++++++++++++++++++++++++
@echo ecommerce
c:
cd C:\git\aoecommerce\scripts
call "build-c#-install - veronica.cmd"
::
@echo off
@echo +++++++++++++++++++++++++++++
@echo form-wizard
c:
cd C:\Git\aoFormWizard\scripts
call "build-veronica.cmd"
::
@echo off
@echo +++++++++++++++++++++++++++++
@echo addon-manager
c:
cd C:\Git\aoAddonManager\scripts
call "build-veronica.cmd"
::
@echo off
@echo +++++++++++++++++++++++++++++
@echo meeting-manager
c:
cd C:\Git\aoMeetingManager\scripts
call "build-veronica.cmd"
::
@echo off
@echo +++++++++++++++++++++++++++++
@echo distance-learning
c:
cd C:\Git\aoDistanceLearning\scripts
call "build-veronica.cmd"
::
@echo off
@echo +++++++++++++++++++++++++++++
@echo contact-manager
c:
cd C:\Git\aoContactManager\scripts
call "build-veronica.cmd"
::
@echo off
@echo +++++++++++++++++++++++++++++
@echo tools
c:
cd C:\Git\aoTools\scripts
call "build-veronica.cmd"
::
@echo off
@echo +++++++++++++++++++++++++++++
@echo html-import
c:
cd C:\Git\aoHtmlImport\scripts
call "build-veronica.cmd"
::
@echo off
@echo +++++++++++++++++++++++++++++
@echo import-wizard
c:
cd C:\Git\aoImportWizard\scripts
call "build-veronica.cmd"



@echo on
:: ==============================================================
::
:: done
::

pause
