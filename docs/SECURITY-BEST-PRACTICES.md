# Security Best Practices for Contensive5

This document outlines security best practices for developing with the Contensive5 platform, with a focus on authentication and authorization patterns.

## Table of Contents

- [Authentication vs Authorization](#authentication-vs-authorization)
- [The "Recognized" State](#the-recognized-state)
- [Secure Coding Patterns](#secure-coding-patterns)
- [Common Vulnerabilities to Avoid](#common-vulnerabilities-to-avoid)
- [Security Checklist](#security-checklist)
- [Examples](#examples)

---

## Authentication vs Authorization

### Authentication (Who are you?)
Authentication verifies that a user has provided valid credentials (username/password, token, etc.) and is logged into the system.

**Check with:** `core.session.isAuthenticated`

### Authorization (What can you do?)
Authorization verifies that an authenticated user has permission to perform a specific action or access specific resources.

**Check with:**
- `core.session.isAuthenticatedAdmin()` - authenticated AND has admin role
- `core.session.isAuthenticatedDeveloper()` - authenticated AND has developer role
- `core.session.isEditing(contentName)` - authenticated AND has edit permission for content
- `PermissionController.getUserContentPermissions()` - get full permission set

---

## The "Recognized" State

### What is "Recognized"?

When the site property `ALLOWAUTORECOGNIZE` is enabled, returning visitors are "recognized" - the system associates them with their member record based on visitor cookies, but **does not authenticate them**.

### Key Differences

| State | session.user.id | session.isAuthenticated | Can Access Sensitive Content? |
|-------|----------------|------------------------|------------------------------|
| **Anonymous** | 0 or guest ID | false | ❌ No |
| **Recognized** | Member ID | **false** | ❌ No |
| **Authenticated** | Member ID | **true** | ✅ Yes (if authorized) |

### Why This Matters

A "recognized" user has a `session.user` object with a valid ID, but **has not proven their identity**. They could be:
- The legitimate user on a shared computer
- Someone who stole the visitor cookie
- The browser with residual session data after logout

**Critical Rule:** Never grant access to sensitive operations based solely on the existence of a user object or user properties. **Always check `isAuthenticated` first.**

---

## Secure Coding Patterns

### ✅ CORRECT: Always Check isAuthenticated First

```csharp
// For general authentication
public void SomeSensitiveOperation() {
    if (!core.session.isAuthenticated) {
        return new ErrorResponse { message = "authentication required" };
    }
    // Now safe to proceed with authenticated user
}

// For admin-only operations
public void AdminOnlyOperation() {
    if (!core.session.isAuthenticatedAdmin()) {
        return new ErrorResponse { message = "admin access required" };
    }
    // Safe to proceed - user is authenticated AND is admin
}

// For content editing
public void EditContent(string contentName, int recordId) {
    if (!core.session.isEditing(contentName)) {
        return new ErrorResponse { message = "edit permission required" };
    }
    // Safe to proceed - user is authenticated AND has edit permission
}
```

### ❌ INCORRECT: Direct User Property Access

```csharp
// WRONG - Does not check authentication!
public void BadExample1() {
    if (core.session.user.admin) {  // ❌ Could be recognized but not authenticated
        // Grant access - SECURITY VULNERABILITY!
    }
}

// WRONG - Checks user existence instead of authentication
public void BadExample2() {
    if (core.session.user.id > 0) {  // ❌ Recognized users have IDs too!
        // Grant access - SECURITY VULNERABILITY!
    }
}

// WRONG - Null check doesn't verify authentication
public void BadExample3() {
    if (core.session.user != null && core.session.user.admin) {  // ❌ Still doesn't check isAuthenticated
        // Grant access - SECURITY VULNERABILITY!
    }
}
```

### ✅ CORRECT: Defense in Depth

Even if a method is called from a context that should already be authenticated, add defensive checks:

```csharp
public static bool isVisibleUserField(CoreController core, bool adminOnly) {
    // First gate: authentication check
    if (!core.session.isAuthenticated) {
        return false;
    }

    // Second gate: authorization check
    if (adminOnly && !core.session.user.admin) {
        return false;
    }

    return true;
}
```

---

## Common Vulnerabilities to Avoid

### 1. Missing Authentication Checks in Addon Execute Methods

**Vulnerable:**
```csharp
public override object Execute(CPBaseClass cp) {
    // Directly process request without authentication
    SaveData(cp.Request.Body);  // ❌ CRITICAL VULNERABILITY
    return "Success";
}
```

**Secure:**
```csharp
public override object Execute(CPBaseClass cp) {
    CoreController core = ((CPClass)cp).core;

    // Check authentication first
    if (!core.session.isAuthenticated) {
        return new ErrorResponse { message = "authentication required" };
    }

    // Check authorization if needed
    if (!core.session.isEditing("ContentName")) {
        return new ErrorResponse { message = "authorization required" };
    }

    // Now safe to process
    SaveData(cp.Request.Body);
    return "Success";
}
```

### 2. Information Disclosure Through Conditional Logic

**Vulnerable:**
```csharp
public string ShowAdminHint(string hint) {
    // ❌ Could evaluate user.admin even if not authenticated
    if (core.session.isEditing() || core.session.user.admin) {
        return $"<div class='admin-hint'>{hint}</div>";
    }
    return "";
}
```

**Secure:**
```csharp
public string ShowAdminHint(string hint) {
    // ✅ Use helper method that checks authentication first
    if (core.session.isEditing() || core.session.isAuthenticatedAdmin()) {
        return $"<div class='admin-hint'>{hint}</div>";
    }
    return "";
}
```

### 3. Session State Assumptions After Logout

**Vulnerable:**
```csharp
public void ProcessRequest() {
    // ❌ After logout, user object might still exist with old properties
    if (user.lastLoginDate != DateTime.MinValue) {
        // Assume user is logged in - WRONG!
    }
}
```

**Secure:**
```csharp
public void ProcessRequest() {
    // ✅ Always check current authentication status
    if (core.session.isAuthenticated && user.lastLoginDate != DateTime.MinValue) {
        // Now safe
    }
}
```

---

## Security Checklist

Use this checklist when reviewing code for security issues:

### For All Sensitive Operations:
- [ ] Is `isAuthenticated` checked before granting access?
- [ ] Are user properties accessed only after authentication verification?
- [ ] Could a "recognized but not authenticated" user access this operation?
- [ ] Is there proper error handling that doesn't leak sensitive information?

### For Addon Execute Methods:
- [ ] Does the method check `isAuthenticated` at the entry point?
- [ ] If modifying data, does it check appropriate permissions (`isEditing()`, `isAuthenticatedAdmin()`, etc.)?
- [ ] Are error responses clear but not overly detailed about security mechanisms?

### For Content/Data Access:
- [ ] Is content access controlled by `isEditing()` or permission checks?
- [ ] Are admin-only features protected by `isAuthenticatedAdmin()` or `isAuthenticatedDeveloper()`?
- [ ] Are field visibility decisions based on authentication status?

### For UI/Display Logic:
- [ ] Are admin hints/tools hidden from unauthenticated users?
- [ ] Does the UI properly reflect authentication state?
- [ ] Are sensitive URLs/endpoints not exposed in client-side code?

---

## Examples

### Example 1: Secure Remote Method

```csharp
public class SecureRemoteMethod : AddonBaseClass {
    public override object Execute(CPBaseClass cp) {
        try {
            CoreController core = ((CPClass)cp).core;

            // Step 1: Verify authentication
            if (!core.session.isAuthenticated) {
                return new {
                    success = false,
                    error = "Authentication required"
                };
            }

            // Step 2: Verify authorization for this specific operation
            if (!core.session.isAuthenticatedAdmin()) {
                return new {
                    success = false,
                    error = "Administrator access required"
                };
            }

            // Step 3: Process request (now safe)
            var request = cp.JSON.Deserialize<RequestModel>(cp.Request.Body);
            var result = ProcessSecureOperation(core, request);

            return new {
                success = true,
                data = result
            };

        } catch (Exception ex) {
            cp.Site.ErrorReport(ex);
            return new {
                success = false,
                error = "An error occurred"  // Don't leak exception details
            };
        }
    }
}
```

### Example 2: Secure Content Editing Check

```csharp
public bool CanEditRecord(CoreController core, string contentName, int recordId) {
    // Check 1: Must be authenticated
    if (!core.session.isAuthenticated) {
        return false;
    }

    // Check 2: Must have edit permission for this content type
    if (!core.session.isEditing(contentName)) {
        return false;
    }

    // Check 3: Additional business logic checks
    var content = DbBaseModel.create<ContentModel>(core.cpParent, recordId);
    if (content == null) {
        return false;
    }

    // Check 4: User-specific authorization (e.g., can only edit own records)
    if (!core.session.user.admin && content.createdBy != core.session.user.id) {
        return false;
    }

    return true;
}
```

### Example 3: Secure API Endpoint with Bearer Token

```csharp
public class SecureApiEndpoint : AddonBaseClass {
    public override object Execute(CPBaseClass cp) {
        CoreController core = ((CPClass)cp).core;

        // Authenticate via bearer token
        string authHeader = cp.Request.GetHeader("Authorization");
        if (!BearerTokenAuthController.tryAuthenticateByBearerToken(core, authHeader, out string errorMessage)) {
            cp.Response.SetStatus("401 Unauthorized");
            return new { error = "Invalid or missing authentication token" };
        }

        // Now verify user is authenticated (belt and suspenders)
        if (!core.session.isAuthenticated) {
            cp.Response.SetStatus("401 Unauthorized");
            return new { error = "Authentication failed" };
        }

        // Check specific permissions if needed
        if (!core.session.isAuthenticatedAdmin()) {
            cp.Response.SetStatus("403 Forbidden");
            return new { error = "Insufficient permissions" };
        }

        // Process authenticated and authorized request
        return ProcessApiRequest(core, cp.Request.Body);
    }
}
```

---

## Testing Security

Always write security tests for sensitive operations:

```csharp
[TestMethod]
public void Security_Operation_RejectsUnauthenticated() {
    using (CPClass cp = new(testAppName)) {
        // Arrange - ensure user is not authenticated
        cp.User.Logout();
        Assert.IsFalse(cp.User.IsAuthenticated);

        // Act - attempt operation
        var result = PerformSensitiveOperation(cp);

        // Assert - operation should be denied
        Assert.IsFalse(result.success);
        Assert.IsTrue(result.error.Contains("authentication"));
    }
}

[TestMethod]
public void Security_Operation_RejectsRecognizedButNotAuthenticated() {
    using (CPClass cp = new(testAppName)) {
        // Arrange - simulate recognized but not authenticated state
        cp.User.Logout();
        Assert.IsTrue(cp.core.session.user.id > 0);  // Recognized
        Assert.IsFalse(cp.core.session.isAuthenticated);  // Not authenticated

        // Act & Assert - should be denied
        var result = PerformSensitiveOperation(cp);
        Assert.IsFalse(result.success);
    }
}
```

---

## Additional Resources

- [OWASP Top 10](https://owasp.org/www-project-top-ten/) - Industry standard security risks
- [OWASP Authentication Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html)
- [OWASP Authorization Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Authorization_Cheat_Sheet.html)

---

## Security Contact

If you discover a security vulnerability in Contensive5, please report it to the security team immediately.

**DO NOT** create public GitHub issues for security vulnerabilities.

---

## Revision History

| Date | Version | Changes | Author |
|------|---------|---------|--------|
| 2026-04-07 | 1.0 | Initial security best practices document | Security Audit |

---

**Remember:** Security is not a feature, it's a requirement. When in doubt, verify authentication first!
