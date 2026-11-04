# Page Template Import Enhancements

## Overview

This plan addresses the challenge of making page templates portable and agent-editable. Currently, templates cannot be round-tripped (pulled from production, edited locally, and pushed to staging) because asset references are split between the template HTML and addon dependencies, causing broken stylesheets and duplicate library loading.

## Goals

1. Enable agents to open a repo with page templates in `ui/layoutFiles/`, make changes, zip the HTML with assets, and upload to a staging site with templates rendering correctly
2. Make templates self-contained with embedded asset references
3. Prevent duplicate library loading when both templates and addons reference common libraries (jQuery, Bootstrap)
4. Maintain designer-friendly workflow with minimal new syntax
5. Preserve existing layoutFiles → wwwFiles asset deployment pattern

## Problem Statement

### Current Issues

1. **Asset references are split** - Template stylesheets referenced via addon dependencies rather than in template HTML
2. **Duplicate library loading** - No deduplication when both template and addons reference jQuery/Bootstrap
3. **Can't round-trip templates** - Can't pull template from prod, edit locally, and push back without losing dependencies
4. **Template assets linked through addons** - Link to stylesheet not in template but through separate "asset" addon

### Example Failure Scenario

1. Designer downloads production template HTML
2. Template has addon dependency on "Bootstrap" addon
3. Template has addon dependency on "Site Assets" addon (which only contains `<link>` to stylesheet)
4. Downloaded HTML has no stylesheet references in the HTML itself
5. Designer edits template locally, zips it, uploads to staging
6. Template renders without styles because addon dependencies were lost

## Solution Design

### Hybrid Approach (Option C)

Combine automatic library detection with explicit meta tag declarations:

1. **Designers embed `<link>` and `<script>` tags directly in template HTML** (head or body)
2. **System auto-detects common libraries** (jQuery, Bootstrap) by scanning HTML content during import
3. **Designers can add `<meta name="provides-addon">` tags** for explicit dependency declarations
4. **Clear error messages** when duplicates occur, showing cause and fix

### Key Principles

- **Preserve entire `<head>` section** - Store in new `PageTemplates.Head` field
- **Preserve entire `<body>` section** - Continue storing in existing `PageTemplates.Body` field
- **Allow asset references anywhere** - Designers can put `<link>` and `<script>` in head or body, both are preserved
- **Append system elements after template head** - Addon styles/scripts, dynamic meta tags appended after template's head elements
- **No URL rewriting** - Asset paths work exactly as they do in designer's local folders
- **No asset migration** - Keep current pattern: HTML → `privateFiles/layoutFiles/`, assets → `wwwFiles/`
- **Scan during import, not render** - One-time library detection cost at import, not per-request

## Implementation Plan

### 1. Database Schema Changes

Add to Page Templates table via collection XML (`aoBase51.xml`):

```xml
<Field>
    <Name>Head</Name>
    <Caption>Head HTML</Caption>
    <FieldType>LongText</FieldType>
    <EditTab>Content</EditTab>
    <HelpMessage>The complete HTML head section from the template file. System-generated elements (addon styles/scripts, dynamic meta tags) are appended after this content during rendering.</HelpMessage>
</Field>
<Field>
    <Name>ProvidedAddons</Name>
    <Caption>Provided Addon Dependencies</Caption>
    <FieldType>Text</FieldType>
    <EditTab>Content</EditTab>
    <HelpMessage>Comma-separated list of addon dependencies satisfied by this template (e.g., "jQuery,Bootstrap 5"). These dependencies will not be loaded again by addons to prevent duplicates.</HelpMessage>
</Field>
```

### 2. HTML Import Tool Updates

Update HTML import processing (`source/Processor/Controllers/htmlController.cs` or relevant controller):

#### During Import/Install:

1. **Parse HTML structure:**
   - Extract entire `<head>` section → store in `PageTemplates.Head`
   - Extract entire `<body>` section → store in `PageTemplates.Body`
   - Parse `<meta name="provides-addon" content="...">` tags from head

2. **Detect common libraries** by scanning both head and body for these patterns:
   ```csharp
   var libraryPatterns = new Dictionary<string, string[]> {
       { "jQuery", new[] { "jquery.min.js", "jquery.js", "jquery-3", "jquery-2", "jquery-1" } },
       { "Bootstrap 4", new[] { "bootstrap/4", "bootstrap.min.js", "bootstrap.min.css" } },
       { "Bootstrap 5", new[] { "bootstrap/5", "bootstrap.min.js", "bootstrap.min.css" } }
   };
   ```

