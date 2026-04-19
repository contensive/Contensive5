# Server Diagnostics Monitoring Setup

This guide explains how to set up automated server diagnostics monitoring for Contensive sites using the Server Diagnostics addon.

## Overview

The Server Diagnostics monitoring system consists of three components:

1. **Server Diagnostics Addon** - Performs multiple server health checks
2. **Windows Task Scheduler** - Runs the checks daily with elevated permissions
3. **Server Diagnostic Monitor** - Displays the diagnostic status at `/status` endpoint

## Checks Performed

The Server Diagnostics addon performs the following checks:

### 1. Windows Updates Check
- Uses Windows Update Agent COM API to check for pending updates
- Requires administrator privileges
- Reports count and titles of pending updates

### 2. Domain Bindings Check
- Verifies all configured domains in config.json are registered in IIS
- Verifies all database domains match config.json domains
- Ensures proper web server configuration

## Architecture

```
┌─────────────────────────────────────────────────┐
│ Windows Task Scheduler (daily, as admin)       │
│  cc.exe -a appName --execute                    │
│         "Server Diagnostics"                    │
└─────────────┬───────────────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────────────┐
│ ServerDiagnosticsAddon                          │
│  - Checks for pending Windows updates           │
│  - Validates domain bindings                    │
│  - Saves to site property:                      │
│    "ServerDiagnosticsStatus" (JSON)             │
└─────────────┬───────────────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────────────┐
│ /status endpoint (Server Diagnostic)            │
│  - Reads "ServerDiagnosticsStatus"              │
│  - Displays check results and status            │
│  - Returns ERROR if critical issues found       │
└─────────────────────────────────────────────────┘
```

## Security Design

The Server Diagnostics addon requires **administrator privileges** to access the Windows Update Agent COM API. Rather than running the entire Contensive TaskService with elevated permissions, this solution:

- Keeps TaskService running as LocalService (minimal privileges)
- Uses Windows Task Scheduler to run the checks with elevated permissions
- Isolates privileged operations to a single, auditable scheduled task
- Follows the principle of least privilege

## Setup Instructions

### 1. Install/Update Contensive

Ensure your Contensive installation includes the Server Diagnostics addon (available in version 5.x.x and later).

To verify the addon is installed:
```bash
cc.exe -a yourAppName --execute "Server Diagnostics"
```

If you see an error about the addon not being found, rebuild and deploy Contensive.

### 2. Create Windows Scheduled Task

You can create the scheduled task using either PowerShell (recommended) or Task Scheduler GUI.

#### Option A: PowerShell (Recommended)

Run this PowerShell script **as Administrator**:

```powershell
# Configuration
$appName = "YourContensiveAppName"  # Replace with your app name
$ccExePath = "C:\Program Files\Contensive\cc.exe"  # Adjust path as needed
$taskName = "Contensive Server Diagnostics - $appName"

# Create the scheduled task action
$action = New-ScheduledTaskAction `
    -Execute $ccExePath `
    -Argument "-a `"$appName`" --execute `"Server Diagnostics`""

# Create the trigger (daily at 2:00 AM)
$trigger = New-ScheduledTaskTrigger -Daily -At 2:00AM

# Create the principal (run as SYSTEM with highest privileges)
$principal = New-ScheduledTaskPrincipal `
    -UserId "SYSTEM" `
    -LogonType ServiceAccount `
    -RunLevel Highest

# Create the settings
$settings = New-ScheduledTaskSettingsSet `
    -AllowStartIfOnBatteries `
    -DontStopIfGoingOnBatteries `
    -StartWhenAvailable `
    -RunOnlyIfNetworkAvailable `
    -ExecutionTimeLimit (New-TimeSpan -Minutes 10)

# Register the scheduled task
Register-ScheduledTask `
    -TaskName $taskName `
    -Action $action `
    -Trigger $trigger `
    -Principal $principal `
    -Settings $settings `
    -Description "Daily server diagnostics checks (Windows updates, domain bindings) for $appName"

