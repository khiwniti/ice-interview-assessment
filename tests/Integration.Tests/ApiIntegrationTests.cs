using BusinessAnalytics.Api;
using BusinessAnalytics.Application.Reports.DTOs;
using BusinessAnalytics.Domain.Entities;
using BusinessAnalytics.Domain.Enums;
using BusinessAnalytics.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Testcontainers.PostgreSql;
using Xunit;

namespace BusinessAnalytics.Integration.Tests;

public class IntegrationTestFixture : IAsyncLifetime
{
    public PostgreSqlContainer DbContainer { get; private set; } = null!;
    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;
    public string AdminToken { get; private set; } = "";
    public string ManagerToken { get; private set; } = "";
    
    public async Task InitializeAsync()
    {
        DbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("business_analytics")
            .WithUsername("app_user")
            .WithPassword("test_password")
            .Build();
        
        await DbContainer.StartAsync();
        
        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<BusinessAnalyticsDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);
                    
                    services.AddDbContext<BusinessAnalyticsDbContext>(options =>
                    {
                        options.UseNpgsql(DbContainer.GetConnectionString());
                    });
                    
                    // Ensure database is created and seeded
                    using var scope = services.BuildServiceProvider().CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<BusinessAnalyticsDbContext>();
                    await context.Database.MigrateAsync();
                });
            });
        
        Client = Factory.CreateClient();
        
        // Login to get tokens
        AdminToken = await GetTokenAsync("admin@company.com", "Admin123!");
        ManagerToken = await GetTokenAsync("eng.manager@company.com", "Manager123!");
    }
    
    public async Task DisposeAsync()
    {
        Client?.Dispose();
        Factory?.Dispose();
        await DbContainer?.DisposeAsync();
    }
    
    private async Task<string> GetTokenAsync(string email, string password)
    {
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        return result.GetProperty("token").GetString() ?? "";
    }
}

public class DepartmentApiTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    
    public DepartmentApiTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task GetDepartments_ReturnsList()
    {
        _fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _fixture.AdminToken);
        
        var response = await _fixture.Client.GetAsync("/api/v1/departments");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var departments = await response.Content.ReadFromJsonAsync<List<JsonElement>>();
        departments.Should().NotBeNull();
        departments!.Count.Should().BeGreaterOrEqualTo(4);
    }
    
    [Fact]
    public async Task CreateDepartment_AsAdmin_Succeeds()
    {
        _fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _fixture.AdminToken);
        
        var request = new { name = "Test Department" };
        var response = await _fixture.Client.PostAsJsonAsync("/api/v1/departments", request);
        
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dept = await response.Content.ReadFromJsonAsync<JsonElement>();
        dept.GetProperty("name").GetString().Should().Be("Test Department");
        dept.GetProperty("isActive").GetBoolean().Should().BeTrue();
    }
    
    [Fact]
    public async Task CreateDepartment_AsManager_ReturnsForbidden()
    {
        _fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _fixture.ManagerToken);
        
        var request = new { name = "Test Department" };
        var response = await _fixture.Client.PostAsJsonAsync("/api/v1/departments", request);
        
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

