using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InstitutionalDiwan.Domain.Entities.Attachments;

namespace InstitutionalDiwan.Infrastructure.Persistence.Configurations;

public sealed class DocumentFileConfiguration : IEntityTypeConfiguration<DocumentFile>
{
    public void Configure(EntityTypeBuilder<DocumentFile> builder)
    {
        builder.ToTable("DOCUMENT_FILES");
        builder.Ignore(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        builder.HasKey(x => x.FileId);
        builder.Property(x => x.FileId).HasColumnName("FileId").ValueGeneratedNever();
        builder.Property(x => x.DocumentId).HasColumnName("DocumentId").IsRequired();
        builder.Property(x => x.FileTypeCode).HasColumnName("FileTypeCode").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Title).HasColumnName("Title").HasMaxLength(300).IsRequired();
        builder.Property(x => x.IsPrimaryScan).HasColumnName("IsPrimaryScan").IsRequired();
        builder.Property(x => x.CurrentVersionNumber).HasColumnName("CurrentVersionNumber").IsRequired();
        builder.Property(x => x.IsArchived).HasColumnName("IsArchived").IsRequired();
        builder.Property(x => x.CreatedByUserId).HasColumnName("CreatedByUserId").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt").HasColumnType("datetime2(3)");

        builder.HasIndex(x => x.DocumentId).HasDatabaseName("IX_DOC_FILES_DOC");

        builder.HasMany(x => x.Versions)
            .WithOne()
            .HasForeignKey(x => x.FileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Versions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class DocumentFileVersionConfiguration : IEntityTypeConfiguration<DocumentFileVersion>
{
    public void Configure(EntityTypeBuilder<DocumentFileVersion> builder)
    {
        builder.ToTable("DOCUMENT_FILE_VERSIONS");
        builder.Ignore(x => x.Id);

        builder.HasKey(x => x.VersionId);
        builder.Property(x => x.VersionId).HasColumnName("VersionId").ValueGeneratedNever();
        builder.Property(x => x.FileId).HasColumnName("FileId").IsRequired();
        builder.Property(x => x.VersionNumber).HasColumnName("VersionNumber").IsRequired();
        builder.Property(x => x.StorageProviderCode).HasColumnName("StorageProviderCode").HasMaxLength(50).IsRequired();
        builder.Property(x => x.RelativePath).HasColumnName("RelativePath").HasMaxLength(1000).IsRequired();
        builder.Property(x => x.OriginalFileName).HasColumnName("OriginalFileName").HasMaxLength(255).IsRequired();
        builder.Property(x => x.MimeType).HasColumnName("MimeType").HasMaxLength(150).IsRequired();
        builder.Property(x => x.FileSizeBytes).HasColumnName("FileSizeBytes").IsRequired();
        builder.Property(x => x.ChecksumSha256).HasColumnName("ChecksumSha256").HasColumnType("char(64)").IsRequired();
        builder.Property(x => x.IsCurrent).HasColumnName("IsCurrent").IsRequired();
        builder.Property(x => x.UploadedByUserId).HasColumnName("UploadedByUserId").IsRequired();
        builder.Property(x => x.ChangeNotes).HasColumnName("ChangeNotes").HasMaxLength(500);
        builder.Property(x => x.UploadedAt).HasColumnName("UploadedAt").HasColumnType("datetime2(3)").IsRequired();

        builder.HasIndex(x => new { x.FileId, x.VersionNumber }).IsUnique().HasDatabaseName("UX_DOC_FILE_VERSIONS_NO");
    }
}

public sealed class DocumentFileTypeConfiguration : IEntityTypeConfiguration<DocumentFileType>
{
    public void Configure(EntityTypeBuilder<DocumentFileType> builder)
    {
        builder.ToTable("DOCUMENT_FILE_TYPES");
        builder.Ignore(x => x.Id);

        builder.HasKey(x => x.Code);
        builder.Property(x => x.Code).HasColumnName("Code").HasMaxLength(50).ValueGeneratedNever();
        builder.Property(x => x.NameAr).HasColumnName("NameAr").HasMaxLength(150).IsRequired();
        builder.Property(x => x.NameEn).HasColumnName("NameEn").HasMaxLength(150);
        builder.Property(x => x.AllowedExtensionsCsv).HasColumnName("AllowedExtensionsCsv").HasMaxLength(300).IsRequired();
        builder.Property(x => x.MaxFileSizeBytes).HasColumnName("MaxFileSizeBytes").IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("IsActive").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
    }
}

public sealed class FileStorageProviderConfiguration : IEntityTypeConfiguration<FileStorageProvider>
{
    public void Configure(EntityTypeBuilder<FileStorageProvider> builder)
    {
        builder.ToTable("FILE_STORAGE_PROVIDERS");
        builder.Ignore(x => x.Id);

        builder.HasKey(x => x.Code);
        builder.Property(x => x.Code).HasColumnName("Code").HasMaxLength(50).ValueGeneratedNever();
        builder.Property(x => x.NameAr).HasColumnName("NameAr").HasMaxLength(150).IsRequired();
        builder.Property(x => x.ProviderType).HasColumnName("ProviderType").HasMaxLength(50).IsRequired();
        builder.Property(x => x.BasePathOrEndpoint).HasColumnName("BasePathOrEndpoint").HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsDefault).HasColumnName("IsDefault").IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("IsActive").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
    }
}
