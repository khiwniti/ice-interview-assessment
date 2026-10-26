using BusinessAnalytics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusinessAnalytics.Infrastructure.Persistence.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("departments");
        
        builder.HasKey(d => d.Id);
        
        builder.Property(d => d.Id)
            .HasDefaultValueSql("gen_random_uuid()");
        
        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(d => d.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
        
        builder.Property(d => d.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamptz");
        
        builder.Property(d => d.UpdatedAt)
            .IsRequired()
            .HasColumnType("timestamptz");
        
        builder.Property(d => d.Version)
            .IsRequired()
            .IsConcurrencyToken();
        
        builder.HasIndex(d => d.Name)
            .IsUnique()
            .HasDatabaseName("ix_departments_name");
        
        builder.HasIndex(d => d.IsActive)
            .HasDatabaseName("ix_departments_is_active");
    }
}