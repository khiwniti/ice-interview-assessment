namespace BusinessAnalytics.Application.Departments.DTOs;

public record DepartmentDto(
    Guid Id,
    string Name,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public record CreateDepartmentRequest(string Name);

public record UpdateDepartmentRequest(string Name);

public record DepartmentListItemDto(
    Guid Id,
    string Name,
    bool IsActive,
    int UserCount,
    int ProjectCount
);