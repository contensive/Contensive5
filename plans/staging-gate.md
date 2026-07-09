# Staging Gate Feature Plan

**Created:** 2026-07-08
**Status:** Planned (not yet implemented)
**Branch:** sprint12

## Problem

Staging and testing environments need a simple way to prevent unauthorized public access without requiring full user authentication. WordPress achieves this with HTTP Basic Auth at the web server level. Contensive needs an equivalent — a lightweight passphrase gate that can be toggled on/off through existing site configuration, blocking all public routes until a shared passphrase is provided.

This is explicitly **not** user authentication. It is a single shared passphrase that rotates daily, stored nowhere in the database — derived deterministically from the app's existing `privateKey` and the current date.

## Design Decision: Site Properties + Computed Passphrase (No New DB Tables)

**No new database tables or collection XML needed.** The feature uses:
- The existing `ccSetup` site property system for the enable/disable flag (same as `blockNonProductionEmail`, `anonymousUserResponseID`, etc.)
- A daily passphrase computed from `appConfig.privateKey + date` — zero secrets to store or manage

This keeps the feature entirely within the Processor project, consistent with existing patterns, and avoids collection XML complexity for what is fundamentally a configuration toggle.

### Why not a separate system (outside the database)?
- Contensive already has a mature site property system (`ccSetup` table) with lazy caching, admin UI access, and `getBoolean`/`setProperty` methods.
- Using it means the feature is toggleable from the admin site without touching config files or restarting the app.
- The passphrase itself is **not** stored in the database — it is computed from `appConfig.privateKey` (already in the app config JSON) and the current date, so there is no secret management burden.

### Why a daily auto-rotating passphrase?
- Eliminates risk of a passphrase being shared beyond its intended audience and lingering indefinitely.
- No admin action needed to rotate — it happens automatically at midnight.
- The passphrase is deterministic (SHA256-based), so any admin can compute it independently from the same `privateKey`.

---

## Implementation Details

### 1. New file: `source/Processor/Controllers/StagingGateController.cs`

A static controller class with three methods:

#### `public static string processGate(CoreController core, string normalizedRoute)`
- Called from `RouteController.executeRoute()` when staging gate is enabled
- Returns empty string to allow the request through, or returns HTML to block it
- Logic flow:
  1. **Bypass admin route** — compare `normalizedRoute` against `normalizeRoute(core.appConfig.adminRoute)` so admins can still log in
  2. **Bypass authenticated users** — if `core.session.isAuthenticated`, allow through (covers admins, developers, and regular logged-in users)
  3. **Bypass robots.txt / favicon.ico** — allow these common non-content requests
  4. **Check staging gate cookie** — read cookie `{cookiePrefix}stagingGate`, decrypt with `SecurityController.decryptTwoWay()`, verify decrypted content matches `stagingGate|{today's date}`
  5. **Check form POST** — read `core.docProperties.getText("stagingGatePassphrase")`, compare against `getDailyPassphrase()`. If correct, set cookie and return empty. If wrong, return form with error message.
  6. **Show form** — no cookie, no submission — return the passphrase form HTML

#### `public static string getDailyPassphrase(CoreController core)`
- Computes `SHA256(privateKey + "|stagingGate|" + yyyy-MM-dd)`, takes first 4 bytes as 8-char hex string
- Uses `core.dateTimeNowMockable` for testability
- Produces codes like `a3f7c1b2` — changes daily at midnight automatically

#### `private static string getStagingGateForm(CoreController core, string errorMessage)`
- Returns a self-contained HTML document with a minimal passphrase form
- Standalone HTML (no dependency on template/layout system) since this runs before route processing
- Simple centered form: title, optional error message, text input, submit button
- Form POSTs to the same URL with field name `stagingGatePassphrase`

#### Cookie approach
- Cookie name: `{core.session.cookiePrefix}stagingGate`
- Cookie value: `SecurityController.encryptTwoWay(core, "stagingGate|yyyy-MM-dd")` — encrypted with the app's `privateKey`
- Expiry: end of current day (`dateTimeNowMockable.Date.AddDays(1)`)
- Set via `core.webServer.addResponseCookie(name, value, expires)` — the 3-parameter overload at `WebServerController.cs:636` handles domain scoping automatically
- Validation: decrypt cookie, check that decrypted content matches today's date string — auto-invalidates on date change

### 2. Modify: `source/Processor/Controllers/SitePropertyController.cs`

Add a `stagingGateEnabled` boolean property following the exact `blockNonProductionEmail` pattern (lines 59-71):

```csharp
public bool stagingGateEnabled {
    get {
        if (_stagingGateEnabled != null) { return (bool)_stagingGateEnabled; }
        _stagingGateEnabled = getBoolean(spStagingGateEnabled, false);
        return (bool)_stagingGateEnabled;
    }
    set {
        _stagingGateEnabled = value;
        setProperty(spStagingGateEnabled, value);
    }
}
private readonly string spStagingGateEnabled = "stagingGateEnabled";
private bool? _stagingGateEnabled = null;
```

Insert after the existing `blockNonProductionEmail` property block (~line 71).

### 3. Modify: `source/Processor/Controllers/RouteController.cs`