3. **Combine detected libraries with meta tag declarations** → store as comma-separated list in `PageTemplates.ProvidedAddons`

4. **Store processed content:**
   - `Head` field = full head section
   - `Body` field = full body section
   - `ProvidedAddons` field = "jQuery,Bootstrap 5,..." (detected + declared)

#### Asset Handling (No Changes):

- HTML files → `privateFiles/layoutFiles/`
- All other files (CSS, JS, images) → `wwwFiles/` preserving folder structure
- No URL rewriting needed

### 3. Collection Install Updates

Update `cp.Layout.UpdatePageTemplate()` method signature and implementation:

```csharp
// Add optional parameters for new fields
public void UpdatePageTemplate(
    string templateName,
    string templateGuid,
    string bodyHtml,
    string headHtml = "",           // NEW
    string providedAddons = ""      // NEW
)
{
    // Existing logic for finding/creating template by GUID or name
    // ...

    // Update fields
    template.Body = bodyHtml;
    template.Head = headHtml;                    // NEW
    template.ProvidedAddons = providedAddons;    // NEW
    template.save();
}
```

Update OnInstall addons in collections to pass head content and provided addons when available.

### 4. Page Rendering Engine Updates

Update page rendering process (likely in Page Manager addon or core rendering controller):

#### Before Processing Addons:

```csharp
// Retrieve template record
var template = DbBaseModel.create<PageTemplateModel>(cp, templateId);

// Parse provided addons list
var providedAddons = template.ProvidedAddons
    .Split(',')
    .Select(x => x.Trim())
    .Where(x => !string.IsNullOrEmpty(x))
    .ToList();

// Mark these addon dependencies as already executed
foreach (var addonName in providedAddons) {
    addonContext.MarkAddonAsExecuted(addonName);
}
```

#### During Page Assembly:

```csharp
// Build complete HTML
var html = new StringBuilder();
html.Append("<!DOCTYPE html>\n<html>\n<head>\n");

// 1. Add template's head content first
html.Append(template.Head);

// 2. Append system-generated head elements
html.Append(GetAddonStylesheets());        // From addons
html.Append(GetAddonScripts());            // From addons
html.Append(GetDynamicMetaTags());         // Canonical, description, og:tags
html.Append(GetOtherSystemHeadElements()); // Other runtime additions

html.Append("\n</head>\n<body>\n");

// 3. Process template body (execute embedded addons, etc.)
html.Append(ProcessTemplateBody(template.Body));

html.Append("\n</body>\n</html>");

return html.ToString();
```

#### Error Handling & Debug Mode:

**Duplicate Detection:**
```csharp
// When addon dependency is requested but already provided
if (providedAddons.Contains(addonName)) {
    var message = $"Duplicate library detected: {addonName} loaded by template and requested by addon [{requestingAddon}]. " +
                  $"Add <meta name='provides-addon' content='{addonName}'> to template head to suppress this warning.";

    if (cp.Site.DebugMode) {
        // Inject HTML comment in output
        return $"<!-- {message} -->";
    } else {
        // Log to application log
        cp.Site.ErrorReport(message);
    }

    return ""; // Skip the duplicate addon
}
```

**Debug Mode Hints:**
```csharp
if (cp.Site.DebugMode) {
    html.Append($"<!-- Skipped addon dependency: {addonName} (already provided by template) -->\n");
}
```

### 5. Pattern Document Updates

Update `patterns/page-template-design-pattern.md` with these sections:

#### Add After "Page Template Record" Section:

```markdown
### Template Head and Body Storage

The system now stores both the `<head>` and `<body>` sections from page template HTML files:

- **Head field** - Stores the entire `<head>` section including meta tags, title, and any designer-included styles/scripts
- **Body field** - Stores the entire `<body>` section with page structure and content

During page rendering, the template's head content is rendered first, followed by system-generated elements (addon styles/scripts, dynamic meta tags, canonical URLs) which are appended after the template's head elements. This ensures a consistent and predictable order.

#### Template Asset References

Designers can include `<link rel="stylesheet">` and `<script src="">` tags anywhere in the template HTML (head or body section) following standard HTML conventions. Both sections are preserved.

**Asset organization:**
- Asset files (CSS, JS, images) should be organized in subfolders alongside the HTML file
- When uploaded via layoutFiles resource, HTML goes to `privateFiles/layoutFiles/` and assets go to `wwwFiles/` preserving folder structure
- Relative paths in HTML work exactly as they do in the designer's local folders

**Example folder structure:**
```
ui/layoutFiles/
├── my-template.html
├── css/
│   ├── bootstrap.min.css
│   └── site.css
├── js/
│   ├── jquery.min.js
│   ├── bootstrap.bundle.min.js
│   └── site.js
└── images/
    └── logo.png
