# IIS Dynamic IP Restrictions — Installation & Configuration

## Purpose

Block abusive users (vulnerability scanners, rapid request bots) at the IIS level before any application code runs. Blocks assets, APIs, and pages equally — no app pool involved.

Dynamic IP Restrictions (DIPR) is a Microsoft-built IIS module that automatically blocks IPs based on:
- **Request rate** — too many requests in a time window
- **Concurrent connections** — too many simultaneous connections from one IP

---

## Step 1 — Install the Module

### Option A: Via Windows Features (GUI)
1. Open **Server Manager → Manage → Add Roles and Features**
2. Navigate to: **Web Server (IIS) → Web Server → Security**
3. Check **IP and Domain Restrictions**
4. Click **Install**

### Option B: Via PowerShell (preferred for servers)
```powershell
Install-WindowsFeature -Name Web-IP-Security
```

### Option C: Via DISM
```cmd
dism /online /enable-feature /featurename:IIS-IPSecurity
```

Verify install:
```powershell
Get-WindowsFeature Web-IP-Security
```
Should show `[X] IP and Domain Restrictions`.

---

## Step 2 — Enable Dynamic Restrictions in IIS Manager

1. Open **IIS Manager**
2. Select your site (not the server node — site-level is preferred)
3. Double-click **IP Address and Domain Restrictions**
4. In the right **Actions** panel, click **Edit Dynamic Restriction Settings**

---

## Step 3 — Configure Denial Based on Request Rate

This is the primary scanner/abuse defense.

| Setting | Recommended Value | Notes |
|---|---|---|
| **Deny IP Address based on the number of requests over a period of time** | Enabled | |
| **Number of requests** | `200` | Requests allowed before blocking triggers |
| **Time period (milliseconds)** | `10000` | 10 seconds — so 200 req/10s = 20 req/sec max |

**Tuning guidance:**
- Start permissive (`500 req / 10000ms`) and tighten after watching logs
- Legitimate users rarely exceed 5–10 req/sec sustained
- Scanners like Nikto run at 50–200+ req/sec
- If you have a search index crawler hitting the site, whitelist it before tightening

---

## Step 4 — Configure Denial Based on Concurrent Connections

Catches slow-rate scanners that maintain many open connections.

| Setting | Recommended Value | Notes |
|---|---|---|
| **Deny IP Address based on the number of concurrent requests** | Enabled | |
| **Maximum number of concurrent requests** | `25` | Per IP |

---

## Step 5 — Set the Deny Action Type

In the **Actions** panel, click **Edit Feature Settings**:

| Setting | Recommended Value | Notes |
|---|---|---|
| **Deny Action Type** | `Abort` | Drops connection silently — gives scanner nothing, not even a 403 |

Options:
- `Abort` — drops connection silently (best for scanners — wastes their time)
- `Unauthorized (401)` — responds with 401
- `Forbidden (403)` — responds with 403 (better if you want to inform legitimate users)
- `Not Found (404)` — lies and says the resource doesn't exist

---

## Step 6 — Set Proxy Mode (Critical for Load Balancers)

If the server is behind a **load balancer, reverse proxy, or CDN**, the request IP seen by IIS will be the proxy IP — not the real client. This would cause DIPR to block the load balancer.

In **Edit Feature Settings**:

| Setting | Value |
|---|---|
| **Enable Proxy Mode** | Enabled (only if behind a proxy/load balancer) |

When enabled, DIPR reads the real client IP from the `X-Forwarded-For` header instead of the TCP connection IP.

**Warning:** Only enable this if you actually have a trusted proxy. Without a proxy, a malicious client can spoof any IP by forging the `X-Forwarded-For` header.

---

## Step 7 — Verify via applicationHost.config

After saving, `C:\Windows\System32\inetsrv\config\applicationHost.config` should contain:

