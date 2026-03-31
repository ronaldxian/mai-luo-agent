using System.Text.Json.Serialization;

namespace OcCli.Models;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(RegistryData))]
[JsonSerializable(typeof(SessionRegistryEntry))]
[JsonSerializable(typeof(CreateGistRequest))]
[JsonSerializable(typeof(UpdateGistRequest))]
[JsonSerializable(typeof(GistResponse))]
[JsonSerializable(typeof(GistFileResponse))]
[JsonSerializable(typeof(GistFileContent))]
[JsonSerializable(typeof(GistInfo))]
[JsonSerializable(typeof(GistDetail))]
[JsonSerializable(typeof(GistFileInfo))]
[JsonSerializable(typeof(SessionInfo))]
[JsonSerializable(typeof(List<SessionInfo>))]
[JsonSerializable(typeof(List<GistResponse>))]
[JsonSerializable(typeof(Dictionary<string, SessionRegistryEntry>))]
[JsonSerializable(typeof(Dictionary<string, GistFileContent>))]
[JsonSerializable(typeof(Dictionary<string, GistFileInfo>))]
[JsonSerializable(typeof(Dictionary<string, GistFileResponse>))]
internal partial class OcCliJsonContext : JsonSerializerContext { }
