# Plan: Block Bots from method= Authentication URLs

## Problem

Bots are hitting pages with `?method=loginbyemailotp` URLs. This wastes server resources, creates log noise, and could be used to probe for vulnerabilities. These URLs should not be indexed by search engines, and bots should be blocked before any form processing occurs.

## Changes Overview

1. **Add `noindex,nofollow` meta tags** when any `method=` query parameter is present
2. **Update default robots.txt** to disallow `method=` URLs
3. **Short-circuit bot visits** in the auth workflow before rendering login forms
4. **Add honeypot field** to the OTP email request form
5. **Add IP-based rate limiting** to OTP send requests
6. **Add aoStatus diagnostic check** to verify robots.txt blocks method= URLs

---

## 1. Set `noFollow` flag when `method=` query parameter is present

When any page is rendered with a `method=` query string parameter, set `core.doc.noFollow = true`. This causes `HtmlController.cs:3008` to emit `<meta name="robots" content="nofollow">` and `<meta name="robots" content="noindex">` in the HTML `<head>`.

### File: `source/Processor/Controllers/ContentRenderController.cs`

Find the location where the page content is about to be rendered (after route resolution, before HTML assembly). Add:

```csharp
// -- if the request includes a method= parameter, block search-engine indexing
if (!string.IsNullOrEmpty(core.docProperties.getText("method"))) {
    core.doc.noFollow = true;
}
```

This should be placed early in the request pipeline so it applies regardless of which `method=` value is used. The `noFollow` property is defined in `DocController.cs:35` and is checked by `HtmlController.cs:3008` during head tag assembly.

### Why ContentRenderController

`PageManagerController.cs:460` already sets `noFollow` from page metadata. The `method=` check should be at the same level -- during content rendering -- so it covers all method= requests site-wide, not just the OTP flow.

---

## 2. Update default robots.txt to disallow method= URLs

### File: `source/Processor/Controllers/SitePropertyController.cs`

At line 1026, the default robots.txt content is:

```csharp
result = "User-agent: *\r\nDisallow: /admin/";
```

Change to:

```csharp
result = "User-agent: *\r\nDisallow: /admin/\r\nDisallow: /*?*method=\r\n";
```

This tells well-behaved crawlers not to fetch any URL containing `method=` as a query parameter.

### Important note

This only affects new sites or sites that have not yet customized their robots.txt file. Existing sites with a saved `settings/RobotsTxtFilename.txt` file will not be affected by this default change. The aoStatus check (item 6) will catch sites that need manual updating.

---

## 3. Short-circuit bot visits in auth workflow

Detected bots should not be served login forms. The session controller already flags bots via `visit.bot` (see `SessionController.cs:399-436`). Use this flag to return empty content before any form rendering.

### File: `source/Processor/Controllers/Authentication/AuthWorkflowController.cs`

At line 193, just before the `method=` routing check, add:

```csharp
//
// -- block bots from accessing method-based authentication forms
if (core.session.visit.bot) {
    return string.Empty;
}
```

This goes immediately before:
```csharp
string requestMethod = core.docProperties.getText("method");
if (allowLoginByEmailOtp && requestMethod == "loginbyemailotp") {
```

The bot check should only gate the `method=` routing, not the entire auth workflow (which also handles CSRF-validated form submissions from real users). Place it after the form-type processing block (which handles POST submissions with CSRF tokens) but before the GET-based method routing.

---

## 4. Add honeypot field to OTP email request form

Add a hidden field that is invisible to real users but gets filled by bots. If the field has a value on submission, silently discard the request.

### File: `source/Processor/Controllers/Authentication/AuthWorkflowController.cs`

In `getLoginOtpEmailForm()` (line 281), after the existing hidden field at line 285:

```csharp
layout += HtmlController.inputHidden("Type", FormTypeLoginByEmailOtpRequest);
```

Add:

