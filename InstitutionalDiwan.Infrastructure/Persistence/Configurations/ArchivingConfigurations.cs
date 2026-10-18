using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InstitutionalDiwan.Domain.Entities.Archiving;

namespace InstitutionalDiwan.Infrastructure.Persistence.Configurations;

public sealed class DocumentArchiveEventConfiguration : IEntityTypeConfiguration<DocumentArchiveEvent>
{
    public void Configure(EntityTypeBuilder<DocumentArchiveEvent> builder)
    {
        builder.ToTable("DOCUMENT_ARCHIVE_EVENTS");
        builder.Ignore(x => x.Id);

        builder.HasKey(x => x.ArchiveEventId);
        builder.Property(x => x.ArchiveEventId).HasColumnName("ArchiveEventId").ValueGeneratedNever();
        builder.Property(x => x.DocumentId).HasColumnName("DocumentId").IsRequired();
        builder.Property(x => x.EventTypeId).HasColumnName("EventTypeId").IsRequired();
        builder.Property(x => x.PreviousStatusId).HasColumnName("PreviousStatusId").IsRequired();
        builder.Property(x => x.NewStatusId).HasColumnName("NewStatusId").IsRequired();
        builder.Property(x => x.ActorUserId).HasColumnName("ActorUserId").IsRequired();
        builder.Property(x => x.Reason).HasColumnName("Reason").HasMaxLength(500);
        builder.Property(x => x.ArchiveLocationId).HasColumnName("ArchiveLocationId");
        builder.Property(x => x.OccurredAt).HasColumnName("OccurredAt").HasColumnType("datetime2(3)").IsRequired();

        builder.HasIndex(x => new { x.DocumentId, x.OccurredAt }).HasDatabaseName("IX_DOC_ARCHIVE_EVENTS_DOC_DATE");

        builder.HasOne<ArchiveEventType>()
            .WithMany()
            .HasForeignKey(x => x.EventTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ArchiveLocation>()
            .WithMany()
            .HasForeignKey(x => x.ArchiveLocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class SavedSearchConfiguration : IEntityTypeConfiguration<SavedSearch>
{
    public void Configure(EntityTypeBuilder<SavedSearch> builder)
    {
        builder.ToTable("SAVED_SEARCHES");
        builder.Ignore(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        builder.HasKey(x => x.SavedSearchId);
        builder.Property(x => x.SavedSearchId).HasColumnName("SavedSearchId").ValueGeneratedNever();
        builder.Property(x => x.UserId).HasColumnName("UserId").IsRequired();
        builder.Property(x => x.Name).HasColumnName("Name").HasMaxLength(150).IsRequired();
        builder.Property(x => x.CriteriaJson).HasColumnName("CriteriaJson").IsRequired();
        builder.Property(x => x.IsDefault).HasColumnName("IsDefault").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt").HasColumnType("datetime2(3)");

        builder.HasIndex(x => x.UserId).HasDatabaseName("IX_SAVED_SEARCHES_USER");
    }
}

public sealed class ArchiveEventTypeConfiguration : IEntityTypeConfiguration<ArchiveEventType>
{
    public void Configure(EntityTypeBuilder<ArchiveEventType> builder)
    {
        builder.ToTable("ARCHIVE_EVENT_TYPES");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(x => x.Desc).HasColumnName("Desc").HasMaxLength(150).IsRequired();
        builder.Property(x => x.DescAr).HasColumnName("DescAr").HasMaxLength(150).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("IsActive").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
    }
}

public sealed class ArchiveLocationConfiguration : IEntityTypeConfiguration<ArchiveLocation>
{
    public void Configure(EntityTypeBuilder<ArchiveLocation> builder)
    {
        builder.ToTable("ARCHIVE_LOCATIONS");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(x => x.Desc).HasColumnName("Desc").HasMaxLength(150).IsRequired();
        builder.Property(x => x.DescAr).HasColumnName("DescAr").HasMaxLength(150).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("IsActive").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
    }
}
