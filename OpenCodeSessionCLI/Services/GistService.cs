using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using OcCli.Models;
using Serilog;

namespace OcCli.Services;

public sealed class GistService
{
    private readonly HttpClient _httpClient;
    private readonly string _token;
    private readonly JsonSerializerOptions _jsonOptions;
    private const string GistApiBase = "https://api.github.com/gists";

    public GistService(HttpClient httpClient, string? token = null)
    {
        _httpClient = httpClient;
        _token = token ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? string.Empty;

        _httpClient.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("OcCli", "1.0.0"));

        if (!string.IsNullOrEmpty(_token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _token);
        }

        _jsonOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = OcCliJsonContext.Default,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<GistInfo?> CreateGistAsync(string title, string filename, string content, CancellationToken ct = default)
    {
        var request = new CreateGistRequest
        {
            Description = title,
            Public = false,
            Files = new Dictionary<string, GistFileContent>
            {
                [filename] = new GistFileContent { Content = content }
            }
        };

        var response = await _httpClient.PostAsJsonAsync(GistApiBase, request, _jsonOptions, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            Log.Logger.Error("Failed to create Gist. Status: {Status}, Error: {Error}", response.StatusCode, error);
            return null;
        }

        var gist = await response.Content.ReadFromJsonAsync<GistResponse>(OcCliJsonContext.Default.GistResponse, ct);
        return gist == null ? null : new GistInfo { Id = gist.Id, Description = gist.Description ?? string.Empty };
    }

    public async Task<GistDetail?> GetGistAsync(string gistId, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"{GistApiBase}/{gistId}", ct);

        if (!response.IsSuccessStatusCode)
        {
            Log.Logger.Error("Failed to get Gist {GistId}. Status: {Status}", gistId, response.StatusCode);
            return null;
        }

        var gist = await response.Content.ReadFromJsonAsync<GistResponse>(OcCliJsonContext.Default.GistResponse, ct);
        if (gist == null) return null;

        return new GistDetail
        {
            Id = gist.Id,
            Description = gist.Description ?? string.Empty,
            Files = gist.Files?.ToDictionary(
                kvp => kvp.Key,
                kvp => new GistFileInfo
                {
                    Filename = kvp.Value.Filename ?? kvp.Key,
                    Content = kvp.Value.Content ?? string.Empty
                }) ?? new()
        };
    }

    public async Task<bool> UpdateGistFileAsync(string gistId, string filename, string content, CancellationToken ct = default)
    {
        var request = new UpdateGistRequest
        {
            Description = $"OpenCode Session: {gistId}",
            Files = new Dictionary<string, GistFileContent>
            {
                [filename] = new GistFileContent { Content = content }
            }
        };

        var response = await _httpClient.PatchAsJsonAsync($"{GistApiBase}/{gistId}", request, _jsonOptions, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            Log.Logger.Error("Failed to update Gist {GistId}. Status: {Status}, Error: {Error}", gistId, response.StatusCode, error);
            return false;
        }

        return true;
    }

    public async Task<bool> DeleteGistAsync(string gistId, CancellationToken ct = default)
    {
        var response = await _httpClient.DeleteAsync($"{GistApiBase}/{gistId}", ct);

        if (!response.IsSuccessStatusCode)
        {
            Log.Logger.Error("Failed to delete Gist {GistId}. Status: {Status}", gistId, response.StatusCode);
            return false;
        }

        return true;
    }

    public async Task<IReadOnlyList<GistInfo>> ListGistsAsync(CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync(GistApiBase, ct);

        if (!response.IsSuccessStatusCode)
        {
            Log.Logger.Error("Failed to list Gists. Status: {Status}", response.StatusCode);
            return [];
        }

        var gists = await response.Content.ReadFromJsonAsync<List<GistResponse>>(OcCliJsonContext.Default.ListGistResponse, ct);
        return gists?.Select(g => new GistInfo { Id = g.Id, Description = g.Description ?? string.Empty }).ToList() ?? [];
    }
}
