using Microsoft.AspNetCore.Mvc;
using Nocturne.API.Attributes;
using Nocturne.Core.Contracts;
using Nocturne.Core.Models;

namespace Nocturne.API.Controllers.V1;

/// <summary>
/// Status controller that provides 1:1 compatibility with Nightscout status endpoint.
/// Returns HTML "STATUS OK" by default (matching Nightscout), or JSON when requested via Accept header.
/// For detailed JSON status, use the V4 status endpoint at /api/v4/status
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class StatusController : ControllerBase
{
    private readonly IStatusService _statusService;
    private readonly ILogger<StatusController> _logger;

    public StatusController(IStatusService statusService, ILogger<StatusController> logger)
    {
        _statusService = statusService;
        _logger = logger;
    }

    /// <summary>
    /// Get the current system status.
    /// Returns HTML by default for Nightscout compatibility, or JSON if Accept header requests it.
    /// </summary>
    /// <returns>Status response in HTML or JSON format</returns>
    [HttpGet]
    [NightscoutEndpoint("/api/v1/status")]
    [Produces("text/html", "application/json")]
    public async Task<IActionResult> GetStatus()
    {
        _logger.LogDebug(
            "Status endpoint requested from {RemoteIpAddress}",
            HttpContext.Connection.RemoteIpAddress
        );

        try
        {
            // Check Accept header to determine response format
            var acceptHeader = Request.Headers.Accept.ToString().ToLowerInvariant();
            var wantsJson = acceptHeader.Contains("application/json");

            if (wantsJson)
            {
                // Return full JSON status for clients that request it
                var status = await _statusService.GetSystemStatusAsync();
                return Ok(status);
            }

            // Default: Return simple HTML "STATUS OK" for Nightscout compatibility
            // This matches the legacy Nightscout behavior exactly
            return Content("<h1>STATUS OK</h1>", "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating status response");

            // Return error status
            var acceptHeader = Request.Headers.Accept.ToString().ToLowerInvariant();
            if (acceptHeader.Contains("application/json"))
            {
                return Ok(
                    new StatusResponse
                    {
                        Status = "error",
                        Name = "Nocturne",
                        Version = "unknown",
                        ServerTime = DateTime.UtcNow,
                    }
                );
            }

            return Content("<h1>STATUS ERROR</h1>", "text/html");
        }
    }
}
