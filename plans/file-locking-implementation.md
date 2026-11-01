# File-Based Locking Implementation for config.json

## Problem
The system was experiencing `UnauthorizedAccessException` errors when trying to create a Global mutex for config.json synchronization:

```
System.UnauthorizedAccessException: Access to the path 'Global\ContensiveConfigFileMutex' is denied.
```

**Root Cause:** Global mutexes (using the `Global\` prefix) require elevated privileges in Windows. When IIS app pools or task runner processes run under non-admin accounts, they cannot create or access Global mutexes.

## Solution
Replaced the Global mutex with **file-based locking** using `FileStream` with exclusive access (`FileShare.None`).

## Implementation Details

### Changes Made
File: `source\Processor\Models\Domain\ServerConfigModel.cs`

1. **Replaced mutex constants:**
   ```csharp
   // OLD:
   private static readonly string ConfigFileMutexName = "Global\\ContensiveConfigFileMutex";
   private const int MutexTimeoutMs = 5000;

   // NEW:
   private static readonly string LockFileName = "config.lock";
   private const int LockTimeoutMs = 5000;
   ```

2. **Added file-based locking method:**
   ```csharp
   private static FileStream AcquireConfigLock(CoreController core) {
       string lockFilePath = core.programDataFiles.convertRelativeToLocalAbsPath(LockFileName);
       DateTime startTime = DateTime.UtcNow;

       while (true) {
           try {
               return new FileStream(
                   lockFilePath,
                   FileMode.OpenOrCreate,
                   FileAccess.ReadWrite,
                   FileShare.None,  // No sharing - exclusive lock
                   1,
                   FileOptions.DeleteOnClose  // Auto-cleanup when disposed
               );
           } catch (IOException) {
               if ((DateTime.UtcNow - startTime).TotalMilliseconds > LockTimeoutMs) {
                   throw new TimeoutException($"Timed out waiting for exclusive access to config.json after {LockTimeoutMs}ms");
               }
               Thread.Sleep(100);  // Wait 100ms before retry
           }
       }
   }
   ```

3. **Updated create() method:**
   ```csharp
   // OLD:
   Mutex configMutex = null;
   bool mutexAcquired = false;
   try {
       configMutex = new Mutex(false, ConfigFileMutexName);
       mutexAcquired = configMutex.WaitOne(MutexTimeoutMs);
       // ...
   } finally {
       if (mutexAcquired) {
           configMutex?.ReleaseMutex();
       }
       configMutex?.Dispose();
   }

   // NEW:
   FileStream lockStream = null;
   try {
       lockStream = AcquireConfigLock(core);
       // ...
   } finally {
       lockStream?.Dispose();
   }
   ```

4. **Updated save() method:** Same pattern as create()

5. **Added using directive:**
   ```csharp
   using System.IO;
   ```

## How File-Based Locking Works

1. **Exclusive Access:** When Process A opens `config.lock` with `FileShare.None`, the OS kernel prevents any other process from opening the same file until Process A closes it.

2. **Cross-User Synchronization:** Unlike Global mutexes, file-based locking works across all user accounts without special privileges:
   - IIS app pools (ApplicationPoolIdentity, NetworkService, etc.)
   - Console applications
   - Windows Services
   - Task runners

3. **Retry with Timeout:** If the lock is held by another process, the code retries every 100ms for up to 5 seconds (same behavior as the previous mutex implementation).

4. **Auto-Cleanup:** The `FileOptions.DeleteOnClose` flag ensures the lock file is automatically deleted when the stream is disposed, even if the process crashes.

## Why This Approach is Better

### Advantages
- ✅ **No special privileges required** - works in all security contexts
- ✅ **Cross-user synchronization** - works across IIS app pools and non-IIS processes
- ✅ **Built into OS kernel** - reliable and battle-tested
- ✅ **Simple and maintainable** - fewer moving parts than mutex with security descriptors
- ✅ **Auto-cleanup** - lock file deleted on process exit

### When to Use File Locking vs. Retry Logic

**Use File-Based Locking (like config.json):**
- Critical configuration files with **read-modify-write patterns**
- Files where **data integrity is critical** (losing an update would be catastrophic)
- Low-frequency updates (not every request)
- Files that control application behavior

**Use Retry Logic (like FileController.saveFile):**
- High-volume file operations
- Files where **last-write-wins** is acceptable
- Content files (HTML, images, uploads, cache files)
- Files with eventual consistency requirements

## Testing
- ✅ Builds successfully with no compilation errors
- ✅ Compatible with both .NET Framework 4.8 and .NET 9.0 targets
- ⚠️ Runtime testing needed to verify multi-process locking behavior

## Related Files
- `source\Processor\Models\Domain\ServerConfigModel.cs` - Updated with file-based locking
- `source\Processor\Controllers\FileController.cs` - Uses retry logic for general file operations (unchanged)

## Migration Notes
When this code is deployed:
1. The old Global mutex will no longer be used
2. A new `config.lock` file will be created in the programData folder during config access
3. The lock file is automatically deleted after each operation (FileOptions.DeleteOnClose)
4. No manual cleanup or migration steps required
