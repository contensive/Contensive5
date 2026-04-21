# Site Monitor: Windows Updates Warning

## Summary

The `/status` JSON endpoint now includes a `windowsUpdates` object. The site monitor should display a warning box at the bottom of each site's monitor card when Windows updates are available.

## JSON Response Change

The `/?method=status&format=json` response now includes a `windowsUpdates` field alongside the existing `status`, `message`, `metrics`, and `diagnostics` fields.

```json
{
  "status": "ok",
  "message": "ok, all tests passed.",
  "metrics": { ... },
  "diagnostics": "...",
  "windowsUpdates": {
    "updatesAvailable": true,
    "updateCount": 3,
    "updateTitles": [
      "2026-04 Cumulative Update for Windows Server 2022",
      "2026-04 .NET 8.0 Update",
      "Security Intelligence Update for Microsoft Defender"
    ],
    "lastChecked": "2026-04-19T10:30:00",
    "checkSuccessful": true,
    "errorMessage": ""
  }
}
```

### Field Reference

| Field | Type | Description |
|-------|------|-------------|
| `windowsUpdates` | object or null | Null when server diagnostics data is unavailable |
| `windowsUpdates.updatesAvailable` | bool | `true` when one or more updates are pending |
| `windowsUpdates.updateCount` | int | Number of pending updates |
| `windowsUpdates.updateTitles` | string[] | Titles of pending updates (up to 20) |
| `windowsUpdates.lastChecked` | datetime | When the server last checked for updates |
| `windowsUpdates.checkSuccessful` | bool | Whether the Windows Update Agent query succeeded |
| `windowsUpdates.errorMessage` | string | Error message if the check failed, empty string otherwise |

## How the Monitor Should Use This Data

### Display Rule

When `windowsUpdates` is not null and `windowsUpdates.updatesAvailable` is `true`, display a yellow warning box at the bottom of the site's monitor card. This follows the same pattern as the existing "No traffic in last 5 minutes" warning.

### Warning Box Content

Display the update count and the last-checked timestamp:

```
{updateCount} Windows update(s) available
Last checked: {lastChecked formatted as local date/time}
```

For example:

```
3 Windows update(s) available
Last checked: Apr 19, 2026 10:30 AM
```

### When Not to Show the Warning

- `windowsUpdates` is `null` — server diagnostics data is not available, do nothing
- `windowsUpdates.updatesAvailable` is `false` — no updates pending, do nothing
- `windowsUpdates.checkSuccessful` is `false` — the update check itself failed; optionally show a gray/muted note saying "Windows update check unavailable" but this is not required

### Relationship to Error Status

Windows updates do **not** affect the `status` field unless updates have been pending for more than 1 week. When updates are pending less than 7 days, `status` will still be `"ok"` and the `windowsUpdates` data provides the warning independently. When updates are pending more than 7 days, the server returns `status: "error"` which the monitor already handles with the red "DIAGNOSTICS FAILED" banner.

### Alert Priority

The Windows updates warning is the lowest priority alert. When multiple alerts are active, display in this order (highest priority first):

1. Diagnostics failed (red) — `status !== "ok"`
2. Critical response time (red) — `avgResponseTime5MinMs > 1000`
3. Above baseline (yellow) — `avgResponseTime5MinMs > avgResponseTimeMs * 1.5`
4. No traffic (yellow) — `hitCount5Min === 0 && uptimeMinutes > 5`
5. Windows updates pending (yellow) — `windowsUpdates?.updatesAvailable`

### Example Implementation

```javascript
function renderWindowsUpdateWarning(data) {
    const wu = data.windowsUpdates;
    if (!wu || !wu.updatesAvailable) {
        return;
    }
    const lastChecked = new Date(wu.lastChecked).toLocaleString();
    const warningBox = document.createElement('div');
    warningBox.className = 'monitor-warning monitor-warning-yellow';
    warningBox.innerHTML = `
        <strong>${wu.updateCount} Windows update(s) available</strong>
        <br><small>Last checked: ${lastChecked}</small>
    `;
    // append to the bottom of the site's monitor card
    siteCard.appendChild(warningBox);
}
```

## Backward Compatibility

The `windowsUpdates` field is additive. Existing monitor code that does not reference this field will continue to work without changes. The field should be treated as optional — always check for null before accessing its properties.
