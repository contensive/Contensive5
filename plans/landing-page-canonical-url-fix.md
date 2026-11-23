# Landing Page Canonical URL Fix

**Date:** 2026-11-22
**Status:** Pending Approval
**Target Branch:** main (small change, additional safeguards included)

## Problem Statement

When a page is set as the domain's landing page, it creates a canonical URL conflict:

1. The page has link alias URLs (e.g., `/home`)
2. The page is also accessible at the domain root (e.g., `/`)
3. Currently, the system treats the highest-ID link alias as canonical
4. This creates duplicate content with two canonical URLs for the same page

The correct behavior should be: when a page is the domain landing page, the domain root URL should ALWAYS be the canonical, and any link aliases should be treated as non-canonical (either redirected or tagged with canonical pointing to `/`).

## Current Architecture

### Canonical URL Logic (RouteController.cs:65-95)

The `addCanonicalTag()` method already has special handling for landing pages:

```csharp
// Landing page special case at lines 70-79
bool hasQueryString = !string.IsNullOrEmpty(queryStringSuffix)
    || !string.IsNullOrEmpty(core.cpParent.Request.QueryString);
if (pageId == core.domain.rootPageId && pageId != 0 && !hasQueryString) {
    // Landing page with no query string, canonical is domain root (no trailing slash)
    string domainRoot = $"{core.webServer.requestProtocol}{core.webServer.requestDomain}";
    core.html.addHeadTag($"<link rel=\"canonical\" href=\"{domainRoot}\">", "canonical tag");
    return;
}
```

**However**, this logic only applies when the route has already been resolved to a landing page. It does NOT apply when the user visits a link alias URL like `/home`.

### Route Resolution (RouteController.cs:310-386)

When a link alias is matched:

1. Line 316: Link alias route found
2. Lines 324-367: Check if redirect is needed based on `core.siteProperties.pageAliasRedirectMethod`:
   - `1` = permanent redirect
   - `2` = temporary redirect
   - other = no redirect, just add canonical tag
3. Lines 350-377: If canonical tag mode, calls `addCanonicalTag()` with the page ID

**Problem**: The `addCanonicalTag()` call at line 354 and 377 passes the `linkAliasPageId`, but it doesn't know whether that page is the domain landing page. The landing page check at line 73 compares `pageId == core.domain.rootPageId`, which would match, BUT the method is called with a `replacementUrl` parameter pointing to the link alias URL, not the domain root.

### Route Map Building (RouteMapModel.cs:159-234)

Link aliases are added to the route map with this logic:

- Lines 185-186: Create a unique key per page+querystring: `pageId + "/" + queryStringSuffix`
- Lines 186-197: If this page/qs combo already exists, mark it as a redirect to the first (highest ID) link alias
- Lines 198-233: If first occurrence, add it to the route map as the canonical

**Problem**: This logic doesn't check if the page is a domain landing page, so it treats all link aliases equally based on ID order only.

### Domain Model (DomainModel.cs:40)

The `rootPageId` field stores which page is the landing page for each domain.

### Redirect/Canonical Setting (SitePropertyController.cs:36-52)

The `pageAliasRedirectMethod` property controls behavior:
- `1` = permanent redirect to canonical
- `2` = temporary redirect to canonical (default)
- other = no redirect, canonical tag only

## Configuration Error: Multiple Domains with Same Landing Page

**Current State:** Not validated anywhere in the codebase.

**Desired State:** Add a warning to `/status` endpoint diagnostics that flags when multiple domains share the same `rootPageId`.

**Resolution Rule:** If this configuration error exists, use the domain with the highest ID as the canonical for that page.

## Solution Design

### 1. Modify RouteMapModel.cs Route Building

**File:** `source/Processor/Models/Domain/RouteMapModel.cs`
**Location:** Lines 159-234 (link alias building section)

**Change:** When building link aliases, check if the page is a domain landing page. If it is, mark ALL link aliases for that page as redirects to the domain root.

