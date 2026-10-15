using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InstitutionalDiwan.Domain.Entities.Relations;

namespace InstitutionalDiwan.Infrastructure.Persistence.Configurations;

public sealed class DocumentRelationConfiguration : IEntityTypeConfiguration<DocumentRelation>
{
    public void Configure(EntityTypeBuilder<DocumentRelation> builder)
    {
        builder.ToTable("DOCUMENT_RELATIONS");
        builder.Ignore(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        builder.HasKey(x => x.RelationId);
        builder.Property(x => x.RelationId).HasColumnName("RelationId").ValueGeneratedNever();
        builder.Property(x => x.SourceDocumentId).HasColumnName("SourceDocumentId").IsRequired();
        builder.Property(x => x.TargetDocumentId).HasColumnName("TargetDocumentId").IsRequired();
        builder.Property(x => x.RelationTypeCode).HasColumnName("RelationTypeCode").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Notes).HasColumnName("Notes").HasMaxLength(500);
        builder.Property(x => x.CreatedByUserId).HasColumnName("CreatedByUserId").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();

        builder.HasIndex(x => new { x.SourceDocumentId, x.TargetDocumentId, x.RelationTypeCode })
            .IsUnique()
            .HasDatabaseName("UX_DOC_RELATIONS_SRC_TGT_TYPE");
    }
}

public sealed class DocumentRelationTypeConfiguration : IEntityTypeConfiguration<DocumentRelationType>
{
    public void Configure(EntityTypeBuilder<DocumentRelationType> builder)
    {
        builder.ToTable("DOCUMENT_RELATION_TYPES");
        builder.Ignore(x => x.Id);

        builder.HasKey(x => x.Code);
        builder.Property(x => x.Code).HasColumnName("Code").HasMaxLength(50).ValueGeneratedNever();
        builder.Property(x => x.NameAr).HasColumnName("NameAr").HasMaxLength(150).IsRequired();
        builder.Property(x => x.NameEn).HasColumnName("NameEn").HasMaxLength(150);
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
        builder.Property(x => x.RelationTypeCode).HasColumnName("RelationTypeCode").HasMaxLength(50).IsRequired();
        builder.Property(x => x.ActionType).HasColumnName("ActionType").HasMaxLength(30).IsRequired();
        builder.Property(x => x.ActorUserId).HasColumnName("ActorUserId").IsRequired();
        builder.Property(x => x.Reason).HasColumnName("Reason").HasMaxLength(500);
        builder.Property(x => x.OccurredAt).HasColumnName("OccurredAt").HasColumnType("datetime2(3)").IsRequired();
    }
}
