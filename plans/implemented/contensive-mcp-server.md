# Contensive MCP Server

The Contensive MCP Server is an ASP.NET Core web application that exposes Contensive website content management operations as MCP (Model Context Protocol) tools over HTTP. It runs on a server accessible to content editors, and multiple users connect to it remotely from Claude Desktop or Claude Code.

## Architecture

```
User A: Claude Desktop ──┐
User B: Claude Desktop ──┤── HTTPS (MCP) ──→ ContensiveMcpServer (ASP.NET Core)
User C: Claude Code    ──┘                           │
                                                     │ HTTPS (public domain)
                                                     v
                                             Contensive Website
                                                     │
                                                     v
                                                 Database
```

The MCP server calls the Contensive website using its **public domain name** (e.g., `https://www.example.com`). This is required because Contensive uses the HTTP `Host` header to identify which site to serve — a single Contensive server can host multiple websites, each with its own domain. Using `localhost` would not route to the correct site.

Each user connects from their local machine using Claude Desktop or Claude Code. Each user authenticates with their own Contensive bearer token, which the MCP server forwards when calling the Contensive remote method endpoints.

### URL-Centric Paradigm

All tools use a **URL as the primary identifier** for every operation. Content managers think in URLs, not internal page IDs. The MCP server resolves each URL to its underlying `(pageId, queryStringSuffix)` context via the link alias table, invisibly.

This means:
- A URL like `/about` maps to a specific Page Content record.
- A URL like `/blog/my-first-post` maps to the same Page Content record (the blog page) but with a `queryStringSuffix` that selects a specific blog post's content.
- You never need to know a `pageId` — just supply the URL you see in the browser.

### Two Template Layers

Pages have two layers of wrapping template:

- **Sub-template** — the Page Content record (`ccpagecontent`). Controls the widget list (`addonList`), parent/child page hierarchy, and nav headline for this page.
- **Main template** — the Page Template record (`cctemplates`). Site-wide shell: navigation, header, footer.

### Multi-Site / Multi-Server Support

Contensive can run multiple websites on the same server or spread across multiple servers. The MCP server supports this:

- **One MCP server per website** — each instance is configured with a specific site's public domain in `Contensive:BaseUrl`
- **The MCP server does not need to run on the same server as the website** — it communicates over HTTP to the site's public URL
- For organizations managing multiple Contensive sites, deploy one MCP server instance per site, each with its own `BaseUrl` and endpoint

## Prerequisites

### On the remote server

- .NET 9.0 runtime or later
- A running Contensive website with the Content API remote methods deployed (included in the base Contensive5 collection)

### For each user

- Claude Desktop or Claude Code
- An admin user account on the Contensive website
- A bearer token for that account

## Build

From the repository root:

```
cd source/ContensiveMcpServer
dotnet build
```

For a self-contained publish (deploy to a server without the .NET SDK):

```
cd source/ContensiveMcpServer
dotnet publish -c Release -r win-x64 --self-contained -o publish
```

## Deploy to the Remote Server

