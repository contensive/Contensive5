# Abuse Detection & IP Blocking Addon Plan

## Problem

Users are abusing the webservice by rapidly requesting endpoints to scan for vulnerabilities (path traversal, SQL injection probes, common scanner paths, etc.). The solution needs to:

1. Detect abusive request patterns in Contensive (rate limiting + suspicious URL patterns)
2. Block detected IPs at the IIS level so assets, scripts, and all resources are blocked — not just Contensive-rendered pages

---

## Architecture Overview

```
HTTP Request
    │
    ▼
IIS IP Security (applicationHost.config)
    │  blocks assets, scripts, everything — no app pool involved
    │
    ▼ (passes through for non-blocked IPs)
Contensive onPageStartEvent Addon
    │  rate-limit check, suspicious URL patterns
    │  writes new blocks to IpBlockList table
    │
    ▼ (every 5 min)
IpBlockSyncAddon (ScheduledAddon)
    │  reads IpBlockList table
    │  writes to applicationHost.config via ServerManager
    └─ purges expired rows
```

---

## Phase 1 — Collection XML (MUST BE FIRST per CLAUDE.md rule)

Add two new `<CDef>` blocks to `source/Processor/aoBase51.xml`.

**Critical:** Per project rules, database tables must be added to the collection XML BEFORE adding C# model classes or any code that uses them. The collection XML is how Contensive creates database schema at install/upgrade time.

### 1a. `IP Request Log` table

Tracks rolling request counts per IP within time windows. Rows are short-lived (auto-purged after 24h).

```xml
<CDef>
  <Name>IP Request Log</Name>
  <TableName>ipRequestLog</TableName>
  <Fields>
    <Field><Name>ipAddress</Name><Type>Text</Type><Size>50</Size></Field>
    <Field><Name>windowStart</Name><Type>Date</Type></Field>
    <Field><Name>requestCount</Name><Type>Integer</Type></Field>
    <Field><Name>suspiciousCount</Name><Type>Integer</Type></Field>
    <Field><Name>lastRequest</Name><Type>Date</Type></Field>
  </Fields>
</CDef>
```

### 1b. `IP Block List` table

Persists blocked IPs with reason, duration, and auto-expiry.

```xml
<CDef>
  <Name>IP Block List</Name>
  <TableName>ipBlockList</TableName>
  <Fields>
    <Field><Name>ipAddress</Name><Type>Text</Type><Size>50</Size></Field>
    <Field><Name>reason</Name><Type>Text</Type><Size>500</Size></Field>
    <Field><Name>blockExpires</Name><Type>Date</Type></Field>
    <Field><Name>requestCount</Name><Type>Integer</Type></Field>
    <Field><Name>autoBlocked</Name><Type>Boolean</Type></Field>
  </Fields>
</CDef>
```

---

## Phase 2 — C# Models

### `IpRequestLogModel.cs`

**File:** `source/Models/Models/Db/IpRequestLogModel.cs`

```csharp
public class IpRequestLogModel : DbBaseModel {
    public static readonly string tableNameDefault = "ipRequestLog";
    public static readonly string contentNameDefault = "IP Request Log";
    public string ipAddress { get; set; }
    public DateTime windowStart { get; set; }
    public int requestCount { get; set; }
    public int suspiciousCount { get; set; }
    public DateTime lastRequest { get; set; }
}
```

### `IpBlockListModel.cs`

**File:** `source/Models/Models/Db/IpBlockListModel.cs`

```csharp
public class IpBlockListModel : DbBaseModel {
    public static readonly string tableNameDefault = "ipBlockList";
    public static readonly string contentNameDefault = "IP Block List";
    public string ipAddress { get; set; }
    public string reason { get; set; }
    public DateTime blockExpires { get; set; }
    public int requestCount { get; set; }
    public bool autoBlocked { get; set; }
}
```

---

## Phase 3 — Abuse Detection Addon

**File:** `source/Processor/Addons/AbuseDetection/AbuseDetectionAddon.cs`

This addon runs on every page via `onPageStartEvent = true`.

### Execution Logic (cheapest checks first)

