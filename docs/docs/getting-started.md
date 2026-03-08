# Getting Started

This section describes the core concepts around how Contensive renders a document. The concepts will be accompanied by hands-on examples that will allow you to learn as you go. Get started here to learn how to configure a web server with Contensive and start implementing new features!
* * *
Optional: Create a Server
-------------------------
This is an optional section that will walk you through getting a server as well as installing an IIS Web Server and SQL Server Express. This new server will be used to host your Contensive application. Feel free to skip this section if you already have an environment with an IIS Web Server and SQL Server Express.
****Click below for instructions on getting a server:****
[How to Create an AWS Server for Contensive](http://contensive.io/start-here-for-how-to-create-an-aws-server-for-contensive)
Feel free to refer to the video if you get stuck.
* * *
Local Contensive Web Server Setup
---------------------------------
We will use a Contensive configuration tool to setup our IIS web server; the tool is executed via the command line. All files for the new Server Group will be contained locally. We will not worry about setting up a remote server for this example.
**Terms to know:**
*   Server Group - contains settings for resources used by any server(s) or application(s) that exist within the group.
**Tools needed:**
*   IIS Manager (should be installed by default on your server)
**Now let's use the configuration tool to create a Server Group and Web Application:**
*   We will configure our IIS web server via the command line.
*   This will result in the creation of our new Server Group.
*   We will create an application inside the Server Group.
*   This will also be carried out via the command line.
*   We will import the default Contensive site into your new application.
*   In doing so, you will be able to access the application from your browser.
****Click below for instructions to perform the tasks described above:****
[How to Configure the Server and Add a Web Application](http://contensive.io/how-to-setup-a-server-and-add-a-web-application)
Feel free to refer to the video if you get stuck.
* * *
Add-on Basics
-------------
This section is centered around gaining a basic understanding of what an Add-on is as well as how to create one. At the highest level, Contensive renders documents by simply executing Add-ons.
**Terms to know:**
*   Document - typically a document containing html code or JSON serialization.
*   Add-on - any feature created by a developer to be executed in an application.
*   Collection - Add-ons are organized into collections. Collections can contain multiple Add-ons that all work together to implement a singular feature.
**Tools needed:**
*   [Microsoft Visual Studio 2017](https://visualstudio.microsoft.com/vs/older-downloads/)
**Now let's create a simple Add-on that displays "Hello World!":**
*   For this example, our Add-on will be an Endpoint Add-on. An Endpoint Add-on is executed by hitting its specific URL.
*   An Endpoint Add-on is executed by hitting its specific URL.
*   The other type of Add-on, which we will cover in a later section, is called a Programmatic Add-on.
*   A Programmatic Add-on is placed directly on to a web page.
*   We will create an Add-on directly from your application's admin site.
*   This Add-on can be made to display "Hello world!" without having to leave the admin site.
*   The Add-on will be contained in an Add-on Collection that you will be tasked with creating.
*   We will then create a .NET C# class in Visual Studio.
*   This class will tell your Add-on to display the same message.
*   Creating .NET C# classes in Visual Studio will be your
default method of implementing more robust functionality to Add-ons.
**Click below for instructions to perform the tasks described above:**
[How to Create an Add-on and Add Visual Studio C# Code](http://contensive.io/how-to-create-an-add-on-and-add-visual-studio-c-code)
Feel free to refer to the video if you get stuck.
* * *
Add-ons are Organized into Collections
--------------------------------------
A collection is a grouping of add-ons and other resources used for a feature. An example of a feature might be a Blog. The Contensive Blog Collection includes all the add-ons, database tables, javascript files, css styles, etc. required for the Blog feature. The Blog collection can be installed on any website create using a Contensive application and the blog is ready to use.
Collections can also be exported from a site into a zip file download. You typically develop features on a development environment, export the collection and install it on your staging and/or production environment.
Create a Getting-Started collection, export it using the Add-on Manager and install it on another site.
Create an Add-on Collection Record with just a name.
Edit the helloworld add-on and add it to the new collection.
See that the add-on is now available in the navigation on the left.
Use the Add-on manager to export the collection zip file.
Upload the collection zip file to a different site.
Test that the helloworld add-on is installed and working.
