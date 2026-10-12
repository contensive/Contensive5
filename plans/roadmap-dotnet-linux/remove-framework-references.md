# Plan: Remove netstandard2.0 Blockers from Processor

## Phase 1 — Dead Imports (safe, no behavior change) [COMPLETED]

Remove unused `using` statements from these files:

1. `source/Processor/Controllers/Authentication/AuthWorkflowController.cs` — remove `using System.Windows.Input;`
2. `source/Processor/Controllers/Authentication/LoginWorkflowController.cs` — remove `using System.Windows.Input;`
3. `source/Processor/Addons/PageManager/OpenGraphEndOfBodyClass.cs` — remove `using System.Windows.Input;`
4. `source/Processor/Models/Domain/DashboardUserConfigModel.cs` — remove `using Microsoft.Web.Administration;`
5. `source/Processor/Addons/WidgetDashboard/WidgetDashboardCmdRemote.cs` — remove `using Microsoft.ClearScript.JavaScript;`

## Phase 2 — Unused References in Processor.csproj (safe, no behavior change) [COMPLETED]

Remove these from `source/Processor/Processor.csproj`:

1. `<UseWPF>true</UseWPF>` — only needed because of the dead imports removed in Phase 1
2. `<PackageReference Include="System.ServiceProcess.ServiceController" .../>` — zero usages in code
3. `<Reference Include="System.Data.Entity.Design">` block — .NET Framework-only assembly, zero usages
4. `<Reference Include="netstandard">` block — .NET Framework facade shim, not needed

## Phase 3 — Critical Blocker: Kernel32.dll P/Invoke [COMPLETED]

**File:** `source/Processor/Controllers/CoreController.cs` lines 130-135

**What it does:** Calls `GetSystemTimePreciseAsFileTime` for sub-microsecond timestamp precision.

**Feature lost if removed:** Sub-microsecond timer resolution. `DateTime.UtcNow` has ~15ms resolution on most Windows systems. This affects the `dateTimeNowMockable` property used for timestamps throughout the platform.

**Change:** Replace with `DateTime.UtcNow`. The 15ms resolution is more than sufficient for a CMS — no Contensive operation requires sub-millisecond precision. Remove the `[DllImport]` declaration.

## Phase 4 — Critical Blocker: ClearScript VBScript/JScript Engines

**File:** `source/Processor/Controllers/Addon/AddonScriptController.cs` (entire file)

**What it does:** Executes legacy VBScript and JScript addons stored in the database. These are addons with `scriptingCode` and `scriptingEntryPoint` fields on `AddonModel`, invoked from `AddonController.cs` lines 607/609.

**Features lost if removed:**
- Ability to execute VBScript addons (legacy feature from Classic ASP era)
- Ability to execute JScript addons (legacy feature)
- Any site that still has VBScript or JScript addons in its addon table will get errors when those addons are invoked

**Change:** Remove `AddonScriptController.cs`. In `AddonController.cs` lines 607/609, replace the calls with a log warning and return empty string — the addon won't execute, with a clear log message that scripting addons are no longer supported. Remove the `Microsoft.ClearScript` package reference from the csproj.

## Phase 5 — Critical Blocker: Nustache.Core (Mustache Templating)

**File:** `source/Processor/Controllers/MustacheController.cs` line 29

**What it does:** Renders Mustache templates (`{{variable}}`, `{{#section}}...{{/section}}`). Used in ~15 call sites across authentication forms, email templates, page rendering, and the edit UI.

**Features lost if removed:** ALL Mustache template rendering — authentication pages, email, page layouts, edit UI would all break. **Cannot remove — must replace.**

**Mustache features used in the codebase:**
- Variable interpolation: `{{name}}`, `{{groupCaption}}`
- Triple-brace (unescaped HTML): `{{{checkboxInput}}}`, `{{{adminNav}}}`, `{{{content}}}` — used heavily in layout templates
- Sections (lists/truthiness): `{{#hasMenu}}...{{/hasMenu}}`, `{{#rowList}}...{{/rowList}}`
- Inverted sections: `{{^hasMenu}}...{{/hasMenu}}` (in `ellipseMenu.html`)
- Implicit iterator: `{{.}}` (in `{{#Phones}}x{{.}}y{{/Phones}}`)
- No partials (`{{>partial}}`), no helpers — only standard Mustache spec features

**Compatibility analysis (Nustache vs Stubble):**

Stubble was designed as a Nustache replacement by the same maintainer. Both implement the Mustache spec, but there are behavioral differences:

1. **Case sensitivity (most likely issue):** Nustache does case-insensitive property lookups by default. Stubble is case-sensitive by default. A template with `{{name}}` will fail silently if the C# property is `Name`. **Fix:** enable `SetIgnoreCaseOnKeyLookup(true)` in the Stubble builder config.

2. **Missing properties:** Nustache is stricter and may throw on missing properties. Stubble silently outputs empty string by default. This is actually safer for a CMS — no change needed.

3. **Partials:** Nustache has built-in template loading for partials. Stubble does not. Not a concern — no partials are used in the codebase.

