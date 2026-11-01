# Request Input Validation & Attack Pattern Detection Plan

## Purpose

Add defense-in-depth protections at the request-loading level to detect and block common attack patterns (SQL injection, XSS, path traversal) before they reach addon code. This complements the existing abuse detection system and protects against attacks even when addons use safe ORM patterns.

---

## Problem Context

### Recent Attack

The link tracking endpoint was probed with a SQL injection attack vector:
```
(select(0)from(select(sleep(15)))v)/*''+(select(0)from(select(sleep(15)))v)+''"+(select(0)from(select(sleep(15)))v)+"*/
```

### Why This Matters

1. **The addon was already safe** — `LinkTrackingLogSubmit` uses the ORM (`DbBaseModel`) throughout, so no SQL injection risk exists
2. **Attackers don't know that** — They probe every endpoint indiscriminately
3. **Defense-in-depth principle** — Block malicious input early, before it reaches any addon code
4. **Behavior-based blocking** — Detecting attack patterns allows IP blocking via the abuse detection system
5. **Protect legacy/third-party addons** — Not all addons may follow ORM best practices

---

## Architecture — Where to Add Input Validation

### Key Decision: Validate Early, Fail Fast

Input validation should happen in `RouteController.executeRoute()` **immediately after the staging gate check** and **before any addon execution**. This ensures:

- All request paths (pages, AJAX, remote methods) are protected
- Minimal performance impact (early rejection)
- Centralized detection logic
- Integration with existing abuse detection system

### Call Stack

```
HTTP Request
    ↓
IIS Dynamic IP Restrictions (rate limiting) ← already implemented
    ↓
IIS IP Security (static deny list) ← already implemented
    ↓
RouteController.executeRoute()
    ↓
StagingGateController.processGate() ← already exists
    ↓
[NEW] SecurityInputValidator.validateRequest() ← ADD HERE
    ↓
(if validation passes)
    ↓
Addon execution (LinkTrackingLogSubmit, etc.)
    ↓
AbuseDetectionAddon (onPageEndEvent) ← already exists
```

---

## Implementation Plan

### Part 1 — Core Security Input Validator (Contensive5/Processor)

**File:** `source/Processor/Controllers/SecurityInputValidator.cs` (new file)

This controller provides static methods for detecting malicious input patterns in request data.

#### Pattern Categories to Detect

| Category | Risk | Patterns to Match | Action |
|---|---|---|---|
| **SQL Injection** | Critical | `union select`, `' or 1=1`, `--`, `;drop`, `;delete`, `;update`, `sleep(`, `waitfor delay`, `@@version`, `information_schema`, `exec(`, `xp_cmdshell` | Block + log |
| **NoSQL Injection** | High | `$where`, `$ne`, `$gt`, `{"$`, `javascript:` in JSON | Block + log |
| **XSS / Script Injection** | High | `<script`, `javascript:`, `onerror=`, `onload=`, `<iframe`, `eval(`, `document.cookie` | Block + log |
| **Path Traversal** | Critical | `../`, `..%2f`, `..%5c`, `%2e%2e`, `etc/passwd`, `windows/system32` | Block + log |
| **Command Injection** | Critical | `\|`, `&&`, `;`, backticks, `$(`, `$\{` in fields that shouldn't contain shell syntax | Block + log |
| **LDAP Injection** | Medium | `*)(`, `*\|`, `)(cn=`, `admin*` in auth-related fields | Block + log |
| **XXE / XML Injection** | Medium | `<!ENTITY`, `<!DOCTYPE`, `SYSTEM` in XML fields | Block + log |
| **Known Scanner Patterns** | High | User-Agent strings: `nikto`, `sqlmap`, `masscan`, `nuclei`, `zgrab`, `nmap` | Block + log |

#### Validation Rules

1. **Check querystring, form data, cookies, and headers** (not just form data)
2. **Case-insensitive pattern matching** (attackers use mixed case to evade detection)
3. **URL-decode before checking** (e.g., `%27` becomes `'`, `%3B` becomes `;`)
4. **Allow patterns in specific safe contexts:**
   - Admin-only requests where `cp.User.IsDeveloper` is true (developers may legitimately post code samples)
   - Fields explicitly marked as "allow code" (e.g., HTML editor content)
