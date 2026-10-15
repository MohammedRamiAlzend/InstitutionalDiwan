using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InstitutionalDiwan.Domain.Entities.Correspondence;

namespace InstitutionalDiwan.Infrastructure.Persistence.Configurations;

public sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("DOCUMENTS");
        builder.Ignore(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        builder.HasKey(x => x.DocumentId);
        builder.Property(x => x.DocumentId).HasColumnName("DocumentId").ValueGeneratedNever();
        builder.Property(x => x.Title).HasColumnName("Title").HasMaxLength(500).IsRequired();
        builder.Property(x => x.SubjectSummary).HasColumnName("SubjectSummary").HasMaxLength(2000);
        builder.Property(x => x.DocumentTypeCode).HasColumnName("DocumentTypeCode").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Direction).HasColumnName("Direction").HasMaxLength(30).IsRequired();
        builder.Property(x => x.ConfidentialityLevel).HasColumnName("ConfidentialityLevel").HasMaxLength(30).IsRequired();
        builder.Property(x => x.PriorityLevel).HasColumnName("PriorityLevel").HasMaxLength(30).IsRequired();
        builder.Property(x => x.CurrentStatusCode).HasColumnName("CurrentStatusCode").HasMaxLength(50).IsRequired();
        builder.Property(x => x.StatusBeforeArchiveCode).HasColumnName("StatusBeforeArchiveCode").HasMaxLength(50);
        builder.Property(x => x.OwningDepartmentId).HasColumnName("OwningDepartmentId").IsRequired();
        builder.Property(x => x.CreatedByUserId).HasColumnName("CreatedByUserId").IsRequired();
        builder.Property(x => x.ActiveOfficialNumberId).HasColumnName("ActiveOfficialNumberId");
        builder.Property(x => x.OfficialNumberText).HasColumnName("OfficialNumberText").HasMaxLength(100);
        builder.Property(x => x.DocumentDate).HasColumnName("DocumentDate").HasColumnType("datetime2(3)");
        builder.Property(x => x.RegistrationDate).HasColumnName("RegistrationDate").HasColumnType("datetime2(3)");
        builder.Property(x => x.ExternalReferenceNumber).HasColumnName("ExternalReferenceNumber").HasMaxLength(100);
        builder.Property(x => x.ExternalDocumentDate).HasColumnName("ExternalDocumentDate").HasColumnType("datetime2(3)");
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt").HasColumnType("datetime2(3)");
        builder.Property(x => x.RowVersion).HasColumnName("RowVersion").IsRowVersion();

        builder.HasIndex(x => new { x.OwningDepartmentId, x.CurrentStatusCode }).HasDatabaseName("IX_DOCUMENTS_DEPT_STATUS");
        builder.HasIndex(x => x.OfficialNumberText).HasDatabaseName("IX_DOCUMENTS_OFFICIAL_NUMBER");
        builder.HasIndex(x => x.RegistrationDate).HasDatabaseName("IX_DOCUMENTS_REG_DATE");

        builder.HasMany(x => x.Participants)
            .WithOne()
            .HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Participants)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.StatusHistory)
            .WithOne()
            .HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.StatusHistory)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class DocumentTypeConfiguration : IEntityTypeConfiguration<DocumentType>
{
    public void Configure(EntityTypeBuilder<DocumentType> builder)
    {
        builder.ToTable("DOCUMENT_TYPES");
        builder.Ignore(x => x.Id);

        builder.HasKey(x => x.Code);
        builder.Property(x => x.Code).HasColumnName("Code").HasMaxLength(50).ValueGeneratedNever();
        builder.Property(x => x.NameAr).HasColumnName("NameAr").HasMaxLength(150).IsRequired();
        builder.Property(x => x.NameEn).HasColumnName("NameEn").HasMaxLength(150);
        builder.Property(x => x.AllowedDirectionScope).HasColumnName("AllowedDirectionScope").HasMaxLength(30).IsRequired();
        builder.Property(x => x.RequiresOfficialNumber).HasColumnName("RequiresOfficialNumber").IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("IsActive").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
    }
}