**Current Logic:**
```csharp
string pageKey = linkAlias.pageId + "/" + linkAlias.queryStringSuffix;
if (usedPages.ContainsKey(pageKey)) {
    // This is a duplicate link alias, redirect to the first
    result.routeDictionary.Add(localRoute, new RouteClass {
        linkAliasRedirect = "/" + usedPages[pageKey]
    });
    continue;
}
```

**New Logic:**
```csharp
string pageKey = linkAlias.pageId + "/" + linkAlias.queryStringSuffix;

// Check if this page is a domain landing page
bool isLandingPage = false;
string landingPageRedirect = "";
// Need to check ALL domains to see if any use this page as landing
// Store this in a Dictionary<int, string> built at the start of the method
// Key: pageId, Value: domain root URL (empty string for the highest-ID domain)
if (landingPageDictionary.ContainsKey(linkAlias.pageId) && string.IsNullOrEmpty(linkAlias.queryStringSuffix)) {
    isLandingPage = true;
    landingPageRedirect = landingPageDictionary[linkAlias.pageId]; // empty string means use "/"
}

if (isLandingPage) {
    // Landing page link alias: always redirect to domain root
    result.routeDictionary.Add(localRoute, new RouteClass {
        physicalRoute = physicalFile,
        virtualRoute = mapRoute,
        routeType = RouteTypeEnum.linkAlias,
        linkAliasId = linkAlias.id,
        linkAliasPageId = linkAlias.pageId,
        linkAliasRedirect = string.IsNullOrEmpty(landingPageRedirect) ? "/" : landingPageRedirect
    });
    continue;
}

if (usedPages.ContainsKey(pageKey)) {
    // This is a duplicate link alias, redirect to the first
    // ... existing logic
}
```

**Additional Work Needed:**
- Build a `Dictionary<int, string>` at the start of the route map creation
- Query all domains to find which pages are landing pages
- For pages used by multiple domains, use the highest domain ID as the canonical

### 2. Add Multi-Domain Landing Page Warning

**File:** `addons/aoStatus/server/Status/StatusRemoteMethod.cs`
**Location:** `RunSiteDiagnostics()` method, around line 303 (after the robots.txt check)

**Change:** Add a new diagnostic check that queries for pages used as landing pages by multiple domains.

```csharp
//
// -- check for pages used as landing page by multiple domains (warning only)
{
    using (DataTable dt = cp.Db.ExecuteQuery(
        "select rootPageId, count(*) as domainCount " +
        "from ccdomains " +
        "where rootPageId > 0 and active > 0 " +
        "group by rootPageId " +
        "having count(*) > 1"
    )) {
        if (dt != null && dt.Rows.Count > 0) {
            foreach (DataRow row in dt.Rows) {
                int pageId = Convert.ToInt32(row["rootPageId"]);
                int domainCount = Convert.ToInt32(row["domainCount"]);
                string pageWarning = $"Page ID {pageId} is configured as the landing page for {domainCount} different domains. This creates canonical URL conflicts. Each domain should have a unique landing page.";
                resultList.AppendLine($"warning, {pageWarning}");
                warningsList.Add(pageWarning);
            }
        } else {
            resultList.AppendLine("ok, no duplicate landing pages across domains.");
        }
    }
}
```

### 3. Update addCanonicalTag() for Safety (Optional Enhancement)

**File:** `source/Processor/Controllers/RouteController.cs`
**Location:** Lines 65-95

**Change:** Add an additional check to ensure landing pages with link aliases don't accidentally get the wrong canonical URL.

This is defensive programming - with the route map changes, this shouldn't be hit, but it provides an extra safety net.

```csharp
// Before line 82 (the "normal page" section), add:
// -- if this is a landing page but was accessed via a link alias, still use domain root
if (pageId == core.domain.rootPageId && pageId != 0 && string.IsNullOrEmpty(queryStringSuffix)) {
    // Landing page accessed via link alias - canonical should be domain root
    string domainRoot = $"{core.webServer.requestProtocol}{core.webServer.requestDomain}";
    core.html.addHeadTag($"<link rel=\"canonical\" href=\"{domainRoot}\">", "canonical tag");
    return;
}
```

Actually, looking at the existing code, this is already handled at lines 70-79. The issue is the `overrideCanonicalPath` parameter.

