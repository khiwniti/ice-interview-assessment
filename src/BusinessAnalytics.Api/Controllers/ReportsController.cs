using BusinessAnalytics.Application.Reports.DTOs;
using BusinessAnalytics.Application.Reports.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace BusinessAnalytics.Api.Controllers;

[ApiController]
[Route("api/v1/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportsController> _logger;
    
    public ReportsController(IReportService reportService, ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }
    
    [HttpGet("departments/progress")]
    [ProducesResponseType(typeof(IReadOnlyList<DepartmentProgressReportDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DepartmentProgressReportDto>>> GetDepartmentProgress(
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var reports = await _reportService.GetDepartmentProgressAsync(departmentId, cancellationToken);
        return Ok(reports);
    }
    
    [HttpGet("projects/{projectId:guid}/summary")]
    [ProducesResponseType(typeof(ProjectSummaryReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectSummaryReportDto>> GetProjectSummary(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var report = await _reportService.GetProjectSummaryAsync(projectId, cancellationToken);
        if (report == null)
            return NotFound();
        
        return Ok(report);
    }
    
    [HttpGet("departments/progress/export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportDepartmentProgress(
        [FromQuery] string format = "json",
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(format, "json", StringComparison.OrdinalIgnoreCase) && 
            !string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Format must be 'json' or 'csv'" });
        }
        
        var data = await _reportService.ExportDepartmentProgressAsync(format, departmentId, cancellationToken);
        
        var contentType = string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase)
            ? "text/csv; charset=utf-8"
            : "application/json";
        
        var fileName = $"department-progress-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.{format.ToLower()}";
        
        Response.Headers.ContentDisposition = $"attachment; filename=\"{fileName}\"";
        
        return File(data, contentType);
    }
}