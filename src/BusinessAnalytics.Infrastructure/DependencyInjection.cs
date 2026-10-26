using BusinessAnalytics.Application.Abstractions;
using BusinessAnalytics.Infrastructure.Persistence;
using BusinessAnalytics.Infrastructure.Persistence.Repositories;
using BusinessAnalytics.Infrastructure.Auditing;
using BusinessAnalytics.Infrastructure.Reporting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace BusinessAnalytics.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found");
        
        services.AddDbContext<BusinessAnalyticsDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(BusinessAnalyticsDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
            });
        });
        
        services.AddIdentity<IdentityUser<Guid>, IdentityRole<Guid>>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<BusinessAnalyticsDbContext>()
        .AddDefaultTokenProviders();
        
        // Repositories
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<ISystemLogRepository, SystemLogRepository>();
        
        // Services
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IReportService, ReportService>();
        
        return services;
    }
}