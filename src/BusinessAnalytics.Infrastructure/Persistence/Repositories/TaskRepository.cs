using BusinessAnalytics.Domain.Entities;
using BusinessAnalytics.Application.Abstractions;
using BusinessAnalytics.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace BusinessAnalytics.Infrastructure.Persistence.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly BusinessAnalyticsDbContext _context;
    
    public TaskRepository(BusinessAnalyticsDbContext context)
    {
        _context = context;
    }
    
    public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }
    
    public async Task<IReadOnlyList<TaskItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Tasks
            .AsNoTracking()
            .Where(t => t.DeletedAt == null)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<TaskItem>> GetByProjectAsync(Guid projectId, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Tasks
            .AsNoTracking()
            .Where(t => t.ProjectId == projectId);
        
        if (!includeDeleted)
            query = query.Where(t => t.DeletedAt == null);
        
        return await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<TaskItem>> GetByAssigneeAsync(Guid userId, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Tasks
            .AsNoTracking()
            .Where(t => t.AssignedToUserId == userId);
        
        if (!includeDeleted)
            query = query.Where(t => t.DeletedAt == null);
        
        return await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<TaskItem>> GetByStatusAsync(Guid projectId, BusinessAnalytics.Domain.Enums.TaskStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Tasks
            .AsNoTracking()
            .Where(t => t.ProjectId == projectId && t.Status == status && t.DeletedAt == null)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<TaskItem?> GetWithProjectAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Tasks
            .Include(t => t.Project)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }
    
    public async Task AddAsync(TaskItem entity, CancellationToken cancellationToken = default)
    {
        await _context.Tasks.AddAsync(entity, cancellationToken);
    }
    
    public async Task UpdateAsync(TaskItem entity, CancellationToken cancellationToken = default)
    {
        _context.Tasks.Update(entity);
        await Task.CompletedTask;
    }
    
    public async Task DeleteAsync(TaskItem entity, CancellationToken cancellationToken = default)
    {
        _context.Tasks.Remove(entity);
        await Task.CompletedTask;
    }
    
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Tasks.AnyAsync(t => t.Id == id, cancellationToken);
    }
}