
# Contensive Best Practices

> All patterns and API reference: [Patterns Index](https://raw.githubusercontent.com/contensive/Contensive5/refs/heads/master/patterns/index.md)

## Overview

This document covers best practices for writing reliable, maintainable Contensive code. Following these conventions ensures consistent error handling and predictable behavior across all addons and libraries.

## Error Handling

All methods should be wrapped in a try/catch block that reports errors using `cp.Site.ErrorReport(ex)`. The behavior in the catch block depends on whether the method is an addon execute method or an internal/library method.

### Addon Execute Methods

Addon `Execute` methods should catch exceptions, report the error, and return a user-friendly string indicating the addon failed. Never allow an unhandled exception to propagate out of an addon execute method.

```csharp
public override object Execute(CPBaseClass cp) {
    try {
        // -- addon logic here
        return result;
    } catch (Exception ex) {
        cp.Site.ErrorReport(ex);
        return "There was an error executing this addon.";
    }
}
```

### Non-Addon Methods (Default: Report and Throw)

For non-addon methods (services, controllers, helpers, etc.), the default practice is to report the error and then rethrow so the caller is aware of the failure. This preserves the exception for upstream handling and prevents silent failures.

```csharp
public void ProcessOrder(CPBaseClass cp, int orderId) {
    try {
        // -- processing logic here
    } catch (Exception ex) {
        cp.Site.ErrorReport(ex);
        throw;
    }
}
```

### Exception: Non-Critical Elements in Critical Workflows

The one exception to the report-and-throw rule is when a method is a non-critical element within a critical workflow that should not be interrupted. Examples include logging, analytics, telemetry, or reporting components. In these cases, report the error and swallow the exception so the main workflow continues uninterrupted.

```csharp
public void SaveOrder(CPBaseClass cp, int orderId) {
    try {
        // -- critical: save the order
        SaveOrderToDatabase(cp, orderId);
        //
        // -- non-critical: log the activity (should not interrupt order save)
        try {
            LogOrderActivity(cp, orderId);
        } catch (Exception ex) {
            cp.Site.ErrorReport(ex);
            // -- do not throw; logging failure should not interrupt the order workflow
        }
    } catch (Exception ex) {
        cp.Site.ErrorReport(ex);
        throw;
    }
}
```

## Database Changes: Collection XML First

When adding a new database table or field, always define it in the addon collection XML file **before** adding corresponding C# code (model properties, controller logic, etc.). The collection XML is the source of truth for database schema — the installer uses it to create and update tables and columns. A field in code without a matching `<Field>` in the collection XML will not exist in the database at runtime.

**Required workflow:**
1. Add the `<CDef>` (new table) or `<Field>` (new column) to the collection XML
2. Then add the C# model property and any code that references the field
3. See [Addon Collection Pattern](addon-collection-pattern.md) for XML syntax

## UI: HTML Selector Naming

When creating HTML classes or IDs, separate concerns between JavaScript binding and CSS styling by following these rules:

- **JavaScript selectors** — Always prefix with `js-`. These selectors exist solely for JavaScript to find and interact with elements.
- **CSS selectors** — Never use the `js-` prefix. These selectors exist solely for styling.
- **Never mix the two** — A `js-` prefixed selector must never appear in CSS. A CSS selector should not be used as a JavaScript binding target.

This separation ensures that styling changes never accidentally break JavaScript behavior, and JavaScript refactors never accidentally break styling.

```html
<!-- correct: js- prefix for JavaScript binding, separate class for styling -->
<button class="btn btn-primary js-submit-form">Submit</button>
```

```javascript
// correct: JavaScript binds to the js- prefixed selector
document.querySelector(".js-submit-form").addEventListener("click", handleSubmit);
```

```css
/* correct: CSS styles the non-prefixed classes only */
.btn-primary { background-color: #007bff; }

/* WRONG: never style a js- selector */
.js-submit-form { background-color: #007bff; }
```

## CSRF Protection on HTML Forms

All HTML forms generated with `cp.Html.Form()` automatically include a CSRF token as a hidden field. When processing a form submission, always call `cp.Html.VerifyFormCsrf()` before acting on the posted data. If verification fails, reject the request.

```csharp
public override object Execute(CPBaseClass cp) {
    try {
        // -- check for form submission
        if (!string.IsNullOrEmpty(cp.Doc.GetText("button"))) {
            // -- verify CSRF token before processing
            if (!cp.Html.VerifyFormCsrf()) {
                return "Invalid form submission.";
            }
            // -- process the form
            string name = cp.Doc.GetText("name");
            // ...
        }
        // -- render the form (CSRF hidden field is injected automatically)
        string innerHtml = ""
            + cp.Html.InputText("name")
            + cp.Html.Button("button", "Submit");
        return cp.Html.Form(innerHtml);
    } catch (Exception ex) {
        cp.Site.ErrorReport(ex);
        return "There was an error executing this addon.";
    }
}
```

- `cp.Html.Form()` automatically injects a hidden `csrfToken` field — no manual step is required when rendering.
- `cp.Html.VerifyFormCsrf()` compares the submitted token against the token stored in the visit session. It returns `true` if they match, `false` otherwise.
- Always verify before performing any state-changing operation (saves, deletes, updates).

## Collection Settings Addon

Every collection should include a settings addon that consolidates all site settings and copy content records an admin would want to view or modify into a single admin settings location. This gives administrators one place to control the collection's behavior without hunting through multiple records or site properties.

## Addon Naming Conventions

Addon names, filenames, and class names should include a suffix that describes the type of addon. Do not use the generic word "Class" as a suffix — use a type-specific suffix instead.

| Addon Type | Suffix | Condition |
|---|---|---|
| Setting | `Setting` | `navTypeId` is Setting (3) |
| Page Widget | `PageWidget` | `content=yes` and/or `template=yes` |
| Remote Method | `Remote` | `remoteMethod=yes` |
| Task | `Task` | `processInterval` is set |
| Event | `Event` | The addon's GUID matches the `addonId` element of an Addon Event Catcher record |
| General Purpose | `Addon` | None of the above conditions apply |

For example, an addon that manages site configuration should be named `SiteConfigSetting` with a class `SiteConfigSetting`, not `SiteConfigClass`. A page widget that displays a hero banner should be `HeroBannerPageWidget`, a remote method that returns search results should be `SearchResultsRemote`, and a background task that sends digest emails should be `DigestEmailTask`.

## Summary

| Method Type | Catch Behavior |
|---|---|
| Addon `Execute` methods | Report error, return user-friendly error string |
| Non-addon methods (default) | Report error, rethrow |
| Non-critical elements in critical workflows (logging, reporting) | Report error, swallow exception |
