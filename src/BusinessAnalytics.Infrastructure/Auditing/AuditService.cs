using BusinessAnalytics.Application.Abstractions;
using BusinessAnalytics.Domain.Entities;
using BusinessAnalytics.Domain.Enums;
using BusinessAnalytics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BusinessAnalytics.Infrastructure.Auditing;

public class AuditService : IAuditService
{
    private readonly BusinessAnalyticsDbContext _context;
    
    public AuditService(BusinessAnalyticsDbContext context)
    {
        _context = context;
    }
    
    public async Task LogAsync(
        AuditAction action,
        string entityType,
        Guid entityId,
        AuditOutcome outcome,
        string traceId,
        Guid? actorUserId = null,
        JsonDocument? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var log = new SystemLog(action, entityType, entityId, outcome, traceId, actorUserId, metadata);
        await _context.SystemLogs.AddAsync(log, cancellationToken);
        // Note: SaveChanges is called by the UnitOfWork in the service layer
    }
}