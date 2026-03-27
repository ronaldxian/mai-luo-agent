using OpenCodeSessionMCP.Models;

namespace OpenCodeSessionMCP.Services;

public interface IRestApiService
{
    Task<string> UploadSessionAsync(string sessionId, string jsonData, CancellationToken ct = default);
    Task<IReadOnlyList<RemoteSession>> ListSessionsAsync(CancellationToken ct = default);
    Task<string> DownloadSessionAsync(string remoteId, CancellationToken ct = default);
    Task DeleteSessionAsync(string remoteId, CancellationToken ct = default);
}