```csharp
layout += "<div style=\"position:absolute;left:-9999px\" aria-hidden=\"true\"><input type=\"text\" name=\"website\" tabindex=\"-1\" autocomplete=\"off\" value=\"\"></div>";
```

### File: `source/Processor/Addons/CustomBlocking/SubmitLoginByEmailRequest.cs`

At the top of `Execute()`, before the email validation, add:

```csharp
// -- honeypot: if the hidden "website" field has a value, silently discard (bot submission)
string honeypot = cp.Doc.GetText("website");
if (!string.IsNullOrEmpty(honeypot)) {
    return new SubmitLoginByEmailRequestResult {
        success = true,
        successMessage = "A one-time access code has been sent to your email address."
    };
}
```

Return a fake success so the bot doesn't know it was caught. No OTP email is actually sent.

---

## 5. Add IP-based rate limiting to OTP requests

The current rate limiting in `LoginByEmailOtpController.createAndSendOtp()` is per-email only. A bot can hit the endpoint with many different emails. Add a per-IP rate limit.

### File: `source/Processor/Controllers/Authentication/LoginByEmailOtpController.cs`

Add a new method and modify `createAndSendOtp` to accept the request IP:

```csharp
/// <summary>
/// Check if the requesting IP has exceeded the OTP send rate limit.
/// </summary>
public static bool isIpRateLimited(CPBaseClass cp, string requestIp) {
    int maxSendsPerIpPerHour = cp.Site.GetInteger("OtpMaxSendsPerIpPerHour", 10);
    if (maxSendsPerIpPerHour <= 0) { return false; }
    string rateSql = $"select count(*) as cnt from ccLoginByEmailOtp where requestIp={cp.Db.EncodeSQLText(requestIp)} and dateadded>dateadd(hour,-1,getdate())";
    using (var cs = cp.CSNew()) {
        if (cs.OpenSQL(rateSql)) {
            return cs.GetInteger("cnt") >= maxSendsPerIpPerHour;
        }
    }
    return false;
}
```

### File: `source/Models/Models/Db/LoginByEmailOtpModel.cs`

Add the `requestIp` field:

```csharp
public string requestIp { get; set; }
```

### File: `source/Processor/aoBase51.xml`

Add a new `<Field>` element inside the `Login By Email Otp` CDef (after the `verifyAttempts` field):

```xml
<Field Name="requestIp" Active="true" AdminOnly="0" Authorable="1" Caption="Request IP" DeveloperOnly="0" EditSortPriority="1060" FieldType="Text" HtmlContent="0" IndexColumn="99" IndexSortDirection="0" IndexSortOrder="0" IndexWidth="0" LookupContent="" NotEditable="0" Password="0" ReadOnly="0" RedirectContent="" RedirectId="" RedirectPath="" Required="0" TextBuffered="0" UniqueName="0" DefaultValue="" RSSTitle="" RSSDescription="" MemberSelectGroup="" EditGroup="" EditTab="" Scramble="0" LookupList="" ManyToManyContent="" ManyToManyRuleContent="" ManyToManyRulePrimaryField="" ManyToManyRuleSecondaryField="">
  <HelpDefault><![CDATA[IP address of the client that requested this OTP code.]]></HelpDefault>
</Field>
```

### File: `source/Processor/Controllers/Authentication/LoginByEmailOtpController.cs`

In `createAndSendOtp()`, store the IP when creating the OTP record and add the IP rate-limit check:

After the existing per-email rate limit block (~line 42), add:

```csharp
// -- rate limit: count OTP records for this IP in the last hour
string requestIp = cp.Request.RemoteIP;
if (isIpRateLimited(cp, requestIp)) {
    return new CreateOtpResult {
        success = false,
        isNewUser = false,
        rateLimited = true
    };
}
```

When saving the OTP record (~line 55), add:

```csharp
otpRecord.requestIp = requestIp;
```

### Site property

- `OtpMaxSendsPerIpPerHour` (default: 10) -- configurable per-IP hourly limit

---

