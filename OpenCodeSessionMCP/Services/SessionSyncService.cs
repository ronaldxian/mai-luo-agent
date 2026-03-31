using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenCodeSessionMCP.Configuration;
using OpenCodeSessionMCP.Models;
using Serilog;

namespace OpenCodeSessionMCP.Services;

public sealed class SessionSyncService
{
    private readonly OpenCodeService _openCodeService;
    private readonly GistService _gistService;
    private readonly RegistryService _registryService;
    private readonly string _workingDirectory;

    public SessionSyncService(
        OpenCodeService openCodeService,
        GistService gistService)
    {
        _openCodeService = openCodeService;
        _gistService = gistService;
        _registryService = new RegistryService();
        _workingDirectory = Directory.GetCurrentDirectory();
    }

    public async Task<ExportResult> ExportAndUploadAsync(
        string sessionId,
        CancellationToken ct = default)
    {
        Log.Logger.Information("Starting session export. SessionId: {SessionId}", sessionId);

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return CreateExportResult("Session ID is required.", false);
        }

        string title;
        try
        {
            var (_, foundTitle) = await GetSessionInfoByPathAsync(null, ct);
            title = foundTitle ?? sessionId;
        }
        catch
        {
            title = sessionId;
        }

        try
        {
            var sessionData = await _openCodeService.ExportSessionAsync(sessionId, ct);
            Log.Logger.Information("Session content length: {Length} characters", sessionData.Length);

            if (_registryService.TryGet(sessionId, out var existingGistId))
            {
                Log.Logger.Information("Updating existing Gist {GistId}", existingGistId);
                var updated = await _gistService.UpdateGistFileAsync(existingGistId, $"{sessionId}.json", sessionData, ct);
                if (!updated)
                {
                    return CreateExportResult("Failed to update Gist", false);
                }
                _registryService.UpdateTitle(sessionId, title);
                Log.Logger.Information("Session updated successfully in Gist {GistId}", existingGistId);
                return new ExportResult
                {
                    Success = true,
                    RemoteId = existingGistId,
                    Message = $"Session updated successfully in Gist: {existingGistId}"
                };
            }
            else
            {
                Log.Logger.Information("Creating new Gist for session {SessionId}", sessionId);
                var gist = await _gistService.CreateGistAsync($"OpenCode Session: {sessionId}", $"{sessionId}.json", sessionData, ct);
                if (gist == null)
                {
                    return CreateExportResult("Failed to create Gist", false);
                }

                _registryService.Register(sessionId, gist.Id, title);
                Log.Logger.Information("Session exported successfully. GistId: {GistId}", gist.Id);
                return new ExportResult
                {
                    Success = true,
                    RemoteId = gist.Id,
                    Message = $"Session exported successfully. Gist: {gist.Id}"
                };
            }
        }
        catch (TimeoutException ex)
        {
            Log.Logger.Error(ex, "Export timed out after 60 seconds");
            return new ExportResult
            {
                Success = false,
                Error = $"Command timed out after 60 seconds: {ex.Message}"
            };
        }
        catch (InvalidOperationException ex)
        {
            Log.Logger.Error(ex, "OpenCode CLI error during export");
            return new ExportResult
            {
                Success = false,
                Error = $"OpenCode CLI error: {ex.Message}"
            };
        }
        catch (HttpRequestException ex)
        {
            Log.Logger.Error(ex, "GitHub API error during upload");
            return new ExportResult
            {
                Success = false,
                Error = $"GitHub API error: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Unexpected error during export");
            return new ExportResult
            {
                Success = false,
                Error = $"Unexpected error: {ex.Message}"
            };
        }
    }

    public async Task<(string? sessionId, string? title)> GetSessionInfoByPathAsync(string? sessionPath, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(sessionPath))
        {
            return (null, null);
        }

        Log.Logger.Information("Extracting session info from sessionPath: {SessionPath}", sessionPath);

        string dbPath;
        try
        {
            dbPath = await _openCodeService.GetDatabasePathAsync(ct);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Failed to get database path from opencode");
            return (null, null);
        }

        if (string.IsNullOrEmpty(dbPath)) return (null, null);

        if (!File.Exists(dbPath))
        {
            Log.Logger.Warning("Database file not found: {DbPath}", dbPath);
            return (null, null);
        }

        try
        {
            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, title FROM session 
                WHERE directory = @directory 
                ORDER BY time_updated DESC 
                LIMIT 1";
            command.Parameters.AddWithValue("@directory", sessionPath);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                var sessionId = reader.GetString(0);
                var title = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                Log.Logger.Information("Extracted SessionId: {SessionId}, Title: {Title} from SessionPath: {SessionPath}", sessionId, title, sessionPath);
                return (sessionId, title);
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Failed to query SQLite database at: {DbPath}", dbPath);
        }

        return (null, null);
    }

    public async Task<ImportResult> DownloadAndImportAsync(
        string sessionId, CancellationToken ct = default)
    {
        string? tempFile = null;
        try
        {
            if (!_registryService.TryGet(sessionId, out var gistId))
            {
                return new ImportResult
                {
                    Success = false,
                    Error = $"Session {sessionId} not found in registry. Please export it first."
                };
            }

            Log.Logger.Information("Downloading from Gist {GistId}", gistId);

            var gist = await _gistService.GetGistAsync(gistId, ct);
            if (gist == null)
            {
                return new ImportResult
                {
                    Success = false,
                    Error = $"Failed to get Gist {gistId}"
                };
            }

            if (!gist.Files.TryGetValue($"{sessionId}.json", out var fileInfo))
            {
                return new ImportResult
                {
                    Success = false,
                    Error = $"File {sessionId}.json not found in Gist {gistId}"
                };
            }

            Log.Logger.Information("Downloaded data, size: {Size} bytes", fileInfo.Content.Length);

            tempFile = Path.Combine(_workingDirectory, $"import_{sessionId}_{Guid.NewGuid():N}.json");
            await File.WriteAllTextAsync(tempFile, fileInfo.Content, ct);
            Log.Logger.Information("Saved to temp file: {File}", tempFile);

            Log.Logger.Information("Calling OpenCode import");
            await _openCodeService.ImportSessionAsync(tempFile, ct);
            Log.Logger.Information("OpenCode import completed");

            Log.Logger.Information("Session imported successfully. SessionId: {SessionId}", sessionId);

            return new ImportResult
            {
                Success = true,
                SessionId = sessionId,
                Message = $"Session imported successfully from Gist: {gistId}"
            };
        }
        catch (TimeoutException ex)
        {
            Log.Logger.Error(ex, "Import timed out after 60 seconds");
            return new ImportResult
            {
                Success = false,
                Error = $"Command timed out after 60 seconds: {ex.Message}"
            };
        }
        catch (InvalidOperationException ex)
        {
            Log.Logger.Error(ex, "OpenCode CLI error during import");
            return new ImportResult
            {
                Success = false,
                Error = $"OpenCode CLI error: {ex.Message}"
            };
        }
        catch (HttpRequestException ex)
        {
            Log.Logger.Error(ex, "GitHub API error during download");
            return new ImportResult
            {
                Success = false,
                Error = $"GitHub API error: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Unexpected error during import");
            return new ImportResult
            {
                Success = false,
                Error = $"Unexpected error: {ex.Message}"
            };
        }
        finally
        {
            if (!string.IsNullOrEmpty(tempFile) && File.Exists(tempFile))
            {
                try
                {
                    File.Delete(tempFile);
                    Log.Logger.Information("Deleted temp file: {File}", tempFile);
                }
                catch (Exception ex)
                {
                    Log.Logger.Warning(ex, "Failed to delete temp file: {File}", tempFile);
                }
            }
        }
    }

    public Task<ListSessionsResult> ListSessionsAsync(CancellationToken ct = default)
    {
        var sessions = _registryService.GetAllWithId();
        Log.Logger.Information("Found {Count} registered sessions", sessions.Count);

        return Task.FromResult(new ListSessionsResult
        {
            Success = true,
            Sessions = sessions.Select(s => new SessionRegistryInfo
            {
                SessionId = s.SessionId,
                Title = s.Entry.Title,
                UpdatedAt = s.Entry.UpdatedAt
            }).ToList()
        });
    }

    public async Task<DeleteResult> DeleteSessionAsync(string sessionId, CancellationToken ct = default)
    {
        try
        {
            if (!_registryService.TryGet(sessionId, out var gistId))
            {
                return new DeleteResult
                {
                    Success = false,
                    Error = $"Session {sessionId} not found in registry"
                };
            }

            var deleted = await _gistService.DeleteGistAsync(gistId, ct);
            if (!deleted)
            {
                return new DeleteResult
                {
                    Success = false,
                    Error = $"Failed to delete Gist {gistId}"
                };
            }

            _registryService.Unregister(sessionId);
            Log.Logger.Information("Session deleted successfully. SessionId: {SessionId}, GistId: {GistId}", sessionId, gistId);

            return new DeleteResult
            {
                Success = true,
                Message = $"Session deleted successfully"
            };
        }
        catch (HttpRequestException ex)
        {
            Log.Logger.Error(ex, "GitHub API error during delete");
            return new DeleteResult
            {
                Success = false,
                Error = $"GitHub API error: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Unexpected error during delete");
            return new DeleteResult
            {
                Success = false,
                Error = $"Unexpected error: {ex.Message}"
            };
        }
    }

    private ExportResult CreateExportResult(string message, bool success)
    {
        if (success)
        {
            Log.Logger.Information(message);
        }
        else
        {
            Log.Logger.Error(message);
        }
        return new ExportResult
        {
            Success = success,
            Error = message
        };
    }
}

public class ListSessionsResult
{
    public bool Success { get; set; }
    public List<SessionRegistryInfo> Sessions { get; set; } = new();
    public string? Error { get; set; }
}

public class SessionRegistryInfo
{
    public string SessionId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public long UpdatedAt { get; set; }
}

public class DeleteResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}
