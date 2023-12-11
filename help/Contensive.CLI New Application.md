
The command line tool (cc.exe) performs maintenance tasks, as well as code execution for any of the applications configured in the application server group.

### Create a New Application with the CLI

cc.exe --newapp

Use the newapp command to create a new application on the server. Before running the command, you should install the latest CLI. 

After running the command, the application can execute features (add-ons).

### Application Name

A new application will be created with this name. For a website, you might typically used the domain name without the .com extension.

### Admin Route

The default Admin is typical

### Primary Domain Name

The primary domain name used for the site. 

### File System

The folders where files will be stored for the application
- app files, the physical location of the the www folder
- cdn files, the location where content files are stored on the server that will be mapped to the http response
- private files, the location where content files are stored that are not mapped to the http response
- temp files, a location where the site can store temporary files.
- files URL, a prefix added to the url location of files in the cdn files when creating urls for the site.
