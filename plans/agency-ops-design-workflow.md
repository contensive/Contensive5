# Agency-Ops Design Workflow Implementation Plan

**Date:** 2026-11-03
**Status:** Draft for Review
**Context:** Implements Option 1 from design vs content management analysis - treating page templates and email templates as design artifacts managed through git repos and deployed via html-import, rather than as directly-editable content.

## Executive Summary

This plan implements a design change workflow for the agency-ops orchestrator environment, enabling content managers to request design changes (like logo updates in page templates) through a Claude-powered agent. The solution maintains design-as-code principles while providing a user-friendly interface for non-technical users.

### Strategic Alignment
- **Design as Code:** Preserves git repos as the source of truth for all HTML/CSS/JS
- **Designer Partnership Ready:** Aligns with roadmap for designer partnerships where designers upload HTML files
- **No Source-of-Truth Conflict:** Avoids divergence between designer-provided files and live site versions
- **Version Control:** All design changes tracked in git with review/approval workflows

## Problem Statement

**Immediate Need:** Customer (Oasis) wants to change a logo in a page template.

**Broader Challenge:** Enable content managers to request and approve design changes without:
- Direct access to git repositories
- Understanding of HTML/CSS/JS
- Knowledge of the html-import tool workflow
- Risk of creating conflicting sources of truth

## Solution Architecture

### Three-Tier Environment Structure

```
┌─────────────────────────────────────────────────────────────────┐
│ AGENCY-OPS ORCHESTRATOR                                         │
│ (GitHub: contensive/agency-ops)                                 │
│                                                                  │
│  User Request: "Change logo in Landing Page Template"           │
│         ↓                                                        │
│  Read Client Config → Invoke Design Agent → Track Progress      │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ CUSTOMER GIT REPOSITORY                                         │
│ (e.g., customer-website-repo/ui/layoutFiles/)                  │
│                                                                  │
│  ├── LandingPageTemplate.html (with meta tags)                  │
│  ├── css/                                                        │
│  ├── js/                                                         │
│  ├── img/                                                        │
│  └── fonts/                                                      │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ STAGING SITE                                                     │
│ (Contensive instance with MCP tools at /mcp)                    │
│                                                                  │
│  1. Create preview page                                          │
│  2. Create preview page template                                 │
│  3. Import HTML via html-import MCP tool                         │
│  4. User reviews at preview URL                                  │
└─────────────────────────────────────────────────────────────────┘
                              ↓
                        User Approves
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ PRODUCTION SITE                                                  │
│ (Contensive instance with MCP tools at /mcp)                    │
│                                                                  │
│  Import HTML via html-import MCP tool                            │
│  (Updates existing page template via meta tags)                 │
└─────────────────────────────────────────────────────────────────┘
```

### Workflow Sequence

```
User Request
    ↓
Orchestrator reads clients/<client-slug>/profile.md
    ↓
Gets: production MCP endpoint, staging MCP endpoint, git repo URL
    ↓
Design agent reads git repo ui/layoutFiles/
    ↓
Finds HTML files with meta tags (name="template" content="Landing Page Template")
    ↓
Makes HTML/CSS/JS changes
    ↓
Commits changes to git branch (preview/YYYYMMDD-<slug>)
    ↓
Pushes to staging trigger (auto-deploys to staging environment)
    ↓
Zips HTML file + all layoutFiles subfolders (css/, js/, img/, fonts/)
    ↓
Calls staging MCP tools:
    - page_create (preview page)
    - page_template_create OR use existing preview template
    - html_import (imports the zip)
    ↓
Returns preview URL to user
    ↓
User reviews and approves
    ↓
Calls production MCP html_import tool
    (Meta tags in HTML direct import to correct page template)
    ↓
Done - production template updated
```

## Deliverables

### 1. New MCP Tools in aoHtmlImport Addon

