using BusinessAnalytics.Application.Reports.DTOs;

namespace BusinessAnalytics.Application.Reports.Abstractions;

public interface IReportService
{
    Task<IReadOnlyList<DepartmentProgressReportDto>> GetDepartmentProgressAsync(
        Guid? departmentId = null, 
        CancellationToken cancellationToken = default);
    
    Task<ProjectSummaryReportDto?> GetProjectSummaryAsync(
        Guid projectId, 
        CancellationToken cancellationToken = default);
    
    Task<byte[]> ExportDepartmentProgressAsync(
        string format, 
        Guid? departmentId = null, 
        CancellationToken cancellationToken = default);
}