using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenCodeSessionMCP.Models;

public sealed class McpRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public JsonElement? Id { get; set; }

    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    [JsonPropertyName("params")]
    public JsonElement? Params { get; set; }
}

public sealed class McpResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public JsonElement? Id { get; set; }

    [JsonPropertyName("result")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Result { get; set; }

    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public McpError? Error { get; set; }
}

public sealed class McpError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Data { get; set; }
}

public static class McpErrorCodes
{
    public const int ParseError = -32700;
    public const int InvalidRequest = -32600;
    public const int MethodNotFound = -32601;
    public const int InvalidParams = -32602;
    public const int InternalError = -32603;
}

public sealed class McpInitializeResult
{
    [JsonPropertyName("protocolVersion")]
    public string ProtocolVersion { get; set; } = "2024-11-05";

    [JsonPropertyName("capabilities")]
    public McpCapabilities Capabilities { get; set; } = new();

    [JsonPropertyName("serverInfo")]
    public McpServerInfo ServerInfo { get; set; } = new();
}

public sealed class McpCapabilities
{
    [JsonPropertyName("tools")]
    public object? Tools { get; set; }
}

public sealed class McpServerInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "opencode-session-sync";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";
}

public sealed class ToolsResult
{
    [JsonPropertyName("tools")]
    public List<McpTool> Tools { get; set; } = [];
}

public sealed class McpTool
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("inputSchema")]
    public McpInputSchema InputSchema { get; set; } = new();
}

public sealed class McpInputSchema
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "object";

    [JsonPropertyName("properties")]
    public Dictionary<string, McpProperty> Properties { get; set; } = [];
}

public sealed class McpProperty
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "string";

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
