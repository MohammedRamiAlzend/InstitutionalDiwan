using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Entities.Organization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstitutionalDiwan.Infrastructure.Persistence.Configurations;

public sealed class DocumentNumberConfiguration : IEntityTypeConfiguration<DocumentNumber>
{
    public void Configure(EntityTypeBuilder<DocumentNumber> builder)
    {
        builder.ToTable("DOCUMENT_NUMBERS");
        builder.Ignore(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        builder.HasKey(x => x.DocumentNumberId);
        builder.Property(x => x.DocumentNumberId).HasColumnName("DocumentNumberId").ValueGeneratedNever();
        builder.Property(x => x.DocumentId).HasColumnName("DocumentId").IsRequired();
        builder.Property(x => x.RegistryId).HasColumnName("RegistryId").IsRequired();
        builder.Property(x => x.ScopeId).HasColumnName("ScopeId").HasDefaultValue(DocumentNumberScope.CentralId).IsRequired();
        builder.Property(x => x.DepartmentId).HasColumnName("DepartmentId");
        builder.Property(x => x.NumberingYear).HasColumnName("NumberingYear").IsRequired();
        builder.Property(x => x.SequenceValue).HasColumnName("SequenceValue").IsRequired();
        builder.Property(x => x.OfficialNumberText).HasColumnName("OfficialNumberText").HasMaxLength(100).IsRequired();
        builder.Property(x => x.StatusId).HasColumnName("StatusId").IsRequired();
        builder.Property(x => x.IssuedByUserId).HasColumnName("IssuedByUserId").IsRequired();
        builder.Property(x => x.IssuedAt).HasColumnName("IssuedAt").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.ReplacedByNumberId).HasColumnName("ReplacedByNumberId");
        builder.Property(x => x.CancellationReason).HasColumnName("CancellationReason").HasMaxLength(500);
        builder.Property(x => x.RegistryDate).HasColumnName("RegistryDate").HasColumnType("datetime2(3)");

        // Central numbers keep their gap-free per-registry sequence; department numbers carry no
        // sequence (the clerk types them), so only central rows take part in this index.
        builder.HasIndex(x => new { x.RegistryId, x.NumberingYear, x.SequenceValue })
            .IsUnique()
            .HasFilter("[ScopeId] = 1")
            .HasDatabaseName("UX_DOC_NUMBERS_REG_YEAR_SEQ");

        // Central numbers stay globally unique across every central registry.
        builder.HasIndex(x => new { x.OfficialNumberText, x.NumberingYear })
            .IsUnique()
            .HasFilter("[ScopeId] = 1")
            .HasDatabaseName("UX_DOC_NUMBERS_TEXT_YEAR");

        // A department may only use a number once per year, but the same text may exist in another
        // department's register — that is the whole point of per-department numbering.
        builder.HasIndex(x => new { x.RegistryId, x.OfficialNumberText, x.NumberingYear })
            .IsUnique()
            .HasFilter("[ScopeId] = 2")
            .HasDatabaseName("UX_DOC_NUMBERS_DEPT_TEXT_YEAR");

        // One register line in use per document per department per year; later arrivals in the year
        // add visits. A department that renumbers the document keeps the replaced lines (StatusId 3)
        // beside the new one, so those are left out of the index.
        builder.HasIndex(x => new { x.DocumentId, x.DepartmentId, x.NumberingYear })
            .IsUnique()
            .HasFilter("[DepartmentId] IS NOT NULL AND [StatusId] <> 3")
            .HasDatabaseName("UX_DOC_NUMBERS_DOC_DEPT");

        builder.HasIndex(x => new { x.DepartmentId, x.RegistryDate })
            .HasDatabaseName("IX_DOC_NUMBERS_DEPT_REG_DATE");

        builder.HasIndex(x => new { x.RegistryId, x.NumberingYear })
            .HasDatabaseName("IX_DOC_NUMBERS_REG_YEAR");

        builder.HasOne<DocumentNumberStatus>().WithMany().HasForeignKey(x => x.StatusId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DocumentNumberScope>().WithMany().HasForeignKey(x => x.ScopeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Department>().WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.History)
            .WithOne()
            .HasForeignKey(x => x.DocumentNumberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.History)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Visits)
            .WithOne()
            .HasForeignKey(x => x.DocumentNumberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Visits)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class DocumentRegistryVisitConfiguration : IEntityTypeConfiguration<DocumentRegistryVisit>
{
    public void Configure(EntityTypeBuilder<DocumentRegistryVisit> builder)
    {
        builder.ToTable("DOCUMENT_REGISTRY_VISITS");
        builder.Ignore(x => x.Id);

        builder.HasKey(x => x.VisitId);
        builder.Property(x => x.VisitId).HasColumnName("VisitId").ValueGeneratedNever();
        builder.Property(x => x.DocumentNumberId).HasColumnName("DocumentNumberId").IsRequired();
        builder.Property(x => x.RegistryDate).HasColumnName("RegistryDate").HasColumnType("date").IsRequired();
        builder.Property(x => x.MovementStepId).HasColumnName("MovementStepId");
        builder.Property(x => x.RecordedByUserId).HasColumnName("RecordedByUserId").IsRequired();
        builder.Property(x => x.RecordedAt).HasColumnName("RecordedAt").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.Notes).HasColumnName("Notes").HasMaxLength(500);
        builder.Property(x => x.SchemaVersionId).HasColumnName("SchemaVersionId");
        builder.Property(x => x.LockedAt).HasColumnName("LockedAt").HasColumnType("datetime2(3)");
        builder.Property(x => x.RecordUpdatedAt).HasColumnName("RecordUpdatedAt").HasColumnType("datetime2(3)");
        builder.Property(x => x.RecordUpdatedByUserId).HasColumnName("RecordUpdatedByUserId");
        builder.Property(x => x.CorrectedAt).HasColumnName("CorrectedAt").HasColumnType("datetime2(3)");
        builder.Property(x => x.CorrectedByUserId).HasColumnName("CorrectedByUserId");
        builder.Property(x => x.CorrectionReason).HasColumnName("CorrectionReason")
            .HasMaxLength(DocumentRegistryVisit.MaxCorrectionReasonLength);
        builder.Ignore(x => x.IsLocked);

        // A date written by hand appears once per register line. Arrivals tied to movement steps may
        // share a date (out and back on the same day), but one step is never recorded twice.
        builder.HasIndex(x => new { x.DocumentNumberId, x.RegistryDate })
            .IsUnique()
            .HasFilter("[MovementStepId] IS NULL")
            .HasDatabaseName("UX_DOC_REG_VISITS_NUMBER_DATE");

        builder.HasIndex(x => new { x.DocumentNumberId, x.MovementStepId })
            .IsUnique()
            .HasFilter("[MovementStepId] IS NOT NULL")
            .HasDatabaseName("UX_DOC_REG_VISITS_NUMBER_STEP");

        // The department register page narrows lines to a form version and finds each line's latest visit on it.
        builder.HasIndex(x => new { x.SchemaVersionId, x.DocumentNumberId, x.RegistryDate })
            .HasDatabaseName("IX_DOC_REG_VISITS_VERSION_NUMBER_DATE");

        builder.HasOne<DocumentTypeSchemaVersion>().WithMany().HasForeignKey(x => x.SchemaVersionId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Values)
            .WithOne()
            .HasForeignKey(x => x.VisitId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Values)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

/// <summary>Values of a department's record form, one row per field per visit.</summary>
public sealed class DepartmentRecordValueConfiguration : IEntityTypeConfiguration<DepartmentRecordValue>
{
    public void Configure(EntityTypeBuilder<DepartmentRecordValue> builder)
    {
        builder.ToTable("DEPARTMENT_RECORD_VALUES");
        builder.Ignore(x => x.Id);

        builder.HasKey(x => x.ValueId);
        builder.Property(x => x.ValueId).HasColumnName("ValueId").ValueGeneratedNever();
        builder.Property(x => x.VisitId).HasColumnName("VisitId").IsRequired();
        builder.Property(x => x.DocumentFieldId).HasColumnName("DocumentFieldId").IsRequired();
        builder.Property(x => x.TextValue).HasColumnName("TextValue").HasMaxLength(InstitutionalDiwan.Domain.ValueObjects.DocumentFieldSettings.MaxLongTextLength);
        builder.Property(x => x.NumberValue).HasColumnName("NumberValue").HasPrecision(28, 8);
        builder.Property(x => x.DateValue).HasColumnName("DateValue").HasColumnType("datetime2(3)");
        builder.Property(x => x.BoolValue).HasColumnName("BoolValue");
        builder.Property(x => x.OptionKey).HasColumnName("OptionKey").HasMaxLength(DocumentField.MaxKeyLength);

        builder.HasIndex(x => new { x.VisitId, x.DocumentFieldId })
            .IsUnique()
            .HasDatabaseName("UX_DEPT_RECORD_VALUES_VISIT_FIELD");

        // Filtering and sorting by one field start from the field, then reach the visit.
        builder.HasIndex(x => new { x.DocumentFieldId, x.VisitId })
            .HasDatabaseName("IX_DEPT_RECORD_VALUES_FIELD_VISIT");

        builder.HasOne<DocumentField>().WithMany().HasForeignKey(x => x.DocumentFieldId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class NumberingRegistryConfiguration : IEntityTypeConfiguration<NumberingRegistry>
{
    public void Configure(EntityTypeBuilder<NumberingRegistry> builder)
    {
        builder.ToTable("NUMBERING_REGISTRIES");
        builder.Ignore(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        builder.HasKey(x => x.RegistryId);
        builder.Property(x => x.RegistryId).HasColumnName("RegistryId").ValueGeneratedNever();
        builder.Property(x => x.Code).HasColumnName("Code").HasMaxLength(50).IsRequired();
        builder.Property(x => x.NameAr).HasColumnName("NameAr").HasMaxLength(200).IsRequired();
        builder.Property(x => x.DepartmentId).HasColumnName("DepartmentId");
        builder.Property(x => x.DocumentTypeId).HasColumnName("DocumentTypeId");
        builder.Property(x => x.DirectionId).HasColumnName("DirectionId");
        builder.Property(x => x.FormatPattern).HasColumnName("FormatPattern").HasMaxLength(100).IsRequired();
        builder.Property(x => x.ResetPolicyId).HasColumnName("ResetPolicyId").IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("IsActive").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("UX_NUMBERING_REGISTRIES_CODE");

        builder.HasOne<DocumentType>().WithMany().HasForeignKey(x => x.DocumentTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DocumentDirection>().WithMany().HasForeignKey(x => x.DirectionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<NumberingResetPolicy>().WithMany().HasForeignKey(x => x.ResetPolicyId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Counters)
            .WithOne()
            .HasForeignKey(x => x.RegistryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Counters)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class NumberingCounterConfiguration : IEntityTypeConfiguration<NumberingCounter>
{
    public void Configure(EntityTypeBuilder<NumberingCounter> builder)
    {
        builder.ToTable("NUMBERING_COUNTERS");
        builder.Ignore(x => x.Id);

        builder.HasKey(x => x.CounterId);
        builder.Property(x => x.CounterId).HasColumnName("CounterId").ValueGeneratedNever();
        builder.Property(x => x.RegistryId).HasColumnName("RegistryId").IsRequired();
        builder.Property(x => x.NumberingYear).HasColumnName("NumberingYear").IsRequired();
        builder.Property(x => x.LastValue).HasColumnName("LastValue").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.RowVersion).HasColumnName("RowVersion").IsRowVersion();

        builder.HasIndex(x => new { x.RegistryId, x.NumberingYear })
            .IsUnique()
            .HasDatabaseName("UX_NUMBERING_COUNTERS_REG_YEAR");
    }
}

public sealed class DocumentNumberHistoryConfiguration : IEntityTypeConfiguration<DocumentNumberHistory>
{
    public void Configure(EntityTypeBuilder<DocumentNumberHistory> builder)
    {
        builder.ToTable("DOCUMENT_NUMBER_HISTORY");
        builder.Ignore(x => x.Id);

        builder.HasKey(x => x.HistoryId);
        builder.Property(x => x.HistoryId).HasColumnName("HistoryId").ValueGeneratedNever();
        builder.Property(x => x.DocumentNumberId).HasColumnName("DocumentNumberId").IsRequired();
        builder.Property(x => x.EventTypeId).HasColumnName("EventTypeId").IsRequired();
        builder.Property(x => x.ActorUserId).HasColumnName("ActorUserId").IsRequired();
        builder.Property(x => x.Notes).HasColumnName("Notes").HasMaxLength(500);
        builder.Property(x => x.OccurredAt).HasColumnName("OccurredAt").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.EffectiveDate).HasColumnName("EffectiveDate").HasColumnType("datetime2(3)");

        builder.HasOne<DocumentNumberEventType>().WithMany().HasForeignKey(x => x.EventTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DocumentNumberStatusConfiguration : IEntityTypeConfiguration<DocumentNumberStatus>
{
    public void Configure(EntityTypeBuilder<DocumentNumberStatus> builder)
    {
        builder.ToTable("DOCUMENT_NUMBER_STATUSES");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(x => x.Desc).HasColumnName("Desc").HasMaxLength(150).IsRequired();
        builder.Property(x => x.DescAr).HasColumnName("DescAr").HasMaxLength(150).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("IsActive").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
    }
}

public sealed class DocumentNumberEventTypeConfiguration : IEntityTypeConfiguration<DocumentNumberEventType>
{
    public void Configure(EntityTypeBuilder<DocumentNumberEventType> builder)
    {
        builder.ToTable("DOCUMENT_NUMBER_EVENT_TYPES");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(x => x.Desc).HasColumnName("Desc").HasMaxLength(150).IsRequired();
        builder.Property(x => x.DescAr).HasColumnName("DescAr").HasMaxLength(150).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("IsActive").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
    }
}

public sealed class DocumentNumberScopeConfiguration : IEntityTypeConfiguration<DocumentNumberScope>
{
    public void Configure(EntityTypeBuilder<DocumentNumberScope> builder)
    {
        builder.ToTable("DOCUMENT_NUMBER_SCOPES");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(x => x.Desc).HasColumnName("Desc").HasMaxLength(150).IsRequired();
        builder.Property(x => x.DescAr).HasColumnName("DescAr").HasMaxLength(150).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("IsActive").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
    }
}

public sealed class NumberingResetPolicyConfiguration : IEntityTypeConfiguration<NumberingResetPolicy>
{
    public void Configure(EntityTypeBuilder<NumberingResetPolicy> builder)
    {
        builder.ToTable("NUMBERING_RESET_POLICIES");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(x => x.Desc).HasColumnName("Desc").HasMaxLength(150).IsRequired();
        builder.Property(x => x.DescAr).HasColumnName("DescAr").HasMaxLength(150).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("IsActive").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
    }
}
