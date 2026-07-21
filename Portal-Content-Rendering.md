# Plan: Portal Content Rendering (Option D - Addon Wrapper)

## Summary

Render content Edit/List views inline within the portal shell instead of redirecting to a separate tab. A single new addon ("Portal Content Editor") accepts a content ID and delegates to the existing `ListView` / `EditView` rendering. The portal framework's `dataContentId` branch is changed from a redirect to executing this addon internally.

## Problem

When a portal feature references content (via `dataContentId`), the current behavior:
1. Navigation links render with `target="_blank"` (opens new tab)
2. If accessed within the portal, `PortalAddon.cs:150-158` redirects to `?cid=X`, leaving the portal context entirely
3. The user loses portal navigation, subnav, and visual consistency

Addon features, by contrast, execute inline within the portal shell via `CP.Addon.Execute()`.

## Approach

Instead of creating a separate addon record per content definition, create **one** reusable addon that reads its content ID from a doc property or addon argument. Modify the portal framework's content-feature branch to execute this addon inline, the same way it executes addon features.

---

## Implementation Steps

### Step 1: Create the Portal Content Editor Addon Class

**New file:** `source/Processor/Addons/PortalFramework/Addons/PortalContentEditorAddon.cs`

This addon:
- Reads `contentId` from `cp.Doc.GetText("contentId")` (set by the portal framework before execution)
- Reads `recordId` from `cp.Doc.GetText("recordId")` to determine Edit vs List mode
- Reads form state parameters (`af`, `aa`, `ad`, `id`, `RT`, `RS`, `wl*/wr*`) from the request to support pagination, form submission, and navigation within the edit/list views
- Casts `CPBaseClass` to `CPClass` to access `CoreController` (required by `ListView.get()` and `EditView.get()`)
- Constructs an `AdminDataModel` with the content ID and form state
- Calls `ProcessFormController.processForms()` and `ProcessActionController.processActions()` for form submissions (save, delete, etc.)
- Calls `ListView.get()` or `EditView.get()` depending on the `af` (admin form) parameter
- Returns the rendered HTML as the addon body

**Key consideration -- form processing:** The existing `AdminContentController.getAdminContent()` handles form processing before rendering. The new addon must replicate this pipeline:
1. Parse `af` (destination form), `aa` (action), `ad` (source form) from the request
2. Build `AdminDataModel` with the content ID and request parameters
3. Run `ProcessFormController.processForms()` then `ProcessActionController.processActions()`
4. Render the appropriate view

**Reference code:** `AdminContentController.cs:30-451` contains the full pipeline. The new addon reuses this logic for the `AdminFormIndex`, `AdminFormEdit`, `AdminFormList_Export`, `AdminFormList_SetColumns`, and `AdminFormList_AdvancedSearch` cases.

### Step 2: Register the Addon in the Collection XML

**File:** `source/Processor/aoBase51.xml`

Add an `<Addon>` element for "Portal Content Editor" with:
- A new GUID (generated via `powershell -Command "[guid]::NewGuid().ToString('B').ToUpper()"`)
- `Type="Add-on"`
- No admin menu entry (this addon is only called programmatically)
- Assembly reference pointing to the Processor assembly
- Namespace pointing to the new class

Add a corresponding constant in `source/Processor/Constants.cs` for the addon GUID.

### Step 3: Modify PortalAddon.cs -- Content Feature Execution

**File:** `source/Processor/Addons/PortalFramework/Addons/PortalAddon.cs`

**Change the `dataContentId` branch (lines 150-158)** from:

```csharp
} else if (dstDataFeature.dataContentId != 0) {
    CP.Response.Redirect("?cid=" + dstDataFeature.dataContentId.ToString());
    var content = CP.AdminUI.CreateLayoutBuilder();
    content.title = ...;
    content.body = "Redirecting to content";
    body = content.getHtml();
```

To:

