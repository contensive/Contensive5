# Trailing Backslash Fix for programFilesPath

## Problem

The `programFilesPath` configuration value was being saved **without a trailing backslash** by `Path.GetDirectoryName()`, but the code expected paths **with trailing backslashes**.

### Example of the Issue

**Config.json had:**
```json
"programFilesPath": "C:\\Program Files\\Contensive\\Cli"
```

**Code checked for:**
```csharp
if (System.IO.File.Exists(serverConfig.programFilesPath + "Processor.dll"))
```

**This created:**
```
C:\Program Files\Contensive\CliProcessor.dll  ❌ (missing backslash separator)
```

**Instead of:**
```
C:\Program Files\Contensive\Cli\Processor.dll  ✅ (correct path)
```

### Result
- File check failed (Processor.dll not found)
- Code fell through to auto-detection
- INFO message logged on every request: "serverConfig.ProgramFilesPath is blank. Using detected path..."
- Even after running `cc --configure`, the path was saved without trailing backslash, perpetuating the problem

## Solution - Two-Part Fix

### Part 1: Write - Ensure Trailing Backslash When Setting

Modified all CLI commands that set `programFilesPath` to add trailing backslash:

1. **[ConfigureCmd.cs:367-370](c:\Git\Contensive5\source\Cli\Views\ConfigureCmd.cs#L367-L370)**
2. **[UpgradeCmd.cs:32-36](c:\Git\Contensive5\source\Cli\Views\UpgradeCmd.cs#L32-L36)**
3. **[NewAppCmd.cs:58-62](c:\Git\Contensive5\source\Cli\Views\NewAppCmd.cs#L58-L62)**
4. **[NewAppFrameworkCmd.cs:75-78](c:\Git\Contensive5\source\Cli\Views\NewAppFrameworkCmd.cs#L75-L78)**

**Pattern applied:**
```csharp
string currentPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
if (!currentPath.EndsWith("\\")) {
    currentPath += "\\";
}
core.serverConfig.programFilesPath = currentPath;
```

### Part 2: Read - Normalize Path When Reading

Modified [CoreController.cs:463-482](c:\Git\Contensive5\source\Processor\Controllers\CoreController.cs#L463-L482) to normalize the path if it's missing the trailing backslash:

**Before:**
```csharp
if (!string.IsNullOrEmpty(serverConfig.programFilesPath)) {
    if (System.IO.File.Exists(serverConfig.programFilesPath + "Processor.dll")) {
        _programFiles = new FileController(this, serverConfig.programFilesPath);
        return _programFiles;
    }
    // ... more checks ...
}
```

**After:**
```csharp
if (!string.IsNullOrEmpty(serverConfig.programFilesPath)) {
    // Normalize path to include trailing backslash
    string normalizedPath = serverConfig.programFilesPath;
    if (!normalizedPath.EndsWith("\\")) {
        normalizedPath += "\\";
        serverConfig.programFilesPath = normalizedPath;
    }
    if (System.IO.File.Exists(normalizedPath + "Processor.dll")) {
        _programFiles = new FileController(this, normalizedPath);
        return _programFiles;
    }
    // ... more checks with normalizedPath ...
}
```

## Files Modified

1. **[ConfigureCmd.cs](c:\Git\Contensive5\source\Cli\Views\ConfigureCmd.cs)** - Add trailing backslash when setting path
2. **[UpgradeCmd.cs](c:\Git\Contensive5\source\Cli\Views\UpgradeCmd.cs)** - Add trailing backslash when setting path
3. **[NewAppCmd.cs](c:\Git\Contensive5\source\Cli\Views\NewAppCmd.cs)** - Add trailing backslash when setting path
4. **[NewAppFrameworkCmd.cs](c:\Git\Contensive5\source\Cli\Views\NewAppFrameworkCmd.cs)** - Add trailing backslash when setting path
5. **[CoreController.cs](c:\Git\Contensive5\source\Processor\Controllers\CoreController.cs)** - Normalize path when reading

## Expected Behavior After Fix

### Scenario 1: Fresh Install (Run cc --configure)
1. Configure command gets current path: `C:\Program Files\Contensive\Cli`
2. Adds trailing backslash: `C:\Program Files\Contensive\Cli\`
3. Saves to config.json with backslash
4. ✅ No INFO messages on web requests

### Scenario 2: Existing Install (Path without backslash in config.json)
1. First web request reads: `C:\Program Files\Contensive\Cli` (no backslash)
2. CoreController normalizes to: `C:\Program Files\Contensive\Cli\`
3. Updates in-memory config
4. ✅ File check succeeds, no INFO message
5. Next CLI command persists the normalized path with backslash

### Scenario 3: Manual Edit (User adds path to config.json)
- If user adds path without backslash, CoreController auto-corrects it
- Next CLI command persists the correction

## Testing

### Before Fix
```bash
# Config shows path without backslash
cat D:\Contensive\config.json | grep programFilesPath
# Output: "programFilesPath": "C:\\Program Files\\Contensive\\Cli",

# Every request logs INFO message
# 2026-08-14 11:04:27.1128|INFO|...|serverConfig.ProgramFilesPath is blank. Using detected path [c:\Program Files\Contensive\Cli\]. Run 'cc --configure' to persist this setting to config.json.
```

### After Fix
```bash
# Run configure
cc --configure

# Config now has path with backslash
cat D:\Contensive\config.json | grep programFilesPath
# Output: "programFilesPath": "C:\\Program Files\\Contensive\\Cli\\",

# No more INFO messages on requests
# ✅ Clean logs
```

## Build Status
✅ **Build succeeded** - All changes compile for both .NET Framework 4.8 and .NET 9.0 targets

## Related Issues Fixed
This fix complements the file locking solution by ensuring the path is always correctly formatted, reducing the frequency of auto-detection and potential config writes.
