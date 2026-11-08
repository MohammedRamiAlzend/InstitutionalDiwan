using System.Text.Json;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstitutionalDiwan.Infrastructure.Persistence.Configurations;

public sealed class DocumentFieldConfiguration : IEntityTypeConfiguration<DocumentField>
{
    public void Configure(EntityTypeBuilder<DocumentField> builder)
    {
        builder.ToTable("DOCUMENT_FIELDS");
        builder.Ignore(x => x.Id);

        builder.HasKey(x => x.DocumentFieldId);
        builder.Property(x => x.DocumentFieldId).HasColumnName("DocumentFieldId").ValueGeneratedNever();
        builder.Property(x => x.DocumentTypeId).HasColumnName("DocumentTypeId").IsRequired();
        builder.Property(x => x.Key).HasColumnName("Key").HasMaxLength(DocumentField.MaxKeyLength).IsRequired();
        builder.Property(x => x.DataType).HasColumnName("DataType").IsRequired();
        builder.Property(x => x.CreatedByUserId).HasColumnName("CreatedByUserId").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();

        builder.HasIndex(x => new { x.DocumentTypeId, x.Key }).IsUnique().HasDatabaseName("UX_DOCUMENT_FIELDS_TYPE_KEY");

        builder.HasOne<DocumentType>().WithMany().HasForeignKey(x => x.DocumentTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DocumentTypeSchemaVersionConfiguration : IEntityTypeConfiguration<DocumentTypeSchemaVersion>
{
    public void Configure(EntityTypeBuilder<DocumentTypeSchemaVersion> builder)
    {
        builder.ToTable("DOCUMENT_TYPE_SCHEMA_VERSIONS");
        builder.Ignore(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        builder.HasKey(x => x.SchemaVersionId);
        builder.Property(x => x.SchemaVersionId).HasColumnName("SchemaVersionId").ValueGeneratedNever();
        builder.Property(x => x.DocumentTypeId).HasColumnName("DocumentTypeId").IsRequired();
        builder.Property(x => x.VersionNumber).HasColumnName("VersionNumber").IsRequired();
        builder.Property(x => x.Status).HasColumnName("Status").IsRequired();
        builder.Property(x => x.CreatedByUserId).HasColumnName("CreatedByUserId").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt").HasColumnType("datetime2(3)");
        builder.Property(x => x.PublishedAt).HasColumnName("PublishedAt").HasColumnType("datetime2(3)");
        builder.Property(x => x.PublishedByUserId).HasColumnName("PublishedByUserId");
        builder.Property(x => x.RowVersion).HasColumnName("RowVersion").IsRowVersion();

        builder.HasIndex(x => new { x.DocumentTypeId, x.VersionNumber })
            .IsUnique()
            .HasDatabaseName("UX_DOC_SCHEMA_VERSIONS_TYPE_NUMBER");

        // At most one editable draft per document type (Status 1 = Draft).
        builder.HasIndex(x => x.DocumentTypeId)
            .IsUnique()
            .HasFilter("[Status] = 1")
            .HasDatabaseName("UX_DOC_SCHEMA_VERSIONS_ONE_DRAFT");

        builder.HasIndex(x => new { x.DocumentTypeId, x.Status }).HasDatabaseName("IX_DOC_SCHEMA_VERSIONS_TYPE_STATUS");

        builder.HasOne<DocumentType>().WithMany().HasForeignKey(x => x.DocumentTypeId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Fields)
            .WithOne()
            .HasForeignKey(x => x.SchemaVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Fields)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class DocumentTypeSchemaVersionFieldConfiguration : IEntityTypeConfiguration<DocumentTypeSchemaVersionField>
{
    private static readonly JsonSerializerOptions OptionsJson = new();

    public void Configure(EntityTypeBuilder<DocumentTypeSchemaVersionField> builder)
    {
        builder.ToTable("DOCUMENT_TYPE_SCHEMA_VERSION_FIELDS");
        builder.Ignore(x => x.Id);

        builder.HasKey(x => x.SchemaVersionFieldId);
        builder.Property(x => x.SchemaVersionFieldId).HasColumnName("SchemaVersionFieldId").ValueGeneratedNever();
        builder.Property(x => x.SchemaVersionId).HasColumnName("SchemaVersionId").IsRequired();
        builder.Property(x => x.DocumentFieldId).HasColumnName("DocumentFieldId").IsRequired();
        builder.Property(x => x.FieldKey).HasColumnName("FieldKey").HasMaxLength(DocumentField.MaxKeyLength).IsRequired();
        builder.Property(x => x.DataType).HasColumnName("DataType").IsRequired();
        builder.Property(x => x.LabelAr).HasColumnName("LabelAr").HasMaxLength(DocumentFieldSettings.MaxLabelLength).IsRequired();
        builder.Property(x => x.IsRequired).HasColumnName("IsRequired").IsRequired();
        builder.Property(x => x.DisplayOrder).HasColumnName("DisplayOrder").IsRequired();
        builder.Property(x => x.RegexPattern).HasColumnName("RegexPattern").HasMaxLength(DocumentFieldSettings.MaxRegexLength);
        builder.Property(x => x.RegexErrorMessageAr).HasColumnName("RegexErrorMessageAr").HasMaxLength(DocumentFieldSettings.MaxRegexMessageLength);
        builder.Property(x => x.MinNumber).HasColumnName("MinNumber").HasPrecision(28, 8);
        builder.Property(x => x.MaxNumber).HasColumnName("MaxNumber").HasPrecision(28, 8);
        builder.Property(x => x.MaxLength).HasColumnName("MaxLength");

        // Select options are a small, always-read-together list: one JSON column instead of a child table.
        builder.Property(x => x.Options)
            .HasColumnName("Options")
            .IsRequired()
            .HasConversion(
                options => JsonSerializer.Serialize(options, OptionsJson),
                json => (IReadOnlyList<DocumentFieldOption>)
                    (JsonSerializer.Deserialize<List<DocumentFieldOption>>(json, OptionsJson) ?? new List<DocumentFieldOption>()),
                new ValueComparer<IReadOnlyList<DocumentFieldOption>>(
                    (left, right) => left!.SequenceEqual(right!),
                    options => options.Aggregate(0, (hash, option) => HashCode.Combine(hash, option)),
                    options => options.ToList()));

        // Existing rows are migrated as filterable: every field was offered as a filter before this option.
        builder.Property(x => x.Behavior)
            .HasColumnName("Behavior")
            .IsRequired()
            .HasDefaultValue(new DocumentFieldBehavior(IsFilterable: true))
            .HasConversion(
                behavior => JsonSerializer.Serialize(behavior, OptionsJson),
                json => JsonSerializer.Deserialize<DocumentFieldBehavior>(json, OptionsJson) ?? new DocumentFieldBehavior(),
                new ValueComparer<DocumentFieldBehavior>(
                    (left, right) => Equals(left, right),
                    behavior => behavior.GetHashCode(),
                    behavior => behavior));

        builder.HasIndex(x => new { x.SchemaVersionId, x.DocumentFieldId })
            .IsUnique()
            .HasDatabaseName("UX_DOC_SCHEMA_VERSION_FIELDS_VERSION_FIELD");

        builder.HasOne<DocumentField>().WithMany().HasForeignKey(x => x.DocumentFieldId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DocumentFieldValueConfiguration : IEntityTypeConfiguration<DocumentFieldValue>
{
    public void Configure(EntityTypeBuilder<DocumentFieldValue> builder)
    {
        builder.ToTable("DOCUMENT_FIELD_VALUES");
        builder.Ignore(x => x.Id);

        builder.HasKey(x => x.DocumentFieldValueId);
        builder.Property(x => x.DocumentFieldValueId).HasColumnName("DocumentFieldValueId").ValueGeneratedNever();
        builder.Property(x => x.DocumentId).HasColumnName("DocumentId").IsRequired();
        builder.Property(x => x.DocumentFieldId).HasColumnName("DocumentFieldId").IsRequired();
        builder.Property(x => x.TextValue).HasColumnName("TextValue").HasMaxLength(DocumentFieldSettings.MaxLongTextLength);
        builder.Property(x => x.NumberValue).HasColumnName("NumberValue").HasPrecision(28, 8);
        builder.Property(x => x.DateValue).HasColumnName("DateValue").HasColumnType("datetime2(3)");
        builder.Property(x => x.BoolValue).HasColumnName("BoolValue");
        builder.Property(x => x.OptionKey).HasColumnName("OptionKey").HasMaxLength(DocumentField.MaxKeyLength);

        // One value per field per document.
        builder.HasIndex(x => new { x.DocumentId, x.DocumentFieldId })
            .IsUnique()
            .HasDatabaseName("UX_DOC_FIELD_VALUES_DOC_FIELD");

        // Registry filters compare one typed column at a time, so each typed column gets its own sparse index.
        builder.HasIndex(x => new { x.DocumentFieldId, x.NumberValue })
            .HasFilter("[NumberValue] IS NOT NULL")
            .HasDatabaseName("IX_DOC_FIELD_VALUES_NUMBER");
        builder.HasIndex(x => new { x.DocumentFieldId, x.DateValue })
            .HasFilter("[DateValue] IS NOT NULL")
            .HasDatabaseName("IX_DOC_FIELD_VALUES_DATE");
        builder.HasIndex(x => new { x.DocumentFieldId, x.OptionKey })
            .HasFilter("[OptionKey] IS NOT NULL")
            .HasDatabaseName("IX_DOC_FIELD_VALUES_OPTION");
        builder.HasIndex(x => new { x.DocumentFieldId, x.BoolValue })
            .HasFilter("[BoolValue] IS NOT NULL")
            .HasDatabaseName("IX_DOC_FIELD_VALUES_BOOL");

        builder.HasOne<DocumentField>().WithMany().HasForeignKey(x => x.DocumentFieldId).OnDelete(DeleteBehavior.Restrict);
    }
}
