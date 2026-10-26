namespace BusinessAnalytics.Application.Users.DTOs;

public record UserDto(
    Guid Id,
    Guid DepartmentId,
    string DepartmentName,
    string Email,
    string DisplayName,
    string Role,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? DeactivatedAt
);

public record CreateUserRequest(
    Guid DepartmentId,
    string Email,
    string DisplayName,
    string Password,
    string Role
);

public record UpdateUserRequest(
    string Email,
    string DisplayName,
    string Role
);

public record UpdateUserPasswordRequest(
    string CurrentPassword,
    string NewPassword
);

public record UserListItemDto(
    Guid Id,
    Guid DepartmentId,
    string DepartmentName,
    string Email,
    string DisplayName,
    string Role,
    bool IsActive
);