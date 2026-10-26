using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BusinessAnalytics.Api.Controllers;

[ApiController]
[Route("health")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;
    
    public HealthController(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }
    
    [HttpGet("live")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> Liveness(CancellationToken cancellationToken)
    {
        var result = await _healthCheckService.CheckHealthAsync(
            predicate: check => check.Tags.Contains("live"), 
            cancellationToken);
        
        return result.Status == HealthStatus.Healthy 
            ? Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow }) 
            : StatusCode(503, new { status = "unhealthy", timestamp = DateTimeOffset.UtcNow });
    }
    
    [HttpGet("ready")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> Readiness(CancellationToken cancellationToken)
    {
        var result = await _healthCheckService.CheckHealthAsync(
            predicate: check => check.Tags.Contains("ready"), 
            cancellationToken);
        
        return result.Status == HealthStatus.Healthy 
            ? Ok(new { status = "ready", timestamp = DateTimeOffset.UtcNow }) 
            : StatusCode(503, new { status = "not ready", timestamp = DateTimeOffset.UtcNow });
    }
}