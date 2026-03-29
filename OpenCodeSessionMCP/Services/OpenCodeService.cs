using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenCodeSessionMCP.Configuration;
using OpenCodeSessionMCP.Models;
using Serilog;

namespace OpenCodeSessionMCP.Services;

public sealed class OpenCodeService
{
    private readonly string _cliPath;
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(60);

    public OpenCodeService(IOptions<AppSettings> settings)
    {
        var configuredPath = settings.Value.OpenCode.CliPath;
        _cliPath = ResolveOpenCodePath(configuredPath);
    }

    private static string ResolveOpenCodePath(string configuredPath)
    {
        if (string.IsNullOrEmpty(configuredPath) || configuredPath == "opencode")
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -Command \"(Get-Command opencode -ErrorAction SilentlyContinue).Source\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(5000);
                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        var resolvedPath = output.Trim();
                        if (!string.IsNullOrEmpty(resolvedPath) && File.Exists(resolvedPath))
                        {
                            Log.Logger.Information("Resolved opencode path: {Path}", resolvedPath);
                            return resolvedPath;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Warning(ex, "Failed to resolve opencode path via PowerShell");
            }
        }

        return configuredPath;
    }

    public async Task<string> ExportSessionAsync(string sessionId, CancellationToken ct = default)
    {
        var args = $"export {sessionId}";
        Log.Logger.Information("Executing: opencode {Args}", args);

        var (exitCode, output, error) = await RunCommandAsync(["export", sessionId], ct);

        if (exitCode != 0)
        {
            Log.Logger.Error("OpenCode export failed. ExitCode: {ExitCode}, Error: {Error}", exitCode, error);
            throw new InvalidOperationException($"OpenCode export failed: {error}");
        }

        return output;
    }

    public async Task ImportSessionAsync(string filePath, CancellationToken ct = default)
    {
        var args = $"import \"{filePath}\"";
        Log.Logger.Information("Executing: opencode {Args}", args);

        var (exitCode, output, error) = await RunCommandAsync(["import", filePath], ct);

        if (exitCode != 0)
        {
            Log.Logger.Error("OpenCode import failed. ExitCode: {ExitCode}, Error: {Error}", exitCode, error);
            throw new InvalidOperationException($"OpenCode import failed: {error}");
        }

        Log.Logger.Information("OpenCode import completed successfully");
    }

    public async Task<IReadOnlyList<SessionInfo>> ListSessionsAsync(CancellationToken ct = default)
    {
        var args = "session list --format json";
        Log.Logger.Information("Executing: opencode {Args}", args);

        var (exitCode, output, error) = await RunCommandAsync(["session", "list", "--format", "json"], ct);

        if (exitCode != 0)
        {
            Log.Logger.Error("OpenCode session list failed. ExitCode: {ExitCode}, Error: {Error}", exitCode, error);
            throw new InvalidOperationException($"OpenCode session list failed: {error}");
        }

        try
        {
            var sessions = JsonSerializer.Deserialize<List<SessionInfo>>(output);
            Log.Logger.Information("Listed {Count} sessions", sessions?.Count ?? 0);
            return sessions ?? [];
        }
        catch (JsonException ex)
        {
            Log.Logger.Error(ex, "Failed to parse session list JSON");
            throw new InvalidOperationException($"Failed to parse session list: {ex.Message}", ex);
        }
    }

    public async Task<string> GetDatabasePathAsync(CancellationToken ct = default)
    {
        Log.Logger.Information("Executing: opencode db path");

        var (exitCode, output, error) = await RunCommandAsync(["db", "path"], ct);

        if (exitCode != 0)
        {
            Log.Logger.Error("OpenCode db path failed. ExitCode: {ExitCode}, Error: {Error}", exitCode, error);
            throw new InvalidOperationException($"OpenCode db path failed: {error}");
        }

        if (string.IsNullOrWhiteSpace(output))
        {
            Log.Logger.Error("OpenCode db path returned empty output");
            throw new InvalidOperationException("OpenCode db path returned empty output");
        }

        Log.Logger.Information("Database path: {DbPath}", output);
        return output;
    }

    private async Task<(int exitCode, string output, string error)> RunCommandAsync(string[] arguments, CancellationToken ct)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _cliPath,
            Arguments = string.Join(" ", arguments),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            CreateNoWindow = true,
            WorkingDirectory = Directory.GetCurrentDirectory()
        };

        foreach (var envKey in Environment.GetEnvironmentVariables().Keys)
        {
            if (envKey is string key && !string.IsNullOrEmpty(key))
            {
                var value = Environment.GetEnvironmentVariable(key);
                if (!string.IsNullOrEmpty(value))
                {
                    startInfo.Environment[key] = value;
                }
            }
        }

        using var process = new Process { StartInfo = startInfo };
        using var timeoutCts = new CancellationTokenSource(_timeout);

        try
        {
            Log.Logger.Information("Starting process: {FileName} {Arguments} in {WorkingDirectory}",
                _cliPath, string.Join(" ", arguments), startInfo.WorkingDirectory);
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            using var timeoutRegistration = timeoutCts.Token.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                        Log.Logger.Warning("Process killed due to timeout");
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger.Error(ex, "Failed to kill process during timeout");
                }
            });

            var waitTask = process.WaitForExitAsync(ct);

            var completedTask = await Task.WhenAny(waitTask, Task.Delay(Timeout.Infinite, ct));

            if (!waitTask.IsCompleted)
            {
                throw new TimeoutException($"Command timed out after {_timeout.TotalSeconds} seconds");
            }

            var exitCode = process.ExitCode;
            var output = await outputTask;
            var error = await errorTask;

            Log.Logger.Information("Process completed. ExitCode: {ExitCode}", exitCode);

            return (exitCode, output.Trim(), error.Trim());
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            Log.Logger.Warning("Process timed out after {Timeout} seconds", _timeout.TotalSeconds);
            throw new TimeoutException($"Command timed out after {_timeout.TotalSeconds} seconds");
        }
        catch (OperationCanceledException)
        {
            Log.Logger.Warning("Process cancelled");
            throw;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Process execution failed");
            throw;
        }
    }
}
