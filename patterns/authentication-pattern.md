
# Authentication Pattern

> All patterns and API reference: [Patterns Index](https://raw.githubusercontent.com/contensive/Contensive5/refs/heads/master/patterns/index.md)

> Source files: `source/Processor/Controllers/SessionController.cs`, `source/Processor/Controllers/Authentication/AuthController.cs`, `source/Processor/Controllers/Authentication/BearerTokenAuthController.cs`, `source/Processor/Addons/Authentication/CreateBearerTokenRemoteMethod.cs`

---

## Overview

Contensive supports four authentication mechanisms, all resolved during session initialization — before any addon executes. The session is established in `SessionController`, which determines the authenticated user in a defined priority order:

1. **Authorization header** — Bearer token or HTTP Basic credentials
2. **Session cookie** — returning user recognized from a prior visit
3. **Guest** — anonymous user created for the current visit

Once the session is initialized, `cp.User.IsAuthenticated` reflects whether the user was explicitly authenticated in this session, and `cp.User.Id` identifies the resolved user.

---

## 1. Username / Password (Form Login)

The standard interactive login flow. Used by the built-in login form and any addon that presents credentials.

**How it works:**

1. User submits a login form with `username` and `password` fields.
2. `AuthController.tryAuthenticate()` validates credentials against the `ccmembers` table.
3. On success, `AuthController.authenticateById()` marks the visit as authenticated (`visit.visitAuthenticated = true`) and associates the user with the visit record.
4. Subsequent requests recognize the user via the visit cookie (see Session Cookie below).

**Site property options that affect this flow:**

| Property | Default | Effect |
|----------|---------|--------|
| `AllowEmailLogin` | false | Allow login by email address in addition to username |
| `AllowNoPasswordLogin` | false | Allow login without a password (non-admin users only) |
| `AllowAutoLogin` | false | Persist authentication across browser sessions |
| `AllowPlainTextPassword` | true | Accept plaintext password comparison |

**Checking authentication state in an addon:**

```csharp
if (!cp.User.IsAuthenticated) {
    return "Please log in to access this content.";
}
```

**Forcing the login form:**

Redirect to the login page by returning an empty string from a route that requires authentication, or call the auth workflow directly:

```csharp
// -- from a page addon: return login form if not authenticated
if (!cp.User.IsAuthenticated) {
    return AuthWorkflowController.processGetAuthWorkflow(core, false, true);
}
```

---

## 2. Bearer Token

A stateless, header-based authentication method suited for API clients and the MCP server. The token is a two-way encrypted string that encodes a random key and an expiry date.

### Creating a Token

A bearer token is generated per-user via the `CreateBearerToken` remote method (addon). The caller must be an admin, or the user themselves.

**Request:**
```
POST /api/CreateBearerToken
Content-Type: application/x-www-form-urlencoded

userId=42
```

**Response:**
```json
{
  "success": true,
  "message": "Bearer token created.",
  "token": "<encrypted-token>",
  "expires": "2027-04-07"
}
```

The token is stored (as its random key component) in `ccmembers.bearerToken`. Each call overwrites the previous key, so a user has at most one valid token at a time. Tokens expire after one year.

### Using a Token

Include the token in the `Authorization` header of every request:

```
Authorization: Bearer <encrypted-token>
```

### How it works internally

1. During session initialization, `SessionController` reads the `Authorization` header.
2. If it begins with `Bearer `, `BearerTokenAuthController.tryAuthenticateByBearerToken()` is called.
3. The token is decrypted, the expiry is checked, and the random key is matched against `ccmembers.bearerToken`.
4. On success, `AuthController.authenticateById()` authenticates the matching user into the session.

**Token validation failures** — any of the following cause silent failure and fall-through to guest:
- Missing or malformed `Authorization` header
- Decryption failure
- Expired token
- No matching active user record

An expired token additionally sets the HTTP response status to `401 Unauthorized`.

---

## 3. HTTP Basic Authentication

Standard HTTP Basic auth (`RFC 7617`). Credentials are base64-encoded as `username:password` in the `Authorization` header. Suited for simple API integrations and tools that natively support Basic auth.

### Using Basic Auth

```
Authorization: Basic <base64(username:password)>
```

Example (username `api_user`, password `s3cret`):
```
Authorization: Basic YXBpX3VzZXI6czNjcmV0
```

### How it works internally

1. During session initialization, `SessionController` reads the `Authorization` header.
2. If it begins with `Basic `, the base64 value is decoded and split on the first `:`.
3. `AuthController.tryAuthenticate()` validates the extracted username and password — the same credential check used by the login form.
4. On success, the user is authenticated into the session.

**Notes:**
- Malformed base64 is silently ignored; the request proceeds as a guest.
- The same site property flags that apply to form login (`AllowEmailLogin`, etc.) apply here.
- Basic auth should only be used over HTTPS to avoid credential exposure.

---

## 4. Session Cookie (Returning User)

Cookie-based recognition for browser sessions. This is not an explicit authentication mechanism — it carries forward authentication that was established in a prior request.

**How it works:**

1. On each request, `SessionController` reads the visit cookie.
2. The cookie is decrypted to a visit record ID.
3. If the visit record is active (not expired, within the 1-hour window), the associated `memberId` identifies the returning user.
4. The user is loaded from `ccmembers` but is only considered *authenticated* if `visit.visitAuthenticated` is true (set during the original login or header-based auth).

**Interaction with header auth:**

If a request includes an `Authorization` header, it takes precedence over the session cookie. The header-authenticated user replaces the cookie-recognized user for that request.

---

## Priority Order

During each request, `SessionController` resolves the user in this order — first match wins:

```
1. Authorization: Bearer <token>   → BearerTokenAuthController.tryAuthenticateByBearerToken()
2. Authorization: Basic <b64>      → AuthController.tryAuthenticate()
3. Visit cookie → visit.memberId   → DbBaseModel.create<PersonModel>()
4. (none)                          → SessionController.createGuest()
```

---

## Checking Authentication in Addons

```csharp
// is the user logged in?
bool authenticated = cp.User.IsAuthenticated;

// is the user an admin?
bool isAdmin = cp.User.IsAdmin;

// current user id (0 if guest)
int userId = cp.User.Id;

// current username
string username = cp.User.Name;
```

For protected addons or remote methods, check at the top of `Execute()`:

```csharp
public override object Execute(CPBaseClass cp) {
    try {
        if (!cp.User.IsAuthenticated) {
            cp.Response.SetStatus(WebServerController.httpResponseStatus.Unauthorized);
            return "{\"success\":false,\"message\":\"Authentication required.\"}";
        }
        // -- addon logic
    } catch (Exception ex) {
        cp.Site.ErrorReport(ex);
        return "There was an error executing this addon.";
    }
}
```

---

## Key Classes

| Class | Location | Responsibility |
|-------|----------|---------------|
| `SessionController` | `Controllers/SessionController.cs` | Session init, resolves user via all auth paths |
| `AuthController` | `Controllers/Authentication/AuthController.cs` | Username/password validation, authenticateById, recognizeById |
| `BearerTokenAuthController` | `Controllers/Authentication/BearerTokenAuthController.cs` | Bearer token decrypt, validate, authenticate |
| `AuthWorkflowController` | `Controllers/Authentication/AuthWorkflowController.cs` | Login form rendering and processing |
| `CreateBearerTokenRemoteMethod` | `Addons/Authentication/CreateBearerTokenRemoteMethod.cs` | Remote method to generate bearer tokens |
| `SecurityController` | `Controllers/SecurityController.cs` | Two-way encryption used by bearer tokens and cookies |
