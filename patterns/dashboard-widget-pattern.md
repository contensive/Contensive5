
# Dashboard Widget Pattern

## Overview
A Dashboard Widget is a Contensive addon that can be added to the control panel dashboard.

This pattern is also referred to as a Design Block.

## Architecture
[In-depth architecture discussion]
A dashboard widget is an addon that
- returns a json string serialized from a class in then namespace Contensive.Processor.Models
- there are 5 types of dashboard widgets defined by the data structure they return
  1) Bar Chart Widget - returning DashboardWidgetBarChartModel
  2) html widget - returning DashboardWidgetHtmlModel
  3) line charge widget - returning SampleLineChartWidget
  4) pie chart widget - returning SamplePieChartWidget
  5) number widgetr - UsersOnlineNumberWidget
- has the <DashboardWidget> node true in the <addon> node of the collection xml file

## Example Addons
- Contensive.Processor.Addons.WidgetDashboardWidgets.PagesToReviewWidget
- Contensive.Processor.Addons.WidgetDashboardWidgets.SampleBarChartWidget
- Contensive.Processor.Addons.WidgetDashboardWidgets.SampleHtmlWidget
- Contensive.Processor.Addons.WidgetDashboardWidgets.SampleLineChartWidget
- Contensive.Processor.Addons.WidgetDashboardWidgets.SamplePieChartWidget
- 

## The Dotnet Component
- The addon references a Dotnet+class that includes an Execute method that returns the rendered layout
- The dotnet execute implements the DesignBlockController.renderWidget() method
- renderWidget is a Generic method with 2 type argument, the Settings content and the view model
- the Settings content is a content definition for the table that describe each instance of the addon added to pages of the site.
- The View Model is a class that exposes public properties for each Mustache property in the layout. Those properties are populated based on the Settings record data, and any other state conditions. For example a page widget may create a form. If there are no requests the widget might set a Mustache property displayForm=true which displays the form in the html layuout. If there are requests, the page widget code may set displayForm=false and display form results.
- The arguments of the renderWidget() call include widgetName, layoutGuid, layoutName, layoutPathFilename, layoutBS5PathFilename.
- widgetName is the name that appears on the widget editor
- layoutGuid is used to lookup the layout needed. If it is missing, the layoutpathfilename file is read and a layout recore is created with the LayoutGuid, LayoutName, and LayoutpathFilename

## Steps to Scaffold a new Dashboard Widget Addon

1) Follow the pattern established in the example addons
2) Determine the Addon Collection in which this addon will be created. If one does not exist, first scaffold a new Addon Collection repository with collection and new dotnet solution.
3) Determine the project in the dotnet solution.
4) **In the Addon Collection XML file, create a new addon node:**
    - Create a new `<Addon>` node
    - Add a name that will be recognized by users when they add it to the dashboard
    - Create a new GUID for the addon
    - Create a `<DotNetClass>` node and use the dotnet project's namespace to the Addons folder
    - **REQUIRED: Add `<DashboardWidget>true</DashboardWidget>` element** - This marks the addon as a dashboard widget and makes it available in the dashboard widget selector
5) In the dotnet project, the Addons folder create a new class that inherits `CPAddonBaseClass`. 
