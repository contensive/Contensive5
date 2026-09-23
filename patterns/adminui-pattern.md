
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

## CreateLayoutBuilderList — Complete Reference

`CreateLayoutBuilderList` creates a tabular list layout with built-in support for column sorting, search, pagination, CSV export, filters, row actions, and form integration. All interactive features (search, sort, pagination, filter application) work via AJAX — the layout serializes the form, POSTs to `/LayoutBuilderCallback`, which re-executes your addon and replaces the grid content without a full page reload.

### AJAX Callback Requirement

Every interactive feature (search, sort, pagination, filters, download) requires the layout to call back into your addon via AJAX. You **must** set `callbackAddonGuid` for these features to work. Without it, search, sort, and pagination are disabled.

```csharp
var layoutBuilder = cp.AdminUI.CreateLayoutBuilderList();
layoutBuilder.callbackAddonGuid = "{your-addon-guid}";
```

Your addon is re-executed on every AJAX callback. It must rebuild the entire LayoutBuilderList each time — reading the current filter/search/sort/pagination state from the layout builder properties and returning the appropriate result. The layout's `getHtml()` method detects whether this is an initial page load or an AJAX callback and returns the correct response format automatically. Your addon should always return the unmodified result of `getHtml()`.

### Initialization Properties

These properties control the overall appearance and behavior of the layout.

| Property | Type | Default | Description |
|---|---|---|---|
| `title` | string | "" | The headline displayed at the top of the layout |
| `description` | string | "" | Description text displayed below the title, wrapped in an HTML paragraph tag |
| `callbackAddonGuid` | string | "" | **Required for interactive features.** The GUID of this addon, used for AJAX callbacks. Include curly braces (e.g., `"{GUID-HERE}"`) |
| `includeForm` | bool | true | Wraps the layout in an HTML form tag |
| `isOuterContainer` | bool | false | Set true if this layout is not nested inside another layout. Adds the outer `#afw` div with styles and JavaScript |
| `includeBodyPadding` | bool | false | Adds default padding around the body content area |
| `includeBodyColor` | bool | false | Adds the default background color to the body content area |
| `paginationPageSizeDefault` | int | 50 | Default number of rows per page. Set to 0 to disable pagination |
| `paginationRecordAlias` | string | "records" | Replaces the word "records" in the pagination display (e.g., "47 records found") |
| `allowDownloadButton` | bool | false | Shows a "Request Download" button that triggers CSV export |

### Messages

Four message types are available for user feedback. Each renders in a distinct style (color/icon). Set to an empty string to hide.

```csharp
layoutBuilder.successMessage = "Record saved successfully.";
layoutBuilder.warningMessage = "Some fields were skipped.";
layoutBuilder.failMessage = "Record could not be saved.";
layoutBuilder.infoMessage = "Processing will complete overnight.";
```

### HTML Injection Points

Three properties allow custom HTML to be inserted around the body content area.

| Property | Description |
|---|---|
| `htmlBeforeBody` | HTML inserted above the data grid |
| `htmlAfterBody` | HTML inserted below the data grid. Common use: injecting custom `<style>` tags |
| `htmlLeftOfBody` | HTML inserted to the left of the data grid |

### Portal Sub-Navigation

When this layout appears as a subsection within a portal feature, set the portal sub-navigation title.

```csharp
layoutBuilder.portalSubNavTitleList.Add("User Management");
```

### Defining Columns

Columns define the table structure — headers, alignment, sorting, and download behavior. Define all columns before populating rows.

#### Column Properties (set-then-add pattern)

Set column properties, then call `addColumn()` to commit the column. Properties reset to defaults after each `addColumn()` call.

| Property | Type | Default | Description |
|---|---|---|---|
| `columnName` | string | "" | The SQL column name. Used as the `sortField` value when the user clicks this column header |
| `columnCaption` | string | "" | Display text in the column header. Can contain HTML (e.g., a checkbox) |
| `columnCaptionClass` | string | "" | CSS class(es) applied to the header cell |
| `columnCellClass` | string | "" | CSS class(es) applied to each data cell in this column |
| `columnSortable` | bool | false | If true, the column header becomes clickable for sorting. Requires `columnName` to be set |
| `columnVisible` | bool | true | If false, the column is hidden from display but can still be included in downloads |
| `columnDownloadable` | bool | true | If true, this column is included in CSV exports |
| `columnWidthPercent` | int | 0 | Column width as a percentage (1-100). Applied as an inline style |

