# Security Features for Addon Developers

**Audience:** Contensive addon developers who need to implement security features in their addons

**Purpose:** This guide shows how to easily implement security best practices in your addons, and recommends new CPBaseClass API methods to make security features trivial to add.

---

## Current State Analysis

### What Security Plans Require from Addon Developers

#### 1. Spam Prevention (security-spam-prevention-best-practices.md)

**Current Implementation Burden:**

For **Honeypot Protection**, developers must:
1. Add HTML to their form template manually
2. Write custom server-side validation code
3. Decide on response strategy (fake success vs error)
4. Handle logging manually

```csharp
// Current approach - developer writes all this code
string honeypotValue = CP.Doc.GetText("website_url");
if (!string.IsNullOrEmpty(honeypotValue)) {
    CP.Utils.AppendLog("FormSubmit, honeypot triggered - rejecting as spam");
    var spamResponse = new SubmitResponseClass() {
        errorList = new List<string>(),
        redirectUrl = settings.redirectUrl,
        thankYouEmbedCode = CP.Utils.EncodeContentForWeb(settings.thankYouEmbedCode)
    };
    return CP.JSON.Serialize(spamResponse);
}
```

For **Rate Limiting**, developers must:
1. Choose a unique key name per form
2. Manually retrieve timestamp from `CP.Visit`
3. Parse the timestamp string
4. Calculate time difference
5. Store new timestamp after success
6. Handle error responses

```csharp
// Current approach - 10+ lines of boilerplate
string rateLimitKey = $"contactUsLastSubmit-{settings.id}";
string lastSubmitStr = CP.Visit.GetText(rateLimitKey, "");
if (!string.IsNullOrEmpty(lastSubmitStr) && DateTime.TryParse(lastSubmitStr, out DateTime lastSubmit)) {
    if ((DateTime.Now - lastSubmit).TotalSeconds < 15) {
        CP.Utils.AppendLog($"FormSubmit, rate limited - resubmit within 15s blocked");
        var rateLimitResponse = new SubmitResponseClass() {
            errorList = new List<string> { "Please wait a moment before submitting again." },
            redirectUrl = "",
            thankYouEmbedCode = ""
        };
        return CP.JSON.Serialize(rateLimitResponse);
    }
}
// ... later after success
CP.Visit.SetProperty(rateLimitKey, DateTime.Now.ToString("o"));
```

**Pain Points:**
- Too much boilerplate code
- Easy to get wrong (timestamp format, key naming, logging)
- No consistency across addons
- Developers might skip security due to complexity

---

#### 2. Request Input Validation (security-request-input-validation-plan.md)

**Current Implementation Burden:**

✅ **Good news:** This validation happens **automatically at the route level**, so addon developers get this protection **for free** without any code changes.

However, developers currently have **no way to:**
- Opt specific fields out of validation (e.g., a code editor field that legitimately contains SQL)
- Check if the current request was flagged as suspicious
- Access the validation results for custom handling

---

## Recommended CPBaseClass API Additions

### 1. CP.Security.CheckHoneypot() — Honeypot Validation Made Trivial

**Purpose:** One-line honeypot validation for forms

**Signature:**
```csharp
public abstract class CPSecurityBaseClass {
    /// <summary>
    /// Check if the honeypot field was filled (indicates bot). Returns true if spam detected.
    /// If spam is detected, the request is automatically logged and the visitor is flagged.
    /// The honeypot field name defaults to "website_url" but can be customized.
    /// </summary>
    /// <param name="honeypotFieldName">Optional custom honeypot field name (default: "website_url")</param>
    /// <returns>True if spam detected (honeypot was filled), false if legitimate user</returns>
    public abstract bool CheckHoneypot(string honeypotFieldName = "website_url");
}
```

**Usage in Addon:**
```csharp
public override object Execute(CPBaseClass cp) {
    // One line replaces 10+ lines of boilerplate
    if (cp.Security.CheckHoneypot()) {
        return JsonResponse.FakeSuccess(); // Bot thinks it succeeded
    }

    // Process legitimate submission...
}
```

