using ModelContextProtocol.Server;
using System.ComponentModel;
//
namespace ContensiveMcpServer.Tools;
//
[McpServerToolType]
public class ResourceTools {
    //
    [McpServerTool, Description("List resources in the site's library. Optionally filter by fileType ('image', 'download', 'video', or omit for all). Returns id, name, filename, altText, fileTypeName, isImage, isDownload, isVideo for each resource.")]
    public static async Task<string> resource_list(
        ContensiveClient client,
        [Description("Filter by type: 'image', 'download', 'video', or omit for all")] string fileType = "",
        [Description("Folder ID to filter (0 for all)")] int folderId = 0,
        [Description("Number of resources to return (default 50)")] int pageSize = 50,
        [Description("Page number for pagination (default 1)")] int pageNumber = 1) {
        //
        var queryParams = new Dictionary<string, string> {
            ["pageSize"] = pageSize.ToString(),
            ["pageNumber"] = pageNumber.ToString()
        };
        if (folderId > 0) { queryParams["folderId"] = folderId.ToString(); }
        if (!string.IsNullOrEmpty(fileType)) { queryParams["fileType"] = fileType; }
        //
        var doc = await client.GetAsync("content-api-resource-list", queryParams);
        return ContensiveClient.FormatResponse(doc);
    }
    //
    [McpServerTool, Description("Upload a file to the site's resource library from base64-encoded content. File type (image, download, video, etc.) is detected automatically from the filename extension.")]
    public static async Task<string> resource_upload(
        ContensiveClient client,
        [Description("The filename including extension, e.g. 'hero.jpg' or 'brochure.pdf'")] string filename,
        [Description("Base64-encoded file content")] string base64Content,
        [Description("Display name for the resource (defaults to filename if omitted)")] string name = "",
        [Description("Alt text for accessibility — used when the resource is an image")] string altText = "",
        [Description("Optional description")] string description = "",
        [Description("Library folder ID (0 for default)")] int folderId = 0) {
        //
        var body = new { filename, base64Content, name, altText, description, folderId };
        var doc = await client.PostAsync("content-api-resource-upload", body);
        return ContensiveClient.FormatResponse(doc);
    }
    //
    [McpServerTool, Description("Update a resource in the library. Supply only the fields you want to change — omitted fields are left unchanged. Use resource_list to find the resource id.")]
    public static async Task<string> resource_update(
        ContensiveClient client,
        [Description("The resource record ID (from resource_list)")] int id,
        [Description("New display name (null to skip)")] string? name = null,
        [Description("New alt text for images (null to skip)")] string? altText = null,
        [Description("New description (null to skip)")] string? description = null,
        [Description("New folder ID (0 to skip)")] int folderId = 0) {
        //
        var body = new { id, name, altText, description, folderId };
        var doc = await client.PostAsync("content-api-resource-update", body);
        return ContensiveClient.FormatResponse(doc);
    }
}
