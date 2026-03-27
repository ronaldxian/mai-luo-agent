using OpenCodeSessionServer.Models;

namespace OpenCodeSessionServer.Services;

public interface ISessionService
{
    Task<UploadResponse> UploadAsync(UploadRequest request);
    Task<List<SessionInfo>> ListTodaySessionsAsync();
    Task<DownloadResponse> DownloadAsync(string id);
    Task DeleteAsync(string id);
}
