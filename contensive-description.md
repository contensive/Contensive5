# Contensive Description

## Contensive Application
Contensive is an web application that runs as a single IIS aspx application and creates a web api web application. 
- It supports an extensable module pattern for adding features called Add-ons.
- it creates a control panel environment for editing data and managing features. Database tables and fields are described in metadata called Content Definitions, stored in tables ccContent and ccFields

## Contensive Add-ons
- An addon is described by a record in the ccAggregateFunction table.
- Each addon record has a field that stores the DotNet Namespace and class of c# class with an execute() method that inherits an abstract base class that describes the signature.
- When the addon is executed, it calls the dotnet execute() method in the class designated by the addon.

## Description of the Contensive Editing Environment
- Contensive tracks database tables with in a table named 'ccTables'. Each record in the ccTables table represents a database table
- Contensive creates an editing interface based on metadata in the ccContent table. Each record in the ccContent table is called a Content Definition (cdef). It contains a foreign key to the cctable table which holds the name of the actual database table used. Each cdef records represents a group of records in that database table. A database table may hold records for one or more cdef.
- Each cdef record has a name that describes the group of records. For example, the table that holds data about users is the ccMembers table.
- The editing environment stores metadata about each field in the database in a table ccFields. the ccFields table has a foreign key to the ccContent table.

## Contensive feature installation
- Feature installation is described in a xml file called a collection.
- A collection file has Add-on nodes and data nodes. A data node stores a database record

## Contensive Coding
- To add code to the Contensive environment you add an add-on by creating a dotnet solution and adding a class that inherits the CPAddonBaseClass and implements the Execute() method
- The execute method includes one argument for the cp object which provides properties and methods to access the system.

## Contensive Portals
A portal is an environment created in the control panel that groups data, reports and tools with a common purpose, like meetings, forms, ecommerce, communications, SEO. etc.
- A portal is described by a record in the database table ccPortals
- A portal has Portal Features that are each descibed by a record in the ccPortalFeatures table
- A portal feature points to either an add-on or to a cdef. When the feature is execute it either displays the data, or executes the addon

## Portal Addon Patterns
- The portal system creates the navigation for portal features. There are 3 types of portal feature adds: List Pages, Edit Pages and Detail Pages
- An Edit page creates a series of edit input fields with captions to let a user edit data. To create an Edit Page, an addon creates an object with cp.addonUi.CreateLayoutBuilderList() and populates the pages elements in that object.
- A List page displays a list of records and is created by populating the properties of the object returned by cp.adminUI.createLayoutBuilderNameValue
- A Detail page displays data and uses cp.AdminUI.createLayoutBuilder()
