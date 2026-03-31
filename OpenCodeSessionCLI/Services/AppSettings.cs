namespace OcCli.Services;

public sealed class AppSettings
{
    public GitHubSettings GitHub { get; set; } = new();
    public OpenCodeSettings OpenCode { get; set; } = new();
}

public sealed class GitHubSettings
{
    public string TokenEnvVar { get; set; } = "GITHUB_TOKEN";
}

public sealed class OpenCodeSettings
{
    public string CliPath { get; set; } = string.Empty;
}
