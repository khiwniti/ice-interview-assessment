using BusinessAnalytics.Domain.Enums;

namespace BusinessAnalytics.Application.Projects.DTOs;

public record ProjectDto(
    Guid Id,
    Guid DepartmentId,
    string DepartmentName,
    Guid OwnerUserId,
    string OwnerName,
    string Name,
    string Description,
    ProjectStatus Status,
    DateTimeOffset? StartAt,
    DateTimeOffset? DueAt,
    int TaskCount,
    int CompletedTaskCount,
    int OverdueTaskCount,
    decimal? ProgressPercent,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? DeletedAt
);

public record CreateProjectRequest(
    Guid DepartmentId,
    Guid OwnerUserId,
    string Name,
    string Description,
    ProjectStatus Status,
    DateTimeOffset? StartAt,
    DateTimeOffset? DueAt
);

public record UpdateProjectRequest(
    string Name,
    string Description,
    ProjectStatus Status,
    DateTimeOffset? StartAt,
    DateTimeOffset? DueAt
);

public record ProjectListItemDto(
    Guid Id,
    Guid DepartmentId,
    string DepartmentName,
    Guid OwnerUserId,
    string OwnerName,
    string Name,
    ProjectStatus Status,
    DateTimeOffset? StartAt,
    DateTimeOffset? DueAt,
    int TaskCount,
    int CompletedTaskCount,
    decimal? ProgressPercent,
    DateTimeOffset CreatedAt
);

public record ProjectSummaryDto(
    Guid Id,
    string Name,
    ProjectStatus Status,
    int TaskCount,
    int CompletedTaskCount,
    int OverdueTaskCount,
    decimal? ProgressPercent,
    DateTimeOffset? StartAt,
    DateTimeOffset? DueAt
);