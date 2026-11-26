# Page Render Context Cache

## Problem

Page rendering makes repeated structural DB queries on every request that resolve the same data for the same page:

1. **Parent chain walk** — one `DbBaseModel.create<PageContentModel>` per hierarchy level (N+1 queries for a page N levels deep)
2. **Template cascade** — up to 5 fallback `DbBaseModel.create<PageTemplateModel>` queries: page → parent pages → domain default → site property → name lookup "Single Column Template" → most recent
3. **Template addon rules** — subselect: `SELECT * FROM ccaggregatefunctions WHERE id IN (SELECT addonId FROM ccAddonTemplateRules WHERE templateId=N)`
4. **Page addon rules** — subselect: `SELECT * FROM ccaggregatefunctions WHERE id IN (SELECT addonId FROM ccAddonPageRules WHERE pageId=N)`

The existing cache infrastructure (table dependency keys, lazy `CacheStore_*` pattern, three-tier Redis/memory/file cache) already supports composite cached objects. Addons, layouts, link aliases, and the route map all use this pattern. Page rendering does not.

This plan introduces two new `CacheStore` classes to cache the structural rendering data per-page, eliminating these queries on cache-warm requests.

This also resolves the invalid `rootPageId` issue — the page load in the cache builder naturally detects and self-heals invalid references without an extra query.

## Branch

Create feature branch `feature/page-render-context-cache` from `main`.

The three guard-clause commits already on `dev` (addCanonicalTag guards, WebServerController rootPageId=0 check) are independent and remain as-is.

## Current Rendering Flow

The rendering path (request → response) currently loads data in this sequence:

```
WebServerController.setRequest()
  └─ DomainModel lookup (by name, cached per-record by DbBaseModel)

RouteController.executeRoute()
  └─ RouteMapModel (composite lazy cache — already efficient)
  └─ LinkAliasModel store (lazy dict cache — already efficient)

PageManagerController.loadPage()
  ├─ PageContentModel for requested page (cached per-record)
  ├─ PageContentModel for EACH parent (N+1 problem) ◄── CACHE THIS
  └─ PageTemplateModel cascade (up to 5 attempts) ◄── CACHE THIS

PageManagerController.getHtmlBody()
  ├─ AddonModel.createList_templateDependencies() (subselect query) ◄── CACHE THIS
  ├─ AddonModel.createList_pageDependencies() (subselect query) ◄── CACHE THIS
  └─ CacheStore_AddonModel (lazy dict cache — already efficient)
```

Items marked ◄── are the per-request queries this plan eliminates.

## Design

Two new cache components, following the established `CacheStore_AddonModel` pattern:

### Component 1: CacheStore_AddonRules (global)

Replaces the per-request subselect queries against `ccAddonTemplateRules` and `ccAddonPageRules`.

**File**: `source/Processor/Models/Domain/CacheStore_AddonRules.cs`

```
CacheStore_AddonRules
  Dictionary<int, List<int>> templateAddonIds  // templateId → addonId list
  Dictionary<int, List<int>> pageAddonIds      // pageId → addonId list

  CacheStore_AddonRules()                      // empty, for cache deserialization
  CacheStore_AddonRules(CoreController core)   // loads all rows from both rule tables

  List<int> getTemplateAddonIds(int templateId)
  List<int> getPageAddonIds(int pageId)
```

- **Cache key**: `"addonRulesCache"`
- **Dependencies**: `ccaddontemplaterules`, `ccaddonpagerules`
- Loaded lazily on first access, stored in persistent cache
- When any addon rule record is added/edited/deleted, the table dependency key invalidates this cache automatically

### Component 2: CacheStore_PageRenderContext (per pageId)

Caches the resolved page hierarchy, template selection, and addon dependency IDs for a specific page.

**File**: `source/Processor/Models/Domain/CacheStore_PageRenderContext.cs`

```
CacheStore_PageRenderContext
  int pageId
  PageContentModel page
  List<PageContentModel> pageToRootList  // first=current, last=root
  int templateId                         // resolved template ID
  string templateReason                  // debug: why this template was chosen
  int[] templateAddonIds                 // from CacheStore_AddonRules
  int[] pageAddonIds                     // from CacheStore_AddonRules
  bool isLandingPage

  CacheStore_PageRenderContext()                                                        // empty, for deserialization
  CacheStore_PageRenderContext(CoreController core, int pageId, DomainModel domain, bool isLandingPage)  // builds from DB

  static (int templateId, string reason) resolveTemplate(                               // extracted cascade logic
      CoreController core, List<PageContentModel> pageToRootList,
      DomainModel domain, bool isLandingPage)
```