4. **Helpers:** Nustache supports non-spec helper functions. Stubble does not. Not a concern — no helpers are used in the codebase.

**Change:**

1. Add `<PackageReference Include="Stubble.Core" ... />` to `Processor.csproj`. StrongNamer (already in the project) handles signing at build time.

2. Replace `MustacheController.renderStringToString` implementation:
   ```csharp
   private static readonly Stubble.Core.StubbleVisitorRenderer stubble =
       new Stubble.Core.Builders.StubbleBuilder()
           .Configure(settings => {
               settings.SetIgnoreCaseOnKeyLookup(true);
           })
           .Build();

   public static string renderStringToString(string template, object dataSet) {
       if (string.IsNullOrEmpty(template)) { return string.Empty; }
       if (dataSet is null) { return template; }
       return stubble.Render(template, dataSet);
   }
   ```
   The renderer is a static singleton (thread-safe). `SetIgnoreCaseOnKeyLookup(true)` maintains Nustache's case-insensitive behavior.

3. Remove the local `Libs\Nustache.Core.dll` file and all its references from `Processor.csproj` (the `<Reference>` block and the `<None Include="Libs\Nustache.Core.dll" ...>` pack item).

4. Run the existing mustache unit tests (`MustacheControllerTest.renderStringToString_Test` and `MustacheControllerTests_copilot.renderStringToString_ShouldRenderTemplateWithDataSet`) to verify compatibility. These tests cover variable interpolation, sections with arrays, and implicit iterator.

## Phase 6 — Critical Blocker: Microsoft.Web.Administration (IIS Management) [COMPLETED]

**File:** `source/Processor/Controllers/WebServerController.cs` lines 549-1413

**What it does:** Full IIS management from the Processor library — creates/verifies/deletes app pools, creates/verifies/deletes websites, manages bindings, creates virtual directories, recycles app pools, and syncs IP security deny lists.

**Features lost if removed:**
- `recycle()` — recycling IIS app pools (`cc --iisreset`)
- `verifyAppPool()` — creating/configuring IIS app pools (`cc -n`)
- `stopAppPool()` — stopping app pools before upgrades
- `deleteAppPool()` — removing app pools (`cc --delete`)
- `verifyWebsite()` — creating IIS sites with bindings (`cc -n`)
- `deleteWebsite()` — removing IIS sites (`cc --delete`)
- `verifyWebsiteBinding()` — adding domain bindings
- `isValidBinding()` — validating IIS bindings exist
- `verifyCdnVirtualDirectory()` — creating CDN virtual directories
- `syncIpBlocksToIIS()` — applying IP deny lists

**This is the largest blocker.** However, none of these methods need to be accessible from Processor.dll at runtime. Call-site analysis shows:

| Method | Called from | Project |
|--------|-----------|---------|
| `verifySite()` | NewAppCmd, NewAppFrameworkCmd, MigrateWebrootCmd | CLI |
| `verifySite()` | BuildController (new builds only, runs from CLI) | Processor |
| `stopAppPool()` | DeleteAppCmd | CLI |
| `deleteAppPool()` | DeleteAppCmd | CLI |
| `deleteWebsite()` | DeleteAppCmd | CLI |
| `verifyWebsiteBinding()` | DomainCmd | CLI |
| `isValidBinding()` | ServerDiagnosticCmd | CLI |
| `recycle()` | iisrecycleCmd | CLI |
| `verifyCdnVirtualDirectory()` | BuildController (runs from CLI) | Processor |
| `syncIpBlocksToIIS()` | CPSiteClass (runs from TaskService) | Processor |

All callers run in the CLI or TaskService process — never from addon code in a web request.

**Change:** Create a new standalone console exe project (`source/IisUtil/IisUtil.csproj`) targeting `net9.0-windows`. Move all IIS management logic from `WebServerController.cs` into this exe. It accepts command-line arguments for each operation:

```
iisutil verify-site --name myApp --domain example.com --path C:\inetpub\myApp
iisutil verify-apppool --name myApp [--framework]
iisutil stop-apppool --name myApp
iisutil delete-apppool --name myApp
iisutil delete-site --name myApp
iisutil recycle --name myApp
iisutil verify-binding --name myApp --domain example.com
iisutil is-valid-binding --name myApp --domain example.com
iisutil verify-cdn-vdir --name myApp --cdn-prefix /cdn --physical-path D:\files
iisutil sync-ip-blocks --name myApp --ip 1.2.3.4 --ip 5.6.7.8
```

In `WebServerController.cs`, replace the IIS methods with thin wrappers that shell out to `iisutil.exe` via `Process.Start()`. The exe path is resolved from the Contensive install directory (same folder as `cc.exe`). Remove the `Microsoft.Web.Administration` package from Processor.csproj entirely.

The new project:
- Added to `ContensiveCommon.sln`
- References `Microsoft.Web.Administration` directly
- Published alongside CLI and TaskService in `build-core.cmd`
- Ships in the deployment zip in a `Cli/` subfolder (same location as `cc.exe`)

No IIS functionality is lost — the operations are identical, just executed out-of-process.
