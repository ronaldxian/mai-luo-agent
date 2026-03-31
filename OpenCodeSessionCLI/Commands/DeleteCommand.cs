using OcCli.Models;
using OcCli.Services;
using Serilog;

namespace OcCli.Commands;

public static class DeleteCommand
{
    public static async Task<int> ExecuteAsync(string[] args)
    {
        var sessionId = ParseSessionId(args);

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            AnsiConsole.Error("[ERROR] Session ID is required.[/]");
            AnsiConsole.Info("[INFO] Usage: oc-cli delete <sessionId>[/]");
            return ExitCodes.GeneralFailure;
        }

        AnsiConsole.Info("[INFO] Starting delete for session: {0}[/]", sessionId);

        try
        {
            var httpClient = new HttpClient();
            var openCodeService = new OpenCodeService(new AppSettings());
            var gistService = new GistService(httpClient);
            var registryService = new RegistryService();
            var syncService = new SessionSyncService(openCodeService, gistService, registryService);

            AnsiConsole.Info("[INFO] Deleting session from Gist...[/]");

            var result = await syncService.DeleteSessionAsync(sessionId);

            if (result.Success)
            {
                AnsiConsole.Done("[DONE] {0}[/]", result.Message);
                return ExitCodes.Success;
            }
            else
            {
                AnsiConsole.Error("[ERROR] {0}[/]", result.Error);
                return DetermineExitCode(result.Error);
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Delete command failed");
            AnsiConsole.Error("[ERROR] {0}[/]", ex.Message);
            return ExitCodes.GeneralFailure;
        }
    }

    private static string? ParseSessionId(string[] args)
    {
        if (args.Length == 0) return null;

        var firstArg = args[0];
        if (firstArg.StartsWith("--session-id="))
            return firstArg["--session-id=".Length..];

        if (args.Length >= 2 && firstArg == "--session-id")
            return args[1];

        return firstArg;
    }

    private static int DetermineExitCode(string? error)
    {
        if (string.IsNullOrEmpty(error)) return ExitCodes.GeneralFailure;

        if (error.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("not found in registry", StringComparison.OrdinalIgnoreCase))
            return ExitCodes.SessionNotFound;

        if (error.Contains("GitHub API", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("Failed to delete Gist", StringComparison.OrdinalIgnoreCase))
            return ExitCodes.ApiError;

        if (error.Contains("network", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("HttpRequest", StringComparison.OrdinalIgnoreCase))
            return ExitCodes.NetworkError;

        return ExitCodes.GeneralFailure;
    }
}
