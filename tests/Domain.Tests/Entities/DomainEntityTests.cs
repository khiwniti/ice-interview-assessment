using BusinessAnalytics.Domain.Entities;
using BusinessAnalytics.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace BusinessAnalytics.Domain.Tests.Entities;

public class DepartmentTests
{
    [Fact]
    public void Create_WithValidName_Succeeds()
    {
        var department = new Department("Engineering");
        
        department.Name.Should().Be("Engineering");
        department.IsActive.Should().BeTrue();
        department.Id.Should().NotBeEmpty();
        department.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        department.Version.Should().Be(1);
    }
    
    [Fact]
    public void Create_WithWhitespace_TrimsName()
    {
        var department = new Department("  Engineering  ");
        
        department.Name.Should().Be("Engineering");
    }
    
    [Fact]
    public void Create_WithEmptyName_Throws()
    {
        var act = () => new Department("");
        
        act.Should().Throw<ArgumentException>()
            .WithMessage("Department name cannot be empty*");
    }
    
    [Fact]
    public void Create_WithWhitespaceOnly_Throws()
    {
        var act = () => new Department("   ");
        
        act.Should().Throw<ArgumentException>()
            .WithMessage("Department name cannot be empty*");
    }
    
    [Fact]
    public void UpdateName_WithValidName_UpdatesAndIncrementsVersion()
    {
        var department = new Department("Engineering");
        var originalVersion = department.Version;
        var originalUpdatedAt = department.UpdatedAt;
        
        Thread.Sleep(10); // Ensure timestamp difference
        department.UpdateName("Marketing");
        
        department.Name.Should().Be("Marketing");
        department.Version.Should().Be(originalVersion + 1);
        department.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }
    
    [Fact]
    public void UpdateName_WithEmptyName_Throws()
    {
        var department = new Department("Engineering");
        
        var act = () => department.UpdateName("");
        
        act.Should().Throw<ArgumentException>()
            .WithMessage("Department name cannot be empty*");
    }
    
    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var department = new Department("Engineering");
        
        department.Deactivate();
        
        department.IsActive.Should().BeFalse();
    }
    
    [Fact]
    public void Activate_SetsIsActiveTrue()
    {
        var department = new Department("Engineering");
        department.Deactivate();
        
        department.Activate();
        
        department.IsActive.Should().BeTrue();
    }
}

public class UserTests
{
    private readonly Guid _departmentId = Guid.NewGuid();
    
    [Fact]
    public void Create_WithValidData_Succeeds()
    {
        var user = new User(_departmentId, "test@company.com", "Test User", "hashed_password", UserRole.Contributor);
        
        user.Email.Should().Be("test@company.com");
        user.NormalizedEmail.Should().Be("TEST@COMPANY.COM");
        user.DisplayName.Should().Be("Test User");
        user.DepartmentId.Should().Be(_departmentId);
        user.Role.Should().Be(UserRole.Contributor);
        user.IsActive.Should().BeTrue();
        user.PasswordHash.Should().Be("hashed_password");
    }
    
    [Fact]
    public void Create_WithEmptyEmail_Throws()
    {
        var act = () => new User(_departmentId, "", "Test User", "hash", UserRole.Viewer);
        
        act.Should().Throw<ArgumentException>()
            .WithMessage("Email is required*");
    }
    
    [Fact]
    public void Create_WithEmptyDisplayName_Throws()
    {
        var act = () => new User(_departmentId, "test@company.com", "", "hash", UserRole.Viewer);
        
        act.Should().Throw<ArgumentException>()
            .WithMessage("Display name is required*");
    }
    
    [Fact]
    public void Create_WithEmptyPasswordHash_Throws()
    {
        var act = () => new User(_departmentId, "test@company.com", "Test User", "", UserRole.Viewer);
        
        act.Should().Throw<ArgumentException>()
            .WithMessage("Password hash is required*");
    }
    
    [Fact]
    public void Create_WithEmptyDepartmentId_Throws()
    {
        var act = () => new User(Guid.Empty, "test@company.com", "Test User", "hash", UserRole.Viewer);
        
        act.Should().Throw<ArgumentException>()
            .WithMessage("Department ID is required*");
    }
    
    [Fact]
    public void UpdateEmail_UpdatesNormalizedEmail()
    {
        var user = new User(_departmentId, "test@company.com", "Test User", "hash", UserRole.Viewer);
        
        user.UpdateEmail("new@company.com");
        
        user.Email.Should().Be("new@company.com");
        user.NormalizedEmail.Should().Be("NEW@COMPANY.COM");
    }
    
