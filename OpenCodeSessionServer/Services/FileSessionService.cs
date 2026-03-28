using System.Text.Json;
using OpenCodeSessionServer.Models;

namespace OpenCodeSessionServer.Services;

public sealed class FileSessionService : ISessionService
{
    private readonly string _rootPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public FileSessionService(string rootPath)
    {
        _rootPath = rootPath;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<UploadResponse> UploadAsync(UploadRequest request)
    {
        var sessionDir = Path.Combine(_rootPath, request.SessionId);
        Directory.CreateDirectory(sessionDir);

        var timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        var sessionFilePath = Path.Combine(sessionDir, $"{request.SessionId}.json");
        var jsonData = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(request.Data));
        await File.WriteAllTextAsync(sessionFilePath, jsonData);

        var metadata = new
        {
            SessionId = request.SessionId,
            Title = request.Title,
            TimeCreated = timestamp,
            TimeUpdated = timestamp
        };
        await File.WriteAllTextAsync(Path.Combine(sessionDir, "metadata.json"), JsonSerializer.Serialize(metadata, _jsonOptions));

        return new UploadResponse(request.SessionId, "Session uploaded successfully");
    }

    public Task<List<SessionInfo>> ListSessionsAsync()
    {
        var sessions = new List<SessionInfo>();

        if (Directory.Exists(_rootPath))
        {
            var sessionDirs = Directory.GetDirectories(_rootPath);
            foreach (var dir in sessionDirs)
            {
                var metadataPath = Path.Combine(dir, "metadata.json");
                if (File.Exists(metadataPath))
                {
                    try
                    {
                        var content = File.ReadAllText(metadataPath);
                        var metadata = JsonSerializer.Deserialize<SessionMetadata>(content, _jsonOptions);
                        if (metadata != null)
                        {
                            sessions.Add(new SessionInfo(
                                metadata.SessionId,
                                metadata.SessionId,
                                metadata.Title ?? metadata.SessionId,
                                metadata.TimeCreated,
                                metadata.TimeUpdated
                            ));
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }

        sessions.Sort((a, b) => b.TimeCreated.CompareTo(a.TimeCreated));
        return Task.FromResult(sessions);
    }

    public async Task<DownloadResponse> DownloadAsync(string id)
    {
        var sessionDir = Path.Combine(_rootPath, id);
        var sessionFilePath = Path.Combine(sessionDir, $"{id}.json");
        var metadataPath = Path.Combine(sessionDir, "metadata.json");

        if (!File.Exists(sessionFilePath))
        {
            throw new FileNotFoundException($"Session not found: {id}");
        }

        var content = await File.ReadAllTextAsync(sessionFilePath);
        var base64Data = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(content));

        string title = id;
        if (File.Exists(metadataPath))
        {
            try
            {
                var metadataContent = File.ReadAllText(metadataPath);
                var metadata = JsonSerializer.Deserialize<SessionMetadata>(metadataContent, _jsonOptions);
                if (metadata != null)
                {
                    title = metadata.Title ?? id;
                }
            }
            catch
            {
            }
        }

        return new DownloadResponse(id, title, base64Data);
    }

    public Task DeleteAsync(string id)
    {
        var sessionDir = Path.Combine(_rootPath, id);

        if (Directory.Exists(sessionDir))
        {
            Directory.Delete(sessionDir, true);
        }

        return Task.CompletedTask;
    }

    private sealed class SessionMetadata
    {
        public string SessionId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public long TimeCreated { get; set; }
        public long TimeUpdated { get; set; }
    }
}