public class ProjectApiTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    
    public ProjectApiTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task GetProjects_ReturnsList()
    {
        _fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _fixture.ManagerToken);
        
        var response = await _fixture.Client.GetAsync("/api/v1/projects");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var projects = await response.Content.ReadFromJsonAsync<List<JsonElement>>();
        projects.Should().NotBeNull();
    }
    
    [Fact]
    public async Task CreateProject_AsManager_Succeeds()
    {
        _fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _fixture.ManagerToken);
        
        // Get a department and owner
        var deptResponse = await _fixture.Client.GetAsync("/api/v1/departments");
        var depts = await deptResponse.Content.ReadFromJsonAsync<List<JsonElement>>();
        var deptId = depts![0].GetProperty("id").GetGuid();
        
        var userResponse = await _fixture.Client.GetAsync("/api/v1/users");
        var users = await userResponse.Content.ReadFromJsonAsync<List<JsonElement>>();
        var ownerId = users![0].GetProperty("id").GetGuid();
        
        var request = new
        {
            departmentId = deptId,
            ownerUserId = ownerId,
            name = "Integration Test Project",
            description = "Created via integration test",
            status = "Active",
            startAt = DateTimeOffset.UtcNow.AddDays(1),
            dueAt = DateTimeOffset.UtcNow.AddDays(30)
        };
        
        var response = await _fixture.Client.PostAsJsonAsync("/api/v1/projects", request);
        
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var project = await response.Content.ReadFromJsonAsync<JsonElement>();
        project.GetProperty("name").GetString().Should().Be("Integration Test Project");
        project.GetProperty("status").GetString().Should().Be("Active");
    }
    
    [Fact]
    public async Task UpdateProject_WithStaleVersion_ReturnsPreconditionFailed()
    {
        _fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _fixture.ManagerToken);
        
        // Create project
        var deptResponse = await _fixture.Client.GetAsync("/api/v1/departments");
        var depts = await deptResponse.Content.ReadFromJsonAsync<List<JsonElement>>();
        var deptId = depts![0].GetProperty("id").GetGuid();
        
        var userResponse = await _fixture.Client.GetAsync("/api/v1/users");
        var users = await userResponse.Content.ReadFromJsonAsync<List<JsonElement>>();
        var ownerId = users![0].GetProperty("id").GetGuid();
        
        var createRequest = new
        {
            departmentId = deptId,
            ownerUserId = ownerId,
            name = "Concurrency Test",
            description = "",
            status = "Active",
            startAt = DateTimeOffset.UtcNow.AddDays(1),
            dueAt = DateTimeOffset.UtcNow.AddDays(30)
        };
        
        var createResponse = await _fixture.Client.PostAsJsonAsync("/api/v1/projects", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var projectId = created.GetProperty("id").GetGuid();
        var version = created.GetProperty("version").GetInt32();
        
        // Update with correct version
        var updateRequest = new
        {
            name = "Updated Name",
            description = "",
            status = "Active",
            startAt = DateTimeOffset.UtcNow.AddDays(1),
            dueAt = DateTimeOffset.UtcNow.AddDays(30)
        };
        
        var updateResponse = await _fixture.Client.PutAsJsonAsync($"/api/v1/projects/{projectId}", updateRequest, 
            new JsonSerializerOptions(), new System.Net.Http.Headers.MediaTypeHeaderValue("application/json"));
        
        // Need to add If-Match header manually
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/projects/{projectId}")
        {
            Content = JsonContent.Create(updateRequest)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _fixture.ManagerToken);
        request.Headers.IfMatch.Add(new EntityTagHeaderValue($"\"{version}\""));
        
        var response = await _fixture.Client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Try with stale version
        request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/projects/{projectId}")
        {
            Content = JsonContent.Create(updateRequest)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _fixture.ManagerToken);
        request.Headers.IfMatch.Add(new EntityTagHeaderValue($"\"{version}\""));
        
        var staleResponse = await _fixture.Client.SendAsync(request);
        staleResponse.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
    }
}

public class TaskApiTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    
    public TaskApiTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task CreateTask_SetsDefaults()
    {
        _fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _fixture.ManagerToken);
        
        // Get a project
        var projectResponse = await _fixture.Client.GetAsync("/api/v1/projects");
        var projects = await projectResponse.Content.ReadFromJsonAsync<List<JsonElement>>();
        var projectId = projects![0].GetProperty("id").GetGuid();
        
        var request = new
        {
            title = "Integration Test Task",
            description = "Test task",
            priority = "High",
            dueAt = DateTimeOffset.UtcNow.AddDays(7)
        };
        
        var response = await _fixture.Client.PostAsJsonAsync($"/api/v1/projects/{projectId}/tasks", request);
        
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var task = await response.Content.ReadFromJsonAsync<JsonElement>();
        task.GetProperty("title").GetString().Should().Be("Integration Test Task");
        task.GetProperty("status").GetString().Should().Be("Todo");
        task.GetProperty("priority").GetString().Should().Be("High");
    }
    
    [Fact]
    public async Task UpdateTaskStatus_ToDone_SetsCompletedAt()
    {
        _fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _fixture.ManagerToken);
        
        // Get a project with tasks
        var projectResponse = await _fixture.Client.GetAsync("/api/v1/projects");
        var projects = await projectResponse.Content.ReadFromJsonAsync<List<JsonElement>>();
        var projectId = projects![0].GetProperty("id").GetGuid();
        
        var taskResponse = await _fixture.Client.GetAsync($"/api/v1/projects/{projectId}/tasks");
        var tasks = await taskResponse.Content.ReadFromJsonAsync<List<JsonElement>>();
        var taskId = tasks![0].GetProperty("id").GetGuid();
        var version = tasks![0].GetProperty("version").GetInt32();
        
        var request = new { status = "Done" };
        var httpRequest = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/tasks/{taskId}/status")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _fixture.ManagerToken);
        httpRequest.Headers.IfMatch.Add(new EntityTagHeaderValue($"\"{version}\""));
        
        var response = await _fixture.Client.SendAsync(httpRequest);
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify completed_at is set
        var getResponse = await _fixture.Client.GetAsync($"/api/v1/tasks/{taskId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedTask = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        updatedTask.GetProperty("completedAt").GetString().Should().NotBeNull();
    }
}

