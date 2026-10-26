using BusinessAnalytics.Domain.Entities;
using BusinessAnalytics.Application.Abstractions;
using BusinessAnalytics.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace BusinessAnalytics.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly BusinessAnalyticsDbContext _context;
    
    public UserRepository(BusinessAnalyticsDbContext context)
    {
        _context = context;
    }
    
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }
    
    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .OrderBy(u => u.DisplayName)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }
    
    public async Task<User?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
    }
    
    public async Task<IReadOnlyList<User>> GetByDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(u => u.DepartmentId == departmentId)
            .OrderBy(u => u.DisplayName)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<User>> GetActiveByDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(u => u.DepartmentId == departmentId && u.IsActive)
            .OrderBy(u => u.DisplayName)
            .ToListAsync(cancellationToken);
    }
    
    public async Task AddAsync(User entity, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(entity, cancellationToken);
    }
    
    public async Task UpdateAsync(User entity, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(entity);
        await Task.CompletedTask;
    }
    
    public async Task DeleteAsync(User entity, CancellationToken cancellationToken = default)
    {
        _context.Users.Remove(entity);
        await Task.CompletedTask;
    }
    
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users.AnyAsync(u => u.Id == id, cancellationToken);
    }
}