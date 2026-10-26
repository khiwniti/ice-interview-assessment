using BusinessAnalytics.Application.Projects.DTOs;
using BusinessAnalytics.Application.Projects.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace BusinessAnalytics.Api.Controllers;

[ApiController]
[Route("api/v1/projects")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly ILogger<ProjectsController> _logger;
    
    public ProjectsController(IProjectService projectService, ILogger<ProjectsController> logger)
    {
        _projectService = projectService;
        _logger = logger;
    }
    
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProjectListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProjectListItemDto>>> GetAll(
        [FromQuery] Guid? departmentId = null,
        [FromQuery] BusinessAnalytics.Domain.Enums.ProjectStatus? status = null,
        [FromQuery] Guid? ownerUserId = null,
        CancellationToken cancellationToken = default)
    {
        var projects = await _projectService.GetAllAsync(departmentId, status, ownerUserId, cancellationToken);
        return Ok(projects);
    }
    
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var project = await _projectService.GetByIdAsync(id, cancellationToken);
        if (project == null)
            return NotFound();
        
        return Ok(project);
    }
    
    [HttpPost]
    [Authorize(Policy = "ManagerOrAdmin")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProjectDto>> Create([FromBody] CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        var actorUserId = GetCurrentUserId();
        
        var project = await _projectService.CreateAsync(request, actorUserId, traceId, cancellationToken);
        
        return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
    }
    
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "ManagerOrAdmin")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    public async Task<ActionResult<ProjectDto>> Update(Guid id, [FromBody] UpdateProjectRequest request, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        var actorUserId = GetCurrentUserId();
        var expectedVersion = GetIfMatchVersion();
        
        var project = await _projectService.UpdateAsync(id, request, expectedVersion, actorUserId, traceId, cancellationToken);
        if (project == null)
            return NotFound();
        
        return Ok(project);
    }
    
    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = "ManagerOrAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateProjectStatusRequest request, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        var actorUserId = GetCurrentUserId();
        var expectedVersion = GetIfMatchVersion();
        
        var result = await _projectService.UpdateStatusAsync(id, request.Status, expectedVersion, actorUserId, traceId, cancellationToken);
        if (!result)
            return NotFound();
        
        return Ok();
    }
    
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "ManagerOrAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        var actorUserId = GetCurrentUserId();
        var expectedVersion = GetIfMatchVersion();
        
        var result = await _projectService.DeleteAsync(id, expectedVersion, actorUserId, traceId, cancellationToken);
        if (!result)
            return NotFound();
        
        return NoContent();
    }
    
    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var id) ? id : Guid.Empty;
    }
    
    private int GetIfMatchVersion()
    {
        var ifMatch = Request.Headers.IfMatch.FirstOrDefault();
        if (string.IsNullOrEmpty(ifMatch))
            throw new InvalidOperationException("If-Match header is required");
        
        return int.TryParse(ifMatch, out var version) ? version : throw new InvalidOperationException("Invalid If-Match version");
    }
    
    public record UpdateProjectStatusRequest(BusinessAnalytics.Domain.Enums.ProjectStatus Status);
}