**Implementation Notes:**
- Automatically logs via `CP.Utils.AppendLog()`
- Automatically sets visit property flag for abuse detection integration
- Returns false if field doesn't exist (safe default)
- No exceptions thrown, safe to call anywhere

---

### 2. CP.Security.CheckRateLimit() — Rate Limiting Made Trivial

**Purpose:** One-line rate limit check for forms

**Signature:**
```csharp
public abstract class CPSecurityBaseClass {
    /// <summary>
    /// Check if the visitor has submitted too recently. Returns true if rate limit exceeded.
    /// Uses visit-level storage tied to the specified action key.
    /// Automatically logs and flags the visitor if limit exceeded.
    /// </summary>
    /// <param name="actionKey">Unique key for this action (e.g., "contactFormSubmit", "commentPost")</param>
    /// <param name="cooldownSeconds">Seconds required between submissions (default: 15)</param>
    /// <returns>True if rate limit exceeded, false if OK to proceed</returns>
    public abstract bool CheckRateLimit(string actionKey, int cooldownSeconds = 15);

    /// <summary>
    /// Record that the visitor successfully completed an action, starting the rate limit cooldown.
    /// Call this AFTER successful form processing.
    /// </summary>
    /// <param name="actionKey">Same key used in CheckRateLimit()</param>
    public abstract void RecordAction(string actionKey);
}
```

**Usage in Addon:**
```csharp
public override object Execute(CPBaseClass cp) {
    // Check honeypot first (cheapest)
    if (cp.Security.CheckHoneypot()) {
        return JsonResponse.FakeSuccess();
    }

    // Check rate limit (one line replaces 15+ lines of boilerplate)
    if (cp.Security.CheckRateLimit("contactFormSubmit", cooldownSeconds: 15)) {
        return JsonResponse.Error("Please wait a moment before submitting again.");
    }

    // Process form...
    // Send email, save to database, etc.

    // Record successful action AFTER processing
    cp.Security.RecordAction("contactFormSubmit");

    return JsonResponse.Success();
}
```

**Implementation Notes:**
- Automatically generates unique visit property keys internally
- Handles timestamp storage and parsing
- Logs rate limit violations
- Integrates with abuse detection system
- Thread-safe, handles concurrent requests

---

### 3. CP.Security.GetHoneypotHtml() — HTML Generation Helper

**Purpose:** Generate the honeypot HTML snippet for form templates

**Signature:**
```csharp
public abstract class CPSecurityBaseClass {
    /// <summary>
    /// Generate HTML for a honeypot field. Insert this in your form template.
    /// The field is positioned off-screen and hidden from screen readers.
    /// </summary>
    /// <param name="fieldName">Field name (default: "website_url")</param>
    /// <returns>HTML string ready to insert in form</returns>
    public abstract string GetHoneypotHtml(string fieldName = "website_url");
}
```

**Usage in Addon:**
```csharp
// In your addon's GetEditorHtml or GetHtml method:
string formHtml = $@"
<form method='post' action='/contactsubmit'>
    {cp.Security.GetHoneypotHtml()}
    <input type='text' name='email' placeholder='Your Email' required>
    <textarea name='message' required></textarea>
    <button type='submit'>Send</button>
</form>
";
```

**Generated HTML:**
```html
<div style="position:absolute;left:-9999px;" aria-hidden="true">
    <input type="text" name="website_url" tabindex="-1" autocomplete="off" value="">
</div>
```

**Implementation Notes:**
- Uses best-practice accessibility attributes
- Field name must match what you pass to `CheckHoneypot()`
- No JavaScript required, works on all browsers

---

### 4. CP.Security.SanitizeInput() — Input Sanitization Helper

**Purpose:** Sanitize user input for safe display or storage

**Signature:**
```csharp
public abstract class CPSecurityBaseClass {
    /// <summary>
    /// Sanitize user input for safe display. Removes or encodes dangerous patterns.
    /// Use this when you need to display user-submitted content.
    /// </summary>
    /// <param name="userInput">Raw user input</param>
    /// <param name="mode">Sanitization mode: "display" (HTML encode), "url" (URL encode), "sql" (strip SQL keywords)</param>
    /// <returns>Sanitized string safe for the specified context</returns>
    public abstract string SanitizeInput(string userInput, string mode = "display");
}
```

