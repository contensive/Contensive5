
# Dashboard Widget Pattern

> All patterns and API reference: [Patterns Index](https://raw.githubusercontent.com/contensive/Contensive5/refs/heads/master/patterns/index.md)

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
- includes optional filter dropdowns for users (see Filters section below)
- widget code should save filter values for future use with user properties (cp.user.setProperty(), cp.user.getInteger(), etc)

### Filters

Dashboard widgets support two filter patterns: a **single filter** (legacy) and **multiple filters**.

#### Single Filter (legacy)

For widgets with one filter dropdown, populate the `filterOptions` list on the model. The selected value is passed to the addon as `cp.Doc.GetText("widgetFilter")`.

```csharp
return new DashboardWidgetBarChartModel() {
    widgetName = "Page Views",
    filterOptions = [
        new() { filterCaption = "1 Day", filterValue = "1d", filterActive = (period == "1d") },
        new() { filterCaption = "1 Week", filterValue = "1w", filterActive = (period == "1w") },
        new() { filterCaption = "1 Month", filterValue = "1m", filterActive = (period == "1m") }
    ],
    // ... chart data
};
```

Reading the filter value in the addon:
```csharp
string period = cp.Doc.GetText("widgetFilter");
```

#### Multiple Filters

For widgets that need multiple independent filter dropdowns (e.g., a date period filter AND a campaign filter), populate the `filters` list with `DashboardWidgetFilterGroup` entries. Each group renders as its own dropdown in the widget header.

Each filter group has:
- `filterName` — unique key used to identify this filter (used as the suffix in `cp.Doc` property names)
- `filterLabel` — display label shown on the dropdown button (leave empty for icon-only)
- `options` — list of `DashboardWidgetBaseModel_FilterOptions` for this filter

```csharp
public override object Execute(CPBaseClass cp) {
    string widgetId = cp.Doc.GetText("widgetId");
    //
    // -- read each named filter value
    string period = cp.Doc.GetText("widgetFilter_period");
    if (string.IsNullOrEmpty(period)) { period = "1w"; }
    string campaign = cp.Doc.GetText("widgetFilter_campaign");
    if (string.IsNullOrEmpty(campaign)) { campaign = "all"; }
    //
    // -- build campaign options from database
    var campaignOptions = new List<DashboardWidgetBaseModel_FilterOptions> {
        new() { filterCaption = "All Campaigns", filterValue = "all", filterActive = (campaign == "all") }
    };
    using var dt = cp.Db.ExecuteQuery("select distinct utmCampaign from ccUtmLog where utmCampaign is not null order by utmCampaign");
    foreach (System.Data.DataRow row in dt.Rows) {
        string name = cp.Utils.EncodeText(row[0]);
        campaignOptions.Add(new() { filterCaption = name, filterValue = name, filterActive = (campaign == name) });
    }
    //
    // -- return the model with multiple filters
    return new DashboardWidgetBarChartModel() {
        widgetName = "UTM Traffic",
        width = 2,
        filters = [
            new DashboardWidgetFilterGroup {
                filterName = "period",
                filterLabel = "Period",
                options = [
                    new() { filterCaption = "1 Day", filterValue = "1d", filterActive = (period == "1d") },
                    new() { filterCaption = "1 Week", filterValue = "1w", filterActive = (period == "1w") },
                    new() { filterCaption = "1 Month", filterValue = "1m", filterActive = (period == "1m") }
                ]
            },
            new DashboardWidgetFilterGroup {
                filterName = "campaign",
                filterLabel = "Campaign",
                options = campaignOptions
            }
        ],
        dataLabels = labels,
        dataSets = dataSets
    };
}
```

Key differences from single-filter:
- Use `filters` instead of `filterOptions`
- Read values with `cp.Doc.GetText($"widgetFilter_{filterName}")` instead of `cp.Doc.GetText("widgetFilter")`
- Each filter group renders as a separate dropdown with its own label
- The legacy `cp.Doc.GetText("widgetFilter")` still works and returns the most recently changed filter value, so existing single-filter widgets require no changes

### Number Widget
- displays a single number with the widget name at the top, a subhead under the number, and a description at the bottom.
- returns an object from class DashboardWidgetBarChartModel from the nuget package Contensive.DbModels
- returns obj.widgetType = Contensive.Processor.Models.WidgetTypeEnum.number
- reference the pattern in example 

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

## Including a Dashboard Widget in a Collection

A collection installs a dashboard widget by including an addon node with the element `<DashboardWidget>Yes</DashboardWidget>`. This makes the dashboard widget available for users to add to the control panel dashboard.

To include the widget on a portal overview page (the dashboard within the portal), create a Portal Feature data-record in the installation collection. This associates the widget addon with a specific portal so it appears on that portal's dashboard automatically.

```xml
<!-- Step 1: Define the addon with DashboardWidget enabled -->
<Addon name="My Widget" guid="{widget-addon-guid}">
    <DotNetClass namespace="MyNamespace.Addons" class="MyWidgetAddon"/>
    <DashboardWidget>Yes</DashboardWidget>
</Addon>

<!-- Step 2: Create a Portal Feature to include it on a portal dashboard -->
<PortalFeature name="My Widget" guid="{portal-feature-guid}">
    <AddonGuid>{widget-addon-guid}</AddonGuid>
    <PortalGuid>{target-portal-guid}</PortalGuid>
</PortalFeature>
```

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
