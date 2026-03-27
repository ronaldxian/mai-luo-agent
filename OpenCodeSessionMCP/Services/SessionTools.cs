using System.ComponentModel;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using OpenCodeSessionMCP.Models;
using OpenCodeSessionMCP.Services;

namespace OpenCodeSessionMCP.Services;

[McpServerToolType]
public static class SessionTools
{
    [McpServerTool, Description("hello")]
    public static async Task<string> Hello()
    {
        return "world";
    }

    [McpServerTool, Description("导出会话并上传到服务器")]
    public static async Task<string> ExportSession(
        IServiceProvider serviceProvider,
        ISessionSyncService sessionSyncService,
        [Description("会话ID (可选，不填则导出当前会话)")] string? sessionId = null,
        [Description("API 地址 (可选，默认为配置中的地址)")] string? serverUrl = null,
        [Description("API Key (可选，默认为配置中的值)")] string? apiKey = null
        //CancellationToken cancellationToken = default
        )
    {
        var logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger("SessionTools");
        logger?.LogInformation("ExportSession called. SessionId: {SessionId}, ServerUrl: {ServerUrl}", sessionId, serverUrl);

        try
        {
            var result = await sessionSyncService.ExportAndUploadAsync(sessionId, serverUrl, apiKey);

            if (result.Success)
            {
                logger?.LogInformation("ExportSession succeeded. RemoteId: {RemoteId}", result.RemoteId);
                return $"Success: {result.Message}";
            }

            logger?.LogWarning("ExportSession failed. Error: {Error}", result.Error);
            return $"Error: {result.Error}";
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "ExportSession exception");
            return $"Error: {ex.Message}";
        }
    }

    [McpServerTool, Description("从服务器下载并导入会话")]
    public static async Task<string> ImportSession(
        IServiceProvider serviceProvider,
        ISessionSyncService sessionSyncService,
        [Description("远程会话ID (必填)")] string remoteId,
        [Description("API 地址 (可选)")] string? serverUrl = null,
        [Description("API Key (可选)")] string? apiKey = null,
        CancellationToken cancellationToken = default)
    {
        var logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger("SessionTools");
        logger?.LogInformation("ImportSession called. RemoteId: {RemoteId}, ServerUrl: {ServerUrl}", remoteId, serverUrl);

        if (string.IsNullOrEmpty(remoteId))
        {
            logger?.LogWarning("ImportSession called with empty remoteId");
            return "Error: Missing required argument: remoteId";
        }

        try
        {
            var result = await sessionSyncService.DownloadAndImportAsync(remoteId, serverUrl, apiKey, cancellationToken);

            if (result.Success)
            {
                logger?.LogInformation("ImportSession succeeded. SessionId: {SessionId}", result.SessionId);
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

    [McpServerTool, Description("列出服务器上的会话")]
    public static async Task<string> ListRemoteSessions(
        IServiceProvider serviceProvider,
        ISessionSyncService sessionSyncService,
        [Description("API 地址 (可选)")] string? serverUrl = null,
        [Description("API Key (可选)")] string? apiKey = null,
        CancellationToken cancellationToken = default)
    {
        var logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger("SessionTools");
        logger?.LogInformation("ListRemoteSessions called. ServerUrl: {ServerUrl}", serverUrl);

        try
        {
            var result = await sessionSyncService.ListRemoteSessionsAsync(serverUrl, apiKey, cancellationToken);

            if (result.Success)
            {
                logger?.LogInformation("ListRemoteSessions succeeded. Count: {Count}", result.Sessions.Count);

                if (result.Sessions.Count == 0)
                {
                    return "No sessions found";
                }

                var lines = result.Sessions.Select(s =>
                    $"- {s.Id}: {s.Title} (Created: {DateTimeOffset.FromUnixTimeMilliseconds(s.TimeCreated).ToLocalTime():g})");
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