    [Fact]
    public void UpdateDisplayName_UpdatesAndIncrementsVersion()
    {
        var user = new User(_departmentId, "test@company.com", "Test User", "hash", UserRole.Viewer);
        var originalVersion = user.Version;
        
        user.UpdateDisplayName("Updated User");
        
        user.DisplayName.Should().Be("Updated User");
        user.Version.Should().Be(originalVersion + 1);
    }
    
    [Fact]
    public void UpdateRole_ChangesRole()
    {
        var user = new User(_departmentId, "test@company.com", "Test User", "hash", UserRole.Viewer);
        
        user.UpdateRole(UserRole.Manager);
        
        user.Role.Should().Be(UserRole.Manager);
    }
    
    [Fact]
    public void ChangePassword_UpdatesHash()
    {
        var user = new User(_departmentId, "test@company.com", "Test User", "old_hash", UserRole.Viewer);
        
        user.ChangePassword("new_hash");
        
        user.PasswordHash.Should().Be("new_hash");
    }
    
    [Fact]
    public void UpdateDepartment_ChangesDepartment()
    {
        var user = new User(_departmentId, "test@company.com", "Test User", "hash", UserRole.Viewer);
        var newDeptId = Guid.NewGuid();
        
        user.UpdateDepartment(newDeptId);
        
        user.DepartmentId.Should().Be(newDeptId);
    }
    
    [Fact]
    public void Deactivate_SetsInactiveAndTimestamp()
    {
        var user = new User(_departmentId, "test@company.com", "Test User", "hash", UserRole.Viewer);
        
        user.Deactivate();
        
        user.IsActive.Should().BeFalse();
        user.DeactivatedAt.Should().NotBeNull();
    }
    
    [Fact]
    public void Activate_SetsActiveAndClearsDeactivatedAt()
    {
        var user = new User(_departmentId, "test@company.com", "Test User", "hash", UserRole.Viewer);
        user.Deactivate();
        
        user.Activate();
        
        user.IsActive.Should().BeTrue();
        user.DeactivatedAt.Should().BeNull();
    }
}

public class ProjectTests
{
    private readonly Guid _departmentId = Guid.NewGuid();
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;
    
    [Fact]
    public void Create_WithValidData_Succeeds()
    {
        var project = new Project(_departmentId, _ownerId, "Test Project", "Description", ProjectStatus.Planned, _now, _now.AddDays(30));
        
        project.Name.Should().Be("Test Project");
        project.Description.Should().Be("Description");
        project.Status.Should().Be(ProjectStatus.Planned);
        project.DepartmentId.Should().Be(_departmentId);
        project.OwnerUserId.Should().Be(_ownerId);
        project.StartAt.Should().Be(_now);
        project.DueAt.Should().Be(_now.AddDays(30));
    }
    
    [Fact]
    public void Create_WithDueBeforeStart_Throws()
    {
        var act = () => new Project(_departmentId, _ownerId, "Test", "", ProjectStatus.Planned, _now.AddDays(30), _now);
        
        act.Should().Throw<ArgumentException>()
            .WithMessage("Due date cannot be before start date*");
    }
    
    [Fact]
    public void Create_WithEmptyName_Throws()
    {
        var act = () => new Project(_departmentId, _ownerId, "", "", ProjectStatus.Planned);
        
        act.Should().Throw<ArgumentException>()
            .WithMessage("Project name cannot be empty*");
    }
    
    [Fact]
    public void UpdateDetails_WithValidData_Updates()
    {
        var project = new Project(_departmentId, _ownerId, "Original", "Desc", ProjectStatus.Planned, _now, _now.AddDays(30));
        var originalVersion = project.Version;
        
        project.UpdateDetails("Updated", "New Desc", ProjectStatus.Active, _now.AddDays(1), _now.AddDays(60));
        
        project.Name.Should().Be("Updated");
        project.Description.Should().Be("New Desc");
        project.Status.Should().Be(ProjectStatus.Active);
        project.Version.Should().Be(originalVersion + 1);
    }
    
