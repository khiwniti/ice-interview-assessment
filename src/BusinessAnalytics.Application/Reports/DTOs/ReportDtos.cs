namespace BusinessAnalytics.Application.Reports.DTOs;

public record DepartmentProgressReportDto(
    Guid DepartmentId,
    string DepartmentName,
    int ProjectCount,
    int TaskCount,
    int CompletedTaskCount,
    int OverdueTaskCount,
    decimal? ProgressPercent,
    DateTimeOffset GeneratedAtUtc
);

public record DepartmentProgressExportDto(
    Guid DepartmentId,
    string DepartmentName,
    int ProjectCount,
    int TaskCount,
    int CompletedTaskCount,
    int OverdueTaskCount,
    string ProgressPercent,
    DateTimeOffset GeneratedAtUtc
);

public record ProjectSummaryReportDto(
    Guid ProjectId,
    string ProjectName,
    string ProjectStatus,
    int TaskCount,
    int CompletedTaskCount,
    int OverdueTaskCount,
    decimal? ProgressPercent,
    DateTimeOffset? StartAt,
    DateTimeOffset? DueAt,
    DateTimeOffset GeneratedAtUtc
);