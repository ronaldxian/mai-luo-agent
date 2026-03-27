namespace OpenCodeSessionServer.Models;

public record UploadRequest(string SessionId, string Data);

public record UploadResponse(string Id, string Message);

public record SessionInfo(string Id, string SessionId, string Title, long TimeCreated, long TimeUpdated);

public record DownloadResponse(string SessionId, string Data);
