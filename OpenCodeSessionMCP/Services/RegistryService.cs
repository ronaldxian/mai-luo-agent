using System.Text.Json;
using Serilog;

namespace OpenCodeSessionMCP.Services;

public sealed class RegistryService
{
    private readonly string _registryPath;
    private readonly JsonSerializerOptions _jsonOptions;
    private RegistryData _cache = new();
    private readonly object _lock = new();

    public RegistryService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appDir = Path.Combine(appData, "opencode-sessions");
        Directory.CreateDirectory(appDir);
        _registryPath = Path.Combine(appDir, "registry.json");

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        Load();
    }

    private void Load()
    {
        lock (_lock)
        {
            if (File.Exists(_registryPath))
            {
                try
                {
                    var json = File.ReadAllText(_registryPath);
                    _cache = JsonSerializer.Deserialize<RegistryData>(json, _jsonOptions) ?? new();
                    Log.Logger.Information("Registry loaded. {Count} sessions registered", _cache.Sessions.Count);
                }
                catch (Exception ex)
                {
                    Log.Logger.Error(ex, "Failed to load registry, starting fresh");
                    _cache = new();
                }
            }
        }
    }

    private void Save()
    {
        lock (_lock)
        {
            try
            {
                var json = JsonSerializer.Serialize(_cache, _jsonOptions);
                File.WriteAllText(_registryPath, json);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Failed to save registry");
            }
        }
    }

    public void Register(string sessionId, string gistId, string title)
    {
        lock (_lock)
        {
            _cache.Sessions[sessionId] = new SessionRegistryEntry
            {
                GistId = gistId,
                Title = title,
                UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            Save();
        }
        Log.Logger.Information("Registered session {SessionId} -> Gist {GistId}", sessionId, gistId);
    }

    public bool TryGet(string sessionId, out string gistId)
    {
        lock (_lock)
        {
            if (_cache.Sessions.TryGetValue(sessionId, out var entry))
            {
                gistId = entry.GistId;
                return true;
            }
        }
        gistId = string.Empty;
        return false;
    }

    public string? GetTitle(string sessionId)
    {
        lock (_lock)
        {
            if (_cache.Sessions.TryGetValue(sessionId, out var entry))
            {
                return entry.Title;
            }
        }
        return null;
    }

    public void UpdateTitle(string sessionId, string title)
    {
        lock (_lock)
        {
            if (_cache.Sessions.TryGetValue(sessionId, out var entry))
            {
                entry.Title = title;
                entry.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                Save();
            }
        }
    }

    public void Unregister(string sessionId)
    {
        lock (_lock)
        {
            if (_cache.Sessions.Remove(sessionId))
            {
                Save();
                Log.Logger.Information("Unregistered session {SessionId}", sessionId);
            }
        }
    }

    public IReadOnlyList<(string SessionId, SessionRegistryEntry Entry)> GetAllWithId()
    {
        lock (_lock)
        {
            return _cache.Sessions
                .Select(kv => (kv.Key, kv.Value))
                .ToList();
        }
    }

    [Obsolete("Use GetAllWithId() instead to preserve session ID")]
    public IReadOnlyList<SessionRegistryEntry> GetAll()
    {
        lock (_lock)
        {
            return _cache.Sessions.Values.ToList();
        }
    }

    public IReadOnlyList<string> GetAllSessionIds()
    {
        lock (_lock)
        {
            return _cache.Sessions.Keys.ToList();
        }
    }

    private class RegistryData
    {
        public Dictionary<string, SessionRegistryEntry> Sessions { get; set; } = new();
    }
}

public class SessionRegistryEntry
{
    public string GistId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public long UpdatedAt { get; set; }
}