```csharp
} else if (dstDataFeature.dataContentId != 0) {
    //
    // -- content feature, render edit/list inline within portal
    CP.Doc.SetProperty(Constants.rnFrameRqs, CP.Doc.RefreshQueryString);
    CP.Doc.AddRefreshQueryString(Constants.rnDstFeatureGuid, dstDataFeature.guid);
    CP.Doc.SetProperty("contentId", dstDataFeature.dataContentId.ToString());
    body = CP.Addon.Execute(Constants.guidAddonPortalContentEditor);
    portalBuilder.subNavTitleList.AddRange(CP.Doc.GetText("portalSubNavTitleList").Split('|'));
```

This mirrors the addon feature branch (lines 139-148) exactly, but sets `contentId` as a doc property so the new addon knows which content to render.

### Step 4: Remove `target="_blank"` from Content Feature Navigation Links

**File:** `source/Processor/Addons/PortalFramework/Addons/PortalAddon.cs`

Content features currently get `linkTarget = "_blank"` and `sublinkTarget = "_blank"`. Change these to empty strings so they navigate within the portal instead of opening a new tab.

**Lines to change:**
- Line 108: `sublinkTarget = subFeature.dataContentId > 0 || ... ? "_blank" : ""`
  - Change to: `sublinkTarget = ""`
- Line 117: `linkTarget = feature.dataContentId > 0 || ... ? "_blank" : ""`
  - Change to: `linkTarget = ""`
- Line 193: `sublinkTarget = dstFeatureSibling.dataContentId > 0 || ... ? "_blank" : ""`
  - Change to: `sublinkTarget = ""`

**Also update content feature links to use `dstFeatureGuid` instead of `cid`:**

Currently, content nav links are constructed with the portal query string but include `target="_blank"`. Once the target is removed, the links must navigate within the portal by using the feature GUID pattern (same as addon features). The link URL should use `dstFeatureGuid` so the portal framework routes to the content feature.

Review whether content feature nav items already use `dstFeatureGuid` in their link construction or if they use `cid` directly. If using `cid`, change to use `dstFeatureGuid` so the portal routing handles them.

### Step 5: Update FeatureListView.cs -- Content Feature Links

**File:** `source/Processor/Addons/PortalFramework/Views/FeatureListView.cs`

**Change lines 33-37** from generating `target="_blank"` links with `cid` parameter:

```csharp
string qs = frameRqs;
qs = cp.Utils.ModifyQueryString(qs, "addonid", "", false);
qs = cp.Utils.ModifyQueryString(qs, Constants.rnDstFeatureGuid, "", false);
qs = cp.Utils.ModifyQueryString(qs, "cid", liFeature.dataContentId.ToString());
items += "<li><a target=\"_blank\" href=\"?" + qs + "\">" + featureHeading + "</a></li>";
```

To generating in-portal links with `dstFeatureGuid` parameter (same as addon features):

```csharp
string qs = cp.Utils.ModifyQueryString(frameRqs, Constants.rnDstFeatureGuid, liFeature.guid);
items += "<li><a href=\"?" + qs + "\">" + featureHeading + "</a></li>";
```

### Step 6: Handle Form Actions Within Portal Context

**Critical issue:** Edit/List views generate HTML forms that post back to the admin route with parameters like `af`, `aa`, `cid`, `id`. After a form submission (save, delete, add), the response needs to stay within the portal context.

**Approach:** The form action URL in Edit/List views is typically the current page URL with hidden form fields for `af`, `aa`, `cid`, etc. Since the portal addon sets `frameRqs` and `RefreshQueryString` to include portal context parameters (`setPortalId`, `dstFeatureGuid`, `addonGuid`), the form's refresh query string should already include these.

**Verify and adjust:**
- Check how `EditView` and `ListView` construct their form action URLs
- Ensure the form action includes the portal context parameters from `RefreshQueryString`
- The portal content editor addon must read the form submission parameters on post-back and process them before re-rendering

**Files to review:**
- `source/Processor/Addons/AdminSite/Views/ListView.cs` -- form action construction
- `source/Processor/Addons/AdminSite/Views/EditView.cs` -- form action construction
- `source/Processor/Controllers/AdminUI/AdminUIController.cs` -- button bar form wrappers

If forms use `core.doc.refreshQueryString` for their action URL, the portal parameters should flow through automatically because Step 3 adds them to `RefreshQueryString` before executing the addon.

