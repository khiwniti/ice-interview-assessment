using BusinessAnalytics.Application.Reports.Abstractions;
using BusinessAnalytics.Application.Reports.DTOs;
using BusinessAnalytics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using CsvHelper;
using System.Globalization;
using System.Text;

namespace BusinessAnalytics.Infrastructure.Reporting;

public class ReportService : IReportService
{
    private readonly BusinessAnalyticsDbContext _context;
    
    public ReportService(BusinessAnalyticsDbContext context)
    {
        _context = context;
    }
    
    public async Task<IReadOnlyList<DepartmentProgressReportDto>> GetDepartmentProgressAsync(
        Guid? departmentId = null, 
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        
        var query = _context.Database.SqlQueryRaw<DepartmentProgressReportDto>(@"
            WITH project_metrics AS (
                SELECT
                    p.id,
                    p.department_id,
                    COUNT(t.id) FILTER (
                        WHERE t.status <> 5
                    ) AS task_count,
                    COUNT(t.id) FILTER (
                        WHERE t.status = 4
                    ) AS completed_task_count,
                    COUNT(t.id) FILTER (
                        WHERE t.status NOT IN (4, 5)
                          AND t.due_at < @now
                    ) AS overdue_task_count
                FROM projects p
                LEFT JOIN tasks t
                    ON t.project_id = p.id
                   AND t.deleted_at IS NULL
                WHERE p.deleted_at IS NULL
                  AND p.status <> 5
                GROUP BY p.id, p.department_id
            )
            SELECT
                d.id AS department_id,
                d.name AS department_name,
                COUNT(pm.id) AS project_count,
                COALESCE(SUM(pm.task_count), 0) AS task_count,
                COALESCE(SUM(pm.completed_task_count), 0) AS completed_task_count,
                COALESCE(SUM(pm.overdue_task_count), 0) AS overdue_task_count,
                ROUND(
                    100.0 * SUM(pm.completed_task_count)
                    / NULLIF(SUM(pm.task_count), 0),
                    2
                ) AS progress_percent,
                @now AS generated_at_utc
            FROM departments d
            LEFT JOIN project_metrics pm
                ON pm.department_id = d.id
            WHERE d.is_active = TRUE
              AND (@departmentId IS NULL OR d.id = @departmentId)
            GROUP BY d.id, d.name
            ORDER BY d.name, d.id", 
            new NpgsqlParameter("now", now),
            new NpgsqlParameter("departmentId", departmentId ?? (object)DBNull.Value));
        
        var results = await query.ToListAsync(cancellationToken);
        return results;
    }
    
    public async Task<ProjectSummaryReportDto?> GetProjectSummaryAsync(
        Guid projectId, 
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        
        var query = _context.Database.SqlQueryRaw<ProjectSummaryReportDto>(@"
            SELECT
                p.id AS project_id,
                p.name AS project_name,
                p.status::text AS project_status,
                COUNT(t.id) FILTER (
                    WHERE t.status <> 5
                ) AS task_count,
                COUNT(t.id) FILTER (
                    WHERE t.status = 4
                ) AS completed_task_count,
                COUNT(t.id) FILTER (
                    WHERE t.status NOT IN (4, 5)
                      AND t.due_at < @now
                ) AS overdue_task_count,
                ROUND(
                    100.0 * COUNT(t.id) FILTER (WHERE t.status = 4)
                    / NULLIF(COUNT(t.id) FILTER (WHERE t.status <> 5), 0),
                    2
                ) AS progress_percent,
                p.start_at,
                p.due_at,
                @now AS generated_at_utc
            FROM projects p
            LEFT JOIN tasks t
                ON t.project_id = p.id
               AND t.deleted_at IS NULL
            WHERE p.id = @projectId
              AND p.deleted_at IS NULL
            GROUP BY p.id, p.name, p.status, p.start_at, p.due_at", 
            new NpgsqlParameter("projectId", projectId),
            new NpgsqlParameter("now", now));
        
        var result = await query.FirstOrDefaultAsync(cancellationToken);
        return result;
    }
    
    public async Task<byte[]> ExportDepartmentProgressAsync(
        string format, 
        Guid? departmentId = null, 
        CancellationToken cancellationToken = default)
    {
        var reports = await GetDepartmentProgressAsync(departmentId, cancellationToken);
        
        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            return ExportToCsv(reports);
        }
        
        // JSON export
        var exportDtos = reports.Select(r => new DepartmentProgressExportDto(
            r.DepartmentId,
            r.DepartmentName,
            r.ProjectCount,
            r.TaskCount,
            r.CompletedTaskCount,
            r.OverdueTaskCount,
            r.ProgressPercent?.ToString("F2") ?? "N/A",
            r.GeneratedAtUtc
        )).ToList();
        
        var json = System.Text.Json.JsonSerializer.Serialize(exportDtos, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });
        
        return Encoding.UTF8.GetBytes(json);
    }
    
    private static byte[] ExportToCsv(IReadOnlyList<DepartmentProgressReportDto> reports)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        
        // Write header
        csv.WriteField("DepartmentId");
        csv.WriteField("DepartmentName");
        csv.WriteField("ProjectCount");
        csv.WriteField("TaskCount");
        csv.WriteField("CompletedTaskCount");
        csv.WriteField("OverdueTaskCount");
        csv.WriteField("ProgressPercent");
        csv.WriteField("GeneratedAtUtc");
        csv.NextRecord();
        
        // Write data
        foreach (var report in reports)
        {
            csv.WriteField(report.DepartmentId);
            csv.WriteField(report.DepartmentName);
            csv.WriteField(report.ProjectCount);
            csv.WriteField(report.TaskCount);
            csv.WriteField(report.CompletedTaskCount);
            csv.WriteField(report.OverdueTaskCount);
            csv.WriteField(report.ProgressPercent?.ToString("F2") ?? "");
            csv.WriteField(report.GeneratedAtUtc.ToString("o"));
            csv.NextRecord();
        }
        
        writer.Flush();
        return memoryStream.ToArray();
    }
}