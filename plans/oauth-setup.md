# OAuth Setup for Claude MCP Server

This document describes how to configure the Contensive OAuth 2.0 endpoints so that Claude (claude.ai or Claude Desktop) can authenticate to the Contensive MCP server using the OAuth flow.

## Overview

When you add a remote MCP server in Claude, it requires OAuth 2.0 authentication. Claude will not accept a raw bearer token directly — it needs to go through the standard OAuth Authorization Code flow with PKCE to obtain one.

Contensive provides all five endpoints that Claude needs, served as remote methods by the Contensive website:

| Endpoint | Served By | Purpose |
|----------|-----------|---------|
| `/.well-known/oauth-authorization-server` | Base collection (`aoBase51`) | Discovery — tells Claude where the auth and token endpoints are |
| `/.well-known/oauth-protected-resource` | MCP addon collection (`aoMcp`) | Tells Claude which authorization server protects the `/mcp` resource |
| `/oauth/register` | Base collection (`aoBase51`) | Dynamic Client Registration — Claude registers itself automatically |
| `/oauth/authorize` | Base collection (`aoBase51`) | Authorization — the user logs in and grants access |
| `/oauth/token` | Base collection (`aoBase51`) | Token exchange — Claude swaps the authorization code for a bearer token |

All endpoints are Contensive remote methods served on the same domain as the website. This means subpath deployments (e.g., `https://www.your-site.com/mcp`) work without any extra reverse proxy configuration.

## How the Flow Works

When a user adds the MCP server URL in Claude, the following happens automatically:

```
1. Claude fetches /.well-known/oauth-protected-resource from the Contensive website
   → learns the Contensive website is the authorization server

2. Claude fetches /.well-known/oauth-authorization-server from the Contensive website
   → learns the authorization, token, and registration endpoints

3. Claude POSTs to /oauth/register on the Contensive website
   → registers itself as an OAuth client and receives a client_id

4. Claude opens a browser window to /oauth/authorize on the Contensive website
   → the user sees a login form and signs in with their Contensive username and password

5. After login, Contensive redirects back to Claude with an authorization code

6. Claude POSTs to /oauth/token on the Contensive website
   → exchanges the authorization code for a bearer token (using PKCE verification)

7. Claude includes the bearer token in all subsequent MCP requests
   → the MCP server forwards it to Contensive, which authenticates the user
```

The user only has to sign in once. After that, Claude holds the token and uses it for all MCP tool calls until it expires (1 year).

## Prerequisites

### Contensive Website

- The site must be running the latest base collection (`aoBase51.xml`) which includes the OAuth remote methods and the `OAuth Clients` and `OAuth Authorization Codes` database tables.
- The MCP addon collection (`aoMcp`) must be installed, which provides the `/mcp` endpoint and the `/.well-known/oauth-protected-resource` discovery endpoint.
- The site must be accessible over HTTPS at its public domain (e.g., `https://www.example.com`).
- The user who will authenticate must have a Contensive account with a username and password. The account must be active.
- For MCP content editing tools, the user must have Admin access.

## Step-by-Step Setup

### Step 1 — Verify the Contensive site has the OAuth endpoints

Open a browser and navigate to:

```
https://www.your-site.com/.well-known/oauth-authorization-server
```

You should see a JSON response like:

```json
{
  "issuer": "https://www.your-site.com",
  "authorization_endpoint": "https://www.your-site.com/oauth/authorize",
  "token_endpoint": "https://www.your-site.com/oauth/token",
  "registration_endpoint": "https://www.your-site.com/oauth/register",
  "response_types_supported": ["code"],
  "code_challenge_methods_supported": ["S256"],
  "grant_types_supported": ["authorization_code"],
  "token_endpoint_auth_methods_supported": ["none"]
}
```

If you get a 404, the base collection needs to be updated. Reinstall the latest `aoBase51` collection.

### Step 2 — Verify the protected resource metadata endpoint

This endpoint is served by the Contensive website via the MCP addon collection (`aoMcp`). Navigate to:

```
https://www.your-site.com/.well-known/oauth-protected-resource
```

You should see:

```json
{
  "resource": "https://www.your-site.com/mcp",
  "authorization_servers": ["https://www.your-site.com"]
}
```