Write-Host "Scheduled task '$taskName' created successfully!" -ForegroundColor Green
```

#### Option B: Task Scheduler GUI

1. Open **Task Scheduler** (`taskschd.msc`)
2. Click **Create Task** (not "Create Basic Task")
3. **General Tab**:
   - Name: `Contensive Server Diagnostics - YourAppName`
   - Description: `Daily server diagnostics checks`
   - Run whether user is logged on or not: ✓
   - Run with highest privileges: ✓
   - Configure for: Windows 10/Windows Server 2016 (or appropriate version)

4. **Triggers Tab**:
   - Click **New**
   - Begin the task: On a schedule
   - Settings: Daily
   - Start time: 2:00 AM (or preferred time)
   - Recur every: 1 days
   - Enabled: ✓

5. **Actions Tab**:
   - Click **New**
   - Action: Start a program
   - Program/script: `C:\Program Files\Contensive\cc.exe`
   - Add arguments: `-a "YourAppName" --execute "Server Diagnostics"`

6. **Conditions Tab**:
   - Start only if the following network connection is available: Any connection ✓

7. **Settings Tab**:
   - Allow task to be run on demand: ✓
   - If the task fails, restart every: 15 minutes
   - Attempt to restart up to: 3 times
   - Stop the task if it runs longer than: 10 minutes

8. Click **OK** and enter administrator credentials when prompted

### 3. Test the Scheduled Task

Run the task manually to verify it works:

```powershell
# PowerShell
Start-ScheduledTask -TaskName "Contensive Server Diagnostics - YourAppName"