    [Fact]
    public void UpdateDetails_WhenDeleted_Throws()
    {
        var project = new Project(_departmentId, _ownerId, "Test", "", ProjectStatus.Planned);
        project.SoftDelete();
        
        var act = () => project.UpdateDetails("New", "", ProjectStatus.Active, null, null);
        
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot update a deleted project");
    }
    
    [Fact]
    public void UpdateDetails_WhenCompleted_Throws()
    {
        var project = new Project(_departmentId, _ownerId, "Test", "", ProjectStatus.Completed);
        
        var act = () => project.UpdateDetails("New", "", ProjectStatus.Active, null, null);
        
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot modify a completed project");
    }
    
    [Fact]
    public void UpdateStatus_ToCompleted_WithNoTasks_Throws()
    {
        var project = new Project(_departmentId, _ownerId, "Test", "", ProjectStatus.Active);
        
        var act = () => project.UpdateStatus(ProjectStatus.Completed);
        
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Project must have at least one non-cancelled task to be completed");
    }
    
    [Fact]
    public void UpdateStatus_ToCompleted_WithPendingTasks_Throws()
    {
        var project = new Project(_departmentId, _ownerId, "Test", "", ProjectStatus.Active);
        // Add a todo task via reflection (since Tasks is private)
        var task = new TaskItem(project.Id, "Task 1", "", TaskPriority.Medium);
        typeof(Project).GetProperty("Tasks")!.SetValue(project, new List<TaskItem> { task });
        
        var act = () => project.UpdateStatus(ProjectStatus.Completed);
        
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("All non-cancelled tasks must be Done to complete the project");
    }
    
    [Fact]
    public void UpdateStatus_ToCompleted_WithAllDone_Succeeds()
    {
        var project = new Project(_departmentId, _ownerId, "Test", "", ProjectStatus.Active);
        var task = new TaskItem(project.Id, "Task 1", "", TaskPriority.Medium);
        task.ChangeStatus(TaskStatus.Done, true);
        typeof(Project).GetProperty("Tasks")!.SetValue(project, new List<TaskItem> { task });
        
        project.UpdateStatus(ProjectStatus.Completed);
        
        project.Status.Should().Be(ProjectStatus.Completed);
    }
    
    [Fact]
    public void SoftDelete_SetsDeletedAtAndDeletesTasks()
    {
        var project = new Project(_departmentId, _ownerId, "Test", "", ProjectStatus.Active);
        var task1 = new TaskItem(project.Id, "Task 1", "", TaskPriority.Medium);
        var task2 = new TaskItem(project.Id, "Task 2", "", TaskPriority.Medium);
        typeof(Project).GetProperty("Tasks")!.SetValue(project, new List<TaskItem> { task1, task2 });
        
        project.SoftDelete();
        
        project.DeletedAt.Should().NotBeNull();
        task1.DeletedAt.Should().NotBeNull();
        task2.DeletedAt.Should().NotBeNull();
    }
    
    [Fact]
    public void CanBeEdited_ReturnsTrueForActiveProject()
    {
        var project = new Project(_departmentId, _ownerId, "Test", "", ProjectStatus.Active);
        
        project.CanBeEdited().Should().BeTrue();
    }
    
    [Fact]
    public void CanBeEdited_ReturnsFalseForCompletedProject()
    {
        var project = new Project(_departmentId, _ownerId, "Test", "", ProjectStatus.Completed);
        
        project.CanBeEdited().Should().BeFalse();
    }
    
    [Fact]
    public void CanBeEdited_ReturnsFalseForCancelledProject()
    {
        var project = new Project(_departmentId, _ownerId, "Test", "", ProjectStatus.Cancelled);
        
        project.CanBeEdited().Should().BeFalse();
    }
    
    [Fact]
    public void CanBeEdited_ReturnsFalseForDeletedProject()
    {
        var project = new Project(_departmentId, _ownerId, "Test", "", ProjectStatus.Active);
        project.SoftDelete();
        
        project.CanBeEdited().Should().BeFalse();
    }
    
    [Fact]
    public void GetNonCancelledTaskCount_ExcludesCancelledAndDeleted()
    {
        var project = new Project(_departmentId, _ownerId, "Test", "", ProjectStatus.Active);
        var task1 = new TaskItem(project.Id, "Task 1", "", TaskPriority.Medium); // Todo
        var task2 = new TaskItem(project.Id, "Task 2", "", TaskPriority.Medium);
        task2.ChangeStatus(TaskStatus.Cancelled, true);
        var task3 = new TaskItem(project.Id, "Task 3", "", TaskPriority.Medium);
        task3.SoftDelete();
        typeof(Project).GetProperty("Tasks")!.SetValue(project, new List<TaskItem> { task1, task2, task3 });
        
        project.GetNonCancelledTaskCount().Should().Be(1);
    }
    
