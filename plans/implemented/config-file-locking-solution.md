# Config.json File Locking Solution

## Problem

After upgrade, multiple `UnauthorizedAccessException` errors occur when trying to write to `D:\Contensive\config.json`:

```
System.UnauthorizedAccessException: Access to the path 'D:\Contensive\config.json' is denied.
   at System.IO.File.WriteAllText(String path, String contents)
   at Contensive.Processor.Controllers.FileController.saveFile_TextBinary(...)
   at Contensive.Processor.Models.Domain.ServerConfigModel.save(CoreController core)
```

### Root Cause

1. **Multiple concurrent processes** writing to the same `config.json` file simultaneously:
   - Multiple IIS worker processes
   - Multiple web applications on the same server
   - CLI commands running during web request processing

2. **No file-level synchronization** - `File.WriteAllText()` opens the file with exclusive access, causing contention

3. **Inadequate exception handling** - The retry logic at [FileController.cs:323](c:\Git\Contensive5\source\Processor\Controllers\FileController.cs#L323) only catches `IOException`, not `UnauthorizedAccessException`

4. **Frequent writes during startup** - The code path shows `CoreController.get_programFiles` writes to config.json when `programFilesPath` is blank, which can happen on multiple threads during app initialization

## Recommended Solutions

### Solution 1: Add File Locking with Mutex (Recommended)

Implement a **named system-wide mutex** to synchronize config.json access across all processes on the server. This follows the pattern already used in [BotDetectionService.cs:41](c:\Git\Contensive5\source\Processor\Controllers\BotDetectionService.cs#L41) which uses `ReaderWriterLockSlim` for thread safety.

**Advantages:**
- Works across multiple processes (IIS workers, CLI, services)
- Minimal code changes
- Proven pattern (similar to BotDetectionService)

**Implementation locations:**

1. **ServerConfigModel.cs** - Add static mutex for config file access
2. **FileController.saveFile_TextBinary** - Extend exception handling to catch `UnauthorizedAccessException`

### Solution 2: Move to Database-Backed Configuration

Store server configuration in the database instead of `config.json`. Keep only minimal bootstrap settings (database connection info, AWS region) in the file.

**Advantages:**
- Database provides ACID transactions
- Natural fit for multi-server environments
- Reduces file I/O contention

**Disadvantages:**
- Requires bootstrap database before config can be loaded (circular dependency)
- More complex migration path
- Doesn't help for the bootstrap config file itself

### Solution 3: Use AWS Secrets Manager (Already Partially Implemented)

The codebase already has [ServerConfigModel.useSecretManager](c:\Git\Contensive5\source\Processor\Models\Domain\ServerConfigModel.cs#L156) support. This moves most configuration to AWS Secrets Manager, leaving only minimal bootstrap data in `config.json`.

**Advantages:**
- Dramatically reduces writes to local `config.json`
- Better security for sensitive data
- Reduces file contention since local file becomes read-mostly

**Disadvantages:**
- Requires AWS infrastructure
- Bootstrap file still needs locking
- Not applicable for on-premises deployments

## Detailed Implementation: Solution 1 (Mutex-Based Locking)

### Step 1: Add Mutex to ServerConfigModel

```csharp
// In ServerConfigModel.cs
public class ServerConfigModel : ServerConfigBaseModel {
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    // Named mutex for cross-process synchronization of config.json access
    private static readonly string ConfigFileMutexName = "Global\\ContensiveConfigFileMutex";
    private const int MutexTimeoutMs = 5000; // 5 second timeout

    // ... existing code ...

    public int save(CoreController core) {
        Mutex configMutex = null;
        try {
            // Acquire cross-process mutex
            configMutex = new Mutex(false, ConfigFileMutexName);
            if (!configMutex.WaitOne(MutexTimeoutMs)) {
                logger.Warn($"{core.logCommonMessage},ServerConfigModel.save timed out waiting for config file mutex");
                throw new TimeoutException($"Timed out waiting for exclusive access to config.json after {MutexTimeoutMs}ms");
            }

            if (useSecretManager) {
                // ... existing secret manager logic ...
            } else {
                // ... existing file-based logic ...
            }
        } catch (Exception ex) {
            logger.Error(ex, $"{core.logCommonMessage}");
        } finally {
            configMutex?.ReleaseMutex();
            configMutex?.Dispose();
        }
        return 0;
    }

    public static ServerConfigModel create(CoreController core) {
        Mutex configMutex = null;
        try {
            // Acquire cross-process mutex for read
            configMutex = new Mutex(false, ConfigFileMutexName);
            if (!configMutex.WaitOne(MutexTimeoutMs)) {
                logger.Warn($"{core.logCommonMessage},ServerConfigModel.create timed out waiting for config file mutex");
                throw new TimeoutException($"Timed out waiting for exclusive access to config.json after {MutexTimeoutMs}ms");
            }

            // ... existing read logic ...
        } catch (Exception ex) {
            logger.Error($"{core.logCommonMessage}", ex, "exception in serverConfigModel.getObject");
            throw;
        } finally {
            configMutex?.ReleaseMutex();
            configMutex?.Dispose();
        }
    }
}
```

### Step 2: Fix Exception Handling in FileController

```csharp
// In FileController.cs, saveFile_TextBinary method
// Line 323: Extend catch to handle UnauthorizedAccessException
catch (IOException) when (retryCount < maxRetries) {
    retryCount++;
    logger.Trace($"{core.logCommonMessage},FileController.saveFile_TextBinary, file contention (IOException) on [{absPath}], retry {retryCount} of {maxRetries}");
    Thread.Sleep(retryDelayMs * retryCount);
} catch (UnauthorizedAccessException) when (retryCount < maxRetries) {
    retryCount++;
    logger.Trace($"{core.logCommonMessage},FileController.saveFile_TextBinary, file access denied on [{absPath}], retry {retryCount} of {maxRetries}");
    Thread.Sleep(retryDelayMs * retryCount * 2); // Wait longer for UnauthorizedAccessException
}
```

### Step 3: Prevent Unnecessary Writes During Startup

The stack trace shows the error originates from `CoreController.get_programFiles` when it tries to save a detected program files path:

```csharp
// In CoreController.cs, get_programFiles property
// Lines 486-491: Only save if we're certain this is the right path
// Add check to prevent writes during concurrent startup
if (string.IsNullOrWhiteSpace(serverConfig.programFilesPath)) {
    logger.Warn($"{logCommonMessage},serverConfig.ProgramFilesPath is blank. Current executable path does NOT includes \\git\\ so assumed program files path environment set.");

    // Probe for program files path
    string probePath = /* ... existing probe logic ... */;

    // Only update in memory, don't save to disk during startup
    // The next administrative save will persist this
    serverConfig.programFilesPath = probePath;

    // DO NOT CALL serverConfig.save(this) here - removes race condition
    _programFiles = new FileController(this, serverConfig.programFilesPath);
    return _programFiles;
}
```

## Alternative: Optimistic Concurrency

Instead of locking, implement optimistic concurrency:

1. Read current config with timestamp/version
2. Make changes in memory
3. Write only if file hasn't changed since read
4. Retry if conflict detected

This is more complex but avoids blocking.

## Testing Strategy

1. **Concurrent write test** - Simulate multiple processes writing config simultaneously
2. **Stress test** - Multiple app pools starting simultaneously
3. **CLI during web requests** - Run CLI commands (domain, enable/disable) during active web traffic
4. **Upgrade scenario** - Run upgrade on multiple apps simultaneously

## Migration Path

1. Implement Solution 1 (mutex-based locking) - low risk, immediate benefit
2. Fix exception handling in FileController - allows better retry on access denied
3. Reduce unnecessary writes (programFilesPath detection) - reduces contention frequency
4. Consider Solution 3 (Secrets Manager) for production deployments - long-term solution

## Related Files

- [ServerConfigModel.cs](c:\Git\Contensive5\source\Processor\Models\Domain\ServerConfigModel.cs) - Config model with save() and create() methods
- [FileController.cs:297-340](c:\Git\Contensive5\source\Processor\Controllers\FileController.cs#L297-L340) - saveFile_TextBinary with retry logic
- [CoreController.cs:486-491](c:\Git\Contensive5\source\Processor\Controllers\CoreController.cs#L486-L491) - programFilesPath auto-detection
- [BotDetectionService.cs:41](c:\Git\Contensive5\source\Processor\Controllers\BotDetectionService.cs#L41) - Example of ReaderWriterLockSlim for thread safety

## Impact Analysis

### Files that write to config.json

Found 20+ locations across:
- CLI commands: DeleteAppCmd, EnableCmd, DisableCmd, DomainCmd, NewAppCmd, UpgradeCmd, etc.
- Build process: BuildController.upgrade
- Runtime: CoreController.get_programFiles (during startup when path is blank)

**High-risk scenario:** During upgrade or new app creation, multiple processes detect blank programFilesPath and all try to write simultaneously.

## Priority

**HIGH** - This is causing production errors after upgrades. The mutex solution is low-risk and can be implemented immediately.
