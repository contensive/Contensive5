using ModelContextProtocol.Server;
using System.ComponentModel;
//
namespace ContensiveMcpServer.Tools;
//
[McpServerToolType]
public class ImageTools {
    //
    [McpServerTool, Description("Upload an image to the site's image library from base64-encoded content.")]
    public static async Task<string> image_upload(
        ContensiveClient client,
        [Description("The filename including extension (e.g. 'hero.jpg')")] string filename,
        [Description("Base64-encoded image content")] string base64Content,
        [Description("Alt text for accessibility")] string altText = "",
        [Description("Library folder ID (0 for default)")] int folderId = 0) {
        //
        var body = new { filename, base64Content, altText, folderId };
        var doc = await client.PostAsync("content-api-image-upload", body);
        return ContensiveClient.FormatResponse(doc);
    }
    //
    [McpServerTool, Description("List images in the site's image library. Optionally filter by folder.")]
    public static async Task<string> image_list(
        ContensiveClient client,
        [Description("Folder ID to filter (0 for all)")] int folderId = 0,
        [Description("Number of images to return")] int pageSize = 50,
        [Description("Page number for pagination")] int pageNumber = 1) {
        //
        var queryParams = new Dictionary<string, string> {
            ["pageSize"] = pageSize.ToString(),
            ["pageNumber"] = pageNumber.ToString()
        };
        if (folderId > 0) { queryParams["folderId"] = folderId.ToString(); }
        //
        var doc = await client.GetAsync("content-api-image-list", queryParams);
        return ContensiveClient.FormatResponse(doc);
    }
    //
    [McpServerTool, Description("Update the alt text of an image in the library.")]
    public static async Task<string> image_alt_text_update(
        ContensiveClient client,
        [Description("The image record ID")] int imageId,
        [Description("New alt text")] string altText) {
        //
        var body = new { imageId, altText };
        var doc = await client.PostAsync("content-api-image-alt-text", body);
        return ContensiveClient.FormatResponse(doc);
    }
}