public sealed class DocumentStatusTypeConfiguration : IEntityTypeConfiguration<DocumentStatusType>
{
    public void Configure(EntityTypeBuilder<DocumentStatusType> builder)
    {
        builder.ToTable("DOCUMENT_STATUS_TYPES");
        builder.Ignore(x => x.Id);

        builder.HasKey(x => x.Code);
        builder.Property(x => x.Code).HasColumnName("Code").HasMaxLength(50).ValueGeneratedNever();
        builder.Property(x => x.NameAr).HasColumnName("NameAr").HasMaxLength(150).IsRequired();
        builder.Property(x => x.NameEn).HasColumnName("NameEn").HasMaxLength(150);
        builder.Property(x => x.IsTerminal).HasColumnName("IsTerminal").IsRequired();
        builder.Property(x => x.IsSystemStatus).HasColumnName("IsSystemStatus").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
    }
}

public sealed class DocumentParticipantConfiguration : IEntityTypeConfiguration<DocumentParticipant>
{
    public void Configure(EntityTypeBuilder<DocumentParticipant> builder)
    {
        builder.ToTable("DOCUMENT_PARTICIPANTS");
        builder.Ignore(x => x.Id);

        builder.HasKey(x => x.ParticipantId);
        builder.Property(x => x.ParticipantId).HasColumnName("ParticipantId").ValueGeneratedNever();
        builder.Property(x => x.DocumentId).HasColumnName("DocumentId").IsRequired();
        builder.Property(x => x.RoleCode).HasColumnName("RoleCode").HasMaxLength(50).IsRequired();
        builder.Property(x => x.TargetType).HasColumnName("TargetType").HasMaxLength(30).IsRequired();
        builder.Property(x => x.InternalDepartmentId).HasColumnName("InternalDepartmentId");
        builder.Property(x => x.ExternalOrganizationId).HasColumnName("ExternalOrganizationId");
        builder.Property(x => x.PersonName).HasColumnName("PersonName").HasMaxLength(200);
        builder.Property(x => x.IsPrimary).HasColumnName("IsPrimary").IsRequired();
        builder.Property(x => x.Notes).HasColumnName("Notes").HasMaxLength(500);
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();

        builder.HasIndex(x => x.DocumentId).HasDatabaseName("IX_DOC_PARTICIPANTS_DOC");
    }
}

public sealed class DocumentStatusHistoryConfiguration : IEntityTypeConfiguration<DocumentStatusHistory>
{
    public void Configure(EntityTypeBuilder<DocumentStatusHistory> builder)
    {
        builder.ToTable("DOCUMENT_STATUS_HISTORY");
        builder.Ignore(x => x.Id);

        builder.HasKey(x => x.HistoryId);
        builder.Property(x => x.HistoryId).HasColumnName("HistoryId").ValueGeneratedNever();
        builder.Property(x => x.DocumentId).HasColumnName("DocumentId").IsRequired();
        builder.Property(x => x.FromStatusCode).HasColumnName("FromStatusCode").HasMaxLength(50);
        builder.Property(x => x.ToStatusCode).HasColumnName("ToStatusCode").HasMaxLength(50).IsRequired();
        builder.Property(x => x.ChangedByUserId).HasColumnName("ChangedByUserId").IsRequired();
        builder.Property(x => x.Reason).HasColumnName("Reason").HasMaxLength(500);
        builder.Property(x => x.ChangedAt).HasColumnName("ChangedAt").HasColumnType("datetime2(3)").IsRequired();

        builder.HasIndex(x => new { x.DocumentId, x.ChangedAt }).HasDatabaseName("IX_DOC_STATUS_HISTORY_DOC_DATE");
    }
}

public sealed class ExternalOrganizationConfiguration : IEntityTypeConfiguration<ExternalOrganization>
{
    public void Configure(EntityTypeBuilder<ExternalOrganization> builder)
    {
        builder.ToTable("EXTERNAL_ORGANIZATIONS");
        builder.Ignore(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        builder.HasKey(x => x.ExternalOrganizationId);
        builder.Property(x => x.ExternalOrganizationId).HasColumnName("ExternalOrganizationId").ValueGeneratedNever();
        builder.Property(x => x.Code).HasColumnName("Code").HasMaxLength(50).IsRequired();
        builder.Property(x => x.NameAr).HasColumnName("NameAr").HasMaxLength(250).IsRequired();
        builder.Property(x => x.NameEn).HasColumnName("NameEn").HasMaxLength(250);
        builder.Property(x => x.OrganizationType).HasColumnName("OrganizationType").HasMaxLength(50).IsRequired();
        builder.Property(x => x.ContactInfo).HasColumnName("ContactInfo").HasMaxLength(500);
        builder.Property(x => x.IsActive).HasColumnName("IsActive").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt").HasColumnType("datetime2(3)");

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("UX_EXTERNAL_ORGS_CODE");
    }
}
