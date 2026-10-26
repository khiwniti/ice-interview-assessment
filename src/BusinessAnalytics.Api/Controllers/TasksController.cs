using BusinessAnalytics.Application.Tasks.DTOs;
using BusinessAnalytics.Application.Tasks.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace BusinessAnalytics.Api.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly ILogger<TasksController> _logger;
    
    public TasksController(ITaskService taskService, ILogger<TasksController> logger)
    {
        _taskService = taskService;
        _logger = logger;
    }
    
    [HttpGet("projects/{projectId:guid}/tasks")]
    [ProducesResponseType(typeof(IReadOnlyList<TaskListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TaskListItemDto>>> GetByProject(Guid projectId, CancellationToken cancellationToken)
    {
        var tasks = await _taskService.GetByProjectAsync(projectId, cancellationToken);
        return Ok(tasks);
    }
    
    [HttpGet("tasks/{id:guid}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var task = await _taskService.GetByIdAsync(id, cancellationToken);
        if (task == null)
            return NotFound();
        
        return Ok(task);
    }
    
    [HttpPost("projects/{projectId:guid}/tasks")]
    [Authorize(Policy = "ManagerOrAdmin")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TaskDto>> Create(Guid projectId, [FromBody] CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        var actorUserId = GetCurrentUserId();
        
        var task = await _taskService.CreateAsync(projectId, request, actorUserId, traceId, cancellationToken);
        
        return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
    }
    
    [HttpPut("tasks/{id:guid}")]
    [Authorize(Policy = "ManagerOrAdmin")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    public async Task<ActionResult<TaskDto>> Update(Guid id, [FromBody] UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        var actorUserId = GetCurrentUserId();
        var expectedVersion = GetIfMatchVersion();
        
        var task = await _taskService.UpdateAsync(id, request, expectedVersion, actorUserId, traceId, cancellationToken);
        if (task == null)
            return NotFound();
        
        return Ok(task);
    }
    
    [HttpPatch("tasks/{id:guid}/status")]
    [Authorize(Policy = "ContributorOrAbove")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateTaskStatusRequest request, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        var actorUserId = GetCurrentUserId();
        var expectedVersion = GetIfMatchVersion();
        
        var result = await _taskService.UpdateStatusAsync(id, request, expectedVersion, actorUserId, traceId, cancellationToken);
        if (!result)
            return NotFound();
        
        return Ok();
    }
    
    [HttpDelete("tasks/{id:guid}")]
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
        
        var result = await _taskService.DeleteAsync(id, expectedVersion, actorUserId, traceId, cancellationToken);
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
}