
There are three steps to creating a new site on a server, installing the software, configuring the server, and setting up the site(s) on that server. These instructions assume this installation is a single server with local cache and Sql Server.

### Installing the software 

Go to Contensive.io and download 
- the Command Line Interface (CLI) installation
- the IIS default site

Install the CLI command line interface program
- Run the CLI installation program which creates a \Program Files\Contensive folder.

Install the windows service which executes background tasks for the application
- for more details see https://learn.microsoft.com/en-us/dotnet/framework/windows-services/how-to-install-and-uninstall-services
- open a command prompt and go to c:\program files\contensive
- run> installutil taskservice.exe

Install Sql Server Express.
- add credentials used by the server to access the sites

### Configuring a server 

- open a command prompt and run cc --configure
- server group = set to the name of the server. This can be used to manage muliple servers working in the same group
- production Server = this is a flag available to the software. Answer yes if this is a produciton server, else no
- AWS credetials = Go to your aws account and create an AMI user for this machine. Create a key and secret key.
- Secrets Manager = no
- AWS region = the region you want to use. Typically us-east-1
- local file system = yes
- file storage location = d
- cloudwatch logging = no
- sql endpoint = (local)
- sql credentials = credentials used by your site
- cache service = local

### Setup a new site on a configured server

- RDP to the server
- run cc -n (sitename)
- setup the domain for the site. In this example we will log into AWS, go to Route53, and setup an A-record (sitename).sitefpo.com
- go back to RDS
- go to d:\contensive\ and edit the config.json. (use the plug-in JSTool to JSFormat), find the site you added and change the app's "domainList" to the (sitename).sitefpo.com
- open IIS which by now has the new site. update binding to just be http (sitename).sitefpo.com
- in IIS, enable the url-rewrite filters to remove-slash and forward http to https
- in a command window at c:/program files x86\win-acme, run wacs and add the https to the one binding on this iis site
- in a command prompt, run cc -a (sitename) --addroot
- in a browser, log into the admin site with root/C0ntensive!
- edit the root profile and make it yours (name, username, password, clear the password-expires-date)
- add username and password for others who will work on the site, and share it with them
- Go to domains and only leave (sitename).sitefpo.com , set home page and set the template to the full-width template