5. **Suspicious, not absolute** — Some patterns may be false positives (e.g., legitimate SQL in a code tutorial), but they're rare enough to warrant blocking and logging

#### Sample Implementation (Pseudocode)

```csharp
public static class SecurityInputValidator {

    /// <summary>
    /// Validates all request input for malicious patterns. Returns null if safe,
    /// or an error response string if malicious input detected.
    /// </summary>
    public static string validateRequest(CoreController core) {
        // -- skip validation for authenticated developers (allow code samples in admin tools)
        if (core.session.user.developer) { return null; }

        // -- check all input sources
        var suspiciousPatterns = new List<SuspiciousPatternMatch>();

        // querystring
        foreach (var kvp in core.webServer.httpContext.Request.QueryString) {
            var matches = detectMaliciousPatterns(kvp.Key, kvp.Value);
            if (matches.Any()) suspiciousPatterns.AddRange(matches);
        }

        // form data
        foreach (var kvp in core.webServer.requestForm) {
            var matches = detectMaliciousPatterns(kvp.Key, kvp.Value);
            if (matches.Any()) suspiciousPatterns.AddRange(matches);
        }

        // cookies (check values only, not names)
        foreach (var kvp in core.webServer.requestCookies) {
            var matches = detectMaliciousPatterns(kvp.Key, kvp.Value.value);
            if (matches.Any()) suspiciousPatterns.AddRange(matches);
        }

        // user-agent header (check for scanner signatures)
        string userAgent = core.webServer.requestBrowser;
        var uaMatches = detectScannerUserAgent(userAgent);
        if (uaMatches.Any()) suspiciousPatterns.AddRange(uaMatches);

        // -- if malicious patterns found, log and block
        if (suspiciousPatterns.Any()) {
            return handleMaliciousInput(core, suspiciousPatterns);
        }

        return null; // validation passed
    }

    private static List<SuspiciousPatternMatch> detectMaliciousPatterns(string fieldName, string fieldValue) {
        var matches = new List<SuspiciousPatternMatch>();
        if (string.IsNullOrEmpty(fieldValue)) return matches;

        // URL-decode to catch encoded attacks
        string decodedValue = System.Web.HttpUtility.UrlDecode(fieldValue);
        string lowerValue = decodedValue.ToLowerInvariant();

        // SQL injection patterns
        string[] sqlPatterns = {
            "union select", "' or 1=1", " or 1=1", "' or '1'='1", "\" or \"1\"=\"1",
            "';--", "\";--", "/*", "*/", "xp_cmdshell", "exec(", "execute(",
            "sleep(", "waitfor delay", "benchmark(", "pg_sleep(", "@@version",
            "information_schema", ";drop ", ";delete ", ";update ", ";insert "
        };
        foreach (var pattern in sqlPatterns) {
            if (lowerValue.Contains(pattern)) {
                matches.Add(new SuspiciousPatternMatch {
                    category = "SQL Injection",
                    pattern = pattern,
                    fieldName = fieldName,
                    fieldValue = fieldValue.Substring(0, Math.Min(fieldValue.Length, 100))
                });
            }
        }

        // Path traversal patterns
        string[] pathPatterns = {
            "../", "..\\", "..%2f", "..%5c", "%2e%2e", "etc/passwd", "windows/system32",
            "boot.ini", "win.ini"
        };
        foreach (var pattern in pathPatterns) {
            if (lowerValue.Contains(pattern)) {
                matches.Add(new SuspiciousPatternMatch {
                    category = "Path Traversal",
                    pattern = pattern,
                    fieldName = fieldName,
                    fieldValue = fieldValue.Substring(0, Math.Min(fieldValue.Length, 100))
                });
            }
        }

        // XSS / Script injection patterns
        string[] xssPatterns = {
            "<script", "javascript:", "onerror=", "onload=", "onmouseover=",
            "<iframe", "eval(", "document.cookie", "document.write"
        };
        foreach (var pattern in xssPatterns) {
            if (lowerValue.Contains(pattern)) {
                matches.Add(new SuspiciousPatternMatch {
                    category = "XSS / Script Injection",
                    pattern = pattern,
                    fieldName = fieldName,
                    fieldValue = fieldValue.Substring(0, Math.Min(fieldValue.Length, 100))
                });
            }
        }

        // Command injection patterns
        string[] cmdPatterns = {
            "| ", "|| ", "&& ", "; ", "$(", "${", "`"
        };
        // Only flag if field name suggests it shouldn't contain shell syntax
        bool isShellSafeField = fieldName.ToLowerInvariant().Contains("code")
                             || fieldName.ToLowerInvariant().Contains("script")
                             || fieldName.ToLowerInvariant().Contains("command");
        if (!isShellSafeField) {
            foreach (var pattern in cmdPatterns) {
                if (lowerValue.Contains(pattern)) {
                    matches.Add(new SuspiciousPatternMatch {
                        category = "Command Injection",
                        pattern = pattern,
                        fieldName = fieldName,
                        fieldValue = fieldValue.Substring(0, Math.Min(fieldValue.Length, 100))
                    });
                }
            }
        }

        return matches;
    }

    private static List<SuspiciousPatternMatch> detectScannerUserAgent(string userAgent) {
        var matches = new List<SuspiciousPatternMatch>();
        if (string.IsNullOrEmpty(userAgent)) return matches;

        string lowerUA = userAgent.ToLowerInvariant();
        string[] scannerSignatures = {
            "nikto", "sqlmap", "masscan", "nuclei", "zgrab", "nmap", "acunetix",
            "nessus", "openvas", "metasploit", "burpsuite", "havij", "pangolin"
        };

        foreach (var signature in scannerSignatures) {
            if (lowerUA.Contains(signature)) {
                matches.Add(new SuspiciousPatternMatch {
                    category = "Scanner Signature",
                    pattern = signature,
                    fieldName = "User-Agent",
                    fieldValue = userAgent.Substring(0, Math.Min(userAgent.Length, 100))
                });
            }
        }

        return matches;
    }

    private static string handleMaliciousInput(CoreController core, List<SuspiciousPatternMatch> matches) {
        // -- log the attack
        string remoteIP = core.webServer.requestRemoteIP;
        string requestUrl = core.webServer.requestUrl;
        string logMessage = $"Malicious input detected from IP {remoteIP} on URL {requestUrl}. " +
                          $"Patterns: {string.Join(", ", matches.Select(m => $"{m.category}:{m.pattern} in {m.fieldName}"))}";
        LogController.logWarn(core, logMessage);

        // -- increment suspicious count in abuse detection (if aoSecurity is installed)
        // This integrates with the existing IP blocking system
        try {
            // Trigger the abuse detection logic by setting a flag in visit properties
            core.visitProperty.setProperty("SecurityInputValidator_Blocked", true);
            core.visitProperty.setProperty("SecurityInputValidator_Reason", matches.First().category);
        } catch {
            // aoSecurity may not be installed, ignore
        }

        // -- send generic 403 response (don't reveal what we detected)
        core.webServer.setResponseStatus(WebServerController.httpResponseStatus.Forbidden);
        return "<html><body><h1>403 Forbidden</h1><p>Your request was blocked for security reasons.</p></body></html>";
    }

    private class SuspiciousPatternMatch {
        public string category { get; set; }
        public string pattern { get; set; }
        public string fieldName { get; set; }
        public string fieldValue { get; set; }
    }
}
```

---

### Part 2 — Integration into RouteController

**File:** `source/Processor/Controllers/RouteController.cs`

Add validation immediately after the staging gate check, around line 150:

```csharp
// -- staging gate: block unauthenticated access when passphrase is set
{
    string stagingGateResult = StagingGateController.processGate(core, requestedRoute);
    if (!string.IsNullOrEmpty(stagingGateResult)) {
        return stagingGateResult;
    }
}
//
// -- [NEW] security input validation: detect and block malicious patterns
{
    string securityValidationResult = SecurityInputValidator.validateRequest(core);
    if (!string.IsNullOrEmpty(securityValidationResult)) {
        return securityValidationResult;
    }
}
//
// -- continue with route execution...
```

---

### Part 3 — Enhance Abuse Detection Addon (aoSecurity)

**File:** `aoSecurity/Addons/AbuseDetectionAddon.cs` (existing file)

Add a check for the `SecurityInputValidator_Blocked` visit property flag. If set, treat it as a "suspicious request" and increment the suspicious count toward the auto-block threshold.

```csharp
// Early in AbuseDetectionAddon.Execute():
if (cp.Visit.GetBoolean("SecurityInputValidator_Blocked")) {
    string reason = cp.Visit.GetText("SecurityInputValidator_Reason");
    // Increment suspicious count in IP Request Log
    // Consider immediate block if multiple blocked requests within short window
    // Log to Security Events table
    cp.Utils.AppendLog($"Security input validator blocked request from {cp.Request.RemoteIP}: {reason}");

    // Clear the flags so they don't trigger again on next request
    cp.Visit.SetProperty("SecurityInputValidator_Blocked", false);
    cp.Visit.SetProperty("SecurityInputValidator_Reason", "");
}
```

---

### Part 4 — Configurable Settings (aoSecurity)

Add site properties to control input validation behavior:

```xml
<Tab Name="Input Validation" heading="Input Validation Settings"
     description="Configure request input validation and attack pattern detection">
    <SiteProperty Caption="Input Validation Enabled" Name="SecurityInputValidator_Enabled"
        Type="boolean" Description="Enable or disable input validation at the request level.">true</SiteProperty>
    <SiteProperty Caption="Block on SQL Injection" Name="SecurityInputValidator_BlockSQL"
        Type="boolean" Description="Block requests containing SQL injection patterns.">true</SiteProperty>
    <SiteProperty Caption="Block on XSS" Name="SecurityInputValidator_BlockXSS"
        Type="boolean" Description="Block requests containing XSS patterns.">true</SiteProperty>
    <SiteProperty Caption="Block on Path Traversal" Name="SecurityInputValidator_BlockPathTraversal"
        Type="boolean" Description="Block requests containing path traversal patterns.">true</SiteProperty>
    <SiteProperty Caption="Block Scanner User-Agents" Name="SecurityInputValidator_BlockScanners"
        Type="boolean" Description="Block requests from known vulnerability scanners.">true</SiteProperty>
    <SiteProperty Caption="Skip Validation for Developers" Name="SecurityInputValidator_SkipDevelopers"
        Type="boolean" Description="Allow developers to bypass input validation (for posting code samples, etc.).">true</SiteProperty>
