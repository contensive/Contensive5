
## Overview

> Use the CLI to create and maintain the server and applications

## Configure Server

> cc.exe --configure 
>
> Use the configure command to setup a new server. Before running the command, you should install the latest CLI. If the server  will include an http connection, like a web app or website, you also the default-iis app from Contensive downloads.
>
> After running the command, the server will be ready to add an application.

### Server Group

The server group is the one or more servers, that may include one or more applications. 
Enter a single word, no spaces or special characters.
You might use a word that represents the server and not the application, like jayDev.

### Secrets Manager (beta)

Answer no for production site.

### Production Server

A simple true or false that passes this parameter to the code environment.

### Local File System vs Remote File System

Reply yes and all application files, such as uploaded content or working files are stored locally on the server.

Reply no and files will be stored in an Amazon S3 bucket and the local file space is a mirror used for file operations. 
Remote files are implemented in three of the four file systems
- CdnFiles, a file storage location used for content, shared between the same application on different servers in the same server group. This file system is also mapped to http for online file availability
- privateFiles, a file storage location used for files that should not be shared with http, shared between the same application on different servers in the same server group. 
- wwwFiles, a filesystem mapped to the physical location of the www files, shared between applications in the same server group. Typically used to for application files, not content files.
- tempFiles, a local filesystem to store ephemeal files (not shared between ). The system automatically deletes tempFiles periodically.

### File Location

Best practice is to have a drive available for just content, drive-d.

### AWS Credentials

Create an aws user for the server group. All applications in the server group share this user by default. 
Individual applcations can override these credentials to isolate applications. 
Policies needed will depend on the configuration.
- S3 access to the bucket for file storage
- SQS access to the queue used for Email message return
- tbd

### AWS region

Enter the region for your application. us-east-1 by default.

### Enable Cloud Watch Logging (beta)

Answer no for production site. This enables sending nLog logging data to Cloud Watch.

### Sql Server Endpoint

For a local database, enter "(local)"

For a remote database like RDS, enter the endpoint for the database engine.

for example, "rds-jaydev.-----------.us-east-1.rds.amazonaws.com"

### Sql Server Credentails

The code accesses, and provides access to the db through the cp.db object, which uses this credential in the connection string.

### Cache Service (beta)

Answer local for development applications. For staging and production servers using mulitple servers, use remote cache.
The sysyem is currently transtioning from MCached to Redis and this transtion. Do not start new configurations in MCached.
