using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Numbering;
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
        builder.Property(x => x.NumberingYear).HasColumnName("NumberingYear").IsRequired();
        builder.Property(x => x.SequenceValue).HasColumnName("SequenceValue").IsRequired();
        builder.Property(x => x.OfficialNumberText).HasColumnName("OfficialNumberText").HasMaxLength(100).IsRequired();
        builder.Property(x => x.StatusId).HasColumnName("StatusId").IsRequired();
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

        builder.HasOne<DocumentNumberStatus>().WithMany().HasForeignKey(x => x.StatusId).OnDelete(DeleteBehavior.Restrict);

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