```
Execute(cp):
  1. Skip if request IP is in localhost/private range (127.x, 10.x, 192.168.x, ::1)
  2. Skip if authenticated admin user
  3. Get ip = cp.Request.RemoteIP
  4. Check cache key "blocked:{ip}" → if present, send 403 and return
  5. Check IpBlockList table for non-expired block on this IP → if found, set cache and send 403
  6. Increment request log for this IP in current 1-minute time window
  7. Check suspicious URL patterns against cp.Request.Path and query string
  8. If requestCount > rateThreshold OR suspiciousCount > suspiciousThreshold → auto-block
  9. Purge stale request log rows older than 24h (throttled: run only 1-in-100 requests)
```

### Suspicious URL Patterns to Detect

| Category | Examples |
|---|---|
| Common scanner paths | `/.env`, `/wp-admin`, `/phpinfo`, `/.git/`, `/actuator/`, `/etc/passwd` |
| Path traversal | `../`, `..%2F`, `%2e%2e` |
| SQL injection probes | `' OR 1=1`, `UNION SELECT`, `--` in query strings |
| Script injection | `<script`, `javascript:`, `onload=` |
| Known scanner user-agents | `nikto`, `sqlmap`, `masscan`, `zgrab`, `nuclei` |

### Configurable Thresholds (via `cp.Site.GetText()`)

| Setting Key | Default | Description |
|---|---|---|
| `AbuseDetect_WindowSeconds` | `60` | Time window for rate counting |
| `AbuseDetect_RateLimit` | `120` | Max requests per window before block |
| `AbuseDetect_SuspiciousLimit` | `5` | Max suspicious requests per window before block |
| `AbuseDetect_BlockMinutes` | `1440` | Block duration (default 24 hours) |
| `AbuseDetect_CacheTTL` | `300` | Cache TTL for block decisions (seconds) |

### Block Response

Set HTTP 403, write a plain message, call `cp.Response.End()` to abort page rendering.

---

## Phase 4 — IIS IP Security Sync

### Why IIS-Level Blocking

Blocking at the Contensive `onPageStartEvent` level only prevents Contensive-rendered pages from serving content to blocked IPs. Static assets (images, CSS, JS, uploaded files) are served directly by IIS and bypass the addon. Adding the blocked IP to IIS IP Address and Domain Restrictions blocks everything — no request even reaches the app pool.

### How It Works Technically

- IIS IP security is stored in `applicationHost.config`
- `Microsoft.Web.Administration` v11.1.0 is **already referenced** in `Processor.csproj`
- `WebServerController.cs` already uses `ServerManager` for site/pool management — ipSecurity follows the same pattern
- Changes via `ServerManager.CommitChanges()` take effect immediately without an IIS recycle

### Why the Web Process Can't Write IIS Config

The app pool worker (`w3wp.exe`) runs under a restricted identity and cannot write to `applicationHost.config`. The sync must run from a privileged process. The Contensive TaskService (already runs with admin rights) is the right place.

### New Method: `WebServerController.syncIpBlocksToIIS()`

Add to `source/Processor/Controllers/WebServerController.cs`:

```csharp
public static void syncIpBlocksToIIS(CoreController core, string siteName) {
    try {
        var now = DateTime.Now;
        var activeBlocks = DbBaseModel.createList<IpBlockListModel>(core.cpParent,
            $"(blockExpires>{core.cpParent.Db.EncodeSQLDate(now)})");

        using var serverManager = new ServerManager();
        var config = serverManager.GetApplicationHostConfiguration();

        // Target site-level only — do NOT use server-level
        var ipSecuritySection = config.GetSection(
            "system.webServer/security/ipSecurity", siteName);

        var ipSecurityCollection = ipSecuritySection.GetCollection();
        ipSecurityCollection.Clear();

        // Default: allow all unmatched IPs, deny blocked ones explicitly
        ipSecuritySection["allowUnlisted"] = true;

        foreach (var block in activeBlocks) {
            var addElement = ipSecurityCollection.CreateElement("add");
            addElement["ipAddress"] = block.ipAddress;
            addElement["allowed"] = false;
            ipSecurityCollection.Add(addElement);
        }

        serverManager.CommitChanges();
    } catch (Exception ex) {
        core.cpParent.Site.ErrorReport(ex);
    }
}
```

