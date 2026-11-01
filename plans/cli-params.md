# CLI Query String Parameter Injection for --execute Command

## Current Flow Understanding

When `--execute` runs an addon:
1. [mainClass.cs:313-320](../source/Cli/mainClass.cs#L313-L320) - parses the `--execute` command and addon name
2. [ExecuteAddonCmd.cs:23-40](../source/Cli/Views/ExecuteAddonCmd.cs#L23-L40) - creates a new CPClass instance and calls `cp.executeAddon()`
3. [CPClass.cs:132-172](../source/Processor/Views/CPClass.cs#L132-L172) - executes the addon
4. Addons access parameters via `cp.doc.getText()` which reads from `core.docProperties`
5. [WebServerController.cs:77-82](../source/Processor/Controllers/WebServerController.cs#L77-L82) - normally populates `docProperties` from HTTP QueryString/Form during web requests

**The problem:** CLI execution doesn't go through WebServerController, so no querystring parameters are available.

## Recommended Command-Line Syntax

I recommend **Option 2** from the approaches below, which uses a clean, standard syntax:

### **Option 2: Dedicated Query String Flag (Recommended)**
```bash
cc -a myapp --execute "MyAddon" --params "name1=value1&name2=value2"
```

Or allowing multiple `--params` for readability:
```bash
cc -a myapp --execute "MyAddon" --params "userid=123" --params "action=update"
```

**Advantages:**
- Clear separation between addon name and parameters
- Standard query string format (`name=value&name=value`)
- Familiar to developers (same as URL querystrings)
- Extensible - could support `--form-params` later if needed
- Doesn't conflict with existing command syntax

**Alternative options considered:**

### Option 1: Inline with Execute (Less Clean)
```bash
cc -a myapp --execute "MyAddon?name1=value1&name2=value2"
```
- Pro: Mimics URL syntax
- Con: Requires parsing the addon argument, could conflict with addon names containing `?`

### Option 3: Individual Name-Value Pairs (Verbose)
```bash
cc -a myapp --execute "MyAddon" --param "name1" "value1" --param "name2" "value2"
```
- Pro: Most explicit
- Con: Very verbose for multiple parameters

## Implementation Approach

### 1. **Modify mainClass.cs**

Add parsing for `--params` flag after `--execute`:

```csharp
case "--execute": {
    string addonArg = getNextCmdArg(args, ref argPtr);
    if (isAppDisabled(cpServer, appName, "--execute")) { break; }
    //
    // -- collect optional query string parameters
    var queryParams = new Dictionary<string, string>();
    while (true) {
        string nextCmd = (argPtr < args.Length) ? args[argPtr] : "";
        if (nextCmd == "--params") {
            argPtr++; // consume --params flag
            string paramString = getNextCmdArg(args, ref argPtr);
            if (!string.IsNullOrWhiteSpace(paramString)) {
                // Parse "name=value&name2=value2" format
                string[] pairs = paramString.Split('&');
                foreach (string pair in pairs) {
                    string[] kv = pair.Split(new char[] { '=' }, 2);
                    if (kv.Length == 2) {
                        queryParams[kv[0]] = kv[1];
                    }
                }
            }
        } else {
            break; // not a --params flag, done collecting
        }
    }
    //
    // -- execute an addon
    writeCommandLine("--execute", appName);
    ExecuteAddonCmd.execute(cpServer, appName, addonArg, queryParams);
    break;
}
```

### 2. **Modify ExecuteAddonCmd.cs**

Update signature and inject parameters into docProperties:

```csharp
public static void execute(CPClass cpServer, string appName, string addonNameOrGuid, Dictionary<string, string> queryParams = null) {
    try {
        if (!cpServer.core.serverConfig.apps.ContainsKey(appName)) {
            Console.WriteLine($"The application [{appName}] was not found in this server group.");
            return;
        }
        if (string.IsNullOrWhiteSpace(addonNameOrGuid)) {
            Console.WriteLine("ERROR, execute requires a parameter for the addon you want to run");
        } else {
            Console.WriteLine($"executing addon [{addonNameOrGuid}], app [{appName}]");
            using (var cp = new CPClass(appName)) {
                //
                // -- inject query string parameters into docProperties
                if (queryParams != null) {
                    foreach (var kvp in queryParams) {
                        cp.core.docProperties.setProperty(
                            kvp.Key,
                            kvp.Value,
                            Contensive.Processor.Models.Domain.DocPropertyModel.DocPropertyTypesEnum.queryString
                        );
                    }
                }
                //
                Console.WriteLine(cp.executeAddon(addonNameOrGuid));
            }
        }
    } catch (Exception ex) {
        Console.WriteLine($"Error: [{ex}]");
    }
}
```

### 3. **Update ExecuteAddonCmd Help Text**

```csharp
internal static readonly string helpText = ""
    + Environment.NewLine
    + Environment.NewLine + "--execute addonGuid|addonName [--params \"name=value&name=value\"]"
    + Environment.NewLine + "    Executes an addon by guid or name (guid attempted first). Requires appName be set first with -a."
    + Environment.NewLine + "    Optional --params flag allows passing querystring-style parameters to the addon."
    + Environment.NewLine + "    Parameters are available via cp.doc.getText('paramName') in the addon."
    + Environment.NewLine + "    Example: cc -a myapp --execute \"MyAddon\" --params \"userid=123&action=update\""
    + "";
```

## Benefits of This Approach

1. **Type-safe** - Parameters are marked as `queryString` type in docProperties
2. **Backward compatible** - Existing `--execute` calls work unchanged
3. **Standard syntax** - Uses familiar `name=value&name=value` format
4. **Multiple parameters** - Supports multiple `--params` flags for readability
5. **Testable** - Addons can be tested from CLI with same parameters as web requests
6. **Consistent** - Addons see parameters via `cp.doc.getText()` regardless of CLI or web invocation

## Usage Examples

```bash
# Simple parameter
cc -a myapp --execute "DataExport" --params "format=csv"

# Multiple parameters
cc -a myapp --execute "DataExport" --params "format=csv&startdate=2026-01-01&enddate=2026-09-01"

# Multiple --params flags (alternative syntax)
cc -a myapp --execute "DataExport" --params "format=csv" --params "startdate=2026-01-01" --params "enddate=2026-09-01"

# Parameters with spaces (use quotes)
cc -a myapp --execute "ReportGenerator" --params "title=Monthly Report&department=Sales"
```

## Summary

This implementation provides a clean, extensible solution that follows established CLI patterns and integrates naturally with the existing Contensive architecture.
