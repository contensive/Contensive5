How to Configure the New Server and Add a Web Application

This article will guide you through using the Contensive configuration tool to configure your web server to utilize a web application that you will also create. The required server environment is a Windows server (or laptop) with an IIS Web Server and SQL Server Express installed.
1.  Create an AWS EC2 server instance with Windows Server 2019+ and IIS Web Server. For details see the following article: [How to Create an Amazon Windows 2019 Server for Contensive
](http://contensive.io/How-to-Create-an-Amazon-Windows-2019-Server-for-Contensive)
2.  Download and install the [Configuration Tool](https://s3.amazonaws.com/contensive/50/Setup.msi) from [http://contensive.io/downloads](http://contensive.io/downloads).
_NOTE: This installs two features, the command line configuration utility and a windows service for managing background tasks.
_
3.  Use the configuration tool to first configure a Server Group:_
NOTE: A Server Group holds the settings for resources used by all applications in the group like connection strings for Db servers, the file system, etc. A Server Group also holds multiple servers that execute the aforementioned applications. Numerous servers and applications can be contained in one Server Group._
Run command prompt as administrator. Navigate to c:\\Program Files (x86)\\kma\\contensive5. Run 'cc' with no arguments for a list of available commands. Run 'cc --status' (note the double minus) for the status of the current Server Group. You can redo the following instructions at any time to re-configure the Server Group that we are about to create.
run >cc --configure
\>> Enter the Server Group Name: (Name the Server Group anything you want like "staging" or the customer's name)
\>> Production Server (y/n)?: (This property can be read by applications in order to enable/disable certain features. Enter 'y' if your web app will be for public use, else 'n' if your web app is for testing purposes)
\>> Local File System (y/n)?: (Enter 'y' for this example)
\>> Enter the drive letter for data storage: (Self-explanatory, enter 'c' or 'd' for example)
\>> SQL Server Endpoint: (Enter "(local)" for this example)
\>> native sqlserver userId: (The user login for this server group. This login is the credentials used to log into SQL Server Manager.)
\>> native sqlserver password: (The password)
\>> Use (l)ocal cache or (m)emcached server?: (Enter 'l' for this example)
4.  Create an application in the Server Group.
_
NOTE: An application is the solution you are creating. It could be a website with 100 servers, a database program that processes data periodically, etc._
run >cc -n
You will be prompted for the following:
1.  Application Name_Enter a name for your application eg. myApp._
2.  Admin Route_
This will be the path you use for the administration site (where you edit data) if your path is online. The default is "admin"._
3.  Primary Domain Name_
Enter a domain name that will be used to reach the application (if applicable). You can change this or add many more later._
4.  App Files_
This is the file folder where the website files such as scripts will run, typically wwwRoot. Enter to accept the default path._
5.  cdn Files_
This is the file path where uploaded files will be stored if they will be available online (this is remote AWS S3 bucket in non-local Server Groups)._
6.  Private Files_
This is the file path where the application will store files that should not be mapped to the web server. Enter to accept the default path._
7.  Temp Files_
This is the file path used for files that have a temporary usage. Enter to accept the default path._
8.  Files url_
This is the prefix used within the website programming. _Enter to accept the default path._
_
5.  Download the [default sample website zip file](https://s3.amazonaws.com/contensive/50/iisDefaultSite.zip) from [http://contensive.io/downloads](http://contensive.io/downloads) and import it into the new IIS site using IIS Web Deploy 3.6+
1.  Verify in Server Roles and Features that IIS Management Tools are all installed. If not, be sure to uninstall Web Deployment before installing Web Deploy.
2.  Restart IIS Manager and the deploy option should now be available when you right-click your site.
3.  Finally, right-click on the site and select Deploy >> Import. Select the downloaded default website and complete the import.
6.  If this server will also be used to run background worker processes, verify installation of the Contensive Task Service and start it.
1.  Open Windows Services and if Contensive Task Service is not in the list, use the [windows install utility](https://learn.microsoft.com/en-us/dotnet/framework/tools/installutil-exe-installer-tool) to add the service
2.  1.  cd \\Program Files\\Contensive
2.  execute \\Windows\\Microsoft.NET\\Framework64\\v4.0.30319\\InstallUtil.exe TaskService.exe
3.  In Windows Services start the task service and change to automatic start.
7.  To make the command line utility easier to use, add to the system path
1.  from the command line, search for "environment variables" and click on "Edit the System Environment Variables"
2.  On the advanced tab, click the \[Environment Variables\] link
3.  In System Variables, double click on Path and add the path c:\\program files\\contensive\\
8.  Create a root account login
1.  from the command line, run the command > cc -a sitename --addroot
2.  It will create an account that expires in 1 hour and display the password
9.  Login to the admin site and setup important initial settings
1.  Setup the Domain.
On the top nav, click settings and click domains (or search for domains), and add the current domain with Normal type and add the default landing page and default template. Leave the domain "\*" and remove all not needed.
2.  Setup Email
1.  Go to settings in the upper-right, and click on Site Settings
2.  In the email tab
1.  set a valid email address for both Admin Email Address and Default From Email Address
2.  check the box \[Use AWS Simple Email Service (SES)\].
3.  For Email Spam Footer, enter what every email should include at the bottom to unsubscribe. Link the click with message. If you will include a custom unsubscribe in each email, leave this blank.
3.  In Security, verify the important settings for this site.
1.  Allow Plain Text password (false recommended)
2.  Clear Plain-Text on conversion (true recommended)
3.  Min length, required characters, block used passwords, and lockup period
4.  Website tab, Favicon - if the site will include a set of favicon files uploaded manually to the www root directory. Uploading a file here for a simpler quick solution
COMMON ISSUES:
1.  ports 80 and 443 must be added to the security group