    [Fact]
    public void GetCompletedTaskCount_CountsOnlyDone()
    {
        var project = new Project(_departmentId, _ownerId, "Test", "", ProjectStatus.Active);
        var task1 = new TaskItem(project.Id, "Task 1", "", TaskPriority.Medium);
        task1.ChangeStatus(TaskStatus.Done, true);
        var task2 = new TaskItem(project.Id, "Task 2", "", TaskPriority.Medium); // Todo
        var task3 = new TaskItem(project.Id, "Task 3", "", TaskPriority.Medium);
        task3.ChangeStatus(TaskStatus.Cancelled, true);
        typeof(Project).GetProperty("Tasks")!.SetValue(project, new List<TaskItem> { task1, task2, task3 });
        
        project.GetCompletedTaskCount().Should().Be(1);
    }
}

public class TaskItemTests
{
    private readonly Guid _projectId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;
    
    [Fact]
    public void Create_WithValidData_Succeeds()
    {
        var task = new TaskItem(_projectId, "Test Task", "Description", TaskPriority.High, _now.AddDays(7), Guid.NewGuid());
        
        task.Title.Should().Be("Test Task");
        task.Description.Should().Be("Description");
        task.Priority.Should().Be(TaskPriority.High);
        task.DueAt.Should().Be(_now.AddDays(7));
        task.Status.Should().Be(TaskStatus.Todo);
        task.ProjectId.Should().Be(_projectId);
    }
    
    [Fact]
    public void Create_WithEmptyTitle_Throws()
    {
        var act = () => new TaskItem(_projectId, "", "", TaskPriority.Medium);
        
        act.Should().Throw<ArgumentException>()
            .WithMessage("Task title cannot be empty*");
    }
    
    [Fact]
    public void UpdateDetails_WithValidData_Updates()
    {
        var task = new TaskItem(_projectId, "Original", "Desc", TaskPriority.Medium);
        var originalVersion = task.Version;
        
        task.UpdateDetails("Updated", "New Desc", TaskPriority.High, _now.AddDays(10));
        
        task.Title.Should().Be("Updated");
        task.Description.Should().Be("New Desc");
        task.Priority.Should().Be(TaskPriority.High);
        task.DueAt.Should().Be(_now.AddDays(10));
        task.Version.Should().Be(originalVersion + 1);
    }
    
    [Fact]
    public void AssignToUser_SetsAssignee()
    {
        var task = new TaskItem(_projectId, "Task", "", TaskPriority.Medium);
        var userId = Guid.NewGuid();
        
        task.AssignToUser(userId);
        
        task.AssignedToUserId.Should().Be(userId);
    }
    
    [Fact]
    public void Unassign_ClearsAssignee()
    {
        var task = new TaskItem(_projectId, "Task", "", TaskPriority.Medium, assignedToUserId: Guid.NewGuid());
        
        task.Unassign();
        
        task.AssignedToUserId.Should().BeNull();
    }
    
    [Fact]
    public void ChangeStatus_ToDone_WhenProjectEditable_SetsCompletedAt()
    {
        var task = new TaskItem(_projectId, "Task", "", TaskPriority.Medium);
        
        task.ChangeStatus(TaskStatus.Done, true);
        
        task.Status.Should().Be(TaskStatus.Done);
        task.CompletedAt.Should().NotBeNull();
    }
    
    [Fact]
    public void ChangeStatus_ToDone_WhenProjectNotEditable_Throws()
    {
        var task = new TaskItem(_projectId, "Task", "", TaskPriority.Medium);
        
        var act = () => task.ChangeStatus(TaskStatus.Done, false);
        
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot mark task as Done in a non-editable project");
    }
    
    [Fact]
    public void ChangeStatus_FromDone_ClearsCompletedAt()
    {
        var task = new TaskItem(_projectId, "Task", "", TaskPriority.Medium);
        task.ChangeStatus(TaskStatus.Done, true);
        
        task.ChangeStatus(TaskStatus.InProgress, true);
        
        task.Status.Should().Be(TaskStatus.InProgress);
        task.CompletedAt.Should().BeNull();
    }
    
