using InstitutionalDiwan.Domain.Entities.Attachments;
using InstitutionalDiwan.Domain.Entities.Drive;
using InstitutionalDiwan.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstitutionalDiwan.Infrastructure.Persistence.Configurations;

public sealed class DriveSpaceConfiguration : IEntityTypeConfiguration<DriveSpace>
{
    public void Configure(EntityTypeBuilder<DriveSpace> builder)
    {
        builder.ToTable("DRIVE_SPACES");
        builder.Ignore(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        builder.HasKey(x => x.SpaceId);
        builder.Property(x => x.SpaceId).HasColumnName("SpaceId").ValueGeneratedNever();
        builder.Property(x => x.Name).HasColumnName("Name").HasMaxLength(150).IsRequired();
        builder.Property(x => x.SpaceTypeId).HasColumnName("SpaceTypeId").IsRequired();
        builder.Property(x => x.OwnerUserId).HasColumnName("OwnerUserId").IsRequired();
        builder.Property(x => x.QuotaBytes).HasColumnName("QuotaBytes");
        builder.Property(x => x.UsedBytes).HasColumnName("UsedBytes").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.CreatedByUserId).HasColumnName("CreatedByUserId").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt").HasColumnType("datetime2(3)");
        builder.Property(x => x.TrashedAt).HasColumnName("TrashedAt").HasColumnType("datetime2(3)");
        builder.Property(x => x.TrashedByUserId).HasColumnName("TrashedByUserId");
        builder.Property(x => x.RowVersion).HasColumnName("RowVersion").IsRowVersion();

        // One personal space per user; shared spaces are unrestricted.
        builder.HasIndex(x => x.OwnerUserId)
            .IsUnique()
            .HasFilter("[SpaceTypeId] = 1")
            .HasDatabaseName("UX_DRIVE_SPACES_PERSONAL");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DriveItemConfiguration : IEntityTypeConfiguration<DriveItem>
{
    public void Configure(EntityTypeBuilder<DriveItem> builder)
    {
        builder.ToTable("DRIVE_ITEMS");
        builder.Ignore(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        builder.HasKey(x => x.ItemId);
        builder.Property(x => x.ItemId).HasColumnName("ItemId").ValueGeneratedNever();
        builder.Property(x => x.SpaceId).HasColumnName("SpaceId").IsRequired();
        builder.Property(x => x.ParentId).HasColumnName("ParentId");
        builder.Property(x => x.ItemKindId).HasColumnName("ItemKindId").IsRequired();
        builder.Property(x => x.Name).HasColumnName("Name").HasMaxLength(255).IsRequired();
        builder.Property(x => x.OwnerUserId).HasColumnName("OwnerUserId").IsRequired();
        builder.Property(x => x.CurrentVersionId).HasColumnName("CurrentVersionId");
        builder.Property(x => x.SizeBytes).HasColumnName("SizeBytes").IsRequired();
        builder.Property(x => x.BreakInheritance).HasColumnName("BreakInheritance").IsRequired();
        builder.Property(x => x.TrashedAt).HasColumnName("TrashedAt").HasColumnType("datetime2(3)");
        builder.Property(x => x.TrashedByUserId).HasColumnName("TrashedByUserId");
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.CreatedByUserId).HasColumnName("CreatedByUserId").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt").HasColumnType("datetime2(3)");
        builder.Property(x => x.RowVersion).HasColumnName("RowVersion").IsRowVersion();

        // Unique names among live siblings; SQL Server treats NULL parents as one root level.
        builder.HasIndex(x => new { x.SpaceId, x.ParentId, x.Name })
            .IsUnique()
            .HasFilter("[TrashedAt] IS NULL")
            .HasDatabaseName("UX_DRIVE_ITEMS_NAME");
        builder.HasIndex(x => new { x.SpaceId, x.ParentId }).HasDatabaseName("IX_DRIVE_ITEMS_PARENT");
        builder.HasIndex(x => x.TrashedAt).HasDatabaseName("IX_DRIVE_ITEMS_TRASHED");
        builder.HasIndex(x => x.OwnerUserId).HasDatabaseName("IX_DRIVE_ITEMS_OWNER");

        builder.HasOne<DriveSpace>()
            .WithMany()
            .HasForeignKey(x => x.SpaceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<DriveItem>()
            .WithMany()
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Restrict, not cascade: the item row is deleted before its versions in a permanent purge.
        builder.HasOne<DriveFileVersion>()
            .WithMany()
            .HasForeignKey(x => x.CurrentVersionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DriveFileVersionConfiguration : IEntityTypeConfiguration<DriveFileVersion>
{
    public void Configure(EntityTypeBuilder<DriveFileVersion> builder)
    {
        builder.ToTable("DRIVE_FILE_VERSIONS");
        builder.Ignore(x => x.Id);

        builder.HasKey(x => x.VersionId);
        builder.Property(x => x.VersionId).HasColumnName("VersionId").ValueGeneratedNever();
        builder.Property(x => x.FileItemId).HasColumnName("FileItemId").IsRequired();
        builder.Property(x => x.VersionNumber).HasColumnName("VersionNumber").IsRequired();
        builder.Property(x => x.StorageProviderId).HasColumnName("StorageProviderId").IsRequired();
        builder.Property(x => x.ObjectKey).HasColumnName("ObjectKey").HasMaxLength(1000).IsRequired();
        builder.Property(x => x.SizeBytes).HasColumnName("SizeBytes").IsRequired();
        builder.Property(x => x.ChecksumSha256).HasColumnName("ChecksumSha256").HasColumnType("char(64)");
        builder.Property(x => x.MimeType).HasColumnName("MimeType").HasMaxLength(150).IsRequired();
        builder.Property(x => x.SourceId).HasColumnName("SourceId").IsRequired();
        builder.Property(x => x.StatusId).HasColumnName("StatusId").IsRequired();
        builder.Property(x => x.CreatedByUserId).HasColumnName("CreatedByUserId").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.CommittedAt).HasColumnName("CommittedAt").HasColumnType("datetime2(3)");

        builder.HasIndex(x => new { x.FileItemId, x.VersionNumber }).IsUnique().HasDatabaseName("UX_DRIVE_VERSIONS_NO");
        builder.HasIndex(x => new { x.StatusId, x.CreatedAt }).HasDatabaseName("IX_DRIVE_VERSIONS_PENDING");

        builder.HasOne<DriveItem>()
            .WithMany()
            .HasForeignKey(x => x.FileItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<FileStorageProvider>()
            .WithMany()
            .HasForeignKey(x => x.StorageProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DrivePermissionConfiguration : IEntityTypeConfiguration<DrivePermission>
{
    public void Configure(EntityTypeBuilder<DrivePermission> builder)
    {
        builder.ToTable("DRIVE_PERMISSIONS");
        builder.Ignore(x => x.Id);

        builder.HasKey(x => x.PermissionId);
        builder.Property(x => x.PermissionId).HasColumnName("PermissionId").ValueGeneratedNever();
        builder.Property(x => x.ItemId).HasColumnName("ItemId").IsRequired();
        builder.Property(x => x.PrincipalTypeId).HasColumnName("PrincipalTypeId").IsRequired();
        // Polymorphic: User.UserId or Department.DepartmentId — no database FK.
        builder.Property(x => x.PrincipalId).HasColumnName("PrincipalId").IsRequired();
        builder.Property(x => x.RoleId).HasColumnName("RoleId").IsRequired();
        builder.Property(x => x.GrantedByUserId).HasColumnName("GrantedByUserId").IsRequired();
        builder.Property(x => x.GrantedAt).HasColumnName("GrantedAt").HasColumnType("datetime2(3)").IsRequired();

        builder.HasIndex(x => new { x.ItemId, x.PrincipalTypeId, x.PrincipalId }).IsUnique().HasDatabaseName("UX_DRIVE_PERMISSIONS");
        builder.HasIndex(x => new { x.PrincipalTypeId, x.PrincipalId }).HasDatabaseName("IX_DRIVE_PERMISSIONS_PRINCIPAL");

        builder.HasOne<DriveItem>()
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.GrantedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DriveDeletionRequestConfiguration : IEntityTypeConfiguration<DriveDeletionRequest>
{
    public void Configure(EntityTypeBuilder<DriveDeletionRequest> builder)
    {
        builder.ToTable("DRIVE_DELETION_REQUESTS");
        builder.Ignore(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        builder.HasKey(x => x.RequestId);
        builder.Property(x => x.RequestId).HasColumnName("RequestId").ValueGeneratedNever();
        builder.Property(x => x.ItemId).HasColumnName("ItemId").IsRequired();
        builder.Property(x => x.RequestedByUserId).HasColumnName("RequestedByUserId").IsRequired();
        builder.Property(x => x.Reason).HasColumnName("Reason").HasMaxLength(500).IsRequired();
        builder.Property(x => x.StatusId).HasColumnName("StatusId").IsRequired();
        builder.Property(x => x.ReviewedByUserId).HasColumnName("ReviewedByUserId");
        builder.Property(x => x.ReviewedAt).HasColumnName("ReviewedAt").HasColumnType("datetime2(3)");
        builder.Property(x => x.ReviewNote).HasColumnName("ReviewNote").HasMaxLength(500);
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();

        // One pending request per item.
        builder.HasIndex(x => x.ItemId)
            .IsUnique()
            .HasFilter("[StatusId] = 1")
            .HasDatabaseName("UX_DRIVE_DELETION_PENDING");
        builder.HasIndex(x => new { x.StatusId, x.CreatedAt }).HasDatabaseName("IX_DRIVE_DELETION_STATUS");

        builder.HasOne<DriveItem>()
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DriveEditSessionConfiguration : IEntityTypeConfiguration<DriveEditSession>
{
    public void Configure(EntityTypeBuilder<DriveEditSession> builder)
    {
        builder.ToTable("DRIVE_EDIT_SESSIONS");
        builder.Ignore(x => x.Id);

        builder.HasKey(x => x.SessionId);
        builder.Property(x => x.SessionId).HasColumnName("SessionId").ValueGeneratedNever();
        builder.Property(x => x.FileItemId).HasColumnName("FileItemId").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("UserId").IsRequired();
        builder.Property(x => x.DocumentKey).HasColumnName("DocumentKey").HasMaxLength(200).IsRequired();
        builder.Property(x => x.BaseVersionId).HasColumnName("BaseVersionId").IsRequired();
        builder.Property(x => x.StatusId).HasColumnName("StatusId").IsRequired();
        builder.Property(x => x.OpenedAt).HasColumnName("OpenedAt").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.LastActivityAt).HasColumnName("LastActivityAt").HasColumnType("datetime2(3)").IsRequired();

        // One open session per document key (item + version).
        builder.HasIndex(x => x.DocumentKey)
            .IsUnique()
            .HasFilter("[StatusId] = 1")
            .HasDatabaseName("UX_DRIVE_SESSIONS_KEY");
        builder.HasIndex(x => new { x.StatusId, x.LastActivityAt }).HasDatabaseName("IX_DRIVE_SESSIONS_IDLE");

        // Restrict on both sides: a permanent purge deletes sessions before the item and its versions.
        builder.HasOne<DriveItem>()
            .WithMany()
            .HasForeignKey(x => x.FileItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<DriveFileVersion>()
            .WithMany()
            .HasForeignKey(x => x.BaseVersionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DriveStarConfiguration : IEntityTypeConfiguration<DriveStar>
{
    public void Configure(EntityTypeBuilder<DriveStar> builder)
    {
        builder.ToTable("DRIVE_STARS");
        builder.Ignore(x => x.Id);

        builder.HasKey(x => x.StarId);
        builder.Property(x => x.StarId).HasColumnName("StarId").ValueGeneratedNever();
        builder.Property(x => x.UserId).HasColumnName("UserId").IsRequired();
        builder.Property(x => x.ItemId).HasColumnName("ItemId").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();

        builder.HasIndex(x => new { x.UserId, x.ItemId }).IsUnique().HasDatabaseName("UX_DRIVE_STARS");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<DriveItem>()
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}