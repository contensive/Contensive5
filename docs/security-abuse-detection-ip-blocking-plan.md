# Abuse Detection & IP Blocking Plan

## Problem

Users are abusing the webservice by rapidly requesting endpoints to scan for vulnerabilities (path traversal, SQL injection probes, common scanner paths, etc.). The solution needs to:

1. Detect abusive request patterns in Contensive (suspicious URL patterns + rate limiting)
2. Block detected IPs at the IIS level so assets, scripts, and all resources are blocked — not just Contensive-rendered pages
3. Provide an admin UI for monitoring and managing blocks

---

## Prerequisites — Already Implemented

### IIS Dynamic IP Restrictions (DIPR)

See [security-iis-dynamic-ip-restrictions-setup.md](security-iis-dynamic-ip-restrictions-setup.md) for setup details.

DIPR handles blunt-force rate-based blocking at the IIS level with no application code. It is configured per-site in IIS and blocks IPs that exceed request-rate or concurrent-connection thresholds. This is the first line of defense and is already in place.

### aoSecurity Collection

The [aoSecurity](https://github.com/contensive/aoSecurity) addon collection provides a **Security Portal** in the admin area with sections for Data, Reports, Tools, and Settings. It already includes:

- Security Events table (`aoSecurityEvents`) for logging security-related events
- Security Settings addon with authentication and password policy configuration
- Portal navigation structure with Data, Reports, Tools, and Settings sections

The abuse detection features in this plan extend that collection.

---

## Architecture Overview — Core vs Addon Split

The key architectural decision is: **what must live in the core (Contensive5/Processor) vs what can be an addon (aoSecurity)?**

### Guiding Principle

If the core Processor code does not directly read/write a database table, that table belongs in the addon collection. Only the minimum enforcement mechanism needs to be in the core. Everything else — detection logic, admin UI, reporting, data tables — goes in aoSecurity for faster iteration.

### What Goes Where

| Component | Location | Rationale |
|---|---|---|
| IP Block List table + model | **aoSecurity** | Core does not query this table directly; the scheduled addon reads it and writes to IIS config |
| IP Request Log table + model | **aoSecurity** | Only used by the detection addon, not by core |
| Abuse Detection addon (onPageStartEvent) | **aoSecurity** | Detection logic, pattern matching, thresholds — all addon-level concerns |
| IP Block IIS Sync addon (scheduled) | **aoSecurity** | Reads the block list table and calls the core sync method |
| `WebServerController.syncIpBlocksToIIS()` | **Core (Processor)** | Requires `Microsoft.Web.Administration` (already referenced in Processor) and must run from the privileged TaskService process. Accepts a list of IP addresses — no dependency on addon tables. |
| Admin Report UI | **aoSecurity** | Security Portal feature — block list viewer, manual block form, activity reports |
| Security Events logging | **aoSecurity** | Already exists in aoSecurity collection |
| Configurable thresholds (site properties) | **aoSecurity** | Settings addon already handles security site properties |

### Updated Architecture Diagram

```
HTTP Request
    |
    v
IIS Dynamic IP Restrictions (DIPR)          <-- already implemented
    |  rate-limit + concurrent connection blocking
    |  no app pool involved
    |
    v (passes through for non-blocked IPs)
IIS IP Security (applicationHost.config)     <-- populated by sync addon
    |  static deny list of IPs from IP Block List table
    |  blocks assets, scripts, everything
    |
    v (passes through for non-blocked IPs)
aoSecurity: Abuse Detection Addon            <-- aoSecurity collection
    |  onPageStartEvent
    |  suspicious URL pattern matching
    |  rate-limit check (application-level)
    |  writes new blocks to IP Block List table
    |  logs events to Security Events table
    |
    v (every 5 min, runs in TaskService)
aoSecurity: IP Block IIS Sync Addon          <-- aoSecurity collection
    |  reads IP Block List table
    |  calls core WebServerController.syncIpBlocksToIIS()
    |  purges expired rows
    |
    v
Core: WebServerController.syncIpBlocksToIIS() <-- Contensive5 Processor
    |  receives list of IPs to block
    |  writes to applicationHost.config via ServerManager
    |  runs with admin privileges in TaskService
```

---

## Part 1 — Core Changes (Contensive5/Processor)

The core changes are minimal. Only one new method is needed.

### 1.1 New Method: `WebServerController.syncIpBlocksToIIS()`

**File:** `source/Processor/Controllers/WebServerController.cs`

This method accepts a simple list of IP address strings and syncs them to the IIS ipSecurity deny list. It has **no dependency** on addon-specific models or tables — the caller (the aoSecurity scheduled addon) is responsible for querying the block list and passing the IPs.

```csharp
/// <summary>
/// Sync a list of blocked IP addresses to the IIS ipSecurity deny list
/// for the specified site. Clears existing deny entries and replaces
/// them with the provided list. Requires admin privileges (TaskService).
/// </summary>
public static void syncIpBlocksToIIS(CoreController core, string siteName, List<string> blockedIpAddresses) {
    try {
        using var serverManager = new ServerManager();
        var config = serverManager.GetApplicationHostConfiguration();
        var ipSecuritySection = config.GetSection(
            "system.webServer/security/ipSecurity", siteName);
        var ipSecurityCollection = ipSecuritySection.GetCollection();
        ipSecurityCollection.Clear();
        ipSecuritySection["allowUnlisted"] = true;
        foreach (var ipAddress in blockedIpAddresses) {
            if (string.IsNullOrWhiteSpace(ipAddress)) continue;
            var addElement = ipSecurityCollection.CreateElement("add");
            addElement["ipAddress"] = ipAddress.Trim();
            addElement["allowed"] = false;
            ipSecurityCollection.Add(addElement);
        }
        serverManager.CommitChanges();
    } catch (Exception ex) {
        core.cpParent.Site.ErrorReport(ex);
    }
}
```

**Key design decisions:**

- Accepts `List<string>` instead of querying a model — keeps core decoupled from addon tables
- Always uses site-level config (`GetSection(..., siteName)`) — never server-level
- Clears and replaces the full deny list each sync — simple, idempotent, no stale entries
- `Microsoft.Web.Administration` is already referenced in `Processor.csproj`
- Must run from TaskService (admin privileges); the app pool worker (`w3wp.exe`) cannot write `applicationHost.config`

### 1.2 No Core Database Tables Needed

The core Processor does not need any new database tables. The `IP Block List` and `IP Request Log` tables are only accessed by the aoSecurity addon code, so they belong in the aoSecurity collection XML.

---

## Part 2 — aoSecurity Collection Changes

All detection logic, database tables, admin UI, and reporting live in the aoSecurity collection for faster iteration.

### 2.1 Collection XML — New CDefs

Add to `collections/aoSecurity/aoSecurity.xml`. GUIDs must be generated via `powershell -Command "[guid]::NewGuid().ToString('B').ToUpper()"`.

#### IP Block List table

Persists blocked IPs with reason, duration, and auto-expiry.

```xml
<CDef Name="IP Block List" ContentTableName="aoIpBlockList" AdminOnly="true"
      DeveloperOnly="false" Active="true" AllowAdd="true" AllowDelete="true"
      AllowContentTracking="false" AllowCalendarEvents="false" AllowTopicRules="false"
      IsBaseContent="false" Guid="{GENERATE-NEW-GUID}">
    <Field Name="ipAddress" FieldType="Text" EditSortPriority="100"
           Guid="{GENERATE-NEW-GUID}" Authorable="true" Active="true">
        <HelpDefault><![CDATA[The blocked IP address.]]></HelpDefault>
    </Field>
    <Field Name="reason" FieldType="Text" EditSortPriority="200"
           Guid="{GENERATE-NEW-GUID}" Authorable="true" Active="true">
        <HelpDefault><![CDATA[Why this IP was blocked (e.g., rate-limit, suspicious-pattern, manual).]]></HelpDefault>
    </Field>
    <Field Name="blockExpires" FieldType="Date" EditSortPriority="300"
           Guid="{GENERATE-NEW-GUID}" Authorable="true" Active="true">
        <HelpDefault><![CDATA[When this block expires. Expired blocks are purged by the sync addon.]]></HelpDefault>
    </Field>
    <Field Name="requestCount" FieldType="Integer" EditSortPriority="400"
           Guid="{GENERATE-NEW-GUID}" Authorable="true" Active="true">
        <HelpDefault><![CDATA[Number of requests that triggered this block.]]></HelpDefault>
    </Field>
    <Field Name="autoBlocked" FieldType="Boolean" EditSortPriority="500"
           Guid="{GENERATE-NEW-GUID}" Authorable="true" Active="true">
        <HelpDefault><![CDATA[True if this block was created automatically by abuse detection. False if manually added by an admin.]]></HelpDefault>
    </Field>
</CDef>
```

#### IP Request Log table

Tracks rolling request counts per IP within time windows. Rows are short-lived (auto-purged after 24h).

```xml
<CDef Name="IP Request Log" ContentTableName="aoIpRequestLog" AdminOnly="true"
      DeveloperOnly="false" Active="true" AllowAdd="true" AllowDelete="true"
      AllowContentTracking="false" AllowCalendarEvents="false" AllowTopicRules="false"
      IsBaseContent="false" Guid="{GENERATE-NEW-GUID}">
    <Field Name="ipAddress" FieldType="Text" EditSortPriority="100"
           Guid="{GENERATE-NEW-GUID}" Authorable="true" Active="true">
        <HelpDefault><![CDATA[The IP address being tracked.]]></HelpDefault>
    </Field>
    <Field Name="windowStart" FieldType="Date" EditSortPriority="200"
           Guid="{GENERATE-NEW-GUID}" Authorable="true" Active="true">
        <HelpDefault><![CDATA[Start of the time window for this request count.]]></HelpDefault>
    </Field>
    <Field Name="requestCount" FieldType="Integer" EditSortPriority="300"
           Guid="{GENERATE-NEW-GUID}" Authorable="true" Active="true">
        <HelpDefault><![CDATA[Total requests from this IP in this time window.]]></HelpDefault>
    </Field>
    <Field Name="suspiciousCount" FieldType="Integer" EditSortPriority="400"
           Guid="{GENERATE-NEW-GUID}" Authorable="true" Active="true">
        <HelpDefault><![CDATA[Number of requests matching suspicious URL patterns in this window.]]></HelpDefault>
    </Field>
    <Field Name="lastRequest" FieldType="Date" EditSortPriority="500"
           Guid="{GENERATE-NEW-GUID}" Authorable="true" Active="true">
        <HelpDefault><![CDATA[Timestamp of the most recent request in this window.]]></HelpDefault>
    </Field>
</CDef>
```

#### SQL Indexes

```xml
<SQLIndex TableName="aoIpBlockList" IndexName="idx_aoIpBlockList_ipAddress" FieldNameList="ipAddress" />
<SQLIndex TableName="aoIpBlockList" IndexName="idx_aoIpBlockList_blockExpires" FieldNameList="blockExpires" />
<SQLIndex TableName="aoIpRequestLog" IndexName="idx_aoIpRequestLog_ipAddress" FieldNameList="ipAddress" />
<SQLIndex TableName="aoIpRequestLog" IndexName="idx_aoIpRequestLog_windowStart" FieldNameList="windowStart" />
```

### 2.2 C# Models (aoSecurity project)

**File:** `server/aoSecurityAddon/Models/IpBlockListModel.cs`

```csharp
public class IpBlockListModel : DbBaseModel {
    public static readonly string tableNameDefault = "aoIpBlockList";
    public static readonly string contentNameDefault = "IP Block List";
    public string ipAddress { get; set; }
    public string reason { get; set; }
    public DateTime blockExpires { get; set; }
    public int requestCount { get; set; }
    public bool autoBlocked { get; set; }
}
```

**File:** `server/aoSecurityAddon/Models/IpRequestLogModel.cs`

```csharp
public class IpRequestLogModel : DbBaseModel {
    public static readonly string tableNameDefault = "aoIpRequestLog";
    public static readonly string contentNameDefault = "IP Request Log";
    public string ipAddress { get; set; }
    public DateTime windowStart { get; set; }
    public int requestCount { get; set; }
    public int suspiciousCount { get; set; }
    public DateTime lastRequest { get; set; }
}
```

### 2.3 Abuse Detection Addon (onPageStartEvent)

**File:** `server/aoSecurityAddon/Addons/AbuseDetectionAddon.cs`

Runs on every page request. Registered with `onPageStartEvent = true`.

#### Execution Logic (cheapest checks first)

```
Execute(cp):
  1. Skip if request IP is in localhost/private range (127.x, 10.x, 192.168.x, ::1)
  2. Skip if authenticated admin user (cp.User.IsAdmin)
  3. Get ip = cp.Request.RemoteIP
  4. Check cache key "blocked:{ip}" -> if present, send 403 and return
  5. Check IP Block List table for non-expired block on this IP -> if found, set cache and send 403
  6. Increment request log for this IP in current time window
  7. Check suspicious URL patterns against cp.Request.Path and query string
  8. If requestCount > rateThreshold OR suspiciousCount > suspiciousThreshold -> auto-block
  9. Log security event to Security Events table (already exists in aoSecurity)
 10. Purge stale request log rows older than 24h (throttled: run only 1-in-100 requests)
```

#### Suspicious URL Patterns to Detect

| Category | Examples |
|---|---|
| Common scanner paths | `/.env`, `/wp-admin`, `/phpinfo`, `/.git/`, `/actuator/`, `/etc/passwd` |
| Path traversal | `../`, `..%2F`, `%2e%2e` |
| SQL injection probes | `' OR 1=1`, `UNION SELECT`, `--` in query strings |
| Script injection | `<script`, `javascript:`, `onload=` |
| Known scanner user-agents | `nikto`, `sqlmap`, `masscan`, `zgrab`, `nuclei` |

#### Configurable Thresholds (via `cp.Site.GetText()`)

These should be added to the Security Settings addon form in aoSecurity.xml under a new "Abuse Detection" tab:

| Setting Key | Default | Description |
|---|---|---|
| `AbuseDetect_WindowSeconds` | `60` | Time window for rate counting |
| `AbuseDetect_RateLimit` | `120` | Max requests per window before block |
| `AbuseDetect_SuspiciousLimit` | `5` | Max suspicious requests per window before block |
| `AbuseDetect_BlockMinutes` | `1440` | Block duration (default 24 hours) |
| `AbuseDetect_CacheTTL` | `300` | Cache TTL for block decisions (seconds) |

#### Block Response

Set HTTP 403, write a plain message, call `cp.Response.End()` to abort page rendering.

### 2.4 IP Block IIS Sync Addon (Scheduled)

**File:** `server/aoSecurityAddon/Addons/IpBlockSyncAddon.cs`

Runs every 5 minutes as a scheduled addon. Bridges the aoSecurity block list table to the core IIS sync method.

```
Execute(cp):
  1. Query IP Block List table for all non-expired blocks
  2. Build List<string> of blocked IP addresses
  3. Call cp.executeAddon() or use reflection to invoke
     WebServerController.syncIpBlocksToIIS() with the IP list
  4. Purge expired auto-blocked rows (blockExpires < now AND autoBlocked = true)
  5. Purge IP Request Log rows older than 24h
  6. Clear cache keys for IPs that were unblocked
```

**Note on calling core methods from an addon:** The addon runs in the TaskService process (which has admin privileges). The simplest approach is for the addon to write the blocked IP list to a well-known site property or file, and have a core scheduled task read it. Alternatively, the core can expose `syncIpBlocksToIIS` through the `CPBaseClass` API. The exact integration mechanism should be determined during implementation.

### 2.5 Security Portal Integration

Add these Portal Feature records to `aoSecurity.xml` under the existing Security portal:

#### Data Section — IP Block List viewer

```xml
<Record Content="Portal Features" Guid="{GENERATE-NEW-GUID}" Name="Security - Data - IP Block List">
    <field Name="Active">1</field>
    <field Name="portalId">{CBE47847-68ED-4FF0-9721-5E29500D1C8C}</field>
    <field Name="heading"><![CDATA[IP Block List]]></field>
    <field Name="parentfeatureid">{1024F866-3062-49DF-AAF5-ECB7668EA964}</field>
    <field Name="addonId"></field>
    <field Name="datacontentid">{GUID-OF-IP-BLOCK-LIST-CDEF}</field>
    <field Name="SortOrder"><![CDATA[IP Block List]]></field>
</Record>
```

#### Data Section — IP Request Log viewer

```xml
<Record Content="Portal Features" Guid="{GENERATE-NEW-GUID}" Name="Security - Data - IP Request Log">
    <field Name="Active">1</field>
    <field Name="portalId">{CBE47847-68ED-4FF0-9721-5E29500D1C8C}</field>
    <field Name="heading"><![CDATA[IP Request Log]]></field>
    <field Name="parentfeatureid">{1024F866-3062-49DF-AAF5-ECB7668EA964}</field>
    <field Name="addonId"></field>
    <field Name="datacontentid">{GUID-OF-IP-REQUEST-LOG-CDEF}</field>
    <field Name="SortOrder"><![CDATA[IP Request Log]]></field>
</Record>
```

#### Tools Section — IP Block Management addon

A custom addon under Tools that provides:

- Active blocks table (IP, reason, expires, request count, unblock button)
- Recent suspicious activity summary (top IPs by suspicious hits, last 24h)
- Manual "block IP" form
- Manual "unblock IP" button per row

```xml
<Record Content="Portal Features" Guid="{GENERATE-NEW-GUID}" Name="Security - Tools - IP Block Management">
    <field Name="Active">1</field>
    <field Name="portalId">{CBE47847-68ED-4FF0-9721-5E29500D1C8C}</field>
    <field Name="heading"><![CDATA[IP Block Management]]></field>
    <field Name="parentfeatureid">{2BF28558-7F12-426D-8E73-0A1FED219930}</field>
    <field Name="addonId">{GUID-OF-IP-BLOCK-MANAGEMENT-ADDON}</field>
    <field Name="datacontentid"></field>
    <field Name="SortOrder"><![CDATA[IP Block Management]]></field>
</Record>
```

#### Settings — Abuse Detection thresholds

Add a new "Abuse Detection" tab to the existing Security Settings addon `FormXML`:

```xml
<Tab Name="Abuse Detection" heading="Abuse Detection Settings"
     description="Configure automatic abuse detection thresholds and blocking behavior">
    <SiteProperty Caption="Detection Enabled" Name="AbuseDetect_Enabled"
        Type="boolean" Description="Enable or disable the abuse detection addon.">true</SiteProperty>
    <SiteProperty Caption="Time Window (seconds)" Name="AbuseDetect_WindowSeconds"
        Type="integer" Description="Time window for rate counting.">60</SiteProperty>
    <SiteProperty Caption="Rate Limit (requests per window)" Name="AbuseDetect_RateLimit"
        Type="integer" Description="Max requests per window before auto-block.">120</SiteProperty>
    <SiteProperty Caption="Suspicious Request Limit" Name="AbuseDetect_SuspiciousLimit"
        Type="integer" Description="Max suspicious requests per window before auto-block.">5</SiteProperty>
    <SiteProperty Caption="Block Duration (minutes)" Name="AbuseDetect_BlockMinutes"
        Type="integer" Description="How long an auto-block lasts. Default 1440 = 24 hours.">1440</SiteProperty>
    <SiteProperty Caption="Cache TTL (seconds)" Name="AbuseDetect_CacheTTL"
        Type="integer" Description="How long block decisions are cached in memory.">300</SiteProperty>
</Tab>
```

### 2.6 Addon Registration in aoSecurity.xml

```xml
<!-- Abuse Detection — runs on every page request -->
<Addon>
    <Name>Abuse Detection</Name>
    <Guid>{GENERATE-NEW-GUID}</Guid>
    <DotNetClass>aoSecurity.Addons.AbuseDetectionAddon</DotNetClass>
    <OnPageStartEvent>Yes</OnPageStartEvent>
    <Description>Detects abusive request patterns (rate limiting, suspicious URLs) and auto-blocks offending IPs.</Description>
</Addon>

<!-- IP Block IIS Sync — scheduled every 5 minutes -->
<Addon>
    <Name>IP Block IIS Sync</Name>
    <Guid>{GENERATE-NEW-GUID}</Guid>
    <DotNetClass>aoSecurity.Addons.IpBlockSyncAddon</DotNetClass>
    <ProcessInterval>5</ProcessInterval>
    <Description>Syncs the IP Block List table to IIS ipSecurity deny rules. Purges expired blocks and stale request logs.</Description>
</Addon>

<!-- IP Block Management — admin tool -->
<Addon>
    <Name>IP Block Management</Name>
    <Guid>{GENERATE-NEW-GUID}</Guid>
    <DotNetClass>aoSecurity.Addons.IpBlockManagementAddon</DotNetClass>
    <Admin>Yes</Admin>
    <Description>Admin tool for viewing active IP blocks, unblocking IPs, and manually blocking IPs.</Description>
</Addon>
```

---

## Implementation Order

### Phase 1 — Core (Contensive5)

1. Add `syncIpBlocksToIIS(CoreController core, string siteName, List<string> blockedIpAddresses)` to `source/Processor/Controllers/WebServerController.cs`

That is the only core change required.

### Phase 2 — aoSecurity Collection XML

2. Add `IP Block List` CDef to `collections/aoSecurity/aoSecurity.xml`
3. Add `IP Request Log` CDef to `collections/aoSecurity/aoSecurity.xml`
4. Add SQL indexes for both tables
5. Add Portal Feature records (Data viewers, Tools addon)
6. Add Abuse Detection tab to Security Settings FormXML
7. Register all three addons (Abuse Detection, IIS Sync, IP Block Management)

### Phase 3 — aoSecurity C# Code

8. Create `IpBlockListModel.cs` in `server/aoSecurityAddon/Models/`
9. Create `IpRequestLogModel.cs` in `server/aoSecurityAddon/Models/`
10. Create `AbuseDetectionAddon.cs` in `server/aoSecurityAddon/Addons/`
11. Create `IpBlockSyncAddon.cs` in `server/aoSecurityAddon/Addons/`
12. Create `IpBlockManagementAddon.cs` in `server/aoSecurityAddon/Addons/`

### Phase 4 — Testing

13. Test abuse detection with Nikto or manual rapid requests against a dev site
14. Verify IIS sync writes correct deny entries to `applicationHost.config`
15. Verify auto-expiry purges blocks after the configured duration
16. Verify admin UI shows blocks, allows manual block/unblock

---

## Integration Point: Addon Calling Core Method

The main design challenge is how the aoSecurity `IpBlockSyncAddon` calls the core `WebServerController.syncIpBlocksToIIS()` method.

**Option A — Expose via CPBaseClass API (preferred)**

Add a method like `cp.Site.SyncIpBlocksToIIS(List<string> ips)` to the public API. Clean, versioned, no reflection needed. Requires a core API addition but keeps the contract explicit.

**Option B — File-based handoff**

The addon writes a JSON file of blocked IPs to a well-known path (e.g., `privateFiles/ipBlockList.json`). A core scheduled task reads the file and calls `syncIpBlocksToIIS()`. Avoids API changes but adds filesystem coupling.

**Option C — Site property handoff**

The addon writes a comma-separated IP list to a site property. A core scheduled task reads it and syncs. Simple but limited by property size.

**Recommendation:** Option A gives the cleanest integration. The API method can be a simple pass-through that calls `WebServerController.syncIpBlocksToIIS()`.

---

## Open Questions

1. **Integration mechanism** — Which option (A/B/C above) for the addon-to-core sync call? Option A is recommended.
2. **Block response** — Should blocked IPs get a custom branded 403 page, or plain text?
3. **Whitelist management** — Should the admin UI support IP whitelists (always-allow) in addition to block lists?
4. **Geo-blocking** — Future consideration: block by country/region using GeoIP lookup?
5. **Rate limit scope** — Should rate limits be per-site or global across all sites on the server?

---

## Key Files Reference

### Core (Contensive5)

| File | Purpose |
|---|---|
| `source/Processor/Controllers/WebServerController.cs` | Add `syncIpBlocksToIIS()` here |

### aoSecurity Collection

| File | Purpose |
|---|---|
| `collections/aoSecurity/aoSecurity.xml` | Collection XML — add CDefs, indexes, addon registrations, portal features |
| `server/aoSecurityAddon/Models/IpBlockListModel.cs` | IP Block List model (create new) |
| `server/aoSecurityAddon/Models/IpRequestLogModel.cs` | IP Request Log model (create new) |
| `server/aoSecurityAddon/Models/SecurityEventModel.cs` | Security Events model (already exists) |
| `server/aoSecurityAddon/Addons/AbuseDetectionAddon.cs` | Abuse detection addon (create new) |
| `server/aoSecurityAddon/Addons/IpBlockSyncAddon.cs` | IIS sync scheduled addon (create new) |
| `server/aoSecurityAddon/Addons/IpBlockManagementAddon.cs` | Admin management UI addon (create new) |

### Documentation

| File | Purpose |
|---|---|
| `docs/security-iis-dynamic-ip-restrictions-setup.md` | IIS DIPR setup guide (already exists) |
| `docs/security-abuse-detection-ip-blocking-plan.md` | This plan document |
