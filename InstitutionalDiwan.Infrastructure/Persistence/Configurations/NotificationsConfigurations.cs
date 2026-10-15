using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InstitutionalDiwan.Domain.Entities.Notifications;

namespace InstitutionalDiwan.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("NOTIFICATIONS");
        builder.Ignore(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        builder.HasKey(x => x.NotificationId);
        builder.Property(x => x.NotificationId).HasColumnName("NotificationId").ValueGeneratedNever();
        builder.Property(x => x.NotificationTypeCode).HasColumnName("NotificationTypeCode").HasMaxLength(50).IsRequired();
        builder.Property(x => x.TitleAr).HasColumnName("TitleAr").HasMaxLength(300).IsRequired();
        builder.Property(x => x.BodyAr).HasColumnName("BodyAr").HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Priority).HasColumnName("Priority").HasMaxLength(30).IsRequired();
        builder.Property(x => x.RelatedDocumentId).HasColumnName("RelatedDocumentId");
        builder.Property(x => x.RelatedMovementId).HasColumnName("RelatedMovementId");
        builder.Property(x => x.ActionUrl).HasColumnName("ActionUrl").HasMaxLength(500);
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();

        builder.HasMany(x => x.Recipients)
            .WithOne()
            .HasForeignKey(x => x.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Recipients)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.OutboxEntries)
            .WithOne()
            .HasForeignKey(x => x.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.OutboxEntries)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class NotificationRecipientConfiguration : IEntityTypeConfiguration<NotificationRecipient>
{
    public void Configure(EntityTypeBuilder<NotificationRecipient> builder)
    {
        builder.ToTable("NOTIFICATION_RECIPIENTS");
        builder.Ignore(x => x.Id);

        builder.HasKey(x => x.RecipientId);
        builder.Property(x => x.RecipientId).HasColumnName("RecipientId").ValueGeneratedNever();
        builder.Property(x => x.NotificationId).HasColumnName("NotificationId").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("UserId").IsRequired();
        builder.Property(x => x.DepartmentId).HasColumnName("DepartmentId");
        builder.Property(x => x.IsRead).HasColumnName("IsRead").IsRequired();
        builder.Property(x => x.ReadAt).HasColumnName("ReadAt").HasColumnType("datetime2(3)");
        builder.Property(x => x.IsDismissed).HasColumnName("IsDismissed").IsRequired();
        builder.Property(x => x.DeliveredAt).HasColumnName("DeliveredAt").HasColumnType("datetime2(3)").IsRequired();

        builder.HasIndex(x => new { x.UserId, x.IsRead }).HasDatabaseName("IX_NOTIFICATION_RECIPIENTS_USER_READ");
    }
}

public sealed class NotificationOutboxConfiguration : IEntityTypeConfiguration<NotificationOutbox>
{
    public void Configure(EntityTypeBuilder<NotificationOutbox> builder)
    {
        builder.ToTable("NOTIFICATION_OUTBOX");
        builder.Ignore(x => x.Id);

        builder.HasKey(x => x.OutboxId);
        builder.Property(x => x.OutboxId).HasColumnName("OutboxId").ValueGeneratedNever();
        builder.Property(x => x.NotificationId).HasColumnName("NotificationId").IsRequired();
        builder.Property(x => x.Channel).HasColumnName("Channel").HasMaxLength(50).IsRequired();
        builder.Property(x => x.PayloadJson).HasColumnName("PayloadJson").IsRequired();
        builder.Property(x => x.Status).HasColumnName("Status").HasMaxLength(30).IsRequired();
        builder.Property(x => x.RetryCount).HasColumnName("RetryCount").IsRequired();
        builder.Property(x => x.LastError).HasColumnName("LastError").HasMaxLength(1000);
        builder.Property(x => x.NextRetryAt).HasColumnName("NextRetryAt").HasColumnType("datetime2(3)");
        builder.Property(x => x.ProcessedAt).HasColumnName("ProcessedAt").HasColumnType("datetime2(3)");
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();

        builder.HasIndex(x => new { x.Status, x.NextRetryAt }).HasDatabaseName("IX_NOTIFICATION_OUTBOX_STATUS_RETRY");
    }
}

public sealed class NotificationTypeConfiguration : IEntityTypeConfiguration<NotificationType>
{
    public void Configure(EntityTypeBuilder<NotificationType> builder)
    {
        builder.ToTable("NOTIFICATION_TYPES");
        builder.Ignore(x => x.Id);

        builder.HasKey(x => x.Code);
        builder.Property(x => x.Code).HasColumnName("Code").HasMaxLength(50).ValueGeneratedNever();
        builder.Property(x => x.NameAr).HasColumnName("NameAr").HasMaxLength(150).IsRequired();
        builder.Property(x => x.TitleTemplateAr).HasColumnName("TitleTemplateAr").HasMaxLength(300).IsRequired();
        builder.Property(x => x.BodyTemplateAr).HasColumnName("BodyTemplateAr").HasMaxLength(2000).IsRequired();
        builder.Property(x => x.DefaultChannelsCsv).HasColumnName("DefaultChannelsCsv").HasMaxLength(150).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("IsActive").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
    }
}
