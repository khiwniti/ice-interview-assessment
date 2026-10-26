using BusinessAnalytics.Domain.Enums;
using System.Text.Json;

namespace BusinessAnalytics.Domain.Entities;

public class SystemLog : BaseEntity
{
    public Guid? ActorUserId { get; private set; }
    public AuditAction Action { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public AuditOutcome Outcome { get; private set; }
    public string TraceId { get; private set; } = string.Empty;
    public JsonDocument? Metadata { get; private set; }
    
    private SystemLog() { } // EF Core
    
    public SystemLog(
        AuditAction action, 
        string entityType, 
        Guid entityId, 
        AuditOutcome outcome, 
        string traceId,
        Guid? actorUserId = null,
        JsonDocument? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(entityType))
            throw new ArgumentException("Entity type is required", nameof(entityType));
        
        if (entityId == Guid.Empty)
            throw new ArgumentException("Entity ID is required", nameof(entityId));
        
        if (string.IsNullOrWhiteSpace(traceId))
            throw new ArgumentException("Trace ID is required", nameof(traceId));
        
        Action = action;
        EntityType = entityType.Trim();
        EntityId = entityId;
        Outcome = outcome;
        TraceId = traceId.Trim();
        ActorUserId = actorUserId;
        Metadata = metadata;
    }
}