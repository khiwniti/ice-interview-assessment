namespace BusinessAnalytics.Domain.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; protected set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; protected set; } = DateTimeOffset.UtcNow;
    public int Version { get; protected set; } = 1;
    
    public void IncrementVersion() => Version++;
    
    public void UpdateTimestamp() => UpdatedAt = DateTimeOffset.UtcNow;
}