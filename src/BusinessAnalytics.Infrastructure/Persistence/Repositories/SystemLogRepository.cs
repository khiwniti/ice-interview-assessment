using BusinessAnalytics.Domain.Entities;
using BusinessAnalytics.Application.Abstractions;
using BusinessAnalytics.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace BusinessAnalytics.Infrastructure.Persistence.Repositories;

public class SystemLogRepository : ISystemLogRepository
{
    private readonly BusinessAnalyticsDbContext _context;
    
    public SystemLogRepository(BusinessAnalyticsDbContext context)
    {
        _context = context;
    }
    
    public async Task<SystemLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SystemLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }
    
    public async Task<IReadOnlyList<SystemLog>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SystemLogs
            .AsNoTracking()
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<SystemLog>> GetByEntityAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        return await _context.SystemLogs
            .AsNoTracking()
            .Where(s => s.EntityType == entityType && s.EntityId == entityId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<SystemLog>> GetByActorAsync(Guid actorUserId, CancellationToken cancellationToken = default)
    {
        return await _context.SystemLogs
            .AsNoTracking()
            .Where(s => s.ActorUserId == actorUserId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<SystemLog>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _context.SystemLogs
            .AsNoTracking()
            .OrderByDescending(s => s.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
    
    public async Task AddAsync(SystemLog entity, CancellationToken cancellationToken = default)
    {
        await _context.SystemLogs.AddAsync(entity, cancellationToken);
    }
    
    public async Task UpdateAsync(SystemLog entity, CancellationToken cancellationToken = default)
    {
        _context.SystemLogs.Update(entity);
        await Task.CompletedTask;
    }
    
    public async Task DeleteAsync(SystemLog entity, CancellationToken cancellationToken = default)
    {
        _context.SystemLogs.Remove(entity);
        await Task.CompletedTask;
    }
    
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SystemLogs.AnyAsync(s => s.Id == id, cancellationToken);
    }
}