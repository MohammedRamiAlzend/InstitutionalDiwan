using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InstitutionalDiwan.Domain.Entities.Numbering;

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
        builder.Property(x => x.NumberingYear).HasColumnName("NumberingYear").IsRequired();
        builder.Property(x => x.SequenceValue).HasColumnName("SequenceValue").IsRequired();
        builder.Property(x => x.OfficialNumberText).HasColumnName("OfficialNumberText").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Status).HasColumnName("Status").HasMaxLength(30).IsRequired();
        builder.Property(x => x.IssuedByUserId).HasColumnName("IssuedByUserId").IsRequired();
        builder.Property(x => x.IssuedAt).HasColumnName("IssuedAt").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.ReplacedByNumberId).HasColumnName("ReplacedByNumberId");
        builder.Property(x => x.CancellationReason).HasColumnName("CancellationReason").HasMaxLength(500);

        builder.HasIndex(x => new { x.RegistryId, x.NumberingYear, x.SequenceValue })
            .IsUnique()
            .HasDatabaseName("UX_DOC_NUMBERS_REG_YEAR_SEQ");

        builder.HasIndex(x => new { x.OfficialNumberText, x.NumberingYear })
            .IsUnique()
            .HasDatabaseName("UX_DOC_NUMBERS_TEXT_YEAR");

        builder.HasMany(x => x.History)
            .WithOne()
            .HasForeignKey(x => x.DocumentNumberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.History)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
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
        builder.Property(x => x.DocumentTypeCode).HasColumnName("DocumentTypeCode").HasMaxLength(50);
        builder.Property(x => x.Direction).HasColumnName("Direction").HasMaxLength(30);
        builder.Property(x => x.FormatPattern).HasColumnName("FormatPattern").HasMaxLength(100).IsRequired();
        builder.Property(x => x.ResetPolicy).HasColumnName("ResetPolicy").HasMaxLength(30).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("IsActive").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("UX_NUMBERING_REGISTRIES_CODE");

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
        builder.Property(x => x.EventType).HasColumnName("EventType").HasMaxLength(30).IsRequired();
        builder.Property(x => x.ActorUserId).HasColumnName("ActorUserId").IsRequired();
        builder.Property(x => x.Notes).HasColumnName("Notes").HasMaxLength(500);
        builder.Property(x => x.OccurredAt).HasColumnName("OccurredAt").HasColumnType("datetime2(3)").IsRequired();
    }
}
