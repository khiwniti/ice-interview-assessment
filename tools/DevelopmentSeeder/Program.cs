using BusinessAnalytics.Domain.Entities;
using BusinessAnalytics.Domain.Enums;
using BusinessAnalytics.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BusinessAnalytics.DevelopmentSeeder;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddInfrastructure(context.Configuration);
            })
            .Build();
        
        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BusinessAnalyticsDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();
        
        // Seed roles
        await SeedRolesAsync(roleManager);
        
        // Seed departments
        await SeedDepartmentsAsync(context);
        
        // Seed users
        await SeedUsersAsync(context, userManager);
        
        // Seed projects and tasks
        await SeedProjectsAndTasksAsync(context);
        
        logger.LogInformation("Development seeding completed");
    }
    
    private static async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager)
    {
        var roles = new[] { "Admin", "Manager", "Contributor", "Viewer" };
        
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }
    }
    
    private static async Task SeedDepartmentsAsync(BusinessAnalyticsDbContext context)
    {
        if (await context.Departments.AnyAsync())
            return;
        
        var departments = new[]
        {
            new Department("Engineering"),
            new Department("Marketing"),
            new Department("Sales"),
            new Department("Operations")
        };
        
        foreach (var dept in departments)
        {
            await context.Departments.AddAsync(dept);
        }
        
        await context.SaveChangesAsync();
    }
    
    private static async Task SeedUsersAsync(BusinessAnalyticsDbContext context, UserManager<IdentityUser<Guid>> userManager)
    {
        if (await context.Users.AnyAsync())
            return;
        
        var engineering = await context.Departments.FirstAsync(d => d.Name == "Engineering");
        var marketing = await context.Departments.FirstAsync(d => d.Name == "Marketing");
        var sales = await context.Departments.FirstAsync(d => d.Name == "Sales");
        var operations = await context.Departments.FirstAsync(d => d.Name == "Operations");
        
        var users = new[]
        {
            new { Email = "admin@company.com", DisplayName = "Admin User", DepartmentId = engineering.Id, Role = UserRole.Admin, Password = "Admin123!" },
            new { Email = "eng.manager@company.com", DisplayName = "Engineering Manager", DepartmentId = engineering.Id, Role = UserRole.Manager, Password = "Manager123!" },
            new { Email = "eng.dev1@company.com", DisplayName = "Senior Developer", DepartmentId = engineering.Id, Role = UserRole.Contributor, Password = "Dev123!" },
            new { Email = "eng.dev2@company.com", DisplayName = "Junior Developer", DepartmentId = engineering.Id, Role = UserRole.Contributor, Password = "Dev123!" },
            new { Email = "mkt.manager@company.com", DisplayName = "Marketing Manager", DepartmentId = marketing.Id, Role = UserRole.Manager, Password = "Manager123!" },
            new { Email = "mkt.specialist@company.com", DisplayName = "Marketing Specialist", DepartmentId = marketing.Id, Role = UserRole.Contributor, Password = "Specialist123!" },
            new { Email = "sales.manager@company.com", DisplayName = "Sales Manager", DepartmentId = sales.Id, Role = UserRole.Manager, Password = "Manager123!" },
            new { Email = "sales.rep@company.com", DisplayName = "Sales Representative", DepartmentId = sales.Id, Role = UserRole.Contributor, Password = "Rep123!" },
            new { Email = "ops.manager@company.com", DisplayName = "Operations Manager", DepartmentId = operations.Id, Role = UserRole.Manager, Password = "Manager123!" },
            new { Email = "viewer@company.com", DisplayName = "Read-only Viewer", DepartmentId = engineering.Id, Role = UserRole.Viewer, Password = "Viewer123!" }
        };
        
        foreach (var userData in users)
        {
            var identityUser = new IdentityUser<Guid>
            {
                UserName = userData.Email,
                Email = userData.Email,
                EmailConfirmed = true
            };
            
            var result = await userManager.CreateAsync(identityUser, userData.Password);
            if (!result.Succeeded)
                continue;
            
            await userManager.AddToRoleAsync(identityUser, userData.Role.ToString());
            
            var passwordHasher = new PasswordHasher<IdentityUser<Guid>>();
            var passwordHash = passwordHasher.HashPassword(identityUser, userData.Password);
            
            var businessUser = new User(
                userData.DepartmentId,
                userData.Email,
                userData.DisplayName,
                passwordHash,
                userData.Role);
            
            // Set the identity user ID to match
            typeof(User).GetProperty("Id")!.SetValue(businessUser, identityUser.Id);
            
            await context.Users.AddAsync(businessUser);
        }
        
        await context.SaveChangesAsync();
    }
    
    private static async Task SeedProjectsAndTasksAsync(BusinessAnalyticsDbContext context)
    {
        if (await context.Projects.AnyAsync())
            return;
        
        var engineering = await context.Departments.FirstAsync(d => d.Name == "Engineering");
        var marketing = await context.Departments.FirstAsync(d => d.Name == "Marketing");
        
        var engManager = await context.Users.FirstAsync(u => u.Email == "eng.manager@company.com");
        var engDev1 = await context.Users.FirstAsync(u => u.Email == "eng.dev1@company.com");
        var engDev2 = await context.Users.FirstAsync(u => u.Email == "eng.dev2@company.com");
        var mktManager = await context.Users.FirstAsync(u => u.Email == "mkt.manager@company.com");
        var mktSpecialist = await context.Users.FirstAsync(u => u.Email == "mkt.specialist@company.com");
        
        var now = DateTimeOffset.UtcNow;
        
        // Engineering projects
        var project1 = new Project(engineering.Id, engManager.Id, "API Refactoring", "Refactor legacy API endpoints", ProjectStatus.Active, now.AddDays(-30), now.AddDays(60));
        var project2 = new Project(engineering.Id, engManager.Id, "Database Migration", "Migrate to PostgreSQL 16", ProjectStatus.Active, now.AddDays(-10), now.AddDays(45));
        var project3 = new Project(engineering.Id, engManager.Id, "Security Audit", "Complete security audit of all services", ProjectStatus.Planned, now.AddDays(5), now.AddDays(90));
        
        // Marketing projects
        var project4 = new Project(marketing.Id, mktManager.Id, "Q4 Campaign Launch", "Launch Q4 marketing campaign", ProjectStatus.Active, now.AddDays(-15), now.AddDays(30));
        var project5 = new Project(marketing.Id, mktManager.Id, "Brand Guidelines Update", "Update brand guidelines document", ProjectStatus.Completed, now.AddDays(-60), now.AddDays(-5));
        
        await context.Projects.AddRangeAsync(project1, project2, project3, project4, project5);
        await context.SaveChangesAsync();
        
        // Tasks for project1 (API Refactoring)
        var tasks1 = new[]
        {
            new TaskItem(project1.Id, "Audit current endpoints", "Review all existing API endpoints", TaskPriority.High, now.AddDays(10), engDev1.Id),
            new TaskItem(project1.Id, "Design new architecture", "Create new API architecture document", TaskPriority.High, now.AddDays(20), engManager.Id),
            new TaskItem(project1.Id, "Implement authentication middleware", "Add JWT authentication", TaskPriority.Critical, now.AddDays(30), engDev1.Id),
            new TaskItem(project1.Id, "Write integration tests", "Add integration tests for new endpoints", TaskPriority.Medium, now.AddDays(45), engDev2.Id),
            new TaskItem(project1.Id, "Deploy to staging", "Deploy refactored API to staging environment", TaskPriority.High, now.AddDays(55), engDev1.Id)
        };
        
        // Tasks for project2 (Database Migration)
        var tasks2 = new[]
        {
            new TaskItem(project2.Id, "Analyze current schema", "Document current database schema", TaskPriority.High, now.AddDays(5), engDev2.Id),
            new TaskItem(project2.Id, "Create migration scripts", "Write EF Core migrations", TaskPriority.Critical, now.AddDays(15), engDev1.Id),
            new TaskItem(project2.Id, "Test migration on staging", "Run migrations against staging data", TaskPriority.High, now.AddDays(25), engDev2.Id),
            new TaskItem(project2.Id, "Production deployment", "Execute migration in production", TaskPriority.Critical, now.AddDays(40), engManager.Id)
        };
        
        // Tasks for project4 (Q4 Campaign)
        var tasks4 = new[]
        {
            new TaskItem(project4.Id, "Define campaign goals", "Set measurable KPIs for Q4", TaskPriority.High, now.AddDays(5), mktManager.Id),
            new TaskItem(project4.Id, "Create ad creatives", "Design social media and email creatives", TaskPriority.Medium, now.AddDays(15), mktSpecialist.Id),
            new TaskItem(project4.Id, "Set up tracking", "Configure analytics and UTM parameters", TaskPriority.High, now.AddDays(10), mktSpecialist.Id),
            new TaskItem(project4.Id, "Launch campaign", "Go live with Q4 campaign", TaskPriority.Critical, now.AddDays(25), mktManager.Id)
        };
        
        // Tasks for project5 (Brand Guidelines - completed)
        var tasks5 = new[]
        {
            new TaskItem(project5.Id, "Audit current guidelines", "Review existing brand documentation", TaskPriority.Medium, now.AddDays(-50), mktSpecialist.Id),
            new TaskItem(project5.Id, "Update logo usage rules", "Define new logo usage guidelines", TaskPriority.High, now.AddDays(-40), mktSpecialist.Id),
            new TaskItem(project5.Id, "Define color palette", "Document primary and secondary colors", TaskPriority.High, now.AddDays(-35), mktSpecialist.Id),
            new TaskItem(project5.Id, "Create template library", "Build templates for common assets", TaskPriority.Medium, now.AddDays(-20), mktManager.Id),
            new TaskItem(project5.Id, "Review and approve", "Final review with stakeholders", TaskPriority.Critical, now.AddDays(-10), mktManager.Id)
        };
        
        // Mark project5 tasks as done
        foreach (var task in tasks5)
        {
            task.ChangeStatus(TaskStatus.Done, true);
        }
        
        await context.Tasks.AddRangeAsync(tasks1);
        await context.Tasks.AddRangeAsync(tasks2);
        await context.Tasks.AddRangeAsync(tasks4);
        await context.Tasks.AddRangeAsync(tasks5);
        
        await context.SaveChangesAsync();
    }
}