**Location:** `C:\Git\aoHtmlImport\`

#### 1.1 MCP Tool Definition

Add to `C:\Git\aoHtmlImport\collections\Html Import\Html Import.xml`:

```xml
<!-- MCP Tool Definition for html_import -->
<CDef name="MCP Tool Definitions" guid="{existing-guid}">
  <Data>
    <record content="MCP Tool Definitions" name="html_import" guid="{GENERATE-NEW-GUID}">
      <field name="category">HTML Import</field>
      <field name="name">html_import</field>
      <field name="description">Imports a zipped HTML file with assets to create or update layouts, page templates, or email templates. The HTML file must include meta tags specifying the import destination (name="template" content="Template Name" or name="layout" content="Layout Name" or name="email" content="Email Name").</field>
      <field name="inputSchema"><![CDATA[{
  "type": "object",
  "properties": {
    "zipFileBase64": {
      "type": "string",
      "description": "Base64-encoded zip file containing HTML file(s) and asset folders (css/, js/, img/, fonts/, etc.)"
    },
    "importType": {
      "type": "string",
      "enum": ["auto", "layout", "pageTemplate", "emailTemplate", "email"],
      "description": "Import destination type. Use 'auto' to let meta tags in HTML determine the destination.",
      "default": "auto"
    },
    "layoutFramework": {
      "type": "string",
      "enum": ["bootstrap4", "bootstrap5"],
      "description": "Bootstrap version for layout records. Defaults to site setting.",
      "default": "bootstrap4"
    }
  },
  "required": ["zipFileBase64"]
}]]></field>
      <field name="endpointUrl">/content-api-html-import</field>
      <field name="httpMethod">POST</field>
      <field name="active">true</field>
      <field name="sortOrder">1000</field>
    </record>
  </Data>
</CDef>

<!-- MCP Server Instructions for HTML Import -->
<record content="MCP Server Instructions" guid="{GENERATE-NEW-GUID}" name="HTML Import">
  <field name="sortOrder">1100</field>
  <field name="category">HTML Import</field>
  <field name="instructions"><![CDATA[HTML Import: Use html_import to upload and install HTML files with assets (CSS, JS, images) to create or update page templates, email templates, or layouts. The HTML file must include meta tags: <meta name="template" content="Page Template Name"> for page templates, <meta name="layout" content="Layout Name"> for layouts, or <meta name="email" content="Email Name"> for emails. Create a zip file containing the HTML file(s) at root level and all asset subfolders (css/, js/, img/, fonts/), then base64-encode it for the zipFileBase64 parameter. The import processes the HTML, extracts assets to the www folder, and creates/updates the target record based on meta tags.]]></field>
</record>
```

**IMPORTANT:** Generate GUIDs using the command specified in CLAUDE.md:
```bash
powershell -Command "[guid]::NewGuid().ToString('B').ToUpper()"
```

#### 1.2 Remote Method Implementation

Create `C:\Git\aoHtmlImport\source\aoHtmlImport\RemoteMethods\HtmlImportRemoteMethod.cs`:

```csharp
using Contensive.BaseClasses;
using System;
using System.IO;
using System.Collections.Generic;

namespace Contensive.Addons.HtmlImport.RemoteMethods {
    /// <summary>
    /// Remote method for MCP html_import tool
    /// Accepts base64-encoded zip file and imports HTML templates/layouts
    /// </summary>
    public class HtmlImportRemoteMethod : AddonBaseClass {

