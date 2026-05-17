
using System.Collections.Generic;
//
namespace Contensive.Addons.Mcp {
    //
    /// <summary>
    /// Static tool definitions for the MCP tools/list response.
    /// Each entry provides the tool name, description, and JSON Schema for its input parameters.
    /// </summary>
    public static class McpToolDefinitions {
        //
        public static List<McpToolDefinition> getToolDefinitions() {
            return new List<McpToolDefinition> {
                //
                // -- page tools
                //
                new McpToolDefinition {
                    name = "page_list",
                    description = "List all pages on the website as a URL-based hierarchy. Returns the current canonical URL for each page, the nav headline, and child pages nested inside. Use the returned URLs with page_get and other tools.",
                    inputSchema = new {
                        type = "object",
                        properties = new { },
                        required = new string[] { }
                    }
                },
                new McpToolDefinition {
                    name = "page_get",
                    description = "Get the full content of a page by its URL. Returns page metadata, template name, and all widgets with their current content fields. Always call this before editing so you know what is on the page and have the widget instanceGuids needed for other tools.",
                    inputSchema = new {
                        type = "object",
                        properties = new {
                            url = new { type = "string", description = "The page URL, e.g. '/' or '/about' or '/blog/my-post'" }
                        },
                        required = new[] { "url" }
                    }
                },
                new McpToolDefinition {
                    name = "page_create",
                    description = "Create a new child page under an existing page. Returns the new page ID and URL. The URL slug is generated automatically from the navHeadline.",
                    inputSchema = new {
                        type = "object",
                        properties = new {
                            parentUrl = new { type = "string", description = "URL of the parent page, e.g. '/about'. Use '/' or omit for a root-level page." },
                            navHeadline = new { type = "string", description = "Navigation headline — used as the page name and to generate the URL slug" },
                            headline = new { type = "string", description = "Headline displayed on the page itself (defaults to navHeadline if omitted)" },
                            templateId = new { type = "integer", description = "Template ID to use. 0 or omit to inherit from the parent page." }
                        },
                        required = new[] { "parentUrl", "navHeadline" }
                    }
                },
                new McpToolDefinition {
                    name = "page_update",
                    description = "Update metadata on an existing page. Supply only the fields you want to change — omitted fields are left unchanged. Use page_get first to see current values.",
                    inputSchema = new {
                        type = "object",
                        properties = new {
                            url = new { type = "string", description = "The page URL, e.g. '/about'" },
                            headline = new { type = "string", description = "Headline displayed on the page (null to skip)" },
                            navHeadline = new { type = "string", description = "Navigation headline shown in menus (null to skip)" },
                            metaDescription = new { type = "string", description = "Meta description for SEO (null to skip)" },
                            metaKeywordList = new { type = "string", description = "Meta keywords for SEO, comma-separated (null to skip)" },
                            structuredData = new { type = "string", description = "Structured data JSON-LD for SEO (null to skip)" },
                            pageTitle = new { type = "string", description = "Browser tab title (null to skip)" },
                            templateId = new { type = "integer", description = "Template ID override. 0 to skip." }
                        },
                        required = new[] { "url" }
                    }
                },
                new McpToolDefinition {
                    name = "page_alias_add",
                    description = "Add a new URL alias to a page. If makeCanonical is true (default), the new alias becomes the primary URL. If false, the existing canonical URL is preserved and the new alias is added as a redirect.",
                    inputSchema = new {
                        type = "object",
                        properties = new {
                            url = new { type = "string", description = "The current page URL used to identify the page, e.g. '/about'" },
                            newAlias = new { type = "string", description = "The new URL alias to add, e.g. '/about-us'" },
                            makeCanonical = new { type = "boolean", description = "If true (default), new alias becomes the canonical URL. If false, existing canonical is preserved and new alias is a redirect." }
                        },
                        required = new[] { "url", "newAlias" }
                    }
                },
                //
                // -- widget tools
                //
                new McpToolDefinition {
                    name = "widget_types",
                    description = "List available widget types (design blocks) that can be added to pages. Returns GUIDs and descriptions needed for page_widget_add.",
                    inputSchema = new {
                        type = "object",
                        properties = new { },
                        required = new string[] { }
                    }
                },
                new McpToolDefinition {
                    name = "page_widget_add",
                    description = "Add a widget to a page. Use widget_types first to find the designBlockTypeGuid. Returns the new widget's instanceGuid, which you can pass to widget_instance_update to set its content.",
                    inputSchema = new {
                        type = "object",
                        properties = new {
                            url = new { type = "string", description = "The page URL, e.g. '/about'" },
                            designBlockTypeGuid = new { type = "string", description = "The widget type GUID from widget_types" },
                            position = new { type = "integer", description = "Position (1-based). 0 or omit to append at end." }
                        },
                        required = new[] { "url", "designBlockTypeGuid" }
                    }
                },
                new McpToolDefinition {
                    name = "page_widget_remove",
                    description = "Remove a widget from a page. Use page_get first to get the instanceGuid of the widget to remove.",
                    inputSchema = new {
                        type = "object",
                        properties = new {
                            url = new { type = "string", description = "The page URL, e.g. '/about'" },
                            instanceGuid = new { type = "string", description = "The instance GUID of the widget to remove (from page_get)" }
                        },
                        required = new[] { "url", "instanceGuid" }
                    }
                },
                new McpToolDefinition {
                    name = "page_widget_reorder",
                    description = "Reorder widgets on a page. Provide instance GUIDs in the desired top-to-bottom order. Use page_get first to see the current widget order and their instanceGuids.",
                    inputSchema = new {
                        type = "object",
                        properties = new {
                            url = new { type = "string", description = "The page URL, e.g. '/about'" },
                            instanceGuidsCommaSeparated = new { type = "string", description = "Instance GUIDs in desired order, comma-separated (from page_get)" }
                        },
                        required = new[] { "url", "instanceGuidsCommaSeparated" }
                    }
                },
                //
                // -- widget instance tools
                //
                new McpToolDefinition {
                    name = "widget_instance_update",
                    description = "Update the content fields of a widget on a page. Call page_get first to see the current field values and get the instanceGuid. The url carries the page context — for pages with dynamic content (e.g. blog posts), use the full URL including the post path so the correct content record is targeted.",
                    inputSchema = new {
                        type = "object",
                        properties = new {
                            url = new { type = "string", description = "The page URL where the widget lives, e.g. '/about' or '/blog/my-post'" },
                            instanceGuid = new { type = "string", description = "The instance GUID of the widget to update (from page_get)" },
                            fieldsJson = new { type = "string", description = "JSON object of field names to new values, e.g. {\"headline\":\"New Title\",\"body\":\"<p>Content</p>\"}" }
                        },
                        required = new[] { "url", "instanceGuid", "fieldsJson" }
                    }
                },
                //
                // -- resource tools
                //
                new McpToolDefinition {
                    name = "resource_list",
                    description = "List resources in the site's library. Optionally filter by fileType ('image', 'download', 'video', or omit for all). Returns id, name, filename, altText, fileTypeName, isImage, isDownload, isVideo for each resource.",
                    inputSchema = new {
                        type = "object",
                        properties = new {
                            fileType = new { type = "string", description = "Filter by type: 'image', 'download', 'video', or omit for all" },
                            folderId = new { type = "integer", description = "Folder ID to filter (0 for all)" },
                            pageSize = new { type = "integer", description = "Number of resources to return (default 50)" },
                            pageNumber = new { type = "integer", description = "Page number for pagination (default 1)" }
                        },
                        required = new string[] { }
                    }
                },
                new McpToolDefinition {
                    name = "resource_upload",
                    description = "Upload a file to the site's resource library from base64-encoded content. File type (image, download, video, etc.) is detected automatically from the filename extension.",
                    inputSchema = new {
                        type = "object",
                        properties = new {
                            filename = new { type = "string", description = "The filename including extension, e.g. 'hero.jpg' or 'brochure.pdf'" },
                            base64Content = new { type = "string", description = "Base64-encoded file content" },
                            name = new { type = "string", description = "Display name for the resource (defaults to filename if omitted)" },
                            altText = new { type = "string", description = "Alt text for accessibility — used when the resource is an image" },
                            description = new { type = "string", description = "Optional description" },
                            folderId = new { type = "integer", description = "Library folder ID (0 for default)" }
                        },
                        required = new[] { "filename", "base64Content" }
                    }
                },
                new McpToolDefinition {
                    name = "resource_update",
                    description = "Update a resource in the library. Supply only the fields you want to change — omitted fields are left unchanged. Use resource_list to find the resource id.",
                    inputSchema = new {
                        type = "object",
                        properties = new {
                            id = new { type = "integer", description = "The resource record ID (from resource_list)" },
                            name = new { type = "string", description = "New display name (null to skip)" },
                            altText = new { type = "string", description = "New alt text for images (null to skip)" },
                            description = new { type = "string", description = "New description (null to skip)" },
                            folderId = new { type = "integer", description = "New folder ID (0 to skip)" }
                        },
                        required = new[] { "id" }
                    }
                }
            };
        }
    }
}
