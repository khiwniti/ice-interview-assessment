using BusinessAnalytics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusinessAnalytics.Infrastructure.Persistence.Configurations;

public class TaskConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("tasks");
        
        builder.HasKey(t => t.Id);
        
        builder.Property(t => t.Id)
            .HasDefaultValueSql("gen_random_uuid()");
        
        builder.Property(t => t.ProjectId)
            .IsRequired();
        
        builder.Property(t => t.AssignedToUserId);
        
        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(t => t.Description)
            .HasMaxLength(2000);
        
        builder.Property(t => t.Status)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(1); // Todo
        
        builder.Property(t => t.Priority)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(2); // Medium
        
        builder.Property(t => t.DueAt)
            .HasColumnType("timestamptz");
        
        builder.Property(t => t.CompletedAt)
            .HasColumnType("timestamptz");
        
        builder.Property(t => t.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamptz");
        
        builder.Property(t => t.UpdatedAt)
            .IsRequired()
            .HasColumnType("timestamptz");
        
        builder.Property(t => t.DeletedAt)
            .HasColumnType("timestamptz");
        
        builder.Property(t => t.Version)
            .IsRequired()
            .IsConcurrencyToken();
        
        builder.HasIndex(t => new { t.ProjectId, t.Status })
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_tasks_project_status");
        
        builder.HasIndex(t => t.AssignedToUserId)
            .HasFilter("deleted_at IS NULL AND assigned_to_user_id IS NOT NULL")
            .HasDatabaseName("ix_tasks_assigned_user");
        
        builder.HasIndex(t => t.DueAt)
            .HasFilter("deleted_at IS NULL AND status NOT IN (4, 5) AND due_at IS NOT NULL")
            .HasDatabaseName("ix_tasks_due_at");
        
        builder.HasIndex(t => t.DeletedAt)
            .HasDatabaseName("ix_tasks_deleted_at");
        
        builder.HasOne(t => t.Project)
            .WithMany(p => p.Tasks)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_tasks_project");
        
        builder.HasOne(t => t.Assignee)
            .WithMany(u => u.AssignedTasks)
            .HasForeignKey(t => t.AssignedToUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_tasks_assignee");
    }
}