**Important:** Always use `GetSection(..., siteName)` (site-level), never the server-level overload. This ensures only the Contensive site is affected.

### New Scheduled Addon: `IpBlockSyncAddon.cs`

**File:** `source/Processor/Addons/AbuseDetection/IpBlockSyncAddon.cs`

- Registered in `aoBase51.xml` as a `ScheduledAddon` running every 5 minutes
- Calls `WebServerController.syncIpBlocksToIIS()` with the site name
- Purges expired rows from `IpBlockList` (where `autoBlocked=true` and `blockExpires < now`)
- Purges `IpRequestLog` rows older than 24h
- Clears site cache keys for IPs that were unblocked (so the `onPageStartEvent` addon stops serving 403s)

---

## Phase 5 — Collection XML Addon Registration

Register both addons in `source/Processor/aoBase51.xml`:

```xml
<Addon>
  <Name>Abuse Detection</Name>
  <Class>Contensive.Processor.Addons.AbuseDetection.AbuseDetectionAddon</Class>
  <OnPageStartEvent>true</OnPageStartEvent>
</Addon>

<Addon>
  <Name>IP Block IIS Sync</Name>
  <Class>Contensive.Processor.Addons.AbuseDetection.IpBlockSyncAddon</Class>
  <ScheduledAddon>true</ScheduledAddon>
  <CronSchedule>*/5 * * * *</CronSchedule>
</Addon>
```

The `onPageStartEvent` flag on the AbuseDetection addon is a site-level setting editable in Admin → Addons, so it can be toggled on/off without a deployment.

---

## Phase 6 — Admin Report Addon (optional, implement later)

A separate addon callable from admin that renders:

- Active blocks table (IP, reason, expires, request count, unblock button)
- Recent suspicious activity log (top IPs by suspicious hits, last 24h)
- Manual "block IP" form

---

## Implementation Order

1. Add `<CDef>` entries to `source/Processor/aoBase51.xml`
2. Create `IpRequestLogModel.cs` in `source/Models/Models/Db/`
3. Create `IpBlockListModel.cs` in `source/Models/Models/Db/`
4. Create `AbuseDetectionAddon.cs` in `source/Processor/Addons/AbuseDetection/`
5. Add `syncIpBlocksToIIS()` to `source/Processor/Controllers/WebServerController.cs`
6. Create `IpBlockSyncAddon.cs` in `source/Processor/Addons/AbuseDetection/`
7. Register both addons in `source/Processor/aoBase51.xml`
8. Test with Nikto or manual rapid requests against a dev site
9. (Later) Admin report addon

---

## Open Questions

1. **Admin whitelist** — Should authenticated admin users be automatically whitelisted? (Recommended: yes)
2. **Private IP whitelist** — Should `127.x`, `10.x`, `192.168.x`, `::1` be whitelisted? (Recommended: yes)
3. **Block response** — Should blocked IPs get a custom branded 403 page, or plain text? (Affects what `Execute()` returns)
4. **IIS Dynamic IP Restrictions** — IIS has a built-in module for pure rate-limiting at the IIS level with no code. If the main concern is scanner speed rather than suspicious URL patterns, enabling that module could handle most rate-limiting, leaving the Contensive addon to focus only on pattern-based detection.

---

## Key Files Reference

| File | Purpose |
|---|---|
| `source/Processor/aoBase51.xml` | Collection XML — add CDefs and addon registrations here FIRST |
| `source/Models/Models/Db/` | C# model classes |
| `source/Processor/Addons/AbuseDetection/` | New addon folder (create this) |
| `source/Processor/Controllers/WebServerController.cs` | Add `syncIpBlocksToIIS()` here |
| `source/Models/Models/Db/AddonModel.cs` | Reference for addon flags (`onPageStartEvent`, etc.) |
| `source/Models/Models/Db/VisitModel.cs` | Has `remote_addr`, `bot`, `loginAttempts` fields for reference |
