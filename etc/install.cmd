

echo ***** problem - must be run elevated, but elevating runs in system32 and the location of this install has the version number in it.

rem install from current path
rem

msiexec.exe /i ContensiveConsole-Debug-x64.msi /qn

xcopy DefaultAspxSite.zip "c:\program files\contensive\"

pause