        public override object Execute(CPBaseClass cp) {
            try {
                // Parse request body as JSON
                string requestBody = cp.Doc.GetText("requestBody");
                if (string.IsNullOrEmpty(requestBody)) {
                    return FormatError(cp, "Missing requestBody");
                }

                dynamic request = Newtonsoft.Json.JsonConvert.DeserializeObject(requestBody);
                string zipFileBase64 = request.zipFileBase64;
                string importType = request.importType ?? "auto";
                string layoutFramework = request.layoutFramework ?? "bootstrap4";

                if (string.IsNullOrEmpty(zipFileBase64)) {
                    return FormatError(cp, "Missing zipFileBase64 parameter");
                }

                // Decode base64 to bytes
                byte[] zipBytes = Convert.FromBase64String(zipFileBase64);

                // Save to temp file
                string tempFolder = cp.TempFiles.CreateUniqueFolder();
                string zipPath = Path.Combine(tempFolder, "import.zip");
                File.WriteAllBytes(zipPath, zipBytes);

                // Determine import type enum
                CPLayoutBaseClass.ImporttypeEnum importTypeEnum = CPLayoutBaseClass.ImporttypeEnum.importTypeSetInHtmlFile;
                int layoutId = 0;
                int pageTemplateId = 0;
                int emailTemplateId = 0;
                int emailId = 0;

                switch (importType.ToLower()) {
                    case "layout":
                        importTypeEnum = CPLayoutBaseClass.ImporttypeEnum.importTypeLayout;
                        break;
                    case "pagetemplate":
                        importTypeEnum = CPLayoutBaseClass.ImporttypeEnum.importTypePageTemplate;
                        break;
                    case "emailtemplate":
                        importTypeEnum = CPLayoutBaseClass.ImporttypeEnum.importTypeEmailTemplate;
                        break;
                    case "email":
                        importTypeEnum = CPLayoutBaseClass.ImporttypeEnum.importTypeEmail;
                        break;
                    default:
                        importTypeEnum = CPLayoutBaseClass.ImporttypeEnum.importTypeSetInHtmlFile;
                        break;
                }

                // Set layout framework site setting if specified
                int currentFramework = cp.Site.GetInteger("HTML PLATFORM VERSION");
                int selectedFramework = layoutFramework.ToLower() == "bootstrap5" ? 5 : 4;
                if (currentFramework != selectedFramework) {
                    cp.Site.SetProperty("HTML PLATFORM VERSION", selectedFramework);
                }

                // Process import
                var userMessageList = new List<string>();
                bool success = cp.Layout.processImportFile(
                    zipPath,
                    importTypeEnum,
                    layoutId,
                    pageTemplateId,
                    emailTemplateId,
                    emailId,
                    ref userMessageList
                );

                // Restore layout framework setting
                if (currentFramework != selectedFramework) {
                    cp.Site.SetProperty("HTML PLATFORM VERSION", currentFramework);
                }

                // Clean up
                cp.TempFiles.DeleteFolder(tempFolder);

                // Invalidate cache
                if (success) {
                    cp.Cache.InvalidateAll();
                }

                // Format response
                return FormatResponse(cp, new {
                    success = success,
                    messages = userMessageList,
                    importType = importTypeEnum.ToString()
                });

            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return FormatError(cp, $"Import failed: {ex.Message}");
            }
        }

        private string FormatResponse(CPBaseClass cp, object data) {
            return Newtonsoft.Json.JsonConvert.SerializeObject(new {
                success = true,
                data = data
            });
        }

        private string FormatError(CPBaseClass cp, string message) {
            return Newtonsoft.Json.JsonConvert.SerializeObject(new {
                success = false,
                error = message
            });
        }
    }
}
```

#### 1.3 Collection XML Addon Registration

Add to `C:\Git\aoHtmlImport\collections\Html Import\Html Import.xml`:

```xml
<Addon name="Html Import Remote Method" guid="{GENERATE-NEW-GUID}" type="Add-on">
    <DotNetClass><![CDATA[Contensive.Addons.HtmlImport.RemoteMethods.HtmlImportRemoteMethod]]></DotNetClass>
    <RemoteMethod>Yes</RemoteMethod>
    <Admin>No</Admin>
    <Content>No</Content>
    <Template>No</Template>
    <Email>No</Email>
    <ProcessRunOnce>No</ProcessRunOnce>
    <Diagnostic>No</Diagnostic>
</Addon>

<AdminNavigatorEntry name="Html Import API" guid="{GENERATE-NEW-GUID}"
                     parentName="Tools"
                     adminOnly="Yes"
                     developerOnly="No"
                     newWindow="No"
                     active="Yes"
                     navIconTitle=""
                     navIconType="0"
                     contentName=""
                     addonName="Html Import Remote Method"
                     linkPage="/content-api-html-import"/>
```

### 2. Agency-Ops Orchestrator Updates

**Location:** `C:\Git\agency-ops\`

#### 2.1 Client Configuration Schema

Update each client profile to include design endpoints.

**Example:** `C:\Git\agency-ops\clients/Oasis/profile.md`

Add section:

```markdown
## **Design Environment**