```

**Example template referencing assets:**
```html
<html>
<head>
    <meta name="template" content="My Site Template">
    <meta name="template-guid" content="{XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX}">
    <meta name="provides-addon" content="jQuery">
    <meta name="provides-addon" content="Bootstrap 5">

    <title>My Site</title>
    <link rel="stylesheet" href="css/bootstrap.min.css">
    <link rel="stylesheet" href="css/site.css">
</head>
<body>
    <header>
        <img src="images/logo.png" alt="Site Logo">
        <nav><!-- site navigation --></nav>
    </header>
    <main>
        <span data-addon="content_box">page content renders here</span>
    </main>
    <footer><!-- site footer --></footer>

    <script src="js/jquery.min.js"></script>
    <script src="js/bootstrap.bundle.min.js"></script>
    <script src="js/site.js"></script>
</body>
</html>
```

#### Library Deduplication

To prevent duplicate loading of common libraries (jQuery, Bootstrap, etc.) when both the template and addons reference them:

**Automatic detection:**
The system automatically scans template HTML (both head and body sections) during import for common library patterns:
- jQuery (jquery.min.js, jquery.js, jquery-3.x.x, etc.)
- Bootstrap 4 (bootstrap/4/, bootstrap.min.js, bootstrap.min.css with version 4)
- Bootstrap 5 (bootstrap/5/, bootstrap.min.js, bootstrap.min.css with version 5)

Detected libraries are registered as "provided dependencies" and will not be loaded again by addon dependencies.

**Explicit declaration:**
Designers can explicitly declare which addon dependencies are satisfied by the template using meta tags:

```html
<head>
    <meta name="provides-addon" content="jQuery">
    <meta name="provides-addon" content="Bootstrap 5">
    <meta name="provides-addon" content="Font Awesome">
</head>
```

Multiple `provides-addon` meta tags can be used for multiple libraries. This is useful for:
- Libraries not automatically detected
- Ensuring specific versions are recognized
- Custom library addons

**Troubleshooting duplicates:**
If you see duplicate library loading (e.g., jQuery appearing twice):
1. Add `<meta name="provides-addon" content="LibraryName">` to the template's head section
2. Or remove the library reference from the template and let the addon dependency provide it

In debug mode, the system will inject HTML comments showing what was deduplicated and any warnings about potential duplicates.

#### Head Element Handling

The template's `<head>` section is fully preserved and rendered first. System-generated elements are appended after in this order:

1. Template's `<head>` content (everything between `<head>` and `</head>` in the template file)
2. Addon-injected stylesheets
3. Addon-injected scripts
4. Dynamic meta tags (canonical URLs, meta descriptions, Open Graph tags, etc.)
5. Other runtime-generated head elements

This consistent append order makes behavior predictable. Designers can include static meta tags, canonicals, stylesheets, and scripts in the template, and the system will add dynamic/addon elements after.

**Example of what designer includes:**
```html
<head>
    <meta name="template" content="My Template">
    <meta name="template-guid" content="{...}">
    <meta name="description" content="Static fallback description">
    <link rel="canonical" href="https://example.com/">
    <link rel="stylesheet" href="css/site.css">
</head>
```

**Example of final rendered head:**
```html
<head>
    <!-- Template's head content -->
    <meta name="template" content="My Template">
    <meta name="template-guid" content="{...}">
    <meta name="description" content="Static fallback description">
    <link rel="canonical" href="https://example.com/">
    <link rel="stylesheet" href="css/site.css">

    <!-- System-appended elements -->
    <link rel="stylesheet" href="/addons/BlogAddon/styles.css">
    <script src="/addons/CommonAddon/utils.js"></script>
    <meta name="description" content="Dynamic page-specific description">
    <link rel="canonical" href="https://example.com/actual-page-url">
    <meta property="og:title" content="Page Title">
</head>
```

Note that dynamic meta tags (description, canonical) will override static template values when both are present, as they appear later in the HTML.
```

#### Update "Meta tags for page template HTML files" Section:

Add after the existing `template` and `template-guid` meta tags:

```markdown
- `name="provides-addon"` — declares an addon dependency that this template satisfies, preventing duplicate library loading. Multiple meta tags can be used for multiple libraries. Common values: "jQuery", "Bootstrap 4", "Bootstrap 5", "Font Awesome". Example:
  ```html
  <meta name="provides-addon" content="jQuery">
  <meta name="provides-addon" content="Bootstrap 5">
  ```
```

### 6. Agent Workflow Documentation

Create new section in pattern document or separate agent guide:

#### Template Edit & Deploy Workflow for Agents

**Scenario:** Agent needs to modify a page template and deploy it to staging site.

**Steps:**

1. **Clone template repository** with `ui/layoutFiles/` structure
   ```bash
   git clone https://github.com/example/site-templates.git
   cd site-templates
   ```

2. **Edit template HTML file** (e.g., `ui/layoutFiles/homepage-template.html`)
   - Make required changes to HTML structure, styles, scripts
   - Ensure asset references use relative paths: `css/main.css`, `js/site.js`, `images/logo.png`
   - Verify `<meta name="template">` and `<meta name="template-guid">` are present in head

3. **Add library declarations** if template includes common libraries
   ```html
   <head>
       <meta name="template" content="Homepage Template">
       <meta name="template-guid" content="{XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX}">
       <meta name="provides-addon" content="jQuery">
       <meta name="provides-addon" content="Bootstrap 5">
   </head>
   ```

4. **Create deployment zip** with HTML and all asset subfolders
   ```bash
   cd ui/layoutFiles
   zip -r homepage-template.zip homepage-template.html css/ js/ images/
   ```

5. **Upload to staging site** via HTML Import tool
   - Via UI: Admin → Site Manager → HTML Import → Upload zip
   - Via API: POST to `/api/html-import` endpoint (if available)

6. **Import tool processes zip automatically:**
   - Extracts `homepage-template.html` → `privateFiles/layoutFiles/`
   - Extracts `css/`, `js/`, `images/` → `wwwFiles/` preserving structure
   - Parses head and body sections
   - Detects libraries from script/link tags
   - Combines with `provides-addon` meta tag declarations
   - Updates Page Template record with `Head`, `Body`, and `ProvidedAddons` fields

7. **Template immediately available** - Navigate to page using this template, all assets work, no duplicates

**Troubleshooting:**

| Problem | Cause | Solution |
|---------|-------|----------|
| Duplicate jQuery/Bootstrap | Template includes library and addon also loads it | Add `<meta name="provides-addon" content="jQuery">` to template head |
| Missing stylesheet | CSS file not in zip or wrong path | Verify CSS file in zip subfolder and path matches reference in HTML |
| 404 on asset | Asset path doesn't match zip structure | Check relative path: `css/main.css` requires `css/` subfolder in zip |
| Template not found | Missing or incorrect template meta tags | Verify `<meta name="template">` and `<meta name="template-guid">` in head |
| Changes not appearing | Browser cache or wrong template GUID | Clear cache, verify GUID matches between HTML and database record |

**Debug Mode:**

Enable debug mode in site settings to see:
- HTML comments showing which addon dependencies were skipped due to template provision
- Warnings about potential duplicate libraries
- Asset resolution paths

```html
<!-- Skipped addon dependency: jQuery (already provided by template) -->
<!-- Skipped addon dependency: Bootstrap 5 (already provided by template) -->
```

### 7. Testing Requirements

#### Unit Tests

Create tests in `source/ProcessorTests/` for:

1. **HTML parsing:**
   - Extract head section correctly
   - Extract body section correctly
   - Parse `provides-addon` meta tags
   - Handle missing head or body tags

2. **Library detection:**
   - Detect jQuery variants (jquery.min.js, jquery.js, jquery-3.x.x)
   - Detect Bootstrap 4 vs Bootstrap 5
   - Combine detected + declared libraries
   - Handle no libraries present

3. **Addon deduplication:**
   - Skip addon when provided by template
   - Execute addon when not provided
   - Handle partial matches (e.g., Bootstrap 4 vs Bootstrap 5)

#### Integration Tests

Create tests in `source/ModelsTests/` or `tests/e2e/` for:

1. **Template import workflow:**
   - Upload zip with HTML + assets
   - Verify HTML extracted to layoutFiles
   - Verify assets extracted to wwwFiles
   - Verify Page Template record created/updated
   - Verify Head, Body, ProvidedAddons fields populated

2. **Page rendering workflow:**
   - Render page with template that provides jQuery
   - Verify jQuery only appears once in final HTML
   - Verify template head appears before system head elements
   - Verify asset links resolve correctly

