using BusinessAnalytics.Domain.Enums;

namespace BusinessAnalytics.Domain.Entities;

public class User : BaseEntity
{
    public Guid DepartmentId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string NormalizedEmail { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; } = UserRole.Viewer;
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset? DeactivatedAt { get; private set; }
    
    private readonly List<Project> _ownedProjects = [];
    public IReadOnlyCollection<Project> OwnedProjects => _ownedProjects.AsReadOnly();
    
    private readonly List<TaskItem> _assignedTasks = [];
    public IReadOnlyCollection<TaskItem> AssignedTasks => _assignedTasks.AsReadOnly();
    
    private readonly List<SystemLog> _systemLogs = [];
    public IReadOnlyCollection<SystemLog> SystemLogs => _systemLogs.AsReadOnly();
    
    private User() { } // EF Core
    
    public User(Guid departmentId, string email, string displayName, string passwordHash, UserRole role = UserRole.Viewer)
    {
        if (departmentId == Guid.Empty)
            throw new ArgumentException("Department ID is required", nameof(departmentId));
        
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(email));
        
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name is required", nameof(displayName));
        
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required", nameof(passwordHash));
        
        DepartmentId = departmentId;
        Email = email.Trim();
        NormalizedEmail = email.Trim().ToUpperInvariant();
        DisplayName = displayName.Trim();
        PasswordHash = passwordHash;
        Role = role;
    }
    
    public void UpdateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(email));
        
        Email = email.Trim();
        NormalizedEmail = email.Trim().ToUpperInvariant();
        UpdateTimestamp();
        IncrementVersion();
    }
    
    public void UpdateDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name is required", nameof(displayName));
        
        DisplayName = displayName.Trim();
        UpdateTimestamp();
        IncrementVersion();
    }
    
    public void UpdateRole(UserRole role)
    {
        Role = role;
        UpdateTimestamp();
        IncrementVersion();
    }
    
    public void ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("Password hash is required", nameof(newPasswordHash));
        
        PasswordHash = newPasswordHash;
        UpdateTimestamp();
        IncrementVersion();
    }
    
    public void UpdateDepartment(Guid departmentId)
    {
        if (departmentId == Guid.Empty)
            throw new ArgumentException("Department ID is required", nameof(departmentId));
        
        DepartmentId = departmentId;
        UpdateTimestamp();
        IncrementVersion();
    }
    
    public void Deactivate()
    {
        IsActive = false;
        DeactivatedAt = DateTimeOffset.UtcNow;
        UpdateTimestamp();
        IncrementVersion();
    }
    
    public void Activate()
    {
        IsActive = true;
        DeactivatedAt = null;
        UpdateTimestamp();
        IncrementVersion();
    }
}