using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Serilog;
using System.ComponentModel;

namespace OpenCodeSessionMCP.Services;

[McpServerToolType]
public static class SessionTools
{
    [McpServerTool, Description("hello - 测试工具是否正常工作")]
    public static string Hello()
    {
        return $"Hello! Current time is {DateTime.Now:yyyy/MM/dd HH:mm:ss}. GitHub Gist sync is ready.";
    }

    [McpServerTool, Description("导出会话并上传到 GitHub Gist")]
    public static async Task<string> ExportSession(IServiceProvider serviceProvider, [Description("会话ID")] string sessionId)
    {
        Log.Logger.Information("ExportSession called. SessionId: {SessionId}", sessionId);

        try
        {
            var sessionSyncService = serviceProvider.GetRequiredService<SessionSyncService>();
            var result = await sessionSyncService.ExportAndUploadAsync(sessionId);

            if (result.Success)
            {
                Log.Logger.Information("ExportSession succeeded. GistId: {GistId}", result.RemoteId);
                return $"Success: {result.Message}";
            }

            Log.Logger.Error("ExportSession failed. Error: {Error}", result.Error);
            return $"Error: {result.Error}";
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "ExportSession exception");
            return $"Error: {ex.Message}";
        }
    }

    [McpServerTool, Description("从 GitHub Gist 下载并导入会话")]
    public static async Task<string> ImportSession(
        IServiceProvider serviceProvider,
        [Description("会话ID")] string sessionId)
    {
        var logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger("SessionTools");
        Log.Logger.Information("ImportSession called. SessionId: {SessionId}", sessionId);

        if (string.IsNullOrEmpty(sessionId))
        {
            logger?.LogWarning("ImportSession called with empty sessionId");
            return "Error: Missing required argument: sessionId";
        }

        try
        {
            var sessionSyncService = serviceProvider.GetRequiredService<SessionSyncService>();
            var result = await sessionSyncService.DownloadAndImportAsync(sessionId);

            if (result.Success)
            {
                Log.Logger.Information("ImportSession succeeded. SessionId: {SessionId}", result.SessionId);
                return $"Success: {result.Message}";
            }

            logger?.LogWarning("ImportSession failed. Error: {Error}", result.Error);
            return $"Error: {result.Error}";
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "ImportSession exception");
            return $"Error: {ex.Message}";
        }
    }

    [McpServerTool, Description("获取会话标题和ID")]
    public static async Task<string> GetSessionInfo(
        IServiceProvider serviceProvider,
        [Description("会话目录路径")] string sessionPath)
    {
        var sessionSyncService = serviceProvider.GetRequiredService<SessionSyncService>();
        var (sessionId, title) = await sessionSyncService.GetSessionInfoByPathAsync(sessionPath);

        if (sessionId == null)
        {
            return "Session not found";
        }

        return $"SessionId: {sessionId}, Title: {title}";
    }

    [McpServerTool, Description("列出所有已同步的会话")]
    public static async Task<string> ListSessions(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger("SessionTools");
        logger?.LogInformation("ListSessions called");

        try
        {
            var sessionSyncService = serviceProvider.GetRequiredService<SessionSyncService>();
            var result = await sessionSyncService.ListSessionsAsync();

            if (result.Success)
            {
                Log.Logger.Information("ListSessions succeeded. Count: {Count}", result.Sessions.Count);

                if (result.Sessions.Count == 0)
                {
                    return "No sessions found. Export a session first.";
                }

                var lines = result.Sessions.Select(s =>
                    $"- {s.SessionId}: {s.Title}");
                return string.Join("\n", lines);
            }

            logger?.LogWarning("ListSessions failed. Error: {Error}", result.Error);
            return $"Error: {result.Error}";
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "ListSessions exception");
            return $"Error: {ex.Message}";
        }
    }

    [McpServerTool, Description("从 GitHub Gist 删除会话")]
    public static async Task<string> DeleteSession(
        IServiceProvider serviceProvider,
        [Description("会话ID")] string sessionId)
    {
        var logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger("SessionTools");
        Log.Logger.Information("DeleteSession called. SessionId: {SessionId}", sessionId);

        if (string.IsNullOrEmpty(sessionId))
        {
            logger?.LogWarning("DeleteSession called with empty sessionId");
            return "Error: Missing required argument: sessionId";
        }

        try
        {
            var sessionSyncService = serviceProvider.GetRequiredService<SessionSyncService>();
            var result = await sessionSyncService.DeleteSessionAsync(sessionId);

            if (result.Success)
            {
                Log.Logger.Information("DeleteSession succeeded.");
                return $"Success: {result.Message}";
            }

            logger?.LogWarning("DeleteSession failed. Error: {Error}", result.Error);
            return $"Error: {result.Error}";
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "DeleteSession exception");
            return $"Error: {ex.Message}";
        }
    }
}
