# Unified Task Model

## Overview

Replace the current fire-and-forget task execution with a unified task model that tracks status via datetime fields, supports download file output, and provides a public API for querying task state. The existing Download table is merged into the Task table as a future phase.

## Status Model

State is derived from datetime fields and failMessage -- no enum:

| State | dateStarted | dateCompleted | failMessage |
|-------|-------------|---------------|-------------|
| Queued | null | null | null |
| Running | set | null | null |
| Completed (success) | set | set | null |
| Completed (failed) | set | set | set |

## Phase 1: Task Status Tracking

### 1.1 Add fields to TaskModel

**File:** `source/Models/Models/Db/TaskModel.cs`

Add fields:

```csharp
/// <summary>
/// error/failure message. Null means success when dateCompleted is set.
/// </summary>
public string failMessage { get; set; }
/// <summary>
/// cdn path/filename for a downloadable result file, if applicable
/// </summary>
public string downloadPathFilename { get; set; }
/// <summary>
/// id of the user who requested the task
/// </summary>
public int requestedBy { get; set; }
```

Note: `dateStarted`, `dateCompleted`, and `timeout` already exist on the model. The existing `resultDownloadId` and `filename` fields remain for backward compatibility during the transition but will be deprecated.

### 1.2 Add fields to collection XML

**File:** `source/Processor/aoBase51.xml`

Add `<Field>` elements to the Tasks CDef for: `failMessage` (text), `downloadPathFilename` (text), `requestedBy` (integer).

### 1.3 Stop deleting task records before execution

**File:** `source/Processor/Controllers/TaskRunnerController.cs`

Current code (line 259):
```csharp
DbBaseModel.delete<TaskModel>(cp, task.id);
```

Replace with:
```csharp
task.dateStarted = cp.core.dateTimeNowMockable;
task.save(cp);
```

After addon execution completes, set:
```csharp
task.dateCompleted = cp.core.dateTimeNowMockable;
```

If the addon throws or returns an error, also set:
```csharp
task.failMessage = ex.Message; // or error string
```

If the addon produces a download result (current `resultDownloadId` path), save the file and set `task.downloadPathFilename` instead of writing to the Download table.

### 1.4 Set timeout when queuing tasks

**File:** `source/Processor/Controllers/Addon/AddonController.cs` (line ~1985)

In `executeAsProcess()`, after creating the task via `addTaskToQueue`, also set the timeout:

```csharp
// timeout cascade: addon.processTimeout > 0 ? addon.processTimeout : 1800 (30 min default)
task.timeout = addon.processTimeout > 0 ? addon.processTimeout : 1800;
```

This requires `addTaskToQueue` to return the task record (or its id) so the caller can set timeout. Alternatively, add a `timeout` parameter to `addTaskToQueue`.

### 1.5 Update addTaskToQueue to accept and store timeout

**File:** `source/Processor/Controllers/taskSchedulerController.cs`

Add an overload or parameter to `addTaskToQueue` that accepts `int timeout` and `int requestedBy`, and stores them on the task record.

```csharp
static public int addTaskToQueue(CoreController core, TaskModel.CmdDetailClass cmdDetail, bool blockDuplicates, int timeout, int requestedBy)
```

Returns the task id (int) instead of bool, to support the new `ExecuteAsProcessTask` method. The existing `bool` overloads remain for backward compatibility but internally call the new method and discard the id.

## Phase 2: Public API (CPBase)

### 2.1 Create CPTaskBaseClass

**File (new):** `source/CPBase/BaseClasses/CPTaskBaseClass.cs`

```csharp
namespace Contensive.BaseClasses {
    public abstract class CPTaskBaseClass {
        /// <summary>
        /// Get the status of a background task by its id.
        /// Returns null if the task id is not found.
        /// </summary>
        public abstract TaskStatusModel GetStatus(int taskId);
    }
}
```

### 2.2 Create TaskStatusModel

**File (new):** `source/CPBase/BaseModels/TaskStatusModel.cs`

```csharp
namespace Contensive.BaseModels {
    public class TaskStatusModel {
        /// <summary>
        /// when the task started executing. null = queued
        /// </summary>
        public DateTime? dateStarted { get; set; }
        /// <summary>
        /// when the task completed. null = not yet completed
        /// </summary>
        public DateTime? dateCompleted { get; set; }
        /// <summary>
        /// error message if the task failed. null = success (when dateCompleted is set)
        /// </summary>
        public string failMessage { get; set; }
        /// <summary>
        /// cdn path/filename for a downloadable result file, if applicable
        /// </summary>
        public string downloadPathFilename { get; set; }
        //
        // -- derived convenience properties
        //
        /// <summary>
        /// true if the task is queued and has not started
        /// </summary>
        public bool isQueued => dateStarted == null;
        /// <summary>
        /// true if the task is currently running
        /// </summary>
        public bool isRunning => dateStarted != null && dateCompleted == null;
        /// <summary>
        /// true if the task completed successfully
        /// </summary>
        public bool isCompleted => dateCompleted != null && string.IsNullOrEmpty(failMessage);
        /// <summary>
        /// true if the task completed with a failure
        /// </summary>
        public bool isFailed => dateCompleted != null && !string.IsNullOrEmpty(failMessage);
    }
}
```