# Wait a few seconds, then check the result
Get-ScheduledTaskInfo -TaskName "Contensive Server Diagnostics - YourAppName"
```

Or in Task Scheduler GUI:
1. Find the task in the task list
2. Right-click → **Run**
3. Check **Last Run Result** (should be 0x0 for success)

### 4. Verify the Status

Check that the status was recorded:

1. Navigate to your site's status endpoint:
   ```
   https://yoursite.com/status
   ```

2. Look for the diagnostic status in the output:
   ```
   ok, all domain bindings valid
   ok, no Windows updates pending (last checked: 2026-04-17 02:00)
   ```
   or
   ```
   warning, 5 Windows update(s) pending (last checked: 2026-04-17 02:00)
   ```

You can also check the site property directly in the database:
```sql
SELECT name, FieldValue
FROM ccSetup
WHERE name = 'ServerDiagnosticsStatus'
AND active > 0
```

## Site Property Data Structure

The `ServerDiagnosticsStatus` site property contains a JSON object:

```json
{
  "lastCheckDate": "2026-04-17T02:00:15.1234567",
  "windowsUpdateCount": 3,
  "windowsUpdateTitles": [
    "Security Update for Windows (KB5012345)",
    "Cumulative Update for .NET Framework",
    "Update for Windows Defender"
  ],
  "windowsUpdateCheckSuccessful": true,
  "windowsUpdateErrorMessage": "",
  "domainBindingsValid": true,
  "domainBindingsErrorMessage": "",
  "allChecksSuccessful": true
}
```

### Fields

- **lastCheckDate** (DateTime): When the checks were last performed
- **windowsUpdateCount** (int): Number of pending Windows updates
- **windowsUpdateTitles** (string[]): List of update titles (max 20)
- **windowsUpdateCheckSuccessful** (bool): Whether the Windows update check completed successfully
- **windowsUpdateErrorMessage** (string): Error details if Windows update check failed
- **domainBindingsValid** (bool): Whether all domain bindings are valid
- **domainBindingsErrorMessage** (string): Error details if domain bindings are invalid
- **allChecksSuccessful** (bool): Whether all checks passed successfully

## Status Monitor Behavior

The `/status` endpoint evaluates the server diagnostics status as follows:

### Domain Bindings

| Condition | Status | Message |
|-----------|--------|---------|
| All bindings valid | OK | `ok, all domain bindings valid` |
| Invalid binding | ERROR | `ERROR, [specific error message from validation]` |

### Windows Updates

| Condition | Status | Message |
|-----------|--------|---------|
| No updates pending | OK | `ok, no Windows updates pending (last checked: ...)` |
| Updates pending, checked < 48 hours ago | WARNING | `warning, N Windows update(s) pending (last checked: ...)` |
| Updates pending, checked > 48 hours ago | ERROR | `ERROR, N Windows update(s) pending (last checked X hours ago).` |
| Check failed | WARNING | `warning, Windows update check failed: ...` |
| Status not available | ERROR | `ERROR, Server diagnostics status not available. Verify scheduled task or run command line.` |

## Troubleshooting

### Task runs but status not updated

1. Check Task Scheduler history:
   - Task Scheduler → View → **Show All Running Tasks**
   - Enable history if disabled: Actions → **Enable All Tasks History**

2. Check Contensive logs:
   ```
   C:\ProgramData\Contensive\AppName\Logs\
   ```

3. Manually run the command to see the output:
   ```cmd
   cd "C:\Program Files\Contensive"
   cc.exe -a "YourAppName" --execute "Server Diagnostics"
   ```

### "Access Denied" Error

The task must run with elevated privileges. Verify:
- Task is configured to **Run with highest privileges**
- Task is running as SYSTEM or an administrator account

### Domain Bindings Check Fails

Common issues:
- Domain not registered in IIS bindings
- Domain in config.json but not in database (or vice versa)
- Application name mismatch

Check IIS bindings:
```powershell
Import-Module WebAdministration
Get-WebBinding
```

### "Windows Update Agent not available"

The Windows Update service may be disabled or the COM API unavailable:
1. Check if Windows Update service is running:
   ```powershell
   Get-Service -Name wuauserv
   ```

2. Start the service if stopped:
   ```powershell
   Start-Service -Name wuauserv
   ```

### Task runs successfully but shows 0 updates when updates are available

Some updates may be hidden or excluded by the search criteria. The addon searches for:
- Not installed (`IsInstalled=0`)
- Software type (`Type='Software'`)
- Not hidden (`IsHidden=0`)

To include hidden updates, modify the search string in ServerDiagnosticsAddon.cs.

## Manual Execution

You can manually run the diagnostics at any time (requires administrator command prompt):

```cmd
cc.exe -a "YourAppName" --execute "Server Diagnostics"
```

Expected output:
```
Server Diagnostics completed (last checked: 2026-04-17 14:30:00)
OK: All domain bindings valid
OK: No Windows updates pending
```

Or with issues:
```
Server Diagnostics completed (last checked: 2026-04-17 14:30:00)
ERROR: ERROR, for application [MyApp], the domain [example.com] configured in config.json is not a registered binding in the web server.
WARNING: 3 Windows update(s) pending
```

## Multi-Application Servers

If you have multiple Contensive applications on the same server, create a separate scheduled task for each application. Each app maintains its own `ServerDiagnosticsStatus` site property.

Example:
```
- Contensive Server Diagnostics - App1
- Contensive Server Diagnostics - App2
- Contensive Server Diagnostics - App3
```

## Maintenance

- **Review diagnostic status regularly** through your monitoring system
- **Fix issues promptly** when errors are reported
- **Update Windows** when updates are available to clear warnings
- **Verify the task runs successfully** (check Last Run Result in Task Scheduler)
- **Adjust the schedule** if needed (daily check is recommended)

## Security Considerations

- The scheduled task runs with SYSTEM privileges (full access to the machine)
- The task only executes a specific Contensive addon - it does not accept user input
- Ensure `cc.exe` and addon files have appropriate ACLs to prevent tampering
- Audit scheduled task changes via Windows Event Log (Event ID 4698, 4699, 4702)

## Related Documentation

- [Server Diagnostic Pattern](../patterns/diagnostic-addon-patter.md)
- [Process Addon Pattern](../patterns/process-addon-pattern.md)
- [Security Best Practices](../patterns/security-best-practices.md)
