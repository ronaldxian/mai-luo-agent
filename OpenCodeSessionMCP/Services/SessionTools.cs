using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using OpenCodeSessionMCP.Configuration;
using Serilog;
using System.ComponentModel;

namespace OpenCodeSessionMCP.Services;

[McpServerToolType]
public static class SessionTools
{
    [McpServerTool, Description("hello")]
    public static async Task<string> Hello(IServiceProvider serviceProvider)
    {
        var s = serviceProvider.GetRequiredService<IOptions<AppSettings>>().Value;
        return string.Format("Hello! Current time is {0}. RestApi ApiKey: {1} 999222", DateTime.Now, s.RestApi.ApiKey);
    }

    [McpServerTool, Description("导出会话并上传到服务器")]
    public static async Task<string> ExportSession(IServiceProvider serviceProvider, [Description("会话ID")] string sessionId)
    {
        Log.Logger.Information("ExportSession called. SessionId: {SessionId}", sessionId);

        try
        {
            var sessionSyncService = serviceProvider.GetRequiredService<SessionSyncService>();
            var result = await sessionSyncService.ExportAndUploadAsync(sessionId);

            if (result.Success)
            {
                Log.Logger.Information("ExportSession succeeded. RemoteId: {RemoteId}", result.RemoteId);
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

    [McpServerTool, Description("从服务器下载并导入会话")]
    public static async Task<string> ImportSession(
        IServiceProvider serviceProvider,
        [Description("远程会话ID")] string sessionId)
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

    [McpServerTool, Description("列出服务器上的会话")]
    public static async Task<string> ListRemoteSessions(
        IServiceProvider serviceProvider,
        SessionSyncService sessionSyncService)
    {
        var logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger("SessionTools");
        logger.LogInformation("ListRemoteSessions called");

        try
        {
            var result = await sessionSyncService.ListRemoteSessionsAsync();

            if (result.Success)
            {
                Log.Logger.Information("ListRemoteSessions succeeded. Count: {Count}", result.Sessions.Count);

                if (result.Sessions.Count == 0)
                {
                    return "No sessions found";
                }

                var lines = result.Sessions.Select(s =>
                    $"- {s.Id}: {s.Title}");
                return string.Join("\n", lines);
            }

            logger?.LogWarning("ListRemoteSessions failed. Error: {Error}", result.Error);
            return $"Error: {result.Error}";
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "ListRemoteSessions exception");
            return $"Error: {ex.Message}";
        }
    }
}
