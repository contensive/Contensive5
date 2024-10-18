

echo ***** problem - must be run elevated, but elevating runs in system32 and the location of this install has the version number in it.

rem uninstall from current path
rem

msiexec.exe /x ContensiveConsole-Debug-x64.msi /qn

pause