**Usage in Addon:**
```csharp
// For displaying user input in HTML
string safeName = cp.Security.SanitizeInput(userEnteredName, "display");
string html = $"<p>Thank you, {safeName}!</p>";

// For embedding in URLs
string safeQuery = cp.Security.SanitizeInput(searchTerm, "url");
string redirectUrl = $"/search?q={safeQuery}";

// For extra safety when storing searchable text (strips SQL keywords just in case)
string safeBio = cp.Security.SanitizeInput(userBio, "sql");
model.biography = safeBio;
```

**Implementation Notes:**
- "display" mode: HTML-encodes `<`, `>`, `&`, `"`, `'`
- "url" mode: URL-encodes per RFC 3986
- "sql" mode: Strips common SQL injection patterns (still use ORM for queries!)
- Does **not** replace proper parameterized queries — always use DbBaseModel for database operations

---

### 5. CP.Security.IsInputSuspicious() — Attack Pattern Detection

**Purpose:** Check if a specific input value looks like an attack

**Signature:**
```csharp
public abstract class CPSecurityBaseClass {
    /// <summary>
    /// Check if user input contains suspicious attack patterns.
    /// Returns true if SQL injection, XSS, or path traversal patterns detected.
    /// Note: The core automatically validates all request data, but you can use this
    /// for additional checks on specific high-risk fields.
    /// </summary>
    /// <param name="userInput">Input to check</param>
    /// <param name="allowPatterns">Comma-separated list of pattern types to allow (e.g., "sql,xss")</param>
    /// <returns>True if suspicious patterns detected, false if clean</returns>
    public abstract bool IsInputSuspicious(string userInput, string allowPatterns = "");
}
```

**Usage in Addon:**
```csharp
// Check if a code snippet field contains suspicious patterns
string codeSnippet = cp.Doc.GetText("codeExample");

// Allow SQL patterns since this is a SQL tutorial site
if (cp.Security.IsInputSuspicious(codeSnippet, allowPatterns: "sql")) {
    return JsonResponse.Error("Your input contains suspicious patterns. Please contact support.");
}
```

**Implementation Notes:**
- Most addons won't need this — core validation covers 99% of cases
- Useful for high-risk scenarios like admin tools, code editors, or API gateways
- Pattern types: "sql", "xss", "path", "command", "nosql"
- Returns false if input is null/empty

---

### 6. CP.Security.GetRequestSecurityInfo() — Security Telemetry

**Purpose:** Get information about security checks performed on this request

**Signature:**
```csharp
public abstract class CPSecurityBaseClass {
    /// <summary>
    /// Get security information about the current request.
    /// Useful for logging, audit trails, or custom security logic.
    /// </summary>
    /// <returns>SecurityInfoClass with validation results</returns>
    public abstract SecurityInfoClass GetRequestSecurityInfo();
}

public class SecurityInfoClass {
    /// <summary>True if core input validation flagged this request as suspicious</summary>
    public bool WasFlaggedSuspicious { get; set; }

    /// <summary>Reason for flagging (e.g., "SQL Injection", "XSS", "Scanner Signature")</summary>
    public string FlagReason { get; set; }

    /// <summary>True if this IP is currently on the block list</summary>
    public bool IsBlocked { get; set; }

    /// <summary>Number of suspicious requests from this IP in the last hour</summary>
    public int SuspiciousRequestCount { get; set; }

    /// <summary>True if this request came from an authenticated developer (bypass validation)</summary>
    public bool IsDeveloperBypass { get; set; }
}
```

**Usage in Addon:**
```csharp
// Check if security systems flagged this request
var securityInfo = cp.Security.GetRequestSecurityInfo();

if (securityInfo.WasFlaggedSuspicious) {
    // Log extra details for audit
    cp.Utils.AppendLog($"Suspicious request processed: {securityInfo.FlagReason}");

    // Maybe require CAPTCHA for suspicious users
    if (securityInfo.SuspiciousRequestCount > 3) {
        return ShowCaptchaForm();
    }
}
```

