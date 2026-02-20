
# Layout Design Pattern

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

1. **Package preparation:** Zip the `/UI` folder with all subfolders
2. **Copy to collection:** Place UI.zip in `/Collections/{CollectionName}` folder
3. **Reference in XML:** Add resource node in addon collection XML file pointing to UI.zip
4. **OnInstall execution:** The OnInstall addon calls `cp.Layout.updateLayout()` for each layout to update Layout database records
5. **Resource extraction:** Collection installer automatically extracts UI.zip files to destination application

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
