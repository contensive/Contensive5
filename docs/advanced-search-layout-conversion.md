# Advanced Search: Convert to Layout Design Pattern

## Goal

Convert `ListViewAdvancedSearch.cs` from inline C#-generated HTML to the layout-design-pattern, using:
- **LayoutBuilder** for the page shell (title, description, buttons, form wrapper)
- **A Mustache layout template** (`AdvancedSearchBody.html`) for the search field rows body content
- **afw-prefixed CSS classes** consistent with the modern admin UI

This allows UI changes to be deployed by editing the HTML layout file without rebuilding the Processor assembly.

---

## Current Architecture

**File:** `source/Processor/Addons/AdminSite/Views/ListViewAdvancedSearch.cs`

The `get()` method:
1. Builds button bar HTML via `AdminUIController.getSectionButtonBar()`
2. Builds title/description via `AdminUIController.getSectionHeader()`
3. Iterates over content fields, generating radio buttons + value inputs per field type (Date, Numeric, Text, Boolean, File, Lookup)
4. Wraps each field row with `AdminUIController.getEditRow()`
5. Wraps content in `ccPanel3DReverse ccAdminEditBody` div
6. Assembles everything into a form with hidden fields (`fieldcnt`, `rnAdminSourceForm`)

All HTML is built inline in C# string concatenation.

---

## Target Architecture

### Layer 1: LayoutBuilder (page shell)

Use `LayoutBuilderClass` (via `cp.AdminUI.CreateLayoutBuilder()`) to provide:
- `title` = "{ContentName} Advanced Search"
- `description` = "Enter criteria for each field..."
- `body` = Mustache-rendered search rows (from Layer 2)
- Form buttons: Cancel, Search (via `addFormButton()`)
- Hidden fields: `fieldcnt`, `rnAdminSourceForm` (via `addFormHidden()`)
- Per-field hidden `fieldname{N}` values (via `addFormHidden()`)

The LayoutBuilder handles the form tag, button bar, title, description wrapper, and afw-styled chrome.

### Layer 2: Mustache layout (search rows body)

