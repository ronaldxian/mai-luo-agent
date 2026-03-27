namespace OpenCodeSessionMCP.Configuration;

public sealed class AppSettings
{
    public RestApiSettings RestApi { get; set; } = new();
    public OpenCodeSettings OpenCode { get; set; } = new();
}

public sealed class RestApiSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}

public sealed class OpenCodeSettings
{
    public string CliPath { get; set; } = "opencode";
}