If you get a 404, the MCP addon collection needs to be installed or updated. The `/.well-known/oauth-protected-resource` remote method was added in the MCP addon collection — reinstall it.

### Step 3 — Confirm the MCP endpoint returns 401 for unauthenticated requests

The `/mcp` remote method requires admin authentication. When hit without a bearer token, Contensive returns a `401` status. This is what triggers Claude to start the OAuth flow. Test it:

```
curl -I https://www.your-site.com/mcp
```

You should see a `401 Unauthorized` response.

### Step 4 — Ensure the user has a Contensive account

The user who will authenticate through OAuth must have:
- An active People record in Contensive
- A username and password set on that record
- Admin access (for MCP content editing tools)

The OAuth login form uses the same credential validation as the standard Contensive admin login.

### Step 5 — Add the MCP server in Claude

#### Claude.ai (Web)

1. Go to Settings > MCP Servers (or the integrations panel)
2. Click "Add MCP Server" (or "Add Integration")
3. Enter the MCP server URL: `https://www.your-site.com/mcp`
4. Claude will discover the OAuth endpoints automatically
5. A browser window opens showing the Contensive login form
6. Sign in with your Contensive username and password
7. After successful login, you are redirected back to Claude
8. The MCP tools are now available in your conversation

#### Claude Desktop

1. Go to Settings > MCP
2. Click "Add MCP Server"
3. Choose "Streamable HTTP"
4. Enter the URL: `https://www.your-site.com/mcp`
5. Claude Desktop will detect the 401 response and start the OAuth flow
6. A browser window opens showing the Contensive login form
7. Sign in and you are redirected back
8. The MCP tools appear in the tool list

#### Claude Code (CLI)

Add to your Claude Code MCP settings:

```json
{
  "mcpServers": {
    "contensive": {
      "url": "https://www.your-site.com/mcp"
    }
  }
}
```

Note: no `Authorization` header is needed — Claude Code will handle the OAuth flow interactively when it first connects.

## How Each Endpoint Works

### `/.well-known/oauth-protected-resource` (MCP Addon Collection)

**Source:** `aoMcp/server/Mcp/OAuthProtectedResourceRemoteMethod.cs`

Served by the Contensive website via the MCP addon collection. Returns a JSON document (RFC 9728) that tells Claude which authorization server to use. The `resource` field is the `/mcp` endpoint URL on the same domain. The `authorization_servers` field points to the Contensive website itself (same domain).

### `/.well-known/oauth-authorization-server` (Base Collection)

**Source:** [OAuthMetadataRemoteMethod.cs](source/Processor/Addons/OAuth/OAuthMetadataRemoteMethod.cs)

Served by the Contensive website via the base collection. Returns OAuth Authorization Server Metadata (RFC 8414) pointing to the authorization, token, and registration endpoints.

### `/oauth/register` (Contensive)

**Source:** [OAuthRegisterRemoteMethod.cs](source/Processor/Addons/OAuth/OAuthRegisterRemoteMethod.cs)

Implements RFC 7591 Dynamic Client Registration. Claude POSTs a JSON body with `redirect_uris`, `client_name`, and other optional fields. Contensive generates a unique `client_id`, stores the registration in the `ccOAuthClients` table, and returns the client metadata.

This endpoint does not require authentication — registration is open so that new MCP clients can self-register before the user has logged in.

**Database table:** `ccOAuthClients` ([OAuthClientModel.cs](source/Models/Models/Db/OAuthClientModel.cs))

### `/oauth/authorize` (Contensive)

**Source:** [OAuthAuthorizeRemoteMethod.cs](source/Processor/Addons/OAuth/OAuthAuthorizeRemoteMethod.cs)

The authorization endpoint. Claude redirects the user's browser here with these query parameters:

| Parameter | Value |
|-----------|-------|
| `response_type` | `code` |
| `client_id` | The client_id from registration |
| `redirect_uri` | Claude's callback URL |
| `code_challenge` | PKCE challenge (BASE64URL(SHA256(code_verifier))) |
| `code_challenge_method` | `S256` |
| `state` | CSRF protection token |

If the user is not authenticated, Contensive shows a login form. After successful login, it generates a random authorization code, stores it in the `ccOAuthCodes` table with the PKCE challenge and a 5-minute expiry, and redirects the browser back to Claude's `redirect_uri` with the code and state.

