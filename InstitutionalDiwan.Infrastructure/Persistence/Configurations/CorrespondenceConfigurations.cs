using InstitutionalDiwan.Domain.Entities.Correspondence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

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
        builder.Property(x => x.DocumentTypeId).HasColumnName("DocumentTypeId").IsRequired();
        builder.Property(x => x.DirectionId).HasColumnName("DirectionId").IsRequired();
        builder.Property(x => x.ConfidentialityLevelId).HasColumnName("ConfidentialityLevelId").IsRequired();
        builder.Property(x => x.PriorityLevelId).HasColumnName("PriorityLevelId").IsRequired();
        builder.Property(x => x.CurrentStatusId).HasColumnName("CurrentStatusId").IsRequired();
        builder.Property(x => x.StatusBeforeArchiveId).HasColumnName("StatusBeforeArchiveId");
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

        builder.HasIndex(x => new { x.OwningDepartmentId, x.CurrentStatusId }).HasDatabaseName("IX_DOCUMENTS_DEPT_STATUS");
        builder.HasIndex(x => x.OfficialNumberText).HasDatabaseName("IX_DOCUMENTS_OFFICIAL_NUMBER");
        builder.HasIndex(x => x.RegistrationDate).HasDatabaseName("IX_DOCUMENTS_REG_DATE");

        builder.HasOne<DocumentType>().WithMany().HasForeignKey(x => x.DocumentTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DocumentDirection>().WithMany().HasForeignKey(x => x.DirectionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DocumentConfidentialityLevel>().WithMany().HasForeignKey(x => x.ConfidentialityLevelId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DocumentPriorityLevel>().WithMany().HasForeignKey(x => x.PriorityLevelId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DocumentStatusType>().WithMany().HasForeignKey(x => x.CurrentStatusId).OnDelete(DeleteBehavior.Restrict);

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

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(x => x.Desc).HasColumnName("Desc").HasMaxLength(150).IsRequired();
        builder.Property(x => x.DescAr).HasColumnName("DescAr").HasMaxLength(150).IsRequired();
        builder.Property(x => x.AllowedDirectionId).HasColumnName("AllowedDirectionId").IsRequired();
        builder.Property(x => x.RequiresOfficialNumber).HasColumnName("RequiresOfficialNumber").IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("IsActive").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();

        builder.HasOne<DocumentDirection>().WithMany().HasForeignKey(x => x.AllowedDirectionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DocumentStatusTypeConfiguration : IEntityTypeConfiguration<DocumentStatusType>
{
    public void Configure(EntityTypeBuilder<DocumentStatusType> builder)
    {
        builder.ToTable("DOCUMENT_STATUS_TYPES");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(x => x.Desc).HasColumnName("Desc").HasMaxLength(150).IsRequired();
        builder.Property(x => x.DescAr).HasColumnName("DescAr").HasMaxLength(150).IsRequired();
        builder.Property(x => x.IsTerminal).HasColumnName("IsTerminal").IsRequired();
        builder.Property(x => x.IsSystemStatus).HasColumnName("IsSystemStatus").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
    }
}

public sealed class DocumentDirectionConfiguration : IEntityTypeConfiguration<DocumentDirection>
{
    public void Configure(EntityTypeBuilder<DocumentDirection> builder)
    {
        builder.ToTable("DOCUMENT_DIRECTIONS");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(x => x.Desc).HasColumnName("Desc").HasMaxLength(150).IsRequired();
        builder.Property(x => x.DescAr).HasColumnName("DescAr").HasMaxLength(150).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("IsActive").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
    }
}

public sealed class DocumentConfidentialityLevelConfiguration : IEntityTypeConfiguration<DocumentConfidentialityLevel>
{
    public void Configure(EntityTypeBuilder<DocumentConfidentialityLevel> builder)
    {
        builder.ToTable("DOCUMENT_CONFIDENTIALITY_LEVELS");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(x => x.Desc).HasColumnName("Desc").HasMaxLength(150).IsRequired();
        builder.Property(x => x.DescAr).HasColumnName("DescAr").HasMaxLength(150).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("IsActive").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
    }
}

public sealed class DocumentPriorityLevelConfiguration : IEntityTypeConfiguration<DocumentPriorityLevel>
{
    public void Configure(EntityTypeBuilder<DocumentPriorityLevel> builder)
    {
        builder.ToTable("DOCUMENT_PRIORITY_LEVELS");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(x => x.Desc).HasColumnName("Desc").HasMaxLength(150).IsRequired();
        builder.Property(x => x.DescAr).HasColumnName("DescAr").HasMaxLength(150).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("IsActive").IsRequired();
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
        builder.Property(x => x.RoleId).HasColumnName("RoleId").IsRequired();
        builder.Property(x => x.TargetTypeId).HasColumnName("TargetTypeId").IsRequired();
        builder.Property(x => x.InternalDepartmentId).HasColumnName("InternalDepartmentId");
        builder.Property(x => x.ExternalOrganizationId).HasColumnName("ExternalOrganizationId");
        builder.Property(x => x.PersonName).HasColumnName("PersonName").HasMaxLength(200);
        builder.Property(x => x.IsPrimary).HasColumnName("IsPrimary").IsRequired();
        builder.Property(x => x.Notes).HasColumnName("Notes").HasMaxLength(500);
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();

        builder.HasIndex(x => x.DocumentId).HasDatabaseName("IX_DOC_PARTICIPANTS_DOC");

        builder.HasOne<ParticipantRole>()
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ParticipantTargetType>()
            .WithMany()
            .HasForeignKey(x => x.TargetTypeId)
            .OnDelete(DeleteBehavior.Restrict);
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
        builder.Property(x => x.FromStatusId).HasColumnName("FromStatusId");
        builder.Property(x => x.ToStatusId).HasColumnName("ToStatusId").IsRequired();
        builder.Property(x => x.ChangedByUserId).HasColumnName("ChangedByUserId").IsRequired();
        builder.Property(x => x.Reason).HasColumnName("Reason").HasMaxLength(500);
        builder.Property(x => x.ChangedAt).HasColumnName("ChangedAt").HasColumnType("datetime2(3)").IsRequired();

        builder.HasIndex(x => new { x.DocumentId, x.ChangedAt }).HasDatabaseName("IX_DOC_STATUS_HISTORY_DOC_DATE");

        builder.HasOne<DocumentStatusType>().WithMany().HasForeignKey(x => x.ToStatusId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DocumentStatusType>().WithMany().HasForeignKey(x => x.FromStatusId).OnDelete(DeleteBehavior.Restrict);
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
        builder.Property(x => x.OrganizationTypeId).HasColumnName("OrganizationTypeId").IsRequired();
        builder.Property(x => x.ContactInfo).HasColumnName("ContactInfo").HasMaxLength(500);
        builder.Property(x => x.IsActive).HasColumnName("IsActive").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt").HasColumnType("datetime2(3)");

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("UX_EXTERNAL_ORGS_CODE");

        builder.HasOne<OrganizationType>()
            .WithMany()
            .HasForeignKey(x => x.OrganizationTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ParticipantRoleConfiguration : IEntityTypeConfiguration<ParticipantRole>
{
    public void Configure(EntityTypeBuilder<ParticipantRole> builder)
    {
        builder.ToTable("PARTICIPANT_ROLES");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(x => x.Desc).HasColumnName("Desc").HasMaxLength(150).IsRequired();
        builder.Property(x => x.DescAr).HasColumnName("DescAr").HasMaxLength(150).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("IsActive").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
    }
}

public sealed class ParticipantTargetTypeConfiguration : IEntityTypeConfiguration<ParticipantTargetType>
{
    public void Configure(EntityTypeBuilder<ParticipantTargetType> builder)
    {
        builder.ToTable("PARTICIPANT_TARGET_TYPES");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(x => x.Desc).HasColumnName("Desc").HasMaxLength(150).IsRequired();
        builder.Property(x => x.DescAr).HasColumnName("DescAr").HasMaxLength(150).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("IsActive").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
    }
}

public sealed class OrganizationTypeConfiguration : IEntityTypeConfiguration<OrganizationType>
{
    public void Configure(EntityTypeBuilder<OrganizationType> builder)
    {
        builder.ToTable("ORGANIZATION_TYPES");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(x => x.Desc).HasColumnName("Desc").HasMaxLength(150).IsRequired();
        builder.Property(x => x.DescAr).HasColumnName("DescAr").HasMaxLength(150).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("IsActive").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
    }
}