### 2.3 Add Task property to CPBaseClass

**File:** `source/CPBase/BaseClasses/CPBaseClass.cs`

Add after the existing `TempFiles` property:

```csharp
/// <summary>
/// Background task management: queue tasks, check status
/// </summary>
public abstract CPTaskBaseClass Task { get; }
```

### 2.4 Create CPTaskClass implementation

**File (new):** `source/Processor/Views/CPTaskClass.cs`

Implements `CPTaskBaseClass.GetStatus(int taskId)`:
- Loads `TaskModel` by id
- Returns null if not found
- Maps fields to `TaskStatusModel`

### 2.5 Add ExecuteAsProcessTask to CPAddonBaseClass

**File:** `source/CPBase/BaseClasses/CPAddonBaseClass.cs`

Add new abstract methods (do NOT modify existing `ExecuteAsProcess` signatures):

```csharp
/// <summary>
/// Execute an addon as a background task. Returns the task id for status tracking.
/// Timeout cascade: taskTimeout > 0 uses taskTimeout, else addon.processTimeout, else 1800 seconds (30 min).
/// </summary>
public abstract int ExecuteAsProcessTask(string addonGuid, Dictionary<string, string> argumentKeyValuePairs, int taskTimeout);

/// <summary>
/// Execute an addon as a background task with default timeout. Returns the task id.
/// </summary>
public abstract int ExecuteAsProcessTask(string addonGuid, Dictionary<string, string> argumentKeyValuePairs);

/// <summary>
/// Execute an addon as a background task with no arguments and default timeout. Returns the task id.
/// </summary>
public abstract int ExecuteAsProcessTask(string addonGuid);
```

### 2.6 Implement ExecuteAsProcessTask in CPAddonClass

**File:** `source/Processor/Views/CPAddonClass.cs`

```csharp
public override int ExecuteAsProcessTask(string guid, Dictionary<string, string> keyValuePairs, int taskTimeout) {
    if (string.IsNullOrEmpty(guid)) { throw new ArgumentException("ExecuteAsProcessTask called with invalid guid"); }
    var addon = cp.core.cacheRuntime.addonCache.create(guid);
    if (addon == null) { throw new ArgumentException($"ExecuteAsProcessTask cannot find Addon for guid [{guid}]"); }
    // timeout cascade: taskTimeout > 0 ? taskTimeout : addon.processTimeout > 0 ? addon.processTimeout : 1800
    int effectiveTimeout = taskTimeout > 0 ? taskTimeout : (addon.processTimeout > 0 ? addon.processTimeout : 1800);
    return cp.core.addon.executeAsProcessTask(addon, keyValuePairs, effectiveTimeout, cp.core.session.user.id);
}
```

The no-timeout overloads pass `0` for taskTimeout.

### 2.7 Update existing ExecuteAsProcess methods

**File:** `source/Processor/Views/CPAddonClass.cs`
**File:** `source/Processor/Controllers/Addon/AddonController.cs`

Change the existing `executeAsProcess` internal method to use the same status-tracking pattern (no more delete-before-execute). The void return is preserved but internally the task record is kept and status-tracked the same way.

Add a new internal method `executeAsProcessTask` that returns the task id:

```csharp
public int executeAsProcessTask(AddonModel addon, Dictionary<string, string> arguments, int timeout, int requestedBy) {
    // same as executeAsProcess but returns task id from addTaskToQueue
}
```

## Phase 3: Housekeep Changes

### 3.1 Add task retention site property

**File:** `source/Processor/aoBase51.xml`

Add to the Tracking tab (line ~3554, before the closing `</Tab>`):

```xml
<SiteProperty Caption="Task retention (minutes)" Name="TaskRetentionMinutes" ReadOnly="0" Type="Integer" Selector="" Description="Completed and failed background tasks are deleted after this many minutes. Set to 0 to disable automatic cleanup. Default is 60.">60</SiteProperty>
```

### 3.2 Update TasksClass housekeep

**File:** `source/Processor/Addons/Housekeeping/TasksClass.cs`

Replace the current daily delete-all SQL with:

**Hourly tasks** (`executeHourlyTasks`):

