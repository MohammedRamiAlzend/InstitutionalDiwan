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
        builder.Property(x => x.ActionType).HasColumnName("ActionType").HasMaxLength(80).IsRequired();
        builder.Property(x => x.EntityType).HasColumnName("EntityType").HasMaxLength(100).IsRequired();
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
