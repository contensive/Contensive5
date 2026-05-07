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

### Page Management

| Tool | Description |
|------|-------------|
| `page_list` | List pages on the website. Optionally filter by parent page ID. |
| `page_get` | Get a single page by ID, including content, metadata, and widget list. |
| `page_create` | Create a new page with headline, parent, and template. |
| `page_update` | Update a page's headline, body HTML, meta description, title, or link alias. |

### Widget Management

| Tool | Description |
|------|-------------|
| `widget_types` | List available widget types (design blocks) with their GUIDs. |
| `page_widget_list` | Get the ordered list of widgets on a page. |
| `page_widget_add` | Add a widget to a page at a specified position. |
| `page_widget_remove` | Remove a widget from a page by instance GUID. |
| `page_widget_reorder` | Reorder widgets by providing instance GUIDs in desired order. |
| `page_widget_add_column_block` | Add a structural column block (widths must total 12). |
| `page_widget_add_to_column` | Add a widget into a specific column of an existing column block. |

### Widget Instance Content

| Tool | Description |
|------|-------------|
| `widget_instance_get` | Read the content fields of a widget instance. |
| `widget_instance_update` | Update content fields of a widget instance with a JSON field map. |

### Image Library

| Tool | Description |
|------|-------------|
| `image_upload` | Upload an image from base64-encoded content. |
| `image_list` | List images in the library, optionally filtered by folder. |
| `image_alt_text_update` | Update the alt text of a library image. |

## Example Workflow

Once configured, a user can give Claude natural language instructions like:

1. "List all the pages on the site"
2. "Show me what widgets are on page 42"
3. "Add a Hero Block widget to the top of the About Us page"
4. "Update the headline on page 15 to 'Welcome to Our Company'"
5. "Upload this image and set it as the hero image on the home page"

Claude uses the MCP tools to carry out these operations on the live Contensive website. Each user's changes are authenticated with their own bearer token, so the Contensive audit trail tracks who made each change.

## Contensive Remote Method Endpoints

The MCP server calls these remote method endpoints on the Contensive website. They are registered as addons in the base collection (`aoBase51.xml`) and require admin-level bearer token authentication.

| Endpoint | HTTP | Description |
|----------|------|-------------|
| `/content-api-page-list` | GET | List pages |
| `/content-api-page-get` | GET | Get single page |
| `/content-api-page-create` | POST | Create page |
| `/content-api-page-update` | POST | Update page |
| `/content-api-widget-types` | GET | List widget types |
| `/content-api-page-addonlist` | GET | Get page widget list |
| `/content-api-page-addonlist-add` | POST | Add widget to page |
| `/content-api-page-addonlist-remove` | POST | Remove widget |
| `/content-api-page-addonlist-reorder` | POST | Reorder widgets |
| `/content-api-page-addonlist-add-column-block` | POST | Add column block |
| `/content-api-page-addonlist-add-to-column` | POST | Add widget to column |
| `/content-api-widget-instance-get` | GET | Get widget instance content |
| `/content-api-widget-instance-update` | POST | Update widget instance content |
| `/content-api-image-upload` | POST | Upload image |
| `/content-api-image-list` | GET | List library images |
| `/content-api-image-alt-text` | POST | Update image alt text |

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
    PageTools.cs                   Page management MCP tools
    WidgetTools.cs                 Widget placement MCP tools
    WidgetInstanceTools.cs         Widget instance content MCP tools
    ImageTools.cs                  Image library MCP tools
```

## Troubleshooting

**"Admin access required" errors:** The user's bearer token is either missing, expired, or belongs to a non-admin user. Generate a new token for that user's admin account.

**Connection refused from Claude:** Verify the MCP server is running on the remote server and the URL in the Claude config is correct. Check that the firewall allows the connection.

**Connection refused from MCP server to Contensive:** Verify the `Contensive:BaseUrl` in `appsettings.json` is the site's public domain URL (e.g., `https://www.example.com`). Do not use `localhost` — Contensive needs the `Host` header to identify the correct site.

**Tool not found in Claude:** Restart Claude Desktop or Claude Code after updating the MCP server configuration.

**Remote method not found (404):** The Contensive website needs to have the latest base collection installed with the Content API remote methods. Reinstall or update the base collection.
