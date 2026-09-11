# Staging Gate Feature Plan

**Created:** 2026-07-08
**Updated:** 2026-09-11
**Status:** Planned (not yet implemented)
**Branch:** sprint12

## Problem

Staging and testing environments need a simple way to prevent unauthorized public access without requiring full user authentication. WordPress achieves this with HTTP Basic Auth at the web server level. Contensive needs an equivalent — a lightweight passphrase gate that can be toggled on/off through existing site configuration, blocking all public routes until a shared passphrase is provided.

This is explicitly **not** user authentication. It is a single shared word configured by the admin in Site Settings, shared with anyone who needs access to the staging site.

## Design Decision: Site Properties + Admin-Configured Word (No New DB Tables)

**No new database tables or collection XML needed.** The feature uses:
- The existing `ccSetup` site property system for both the enable flag and the passphrase word (same pattern as `blockNonProductionEmail`, `anonymousUserResponseID`, etc.)
- A simple word configured by the admin in the Site Settings form, Website tab
- The gate is active when the passphrase word is non-empty; no separate enable/disable toggle needed

This keeps the feature entirely within the Processor project, consistent with existing patterns, and avoids collection XML complexity for what is fundamentally a configuration setting.

### Why a simple fixed word?
- Easy to share with clients, testers, and stakeholders — just tell them the word
- Admin sets and changes it from Site Settings > Website tab — no config files, no restarts
- Clearing the word disables the gate — one field controls both enable and passphrase
- No cryptographic complexity — the purpose is to deter casual public access, not provide security

---

## Implementation Details

### 1. New file: `source/Processor/Controllers/StagingGateController.cs`

A static controller class with two methods:

#### `public static string processGate(CoreController core, string normalizedRoute)`
- Called from `RouteController.executeRoute()` when staging gate passphrase is set
- Returns empty string to allow the request through, or returns HTML to block it
- Logic flow:
  1. **Check if gate is active** — read `core.siteProperties.stagingGatePassphrase`; if empty, return empty (gate disabled)
  2. **Bypass admin route** — compare `normalizedRoute` against `normalizeRoute(core.appConfig.adminRoute)` so admins can still log in
  3. **Bypass authenticated users** — if `core.session.isAuthenticated`, allow through (covers admins, developers, and regular logged-in users)
  4. **Bypass robots.txt / favicon.ico** — allow these common non-content requests
  5. **Check staging gate cookie** — read cookie `{cookiePrefix}stagingGate`, decrypt with `SecurityController.decryptTwoWay()`, verify decrypted content matches `stagingGate|{passphrase}`
  6. **Check form POST** — read `core.docProperties.getText("stagingGatePassphrase")`, compare (case-insensitive) against the configured passphrase. If correct, set cookie and return empty. If wrong, return form with error message.
  7. **Show form** — no cookie, no submission — return the passphrase form HTML

#### `private static string getStagingGateForm(string errorMessage)`
- Returns a self-contained HTML document with a minimal passphrase form
- Standalone HTML (no dependency on template/layout system) since this runs before route processing
- Simple centered form: title, optional error message, text input, submit button
- Form POSTs to the same URL with field name `stagingGatePassphrase`

#### Cookie approach
- Cookie name: `{core.session.cookiePrefix}stagingGate`
- Cookie value: `SecurityController.encryptTwoWay(core, "stagingGate|{passphrase}")` — encrypted with the app's `privateKey`
- Expiry: 30 days (long-lived since the passphrase is fixed; cookie auto-invalidates if admin changes the word)
- Set via `core.webServer.addResponseCookie(name, value, expires)` — the 3-parameter overload at `WebServerController.cs:636` handles domain scoping automatically
- Validation: decrypt cookie, check that decrypted content matches `stagingGate|{current passphrase}` — auto-invalidates if admin changes the word

### 2. Modify: `source/Processor/Controllers/SitePropertyController.cs`

Add a `stagingGatePassphrase` string property following the text property pattern:

```csharp
public string stagingGatePassphrase {
    get {
        if (_stagingGatePassphrase != null) { return _stagingGatePassphrase; }
        _stagingGatePassphrase = getText(spStagingGatePassphrase, "");
        return _stagingGatePassphrase;
    }
    set {
        _stagingGatePassphrase = value;
        setProperty(spStagingGatePassphrase, value);
    }
}
private readonly string spStagingGatePassphrase = "stagingGatePassphrase";
private string _stagingGatePassphrase = null;
```

