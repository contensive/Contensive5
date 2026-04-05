
# AdminUI Pattern

> All patterns and API reference: [Patterns Index](https://raw.githubusercontent.com/contensive/Contensive5/refs/heads/master/patterns/index.md)

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
- calls the getHtml method and returns its response. it is important that the client program return the unmodified complete response from the getHtml method.

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
// addon guid string best practice is to include the curly brace prefix and suffix
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


### Implementing Pagination in a List LayoutBuilder

The List LayoutBuilder supports built-in pagination. The layout handles the pagination UI automatically — the client program is responsible for setting the total record count, using the page number and page size in its SQL query, and providing the callback addon GUID so the layout can re-execute the addon on page changes.

#### Key Properties

- **`layoutBuilder.paginationPageNumber`** — The current page number (1-based). Read this value and use it in your SQL OFFSET calculation.
- **`layoutBuilder.paginationPageSize`** — The number of rows per page. Read this value to limit your SQL query results.
- **`layoutBuilder.callbackAddonGuid`** — Set this to your addon's GUID. Best practice is to include the curly brace prefix and suffix. The pagination UI uses this to call back into your addon when the user navigates pages.
- **`layoutBuilder.recordCount`** — Set this to the total number of records matching your query (before pagination). The layout uses this to calculate total pages and render pagination controls.
- **`layoutBuilder.sqlOrderBy`** — The current sort order selected by the user through column header clicks. Use this in your SQL `ORDER BY` clause, falling back to a default if empty.

#### Pagination Pattern

The implementation follows these steps:

1. **Set the callback GUID** so the layout can re-invoke your addon on page changes.
2. **Run a COUNT query** to get the total record count and assign it to `layoutBuilder.recordCount`.
3. **Run a SELECT query** using `layoutBuilder.paginationPageNumber` and `layoutBuilder.paginationPageSize` in a SQL `OFFSET...FETCH NEXT` clause.
4. **Use `layoutBuilder.sqlOrderBy`** in your `ORDER BY` clause, with a fallback default.

#### Example: AccountManagerUsersAddon

See `\examples\LayoutBuilderListExample\AccountManagerUsersAddon.cs` for a full working example. The key sections are shown below.

```csharp
// -- init layoutbuilder with callback guid for pagination
var layoutBuilder = cp.AdminUI.CreateLayoutBuilderList();
layoutBuilder.title = "Users";
layoutBuilder.callbackAddonGuid = Constants.guidAddonUserList;

// -- get the total record count (before pagination)
string sqlCount = @$"
    select count(*)
    from ccmembers u
        left join mmMembershipPeopleRules mar on mar.memberId=u.id
        left join abaccounts a on a.id=mar.accountid
    where 1=1
        and(a.id>0)
        {sqlWhere}
    ";
using (DataTable dt = cp.Db.ExecuteQuery(sqlCount)) {
    if (dt?.Rows != null && dt.Rows.Count == 1) {
        layoutBuilder.recordCount = cp.Utils.EncodeInteger(dt.Rows[0][0]);
    }
}

// -- query data with pagination using OFFSET...FETCH NEXT
string sql = @$"
    select
        u.id as userId, u.name as userName,
        a.id as accountid, a.name as accountname
    from ccmembers u
        left join mmMembershipPeopleRules mar on mar.memberId=u.id
        left join abaccounts a on a.id=mar.accountid
    where 1=1
        and(a.id>0)
        {sqlWhere}
    order by
        {(string.IsNullOrEmpty(layoutBuilder.sqlOrderBy) ? "u.name" : layoutBuilder.sqlOrderBy)}
    OFFSET
        {(layoutBuilder.paginationPageNumber - 1) * layoutBuilder.paginationPageSize} ROWS
        FETCH NEXT {layoutBuilder.paginationPageSize} ROWS ONLY
    ";
```

The `OFFSET` is calculated as `(paginationPageNumber - 1) * paginationPageSize` because `paginationPageNumber` is 1-based. The layout automatically renders page navigation controls based on `recordCount` and `paginationPageSize`.

### CreateLayoutBuilder

Creates an instance of LayoutBuilderBaseClass for basic forms.

**When to use:** Choose this layout when you need a simple, single-section admin page that displays read-only content, status information, or a basic form that doesn't fit the structured name/value pair pattern. This is the most general-purpose layout and is appropriate when none of the more specialized layouts apply — for example, displaying a schema summary, a confirmation page, or a simple output report.

Examples of this layoutbuilder
- \source\Processor\Addons\Tools\ContentSchemaToolClass.cs
- \source\Processor\Addons\Tools\ConfigureEditClass.cs
- \source\Processor\Addons\Tools\ContentChildToolClass.cs
- \source\Processor\Addons\Tools\CreateGUIDToolClass.cs

### CreateLayoutBuilderList

Creates an instance of LayoutBuilderListBaseClass object for tabular lists of data rows with filters.

**When to use:** Choose this layout when displaying a list or table of records that users need to browse, search, filter, and paginate. This is the right choice for any report or data listing — such as a user list, order history, log viewer, or any admin tool that queries rows from a database and presents them in a grid with sortable columns. Supports built-in pagination, column sorting, and the filter system described above.

Examples of this layoutbuilder
- \source\Processor\Addons\LayoutBuilder\SampleLayoutBuilderList.cs
- \examples\LayoutBuilderListExample\README.md

### CreateLayoutBuilderNameValue

Creates an instance of LayoutBuilderNameValueBaseClass for forms with lists of input boxes.

**When to use:** Choose this layout when building a form or settings tool that collects a series of user inputs as labeled name/value pairs. This is the right choice for configuration screens, settings panels, data entry forms, or any tool where the user fills in a series of fields (text boxes, dropdowns, checkboxes) each with a caption label on the left and an input control on the right. Examples include site settings, user profile editors, import configuration tools, and API key entry forms.

Examples of this layoutbuilder
- \examples\LayoutBuilderNameValueExample\README.md

### CreateLayoutBuilderTabbedBody

Creates an instance of LayoutBuilderTabbedBodyBaseClass for a layout that includes navigation across the top and a body of content.

**When to use:** Choose this layout when a tool or page has multiple distinct sections that should be organized into tabs. This is appropriate when the content is too complex for a single view and naturally divides into logical groupings — for example, a settings page with "General", "Advanced", and "Permissions" tabs, or a record editor with separate tabs for details, related records, and activity history.

### CreateLayoutBuilderToolForm

Creates an instance of LayoutBuilderToolFormBaseClass for the structure of a tool that the client populates the body.

**When to use:** Choose this layout when you need full control over the body HTML while still getting the standard admin UI chrome (title bar, button bar, wrapper structure). This is the right choice for custom tools that require specialized layouts, embedded controls, or HTML structures that don't fit the predefined patterns — such as a drag-and-drop interface, a visual editor, a dashboard with custom widgets, or any tool where you need to build the inner content yourself but want it wrapped consistently in the admin UI frame.

### CreateLayoutBuilderTwoColumnLeft

Creates an instance of LayoutBuilderTwoColumnLeftBaseClass for a simple 2-column layout populated by other layout builders. The left column is wider than the right.

**When to use:** Choose this layout when the page has a primary content area and a secondary sidebar, where the main content should be emphasized on the left. This is appropriate for detail views with a summary sidebar on the right, or any tool where the primary information (a list, form, or report) is in the wider left column and supplemental content (filters, related links, quick stats) sits in the narrower right column. Each column is populated with another layout builder instance.

### CreateLayoutBuilderTwoColumnRight

Creates an instance of LayoutBuilderTwoColumnRightBaseClass for a simple 2-column layout populated by other layout builders. The right column is wider than the left.

**When to use:** Choose this layout when the page has a navigation or sidebar panel on the left with primary content on the right. This is appropriate for tools with a left-hand navigation menu, tree view, or category list that drives the main content displayed in the wider right column — for example, a content explorer with a folder tree on the left and file details on the right. Each column is populated with another layout builder instance.

