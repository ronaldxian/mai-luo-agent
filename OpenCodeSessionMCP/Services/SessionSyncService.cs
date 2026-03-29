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
    private readonly AppSettings _settings;
    private readonly string _workingDirectory;

    public SessionSyncService(
        OpenCodeService openCodeService,
        IOptions<AppSettings> settings)
    {
        _openCodeService = openCodeService;
        _settings = settings.Value;
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

            if (string.IsNullOrEmpty(_settings.RestApi.BaseUrl))
            {
                Log.Logger.Warning("API settings are not configured. BaseUrl: {BaseUrl}", _settings.RestApi.BaseUrl);
                return CreateExportResult("API settings are not configured. Please set the BaseUrl in the configuration.", false);
            }

            var base64Data = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(sessionData));
            Log.Logger.Information("Read exported session data, size: {Size} bytes", base64Data.Length);

            var effectiveServerUrl = _settings.RestApi.BaseUrl;
            var effectiveApiKey = _settings.RestApi.ApiKey;

            Log.Logger.Information("Uploading to server: {ServerUrl}", effectiveServerUrl);

            var apiService = new RestApiService(new HttpClient(), effectiveServerUrl, effectiveApiKey);
            var remoteId = await apiService.UploadSessionAsync(sessionId, title, base64Data, ct);

            Log.Logger.Information("Session exported successfully. RemoteId: {RemoteId}", remoteId);

            return new ExportResult
            {
                Success = true,
                RemoteId = remoteId,
                Message = $"Session exported successfully. Remote ID: {remoteId}"
            };
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
            Log.Logger.Error(ex, "API error during upload. ServerUrl: {ServerUrl}", _settings.RestApi.BaseUrl);
            return new ExportResult
            {
                Success = false,
                Error = $"API error: {ex.Message}"
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

    private async Task<(string? sessionId, string? title)> TryFindSessionIdByPathAsync(string? sessionPath, CancellationToken ct)
    {
        Log.Logger.Information("SessionId is not provided, attempting to extract from sessionPath: {SessionPath}", sessionPath);

        string sessionId = string.Empty;
        string title = string.Empty;

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
            command.Parameters.AddWithValue("@directory", sessionPath ?? string.Empty);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                sessionId = reader.GetString(0);
                title = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
            }

            Log.Logger.Information("SQLite query executed. SessionId: {SessionId}, Title: {Title}", sessionId, title);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Failed to query SQLite database at: {DbPath}", dbPath);
            return (null, null);
        }

        if (string.IsNullOrEmpty(sessionId)) return (null, null);

        Log.Logger.Information("Extracted SessionId: {SessionId}, Title: {Title} from SessionPath: {SessionPath}", sessionId, title, sessionPath);
        return (sessionId, title);
    }

    public async Task<(string? sessionId, string? title)> GetSessionInfoByPathAsync(string sessionPath, CancellationToken ct = default)
    {
        return await TryFindSessionIdByPathAsync(sessionPath, ct);
    }

    public async Task<ImportResult> DownloadAndImportAsync(
        string remoteId, CancellationToken ct = default)
    {
        string? tempFile = null;
        try
        {
            Log.Logger.Information("Downloading from server: {ServerUrl}", _settings.RestApi.BaseUrl);

            var apiService = new RestApiService(new HttpClient(), _settings.RestApi.BaseUrl, _settings.RestApi.ApiKey);
            var jsonData = await apiService.DownloadSessionAsync(remoteId, ct);
            Log.Logger.Information("Downloaded data, size: {Size} bytes", jsonData.Length);

            tempFile = Path.Combine(_workingDirectory, $"import_{remoteId}_{Guid.NewGuid():N}.json");
            await File.WriteAllTextAsync(tempFile, jsonData, ct);
            Log.Logger.Information("Saved to temp file: {File}", tempFile);

            Log.Logger.Information("Calling OpenCode import");
            await _openCodeService.ImportSessionAsync(tempFile, ct);
            Log.Logger.Information("OpenCode import completed");

            Log.Logger.Information("Session imported successfully. SessionId: {SessionId}", remoteId);

            return new ImportResult
            {
                Success = true,
                SessionId = remoteId,
                Message = $"Session imported successfully"
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
            Log.Logger.Error(ex, "API error during download. RemoteId: {RemoteId}", remoteId);
            return new ImportResult
            {
                Success = false,
                Error = $"API error: {ex.Message}"
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

    public async Task<ListRemoteSessionsResult> ListRemoteSessionsAsync(CancellationToken ct = default)
    {
        try
        {
            var apiService = new RestApiService(new HttpClient(), _settings.RestApi.BaseUrl, _settings.RestApi.ApiKey);
            var sessions = await apiService.ListSessionsAsync(ct);

            Log.Logger.Information("Found {Count} sessions", sessions.Count);

            return new ListRemoteSessionsResult
            {
                Success = true,
                Sessions = sessions.ToList()
            };
        }
        catch (HttpRequestException ex)
        {
            Log.Logger.Error(ex, "API error listing sessions. ServerUrl: {ServerUrl}", _settings.RestApi.BaseUrl);
            return new ListRemoteSessionsResult
            {
                Success = false,
                Error = $"API error: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Unexpected error listing sessions");
            return new ListRemoteSessionsResult
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
