using InstitutionalDiwan.Domain.Entities.Attachments;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Decisions;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Entities.Organization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstitutionalDiwan.Infrastructure.Persistence.Configurations;

public sealed class DecisionTypeConfiguration : IEntityTypeConfiguration<DecisionType>
{
    public void Configure(EntityTypeBuilder<DecisionType> builder)
    {
        builder.ToTable("DECISION_TYPES");
        builder.Ignore(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        builder.HasKey(x => x.DecisionTypeId);
        builder.Property(x => x.DecisionTypeId).HasColumnName("DecisionTypeId").ValueGeneratedNever();
        builder.Property(x => x.NameAr).HasColumnName("NameAr").HasMaxLength(DecisionType.MaxNameLength).IsRequired();
        builder.Property(x => x.IsAppealable).HasColumnName("IsAppealable").IsRequired();
        builder.Property(x => x.EffectId).HasColumnName("EffectId").IsRequired();
        builder.Property(x => x.DestinationDepartmentId).HasColumnName("DestinationDepartmentId");
        builder.Property(x => x.IsActive).HasColumnName("IsActive").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt").HasColumnType("datetime2(3)");

        builder.HasOne<Department>().WithMany().HasForeignKey(x => x.DestinationDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.DocumentTypes)
            .WithOne()
            .HasForeignKey(x => x.DecisionTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.DocumentTypes).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class DecisionTypeDocumentTypeConfiguration : IEntityTypeConfiguration<DecisionTypeDocumentType>
{
    public void Configure(EntityTypeBuilder<DecisionTypeDocumentType> builder)
    {
        builder.ToTable("DECISION_TYPE_DOCUMENT_TYPES");
        builder.HasKey(x => new { x.DecisionTypeId, x.DocumentTypeId });
        builder.Property(x => x.DecisionTypeId).HasColumnName("DecisionTypeId");
        builder.Property(x => x.DocumentTypeId).HasColumnName("DocumentTypeId");

        builder.HasOne<DocumentType>().WithMany().HasForeignKey(x => x.DocumentTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DocumentDecisionConfiguration : IEntityTypeConfiguration<DocumentDecision>
{
    public void Configure(EntityTypeBuilder<DocumentDecision> builder)
    {
        builder.ToTable("DOCUMENT_DECISIONS");
        builder.Ignore(x => x.Id);
        builder.Ignore(x => x.DomainEvents);
        builder.Ignore(x => x.IsInForce);

        builder.HasKey(x => x.DecisionId);
        builder.Property(x => x.DecisionId).HasColumnName("DecisionId").ValueGeneratedNever();
        builder.Property(x => x.DocumentId).HasColumnName("DocumentId").IsRequired();
        builder.Property(x => x.DecisionTypeId).HasColumnName("DecisionTypeId").IsRequired();
        builder.Property(x => x.DecisionNumber).HasColumnName("DecisionNumber").HasMaxLength(DocumentDecision.MaxNumberLength).IsRequired();
        builder.Property(x => x.DecisionDate).HasColumnName("DecisionDate").HasColumnType("date").IsRequired();
        builder.Property(x => x.DecisionYear).HasColumnName("DecisionYear").IsRequired();
        builder.Property(x => x.IssuingDepartmentId).HasColumnName("IssuingDepartmentId").IsRequired();
        builder.Property(x => x.RegistryVisitId).HasColumnName("RegistryVisitId").IsRequired();
        builder.Property(x => x.Summary).HasColumnName("Summary").HasMaxLength(DocumentDecision.MaxSummaryLength);
        builder.Property(x => x.StatusId).HasColumnName("StatusId").IsRequired();
        builder.Property(x => x.IsAppealable).HasColumnName("IsAppealable").IsRequired();
        builder.Property(x => x.EffectId).HasColumnName("EffectId").IsRequired();
        builder.Property(x => x.DestinationDepartmentId).HasColumnName("DestinationDepartmentId");
        builder.Property(x => x.DocumentFileId).HasColumnName("DocumentFileId");
        builder.Property(x => x.RecordedByUserId).HasColumnName("RecordedByUserId").IsRequired();
        builder.Property(x => x.RecordedAt).HasColumnName("RecordedAt").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.StatusReason).HasColumnName("StatusReason").HasMaxLength(DocumentDecision.MaxReasonLength);
        builder.Property(x => x.StatusChangedAt).HasColumnName("StatusChangedAt").HasColumnType("datetime2(3)");

        builder.HasIndex(x => new { x.IssuingDepartmentId, x.DecisionYear, x.DecisionNumber })
            .IsUnique()
            .HasDatabaseName("UX_DOCUMENT_DECISIONS_DEPT_YEAR_NUMBER");

        // One decision in force per document. Statuses 1 issued, 2 appealed, 3 final.
        // Bracketed identifiers match the other partial indexes, which SQLite tests already apply.
        builder.HasIndex(x => x.DocumentId)
            .IsUnique()
            .HasFilter("[StatusId] IN (1, 2, 3)")
            .HasDatabaseName("UX_DOCUMENT_DECISIONS_ONE_ACTIVE");

        builder.HasOne<Document>().WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DecisionType>().WithMany().HasForeignKey(x => x.DecisionTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Department>().WithMany().HasForeignKey(x => x.IssuingDepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Department>().WithMany().HasForeignKey(x => x.DestinationDepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DocumentRegistryVisit>().WithMany().HasForeignKey(x => x.RegistryVisitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DocumentFile>().WithMany().HasForeignKey(x => x.DocumentFileId).OnDelete(DeleteBehavior.Restrict);
    }
}