```xml
<system.webServer>
  <security>
    <ipSecurity allowUnlisted="true">
      <!-- static manual blocks appear here -->
    </ipSecurity>
    <dynamicIpSecurity enableLoggingOnlyMode="false" denyAction="AbortRequest">
      <denyByConcurrentRequests enabled="true" maxConcurrentRequests="25" />
      <denyByRequestRate enabled="true" maxRequests="200" requestIntervalInMilliseconds="10000" />
    </dynamicIpSecurity>
  </security>
</system.webServer>
```

### PowerShell Alternative (useful for scripted deployment)

```powershell
$siteName = "YourSiteName"

# Request rate limiting
Set-WebConfigurationProperty -pspath "MACHINE/WEBROOT/APPHOST/$siteName" `
    -filter "system.webServer/security/dynamicIpSecurity/denyByRequestRate" `
    -name "enabled" -value "True"

Set-WebConfigurationProperty -pspath "MACHINE/WEBROOT/APPHOST/$siteName" `
    -filter "system.webServer/security/dynamicIpSecurity/denyByRequestRate" `
    -name "maxRequests" -value 200

Set-WebConfigurationProperty -pspath "MACHINE/WEBROOT/APPHOST/$siteName" `
    -filter "system.webServer/security/dynamicIpSecurity/denyByRequestRate" `
    -name "requestIntervalInMilliseconds" -value 10000

# Concurrent connection limiting
Set-WebConfigurationProperty -pspath "MACHINE/WEBROOT/APPHOST/$siteName" `
    -filter "system.webServer/security/dynamicIpSecurity/denyByConcurrentRequests" `
    -name "enabled" -value "True"

Set-WebConfigurationProperty -pspath "MACHINE/WEBROOT/APPHOST/$siteName" `
    -filter "system.webServer/security/dynamicIpSecurity/denyByConcurrentRequests" `
    -name "maxConcurrentRequests" -value 25

# Deny action
Set-WebConfigurationProperty -pspath "MACHINE/WEBROOT/APPHOST/$siteName" `
    -filter "system.webServer/security/dynamicIpSecurity" `
    -name "denyAction" -value "AbortRequest"
```

---

## Step 8 — Whitelist Known Safe IPs

Before going live, add static **Allow** rules for IPs that should never be blocked:

1. In **IP Address and Domain Restrictions**, click **Add Allow Entry**
2. Add:
   - Your office/VPN IP range
   - Known crawlers you want to allow (Googlebot ranges if needed)
   - Your monitoring service IPs
   - Your own server IP (if it makes internal requests)

### Via PowerShell

```powershell
Add-WebConfigurationProperty -pspath "MACHINE/WEBROOT/APPHOST/$siteName" `
    -filter "system.webServer/security/ipSecurity" `
    -name "." `
    -value @{ipAddress="203.0.113.10"; allowed="True"}
```

---

## Step 9 — Monitor Effectiveness

DIPR blocks are **not logged by default** in IIS access logs when using `Abort` mode (the connection is dropped before a response is written).

To see what is being blocked:
- Switch temporarily to `Forbidden` deny mode to see 403s appear in IIS logs
- Enable **Failed Request Tracing** for status 403
- Check **Event Viewer → Windows Logs → Application**, filter for source `IIS-W3SVC-WP`

---

## Relationship to the Contensive Abuse Detection Addon

See [abuse-detection-ip-blocking-plan.md](abuse-detection-ip-blocking-plan.md) for the full Contensive addon plan.

With DIPR in place, the two systems complement each other:

| Concern | Handled By |
|---|---|
| Raw request rate (scanner speed) | IIS DIPR — no code needed |
| Suspicious URL patterns (`/.env`, SQL probes) | Contensive `AbuseDetectionAddon` |
| Persistent block list (survives IIS recycle) | Contensive `IpBlockList` table |
| Manual admin blocks | Contensive admin UI |
| Syncing manual/pattern blocks back to IIS | Contensive `IpBlockSyncAddon` (scheduled) |

DIPR handles blunt-force rate blocking instantly at the IIS level. The Contensive addon handles smarter pattern-based detection with a persistent, manageable block list.
