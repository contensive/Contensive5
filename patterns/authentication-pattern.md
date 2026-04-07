
# Authentication Pattern

> All patterns and API reference: [Patterns Index](https://raw.githubusercontent.com/contensive/Contensive5/refs/heads/master/patterns/index.md)

> Source files: `source/Processor/Controllers/SessionController.cs`, `source/Processor/Controllers/CoreController.cs`, `source/Processor/Controllers/Authentication/AuthController.cs`, `source/Processor/Controllers/Authentication/AuthEventController.cs`, `source/Processor/Controllers/Authentication/BearerTokenAuthController.cs`, `source/Processor/Addons/Authentication/CreateBearerTokenRemoteMethod.cs`

---

## Overview

Authentication is resolved in two sequential phases on every request, before any addon executes.

**Phase 1 — Session initialization (`SessionController`):** Passive signals are checked — stateless auth headers and the session cookie. These carry forward existing identity without examining request content.

**Phase 2 — Authentication event (`AuthEventController` via `CoreController`):** Active request signals are processed — form submissions, query string credentials, link tokens, and auto-login/auto-recognize rules. If the default event produces no authentication, a custom `authentication event` fires, allowing addons to provide their own auth logic.

After both phases, `cp.User.IsAuthenticated` reflects whether the user was explicitly authenticated, and `cp.User.Id` identifies the resolved user.

---

## Authenticated vs. Recognized

These are distinct states with different security implications:

| State | `cp.User.Id` | `cp.User.IsAuthenticated` | Meaning |
|-------|-------------|--------------------------|---------|
| **Guest** | 0 | false | No identity established |
| **Recognized** | Member ID | **false** | Identity inferred, not proven |
| **Authenticated** | Member ID | **true** | Identity explicitly verified |

A **recognized** user has been identified — the system associates them with a member record — but they have not proven their identity for this session. Recognition is used for personalization and tracking. **Never grant access to sensitive operations based on recognition alone.** See [Security Best Practices](security-best-practices.md).

`AuthController.authenticateById()` produces an authenticated session. `AuthController.recognizeById()` produces only a recognized session.

---

## Phase 1: Session Initialization

`SessionController` resolves user identity from passive signals in this priority order — first match wins:

```
1. Authorization: Bearer <token>   → BearerTokenAuthController.tryAuthenticateByBearerToken()
2. Authorization: Basic <b64>      → AuthController.tryAuthenticate()
3. Visit cookie → visit.memberId   → DbBaseModel.create<PersonModel>()
4. (none)                          → SessionController.createGuest()
```

### 1a. Bearer Token

A stateless, header-based authentication method suited for API clients. The token is a two-way encrypted string encoding a random key and an expiry date.

**Using a token:**
```
Authorization: Bearer <encrypted-token>
```

**How it works:**
1. `SessionController` reads the `Authorization` header.
2. `BearerTokenAuthController.tryAuthenticateByBearerToken()` decrypts the token and checks expiry.
3. The random key is matched against `ccmembers.bearerToken`.
4. On success, `AuthController.authenticateById()` authenticates the matching user.

An expired token sets the HTTP response status to `401 Unauthorized`. All other failures fall through silently to the next option.

**Creating a token** via the `CreateBearerToken` remote method (admin or self only):

```
POST /api/CreateBearerToken
userId=42
```
```json
{ "success": true, "token": "<encrypted-token>", "expires": "2027-04-07" }
```

The random key is stored in `ccmembers.bearerToken`. Each call overwrites the previous, so a user has at most one valid token at a time. Tokens expire after one year.

### 1b. HTTP Basic Authentication

Standard HTTP Basic auth (RFC 7617). Credentials are base64-encoded as `username:password`.

```
Authorization: Basic <base64(username:password)>
```

**How it works:**
1. `SessionController` decodes the base64 credential and splits on the first `:`.
2. `AuthController.tryAuthenticate()` validates the credentials — same check as form login.
3. On success, the user is authenticated into the session.

Malformed base64 falls through silently. Basic auth should only be used over HTTPS.

### 1c. Session Cookie (Carry-Forward)

Restores the user from a previous request. Does not produce new authentication — it carries forward whatever state (`visitAuthenticated`) was established when the visit record was created.

**How it works:**
1. The visit cookie is decrypted to a visit record ID.
2. If the visit is still active (within the 1-hour window), `visit.memberId` identifies the user.
3. The user is loaded from `ccmembers`. `visit.visitAuthenticated` determines whether they are authenticated or merely recognized.

---

## Phase 2: Authentication Event

After session initialization, `CoreController` calls `AuthEventController.processAuthenticationDefaultEvent()`. This processes active request signals that may produce new authentication or recognition for this request. If no default handler succeeds, the custom `authentication event` fires, allowing addons to provide their own auth logic.

### 2a. Form Login (Username / Password)

The standard interactive login flow, used by the built-in login form.

**How it works:**
1. A POST request includes `type=login` (or the legacy value `l09H58a195`) along with `username` and `password`.
2. `LoginWorkflowController.processLogin()` validates credentials.
3. On success, `AuthController.authenticateById()` marks the session authenticated and associates the user with the visit record. Subsequent requests carry this forward via the session cookie.

**Site properties:**

| Property | Default | Effect |
|----------|---------|--------|
| `AllowEmailLogin` | false | Accept email address as the username field |
| `AllowNoPasswordLogin` | false | Allow login without a password (non-admin users only) |
| `AllowPlainTextPassword` | true | Accept plaintext password comparison |

**Triggering the login form from an addon:**
```csharp
if (!cp.User.IsAuthenticated) {
    return AuthWorkflowController.processGetAuthWorkflow(core, false, true);
}
```

### 2b. Query String Credentials (authAction)

An alternative login path via query string or POST parameters, independent of the standard form type field.

**How it works:**
- Request includes `authAction=login` plus `authUsername` and `authPassword`.
- `AuthController.preflightAuthentication_returnUserId()` validates credentials, then `authenticateById()` authenticates the session.

### 2c. Link Authentication

A user who receives a link containing an encrypted `eid` token is automatically **authenticated** when they click it — no login form required. The token identifies the member the link was generated for.

**How it works:**
1. The request URL includes `eid=<encrypted-token>`.
2. `SecurityController.decodeToken()` decrypts the token to a user ID and expiry.
3. If the token is valid and not expired, and `AllowLinkLogin` is enabled, `AuthController.authenticateById()` authenticates that user.

**Site property:**

| Property | Default | Effect |
|----------|---------|--------|
| `AllowLinkLogin` | true | Enable link-based authentication |

If `AllowLinkLogin` is disabled, the system falls through to check `AllowLinkRecognize` (see below).

### 2d. Link Recognition

Similar to link authentication, but produces only a **recognized** (not authenticated) session. The user is identified as the link recipient without being logged in.

**How it works:**
1. Same `eid` token flow as link authentication.
2. If `AllowLinkLogin` is disabled but `AllowLinkRecognize` is enabled, `AuthController.recognizeById()` recognizes the user rather than authenticating them.

**Site property:**

| Property | Default | Effect |
|----------|---------|--------|
| `AllowLinkRecognize` | true | Enable link-based recognition |

Link authentication takes precedence over link recognition when both are enabled.

### 2e. Auto-Login (Returning Visitor)

On the **first hit of a new visit**, if the visitor cookie identifies a known member with `autoLogin` enabled, that user is automatically **authenticated** without any action on their part.

**How it works:**
1. Only applies when `visit.pageVisits == 0` (first hit of a new visit).
2. `visitor.memberId` identifies the returning user.
3. If `AllowAutoLogin` is enabled, `AuthController.authenticateById()` is called with `requireUserAutoLogin=true` — the user's `autoLogin` flag must be set.

**Site property:**

| Property | Default | Effect |
|----------|---------|--------|
| `AllowAutoLogin` | false | Enable automatic login for returning visitors |

The `autoLogin` flag on the user record is set when they log in with "remember me" and cleared on explicit logout.

### 2f. Auto-Recognition (Returning Visitor)

On the first hit of a new visit, if auto-login is not enabled, a returning visitor can be **recognized** (but not authenticated) based on their visitor cookie.

**How it works:**
1. Same first-hit condition as auto-login (`visit.pageVisits == 0`).
2. If `AllowAutoLogin` is disabled but `AllowAutoRecognize` is enabled, `AuthController.recognizeById()` recognizes the visitor's known member identity.

**Site property:**

| Property | Default | Effect |
|----------|---------|--------|
| `AllowAutoRecognize` | false | Enable automatic recognition for returning visitors |

Auto-login takes precedence over auto-recognition when both are enabled.

---

## Complete Priority Order

```
Phase 1 — SessionController (passive signals):
  1. Authorization: Bearer <token>       → authenticateById()
  2. Authorization: Basic <b64>          → authenticateById()
  3. Visit cookie → visit.memberId       → carry-forward (auth or recognized)
  4. (none)                              → guest

Phase 2 — AuthEventController (active request signals):
  5. type=login + username/password      → authenticateById()
  6. authAction=login + authUsername/pw  → authenticateById()
  7. eid token + AllowLinkLogin          → authenticateById()
  8. eid token + AllowLinkRecognize      → recognizeById()
  9. first hit + AllowAutoLogin          → authenticateById()
 10. first hit + AllowAutoRecognize      → recognizeById()
 11. (none) → custom "authentication event" addon hook fires
```

---

## Checking Authentication State in Addons

```csharp
// is the user explicitly authenticated?
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
| `SessionController` | `Controllers/SessionController.cs` | Phase 1: resolves user from headers and session cookie |
| `AuthEventController` | `Controllers/Authentication/AuthEventController.cs` | Phase 2: form login, link auth/recognition, auto-login/recognize |
| `AuthController` | `Controllers/Authentication/AuthController.cs` | `authenticateById()`, `recognizeById()`, credential validation |
| `BearerTokenAuthController` | `Controllers/Authentication/BearerTokenAuthController.cs` | Bearer token decrypt, validate, authenticate |
| `AuthWorkflowController` | `Controllers/Authentication/AuthWorkflowController.cs` | Login form rendering and processing |
| `LoginWorkflowController` | `Controllers/Authentication/LoginWorkflowController.cs` | Credential validation for form login |
| `CreateBearerTokenRemoteMethod` | `Addons/Authentication/CreateBearerTokenRemoteMethod.cs` | Remote method to generate bearer tokens |
| `SecurityController` | `Controllers/SecurityController.cs` | Two-way encryption for bearer tokens, cookies, and link tokens |
