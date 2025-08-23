# Contensive Structure

## Contensive Application

Contensive is an application environment that routes execute requests through an extensability interface to add-ons, and provides an api mechansm for those add-ons that provides hardware and service layer. Code can be run from a terminal/command line, task service/asdf, an applicaiton server like a webserver, or from any application that can include a dotnet nuget package.

A basic installation includes these typical use-cases:

- An application environment api that supports remote cache, files, database, email, etc for scale-out applications.
- A simple single-server installation pattern for scale-up applications and developer server configuration.
- A simple extensable module pattern for installing Add-ons.
- It supports four types of files: public/cdn, private, temporary, and access to the www root.
- A website interface that includes a route to a control panel interface with built-in database editing features, and the ability to run any addon.
- A website page routing/editing/creation platform.
- A web application api to build dotnet server-side endpoints.

## Contensive Add-ons
- An addon is described by a record in the ccAggregateFunction table.
- When the addon is executed it can manipulate the system in any way, and it can return either a string or an object that is serialize to json.
- An addon can be configured to execute from another addon, as an endpoint, as an asynchonous service task, or as a widget on a webpage that supports a drag-and-drop web management mechanism.
- An addon can return content from static text, an embedded script or from a reference to a dotnet executable project.

## Contensive Database Interface
- Contensive provides a programming interface through the cp.db object, with methods like cp.db.execute( sqlQuery )
- Contensive tracks database tables in a table named 'ccTables'. Each record in the ccTables table represents a database table.
- Contensive creates an editing interface in the control panel based on metadata in the ccContent table. Each record in the ccContent table is called a Content Definition (cdef). It contains a foreign key to the cctable table which holds the name of the actual database table referenced. Metadata for each field in the table is stored in the ccfields table.
- One table can store the records for one or more cdef metadata.
- A cdef can have a parent cdef. The parent cdef by definition includes all the records of its child cdef.
- Every record contains a group of standard fields:
 - id -- an integer unique value
 - name -- a string that represents the name of the record
 - dateAdded -- the date time when the record was created
 - createdBy -- the id of the ccmembers table of the user who created the record
 - modifiedDate -- the date time when the record was last editing in the control panel
 - modifiedBy -- the id of the ccmembers table of the user who last modified the record in the control panel
 - contentControlId -- the id of the cccontent table of the cdef metadata record that describes the data in this record

## Contensive feature installation
- Feature installation is described in a xml file called a collection.
- A collection file has Add-on nodes and data nodes. A data node stores a database record

## Contensive Dotnet Add-on Executable
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
