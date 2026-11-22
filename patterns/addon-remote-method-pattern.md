
# Remote Method Pattern

> All patterns and API reference: [Patterns Index](https://raw.githubusercontent.com/contensive/Contensive5/refs/heads/master/patterns/index.md)

## Overview

A Remote Method is an addon that is executed when an HTTP request matches the addon's name as a path segment. The addon can update application data and its return value becomes the HTTP response body.

## URL Format

Remote methods are invoked as path segments, not query string parameters. The addon name becomes a route in the URL path.

```
CORRECT:   /remoteMethodName
WRONG:     ?remotemethod=remoteMethodName
```

For example, an addon named `searchResultsRemote` is called at:
```
GET  {baseURL}/searchResultsRemote
POST {baseURL}/searchResultsRemote
```

State-changing operations (create, update, delete) must use POST — see [POST for State-Changing Remote Methods](security-best-practices.md#post-for-state-changing-remote-methods).

## Architecture

## The Dotnet Component
- The addon references a Dotnet class that includes an Execute method that returns the rendered layout

## HTML Document Mode

Remote methods that serve complete standalone pages (admin interfaces, reports, custom endpoints) typically need the `htmlDocument` property set to `Yes`.

### What htmlDocument Does

The `htmlDocument` property controls whether the addon's output bypasses the page template assembly pipeline:

- **htmlDocument=No (default)**: The addon returns an HTML fragment that the platform integrates into the page template system. The platform applies master pages, page templates, and standard head/body assembly.

- **htmlDocument=Yes**: The addon returns a complete HTML document that becomes the entire HTTP response. The platform bypasses page templates and serves the addon's return value directly to the browser.

### When to Use htmlDocument=Yes

Use `htmlDocument=Yes` for remote methods that need complete control over the HTTP response:

- Standalone admin interfaces (reports, dashboards, tools)
- Custom API endpoints returning complete HTML
- Pages that require specific document structure without site chrome
- Legacy integrations delivering complete HTML documents

Use `htmlDocument=No` (default) for remote methods that should be integrated into the site's standard page layout.

### Layout Files and htmlDocument

**Important**: The `htmlDocument` property controls **runtime behavior** (page template assembly), not **design-time file structure**.

Layout files in `ui/layoutFiles/` should **always be complete HTML documents** (with `<!doctype html>`, `<html>`, `<head>`, `<body>`) regardless of the `htmlDocument` setting. This allows:

- Designers to preview and edit layouts as standalone files
- The build system to read meta tags from `<head>` for auto-registration
- Proper syntax highlighting and validation in editors

The distinction is:
- **Design-time**: Layout files are complete HTML documents for designer workflow
- **Runtime**: The `htmlDocument` property determines whether the platform bypasses page template assembly

### Configuration

Set `htmlDocument` in the addon XML:

```xml
<Addon name="my-remote-method" guid="{...}" type="Add-on">
    <DotNetClass><![CDATA[MyNamespace.MyRemoteMethodAddon]]></DotNetClass>
    <RemoteMethod>Yes</RemoteMethod>
    <HtmlDocument>Yes</HtmlDocument>
</Addon>
```

### Example: Complete Standalone Page

**Addon XML** (in collection XML):
```xml
<Addon name="seo-analytics-report" guid="{D1E2F3A4-B5C6-4D7E-8F9A-0B1C2D3E4F5A}" type="Add-on">
    <DotNetClass><![CDATA[Contensive.Analytics.SeoAnalyticsReportAddon]]></DotNetClass>
    <RemoteMethod>Yes</RemoteMethod>
    <HtmlDocument>Yes</HtmlDocument>
</Addon>
```

**Layout File** (ui/layoutFiles/SeoAnalyticsReport.html):
```html
<!doctype html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <meta name="layout" content="SEO Analytics Report">
    <meta name="layout-guid" content="{9E3F1A2B-7C4D-5E6F-8A9B-0C1D2E3F4A5B}">
    <title>SEO Analytics Report</title>
    <style>
        /* Complete styles for standalone page */
        body { font-family: sans-serif; margin: 0; padding: 20px; }
    </style>
</head>
<body>
    <h1>{{title}}</h1>
    <div class="content">{{content}}</div>
    <script>
        // JavaScript for standalone functionality
    </script>
</body>
</html>
```

**Addon Code**:
```csharp
public class SeoAnalyticsReportAddon : AddonBaseClass {
    public override object Execute(CPBaseClass cp) {
        try {
            if (!cp.User.IsAdmin) {
                return "<p>Admin access required</p>";
            }

            // Get the complete HTML document layout
            string layout = cp.Layout.GetLayout(
                Constants.guidLayoutSeoAnalyticsReport,
                Constants.nameLayoutSeoAnalyticsReport,
                "SeoAnalyticsReport.html");

            var viewModel = new {
                title = "SEO Analytics Report",
                content = "Report content here"
            };

            // Return complete HTML document - platform serves it as-is
            return cp.Mustache.Render(layout, viewModel);
        } catch (Exception ex) {
            cp.Site.ErrorReport(ex);
            return string.Empty;
        }
    }
}
```

Accessed at: `https://example.com/seo-analytics-report`

The browser receives the complete HTML document from the layout file without any page template wrapping.

## Steps to Scaffold a new Remote Method Addon

1) follow the pattern established in the example RemoteMethodExample solution
2) Determine the Addon Collection in which this addon will be created. If one does not exists, first scaffold  a new Addon Collection repository with collection and new dotnet solution.
3) Determine The project in the dotnet solution. 
4) In the Addon Collection create a new addon node
    - Create a new Addon node
    - add a name that will be the endpoint that executes the addon. Best practice is to use only lowercase characters
    - ceate a new guid for the addon
    - create a dotnetclass node and use the dotnet projects namespace to the Addons folder
    - mark the node remotemethod true to allow this addon to be executed from an endpoint
5) In the dotnet project, the Addons folder create a new class that inherits CPBaseClass. 
