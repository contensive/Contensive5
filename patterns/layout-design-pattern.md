
# Layout Design Pattern

> All patterns and API reference: [Patterns Index](https://raw.githubusercontent.com/contensive/Contensive5/refs/heads/master/patterns/index.md)

## Overview
UI Design and server-side code at managed separately. All UI design elements are managed with layout files for addon design and template files for website design. Server-side code are created as dotnet classes compatible with the Contensive addon pattern.

## Base architecture and other pattens

- [Contensive Architecture](https://github.com/contensive/Contensive5/blob/master/patterns/contensive-architecture.md)

Best practice is to never include html code elements in the programming code. Html code is developed in html files in the /ui folder and is deployed to Layout records. Programming code reads layout records with the cp.Layout methods and merges html with data using the cp.Mustache methods.

When an addon returns html, or any textural content, the content should stored in a layout file and merged with programmatically created data with the mustache templating pattern. The textural content should be created in text or html files stored in the /ui folder of the repository with any referenced assets for the layout in subfolders of the /ui folder. This /ui folder manages all html and design documents managed by the designer.

### Layout Structure Best Practices

Html layouts should include:
- **Inline styles at the front** - Ensures styles are scoped to the widget and load immediately without external file dependencies
- **JavaScript at the end** - Allows DOM elements to load before script execution

This pattern keeps widgets self-contained and avoids conflicts when multiple widgets are used on the same page. For production deployments requiring optimization, consider using a build process to extract and bundle styles/scripts.

### Development Workflow

1. **Designer creates HTML layouts** in `/UI` folder with assets in subfolders
2. **During design phase:** Designers use the html-import tool to test layouts by deploying to Layout records
3. **Code references layouts** using `cp.Layout.GetLayout()` methods
4. **Data merging** uses `cp.Mustache.Render()` to combine layout with view models

### Deployment Workflow (Installation)

#### UI folder structure and build packaging

The `/ui/` folder contains four subfolders for different types of UI assets:

```
ui/
  wwwFiles/       → public web root files (CSS, JS, images served via HTTP)
  cdnFiles/       → CDN/content files
  privateFiles/   → server-side private files (not publicly accessible)
  layoutFiles/    → HTML layout templates used by cp.Layout methods
```

During the build, the build script compresses each subfolder into a zip file of the same name and copies it to the collection folder:

```
ui/wwwFiles/      → collections/{CollectionName}/wwwFiles.zip
ui/cdnFiles/      → collections/{CollectionName}/cdnFiles.zip
ui/privateFiles/  → collections/{CollectionName}/privateFiles.zip
ui/layoutFiles/   → collections/{CollectionName}/layoutFiles.zip
```

Each zip file must have a corresponding `<Resource>` entry in the collection XML so the installer knows to extract it during deployment:

```xml
<Resource name="wwwFiles.zip" type="www" path="wwwFiles" />
<Resource name="cdnFiles.zip" type="cdn" path="cdnFiles" />
<Resource name="privateFiles.zip" type="privateFiles" path="privateFiles" />
<Resource name="layoutFiles.zip" type="content" path="layoutFiles" />
```

You only need resource entries for subfolders that contain files. If a subfolder is empty or unused, you can omit its resource entry.

#### Special handling for layoutFiles

The `layoutFiles.zip` resource extracts HTML layout files to the server's file system. There are two ways these files become Layout database records:

**Option A — Meta tags in the HTML file (recommended for layoutFiles resources)**

When layout HTML files are installed via a `layoutFiles` resource, the installer automatically scans each HTML file for meta tags and creates or updates the corresponding database records. No OnInstall addon code is needed.

Add these meta tags to the `<head>` of each layout HTML file:

```html
<meta name="layout" content="My Layout Name">
<meta name="layout-guid" content="{XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX}">
```

- `name="layout"` — the layout record name. This is required for the installer to recognize the file as a layout.
- `name="layout-guid"` — the GUID that uniquely identifies the layout record. When provided, the installer uses this GUID to find or create the record, ensuring a stable identity across installs. If omitted, the installer falls back to name-based lookup which can cause duplicates if the name changes.

Both meta tags should always be included. The GUID ensures idempotent installs and prevents duplicate records.

**Option B — OnInstall addon with `cp.Layout.updateLayout()`**

If the HTML file does not contain meta tags, the OnInstall addon must call `cp.Layout.updateLayout()` for each layout file to read it from disk and create or update the corresponding record in the Layouts database table.

Without either meta tags or an `updateLayout()` call, the HTML file will exist on disk but `cp.Layout.GetLayout()` will return empty because there is no database record pointing to it.

#### Complete checklist for adding a new layout

Every new layout requires all four steps below. Missing any step will cause the layout to either not deploy or not be available at runtime.

**Step 1 — Create the HTML layout file** in `/ui/layoutFiles/` with meta tags:

```html
<!-- ui/layoutFiles/MyNewLayout.html -->
<html>
<head>
    <meta name="layout" content="My New Layout">
    <meta name="layout-guid" content="{XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX}">
</head>
<body>
    <!-- layout content here -->
</body>
</html>
```

With these meta tags, the installer automatically creates or updates the Layout database record when the `layoutFiles.zip` resource is installed. If you use meta tags, Step 4 (the OnInstall `updateLayout()` call) is optional.

**Step 2 — Add constants** for the layout GUID, name, and filename in `constants.cs`:

```csharp
public const string guidLayoutMyNewLayout = "{XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX}";
public const string nameLayoutMyNewLayout = "My New Layout";
public const string pathFilenameLayoutMyNewLayout = "MyNewLayout.html";
```

**Step 3 — Verify the `layoutFiles.zip` resource exists** in the collection XML:

```xml
<Resource name="layoutFiles.zip" type="content" path="layoutFiles" />
```

This single entry covers all files in the `layoutFiles/` folder. If it already exists (because other layouts are deployed), no XML change is needed.

**Step 4 — Add `cp.Layout.updateLayout()` call** in the OnInstall addon:

```csharp
cp.Layout.updateLayout(
    Constants.guidLayoutMyNewLayout,
    Constants.nameLayoutMyNewLayout,
    Constants.pathFilenameLayoutMyNewLayout  // filename only, no path prefix
);
```

**Important:** The third argument is the **filename only** (e.g., `"MyNewLayout.html"`), not a path. The platform resolves the file from the deployed `layoutFiles` folder automatically.

#### What happens if a step is missed

| Missing step | Result |
|-------------|--------|
| Step 1 (no HTML file) | Nothing to zip or deploy — `updateLayout()` finds no file to read |
| Step 3 (no resource entry) | The zip file is in the collection package but the installer ignores it — files are not extracted to the server |
| Step 4 (no `updateLayout()` call) | The file is on disk but no Layout database record exists — `cp.Layout.GetLayout()` returns empty |

### Example: Using Layouts with Mustache Templates

**C# Addon Code (Server/MyAddon/MyWidget.cs):**
```csharp
public override object Execute(CPBaseClass cp) {
    // Get layout from database
    string layout = cp.Layout.GetLayout("MyWidgetLayout");

    // Create data model
    var viewModel = new {
        headline = "Welcome",
        description = "This is content from the database",
        buttonText = "Learn More",
        buttonUrl = "/about"
    };

    // Merge with Mustache
    return cp.Mustache.Render(layout, viewModel);
}
```

**HTML Layout File (UI/MyWidgetLayout.html):**
```html
<style>
.myWidget { padding: 20px; }
</style>

<div class="myWidget">
    <h2>{{headline}}</h2>
    <p>{{description}}</p>
    <a href="{{buttonUrl}}">{{buttonText}}</a>
</div>

<script>
console.log('Widget loaded');
</script>
```

**OnInstall Addon:**
```csharp
public override object Execute(CPBaseClass cp) {
    cp.Layout.updateLayout(
        layoutGuid: "{12345678-1234-1234-1234-123456789012}",
        layoutName: "MyWidgetLayout",
        layoutPathFilename: "MyWidgetLayout.html"
    );
    return string.Empty;
}
```

### Layout Reference Constants

Define layout references in a Constants class for type-safety and maintainability:

```csharp
public static class Constants {
    public const string guidLayoutPageWidgetExample = "{12345678-1234-1234-1234-123456789012}";
    public const string nameLayoutPageWidgetExample = "Page Widget Example Layout";
    public const string pathFilenameLayoutPageWidgetExample = "PageWidgetExampleLayout.html";
}
```

Use these constants when calling `cp.Layout.updateLayout()` and referencing layouts in addon code.

### Key API Methods

**cp.Layout Methods:**
- `cp.Layout.GetLayout(string nameOrGuid)` - Retrieves layout content from database
- `cp.Layout.updateLayout(string guid, string name, string pathFilename)` - Updates Layout database record from file resource

**cp.Mustache Methods:**
- `cp.Mustache.Render(string template, object data)` - Merges Mustache template with data model
- Supports standard Mustache syntax: `{{variable}}`, `{{#section}}`, `{{^inverse}}`, `{{{unescaped}}}`

## Full Examples
Complete examples are in the /examples folder of this repository
