using BusinessAnalytics.Application.Abstractions;
using BusinessAnalytics.Application.Tasks.DTOs;
using BusinessAnalytics.Domain.Entities;
using BusinessAnalytics.Domain.Enums;
using BusinessAnalytics.Domain.Abstractions;

namespace BusinessAnalytics.Application.Tasks.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    
    public TaskService(
        ITaskRepository taskRepository,
        IProjectRepository projectRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IAuditService auditService)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
    }
    
    public async Task<TaskDto> CreateAsync(Guid projectId, CreateTaskRequest request, Guid actorUserId, string traceId, CancellationToken cancellationToken = default)
    {
        var project = await _projectRepository.GetWithTasksAsync(projectId, cancellationToken);
        if (project == null)
            throw new InvalidOperationException("Project not found");
        
        if (!project.CanBeEdited())
            throw new InvalidOperationException("Cannot add tasks to a project that cannot be edited");
        
        if (request.AssignedToUserId.HasValue)
        {
            var assignee = await _userRepository.GetByIdAsync(request.AssignedToUserId.Value, cancellationToken);
            if (assignee == null)
                throw new InvalidOperationException("Assigned user not found");
            
            if (!assignee.IsActive)
                throw new InvalidOperationException("Cannot assign task to inactive user");
            
            if (assignee.DepartmentId != project.DepartmentId)
                throw new InvalidOperationException("Assigned user must belong to the project's department");
        }
        
        var task = new TaskItem(
            projectId,
            request.Title,
            request.Description,
            request.Priority,
            request.DueAt,
            request.AssignedToUserId);
        
        await _taskRepository.AddAsync(task, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        await _auditService.LogAsync(
            AuditAction.Created, 
            nameof(TaskItem), 
            task.Id, 
            AuditOutcome.Success, 
            traceId, 
            actorUserId, 
            cancellationToken: cancellationToken);
        
        return await MapToDtoAsync(task, cancellationToken);
    }
    
    public async Task<TaskDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetWithProjectAsync(id, cancellationToken);
        if (task == null) return null;
        return await MapToDtoAsync(task, cancellationToken);
    }
    
    public async Task<IReadOnlyList<TaskListItemDto>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var tasks = await _taskRepository.GetByProjectAsync(projectId, false, cancellationToken);
        var results = new List<TaskListItemDto>();
        
        foreach (var task in tasks.Where(t => t.DeletedAt == null))
        {
            var assignee = task.AssignedToUserId.HasValue 
                ? await _userRepository.GetByIdAsync(task.AssignedToUserId.Value, cancellationToken) 
                : null;
            
            results.Add(new TaskListItemDto(
                task.Id,
                task.ProjectId,
                task.Project?.Name ?? string.Empty,
                task.AssignedToUserId,
                assignee?.DisplayName,
                task.Title,
                task.Status,
                task.Priority,
                task.DueAt,
                task.IsOverdue(DateTimeOffset.UtcNow),
                task.CreatedAt
            ));
        }
        
        return results.OrderByDescending(t => t.CreatedAt).ToList();
    }
    
    public async Task<TaskDto?> UpdateAsync(Guid id, UpdateTaskRequest request, int expectedVersion, Guid actorUserId, string traceId, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetWithProjectAsync(id, cancellationToken);
        if (task == null)
            return null;
        
        if (task.Version != expectedVersion)
            throw new InvalidOperationException("Task has been modified by another user");
        
        if (task.DeletedAt.HasValue)
            throw new InvalidOperationException("Cannot update a deleted task");
        
        if (request.AssignedToUserId.HasValue)
        {
            var assignee = await _userRepository.GetByIdAsync(request.AssignedToUserId.Value, cancellationToken);
            if (assignee == null)
                throw new InvalidOperationException("Assigned user not found");
            
            if (!assignee.IsActive)
                throw new InvalidOperationException("Cannot assign task to inactive user");
            
            var project = await _projectRepository.GetByIdAsync(task.ProjectId, cancellationToken);
            if (assignee.DepartmentId != project?.DepartmentId)
                throw new InvalidOperationException("Assigned user must belong to the project's department");
        }
        
        task.UpdateDetails(request.Title, request.Description, request.Priority, request.DueAt);
        
        if (request.AssignedToUserId.HasValue)
            task.AssignToUser(request.AssignedToUserId.Value);
        else if (task.AssignedToUserId.HasValue)
            task.Unassign();
        
        await _taskRepository.UpdateAsync(task, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        await _auditService.LogAsync(
            AuditAction.Updated, 
            nameof(TaskItem), 
            task.Id, 
            AuditOutcome.Success, 
            traceId, 
            actorUserId, 
            cancellationToken: cancellationToken);
        
        return await MapToDtoAsync(task, cancellationToken);
    }
    
    public async Task<bool> UpdateStatusAsync(Guid id, UpdateTaskStatusRequest request, int expectedVersion, Guid actorUserId, string traceId, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetWithProjectAsync(id, cancellationToken);
        if (task == null)
            return false;
        
        if (task.Version != expectedVersion)
            throw new InvalidOperationException("Task has been modified by another user");
        
        var project = await _projectRepository.GetWithTasksAsync(task.ProjectId, cancellationToken);
        var isProjectEditable = project?.CanBeEdited() ?? false;
        
        task.ChangeStatus(request.Status, isProjectEditable);
        await _taskRepository.UpdateAsync(task, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        await _auditService.LogAsync(
            AuditAction.StatusChanged, 
            nameof(TaskItem), 
            task.Id, 
            AuditOutcome.Success, 
            traceId, 
            actorUserId, 
            cancellationToken: cancellationToken);
        
        return true;
    }
    
    public async Task<bool> DeleteAsync(Guid id, int expectedVersion, Guid actorUserId, string traceId, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetWithProjectAsync(id, cancellationToken);
        if (task == null)
            return false;
        
        if (task.Version != expectedVersion)
            throw new InvalidOperationException("Task has been modified by another user");
        
        task.SoftDelete();
        await _taskRepository.UpdateAsync(task, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        await _auditService.LogAsync(
            AuditAction.Deleted, 
            nameof(TaskItem), 
            task.Id, 
            AuditOutcome.Success, 
            traceId, 
            actorUserId, 
            cancellationToken: cancellationToken);
        
        return true;
    }
    
    private async Task<TaskDto> MapToDtoAsync(TaskItem task, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(task.ProjectId, cancellationToken);
        var assignee = task.AssignedToUserId.HasValue
            ? await _userRepository.GetByIdAsync(task.AssignedToUserId.Value, cancellationToken)
            : null;
        
        return new TaskDto(
            task.Id,
            task.ProjectId,
            project?.Name ?? string.Empty,
            task.AssignedToUserId,
            assignee?.DisplayName,
            task.Title,
            task.Description,
            task.Status,
            task.Priority,
            task.DueAt,
            task.CompletedAt,
            task.IsOverdue(DateTimeOffset.UtcNow),
            task.CreatedAt,
            task.UpdatedAt,
            task.DeletedAt
        );
    }
}

public interface ITaskService
{
    Task<TaskDto> CreateAsync(Guid projectId, CreateTaskRequest request, Guid actorUserId, string traceId, CancellationToken cancellationToken = default);
    Task<TaskDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskListItemDto>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<TaskDto?> UpdateAsync(Guid id, UpdateTaskRequest request, int expectedVersion, Guid actorUserId, string traceId, CancellationToken cancellationToken = default);
    Task<bool> UpdateStatusAsync(Guid id, UpdateTaskStatusRequest request, int expectedVersion, Guid actorUserId, string traceId, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, int expectedVersion, Guid actorUserId, string traceId, CancellationToken cancellationToken = default);
}