1. **Clean up completed/failed tasks**: delete tasks where `dateCompleted` is set and older than `TaskRetentionMinutes` (site property, default 60). Also delete any associated download files from cdn before deleting the record.

2. **Mark orphaned running tasks as failed**: find tasks where `dateStarted` is set, `dateCompleted` is null, and `dateStarted` is older than 30 minutes. Set `dateCompleted = now` and `failMessage = "Task timed out (no completion received)"`.

**Daily tasks** (`executeDailyTasks`):

Remove the current blanket delete SQL (`delete from cctasks where (cmdRunner is not null)or(DateAdded<DATEADD(day,-1,...))`). The hourly task now handles cleanup properly.

## Phase 4: Download Table Migration

### 4.1 Update addTaskToQueue for downloads

**File:** `source/Processor/Controllers/taskSchedulerController.cs`

The existing `addTaskToQueue` overload that accepts `downloadName` and `downloadFilename` should store the download file path directly on the task record's `downloadPathFilename` field instead of creating a separate Download record. The `resultDownloadId` field is no longer populated for new tasks.

### 4.2 Update TaskRunnerController for downloads

**File:** `source/Processor/Controllers/TaskRunnerController.cs`

After addon execution, if the addon produces a result and the task has a download expectation:
- Save the result content to the path in `task.downloadPathFilename`
- Set `task.dateCompleted`
- The existing Download-writing code (lines 281-296) is removed

### 4.3 Update Download Manager to read from Task table

**File:** `source/Processor/Addons/Tools/DownloadManager.cs`

Change from querying `DownloadModel` to querying `TaskModel` where `downloadPathFilename` is not empty and `requestedBy` matches. The status display logic maps to the new datetime fields:
- `dateCompleted` null + `dateStarted` null = "Queued"
- `dateCompleted` null + `dateStarted` set = "Pending" (with spinner)
- `dateCompleted` set + `failMessage` null = download link using `downloadPathFilename`
- `dateCompleted` set + `failMessage` set = "Error: {failMessage}"

### 4.4 Deprecate DownloadModel

**File:** `source/Models/Models/Db/DownloadModel.cs`

Mark the class with `[Obsolete("Use TaskModel with downloadPathFilename instead.", false)]`. Do not delete the table yet -- existing data remains accessible.

### 4.5 Housekeep cleanup of download files

**File:** `source/Processor/Addons/Housekeeping/TasksClass.cs`

When deleting completed tasks that have a `downloadPathFilename`, delete the file from cdn files before deleting the task record.

## File Change Summary

| File | Phase | Change |
|------|-------|--------|
| `source/Models/Models/Db/TaskModel.cs` | 1 | Add failMessage, downloadPathFilename, requestedBy fields |
| `source/Processor/aoBase51.xml` | 1,3 | Add Task CDef fields; add TaskRetentionMinutes site property to Tracking tab |
| `source/Processor/Controllers/TaskRunnerController.cs` | 1,4 | Stop deleting tasks; set dateStarted/dateCompleted/failMessage; remove Download writes |
| `source/Processor/Controllers/taskSchedulerController.cs` | 1,4 | Return task id; accept timeout and requestedBy params; store downloadPathFilename |
| `source/Processor/Controllers/Addon/AddonController.cs` | 1,2 | Add executeAsProcessTask returning task id; update executeAsProcess to use same pattern |
| `source/CPBase/BaseClasses/CPTaskBaseClass.cs` | 2 | New file: abstract GetStatus method |
| `source/CPBase/BaseModels/TaskStatusModel.cs` | 2 | New file: status model with derived booleans |
| `source/CPBase/BaseClasses/CPBaseClass.cs` | 2 | Add abstract Task property |
| `source/CPBase/BaseClasses/CPAddonBaseClass.cs` | 2 | Add ExecuteAsProcessTask abstract methods |
| `source/Processor/Views/CPTaskClass.cs` | 2 | New file: implements CPTaskBaseClass |
| `source/Processor/Views/CPAddonClass.cs` | 2 | Implement ExecuteAsProcessTask; update ExecuteAsProcess |
| `source/Processor/Views/CPClass.cs` | 2 | Wire up Task property |
| `source/Processor/Addons/Housekeeping/TasksClass.cs` | 3 | Retention-based cleanup; orphan detection |
| `source/Processor/Addons/Tools/DownloadManager.cs` | 4 | Query TaskModel instead of DownloadModel |
| `source/Models/Models/Db/DownloadModel.cs` | 4 | Mark Obsolete |

## Execution Order

Phases 1 and 2 can be implemented together as they are interdependent. Phase 3 follows immediately. Phase 4 can be done separately as a follow-up since it has no external dependencies.
