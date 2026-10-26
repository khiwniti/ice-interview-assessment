using BusinessAnalytics.Application.Users.DTOs;
using BusinessAnalytics.Application.Users.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace BusinessAnalytics.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;
    
    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }
    
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserListItemDto>>> GetAll(CancellationToken cancellationToken)
    {
        var users = await _userService.GetAllAsync(cancellationToken);
        return Ok(users);
    }
    
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetByIdAsync(id, cancellationToken);
        if (user == null)
            return NotFound();
        
        return Ok(user);
    }
    
    [HttpGet("department/{departmentId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<UserListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserListItemDto>>> GetByDepartment(Guid departmentId, CancellationToken cancellationToken)
    {
        var users = await _userService.GetByDepartmentAsync(departmentId, cancellationToken);
        return Ok(users);
    }
    
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        var actorUserId = GetCurrentUserId();
        
        var user = await _userService.CreateAsync(request, actorUserId, traceId, cancellationToken);
        
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }
    
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        var actorUserId = GetCurrentUserId();
        
        var user = await _userService.UpdateAsync(id, request, actorUserId, traceId, cancellationToken);
        if (user == null)
            return NotFound();
        
        return Ok(user);
    }
    
    [HttpPut("{id:guid}/password")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdatePassword(Guid id, [FromBody] UpdateUserPasswordRequest request, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        var actorUserId = GetCurrentUserId();
        
        var result = await _userService.UpdatePasswordAsync(id, request, actorUserId, traceId, cancellationToken);
        if (!result)
            return NotFound();
        
        return Ok();
    }
    
    [HttpPut("{id:guid}/department")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateDepartment(Guid id, [FromBody] UpdateDepartmentRequest request, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        var actorUserId = GetCurrentUserId();
        
        var result = await _userService.UpdateDepartmentAsync(id, request.DepartmentId, actorUserId, traceId, cancellationToken);
        if (!result)
            return NotFound();
        
        return Ok();
    }
    
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        var actorUserId = GetCurrentUserId();
        
        var result = await _userService.DeactivateAsync(id, actorUserId, traceId, cancellationToken);
        if (!result)
            return NotFound();
        
        return NoContent();
    }
    
    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        var actorUserId = GetCurrentUserId();
        
        var result = await _userService.ActivateAsync(id, actorUserId, traceId, cancellationToken);
        if (!result)
            return NotFound();
        
        return Ok();
    }
    
    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var id) ? id : Guid.Empty;
    }
    
    // DTO for department update
    public record UpdateDepartmentRequest(Guid DepartmentId);
}