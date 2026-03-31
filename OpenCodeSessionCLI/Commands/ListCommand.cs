using OcCli.Models;
using OcCli.Services;

namespace OcCli.Commands;

public static class ListCommand
{
    public static int Execute(string[] args)
    {
        try
        {
            var registryService = new RegistryService();
            var openCodeService = new OpenCodeService(new AppSettings());
            var gistService = new GistService(new HttpClient());
            var syncService = new SessionSyncService(openCodeService, gistService, registryService);

            AnsiConsole.Info("[INFO] Fetching registered sessions...[/]");

            var result = syncService.ListSessions();

            if (!result.Success)
            {
                AnsiConsole.Error("[ERROR] {0}[/]", result.Error);
                return ExitCodes.GeneralFailure;
            }

            if (result.Sessions.Count == 0)
            {
                AnsiConsole.Warn("[WARN] No sessions found in registry.[/]");
                return ExitCodes.Success;
            }

            AnsiConsole.Info("[INFO] Found {0} registered session(s):[/]", result.Sessions.Count);
            Console.WriteLine();

            foreach (var session in result.Sessions)
            {
                var updatedAt = DateTimeOffset.FromUnixTimeMilliseconds(session.UpdatedAt)
                    .ToLocalTime()
                    .ToString("yyyy-MM-dd HH:mm:ss");
                Console.WriteLine($"  Session ID: {session.SessionId}");
                Console.WriteLine($"  Title:      {session.Title}");
                Console.WriteLine($"  Updated:    {updatedAt}");
                Console.WriteLine();
            }

            return ExitCodes.Success;
        }
        catch (Exception ex)
        {
            AnsiConsole.Error("[ERROR] {0}[/]", ex.Message);
            return ExitCodes.GeneralFailure;
        }
    }
}
