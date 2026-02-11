
# Dashboard Widget Pattern

## Overview
A Dashboard Widget is a Contensive addon that can be added to the control panel dashboard.

## Architecture
[In-depth architecture discussion]
A dashboard widget is an addon that
- returns a json string serialized from a class in then namespace Contensive.Processor.Models
- this json string is used by the Widget dashboard to render the widget using layouts in the \ui\widgetdashboard folder
- there are 5 types of dashboard widgets
  1) Bar Chart Widget
  2) html widget - returning DashboardWidgetHtmlModel
  3) line chart widget - returning SampleLineChartWidget
  4) pie chart widget - returning SamplePieChartWidget
  5) number widget - UsersOnlineNumberWidget
- has the <DashboardWidget> node true in the <addon> node of the collection xml file

### features common to all dashboard widget types
- executed only from within the WidgetDashboard code
- returns an object from classes defined in nuget package Contensive.DbModels, defined withing the description of each widget type
- includes a refresh pattern that causes the dashboard to call a dashboard ajax endpoint, which executes the widget code and automatically repaints the widget.
- includes an optional filter options in a drop-down for users. When selected the dashboard refreshes the widget and includes the request argument "widgetFilter"
- widget code should save the widgetFilter for future use with user properties (cp.user.setProperty(), cp.user.getInteger(), etc)

### Number Widget
- displays a single number with the widget name at the top, a subhead under the number, and a description at the bottom.
- returns an object from class DashboardWidgetBarChartModel from the nuget package Contensive.DbModels
- returns obj.widgetType = Contensive.Processor.Models.WidgetTypeEnum.number

### Bar Chart  Widget
- displays a bar chart with the widget name at the top and a description at the bottom.
- returns an object from class DashboardWidgetBarChartModel. Addons find this class in the nuget package Contensive.DbModels
- returns obj.widgetType = Contensive.Processor.Models.WidgetTypeEnum.number

### HTML Widget
- Contensive.Processor.Addons.WidgetDashboardWidgets.SampleHtmlWidget

### Line Chart Widget
- Contensive.Processor.Addons.WidgetDashboardWidgets.SampleLineChartWidget

### Pie Chart Widget
- Contensive.Processor.Addons.WidgetDashboardWidgets.SamplePieChartWidget

## Example Addons
All these examples are in this repository. The collection file /source/Processor/aoBase51.xml installs these examples.

### Pages To Review Dashboard Number Widget
- number widgets display a single number with a subhead under the number and a description at the bottom.
- namespace:  Contensive.Processor.Addons.WidgetDashboardWidgets.PagesToReviewWidget
- number widgets use the layout in \ui\widgetdashboard\DashboardWidgetNumberLayout.html

### Pages To Review Dashboard Widget
- Contensive.Processor.Addons.WidgetDashboardWidgets.SampleBarChartWidget

### Pages To Review Dashboard Widget
- Contensive.Processor.Addons.WidgetDashboardWidgets.SampleHtmlWidget

### Pages To Review Dashboard Widget
- Contensive.Processor.Addons.WidgetDashboardWidgets.SampleLineChartWidget

### Pages To Review Dashboard Widget
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
