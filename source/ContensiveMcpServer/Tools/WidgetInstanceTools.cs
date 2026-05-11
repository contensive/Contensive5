using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
//
namespace ContensiveMcpServer.Tools;
//
[McpServerToolType]
public class WidgetInstanceTools {
    //
    [McpServerTool, Description("Update the content fields of a widget on a page. Call page_get first to see the current field values and get the instanceGuid. The url carries the page context — for pages with dynamic content (e.g. blog posts), use the full URL including the post path so the correct content record is targeted.")]
    public static async Task<string> widget_instance_update(
        ContensiveClient client,
        [Description("The page URL where the widget lives, e.g. '/about' or '/blog/my-post'")] string url,
        [Description("The instance GUID of the widget to update (from page_get)")] string instanceGuid,
        [Description("JSON object of field names to new values, e.g. {\"headline\":\"New Title\",\"body\":\"<p>Content</p>\"}")] string fieldsJson) {
        //
        Dictionary<string, string>? fields;
        try {
            fields = JsonSerializer.Deserialize<Dictionary<string, string>>(fieldsJson);
        } catch {
            return "{\"success\":false,\"message\":\"Invalid fieldsJson — must be a JSON object mapping field names to string values.\"}";
        }
        if (fields == null || fields.Count == 0) {
            return "{\"success\":false,\"message\":\"fieldsJson must contain at least one field.\"}";
        }
        //
        var body = new { url, instanceGuid, fields };
        var doc = await client.PostAsync("content-api-widget-instance-update", body);
        return ContensiveClient.FormatResponse(doc);
    }
}