---

## Implementation Priority & Effort

| Method | Priority | Effort | Impact |
|---|---|---|---|
| `CheckHoneypot()` | High | Low (1-2 hours) | Very High — eliminates most bot spam with 1 line of code |
| `CheckRateLimit()` | High | Medium (3-4 hours) | Very High — stops spam bursts and prevents abuse |
| `RecordAction()` | High | Low (30 min) | High — required for rate limiting |
| `GetHoneypotHtml()` | Medium | Low (30 min) | High — makes honeypot trivial to add |
| `SanitizeInput()` | Medium | Low (1 hour) | Medium — convenience wrapper for existing encoding methods |
| `IsInputSuspicious()` | Low | Medium (2-3 hours) | Low — most scenarios covered by core validation |
| `GetRequestSecurityInfo()` | Low | Medium (2-3 hours) | Low — advanced use cases only |

**Recommended Implementation Order:**
1. `CheckHoneypot()` + `GetHoneypotHtml()` — Biggest immediate impact
2. `CheckRateLimit()` + `RecordAction()` — Pairs with honeypot
3. `SanitizeInput()` — Simple helper, immediate value
4. `GetRequestSecurityInfo()` — Nice-to-have for advanced scenarios
5. `IsInputSuspicious()` — Only if demand arises

---

## Developer Experience Comparison

### Before: Manual Implementation

```csharp
public override object Execute(CPBaseClass cp) {
    // 25+ lines of security boilerplate
    string honeypotValue = cp.Doc.GetText("website_url");
    if (!string.IsNullOrEmpty(honeypotValue)) {
        cp.Utils.AppendLog("FormSubmit, honeypot triggered");
        return FakeSuccessResponse();
    }

    string rateLimitKey = $"contactUsLastSubmit-{formId}";
    string lastSubmitStr = cp.Visit.GetText(rateLimitKey, "");
    if (!string.IsNullOrEmpty(lastSubmitStr) && DateTime.TryParse(lastSubmitStr, out DateTime lastSubmit)) {
        if ((DateTime.Now - lastSubmit).TotalSeconds < 15) {
            cp.Utils.AppendLog($"FormSubmit, rate limited");
            return ErrorResponse("Please wait before submitting again.");
        }
    }

    // Actually process form
    ProcessForm();

    cp.Visit.SetProperty(rateLimitKey, DateTime.Now.ToString("o"));
    return SuccessResponse();
}
```

### After: Using New API Methods

```csharp
public override object Execute(CPBaseClass cp) {
    // 2 lines of security, crystal clear intent
    if (cp.Security.CheckHoneypot()) return FakeSuccessResponse();
    if (cp.Security.CheckRateLimit("contactFormSubmit")) return ErrorResponse("Please wait before submitting again.");

    // Actually process form
    ProcessForm();

    cp.Security.RecordAction("contactFormSubmit");
    return SuccessResponse();
}
```

**Benefits:**
- 90% less boilerplate
- Consistent behavior across all addons
- Hard to get wrong
- Self-documenting code
- Automatic integration with abuse detection system

---

## HTML Template Example

### Before: Manual Honeypot HTML

Developers have to copy/paste this from documentation and remember the exact attributes:

```html
<div style="position:absolute;left:-9999px;" aria-hidden="true">
    <input type="text" name="website_url" tabindex="-1" autocomplete="off" value="">
</div>
```

### After: Using GetHoneypotHtml()

```html
<%
    // In your addon's layout or template rendering code
    string honeypotHtml = cp.Security.GetHoneypotHtml();
%>
<form method="post" action="/contactsubmit">
    <%= honeypotHtml %>
    <input type="email" name="email" required>
    <textarea name="message" required></textarea>
    <button type="submit">Send</button>
</form>
```

Even better with design block pattern:

```csharp
// In your design block's GetHtml() method
public override string GetHtml(CPBaseClass cp) {
    return $@"
        <form method='post' action='/contactsubmit'>
            {cp.Security.GetHoneypotHtml()}
            <input type='email' name='email' required>
            <textarea name='message' required></textarea>
            <button type='submit'>Send</button>
        </form>
    ";
}
```

---

## Security Checklist for Form Addons

Use this checklist when building or auditing a form-handling addon:

### Client-Side (HTML Template)
- [ ] Add honeypot field via `cp.Security.GetHoneypotHtml()`
- [ ] Use HTTPS for form action (automatically handled by Contensive)
- [ ] Set `autocomplete="off"` on sensitive fields (passwords, credit cards)

### Server-Side (Addon Execute Method)
- [ ] Check honeypot: `if (cp.Security.CheckHoneypot()) return FakeSuccess();`
- [ ] Check rate limit: `if (cp.Security.CheckRateLimit("actionKey")) return Error();`
- [ ] Use ORM for all database operations (never raw SQL)
- [ ] Sanitize output when displaying user input: `cp.Security.SanitizeInput(input, "display")`
- [ ] Record action after success: `cp.Security.RecordAction("actionKey");`
- [ ] Log security events: `cp.Utils.AppendLog("description")`

### Additional Best Practices
- [ ] Validate email format before sending emails
- [ ] Truncate text fields to prevent database overflow
- [ ] Use `cp.User.IsAuthenticated` checks for protected actions
- [ ] Never trust client-side validation alone

---

## Migration Guide for Existing Addons

If you have existing addons with manual security implementations:

### Step 1: Update Honeypot Check

**Before:**
```csharp
string honeypotValue = cp.Doc.GetText("website_url");
if (!string.IsNullOrEmpty(honeypotValue)) {
    cp.Utils.AppendLog("honeypot triggered");
    return FakeSuccess();
}
```

**After:**
```csharp
if (cp.Security.CheckHoneypot()) return FakeSuccess();
```

### Step 2: Update Rate Limit Check

**Before:**
```csharp
string rateLimitKey = $"formSubmit-{formId}";
string lastSubmitStr = cp.Visit.GetText(rateLimitKey, "");
if (!string.IsNullOrEmpty(lastSubmitStr) && DateTime.TryParse(lastSubmitStr, out DateTime lastSubmit)) {
    if ((DateTime.Now - lastSubmit).TotalSeconds < 15) {
        return Error("Please wait");
    }
}
// ... later
cp.Visit.SetProperty(rateLimitKey, DateTime.Now.ToString("o"));
```

**After:**
```csharp
if (cp.Security.CheckRateLimit("formSubmit", 15)) return Error("Please wait");
// ... later
cp.Security.RecordAction("formSubmit");
```

### Step 3: Update HTML Templates

**Before:**
```html
<div style="position:absolute;left:-9999px;" aria-hidden="true">
    <input type="text" name="website_url" tabindex="-1" autocomplete="off" value="">
</div>
```

**After:**
```csharp
{cp.Security.GetHoneypotHtml()}
```

---

## Core Implementation Guidelines

For the Contensive core team implementing these methods:

### CPSecurityBaseClass Location
- File: `source/CPBase/BaseClasses/CPSecurityBaseClass.cs` (already exists)
- Add new abstract methods to base class
- Implement in `source/Processor/Views/CPSecurityClass.cs`

### Implementation Details

**CheckHoneypot:**
```csharp
// In CPSecurityClass.cs
public override bool CheckHoneypot(string honeypotFieldName = "website_url") {
    string honeypotValue = cp.core.docProperties.getText(honeypotFieldName);
    if (string.IsNullOrEmpty(honeypotValue)) return false;

    // Log the attempt
    cp.core.cpParent.Utils.AppendLog($"Security.CheckHoneypot, honeypot field [{honeypotFieldName}] was filled, rejecting as spam");

    // Set visit property for abuse detection integration
    cp.core.visitProperty.setProperty("SpamDetected_Honeypot", true);

    return true;
}
```