## Risk Assessment

### Routing Risk: LOW-MEDIUM

**Risk Factors:**
1. Changes are isolated to route map building - doesn't affect route execution
2. Route map building already has complex logic for handling duplicates
3. The route dictionary is cached and rebuilt on changes - easy to invalidate if issues occur

**Mitigation:**
1. Add null checks and try-catch around domain landing page dictionary building
2. Add logging for when a link alias is marked as a landing page redirect
3. If the domain query fails, fall back to current behavior (no landing page special handling)
4. The route map is already fault-tolerant (line 238-241) - returns empty map on exception

### Canonical Tag Risk: LOW

**Risk Factors:**
1. The canonical tag logic already has landing page handling
2. Only adding a defensive check, not changing core logic
3. Canonical tags are HTML-only, don't affect routing

### Status Endpoint Risk: VERY LOW

**Risk Factors:**
1. Status endpoint only adds a warning, doesn't change behavior
2. Query is simple and isolated
3. Wrapped in try-catch with error handling

**Mitigation:**
1. Wrap in try-catch like all other diagnostic checks
2. Use `warningsList.Add()` pattern already established in the code
3. If query fails, don't fail the status check - just skip this diagnostic

### Overall Risk: LOW

This is a suitable change for the main branch with the following safeguards:

1. **Null checks** on domain queries
2. **Try-catch blocks** around new dictionary building
3. **Logging** for debugging if issues arise
4. **Fallback behavior** if domain query fails (use current logic)
5. **Route map cache invalidation** ensures any issues can be fixed by clearing cache

## Implementation Steps

### Step 1: Add Multi-Domain Landing Page Diagnostic (Lowest Risk)

1. Edit `addons/aoStatus/server/Status/StatusRemoteMethod.cs`
2. Add the diagnostic check after line 323
3. Test by temporarily setting two domains to same landing page
4. Verify warning appears in `/status?format=json` output

### Step 2: Modify Route Map Building (Core Change)

1. Edit `source/Processor/Models/Domain/RouteMapModel.cs`
2. Add method to build landing page dictionary at start of `create()` method (after line 93):
   ```csharp
   Dictionary<int, string> landingPageDict = buildLandingPageDictionary(core);
   ```
3. Add new private method `buildLandingPageDictionary()` to query domains and build the dictionary
4. Modify the link alias loop (lines 162-234) to check if page is landing page
5. Add logging when a link alias is marked as landing page redirect
6. Test with a page that is both a landing page and has link aliases

### Step 3: Add Defensive Canonical Tag Check (Safety Net)

1. Edit `source/Processor/Controllers/RouteController.cs`
2. Review the existing landing page logic (lines 70-79)
3. Verify it already handles the case correctly
4. If needed, add additional check before line 82
5. Test by accessing landing page via link alias URL

### Step 4: Testing

1. Create test page with link alias `/test-home`
2. Set it as domain landing page
3. Access `/test-home` - should redirect to `/` (or show canonical tag pointing to `/`)
4. Access `/` - should show page content with canonical pointing to `/`
5. Check route map cache to verify redirect is set correctly
6. Run `/status?format=json` to verify no warnings for single landing page
7. Temporarily set two domains to same landing page, verify warning appears

## Files to Modify

1. `source/Processor/Models/Domain/RouteMapModel.cs` - Core logic change
2. `source/Processor/Controllers/RouteController.cs` - Defensive check (if needed)
3. `addons/aoStatus/server/Status/StatusRemoteMethod.cs` - Diagnostic warning

## Rollback Plan

If issues occur:

1. **Immediate**: Clear route map cache via admin site (clears all cached routes)
2. **Short-term**: Revert the commit to `RouteMapModel.cs`
3. **Verification**: Check error logs for exceptions in route building
4. **Testing**: Verify routes resolve correctly after cache clear

## Open Questions

None - all requirements clarified through Q&A session.

## Notes

- The existing canonical tag logic already has landing page awareness
- The issue is in route map building, not route execution
- The fix ensures landing pages use domain root as canonical across all code paths
- Status endpoint warning helps identify configuration errors
