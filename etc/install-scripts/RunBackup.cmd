rem
rem --------------------------------------------------------
rem
rem c:\Program files\kma\BackupTools\ -- all backup files
rem c:\Program files\7-zip\ -- installed
rem C:\Program Files\Microsoft SQL Server\100\Tools\Binn\sqlcmd -- installed
rem FTPScript.txt - details about where the file will be stored
rem
rem SQL Express backup into a temp folder
rem 7-zip the temp folder, the inetpub folder and the contensive addons into a single zip
rem remove the temp folder
rem ftp the single file to another server
rem save several of the files locally
rem
rem This file and the SQLExpressBackup.sql file should be in c:\Program files\kma
rem edit SQLExpressBackup.sql and verify the e:\DbBackup folder set correctly
rem
rem --------------------------------------------------------


c:
cd "C:\Program Files (x86)\kma\BackupTools"

rem
rem --------------------------------------------------------
rem Data drive where the server content and backup data are
rem --------------------------------------------------------
rem

set localDrive=c:

rem
rem --------------------------------------------------------
rem Path to the SQL Server Express sqlcmd.exe file
rem --------------------------------------------------------
rem

REM set sqlcmdPath=c:\Program Files\Microsoft SQL Server\100\Tools\Binn\sqlcmd
set sqlcmdPath=C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd

rem
rem --------------------------------------------------------
rem verify folders needed
rem --------------------------------------------------------
rem

cd /D %localDrive%\

md DbBackup

rem
rem --------------------------------------------------------
rem SQL Express Backup
rem move all backups into the archive\db folder
rem backup todays files into the backup\db to be zipped
rem the .sql file has the destination path in it
rem --------------------------------------------------------
rem

:backupDb

cd /D %localDrive%\

"%sqlcmdPath%" -S .\SQLEXPRESS -i "C:\Program Files\Contensive\scripts\SQLExpressBackup.sql" -o d:\DbBackup\backup.log -b
rem "%sqlcmdPath%" -S LOCALHOST -i "C:\Program Files\Contensive\scripts\SQLExpressBackup.sql" -o d:\DbBackup\backup.log -b

rem
rem --------------------------------------------------------
rem copy everything from \inetpub\db into \Backup-Temp
rem	because 7zip can not copy some files if there are in use
rem --------------------------------------------------------
rem

