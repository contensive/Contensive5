The command line tool (cc.exe) performs maintenance tasks, as well as code execution for any of the applications configured in the application server group.

### Configure a Server with the CLI

cc.exe --configure 

Use the configure command to setup a new server. Before running the command, you should install the latest CLI. If the server  will include an http connection, like a web app or website, you also the default-iis app from Contensive downloads.
After running the command, the server will be ready to add an application.

### Server Group

The server group is the name of this server, and it may include one or more applications. 
Enter a single word, no spaces or special characters.
You might use a word that represents the server and not the application, like jayDev.

### Secrets Manager (beta)

Answer no if you are not beta testing.

### Production Server

A simple true or false that is passed to the code environment.

### Local File System vs Remote File System

For a single server instalation, answer local file system.

A local file systems saves uploaded files to the local server. A remote file system uses the local server as a mirror of a remote file storage area on AWS S3. There are four file systems:

- cdnFiles, a file storage location used for content that can be accessed online. This file system is also mapped to http for online file availability

- privateFiles, a file storage location used for files that should not be shared with http. 

- wwwFiles, a filesystem mapped to the physical location of the www files. Typically used to for application files, not content files.

- tempFiles, a local filesystem to store ephemeral files (not shared between ). The system automatically deletes tempFiles periodically.

### File Location

Best practice is to have a drive available for just content, drive-d.

### AWS Credentials

Create an aws user for the server group. All applications in the server group share this user by default. 
Individual applcations can override these credentials to isolate applications. 
Policies needed will depend on the configuration.
- SES if the system will send emails
- SQS and SNS if the system will process blocked emails
- S3 if the system will need to access an S3 bucket

### AWS region

Enter the region for your application. us-east-1 by default.

### Enable Cloud Watch Logging (beta)

Answer no for non-beta sites. This enables nLog logging data to Cloud Watch.

### Sql Server Endpoint

For a local database, enter "(local)"

For a remote database like RDS, enter the endpoint for the database engine.
for example, "rds-jaydev.-----------.us-east-1.rds.amazonaws.com"

### Sql Server Credentails

Credential used to access the Sql Server.

### Cache Service (beta)

Answer local for non-beta sites. Remote cache expects a Redis server
