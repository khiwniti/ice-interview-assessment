using BusinessAnalytics.Domain.Abstractions;
using BusinessAnalytics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BusinessAnalytics.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly BusinessAnalyticsDbContext _context;
    
    public UnitOfWork(BusinessAnalyticsDbContext context)
    {
        _context = context;
    }
    
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}