3. **Round-trip workflow:**
   - Export template from site
   - Edit HTML locally
   - Re-import to site
   - Verify changes appear and no duplicates

## Migration Strategy

### Backwards Compatibility

**Existing templates without Head field:**
- If `Head` is null/empty, system behaves as before (no head content from template)
- If `ProvidedAddons` is null/empty, no deduplication occurs (existing behavior)
- No breaking changes to existing templates

**Gradual adoption:**
- Designers can continue using old pattern (addon dependencies, no embedded assets)
- New pattern is opt-in via including head content and asset references
- Both patterns work simultaneously

### Migration Steps

1. **Deploy database schema changes** - Add `Head` and `ProvidedAddons` fields to Page Templates table
2. **Deploy HTML import updates** - Process head/body sections and detect libraries
3. **Deploy rendering updates** - Use Head field and deduplicate addons
4. **Update documentation** - Pattern document and agent guides
5. **Migrate templates gradually** - Update templates one at a time as needed

### Rollback Plan

If issues arise:
- New fields (Head, ProvidedAddons) can be ignored by reverting code changes
- Database schema changes are additive (no deletions), safe to leave in place
- Revert to previous behavior by deploying previous version of code

## Success Criteria

1. **Agent can edit and deploy templates** without manual intervention
2. **No duplicate library loading** when template and addons both reference jQuery/Bootstrap
3. **Assets resolve correctly** using existing layoutFiles/wwwFiles pattern
4. **Clear error messages** guide designers to fix duplicate issues
5. **Backwards compatible** with existing templates
6. **Performance impact minimal** - library detection happens at import, not per-render

## Future Enhancements

### Phase 2 Possibilities:

1. **Template versioning** - Track template changes over time, rollback capability
2. **Asset optimization** - Minify/bundle assets during import
3. **Dependency validation** - Warn if template references asset not in zip
4. **Library version detection** - Distinguish jQuery 3.x vs 2.x vs 1.x
5. **Debug dashboard** - UI showing which templates provide which addons
6. **API endpoint** - Programmatic template upload/download for agents
7. **Template marketplace** - Share/download community templates

### Debug Mode Enhancements:

1. **Visual dependency graph** - Show which addons loaded by template vs page vs widgets
2. **Asset resolution logging** - Show full path resolution for each asset reference
3. **Performance metrics** - Time spent in library detection, addon deduplication
4. **Lint warnings** - Suggest missing `provides-addon` declarations

## Open Questions

1. **Library version matching** - Should detection distinguish between Bootstrap 4 vs 5, jQuery 3.x vs 2.x? Current plan: Yes for Bootstrap (4 vs 5), pattern-based for jQuery
2. **Case sensitivity** - Should `provides-addon` values be case-insensitive? Recommend: Case-insensitive comparison
3. **Partial matches** - Should "Bootstrap" match both "Bootstrap 4" and "Bootstrap 5" addons? Recommend: Exact match only
4. **Meta tag vs field priority** - If both `provides-addon` meta tag and detected library exist, which wins? Recommend: Combine both (union)
5. **HTML Import API** - Does programmatic API exist or need to be created? Required for full agent workflow
6. **Existing addon dependencies** - Should templates keep their addon dependency records for backwards compatibility? Recommend: Yes, but mark as deprecated/superseded by template provision

## Implementation Timeline

**Estimated effort:** 2-3 weeks (one developer)

### Week 1: Database & Import
- [ ] Add database fields (Head, ProvidedAddons) to collection XML
- [ ] Update HTML import tool to parse head/body and detect libraries
- [ ] Update `cp.Layout.UpdatePageTemplate()` method
- [ ] Unit tests for parsing and detection

### Week 2: Rendering & Deduplication
- [ ] Update page rendering to use Head field
- [ ] Implement addon deduplication logic
- [ ] Add debug mode logging and HTML comments
- [ ] Integration tests for rendering workflow

### Week 3: Documentation & Testing
- [ ] Update pattern document with new sections
- [ ] Create agent workflow guide
- [ ] E2E tests for full round-trip workflow
- [ ] Performance testing and optimization
- [ ] Code review and deployment

## Related Documents

- [Page Template Design Pattern](../patterns/page-template-design-pattern.md) - Current pattern (to be updated)
- [Addon Collection Pattern](../patterns/addon-collection-pattern.md) - Collection XML syntax
- [Testing Pattern](../patterns/testing-pattern.md) - Testing guidelines

## Change Log

- 2026-11-03: Initial plan created based on discussion with jay
