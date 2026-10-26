using BusinessAnalytics.Domain.Entities;
using BusinessAnalytics.Application.Abstractions;
using BusinessAnalytics.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace BusinessAnalytics.Infrastructure.Persistence.Repositories;

public class DepartmentRepository : IDepartmentRepository
{
    private readonly BusinessAnalyticsDbContext _context;
    
    public DepartmentRepository(BusinessAnalyticsDbContext context)
    {
        _context = context;
    }
    
    public async Task<Department?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }
    
    public async Task<IReadOnlyList<Department>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<Department?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Name == name, cancellationToken);
    }
    
    public async Task<IReadOnlyList<Department>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Departments
            .AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .ToListAsync(cancellationToken);
    }
    
    public async Task AddAsync(Department entity, CancellationToken cancellationToken = default)
    {
        await _context.Departments.AddAsync(entity, cancellationToken);
    }
    
    public async Task UpdateAsync(Department entity, CancellationToken cancellationToken = default)
    {
        _context.Departments.Update(entity);
        await Task.CompletedTask;
    }
    
    public async Task DeleteAsync(Department entity, CancellationToken cancellationToken = default)
    {
        _context.Departments.Remove(entity);
        await Task.CompletedTask;
    }
    
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Departments.AnyAsync(d => d.Id == id, cancellationToken);
    }
}