## 6. Add aoStatus diagnostic check for robots.txt method= blocking

The `/status` endpoint should verify that the site's robots.txt contains a `Disallow` rule for `method=` URLs. This is an informational check (does not fail the overall status), reported in the `security` section of the JSON response.

### File: `source/CPBase/BaseModels/StatusResponseModel.cs`

Add to `StatusSecurityModel`:

```csharp
/// <summary>
/// True if the site's robots.txt contains a Disallow rule for method= query parameters.
/// False means bots may index authentication/workflow URLs.
/// </summary>
public bool? robotsBlocksMethodUrls { get; set; }
```

### File: `addons/aoStatus/server/Status/SecurityDiagnosticsController.cs`

In `GetSecurityInfo()`, add the robots.txt check:

```csharp
robotsBlocksMethodUrls = CheckRobotsTxtBlocksMethod(cp)
```

Add a new private method:

```csharp
/// <summary>
/// Check if the site's robots.txt contains a Disallow rule that covers method= URLs.
/// </summary>
private static bool CheckRobotsTxtBlocksMethod(CPBaseClass cp) {
    string robotsTxt = cp.Site.GetText("robotsTxt");
    if (string.IsNullOrEmpty(robotsTxt)) { return false; }
    // -- check for any disallow rule containing "method="
    foreach (string line in robotsTxt.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)) {
        string trimmed = line.Trim();
        if (trimmed.StartsWith("Disallow:", StringComparison.OrdinalIgnoreCase)) {
            string path = trimmed.Substring("Disallow:".Length).Trim();
            if (path.IndexOf("method=", StringComparison.OrdinalIgnoreCase) >= 0) {
                return true;
            }
        }
    }
    return false;
}
```

### File: `addons/aoStatus/server/Status/StatusRemoteMethod.cs`

In `RunSiteDiagnostics()`, add a warning (non-failing) check after the site warning alarms check (~line 257):

```csharp
// -- check robots.txt blocks method= URLs
{
    string robotsTxt = cp.Site.GetText("robotsTxt");
    bool blocksMethod = false;
    if (!string.IsNullOrEmpty(robotsTxt)) {
        foreach (string line in robotsTxt.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)) {
            string trimmed = line.Trim();
            if (trimmed.StartsWith("Disallow:", StringComparison.OrdinalIgnoreCase) && trimmed.IndexOf("method=", StringComparison.OrdinalIgnoreCase) >= 0) {
                blocksMethod = true;
                break;
            }
        }
    }
    if (!blocksMethod) {
        resultList.AppendLine("warning, robots.txt does not contain a Disallow rule for method= URLs. Add 'Disallow: /*?*method=' to prevent bots from indexing authentication pages.");
    } else {
        resultList.AppendLine("ok, robots.txt blocks method= URLs.");
    }
}
```

This is a **warning** (not an error) so it does not fail the overall status check.

---

## Implementation Order

| Step | Area | Files Changed |
|------|------|--------------|
| 1 | noindex meta tags | `ContentRenderController.cs` |
| 2 | Default robots.txt | `SitePropertyController.cs` |
| 3 | Bot short-circuit | `AuthWorkflowController.cs` |
| 4 | Honeypot field | `AuthWorkflowController.cs`, `SubmitLoginByEmailRequest.cs` |
| 5 | IP rate limiting | `aoBase51.xml`, `LoginByEmailOtpModel.cs`, `LoginByEmailOtpController.cs` |
| 6 | aoStatus check | `StatusResponseModel.cs`, `SecurityDiagnosticsController.cs`, `StatusRemoteMethod.cs` |

## New Site Properties

| Property | Default | Description |
|----------|---------|-------------|
| `OtpMaxSendsPerIpPerHour` | 10 | Maximum OTP emails per IP address per hour |

## New Database Fields

| Table | Field | Type | Description |
|-------|-------|------|-------------|
| `ccLoginByEmailOtp` | `requestIp` | Text | IP address of the requesting client |
