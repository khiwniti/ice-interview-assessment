using BusinessAnalytics.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BusinessAnalytics.Infrastructure.Persistence;

public class BusinessAnalyticsDbContext : IdentityDbContext<IdentityUser<Guid>, IdentityRole<Guid>, Guid>
{
    public BusinessAnalyticsDbContext(DbContextOptions<BusinessAnalyticsDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<SystemLog> SystemLogs => Set<SystemLog>();
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        builder.ApplyConfigurationsFromAssembly(typeof(BusinessAnalyticsDbContext).Assembly);
        
        // Configure Identity tables to use UUIDs
        builder.Entity<IdentityUser<Guid>>().ToTable("aspnet_users");
        builder.Entity<IdentityRole<Guid>>().ToTable("aspnet_roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("aspnet_user_roles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("aspnet_user_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("aspnet_user_logins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("aspnet_role_claims");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("aspnet_user_tokens");
    }
}