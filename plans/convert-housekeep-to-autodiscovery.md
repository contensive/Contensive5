# Plan: Convert Housekeeping Classes to Interface-Based Auto-Discovery

## Context

Five housekeeping classes (`TasksClass`, `AdminRecentClass`, `ContentWatchListRulesClass`, `EmailBounceListClass`, `ContentClass`) had real cleanup logic that was never wired into the orchestrators because the current pattern requires manually adding a call to two separate files whenever a new class is created. This plan replaces that error-prone manual wiring with reflection-based auto-discovery so that any class implementing `IHousekeepTask` is automatically included in the housekeeping cycle.

## Files Overview

**3 new files:**
- `source/Processor/Addons/Housekeeping/IHousekeepTask.cs` -- interface contract
- `source/Processor/Addons/Housekeeping/HousekeepOrder.cs` -- named order constants
- `source/Processor/Addons/Housekeeping/HousekeepTaskDiscovery.cs` -- assembly-scanning discovery

**2 orchestrator rewrites:**
- `source/Processor/Addons/Housekeeping/HousekeepHourlyTasksClass.cs` -- replace manual call list with discovery loop
- `source/Processor/Addons/Housekeeping/HousekeepDailyTasksClass.cs` -- replace manual call list with discovery loop

**4 method renames** (Phase 2):
- `SiteWarningsClass.setWarnings` -> `executeHourlyTasks`
- `TempFilesClass.deleteFiles` -> `executeHourlyTasks`
- `VisitSummaryClass.updateVisitSummary` -> `executeDailyTasks`
- `ViewingSummaryClass.updateViewingSummary` -> `executeDailyTasks`

**37 implementation classes** converted from `static class` to `class : IHousekeepTask`

---

## Phase 1: Define Interface and Order Constants

Create `IHousekeepTask.cs` with three members:
- `int order { get; }` -- controls execution sequence (lower = first)
- `void executeHourlyTasks(HouseKeepEnvironmentModel env)`
- `void executeDailyTasks(HouseKeepEnvironmentModel env)`

Create `HousekeepOrder.cs` with `int` constants at increments of 100:

| Constant | Value | Classes |
|---|---|---|
| Summaries | 100 | VisitSummaryClass, ViewingSummaryClass |
| People | 200 | PersonClass |
| TempFiles | 300 | TempFilesClass |
| AddonFolder | 400 | AddonFolderClass |
| Metadata | 500 | ContentClass, ContentFieldClass |
| Tables | 550 | TablesClass |
| Content | 600 | MenuEntry, RemoteQuery, PageContent, AddonContentFieldTypeRule, AddonContentTriggerRule, ContentWatch, EmailDrop, EmailLog, FieldHelp, GroupRules, MemberRule, Metadata, LinkAlias, AddonEventCatchers |
| Properties | 700 | UserProperyClass, VisitPropertyClass, VisitorPropertyClass |
| VisitsAndViewings | 800 | VisitClass, VisitorClass, ViewingsClass |
| FilenameTest | 850 | FilenameTestClass |
| Logs | 900 | ActivityLogClass |
| PostProcess | 1000 | TasksClass, AdminRecentClass, ContentWatchListRulesClass, EmailBounceListClass |
| BotDetection | 1050 | BotDetectionUpdateClass |
| SiteWarnings | 1100 | SiteWarningsClass |
| SqlMaintenance | 1200 | SqlMaintenanceClass |

**Build checkpoint:** new files are unused, build should pass.

---

## Phase 2: Rename Non-Standard Methods

Standardize 4 classes that use custom method names so every class has the same `executeHourlyTasks`/`executeDailyTasks` pattern before the bulk conversion:

1. `SiteWarningsClass.cs` -- rename `setWarnings()` to `executeHourlyTasks()`, add empty `executeDailyTasks()`
2. `TempFilesClass.cs` -- rename `deleteFiles()` to `executeHourlyTasks()`, add empty `executeDailyTasks()`
3. `VisitSummaryClass.cs` -- rename `updateVisitSummary()` to `executeDailyTasks()`
4. `ViewingSummaryClass.cs` -- rename `updateViewingSummary()` to `executeDailyTasks()`

Update orchestrator calls to match:
- `HousekeepHourlyTasksClass.cs`: `TempFilesClass.deleteFiles(env)` -> `.executeHourlyTasks(env)`, `SiteWarningsClass.setWarnings(env)` -> `.executeHourlyTasks(env)`
- `HousekeepDailyTasksClass.cs`: `VisitSummaryClass.updateVisitSummary(env)` -> `.executeDailyTasks(env)`, `ViewingSummaryClass.updateViewingSummary(env)` -> `.executeDailyTasks(env)`

**Build checkpoint:** behavior identical, all calls use standard names.

---

## Phase 3: Convert Static Classes to Instance Classes

For each of the 37 implementation classes, apply this mechanical transformation:

1. Remove `static` from class declaration
2. Add `: IHousekeepTask`
3. Add `public int order => HousekeepOrder.XXX;`
4. Change `private static readonly Logger logger` to `private readonly Logger logger`
5. Remove `static` from `executeHourlyTasks`, `executeDailyTasks`, and all private helpers
6. Remove class-name prefixes from internal self-calls (e.g. `VisitSummaryClass.summarizePeriod(...)` -> `summarizePeriod(...)`)
7. If missing `executeHourlyTasks` or `executeDailyTasks`, add an empty implementation
8. `const` fields stay as-is (they're implicitly static)

**Do in batches of 5-7 classes**, updating the orchestrator calls temporarily to `new ClassName().method(env)` after each batch, building between batches.

Special cases:
- `EmailBounceListClass` -- already non-static, just add interface + remove `static` from methods
- `TempFilesClass.deleteOldFilesReturnFilesRemaining` -- currently `public static`, make `private` instance (no external callers)

---

## Phase 4: Replace Orchestrators with Auto-Discovery

Create `HousekeepTaskDiscovery.cs`:
- Scans `Assembly.GetExecutingAssembly().GetTypes()` for `IHousekeepTask` implementations
- Creates instances via `Activator.CreateInstance(type)`
- Sorts by `order` then by class name (deterministic secondary sort)
- Caches result with thread-safe lazy initialization
- Handles `ReflectionTypeLoadException` gracefully
- Logs discovered tasks: `"discovered {count} tasks: {names}"`

Rewrite `HousekeepHourlyTasksClass.executeHourlyTasks()`:
- Replace manual call list with `foreach (var task in HousekeepTaskDiscovery.getOrderedTasks())`
- Wrap each `task.executeHourlyTasks(env)` in its own try/catch so one failure doesn't block others
- Keep the sql timeout save/restore in outer try/finally

Rewrite `HousekeepDailyTasksClass.executeDailyTasks()` with same pattern.

**Behavioral improvement:** currently one class throwing aborts the entire cycle. New pattern isolates failures per-task.

`HousekeepTask.cs` (entry point) is unchanged -- it still calls the orchestrators.

---

## Phase 5: Verification

1. Build both targets: `dotnet build source/Processor/Processor.csproj` (multi-targets net48 + net9.0-windows)
2. Grep for any remaining references to renamed methods (`setWarnings`, `deleteFiles`, `updateVisitSummary`, `updateViewingSummary`)
3. Grep for any remaining `static class` declarations in the Housekeeping folder (should be only `HousekeepOrder`, `HousekeepTaskDiscovery`, `HousekeepHourlyTasksClass`, `HousekeepDailyTasksClass`)
4. Verify the discovery log line shows all 37 classes in correct order