### Step 7: Handle Edit-to-List and List-to-Edit Navigation

When a user clicks a record in the list view, it navigates to the edit view for that record. When saving an edit, it returns to the list view. These transitions must stay within the portal context.

**List-to-Edit:** The list view generates edit links like `?af=4&cid=X&id=Y`. These need to also include the portal context parameters. Since `RefreshQueryString` includes portal parameters, edit links built from it should work. Verify this.

**Edit-to-List:** After saving, `AdminContentController` checks for `EditReferer` or falls back to showing the index form. In the portal content editor addon, after form processing, if the destination form changes (e.g., from edit back to index), the addon should render the new form rather than redirecting.

**Edit-to-Edit (add new):** The "Add" button creates a new record and shows the edit form. This should work within the same addon execution cycle.

### Step 8: Handle the `cid` Parameter on Direct Access

When the portal framework receives a request with `dstFeatureGuid` pointing to a content feature, the new code (Step 3) sets the `contentId` doc property. But on subsequent form post-backs within the portal, the `cid` parameter may already be in the query string from the edit/list form actions.

The portal content editor addon should read `contentId` from:
1. The doc property set by the portal framework (primary)
2. Falling back to `cp.Doc.GetInteger("cid")` if the doc property is not set (for form post-backs)

This ensures the content ID persists across form submissions within the portal.

---

## Files Modified

| File | Change |
|------|--------|
| `source/Processor/Addons/PortalFramework/Addons/PortalContentEditorAddon.cs` | **New file** -- wrapper addon |
| `source/Processor/Addons/PortalFramework/Addons/PortalAddon.cs` | Change `dataContentId` branch from redirect to inline execution |
| `source/Processor/Addons/PortalFramework/Views/FeatureListView.cs` | Change content links from `target="_blank"` + `cid` to in-portal `dstFeatureGuid` links |
| `source/Processor/aoBase51.xml` | Register new "Portal Content Editor" addon |
| `source/Processor/Constants.cs` | Add GUID constant for new addon |

## Files Reviewed (may need changes based on Step 6/7 findings)

| File | Reason |
|------|--------|
| `source/Processor/Addons/AdminSite/Views/ListView.cs` | Verify form action URLs include portal context |
| `source/Processor/Addons/AdminSite/Views/EditView.cs` | Verify form action URLs include portal context |
| `source/Processor/Controllers/AdminUI/AdminUIController.cs` | Verify button bar form wrappers use RefreshQueryString |
| `source/Processor/Addons/AdminSite/Controllers/AdminContentController.cs` | Reference for form processing pipeline |

## Risks and Mitigations

### CPClass cast
`ListView.get()` and `EditView.get()` require `CPClass` and `CoreController`, but the portal framework operates on `CPBaseClass`. The wrapper addon will need to cast `CPBaseClass` to `CPClass`. This is safe because the admin site always uses `CPClass` internally, but it creates a coupling between the portal framework and the processor internals. This is acceptable since both live in the same assembly.

### Form state conflicts
Portal parameters (`setPortalId`, `dstFeatureGuid`, `addonGuid`) and edit/list parameters (`af`, `aa`, `cid`, `id`, `wl*/wr*`) could conflict. Mitigation: the portal framework sets its parameters on `RefreshQueryString` before invoking the addon, and the edit/list views add their own parameters. They use different key names, so no collisions should occur.

### Post-back routing
After a form submission (save/delete), the edit/list pipeline may try to redirect. The wrapper addon needs to intercept redirects and re-render within the portal instead. This is the highest-risk area and may require the most iteration.

### Existing direct `?cid=` access
The existing `AdminContentController` code path for direct `?cid=` access remains unchanged. Users can still access content via the admin tree navigation or direct URLs. This is intentional -- Option D does not break any existing functionality.

## Out of Scope

- Styling convergence between portal and legacy Edit/List views (can be done incrementally later)
- Redirecting direct `?cid=` access into portals (considered and deferred)
- Migrating existing `dataContentId` portal feature records to addon-based records (not needed -- the portal code handles `dataContentId` internally)
- Adding a "View in Portal" banner on direct `?cid=` access (enhancement for later)
