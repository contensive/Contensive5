
# Page Widget Pattern

## Overview
[Detailed explanation, history, why this pattern exists]
A Page Widget is an addon that can be drag-and-dropped on a web page of a Contensive Website using the Contensive Widget Tool.

This pattern is also referred to as a Design Block.

## Architecture
[In-depth architecture discussion]
A page widget has several components
- Layout File with html, styles and javascript
- A dotnet component
- A content definition used to hold settings. Each time the widget is dropped on a page, the system creates a new settings record.
- The Contensive edit system lets the admin user customize the widget by editing values of the settings record.
- The addon installation may create other database tables and field to support the functionality supported by the dotnet component.

## The Add-on
- A page widget is an addon that can be dropped on a page by a content manager
- To enable the drag and drop, set the field "Content" in teh addon
- set the namespace and class to the addon field "DotNetClass"
- set the field "category" to the category and subscategory (separated by a dot) used on the edit system
- the field "instanceSettingPrimaryContentId" is set to the Settings content definition

## The Layout
- An html layout file implementing Mustache templating. 
- The layout file should have a style tag at the top with all html styles needed for the layout
- The layout file should include a script tag at the end with all javascript needed for the layout
- The layout file javascript should be created so more than one of the same addon can appear on the same page. This is best implemented by prefixing unique javascript labels with a mustache generated string.
- The layout html file is created in the UI folder. During build it is copied to the project's collection and zipped into the collection file. It is included in the xml collection file in a resource node.
- The OnInstall addon for the addon collection should call CP.Layout.updateLayout() to populate the layout record from the file.

## The Dotnet Component
- The addon references a Dotnet+class that includes an Execute method that returns the rendered layout
- The dotnet execute implements the DesignBlockController.renderWidget() method
- renderWidget is a Generic method with 2 type argument, the Settings content and the view model
- the Settings content is a content definition for the table that describe each instance of the addon added to pages of the site.
- The View Model is a class that exposes public properties for each Mustache property in the layout. Those properties are populated based on the Settings record data, and any other state conditions. For example a page widget may create a form. If there are no requests the widget might set a Mustache property displayForm=true which displays the form in the html layuout. If there are requests, the page widget code may set displayForm=false and display form results.
- The arguments of the renderWidget() call include widgetName, layoutGuid, layoutName, layoutPathFilename, layoutBS5PathFilename.
- widgetName is the name that appears on the widget editor
- layoutGuid is used to lookup the layout needed. If it is missing, the layoutpathfilename file is read and a layout recore is created with the LayoutGuid, LayoutName, and LayoutpathFilename

## Steps to Scaffold a new Page Widget Addon

1) follow the pattern established in the example PageWidgetExample solution
2) Determine the Addon Collection in which this addon will be created. If one does not exists, first scaffold  a new Addon Collection repository with collection and new dotnet solution.
3) Determine The project in the dotnet solution. 
4) In the Addon Collection create a new addon node
    - Create a new Addon node
    - add a name that will be recognized by users when they add it to a page
    - ceate a new guid for the addon
    - create a dotnetclass node and use the dotnet projects namespace to the Addons folder
    - mark the node Content true to allow this addon to be dropped into content
5) In the dotnet project, the Addons folder create a new class that inherits CPBaseClass. 
