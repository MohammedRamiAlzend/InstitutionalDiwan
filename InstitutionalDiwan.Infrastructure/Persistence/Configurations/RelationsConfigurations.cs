using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Relations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstitutionalDiwan.Infrastructure.Persistence.Configurations;

public sealed class DocumentRelationConfiguration : IEntityTypeConfiguration<DocumentRelation>
{
    public void Configure(EntityTypeBuilder<DocumentRelation> builder)
    {
        builder.ToTable(
            "DOCUMENT_RELATIONS",
            table => table.HasCheckConstraint("CK_DOC_RELATIONS_NOT_SELF", "[SourceDocumentId] <> [TargetDocumentId]"));
        builder.Ignore(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        builder.HasKey(x => x.RelationId);
        builder.Property(x => x.RelationId).HasColumnName("RelationId").ValueGeneratedNever();
        builder.Property(x => x.SourceDocumentId).HasColumnName("SourceDocumentId").IsRequired();
        builder.Property(x => x.TargetDocumentId).HasColumnName("TargetDocumentId").IsRequired();
        builder.Property(x => x.RelationTypeId).HasColumnName("RelationTypeId").IsRequired();
        builder.Property(x => x.Notes).HasColumnName("Notes").HasMaxLength(500);
        builder.Property(x => x.CreatedByUserId).HasColumnName("CreatedByUserId").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();

        builder.HasIndex(x => new { x.SourceDocumentId, x.TargetDocumentId, x.RelationTypeId })
            .IsUnique()
            .HasDatabaseName("UX_DOC_RELATIONS_SRC_TGT_TYPE");
        builder.HasIndex(x => x.TargetDocumentId).HasDatabaseName("IX_DOC_RELATIONS_TARGET");

        builder.HasOne<DocumentRelationType>()
            .WithMany()
            .HasForeignKey(x => x.RelationTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Document>()
            .WithMany()
            .HasForeignKey(x => x.SourceDocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Document>()
            .WithMany()
            .HasForeignKey(x => x.TargetDocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DocumentRelationTypeConfiguration : IEntityTypeConfiguration<DocumentRelationType>
{
    public void Configure(EntityTypeBuilder<DocumentRelationType> builder)
    {
        builder.ToTable("DOCUMENT_RELATION_TYPES");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(x => x.Desc).HasColumnName("Desc").HasMaxLength(150).IsRequired();
        builder.Property(x => x.DescAr).HasColumnName("DescAr").HasMaxLength(150).IsRequired();
        builder.Property(x => x.IsSymmetric).HasColumnName("IsSymmetric").IsRequired();
        builder.Property(x => x.PreventCycles).HasColumnName("PreventCycles").IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("IsActive").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
    }
}

public sealed class DocumentRelationEventConfiguration : IEntityTypeConfiguration<DocumentRelationEvent>
{
    public void Configure(EntityTypeBuilder<DocumentRelationEvent> builder)
    {
        builder.ToTable("DOCUMENT_RELATION_EVENTS");
        builder.Ignore(x => x.Id);

        builder.HasKey(x => x.RelationEventId);
        builder.Property(x => x.RelationEventId).HasColumnName("RelationEventId").ValueGeneratedNever();
        builder.Property(x => x.RelationId).HasColumnName("RelationId").IsRequired();
        builder.Property(x => x.SourceDocumentId).HasColumnName("SourceDocumentId").IsRequired();
        builder.Property(x => x.TargetDocumentId).HasColumnName("TargetDocumentId").IsRequired();
        builder.Property(x => x.RelationTypeId).HasColumnName("RelationTypeId").IsRequired();
        builder.Property(x => x.ActionTypeId).HasColumnName("ActionTypeId").IsRequired();
        builder.Property(x => x.ActorUserId).HasColumnName("ActorUserId").IsRequired();
        builder.Property(x => x.Reason).HasColumnName("Reason").HasMaxLength(500);
        builder.Property(x => x.OccurredAt).HasColumnName("OccurredAt").HasColumnType("datetime2(3)").IsRequired();

        // The history outlives a removed relation, so RelationId deliberately has no foreign key.
        builder.HasIndex(x => new { x.SourceDocumentId, x.OccurredAt }).HasDatabaseName("IX_DOC_REL_EVENTS_SOURCE_DATE");
        builder.HasIndex(x => new { x.TargetDocumentId, x.OccurredAt }).HasDatabaseName("IX_DOC_REL_EVENTS_TARGET_DATE");

        builder.HasOne<Document>()
            .WithMany()
            .HasForeignKey(x => x.SourceDocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Document>()
            .WithMany()
            .HasForeignKey(x => x.TargetDocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<DocumentRelationType>()
            .WithMany()
            .HasForeignKey(x => x.RelationTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<RelationActionType>()
            .WithMany()
            .HasForeignKey(x => x.ActionTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class RelationActionTypeConfiguration : IEntityTypeConfiguration<RelationActionType>
{
    public void Configure(EntityTypeBuilder<RelationActionType> builder)
    {
        builder.ToTable("RELATION_ACTION_TYPES");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(x => x.Desc).HasColumnName("Desc").HasMaxLength(150).IsRequired();
        builder.Property(x => x.DescAr).HasColumnName("DescAr").HasMaxLength(150).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("IsActive").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
    }
}
