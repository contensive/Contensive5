# Plan: Add "Default Landing Page Template" Site Property

## Summary

Add a new site property "Default Landing Page Template Id" that provides a separate default template for landing pages. Landing pages will check: (1) the page record's own template, (2) this new site property, (3) then fall back to the existing non-landing-page flow (parent chain -> domain -> site default).

Non-landing pages are unchanged.

## How Landing Pages Are Identified

A page is the landing page when `requestedPageId == 0` (no page was requested by route), which causes `getLandingPage()` to be called at [PageManagerController.cs:1300](source/Processor/Controllers/PageManagerController.cs#L1300). After that call, the resolved page equals the domain's `rootPageId` (or the `LandingPageID` site property fallback).

At the template resolution block (line 1340), we can detect a landing page by checking whether `requestedPageId == 0`. This is the same condition that triggered `getLandingPage()` earlier in the method.

## Changes

### 1. Add site property to collection XML

**File:** [aoBase51.xml](source/Processor/aoBase51.xml) (near line 3668, next to existing "Default TemplateId")

Add a new `<SiteProperty>` element:

```xml
<SiteProperty Caption="Default Landing Page Template" Name="Default Landing Page Template Id" ReadOnly="false" Type="Lookup" Selector="[None:0|ListID(Page Templates)]" Description="The template used for landing pages when no template is set on the page record itself. When empty, landing pages fall back to the standard template selection (parent chain, domain default, site default)."></SiteProperty>
```

The `[None:0|...]` selector allows the value to be unset (0), so existing sites are not affected.

### 2. Add typed property to SitePropertyController

**File:** [SitePropertyController.cs](source/Processor/Controllers/SitePropertyController.cs) (near line 392, after `systemPageTemplateId`)

Add a cached property following the same pattern as `systemPageTemplateId`:

```csharp
/// <summary>
/// Optional template for landing pages. When set, landing pages use this template
/// instead of inheriting from the parent chain or domain default.
/// When 0, landing pages follow the standard template resolution flow.
/// </summary>
public int defaultLandingPageTemplateId {
    get {
        if (_defaultLandingPageTemplateId != null) { return (int)_defaultLandingPageTemplateId; }
        _defaultLandingPageTemplateId = getInteger(defaultLandingPageTemplateId_spName, 0);
        return (int)_defaultLandingPageTemplateId;
    }
    set {
        _defaultLandingPageTemplateId = value;
        setProperty(defaultLandingPageTemplateId_spName, (int)_defaultLandingPageTemplateId);
    }
}
private const string defaultLandingPageTemplateId_spName = "Default Landing Page Template Id";
private int? _defaultLandingPageTemplateId;
```

### 3. Modify template resolution in loadPage()

**File:** [PageManagerController.cs](source/Processor/Controllers/PageManagerController.cs) (lines 1340-1404)

Replace the template resolution block with a landing-page-aware version:

**Landing page flow (when `requestedPageId == 0`):**
1. Check the page record's own `templateId` — use it if set
2. Check site property "Default Landing Page Template Id" — use it if set
3. Fall through to the standard fallback chain below

**Non-landing page flow (unchanged):**
1. Walk `pageToRootList` checking each page's `templateId` — use the first one found
2. Fall through to the standard fallback chain below

**Standard fallback chain (unchanged, shared by both flows):**
1. Domain's `defaultTemplateId`
2. Site property "Default TemplateId"
3. Template named "Single Column Template" (legacy)
4. Most recent template by ID
5. Create new default template

The restructured code:

```csharp
// -- get template from pages
core.doc.pageController.template = null;
bool isLandingPage = requestedPageId.Equals(0);
if (isLandingPage) {
    //
    // -- landing page: check page record template, then landing page default template
    if (requestedPage.templateId > 0) {
        core.doc.pageController.template = DbBaseModel.create<PageTemplateModel>(core.cpParent, requestedPage.templateId);
        if (core.doc.pageController.template == null) {
            requestedPage.templateId = 0;
            requestedPage.save(core.cpParent);
        } else {
            core.doc.pageController.templateReason = "This template was used because it is selected by the current page.";
        }
    }
    if (core.doc.pageController.template == null) {
        //
        // -- landing page: check default landing page template site property
        int landingTemplateId = core.siteProperties.defaultLandingPageTemplateId;
        if (landingTemplateId > 0) {
            core.doc.pageController.template = DbBaseModel.create<PageTemplateModel>(core.cpParent, landingTemplateId);
            if (core.doc.pageController.template != null) {
                core.doc.pageController.templateReason = "This template was used because it is the default landing page template.";
            }
        }
    }
    // -- if still null, fall through to standard fallback chain below
} else {
    //
    // -- non-landing page: walk parent chain (existing logic, unchanged)
    foreach (PageContentModel page in core.doc.pageController.pageToRootList) {
        ... existing parent-walk code unchanged ...
    }
}
//
// -- standard fallback chain (domain -> site default -> legacy)
// -- unchanged, serves both landing and non-landing pages
if (core.doc.pageController.template == null) {
    ... existing fallback code from lines 1361-1404 unchanged ...
}
```

### 4. No other changes needed

- **PageContentModel** — no changes. Landing pages already have a `templateId` field.
- **CPBase** — no public API changes. The new property is internal site config.
- **Legacy `getPageDynamicLink_GetTemplateID`** in DocController — no change needed.
- **BuildController** — no change needed. Initial site setup already sets `landingPage.templateId = defaultTemplate.id`, which hits check #1 in the landing page flow.

## Files Modified

| File | Change |
|------|--------|
| `source/Processor/aoBase51.xml` | Add site property definition |
| `source/Processor/Controllers/SitePropertyController.cs` | Add typed property accessor |
| `source/Processor/Controllers/PageManagerController.cs` | Modify template resolution logic |

## Backward Compatibility

- **Existing sites:** "Default Landing Page Template Id" defaults to 0 (unset), so landing pages fall through to the existing flow. No behavior change unless the admin explicitly sets this property.
- **"Default TemplateId"** site property continues to work as the final site-level fallback for all pages.
- **No CPBase breaking changes.**
