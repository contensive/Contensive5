# LayoutBuilder API Reference

> Source files: `source/CPBase/BaseClasses/LayoutBuilder/LayoutBuilderBaseClass.cs`, `LayoutBuilderListBaseClass.cs`, `LayoutBuilderToolFormBaseClass.cs`, `LayoutBuilderTwoColumnLeftBaseClass.cs`, `LayoutBuilderTwoColumnRightBaseClass.cs`, `LayoutBuilderTabbedBodyBaseClass.cs`, `LayoutBuilderNameValueBaseClass.cs`

LayoutBuilder classes create admin UI forms, lists, and layouts. All inherit from `LayoutBuilderBaseClass`.

---

## LayoutBuilderBaseClass (abstract base)

Constructor: `LayoutBuilderBaseClass(CPBaseClass cp)`

### Core Properties

| Property | Type | Description |
|----------|------|-------------|
| `title` | `string` | Headline at the top of the form |
| `body` | `string` | Main HTML body content |
| `description` | `string` | Description text, wrapped in `<p>` tag |
| `callbackAddonGuid` | `string` | GUID of addon called for pagination/search updates (typically the same remote method addon) |
| `includeForm` | `bool` | Include a `<form>` tag around the layout. Default true. |
| `isOuterContainer` | `bool` | If true, includes outer div with `id="afw"` plus styles/JS |
| `includeBodyPadding` | `bool` | Add default padding around the body |
| `includeBodyColor` | `bool` | Add default background color to the body |

### Message Properties

| Property | Type | Description |
|----------|------|-------------|
| `successMessage` | `string` | Success message display |
| `infoMessage` | `string` | Informational message display |
| `warningMessage` | `string` | Warning message display |
| `failMessage` | `string` | Error/failure message display |

### HTML Sections

| Property | Type | Description |
|----------|------|-------------|
| `htmlLeftOfBody` | `string` | HTML block to the left of body (typically filters) |
| `htmlBeforeBody` | `string` | HTML block above the body |
| `htmlAfterBody` | `string` | HTML block below the body |

### Download Support

| Property | Type | Description |
|----------|------|-------------|
| `allowDownloadButton` | `bool` | Show download button. When clicked, an AJAX request sets `requestDownload=true`. |
| `requestDownload` | `bool` | (read-only) True when the user clicked download. Return download data instead of HTML. |

### Portal Sub-Navigation

| Property | Type | Description |
|----------|------|-------------|
| `portalSubNavTitleList` | `List<string>` | Populate title breadcrumbs in portal sub-nav |

### Form Buttons

```csharp
void addFormButton(string buttonValue)
void addFormButton(string buttonValue, string buttonName)
void addFormButton(string buttonValue, string buttonName, string buttonId)
void addFormButton(string buttonValue, string buttonName, string buttonId, string buttonClass)

void addLinkButton(string buttonCaption, string link)
void addLinkButton(string buttonCaption, string link, string htmlId)
void addLinkButton(string buttonCaption, string link, string htmlId, string htmlClass)
```

### Form Hidden Inputs

```csharp
void addFormHidden(string name, string value)
void addFormHidden(string name, string value, string htmlId)
void addFormHidden(string name, int value)
void addFormHidden(string name, int value, string htmlId)
void addFormHidden(string name, double value)
void addFormHidden(string name, double value, string htmlId)
void addFormHidden(string name, DateTime value)
void addFormHidden(string name, DateTime value, string htmlId)
void addFormHidden(string name, bool value)
void addFormHidden(string name, bool value, string htmlId)
```

### Filters

Filters persist across page loads using visit properties.

#### Get Filter Values

```csharp
string getFilterText(string filterHtmlName, string viewName)
int getFilterInteger(string filterHtmlName, string viewName)
bool getFilterBoolean(string filterHtmlName, string viewName)
DateTime? getFilterDate(string filterHtmlName, string viewName)
```

| Parameter | Description |
|-----------|-------------|
| `filterHtmlName` | The HTML name attribute of the filter input |
| `viewName` | Unique name for the view/form (e.g., "attendee list"). Prevents filter collisions across pages. |

#### Add Filter UI Elements

```csharp
void addFilterGroup(string caption)
void addFilterCheckbox(string caption, string filterHtmlName, string htmlValue, bool selected)
void addFilterRadio(string caption, string filterHtmlName, string filterValue, bool filterSelected)
void addFilterTextInput(string caption, string filterHtmlName, string filterValue)
void addFilterDateInput(string caption, string filterHtmlName, DateTime? filterValue)
void addFilterSelect(string caption, string filterHtmlName, List<NameValueSelected> options)
void addFilterSelectContent(string caption, string filterHtmlName, int filterValue, string content, string sqlCriteria)
void addFilterSelectContent(string caption, string filterHtmlName, int filterValue, string content, string sqlCriteria, string nonCaption)
```

