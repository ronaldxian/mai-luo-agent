using OpenCodeSessionMCP.Models;

namespace OpenCodeSessionMCP.Services;

public interface IOpenCodeService
{
    Task<string> ExportSessionAsync(string? sessionId, CancellationToken ct = default);
    Task ImportSessionAsync(string filePath, CancellationToken ct = default);
    Task<IReadOnlyList<SessionInfo>> ListSessionsAsync(CancellationToken ct = default);
}
