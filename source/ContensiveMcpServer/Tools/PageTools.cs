using ModelContextProtocol.Server;
using System.ComponentModel;
//
namespace ContensiveMcpServer.Tools;
//
[McpServerToolType]
public class PageTools {
    //
    [McpServerTool, Description("List pages on the website. Optionally filter by parent page ID to see child pages.")]
    public static async Task<string> page_list(
        ContensiveClient client,
        [Description("Parent page ID to filter children. Omit or 0 for all pages.")] int parentId = 0,
        [Description("Number of pages to return (default 100)")] int pageSize = 100,
        [Description("Page number for pagination (default 1)")] int pageNumber = 1) {
        //
        var queryParams = new Dictionary<string, string> {
            ["pageSize"] = pageSize.ToString(),
            ["pageNumber"] = pageNumber.ToString()
        };
        if (parentId > 0) { queryParams["parentId"] = parentId.ToString(); }
        //
        var doc = await client.GetAsync("content-api-page-list", queryParams);
        return ContensiveClient.FormatResponse(doc);
    }
    //
    [McpServerTool, Description("Get a single page by ID, including its content, metadata, and widget list.")]
    public static async Task<string> page_get(
        ContensiveClient client,
        [Description("The page ID to retrieve")] int pageId) {
        //
        var queryParams = new Dictionary<string, string> {
            ["pageId"] = pageId.ToString()
        };
        var doc = await client.GetAsync("content-api-page-get", queryParams);
        return ContensiveClient.FormatResponse(doc);
    }
    //
    [McpServerTool, Description("Create a new page on the website.")]
    public static async Task<string> page_create(
        ContensiveClient client,
        [Description("Page headline displayed on the page")] string headline,
        [Description("Page name used internally and in URLs")] string name = "",
        [Description("Parent page ID for site hierarchy (0 for root)")] int parentId = 0,
        [Description("Template ID for the page layout")] int templateId = 0,
        [Description("URL slug for the page")] string linkAlias = "") {
        //
        var body = new {
            headline,
            name = string.IsNullOrEmpty(name) ? headline : name,
            parentId,
            templateId,
            linkAlias
        };
        var doc = await client.PostAsync("content-api-page-create", body);
        return ContensiveClient.FormatResponse(doc);
    }
    //
    [McpServerTool, Description("Update an existing page's content and metadata. Only provide fields you want to change.")]
    public static async Task<string> page_update(
        ContensiveClient client,
        [Description("The page ID to update")] int pageId,
        [Description("New headline (null to skip)")] string? headline = null,
        [Description("New HTML body content (null to skip)")] string? bodyHtml = null,
        [Description("New meta description (null to skip)")] string? metaDescription = null,
        [Description("New page title for browser tab (null to skip)")] string? pageTitle = null,
        [Description("New URL slug (null to skip)")] string? linkAlias = null) {
        //
        var body = new Dictionary<string, object> { ["pageId"] = pageId };
        if (headline != null) { body["headline"] = headline; }
        if (bodyHtml != null) { body["bodyHtml"] = bodyHtml; }
        if (metaDescription != null) { body["metaDescription"] = metaDescription; }
        if (pageTitle != null) { body["pageTitle"] = pageTitle; }
        if (linkAlias != null) { body["linkAlias"] = linkAlias; }
        //
        var doc = await client.PostAsync("content-api-page-update", body);
        return ContensiveClient.FormatResponse(doc);
    }
}