* Git repository URL: https://github.com/<org>/<repo>.git
* Git repository local path: C:\Git\<repo-name>
* Production MCP endpoint: https://oasis-internal-medicine.com/mcp
* Staging MCP endpoint: https://staging.oasis-internal-medicine.com/mcp
* Staging auto-deploy branch pattern: preview/*
* Design assets path in repo: ui/layoutFiles/
```

#### 2.2 Design Agent Updates

**File:** `C:\Git\agency-ops\.claude\agents\design\web-designer.md`

**Changes Required:**

Add new section after line 33:

```markdown
## Contensive Design Change Workflow

When your brief requests changes to an existing Contensive page template or email template:

1. **Locate the template:** Read the git repo path from the brief (from client profile). Look in `ui/layoutFiles/` for HTML files with meta tags:
   - Page templates: `<meta name="template" content="Template Name">`
   - Email templates: `<meta name="email" content="Email Name">`
   - Layouts: `<meta name="layout" content="Layout Name">`

2. **Make changes:** Edit the HTML/CSS/JS files. Respect existing meta tags - they control where the import installs.

3. **Create preview:**
   - Zip the HTML file(s) at root level + all subfolders (css/, js/, img/, fonts/, etc.)
   - Use staging site MCP endpoint from brief to:
     a. Create a preview page: `page_create(name="Preview - <template-name>", ...)`
     b. Call `html_import(zipFileBase64="<base64-encoded-zip>", importType="auto")`
   - The import will create/update the page template based on meta tags
   - Return the preview page URL to the user

4. **Deploy to production (only after user approval):**
   - Use production site MCP endpoint from brief
   - Call `html_import(zipFileBase64="<same-zip>", importType="auto")`
   - Meta tags in HTML direct the import to update the correct production template

5. **Commit changes:**
   - Commit the HTML/CSS/JS changes to git on a preview branch
   - Push to trigger staging auto-deploy (if configured)

**Key principles:**
- Git repo is always the source of truth
- Staging shows draft work before production
- Meta tags in HTML control import destination
- Same zip file deploys to both staging and production
```

#### 2.3 Orchestrator Instructions

**File:** `C:\Git\agency-ops\.claude\agents\orchestrator.md`

Add to the task types table (line 14):

```markdown
| design-change | Change logo, colors, or layout in page/email template | `intake/design-change.md` |
```

Create new intake checklist:

**File:** `C:\Git\agency-ops\intake\design-change.md`

```markdown
# Design Change Intake Checklist

Use this for requests to modify existing page templates, email templates, or layouts (logo changes, color updates, layout adjustments, copy changes in templates).

## Required

- [ ] Client identified (has profile in `clients/<slug>/profile.md`)
- [ ] Template identified (page template name, email template name, or layout name)
- [ ] What to change (logo, color, copy, layout element, etc.)
- [ ] Scope clear (single template, multiple templates, site-wide change)
- [ ] Git repo URL and local path known (from client profile)
- [ ] Production MCP endpoint known (from client profile)
- [ ] Staging MCP endpoint known (from client profile)
- [ ] User approval required before production deploy (always yes for design changes)

## Nice to Have

- [ ] New asset files provided (logos, images, etc.)
- [ ] Brand colors/fonts specified (if changing design elements)
- [ ] Responsive behavior specified (mobile, tablet, desktop)
- [ ] Accessibility requirements (contrast, alt text, focus states)

## Assumptions to Record

- If staging MCP endpoint missing: Skip staging preview, deploy directly to production after approval
- If git repo path unknown: Ask user to `/add-dir <repo-path>` before planning
- If template name unclear: List templates found in `ui/layoutFiles/` and ask user to select
- If no approval mentioned: Assume user must approve before production deploy

## Acceptance Criteria

- [ ] Template updated in git repository on preview branch
- [ ] Staging preview created and URL provided to user
- [ ] User approved staging preview
- [ ] Production template updated via html-import
- [ ] Git changes committed and pushed

## Hand-Off to web-designer

Include in the brief:
- Client name, brand colors, fonts (from profile)
- Git repo URL and local path
- Template name and what to change
- Production MCP endpoint
- Staging MCP endpoint
- New asset files (if provided)
- Acceptance criteria from plan
```

### 3. Supporting Scripts

#### 3.1 Zip Creation Helper (Optional)

For agents that need to zip layoutFiles programmatically:

**File:** `C:\Git\agency-ops\scripts\zip-layout-files.ps1`

```powershell
# Zips HTML file(s) and all subfolders in ui/layoutFiles for html-import
param(
    [Parameter(Mandatory)][string]$LayoutFilesPath,
    [Parameter(Mandatory)][string]$OutputZipPath
)

if (-not (Test-Path $LayoutFilesPath)) {
    throw "Layout files path not found: $LayoutFilesPath"
}

# Remove existing zip
if (Test-Path $OutputZipPath) {
    Remove-Item $OutputZipPath -Force
}

# Compress all contents (HTML at root + subfolders)
Compress-Archive -Path "$LayoutFilesPath\*" -DestinationPath $OutputZipPath -Force

Write-Host "Created zip: $OutputZipPath"
Write-Host "Contents:"
Get-ChildItem $LayoutFilesPath | ForEach-Object { Write-Host "  $_" }
```

**Usage in agent:**
```bash
pwsh C:/Git/agency-ops/scripts/zip-layout-files.ps1 -LayoutFilesPath "C:/Git/customer-repo/ui/layoutFiles" -OutputZipPath "C:/temp/import.zip"
```

Then base64-encode for MCP tool:
```bash
pwsh -Command "[Convert]::ToBase64String([IO.File]::ReadAllBytes('C:/temp/import.zip'))"
```

## Implementation Steps

### Phase 1: MCP Tool Development (aoHtmlImport)

**Owner:** Developer (manual implementation, not orchestrator)

1. Generate GUIDs for new collection XML elements
2. Add MCP Tool Definition record to collection XML
3. Add MCP Server Instructions record to collection XML
4. Create `RemoteMethods/HtmlImportRemoteMethod.cs`
5. Register remote method addon in collection XML
6. Update `.csproj` to include new files
7. Build and test locally
8. Commit to aoHtmlImport repo

**Testing:**
- Create test zip with HTML file containing meta tags + css/js folders
- Base64-encode the zip
- Call the remote method via Postman or curl
- Verify template created/updated
- Verify assets copied to www folder

**Acceptance Criteria:**
- MCP tool appears in MCP server tool list after collection install
- Tool accepts base64-encoded zip
- Tool creates/updates templates based on meta tags
- Tool returns success/error messages
- Assets extracted to correct www paths

### Phase 2: Client Profile Updates (Agency-Ops)

**Owner:** Orchestrator (can automate this)

For each client that will use design change workflow:

1. Read existing `clients/<client-slug>/profile.md`
2. Add "Design Environment" section with:
   - Git repository URL
   - Git repository local path
   - Production MCP endpoint
   - Staging MCP endpoint
3. Verify endpoints are accessible (test call to /mcp)
4. Commit profile updates

**For Oasis (immediate need):**
- Identify git repo (ask user if not in profile)
- Confirm production URL: https://oasis-internal-medicine.com
- Ask user for staging URL
- Update `clients/Oasis/profile.md`

**Acceptance Criteria:**
- Oasis profile includes all design environment fields
- Production MCP endpoint verified accessible
- Staging MCP endpoint verified accessible (or marked as TODO)

### Phase 3: Agent Instructions Update (Agency-Ops)

**Owner:** Orchestrator (can automate this)

1. Update `web-designer.md` with Contensive Design Change Workflow section
2. Update `orchestrator.md` to add design-change task type
3. Create `intake/design-change.md` checklist
4. Test workflow with a sample design change request

**Testing Workflow:**
```
New job for Oasis.
Type: Design change
What: Change the logo in the footer of the Landing Page Template
Where: ui/layoutFiles/LandingPageTemplate.html
The new logo file is at C:/temp/new-logo.png
```

**Acceptance Criteria:**
- Orchestrator recognizes design-change task type
- Orchestrator reads intake/design-change.md
- Orchestrator dispatches to web-designer with complete brief
- web-designer follows Contensive Design Change Workflow
- Staging preview created
- Production deploy happens only after approval

### Phase 4: End-to-End Validation

**Owner:** Manual testing (user + orchestrator)

1. **Test Case 1: Logo Change (Oasis)**
   - Request: "Change logo in Landing Page Template footer"
   - Verify: Staging preview shows new logo
   - Verify: Production template updates after approval
   - Verify: Git repo has committed changes

2. **Test Case 2: Color Update**
   - Request: "Change primary color from #46595F to #2C3E50 in all templates"
   - Verify: Multiple templates identified
   - Verify: CSS changes made correctly
   - Verify: Staging preview for each template
   - Verify: Production deploy after approval

3. **Test Case 3: New Template Section**
   - Request: "Add testimonials section to Services Template"
   - Verify: HTML structure added
   - Verify: Responsive behavior correct
   - Verify: Staging preview functional
   - Verify: Production deploy successful

**Acceptance Criteria:**
- All test cases complete without errors
- User can request changes without technical knowledge
- Staging preview accurately reflects changes
- Production updates correctly after approval
- Git repo remains source of truth

## Meta Tags Reference

HTML files in `ui/layoutFiles/` use meta tags to specify import destination. The aoHtmlImport tool and Contensive's `cp.Layout.processImportFile` method read these tags.

### Page Template

```html
<meta name="template" content="Landing Page Template">
```

- Creates or updates a page template record with name "Landing Page Template"
- Only the `<body>` content is imported
- Assets (css, js, img) copied to www folder with paths preserved

### Email Template

```html
<meta name="email" content="Welcome Email">
```

- Creates or updates an email template record with name "Welcome Email"
- Entire HTML document imported (including `<head>`)
- If head elements missing, system creates them

### Layout (for Addons)

```html
<meta name="layout" content="Blog Post Layout">
```

- Creates or updates a layout record with name "Blog Post Layout"
- Only the `<body>` content is imported
- Used by addons for rendering

### Additional Meta Tags

The aoHtmlImport tool supports additional data attributes for Mustache templating:

- `data-body` - Marks the region to import (if not found, entire body used)
- `data-layout="Layout-Name"` - Extracts content to a separate layout record
- `data-delete` - Removes element from final import
- `data-mustache-section="property"` - Creates Mustache section tags
- `data-cdn="src"` - Copies file to CDN and updates path
- `data-innertext="{{property}}"` - Sets inner text to Mustache variable

See [HtmlImportAddon.cs:120-326](C:\Git\aoHtmlImport\source\aoHtmlImport\Addons\HtmlImportAddon.cs) for complete documentation.

## Zip Structure

The html_import MCP tool expects a zip file with this structure:

```
import.zip
├── TemplateName.html          (HTML file at root, with meta tags)
├── AnotherTemplate.html        (optional - multiple HTML files supported)
├── css/
│   ├── site.css
│   └── ...
├── js/
│   ├── site.js
│   └── ...
├── img/
│   ├── logo.png
│   └── ...
└── fonts/
    ├── custom-font.woff2
    └── ...
```

**Rules:**
- HTML files at root level
- All asset subfolders included (css, js, img, fonts, etc.)
- Paths in HTML are relative: `css/site.css`, `img/logo.png`
- Import extracts assets to www folder preserving structure
- Multiple HTML files can be in one zip (each imports to its meta tag destination)

**Agent Implementation:**

Using PowerShell `Compress-Archive`:
```powershell
Compress-Archive -Path "C:/Git/repo/ui/layoutFiles/*" -DestinationPath "C:/temp/import.zip" -Force
```

Using Bash and 7-Zip:
```bash
cd "C:/Git/repo/ui/layoutFiles"
"c:/program files/7-zip/7z.exe" a -tzip "C:/temp/import.zip" *
```

Then base64-encode:
```powershell
$bytes = [IO.File]::ReadAllBytes("C:/temp/import.zip")
$base64 = [Convert]::ToBase64String($bytes)
# Pass $base64 to html_import MCP tool as zipFileBase64 parameter
```

## Open Questions / Decisions Needed

1. **Staging Auto-Deploy:** Does staging environment auto-deploy from git branches, or does the agent need to trigger deployment manually?
   - **Recommendation:** Document the pattern in client profiles. If auto-deploy exists, specify branch pattern (e.g., `preview/*`). If manual, agent uses staging MCP tools only.

2. **Preview Page Cleanup:** Should staging preview pages be automatically deleted after production deploy?
   - **Recommendation:** Leave preview pages for historical reference. Add cleanup task to monthly maintenance.

3. **Multi-Template Changes:** If a design change affects multiple templates (e.g., site-wide color change), should each template have its own staging preview?
   - **Recommendation:** Create one preview page per template, all linked in the approval message to user.

4. **Git Branch Strategy:** What branch naming convention for design changes?
   - **Recommendation:** `preview/YYYYMMDD-<client-slug>-<short-description>` (e.g., `preview/20261103-oasis-logo-update`)

5. **Asset File Upload:** How should users provide new assets (logos, images)?
   - **Recommendation:** User uploads to temp location (C:/temp/, cloud storage), provides path in request. Agent copies to git repo ui/layoutFiles/img/.

6. **Rollback:** If a design change breaks production, how to rollback?
   - **Recommendation:** Git revert commit, then re-run html_import with previous version. Document in runbook.

## Success Metrics

- **User Experience:** Non-technical users can request design changes without learning git/HTML
- **Accuracy:** Changes deploy correctly on first attempt (>90% success rate)
- **Speed:** Logo change request to production deploy < 30 minutes (with user approval)
- **Safety:** Zero production incidents from design deployments
- **Auditability:** Every design change tracked in git with commit messages

## Next Steps

1. **Immediate (Oasis logo change):**
   - User: Implement Phase 1 (MCP tool in aoHtmlImport)
   - User: Provide Oasis git repo URL and staging MCP endpoint
   - Orchestrator: Implement Phase 2 (update Oasis profile)
   - Orchestrator: Implement Phase 3 (update agent instructions)
   - User: Execute logo change request as Phase 4 validation

2. **Short Term (next 2 weeks):**
   - Validate workflow with 3-5 additional design changes
   - Document common patterns in runbook
   - Update other client profiles as needed

3. **Long Term (roadmap):**
   - Designer partnership integration (external designers upload templates)
   - Template versioning and history UI
   - Automated visual regression testing for design changes
   - Integration with design systems / component libraries

## Appendix A: Example Agent Brief

When orchestrator dispatches to web-designer for a design change, the brief should include:

```markdown
# Design Change Brief - Oasis Logo Update

**Job ID:** 20261103-oasis-logo-update
**Client:** Oasis Internal Medicine
**Task Type:** Design Change

## Request Summary
Change the logo in the footer of the Landing Page Template to the new logo file.

## Client Context
- **Business:** Concierge internal medicine practice
- **Brand Colors:** #46595F (Deep Slate), #92B192 (Oasis Sage), #F3F7F3 (Sage Light)
- **Fonts:** Montserrat (headings), Roboto (body)
- **Voice:** Calm, warm, professional

## Technical Environment
- **Git Repo:** https://github.com/contensive/oasis-website.git
- **Local Path:** C:\Git\oasis-website
- **Design Assets Path:** ui/layoutFiles/
- **Production MCP:** https://oasis-internal-medicine.com/mcp
- **Staging MCP:** https://staging.oasis-internal-medicine.com/mcp

## Task Details
1. Locate template: ui/layoutFiles/LandingPageTemplate.html (has `<meta name="template" content="Landing Page Template">`)
2. Replace footer logo: Currently `img/Oasis_new_Logo_lg.webp`, replace with C:/temp/new-logo.png
3. Copy new logo to ui/layoutFiles/img/ in git repo
4. Update HTML to reference new logo filename
5. Create staging preview (see Contensive Design Change Workflow in your instructions)
6. After user approves, deploy to production

## New Assets
- New logo: C:/temp/new-logo.png (copy to ui/layoutFiles/img/oasis-logo-2024.png)

## Acceptance Criteria
- [ ] Footer logo updated in Landing Page Template
- [ ] New logo file in git repo at ui/layoutFiles/img/oasis-logo-2024.png
- [ ] Staging preview URL provided
- [ ] Production template updated after approval
- [ ] Git changes committed to preview branch

## Output Location
- Report: jobs/20261103-oasis-logo-update/reports/01-web-designer.md
- Git branch: preview/20261103-oasis-logo-update
```

## Appendix B: MCP Tool Usage Examples

### Example 1: Import Page Template

```javascript
// JavaScript example using MCP tool
const fs = require('fs');
const path = require('path');

// Read zip file
const zipPath = 'C:/temp/landing-page.zip';
const zipBuffer = fs.readFileSync(zipPath);
const zipBase64 = zipBuffer.toString('base64');

// Call html_import tool
const response = await mcpClient.callTool('html_import', {
  zipFileBase64: zipBase64,
  importType: 'auto',  // Let meta tags determine destination
  layoutFramework: 'bootstrap5'
});

console.log('Import result:', response);
// { success: true, data: { success: true, messages: [...], importType: 'importTypePageTemplate' } }
```

### Example 2: Import Multiple Templates

```javascript
// One zip can contain multiple HTML files
// Each imports to its own destination based on meta tags

// landing.html has: <meta name="template" content="Landing Page">
// services.html has: <meta name="template" content="Services Page">
// Both in same zip with shared css/js/img folders

const response = await mcpClient.callTool('html_import', {
  zipFileBase64: multiTemplateZipBase64,
  importType: 'auto'
});

// Both templates imported in single operation
```

### Example 3: Error Handling

```javascript
try {
  const response = await mcpClient.callTool('html_import', {
    zipFileBase64: invalidZipBase64,
    importType: 'auto'
  });

  if (!response.success) {
    console.error('Import failed:', response.error);
    // Handle error: "Import failed: Invalid zip file format"
  }

  if (response.data && !response.data.success) {
    console.warn('Import completed with warnings:', response.data.messages);
    // Handle warnings: ["Warning: No meta tag found, using default template name"]
  }

} catch (err) {
  console.error('MCP tool call failed:', err);
}
```

## Appendix C: Git Workflow for Design Changes

### Branch Strategy

```bash
# Create preview branch from main
git checkout main
git pull origin main
git checkout -b preview/20261103-oasis-logo-update

# Make changes
# (web-designer agent edits files)

# Commit with descriptive message
git add ui/layoutFiles/LandingPageTemplate.html
git add ui/layoutFiles/img/oasis-logo-2024.png
git commit -m "Update footer logo in Landing Page Template

- Replace Oasis_new_Logo_lg.webp with oasis-logo-2024.png
- Requested by: Dr. Singh
- Job: 20261103-oasis-logo-update

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"

# Push to trigger staging auto-deploy (if configured)
git push -u origin preview/20261103-oasis-logo-update
```

### After Production Deploy

```bash
# Merge preview branch to main
git checkout main
git merge preview/20261103-oasis-logo-update
git push origin main

# Clean up preview branch (optional)
git branch -d preview/20261103-oasis-logo-update
git push origin --delete preview/20261103-oasis-logo-update
```

## Conclusion

This plan implements a design change workflow that:
- ✅ Maintains git as the source of truth
- ✅ Provides user-friendly interface for non-technical users
- ✅ Includes staging preview before production
- ✅ Supports designer partnership roadmap
- ✅ Prevents source-of-truth conflicts
- ✅ Tracks all changes in version control

The immediate Oasis logo change can proceed once Phase 1 (MCP tool) is implemented and Phases 2-3 (orchestrator updates) are completed.

**Estimated Implementation Time:**
- Phase 1 (MCP tool): Manual developer work, ~4 hours
- Phase 2 (client profiles): Orchestrator automation, ~30 minutes
- Phase 3 (agent instructions): Orchestrator automation, ~1 hour
- Phase 4 (validation): Manual testing, ~2 hours

**Total:** ~7.5 hours to full production capability