    [Fact]
    public void ChangeStatus_SameStatus_DoesNothing()
    {
        var task = new TaskItem(_projectId, "Task", "", TaskPriority.Medium);
        var originalVersion = task.Version;
        var originalUpdatedAt = task.UpdatedAt;
        
        task.ChangeStatus(TaskStatus.Todo, true);
        
        task.Status.Should().Be(TaskStatus.Todo);
        task.Version.Should().Be(originalVersion);
        task.UpdatedAt.Should().BeCloseTo(originalUpdatedAt, TimeSpan.FromSeconds(1));
    }
    
    [Fact]
    public void SoftDelete_SetsDeletedAt()
    {
        var task = new TaskItem(_projectId, "Task", "", TaskPriority.Medium);
        
        task.SoftDelete();
        
        task.DeletedAt.Should().NotBeNull();
    }
    
    [Fact]
    public void IsOverdue_ReturnsTrueForPastDueUnfinishedTask()
    {
        var task = new TaskItem(_projectId, "Task", "", TaskPriority.Medium, _now.AddDays(-1));
        
        task.IsOverdue(_now).Should().BeTrue();
    }
    
    [Fact]
    public void IsOverdue_ReturnsFalseForDoneTask()
    {
        var task = new TaskItem(_projectId, "Task", "", TaskPriority.Medium, _now.AddDays(-1));
        task.ChangeStatus(TaskStatus.Done, true);
        
        task.IsOverdue(_now).Should().BeFalse();
    }
    
    [Fact]
    public void IsOverdue_ReturnsFalseForCancelledTask()
    {
        var task = new TaskItem(_projectId, "Task", "", TaskPriority.Medium, _now.AddDays(-1));
        task.ChangeStatus(TaskStatus.Cancelled, true);
        
        task.IsOverdue(_now).Should().BeFalse();
    }
    
    [Fact]
    public void IsOverdue_ReturnsFalseForNoDueDate()
    {
        var task = new TaskItem(_projectId, "Task", "", TaskPriority.Medium);
        
        task.IsOverdue(_now).Should().BeFalse();
    }
    
    [Fact]
    public void IsOverdue_ReturnsFalseForFutureDueDate()
    {
        var task = new TaskItem(_projectId, "Task", "", TaskPriority.Medium, _now.AddDays(1));
        
        task.IsOverdue(_now).Should().BeFalse();
    }
    
    [Fact]
    public void IsOverdue_ReturnsFalseForDeletedTask()
    {
        var task = new TaskItem(_projectId, "Task", "", TaskPriority.Medium, _now.AddDays(-1));
        task.SoftDelete();
        
        task.IsOverdue(_now).Should().BeFalse();
    }
}

public class SystemLogTests
{
    [Fact]
    public void Create_WithValidData_Succeeds()
    {
        var traceId = "trace-123";
        var entityId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var metadata = JsonDocument.Parse("{\"key\":\"value\"}");
        
        var log = new SystemLog(AuditAction.Created, "Project", entityId, AuditOutcome.Success, traceId, actorId, metadata);
        
        log.Action.Should().Be(AuditAction.Created);
        log.EntityType.Should().Be("Project");
        log.EntityId.Should().Be(entityId);
        log.Outcome.Should().Be(AuditOutcome.Success);
        log.TraceId.Should().Be(traceId);
        log.ActorUserId.Should().Be(actorId);
        log.Metadata.Should().Be(metadata);
    }
    
    [Fact]
    public void Create_WithEmptyEntityType_Throws()
    {
        var act = () => new SystemLog(AuditAction.Created, "", Guid.NewGuid(), AuditOutcome.Success, "trace");
        
        act.Should().Throw<ArgumentException>()
            .WithMessage("Entity type is required*");
    }
    
    [Fact]
    public void Create_WithEmptyEntityId_Throws()
    {
        var act = () => new SystemLog(AuditAction.Created, "Project", Guid.Empty, AuditOutcome.Success, "trace");
        
        act.Should().Throw<ArgumentException>()
            .WithMessage("Entity ID is required*");
    }
    
    [Fact]
    public void Create_WithEmptyTraceId_Throws()
    {
        var act = () => new SystemLog(AuditAction.Created, "Project", Guid.NewGuid(), AuditOutcome.Success, "");
        
        act.Should().Throw<ArgumentException>()
            .WithMessage("Trace ID is required*");
    }
}