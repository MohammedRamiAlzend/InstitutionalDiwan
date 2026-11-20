using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstitutionalDiwan.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261119091628_Add_Drive_Module : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "DriveAllowedExtensionsCsv",
            table: "INSTITUTION_SETTINGS",
            type: "nvarchar(1000)",
            maxLength: 1000,
            nullable: false,
            defaultValue: ".doc,.docx,.xls,.xlsx,.ppt,.pptx,.pdf,.txt,.md,.csv,.jpg,.jpeg,.png,.gif,.webp,.svg,.mp4,.webm,.mkv,.mov,.mp3,.wav,.zip,.7z,.rar");

        migrationBuilder.AddColumn<long>(
            name: "DriveDefaultQuotaBytes",
            table: "INSTITUTION_SETTINGS",
            type: "bigint",
            nullable: false,
            defaultValue: 5368709120L);

        migrationBuilder.AddColumn<long>(
            name: "DriveMaxFileSizeBytes",
            table: "INSTITUTION_SETTINGS",
            type: "bigint",
            nullable: false,
            defaultValue: 209715200L);

        migrationBuilder.CreateTable(
            name: "DRIVE_SPACES",
            columns: table => new
            {
                SpaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                SpaceTypeId = table.Column<int>(type: "int", nullable: false),
                OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                QuotaBytes = table.Column<long>(type: "bigint", nullable: true),
                UsedBytes = table.Column<long>(type: "bigint", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                TrashedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                TrashedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DRIVE_SPACES", x => x.SpaceId);
                table.ForeignKey(
                    name: "FK_DRIVE_SPACES_USERS_OwnerUserId",
                    column: x => x.OwnerUserId,
                    principalTable: "USERS",
                    principalColumn: "UserId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "DRIVE_DELETION_REQUESTS",
            columns: table => new
            {
                RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                StatusId = table.Column<int>(type: "int", nullable: false),
                ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                ReviewedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                ReviewNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DRIVE_DELETION_REQUESTS", x => x.RequestId);
                table.ForeignKey(
                    name: "FK_DRIVE_DELETION_REQUESTS_USERS_RequestedByUserId",
                    column: x => x.RequestedByUserId,
                    principalTable: "USERS",
                    principalColumn: "UserId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DRIVE_DELETION_REQUESTS_USERS_ReviewedByUserId",
                    column: x => x.ReviewedByUserId,
                    principalTable: "USERS",
                    principalColumn: "UserId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "DRIVE_EDIT_SESSIONS",
            columns: table => new
            {
                SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                FileItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DocumentKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                BaseVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                StatusId = table.Column<int>(type: "int", nullable: false),
                OpenedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                LastActivityAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DRIVE_EDIT_SESSIONS", x => x.SessionId);
                table.ForeignKey(
                    name: "FK_DRIVE_EDIT_SESSIONS_USERS_UserId",
                    column: x => x.UserId,
                    principalTable: "USERS",
                    principalColumn: "UserId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "DRIVE_FILE_VERSIONS",
            columns: table => new
            {
                VersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                FileItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                VersionNumber = table.Column<int>(type: "int", nullable: false),
                StorageProviderId = table.Column<int>(type: "int", nullable: false),
                ObjectKey = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                ChecksumSha256 = table.Column<string>(type: "char(64)", nullable: true),
                MimeType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                SourceId = table.Column<int>(type: "int", nullable: false),
                StatusId = table.Column<int>(type: "int", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                CommittedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DRIVE_FILE_VERSIONS", x => x.VersionId);
                table.ForeignKey(
                    name: "FK_DRIVE_FILE_VERSIONS_FILE_STORAGE_PROVIDERS_StorageProviderId",
                    column: x => x.StorageProviderId,
                    principalTable: "FILE_STORAGE_PROVIDERS",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DRIVE_FILE_VERSIONS_USERS_CreatedByUserId",
                    column: x => x.CreatedByUserId,
                    principalTable: "USERS",
                    principalColumn: "UserId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "DRIVE_ITEMS",
            columns: table => new
            {
                ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SpaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                ItemKindId = table.Column<int>(type: "int", nullable: false),
                Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CurrentVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                BreakInheritance = table.Column<bool>(type: "bit", nullable: false),
                TrashedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                TrashedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DRIVE_ITEMS", x => x.ItemId);
                table.ForeignKey(
                    name: "FK_DRIVE_ITEMS_DRIVE_FILE_VERSIONS_CurrentVersionId",
                    column: x => x.CurrentVersionId,
                    principalTable: "DRIVE_FILE_VERSIONS",
                    principalColumn: "VersionId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DRIVE_ITEMS_DRIVE_ITEMS_ParentId",
                    column: x => x.ParentId,
                    principalTable: "DRIVE_ITEMS",
                    principalColumn: "ItemId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DRIVE_ITEMS_DRIVE_SPACES_SpaceId",
                    column: x => x.SpaceId,
                    principalTable: "DRIVE_SPACES",
                    principalColumn: "SpaceId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DRIVE_ITEMS_USERS_OwnerUserId",
                    column: x => x.OwnerUserId,
                    principalTable: "USERS",
                    principalColumn: "UserId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "DRIVE_PERMISSIONS",
            columns: table => new
            {
                PermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PrincipalTypeId = table.Column<int>(type: "int", nullable: false),
                PrincipalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RoleId = table.Column<int>(type: "int", nullable: false),
                GrantedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                GrantedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DRIVE_PERMISSIONS", x => x.PermissionId);
                table.ForeignKey(
                    name: "FK_DRIVE_PERMISSIONS_DRIVE_ITEMS_ItemId",
                    column: x => x.ItemId,
                    principalTable: "DRIVE_ITEMS",
                    principalColumn: "ItemId",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_DRIVE_PERMISSIONS_USERS_GrantedByUserId",
                    column: x => x.GrantedByUserId,
                    principalTable: "USERS",
                    principalColumn: "UserId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "DRIVE_STARS",
            columns: table => new
            {
                StarId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DRIVE_STARS", x => x.StarId);
                table.ForeignKey(
                    name: "FK_DRIVE_STARS_DRIVE_ITEMS_ItemId",
                    column: x => x.ItemId,
                    principalTable: "DRIVE_ITEMS",
                    principalColumn: "ItemId",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_DRIVE_STARS_USERS_UserId",
                    column: x => x.UserId,
                    principalTable: "USERS",
                    principalColumn: "UserId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_DRIVE_DELETION_REQUESTS_RequestedByUserId",
            table: "DRIVE_DELETION_REQUESTS",
            column: "RequestedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_DRIVE_DELETION_REQUESTS_ReviewedByUserId",
            table: "DRIVE_DELETION_REQUESTS",
            column: "ReviewedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_DRIVE_DELETION_STATUS",
            table: "DRIVE_DELETION_REQUESTS",
            columns: new[] { "StatusId", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "UX_DRIVE_DELETION_PENDING",
            table: "DRIVE_DELETION_REQUESTS",
            column: "ItemId",
            unique: true,
            filter: "[StatusId] = 1");

        migrationBuilder.CreateIndex(
            name: "IX_DRIVE_EDIT_SESSIONS_BaseVersionId",
            table: "DRIVE_EDIT_SESSIONS",
            column: "BaseVersionId");

        migrationBuilder.CreateIndex(
            name: "IX_DRIVE_EDIT_SESSIONS_FileItemId",
            table: "DRIVE_EDIT_SESSIONS",
            column: "FileItemId");

        migrationBuilder.CreateIndex(
            name: "IX_DRIVE_EDIT_SESSIONS_UserId",
            table: "DRIVE_EDIT_SESSIONS",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_DRIVE_SESSIONS_IDLE",
            table: "DRIVE_EDIT_SESSIONS",
            columns: new[] { "StatusId", "LastActivityAt" });

        migrationBuilder.CreateIndex(
            name: "UX_DRIVE_SESSIONS_KEY",
            table: "DRIVE_EDIT_SESSIONS",
            column: "DocumentKey",
            unique: true,
            filter: "[StatusId] = 1");

        migrationBuilder.CreateIndex(
            name: "IX_DRIVE_FILE_VERSIONS_CreatedByUserId",
            table: "DRIVE_FILE_VERSIONS",
            column: "CreatedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_DRIVE_FILE_VERSIONS_StorageProviderId",
            table: "DRIVE_FILE_VERSIONS",
            column: "StorageProviderId");

        migrationBuilder.CreateIndex(
            name: "IX_DRIVE_VERSIONS_PENDING",
            table: "DRIVE_FILE_VERSIONS",
            columns: new[] { "StatusId", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "UX_DRIVE_VERSIONS_NO",
            table: "DRIVE_FILE_VERSIONS",
            columns: new[] { "FileItemId", "VersionNumber" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DRIVE_ITEMS_CurrentVersionId",
            table: "DRIVE_ITEMS",
            column: "CurrentVersionId");

        migrationBuilder.CreateIndex(
            name: "IX_DRIVE_ITEMS_OWNER",
            table: "DRIVE_ITEMS",
            column: "OwnerUserId");

        migrationBuilder.CreateIndex(
            name: "IX_DRIVE_ITEMS_PARENT",
            table: "DRIVE_ITEMS",
            columns: new[] { "SpaceId", "ParentId" });

        migrationBuilder.CreateIndex(
            name: "IX_DRIVE_ITEMS_ParentId",
            table: "DRIVE_ITEMS",
            column: "ParentId");

        migrationBuilder.CreateIndex(
            name: "IX_DRIVE_ITEMS_TRASHED",
            table: "DRIVE_ITEMS",
            column: "TrashedAt");

        migrationBuilder.CreateIndex(
            name: "UX_DRIVE_ITEMS_NAME",
            table: "DRIVE_ITEMS",
            columns: new[] { "SpaceId", "ParentId", "Name" },
            unique: true,
            filter: "[TrashedAt] IS NULL");

        migrationBuilder.CreateIndex(
            name: "IX_DRIVE_PERMISSIONS_GrantedByUserId",
            table: "DRIVE_PERMISSIONS",
            column: "GrantedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_DRIVE_PERMISSIONS_PRINCIPAL",
            table: "DRIVE_PERMISSIONS",
            columns: new[] { "PrincipalTypeId", "PrincipalId" });

        migrationBuilder.CreateIndex(
            name: "UX_DRIVE_PERMISSIONS",
            table: "DRIVE_PERMISSIONS",
            columns: new[] { "ItemId", "PrincipalTypeId", "PrincipalId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UX_DRIVE_SPACES_PERSONAL",
            table: "DRIVE_SPACES",
            column: "OwnerUserId",
            unique: true,
            filter: "[SpaceTypeId] = 1");

        migrationBuilder.CreateIndex(
            name: "IX_DRIVE_STARS_ItemId",
            table: "DRIVE_STARS",
            column: "ItemId");

        migrationBuilder.CreateIndex(
            name: "UX_DRIVE_STARS",
            table: "DRIVE_STARS",
            columns: new[] { "UserId", "ItemId" },
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_DRIVE_DELETION_REQUESTS_DRIVE_ITEMS_ItemId",
            table: "DRIVE_DELETION_REQUESTS",
            column: "ItemId",
            principalTable: "DRIVE_ITEMS",
            principalColumn: "ItemId",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_DRIVE_EDIT_SESSIONS_DRIVE_FILE_VERSIONS_BaseVersionId",
            table: "DRIVE_EDIT_SESSIONS",
            column: "BaseVersionId",
            principalTable: "DRIVE_FILE_VERSIONS",
            principalColumn: "VersionId",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_DRIVE_EDIT_SESSIONS_DRIVE_ITEMS_FileItemId",
            table: "DRIVE_EDIT_SESSIONS",
            column: "FileItemId",
            principalTable: "DRIVE_ITEMS",
            principalColumn: "ItemId",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_DRIVE_FILE_VERSIONS_DRIVE_ITEMS_FileItemId",
            table: "DRIVE_FILE_VERSIONS",
            column: "FileItemId",
            principalTable: "DRIVE_ITEMS",
            principalColumn: "ItemId",
            onDelete: ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_DRIVE_FILE_VERSIONS_DRIVE_ITEMS_FileItemId",
            table: "DRIVE_FILE_VERSIONS");

        migrationBuilder.DropTable(
            name: "DRIVE_DELETION_REQUESTS");

        migrationBuilder.DropTable(
            name: "DRIVE_EDIT_SESSIONS");

        migrationBuilder.DropTable(
            name: "DRIVE_PERMISSIONS");

        migrationBuilder.DropTable(
            name: "DRIVE_STARS");

        migrationBuilder.DropTable(
            name: "DRIVE_ITEMS");

        migrationBuilder.DropTable(
            name: "DRIVE_FILE_VERSIONS");

        migrationBuilder.DropTable(
            name: "DRIVE_SPACES");

        migrationBuilder.DropColumn(
            name: "DriveAllowedExtensionsCsv",
            table: "INSTITUTION_SETTINGS");

        migrationBuilder.DropColumn(
            name: "DriveDefaultQuotaBytes",
            table: "INSTITUTION_SETTINGS");

        migrationBuilder.DropColumn(
            name: "DriveMaxFileSizeBytes",
            table: "INSTITUTION_SETTINGS");
    }
}
