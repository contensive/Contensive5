
# AdminUI Pattern

The adminUI pattern is used to create tools and reports that display in the contensive control panel with a consistent look and feel.

## Overview

Admin UI is implemented with LayoutBuilder classes that create the html for common Admin UI cases.

LayoutBuilder classes are created using one of the following factory methods, explained later
- cp.AdminUI.CreateLayoutBuilder
- cp.AdminUI.CreateLayoutBuilderList
- cp.AdminUI.CreateLayoutBuilderNameValue
- cp.AdminUI.CreateLayoutBuilderTabbedBody
- cp.AdminUI.CreateLayoutBuilderToolForm
- cp.AdminUI.CreateLayoutBuilderTwoColumnLeft
- cp.AdminUI.CreateLayoutBuilderTwoColumnRight

The primary adminUI code workflow is
- the client program creates a layoutbuilder instance by calling one of the cp.AdminUI methods
- the client populates its properties
- calls the getHtml method and returns its response. it is imporatant that the client program return the unmodified complete response from the the getHtml method.

The resulting string includes the entire view, including possible pagination, filters, refresh etc. If those features require ajax data, the layout build handles this by
- calling a layoutbuilder endpoint
- the endpoint executes the client program (which initially called the cpAdminUI interface)
- the LayoutBuilder then returns in the getHtml() methods the expected response for the ajac call, which the client program should return without modification.

These LayoutBuilder helpers are created internally with the Mustache Layout Pattern

### Adding Filters to reports

Filters allow users to narrow down report data. They appear in a filter panel on the layout and persist across page loads within a visit. The filter system has three parts: reading filter values, adding filter UI inputs, and managing active filter indicators.

#### Filter Groups

Filters are organized into groups. Call `addFilterGroup(caption)` to create a new group before adding filter inputs. If you add a filter without creating a group first, a default group with no caption is created automatically.

```csharp
layoutBuilder.addFilterGroup("User Filters");
```

#### Reading Filter Values (getFilter methods)

Use these methods to read the current value of a filter. They incorporate both the current request and visit state, so filters persist across page loads within a user's visit. Each takes a `filterHtmlName` (matching the html name used when adding the filter) and a `viewName` (a unique name for this view/form, so filters on different pages don't conflict).

- **getFilterBoolean(filterHtmlName, viewName)** - Read a checkbox filter value. Returns `bool`.
- **getFilterText(filterHtmlName, viewName)** - Read a text or radio filter value. Returns `string`.
- **getFilterInteger(filterHtmlName, viewName)** - Read a select or numeric filter value. Returns `int`.
- **getFilterDate(filterHtmlName, viewName)** - Read a date filter value. Returns `DateTime?` (null if not set).

These methods also handle the "removeFilter" request automatically. When a user clicks an active filter's remove button, the corresponding filter value is cleared from the visit.

#### Adding Filter Inputs (addFilter methods)

These methods add filter UI controls to the current filter group. When a filter has a value, an active filter indicator is automatically added so users can see and remove active filters.

- **addFilterCheckbox(caption, htmlName, htmlValue, selected)** - Adds a checkbox filter. `selected` should be the value from `getFilterBoolean()`.

- **addFilterRadio(caption, htmlName, htmlValue, selected)** - Adds a radio button filter. Multiple radio filters with the same `htmlName` form a radio group.

- **addFilterTextInput(caption, htmlName, htmlValue)** - Adds a text input filter. `htmlValue` should be the value from `getFilterText()`.

- **addFilterDateInput(caption, htmlName, htmlDateValue)** - Adds a date input filter. `htmlDateValue` is a `DateTime?` from `getFilterDate()`.

- **addFilterSelect(caption, htmlName, options)** - Adds a select dropdown filter. `options` is a `List<NameValueSelected>` where each item has `name` (display text), `value` (html value), and `selected` (bool).

- **addFilterSelectContent(caption, htmlName, htmlValue, content, sqlCriteria)** - Adds a select dropdown filter populated from a content table. `content` is the content name, `sqlCriteria` is an optional SQL where clause to filter the options, and `htmlValue` is the currently selected id from `getFilterInteger()`.

