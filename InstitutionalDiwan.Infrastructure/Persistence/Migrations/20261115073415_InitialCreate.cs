using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstitutionalDiwan.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261115073415_InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ACCESS_LEVELS",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false),
                Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                IsDefault = table.Column<bool>(type: "bit", nullable: false),
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
                IsDefault = table.Column<bool>(type: "bit", nullable: false),
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
                IsDefault = table.Column<bool>(type: "bit", nullable: false),
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
                IsDefault = table.Column<bool>(type: "bit", nullable: false),
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
                IsDefault = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AUDIT_ACTION_TYPES", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AUDIT_CHECKPOINTS",
            columns: table => new
            {
                CheckpointId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                FromAuditLogId = table.Column<long>(type: "bigint", nullable: false),
                ToAuditLogId = table.Column<long>(type: "bigint", nullable: false),
                TerminalHash = table.Column<string>(type: "char(64)", nullable: false),
                RecordCount = table.Column<long>(type: "bigint", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AUDIT_CHECKPOINTS", x => x.CheckpointId);
            });

        migrationBuilder.CreateTable(
            name: "AUDIT_ENTITY_TYPES",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false),
                Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                IsDefault = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AUDIT_ENTITY_TYPES", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AUDIT_INTEGRITY_CHECKS",
            columns: table => new
            {
                CheckId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                StartAuditLogId = table.Column<long>(type: "bigint", nullable: false),
                EndAuditLogId = table.Column<long>(type: "bigint", nullable: false),
                IsValid = table.Column<bool>(type: "bit", nullable: false),
                FirstCorruptedLogId = table.Column<long>(type: "bigint", nullable: true),
                FailureDetails = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                ExecutedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ExecutedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AUDIT_INTEGRITY_CHECKS", x => x.CheckId);
            });

        migrationBuilder.CreateTable(
            name: "DEPARTMENT_TYPES",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false),
                HierarchyLevelOrder = table.Column<int>(type: "int", nullable: false),
                Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                IsDefault = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DEPARTMENT_TYPES", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "DOCUMENT_CONFIDENTIALITY_LEVELS",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false),
                Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                IsDefault = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DOCUMENT_CONFIDENTIALITY_LEVELS", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "DOCUMENT_DIRECTIONS",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false),
                Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                IsDefault = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DOCUMENT_DIRECTIONS", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "DOCUMENT_FILE_TYPES",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false),
                AllowedExtensionsCsv = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                MaxFileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                IsDefault = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DOCUMENT_FILE_TYPES", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "DOCUMENT_NUMBER_EVENT_TYPES",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false),
                Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                IsDefault = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DOCUMENT_NUMBER_EVENT_TYPES", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "DOCUMENT_NUMBER_SCOPES",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false),
                Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                IsDefault = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DOCUMENT_NUMBER_SCOPES", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "DOCUMENT_NUMBER_STATUSES",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false),
                Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                IsDefault = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DOCUMENT_NUMBER_STATUSES", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "DOCUMENT_PERSON_ROLES",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false),
                Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                IsDefault = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DOCUMENT_PERSON_ROLES", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "DOCUMENT_PRIORITY_LEVELS",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false),
                Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                IsDefault = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DOCUMENT_PRIORITY_LEVELS", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "DOCUMENT_RELATION_TYPES",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false),
                IsSymmetric = table.Column<bool>(type: "bit", nullable: false),
                PreventCycles = table.Column<bool>(type: "bit", nullable: false),
                Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                IsDefault = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DOCUMENT_RELATION_TYPES", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "DOCUMENT_STATUS_TYPES",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false),
                IsTerminal = table.Column<bool>(type: "bit", nullable: false),
                IsSystemStatus = table.Column<bool>(type: "bit", nullable: false),
                Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                IsDefault = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DOCUMENT_STATUS_TYPES", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "EMPLOYEES",
            columns: table => new
            {
                EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                EmployeeNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                FullNameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                FullNameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                JobTitleAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EMPLOYEES", x => x.EmployeeId);
            });

        migrationBuilder.CreateTable(
            name: "GENDERS",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false),
                Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                IsDefault = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GENDERS", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "MOVEMENT_EVENT_TYPES",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false),
                Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                IsDefault = table.Column<bool>(type: "bit", nullable: false),
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
                IsDefault = table.Column<bool>(type: "bit", nullable: false),
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
                IsDefault = table.Column<bool>(type: "bit", nullable: false),
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
                IsDefault = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MOVEMENT_TYPES", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "NATIONALITIES",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false),
                Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                IsDefault = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NATIONALITIES", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "NOTIFICATION_CHANNELS",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false),
                Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                IsDefault = table.Column<bool>(type: "bit", nullable: false),
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
                IsDefault = table.Column<bool>(type: "bit", nullable: false),
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
                IsDefault = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NOTIFICATION_PRIORITIES", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "NOTIFICATION_TYPES",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false),
                TitleTemplateAr = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                BodyTemplateAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                DefaultChannelsCsv = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                IsDefault = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NOTIFICATION_TYPES", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "NUMBERING_MODES",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false),
                Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                IsDefault = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NUMBERING_MODES", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "NUMBERING_RESET_POLICIES",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false),
                Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                IsDefault = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NUMBERING_RESET_POLICIES", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "ORGANIZATION_TYPES",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false),
                Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                IsDefault = table.Column<bool>(type: "bit", nullable: false),
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
                IsDefault = table.Column<bool>(type: "bit", nullable: false),
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
                IsDefault = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PARTICIPANT_TARGET_TYPES", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "PERMISSIONS",
            columns: table => new
            {
                Code = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                ModuleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PERMISSIONS", x => x.Code);
            });

        migrationBuilder.CreateTable(
            name: "RELATION_ACTION_TYPES",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false),
                Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                IsDefault = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RELATION_ACTION_TYPES", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "ROLES",
            columns: table => new
            {
                RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                NameAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                NameEn = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                IsSystemRole = table.Column<bool>(type: "bit", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ROLES", x => x.RoleId);
            });

        migrationBuilder.CreateTable(
            name: "STORAGE_PROVIDER_TYPES",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false),
                Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                IsDefault = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_STORAGE_PROVIDER_TYPES", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "SYSTEM_ERRORS",
            columns: table => new
            {
                SystemErrorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Fingerprint = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                Source = table.Column<int>(type: "int", nullable: false),
                Kind = table.Column<int>(type: "int", nullable: false),
                Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                Details = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: true),
                Path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                HttpMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                StatusCode = table.Column<int>(type: "int", nullable: true),
                LastUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                LastUsername = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                TraceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                OccurrenceCount = table.Column<int>(type: "int", nullable: false),
                FirstSeenAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                LastSeenAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                ResolvedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SYSTEM_ERRORS", x => x.SystemErrorId);
            });

        migrationBuilder.CreateTable(
            name: "USER_SCOPE_TYPES",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false),
                Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                IsDefault = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_USER_SCOPE_TYPES", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "DOCUMENT_ACCESS_GRANTS",
            columns: table => new
            {
                GrantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AccessLevelId = table.Column<int>(type: "int", nullable: false),
                GrantedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                GrantedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                RevokedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                RevokedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DOCUMENT_ACCESS_GRANTS", x => x.GrantId);
                table.ForeignKey(
                    name: "FK_DOCUMENT_ACCESS_GRANTS_ACCESS_LEVELS_AccessLevelId",
                    column: x => x.AccessLevelId,
                    principalTable: "ACCESS_LEVELS",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "AUDIT_LOGS",
            columns: table => new
            {
                AuditLogId = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                OccurredAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                ActorDepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                ModuleName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                ActionTypeId = table.Column<int>(type: "int", nullable: false),
                EntityTypeId = table.Column<int>(type: "int", nullable: false),
                EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                RelatedDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                OldValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                NewValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                PreviousHash = table.Column<string>(type: "char(64)", nullable: false),
                EventHash = table.Column<string>(type: "char(64)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AUDIT_LOGS", x => x.AuditLogId);
                table.ForeignKey(
                    name: "FK_AUDIT_LOGS_AUDIT_ACTION_TYPES_ActionTypeId",
                    column: x => x.ActionTypeId,
                    principalTable: "AUDIT_ACTION_TYPES",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_AUDIT_LOGS_AUDIT_ENTITY_TYPES_EntityTypeId",
                    column: x => x.EntityTypeId,
                    principalTable: "AUDIT_ENTITY_TYPES",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "DEPARTMENTS",
            columns: table => new
            {
                DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                DepartmentTypeId = table.Column<int>(type: "int", nullable: false),
                ParentDepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                ManagerEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CanSendExternal = table.Column<bool>(type: "bit", nullable: false),
                CanReceiveExternal = table.Column<bool>(type: "bit", nullable: false),
                Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DEPARTMENTS", x => x.DepartmentId);
                table.ForeignKey(
                    name: "FK_DEPARTMENTS_DEPARTMENTS_ParentDepartmentId",
                    column: x => x.ParentDepartmentId,
                    principalTable: "DEPARTMENTS",
                    principalColumn: "DepartmentId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DEPARTMENTS_DEPARTMENT_TYPES_DepartmentTypeId",
                    column: x => x.DepartmentTypeId,
                    principalTable: "DEPARTMENT_TYPES",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "DOCUMENT_TYPES",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false),
                AllowedDirectionId = table.Column<int>(type: "int", nullable: false),
                RequiresOfficialNumber = table.Column<bool>(type: "bit", nullable: false),
                Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                IsDefault = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DOCUMENT_TYPES", x => x.Id);
                table.ForeignKey(
                    name: "FK_DOCUMENT_TYPES_DOCUMENT_DIRECTIONS_AllowedDirectionId",
                    column: x => x.AllowedDirectionId,
                    principalTable: "DOCUMENT_DIRECTIONS",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "DOCUMENT_FILES",
            columns: table => new
            {
                FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                FileTypeId = table.Column<int>(type: "int", nullable: false),
                Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                IsPrimaryScan = table.Column<bool>(type: "bit", nullable: false),
                CurrentVersionNumber = table.Column<int>(type: "int", nullable: false),
                IsArchived = table.Column<bool>(type: "bit", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DOCUMENT_FILES", x => x.FileId);
                table.ForeignKey(
                    name: "FK_DOCUMENT_FILES_DOCUMENT_FILE_TYPES_FileTypeId",
                    column: x => x.FileTypeId,
                    principalTable: "DOCUMENT_FILE_TYPES",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "DOCUMENT_MOVEMENTS",
            columns: table => new
            {
                MovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                MovementTypeId = table.Column<int>(type: "int", nullable: false),
                SourceDepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DestinationDepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AssignedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                StatusId = table.Column<int>(type: "int", nullable: false),
                CurrentStepNo = table.Column<int>(type: "int", nullable: false),
                Instructions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                DueDate = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                ClosedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DOCUMENT_MOVEMENTS", x => x.MovementId);
                table.ForeignKey(
                    name: "FK_DOCUMENT_MOVEMENTS_MOVEMENT_STATUSES_StatusId",
                    column: x => x.StatusId,
                    principalTable: "MOVEMENT_STATUSES",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DOCUMENT_MOVEMENTS_MOVEMENT_TYPES_MovementTypeId",
                    column: x => x.MovementTypeId,
                    principalTable: "MOVEMENT_TYPES",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "PERSONS",
            columns: table => new
            {
                PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                FullName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                DisplayName = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                NormalizedName = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                FatherName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                MotherName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                NationalNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                CivilRegistryNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                CivilRegistryPlace = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                BirthPlace = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                GenderId = table.Column<int>(type: "int", nullable: true),
                NationalityId = table.Column<int>(type: "int", nullable: true),
                Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PERSONS", x => x.PersonId);
                table.ForeignKey(
                    name: "FK_PERSONS_GENDERS_GenderId",
                    column: x => x.GenderId,
                    principalTable: "GENDERS",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_PERSONS_NATIONALITIES_NationalityId",
                    column: x => x.NationalityId,
                    principalTable: "NATIONALITIES",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "EXTERNAL_ORGANIZATIONS",
            columns: table => new
            {
                ExternalOrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                NameAr = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                NameEn = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                OrganizationTypeId = table.Column<int>(type: "int", nullable: false),
                ContactInfo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EXTERNAL_ORGANIZATIONS", x => x.ExternalOrganizationId);
                table.ForeignKey(
                    name: "FK_EXTERNAL_ORGANIZATIONS_ORGANIZATION_TYPES_OrganizationTypeId",
                    column: x => x.OrganizationTypeId,
                    principalTable: "ORGANIZATION_TYPES",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ROLE_PERMISSIONS",
            columns: table => new
            {
                RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PermissionCode = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                GrantedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ROLE_PERMISSIONS", x => new { x.RoleId, x.PermissionCode });
                table.ForeignKey(
                    name: "FK_ROLE_PERMISSIONS_ROLES_RoleId",
                    column: x => x.RoleId,
                    principalTable: "ROLES",
                    principalColumn: "RoleId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "FILE_STORAGE_PROVIDERS",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false),
                ProviderTypeId = table.Column<int>(type: "int", nullable: false),
                BasePathOrEndpoint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                IsDefault = table.Column<bool>(type: "bit", nullable: false),
                Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FILE_STORAGE_PROVIDERS", x => x.Id);
                table.ForeignKey(
                    name: "FK_FILE_STORAGE_PROVIDERS_STORAGE_PROVIDER_TYPES_ProviderTypeId",
                    column: x => x.ProviderTypeId,
                    principalTable: "STORAGE_PROVIDER_TYPES",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "USERS",
            columns: table => new
            {
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                PrimaryDepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ScopeTypeId = table.Column<int>(type: "int", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                MustChangePassword = table.Column<bool>(type: "bit", nullable: false),
                LastLoginAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_USERS", x => x.UserId);
                table.ForeignKey(
                    name: "FK_USERS_USER_SCOPE_TYPES_ScopeTypeId",
                    column: x => x.ScopeTypeId,
                    principalTable: "USER_SCOPE_TYPES",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "DECISION_TYPES",
            columns: table => new
            {
                DecisionTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                IsAppealable = table.Column<bool>(type: "bit", nullable: false),
                EffectId = table.Column<int>(type: "int", nullable: false),
                DestinationDepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DECISION_TYPES", x => x.DecisionTypeId);
                table.ForeignKey(
                    name: "FK_DECISION_TYPES_DEPARTMENTS_DestinationDepartmentId",
                    column: x => x.DestinationDepartmentId,
                    principalTable: "DEPARTMENTS",
                    principalColumn: "DepartmentId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "INSTITUTION_SETTINGS",
            columns: table => new
            {
                SettingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                InstitutionNameAr = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                InstitutionNameEn = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                LogoPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                LogoUpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                NumberingModeId = table.Column<int>(type: "int", nullable: false),
                NumberingResetPolicyId = table.Column<int>(type: "int", nullable: false),
                DefaultTimezone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                CentralDiwanDepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                ArchiveDiwanDepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                OtherDiwansMayCreateDocuments = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                ResponsibleEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_INSTITUTION_SETTINGS", x => x.SettingId);
                table.ForeignKey(
                    name: "FK_INSTITUTION_SETTINGS_DEPARTMENTS_ArchiveDiwanDepartmentId",
                    column: x => x.ArchiveDiwanDepartmentId,
                    principalTable: "DEPARTMENTS",
                    principalColumn: "DepartmentId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_INSTITUTION_SETTINGS_DEPARTMENTS_CentralDiwanDepartmentId",
                    column: x => x.CentralDiwanDepartmentId,
                    principalTable: "DEPARTMENTS",
                    principalColumn: "DepartmentId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_INSTITUTION_SETTINGS_EMPLOYEES_ResponsibleEmployeeId",
                    column: x => x.ResponsibleEmployeeId,
                    principalTable: "EMPLOYEES",
                    principalColumn: "EmployeeId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_INSTITUTION_SETTINGS_NUMBERING_MODES_NumberingModeId",
                    column: x => x.NumberingModeId,
                    principalTable: "NUMBERING_MODES",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_INSTITUTION_SETTINGS_NUMBERING_RESET_POLICIES_NumberingResetPolicyId",
                    column: x => x.NumberingResetPolicyId,
                    principalTable: "NUMBERING_RESET_POLICIES",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "DOCUMENT_FIELDS",
            columns: table => new
            {
                DocumentFieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DocumentTypeId = table.Column<int>(type: "int", nullable: false),
                DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                Key = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                DataType = table.Column<int>(type: "int", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DOCUMENT_FIELDS", x => x.DocumentFieldId);
                table.ForeignKey(
                    name: "FK_DOCUMENT_FIELDS_DEPARTMENTS_DepartmentId",
                    column: x => x.DepartmentId,
                    principalTable: "DEPARTMENTS",
                    principalColumn: "DepartmentId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DOCUMENT_FIELDS_DOCUMENT_TYPES_DocumentTypeId",
                    column: x => x.DocumentTypeId,
                    principalTable: "DOCUMENT_TYPES",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "DOCUMENT_TYPE_SCHEMA_VERSIONS",
            columns: table => new
            {
                SchemaVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DocumentTypeId = table.Column<int>(type: "int", nullable: false),
                DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                VersionNumber = table.Column<int>(type: "int", nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                PublishedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                PublishedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DOCUMENT_TYPE_SCHEMA_VERSIONS", x => x.SchemaVersionId);
                table.ForeignKey(
                    name: "FK_DOCUMENT_TYPE_SCHEMA_VERSIONS_DEPARTMENTS_DepartmentId",
                    column: x => x.DepartmentId,
                    principalTable: "DEPARTMENTS",
                    principalColumn: "DepartmentId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DOCUMENT_TYPE_SCHEMA_VERSIONS_DOCUMENT_TYPES_DocumentTypeId",
                    column: x => x.DocumentTypeId,
                    principalTable: "DOCUMENT_TYPES",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "NUMBERING_REGISTRIES",
            columns: table => new
            {
                RegistryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                DocumentTypeId = table.Column<int>(type: "int", nullable: true),
                DirectionId = table.Column<int>(type: "int", nullable: true),
                FormatPattern = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                ResetPolicyId = table.Column<int>(type: "int", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NUMBERING_REGISTRIES", x => x.RegistryId);
                table.ForeignKey(
                    name: "FK_NUMBERING_REGISTRIES_DOCUMENT_DIRECTIONS_DirectionId",
                    column: x => x.DirectionId,
                    principalTable: "DOCUMENT_DIRECTIONS",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_NUMBERING_REGISTRIES_DOCUMENT_TYPES_DocumentTypeId",
                    column: x => x.DocumentTypeId,
                    principalTable: "DOCUMENT_TYPES",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_NUMBERING_REGISTRIES_NUMBERING_RESET_POLICIES_ResetPolicyId",
                    column: x => x.ResetPolicyId,
                    principalTable: "NUMBERING_RESET_POLICIES",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "DOCUMENT_MOVEMENT_EVENTS",
            columns: table => new
            {
                EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                MovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                StepNo = table.Column<int>(type: "int", nullable: false),
                EventTypeId = table.Column<int>(type: "int", nullable: false),
                ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                OccurredAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DOCUMENT_MOVEMENT_EVENTS", x => x.EventId);
                table.ForeignKey(
                    name: "FK_DOCUMENT_MOVEMENT_EVENTS_DOCUMENT_MOVEMENTS_MovementId",
                    column: x => x.MovementId,
                    principalTable: "DOCUMENT_MOVEMENTS",
                    principalColumn: "MovementId",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_DOCUMENT_MOVEMENT_EVENTS_MOVEMENT_EVENT_TYPES_EventTypeId",
                    column: x => x.EventTypeId,
                    principalTable: "MOVEMENT_EVENT_TYPES",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "DOCUMENT_FILE_VERSIONS",
            columns: table => new
            {
                VersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                VersionNumber = table.Column<int>(type: "int", nullable: false),
                StorageProviderId = table.Column<int>(type: "int", nullable: false),
                RelativePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                MimeType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                ChecksumSha256 = table.Column<string>(type: "char(64)", nullable: false),
                IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ChangeNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                UploadedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DOCUMENT_FILE_VERSIONS", x => x.VersionId);
                table.ForeignKey(
                    name: "FK_DOCUMENT_FILE_VERSIONS_DOCUMENT_FILES_FileId",
                    column: x => x.FileId,
                    principalTable: "DOCUMENT_FILES",
                    principalColumn: "FileId",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_DOCUMENT_FILE_VERSIONS_FILE_STORAGE_PROVIDERS_StorageProviderId",
                    column: x => x.StorageProviderId,
                    principalTable: "FILE_STORAGE_PROVIDERS",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "SAVED_SEARCHES",
            columns: table => new
            {
                SavedSearchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                CriteriaJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                IsDefault = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SAVED_SEARCHES", x => x.SavedSearchId);
                table.ForeignKey(
                    name: "FK_SAVED_SEARCHES_USERS_UserId",
                    column: x => x.UserId,
                    principalTable: "USERS",
                    principalColumn: "UserId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "USER_DEPARTMENT_ASSIGNMENTS",
            columns: table => new
            {
                AssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AssignmentTypeId = table.Column<int>(type: "int", nullable: false),
                IncludeSubtree = table.Column<bool>(type: "bit", nullable: false),
                StartDate = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                EndDate = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_USER_DEPARTMENT_ASSIGNMENTS", x => x.AssignmentId);
                table.ForeignKey(
                    name: "FK_USER_DEPARTMENT_ASSIGNMENTS_ASSIGNMENT_TYPES_AssignmentTypeId",
                    column: x => x.AssignmentTypeId,
                    principalTable: "ASSIGNMENT_TYPES",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_USER_DEPARTMENT_ASSIGNMENTS_USERS_UserId",
                    column: x => x.UserId,
                    principalTable: "USERS",
                    principalColumn: "UserId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "USER_ROLES",
            columns: table => new
            {
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AssignedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                AssignedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_USER_ROLES", x => new { x.UserId, x.RoleId });
                table.ForeignKey(
                    name: "FK_USER_ROLES_USERS_UserId",
                    column: x => x.UserId,
                    principalTable: "USERS",
                    principalColumn: "UserId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "DECISION_TYPE_DOCUMENT_TYPES",
            columns: table => new
            {
                DecisionTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DocumentTypeId = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DECISION_TYPE_DOCUMENT_TYPES", x => new { x.DecisionTypeId, x.DocumentTypeId });
                table.ForeignKey(
                    name: "FK_DECISION_TYPE_DOCUMENT_TYPES_DECISION_TYPES_DecisionTypeId",
                    column: x => x.DecisionTypeId,
                    principalTable: "DECISION_TYPES",
                    principalColumn: "DecisionTypeId",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_DECISION_TYPE_DOCUMENT_TYPES_DOCUMENT_TYPES_DocumentTypeId",
                    column: x => x.DocumentTypeId,
                    principalTable: "DOCUMENT_TYPES",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "INSTITUTION_LOGOS",
            columns: table => new
            {
                SettingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ContentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                Data = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_INSTITUTION_LOGOS", x => x.SettingId);
                table.ForeignKey(
                    name: "FK_INSTITUTION_LOGOS_INSTITUTION_SETTINGS_SettingId",
                    column: x => x.SettingId,
                    principalTable: "INSTITUTION_SETTINGS",
                    principalColumn: "SettingId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "DOCUMENT_TYPE_SCHEMA_VERSION_FIELDS",
            columns: table => new
            {
                SchemaVersionFieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SchemaVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DocumentFieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                FieldKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                DataType = table.Column<int>(type: "int", nullable: false),
                LabelAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                IsRequired = table.Column<bool>(type: "bit", nullable: false),
                DisplayOrder = table.Column<int>(type: "int", nullable: false),
                RegexPattern = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                RegexErrorMessageAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                MinNumber = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: true),
                MaxNumber = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: true),
                MaxLength = table.Column<int>(type: "int", nullable: true),
                Options = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Behavior = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "{\"DefaultValue\":null,\"IsReadOnly\":false,\"IsFilterable\":true,\"ShowInTable\":false,\"PlaceholderAr\":null,\"HelpTextAr\":null,\"MinLength\":null,\"MinDate\":null,\"MaxDate\":null,\"VisibleWhenFieldKey\":null,\"VisibleWhenValue\":null,\"IsHalfWidth\":false,\"SectionTitleAr\":null,\"OptionSource\":0,\"LookupCatalog\":null}")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DOCUMENT_TYPE_SCHEMA_VERSION_FIELDS", x => x.SchemaVersionFieldId);
                table.ForeignKey(
                    name: "FK_DOCUMENT_TYPE_SCHEMA_VERSION_FIELDS_DOCUMENT_FIELDS_DocumentFieldId",
                    column: x => x.DocumentFieldId,
                    principalTable: "DOCUMENT_FIELDS",
                    principalColumn: "DocumentFieldId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DOCUMENT_TYPE_SCHEMA_VERSION_FIELDS_DOCUMENT_TYPE_SCHEMA_VERSIONS_SchemaVersionId",
                    column: x => x.SchemaVersionId,
                    principalTable: "DOCUMENT_TYPE_SCHEMA_VERSIONS",
                    principalColumn: "SchemaVersionId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "DOCUMENTS",
            columns: table => new
            {
                DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                SubjectSummary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                DisplayName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                DocumentTypeId = table.Column<int>(type: "int", nullable: false),
                DirectionId = table.Column<int>(type: "int", nullable: true),
                ConfidentialityLevelId = table.Column<int>(type: "int", nullable: true),
                PriorityLevelId = table.Column<int>(type: "int", nullable: true),
                CurrentStatusId = table.Column<int>(type: "int", nullable: false),
                StatusBeforeArchiveId = table.Column<int>(type: "int", nullable: true),
                OwningDepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ActiveOfficialNumberId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                OfficialNumberText = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                RegistrationDate = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                SchemaVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DOCUMENTS", x => x.DocumentId);
                table.ForeignKey(
                    name: "FK_DOCUMENTS_DOCUMENT_CONFIDENTIALITY_LEVELS_ConfidentialityLevelId",
                    column: x => x.ConfidentialityLevelId,
                    principalTable: "DOCUMENT_CONFIDENTIALITY_LEVELS",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DOCUMENTS_DOCUMENT_DIRECTIONS_DirectionId",
                    column: x => x.DirectionId,
                    principalTable: "DOCUMENT_DIRECTIONS",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DOCUMENTS_DOCUMENT_PRIORITY_LEVELS_PriorityLevelId",
                    column: x => x.PriorityLevelId,
                    principalTable: "DOCUMENT_PRIORITY_LEVELS",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DOCUMENTS_DOCUMENT_STATUS_TYPES_CurrentStatusId",
                    column: x => x.CurrentStatusId,
                    principalTable: "DOCUMENT_STATUS_TYPES",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DOCUMENTS_DOCUMENT_TYPES_DocumentTypeId",
                    column: x => x.DocumentTypeId,
                    principalTable: "DOCUMENT_TYPES",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DOCUMENTS_DOCUMENT_TYPE_SCHEMA_VERSIONS_SchemaVersionId",
                    column: x => x.SchemaVersionId,
                    principalTable: "DOCUMENT_TYPE_SCHEMA_VERSIONS",
                    principalColumn: "SchemaVersionId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "NUMBERING_COUNTERS",
            columns: table => new
            {
                CounterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RegistryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                NumberingYear = table.Column<int>(type: "int", nullable: false),
                LastValue = table.Column<long>(type: "bigint", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NUMBERING_COUNTERS", x => x.CounterId);
                table.ForeignKey(
                    name: "FK_NUMBERING_COUNTERS_NUMBERING_REGISTRIES_RegistryId",
                    column: x => x.RegistryId,
                    principalTable: "NUMBERING_REGISTRIES",
                    principalColumn: "RegistryId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "DOCUMENT_ARCHIVE_EVENTS",
            columns: table => new
            {
                ArchiveEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                EventTypeId = table.Column<int>(type: "int", nullable: false),
                PreviousStatusId = table.Column<int>(type: "int", nullable: false),
                NewStatusId = table.Column<int>(type: "int", nullable: false),
                ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                ArchiveLocationId = table.Column<int>(type: "int", nullable: true),
                OccurredAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DOCUMENT_ARCHIVE_EVENTS", x => x.ArchiveEventId);
                table.ForeignKey(
                    name: "FK_DOCUMENT_ARCHIVE_EVENTS_ARCHIVE_EVENT_TYPES_EventTypeId",
                    column: x => x.EventTypeId,
                    principalTable: "ARCHIVE_EVENT_TYPES",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DOCUMENT_ARCHIVE_EVENTS_ARCHIVE_LOCATIONS_ArchiveLocationId",
                    column: x => x.ArchiveLocationId,
                    principalTable: "ARCHIVE_LOCATIONS",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DOCUMENT_ARCHIVE_EVENTS_DOCUMENTS_DocumentId",
                    column: x => x.DocumentId,
                    principalTable: "DOCUMENTS",
                    principalColumn: "DocumentId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DOCUMENT_ARCHIVE_EVENTS_DOCUMENT_STATUS_TYPES_NewStatusId",
                    column: x => x.NewStatusId,
                    principalTable: "DOCUMENT_STATUS_TYPES",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DOCUMENT_ARCHIVE_EVENTS_DOCUMENT_STATUS_TYPES_PreviousStatusId",
                    column: x => x.PreviousStatusId,
                    principalTable: "DOCUMENT_STATUS_TYPES",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DOCUMENT_ARCHIVE_EVENTS_USERS_ActorUserId",
                    column: x => x.ActorUserId,
                    principalTable: "USERS",
                    principalColumn: "UserId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "DOCUMENT_FIELD_VALUES",
            columns: table => new
            {
                DocumentFieldValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DocumentFieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TextValue = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                NumberValue = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: true),
                DateValue = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                BoolValue = table.Column<bool>(type: "bit", nullable: true),
                OptionKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DOCUMENT_FIELD_VALUES", x => x.DocumentFieldValueId);
                table.ForeignKey(
                    name: "FK_DOCUMENT_FIELD_VALUES_DOCUMENTS_DocumentId",
                    column: x => x.DocumentId,
                    principalTable: "DOCUMENTS",
                    principalColumn: "DocumentId",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_DOCUMENT_FIELD_VALUES_DOCUMENT_FIELDS_DocumentFieldId",
                    column: x => x.DocumentFieldId,
                    principalTable: "DOCUMENT_FIELDS",
                    principalColumn: "DocumentFieldId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "DOCUMENT_NUMBERS",
            columns: table => new
            {
                DocumentNumberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RegistryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ScopeId = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                NumberingYear = table.Column<int>(type: "int", nullable: false),
                SequenceValue = table.Column<long>(type: "bigint", nullable: false),
                OfficialNumberText = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                StatusId = table.Column<int>(type: "int", nullable: false),
                IssuedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                IssuedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                ReplacedByNumberId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                RegistryDate = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DOCUMENT_NUMBERS", x => x.DocumentNumberId);
                table.ForeignKey(
                    name: "FK_DOCUMENT_NUMBERS_DEPARTMENTS_DepartmentId",
                    column: x => x.DepartmentId,
                    principalTable: "DEPARTMENTS",
                    principalColumn: "DepartmentId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DOCUMENT_NUMBERS_DOCUMENTS_DocumentId",
                    column: x => x.DocumentId,
                    principalTable: "DOCUMENTS",
                    principalColumn: "DocumentId",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_DOCUMENT_NUMBERS_DOCUMENT_NUMBER_SCOPES_ScopeId",
                    column: x => x.ScopeId,
                    principalTable: "DOCUMENT_NUMBER_SCOPES",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DOCUMENT_NUMBERS_DOCUMENT_NUMBER_STATUSES_StatusId",
                    column: x => x.StatusId,
                    principalTable: "DOCUMENT_NUMBER_STATUSES",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "DOCUMENT_PARTICIPANTS",
            columns: table => new
            {
                ParticipantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RoleId = table.Column<int>(type: "int", nullable: false),
                TargetTypeId = table.Column<int>(type: "int", nullable: false),
                InternalDepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                ExternalOrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                PersonName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DOCUMENT_PARTICIPANTS", x => x.ParticipantId);
                table.ForeignKey(
                    name: "FK_DOCUMENT_PARTICIPANTS_DOCUMENTS_DocumentId",
                    column: x => x.DocumentId,
                    principalTable: "DOCUMENTS",
                    principalColumn: "DocumentId",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_DOCUMENT_PARTICIPANTS_PARTICIPANT_ROLES_RoleId",
                    column: x => x.RoleId,
                    principalTable: "PARTICIPANT_ROLES",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DOCUMENT_PARTICIPANTS_PARTICIPANT_TARGET_TYPES_TargetTypeId",
                    column: x => x.TargetTypeId,
                    principalTable: "PARTICIPANT_TARGET_TYPES",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "DOCUMENT_PERSONS",
            columns: table => new
            {
                DocumentPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RoleId = table.Column<int>(type: "int", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DOCUMENT_PERSONS", x => x.DocumentPersonId);
                table.ForeignKey(
                    name: "FK_DOCUMENT_PERSONS_DOCUMENTS_DocumentId",
                    column: x => x.DocumentId,
                    principalTable: "DOCUMENTS",
                    principalColumn: "DocumentId",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_DOCUMENT_PERSONS_DOCUMENT_PERSON_ROLES_RoleId",
                    column: x => x.RoleId,
                    principalTable: "DOCUMENT_PERSON_ROLES",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DOCUMENT_PERSONS_PERSONS_PersonId",
                    column: x => x.PersonId,
                    principalTable: "PERSONS",
                    principalColumn: "PersonId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "DOCUMENT_RELATION_EVENTS",
            columns: table => new
            {
                RelationEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SourceDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TargetDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RelationTypeId = table.Column<int>(type: "int", nullable: false),
                ActionTypeId = table.Column<int>(type: "int", nullable: false),
                ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                OccurredAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DOCUMENT_RELATION_EVENTS", x => x.RelationEventId);
                table.ForeignKey(
                    name: "FK_DOCUMENT_RELATION_EVENTS_DOCUMENTS_SourceDocumentId",
                    column: x => x.SourceDocumentId,
                    principalTable: "DOCUMENTS",
                    principalColumn: "DocumentId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DOCUMENT_RELATION_EVENTS_DOCUMENTS_TargetDocumentId",
                    column: x => x.TargetDocumentId,
                    principalTable: "DOCUMENTS",
                    principalColumn: "DocumentId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DOCUMENT_RELATION_EVENTS_DOCUMENT_RELATION_TYPES_RelationTypeId",
                    column: x => x.RelationTypeId,
                    principalTable: "DOCUMENT_RELATION_TYPES",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DOCUMENT_RELATION_EVENTS_RELATION_ACTION_TYPES_ActionTypeId",
                    column: x => x.ActionTypeId,
                    principalTable: "RELATION_ACTION_TYPES",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DOCUMENT_RELATION_EVENTS_USERS_ActorUserId",
                    column: x => x.ActorUserId,
                    principalTable: "USERS",
                    principalColumn: "UserId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "DOCUMENT_RELATIONS",
            columns: table => new
            {
                RelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SourceDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TargetDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RelationTypeId = table.Column<int>(type: "int", nullable: false),
                Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DOCUMENT_RELATIONS", x => x.RelationId);
                table.CheckConstraint("CK_DOC_RELATIONS_NOT_SELF", "[SourceDocumentId] <> [TargetDocumentId]");
                table.ForeignKey(
                    name: "FK_DOCUMENT_RELATIONS_DOCUMENTS_SourceDocumentId",
                    column: x => x.SourceDocumentId,
                    principalTable: "DOCUMENTS",
                    principalColumn: "DocumentId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DOCUMENT_RELATIONS_DOCUMENTS_TargetDocumentId",
                    column: x => x.TargetDocumentId,
                    principalTable: "DOCUMENTS",
                    principalColumn: "DocumentId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DOCUMENT_RELATIONS_DOCUMENT_RELATION_TYPES_RelationTypeId",
                    column: x => x.RelationTypeId,
                    principalTable: "DOCUMENT_RELATION_TYPES",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DOCUMENT_RELATIONS_USERS_CreatedByUserId",
                    column: x => x.CreatedByUserId,
                    principalTable: "USERS",
                    principalColumn: "UserId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "DOCUMENT_STATUS_HISTORY",
            columns: table => new
            {
                HistoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                FromStatusId = table.Column<int>(type: "int", nullable: true),
                ToStatusId = table.Column<int>(type: "int", nullable: false),
                ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                ChangedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DOCUMENT_STATUS_HISTORY", x => x.HistoryId);
                table.ForeignKey(
                    name: "FK_DOCUMENT_STATUS_HISTORY_DOCUMENTS_DocumentId",
                    column: x => x.DocumentId,
                    principalTable: "DOCUMENTS",
                    principalColumn: "DocumentId",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_DOCUMENT_STATUS_HISTORY_DOCUMENT_STATUS_TYPES_FromStatusId",
                    column: x => x.FromStatusId,
                    principalTable: "DOCUMENT_STATUS_TYPES",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DOCUMENT_STATUS_HISTORY_DOCUMENT_STATUS_TYPES_ToStatusId",
                    column: x => x.ToStatusId,
                    principalTable: "DOCUMENT_STATUS_TYPES",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "NOTIFICATIONS",
            columns: table => new
            {
                NotificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                NotificationTypeId = table.Column<int>(type: "int", nullable: false),
                TitleAr = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                BodyAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                PriorityId = table.Column<int>(type: "int", nullable: false),
                RelatedDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                RelatedMovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                ActionUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NOTIFICATIONS", x => x.NotificationId);
                table.ForeignKey(
                    name: "FK_NOTIFICATIONS_DOCUMENTS_RelatedDocumentId",
                    column: x => x.RelatedDocumentId,
                    principalTable: "DOCUMENTS",
                    principalColumn: "DocumentId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_NOTIFICATIONS_DOCUMENT_MOVEMENTS_RelatedMovementId",
                    column: x => x.RelatedMovementId,
                    principalTable: "DOCUMENT_MOVEMENTS",
                    principalColumn: "MovementId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_NOTIFICATIONS_NOTIFICATION_PRIORITIES_PriorityId",
                    column: x => x.PriorityId,
                    principalTable: "NOTIFICATION_PRIORITIES",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_NOTIFICATIONS_NOTIFICATION_TYPES_NotificationTypeId",
                    column: x => x.NotificationTypeId,
                    principalTable: "NOTIFICATION_TYPES",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "DOCUMENT_NUMBER_HISTORY",
            columns: table => new
            {
                HistoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DocumentNumberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                EventTypeId = table.Column<int>(type: "int", nullable: false),
                ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                OccurredAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                EffectiveDate = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DOCUMENT_NUMBER_HISTORY", x => x.HistoryId);
                table.ForeignKey(
                    name: "FK_DOCUMENT_NUMBER_HISTORY_DOCUMENT_NUMBERS_DocumentNumberId",
                    column: x => x.DocumentNumberId,
                    principalTable: "DOCUMENT_NUMBERS",
                    principalColumn: "DocumentNumberId",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_DOCUMENT_NUMBER_HISTORY_DOCUMENT_NUMBER_EVENT_TYPES_EventTypeId",
                    column: x => x.EventTypeId,
                    principalTable: "DOCUMENT_NUMBER_EVENT_TYPES",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "DOCUMENT_REGISTRY_VISITS",
            columns: table => new
            {
                VisitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DocumentNumberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RegistryDate = table.Column<DateTime>(type: "date", nullable: false),
                MovementStepId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                RecordedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RecordedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                SchemaVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                LockedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                RecordUpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                RecordUpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                CorrectedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                CorrectedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                CorrectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DOCUMENT_REGISTRY_VISITS", x => x.VisitId);
                table.ForeignKey(
                    name: "FK_DOCUMENT_REGISTRY_VISITS_DOCUMENT_NUMBERS_DocumentNumberId",
                    column: x => x.DocumentNumberId,
                    principalTable: "DOCUMENT_NUMBERS",
                    principalColumn: "DocumentNumberId",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_DOCUMENT_REGISTRY_VISITS_DOCUMENT_TYPE_SCHEMA_VERSIONS_SchemaVersionId",
                    column: x => x.SchemaVersionId,
                    principalTable: "DOCUMENT_TYPE_SCHEMA_VERSIONS",
                    principalColumn: "SchemaVersionId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "NOTIFICATION_OUTBOX",
            columns: table => new
            {
                OutboxId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                NotificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ChannelId = table.Column<int>(type: "int", nullable: false),
                PayloadJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                StatusId = table.Column<int>(type: "int", nullable: false),
                RetryCount = table.Column<int>(type: "int", nullable: false),
                LastError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                NextRetryAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                ProcessedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NOTIFICATION_OUTBOX", x => x.OutboxId);
                table.ForeignKey(
                    name: "FK_NOTIFICATION_OUTBOX_NOTIFICATIONS_NotificationId",
                    column: x => x.NotificationId,
                    principalTable: "NOTIFICATIONS",
                    principalColumn: "NotificationId",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_NOTIFICATION_OUTBOX_NOTIFICATION_CHANNELS_ChannelId",
                    column: x => x.ChannelId,
                    principalTable: "NOTIFICATION_CHANNELS",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_NOTIFICATION_OUTBOX_NOTIFICATION_OUTBOX_STATUSES_StatusId",
                    column: x => x.StatusId,
                    principalTable: "NOTIFICATION_OUTBOX_STATUSES",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "NOTIFICATION_RECIPIENTS",
            columns: table => new
            {
                RecipientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                NotificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                IsRead = table.Column<bool>(type: "bit", nullable: false),
                ReadAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                IsDismissed = table.Column<bool>(type: "bit", nullable: false),
                DeliveredAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NOTIFICATION_RECIPIENTS", x => x.RecipientId);
                table.ForeignKey(
                    name: "FK_NOTIFICATION_RECIPIENTS_DEPARTMENTS_DepartmentId",
                    column: x => x.DepartmentId,
                    principalTable: "DEPARTMENTS",
                    principalColumn: "DepartmentId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_NOTIFICATION_RECIPIENTS_NOTIFICATIONS_NotificationId",
                    column: x => x.NotificationId,
                    principalTable: "NOTIFICATIONS",
                    principalColumn: "NotificationId",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_NOTIFICATION_RECIPIENTS_USERS_UserId",
                    column: x => x.UserId,
                    principalTable: "USERS",
                    principalColumn: "UserId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "DEPARTMENT_RECORD_VALUES",
            columns: table => new
            {
                ValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                VisitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DocumentFieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TextValue = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                NumberValue = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: true),
                DateValue = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                BoolValue = table.Column<bool>(type: "bit", nullable: true),
                OptionKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DEPARTMENT_RECORD_VALUES", x => x.ValueId);
                table.ForeignKey(
                    name: "FK_DEPARTMENT_RECORD_VALUES_DOCUMENT_FIELDS_DocumentFieldId",
                    column: x => x.DocumentFieldId,
                    principalTable: "DOCUMENT_FIELDS",
                    principalColumn: "DocumentFieldId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DEPARTMENT_RECORD_VALUES_DOCUMENT_REGISTRY_VISITS_VisitId",
                    column: x => x.VisitId,
                    principalTable: "DOCUMENT_REGISTRY_VISITS",
                    principalColumn: "VisitId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "DOCUMENT_DECISIONS",
            columns: table => new
            {
                DecisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DecisionTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DecisionNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                DecisionDate = table.Column<DateOnly>(type: "date", nullable: false),
                DecisionYear = table.Column<int>(type: "int", nullable: false),
                IssuingDepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RegistryVisitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                StatusId = table.Column<int>(type: "int", nullable: false),
                IsAppealable = table.Column<bool>(type: "bit", nullable: false),
                EffectId = table.Column<int>(type: "int", nullable: false),
                DestinationDepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                DocumentFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                RecordedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RecordedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                StatusReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                StatusChangedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DOCUMENT_DECISIONS", x => x.DecisionId);
                table.ForeignKey(
                    name: "FK_DOCUMENT_DECISIONS_DECISION_TYPES_DecisionTypeId",
                    column: x => x.DecisionTypeId,
                    principalTable: "DECISION_TYPES",
                    principalColumn: "DecisionTypeId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DOCUMENT_DECISIONS_DEPARTMENTS_DestinationDepartmentId",
                    column: x => x.DestinationDepartmentId,
                    principalTable: "DEPARTMENTS",
                    principalColumn: "DepartmentId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DOCUMENT_DECISIONS_DEPARTMENTS_IssuingDepartmentId",
                    column: x => x.IssuingDepartmentId,
                    principalTable: "DEPARTMENTS",
                    principalColumn: "DepartmentId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DOCUMENT_DECISIONS_DOCUMENTS_DocumentId",
                    column: x => x.DocumentId,
                    principalTable: "DOCUMENTS",
                    principalColumn: "DocumentId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DOCUMENT_DECISIONS_DOCUMENT_FILES_DocumentFileId",
                    column: x => x.DocumentFileId,
                    principalTable: "DOCUMENT_FILES",
                    principalColumn: "FileId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DOCUMENT_DECISIONS_DOCUMENT_REGISTRY_VISITS_RegistryVisitId",
                    column: x => x.RegistryVisitId,
                    principalTable: "DOCUMENT_REGISTRY_VISITS",
                    principalColumn: "VisitId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "DOCUMENT_MOVEMENT_STEPS",
            columns: table => new
            {
                StepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                MovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                StepNo = table.Column<int>(type: "int", nullable: false),
                FromDepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ToDepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AssignedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                StepStatusId = table.Column<int>(type: "int", nullable: false),
                Instructions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                OutcomeNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                SentAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                ReceivedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                ReceivedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                RegistryNumberId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                RegistryVisitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                CompletedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                DueDate = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DOCUMENT_MOVEMENT_STEPS", x => x.StepId);
                table.ForeignKey(
                    name: "FK_DOCUMENT_MOVEMENT_STEPS_DOCUMENT_MOVEMENTS_MovementId",
                    column: x => x.MovementId,
                    principalTable: "DOCUMENT_MOVEMENTS",
                    principalColumn: "MovementId",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_DOCUMENT_MOVEMENT_STEPS_DOCUMENT_REGISTRY_VISITS_RegistryVisitId",
                    column: x => x.RegistryVisitId,
                    principalTable: "DOCUMENT_REGISTRY_VISITS",
                    principalColumn: "VisitId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DOCUMENT_MOVEMENT_STEPS_MOVEMENT_STEP_STATUSES_StepStatusId",
                    column: x => x.StepStatusId,
                    principalTable: "MOVEMENT_STEP_STATUSES",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "DECISION_APPEALS",
            columns: table => new
            {
                AppealId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DecisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AppealDate = table.Column<DateOnly>(type: "date", nullable: false),
                AppellantDepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                OutcomeId = table.Column<int>(type: "int", nullable: false),
                OutcomeDate = table.Column<DateOnly>(type: "date", nullable: true),
                RecordedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RecordedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                OutcomeRecordedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                OutcomeRecordedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DECISION_APPEALS", x => x.AppealId);
                table.ForeignKey(
                    name: "FK_DECISION_APPEALS_DEPARTMENTS_AppellantDepartmentId",
                    column: x => x.AppellantDepartmentId,
                    principalTable: "DEPARTMENTS",
                    principalColumn: "DepartmentId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DECISION_APPEALS_DOCUMENT_DECISIONS_DecisionId",
                    column: x => x.DecisionId,
                    principalTable: "DOCUMENT_DECISIONS",
                    principalColumn: "DecisionId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AUDIT_LOGS_ActionTypeId",
            table: "AUDIT_LOGS",
            column: "ActionTypeId");

        migrationBuilder.CreateIndex(
            name: "IX_AUDIT_LOGS_DOC",
            table: "AUDIT_LOGS",
            column: "RelatedDocumentId");

        migrationBuilder.CreateIndex(
            name: "IX_AUDIT_LOGS_ENTITY",
            table: "AUDIT_LOGS",
            columns: new[] { "EntityTypeId", "EntityId" });

        migrationBuilder.CreateIndex(
            name: "IX_AUDIT_LOGS_OCCURRED_AT",
            table: "AUDIT_LOGS",
            column: "OccurredAtUtc");

        migrationBuilder.CreateIndex(
            name: "UX_AUDIT_LOGS_PREVIOUS_HASH",
            table: "AUDIT_LOGS",
            column: "PreviousHash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DECISION_APPEALS_AppellantDepartmentId",
            table: "DECISION_APPEALS",
            column: "AppellantDepartmentId");

        migrationBuilder.CreateIndex(
            name: "IX_DECISION_APPEALS_DECISION",
            table: "DECISION_APPEALS",
            column: "DecisionId");

        migrationBuilder.CreateIndex(
            name: "UX_DECISION_APPEALS_ONE_PENDING",
            table: "DECISION_APPEALS",
            column: "DecisionId",
            unique: true,
            filter: "[OutcomeId] = 1");

        migrationBuilder.CreateIndex(
            name: "IX_DECISION_TYPE_DOCUMENT_TYPES_DocumentTypeId",
            table: "DECISION_TYPE_DOCUMENT_TYPES",
            column: "DocumentTypeId");

        migrationBuilder.CreateIndex(
            name: "IX_DECISION_TYPES_DestinationDepartmentId",
            table: "DECISION_TYPES",
            column: "DestinationDepartmentId");

        migrationBuilder.CreateIndex(
            name: "IX_DEPT_RECORD_VALUES_FIELD_VISIT",
            table: "DEPARTMENT_RECORD_VALUES",
            columns: new[] { "DocumentFieldId", "VisitId" });

        migrationBuilder.CreateIndex(
            name: "UX_DEPT_RECORD_VALUES_VISIT_FIELD",
            table: "DEPARTMENT_RECORD_VALUES",
            columns: new[] { "VisitId", "DocumentFieldId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DEPARTMENTS_PARENT",
            table: "DEPARTMENTS",
            column: "ParentDepartmentId");

        migrationBuilder.CreateIndex(
            name: "IX_DEPARTMENTS_TYPE_ACTIVE",
            table: "DEPARTMENTS",
            columns: new[] { "DepartmentTypeId", "IsActive" });

        migrationBuilder.CreateIndex(
            name: "UX_DEPARTMENTS_CODE",
            table: "DEPARTMENTS",
            column: "Code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DOC_ACCESS_GRANTS_DOC_USER",
            table: "DOCUMENT_ACCESS_GRANTS",
            columns: new[] { "DocumentId", "UserId", "IsRevoked" });

        migrationBuilder.CreateIndex(
            name: "IX_DOCUMENT_ACCESS_GRANTS_AccessLevelId",
            table: "DOCUMENT_ACCESS_GRANTS",
            column: "AccessLevelId");

        migrationBuilder.CreateIndex(
            name: "IX_DOC_ARCHIVE_EVENTS_DOC_DATE",
            table: "DOCUMENT_ARCHIVE_EVENTS",
            columns: new[] { "DocumentId", "OccurredAt" });

        migrationBuilder.CreateIndex(
            name: "IX_DOC_ARCHIVE_EVENTS_TYPE_DATE",
            table: "DOCUMENT_ARCHIVE_EVENTS",
            columns: new[] { "EventTypeId", "OccurredAt" });

        migrationBuilder.CreateIndex(
            name: "IX_DOCUMENT_ARCHIVE_EVENTS_ActorUserId",
            table: "DOCUMENT_ARCHIVE_EVENTS",
            column: "ActorUserId");

        migrationBuilder.CreateIndex(
            name: "IX_DOCUMENT_ARCHIVE_EVENTS_ArchiveLocationId",
            table: "DOCUMENT_ARCHIVE_EVENTS",
            column: "ArchiveLocationId");

        migrationBuilder.CreateIndex(
            name: "IX_DOCUMENT_ARCHIVE_EVENTS_NewStatusId",
            table: "DOCUMENT_ARCHIVE_EVENTS",
            column: "NewStatusId");

        migrationBuilder.CreateIndex(
            name: "IX_DOCUMENT_ARCHIVE_EVENTS_PreviousStatusId",
            table: "DOCUMENT_ARCHIVE_EVENTS",
            column: "PreviousStatusId");

        migrationBuilder.CreateIndex(
            name: "IX_DOCUMENT_DECISIONS_DecisionTypeId",
            table: "DOCUMENT_DECISIONS",
            column: "DecisionTypeId");

        migrationBuilder.CreateIndex(
            name: "IX_DOCUMENT_DECISIONS_DestinationDepartmentId",
            table: "DOCUMENT_DECISIONS",
            column: "DestinationDepartmentId");

        migrationBuilder.CreateIndex(
            name: "IX_DOCUMENT_DECISIONS_DocumentFileId",
            table: "DOCUMENT_DECISIONS",
            column: "DocumentFileId");

        migrationBuilder.CreateIndex(
            name: "IX_DOCUMENT_DECISIONS_RegistryVisitId",
            table: "DOCUMENT_DECISIONS",
            column: "RegistryVisitId");

        migrationBuilder.CreateIndex(
            name: "UX_DOCUMENT_DECISIONS_DEPT_YEAR_NUMBER",
            table: "DOCUMENT_DECISIONS",
            columns: new[] { "IssuingDepartmentId", "DecisionYear", "DecisionNumber" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UX_DOCUMENT_DECISIONS_ONE_ACTIVE",
            table: "DOCUMENT_DECISIONS",
            column: "DocumentId",
            unique: true,
            filter: "[StatusId] IN (1, 2, 3)");

        migrationBuilder.CreateIndex(
            name: "IX_DOC_FIELD_VALUES_BOOL",
            table: "DOCUMENT_FIELD_VALUES",
            columns: new[] { "DocumentFieldId", "BoolValue" },
            filter: "[BoolValue] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_DOC_FIELD_VALUES_DATE",
            table: "DOCUMENT_FIELD_VALUES",
            columns: new[] { "DocumentFieldId", "DateValue" },
            filter: "[DateValue] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_DOC_FIELD_VALUES_NUMBER",
            table: "DOCUMENT_FIELD_VALUES",
            columns: new[] { "DocumentFieldId", "NumberValue" },
            filter: "[NumberValue] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_DOC_FIELD_VALUES_OPTION",
            table: "DOCUMENT_FIELD_VALUES",
            columns: new[] { "DocumentFieldId", "OptionKey" },
            filter: "[OptionKey] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "UX_DOC_FIELD_VALUES_DOC_FIELD",
            table: "DOCUMENT_FIELD_VALUES",
            columns: new[] { "DocumentId", "DocumentFieldId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DOCUMENT_FIELDS_DepartmentId",
            table: "DOCUMENT_FIELDS",
            column: "DepartmentId");

        migrationBuilder.CreateIndex(
            name: "UX_DOCUMENT_FIELDS_TYPE_DEPT_KEY",
            table: "DOCUMENT_FIELDS",
            columns: new[] { "DocumentTypeId", "DepartmentId", "Key" },
            unique: true,
            filter: "[DepartmentId] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "UX_DOCUMENT_FIELDS_TYPE_KEY",
            table: "DOCUMENT_FIELDS",
            columns: new[] { "DocumentTypeId", "Key" },
            unique: true,
            filter: "[DepartmentId] IS NULL");

        migrationBuilder.CreateIndex(
            name: "IX_DOCUMENT_FILE_VERSIONS_StorageProviderId",
            table: "DOCUMENT_FILE_VERSIONS",
            column: "StorageProviderId");

        migrationBuilder.CreateIndex(
            name: "UX_DOC_FILE_VERSIONS_NO",
            table: "DOCUMENT_FILE_VERSIONS",
            columns: new[] { "FileId", "VersionNumber" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DOC_FILES_DOC",
            table: "DOCUMENT_FILES",
            column: "DocumentId");

        migrationBuilder.CreateIndex(
            name: "IX_DOCUMENT_FILES_FileTypeId",
            table: "DOCUMENT_FILES",
            column: "FileTypeId");

        migrationBuilder.CreateIndex(
            name: "IX_DOC_MOVEMENT_EVENTS_MOV_DATE",
            table: "DOCUMENT_MOVEMENT_EVENTS",
            columns: new[] { "MovementId", "OccurredAt" });

        migrationBuilder.CreateIndex(
            name: "IX_DOCUMENT_MOVEMENT_EVENTS_EventTypeId",
            table: "DOCUMENT_MOVEMENT_EVENTS",
            column: "EventTypeId");

        migrationBuilder.CreateIndex(
            name: "IX_DOCUMENT_MOVEMENT_STEPS_RegistryVisitId",
            table: "DOCUMENT_MOVEMENT_STEPS",
            column: "RegistryVisitId");

        migrationBuilder.CreateIndex(
            name: "IX_DOCUMENT_MOVEMENT_STEPS_StepStatusId",
            table: "DOCUMENT_MOVEMENT_STEPS",
            column: "StepStatusId");

        migrationBuilder.CreateIndex(
            name: "UX_DOC_MOVEMENT_STEPS_NO",
            table: "DOCUMENT_MOVEMENT_STEPS",
            columns: new[] { "MovementId", "StepNo" },
            unique: true);

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
            name: "IX_DOCUMENT_NUMBER_HISTORY_DocumentNumberId",
            table: "DOCUMENT_NUMBER_HISTORY",
            column: "DocumentNumberId");

        migrationBuilder.CreateIndex(
            name: "IX_DOCUMENT_NUMBER_HISTORY_EventTypeId",
            table: "DOCUMENT_NUMBER_HISTORY",
            column: "EventTypeId");

        migrationBuilder.CreateIndex(
            name: "IX_DOC_NUMBERS_DEPT_REG_DATE",
            table: "DOCUMENT_NUMBERS",
            columns: new[] { "DepartmentId", "RegistryDate" });

        migrationBuilder.CreateIndex(
            name: "IX_DOC_NUMBERS_REG_YEAR",
            table: "DOCUMENT_NUMBERS",
            columns: new[] { "RegistryId", "NumberingYear" });

        migrationBuilder.CreateIndex(
            name: "IX_DOCUMENT_NUMBERS_ScopeId",
            table: "DOCUMENT_NUMBERS",
            column: "ScopeId");

        migrationBuilder.CreateIndex(
            name: "IX_DOCUMENT_NUMBERS_StatusId",
            table: "DOCUMENT_NUMBERS",
            column: "StatusId");

        migrationBuilder.CreateIndex(
            name: "UX_DOC_NUMBERS_DEPT_TEXT_YEAR",
            table: "DOCUMENT_NUMBERS",
            columns: new[] { "RegistryId", "OfficialNumberText", "NumberingYear" },
            unique: true,
            filter: "[ScopeId] = 2");

        migrationBuilder.CreateIndex(
            name: "UX_DOC_NUMBERS_DOC_DEPT",
            table: "DOCUMENT_NUMBERS",
            columns: new[] { "DocumentId", "DepartmentId", "NumberingYear" },
            unique: true,
            filter: "[DepartmentId] IS NOT NULL AND [StatusId] <> 3");

        migrationBuilder.CreateIndex(
            name: "UX_DOC_NUMBERS_REG_YEAR_SEQ",
            table: "DOCUMENT_NUMBERS",
            columns: new[] { "RegistryId", "NumberingYear", "SequenceValue" },
            unique: true,
            filter: "[ScopeId] = 1");

        migrationBuilder.CreateIndex(
            name: "UX_DOC_NUMBERS_TEXT_YEAR",
            table: "DOCUMENT_NUMBERS",
            columns: new[] { "OfficialNumberText", "NumberingYear" },
            unique: true,
            filter: "[ScopeId] = 1");

        migrationBuilder.CreateIndex(
            name: "IX_DOC_PARTICIPANTS_DOC",
            table: "DOCUMENT_PARTICIPANTS",
            column: "DocumentId");

        migrationBuilder.CreateIndex(
            name: "IX_DOCUMENT_PARTICIPANTS_RoleId",
            table: "DOCUMENT_PARTICIPANTS",
            column: "RoleId");

        migrationBuilder.CreateIndex(
            name: "IX_DOCUMENT_PARTICIPANTS_TargetTypeId",
            table: "DOCUMENT_PARTICIPANTS",
            column: "TargetTypeId");

        migrationBuilder.CreateIndex(
            name: "IX_DOCUMENT_PERSONS_PERSON_DOC",
            table: "DOCUMENT_PERSONS",
            columns: new[] { "PersonId", "DocumentId" });

        migrationBuilder.CreateIndex(
            name: "IX_DOCUMENT_PERSONS_RoleId",
            table: "DOCUMENT_PERSONS",
            column: "RoleId");

        migrationBuilder.CreateIndex(
            name: "UX_DOCUMENT_PERSONS_DOC_PERSON",
            table: "DOCUMENT_PERSONS",
            columns: new[] { "DocumentId", "PersonId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DOC_REG_VISITS_VERSION_NUMBER_DATE",
            table: "DOCUMENT_REGISTRY_VISITS",
            columns: new[] { "SchemaVersionId", "DocumentNumberId", "RegistryDate" });

        migrationBuilder.CreateIndex(
            name: "UX_DOC_REG_VISITS_NUMBER_DATE",
            table: "DOCUMENT_REGISTRY_VISITS",
            columns: new[] { "DocumentNumberId", "RegistryDate" },
            unique: true,
            filter: "[MovementStepId] IS NULL");

        migrationBuilder.CreateIndex(
            name: "UX_DOC_REG_VISITS_NUMBER_STEP",
            table: "DOCUMENT_REGISTRY_VISITS",
            columns: new[] { "DocumentNumberId", "MovementStepId" },
            unique: true,
            filter: "[MovementStepId] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_DOC_REL_EVENTS_SOURCE_DATE",
            table: "DOCUMENT_RELATION_EVENTS",
            columns: new[] { "SourceDocumentId", "OccurredAt" });

        migrationBuilder.CreateIndex(
            name: "IX_DOC_REL_EVENTS_TARGET_DATE",
            table: "DOCUMENT_RELATION_EVENTS",
            columns: new[] { "TargetDocumentId", "OccurredAt" });

        migrationBuilder.CreateIndex(
            name: "IX_DOCUMENT_RELATION_EVENTS_ActionTypeId",
            table: "DOCUMENT_RELATION_EVENTS",
            column: "ActionTypeId");

        migrationBuilder.CreateIndex(
            name: "IX_DOCUMENT_RELATION_EVENTS_ActorUserId",
            table: "DOCUMENT_RELATION_EVENTS",
            column: "ActorUserId");

        migrationBuilder.CreateIndex(
            name: "IX_DOCUMENT_RELATION_EVENTS_RelationTypeId",
            table: "DOCUMENT_RELATION_EVENTS",
            column: "RelationTypeId");

        migrationBuilder.CreateIndex(
            name: "IX_DOC_RELATIONS_TARGET",
            table: "DOCUMENT_RELATIONS",
            column: "TargetDocumentId");

        migrationBuilder.CreateIndex(
            name: "IX_DOCUMENT_RELATIONS_CreatedByUserId",
            table: "DOCUMENT_RELATIONS",
            column: "CreatedByUserId");

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
            name: "IX_DOC_STATUS_HISTORY_DOC_DATE",
            table: "DOCUMENT_STATUS_HISTORY",
            columns: new[] { "DocumentId", "ChangedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_DOCUMENT_STATUS_HISTORY_FromStatusId",
            table: "DOCUMENT_STATUS_HISTORY",
            column: "FromStatusId");

        migrationBuilder.CreateIndex(
            name: "IX_DOCUMENT_STATUS_HISTORY_ToStatusId",
            table: "DOCUMENT_STATUS_HISTORY",
            column: "ToStatusId");

        migrationBuilder.CreateIndex(
            name: "IX_DOCUMENT_TYPE_SCHEMA_VERSION_FIELDS_DocumentFieldId",
            table: "DOCUMENT_TYPE_SCHEMA_VERSION_FIELDS",
            column: "DocumentFieldId");

        migrationBuilder.CreateIndex(
            name: "UX_DOC_SCHEMA_VERSION_FIELDS_VERSION_FIELD",
            table: "DOCUMENT_TYPE_SCHEMA_VERSION_FIELDS",
            columns: new[] { "SchemaVersionId", "DocumentFieldId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DOC_SCHEMA_VERSIONS_TYPE_STATUS",
            table: "DOCUMENT_TYPE_SCHEMA_VERSIONS",
            columns: new[] { "DocumentTypeId", "DepartmentId", "Status" });

        migrationBuilder.CreateIndex(
            name: "IX_DOCUMENT_TYPE_SCHEMA_VERSIONS_DepartmentId",
            table: "DOCUMENT_TYPE_SCHEMA_VERSIONS",
            column: "DepartmentId");

        migrationBuilder.CreateIndex(
            name: "UX_DOC_SCHEMA_VERSIONS_ONE_DEPT_DRAFT",
            table: "DOCUMENT_TYPE_SCHEMA_VERSIONS",
            columns: new[] { "DocumentTypeId", "DepartmentId" },
            unique: true,
            filter: "[Status] = 1 AND [DepartmentId] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "UX_DOC_SCHEMA_VERSIONS_ONE_DRAFT",
            table: "DOCUMENT_TYPE_SCHEMA_VERSIONS",
            column: "DocumentTypeId",
            unique: true,
            filter: "[Status] = 1 AND [DepartmentId] IS NULL");

        migrationBuilder.CreateIndex(
            name: "UX_DOC_SCHEMA_VERSIONS_TYPE_DEPT_NUMBER",
            table: "DOCUMENT_TYPE_SCHEMA_VERSIONS",
            columns: new[] { "DocumentTypeId", "DepartmentId", "VersionNumber" },
            unique: true,
            filter: "[DepartmentId] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "UX_DOC_SCHEMA_VERSIONS_TYPE_NUMBER",
            table: "DOCUMENT_TYPE_SCHEMA_VERSIONS",
            columns: new[] { "DocumentTypeId", "VersionNumber" },
            unique: true,
            filter: "[DepartmentId] IS NULL");

        migrationBuilder.CreateIndex(
            name: "IX_DOCUMENT_TYPES_AllowedDirectionId",
            table: "DOCUMENT_TYPES",
            column: "AllowedDirectionId");

        migrationBuilder.CreateIndex(
            name: "IX_DOCUMENTS_ConfidentialityLevelId",
            table: "DOCUMENTS",
            column: "ConfidentialityLevelId");

        migrationBuilder.CreateIndex(
            name: "IX_DOCUMENTS_CurrentStatusId",
            table: "DOCUMENTS",
            column: "CurrentStatusId");

        migrationBuilder.CreateIndex(
            name: "IX_DOCUMENTS_DEPT_STATUS",
            table: "DOCUMENTS",
            columns: new[] { "OwningDepartmentId", "CurrentStatusId" });

        migrationBuilder.CreateIndex(
            name: "IX_DOCUMENTS_DirectionId",
            table: "DOCUMENTS",
            column: "DirectionId");

        migrationBuilder.CreateIndex(
            name: "IX_DOCUMENTS_DocumentTypeId",
            table: "DOCUMENTS",
            column: "DocumentTypeId");

        migrationBuilder.CreateIndex(
            name: "IX_DOCUMENTS_OFFICIAL_NUMBER",
            table: "DOCUMENTS",
            column: "OfficialNumberText");

        migrationBuilder.CreateIndex(
            name: "IX_DOCUMENTS_PriorityLevelId",
            table: "DOCUMENTS",
            column: "PriorityLevelId");

        migrationBuilder.CreateIndex(
            name: "IX_DOCUMENTS_REG_DATE",
            table: "DOCUMENTS",
            column: "RegistrationDate");

        migrationBuilder.CreateIndex(
            name: "IX_DOCUMENTS_SCHEMA_VERSION",
            table: "DOCUMENTS",
            column: "SchemaVersionId");

        migrationBuilder.CreateIndex(
            name: "IX_EMPLOYEES_DEPT",
            table: "EMPLOYEES",
            column: "DepartmentId");

        migrationBuilder.CreateIndex(
            name: "IX_EXTERNAL_ORGANIZATIONS_OrganizationTypeId",
            table: "EXTERNAL_ORGANIZATIONS",
            column: "OrganizationTypeId");

        migrationBuilder.CreateIndex(
            name: "UX_EXTERNAL_ORGS_CODE",
            table: "EXTERNAL_ORGANIZATIONS",
            column: "Code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_FILE_STORAGE_PROVIDERS_ProviderTypeId",
            table: "FILE_STORAGE_PROVIDERS",
            column: "ProviderTypeId");

        migrationBuilder.CreateIndex(
            name: "IX_INSTITUTION_SETTINGS_ARCHIVE_DIWAN",
            table: "INSTITUTION_SETTINGS",
            column: "ArchiveDiwanDepartmentId");

        migrationBuilder.CreateIndex(
            name: "IX_INSTITUTION_SETTINGS_CENTRAL_DIWAN",
            table: "INSTITUTION_SETTINGS",
            column: "CentralDiwanDepartmentId");

        migrationBuilder.CreateIndex(
            name: "IX_INSTITUTION_SETTINGS_NumberingModeId",
            table: "INSTITUTION_SETTINGS",
            column: "NumberingModeId");

        migrationBuilder.CreateIndex(
            name: "IX_INSTITUTION_SETTINGS_NumberingResetPolicyId",
            table: "INSTITUTION_SETTINGS",
            column: "NumberingResetPolicyId");

        migrationBuilder.CreateIndex(
            name: "IX_INSTITUTION_SETTINGS_ResponsibleEmployeeId",
            table: "INSTITUTION_SETTINGS",
            column: "ResponsibleEmployeeId");

        migrationBuilder.CreateIndex(
            name: "IX_NOTIFICATION_OUTBOX_ChannelId",
            table: "NOTIFICATION_OUTBOX",
            column: "ChannelId");

        migrationBuilder.CreateIndex(
            name: "IX_NOTIFICATION_OUTBOX_NotificationId",
            table: "NOTIFICATION_OUTBOX",
            column: "NotificationId");

        migrationBuilder.CreateIndex(
            name: "IX_NOTIFICATION_OUTBOX_STATUS_RETRY",
            table: "NOTIFICATION_OUTBOX",
            columns: new[] { "StatusId", "NextRetryAt" });

        migrationBuilder.CreateIndex(
            name: "IX_NOTIFICATION_RECIPIENTS_DELIVERED",
            table: "NOTIFICATION_RECIPIENTS",
            column: "DeliveredAt");

        migrationBuilder.CreateIndex(
            name: "IX_NOTIFICATION_RECIPIENTS_DepartmentId",
            table: "NOTIFICATION_RECIPIENTS",
            column: "DepartmentId");

        migrationBuilder.CreateIndex(
            name: "IX_NOTIFICATION_RECIPIENTS_INBOX",
            table: "NOTIFICATION_RECIPIENTS",
            columns: new[] { "UserId", "IsDismissed", "DeliveredAt" });

        migrationBuilder.CreateIndex(
            name: "IX_NOTIFICATION_RECIPIENTS_USER_READ",
            table: "NOTIFICATION_RECIPIENTS",
            columns: new[] { "UserId", "IsRead" });

        migrationBuilder.CreateIndex(
            name: "UX_NOTIFICATION_RECIPIENTS_NOTIFICATION_USER",
            table: "NOTIFICATION_RECIPIENTS",
            columns: new[] { "NotificationId", "UserId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_NOTIFICATIONS_CREATED",
            table: "NOTIFICATIONS",
            column: "CreatedAt");

        migrationBuilder.CreateIndex(
            name: "IX_NOTIFICATIONS_NotificationTypeId",
            table: "NOTIFICATIONS",
            column: "NotificationTypeId");

        migrationBuilder.CreateIndex(
            name: "IX_NOTIFICATIONS_PriorityId",
            table: "NOTIFICATIONS",
            column: "PriorityId");

        migrationBuilder.CreateIndex(
            name: "IX_NOTIFICATIONS_RelatedDocumentId",
            table: "NOTIFICATIONS",
            column: "RelatedDocumentId");

        migrationBuilder.CreateIndex(
            name: "IX_NOTIFICATIONS_RelatedMovementId",
            table: "NOTIFICATIONS",
            column: "RelatedMovementId");

        migrationBuilder.CreateIndex(
            name: "UX_NUMBERING_COUNTERS_REG_YEAR",
            table: "NUMBERING_COUNTERS",
            columns: new[] { "RegistryId", "NumberingYear" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_NUMBERING_REGISTRIES_DirectionId",
            table: "NUMBERING_REGISTRIES",
            column: "DirectionId");

        migrationBuilder.CreateIndex(
            name: "IX_NUMBERING_REGISTRIES_DocumentTypeId",
            table: "NUMBERING_REGISTRIES",
            column: "DocumentTypeId");

        migrationBuilder.CreateIndex(
            name: "IX_NUMBERING_REGISTRIES_ResetPolicyId",
            table: "NUMBERING_REGISTRIES",
            column: "ResetPolicyId");

        migrationBuilder.CreateIndex(
            name: "UX_NUMBERING_REGISTRIES_CODE",
            table: "NUMBERING_REGISTRIES",
            column: "Code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PERSONS_GenderId",
            table: "PERSONS",
            column: "GenderId");

        migrationBuilder.CreateIndex(
            name: "IX_PERSONS_NationalityId",
            table: "PERSONS",
            column: "NationalityId");

        migrationBuilder.CreateIndex(
            name: "IX_PERSONS_NORMALIZED_NAME",
            table: "PERSONS",
            column: "NormalizedName");

        migrationBuilder.CreateIndex(
            name: "UX_PERSONS_NATIONAL_NUMBER",
            table: "PERSONS",
            column: "NationalNumber",
            unique: true,
            filter: "[NationalNumber] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "UX_ROLES_CODE",
            table: "ROLES",
            column: "Code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SAVED_SEARCHES_USER",
            table: "SAVED_SEARCHES",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "UX_SAVED_SEARCHES_USER_DEFAULT",
            table: "SAVED_SEARCHES",
            column: "UserId",
            unique: true,
            filter: "[IsDefault] = 1");

        migrationBuilder.CreateIndex(
            name: "UX_SAVED_SEARCHES_USER_NAME",
            table: "SAVED_SEARCHES",
            columns: new[] { "UserId", "Name" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SYSTEM_ERRORS_LAST_SEEN",
            table: "SYSTEM_ERRORS",
            column: "LastSeenAt");

        migrationBuilder.CreateIndex(
            name: "UX_SYSTEM_ERRORS_FINGERPRINT",
            table: "SYSTEM_ERRORS",
            column: "Fingerprint",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_USER_DEPARTMENT_ASSIGNMENTS_AssignmentTypeId",
            table: "USER_DEPARTMENT_ASSIGNMENTS",
            column: "AssignmentTypeId");

        migrationBuilder.CreateIndex(
            name: "IX_USER_DEPT_ASSIGNMENTS_USER_ACTIVE",
            table: "USER_DEPARTMENT_ASSIGNMENTS",
            columns: new[] { "UserId", "IsActive" });

        migrationBuilder.CreateIndex(
            name: "IX_USERS_PRIMARY_DEPT",
            table: "USERS",
            column: "PrimaryDepartmentId");

        migrationBuilder.CreateIndex(
            name: "IX_USERS_ScopeTypeId",
            table: "USERS",
            column: "ScopeTypeId");

        migrationBuilder.CreateIndex(
            name: "UX_USERS_USERNAME",
            table: "USERS",
            column: "Username",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AUDIT_CHECKPOINTS");

        migrationBuilder.DropTable(
            name: "AUDIT_INTEGRITY_CHECKS");

        migrationBuilder.DropTable(
            name: "AUDIT_LOGS");

        migrationBuilder.DropTable(
            name: "DECISION_APPEALS");

        migrationBuilder.DropTable(
            name: "DECISION_TYPE_DOCUMENT_TYPES");

        migrationBuilder.DropTable(
            name: "DEPARTMENT_RECORD_VALUES");

        migrationBuilder.DropTable(
            name: "DOCUMENT_ACCESS_GRANTS");

        migrationBuilder.DropTable(
            name: "DOCUMENT_ARCHIVE_EVENTS");

        migrationBuilder.DropTable(
            name: "DOCUMENT_FIELD_VALUES");

        migrationBuilder.DropTable(
            name: "DOCUMENT_FILE_VERSIONS");

        migrationBuilder.DropTable(
            name: "DOCUMENT_MOVEMENT_EVENTS");

        migrationBuilder.DropTable(
            name: "DOCUMENT_MOVEMENT_STEPS");

        migrationBuilder.DropTable(
            name: "DOCUMENT_NUMBER_HISTORY");

        migrationBuilder.DropTable(
            name: "DOCUMENT_PARTICIPANTS");

        migrationBuilder.DropTable(
            name: "DOCUMENT_PERSONS");

        migrationBuilder.DropTable(
            name: "DOCUMENT_RELATION_EVENTS");

        migrationBuilder.DropTable(
            name: "DOCUMENT_RELATIONS");

        migrationBuilder.DropTable(
            name: "DOCUMENT_STATUS_HISTORY");

        migrationBuilder.DropTable(
            name: "DOCUMENT_TYPE_SCHEMA_VERSION_FIELDS");

        migrationBuilder.DropTable(
            name: "EXTERNAL_ORGANIZATIONS");

        migrationBuilder.DropTable(
            name: "INSTITUTION_LOGOS");

        migrationBuilder.DropTable(
            name: "NOTIFICATION_OUTBOX");

        migrationBuilder.DropTable(
            name: "NOTIFICATION_RECIPIENTS");

        migrationBuilder.DropTable(
            name: "NUMBERING_COUNTERS");

        migrationBuilder.DropTable(
            name: "PERMISSIONS");

        migrationBuilder.DropTable(
            name: "ROLE_PERMISSIONS");

        migrationBuilder.DropTable(
            name: "SAVED_SEARCHES");

        migrationBuilder.DropTable(
            name: "SYSTEM_ERRORS");

        migrationBuilder.DropTable(
            name: "USER_DEPARTMENT_ASSIGNMENTS");

        migrationBuilder.DropTable(
            name: "USER_ROLES");

        migrationBuilder.DropTable(
            name: "AUDIT_ACTION_TYPES");

        migrationBuilder.DropTable(
            name: "AUDIT_ENTITY_TYPES");

        migrationBuilder.DropTable(
            name: "DOCUMENT_DECISIONS");

        migrationBuilder.DropTable(
            name: "ACCESS_LEVELS");

        migrationBuilder.DropTable(
            name: "ARCHIVE_EVENT_TYPES");

        migrationBuilder.DropTable(
            name: "ARCHIVE_LOCATIONS");

        migrationBuilder.DropTable(
            name: "FILE_STORAGE_PROVIDERS");

        migrationBuilder.DropTable(
            name: "MOVEMENT_EVENT_TYPES");

        migrationBuilder.DropTable(
            name: "MOVEMENT_STEP_STATUSES");

        migrationBuilder.DropTable(
            name: "DOCUMENT_NUMBER_EVENT_TYPES");

        migrationBuilder.DropTable(
            name: "PARTICIPANT_ROLES");

        migrationBuilder.DropTable(
            name: "PARTICIPANT_TARGET_TYPES");

        migrationBuilder.DropTable(
            name: "DOCUMENT_PERSON_ROLES");

        migrationBuilder.DropTable(
            name: "PERSONS");

        migrationBuilder.DropTable(
            name: "RELATION_ACTION_TYPES");

        migrationBuilder.DropTable(
            name: "DOCUMENT_RELATION_TYPES");

        migrationBuilder.DropTable(
            name: "DOCUMENT_FIELDS");

        migrationBuilder.DropTable(
            name: "ORGANIZATION_TYPES");

        migrationBuilder.DropTable(
            name: "INSTITUTION_SETTINGS");

        migrationBuilder.DropTable(
            name: "NOTIFICATION_CHANNELS");

        migrationBuilder.DropTable(
            name: "NOTIFICATION_OUTBOX_STATUSES");

        migrationBuilder.DropTable(
            name: "NOTIFICATIONS");

        migrationBuilder.DropTable(
            name: "NUMBERING_REGISTRIES");

        migrationBuilder.DropTable(
            name: "ROLES");

        migrationBuilder.DropTable(
            name: "ASSIGNMENT_TYPES");

        migrationBuilder.DropTable(
            name: "USERS");

        migrationBuilder.DropTable(
            name: "DECISION_TYPES");

        migrationBuilder.DropTable(
            name: "DOCUMENT_FILES");

        migrationBuilder.DropTable(
            name: "DOCUMENT_REGISTRY_VISITS");

        migrationBuilder.DropTable(
            name: "STORAGE_PROVIDER_TYPES");

        migrationBuilder.DropTable(
            name: "GENDERS");

        migrationBuilder.DropTable(
            name: "NATIONALITIES");

        migrationBuilder.DropTable(
            name: "EMPLOYEES");

        migrationBuilder.DropTable(
            name: "NUMBERING_MODES");

        migrationBuilder.DropTable(
            name: "DOCUMENT_MOVEMENTS");

        migrationBuilder.DropTable(
            name: "NOTIFICATION_PRIORITIES");

        migrationBuilder.DropTable(
            name: "NOTIFICATION_TYPES");

        migrationBuilder.DropTable(
            name: "NUMBERING_RESET_POLICIES");

        migrationBuilder.DropTable(
            name: "USER_SCOPE_TYPES");

        migrationBuilder.DropTable(
            name: "DOCUMENT_FILE_TYPES");

        migrationBuilder.DropTable(
            name: "DOCUMENT_NUMBERS");

        migrationBuilder.DropTable(
            name: "MOVEMENT_STATUSES");

        migrationBuilder.DropTable(
            name: "MOVEMENT_TYPES");

        migrationBuilder.DropTable(
            name: "DOCUMENTS");

        migrationBuilder.DropTable(
            name: "DOCUMENT_NUMBER_SCOPES");

        migrationBuilder.DropTable(
            name: "DOCUMENT_NUMBER_STATUSES");

        migrationBuilder.DropTable(
            name: "DOCUMENT_CONFIDENTIALITY_LEVELS");

        migrationBuilder.DropTable(
            name: "DOCUMENT_PRIORITY_LEVELS");

        migrationBuilder.DropTable(
            name: "DOCUMENT_STATUS_TYPES");

        migrationBuilder.DropTable(
            name: "DOCUMENT_TYPE_SCHEMA_VERSIONS");

        migrationBuilder.DropTable(
            name: "DEPARTMENTS");

        migrationBuilder.DropTable(
            name: "DOCUMENT_TYPES");

        migrationBuilder.DropTable(
            name: "DEPARTMENT_TYPES");

        migrationBuilder.DropTable(
            name: "DOCUMENT_DIRECTIONS");
    }
}
