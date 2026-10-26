using BusinessAnalytics.Domain.Entities;
using BusinessAnalytics.Application.Abstractions;
using BusinessAnalytics.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace BusinessAnalytics.Infrastructure.Persistence.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly BusinessAnalyticsDbContext _context;
    
    public ProjectRepository(BusinessAnalyticsDbContext context)
    {
        _context = context;
    }
    
    public async Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }
    
    public async Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Projects
            .AsNoTracking()
            .Where(p => p.DeletedAt == null)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<Project>> GetByDepartmentAsync(Guid departmentId, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Projects
            .AsNoTracking()
            .Where(p => p.DepartmentId == departmentId);
        
        if (!includeDeleted)
            query = query.Where(p => p.DeletedAt == null);
        
        return await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<Project>> GetByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.Projects
            .AsNoTracking()
            .Where(p => p.OwnerUserId == ownerUserId && p.DeletedAt == null)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<Project>> GetByStatusAsync(Guid departmentId, BusinessAnalytics.Domain.Enums.ProjectStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Projects
            .AsNoTracking()
            .Where(p => p.DepartmentId == departmentId && p.Status == status && p.DeletedAt == null)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<Project?> GetWithTasksAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Projects
            .Include(p => p.Tasks.Where(t => t.DeletedAt == null))
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }
    
    public async Task AddAsync(Project entity, CancellationToken cancellationToken = default)
    {
        await _context.Projects.AddAsync(entity, cancellationToken);
    }
    
    public async Task UpdateAsync(Project entity, CancellationToken cancellationToken = default)
    {
        _context.Projects.Update(entity);
        await Task.CompletedTask;
    }
    
    public async Task DeleteAsync(Project entity, CancellationToken cancellationToken = default)
    {
        _context.Projects.Remove(entity);
        await Task.CompletedTask;
    }
    
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Projects.AnyAsync(p => p.Id == id, cancellationToken);
    }
}