using ModelContextProtocol.Server;
using System.ComponentModel;
//
namespace ContensiveMcpServer.Tools;
//
[McpServerToolType]
public class PageTools {
    //
    [McpServerTool, Description("List all pages on the website as a URL-based hierarchy. Returns the current canonical URL for each page, the nav headline, and child pages nested inside. Use the returned URLs with page_get and other tools.")]
    public static async Task<string> page_list(ContensiveClient client) {
        var doc = await client.GetAsync("content-api-page-list");
        return ContensiveClient.FormatResponse(doc);
    }
    //
    [McpServerTool, Description("Get the full content of a page by its URL. Returns page metadata, template name, and all widgets with their current content fields. Always call this before editing so you know what is on the page and have the widget instanceGuids needed for other tools.")]
    public static async Task<string> page_get(
        ContensiveClient client,
        [Description("The page URL, e.g. '/' or '/about' or '/blog/my-post'")] string url) {
        //
        var queryParams = new Dictionary<string, string> { ["url"] = url };
        var doc = await client.GetAsync("content-api-page-get", queryParams);
        return ContensiveClient.FormatResponse(doc);
    }
    //
    [McpServerTool, Description("Create a new child page under an existing page. Returns the new page ID and URL. The URL slug is generated automatically from the navHeadline.")]
    public static async Task<string> page_create(
        ContensiveClient client,
        [Description("URL of the parent page, e.g. '/about'. Use '/' or omit for a root-level page.")] string parentUrl,
        [Description("Navigation headline — used as the page name and to generate the URL slug")] string navHeadline,
        [Description("Headline displayed on the page itself (defaults to navHeadline if omitted)")] string headline = "",
        [Description("Template ID to use. 0 or omit to inherit from the parent page.")] int templateId = 0) {
        //
        var body = new { parentUrl, navHeadline, headline, templateId };
        var doc = await client.PostAsync("content-api-page-create", body);
        return ContensiveClient.FormatResponse(doc);
    }
    //
    [McpServerTool, Description("Add a new URL alias to a page. If makeCanonical is true (default), the new alias becomes the primary URL. If false, the existing canonical URL is preserved and the new alias is added as a redirect.")]
    public static async Task<string> page_alias_add(
        ContensiveClient client,
        [Description("The current page URL used to identify the page, e.g. '/about'")] string url,
        [Description("The new URL alias to add, e.g. '/about-us'")] string newAlias,
        [Description("If true (default), new alias becomes the canonical URL. If false, existing canonical is preserved and new alias is a redirect.")] bool makeCanonical = true) {
        //
        var body = new { url, newAlias, makeCanonical };
        var doc = await client.PostAsync("content-api-page-alias-add", body);
        return ContensiveClient.FormatResponse(doc);
    }
    //
    [McpServerTool, Description("Update metadata on an existing page. Supply only the fields you want to change — omitted fields are left unchanged. Use page_get first to see current values.")]
    public static async Task<string> page_update(
        ContensiveClient client,
        [Description("The page URL, e.g. '/about'")] string url,
        [Description("Headline displayed on the page (null to skip)")] string? headline = null,
        [Description("Navigation headline shown in menus (null to skip)")] string? navHeadline = null,
        [Description("Meta description for SEO (null to skip)")] string? metaDescription = null,
        [Description("Meta keywords for SEO, comma-separated (null to skip)")] string? metaKeywordList = null,
        [Description("Structured data JSON-LD for SEO (null to skip)")] string? structuredData = null,
        [Description("Browser tab title (null to skip)")] string? pageTitle = null,
        [Description("Template ID override. 0 to skip.")] int templateId = 0) {
        //
        var body = new Dictionary<string, object> { ["url"] = url };
        if (headline != null) { body["headline"] = headline; }
        if (navHeadline != null) { body["navHeadline"] = navHeadline; }
        if (metaDescription != null) { body["metaDescription"] = metaDescription; }
        if (metaKeywordList != null) { body["metaKeywordList"] = metaKeywordList; }
        if (structuredData != null) { body["structuredData"] = structuredData; }
        if (pageTitle != null) { body["pageTitle"] = pageTitle; }
        if (templateId > 0) { body["templateId"] = templateId; }
        //
        var doc = await client.PostAsync("content-api-page-update", body);
        return ContensiveClient.FormatResponse(doc);
    }
}
