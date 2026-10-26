using BusinessAnalytics.Domain.Enums;

namespace BusinessAnalytics.Domain.Entities;

public class TaskItem : BaseEntity
{
    public Guid ProjectId { get; private set; }
    public Guid? AssignedToUserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public TaskStatus Status { get; private set; } = TaskStatus.Todo;
    public TaskPriority Priority { get; private set; } = TaskPriority.Medium;
    public DateTimeOffset? DueAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    
    private TaskItem() { } // EF Core
    
    public TaskItem(Guid projectId, string title, string description = "", TaskPriority priority = TaskPriority.Medium, DateTimeOffset? dueAt = null, Guid? assignedToUserId = null)
    {
        if (projectId == Guid.Empty)
            throw new ArgumentException("Project ID is required", nameof(projectId));
        
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Task title cannot be empty", nameof(title));
        
        ProjectId = projectId;
        Title = title.Trim();
        Description = description?.Trim() ?? string.Empty;
        Priority = priority;
        DueAt = dueAt;
        AssignedToUserId = assignedToUserId;
    }
    
    public void UpdateDetails(string title, string description, TaskPriority priority, DateTimeOffset? dueAt)
    {
        if (DeletedAt.HasValue)
            throw new InvalidOperationException("Cannot update a deleted task");
        
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Task title cannot be empty", nameof(title));
        
        Title = title.Trim();
        Description = description?.Trim() ?? string.Empty;
        Priority = priority;
        DueAt = dueAt;
        UpdateTimestamp();
        IncrementVersion();
    }
    
    public void AssignToUser(Guid userId)
    {
        if (DeletedAt.HasValue)
            throw new InvalidOperationException("Cannot assign a deleted task");
        
        AssignedToUserId = userId;
        UpdateTimestamp();
        IncrementVersion();
    }
    
    public void Unassign()
    {
        if (DeletedAt.HasValue)
            throw new InvalidOperationException("Cannot unassign a deleted task");
        
        AssignedToUserId = null;
        UpdateTimestamp();
        IncrementVersion();
    }
    
    public void ChangeStatus(TaskStatus newStatus, bool isProjectEditable)
    {
        if (DeletedAt.HasValue)
            throw new InvalidOperationException("Cannot change status of a deleted task");
        
        // Business rule: can only mark Done if project is active and editable
        if (newStatus == TaskStatus.Done && !isProjectEditable)
            throw new InvalidOperationException("Cannot mark task as Done in a non-editable project");
        
        if (Status == newStatus)
            return;
        
        if (newStatus == TaskStatus.Done)
        {
            CompletedAt = DateTimeOffset.UtcNow;
        }
        else if (Status == TaskStatus.Done)
        {
            // Reopening a completed task
            CompletedAt = null;
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
    }
    
    public bool IsOverdue(DateTimeOffset now) => 
        DeletedAt == null && 
        Status != TaskStatus.Done && 
        Status != TaskStatus.Cancelled && 
        DueAt.HasValue && 
        DueAt.Value < now;
}