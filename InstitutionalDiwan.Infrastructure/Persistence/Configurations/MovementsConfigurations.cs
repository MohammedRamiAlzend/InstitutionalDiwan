using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InstitutionalDiwan.Domain.Entities.Movements;

namespace InstitutionalDiwan.Infrastructure.Persistence.Configurations;

public sealed class DocumentMovementConfiguration : IEntityTypeConfiguration<DocumentMovement>
{
    public void Configure(EntityTypeBuilder<DocumentMovement> builder)
    {
        builder.ToTable("DOCUMENT_MOVEMENTS");
        builder.Ignore(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        builder.HasKey(x => x.MovementId);
        builder.Property(x => x.MovementId).HasColumnName("MovementId").ValueGeneratedNever();
        builder.Property(x => x.DocumentId).HasColumnName("DocumentId").IsRequired();
        builder.Property(x => x.MovementType).HasColumnName("MovementType").HasMaxLength(50).IsRequired();
        builder.Property(x => x.SourceDepartmentId).HasColumnName("SourceDepartmentId").IsRequired();
        builder.Property(x => x.DestinationDepartmentId).HasColumnName("DestinationDepartmentId").IsRequired();
        builder.Property(x => x.AssignedToUserId).HasColumnName("AssignedToUserId");
        builder.Property(x => x.StatusCode).HasColumnName("StatusCode").HasMaxLength(30).IsRequired();
        builder.Property(x => x.CurrentStepNo).HasColumnName("CurrentStepNo").IsRequired();
        builder.Property(x => x.Instructions).HasColumnName("Instructions").HasMaxLength(1000);
        builder.Property(x => x.DueDate).HasColumnName("DueDate").HasColumnType("datetime2(3)");
        builder.Property(x => x.CreatedByUserId).HasColumnName("CreatedByUserId").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.ClosedAt).HasColumnName("ClosedAt").HasColumnType("datetime2(3)");
        builder.Property(x => x.RowVersion).HasColumnName("RowVersion").IsRowVersion();

        builder.HasIndex(x => new { x.DocumentId, x.StatusCode }).HasDatabaseName("IX_DOC_MOVEMENTS_DOC_STATUS");
        builder.HasIndex(x => new { x.DestinationDepartmentId, x.StatusCode }).HasDatabaseName("IX_DOC_MOVEMENTS_DEST_STATUS");

        builder.HasMany(x => x.Steps)
            .WithOne()
            .HasForeignKey(x => x.MovementId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Steps)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Events)
            .WithOne()
            .HasForeignKey(x => x.MovementId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Events)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class DocumentMovementStepConfiguration : IEntityTypeConfiguration<DocumentMovementStep>
{
    public void Configure(EntityTypeBuilder<DocumentMovementStep> builder)
    {
        builder.ToTable("DOCUMENT_MOVEMENT_STEPS");
        builder.Ignore(x => x.Id);

        builder.HasKey(x => x.StepId);
        builder.Property(x => x.StepId).HasColumnName("StepId").ValueGeneratedNever();
        builder.Property(x => x.MovementId).HasColumnName("MovementId").IsRequired();
        builder.Property(x => x.StepNo).HasColumnName("StepNo").IsRequired();
        builder.Property(x => x.FromDepartmentId).HasColumnName("FromDepartmentId").IsRequired();
        builder.Property(x => x.ToDepartmentId).HasColumnName("ToDepartmentId").IsRequired();
        builder.Property(x => x.AssignedToUserId).HasColumnName("AssignedToUserId");
        builder.Property(x => x.StepStatus).HasColumnName("StepStatus").HasMaxLength(30).IsRequired();
        builder.Property(x => x.Instructions).HasColumnName("Instructions").HasMaxLength(1000);
        builder.Property(x => x.OutcomeNotes).HasColumnName("OutcomeNotes").HasMaxLength(1000);
        builder.Property(x => x.SentAt).HasColumnName("SentAt").HasColumnType("datetime2(3)");
        builder.Property(x => x.ReceivedAt).HasColumnName("ReceivedAt").HasColumnType("datetime2(3)");
        builder.Property(x => x.ReceivedByUserId).HasColumnName("ReceivedByUserId");
        builder.Property(x => x.CompletedAt).HasColumnName("CompletedAt").HasColumnType("datetime2(3)");
        builder.Property(x => x.DueDate).HasColumnName("DueDate").HasColumnType("datetime2(3)");

        builder.HasIndex(x => new { x.MovementId, x.StepNo }).IsUnique().HasDatabaseName("UX_DOC_MOVEMENT_STEPS_NO");
    }
}

public sealed class DocumentMovementEventConfiguration : IEntityTypeConfiguration<DocumentMovementEvent>
{
    public void Configure(EntityTypeBuilder<DocumentMovementEvent> builder)
    {
        builder.ToTable("DOCUMENT_MOVEMENT_EVENTS");
        builder.Ignore(x => x.Id);

        builder.HasKey(x => x.EventId);
        builder.Property(x => x.EventId).HasColumnName("EventId").ValueGeneratedNever();
        builder.Property(x => x.MovementId).HasColumnName("MovementId").IsRequired();
        builder.Property(x => x.StepNo).HasColumnName("StepNo").IsRequired();
        builder.Property(x => x.EventType).HasColumnName("EventType").HasMaxLength(50).IsRequired();
        builder.Property(x => x.ActorUserId).HasColumnName("ActorUserId").IsRequired();
        builder.Property(x => x.Notes).HasColumnName("Notes").HasMaxLength(1000);
        builder.Property(x => x.OccurredAt).HasColumnName("OccurredAt").HasColumnType("datetime2(3)").IsRequired();

        builder.HasIndex(x => new { x.MovementId, x.OccurredAt }).HasDatabaseName("IX_DOC_MOVEMENT_EVENTS_MOV_DATE");
    }
}
