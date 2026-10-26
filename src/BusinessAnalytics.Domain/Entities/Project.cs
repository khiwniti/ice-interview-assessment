using BusinessAnalytics.Domain.Enums;

namespace BusinessAnalytics.Domain.Entities;

public class Project : BaseEntity
{
    public Guid DepartmentId { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public ProjectStatus Status { get; private set; } = ProjectStatus.Planned;
    public DateTimeOffset? StartAt { get; private set; }
    public DateTimeOffset? DueAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    
    private readonly List<TaskItem> _tasks = [];
    public IReadOnlyCollection<TaskItem> Tasks => _tasks.AsReadOnly();
    
    private Project() { } // EF Core
    
    public Project(Guid departmentId, Guid ownerUserId, string name, string description = "", ProjectStatus status = ProjectStatus.Planned, DateTimeOffset? startAt = null, DateTimeOffset? dueAt = null)
    {
        if (departmentId == Guid.Empty)
            throw new ArgumentException("Department ID is required", nameof(departmentId));
        
        if (ownerUserId == Guid.Empty)
            throw new ArgumentException("Owner user ID is required", nameof(ownerUserId));
        
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Project name cannot be empty", nameof(name));
        
        if (startAt.HasValue && dueAt.HasValue && dueAt < startAt)
            throw new ArgumentException("Due date cannot be before start date", nameof(dueAt));
        
        DepartmentId = departmentId;
        OwnerUserId = ownerUserId;
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Status = status;
        StartAt = startAt;
        DueAt = dueAt;
    }
    
    public void UpdateDetails(string name, string description, ProjectStatus status, DateTimeOffset? startAt, DateTimeOffset? dueAt)
    {
        if (DeletedAt.HasValue)
            throw new InvalidOperationException("Cannot update a deleted project");
        
        if (Status == ProjectStatus.Completed)
            throw new InvalidOperationException("Cannot modify a completed project");
        
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Project name cannot be empty", nameof(name));
        
        if (startAt.HasValue && dueAt.HasValue && dueAt < startAt)
            throw new ArgumentException("Due date cannot be before start date", nameof(dueAt));
        
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Status = status;
        StartAt = startAt;
        DueAt = dueAt;
        UpdateTimestamp();
        IncrementVersion();
    }
    
    public void UpdateStatus(ProjectStatus newStatus)
    {
        if (DeletedAt.HasValue)
            throw new InvalidOperationException("Cannot update a deleted project");
        
        // Business rule: can only become Completed if all non-cancelled tasks are Done and at least one non-cancelled task exists
        if (newStatus == ProjectStatus.Completed)
        {
            var nonCancelledTasks = _tasks.Where(t => t.Status != TaskStatus.Cancelled && t.DeletedAt == null).ToList();
            if (!nonCancelledTasks.Any())
                throw new InvalidOperationException("Project must have at least one non-cancelled task to be completed");
            
            if (nonCancelledTasks.Any(t => t.Status != TaskStatus.Done))
                throw new InvalidOperationException("All non-cancelled tasks must be Done to complete the project");
        }
        
        // Business rule: reopening a completed project
        if (Status == ProjectStatus.Completed && newStatus != ProjectStatus.Completed)
        {
            // Allow reopening
        }
        
        Status = newStatus;
        UpdateTimestamp();
        IncrementVersion();
    }
    
    public void SoftDelete()
    {
        if (DeletedAt.HasValue)
            return; // Already deleted
        
        DeletedAt = DateTimeOffset.UtcNow;
        UpdateTimestamp();
        IncrementVersion();
        
        // Soft-delete all active tasks
        foreach (var task in _tasks.Where(t => t.DeletedAt == null))
        {
            task.SoftDelete();
        }
    }
    
    public bool CanBeEdited() => DeletedAt == null && Status != ProjectStatus.Completed && Status != ProjectStatus.Cancelled;
    
    public int GetNonCancelledTaskCount() => _tasks.Count(t => t.Status != TaskStatus.Cancelled && t.DeletedAt == null);
    
    public int GetCompletedTaskCount() => _tasks.Count(t => t.Status == TaskStatus.Done && t.DeletedAt == null);
    
    public int GetOverdueTaskCount(DateTimeOffset now) => _tasks.Count(t => 
        t.DeletedAt == null && 
        t.Status != TaskStatus.Done && 
        t.Status != TaskStatus.Cancelled && 
        t.DueAt.HasValue && 
        t.DueAt.Value < now);
}