- **Cache key**: `"pageRenderContext-{pageId}"`
- **Dependencies**: `ccpagecontent`, `cctemplates`, `ccdomains`, `ccaddontemplaterules`, `ccaddonpagerules`
- Built lazily on first visit to each page
- Only visited pages consume cache storage

**Build logic** (constructor, runs only on cache miss):

1. Load page via `DbBaseModel.create<PageContentModel>(core.cpParent, pageId)` — already per-record cached
2. Walk `parentId` chain to build `pageToRootList`, same logic as `loadPage()` lines 1314-1327
3. Call `resolveTemplate()` — extracted from `loadPage()` lines 1340-1436, same cascade with same self-healing of invalid templateIds
4. Look up addon IDs from `core.cacheRuntime.addonRulesCache.getTemplateAddonIds()` / `getPageAddonIds()` — no DB query, just dictionary lookups

**Why store IDs not full models for template/addons**: `PageTemplateModel` is already cached per-record by `DbBaseModel`. `AddonModel` instances are already in `CacheStore_AddonModel`. Storing IDs keeps the cached object small and avoids stale nested objects.

## Changes to Existing Files

### CacheRuntimeController.cs

**Pattern to follow**: the `addonCache` lazy property at lines 89-123.

Add:

1. **`addonRulesCache`** lazy property — same pattern: check `_addonRulesCache` local → check `core.cache.getObject<CacheStore_AddonRules>()` → build → store with dependency keys → return
2. **`clearAddonRules()`** — invalidate cache key, null local
3. **`getPageRenderContext(int pageId, DomainModel domain, bool isLandingPage)`** — check `_pageRenderContextLocal[pageId]` → check `core.cache.getObject<CacheStore_PageRenderContext>(key)` → build → store → return
4. **`clearPageRenderContext()`** — clear local dict (persistent entries invalidated through table dependency keys)
5. **`_pageRenderContextLocal`** — `Dictionary<int, CacheStore_PageRenderContext>`, scoped to request lifetime
6. Update **`clear()`** (line 52) to call both new clear methods

### PageManagerController.cs — loadPage() (line 1278)

Replace the parent-chain walk (lines 1314-1327) and template cascade (lines 1340-1436) with:

```csharp
// -- get or build the page render context from cache
var ctx = core.cacheRuntime.getPageRenderContext(resolvedPageId, domain, isLandingPage);

// -- apply context to pageController
core.doc.pageController.page = ctx.page;
core.doc.pageController.pageToRootList = ctx.pageToRootList;
core.doc.pageController.template = DbBaseModel.create<PageTemplateModel>(core.cpParent, ctx.templateId);
core.doc.pageController.templateReason = ctx.templateReason;
```

The initial page load for redirect checking (`requestedPage.link`) stays as-is — it's a single per-record cached lookup.

### PageManagerController.cs — getHtmlBody() (lines 145-163)

Replace:

```csharp
// CURRENT: two subselect queries
List<AddonModel> templateAddonList = AddonModel.createList_templateDependencies(core.cpParent, templateId);
List<AddonModel> pageAddonList = AddonModel.createList_pageDependencies(core.cpParent, pageId);
```

With:

```csharp
// NEW: dictionary lookups from cached context + addon cache
var ctx = core.cacheRuntime.getPageRenderContext(page.id, core.domain, requestedPageId == 0);
foreach (int addonId in ctx.templateAddonIds) {
    AddonModel addon = core.cacheRuntime.addonCache.create(addonId);
    if (addon != null) { /* execute */ }
}
foreach (int addonId in ctx.pageAddonIds) {
    AddonModel addon = core.cacheRuntime.addonCache.create(addonId);
    if (addon != null) { /* execute */ }
}
```

### WebServerController.cs

No changes. The `rootPageId == 0` guard on `dev` handles the zero case. Invalid non-zero `rootPageId` is detected during `PageRenderContext` build when `DbBaseModel.create<PageContentModel>` returns null, triggering `getLandingPage()` which self-heals the domain record — same behavior as today, no extra query.

## Invalidation

