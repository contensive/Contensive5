How to Configure a Server and Add a Web Application

This guide walks you through using the Contensive CLI to configure your web server and create a web application. The required server environment is a Windows server (or laptop) with IIS Web Server and SQL Server Express installed.

1. Create a Windows Server with IIS. For AWS, see: [How to Create an Amazon Windows Server for Contensive](http://contensive.io/How-to-Create-an-Amazon-Windows-2019-Server-for-Contensive)

2. Install the .NET 9.0 Hosting Bundle from https://dotnet.microsoft.com/download/dotnet/9.0 (get the "Hosting Bundle" under Windows). After installing, restart IIS: `iisreset`

3. Extract the Contensive deployment zip (contensive.zip) to a temporary folder and run `install.cmd` as Administrator. This installs:
   - CLI (cc.exe) added to the system PATH
   - TaskService registered as a Windows service
   - WebApi package for new application deployments

4. Open a NEW command prompt as Administrator and configure the Server Group:

   _NOTE: A Server Group holds the settings for resources used by all applications in the group like connection strings for database servers, the file system, etc._

   run `cc --configure`

   You will be prompted for:
   - Server Group Name (e.g., "staging" or a customer name)
   - Production Server (y/n) — controls feature flags readable by applications
   - Local File System (y/n) — enter 'y' for local storage
   - Drive letter for data storage (e.g., 'c' or 'd')
   - SQL Server Endpoint (e.g., "(local)")
   - SQL Server userId and password
   - Cache type: (l)ocal or (m)emcached

5. Create an application in the Server Group.

   _NOTE: An application is the solution you are creating. It could be a website, a data processing service, etc._

   For a .NET Core WebApi site (recommended for all new sites):

   run `cc -n appName domainName`

   For a legacy .NET Framework ASPX site (existing sites only):

   run `cc -nf appName domainName`

   This creates:
   - A SQL Server database
   - Local file storage folders (www, files, private, temp)
   - An IIS site with the appropriate binaries deployed

6. Verify the Contensive Task Service is running. This service handles scheduled tasks and background processes.
   - Open Windows Services (services.msc)
   - Find "Contensive Task Service" — it should be Running with Automatic startup
   - If not running: `sc start "Contensive Task Service"`

7. To make the CLI easier to use from any directory, verify it is in the system PATH (the installer does this automatically):
   - From a new command prompt, run: `cc --version`
   - If not found, add `C:\Program Files\Contensive\Cli` to the system PATH

8. Create a root account login:
   - Run: `cc -a appName --addroot`
   - It will create an account that expires in 1 hour and display the password

9. Login to the admin site and configure initial settings:
   - Setup the Domain: settings > domains, add the current domain with Normal type, set the default landing page and default template
   - Setup Email: settings > Site Settings > Email tab
     - Set valid Admin Email Address and Default From Email Address
     - Check "Use AWS Simple Email Service (SES)" if using AWS
     - Configure Email Spam Footer for unsubscribe compliance
   - Security: settings > Site Settings > Security tab
     - Review password policy (min length, required characters, lockout period)
   - Website tab: upload a favicon if needed

COMMON ISSUES:
- Ports 80 and 443 must be open in your firewall/security group
- If cc.exe is not found, open a new command prompt (PATH updates require a new session)
- If the Task Service won't start, run `cc --configure` first, then check Windows Event Log
