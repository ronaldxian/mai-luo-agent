namespace OcCli.Models;

public static class ExitCodes
{
    public const int Success = 0;
    public const int GeneralFailure = 1;
    public const int SessionNotFound = 2;
    public const int NetworkError = 3;
    public const int ApiError = 4;
    public const int Timeout = 5;
}

public sealed class ExportResult
{
    public bool Success { get; set; }
    public string? RemoteId { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}

public sealed class ImportResult
{
    public bool Success { get; set; }
    public string? SessionId { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}

public sealed class ListSessionsResult
{
    public bool Success { get; set; }
    public List<SessionRegistryInfo> Sessions { get; set; } = new();
    public string? Error { get; set; }
}

public sealed class SessionRegistryInfo
{
    public string SessionId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public long UpdatedAt { get; set; }
}

public sealed class DeleteResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}
