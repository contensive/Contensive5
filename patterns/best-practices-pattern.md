
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

## Summary

| Method Type | Catch Behavior |
|---|---|
| Addon `Execute` methods | Report error, return user-friendly error string |
| Non-addon methods (default) | Report error, rethrow |
| Non-critical elements in critical workflows (logging, reporting) | Report error, swallow exception |