- **addFilterSelectContent(caption, htmlName, htmlValue, content, sqlCriteria, nonCaption)** - Same as above with a `nonCaption` parameter that adds an unselected option at the top (e.g., "Select One" or "All").

#### Active Filters

Active filters are managed automatically. When a filter has a value, an active filter indicator is added showing the filter caption with a remove button. You can also add them manually:

- **addActiveFilter(caption, name, value)** - Adds a clickable active filter indicator. When clicked, it submits the form with `name=value`, which the getFilter methods detect to clear that filter.

#### Typical Filter Usage Pattern

```csharp
var layoutBuilder = cp.AdminUI.CreateLayoutBuilderList();
layoutBuilder.callbackAddonGuid = "{your-addon-guid}";

// -- read filter values first (persisted in visit)
bool filterAdminOnly = layoutBuilder.getFilterBoolean("filterAdminOnly", "myReport");
int filterCategoryId = layoutBuilder.getFilterInteger("filterCategoryId", "myReport");
string filterName = layoutBuilder.getFilterText("filterName", "myReport");
DateTime? filterAfterDate = layoutBuilder.getFilterDate("filterAfterDate", "myReport");

// -- add filter UI inputs
layoutBuilder.addFilterGroup("Filters");
layoutBuilder.addFilterCheckbox("Admin Only", "filterAdminOnly", "1", filterAdminOnly);
layoutBuilder.addFilterSelectContent("Category", "filterCategoryId", filterCategoryId, "Categories", "", "All Categories");
layoutBuilder.addFilterTextInput("Name", "filterName", filterName);
layoutBuilder.addFilterDateInput("After Date", "filterAfterDate", filterAfterDate);

// -- use filter values in your query
string sqlWhere = "(1=1)";
if (filterAdminOnly) { sqlWhere += " and(admin>0)"; }
if (filterCategoryId > 0) { sqlWhere += $" and(categoryId={filterCategoryId})"; }
if (!string.IsNullOrEmpty(filterName)) { sqlWhere += $" and(name like {cp.Db.EncodeSQLTextLike(filterName)})"; }
if (filterAfterDate.HasValue) { sqlWhere += $" and(dateAdded>={cp.Db.EncodeSQLDate((DateTime)filterAfterDate)})"; }
```


### CreateLayoutBuilder

creates an instance of LayoutBuilderBaseClass for basic forms

Examples of this layoutbuilder
- \source\Processor\Addons\Tools\ContentSchemaToolClass.cs
- \source\Processor\Addons\Tools\ConfigureEditClass.cs
- \source\Processor\Addons\Tools\ContentChildToolClass.cs
- \source\Processor\Addons\Tools\CreateGUIDToolClass.cs

### CreateLayoutBuilderList

creates an instance of LayoutBuilderListBaseClass object for tabular lists of data rows with filters

Examples of this layoutbuilder
- \source\Processor\Addons\LayoutBuilder\SampleLayoutBuilderList.cs
- \examples\LayoutBuilderListExample\README.md

### CreateLayoutBuilderNameValue

creates an instance of LayoutBuilderNameValueBaseClass for forms with lists of input boxes

Examples of this layoutbuilder
- \examples\LayoutBuilderNameValueExample\README.md

### CreateLayoutBuilderTabbedBody

creates an instance of LayoutBuilderTabbedBodyBaseClass for a layout that includes navigation accross the top and a body of content

### CreateLayoutBuilderToolForm

creates an instance of LayoutBuilderToolFormBaseClass for the structure of a tool that the client populates the body

### CreateLayoutBuilderTwoColumnLeft

creates an instance of LayoutBuilderTwoColumnLeftBaseClass for a simple 2 column layout populated by other layoutbuilders. The left column is wider than the right.

### CreateLayoutBuilderTwoColumnRight

creates an instance of LayoutBuilderTwoColumnRightBaseClass for a simple 2 column layout populated by other layoutbuilders. The right column is wider than the left.

