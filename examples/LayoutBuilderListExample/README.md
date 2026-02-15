# CreateLayoutBuilderList Pattern Example

This folder contains a complete example demonstrating the `CreateLayoutBuilderList` pattern used in Contensive AdminUI development.

## Overview

The `CreateLayoutBuilderList` method creates a builder object for constructing paginated, searchable, sortable list views in the Contensive Admin UI. This pattern provides a consistent way to display data tables with built-in features like:

- Column configuration with custom CSS classes
- Pagination
- Search/filtering
- Sorting
- Form integration
- Custom HTML sections

## Example File

**AccountManagerUsersAddon.cs** - Main example showing how to use the LayoutBuilderList pattern to create a user management list view.

## Key Pattern Elements

### 1. Initialize the LayoutBuilder

```csharp
var layoutBuilder = cp.AdminUI.CreateLayoutBuilderList();
layoutBuilder.title = "Users";
layoutBuilder.description = "Users represent individuals in accounts...";
layoutBuilder.callbackAddonGuid = Constants.guidAddonUserList;
layoutBuilder.includeForm = true;
```

### 2. Configure Columns

For each column, set properties then call `addColumn()`:

```csharp
layoutBuilder.columnCaption = "User";
layoutBuilder.columnCaptionClass = "afwTextAlignLeft";
layoutBuilder.columnCellClass = "afwTextAlignLeft";
layoutBuilder.addColumn();
```

### 3. Use Built-in Search and Pagination Properties

The LayoutBuilder automatically populates these from query string parameters:

- `layoutBuilder.sqlSearchTerm` - User's search input
- `layoutBuilder.sqlOrderBy` - Selected sort column
- `layoutBuilder.paginationPageNumber` - Current page (1-based)
- `layoutBuilder.paginationPageSize` - Records per page

### 4. Set Record Count

After querying total records:

```csharp
layoutBuilder.recordCount = totalRecordCount;
```

### 5. Populate Rows

For each data row:

```csharp
layoutBuilder.addRow();
layoutBuilder.setCell("Cell content for column 1");
layoutBuilder.setCell("Cell content for column 2");
// ... one setCell() per column
```

### 6. Add Form Elements (Optional)

```csharp
layoutBuilder.addFormButton(Constants.buttonCancel);
layoutBuilder.addFormHidden("fieldName", "fieldValue");
```

### 7. Render the HTML

```csharp
return layoutBuilder.getHtml();
```

## Implementation Pattern Flow

The example in `AccountManagerUsersAddon.cs` follows this flow:

1. **Authentication/Authorization** (lines 29-35) - Verify user permissions
2. **Portal Validation** (lines 33-35) - Ensure running in correct context
3. **Form Processing** (line 38) - Handle button clicks and form submissions
4. **Initialize LayoutBuilder** (lines 79-83) - Set title, description, form settings
5. **Configure Columns** (lines 86-104) - Define table structure
6. **Build SQL with Filters** (lines 106-125) - Use `sqlSearchTerm` for WHERE clause
7. **Get Record Count** (lines 110-125) - Query total for pagination
8. **Query Data with Pagination** (lines 128-143) - Use `paginationPageNumber` and `paginationPageSize` for OFFSET/FETCH
9. **Populate Rows** (lines 148-178) - Loop through results, call `addRow()` and `setCell()`
10. **Add Form Elements** (lines 188-192) - Buttons and hidden fields
11. **Render** (line 194) - Return HTML

## Supporting Classes

The example includes minimal stub implementations of supporting classes to demonstrate dependencies:

- **ApplicationModel** - Wrapper for CPBaseClass providing application context
- **Constants** - Application constants for form fields, GUIDs, etc.
- **SecurityController** - Authorization helpers
- **PortalController** - Portal navigation helpers
- **AccountEditAddon** - Related addon referenced for navigation

## Notes

- This example is self-contained for documentation purposes
- Framework classes (CPBaseClass, AddonBaseClass, LayoutBuilderList) are not included
- The code demonstrates the pattern but is not meant to execute standalone
- SQL queries use SQL Server syntax (OFFSET/FETCH for pagination)
