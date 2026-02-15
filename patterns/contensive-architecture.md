
# Contensive Architecture

## Overview
[Detailed explanation, history, why this pattern exists]
The Contensive platform creates an extensabilty framework for managing and executing addons in several context, including websites, web applications, command line, and background tasks (windows services)

## Contensive Collections
A collection is a group of features that are installed and maintained together. For example Ecommerce is a collection that includes account management features, reports, and an online catalog.

## Contensive Features
A Feature is a single funtionality. For example a SiteMap is a feature that can be dropped on a page and might be composed of multiple addons. Many features might be installed and maintained together in one collection. For example to create a sitemap that can be drag-and-dropped on a Contensive website, you would first create a Page-Widget addon that renders the html with javascript and styles for the page, and would also process any form submissions. But it might use a second addon that is a Remote-Method addon which responds to an endpoint and would return the list of pages from the server used in the map.

## Contensive Addons
An Addon is a pattern that creates an executable function on a Contensive website. 

There are several contexts in which an addon can be executed
1) Background Tasks -- An addon can be executed on a schedules bases in a background process
2) Page Widget -- Page Widget addons are added to website pages and are executed when the page is rendered
3) Remote Methods -- Addons marked as remote methods are executed when their addon name matches the endpoint in an http request
4) Dependency -- if an addon is marked as a dependency for another addon, it is executed once before the dependent addon.
5) An addon can be marked to be included 
6) Dashboard Widget -- The addon is used on the Control Panel dashboard

When an addon is executed is has access to modify the application and it can return a string that is added to the current context.

An Addon is composed of the database fields in the ccAggregateFunctions table as defined in the Addon Collection file aoBase51.xml. The CDEF (content definition) node named Add-ons defines and describes each field.

## Architecture
[In-depth architecture discussion]

The Contensive software executes Add-ons. 

A Contensive installation (one server) is in one Contensive Node and one Node there can contain one or more Contensive applications. One Contensive Node can shared with multiple installations (each sharing the same applications)

A Contensive installation 
- uses a Sql Server database which may be local or remote.
- can be stand-alone with all content files stored locally on the server
- can be stand-alone with content files stored in an AWS S3 bucket
- can include multiple server installations, each sharing the same database and content files.

In a Contensive installation, you can create one or more Contensive Applcations. Each application has access to 
1) A sql server database built with the command >cc -new
2) A file system named cdnFiles that is shared with all other instances of this Contensive node, and publically in an https source
3) A file system named tempFiles that is available only on the local server and are automatically deleted periocially
4) A file system named wwwRoot that is shared with all other instances of this Contensive node and shares file in the webserver
5) a file system named privateFiles that is shared with all other instances of this Contensive node, but is not shared publically.

There are three primary Contensive execution environments.
1) The command line tool cc.exe with access to all applications in the node
2) The Windows Service TaskService.exe  with access to all applications in the node
3) An ASPX website created with the IIS deployment file DefaultAspxSite.zip that has access to only one application in the node.

Contensive Command line and windows servcie are installed with the installation file ContensiveConsole-Debug-x64.msi. After installation the command >cc -u is run to upgrade existing applications. 

A Contensive website is created by importing the DefaultAspxSite.zip file into IIS. The website connects to the local Contensive node to a Contensive application with the same name as the IIS site. A Contensive website has a public website implemented with the Addon PageManager. The Add-on 'Admin Site' creates a Control Panel availabe on the endpoint /controlpanel and is used to manage the site. Public web pages are created from Page Templates and Content Pages, accessable by endpoints defined in the Page Content record.

An Addon Collection is a zip file that includes one XML Addon Collection file plus all files needed for the execution of the Addons included in the collection. The XML addon Collection file is controlled  with the public file XML Style Definitions https://contensive.s3.amazonaws.com/xsd/Collection.xsd

Addon Collections are installed in one of serveral methods
1) A Collection file can be installed in one ore more applicaions using the command line tool, cc.exe
2) A Collection file can be installed from the Control Panel using an Add-on called Add-on Manager.
3) Addon Collection Files that are used commonly are stored in a shared online the Collection Library. These collections can be installed from the command line tool and from the Add-on Manager.

An Addon Collection can specify one the addons as the OnInstall addon, which then automatically executes that addon when installation is complete. The OnInstall addon is used to 
- migrate data
- install layout files. When a layout file is included as a resource node, the installation automatically copies the file to the destination application, but the OnInstall must call CP.Layout.updateLayout() to update the database Layout record.

## Content Definitions
A content definition is metadata for a database table and its fields. This metadata is used to create the database table during installation and is used to create the Contensive Control Panel edit screen and data list pages for the data.

The content definition "content" defines all the content definitions in the application

## Development Folder Structure

One Addon Collection contains the installable features from one DotNet project. A solution might contain multiple projects, each of which would have its own addon collection zip file.

Each solution and its project and collections are stored in one Git repository.

This is the typical development folder structure

- Collections -- contains a folder for each addon collection, named the same as the collection
- Scripts -- contains automation scripts
- Server -- contains the dotnet solution, with a subfolder for each dotnet project in the solution
- UI -- contains the html layouts and html page templates installed with the collections in this repo
- HelpFiles - contains the markdown help documents installed with this collection


## Full Examples
[Multiple complete examples with explanations]