**Database table:** `ccOAuthCodes` ([OAuthCodeModel.cs](source/Models/Models/Db/OAuthCodeModel.cs))

### `/oauth/token` (Contensive)

**Source:** [OAuthTokenRemoteMethod.cs](source/Processor/Addons/OAuth/OAuthTokenRemoteMethod.cs)

The token endpoint. Claude POSTs here with:

| Parameter | Value |
|-----------|-------|
| `grant_type` | `authorization_code` |
| `code` | The authorization code received from `/oauth/authorize` |
| `redirect_uri` | Must match the original redirect_uri |
| `code_verifier` | The PKCE verifier (Claude proves it initiated the flow) |
| `client_id` | The registered client_id |

Contensive:
1. Looks up the authorization code in `ccOAuthCodes`
2. Verifies it has not expired (5-minute window)
3. Verifies the `redirect_uri` and `client_id` match
4. Verifies PKCE: computes `BASE64URL(SHA256(code_verifier))` and compares it to the stored `code_challenge`
5. Deletes the code record (single-use)
6. Generates a 40-character random key, stores it on the user's People record (`bearerToken` field)
7. Builds an encrypted bearer token (random key + 1-year expiry, AES encrypted)
8. Returns the token:

```json
{
  "access_token": "<encrypted-bearer-token>",
  "token_type": "Bearer",
  "expires_in": 31536000
}
```

From this point on, Claude includes `Authorization: Bearer <token>` on every MCP request. The MCP server forwards it to Contensive, which decrypts it, matches the random key to a People record, and authenticates the user.

## Important Notes

### One token per user

The OAuth token exchange writes a new `bearerToken` value to the People record. Each user can only have one active bearer token at a time. If a user authenticates via OAuth again, or if someone clicks "Create Bearer Token" in the admin UI for that user, the previous token is invalidated.

### Token expiration

Bearer tokens expire after 1 year. When the token expires, Claude will receive a 401 response and should re-trigger the OAuth flow. The user will need to log in again.

### HTTPS required

All URLs must use HTTPS in production. OAuth authorization codes and bearer tokens are transmitted over these connections. The Contensive website and Claude's redirect URI must both use HTTPS.

### No client secrets

The OAuth implementation uses public clients (`token_endpoint_auth_methods_supported: ["none"]`). This is correct for MCP clients like Claude that cannot securely store a client secret. Security relies on PKCE (S256) to prevent authorization code interception.

### CORS

If Claude.ai (the web app) calls the Contensive OAuth endpoints directly from the browser, the Contensive site may need CORS headers allowing the Claude origin. If the flow is handled via browser redirects (which is the standard OAuth pattern), CORS is not needed.

## Troubleshooting

**Claude says "Unable to connect" or does not show the login page**
- Verify the Contensive website is reachable over HTTPS from the internet
- Check that `https://www.your-site.com/.well-known/oauth-protected-resource` returns valid JSON
- Check that `https://www.your-site.com/.well-known/oauth-authorization-server` returns valid JSON

**The login page appears but login fails**
- The user must have a username and password set on their People record
- Check that the account is active and not expired (`dateExpires` field)
- Try logging in through the normal Contensive admin site to verify credentials work

**Login succeeds but Claude shows an error after redirect**
- Check the Contensive error log for exceptions in the OAuth endpoints
- Verify the authorization code was created in the `ccOAuthCodes` table (admin > OAuth Authorization Codes)
- The code expires after 5 minutes — if there is a delay between login and the token exchange, it may have expired

**"PKCE verification failed"**
- This indicates the `code_verifier` sent to `/oauth/token` does not match the `code_challenge` sent to `/oauth/authorize`. This is handled by Claude automatically and should not occur under normal circumstances. Check for proxy or middleware that may be modifying request parameters.

**MCP tools work initially but stop working later**
- The bearer token may have expired (1-year lifetime) — re-authenticate via OAuth
- Another token may have been created for the same user (via admin UI or another OAuth login), invalidating the current one

**"Admin access required" from MCP tools**
- The OAuth flow authenticates the user, but the user's People record must also have Admin access for the content editing tools to work. Grant Admin access in the Contensive admin site.

**OAuth clients accumulating in the database**
- Each time Claude registers (or re-registers), a new record is created in `ccOAuthClients`. These are harmless but can be periodically cleaned up from the admin site under OAuth Clients.
