using InstitutionalDiwan.Domain.Entities.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstitutionalDiwan.Infrastructure.Persistence.Configurations;

public sealed class SystemErrorConfiguration : IEntityTypeConfiguration<SystemError>
{
    public void Configure(EntityTypeBuilder<SystemError> builder)
    {
        builder.ToTable("SYSTEM_ERRORS");
        builder.Ignore(x => x.Id);

        builder.HasKey(x => x.SystemErrorId);
        builder.Property(x => x.SystemErrorId).HasColumnName("SystemErrorId").ValueGeneratedNever();
        builder.Property(x => x.Fingerprint).HasColumnName("Fingerprint").HasMaxLength(SystemError.FingerprintLength).IsRequired();
        builder.Property(x => x.Source).HasColumnName("Source").IsRequired();
        builder.Property(x => x.Kind).HasColumnName("Kind").IsRequired();
        builder.Property(x => x.Title).HasColumnName("Title").HasMaxLength(SystemError.MaxTitleLength).IsRequired();
        builder.Property(x => x.Message).HasColumnName("Message").HasMaxLength(SystemError.MaxMessageLength).IsRequired();
        builder.Property(x => x.Details).HasColumnName("Details").HasMaxLength(SystemError.MaxDetailsLength);
        builder.Property(x => x.Path).HasColumnName("Path").HasMaxLength(SystemError.MaxPathLength);
        builder.Property(x => x.HttpMethod).HasColumnName("HttpMethod").HasMaxLength(10);
        builder.Property(x => x.StatusCode).HasColumnName("StatusCode");
        builder.Property(x => x.LastUserId).HasColumnName("LastUserId");
        builder.Property(x => x.LastUsername).HasColumnName("LastUsername").HasMaxLength(200);
        builder.Property(x => x.UserAgent).HasColumnName("UserAgent").HasMaxLength(500);
        builder.Property(x => x.TraceId).HasColumnName("TraceId").HasMaxLength(100);
        builder.Property(x => x.OccurrenceCount).HasColumnName("OccurrenceCount").IsRequired();
        builder.Property(x => x.FirstSeenAt).HasColumnName("FirstSeenAt").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.LastSeenAt).HasColumnName("LastSeenAt").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.Status).HasColumnName("Status").IsRequired();
        builder.Property(x => x.Notes).HasColumnName("Notes").HasMaxLength(SystemError.MaxNotesLength);
        builder.Property(x => x.ResolvedAt).HasColumnName("ResolvedAt").HasColumnType("datetime2(3)");
        builder.Property(x => x.UpdatedByUserId).HasColumnName("UpdatedByUserId");
        builder.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt").HasColumnType("datetime2(3)");

        builder.HasIndex(x => x.Fingerprint).IsUnique().HasDatabaseName("UX_SYSTEM_ERRORS_FINGERPRINT");
        builder.HasIndex(x => x.LastSeenAt).HasDatabaseName("IX_SYSTEM_ERRORS_LAST_SEEN");
    }
}