</Tab>
```

---

## Testing Plan

### Test 1 — SQL Injection Detection

**Request:**
```
POST /linktrackingsubmit
buttonText=(select(0)from(select(sleep(15)))v)
```

**Expected:**
- Request blocked with 403
- Log entry created with pattern details
- IP flagged in abuse detection
- No addon code executed

### Test 2 — Path Traversal Detection

**Request:**
```
GET /page?file=../../../etc/passwd
```

**Expected:**
- Request blocked with 403
- Log entry created
- IP flagged

### Test 3 — XSS Detection

**Request:**
```
POST /contactus
message=<script>alert('xss')</script>
```

**Expected:**
- Request blocked with 403
- Log entry created
- IP flagged

### Test 4 — Scanner User-Agent Detection

**Request:**
```
GET /
User-Agent: sqlmap/1.5.12
```

**Expected:**
- Request blocked with 403
- Log entry created
- IP flagged and likely auto-blocked immediately

### Test 5 — Developer Bypass

**Request:** Same as Test 1, but authenticated as a developer

**Expected:**
- Request passes validation
- Addon executes normally
- No blocking (developers can legitimately post code)

---

## Performance Considerations

### Overhead

- **Pattern matching:** ~1-2ms per request (regex-free string matching)
- **Early rejection:** Malicious requests never reach addon code, actually *improves* performance under attack
- **Caching:** Consider caching clean request hashes for repeat legitimate traffic (optional optimization)

### Optimization

- Use `string.Contains()` instead of regex (faster for simple patterns)
- Check cheapest patterns first (scanner user-agents are fast to check)
- Skip validation entirely for static assets (`.css`, `.js`, `.jpg`) — handle in IIS, not in app code

---

## Monitoring & Reporting

Add a new report in the Security Portal (aoSecurity):

**Report:** "Blocked Attack Attempts (Last 7 Days)"

**Query:** Security Events table filtered by `SecurityInputValidator_Blocked` flag

**Columns:**
- Date/Time
- IP Address
- Attack Category (SQL Injection, XSS, etc.)
- Blocked Pattern
- Field Name
- Request URL
- User-Agent

This report helps admins:
- Identify active attack campaigns
- Tune detection rules (reduce false positives)
- Understand which endpoints are being targeted

---

## Integration with Existing Plans

This plan complements the existing security implementations:

| Layer | System | Purpose |
|---|---|---|
| 1. IIS DIPR | security-iis-dynamic-ip-restrictions-setup.md | Rate-limit at IIS level, no code involved |
| 2. IIS IP Security | security-abuse-detection-ip-blocking-plan.md | Static deny list from abuse detection DB |
| 3. **Input Validation** | **This plan (NEW)** | **Detect attack patterns in request data** |
| 4. ORM Protection | Database Models Pattern | Parameterized queries prevent SQL injection |
| 5. Spam Prevention | security-spam-prevention-best-practices.md | Honeypot + rate limiting for forms |
| 6. Abuse Detection | security-abuse-detection-ip-blocking-plan.md | Pattern-based IP blocking and logging |

Together, these layers provide **defense-in-depth** against web application attacks.

---

## Implementation Order

### Phase 1 — Core Validator
1. Create `SecurityInputValidator.cs` in `source/Processor/Controllers/`
2. Implement pattern detection methods (SQL, XSS, path traversal, scanners)
3. Add unit tests for each pattern category

### Phase 2 — Route Integration
4. Add validation call in `RouteController.executeRoute()` after staging gate
5. Test with manual attack payloads

### Phase 3 — Abuse Detection Integration
6. Enhance `AbuseDetectionAddon` to check for blocked input flags
7. Increment suspicious counts and trigger auto-blocks

### Phase 4 — Admin UI & Reporting
8. Add "Input Validation" tab to Security Settings in aoSecurity.xml
9. Create "Blocked Attack Attempts" report in Security Portal
10. Add security event logging for blocked attempts

### Phase 5 — Testing & Tuning
11. Test with real vulnerability scanners (Nikto, SQLMap)
12. Check for false positives on legitimate traffic
13. Tune patterns and thresholds based on logs

---

## Open Questions

1. **False Positive Handling** — How should admins whitelist specific patterns for legitimate use cases? (e.g., a code tutorial site that posts SQL examples)
   - **Recommendation:** Add per-field exemptions in site properties, e.g., `SecurityInputValidator_ExemptFields=code,sample,tutorial`

2. **Performance Monitoring** — Should we add telemetry to track validation overhead?
   - **Recommendation:** Log timing metrics in debug mode, add Performance counter for validation time

3. **Pattern Updates** — How should attack patterns be updated without code deployments?
   - **Recommendation:** Store patterns in a CDef table, allow admins to add/edit patterns via admin UI

4. **Automatic Pattern Learning** — Should the system learn new attack patterns from blocked requests?
   - **Recommendation:** Future enhancement — ML-based anomaly detection, but start with static pattern matching

---

## Key Files

### Core (Contensive5)

| File | Purpose |
|---|---|
| `source/Processor/Controllers/SecurityInputValidator.cs` | New validator controller (create) |
| `source/Processor/Controllers/RouteController.cs` | Add validation call after staging gate (modify) |

### aoSecurity Collection

| File | Purpose |
|---|---|
| `server/aoSecurityAddon/Addons/AbuseDetectionAddon.cs` | Check for blocked input flags (modify) |
| `collections/aoSecurity/aoSecurity.xml` | Add Input Validation settings tab (modify) |

---

## Summary

This plan adds a critical **defense-in-depth layer** that:

1. ✅ Detects common attack patterns (SQL injection, XSS, path traversal, scanners)
2. ✅ Blocks attacks **before** they reach addon code
3. ✅ Integrates with existing abuse detection and IP blocking systems
4. ✅ Provides monitoring and reporting for security teams
5. ✅ Has minimal performance impact (<2ms per request)
6. ✅ Is configurable per-site via admin UI

The LinkTrackingLogSubmit addon was already safe due to ORM usage, but this plan ensures **all addons** (including legacy or third-party code) are protected.
