using ModelContextProtocol.Server;
using System.ComponentModel;
//
namespace ContensiveMcpServer.Tools;
//
[McpServerToolType]
public class WidgetTools {
    //
    [McpServerTool, Description("List available widget types (design blocks) that can be added to pages. Returns GUIDs needed for adding widgets.")]
    public static async Task<string> widget_types(ContensiveClient client) {
        var doc = await client.GetAsync("content-api-widget-types");
        return ContensiveClient.FormatResponse(doc);
    }
    //
    [McpServerTool, Description("Get the ordered list of widgets currently on a page, including their type GUIDs and instance GUIDs.")]
    public static async Task<string> page_widget_list(
        ContensiveClient client,
        [Description("The page ID")] int pageId) {
        //
        var queryParams = new Dictionary<string, string> { ["pageId"] = pageId.ToString() };
        var doc = await client.GetAsync("content-api-page-addonlist", queryParams);
        return ContensiveClient.FormatResponse(doc);
    }
    //
    [McpServerTool, Description("Add a widget to a page. Use widget_types to find the designBlockTypeGuid first.")]
    public static async Task<string> page_widget_add(
        ContensiveClient client,
        [Description("The page ID")] int pageId,
        [Description("The widget type GUID from widget_types")] string designBlockTypeGuid,
        [Description("Position (1-based). 0 or omit to append at end.")] int position = 0) {
        //
        var body = new { pageId, designBlockTypeGuid, position };
        var doc = await client.PostAsync("content-api-page-addonlist-add", body);
        return ContensiveClient.FormatResponse(doc);
    }
    //
    [McpServerTool, Description("Remove a widget from a page by its instance GUID.")]
    public static async Task<string> page_widget_remove(
        ContensiveClient client,
        [Description("The page ID")] int pageId,
        [Description("The instance GUID of the widget to remove")] string instanceGuid) {
        //
        var body = new { pageId, instanceGuid };
        var doc = await client.PostAsync("content-api-page-addonlist-remove", body);
        return ContensiveClient.FormatResponse(doc);
    }
    //
    [McpServerTool, Description("Reorder widgets on a page by providing instance GUIDs in the desired order.")]
    public static async Task<string> page_widget_reorder(
        ContensiveClient client,
        [Description("The page ID")] int pageId,
        [Description("Instance GUIDs in desired order, comma-separated")] string instanceGuidsCommaSeparated) {
        //
        var instanceGuids = instanceGuidsCommaSeparated.Split(',').Select(g => g.Trim()).ToList();
        var body = new { pageId, instanceGuids };
        var doc = await client.PostAsync("content-api-page-addonlist-reorder", body);
        return ContensiveClient.FormatResponse(doc);
    }
    //
    [McpServerTool, Description("Add a structural column block to a page. Column widths must total 12 (e.g. 6,6 for two equal columns or 4,4,4 for three).")]
    public static async Task<string> page_widget_add_column_block(
        ContensiveClient client,
        [Description("The page ID")] int pageId,
        [Description("The column block widget type GUID")] string designBlockTypeGuid,
        [Description("Column widths comma-separated, must total 12 (e.g. '6,6' or '4,4,4')")] string columnWidthsCommaSeparated,
        [Description("Position (1-based). 0 or omit to append.")] int position = 0) {
        //
        var columnWidths = columnWidthsCommaSeparated.Split(',').Select(w => int.Parse(w.Trim())).ToList();
        var body = new { pageId, designBlockTypeGuid, columnWidths, position };
        var doc = await client.PostAsync("content-api-page-addonlist-add-column-block", body);
        return ContensiveClient.FormatResponse(doc);
    }
    //
    [McpServerTool, Description("Add a widget into a specific column of an existing column block.")]
    public static async Task<string> page_widget_add_to_column(
        ContensiveClient client,
        [Description("The page ID")] int pageId,
        [Description("Instance GUID of the parent column block")] string parentInstanceGuid,
        [Description("Column index (0-based)")] int columnIndex,
        [Description("Widget type GUID to add")] string designBlockTypeGuid,
        [Description("Position within the column (1-based). 0 to append.")] int position = 0) {
        //
        var body = new { pageId, parentInstanceGuid, columnIndex, designBlockTypeGuid, position };
        var doc = await client.PostAsync("content-api-page-addonlist-add-to-column", body);
        return ContensiveClient.FormatResponse(doc);
    }
}