| Trigger | What invalidates |
|---------|-----------------|
| Edit any page record | All `pageRenderContext-*` (via `ccpagecontent` table dep) |
| Edit a template record | All `pageRenderContext-*` (via `cctemplates` table dep) |
| Edit domain record | All `pageRenderContext-*` (via `ccdomains` table dep) |
| Add/edit/delete addon rule | `addonRulesCache` + all `pageRenderContext-*` |
| Edit an addon | `addonCache` only (rules and page contexts unaffected) |

This is intentionally broad — these tables change infrequently (admin-time operations). A future optimization could use per-record dependency keys to narrow invalidation scope.

## Queries Eliminated Per Request (cache warm)

| Query | Current | After |
|-------|---------|-------|
| Parent chain walk (per level) | N queries | 0 (cached in pageToRootList) |
| Template cascade | 1-5 queries | 0 (cached templateId) |
| Template addon rules subselect | 1 query | 0 (cached from addonRulesCache) |
| Page addon rules subselect | 1 query | 0 (cached from addonRulesCache) |
| **Total structural queries** | **~4-10** | **0** |

On cache miss (first visit to a page, or after any table invalidation), the same queries run once and results are cached.

## Implementation Order

| Step | Description | Files |
|------|-------------|-------|
| 1 | Create `CacheStore_AddonRules` | New: `source/Processor/Models/Domain/CacheStore_AddonRules.cs` |
| 2 | Add `addonRulesCache` to CacheRuntimeController | Modify: `source/Processor/Controllers/CacheRuntimeController.cs` |
| 3 | Create `CacheStore_PageRenderContext` with `resolveTemplate()` | New: `source/Processor/Models/Domain/CacheStore_PageRenderContext.cs` |
| 4 | Add `getPageRenderContext()` to CacheRuntimeController | Modify: `source/Processor/Controllers/CacheRuntimeController.cs` |
| 5 | Refactor `loadPage()` to use cached context | Modify: `source/Processor/Controllers/PageManagerController.cs` |
| 6 | Refactor `getHtmlBody()` addon deps to use cached IDs | Modify: `source/Processor/Controllers/PageManagerController.cs` |
| 7 | Write integration tests | New: test files in `source/ProcessorTests/` |
| 8 | Run full test suite | — |

Steps 1-2 and 3-4 are independently testable before the integration steps 5-6.

## Reference Files

| File | Relevance |
|------|-----------|
| `source/Processor/Models/Domain/CacheStore_AddonModel.cs` | Pattern to follow — constructor, empty constructor, dictionary structure, cache serialization |
| `source/Processor/Controllers/CacheRuntimeController.cs` | Lazy property pattern (lines 89-123), LayoutDict pattern (lines 207-297), `clear()` method |
| `source/Processor/Controllers/PageManagerController.cs` | `loadPage()` line 1278, `getHtmlBody()` line 61, `getLandingPage()` line 1478, template cascade lines 1340-1436 |
| `source/Models/Models/Db/AddonTemplateRuleModel.cs` | Table: `ccaddontemplaterules`, fields: `addonId`, `templateId` |
| `source/Models/Models/Db/AddonPageRuleModel.cs` | Table: `ccaddonpagerules`, fields: `addonId`, `pageId` |
| `source/Models/Models/Db/PageContentModel.cs` | Table: `ccpagecontent` |
| `source/Models/Models/Db/PageTemplateModel.cs` | Table: `cctemplates` |
| `source/Models/Models/Db/DomainModel.cs` | Table: `ccdomains` |
| `source/Processor/Models/Domain/RouteMapModel.cs` | Composite cache with multi-table dependency example |

## Verification

1. Build: `dotnet build source/Processor/Processor.csproj -f net48`
2. Run existing tests: `dotnet test source/ProcessorTests/` and `dotnet test source/ModelsTests/`
3. New integration tests:
   - `CacheStore_AddonRules` builds correct dictionaries from DB rows
   - `CacheStore_PageRenderContext` builds correct parent chain and resolves template via cascade
   - Cache invalidation triggers rebuild on next access
   - Self-healing of invalid `templateId` and `rootPageId` during cache build
   - Landing page template cascade selects correct template
   - Addon IDs from cached context match addon IDs from direct DB subselect

## Future Optimizations (not in this plan)

- **Per-record dependency keys**: Narrow invalidation so editing page 5 only invalidates `pageRenderContext-5` and its children, not all pages
- **Addon output caching**: Cache the HTML output of addon execution, not just the structural data
- **Redirect link in context**: Store `page.link` in the context so even the initial page load can be skipped on cache hit
