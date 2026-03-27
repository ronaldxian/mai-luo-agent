using OpenCodeSessionMCP.Models;

namespace OpenCodeSessionMCP.Services;

public interface ISessionSyncService
{
    Task<ExportResult> ExportAndUploadAsync(string? sessionId, string? serverUrl, string? apiKey, CancellationToken ct = default);
    Task<ImportResult> DownloadAndImportAsync(string remoteId, string? serverUrl, string? apiKey, CancellationToken ct = default);
    Task<ListRemoteSessionsResult> ListRemoteSessionsAsync(string? serverUrl, string? apiKey, CancellationToken ct = default);
}