```csharp
// -- first column: ID (sortable, downloadable)
layoutBuilder.columnName = "id";
layoutBuilder.columnCaption = "ID";
layoutBuilder.columnCaptionClass = $"{AfwStyles.afwWidth100px} {AfwStyles.afwTextAlignCenter}";
layoutBuilder.columnCellClass = $"{AfwStyles.afwWidth100px} {AfwStyles.afwTextAlignCenter}";
layoutBuilder.columnSortable = true;
layoutBuilder.columnDownloadable = true;

// -- commit the column definition and move to the next
layoutBuilder.addColumn();

// -- second column: Name (sortable)
layoutBuilder.columnName = "name";
layoutBuilder.columnCaption = "Name";
layoutBuilder.columnCaptionClass = $"{AfwStyles.afwTextAlignLeft}";
layoutBuilder.columnCellClass = $"{AfwStyles.afwTextAlignLeft}";
layoutBuilder.columnSortable = true;
```

#### Alternative: Object-based column definition

```csharp
layoutBuilder.addColumn(new ReportListColumnBaseClass {
    name = "dateAdded",
    caption = "Date Added",
    captionClass = $"{AfwStyles.afwWidth200px} {AfwStyles.afwTextAlignCenter}",
    cellClass = $"{AfwStyles.afwWidth200px} {AfwStyles.afwTextAlignLeft}",
    sortable = true,
    visible = true,
    downloadable = true,
    columnWidthPercent = 20
});
```

Note: `ReportListColumnBaseClass.downloadable` defaults to `false`, while the property-based pattern defaults `columnDownloadable` to `true` when `addColumn()` is called. Set explicitly to be safe.

#### AfwStyles CSS Class Constants

The `AfwStyles` static class provides predefined CSS class constants. Each constant includes a trailing space so they can be concatenated directly.

**Percentage widths:** `afwWidth10` through `afwWidth100` (10% increments)

**Pixel widths:** `afwWidth10px`, `afwWidth20px`, `afwWidth30px`, `afwWidth40px`, `afwWidth50px`, `afwWidth60px`, `afwWidth70px`, `afwWidth80px`, `afwWidth90px`, `afwWidth100px`, `afwWidth200px`, `afwWidth300px`, `afwWidth400px`, `afwWidth500px`

**Left margins:** `afwMarginLeft100px` through `afwMarginLeft500px` (100px increments)

**Text alignment:** `afwTextAlignLeft`, `afwTextAlignRight`, `afwTextAlignCenter`

### Populating Rows and Cells

After defining columns, iterate your data and populate rows.

```csharp
foreach (DataRow row in dt.Rows) {
    layoutBuilder.addRow();
    layoutBuilder.setCell(cp.Utils.EncodeInteger(row["id"]));
    layoutBuilder.setCell(cp.Utils.EncodeText(row["name"]));
    layoutBuilder.setCell(cp.Utils.EncodeDate(row["dateAdded"]));
}
```

#### `addRow()`

Starts a new row. Call once per data record, before `setCell()` calls.

#### `setCell()` — Display and download content

Cells are set sequentially — the first `setCell()` after `addRow()` fills the first column, the second fills the second column, and so on.

**Single-value overloads** — the same value is used for both display and CSV export:
- `setCell(string content)` — HTML string (display) and plain text (download)
- `setCell(int content)`
- `setCell(double content)`
- `setCell(bool content)`
- `setCell(DateTime? content)`

**Dual-value overloads** — separate values for display HTML and CSV download:
- `setCell(string reportContent, string downloadContent)` — e.g., display a link but download plain text
- `setCell(string reportContent, int downloadContent)`
- `setCell(string reportContent, double downloadContent)`
- `setCell(string reportContent, DateTime downloadContent)`
- `setCell(string reportContent, bool downloadContent)`

The dual-value pattern is essential when display content contains HTML (links, formatting) but the CSV export should contain raw data:

```csharp
// -- display shows a clickable link; CSV export gets the plain name
string userLink = $"<a href=\"{editUrl}\">{userName}</a>";
layoutBuilder.setCell(userLink, userName);
```

#### Row Styling

```csharp
layoutBuilder.addRow();
layoutBuilder.addRowClass("myCustomHighlight");
```

