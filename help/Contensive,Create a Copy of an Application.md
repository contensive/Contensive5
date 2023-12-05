
reate a copy of an application. You might do this creating a dev site from a staging site

### Copy a local Application from one server to another

- Restore the source database
- Copy the four filesystem folders from the source application, cdnFiles, privateFiles, tempFiles, and wwwFiles
- copy the config file from the source server (typically d:\Contensive\config.json)
- update the config.json file to include the new database connection