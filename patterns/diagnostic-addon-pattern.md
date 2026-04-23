
# Diagnostic Addon Pattern

> All patterns and API reference: [Patterns Index](https://raw.githubusercontent.com/contensive/Contensive5/refs/heads/master/patterns/index.md)

## Overview
A diagnostic addon is an addon configured to be run by the system when the /status method is executed.

The /status method is typically used to monitor a website or application.

## Return Format
A diagnostic addon must follow this format for its return value:

**Success**: Returns "ok" as the first two characters
- Example: `"ok: All checks passed"`
- Must NOT contain the word "ERROR" anywhere in the response

**Failure**: Returns "ERROR" as the first five characters
- Example: `"ERROR: Database connection failed"`
- If "ERROR" appears anywhere in the response, it must also be the first 5 characters

**Multiple Tests**: Each test can be on a new line, starting with "ok" or "ERROR"
```
ok: All ecommerce diagnostic checks passed
ok: Station Device Configuration - All 2 active Station Device(s) are properly configured
ok: Database connectivity - Connection successful
```

Or with errors:
```
ERROR: Ecommerce diagnostic checks failed
ERROR: Station Device Configuration - 1 of 2 active Station Device(s) missing configuration
ok: Database connectivity - Connection successful
```

## Architecture
A diagnostic addon is constructed the same as any addon, but in the addon record the diagnostic checkbox is checked.

If a diagnostic addon returns the first two characters "ok", the test is assumed to have passed. Any other result (or any result containing "ERROR") will flag an error to the status process and cause that method to fail.

## Example Implementation

```csharp
public class MyDiagnosticAddon : AddonBaseClass {
    public override object Execute(CPBaseClass cp) {
        try {
            var output = new StringBuilder();
            bool hasError = false;

            // Run diagnostic checks
            if (!checkDatabase(cp)) {
                hasError = true;
                output.AppendLine("ERROR: Database connectivity - Connection failed");
            } else {
                output.AppendLine("ok: Database connectivity - Connection successful");
            }

            if (!checkCache(cp)) {
                hasError = true;
                output.AppendLine("ERROR: Cache connectivity - Redis not responding");
            } else {
                output.AppendLine("ok: Cache connectivity - Redis operational");
            }

            // Return with appropriate prefix
            if (hasError) {
                return $"ERROR: System diagnostic checks failed{Environment.NewLine}{output}";
            }

            return $"ok: All system diagnostic checks passed{Environment.NewLine}{output}";

        } catch (Exception ex) {
            cp.Site.ErrorReport(ex);
            return $"ERROR: Exception during diagnostic - {ex.Message}";
        }
    }

    private bool checkDatabase(CPBaseClass cp) {
        // Check database connection
        return true;
    }

    private bool checkCache(CPBaseClass cp) {
        // Check cache connectivity
        return true;
    }
}
```

## Collection Registration

In your collection XML, set the `<Diagnostic>` element to `Yes`:

```xml
<Addon Name="System Diagnostics" Guid="{...}" Type="Tool">
    <DotNetClass><![CDATA[MyNamespace.MyDiagnosticAddon]]></DotNetClass>
    <Diagnostic>Yes</Diagnostic>
    <Category><![CDATA[System]]></Category>
    <!-- other addon properties -->
</Addon>
```

## Best Practices

1. **Keep it simple**: Diagnostic checks should be fast and focused on critical functionality
2. **No authentication required**: Diagnostic addons are called by monitoring systems, not users
3. **Strip HTML**: Return plain text, not HTML (monitoring systems expect text)
4. **Handle exceptions**: Always catch exceptions and return "ERROR: [message]"
5. **Be specific**: Include meaningful error messages that help identify the issue
6. **Test independently**: Each diagnostic check should be independent and not affect others