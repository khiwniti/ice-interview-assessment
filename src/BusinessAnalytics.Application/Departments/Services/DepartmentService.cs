using BusinessAnalytics.Application.Abstractions;
using BusinessAnalytics.Application.Departments.DTOs;
using BusinessAnalytics.Domain.Entities;
using BusinessAnalytics.Domain.Enums;
using BusinessAnalytics.Domain.Abstractions;

namespace BusinessAnalytics.Application.Departments.Services;

public class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    
    public DepartmentService(
        IDepartmentRepository departmentRepository,
        IUnitOfWork unitOfWork,
        IAuditService auditService)
    {
        _departmentRepository = departmentRepository;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
    }
    
    public async Task<DepartmentDto> CreateAsync(CreateDepartmentRequest request, Guid actorUserId, string traceId, CancellationToken cancellationToken = default)
    {
        var existing = await _departmentRepository.GetByNameAsync(request.Name, cancellationToken);
        if (existing != null)
            throw new InvalidOperationException($"Department with name '{request.Name}' already exists");
        
        var department = new Department(request.Name);
        await _departmentRepository.AddAsync(department, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        await _auditService.LogAsync(
            AuditAction.Created, 
            nameof(Department), 
            department.Id, 
            AuditOutcome.Success, 
            traceId, 
            actorUserId, 
            cancellationToken: cancellationToken);
        
        return MapToDto(department);
    }
    
    public async Task<DepartmentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var department = await _departmentRepository.GetByIdAsync(id, cancellationToken);
        return department != null ? MapToDto(department) : null;
    }
    
    public async Task<IReadOnlyList<DepartmentListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var departments = await _departmentRepository.GetAllAsync(cancellationToken);
        var results = new List<DepartmentListItemDto>();
        
        foreach (var d in departments)
        {
            results.Add(new DepartmentListItemDto(
                d.Id,
                d.Name,
                d.IsActive,
                d.Users.Count,
                d.Projects.Count(p => p.DeletedAt == null && p.Status != ProjectStatus.Cancelled)
            ));
        }
        
        return results;
    }
    
    public async Task<DepartmentDto?> UpdateAsync(Guid id, UpdateDepartmentRequest request, Guid actorUserId, string traceId, CancellationToken cancellationToken = default)
    {
        var department = await _departmentRepository.GetByIdAsync(id, cancellationToken);
        if (department == null)
            return null;
        
        var existing = await _departmentRepository.GetByNameAsync(request.Name, cancellationToken);
        if (existing != null && existing.Id != id)
            throw new InvalidOperationException($"Department with name '{request.Name}' already exists");
        
        department.UpdateName(request.Name);
        await _departmentRepository.UpdateAsync(department, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        await _auditService.LogAsync(
            AuditAction.Updated, 
            nameof(Department), 
            department.Id, 
            AuditOutcome.Success, 
            traceId, 
            actorUserId, 
            cancellationToken: cancellationToken);
        
        return MapToDto(department);
    }
    
    public async Task<bool> DeleteAsync(Guid id, Guid actorUserId, string traceId, CancellationToken cancellationToken = default)
    {
        var department = await _departmentRepository.GetByIdAsync(id, cancellationToken);
        if (department == null)
            return false;
        
        // Check if department has active projects or users
        if (department.Projects.Any(p => p.DeletedAt == null) || department.Users.Any(u => u.IsActive))
            throw new InvalidOperationException("Cannot deactivate department with active projects or users");
        
        department.Deactivate();
        await _departmentRepository.UpdateAsync(department, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        await _auditService.LogAsync(
            AuditAction.Deleted, 
            nameof(Department), 
            department.Id, 
            AuditOutcome.Success, 
            traceId, 
            actorUserId, 
            cancellationToken: cancellationToken);
        
        return true;
    }
    
    public async Task<bool> ActivateAsync(Guid id, Guid actorUserId, string traceId, CancellationToken cancellationToken = default)
    {
        var department = await _departmentRepository.GetByIdAsync(id, cancellationToken);
        if (department == null)
            return false;
        
        department.Activate();
        await _departmentRepository.UpdateAsync(department, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        await _auditService.LogAsync(
            AuditAction.Updated, 
            nameof(Department), 
            department.Id, 
            AuditOutcome.Success, 
            traceId, 
            actorUserId, 
            cancellationToken: cancellationToken);
        
        return true;
    }
    
    private static DepartmentDto MapToDto(Department department) => new(
        department.Id,
        department.Name,
        department.IsActive,
        department.CreatedAt,
        department.UpdatedAt
    );
}

public interface IDepartmentService
{
    Task<DepartmentDto> CreateAsync(CreateDepartmentRequest request, Guid actorUserId, string traceId, CancellationToken cancellationToken = default);
    Task<DepartmentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DepartmentListItemDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<DepartmentDto?> UpdateAsync(Guid id, UpdateDepartmentRequest request, Guid actorUserId, string traceId, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, Guid actorUserId, string traceId, CancellationToken cancellationToken = default);
    Task<bool> ActivateAsync(Guid id, Guid actorUserId, string traceId, CancellationToken cancellationToken = default);
}