#### Active Filters (removable pills)

```csharp
void addActiveFilter(string caption, string removeFilterRequestName, string requestNameToClear)
```

### Render

```csharp
string getHtml()    // render the layout to HTML
```

### CSS Style Constants (AfwStyles)

```csharp
AfwStyles.afwWidth10 through AfwStyles.afwWidth100      // percentage widths (10% increments)
AfwStyles.afwWidth10px through AfwStyles.afwWidth500px   // pixel widths
AfwStyles.afwTextAlignRight, AfwStyles.afwTextAlignLeft, AfwStyles.afwTextAlignCenter
AfwStyles.afwMarginLeft100px through AfwStyles.afwMarginLeft500px
```

### NameValueSelected (helper class)

```csharp
public class NameValueSelected(string name, string value, bool selected)
```

Used for select filter options.

---

## LayoutBuilderListBaseClass

Inherits `LayoutBuilderBaseClass`. Creates tabular list/report layouts with columns, rows, pagination, sorting, search, and ellipsis menus.

Constructor: `LayoutBuilderListBaseClass(CPBaseClass cp)`

### Pagination Properties

| Property | Type | Description |
|----------|------|-------------|
| `paginationPageSizeDefault` | `int` (get/set) | Default records per page |
| `paginationPageSize` | `int` (read-only) | Current page size (may be changed by user) |
| `paginationPageNumber` | `int` (read-only) | Current 1-based page number |
| `paginationRecordAlias` | `string` (get/set) | Replaces "records" in "# records found" |
| `recordCount` | `int` (get/set) | Total records in dataset (not just displayed page) |

```csharp
void paginationReset()   // reset to page 1 (call before accessing pagination when filter changes)
```

### SQL Helpers (read-only)

| Property | Type | Description |
|----------|------|-------------|
| `sqlSearchTerm` | `string` | SQL-safe search term from user's search box input |
| `sqlOrderBy` | `string` | SQL ORDER BY clause from user clicking column headers |

### Column Definition

Set these properties before calling `addColumn()`:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `columnName` | `string` | | Internal name for the column |
| `columnCaption` | `string` | | Display header caption |
| `columnCaptionClass` | `string` | | CSS class for header cell |
| `columnCellClass` | `string` | | CSS class for data cells |
| `columnSortable` | `bool` | `false` | Enable click-to-sort on column header |
| `columnVisible` | `bool` | `true` | Show column in rendered output |
| `columnDownloadable` | `bool` | `false` | Include column in CSV download |
| `columnWidthPercent` | `int` | `10` | Column width as percentage (1-100) |

```csharp
void addColumn()
void addColumn(ReportListColumnBaseClass column)
```

**ReportListColumnBaseClass** has the same properties as the column* properties above.

### Row and Cell Operations

```csharp
void addRow()
void addRowClass(string styleClass)
bool excludeRowFromDownload { get; set; }   // exclude current row from CSV download

// Set cell content — multiple type overloads
void setCell(string content)
void setCell(string reportContent, string downloadContent)
void setCell(string reportContent, int downloadContent)
void setCell(string reportContent, double downloadContent)
void setCell(string reportContent, DateTime downloadContent)
void setCell(string reportContent, bool downloadContent)
void setCell(int content)
void setCell(double content)
void setCell(bool content)
void setCell(DateTime? content)
// ... additional overloads for separate download values
```

### Row Ellipsis Menu

```csharp
void addRowEllipseMenuItem(string name, string url)
```

Adds a dropdown menu item to the current row's action ellipsis (...) button.

### Complete List Example

```csharp
public override object Execute(CPBaseClass cp) {
    var list = cp.AdminUI.CreateLayoutBuilderList();
    list.title = "Staff Directory";
    list.callbackAddonGuid = cp.Addon.ccGuid;
    list.isOuterContainer = true;
    list.paginationPageSizeDefault = 25;

    // Define columns
    list.columnName = "name";
    list.columnCaption = "Name";
    list.columnSortable = true;
    list.columnWidthPercent = 40;
    list.columnDownloadable = true;
    list.addColumn();

    list.columnName = "email";
    list.columnCaption = "Email";
    list.columnSortable = true;
    list.columnWidthPercent = 40;
    list.columnDownloadable = true;
    list.addColumn();

    list.columnName = "actions";
    list.columnCaption = "";
    list.columnWidthPercent = 20;
    list.addColumn();

    // Build query with search/sort/pagination
    string criteria = "(active>0)";
    if (!string.IsNullOrEmpty(list.sqlSearchTerm)) {
        criteria += $" and (name like '%{list.sqlSearchTerm}%' or email like '%{list.sqlSearchTerm}%')";
    }
    string orderBy = string.IsNullOrEmpty(list.sqlOrderBy) ? "name" : list.sqlOrderBy;

    var people = DbBaseModel.createList<PersonModel>(cp, criteria, orderBy, list.paginationPageSize, list.paginationPageNumber);
    list.recordCount = /* total count query */;

    // Add rows
    foreach (var person in people) {
        list.addRow();
        list.setCell(person.name);
        list.setCell(person.email);
        list.setCell("");
        list.addRowEllipseMenuItem("Edit", $"/admin?id={person.id}");
        list.addRowEllipseMenuItem("Delete", $"/admin?delete={person.id}");
    }

    return list.getHtml();
}
```

