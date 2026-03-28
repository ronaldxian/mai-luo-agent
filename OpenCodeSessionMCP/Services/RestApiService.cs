using System.Net.Http.Json;
using System.Text.Json;
using OpenCodeSessionMCP.Configuration;
using OpenCodeSessionMCP.Models;

namespace OpenCodeSessionMCP.Services;

public sealed class RestApiService : IRestApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly JsonSerializerOptions _jsonOptions;

    public RestApiService(HttpClient httpClient, AppSettings settings)
    {
        _httpClient = httpClient;
        _baseUrl = settings.RestApi.BaseUrl.TrimEnd('/');

        if (!string.IsNullOrEmpty(settings.RestApi.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", settings.RestApi.ApiKey);
        }

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public RestApiService(HttpClient httpClient, string baseUrl, string apiKey)
    {
        _httpClient = httpClient;
        _baseUrl = baseUrl.TrimEnd('/');

        if (!string.IsNullOrEmpty(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        }

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<string> UploadSessionAsync(string sessionId, string title, string jsonData, CancellationToken ct = default)
    {
        var base64Data = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(jsonData));
        
        var request = new UploadRequest
        {
            SessionId = sessionId,
            Title = title,
            Data = base64Data
        };

        var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/sessions/upload", request, _jsonOptions, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"API upload failed: {response.StatusCode} - {error}");
        }

        var result = await response.Content.ReadFromJsonAsync<UploadResponse>(_jsonOptions, ct);

        return result?.Id ?? throw new InvalidOperationException("Upload response missing id");
    }

    public async Task<IReadOnlyList<RemoteSession>> ListSessionsAsync(CancellationToken ct = default)
    {
        var response = await _httpClient.GetFromJsonAsync<SessionListResponse>($"{_baseUrl}/sessions", _jsonOptions, ct);

        return response?.Sessions ?? [];
    }

    public async Task<string> DownloadSessionAsync(string remoteId, CancellationToken ct = default)
    {
        var response = await _httpClient.GetFromJsonAsync<DownloadResponse>($"{_baseUrl}/sessions/{remoteId}", _jsonOptions, ct);

        if (string.IsNullOrEmpty(response?.Data))
        {
            throw new InvalidOperationException("Download response missing data");
        }

        var jsonData = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(response.Data));
        return jsonData;
    }

    public async Task DeleteSessionAsync(string remoteId, CancellationToken ct = default)
    {
        var response = await _httpClient.DeleteAsync($"{_baseUrl}/sessions/{remoteId}", ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"API delete failed: {response.StatusCode} - {error}");
        }
    }
}
