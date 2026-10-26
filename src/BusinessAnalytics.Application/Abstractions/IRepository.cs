using BusinessAnalytics.Domain.Entities;

namespace BusinessAnalytics.Application.Abstractions;

public interface IDepartmentRepository : IRepository<Department>
{
    Task<Department?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Department>> GetActiveAsync(CancellationToken cancellationToken = default);
}

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetByDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetActiveByDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default);
}

public interface IProjectRepository : IRepository<Project>
{
    Task<IReadOnlyList<Project>> GetByDepartmentAsync(Guid departmentId, bool includeDeleted = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Project>> GetByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Project>> GetByStatusAsync(Guid departmentId, ProjectStatus status, CancellationToken cancellationToken = default);
    Task<Project?> GetWithTasksAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface ITaskRepository : IRepository<TaskItem>
{
    Task<IReadOnlyList<TaskItem>> GetByProjectAsync(Guid projectId, bool includeDeleted = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskItem>> GetByAssigneeAsync(Guid userId, bool includeDeleted = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskItem>> GetByStatusAsync(Guid projectId, TaskStatus status, CancellationToken cancellationToken = default);
    Task<TaskItem?> GetWithProjectAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface ISystemLogRepository : IRepository<SystemLog>
{
    Task<IReadOnlyList<SystemLog>> GetByEntityAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SystemLog>> GetByActorAsync(Guid actorUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SystemLog>> GetRecentAsync(int count, CancellationToken cancellationToken = default);
}