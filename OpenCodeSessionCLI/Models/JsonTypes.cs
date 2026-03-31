using System.Text.Json.Serialization;

namespace OcCli.Models;

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

public class SessionRegistryEntry
{
    public string GistId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public long UpdatedAt { get; set; }
}

internal class RegistryData
{
    public Dictionary<string, SessionRegistryEntry> Sessions { get; set; } = new();
}

internal class CreateGistRequest
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("public")]
    public bool Public { get; set; }

    [JsonPropertyName("files")]
    public Dictionary<string, GistFileContent> Files { get; set; } = new();
}

internal class UpdateGistRequest
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("files")]
    public Dictionary<string, GistFileContent> Files { get; set; } = new();
}

internal class GistFileContent
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

internal class GistResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("files")]
    public Dictionary<string, GistFileResponse>? Files { get; set; }
}

internal class GistFileResponse
{
    [JsonPropertyName("filename")]
    public string? Filename { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }
}

public class GistInfo
{
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class GistDetail
{
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, GistFileInfo> Files { get; set; } = new();
}

public class GistFileInfo
{
    public string Filename { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