1. Copy the published output to the remote server (e.g., `C:\ContensiveMcpServer\`)

2. Edit `appsettings.json` on the server:

```json
{
  "Contensive": {
    "BaseUrl": "https://www.your-site.com"
  },
  "Urls": "http://localhost:5100"
}
```

| Setting | Description |
|---------|-------------|
| `Contensive:BaseUrl` | The **public domain URL** of the Contensive website (e.g., `https://www.example.com`). The MCP server uses this to call the site's remote method endpoints. Must be the actual domain — not `localhost` — because Contensive uses the `Host` header to route to the correct site in multi-site environments. |
| `Urls` | The local address the MCP server listens on (e.g., `http://localhost:5100`). External users reach it through a reverse proxy or dedicated subdomain — see below. |

3. Run the MCP server:

```
ContensiveMcpServer.exe
```

For production, register it as a Windows service or use IIS as a reverse proxy.

### Run as a Windows Service

```
sc create ContensiveMcpServer binPath="C:\ContensiveMcpServer\ContensiveMcpServer.exe" start=auto
sc start ContensiveMcpServer
```

### Expose to External Users

The MCP server must be reachable from each user's machine over HTTPS. There are three options:

#### Option A: Dedicated subdomain (recommended)

Set up a subdomain like `mcp.example.com` with its own DNS record pointing to the server. Configure IIS or another reverse proxy to forward `https://mcp.example.com` to `http://localhost:5100`.

| | |
|---|---|
| Advantages | Clean URL, standard port 443 works through all firewalls, independently secured, can use separate WAF rules, easy to revoke by removing DNS |
| Disadvantages | Requires DNS record and SSL certificate (wildcard cert works) |

Users configure Claude with: `https://mcp.example.com`

#### Option B: Subpath on existing domain

Configure IIS to reverse proxy a path (e.g., `/mcp`) to `http://localhost:5100`. Users connect to `https://www.example.com/mcp`.

| | |
|---|---|
| Advantages | No extra DNS or certificate, standard port, works through all firewalls |
| Disadvantages | Shares origin with the website (security implications), requires IIS URL Rewrite module |

Users configure Claude with: `https://www.example.com/mcp`

#### Option C: Same domain, different port

Set `Urls` to `https://*:5100` with an SSL certificate, or `http://*:5100` behind a firewall.

| | |
|---|---|
| Advantages | Simple setup, no reverse proxy needed |
| Disadvantages | Non-standard port may be blocked by corporate firewalls, need to open extra port |

Users configure Claude with: `https://www.example.com:5100`

## Generate a Bearer Token

Each user needs their own bearer token tied to their admin account. Use the Contensive admin UI: edit the user's People record and click "Create Bearer Token."

Or use curl (must be authenticated as an admin):

```
curl -X POST https://your-site.com/createBearerToken \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "userId=1"
```

The response returns an encrypted token string. Each user saves their own token for client configuration.

## Configure Claude Desktop (Each User)

Add the MCP server to the Claude Desktop configuration file on the user's local machine.

**Windows:** `%APPDATA%\Claude\claude_desktop_config.json`
**macOS:** `~/Library/Application Support/Claude/claude_desktop_config.json`

```json
{
  "mcpServers": {
    "contensive": {
      "url": "https://mcp.example.com",
      "headers": {
        "Authorization": "Bearer user-specific-token-here"
      }
    }
  }
}
```

Replace `https://mcp.example.com` with the actual URL where the MCP server is reachable (depends on your deployment — subdomain, subpath, or port).

Replace `user-specific-token-here` with the user's own bearer token.

Restart Claude Desktop after saving the configuration.

## Configure Claude Code (Each User)

Add the MCP server to Claude Code settings on the user's local machine:

```json
{
  "mcpServers": {
    "contensive": {
      "url": "https://mcp.example.com",
      "headers": {
        "Authorization": "Bearer user-specific-token-here"
      }
    }
  }
}
```

## Available MCP Tools

All tools identify pages by **URL**, not by internal page ID.

### Page Management

| Tool | Description |
|------|-------------|
| `page_list` | List all pages as a URL-based hierarchy. Returns current canonical URLs with nav headlines and nested children. |
| `page_get` | Get the full content of a page by URL — metadata, template, and all widgets with their current field values. Always call this before editing. |
| `page_create` | Create a new child page under a parent URL. Returns the new page ID and generated URL. |
| `page_update` | Update page metadata (headline, nav headline, meta description, meta keywords, structured data, page title, template). Omit fields to leave them unchanged. |
| `page_alias_add` | Add a new URL alias to a page. If `makeCanonical` is true (default), the new alias becomes the primary URL. If false, the new alias is added as a redirect and the existing canonical URL is preserved. |

### Widget Placement

| Tool | Description |
|------|-------------|
| `widget_types` | List available widget types (design blocks) with their GUIDs. |
| `page_widget_add` | Add a widget to a page at a given position. Returns the new instanceGuid. |
| `page_widget_remove` | Remove a widget from a page by instanceGuid. |
| `page_widget_reorder` | Reorder widgets by providing instanceGuids in the desired order. |

### Widget Content Editing

| Tool | Description |
|------|-------------|
| `widget_instance_update` | Update the content fields of a widget. Supply the page URL, the widget's instanceGuid (from `page_get`), and a JSON map of field names to new values. |

### Resource Library

| Tool | Description |
|------|-------------|
| `resource_list` | List resources in the library. Optional `fileType` filter: `image`, `download`, `video`, or omit for all. Returns type flags (`isImage`, `isDownload`, `isVideo`) for each entry. |
| `resource_upload` | Upload any file from base64-encoded content. File type is detected automatically from the extension. |
| `resource_update` | Update a resource's name, alt text (for images), description, or folder. Omit fields to leave them unchanged. |

## Typical Workflow

Once configured, a user gives Claude natural language instructions:

```
1. page_list                              → find the page URL
2. page_get(url)                          → see current page content and widget instanceGuids
3. widget_instance_update(url, guid, {…}) → edit a widget's content
```

Or to add a new widget:

```
1. widget_types                           → find the designBlockTypeGuid for the widget type
2. page_widget_add(url, typeGuid)         → add widget, returns instanceGuid
3. widget_instance_update(url, guid, {…}) → populate the widget's content
```

**Example — "Add a 3rd bullet to the homepage text":**

```
1. page_get("/")
   → widgets: [{instanceGuid: "abc-123", designBlockTypeName: "Text Block",
                fields: {body: "<ul><li>Simple</li><li>Powerful</li></ul>"}}]
2. widget_instance_update("/", "abc-123",
   {body: "<ul><li>Simple</li><li>Powerful</li><li>Using AI should be simple and easy.</li></ul>"})
```

## Contensive Remote Method Endpoints

The MCP server calls these remote method endpoints on the Contensive website. They are registered as addons in the base collection (`aoBase51.xml`) and require admin-level bearer token authentication.

| Endpoint | HTTP | Description |
|----------|------|-------------|
| `/content-api-page-list` | GET | List pages as URL tree (current link aliases) |
| `/content-api-page-get` | GET | Get page by URL — full unified snapshot |
| `/content-api-page-create` | POST | Create page under a parent URL |
| `/content-api-page-update` | POST | Update page metadata by URL |
| `/content-api-widget-types` | GET | List widget types |
| `/content-api-page-addonlist-add` | POST | Add widget to page by URL |
| `/content-api-page-addonlist-remove` | POST | Remove widget by URL + instanceGuid |
| `/content-api-page-addonlist-reorder` | POST | Reorder widgets by URL |
| `/content-api-widget-instance-update` | POST | Update widget content by URL + instanceGuid |
| `/content-api-resource-list` | GET | List library resources (images, downloads, video, etc.) |
| `/content-api-resource-upload` | POST | Upload a resource file |
| `/content-api-resource-update` | POST | Update resource name, alt text, description, or folder |

## Security

- Each user authenticates with their own bearer token, passed in the `Authorization` header of their MCP client configuration.
- The MCP server forwards the bearer token to the Contensive website, which validates it and identifies the user.
- Tokens are encrypted and have a configurable expiration (default 1 year).
- Store bearer tokens securely. Do not commit them to source control.
- Use HTTPS in production (via IIS reverse proxy or direct certificate configuration) to protect bearer tokens in transit.
- Only grant MCP access to users who should have admin-level content editing permissions.

## Project Structure

```
source/ContensiveMcpServer/
  ContensiveMcpServer.csproj      ASP.NET Core web project
  Program.cs                       Web host with MCP HTTP transport and auth
  ContensiveClient.cs              HTTP client wrapper for Contensive API calls
  ContensiveClientFactory.cs       Per-request client factory (injects user's bearer token)
  appsettings.json                 Server configuration (base URL and listen port)
  Tools/
    PageTools.cs                   Page management MCP tools (page_list, page_get, page_create, page_update)
    WidgetTools.cs                 Widget placement MCP tools (widget_types, page_widget_add/remove/reorder)
    WidgetInstanceTools.cs         Widget content editing (widget_instance_update)
    ImageTools.cs                  Image library MCP tools

source/Processor/Addons/ContentApi/
  UrlResolverHelper.cs             Shared URL → (pageId, queryStringSuffix) resolution
  ContentApiAuth.cs                Admin bearer token authentication
  ContentApiHelper.cs              JSON response helpers
  ContentApiResponse.cs            Response envelope model
  PageListRemoteMethod.cs          Returns current link alias tree
  PageGetRemoteMethod.cs           Returns unified page snapshot by URL
  PageCreateRemoteMethod.cs        Creates page under parent URL
  PageUpdateRemoteMethod.cs        Updates page metadata by URL
  PageAddonListAddRemoteMethod.cs  Adds widget to page by URL
  PageAddonListRemoveRemoteMethod.cs   Removes widget by URL + instanceGuid
  PageAddonListReorderRemoteMethod.cs  Reorders widgets by URL
  WidgetInstanceUpdateRemoteMethod.cs  Updates widget content by URL + instanceGuid
  WidgetTypesRemoteMethod.cs       Lists available widget types
  WidgetInstanceGetRemoteMethod.cs (legacy — superseded by page_get unified snapshot)
  PageAddonListGetRemoteMethod.cs  (legacy — superseded by page_get unified snapshot)
```

## Troubleshooting

**"Admin access required" errors:** The user's bearer token is either missing, expired, or belongs to a non-admin user. Generate a new token for that user's admin account.

**"No page found for url" errors:** The URL does not match any record in the link alias table. Use `page_list` to see available URLs, or verify the URL in the browser matches what you are passing.

**Connection refused from Claude:** Verify the MCP server is running on the remote server and the URL in the Claude config is correct. Check that the firewall allows the connection.

**Connection refused from MCP server to Contensive:** Verify the `Contensive:BaseUrl` in `appsettings.json` is the site's public domain URL (e.g., `https://www.example.com`). Do not use `localhost` — Contensive needs the `Host` header to identify the correct site.

**Tool not found in Claude:** Restart Claude Desktop or Claude Code after updating the MCP server configuration.

**Remote method not found (404):** The Contensive website needs to have the latest base collection installed with the Content API remote methods. Reinstall or update the base collection.