A new layout file renders the search field rows. The C# code builds a view model with a list of field objects, each containing:
- `caption` - field display name
- `fieldIndex` - the field pointer (for form field naming)
- Row-type booleans: `isDate`, `isNumeric`, `isText`, `isBoolean`, `isFile`, `isLookup`
- A list of `radioOptions` (each with `name`, `value`, `checked`, `label`, `radioId`)
- `hasValueInput` - whether this row has a text/date input
- `valueInputHtml` - the rendered input element (text input or date input, pre-built in C# since `HtmlController.inputText_Legacy` / `HtmlController.inputDate` are the canonical input generators)

The Mustache template iterates `{{#fields}}` and uses sections like `{{#hasValueInput}}` to conditionally show the value input alongside the radio options.

### Why pre-render `valueInputHtml` in C#

The value inputs (`inputText_Legacy`, `inputDate`) include:
- Dynamic `onFocus` handlers referencing radio button IDs
- Core-dependent width settings (`core.siteProperties.defaultFormInputWidth`)
- HTML encoding of default values

These are tightly coupled to the C# input helpers. Pre-rendering them and passing as `{{{valueInputHtml}}}` (triple-mustache, unescaped) keeps the template clean while preserving exact input behavior.

---

## Implementation Steps

### Step 1 - Generate a GUID for the new layout

Run: `powershell -Command "[guid]::NewGuid().ToString('B').ToUpper()"` to get a GUID for the layout.

### Step 2 - Create the layout HTML file

**File:** `ui/layoutFiles/BaseAssets/AdvancedSearchBody.html`

```html
<!doctype html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <meta name="layout" content="Admin Advanced Search Body">
    <meta name="layout-guid" content="{GENERATED-GUID}">
    <title>Admin Advanced Search Body</title>
</head>
<body>
<div class="afwAdvancedSearch">
    {{#fields}}
    <div class="afwFormRow">
        <div class="afwFormRowName">{{caption}}</div>
        <div class="afwFormRowValue">
            <div class="d-flex flex-wrap align-items-center">
                {{#radioOptions}}
                <label class="d-inline-flex align-items-center me-3">
                    {{#checked}}
                    <input type="radio" name="{{name}}" value="{{value}}" id="{{radioId}}" class="me-1" checked>
                    {{/checked}}
                    {{^checked}}
                    <input type="radio" name="{{name}}" value="{{value}}" id="{{radioId}}" class="me-1">
                    {{/checked}}
                    {{label}}
                </label>
                {{/radioOptions}}
                {{#hasValueInput}}
                <div style="max-width:300px;flex:1 1 auto;">
                    {{{valueInputHtml}}}
                </div>
                {{/hasValueInput}}
            </div>
        </div>
    </div>
    {{/fields}}
</div>
</body>
</html>
```

Key design decisions:
- Uses `afwFormRow` / `afwFormRowName` / `afwFormRowValue` classes (already defined in AdminUILayoutBuilderBase.html) for the two-column label+content layout
- Radio buttons and value input are in a single `d-flex` row (same line)
- Value inputs use `form-control` class (Bootstrap) for modern styling
- Radio options are fully rendered from the view model (not hardcoded per type), so field-type logic stays in C# but the HTML structure comes from the template
- `{{{valueInputHtml}}}` uses triple-mustache to avoid HTML escaping

### Step 3 - Add constants

**File:** `source/Processor/Addons/AdminSite/Constants.cs`

Add inside the `Constants` class:
```csharp
internal const string guidLayoutAdvancedSearchBody = "{GENERATED-GUID}";
internal const string nameLayoutAdvancedSearchBody = "Admin Advanced Search Body";
internal const string pathFilenameLayoutAdvancedSearchBody = "BaseAssets\\AdvancedSearchBody.html";
```

### Step 4 - Create the view model class

**File:** `source/Processor/Addons/AdminSite/Models/AdvancedSearchViewModel.cs`

```csharp
using System.Collections.Generic;

namespace Contensive.Processor.Addons.AdminSite.Models {
    public class AdvancedSearchViewModel {
        public List<AdvancedSearchFieldViewModel> fields { get; set; } = new();
    }

    public class AdvancedSearchFieldViewModel {
        public string caption { get; set; }
        public int fieldIndex { get; set; }
        public List<AdvancedSearchRadioOption> radioOptions { get; set; } = new();
        public bool hasValueInput { get; set; }
        public string valueInputHtml { get; set; }
    }

    public class AdvancedSearchRadioOption {
        public string name { get; set; }
        public string value { get; set; }
        public bool @checked { get; set; }
        public string label { get; set; }
        public string radioId { get; set; }
    }
}
```

### Step 5 - Refactor `ListViewAdvancedSearch.get()`

Replace the current `get()` method body to:

1. **Create LayoutBuilder** for the page shell:
   ```csharp
   var layout = cp.AdminUI.CreateLayoutBuilder();
   layout.title = $"{adminData.adminContent.name} Advanced Search";
   layout.description = "Enter criteria for each field to identify and select your results. The results of a search will have to have all of the criteria you enter.";
   layout.isOuterContainer = true;
   ```

2. **Build the view model** by iterating fields (same loop logic as today), but instead of building HTML strings, populate `AdvancedSearchViewModel.fields` list with:
   - `caption` from `field.caption`
   - `radioOptions` list built per field type (same options as today: ignore/empty/not-empty/equals/gt/lt/includes/true/false)
   - `valueInputHtml` pre-rendered from `HtmlController.inputText_Legacy()` or `HtmlController.inputDate()` with the `form-control` class

3. **Render the body** via Mustache:
   ```csharp
   string bodyLayout = cp.Layout.GetLayout(
       Constants.guidLayoutAdvancedSearchBody,
       Constants.nameLayoutAdvancedSearchBody,
       Constants.pathFilenameLayoutAdvancedSearchBody
   );
   var viewModel = new AdvancedSearchViewModel { fields = fieldList };
   layout.body = cp.Mustache.Render(bodyLayout, viewModel);
   ```

4. **Add form buttons and hidden fields**:
   ```csharp
   layout.addFormButton(ButtonCancel);
   layout.addFormButton(ButtonSearch);
   layout.addFormHidden("fieldcnt", FieldCnt);
   layout.addFormHidden(rnAdminSourceForm, AdminFormList_AdvancedSearch);
   // per-field hidden fieldname values
   foreach field: layout.addFormHidden($"fieldname{ptr}", fieldName);
   ```

5. **Return the rendered page**:
   ```csharp
   return layout.getHtml();
   ```

### Step 6 - Verify `layoutFiles.zip` resource exists in collection XML

**File:** `source/Processor/aoBase51.xml`

Check that a `<Resource name="layoutFiles.zip" type="content" path="layoutFiles" />` entry exists. If already present (it should be, since other layouts exist), no change needed.

### Step 7 - Remove unused code

After the refactor:
- The `AdminUIController.getSectionHeader()`, `AdminUIController.getSectionButtonBar()`, and `AdminUIController.getEditRow()` calls are removed from this file (they remain available for other admin pages)
- The `StringBuilderLegacyController` stream assembly code is removed
- The `ccPanel3DReverse ccAdminEditBody` wrapper div is removed (LayoutBuilder handles the page chrome)

### Step 8 - Build and test

- Build the Processor project: `dotnet build source/Processor/Processor.csproj`
- Verify no compile errors
- Deploy and test the advanced search page visually to confirm:
  - Radio options and text input on same line
  - Light grey background (via afw styles)
  - Form-control styled text inputs
  - Search and Cancel buttons work
  - Search criteria are correctly saved and pre-populated on return

---

## Files Changed

| File | Change |
|------|--------|
| `ui/layoutFiles/BaseAssets/AdvancedSearchBody.html` | **New** - Mustache layout for search rows |
| `source/Processor/Addons/AdminSite/Constants.cs` | Add layout GUID/name/path constants |
| `source/Processor/Addons/AdminSite/Models/AdvancedSearchViewModel.cs` | **New** - View model classes |
| `source/Processor/Addons/AdminSite/Views/ListViewAdvancedSearch.cs` | Refactor to use LayoutBuilder + Mustache |

## Files Unchanged

| File | Reason |
|------|--------|
| `source/Processor/aoBase51.xml` | `layoutFiles.zip` resource should already exist |
| `source/Processor/Controllers/AdminUI/AdminUIController.cs` | No changes - getEditRow etc. still used by other pages |
| `cdn/source/adminUI.css` | No changes - ccAdvSearchText class retained for backward compat but no longer used by this page |

---

## Risks and Mitigations

| Risk | Mitigation |
|------|-----------|
| Mustache `checked` keyword conflict | Use `@checked` in C# (verbatim identifier) which serializes to `checked` in JSON for Mustache |
| Value input `onFocus` handler references radio IDs | Pre-render input HTML in C# with correct IDs, pass via triple-mustache `{{{valueInputHtml}}}` |
| LayoutBuilder form wrapper conflicts with admin form mechanism | Use `addFormHidden()` to inject admin hidden fields into the LayoutBuilder's form |
| Layout file not found at runtime (first deploy) | Layout file has meta tags for auto-registration; `cp.Layout.GetLayout()` three-arg overload falls back to CDN file path |
