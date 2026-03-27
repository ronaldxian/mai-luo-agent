using Microsoft.AspNetCore.Mvc;
using OpenCodeSessionServer.Models;
using OpenCodeSessionServer.Services;

namespace OpenCodeSessionServer.Controllers;

[ApiController]
[Route("[controller]")]
public class SessionsController : ControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly ILogger<SessionsController> _logger;

    public SessionsController(ISessionService sessionService, ILogger<SessionsController> logger)
    {
        _sessionService = sessionService;
        _logger = logger;
    }

    [HttpPost("upload")]
    public async Task<ActionResult<UploadResponse>> Upload([FromBody] UploadRequest request)
    {
        try
        {
            _logger.LogInformation("Uploading session: {SessionId}", request.SessionId);
            var response = await _sessionService.UploadAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload session: {SessionId}", request.SessionId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<SessionInfo>>> List()
    {
        try
        {
            _logger.LogInformation("Listing today's sessions");
            var sessions = await _sessionService.ListTodaySessionsAsync();
            return Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list sessions");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DownloadResponse>> Download(string id)
    {
        try
        {
            _logger.LogInformation("Downloading session: {Id}", id);
            var response = await _sessionService.DownloadAsync(id);
            return Ok(response);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { error = $"Session not found: {id}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download session: {Id}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            _logger.LogInformation("Deleting session: {Id}", id);
            await _sessionService.DeleteAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete session: {Id}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
