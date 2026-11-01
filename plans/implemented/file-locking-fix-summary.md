# File Locking Fix - Final Summary

## Issues Identified and Fixed

### Issue 1: Warning displayed during `cc --configure`
**Problem:** The info message about blank programFilesPath was logged as WARN level, appearing as an error.

**Fix:** Changed log level from `Warn` to `Info` in [CoreController.cs:490](c:\Git\Contensive5\source\Processor\Controllers\CoreController.cs#L490)

**Result:** The message still appears (as it should), but as informational rather than a warning.

---

### Issue 2: Configure command exits immediately on Enter
**Problem:** The configure command checked for `defaultaspxsite.zip` and exited if not found. This file is no longer required as it now installs with the site.

**Fix:** Removed the obsolete file check from [ConfigureCmd.cs:33-38](c:\Git\Contensive5\source\Cli\Views\ConfigureCmd.cs#L33-L38)

**Result:** Configure command now proceeds through all prompts normally.

---

### Issue 3: UnauthorizedAccessException still occurring (Critical Bugs Found)

**Problem:** Despite adding mutex locking, the errors continued. Investigation revealed TWO critical bugs in the mutex implementation:

#### Bug 3a: Mutex not properly released
**Location:** ServerConfigModel.cs `save()` and `create()` methods

**Problem:**
```csharp
// BEFORE (BUGGY CODE)
configMutex = new Mutex(false, ConfigFileMutexName);
if (!configMutex.WaitOne(MutexTimeoutMs)) {
    throw new TimeoutException(...);  // Throws exception
}
// ... do work ...
} finally {
    configMutex?.ReleaseMutex();  // BUG: Tries to release mutex we don't own!
    configMutex?.Dispose();
}
```

**Why this is a bug:** If `WaitOne()` returns `false` (timeout), we throw an exception but the `finally` block still tries to call `ReleaseMutex()` on a mutex we never acquired. This throws `ApplicationException: Object synchronization method was called from an unsynchronized block of code.`

**Fix:**
```csharp
// AFTER (FIXED CODE)
bool mutexAcquired = false;
configMutex = new Mutex(false, ConfigFileMutexName);
mutexAcquired = configMutex.WaitOne(MutexTimeoutMs);
if (!mutexAcquired) {
    throw new TimeoutException(...);
}
// ... do work ...
} finally {
    if (mutexAcquired) {  // Only release if we acquired it
        configMutex?.ReleaseMutex();
    }
    configMutex?.Dispose();
}
```

#### Bug 3b: Exceptions swallowed in save() method
**Location:** ServerConfigModel.cs `save()` method line 341-343

**Problem:**
```csharp
// BEFORE (BUGGY CODE)
} catch (Exception ex) {
    logger.Error(ex, $"{core.logCommonMessage}");
    // BUG: Exception is logged but NOT rethrown!
}
return 0;  // Caller thinks save succeeded even if it failed
```

**Why this is a bug:** If the save fails (mutex timeout, file locked, etc.), the exception is logged but the method returns normally (0). The caller (e.g., configure command) thinks the save succeeded when it actually failed. This is why `programFilesPath` wasn't being persisted.

**Fix:**
```csharp
// AFTER (FIXED CODE)
} catch (Exception ex) {
    logger.Error(ex, $"{core.logCommonMessage}");
    throw;  // Rethrow so caller knows it failed
}
return 0;
```

---

### Issue 4: Global Mutex Permissions Error (New Issue - 2026-08-14)

**Problem:** After implementing mutex locking, new errors appeared:
```
System.UnauthorizedAccessException: Access to the path 'Global\ContensiveConfigFileMutex' is denied.
```

**Root Cause:** Global mutexes require elevated privileges (admin rights). IIS app pools and task runner processes typically run under non-admin accounts.

**Fix:** Replaced Global mutex with **file-based locking** using `FileStream` with exclusive access:
- Uses `config.lock` file with `FileShare.None` (exclusive lock)
- Works across all user accounts without special privileges
- Auto-cleanup with `FileOptions.DeleteOnClose`
- Same retry/timeout behavior as mutex (5 seconds, 100ms intervals)

**Files Modified:**
- [ServerConfigModel.cs](c:\Git\Contensive5\source\Processor\Models\Domain\ServerConfigModel.cs)
  - Replaced `ConfigFileMutexName` with `LockFileName = "config.lock"`
  - Added `AcquireConfigLock()` method using FileStream
  - Updated `create()` and `save()` methods to use file locking
  - Added `using System.IO;`

**See:** [file-locking-implementation.md](file-locking-implementation.md) for detailed implementation notes.

---

### Issue 5: Collections.xml File Contention (Discovered 2026-08-14)

**Problem:** Similar file locking errors for other files like `Collections.xml`:
```
System.IO.IOException: The process cannot access the file 'd:\inetpub\cio\private\addons\Collections.xml'
because it is being used by another process.
```

**Root Cause:** Read operations (`readFileText`, `readFileBinary`) had no retry logic. Write operations already had retry logic added earlier.

**Fix:** Added retry logic with `FileShare.ReadWrite` for read operations:
- Allows concurrent reads and writes
- Retries up to 5 times with exponential backoff (50ms, 100ms, 150ms, 200ms, 250ms)
- Handles both `IOException` and `UnauthorizedAccessException`
- Same pattern as existing write retry logic

**Files Modified:**
- [FileController.cs](c:\Git\Contensive5\source\Processor\Controllers\FileController.cs)
  - `readFileText()`: Lines 155-178 - Added retry loop with FileShare.ReadWrite
  - `readFileBinary()`: Lines 217-240 - Added retry loop with FileShare.ReadWrite

---

## Files Modified Summary

1. **[CoreController.cs](c:\Git\Contensive5\source\Processor\Controllers\CoreController.cs)**
   - Line 490: Changed `logger.Warn` to `logger.Info`
   - Line 491: Removed `serverConfig.save(this)` call

2. **[ConfigureCmd.cs](c:\Git\Contensive5\source\Cli\Views\ConfigureCmd.cs)**
   - Lines 33-38: Removed obsolete `defaultaspxsite.zip` check

3. **[FileController.cs](c:\Git\Contensive5\source\Processor\Controllers\FileController.cs)**
   - Lines 323-327: Added `UnauthorizedAccessException` handling to write retry logic
   - Lines 155-178: Added retry logic with `FileShare.ReadWrite` to `readFileText()`
   - Lines 217-240: Added retry logic with `FileShare.ReadWrite` to `readFileBinary()`

4. **[ServerConfigModel.cs](c:\Git\Contensive5\source\Processor\Models\Domain\ServerConfigModel.cs)**
   - Replaced Global mutex with file-based locking
   - Added `AcquireConfigLock()` method using FileStream
   - Fixed `save()` method to rethrow exceptions
   - Fixed `create()` method with proper lock release
   - Added `using System.IO;` and `using System.Threading;`

---

## Why the Error Was Persisting

The combination of bugs created a cascading failure:

1. **Configure ran** and tried to save `programFilesPath`
2. **Mutex bug** caused exception when trying to release mutex not owned
3. **Exception swallowing** made configure think save succeeded
4. **Config file never updated** - programFilesPath remained blank
5. **Every web request** triggered runtime detection, all trying to access config.json
6. **File contention** caused UnauthorizedAccessException errors

---

## Testing Recommendations

### Test 1: Verify Configure Works
```bash
cc --configure
# Should complete all prompts and save programFilesPath successfully
```

### Test 2: Verify programFilesPath Persisted
```bash
# Check that config.json now has programFilesPath set
cat D:\Contensive\config.json | grep programFilesPath
```

### Test 3: Verify No More Errors
1. Recycle IIS app pools
2. Generate concurrent web requests
3. Check logs - should see NO UnauthorizedAccessException errors
4. May see one INFO message about detecting path, then none

### Test 4: Stress Test
1. Delete programFilesPath from config.json
2. Start multiple app pools simultaneously
3. Generate 50+ concurrent requests
4. Verify only INFO messages (not errors) and path gets detected

---

## Expected Behavior After Fix

1. **First run after upgrade (programFilesPath blank):**
   - Each process detects path and logs INFO message
   - Uses detected path in-memory (no file write during request)
   - Works normally

2. **After running `cc --configure`:**
   - programFilesPath is persisted to config.json
   - INFO message no longer appears
   - No file locking issues

3. **Concurrent operations:**
   - Multiple processes can read config.json safely
   - Config writes (CLI commands) are serialized by mutex
   - No UnauthorizedAccessException errors

---

## Build Status
✅ **Build succeeded** - All changes compile without errors
