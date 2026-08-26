# OAuth Setup for Claude MCP Server

This document describes how to configure the Contensive OAuth 2.0 endpoints so that Claude (claude.ai or Claude Desktop) can authenticate to the Contensive MCP server using the OAuth flow.

## Overview

When you add a remote MCP server in Claude, it requires OAuth 2.0 authentication. Claude will not accept a raw bearer token directly — it needs to go through the standard OAuth Authorization Code flow with PKCE to obtain one.

Contensive provides the four endpoints that Claude needs:

| Endpoint | Purpose |
|----------|---------|
| `/.well-known/oauth-authorization-server` | Discovery — tells Claude where the auth and token endpoints are |
| `/.well-known/oauth-protected-resource` | Tells Claude which authorization server protects this MCP resource |
| `/oauth/register` | Dynamic Client Registration — Claude registers itself automatically |
| `/oauth/authorize` | Authorization — the user logs in and grants access |
| `/oauth/token` | Token exchange — Claude swaps the authorization code for a bearer token |

The first two endpoints are served by the MCP server itself. The remaining three are Contensive remote methods served by the Contensive website.

## How the Flow Works

When a user adds the MCP server URL in Claude, the following happens automatically:

```
1. Claude fetches /.well-known/oauth-protected-resource from the MCP server
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
- The site must be accessible over HTTPS at its public domain (e.g., `https://www.example.com`).
- The user who will authenticate must have a Contensive account with a username and password. The account must be active.
- For MCP content editing tools, the user must have Admin access.

### MCP Server

- The MCP server must be deployed and reachable over HTTPS from the internet (Claude.ai needs to reach it).
- `appsettings.json` must have `Contensive:BaseUrl` set to the Contensive website's public URL.
- If the MCP server's public URL differs from its listen address (e.g., behind a reverse proxy), set `Contensive:McpPublicUrl` to the public URL.

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

### Step 2 — Verify the MCP server has the protected resource metadata

Navigate to:

```
https://mcp.your-site.com/.well-known/oauth-protected-resource
```

You should see:

```json
{
  "resource": "https://mcp.your-site.com",
  "authorization_servers": ["https://www.your-site.com"]
}
```

The `authorization_servers` value must match the Contensive website's public URL (the `Contensive:BaseUrl` from `appsettings.json`).

### Step 3 — Confirm the MCP server returns 401 for unauthenticated requests

The MCP server must return a `401` status with a `WWW-Authenticate` header for unauthenticated requests. This is what triggers Claude to start the OAuth flow. Test it:

```
curl -I https://mcp.your-site.com/mcp
```

You should see:

```
HTTP/1.1 401 Unauthorized
WWW-Authenticate: Bearer realm="Contensive", resource_metadata="https://mcp.your-site.com/.well-known/oauth-protected-resource"
```

This is already implemented in the MCP server's `Program.cs` middleware.

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
3. Enter the MCP server URL: `https://mcp.your-site.com`
4. Claude will discover the OAuth endpoints automatically
5. A browser window opens showing the Contensive login form
6. Sign in with your Contensive username and password
7. After successful login, you are redirected back to Claude
8. The MCP tools are now available in your conversation

#### Claude Desktop

1. Go to Settings > MCP
2. Click "Add MCP Server"
3. Choose "Streamable HTTP"
4. Enter the URL: `https://mcp.your-site.com/mcp`
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
      "url": "https://mcp.your-site.com/mcp"
    }
  }
}
```

Note: no `Authorization` header is needed — Claude Code will handle the OAuth flow interactively when it first connects.

## MCP Server Configuration Reference

The MCP server `appsettings.json` settings relevant to OAuth:

```json
{
  "Contensive": {
    "BaseUrl": "https://www.your-site.com",
    "McpPublicUrl": "https://mcp.your-site.com"
  },
  "Urls": "http://localhost:5100"
}
```

| Setting | Required | Description |
|---------|----------|-------------|
| `Contensive:BaseUrl` | Yes | The Contensive website's public URL. Used as the `authorization_servers` value in the protected resource metadata, and as the base for all OAuth endpoint URLs. |
| `Contensive:McpPublicUrl` | No | The MCP server's public URL as seen by Claude. Only needed if the MCP server is behind a reverse proxy and its listen address differs from its public URL. Used as the `resource` value in the protected resource metadata. Defaults to the first URL in `Urls`. |
| `Urls` | Yes | The local address the MCP server listens on. |

## How Each Endpoint Works

### `/.well-known/oauth-protected-resource` (MCP Server)

**Source:** [Program.cs:95-102](source/ContensiveMcpServer/Program.cs#L95-L102)

Served by the MCP server. Returns a JSON document (RFC 9728) that tells Claude which authorization server to use. The `resource` field is the MCP server's own public URL. The `authorization_servers` field points to the Contensive website.

### `/.well-known/oauth-authorization-server` (Contensive + MCP Server)

**Source:** [OAuthMetadataRemoteMethod.cs](source/Processor/Addons/OAuth/OAuthMetadataRemoteMethod.cs), [Program.cs:106-119](source/ContensiveMcpServer/Program.cs#L106-L119)

Both the Contensive website and the MCP server serve this endpoint. The MCP server's copy is a convenience — some clients look for it on the MCP server before following the `authorization_servers` pointer. Both return the same metadata pointing to the Contensive OAuth endpoints.

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

All URLs must use HTTPS in production. OAuth authorization codes and bearer tokens are transmitted over these connections. The Contensive website, the MCP server's public URL, and Claude's redirect URI must all use HTTPS.

### No client secrets

The OAuth implementation uses public clients (`token_endpoint_auth_methods_supported: ["none"]`). This is correct for MCP clients like Claude that cannot securely store a client secret. Security relies on PKCE (S256) to prevent authorization code interception.

### CORS

If Claude.ai (the web app) calls the Contensive OAuth endpoints directly from the browser, the Contensive site may need CORS headers allowing the Claude origin. If the flow is handled via browser redirects (which is the standard OAuth pattern), CORS is not needed.

## Troubleshooting

**Claude says "Unable to connect" or does not show the login page**
- Verify the MCP server is reachable over HTTPS from the internet
- Check that `/.well-known/oauth-protected-resource` returns valid JSON
- Check that the `Contensive:BaseUrl` in the MCP server config matches the actual site URL

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
