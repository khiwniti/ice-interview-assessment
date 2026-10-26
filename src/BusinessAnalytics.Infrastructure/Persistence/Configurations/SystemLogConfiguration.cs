using BusinessAnalytics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace BusinessAnalytics.Infrastructure.Persistence.Configurations;

public class SystemLogConfiguration : IEntityTypeConfiguration<SystemLog>
{
    public void Configure(EntityTypeBuilder<SystemLog> builder)
    {
        builder.ToTable("system_logs");
        
        builder.HasKey(s => s.Id);
        
        builder.Property(s => s.Id)
            .HasDefaultValueSql("gen_random_uuid()");
        
        builder.Property(s => s.ActorUserId);
        
        builder.Property(s => s.Action)
            .IsRequired()
            .HasConversion<int>();
        
        builder.Property(s => s.EntityType)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(s => s.EntityId)
            .IsRequired();
        
        builder.Property(s => s.Outcome)
            .IsRequired()
            .HasConversion<int>();
        
        builder.Property(s => s.TraceId)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(s => s.Metadata)
            .HasColumnType("jsonb");
        
        builder.Property(s => s.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamptz");
        
        builder.HasIndex(s => s.CreatedAt)
            .HasDatabaseName("ix_system_logs_created_at_desc")
            .IsDescending();
        
        builder.HasIndex(s => new { s.EntityType, s.EntityId, s.CreatedAt })
            .HasDatabaseName("ix_system_logs_entity_type_entity_id_created_at");
        
        builder.HasIndex(s => s.ActorUserId)
            .HasDatabaseName("ix_system_logs_actor_user_id");
        
        builder.HasOne(s => s.Actor)
            .WithMany(u => u.SystemLogs)
            .HasForeignKey(s => s.ActorUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_system_logs_actor");
    }
}