Insert after the existing `blockNonProductionEmail` property block (~line 71).

### 3. Modify: `source/Processor/aoBase51.xml` — Site Settings FormXML

Add a new `SiteProperty` element in the **Website tab** of the Site Settings addon:

```xml
<SiteProperty Caption="Staging Gate Passphrase" Name="stagingGatePassphrase" ReadOnly="0" Type="text" Selector="" Description="Enter a simple word to block anonymous access to this site. Visitors must enter this word before they can view any page. Leave blank to disable the gate. The admin route is always accessible so administrators can log in."></SiteProperty>
```

This goes inside the existing `<Tab Name="Website" ...>` element, alongside other website settings.

### 4. Modify: `source/Processor/Controllers/RouteController.cs`

Insert the gate check in `executeRoute()` at approximately line 100 (after route normalization at lines 89-99, before `tryExecuteAjaxfnRoute` at line 104):

```csharp
//
// -- staging gate: block unauthenticated access when passphrase is set
{
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
| `getText()` | `SitePropertyController.cs` | — | Read passphrase string from ccSetup |
| `setProperty()` | `SitePropertyController.cs` | 580 | Write passphrase to ccSetup |
| `encryptTwoWay()` | `SecurityController.cs` | 131 | Encrypt cookie value with app privateKey |
| `decryptTwoWay()` | `SecurityController.cs` | — | Decrypt cookie value |
| `addResponseCookie()` | `WebServerController.cs` | 636 | Set cookie with auto domain scoping |
| `requestCookie()` | `WebServerController.cs` | 492 | Read cookie from request |
| `docProperties.getText()` | via `WebServerController.cs` | 81 | Read form POST field (auto-loaded from HTTP form body) |
| `session.isAuthenticated` | `SessionController.cs` | — | Check if user is logged in |
| `session.cookiePrefix` | `SessionController.cs` | 560 | App-specific cookie name prefix |
| `normalizeRoute()` | `RouteController.cs` | 25 | Normalize routes for comparison |
| `blockNonProductionEmail` | `SitePropertyController.cs` | 59-71 | Pattern template for cached site property |

---

## How an Admin Manages the Passphrase

1. Log into the admin site (the admin route is always exempt from the gate)
2. Navigate to **Site Settings > Website** tab
3. Set the **Staging Gate Passphrase** field to any simple word (e.g., "preview", "staging", "demo")
4. Share that word with anyone who needs access to the staging site
5. To disable the gate, clear the field and save

---

## Verification Checklist

1. **Enable the gate**: In admin Site Settings > Website, set "Staging Gate Passphrase" to a word (e.g., "preview")
2. **Anonymous visit**: Open the site in an incognito browser — should see the passphrase form instead of site content
3. **Wrong passphrase**: Enter an incorrect value — should see error message and form again
4. **Correct passphrase**: Enter the configured word — should see site content, cookie is set
5. **Subsequent requests**: Refresh the page — cookie bypasses the form, content loads directly
6. **Admin bypass**: Navigate to `/admin` — should load the admin login without requiring the passphrase
7. **Authenticated bypass**: Log in as any user, then visit public pages — no gate displayed
8. **Change passphrase**: Change the word in Site Settings — existing cookies should invalidate, visitors must re-enter the new word
9. **Disable the gate**: Clear the passphrase field — anonymous visitors see normal site content
10. **Build**: Run `dotnet build source/Processor/Processor.csproj` to verify compilation

## Edge Cases to Consider During Implementation

- **POST body consumption**: The gate form POSTs to the same URL. After passphrase validation succeeds, the original POST data (if the user was submitting a different form) will be lost. Acceptable for staging use.
- **AJAX requests**: JavaScript clients making AJAX requests will receive the HTML form instead of expected JSON. Acceptable for staging. Could later detect `Accept: application/json` and return a 401 JSON response.
- **Form field collision**: The field name `stagingGatePassphrase` is prefixed to avoid collision with addon form fields.
- **Static files**: Already handled by `app.UseStaticFiles()` in `Program.cs` (lines 105/111) before the `MapFallback` handler, so they never reach `executeRoute()`. No special handling needed.
- **Case sensitivity**: Passphrase comparison is case-insensitive for ease of use (a staging gate is not a security boundary).
