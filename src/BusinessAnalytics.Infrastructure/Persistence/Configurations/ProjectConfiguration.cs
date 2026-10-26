using BusinessAnalytics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusinessAnalytics.Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");
        
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Id)
            .HasDefaultValueSql("gen_random_uuid()");
        
        builder.Property(p => p.DepartmentId)
            .IsRequired();
        
        builder.Property(p => p.OwnerUserId)
            .IsRequired();
        
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(p => p.Description)
            .HasMaxLength(2000);
        
        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(1); // Planned
        
        builder.Property(p => p.StartAt)
            .HasColumnType("timestamptz");
        
        builder.Property(p => p.DueAt)
            .HasColumnType("timestamptz");
        
        builder.Property(p => p.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamptz");
        
        builder.Property(p => p.UpdatedAt)
            .IsRequired()
            .HasColumnType("timestamptz");
        
        builder.Property(p => p.DeletedAt)
            .HasColumnType("timestamptz");
        
        builder.Property(p => p.Version)
            .IsRequired()
            .IsConcurrencyToken();
        
        builder.HasIndex(p => new { p.DepartmentId, p.Status })
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_projects_department_status");
        
        builder.HasIndex(p => p.OwnerUserId)
            .HasDatabaseName("ix_projects_owner_user_id");
        
        builder.HasIndex(p => p.DeletedAt)
            .HasDatabaseName("ix_projects_deleted_at");
        
        builder.HasOne(p => p.Department)
            .WithMany(d => d.Projects)
            .HasForeignKey(p => p.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_projects_department");
        
        builder.HasOne(p => p.Owner)
            .WithMany(u => u.OwnedProjects)
            .HasForeignKey(p => p.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_projects_owner");
    }
}