---

## LayoutBuilderNameValueBaseClass

Creates name-value pair form layouts with optional fieldsets. Used for settings/configuration forms.

Constructor: `LayoutBuilderNameValueBaseClass(CPBaseClass cp)`

### Row Properties

Set these before calling `addRow()` or `addColumn()`:

| Property | Type | Description |
|----------|------|-------------|
| `rowName` | `string` | Caption/label for the row |
| `rowValue` | `string` | HTML content for the value (text input, checkbox, select, etc.) |
| `rowHelp` | `string` | Optional help text for the row |
| `rowHtmlId` | `string` | HTML id of the input element (links the caption label) |

### Methods

```csharp
void addRow()                                    // add row, set rowName/rowValue/rowHelp after
void addRow(string rowHeading)                   // add row with a section heading
void addRow(string rowHeading, string instructions)  // add row with heading and instructions
void addColumn()                                 // add a second column to current row
void openFieldSet(string caption)                // start a bordered group with legend caption
void closeFieldSet()                             // end the fieldset
```

### Example

```csharp
var form = cp.AdminUI.CreateLayoutBuilderNameValue();
form.title = "Site Settings";
form.isOuterContainer = true;
form.addFormButton("Save");

form.openFieldSet("General");

form.addRow();
form.rowName = "Site Name";
form.rowValue = cp.Html.InputText("siteName", cp.Site.GetText("siteName"));
form.rowHelp = "The display name for this site";

form.addRow();
form.rowName = "Enable Feature X";
form.rowValue = cp.Html.CheckBox("featureX", cp.Site.GetBoolean("featureX"));

form.closeFieldSet();

form.addFormHidden("formId", "siteSettings");
return form.getHtml();
```

---

## LayoutBuilderTabbedBodyBaseClass

Creates tabbed navigation layouts.

Constructor: `LayoutBuilderTabbedBodyBaseClass(CPBaseClass cp)`

### Tab Properties

| Property | Type | Description |
|----------|------|-------------|
| `tabCaption` | `string` | Display text on the tab |
| `tabStyleClass` | `string` | Custom CSS class for the tab |
| `tabLink` | `string` | URL the tab navigates to when clicked |

### Methods

```csharp
void addTab()                      // add a new tab entry
void setActiveTab(string caption)  // highlight a tab by its caption
```

### Example

```csharp
var tabs = cp.AdminUI.CreateLayoutBuilderTabbedBody();
tabs.title = "Dashboard";

tabs.tabCaption = "Overview";
tabs.tabLink = $"?tab=overview";

tabs.addTab();
tabs.tabCaption = "Settings";
tabs.tabLink = $"?tab=settings";

tabs.setActiveTab("Overview");
tabs.body = overviewHtml;
return tabs.getHtml();
```

---

## LayoutBuilderTwoColumnLeftBaseClass / LayoutBuilderTwoColumnRightBaseClass

Two-column layouts with emphasis on left or right column.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `contentLeft` | `string` | HTML for the left column |
| `contentRight` | `string` | HTML for the right column |

Plus all base properties (`title`, `body`, messages, etc.)

### Example

```csharp
var layout = cp.AdminUI.CreateLayoutBuilderTwoColumnRight();
layout.title = "Record Detail";
layout.contentLeft = summaryHtml;
layout.contentRight = detailFormHtml;
return layout.getHtml();
```

---

## Common Workflow Pattern

1. Create the appropriate layout builder via `cp.AdminUI.CreateLayoutBuilder*()`
2. Set `title`, `isOuterContainer`, `callbackAddonGuid`
3. Add content (columns/rows for lists, name/value pairs for forms, etc.)
4. Add form buttons and hidden fields
5. Add filters if needed
6. Call `getHtml()` to render

### Handling Form Submissions

```csharp
string button = cp.Doc.GetText("button");
if (!string.IsNullOrEmpty(button)) {
    switch (button) {
        case "Save":
            // process form
            form.successMessage = "Settings saved.";
            break;
        case "Delete":
            // process delete
            break;
    }
}
```

### Handling Downloads

```csharp
var list = cp.AdminUI.CreateLayoutBuilderList();
list.allowDownloadButton = true;

if (list.requestDownload) {
    // Return CSV data instead of HTML
    return csvData;
}

// Normal HTML rendering
return list.getHtml();
```