Insert the gate check in `executeRoute()` at approximately line 100 (after route normalization at lines 89-99, before `tryExecuteAjaxfnRoute` at line 104):

```csharp
//
// -- staging gate: block unauthenticated access when enabled
if (core.siteProperties.stagingGateEnabled) {
    string stagingGateResult = StagingGateController.processGate(core, requestedRoute);
    if (!string.IsNullOrEmpty(stagingGateResult)) {
        return stagingGateResult;
    }
}
```

This location ensures:
- **ALL routes are gated** (page manager, remote methods, AJAX, link aliases — everything)
- The `core` object is fully initialized (DB, session, cookies, docProperties all available)
- The admin route is accessible for bypass checking via `core.appConfig.adminRoute`

---

## Request Pipeline Context

Understanding where the gate sits in the request flow:

```
HTTP Request → Program.cs executeManagedRoute() [line 127]
  → CPClass constructor → CoreController constructor [line 650]
    → Session initialization [line 700]
    → Auth event processing [line 707]
  → RouteController.executeRoute() [line 64]
    → Route normalization [lines 89-99]
    → *** STAGING GATE CHECK HERE *** [new, ~line 100]
    → AJAX routes, email intercepts, method routes
    → Route dictionary lookup (addons)
    → Default page route → PageManagerController (existing anonymous blocking at line 64)
  → Response
```

The existing `anonymousUserResponseID` blocking in `PageManagerController.getHtmlBody()` (line 64-117) only blocks page manager routes. The staging gate is different — it blocks ALL routes at the `RouteController` level.

---

## Key Existing Code Reused

| What | File | Line | Purpose |
|------|------|------|---------|
| `getBoolean()` | `SitePropertyController.cs` | 798 | Read enable flag from ccSetup |
| `setProperty()` | `SitePropertyController.cs` | 580 | Write enable flag to ccSetup |
| `encryptTwoWay()` | `SecurityController.cs` | 131 | Encrypt cookie value with app privateKey |
| `decryptTwoWay()` | `SecurityController.cs` | — | Decrypt cookie value |
| `appConfig.privateKey` | `AppConfigModel.cs` | 37 | Seed for passphrase derivation |
| `dateTimeNowMockable` | `CoreController.cs` | 125 | Mockable date for testability |
| `addResponseCookie()` | `WebServerController.cs` | 636 | Set cookie with auto domain scoping |
| `requestCookie()` | `WebServerController.cs` | 492 | Read cookie from request |
| `docProperties.getText()` | via `WebServerController.cs` | 81 | Read form POST field (auto-loaded from HTTP form body) |
| `session.isAuthenticated` | `SessionController.cs` | — | Check if user is logged in |
| `session.cookiePrefix` | `SessionController.cs` | 560 | App-specific cookie name prefix |
| `normalizeRoute()` | `RouteController.cs` | 25 | Normalize routes for comparison |
| `blockNonProductionEmail` | `SitePropertyController.cs` | 59-71 | Pattern template for the new boolean site property |

---

## How an Admin Gets Today's Passphrase

The admin logs into the admin site (the admin route is exempt from the gate), and the daily passphrase can be displayed in site settings or a diagnostic view. This is a follow-up UI task. Initially, the passphrase can be retrieved by:
- Anyone with DB access computing it from `privateKey` + date
- Calling `StagingGateController.getDailyPassphrase()` from a developer addon
- A future admin settings panel addition

---

## Verification Checklist

1. **Enable the gate**: Set site property `stagingGateEnabled` to `true` in ccSetup (via admin UI or direct DB: `UPDATE ccSetup SET FieldValue='true' WHERE name='stagingGateEnabled'`)
2. **Anonymous visit**: Open the site in an incognito browser — should see the passphrase form instead of site content
3. **Wrong passphrase**: Enter an incorrect value — should see error message and form again
4. **Correct passphrase**: Enter today's computed passphrase — should see site content, cookie is set
5. **Subsequent requests**: Refresh the page — cookie bypasses the form, content loads directly
6. **Admin bypass**: Navigate to `/admin` — should load the admin login without requiring the passphrase
7. **Authenticated bypass**: Log in as any user, then visit public pages — no gate displayed
8. **Daily rotation**: Verify that tomorrow's passphrase differs (change mockable date in tests)
9. **Disable the gate**: Set `stagingGateEnabled` to `false` — anonymous visitors see normal site content
10. **Build**: Run `dotnet build source/Processor/Processor.csproj` to verify compilation

## Edge Cases to Consider During Implementation

- **POST body consumption**: The gate form POSTs to the same URL. After passphrase validation succeeds, the original POST data (if the user was submitting a different form) will be lost. Acceptable for staging use.
- **AJAX requests**: JavaScript clients making AJAX requests will receive the HTML form instead of expected JSON. Acceptable for staging. Could later detect `Accept: application/json` and return a 401 JSON response.
- **Form field collision**: The field name `stagingGatePassphrase` is prefixed to avoid collision with addon form fields.
- **Static files**: Already handled by `app.UseStaticFiles()` in `Program.cs` (lines 105/111) before the `MapFallback` handler, so they never reach `executeRoute()`. No special handling needed.
