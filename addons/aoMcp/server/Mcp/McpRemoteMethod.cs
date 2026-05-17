
using Contensive.BaseClasses;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
//
namespace Contensive.Addons.Mcp {
    //
    /// <summary>
    /// MCP endpoint addon. Handles the /mcp route.
    /// Parses JSON-RPC 2.0 requests and dispatches to the appropriate MCP method.
    /// </summary>
    public class McpRemoteMethod : AddonBaseClass {
        //
        // -- MCP protocol version
        private const string McpProtocolVersion = "2025-03-26";
        //
        // -- tool dispatch table
        private static readonly Dictionary<string, Func<CPBaseClass, Dictionary<string, object>, string>> toolHandlers = new Dictionary<string, Func<CPBaseClass, Dictionary<string, object>, string>>(StringComparer.OrdinalIgnoreCase) {
            ["page_list"] = McpToolMethods.pageList,
            ["page_get"] = McpToolMethods.pageGet,
            ["page_create"] = McpToolMethods.pageCreate,
            ["page_update"] = McpToolMethods.pageUpdate,
            ["page_alias_add"] = McpToolMethods.pageAliasAdd,
            ["widget_types"] = McpToolMethods.widgetTypes,
            ["page_widget_add"] = McpToolMethods.pageWidgetAdd,
            ["page_widget_remove"] = McpToolMethods.pageWidgetRemove,
            ["page_widget_reorder"] = McpToolMethods.pageWidgetReorder,
            ["widget_instance_update"] = McpToolMethods.widgetInstanceUpdate,
            ["resource_list"] = McpToolMethods.resourceList,
            ["resource_upload"] = McpToolMethods.resourceUpload,
            ["resource_update"] = McpToolMethods.resourceUpdate
        };
        //
        public override object Execute(CPBaseClass cp) {
            try {
                cp.Response.SetType("application/json");
                cp.Response.AddHeader("Cache-Control", "no-store");
                //
                // -- require admin auth
                if (!cp.User.IsAdmin) {
                    cp.Response.SetStatus("401 Unauthorized");
                    return jsonRpcError(null, -32001, "Authentication required.");
                }
                //
                string requestBody = cp.Request.Body;
                if (string.IsNullOrEmpty(requestBody)) {
                    return jsonRpcError(null, -32700, "Parse error: empty request body.");
                }
                //
                var request = cp.JSON.Deserialize<JsonRpcRequest>(requestBody);
                if (request == null || string.IsNullOrEmpty(request.method)) {
                    return jsonRpcError(null, -32700, "Parse error: invalid JSON-RPC request.");
                }
                //
                // -- notifications have no id and require no response
                if (request.method == "notifications/initialized") {
                    return "";
                }
                //
                switch (request.method) {
                    case "initialize":
                        return handleInitialize(cp, request);
                    case "tools/list":
                        return handleToolsList(cp, request);
                    case "tools/call":
                        return handleToolsCall(cp, request);
                    case "ping":
                        return jsonRpcSuccess(request.id, new { });
                    default:
                        return jsonRpcError(request.id, -32601, $"Method not found: '{request.method}'.");
                }
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return jsonRpcError(null, -32603, $"Internal error: {ex.Message}");
            }
        }
        //
        // ====================================================================================================
        // -- initialize
        // ====================================================================================================
        //
        private string handleInitialize(CPBaseClass cp, JsonRpcRequest request) {
            var result = new {
                protocolVersion = McpProtocolVersion,
                capabilities = new {
                    tools = new { }
                },
                serverInfo = new {
                    name = "Contensive MCP Server",
                    version = "1.0.0"
                }
            };
            return jsonRpcSuccess(request.id, result);
        }
        //
        // ====================================================================================================
        // -- tools/list
        // ====================================================================================================
        //
        private string handleToolsList(CPBaseClass cp, JsonRpcRequest request) {
            var tools = McpToolDefinitions.getToolDefinitions();
            var result = new {
                tools = tools.Select(t => new {
                    t.name,
                    t.description,
                    t.inputSchema
                }).ToList()
            };
            return jsonRpcSuccess(request.id, result);
        }
        //
        // ====================================================================================================
        // -- tools/call
        // ====================================================================================================
        //
        private string handleToolsCall(CPBaseClass cp, JsonRpcRequest request) {
            //
            // -- extract tool name and arguments from params
            if (request.@params == null) {
                return jsonRpcError(request.id, -32602, "Invalid params: params is required for tools/call.");
            }
            //
            string toolName = "";
            if (request.@params.ContainsKey("name")) {
                toolName = request.@params["name"]?.ToString() ?? "";
            }
            if (string.IsNullOrEmpty(toolName)) {
                return jsonRpcError(request.id, -32602, "Invalid params: 'name' is required.");
            }
            //
            Dictionary<string, object> arguments = null;
            if (request.@params.ContainsKey("arguments") && request.@params["arguments"] != null) {
                //
                // -- arguments may come as a Dictionary or as a serialized object; handle both
                try {
                    string argsJson = cp.JSON.Serialize(request.@params["arguments"]);
                    arguments = cp.JSON.Deserialize<Dictionary<string, object>>(argsJson);
                } catch {
                    arguments = new Dictionary<string, object>();
                }
            }
            if (arguments == null) {
                arguments = new Dictionary<string, object>();
            }
            //
            if (!toolHandlers.TryGetValue(toolName, out var handler)) {
                return jsonRpcError(request.id, -32602, $"Unknown tool: '{toolName}'.");
            }
            //
            string toolResult = handler(cp, arguments);
            //
            var result = new {
                content = new[] {
                    new { type = "text", text = toolResult }
                }
            };
            return jsonRpcSuccess(request.id, result);
        }
        //
        // ====================================================================================================
        // -- JSON-RPC response helpers
        // ====================================================================================================
        //
        private static readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings {
            NullValueHandling = NullValueHandling.Ignore
        };
        //
        private static string jsonRpcSuccess(object id, object result) {
            return JsonConvert.SerializeObject(new JsonRpcResponse {
                id = id,
                result = result
            }, jsonSettings);
        }
        //
        private static string jsonRpcError(object id, int code, string message) {
            return JsonConvert.SerializeObject(new JsonRpcResponse {
                id = id,
                error = new JsonRpcError { code = code, message = message }
            }, jsonSettings);
        }
    }
}
