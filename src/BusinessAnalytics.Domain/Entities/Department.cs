using BusinessAnalytics.Domain.Enums;

namespace BusinessAnalytics.Domain.Entities;

public class Department : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    
    private readonly List<User> _users = [];
    public IReadOnlyCollection<User> Users => _users.AsReadOnly();
    
    private readonly List<Project> _projects = [];
    public IReadOnlyCollection<Project> Projects => _projects.AsReadOnly();
    
    private Department() { } // EF Core
    
    public Department(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Department name cannot be empty", nameof(name));
        
        Name = name.Trim();
    }
    
    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Department name cannot be empty", nameof(name));
        
        Name = name.Trim();
        UpdateTimestamp();
        IncrementVersion();
    }
    
    public void Activate()
    {
        IsActive = true;
        UpdateTimestamp();
        IncrementVersion();
    }
    
    public void Deactivate()
    {
        IsActive = false;
        UpdateTimestamp();
        IncrementVersion();
    }
}