public class ReportApiTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    
    public ReportApiTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task GetDepartmentProgress_ReturnsReport()
    {
        _fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _fixture.ManagerToken);
        
        var response = await _fixture.Client.GetAsync("/api/v1/reports/departments/progress");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reports = await response.Content.ReadFromJsonAsync<List<DepartmentProgressReportDto>>();
        reports.Should().NotBeNull();
        reports!.Count.Should().BeGreaterOrEqualTo(4);
        
        // Verify report structure
        foreach (var report in reports)
        {
            report.DepartmentId.Should().NotBeEmpty();
            report.DepartmentName.Should().NotBeNullOrEmpty();
            report.ProjectCount.Should().BeGreaterOrEqualTo(0);
            report.TaskCount.Should().BeGreaterOrEqualTo(0);
            report.CompletedTaskCount.Should().BeGreaterOrEqualTo(0);
            report.OverdueTaskCount.Should().BeGreaterOrEqualTo(0);
            report.GeneratedAtUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
        }
    }
    
    [Fact]
    public async Task GetProjectSummary_ReturnsSummary()
    {
        _fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _fixture.ManagerToken);
        
        var projectResponse = await _fixture.Client.GetAsync("/api/v1/projects");
        var projects = await projectResponse.Content.ReadFromJsonAsync<List<JsonElement>>();
        var projectId = projects![0].GetProperty("id").GetGuid();
        
        var response = await _fixture.Client.GetAsync($"/api/v1/reports/projects/{projectId}/summary");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var summary = await response.Content.ReadFromJsonAsync<JsonElement>();
        summary.GetProperty("projectId").GetGuid().Should().Be(projectId);
        summary.GetProperty("projectName").GetString().Should().NotBeNullOrEmpty();
    }
    
    [Fact]
    public async Task ExportDepartmentProgress_Json_ReturnsFile()
    {
        _fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _fixture.ManagerToken);
        
        var response = await _fixture.Client.GetAsync("/api/v1/reports/departments/progress/export?format=json");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        response.Content.Headers.ContentDisposition.Should().NotBeNull();
        
        var content = await response.Content.ReadAsByteArrayAsync();
        content.Length.Should().BeGreaterThan(0);
    }
    
    [Fact]
    public async Task ExportDepartmentProgress_Csv_ReturnsFile()
    {
        _fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _fixture.ManagerToken);
        
        var response = await _fixture.Client.GetAsync("/api/v1/reports/departments/progress/export?format=csv");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        response.Content.Headers.ContentDisposition.Should().NotBeNull();
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("DepartmentId,DepartmentName");
    }
    
    [Fact]
    public async Task JsonAndCsvExports_HaveSameValues()
    {
        _fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _fixture.ManagerToken);
        
        var jsonResponse = await _fixture.Client.GetAsync("/api/v1/reports/departments/progress/export?format=json");
        var csvResponse = await _fixture.Client.GetAsync("/api/v1/reports/departments/progress/export?format=csv");
        
        jsonResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        csvResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var jsonContent = await jsonResponse.Content.ReadAsStringAsync();
        var csvContent = await csvResponse.Content.ReadAsStringAsync();
        
        var jsonReports = JsonSerializer.Deserialize<List<JsonElement>>(jsonContent);
        var csvLines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        // CSV has header + data rows
        csvLines.Length.Should().Be(jsonReports!.Count + 1);
    }
}

public class HealthCheckTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    
    public HealthCheckTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task LivenessEndpoint_ReturnsHealthy()
    {
        var response = await _fixture.Client.GetAsync("/health/live");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("status").GetString().Should().Be("healthy");
    }
    
    [Fact]
    public async Task ReadinessEndpoint_ReturnsReady()
    {
        var response = await _fixture.Client.GetAsync("/health/ready");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("status").GetString().Should().Be("ready");
    }
}