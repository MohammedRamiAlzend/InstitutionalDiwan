using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstitutionalDiwan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Refactor_AllRemaining_ToLookups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_NOTIFICATION_TYPES",
                table: "NOTIFICATION_TYPES");

            migrationBuilder.DropIndex(
                name: "IX_NOTIFICATION_OUTBOX_STATUS_RETRY",
                table: "NOTIFICATION_OUTBOX");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FILE_STORAGE_PROVIDERS",
                table: "FILE_STORAGE_PROVIDERS");

            migrationBuilder.DropIndex(
                name: "UX_DOC_RELATIONS_SRC_TGT_TYPE",
                table: "DOCUMENT_RELATIONS");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DOCUMENT_RELATION_TYPES",
                table: "DOCUMENT_RELATION_TYPES");

            migrationBuilder.DropIndex(
                name: "IX_DOC_MOVEMENTS_DEST_STATUS",
                table: "DOCUMENT_MOVEMENTS");

            migrationBuilder.DropIndex(
                name: "IX_DOC_MOVEMENTS_DOC_STATUS",
                table: "DOCUMENT_MOVEMENTS");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DOCUMENT_FILE_TYPES",
                table: "DOCUMENT_FILE_TYPES");

            migrationBuilder.DropColumn(
                name: "ScopeType",
                table: "USERS");

            migrationBuilder.DropColumn(
                name: "AssignmentType",
                table: "USER_DEPARTMENT_ASSIGNMENTS");

            migrationBuilder.DropColumn(
                name: "NotificationTypeCode",
                table: "NOTIFICATIONS");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "NOTIFICATIONS");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "NOTIFICATION_TYPES");

            migrationBuilder.DropColumn(
                name: "Channel",
                table: "NOTIFICATION_OUTBOX");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "NOTIFICATION_OUTBOX");

            migrationBuilder.DropColumn(
                name: "NumberingMode",
                table: "INSTITUTION_SETTINGS");

            migrationBuilder.DropColumn(
                name: "NumberingResetPolicy",
                table: "INSTITUTION_SETTINGS");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "FILE_STORAGE_PROVIDERS");

            migrationBuilder.DropColumn(
                name: "ProviderType",
                table: "FILE_STORAGE_PROVIDERS");

            migrationBuilder.DropColumn(
                name: "OrganizationType",
                table: "EXTERNAL_ORGANIZATIONS");

            migrationBuilder.DropColumn(
                name: "RelationTypeCode",
                table: "DOCUMENT_RELATIONS");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "DOCUMENT_RELATION_TYPES");

            migrationBuilder.DropColumn(
                name: "NameEn",
                table: "DOCUMENT_RELATION_TYPES");

            migrationBuilder.DropColumn(
                name: "ActionType",
                table: "DOCUMENT_RELATION_EVENTS");

            migrationBuilder.DropColumn(
                name: "RelationTypeCode",
                table: "DOCUMENT_RELATION_EVENTS");

            migrationBuilder.DropColumn(
                name: "RoleCode",
                table: "DOCUMENT_PARTICIPANTS");

            migrationBuilder.DropColumn(
                name: "TargetType",
                table: "DOCUMENT_PARTICIPANTS");

            migrationBuilder.DropColumn(
                name: "MovementType",
                table: "DOCUMENT_MOVEMENTS");

            migrationBuilder.DropColumn(
                name: "StatusCode",
                table: "DOCUMENT_MOVEMENTS");

            migrationBuilder.DropColumn(
                name: "StepStatus",
                table: "DOCUMENT_MOVEMENT_STEPS");

            migrationBuilder.DropColumn(
                name: "EventType",
                table: "DOCUMENT_MOVEMENT_EVENTS");

            migrationBuilder.DropColumn(
                name: "FileTypeCode",
                table: "DOCUMENT_FILES");

            migrationBuilder.DropColumn(
                name: "StorageProviderCode",
                table: "DOCUMENT_FILE_VERSIONS");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "DOCUMENT_FILE_TYPES");

            migrationBuilder.DropColumn(
                name: "NameEn",
                table: "DOCUMENT_FILE_TYPES");

            migrationBuilder.DropColumn(
                name: "ArchiveLocationCode",
                table: "DOCUMENT_ARCHIVE_EVENTS");

            migrationBuilder.DropColumn(
                name: "EventType",
                table: "DOCUMENT_ARCHIVE_EVENTS");

            migrationBuilder.DropColumn(
                name: "NewStatusCode",
                table: "DOCUMENT_ARCHIVE_EVENTS");

            migrationBuilder.DropColumn(
                name: "PreviousStatusCode",
                table: "DOCUMENT_ARCHIVE_EVENTS");

            migrationBuilder.DropColumn(
                name: "AccessLevel",
                table: "DOCUMENT_ACCESS_GRANTS");

            migrationBuilder.DropColumn(
                name: "ActionType",
                table: "AUDIT_LOGS");

            migrationBuilder.DropColumn(
                name: "EntityType",
                table: "AUDIT_LOGS");

            migrationBuilder.RenameColumn(
                name: "NameAr",
                table: "NOTIFICATION_TYPES",
                newName: "DescAr");

            migrationBuilder.RenameColumn(
                name: "NameAr",
                table: "FILE_STORAGE_PROVIDERS",
                newName: "DescAr");

            migrationBuilder.RenameColumn(
                name: "NameAr",
                table: "DOCUMENT_RELATION_TYPES",
                newName: "DescAr");

            migrationBuilder.RenameColumn(
                name: "NameAr",
                table: "DOCUMENT_FILE_TYPES",
                newName: "DescAr");

            migrationBuilder.AddColumn<int>(
                name: "ScopeTypeId",
                table: "USERS",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AssignmentTypeId",
                table: "USER_DEPARTMENT_ASSIGNMENTS",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NotificationTypeId",
                table: "NOTIFICATIONS",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PriorityId",
                table: "NOTIFICATIONS",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "NOTIFICATION_TYPES",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Desc",
                table: "NOTIFICATION_TYPES",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ChannelId",
                table: "NOTIFICATION_OUTBOX",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StatusId",
                table: "NOTIFICATION_OUTBOX",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NumberingModeId",
                table: "INSTITUTION_SETTINGS",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NumberingResetPolicyId",
                table: "INSTITUTION_SETTINGS",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "FILE_STORAGE_PROVIDERS",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Desc",
                table: "FILE_STORAGE_PROVIDERS",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ProviderTypeId",
                table: "FILE_STORAGE_PROVIDERS",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationTypeId",
                table: "EXTERNAL_ORGANIZATIONS",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RelationTypeId",
                table: "DOCUMENT_RELATIONS",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "DOCUMENT_RELATION_TYPES",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Desc",
                table: "DOCUMENT_RELATION_TYPES",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ActionTypeId",
                table: "DOCUMENT_RELATION_EVENTS",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RelationTypeId",
                table: "DOCUMENT_RELATION_EVENTS",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RoleId",
                table: "DOCUMENT_PARTICIPANTS",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TargetTypeId",
                table: "DOCUMENT_PARTICIPANTS",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MovementTypeId",
                table: "DOCUMENT_MOVEMENTS",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StatusId",
                table: "DOCUMENT_MOVEMENTS",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StepStatusId",
                table: "DOCUMENT_MOVEMENT_STEPS",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EventTypeId",
                table: "DOCUMENT_MOVEMENT_EVENTS",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FileTypeId",
                table: "DOCUMENT_FILES",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StorageProviderId",
                table: "DOCUMENT_FILE_VERSIONS",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "DOCUMENT_FILE_TYPES",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Desc",
                table: "DOCUMENT_FILE_TYPES",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ArchiveLocationId",
                table: "DOCUMENT_ARCHIVE_EVENTS",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EventTypeId",
                table: "DOCUMENT_ARCHIVE_EVENTS",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NewStatusId",
                table: "DOCUMENT_ARCHIVE_EVENTS",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PreviousStatusId",
                table: "DOCUMENT_ARCHIVE_EVENTS",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AccessLevelId",
                table: "DOCUMENT_ACCESS_GRANTS",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ActionTypeId",
                table: "AUDIT_LOGS",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EntityTypeId",
                table: "AUDIT_LOGS",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_NOTIFICATION_TYPES",
                table: "NOTIFICATION_TYPES",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FILE_STORAGE_PROVIDERS",
                table: "FILE_STORAGE_PROVIDERS",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DOCUMENT_RELATION_TYPES",
                table: "DOCUMENT_RELATION_TYPES",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DOCUMENT_FILE_TYPES",
                table: "DOCUMENT_FILE_TYPES",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "ACCESS_LEVELS",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ACCESS_LEVELS", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ARCHIVE_EVENT_TYPES",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ARCHIVE_EVENT_TYPES", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ARCHIVE_LOCATIONS",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ARCHIVE_LOCATIONS", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ASSIGNMENT_TYPES",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ASSIGNMENT_TYPES", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AUDIT_ACTION_TYPES",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AUDIT_ACTION_TYPES", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AUDIT_ENTITY_TYPES",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AUDIT_ENTITY_TYPES", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MOVEMENT_EVENT_TYPES",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MOVEMENT_EVENT_TYPES", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MOVEMENT_STATUSES",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MOVEMENT_STATUSES", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MOVEMENT_STEP_STATUSES",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MOVEMENT_STEP_STATUSES", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MOVEMENT_TYPES",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MOVEMENT_TYPES", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NOTIFICATION_CHANNELS",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NOTIFICATION_CHANNELS", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NOTIFICATION_OUTBOX_STATUSES",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NOTIFICATION_OUTBOX_STATUSES", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NOTIFICATION_PRIORITIES",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NOTIFICATION_PRIORITIES", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NUMBERING_MODES",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NUMBERING_MODES", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ORGANIZATION_TYPES",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ORGANIZATION_TYPES", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PARTICIPANT_ROLES",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PARTICIPANT_ROLES", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PARTICIPANT_TARGET_TYPES",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PARTICIPANT_TARGET_TYPES", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RELATION_ACTION_TYPES",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RELATION_ACTION_TYPES", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "STORAGE_PROVIDER_TYPES",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_STORAGE_PROVIDER_TYPES", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "USER_SCOPE_TYPES",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USER_SCOPE_TYPES", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_USERS_ScopeTypeId",
                table: "USERS",
                column: "ScopeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_USER_DEPARTMENT_ASSIGNMENTS_AssignmentTypeId",
                table: "USER_DEPARTMENT_ASSIGNMENTS",
                column: "AssignmentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_NOTIFICATIONS_NotificationTypeId",
                table: "NOTIFICATIONS",
                column: "NotificationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_NOTIFICATIONS_PriorityId",
                table: "NOTIFICATIONS",
                column: "PriorityId");

            migrationBuilder.CreateIndex(
                name: "IX_NOTIFICATION_OUTBOX_ChannelId",
                table: "NOTIFICATION_OUTBOX",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_NOTIFICATION_OUTBOX_STATUS_RETRY",
                table: "NOTIFICATION_OUTBOX",
                columns: new[] { "StatusId", "NextRetryAt" });

            migrationBuilder.CreateIndex(
                name: "IX_INSTITUTION_SETTINGS_NumberingModeId",
                table: "INSTITUTION_SETTINGS",
                column: "NumberingModeId");

            migrationBuilder.CreateIndex(
                name: "IX_INSTITUTION_SETTINGS_NumberingResetPolicyId",
                table: "INSTITUTION_SETTINGS",
                column: "NumberingResetPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_FILE_STORAGE_PROVIDERS_ProviderTypeId",
                table: "FILE_STORAGE_PROVIDERS",
                column: "ProviderTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EXTERNAL_ORGANIZATIONS_OrganizationTypeId",
                table: "EXTERNAL_ORGANIZATIONS",
                column: "OrganizationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_RELATIONS_RelationTypeId",
                table: "DOCUMENT_RELATIONS",
                column: "RelationTypeId");

            migrationBuilder.CreateIndex(
                name: "UX_DOC_RELATIONS_SRC_TGT_TYPE",
                table: "DOCUMENT_RELATIONS",
                columns: new[] { "SourceDocumentId", "TargetDocumentId", "RelationTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_RELATION_EVENTS_ActionTypeId",
                table: "DOCUMENT_RELATION_EVENTS",
                column: "ActionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_RELATION_EVENTS_RelationTypeId",
                table: "DOCUMENT_RELATION_EVENTS",
                column: "RelationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_PARTICIPANTS_RoleId",
                table: "DOCUMENT_PARTICIPANTS",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_PARTICIPANTS_TargetTypeId",
                table: "DOCUMENT_PARTICIPANTS",
                column: "TargetTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DOC_MOVEMENTS_DEST_STATUS",
                table: "DOCUMENT_MOVEMENTS",
                columns: new[] { "DestinationDepartmentId", "StatusId" });

            migrationBuilder.CreateIndex(
                name: "IX_DOC_MOVEMENTS_DOC_STATUS",
                table: "DOCUMENT_MOVEMENTS",
                columns: new[] { "DocumentId", "StatusId" });

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_MOVEMENTS_MovementTypeId",
                table: "DOCUMENT_MOVEMENTS",
                column: "MovementTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_MOVEMENTS_StatusId",
                table: "DOCUMENT_MOVEMENTS",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_MOVEMENT_STEPS_StepStatusId",
                table: "DOCUMENT_MOVEMENT_STEPS",
                column: "StepStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_MOVEMENT_EVENTS_EventTypeId",
                table: "DOCUMENT_MOVEMENT_EVENTS",
                column: "EventTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_FILES_FileTypeId",
                table: "DOCUMENT_FILES",
                column: "FileTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_FILE_VERSIONS_StorageProviderId",
                table: "DOCUMENT_FILE_VERSIONS",
                column: "StorageProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_ARCHIVE_EVENTS_ArchiveLocationId",
                table: "DOCUMENT_ARCHIVE_EVENTS",
                column: "ArchiveLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_ARCHIVE_EVENTS_EventTypeId",
                table: "DOCUMENT_ARCHIVE_EVENTS",
                column: "EventTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_ACCESS_GRANTS_AccessLevelId",
                table: "DOCUMENT_ACCESS_GRANTS",
                column: "AccessLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_AUDIT_LOGS_ActionTypeId",
                table: "AUDIT_LOGS",
                column: "ActionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_AUDIT_LOGS_ENTITY",
                table: "AUDIT_LOGS",
                columns: new[] { "EntityTypeId", "EntityId" });

            migrationBuilder.AddForeignKey(
                name: "FK_AUDIT_LOGS_AUDIT_ACTION_TYPES_ActionTypeId",
                table: "AUDIT_LOGS",
                column: "ActionTypeId",
                principalTable: "AUDIT_ACTION_TYPES",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AUDIT_LOGS_AUDIT_ENTITY_TYPES_EntityTypeId",
                table: "AUDIT_LOGS",
                column: "EntityTypeId",
                principalTable: "AUDIT_ENTITY_TYPES",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENT_ACCESS_GRANTS_ACCESS_LEVELS_AccessLevelId",
                table: "DOCUMENT_ACCESS_GRANTS",
                column: "AccessLevelId",
                principalTable: "ACCESS_LEVELS",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENT_ARCHIVE_EVENTS_ARCHIVE_EVENT_TYPES_EventTypeId",
                table: "DOCUMENT_ARCHIVE_EVENTS",
                column: "EventTypeId",
                principalTable: "ARCHIVE_EVENT_TYPES",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENT_ARCHIVE_EVENTS_ARCHIVE_LOCATIONS_ArchiveLocationId",
                table: "DOCUMENT_ARCHIVE_EVENTS",
                column: "ArchiveLocationId",
                principalTable: "ARCHIVE_LOCATIONS",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENT_FILE_VERSIONS_FILE_STORAGE_PROVIDERS_StorageProviderId",
                table: "DOCUMENT_FILE_VERSIONS",
                column: "StorageProviderId",
                principalTable: "FILE_STORAGE_PROVIDERS",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENT_FILES_DOCUMENT_FILE_TYPES_FileTypeId",
                table: "DOCUMENT_FILES",
                column: "FileTypeId",
                principalTable: "DOCUMENT_FILE_TYPES",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENT_MOVEMENT_EVENTS_MOVEMENT_EVENT_TYPES_EventTypeId",
                table: "DOCUMENT_MOVEMENT_EVENTS",
                column: "EventTypeId",
                principalTable: "MOVEMENT_EVENT_TYPES",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENT_MOVEMENT_STEPS_MOVEMENT_STEP_STATUSES_StepStatusId",
                table: "DOCUMENT_MOVEMENT_STEPS",
                column: "StepStatusId",
                principalTable: "MOVEMENT_STEP_STATUSES",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENT_MOVEMENTS_MOVEMENT_STATUSES_StatusId",
                table: "DOCUMENT_MOVEMENTS",
                column: "StatusId",
                principalTable: "MOVEMENT_STATUSES",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENT_MOVEMENTS_MOVEMENT_TYPES_MovementTypeId",
                table: "DOCUMENT_MOVEMENTS",
                column: "MovementTypeId",
                principalTable: "MOVEMENT_TYPES",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENT_PARTICIPANTS_PARTICIPANT_ROLES_RoleId",
                table: "DOCUMENT_PARTICIPANTS",
                column: "RoleId",
                principalTable: "PARTICIPANT_ROLES",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENT_PARTICIPANTS_PARTICIPANT_TARGET_TYPES_TargetTypeId",
                table: "DOCUMENT_PARTICIPANTS",
                column: "TargetTypeId",
                principalTable: "PARTICIPANT_TARGET_TYPES",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENT_RELATION_EVENTS_DOCUMENT_RELATION_TYPES_RelationTypeId",
                table: "DOCUMENT_RELATION_EVENTS",
                column: "RelationTypeId",
                principalTable: "DOCUMENT_RELATION_TYPES",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENT_RELATION_EVENTS_RELATION_ACTION_TYPES_ActionTypeId",
                table: "DOCUMENT_RELATION_EVENTS",
                column: "ActionTypeId",
                principalTable: "RELATION_ACTION_TYPES",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENT_RELATIONS_DOCUMENT_RELATION_TYPES_RelationTypeId",
                table: "DOCUMENT_RELATIONS",
                column: "RelationTypeId",
                principalTable: "DOCUMENT_RELATION_TYPES",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EXTERNAL_ORGANIZATIONS_ORGANIZATION_TYPES_OrganizationTypeId",
                table: "EXTERNAL_ORGANIZATIONS",
                column: "OrganizationTypeId",
                principalTable: "ORGANIZATION_TYPES",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FILE_STORAGE_PROVIDERS_STORAGE_PROVIDER_TYPES_ProviderTypeId",
                table: "FILE_STORAGE_PROVIDERS",
                column: "ProviderTypeId",
                principalTable: "STORAGE_PROVIDER_TYPES",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_INSTITUTION_SETTINGS_NUMBERING_MODES_NumberingModeId",
                table: "INSTITUTION_SETTINGS",
                column: "NumberingModeId",
                principalTable: "NUMBERING_MODES",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_INSTITUTION_SETTINGS_NUMBERING_RESET_POLICIES_NumberingResetPolicyId",
                table: "INSTITUTION_SETTINGS",
                column: "NumberingResetPolicyId",
                principalTable: "NUMBERING_RESET_POLICIES",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NOTIFICATION_OUTBOX_NOTIFICATION_CHANNELS_ChannelId",
                table: "NOTIFICATION_OUTBOX",
                column: "ChannelId",
                principalTable: "NOTIFICATION_CHANNELS",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NOTIFICATION_OUTBOX_NOTIFICATION_OUTBOX_STATUSES_StatusId",
                table: "NOTIFICATION_OUTBOX",
                column: "StatusId",
                principalTable: "NOTIFICATION_OUTBOX_STATUSES",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NOTIFICATIONS_NOTIFICATION_PRIORITIES_PriorityId",
                table: "NOTIFICATIONS",
                column: "PriorityId",
                principalTable: "NOTIFICATION_PRIORITIES",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NOTIFICATIONS_NOTIFICATION_TYPES_NotificationTypeId",
                table: "NOTIFICATIONS",
                column: "NotificationTypeId",
                principalTable: "NOTIFICATION_TYPES",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_USER_DEPARTMENT_ASSIGNMENTS_ASSIGNMENT_TYPES_AssignmentTypeId",
                table: "USER_DEPARTMENT_ASSIGNMENTS",
                column: "AssignmentTypeId",
                principalTable: "ASSIGNMENT_TYPES",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_USERS_USER_SCOPE_TYPES_ScopeTypeId",
                table: "USERS",
                column: "ScopeTypeId",
                principalTable: "USER_SCOPE_TYPES",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AUDIT_LOGS_AUDIT_ACTION_TYPES_ActionTypeId",
                table: "AUDIT_LOGS");

            migrationBuilder.DropForeignKey(
                name: "FK_AUDIT_LOGS_AUDIT_ENTITY_TYPES_EntityTypeId",
                table: "AUDIT_LOGS");

            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENT_ACCESS_GRANTS_ACCESS_LEVELS_AccessLevelId",
                table: "DOCUMENT_ACCESS_GRANTS");

            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENT_ARCHIVE_EVENTS_ARCHIVE_EVENT_TYPES_EventTypeId",
                table: "DOCUMENT_ARCHIVE_EVENTS");

            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENT_ARCHIVE_EVENTS_ARCHIVE_LOCATIONS_ArchiveLocationId",
                table: "DOCUMENT_ARCHIVE_EVENTS");

            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENT_FILE_VERSIONS_FILE_STORAGE_PROVIDERS_StorageProviderId",
                table: "DOCUMENT_FILE_VERSIONS");

            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENT_FILES_DOCUMENT_FILE_TYPES_FileTypeId",
                table: "DOCUMENT_FILES");

            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENT_MOVEMENT_EVENTS_MOVEMENT_EVENT_TYPES_EventTypeId",
                table: "DOCUMENT_MOVEMENT_EVENTS");

            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENT_MOVEMENT_STEPS_MOVEMENT_STEP_STATUSES_StepStatusId",
                table: "DOCUMENT_MOVEMENT_STEPS");

            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENT_MOVEMENTS_MOVEMENT_STATUSES_StatusId",
                table: "DOCUMENT_MOVEMENTS");

            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENT_MOVEMENTS_MOVEMENT_TYPES_MovementTypeId",
                table: "DOCUMENT_MOVEMENTS");

            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENT_PARTICIPANTS_PARTICIPANT_ROLES_RoleId",
                table: "DOCUMENT_PARTICIPANTS");

            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENT_PARTICIPANTS_PARTICIPANT_TARGET_TYPES_TargetTypeId",
                table: "DOCUMENT_PARTICIPANTS");

            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENT_RELATION_EVENTS_DOCUMENT_RELATION_TYPES_RelationTypeId",
                table: "DOCUMENT_RELATION_EVENTS");

            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENT_RELATION_EVENTS_RELATION_ACTION_TYPES_ActionTypeId",
                table: "DOCUMENT_RELATION_EVENTS");

            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENT_RELATIONS_DOCUMENT_RELATION_TYPES_RelationTypeId",
                table: "DOCUMENT_RELATIONS");

            migrationBuilder.DropForeignKey(
                name: "FK_EXTERNAL_ORGANIZATIONS_ORGANIZATION_TYPES_OrganizationTypeId",
                table: "EXTERNAL_ORGANIZATIONS");

            migrationBuilder.DropForeignKey(
                name: "FK_FILE_STORAGE_PROVIDERS_STORAGE_PROVIDER_TYPES_ProviderTypeId",
                table: "FILE_STORAGE_PROVIDERS");

            migrationBuilder.DropForeignKey(
                name: "FK_INSTITUTION_SETTINGS_NUMBERING_MODES_NumberingModeId",
                table: "INSTITUTION_SETTINGS");

            migrationBuilder.DropForeignKey(
                name: "FK_INSTITUTION_SETTINGS_NUMBERING_RESET_POLICIES_NumberingResetPolicyId",
                table: "INSTITUTION_SETTINGS");

            migrationBuilder.DropForeignKey(
                name: "FK_NOTIFICATION_OUTBOX_NOTIFICATION_CHANNELS_ChannelId",
                table: "NOTIFICATION_OUTBOX");

            migrationBuilder.DropForeignKey(
                name: "FK_NOTIFICATION_OUTBOX_NOTIFICATION_OUTBOX_STATUSES_StatusId",
                table: "NOTIFICATION_OUTBOX");

            migrationBuilder.DropForeignKey(
                name: "FK_NOTIFICATIONS_NOTIFICATION_PRIORITIES_PriorityId",
                table: "NOTIFICATIONS");

            migrationBuilder.DropForeignKey(
                name: "FK_NOTIFICATIONS_NOTIFICATION_TYPES_NotificationTypeId",
                table: "NOTIFICATIONS");

            migrationBuilder.DropForeignKey(
                name: "FK_USER_DEPARTMENT_ASSIGNMENTS_ASSIGNMENT_TYPES_AssignmentTypeId",
                table: "USER_DEPARTMENT_ASSIGNMENTS");

            migrationBuilder.DropForeignKey(
                name: "FK_USERS_USER_SCOPE_TYPES_ScopeTypeId",
                table: "USERS");

            migrationBuilder.DropTable(
                name: "ACCESS_LEVELS");

            migrationBuilder.DropTable(
                name: "ARCHIVE_EVENT_TYPES");

            migrationBuilder.DropTable(
                name: "ARCHIVE_LOCATIONS");

            migrationBuilder.DropTable(
                name: "ASSIGNMENT_TYPES");

            migrationBuilder.DropTable(
                name: "AUDIT_ACTION_TYPES");

            migrationBuilder.DropTable(
                name: "AUDIT_ENTITY_TYPES");

            migrationBuilder.DropTable(
                name: "MOVEMENT_EVENT_TYPES");

            migrationBuilder.DropTable(
                name: "MOVEMENT_STATUSES");

            migrationBuilder.DropTable(
                name: "MOVEMENT_STEP_STATUSES");

            migrationBuilder.DropTable(
                name: "MOVEMENT_TYPES");

            migrationBuilder.DropTable(
                name: "NOTIFICATION_CHANNELS");

            migrationBuilder.DropTable(
                name: "NOTIFICATION_OUTBOX_STATUSES");

            migrationBuilder.DropTable(
                name: "NOTIFICATION_PRIORITIES");

            migrationBuilder.DropTable(
                name: "NUMBERING_MODES");

            migrationBuilder.DropTable(
                name: "ORGANIZATION_TYPES");

            migrationBuilder.DropTable(
                name: "PARTICIPANT_ROLES");

            migrationBuilder.DropTable(
                name: "PARTICIPANT_TARGET_TYPES");

            migrationBuilder.DropTable(
                name: "RELATION_ACTION_TYPES");

            migrationBuilder.DropTable(
                name: "STORAGE_PROVIDER_TYPES");

            migrationBuilder.DropTable(
                name: "USER_SCOPE_TYPES");

            migrationBuilder.DropIndex(
                name: "IX_USERS_ScopeTypeId",
                table: "USERS");

            migrationBuilder.DropIndex(
                name: "IX_USER_DEPARTMENT_ASSIGNMENTS_AssignmentTypeId",
                table: "USER_DEPARTMENT_ASSIGNMENTS");

            migrationBuilder.DropIndex(
                name: "IX_NOTIFICATIONS_NotificationTypeId",
                table: "NOTIFICATIONS");

            migrationBuilder.DropIndex(
                name: "IX_NOTIFICATIONS_PriorityId",
                table: "NOTIFICATIONS");

            migrationBuilder.DropPrimaryKey(
                name: "PK_NOTIFICATION_TYPES",
                table: "NOTIFICATION_TYPES");

            migrationBuilder.DropIndex(
                name: "IX_NOTIFICATION_OUTBOX_ChannelId",
                table: "NOTIFICATION_OUTBOX");

            migrationBuilder.DropIndex(
                name: "IX_NOTIFICATION_OUTBOX_STATUS_RETRY",
                table: "NOTIFICATION_OUTBOX");

            migrationBuilder.DropIndex(
                name: "IX_INSTITUTION_SETTINGS_NumberingModeId",
                table: "INSTITUTION_SETTINGS");

            migrationBuilder.DropIndex(
                name: "IX_INSTITUTION_SETTINGS_NumberingResetPolicyId",
                table: "INSTITUTION_SETTINGS");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FILE_STORAGE_PROVIDERS",
                table: "FILE_STORAGE_PROVIDERS");

            migrationBuilder.DropIndex(
                name: "IX_FILE_STORAGE_PROVIDERS_ProviderTypeId",
                table: "FILE_STORAGE_PROVIDERS");

            migrationBuilder.DropIndex(
                name: "IX_EXTERNAL_ORGANIZATIONS_OrganizationTypeId",
                table: "EXTERNAL_ORGANIZATIONS");

            migrationBuilder.DropIndex(
                name: "IX_DOCUMENT_RELATIONS_RelationTypeId",
                table: "DOCUMENT_RELATIONS");

            migrationBuilder.DropIndex(
                name: "UX_DOC_RELATIONS_SRC_TGT_TYPE",
                table: "DOCUMENT_RELATIONS");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DOCUMENT_RELATION_TYPES",
                table: "DOCUMENT_RELATION_TYPES");

            migrationBuilder.DropIndex(
                name: "IX_DOCUMENT_RELATION_EVENTS_ActionTypeId",
                table: "DOCUMENT_RELATION_EVENTS");

            migrationBuilder.DropIndex(
                name: "IX_DOCUMENT_RELATION_EVENTS_RelationTypeId",
                table: "DOCUMENT_RELATION_EVENTS");

            migrationBuilder.DropIndex(
                name: "IX_DOCUMENT_PARTICIPANTS_RoleId",
                table: "DOCUMENT_PARTICIPANTS");

            migrationBuilder.DropIndex(
                name: "IX_DOCUMENT_PARTICIPANTS_TargetTypeId",
                table: "DOCUMENT_PARTICIPANTS");

            migrationBuilder.DropIndex(
                name: "IX_DOC_MOVEMENTS_DEST_STATUS",
                table: "DOCUMENT_MOVEMENTS");

            migrationBuilder.DropIndex(
                name: "IX_DOC_MOVEMENTS_DOC_STATUS",
                table: "DOCUMENT_MOVEMENTS");

            migrationBuilder.DropIndex(
                name: "IX_DOCUMENT_MOVEMENTS_MovementTypeId",
                table: "DOCUMENT_MOVEMENTS");

            migrationBuilder.DropIndex(
                name: "IX_DOCUMENT_MOVEMENTS_StatusId",
                table: "DOCUMENT_MOVEMENTS");

            migrationBuilder.DropIndex(
                name: "IX_DOCUMENT_MOVEMENT_STEPS_StepStatusId",
                table: "DOCUMENT_MOVEMENT_STEPS");

            migrationBuilder.DropIndex(
                name: "IX_DOCUMENT_MOVEMENT_EVENTS_EventTypeId",
                table: "DOCUMENT_MOVEMENT_EVENTS");

            migrationBuilder.DropIndex(
                name: "IX_DOCUMENT_FILES_FileTypeId",
                table: "DOCUMENT_FILES");

            migrationBuilder.DropIndex(
                name: "IX_DOCUMENT_FILE_VERSIONS_StorageProviderId",
                table: "DOCUMENT_FILE_VERSIONS");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DOCUMENT_FILE_TYPES",
                table: "DOCUMENT_FILE_TYPES");

            migrationBuilder.DropIndex(
                name: "IX_DOCUMENT_ARCHIVE_EVENTS_ArchiveLocationId",
                table: "DOCUMENT_ARCHIVE_EVENTS");

            migrationBuilder.DropIndex(
                name: "IX_DOCUMENT_ARCHIVE_EVENTS_EventTypeId",
                table: "DOCUMENT_ARCHIVE_EVENTS");

            migrationBuilder.DropIndex(
                name: "IX_DOCUMENT_ACCESS_GRANTS_AccessLevelId",
                table: "DOCUMENT_ACCESS_GRANTS");

            migrationBuilder.DropIndex(
                name: "IX_AUDIT_LOGS_ActionTypeId",
                table: "AUDIT_LOGS");

            migrationBuilder.DropIndex(
                name: "IX_AUDIT_LOGS_ENTITY",
                table: "AUDIT_LOGS");

            migrationBuilder.DropColumn(
                name: "ScopeTypeId",
                table: "USERS");

            migrationBuilder.DropColumn(
                name: "AssignmentTypeId",
                table: "USER_DEPARTMENT_ASSIGNMENTS");

            migrationBuilder.DropColumn(
                name: "NotificationTypeId",
                table: "NOTIFICATIONS");

            migrationBuilder.DropColumn(
                name: "PriorityId",
                table: "NOTIFICATIONS");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "NOTIFICATION_TYPES");

            migrationBuilder.DropColumn(
                name: "Desc",
                table: "NOTIFICATION_TYPES");

            migrationBuilder.DropColumn(
                name: "ChannelId",
                table: "NOTIFICATION_OUTBOX");

            migrationBuilder.DropColumn(
                name: "StatusId",
                table: "NOTIFICATION_OUTBOX");

            migrationBuilder.DropColumn(
                name: "NumberingModeId",
                table: "INSTITUTION_SETTINGS");

            migrationBuilder.DropColumn(
                name: "NumberingResetPolicyId",
                table: "INSTITUTION_SETTINGS");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "FILE_STORAGE_PROVIDERS");

            migrationBuilder.DropColumn(
                name: "Desc",
                table: "FILE_STORAGE_PROVIDERS");

            migrationBuilder.DropColumn(
                name: "ProviderTypeId",
                table: "FILE_STORAGE_PROVIDERS");

            migrationBuilder.DropColumn(
                name: "OrganizationTypeId",
                table: "EXTERNAL_ORGANIZATIONS");

            migrationBuilder.DropColumn(
                name: "RelationTypeId",
                table: "DOCUMENT_RELATIONS");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "DOCUMENT_RELATION_TYPES");

            migrationBuilder.DropColumn(
                name: "Desc",
                table: "DOCUMENT_RELATION_TYPES");

            migrationBuilder.DropColumn(
                name: "ActionTypeId",
                table: "DOCUMENT_RELATION_EVENTS");

            migrationBuilder.DropColumn(
                name: "RelationTypeId",
                table: "DOCUMENT_RELATION_EVENTS");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "DOCUMENT_PARTICIPANTS");

            migrationBuilder.DropColumn(
                name: "TargetTypeId",
                table: "DOCUMENT_PARTICIPANTS");

            migrationBuilder.DropColumn(
                name: "MovementTypeId",
                table: "DOCUMENT_MOVEMENTS");

            migrationBuilder.DropColumn(
                name: "StatusId",
                table: "DOCUMENT_MOVEMENTS");

            migrationBuilder.DropColumn(
                name: "StepStatusId",
                table: "DOCUMENT_MOVEMENT_STEPS");

            migrationBuilder.DropColumn(
                name: "EventTypeId",
                table: "DOCUMENT_MOVEMENT_EVENTS");

            migrationBuilder.DropColumn(
                name: "FileTypeId",
                table: "DOCUMENT_FILES");

            migrationBuilder.DropColumn(
                name: "StorageProviderId",
                table: "DOCUMENT_FILE_VERSIONS");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "DOCUMENT_FILE_TYPES");

            migrationBuilder.DropColumn(
                name: "Desc",
                table: "DOCUMENT_FILE_TYPES");

            migrationBuilder.DropColumn(
                name: "ArchiveLocationId",
                table: "DOCUMENT_ARCHIVE_EVENTS");

            migrationBuilder.DropColumn(
                name: "EventTypeId",
                table: "DOCUMENT_ARCHIVE_EVENTS");

            migrationBuilder.DropColumn(
                name: "NewStatusId",
                table: "DOCUMENT_ARCHIVE_EVENTS");

            migrationBuilder.DropColumn(
                name: "PreviousStatusId",
                table: "DOCUMENT_ARCHIVE_EVENTS");

            migrationBuilder.DropColumn(
                name: "AccessLevelId",
                table: "DOCUMENT_ACCESS_GRANTS");

            migrationBuilder.DropColumn(
                name: "ActionTypeId",
                table: "AUDIT_LOGS");

            migrationBuilder.DropColumn(
                name: "EntityTypeId",
                table: "AUDIT_LOGS");

            migrationBuilder.RenameColumn(
                name: "DescAr",
                table: "NOTIFICATION_TYPES",
                newName: "NameAr");

            migrationBuilder.RenameColumn(
                name: "DescAr",
                table: "FILE_STORAGE_PROVIDERS",
                newName: "NameAr");

            migrationBuilder.RenameColumn(
                name: "DescAr",
                table: "DOCUMENT_RELATION_TYPES",
                newName: "NameAr");

            migrationBuilder.RenameColumn(
                name: "DescAr",
                table: "DOCUMENT_FILE_TYPES",
                newName: "NameAr");

            migrationBuilder.AddColumn<string>(
                name: "ScopeType",
                table: "USERS",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AssignmentType",
                table: "USER_DEPARTMENT_ASSIGNMENTS",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NotificationTypeCode",
                table: "NOTIFICATIONS",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "NOTIFICATIONS",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "NOTIFICATION_TYPES",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Channel",
                table: "NOTIFICATION_OUTBOX",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "NOTIFICATION_OUTBOX",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NumberingMode",
                table: "INSTITUTION_SETTINGS",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NumberingResetPolicy",
                table: "INSTITUTION_SETTINGS",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "FILE_STORAGE_PROVIDERS",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProviderType",
                table: "FILE_STORAGE_PROVIDERS",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OrganizationType",
                table: "EXTERNAL_ORGANIZATIONS",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RelationTypeCode",
                table: "DOCUMENT_RELATIONS",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "DOCUMENT_RELATION_TYPES",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameEn",
                table: "DOCUMENT_RELATION_TYPES",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActionType",
                table: "DOCUMENT_RELATION_EVENTS",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RelationTypeCode",
                table: "DOCUMENT_RELATION_EVENTS",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RoleCode",
                table: "DOCUMENT_PARTICIPANTS",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TargetType",
                table: "DOCUMENT_PARTICIPANTS",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MovementType",
                table: "DOCUMENT_MOVEMENTS",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StatusCode",
                table: "DOCUMENT_MOVEMENTS",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StepStatus",
                table: "DOCUMENT_MOVEMENT_STEPS",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EventType",
                table: "DOCUMENT_MOVEMENT_EVENTS",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FileTypeCode",
                table: "DOCUMENT_FILES",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StorageProviderCode",
                table: "DOCUMENT_FILE_VERSIONS",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "DOCUMENT_FILE_TYPES",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameEn",
                table: "DOCUMENT_FILE_TYPES",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArchiveLocationCode",
                table: "DOCUMENT_ARCHIVE_EVENTS",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EventType",
                table: "DOCUMENT_ARCHIVE_EVENTS",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NewStatusCode",
                table: "DOCUMENT_ARCHIVE_EVENTS",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PreviousStatusCode",
                table: "DOCUMENT_ARCHIVE_EVENTS",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AccessLevel",
                table: "DOCUMENT_ACCESS_GRANTS",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ActionType",
                table: "AUDIT_LOGS",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EntityType",
                table: "AUDIT_LOGS",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_NOTIFICATION_TYPES",
                table: "NOTIFICATION_TYPES",
                column: "Code");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FILE_STORAGE_PROVIDERS",
                table: "FILE_STORAGE_PROVIDERS",
                column: "Code");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DOCUMENT_RELATION_TYPES",
                table: "DOCUMENT_RELATION_TYPES",
                column: "Code");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DOCUMENT_FILE_TYPES",
                table: "DOCUMENT_FILE_TYPES",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_NOTIFICATION_OUTBOX_STATUS_RETRY",
                table: "NOTIFICATION_OUTBOX",
                columns: new[] { "Status", "NextRetryAt" });

            migrationBuilder.CreateIndex(
                name: "UX_DOC_RELATIONS_SRC_TGT_TYPE",
                table: "DOCUMENT_RELATIONS",
                columns: new[] { "SourceDocumentId", "TargetDocumentId", "RelationTypeCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DOC_MOVEMENTS_DEST_STATUS",
                table: "DOCUMENT_MOVEMENTS",
                columns: new[] { "DestinationDepartmentId", "StatusCode" });

            migrationBuilder.CreateIndex(
                name: "IX_DOC_MOVEMENTS_DOC_STATUS",
                table: "DOCUMENT_MOVEMENTS",
                columns: new[] { "DocumentId", "StatusCode" });
        }
    }
}