Adds a CSS class to the `<tr>` element for the current row. Useful for conditional highlighting (e.g., overdue items, alerts).

#### Excluding Rows from Download

```csharp
layoutBuilder.addRow();
layoutBuilder.excludeRowFromDownload = true;
```

When true, this row is included in the HTML display but excluded from the CSV export.

### Column Sorting

Column sorting is automatic for columns with `columnSortable = true`. The user clicks a column header to cycle through three states: ascending, descending, cleared. A sort indicator arrow (&#9650; / &#9660;) appears next to the active sort column.

**Reading sort state in your addon:**

| Property | Type | Description |
|---|---|---|
| `sortField` | string | The `columnName` of the column being sorted, or `""` if no sort is active |
| `sortDirection` | string | `"asc"`, `"desc"`, or `""` (no sort) |

Use `sortField` and `sortDirection` to build your SQL `ORDER BY` clause, with a fallback default:

```csharp
string sql = $"select id, name, dateAdded from ccmembers where {sqlWhere}";

// -- apply sort from user column click, or fall back to default
if (!string.IsNullOrEmpty(layoutBuilder.sortField)) {
    sql += $" order by {layoutBuilder.sortField}";
    if (layoutBuilder.sortDirection == "desc") { sql += " desc"; }
} else {
    sql += " order by name";
}
```

Sort state persists across AJAX callbacks within the user's visit (stored in a visit property keyed by `callbackAddonGuid`). Changing the sort resets pagination to page 1 automatically.

**Deprecated:** `sqlOrderBy` returns a pre-built `ORDER BY` clause string. Use `sortField`/`sortDirection` instead for explicit control.

### Search

The layout renders a search input box with debounced keystroke handling — typing pauses for 1 second before triggering an AJAX refresh. A clear button is provided to reset the search. Searching resets pagination to page 1.

**Reading the search term:**

```csharp
string searchTerm = layoutBuilder.sqlSearchTerm;
```

Returns the user's current search input, persisted across callbacks within the visit. Use it in your SQL WHERE clause:

```csharp
if (!string.IsNullOrEmpty(layoutBuilder.sqlSearchTerm)) {
    sqlWhere += $" and(name like {cp.Db.EncodeSQLTextLike(layoutBuilder.sqlSearchTerm)})";
}
```

To search multiple columns:

```csharp
if (!string.IsNullOrEmpty(layoutBuilder.sqlSearchTerm)) {
    string likeTerm = cp.Db.EncodeSQLTextLike(layoutBuilder.sqlSearchTerm);
    sqlWhere += $" and((u.name like {likeTerm}) or (a.name like {likeTerm}))";
}
```

Search is enabled automatically when `callbackAddonGuid` is set.

### Implementing Pagination

The List LayoutBuilder supports built-in pagination. The layout handles the pagination UI automatically — the client program is responsible for setting the total record count, using the page number and page size in its SQL query, and providing the callback addon GUID so the layout can re-execute the addon on page changes.

Pagination is enabled when `callbackAddonGuid` is set AND `recordCount` exceeds `paginationPageSize`.

#### Key Properties

| Property | Type | Description |
|---|---|---|
| `paginationPageNumber` | int (read-only) | The current page number (1-based). Read this value and use it in your SQL OFFSET calculation |
| `paginationPageSize` | int (read-only) | The effective number of rows per page. Defaults to `paginationPageSizeDefault` (50). During download requests, this returns 9,999,999 (all records) |
| `paginationPageSizeDefault` | int | Set this to control rows per page. Default: 50 |
| `recordCount` | int | **You must set this.** Total number of records matching your query (before pagination). The layout uses this to calculate total pages and render navigation |
| `paginationRecordAlias` | string | Replaces "records" in display text. Default: `"records"` |

#### `paginationReset()`

Forces pagination back to page 1. Call this when a filter changes so the user doesn't land on a page that no longer exists.

#### Pagination Pattern

1. **Set the callback GUID** so the layout can re-invoke your addon on page changes.
2. **Run a COUNT query** to get the total record count and assign it to `layoutBuilder.recordCount`.
3. **Run a SELECT query** using `layoutBuilder.paginationPageNumber` and `layoutBuilder.paginationPageSize` in a SQL `OFFSET...FETCH NEXT` clause.
4. **Use `sortField`/`sortDirection`** in your `ORDER BY` clause, with a fallback default.

```csharp
var layoutBuilder = cp.AdminUI.CreateLayoutBuilderList();
layoutBuilder.title = "Users";
layoutBuilder.callbackAddonGuid = "{your-addon-guid}";

// -- get total record count (before pagination)
string sqlCount = $"select count(*) from ccmembers where {sqlWhere}";
using (DataTable dt = cp.Db.ExecuteQuery(sqlCount)) {
    if (dt?.Rows != null && dt.Rows.Count == 1) {
        layoutBuilder.recordCount = cp.Utils.EncodeInteger(dt.Rows[0][0]);
    }
}

// -- query with pagination
string sql = $@"
    select id, name, dateAdded
    from ccmembers
    where {sqlWhere}
    order by {(string.IsNullOrEmpty(layoutBuilder.sortField) ? "name" : $"{layoutBuilder.sortField} {layoutBuilder.sortDirection}")}
    OFFSET {(layoutBuilder.paginationPageNumber - 1) * layoutBuilder.paginationPageSize} ROWS
    FETCH NEXT {layoutBuilder.paginationPageSize} ROWS ONLY";
```

The `OFFSET` is calculated as `(paginationPageNumber - 1) * paginationPageSize` because `paginationPageNumber` is 1-based. The layout automatically renders page navigation controls based on `recordCount` and `paginationPageSize`.

### Adding Filters to Reports

**IMPORTANT: Always use the built-in LayoutBuilder filter methods.** Do NOT implement filters by manually adding HTML inputs (e.g., `cp.Html.SelectContent()`, `cp.Html.InputText()`) to `htmlBeforeBody`, `htmlLeftOfBody`, or the body, and then reading them with `cp.Doc.GetText()` / `cp.Doc.GetInteger()` / `cp.Doc.GetBoolean()`. That approach bypasses the LayoutBuilder's filter persistence (values are lost on AJAX refresh), does not render active filter indicators (removable pills), and does not integrate with the filter dropdown UI.

Instead, use `layoutBuilder.getFilterText()` / `getFilterInteger()` / `getFilterBoolean()` / `getFilterDate()` to read filter values, and `layoutBuilder.addFilterTextInput()` / `addFilterSelect()` / `addFilterSelectContent()` / `addFilterCheckbox()` / `addFilterRadio()` / `addFilterDateInput()` to add filter UI. These methods handle visit persistence, active filter badges, and AJAX compatibility automatically.

Filters allow users to narrow down report data. They appear in a filter dropdown panel on the layout and persist across page loads within a visit. The filter system has three parts: reading filter values, adding filter UI inputs, and managing active filter indicators.

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

**Important:** Read filter values **before** adding filter UI inputs. The read values are used both to build your SQL query and to set the current state of the filter controls.

#### Adding Filter Inputs (addFilter methods)

These methods add filter UI controls to the current filter group. When a filter has a value, an active filter indicator is automatically added so users can see and remove active filters.

- **addFilterCheckbox(caption, htmlName, htmlValue, selected)** - Adds a checkbox filter. `selected` should be the value from `getFilterBoolean()`.

- **addFilterRadio(caption, htmlName, htmlValue, selected)** - Adds a radio button filter. Multiple radio filters with the same `htmlName` form a radio group.

- **addFilterTextInput(caption, htmlName, htmlValue)** - Adds a text input filter. `htmlValue` should be the value from `getFilterText()`.

- **addFilterDateInput(caption, htmlName, htmlDateValue)** - Adds a date input filter. `htmlDateValue` is a `DateTime?` from `getFilterDate()`.

- **addFilterSelect(caption, htmlName, options, defaultValue = "")** - Adds a select dropdown filter. `options` is a `List<NameValueSelected>` where each item has `name` (display text), `value` (html value), and `selected` (bool). Optional `defaultValue` parameter: if specified, no active filter chip will be displayed when the selected value matches `defaultValue` — this prevents showing chips for the default state, which would otherwise allow users to click X without any visible effect.

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

#### Hiding Default Filter Chips

When a filter select has a meaningful default value (like "All", "Current Week", or value "0"), you can prevent the chip from displaying when that default is selected. This avoids the confusing UX where clicking the X button doesn't appear to do anything because it just returns to the default state.

```csharp
// Read filter value with default handling
string filterPeriod = layoutBuilder.getFilterText("filterPeriod", "myReport");
if (string.IsNullOrEmpty(filterPeriod)) {
    filterPeriod = "currentWeek";  // default value
}

// Build options list
var periodOptions = new List<NameValueSelected> {
    new NameValueSelected("Current Week", "currentWeek", filterPeriod == "currentWeek"),
    new NameValueSelected("Previous Week", "previousWeek", filterPeriod == "previousWeek"),
    new NameValueSelected("All Time", "all", filterPeriod == "all")
};

// Add filter with defaultValue parameter - no chip shown when "currentWeek" is selected
layoutBuilder.addFilterSelect("Period", "filterPeriod", periodOptions, defaultValue: "currentWeek");

// Example with integer default (like "All Sources" = 0)
int filterSourceId = layoutBuilder.getFilterInteger("filterSourceId", "myReport");
var sourceOptions = new List<NameValueSelected> {
    new NameValueSelected("All Sources", "0", filterSourceId == 0),
    new NameValueSelected("Source A", "1", filterSourceId == 1),
    new NameValueSelected("Source B", "2", filterSourceId == 2)
};
layoutBuilder.addFilterSelect("Source", "filterSourceId", sourceOptions, defaultValue: "0");
```

The `defaultValue` parameter is optional with an empty string default. If you don't specify it, chips will display for all selected values (existing behavior is preserved).

### CSV Export / Download

The LayoutBuilderList can generate a CSV file from the grid data and trigger a browser download.

#### Enabling Download

```csharp
layoutBuilder.allowDownloadButton = true;
```

This adds a "Request Download" button to the layout. When the user clicks it, the layout makes an AJAX POST with `downloadRequest=1`, which re-executes your addon.

#### How It Works

When `allowDownloadButton` is true and the user clicks the download button:

1. The layout's AJAX JavaScript POSTs the form with `downloadRequest=1`
2. Your addon is re-executed via the callback
3. `layoutBuilder.requestDownload` returns `true`
4. `layoutBuilder.paginationPageSize` automatically returns 9,999,999 (all records)
5. `layoutBuilder.paginationPageNumber` automatically returns 1
6. Your addon should populate all rows normally (the large page size means all records are fetched)
7. `getHtml()` detects the download request, builds a CSV from the grid data, saves it to CDN, and returns the download URL
8. The client-side JavaScript triggers the browser download

**Your addon does not need special download handling.** Just set `allowDownloadButton = true`, populate the grid as usual, and the layout does the rest. The pagination properties automatically adjust to include all records during a download.

#### Controlling What Gets Downloaded

- **`columnDownloadable`** (per column) — Set `false` to exclude a column from CSV export (e.g., checkbox columns, action links). Default: `true`.
- **`excludeRowFromDownload`** (per row) — Set `true` after `addRow()` to exclude a row from CSV export.
- **Dual-value `setCell()`** — Use `setCell(reportContent, downloadContent)` overloads to provide clean data for CSV while displaying formatted HTML in the grid.

```csharp
// -- checkbox column: visible but not in CSV
layoutBuilder.columnCaption = "<input type=\"checkbox\" id=\"selectAll\">";
layoutBuilder.columnDownloadable = false;
layoutBuilder.columnVisible = true;

// ...

// -- data cell: link in HTML, plain text in CSV
string displayHtml = $"<a href=\"{url}\">{name}</a>";
layoutBuilder.setCell(displayHtml, name);
```

### Row Ellipse (Action) Menus

Each row can have a dropdown action menu (three-dot / ellipse icon) that appears on the last visible column.

```csharp
layoutBuilder.addRow();
layoutBuilder.addRowEllipseMenuItem("Edit", $"/admin?af=4&cid={contentId}&id={recordId}");
layoutBuilder.addRowEllipseMenuItem("View History", $"/admin?tool=history&id={recordId}");
// ... set cells as usual
```

Call `addRowEllipseMenuItem(name, url)` after `addRow()` and before or after `setCell()` calls. Multiple menu items can be added per row. Rows without menu items show no ellipse icon.

### Form Buttons

Buttons appear in a button bar at the top and bottom of the layout. Two types are available:

#### Submit Buttons

```csharp
layoutBuilder.addFormButton("Save");
layoutBuilder.addFormButton("Delete", "button", "btnDelete");
layoutBuilder.addFormButton("Archive", "button", "btnArchive", "btn-warning");
```

Overloads:
- `addFormButton(buttonValue)` — Caption and submitted value
- `addFormButton(buttonValue, buttonName)` — Custom form field name
- `addFormButton(buttonValue, buttonName, buttonId)` — With HTML id
- `addFormButton(buttonValue, buttonName, buttonId, buttonClass)` — With CSS class

Read the clicked button in your form processing:
```csharp
string button = cp.Doc.GetText("button");
if (button == "Save") { /* handle save */ }
```

#### Link Buttons

Non-submitting buttons that navigate to a URL:

```csharp
layoutBuilder.addLinkButton("Back to List", "/admin?feature=list");
layoutBuilder.addLinkButton("Help", "https://docs.example.com", "btnHelp");
layoutBuilder.addLinkButton("Export", "/export", "btnExport", "btn-secondary");
```

### Hidden Form Fields

Add hidden inputs to the form for passing state across requests:

```csharp
layoutBuilder.addFormHidden("formId", "userList");
layoutBuilder.addFormHidden("accountId", accountId);
layoutBuilder.addFormHidden("rowCnt", rowPtr);
```

Overloads accept `string`, `int`, `double`, `DateTime`, and `bool` values. Each has an optional `htmlId` parameter:

```csharp
layoutBuilder.addFormHidden("targetDate", DateTime.Now, "hidTargetDate");
```

### Row Selection / Checkboxes

There is no built-in checkbox/selection mechanism. Implement row selection by adding manual checkboxes via `setCell()`.

#### Pattern

1. Define a non-sortable, non-downloadable column for the checkbox
2. Optionally add a "select all" checkbox in the column header
3. Add per-row checkboxes with `setCell()`
4. Track the row count with a hidden field

```csharp
// -- column header with "select all" checkbox
layoutBuilder.columnCaption = "<input type=\"checkbox\" id=\"selectAllNone\">";
layoutBuilder.columnCaptionClass = $"{AfwStyles.afwWidth20px} {AfwStyles.afwTextAlignCenter}";
layoutBuilder.columnCellClass = $"{AfwStyles.afwWidth20px} {AfwStyles.afwTextAlignCenter}";
layoutBuilder.columnDownloadable = false;
layoutBuilder.columnSortable = false;

// ... in the row loop:
layoutBuilder.addRow();
string checkbox = cp.Html.CheckBox($"row{rowPtr}", false, "selectRowCheckbox");
string hiddenId = cp.Html5.Hidden($"rowId{rowPtr}", recordId.ToString());
layoutBuilder.setCell(checkbox + hiddenId);

// ... after the loop:
layoutBuilder.addFormHidden("rowCnt", rowPtr);
```

Process selected rows in your form handler:

```csharp
int rowCnt = cp.Doc.GetInteger("rowCnt");
for (int i = 0; i < rowCnt; i++) {
    if (cp.Doc.GetBoolean($"row{i}")) {
        int selectedId = cp.Doc.GetInteger($"rowId{i}");
        // process selected row
    }
}
```

### Complete Example

This example demonstrates all major features: filters, search, sorting, pagination, CSV download, row styling, buttons, and hidden fields.

```csharp
public override object Execute(CPBaseClass cp) {
    //
    // -- authenticate/authorize
    if (!cp.User.IsAdmin) { return "You do not have permission."; }
    //
    // -- create the layout
    var layoutBuilder = cp.AdminUI.CreateLayoutBuilderList();
    layoutBuilder.title = "Member Report";
    layoutBuilder.callbackAddonGuid = "{YOUR-ADDON-GUID-HERE}";
    layoutBuilder.allowDownloadButton = true;
    layoutBuilder.isOuterContainer = true;
    layoutBuilder.includeBodyColor = true;
    layoutBuilder.includeBodyPadding = true;
    //
    // -- read filter values (before adding filter UI)
    bool filterAdminOnly = layoutBuilder.getFilterBoolean("filterAdminOnly", "memberReport");
    int filterGroupId = layoutBuilder.getFilterInteger("filterGroupId", "memberReport");
    string filterName = layoutBuilder.getFilterText("filterName", "memberReport");
    //
    // -- add filter UI
    layoutBuilder.addFilterGroup("Filters");
    layoutBuilder.addFilterCheckbox("Admin Only", "filterAdminOnly", "1", filterAdminOnly);
    layoutBuilder.addFilterSelectContent("Group", "filterGroupId", filterGroupId, "Groups", "", "All Groups");
    layoutBuilder.addFilterTextInput("Name Contains", "filterName", filterName);
    //
    // -- define columns
    layoutBuilder.columnName = "id";
    layoutBuilder.columnCaption = "ID";
    layoutBuilder.columnCaptionClass = $"{AfwStyles.afwWidth100px} {AfwStyles.afwTextAlignCenter}";
    layoutBuilder.columnCellClass = $"{AfwStyles.afwWidth100px} {AfwStyles.afwTextAlignCenter}";
    layoutBuilder.columnSortable = true;
    //
    layoutBuilder.addColumn();
    layoutBuilder.columnName = "name";
    layoutBuilder.columnCaption = "Name";
    layoutBuilder.columnCaptionClass = $"{AfwStyles.afwTextAlignLeft}";
    layoutBuilder.columnCellClass = $"{AfwStyles.afwTextAlignLeft}";
    layoutBuilder.columnSortable = true;
    //
    layoutBuilder.addColumn();
    layoutBuilder.columnName = "dateAdded";
    layoutBuilder.columnCaption = "Date Added";
    layoutBuilder.columnCaptionClass = $"{AfwStyles.afwWidth200px} {AfwStyles.afwTextAlignCenter}";
    layoutBuilder.columnCellClass = $"{AfwStyles.afwWidth200px} {AfwStyles.afwTextAlignLeft}";
    layoutBuilder.columnSortable = true;
    //
    // -- build SQL WHERE from filters and search
    string sqlWhere = "(1=1)";
    if (filterAdminOnly) { sqlWhere += " and(admin>0)"; }
    if (filterGroupId > 0) { sqlWhere += $" and(groupId={filterGroupId})"; }
    if (!string.IsNullOrEmpty(filterName)) { sqlWhere += $" and(name like {cp.Db.EncodeSQLTextLike(filterName)})"; }
    if (!string.IsNullOrEmpty(layoutBuilder.sqlSearchTerm)) {
        sqlWhere += $" and(name like {cp.Db.EncodeSQLTextLike(layoutBuilder.sqlSearchTerm)})";
    }
    //
    // -- get total record count for pagination
    using (DataTable dt = cp.Db.ExecuteQuery($"select count(*) from ccmembers where {sqlWhere}")) {
        if (dt?.Rows != null && dt.Rows.Count == 1) {
            layoutBuilder.recordCount = cp.Utils.EncodeInteger(dt.Rows[0][0]);
        }
    }
    //
    // -- build data query with sort and pagination
    string orderBy = "name";
    if (!string.IsNullOrEmpty(layoutBuilder.sortField)) {
        orderBy = layoutBuilder.sortField;
        if (layoutBuilder.sortDirection == "desc") { orderBy += " desc"; }
    }
    string sql = $@"
        select id, name, dateAdded
        from ccmembers
        where {sqlWhere}
        order by {orderBy}
        OFFSET {(layoutBuilder.paginationPageNumber - 1) * layoutBuilder.paginationPageSize} ROWS
        FETCH NEXT {layoutBuilder.paginationPageSize} ROWS ONLY";
    //
    // -- populate rows
    int rowPtr = 0;
    using (DataTable dt = cp.Db.ExecuteQuery(sql)) {
        if (dt?.Rows != null) {
            foreach (DataRow row in dt.Rows) {
                layoutBuilder.addRow();
                //
                layoutBuilder.setCell(cp.Utils.EncodeInteger(row["id"]));
                //
                string name = cp.Utils.EncodeText(row["name"]);
                string editLink = $"<a href=\"/admin?af=4&cid=5&id={row["id"]}\">{name}</a>";
                layoutBuilder.setCell(editLink, name);
                //
                layoutBuilder.setCell(cp.Utils.EncodeDate(row["dateAdded"]));
                //
                rowPtr++;
            }
        }
    }
    //
    // -- form elements
    layoutBuilder.addFormButton("Cancel");
    layoutBuilder.addFormButton("Process Selected");
    layoutBuilder.addFormHidden("rowCnt", rowPtr);
    //
    return layoutBuilder.getHtml();
}
```

### Execution Order Summary

The correct order of operations when building a LayoutBuilderList:

1. **Create** the layout builder with `cp.AdminUI.CreateLayoutBuilderList()`
2. **Set** `callbackAddonGuid` and initialization properties (`title`, `allowDownloadButton`, etc.)
3. **Read** filter values with `getFilterBoolean()`, `getFilterInteger()`, etc. — never use `cp.Doc.GetText()` for filters
4. **Add** filter UI with `addFilterGroup()`, `addFilterCheckbox()`, etc. — never add raw HTML filter inputs to the body or htmlLeftOfBody
5. **Define** columns (set properties + `addColumn()` for each)
6. **Build** your SQL WHERE clause from filters and `sqlSearchTerm`
7. **Query** record count and set `layoutBuilder.recordCount`
8. **Query** data using `sortField`/`sortDirection`, `paginationPageNumber`, `paginationPageSize`
9. **Populate** rows with `addRow()` + `setCell()` calls
10. **Add** form buttons and hidden fields
11. **Set** additional properties (`description`, `htmlAfterBody`, `portalSubNavTitleList`, messages)
12. **Return** `layoutBuilder.getHtml()` — always return the unmodified result

---

## CreateLayoutBuilder

Creates an instance of LayoutBuilderBaseClass for basic forms.

**When to use:** Choose this layout when you need a simple, single-section admin page that displays read-only content, status information, or a basic form that doesn't fit the structured name/value pair pattern. This is the most general-purpose layout and is appropriate when none of the more specialized layouts apply — for example, displaying a schema summary, a confirmation page, or a simple output report.

Examples of this layoutbuilder
- \source\Processor\Addons\Tools\ContentSchemaToolClass.cs
- \source\Processor\Addons\Tools\ConfigureEditClass.cs
- \source\Processor\Addons\Tools\ContentChildToolClass.cs
- \source\Processor\Addons\Tools\CreateGUIDToolClass.cs

## CreateLayoutBuilderNameValue

Creates an instance of LayoutBuilderNameValueBaseClass for forms with lists of input boxes.

**When to use:** Choose this layout when building a form or settings tool that collects a series of user inputs as labeled name/value pairs. This is the right choice for configuration screens, settings panels, data entry forms, or any tool where the user fills in a series of fields (text boxes, dropdowns, checkboxes) each with a caption label on the left and an input control on the right. Examples include site settings, user profile editors, import configuration tools, and API key entry forms.

Examples of this layoutbuilder
- \examples\LayoutBuilderNameValueExample\README.md

## CreateLayoutBuilderTabbedBody

Creates an instance of LayoutBuilderTabbedBodyBaseClass for a layout that includes navigation across the top and a body of content.

**When to use:** Choose this layout when a tool or page has multiple distinct sections that should be organized into tabs. This is appropriate when the content is too complex for a single view and naturally divides into logical groupings — for example, a settings page with "General", "Advanced", and "Permissions" tabs, or a record editor with separate tabs for details, related records, and activity history.

## CreateLayoutBuilderToolForm

Creates an instance of LayoutBuilderToolFormBaseClass for the structure of a tool that the client populates the body.

**When to use:** Choose this layout when you need full control over the body HTML while still getting the standard admin UI chrome (title bar, button bar, wrapper structure). This is the right choice for custom tools that require specialized layouts, embedded controls, or HTML structures that don't fit the predefined patterns — such as a drag-and-drop interface, a visual editor, a dashboard with custom widgets, or any tool where you need to build the inner content yourself but want it wrapped consistently in the admin UI frame.

## CreateLayoutBuilderTwoColumnLeft

Creates an instance of LayoutBuilderTwoColumnLeftBaseClass for a simple 2-column layout populated by other layout builders. The left column is wider than the right.

**When to use:** Choose this layout when the page has a primary content area and a secondary sidebar, where the main content should be emphasized on the left. This is appropriate for detail views with a summary sidebar on the right, or any tool where the primary information (a list, form, or report) is in the wider left column and supplemental content (filters, related links, quick stats) sits in the narrower right column. Each column is populated with another layout builder instance.

## CreateLayoutBuilderTwoColumnRight

Creates an instance of LayoutBuilderTwoColumnRightBaseClass for a simple 2-column layout populated by other layout builders. The right column is wider than the left.

**When to use:** Choose this layout when the page has a navigation or sidebar panel on the left with primary content on the right. This is appropriate for tools with a left-hand navigation menu, tree view, or category list that drives the main content displayed in the wider right column — for example, a content explorer with a folder tree on the left and file details on the right. Each column is populated with another layout builder instance.
