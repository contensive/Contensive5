using ModelContextProtocol.Server;
using System.ComponentModel;
//
namespace ContensiveMcpServer.Tools;
//
[McpServerToolType]
public class WidgetTools {
    //
    [McpServerTool, Description("List available widget types (design blocks) that can be added to pages. Returns GUIDs and descriptions needed for page_widget_add.")]
    public static async Task<string> widget_types(ContensiveClient client) {
        var doc = await client.GetAsync("content-api-widget-types");
        return ContensiveClient.FormatResponse(doc);
    }
    //
    [McpServerTool, Description("Add a widget to a page. Use widget_types first to find the designBlockTypeGuid. Returns the new widget's instanceGuid, which you can pass to widget_instance_update to set its content.")]
    public static async Task<string> page_widget_add(
        ContensiveClient client,
        [Description("The page URL, e.g. '/about'")] string url,
        [Description("The widget type GUID from widget_types")] string designBlockTypeGuid,
        [Description("Position (1-based). 0 or omit to append at end.")] int position = 0) {
        //
        var body = new { url, designBlockTypeGuid, position };
        var doc = await client.PostAsync("content-api-page-addonlist-add", body);
        return ContensiveClient.FormatResponse(doc);
    }
    //
    [McpServerTool, Description("Remove a widget from a page. Use page_get first to get the instanceGuid of the widget to remove.")]
    public static async Task<string> page_widget_remove(
        ContensiveClient client,
        [Description("The page URL, e.g. '/about'")] string url,
        [Description("The instance GUID of the widget to remove (from page_get)")] string instanceGuid) {
        //
        var body = new { url, instanceGuid };
        var doc = await client.PostAsync("content-api-page-addonlist-remove", body);
        return ContensiveClient.FormatResponse(doc);
    }
    //
    [McpServerTool, Description("Reorder widgets on a page. Provide instance GUIDs in the desired top-to-bottom order. Use page_get first to see the current widget order and their instanceGuids.")]
    public static async Task<string> page_widget_reorder(
        ContensiveClient client,
        [Description("The page URL, e.g. '/about'")] string url,
        [Description("Instance GUIDs in desired order, comma-separated (from page_get)")] string instanceGuidsCommaSeparated) {
        //
        var instanceGuids = instanceGuidsCommaSeparated.Split(',').Select(g => g.Trim()).ToList();
        var body = new { url, instanceGuids };
        var doc = await client.PostAsync("content-api-page-addonlist-reorder", body);
        return ContensiveClient.FormatResponse(doc);
    }
}
