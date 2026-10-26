using BusinessAnalytics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusinessAnalytics.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        
        builder.HasKey(u => u.Id);
        
        builder.Property(u => u.Id)
            .HasDefaultValueSql("gen_random_uuid()");
        
        builder.Property(u => u.DepartmentId)
            .IsRequired();
        
        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);
        
        builder.Property(u => u.NormalizedEmail)
            .IsRequired()
            .HasMaxLength(256);
        
        builder.Property(u => u.DisplayName)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(512);
        
        builder.Property(u => u.Role)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(4); // Viewer
        
        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
        
        builder.Property(u => u.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamptz");
        
        builder.Property(u => u.UpdatedAt)
            .IsRequired()
            .HasColumnType("timestamptz");
        
        builder.Property(u => u.DeactivatedAt)
            .HasColumnType("timestamptz");
        
        builder.Property(u => u.Version)
            .IsRequired()
            .IsConcurrencyToken();
        
        builder.HasIndex(u => u.NormalizedEmail)
            .IsUnique()
            .HasDatabaseName("ix_users_normalized_email");
        
        builder.HasIndex(u => u.DepartmentId)
            .HasDatabaseName("ix_users_department_id");
        
        builder.HasIndex(u => u.IsActive)
            .HasDatabaseName("ix_users_is_active");
        
        builder.HasOne(u => u.Department)
            .WithMany(d => d.Users)
            .HasForeignKey(u => u.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_users_department");
    }
}