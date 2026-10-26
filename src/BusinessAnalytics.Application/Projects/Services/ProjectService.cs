using BusinessAnalytics.Application.Abstractions;
using BusinessAnalytics.Application.Projects.DTOs;
using BusinessAnalytics.Domain.Entities;
using BusinessAnalytics.Domain.Enums;
using BusinessAnalytics.Domain.Abstractions;

namespace BusinessAnalytics.Application.Projects.Services;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    
    public ProjectService(
        IProjectRepository projectRepository,
        IDepartmentRepository departmentRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IAuditService auditService)
    {
        _projectRepository = projectRepository;
        _departmentRepository = departmentRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
    }
    
    public async Task<ProjectDto> CreateAsync(CreateProjectRequest request, Guid actorUserId, string traceId, CancellationToken cancellationToken = default)
    {
        var department = await _departmentRepository.GetByIdAsync(request.DepartmentId, cancellationToken);
        if (department == null)
            throw new InvalidOperationException("Department not found");
        
        if (!department.IsActive)
            throw new InvalidOperationException("Cannot create project in inactive department");
        
        var owner = await _userRepository.GetByIdAsync(request.OwnerUserId, cancellationToken);
        if (owner == null)
            throw new InvalidOperationException("Owner user not found");
        
        if (!owner.IsActive)
            throw new InvalidOperationException("Owner user is not active");
        
        if (owner.DepartmentId != request.DepartmentId)
            throw new InvalidOperationException("Owner must belong to the project's department");
        
        var project = new Project(
            request.DepartmentId,
            request.OwnerUserId,
            request.Name,
            request.Description,
            request.Status,
            request.StartAt,
            request.DueAt);
        
        await _projectRepository.AddAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        await _auditService.LogAsync(
            AuditAction.Created, 
            nameof(Project), 
            project.Id, 
            AuditOutcome.Success, 
            traceId, 
            actorUserId, 
            cancellationToken: cancellationToken);
        
        return await MapToDtoAsync(project, cancellationToken);
    }
    
    public async Task<ProjectDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var project = await _projectRepository.GetWithTasksAsync(id, cancellationToken);
        if (project == null) return null;
        return await MapToDtoAsync(project, cancellationToken);
    }
    
    public async Task<IReadOnlyList<ProjectListItemDto>> GetAllAsync(
        Guid? departmentId = null, 
        ProjectStatus? status = null, 
        Guid? ownerUserId = null,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Project> projects;
        
        if (departmentId.HasValue)
        {
            projects = await _projectRepository.GetByDepartmentAsync(departmentId.Value, false, cancellationToken);
        }
        else if (ownerUserId.HasValue)
        {
            projects = await _projectRepository.GetByOwnerAsync(ownerUserId.Value, cancellationToken);
        }
        else if (status.HasValue)
        {
            // This would need a department ID, so get all and filter
            projects = await _projectRepository.GetAllAsync(cancellationToken);
        }
        else
        {
            projects = await _projectRepository.GetAllAsync(cancellationToken);
        }
        
        // Apply filters
        var filtered = projects.Where(p => p.DeletedAt == null);
        
        if (status.HasValue)
            filtered = filtered.Where(p => p.Status == status.Value);
        
        if (ownerUserId.HasValue)
            filtered = filtered.Where(p => p.OwnerUserId == ownerUserId.Value);
        
        var results = new List<ProjectListItemDto>();
        foreach (var project in filtered)
        {
            var department = await _departmentRepository.GetByIdAsync(project.DepartmentId, cancellationToken);
            var owner = await _userRepository.GetByIdAsync(project.OwnerUserId, cancellationToken);
            
            var taskCount = project.GetNonCancelledTaskCount();
            var completedCount = project.GetCompletedTaskCount();
            var progress = taskCount > 0 ? (decimal)completedCount / taskCount * 100 : null;
            
            results.Add(new ProjectListItemDto(
                project.Id,
                project.DepartmentId,
                department?.Name ?? string.Empty,
                project.OwnerUserId,
                owner?.DisplayName ?? string.Empty,
                project.Name,
                project.Status,
                project.StartAt,
                project.DueAt,
                taskCount,
                completedCount,
                progress?.Round(2),
                project.CreatedAt
            ));
        }
        
        return results.OrderByDescending(p => p.CreatedAt).ToList();
    }
    
    public async Task<ProjectDto?> UpdateAsync(Guid id, UpdateProjectRequest request, int expectedVersion, Guid actorUserId, string traceId, CancellationToken cancellationToken = default)
    {
        var project = await _projectRepository.GetWithTasksAsync(id, cancellationToken);
        if (project == null)
            return null;
        
        if (project.Version != expectedVersion)
            throw new InvalidOperationException("Project has been modified by another user");
        
        if (!project.CanBeEdited())
            throw new InvalidOperationException("Project cannot be edited in its current state");
        
        project.UpdateDetails(request.Name, request.Description, request.Status, request.StartAt, request.DueAt);
        await _projectRepository.UpdateAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        await _auditService.LogAsync(
            AuditAction.Updated, 
            nameof(Project), 
            project.Id, 
            AuditOutcome.Success, 
            traceId, 
            actorUserId, 
            cancellationToken: cancellationToken);
        
        return await MapToDtoAsync(project, cancellationToken);
    }
    
    public async Task<bool> UpdateStatusAsync(Guid id, ProjectStatus newStatus, int expectedVersion, Guid actorUserId, string traceId, CancellationToken cancellationToken = default)
    {
        var project = await _projectRepository.GetWithTasksAsync(id, cancellationToken);
        if (project == null)
            return false;
        
        if (project.Version != expectedVersion)
            throw new InvalidOperationException("Project has been modified by another user");
        
        project.UpdateStatus(newStatus);
        await _projectRepository.UpdateAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        await _auditService.LogAsync(
            AuditAction.StatusChanged, 
            nameof(Project), 
            project.Id, 
            AuditOutcome.Success, 
            traceId, 
            actorUserId, 
            cancellationToken: cancellationToken);
        
        return true;
    }
    
    public async Task<bool> DeleteAsync(Guid id, int expectedVersion, Guid actorUserId, string traceId, CancellationToken cancellationToken = default)
    {
        var project = await _projectRepository.GetWithTasksAsync(id, cancellationToken);
        if (project == null)
            return false;
        
        if (project.Version != expectedVersion)
            throw new InvalidOperationException("Project has been modified by another user");
        
        project.SoftDelete();
        await _projectRepository.UpdateAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        await _auditService.LogAsync(
            AuditAction.Deleted, 
            nameof(Project), 
            project.Id, 
            AuditOutcome.Success, 
            traceId, 
            actorUserId, 
            cancellationToken: cancellationToken);
        
        return true;
    }
    
    private async Task<ProjectDto> MapToDtoAsync(Project project, CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetByIdAsync(project.DepartmentId, cancellationToken);
        var owner = await _userRepository.GetByIdAsync(project.OwnerUserId, cancellationToken);
        
        var taskCount = project.GetNonCancelledTaskCount();
        var completedCount = project.GetCompletedTaskCount();
        var overdueCount = project.GetOverdueTaskCount(DateTimeOffset.UtcNow);
        var progress = taskCount > 0 ? (decimal)completedCount / taskCount * 100 : null;
        
        return new ProjectDto(
            project.Id,
            project.DepartmentId,
            department?.Name ?? string.Empty,
            project.OwnerUserId,
            owner?.DisplayName ?? string.Empty,
            project.Name,
            project.Description,
            project.Status,
            project.StartAt,
            project.DueAt,
            taskCount,
            completedCount,
            overdueCount,
            progress?.Round(2),
            project.CreatedAt,
            project.UpdatedAt,
            project.DeletedAt
        );
    }
}

public interface IProjectService
{
    Task<ProjectDto> CreateAsync(CreateProjectRequest request, Guid actorUserId, string traceId, CancellationToken cancellationToken = default);
    Task<ProjectDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectListItemDto>> GetAllAsync(Guid? departmentId = null, ProjectStatus? status = null, Guid? ownerUserId = null, CancellationToken cancellationToken = default);
    Task<ProjectDto?> UpdateAsync(Guid id, UpdateProjectRequest request, int expectedVersion, Guid actorUserId, string traceId, CancellationToken cancellationToken = default);
    Task<bool> UpdateStatusAsync(Guid id, ProjectStatus newStatus, int expectedVersion, Guid actorUserId, string traceId, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, int expectedVersion, Guid actorUserId, string traceId, CancellationToken cancellationToken = default);
}