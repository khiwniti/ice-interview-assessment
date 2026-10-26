namespace BusinessAnalytics.Domain.Enums;

public enum ProjectStatus
{
    Planned = 1,
    Active = 2,
    OnHold = 3,
    Completed = 4,
    Cancelled = 5
}

public enum TaskStatus
{
    Todo = 1,
    InProgress = 2,
    Blocked = 3,
    Done = 4,
    Cancelled = 5
}

public enum TaskPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum UserRole
{
    Admin = 1,
    Manager = 2,
    Contributor = 3,
    Viewer = 4
}

public enum AuditAction
{
    Created = 1,
    Updated = 2,
    Deleted = 3,
    StatusChanged = 4,
    Assigned = 5,
    Unassigned = 6
}

public enum AuditOutcome
{
    Success = 1,
    Failure = 2
}