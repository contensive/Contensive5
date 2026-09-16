
# Security Best Practices

> All patterns and API reference: [Patterns Index](https://raw.githubusercontent.com/contensive/Contensive5/refs/heads/master/patterns/index.md)

> Source files: `source/Processor/Controllers/SessionController.cs`, `source/Processor/Controllers/Authentication/AuthController.cs`

---

## Overview

This document covers secure coding patterns for Contensive development, focused on authentication and authorization checks. For the full description of how each authentication mechanism works, see [Authentication Pattern](authentication-pattern.md).

---

## Authentication vs Authorization

### Authentication (Who are you?)
Authentication verifies that a user has provided valid credentials and is logged into the system.

**Check with:** `core.session.isAuthenticated`

### Authorization (What can you do?)
Authorization verifies that an authenticated user has permission to perform a specific action or access specific resources.

**Check with:**
- `core.session.isAuthenticatedAdmin()` — authenticated AND has admin role
- `core.session.isAuthenticatedDeveloper()` — authenticated AND has developer role
- `core.session.isEditing(contentName)` — authenticated AND has edit permission for content
- `PermissionController.getUserContentPermissions()` — get full permission set

---

## The "Recognized" State

When the site property `ALLOWAUTORECOGNIZE` is enabled, returning visitors are "recognized" — the system associates them with their member record based on visitor cookies, but **does not authenticate them**.

| State | session.user.id | session.isAuthenticated | Can Access Sensitive Content? |
|-------|----------------|------------------------|------------------------------|
| **Anonymous** | 0 or guest ID | false | No |
| **Recognized** | Member ID | **false** | No |
| **Authenticated** | Member ID | **true** | Yes (if authorized) |

A "recognized" user has a `session.user` object with a valid ID, but **has not proven their identity**. They could be the legitimate user on a shared computer, someone who obtained the visitor cookie, or a browser with residual session data after logout.

**Critical Rule:** Never grant access to sensitive operations based solely on the existence of a user object or user ID. **Always check `isAuthenticated` first.**

---

## Secure Coding Patterns

### Correct: Always Check isAuthenticated First

```csharp
// for general authentication
if (!core.session.isAuthenticated) {
    return new ErrorResponse { message = "authentication required" };
}

// for admin-only operations
if (!core.session.isAuthenticatedAdmin()) {
    return new ErrorResponse { message = "admin access required" };
}

// for content editing
if (!core.session.isEditing("ContentName")) {
    return new ErrorResponse { message = "edit permission required" };
}
```

### Incorrect: Direct User Property Access

```csharp
// WRONG - Does not check authentication; recognized users have admin=true too
if (core.session.user.admin) { }

// WRONG - Recognized users have IDs too
if (core.session.user.id > 0) { }

// WRONG - Null check does not verify authentication
if (core.session.user != null && core.session.user.admin) { }
```

### Correct: Defense in Depth

Even if a method is called from a context that should already be authenticated, add defensive checks:

```csharp
public static bool isVisibleUserField(CoreController core, bool adminOnly) {
    if (!core.session.isAuthenticated) {
        return false;
    }
    if (adminOnly && !core.session.user.admin) {
        return false;
    }
    return true;
}
```

---

## Common Vulnerabilities to Avoid

### 1. Missing Authentication Check in Addon Execute Methods

```csharp
// VULNERABLE
public override object Execute(CPBaseClass cp) {
    SaveData(cp.Request.Body);  // no authentication check
    return "Success";
}

// SECURE
public override object Execute(CPBaseClass cp) {
    try {
        CoreController core = ((CPClass)cp).core;
        if (!core.session.isAuthenticated) {
            cp.Response.SetStatus(WebServerController.httpResponseStatus.Unauthorized);
            return "{\"success\":false,\"message\":\"Authentication required.\"}";
        }
        if (!core.session.isEditing("ContentName")) {
            cp.Response.SetStatus(WebServerController.httpResponseStatus.Unauthorized);
            return "{\"success\":false,\"message\":\"Edit permission required.\"}";
        }
        SaveData(cp.Request.Body);
        return "Success";
    } catch (Exception ex) {
        cp.Site.ErrorReport(ex);
        return "There was an error executing this addon.";
    }
}
```

### 2. Information Disclosure Through Conditional Logic

```csharp
// VULNERABLE - evaluates user.admin even on unathenticated users
if (core.session.isEditing() || core.session.user.admin) { }

// SECURE - isAuthenticatedAdmin() checks authentication first
if (core.session.isEditing() || core.session.isAuthenticatedAdmin()) { }
```

### 3. Session State Assumptions After Logout

```csharp
// VULNERABLE - user object may still exist with old properties after logout
if (user.lastLoginDate != DateTime.MinValue) { }

// SECURE
if (core.session.isAuthenticated && user.lastLoginDate != DateTime.MinValue) { }
```

---

## Rate Limiting & Anti-Enumeration

### Rate Limiting for Side-Effect Endpoints

Any endpoint that triggers an external action — sending email, sending SMS, creating tokens, charging money — must be throttled per identifier. Without rate limiting, an attacker can automate requests to flood a user's inbox, exhaust email API quotas, or degrade application performance.

**Endpoints that require rate limiting:**
- Password reset (per email address)
- OTP / verification code requests (per email or phone)
- Contact forms and "email a friend" features (per sender)
- Account verification re-send (per email)
- Any "send me a link" flow

**Pattern:** Use `core.cache` to track attempt counts with automatic expiration. The cache is Redis-backed in production (safe across load-balanced servers) with transparent fallback to local memory cache. `getInteger()` returns 0 on cache miss, which is the correct initial value.

```csharp
// -- rate limit: max N requests per identifier per time window
string rateLimitKey = $"action-name/{identifier.Trim().ToLowerInvariant()}";
int attemptCount = core.cache.getInteger(rateLimitKey);
if (attemptCount >= maxAttempts) {
    //
    // -- silently succeed to avoid revealing throttle state
    return true;
}
//
// ... perform action (send email, create token, etc.) ...
//
// -- increment counter with time-window expiration
core.cache.storeObject(rateLimitKey, attemptCount + 1, core.dateTimeNowMockable.AddMinutes(windowMinutes));
```

**Key design decisions:**
- **Silent success on throttle.** Return the same response as a successful request. If the attacker sees a different response when rate-limited, they know the limit exists and can adjust their timing.
- **Per-identifier, not per-IP.** Rate limit by the target (email address, phone number) to prevent a single target from being bombed regardless of how many source IPs the attacker uses. Per-IP rate limiting is a complementary layer handled separately.
- **Increment on all code paths.** Whether the action succeeds, the user is not found, or the email fails to send — always increment the counter so that failed lookups still consume rate limit budget.

### Generic Response Pattern (Anti-Enumeration)

Endpoints that look up users by email or username must return identical responses for found and not-found cases. Different responses allow attackers to discover which email addresses have accounts (user enumeration), which enables targeted phishing and credential-stuffing attacks.

**This applies to:**
- Password reset ("no account found" vs. "reset link sent")
- Registration ("email already taken" vs. success)
- Login ("invalid username" vs. "invalid password")

```csharp
// VULNERABLE - reveals whether the email exists
if (userList.Count == 0) {
    userErrorMessage = $"There is no user with this email [{requestEmail}].";
    return false;
}

// SECURE - always show the same confirmation regardless of email lookup result
if (userList.Count == 0) {
    //
    // -- no user found, return true to avoid revealing whether the email exists
    return true;
}
// The caller renders the same "check your email" confirmation page in both cases
```

**Guidelines:**
- Always return the same HTTP status code for found and not-found cases
- Always render the same confirmation page or message
- Avoid timing side channels — don't skip expensive operations (email send, token creation) and return faster for unknown emails. The time difference can reveal account existence.
- Log the attempt internally for monitoring even when suppressing the user-facing error

---

## Examples

### Example 1: Secure Remote Method

```csharp
public class SecureRemoteMethod : AddonBaseClass {
    public override object Execute(CPBaseClass cp) {
        try {
            CoreController core = ((CPClass)cp).core;
            if (!core.session.isAuthenticated) {
                cp.Response.SetStatus(WebServerController.httpResponseStatus.Unauthorized);
                return cp.JSON.Serialize(new { success = false, error = "Authentication required." });
            }
            if (!core.session.isAuthenticatedAdmin()) {
                cp.Response.SetStatus(WebServerController.httpResponseStatus.Unauthorized);
                return cp.JSON.Serialize(new { success = false, error = "Administrator access required." });
            }
            var result = ProcessSecureOperation(core, cp.Request.Body);
            return cp.JSON.Serialize(new { success = true, data = result });
        } catch (Exception ex) {
            cp.Site.ErrorReport(ex);
            return cp.JSON.Serialize(new { success = false, error = "An error occurred." });
        }
    }
}
```

### Example 2: Secure Content Editing Check

```csharp
public bool CanEditRecord(CoreController core, string contentName, int recordId) {
    if (!core.session.isAuthenticated) { return false; }
    if (!core.session.isEditing(contentName)) { return false; }
    var content = DbBaseModel.create<ContentModel>(core.cpParent, recordId);
    if (content == null) { return false; }
    // user-specific check: only edit own records unless admin
    if (!core.session.user.admin && content.createdBy != core.session.user.id) { return false; }
    return true;
}
```

### Example 3: Secure API Endpoint Using Bearer or Basic Auth

Bearer token and Basic auth are resolved automatically during session initialization — no manual token parsing is needed in addon code. Simply check `isAuthenticated` as usual:

```csharp
public class SecureApiEndpoint : AddonBaseClass {
    public override object Execute(CPBaseClass cp) {
        try {
            CoreController core = ((CPClass)cp).core;
            // -- bearer token and basic auth are handled during session init;
            //    check isAuthenticated regardless of which mechanism was used
            if (!core.session.isAuthenticated) {
                cp.Response.SetStatus(WebServerController.httpResponseStatus.Unauthorized);
                return cp.JSON.Serialize(new { success = false, error = "Authentication required." });
            }
            if (!core.session.isAuthenticatedAdmin()) {
                cp.Response.SetStatus(WebServerController.httpResponseStatus.Unauthorized);
                return cp.JSON.Serialize(new { success = false, error = "Insufficient permissions." });
            }
            return ProcessApiRequest(core, cp.Request.Body);
        } catch (Exception ex) {
            cp.Site.ErrorReport(ex);
            return cp.JSON.Serialize(new { success = false, error = "An error occurred." });
        }
    }
}
```

---

## Form Security & Spam Prevention

### Overview

Form-handling addons (contact forms, comment submission, user registration, etc.) are prime targets for spam bots and abuse. Contensive provides built-in security methods to protect forms with minimal code.

### Required Security Layers

Every form-handling addon should implement these protections in order:

1. **Honeypot Check** — catches simple bots (fastest check)
2. **Rate Limiting** — prevents spam bursts and abuse
3. **Authentication Check** — if the form requires login
4. **Input Validation** — business logic validation
5. **ORM for Database Operations** — prevents SQL injection

### cp.Security API Methods

#### CheckHoneypot() — Bot Detection

Detects if a hidden "honeypot" field was filled by a bot. Returns `true` if spam detected.

```csharp
public override object Execute(CPBaseClass cp) {
    // One-line bot detection
    if (cp.Security.CheckHoneypot()) {
        // Return fake success so bot thinks it succeeded
        return cp.JSON.Serialize(new { success = true, message = "Thank you!" });
    }

    // Process legitimate form...
}
```

**Usage Notes:**
- Automatically logs spam attempts
- Integrates with abuse detection system
- Default field name is "website_url" (customize if needed)
- Must pair with `GetHoneypotHtml()` in your form template

#### GetHoneypotHtml() — HTML Generation

Generates the honeypot field HTML for your form template. Field is hidden from real users but visible to bots.

```csharp
// In your addon's HTML/template generation:
string formHtml = $@"
    <form method='post' action='/submitform'>
        {cp.Security.GetHoneypotHtml()}
        <input type='email' name='email' placeholder='Your Email' required>
        <textarea name='message' placeholder='Your Message' required></textarea>
        <button type='submit'>Send</button>
    </form>
";
return formHtml;
```

**Generated HTML:**
```html
<div style="position:absolute;left:-9999px;" aria-hidden="true">
    <input type="text" name="website_url" tabindex="-1" autocomplete="off" value="">
</div>
```

#### CheckRateLimit() — Spam Burst Prevention

Prevents rapid-fire submissions from the same visitor. Returns `true` if rate limit exceeded.

```csharp
public override object Execute(CPBaseClass cp) {
    // Check honeypot first (cheapest)
    if (cp.Security.CheckHoneypot()) {
        return FakeSuccessResponse();
    }

    // Check rate limit (prevent spam bursts and double-submits)
    if (cp.Security.CheckRateLimit("contactFormSubmit", cooldownSeconds: 15)) {
        return cp.JSON.Serialize(new {
            success = false,
            error = "Please wait a moment before submitting again."
        });
    }

    // Process form...
    ProcessFormSubmission();

    // Record successful action AFTER processing
    cp.Security.RecordAction("contactFormSubmit");

    return cp.JSON.Serialize(new { success = true });
}
```

**Parameters:**
- `actionKey` — Unique identifier for this action (e.g., "contactForm", "commentPost")
- `cooldownSeconds` — Seconds required between submissions (default: 15)

**Usage Notes:**
- Automatically logs rate limit violations
- Uses visit-level storage (tied to IP + browser)
- Thread-safe for concurrent requests
- Must call `RecordAction()` after successful form processing

#### RecordAction() — Mark Successful Action

Records that the visitor successfully completed an action, starting the rate limit cooldown timer.

```csharp
// Call AFTER successful form processing
cp.Security.RecordAction("contactFormSubmit");
```

**Important:** Only call this after the form has been successfully processed (validation passed, email sent, data saved, etc.). Don't call it if form validation fails.

#### SanitizeInput() — Input Cleaning

Sanitizes user input for safe display or storage.

```csharp
// For displaying user input in HTML
string safeName = cp.Security.SanitizeInput(userName, "display");
string html = $"<p>Thank you, {safeName}!</p>";

// For embedding in URLs
string safeQuery = cp.Security.SanitizeInput(searchTerm, "url");
string redirectUrl = $"/search?q={safeQuery}";

// For extra safety when storing searchable text
string safeBio = cp.Security.SanitizeInput(userBio, "sql");
```

**Modes:**
- `"display"` — HTML-encodes dangerous characters (default)
- `"url"` — URL-encodes per RFC 3986
- `"sql"` — Strips common SQL injection patterns (still use ORM for queries!)

**Important:** This does NOT replace parameterized queries. Always use `DbBaseModel` for database operations.

### Complete Form Security Example

```csharp
public class ContactFormSubmit : AddonBaseClass {
    public override object Execute(CPBaseClass cp) {
        try {
            // 1. Check honeypot (catches bots - cheapest check)
            if (cp.Security.CheckHoneypot()) {
                cp.Utils.AppendLog("ContactForm: honeypot triggered");
                return cp.JSON.Serialize(new {
                    success = true,
                    message = "Thank you for your message!"
                });
            }

            // 2. Check rate limit (prevents spam bursts)
            if (cp.Security.CheckRateLimit("contactFormSubmit", 15)) {
                return cp.JSON.Serialize(new {
                    success = false,
                    error = "Please wait a moment before submitting again."
                });
            }

            // 3. Validate required fields
            string email = cp.Doc.GetText("email");
            string message = cp.Doc.GetText("message");

            if (string.IsNullOrWhiteSpace(email)) {
                return cp.JSON.Serialize(new {
                    success = false,
                    error = "Email is required."
                });
            }

            if (string.IsNullOrWhiteSpace(message)) {
                return cp.JSON.Serialize(new {
                    success = false,
                    error = "Message is required."
                });
            }

            // 4. Sanitize input for storage and display
            string safeEmail = cp.Security.SanitizeInput(email, "display");
            string safeMessage = cp.Security.SanitizeInput(message, "display");

            // 5. Save to database using ORM (prevents SQL injection)
            var submission = ContactSubmission.addDefault<ContactSubmission>(cp);
            submission.email = safeEmail.Substring(0, Math.Min(safeEmail.Length, 255));
            submission.message = safeMessage;
            submission.submittedDate = DateTime.Now;
            submission.save(cp);

            // 6. Send notification email
            string emailBody = $"<p>New contact form submission:</p><p><strong>From:</strong> {safeEmail}</p><p><strong>Message:</strong> {safeMessage}</p>";
            cp.Email.send("admin@example.com", "Contact Form Submission", emailBody);

            // 7. Record successful action (starts rate limit cooldown)
            cp.Security.RecordAction("contactFormSubmit");

            return cp.JSON.Serialize(new {
                success = true,
                message = "Thank you for your message!"
            });

        } catch (Exception ex) {
            cp.Site.ErrorReport(ex);
            return cp.JSON.Serialize(new {
                success = false,
                error = "An error occurred. Please try again later."
            });
        }
    }
}
```

### Form Template Example

```html
<div class="contact-form">
    <form id="contactForm" method="post" action="/contactformsubmit">
        <!-- Honeypot field (hidden from users, visible to bots) -->
        <div style="position:absolute;left:-9999px;" aria-hidden="true">
            <input type="text" name="website_url" tabindex="-1" autocomplete="off" value="">
        </div>

        <div class="form-group">
            <label for="email">Your Email *</label>
            <input type="email" id="email" name="email" required>
        </div>

        <div class="form-group">
            <label for="message">Message *</label>
            <textarea id="message" name="message" rows="5" required></textarea>
        </div>

        <button type="submit">Send Message</button>
    </form>
</div>
```

**Or use the helper method:**

```csharp
string formHtml = $@"
<div class='contact-form'>
    <form id='contactForm' method='post' action='/contactformsubmit'>
        {cp.Security.GetHoneypotHtml()}

        <div class='form-group'>
            <label for='email'>Your Email *</label>
            <input type='email' id='email' name='email' required>
        </div>

        <div class='form-group'>
            <label for='message'>Message *</label>
            <textarea id='message' name='message' rows='5' required></textarea>
        </div>

        <button type='submit'>Send Message</button>
    </form>
</div>
";
```

### Security Processing Order

Execute security checks in this order for optimal performance:

1. **Honeypot** — Cheapest check, eliminates most bots
2. **Rate Limit** — Prevents processing load from rapid submissions
3. **Authentication** — If form requires login
4. **Business Validation** — Required fields, format checks
5. **Database Operations** — Using ORM with parameterized queries
6. **Record Action** — Only after all validation passes

### Form Security Checklist

When building or auditing a form-handling addon:

#### Client-Side (HTML Template)
- [ ] Honeypot field added via `cp.Security.GetHoneypotHtml()` or manual HTML
- [ ] Honeypot positioned off-screen with `position:absolute;left:-9999px;`
- [ ] Honeypot has `aria-hidden="true"` for accessibility
- [ ] Honeypot has `tabindex="-1"` to prevent keyboard navigation
- [ ] Form uses HTTPS (automatically handled by Contensive)

#### Server-Side (Addon Execute Method)
- [ ] Check honeypot: `if (cp.Security.CheckHoneypot()) return FakeSuccess();`
- [ ] Check rate limit: `if (cp.Security.CheckRateLimit("actionKey")) return Error();`
- [ ] Validate authentication if required: `if (!cp.User.IsAuthenticated) return Unauthorized();`
- [ ] Validate required fields and business logic
- [ ] Sanitize output when displaying user input: `cp.Security.SanitizeInput(input, "display")`
- [ ] Use ORM (`DbBaseModel`) for all database operations (never raw SQL)
- [ ] Truncate text fields to prevent database overflow
- [ ] Record action after success: `cp.Security.RecordAction("actionKey");`
- [ ] Log important events: `cp.Utils.AppendLog("description")`
- [ ] Wrap in try/catch with proper error response

#### Security Features
- [ ] Different responses for bot detection (fake success) vs. rate limits (error)
- [ ] Same cooldown key used in `CheckRateLimit()` and `RecordAction()`
- [ ] Rate limit cooldown appropriate for use case (15s for contact forms, 60s+ for password resets)
- [ ] No sensitive information leaked in error messages

---

## Security Checklist

### All Sensitive Operations
- [ ] Is `isAuthenticated` checked before granting access?
- [ ] Are user properties accessed only after authentication verification?
- [ ] Could a recognized-but-unauthenticated user reach this operation?
- [ ] Do error responses avoid leaking internal details?

### Addon Execute Methods
- [ ] Does the method check `isAuthenticated` at the entry point?
- [ ] If modifying data, does it check appropriate permissions (`isEditing()`, `isAuthenticatedAdmin()`, etc.)?

### Content / Data Access
- [ ] Is content access controlled by `isEditing()` or permission checks?
- [ ] Are admin-only features protected by `isAuthenticatedAdmin()` or `isAuthenticatedDeveloper()`?

### UI / Display Logic
- [ ] Are admin tools hidden from unauthenticated users?
- [ ] Are sensitive URLs/endpoints not exposed in client-side code?

### Endpoints That Trigger External Actions (Email, SMS, Tokens)
- [ ] Is the endpoint rate-limited per target identifier (email address, phone number, userId)?
- [ ] Does the response avoid revealing whether the user or email exists in the system?
- [ ] Do success and failure code paths return the same response to the caller?
- [ ] Is the rate limit counter incremented on all code paths (success, user-not-found, send failure)?
- [ ] Could an attacker use this endpoint to flood a user's inbox or phone with automated requests?

### Form-Handling Addons (Contact Forms, Comments, Registration, etc.)
- [ ] Honeypot field included in HTML template via `cp.Security.GetHoneypotHtml()`
- [ ] Server checks honeypot: `cp.Security.CheckHoneypot()` returns fake success if spam
- [ ] Server checks rate limit: `cp.Security.CheckRateLimit("actionKey")` before processing
- [ ] Server records action: `cp.Security.RecordAction("actionKey")` after successful submit
- [ ] User input sanitized before display: `cp.Security.SanitizeInput(input, "display")`
- [ ] All database operations use ORM (`DbBaseModel.create`, `model.save()`)
- [ ] Text fields truncated to database column length limits
- [ ] Security checks executed in correct order: honeypot → rate limit → auth → validation → processing

---

## Testing Security

```csharp
[TestMethod]
public void Security_Operation_RejectsUnauthenticated() {
    using (CPClass cp = new(testAppName)) {
        cp.User.Logout();
        Assert.IsFalse(cp.User.IsAuthenticated);
        var result = PerformSensitiveOperation(cp);
        Assert.IsFalse(result.success);
        Assert.IsTrue(result.error.Contains("authentication"));
    }
}

[TestMethod]
public void Security_Operation_RejectsRecognizedButNotAuthenticated() {
    using (CPClass cp = new(testAppName)) {
        cp.User.Logout();
        Assert.IsTrue(cp.core.session.user.id > 0);        // recognized
        Assert.IsFalse(cp.core.session.isAuthenticated);   // not authenticated
        var result = PerformSensitiveOperation(cp);
        Assert.IsFalse(result.success);
    }
}
```