**CheckRateLimit:**
```csharp
public override bool CheckRateLimit(string actionKey, int cooldownSeconds = 15) {
    if (string.IsNullOrWhiteSpace(actionKey)) return false;

    string rateLimitKey = $"rateLimit_{actionKey}";
    string lastActionStr = cp.core.visitProperty.getText(rateLimitKey);

    if (string.IsNullOrEmpty(lastActionStr)) return false;
    if (!DateTime.TryParse(lastActionStr, out DateTime lastAction)) return false;

    double secondsSinceLastAction = (DateTime.Now - lastAction).TotalSeconds;
    if (secondsSinceLastAction < cooldownSeconds) {
        cp.core.cpParent.Utils.AppendLog($"Security.CheckRateLimit, action [{actionKey}] rate limited, {secondsSinceLastAction:F1}s since last action (cooldown: {cooldownSeconds}s)");
        cp.core.visitProperty.setProperty("RateLimitViolation", true);
        return true;
    }

    return false;
}

public override void RecordAction(string actionKey) {
    if (string.IsNullOrWhiteSpace(actionKey)) return;
    string rateLimitKey = $"rateLimit_{actionKey}";
    cp.core.visitProperty.setProperty(rateLimitKey, DateTime.Now.ToString("o"));
}
```

**GetHoneypotHtml:**
```csharp
public override string GetHoneypotHtml(string fieldName = "website_url") {
    return $@"<div style=""position:absolute;left:-9999px;"" aria-hidden=""true""><input type=""text"" name=""{fieldName}"" tabindex=""-1"" autocomplete=""off"" value=""""></div>";
}
```

**SanitizeInput:**
```csharp
public override string SanitizeInput(string userInput, string mode = "display") {
    if (string.IsNullOrEmpty(userInput)) return userInput;

    switch (mode.ToLowerInvariant()) {
        case "display":
            return System.Web.HttpUtility.HtmlEncode(userInput);
        case "url":
            return System.Web.HttpUtility.UrlEncode(userInput);
        case "sql":
            // Strip common SQL keywords (still use ORM for queries!)
            string cleaned = userInput;
            string[] sqlKeywords = { "union", "select", "drop", "delete", "update", "insert", "exec", "--", "/*", "*/" };
            foreach (var keyword in sqlKeywords) {
                cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, keyword, "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }
            return cleaned;
        default:
            return System.Web.HttpUtility.HtmlEncode(userInput);
    }
}
```

---

## Summary

### Current Pain Points
1. **Too much boilerplate** — 25+ lines of security code per form addon
2. **Inconsistent implementation** — Each developer does it differently
3. **Easy to get wrong** — Timestamp parsing, key naming, logging
4. **Low adoption** — Developers skip security due to complexity

### Proposed Solution
Add 7 simple methods to `CPSecurityBaseClass` that reduce security implementation to 2-3 lines of code:

| Method | Purpose | Lines Saved |
|---|---|---|
| `CheckHoneypot()` | Detect bot submissions | ~10 lines |
| `CheckRateLimit()` | Prevent spam bursts | ~15 lines |
| `RecordAction()` | Track successful actions | N/A (required pair) |
| `GetHoneypotHtml()` | Generate honeypot field | Eliminates copy/paste |
| `SanitizeInput()` | Clean user input | ~5 lines |
| `IsInputSuspicious()` | Advanced pattern detection | ~20 lines |
| `GetRequestSecurityInfo()` | Security telemetry | ~10 lines |

### Benefits
- ✅ **90% less boilerplate** in form addons
- ✅ **Consistent security** across all addons
- ✅ **Hard to get wrong** — one method call
- ✅ **Self-documenting** — clear intent
- ✅ **Automatic integration** with abuse detection
- ✅ **Higher adoption** — trivial to add security

### Implementation Effort
- Total: ~10-15 hours for all methods
- High-priority methods (honeypot + rate limit): ~5 hours
- Can be implemented incrementally

### Developer Experience
**Before:** "Security is too complicated, I'll skip it"
**After:** "Security is two lines of code, why wouldn't I add it?"
