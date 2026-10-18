using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InstitutionalDiwan.Domain.Entities.Audit;

namespace InstitutionalDiwan.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AUDIT_LOGS");
        builder.Ignore(x => x.Id);

        builder.HasKey(x => x.AuditLogId);
        builder.Property(x => x.AuditLogId).HasColumnName("AuditLogId").UseIdentityColumn();
        builder.Property(x => x.OccurredAtUtc).HasColumnName("OccurredAtUtc").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.ActorUserId).HasColumnName("ActorUserId");
        builder.Property(x => x.ActorDepartmentId).HasColumnName("ActorDepartmentId");
        builder.Property(x => x.ModuleName).HasColumnName("ModuleName").HasMaxLength(80).IsRequired();
        builder.Property(x => x.ActionTypeId).HasColumnName("ActionTypeId").IsRequired();
        builder.Property(x => x.EntityTypeId).HasColumnName("EntityTypeId").IsRequired();
        builder.Property(x => x.EntityId).HasColumnName("EntityId").HasMaxLength(100).IsRequired();
        builder.Property(x => x.RelatedDocumentId).HasColumnName("RelatedDocumentId");
        builder.Property(x => x.OldValuesJson).HasColumnName("OldValuesJson");
        builder.Property(x => x.NewValuesJson).HasColumnName("NewValuesJson");
        builder.Property(x => x.IpAddress).HasColumnName("IpAddress").HasMaxLength(64);
        builder.Property(x => x.CorrelationId).HasColumnName("CorrelationId").HasMaxLength(100);
        builder.Property(x => x.PreviousHash).HasColumnName("PreviousHash").HasColumnType("char(64)").IsRequired();
        builder.Property(x => x.EventHash).HasColumnName("EventHash").HasColumnType("char(64)").IsRequired();

        builder.HasIndex(x => x.OccurredAtUtc).HasDatabaseName("IX_AUDIT_LOGS_OCCURRED_AT");
        builder.HasIndex(x => x.RelatedDocumentId).HasDatabaseName("IX_AUDIT_LOGS_DOC");
        builder.HasIndex(x => new { x.EntityTypeId, x.EntityId }).HasDatabaseName("IX_AUDIT_LOGS_ENTITY");

        builder.HasOne<AuditActionType>()
            .WithMany()
            .HasForeignKey(x => x.ActionTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AuditEntityType>()
            .WithMany()
            .HasForeignKey(x => x.EntityTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AuditCheckpointConfiguration : IEntityTypeConfiguration<AuditCheckpoint>
{
    public void Configure(EntityTypeBuilder<AuditCheckpoint> builder)
    {
        builder.ToTable("AUDIT_CHECKPOINTS");
        builder.Ignore(x => x.Id);

        builder.HasKey(x => x.CheckpointId);
        builder.Property(x => x.CheckpointId).HasColumnName("CheckpointId").ValueGeneratedNever();
        builder.Property(x => x.FromAuditLogId).HasColumnName("FromAuditLogId").IsRequired();
        builder.Property(x => x.ToAuditLogId).HasColumnName("ToAuditLogId").IsRequired();
        builder.Property(x => x.TerminalHash).HasColumnName("TerminalHash").HasColumnType("char(64)").IsRequired();
        builder.Property(x => x.RecordCount).HasColumnName("RecordCount").IsRequired();
        builder.Property(x => x.CreatedByUserId).HasColumnName("CreatedByUserId").IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("CreatedAtUtc").HasColumnType("datetime2(3)").IsRequired();
    }
}

public sealed class AuditIntegrityCheckConfiguration : IEntityTypeConfiguration<AuditIntegrityCheck>
{
    public void Configure(EntityTypeBuilder<AuditIntegrityCheck> builder)
    {
        builder.ToTable("AUDIT_INTEGRITY_CHECKS");
        builder.Ignore(x => x.Id);

        builder.HasKey(x => x.CheckId);
        builder.Property(x => x.CheckId).HasColumnName("CheckId").ValueGeneratedNever();
        builder.Property(x => x.StartAuditLogId).HasColumnName("StartAuditLogId").IsRequired();
        builder.Property(x => x.EndAuditLogId).HasColumnName("EndAuditLogId").IsRequired();
        builder.Property(x => x.IsValid).HasColumnName("IsValid").IsRequired();
        builder.Property(x => x.FirstCorruptedLogId).HasColumnName("FirstCorruptedLogId");
        builder.Property(x => x.FailureDetails).HasColumnName("FailureDetails").HasMaxLength(1000);
        builder.Property(x => x.ExecutedByUserId).HasColumnName("ExecutedByUserId").IsRequired();
        builder.Property(x => x.ExecutedAtUtc).HasColumnName("ExecutedAtUtc").HasColumnType("datetime2(3)").IsRequired();
    }
}

public sealed class AuditActionTypeConfiguration : IEntityTypeConfiguration<AuditActionType>
{
    public void Configure(EntityTypeBuilder<AuditActionType> builder)
    {
        builder.ToTable("AUDIT_ACTION_TYPES");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(x => x.Desc).HasColumnName("Desc").HasMaxLength(150).IsRequired();
        builder.Property(x => x.DescAr).HasColumnName("DescAr").HasMaxLength(150).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("IsActive").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
    }
}

public sealed class AuditEntityTypeConfiguration : IEntityTypeConfiguration<AuditEntityType>
{
    public void Configure(EntityTypeBuilder<AuditEntityType> builder)
    {
        builder.ToTable("AUDIT_ENTITY_TYPES");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(x => x.Desc).HasColumnName("Desc").HasMaxLength(150).IsRequired();
        builder.Property(x => x.DescAr).HasColumnName("DescAr").HasMaxLength(150).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("IsActive").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
    }
}
