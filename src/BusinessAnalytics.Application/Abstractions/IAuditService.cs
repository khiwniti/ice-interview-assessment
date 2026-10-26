using BusinessAnalytics.Domain.Entities;
using BusinessAnalytics.Domain.Enums;
using System.Text.Json;

namespace BusinessAnalytics.Application.Abstractions;

public interface IAuditService
{
    Task LogAsync(
        AuditAction action,
        string entityType,
        Guid entityId,
        AuditOutcome outcome,
        string traceId,
        Guid? actorUserId = null,
        JsonDocument? metadata = null,
        CancellationToken cancellationToken = default);
}