using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
//
namespace ContensiveMcpServer.Tools;
//
[McpServerToolType]
public class WidgetInstanceTools {
    //
    [McpServerTool, Description("Read the content fields of a widget instance. Use this to see what content a widget currently has.")]
    public static async Task<string> widget_instance_get(
        ContensiveClient client,
        [Description("The widget type GUID (from widget_types)")] string designBlockTypeGuid,
        [Description("The instance GUID (from page_widget_list)")] string instanceGuid) {
        //
        var queryParams = new Dictionary<string, string> {
            ["designBlockTypeGuid"] = designBlockTypeGuid,
            ["instanceGuid"] = instanceGuid
        };
        var doc = await client.GetAsync("content-api-widget-instance-get", queryParams);
        return ContensiveClient.FormatResponse(doc);
    }
    //
    [McpServerTool, Description("Update content fields of a widget instance. Provide a JSON object of field names to values.")]
    public static async Task<string> widget_instance_update(
        ContensiveClient client,
        [Description("The widget type GUID")] string designBlockTypeGuid,
        [Description("The instance GUID")] string instanceGuid,
        [Description("JSON object of field names to values, e.g. {\"headline\":\"New Title\",\"body\":\"<p>Content</p>\"}")] string fieldsJson) {
        //
        Dictionary<string, string>? fields;
        try {
            fields = JsonSerializer.Deserialize<Dictionary<string, string>>(fieldsJson);
        } catch {
            return "{\"success\":false,\"message\":\"Invalid fieldsJson. Must be a JSON object of field names to string values.\"}";
        }
        //
        var body = new { designBlockTypeGuid, instanceGuid, fields };
        var doc = await client.PostAsync("content-api-widget-instance-update", body);
        return ContensiveClient.FormatResponse(doc);
    }
}
