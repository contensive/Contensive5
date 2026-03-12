# Contensive Examples

Working code examples for Contensive5 addon development. Each example demonstrates a specific pattern with real, compilable code.

## Minimal Examples

- [SampleAddonClass.cs](SampleAddonClass.cs) — simplest possible addon; inherits `AddonBaseClass`, returns "Hello World" from `Execute()`
- [DesignBlockSampleClass.cs](DesignBlockSampleClass.cs) — basic design block with settings model, view model, Mustache rendering, and edit wrapper
- [SampleRemoteClass.cs](SampleRemoteClass.cs) — remote method addon returning JSON; shows request parsing (`cp.Doc.GetText`, `cp.JSON.Deserialize`), response formatting, and error handling

## Full Project Examples

### [DashboardWidgetExample](DashboardWidgetExample/)

Dashboard widgets for the admin control panel. Covers Number, Pie Chart, HTML, Line Chart, and Bar Chart widget types with periodic refresh and click-forwarding.

**Pattern:** [Dashboard Widget Pattern](../patterns/dashboard-widget-pattern.md)

### [LayoutBuilderListExample](LayoutBuilderListExample/)

Paginated, searchable, sortable data table built with `cp.AdminUI.CreateLayoutBuilderList()`. Demonstrates column configuration, pagination (pageNumber/pageSize with SQL offset/fetch), search filtering, sorting, row/cell population, and form button integration.

**Key API:** `cp.AdminUI.CreateLayoutBuilderList()`
**Pattern:** [AdminUI Pattern](../patterns/adminui-pattern.md)

### [LayoutBuilderNameValueExample](LayoutBuilderNameValueExample/)

Form-based UI with name-value pair layout built with `cp.AdminUI.CreateLayoutBuilderNameValue()`. Demonstrates multi-step wizard workflow, form submission handling, hidden fields for state preservation, and success/error messaging. Includes a Stripe checkout integration example.

**Key API:** `cp.AdminUI.CreateLayoutBuilderNameValue()`
**Pattern:** [AdminUI Pattern](../patterns/adminui-pattern.md)

### [PageWidgetExample](PageWidgetExample/)

Page widget with database-backed settings, view model, and Mustache template rendering. Demonstrates `SettingsBaseModel` inheritance, image handling with aspect ratio management, Bootstrap 5 styling, and `DesignBlockController.renderWidget<>()`.

**Pattern:** [Page Widget Pattern](../patterns/page-widget-pattern.md), [Layout Design Pattern](../patterns/layout-design-pattern.md)

## Key APIs Demonstrated

| API | Example |
|-----|---------|
| `AddonBaseClass` / `Execute()` | All examples |
| `cp.AdminUI.CreateLayoutBuilderList()` | LayoutBuilderListExample |
| `cp.AdminUI.CreateLayoutBuilderNameValue()` | LayoutBuilderNameValueExample |
| `DbBaseModel` / `createList<T>()` | SampleRemoteClass, PageWidgetExample |
| `cp.Mustache.Render()` | DesignBlockSampleClass, PageWidgetExample |
| `cp.JSON.Deserialize<T>()` / `cp.Doc.GetText()` | SampleRemoteClass |
| `cp.Content.GetEditWrapper()` | DesignBlockSampleClass |
| `cp.Site.ErrorReport(ex)` | All examples |
