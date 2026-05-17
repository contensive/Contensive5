
using System.Collections.Generic;
//
namespace Contensive.Addons.Mcp {
    //
    public class JsonRpcRequest {
        public string jsonrpc { get; set; }
        public string method { get; set; }
        public object id { get; set; }
        public Dictionary<string, object> @params { get; set; }
    }
    //
    public class JsonRpcResponse {
        public string jsonrpc { get; set; } = "2.0";
        public object id { get; set; }
        public object result { get; set; }
        public JsonRpcError error { get; set; }
    }
    //
    public class JsonRpcError {
        public int code { get; set; }
        public string message { get; set; }
    }
    //
    public class McpToolDefinition {
        public string name { get; set; }
        public string description { get; set; }
        public object inputSchema { get; set; }
    }
}
