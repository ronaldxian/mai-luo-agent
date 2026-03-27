using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenCodeSessionMCP.Configuration;
using OpenCodeSessionMCP.Models;

namespace OpenCodeSessionMCP.Services;

public sealed class SessionSyncService : ISessionSyncService
{
    private readonly IOpenCodeService _openCodeService;
    private readonly AppSettings _settings;
    private readonly string _workingDirectory;
    private readonly ILogger<SessionSyncService> _logger;

    public SessionSyncService(
        IOpenCodeService openCodeService, 
        IOptions<AppSettings> settings,
        ILogger<SessionSyncService> logger)
    {
        _openCodeService = openCodeService;
        _settings = settings.Value;
        _workingDirectory = Directory.GetCurrentDirectory();
        _logger = logger;
    }

    public async Task<ExportResult> ExportAndUploadAsync(
        string? sessionId,
        string? serverUrl,
        string? apiKey,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Starting session export. SessionId: {SessionId}, ServerUrl: {ServerUrl}", sessionId, serverUrl ?? _settings.RestApi.BaseUrl);

        try
        {
            var exportedFile = await _openCodeService.ExportSessionAsync(sessionId, ct);
            _logger.LogDebug("OpenCode export completed. File: {File}", exportedFile);

            var jsonData = await File.ReadAllTextAsync(exportedFile, ct);
            _logger.LogDebug("Read exported file, size: {Size} bytes", jsonData.Length);

            var sessionData = JsonDocument.Parse(jsonData);
            var sessionIdFromFile = sessionData.RootElement.TryGetProperty("id", out var idProp)
                ? idProp.GetString() ?? sessionId ?? "unknown"
                : sessionId ?? "unknown";

            var effectiveServerUrl = string.IsNullOrEmpty(serverUrl) ? _settings.RestApi.BaseUrl : serverUrl;
            var effectiveApiKey = string.IsNullOrEmpty(apiKey) ? _settings.RestApi.ApiKey : apiKey;

            _logger.LogInformation("Uploading to server: {ServerUrl}", effectiveServerUrl);

            var apiService = new RestApiService(new HttpClient(), effectiveServerUrl, effectiveApiKey);
            var remoteId = await apiService.UploadSessionAsync(sessionIdFromFile, jsonData, ct);

            _logger.LogInformation("Session exported successfully. RemoteId: {RemoteId}", remoteId);

            return new ExportResult
            {
                Success = true,
                RemoteId = remoteId,
                Message = $"Session exported successfully. Remote ID: {remoteId}"
            };
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Export timed out after 60 seconds");
            return new ExportResult
            {
                Success = false,
                Error = $"Command timed out after 60 seconds: {ex.Message}"
            };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "OpenCode CLI error during export");
            return new ExportResult
            {
                Success = false,
                Error = $"OpenCode CLI error: {ex.Message}"
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "API error during upload. ServerUrl: {ServerUrl}", serverUrl ?? _settings.RestApi.BaseUrl);
            return new ExportResult
            {
                Success = false,
                Error = $"API error: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during export");
            return new ExportResult
            {
                Success = false,
                Error = $"Unexpected error: {ex.Message}"
            };
        }
    }

    public async Task<ImportResult> DownloadAndImportAsync(
        string remoteId,
        string? serverUrl,
        string? apiKey,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Starting session import. RemoteId: {RemoteId}, ServerUrl: {ServerUrl}", remoteId, serverUrl ?? _settings.RestApi.BaseUrl);

        string? tempFile = null;

        try
        {
            var effectiveServerUrl = string.IsNullOrEmpty(serverUrl) ? _settings.RestApi.BaseUrl : serverUrl;
            var effectiveApiKey = string.IsNullOrEmpty(apiKey) ? _settings.RestApi.ApiKey : apiKey;

            _logger.LogInformation("Downloading from server: {ServerUrl}", effectiveServerUrl);

            var apiService = new RestApiService(new HttpClient(), effectiveServerUrl, effectiveApiKey);
            var jsonData = await apiService.DownloadSessionAsync(remoteId, ct);
            _logger.LogDebug("Downloaded data, size: {Size} bytes", jsonData.Length);

            tempFile = Path.Combine(_workingDirectory, $"import_{remoteId}_{Guid.NewGuid():N}.json");
            await File.WriteAllTextAsync(tempFile, jsonData, ct);
            _logger.LogDebug("Saved to temp file: {File}", tempFile);

            _logger.LogInformation("Calling OpenCode import");
            await _openCodeService.ImportSessionAsync(tempFile, ct);
            _logger.LogInformation("OpenCode import completed");

            var sessionData = JsonDocument.Parse(jsonData);
            var sessionId = sessionData.RootElement.TryGetProperty("id", out var idProp)
                ? idProp.GetString()
                : null;

            _logger.LogInformation("Session imported successfully. SessionId: {SessionId}", sessionId ?? remoteId);

            return new ImportResult
            {
                Success = true,
                SessionId = sessionId ?? remoteId,
                Message = $"Session imported successfully"
            };
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Import timed out after 60 seconds");
            return new ImportResult
            {
                Success = false,
                Error = $"Command timed out after 60 seconds: {ex.Message}"
            };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "OpenCode CLI error during import");
            return new ImportResult
            {
                Success = false,
                Error = $"OpenCode CLI error: {ex.Message}"
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "API error during download. RemoteId: {RemoteId}", remoteId);
            return new ImportResult
            {
                Success = false,
                Error = $"API error: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during import");
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
                    _logger.LogDebug("Deleted temp file: {File}", tempFile);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temp file: {File}", tempFile);
                }
            }
        }
    }

    public async Task<ListRemoteSessionsResult> ListRemoteSessionsAsync(
        string? serverUrl,
        string? apiKey,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Listing remote sessions. ServerUrl: {ServerUrl}", serverUrl ?? _settings.RestApi.BaseUrl);

        try
        {
            var effectiveServerUrl = string.IsNullOrEmpty(serverUrl) ? _settings.RestApi.BaseUrl : serverUrl;
            var effectiveApiKey = string.IsNullOrEmpty(apiKey) ? _settings.RestApi.ApiKey : apiKey;

            var apiService = new RestApiService(new HttpClient(), effectiveServerUrl, effectiveApiKey);
            var sessions = await apiService.ListSessionsAsync(ct);

            _logger.LogInformation("Found {Count} sessions", sessions.Count);

            return new ListRemoteSessionsResult
            {
                Success = true,
                Sessions = sessions.ToList()
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "API error listing sessions. ServerUrl: {ServerUrl}", serverUrl ?? _settings.RestApi.BaseUrl);
            return new ListRemoteSessionsResult
            {
                Success = false,
                Error = $"API error: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error listing sessions");
            return new ListRemoteSessionsResult
            {
                Success = false,
                Error = $"Unexpected error: {ex.Message}"
            };
        }
    }
}
