using BusinessAnalytics.Application.Abstractions;
using BusinessAnalytics.Application.Users.DTOs;
using BusinessAnalytics.Domain.Entities;
using BusinessAnalytics.Domain.Enums;
using BusinessAnalytics.Domain.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace BusinessAnalytics.Application.Users.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly IPasswordHasher<User> _passwordHasher;
    
    public UserService(
        IUserRepository userRepository,
        IDepartmentRepository departmentRepository,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        IPasswordHasher<User> passwordHasher)
    {
        _userRepository = userRepository;
        _departmentRepository = departmentRepository;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _passwordHasher = passwordHasher;
    }
    
    public async Task<UserDto> CreateAsync(CreateUserRequest request, Guid actorUserId, string traceId, CancellationToken cancellationToken = default)
    {
        var department = await _departmentRepository.GetByIdAsync(request.DepartmentId, cancellationToken);
        if (department == null)
            throw new InvalidOperationException("Department not found");
        
        if (!department.IsActive)
            throw new InvalidOperationException("Cannot create user in inactive department");
        
        var existingUser = await _userRepository.GetByNormalizedEmailAsync(request.Email.ToUpperInvariant(), cancellationToken);
        if (existingUser != null)
            throw new InvalidOperationException($"User with email '{request.Email}' already exists");
        
        var passwordHash = _passwordHasher.HashPassword(null!, request.Password);
        var user = new User(
            request.DepartmentId, 
            request.Email, 
            request.DisplayName, 
            passwordHash,
            Enum.Parse<UserRole>(request.Role, true));
        
        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        await _auditService.LogAsync(
            AuditAction.Created, 
            nameof(User), 
            user.Id, 
            AuditOutcome.Success, 
            traceId, 
            actorUserId, 
            cancellationToken: cancellationToken);
        
        return MapToDto(user, department.Name);
    }
    
    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user == null) return null;
        
        var department = await _departmentRepository.GetByIdAsync(user.DepartmentId, cancellationToken);
        return MapToDto(user, department?.Name ?? string.Empty);
    }
    
    public async Task<IReadOnlyList<UserListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        var results = new List<UserListItemDto>();
        
        foreach (var user in users)
        {
            var department = await _departmentRepository.GetByIdAsync(user.DepartmentId, cancellationToken);
            results.Add(new UserListItemDto(
                user.Id,
                user.DepartmentId,
                department?.Name ?? string.Empty,
                user.Email,
                user.DisplayName,
                user.Role.ToString(),
                user.IsActive
            ));
        }
        
        return results;
    }
    
    public async Task<IReadOnlyList<UserListItemDto>> GetByDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetByDepartmentAsync(departmentId, cancellationToken);
        var department = await _departmentRepository.GetByIdAsync(departmentId, cancellationToken);
        var results = new List<UserListItemDto>();
        
        foreach (var user in users)
        {
            results.Add(new UserListItemDto(
                user.Id,
                user.DepartmentId,
                department?.Name ?? string.Empty,
                user.Email,
                user.DisplayName,
                user.Role.ToString(),
                user.IsActive
            ));
        }
        
        return results;
    }
    
    public async Task<UserDto?> UpdateAsync(Guid id, UpdateUserRequest request, Guid actorUserId, string traceId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user == null)
            return null;
        
        var existingUser = await _userRepository.GetByNormalizedEmailAsync(request.Email.ToUpperInvariant(), cancellationToken);
        if (existingUser != null && existingUser.Id != id)
            throw new InvalidOperationException($"User with email '{request.Email}' already exists");
        
        user.UpdateEmail(request.Email);
        user.UpdateDisplayName(request.DisplayName);
        user.UpdateRole(Enum.Parse<UserRole>(request.Role, true));
        
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        await _auditService.LogAsync(
            AuditAction.Updated, 
            nameof(User), 
            user.Id, 
            AuditOutcome.Success, 
            traceId, 
            actorUserId, 
            cancellationToken: cancellationToken);
        
        var department = await _departmentRepository.GetByIdAsync(user.DepartmentId, cancellationToken);
        return MapToDto(user, department?.Name ?? string.Empty);
    }
    
    public async Task<bool> UpdatePasswordAsync(Guid id, UpdateUserPasswordRequest request, Guid actorUserId, string traceId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user == null)
            return false;
        
        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
        if (verificationResult == PasswordVerificationResult.Failed)
            throw new InvalidOperationException("Current password is incorrect");
        
        var newPasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        user.ChangePassword(newPasswordHash);
        
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        await _auditService.LogAsync(
            AuditAction.Updated, 
            nameof(User), 
            user.Id, 
            AuditOutcome.Success, 
            traceId, 
            actorUserId, 
            cancellationToken: cancellationToken);
        
        return true;
    }
    
    public async Task<bool> UpdateDepartmentAsync(Guid id, Guid newDepartmentId, Guid actorUserId, string traceId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user == null)
            return false;
        
        var department = await _departmentRepository.GetByIdAsync(newDepartmentId, cancellationToken);
        if (department == null)
            throw new InvalidOperationException("Department not found");
        
        if (!department.IsActive)
            throw new InvalidOperationException("Cannot assign user to inactive department");
        
        user.UpdateDepartment(newDepartmentId);
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        await _auditService.LogAsync(
            AuditAction.Updated, 
            nameof(User), 
            user.Id, 
            AuditOutcome.Success, 
            traceId, 
            actorUserId, 
            cancellationToken: cancellationToken);
        
        return true;
    }
    
    public async Task<bool> DeactivateAsync(Guid id, Guid actorUserId, string traceId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user == null)
            return false;
        
        user.Deactivate();
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        await _auditService.LogAsync(
            AuditAction.Deleted, 
            nameof(User), 
            user.Id, 
            AuditOutcome.Success, 
            traceId, 
            actorUserId, 
            cancellationToken: cancellationToken);
        
        return true;
    }
    
    
    public async Task<UserDto?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (user == null) return null;
        
        var department = await _departmentRepository.GetByIdAsync(user.DepartmentId, cancellationToken);
        return MapToDto(user, department?.Name ?? string.Empty);
    }
    
    public async Task<bool> ActivateAsync(Guid id, Guid actorUserId, string traceId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user == null)
            return false;
        
        user.Activate();
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        await _auditService.LogAsync(
            AuditAction.Updated, 
            nameof(User), 
            user.Id, 
            AuditOutcome.Success, 
            traceId, 
            actorUserId, 
            cancellationToken: cancellationToken);
        
        return true;
    }
    
    private static UserDto MapToDto(User user, string departmentName) => new(
        user.Id,
        user.DepartmentId,
        departmentName,
        user.Email,
        user.DisplayName,
        user.Role.ToString(),
        user.IsActive,
        user.CreatedAt,
        user.UpdatedAt,
        user.DeactivatedAt
    );
}

public interface IUserService
{
    Task<UserDto> CreateAsync(CreateUserRequest request, Guid actorUserId, string traceId, CancellationToken cancellationToken = default);
    Task<UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserListItemDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserListItemDto>> GetByDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default);
    Task<UserDto?> UpdateAsync(Guid id, UpdateUserRequest request, Guid actorUserId, string traceId, CancellationToken cancellationToken = default);
    Task<bool> UpdatePasswordAsync(Guid id, UpdateUserPasswordRequest request, Guid actorUserId, string traceId, CancellationToken cancellationToken = default);
    Task<bool> UpdateDepartmentAsync(Guid id, Guid newDepartmentId, Guid actorUserId, string traceId, CancellationToken cancellationToken = default);
    Task<bool> DeactivateAsync(Guid id, Guid actorUserId, string traceId, CancellationToken cancellationToken = default);
    Task<UserDto?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ActivateAsync(Guid id, Guid actorUserId, string traceId, CancellationToken cancellationToken = default);
}