using BusinessAnalytics.Domain.Entities;
using BusinessAnalytics.Application.Abstractions;
using BusinessAnalytics.Infrastructure.Persistence;
using BusinessAnalytics.Api.Controllers;
using NetArchTest.Rules;
using Xunit;
using FluentAssertions;

namespace BusinessAnalytics.Architecture.Tests;

public class ArchitectureTests
{
    [Fact]
    public void Domain_ShouldNotReference_Infrastructure()
    {
        var domainAssembly = typeof(Department).Assembly;
        var infrastructureAssembly = typeof(BusinessAnalyticsDbContext).Assembly;
        
        var result = Types.InAssembly(domainAssembly)
            .ShouldNot()
            .HaveDependencyOn(infrastructureAssembly.GetName().Name!)
            .GetResult();
        
        result.IsSuccessful.Should().BeTrue();
    }
    
    [Fact]
    public void Domain_ShouldNotReference_Api()
    {
        var domainAssembly = typeof(Department).Assembly;
        var apiAssembly = typeof(DepartmentsController).Assembly;
        
        var result = Types.InAssembly(domainAssembly)
            .ShouldNot()
            .HaveDependencyOn(apiAssembly.GetName().Name!)
            .GetResult();
        
        result.IsSuccessful.Should().BeTrue();
    }
    
    [Fact]
    public void Domain_ShouldNotReference_EntityFramework()
    {
        var domainAssembly = typeof(Department).Assembly;
        
        var result = Types.InAssembly(domainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();
        
        result.IsSuccessful.Should().BeTrue();
    }
    
    [Fact]
    public void Domain_ShouldNotReference_AspNetCore()
    {
        var domainAssembly = typeof(Department).Assembly;
        
        var result = Types.InAssembly(domainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.AspNetCore")
            .GetResult();
        
        result.IsSuccessful.Should().BeTrue();
    }
    
    [Fact]
    public void Application_ShouldNotReference_Api()
    {
        var appAssembly = typeof(IDepartmentRepository).Assembly;
        var apiAssembly = typeof(DepartmentsController).Assembly;
        
        var result = Types.InAssembly(appAssembly)
            .ShouldNot()
            .HaveDependencyOn(apiAssembly.GetName().Name!)
            .GetResult();
        
        result.IsSuccessful.Should().BeTrue();
    }
    
    [Fact]
    public void Application_ShouldNotReference_EntityFramework()
    {
        var appAssembly = typeof(IDepartmentRepository).Assembly;
        
        var result = Types.InAssembly(appAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();
        
        result.IsSuccessful.Should().BeTrue();
    }
    
    [Fact]
    public void Application_ShouldNotReference_AspNetCore()
    {
        var appAssembly = typeof(IDepartmentRepository).Assembly;
        
        var result = Types.InAssembly(appAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.AspNetCore")
            .GetResult();
        
        result.IsSuccessful.Should().BeTrue();
    }
    
    [Fact]
    public void Infrastructure_ShouldReference_Domain_And_Application()
    {
        var infraAssembly = typeof(BusinessAnalyticsDbContext).Assembly;
        var domainAssembly = typeof(Department).Assembly;
        var appAssembly = typeof(IDepartmentRepository).Assembly;
        
        var result = Types.InAssembly(infraAssembly)
            .Should()
            .HaveDependencyOn(domainAssembly.GetName().Name!)
            .And()
            .HaveDependencyOn(appAssembly.GetName().Name!)
            .GetResult();
        
        result.IsSuccessful.Should().BeTrue();
    }
    
    [Fact]
    public void Controllers_ShouldNotContain_BusinessLogic()
    {
        // Controllers should not directly use EF Core DbContext
        var apiAssembly = typeof(DepartmentsController).Assembly;
        
        var result = Types.InAssembly(apiAssembly)
            .That()
            .ResideInNamespace("BusinessAnalytics.Api.Controllers")
            .ShouldNot()
            .HaveDependencyOn("BusinessAnalytics.Infrastructure.Persistence.BusinessAnalyticsDbContext")
            .GetResult();
        
        result.IsSuccessful.Should().BeTrue();
    }
    
    [Fact]
    public void Controllers_ShouldNotContain_Sql()
    {
        var apiAssembly = typeof(DepartmentsController).Assembly;
        
        var result = Types.InAssembly(apiAssembly)
            .That()
            .ResideInNamespace("BusinessAnalytics.Api.Controllers")
            .ShouldNot()
            .HaveDependencyOn("Npgsql")
            .GetResult();
        
        result.IsSuccessful.Should().BeTrue();
    }
    
    [Fact]
    public void Repositories_ShouldBe_In_Infrastructure()
    {
        var infraAssembly = typeof(BusinessAnalyticsDbContext).Assembly;
        var appAssembly = typeof(IDepartmentRepository).Assembly;
        
        // All implementations of Application repository interfaces should be in Infrastructure
        var interfaceTypes = Types.InAssembly(appAssembly)
            .That()
            .AreInterfaces()
            .And()
            .HaveNameEndingWith("Repository")
            .GetTypes();
        
        foreach (var interfaceType in interfaceTypes)
        {
            var implementations = Types.InAssembly(infraAssembly)
                .That()
                .ImplementInterface(interfaceType)
                .GetTypes();
            
            implementations.Should().NotBeEmpty($"No implementation found for {interfaceType.Name} in Infrastructure");
        }
    }
    
    [Fact]
    public void Entities_ShouldBe_In_Domain()
    {
        var domainAssembly = typeof(Department).Assembly;
        
        var entityTypes = Types.InAssembly(domainAssembly)
            .That()
            .Inherit(typeof(BaseEntity))
            .GetTypes();
        
        entityTypes.Should().Contain(t => t.Name == "Department");
        entityTypes.Should().Contain(t => t.Name == "User");
        entityTypes.Should().Contain(t => t.Name == "Project");
        entityTypes.Should().Contain(t => t.Name == "TaskItem");
        entityTypes.Should().Contain(t => t.Name == "SystemLog");
    }
    
    [Fact]
    public void Enums_ShouldBe_In_Domain()
    {
        var domainAssembly = typeof(Department).Assembly;
        
        var enumTypes = Types.InAssembly(domainAssembly)
            .That()
            .AreEnums()
            .GetTypes();
        
        enumTypes.Should().Contain(t => t.Name == "ProjectStatus");
        enumTypes.Should().Contain(t => t.Name == "TaskStatus");
        enumTypes.Should().Contain(t => t.Name == "TaskPriority");
        enumTypes.Should().Contain(t => t.Name == "UserRole");
        enumTypes.Should().Contain(t => t.Name == "AuditAction");
        enumTypes.Should().Contain(t => t.Name == "AuditOutcome");
    }
}