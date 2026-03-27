using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenCodeSessionMCP.Configuration;
using OpenCodeSessionMCP.Models;

namespace OpenCodeSessionMCP.Services;

public sealed class OpenCodeService : IOpenCodeService
{
    private readonly string _cliPath;
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(60);
    private readonly ILogger<OpenCodeService> _logger;

    public OpenCodeService(IOptions<AppSettings> settings, ILogger<OpenCodeService> logger)
    {
        _cliPath = settings.Value.OpenCode.CliPath;
        _logger = logger;
    }

    public async Task<string> ExportSessionAsync(string? sessionId, CancellationToken ct = default)
    {
        var args = string.IsNullOrEmpty(sessionId) ? "export" : $"export {sessionId}";
        _logger.LogDebug("Executing: opencode {Args}", args);

        var (exitCode, output, error) = await RunCommandAsync(args, ct);

        if (exitCode != 0)
        {
            _logger.LogError("OpenCode export failed. ExitCode: {ExitCode}, Error: {Error}", exitCode, error);
            throw new InvalidOperationException($"OpenCode export failed: {error}");
        }

        _logger.LogDebug("OpenCode export output: {Output}", output);

        var exportedFile = FindExportedFile(output);
        if (string.IsNullOrEmpty(exportedFile))
        {
            _logger.LogError("Could not find exported file from output: {Output}", output);
            throw new InvalidOperationException($"Could not find exported file from output: {output}");
        }

        _logger.LogDebug("Exported file path: {File}", exportedFile);
        return exportedFile;
    }

    public async Task ImportSessionAsync(string filePath, CancellationToken ct = default)
    {
        var args = $"import \"{filePath}\"";
        _logger.LogDebug("Executing: opencode {Args}", args);

        var (exitCode, output, error) = await RunCommandAsync(args, ct);

        if (exitCode != 0)
        {
            _logger.LogError("OpenCode import failed. ExitCode: {ExitCode}, Error: {Error}", exitCode, error);
            throw new InvalidOperationException($"OpenCode import failed: {error}");
        }

        _logger.LogDebug("OpenCode import completed successfully");
    }

    public async Task<IReadOnlyList<SessionInfo>> ListSessionsAsync(CancellationToken ct = default)
    {
        var args = "session list --format json";
        _logger.LogDebug("Executing: opencode {Args}", args);

        var (exitCode, output, error) = await RunCommandAsync(args, ct);

        if (exitCode != 0)
        {
            _logger.LogError("OpenCode session list failed. ExitCode: {ExitCode}, Error: {Error}", exitCode, error);
            throw new InvalidOperationException($"OpenCode session list failed: {error}");
        }

        try
        {
            var sessions = JsonSerializer.Deserialize<List<SessionInfo>>(output);
            _logger.LogDebug("Listed {Count} sessions", sessions?.Count ?? 0);
            return sessions ?? [];
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse session list JSON");
            throw new InvalidOperationException($"Failed to parse session list: {ex.Message}", ex);
        }
    }

    private async Task<(int exitCode, string output, string error)> RunCommandAsync(string arguments, CancellationToken ct)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _cliPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(_timeout);

        try
        {
            _logger.LogDebug("Starting process: {FileName} {Arguments}", _cliPath, arguments);
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync(cts.Token);
            var errorTask = process.StandardError.ReadToEndAsync(cts.Token);

            await process.WaitForExitAsync(cts.Token);

            var output = await outputTask;
            var error = await errorTask;

            _logger.LogDebug("Process completed. ExitCode: {ExitCode}", process.ExitCode);

            return (process.ExitCode, output.Trim(), error.Trim());
        }
        catch (OperationCanceledException)
        {
            try
            {
                process.Kill(entireProcessTree: true);
                _logger.LogWarning("Process killed due to timeout");
            }
            catch
            {
            }

            throw new TimeoutException($"Command timed out after {_timeout.TotalSeconds} seconds");
        }
    }

    private static string FindExportedFile(string output)
    {
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (line.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                return line.Trim();
            }
        }

        if (output.Contains(".json"))
        {
            var startIndex = output.LastIndexOf('"') + 1;
            var endIndex = output.LastIndexOf(".json") + 4;
            if (startIndex < endIndex && endIndex <= output.Length)
            {
                return output[startIndex..endIndex];
            }
        }

        return output.Trim();
    }
}
