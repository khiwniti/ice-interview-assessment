using BusinessAnalytics.Domain.Enums;

namespace BusinessAnalytics.Application.Tasks.DTOs;

public record TaskDto(
    Guid Id,
    Guid ProjectId,
    string ProjectName,
    Guid? AssignedToUserId,
    string? AssignedToUserName,
    string Title,
    string Description,
    TaskStatus Status,
    TaskPriority Priority,
    DateTimeOffset? DueAt,
    DateTimeOffset? CompletedAt,
    bool IsOverdue,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? DeletedAt
);

public record CreateTaskRequest(
    string Title,
    string Description,
    TaskPriority Priority,
    DateTimeOffset? DueAt,
    Guid? AssignedToUserId
);

public record UpdateTaskRequest(
    string Title,
    string Description,
    TaskPriority Priority,
    DateTimeOffset? DueAt,
    Guid? AssignedToUserId
);

public record UpdateTaskStatusRequest(
    TaskStatus Status
);

public record TaskListItemDto(
    Guid Id,
    Guid ProjectId,
    string ProjectName,
    Guid? AssignedToUserId,
    string? AssignedToUserName,
    string Title,
    TaskStatus Status,
    TaskPriority Priority,
    DateTimeOffset? DueAt,
    bool IsOverdue,
    DateTimeOffset CreatedAt
);