using System.Text.Json.Serialization;

namespace OpenCodeSessionMCP.Models;

public sealed class SessionInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("time_created")]
    public long TimeCreated { get; set; }

    [JsonPropertyName("project_id")]
    public string? ProjectId { get; set; }
}

public sealed class RemoteSession
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("time_created")]
    public long TimeCreated { get; set; }

    [JsonPropertyName("time_updated")]
    public long TimeUpdated { get; set; }
}

public sealed class SessionListResponse
{
    [JsonPropertyName("sessions")]
    public List<RemoteSession> Sessions { get; set; } = [];
}

public sealed class UploadRequest
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public string Data { get; set; } = string.Empty;
}

public sealed class UploadResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

public sealed class DownloadResponse
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public string Data { get; set; } = string.Empty;
}

public sealed class ExportResult
{
    public bool Success { get; set; }
    public string? RemoteId { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}

public sealed class ImportResult
{
    public bool Success { get; set; }
    public string? SessionId { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}

public sealed class ListRemoteSessionsResult
{
    public bool Success { get; set; }
    public List<RemoteSession> Sessions { get; set; } = [];
    public string? Error { get; set; }
}
