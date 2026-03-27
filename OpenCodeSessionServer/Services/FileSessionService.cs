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
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        var directory = Path.Combine(_rootPath, today);
        Directory.CreateDirectory(directory);

        var timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        var fileName = $"{request.SessionId}_{timestamp}.json";
        var filePath = Path.Combine(directory, fileName);

        var jsonData = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(request.Data));

        var sessionData = new SessionFileData
        {
            SessionId = request.SessionId,
            Data = jsonData,
            TimeCreated = timestamp,
            TimeUpdated = timestamp
        };

        var json = JsonSerializer.Serialize(sessionData, _jsonOptions);
        await File.WriteAllTextAsync(filePath, json);

        return new UploadResponse(fileName.Replace(".json", ""), "Session uploaded successfully");
    }

    public Task<List<SessionInfo>> ListTodaySessionsAsync()
    {
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        var directory = Path.Combine(_rootPath, today);

        var sessions = new List<SessionInfo>();

        if (Directory.Exists(directory))
        {
            var files = Directory.GetFiles(directory, "*.json");
            foreach (var file in files)
            {
                try
                {
                    var content = File.ReadAllText(file);
                    var data = JsonSerializer.Deserialize<SessionFileData>(content, _jsonOptions);
                    if (data != null)
                    {
                        sessions.Add(new SessionInfo(
                            Path.GetFileNameWithoutExtension(file),
                            data.SessionId,
                            data.Title ?? data.SessionId,
                            data.TimeCreated,
                            data.TimeUpdated
                        ));
                    }
                }
                catch
                {
                    // Skip invalid files
                }
            }
        }

        sessions.Sort((a, b) => b.TimeCreated.CompareTo(a.TimeCreated));
        return Task.FromResult(sessions);
    }

    public async Task<DownloadResponse> DownloadAsync(string id)
    {
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        var directory = Path.Combine(_rootPath, today);
        var filePath = Path.Combine(directory, $"{id}.json");

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Session not found: {id}");
        }

        var content = await File.ReadAllTextAsync(filePath);
        var data = JsonSerializer.Deserialize<SessionFileData>(content, _jsonOptions);

        if (data == null)
        {
            throw new InvalidOperationException("Invalid session data");
        }

        var base64Data = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(data.Data));
        return new DownloadResponse(data.SessionId, base64Data);
    }

    public Task DeleteAsync(string id)
    {
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        var directory = Path.Combine(_rootPath, today);
        var filePath = Path.Combine(directory, $"{id}.json");

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }

    private sealed class SessionFileData
    {
        public string SessionId { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
        public string? Title { get; set; }
        public long TimeCreated { get; set; }
        public long TimeUpdated { get; set; }
    }
}
