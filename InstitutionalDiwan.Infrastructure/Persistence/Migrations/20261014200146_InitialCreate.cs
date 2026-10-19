using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstitutionalDiwan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "AUDIT_LOGS",
                columns: table => new
                {
                    AuditLogId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActorDepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModuleName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
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
                });

            migrationBuilder.CreateTable(
                name: "DEPARTMENT_TYPES",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    HierarchyLevelOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DEPARTMENT_TYPES", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "DOCUMENT_ACCESS_GRANTS",
                columns: table => new
                {
                    GrantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccessLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
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
                });

            migrationBuilder.CreateTable(
                name: "DOCUMENT_ARCHIVE_EVENTS",
                columns: table => new
                {
                    ArchiveEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PreviousStatusCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NewStatusCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ArchiveLocationCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DOCUMENT_ARCHIVE_EVENTS", x => x.ArchiveEventId);
                });

            migrationBuilder.CreateTable(
                name: "DOCUMENT_FILE_TYPES",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    AllowedExtensionsCsv = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    MaxFileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DOCUMENT_FILE_TYPES", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "DOCUMENT_FILES",
                columns: table => new
                {
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileTypeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
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
                });

            migrationBuilder.CreateTable(
                name: "DOCUMENT_MOVEMENTS",
                columns: table => new
                {
                    MovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MovementType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceDepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DestinationDepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StatusCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
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
                });

            migrationBuilder.CreateTable(
                name: "DOCUMENT_NUMBERS",
                columns: table => new
                {
                    DocumentNumberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegistryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumberingYear = table.Column<int>(type: "int", nullable: false),
                    SequenceValue = table.Column<long>(type: "bigint", nullable: false),
                    OfficialNumberText = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IssuedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    ReplacedByNumberId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DOCUMENT_NUMBERS", x => x.DocumentNumberId);
                });

            migrationBuilder.CreateTable(
                name: "DOCUMENT_RELATION_EVENTS",
                columns: table => new
                {
                    RelationEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelationTypeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DOCUMENT_RELATION_EVENTS", x => x.RelationEventId);
                });

            migrationBuilder.CreateTable(
                name: "DOCUMENT_RELATION_TYPES",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    IsSymmetric = table.Column<bool>(type: "bit", nullable: false),
                    PreventCycles = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DOCUMENT_RELATION_TYPES", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "DOCUMENT_RELATIONS",
                columns: table => new
                {
                    RelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelationTypeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DOCUMENT_RELATIONS", x => x.RelationId);
                });

            migrationBuilder.CreateTable(
                name: "DOCUMENT_STATUS_TYPES",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    IsTerminal = table.Column<bool>(type: "bit", nullable: false),
                    IsSystemStatus = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DOCUMENT_STATUS_TYPES", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "DOCUMENT_TYPES",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    AllowedDirectionScope = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RequiresOfficialNumber = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DOCUMENT_TYPES", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "DOCUMENTS",
                columns: table => new
                {
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SubjectSummary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DocumentTypeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ConfidentialityLevel = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PriorityLevel = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CurrentStatusCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StatusBeforeArchiveCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OwningDepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActiveOfficialNumberId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OfficialNumberText = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DocumentDate = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    RegistrationDate = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    ExternalReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ExternalDocumentDate = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DOCUMENTS", x => x.DocumentId);
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
                name: "EXTERNAL_ORGANIZATIONS",
                columns: table => new
                {
                    ExternalOrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    OrganizationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ContactInfo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EXTERNAL_ORGANIZATIONS", x => x.ExternalOrganizationId);
                });

            migrationBuilder.CreateTable(
                name: "FILE_STORAGE_PROVIDERS",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ProviderType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BasePathOrEndpoint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FILE_STORAGE_PROVIDERS", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "INSTITUTION_SETTINGS",
                columns: table => new
                {
                    SettingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstitutionNameAr = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    InstitutionNameEn = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    LogoPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NumberingMode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NumberingResetPolicy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DefaultTimezone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_INSTITUTION_SETTINGS", x => x.SettingId);
                });

            migrationBuilder.CreateTable(
                name: "NOTIFICATION_TYPES",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TitleTemplateAr = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    BodyTemplateAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DefaultChannelsCsv = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NOTIFICATION_TYPES", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "NOTIFICATIONS",
                columns: table => new
                {
                    NotificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificationTypeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TitleAr = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    BodyAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RelatedDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RelatedMovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActionUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NOTIFICATIONS", x => x.NotificationId);
                });

            migrationBuilder.CreateTable(
                name: "NUMBERING_REGISTRIES",
                columns: table => new
                {
                    RegistryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DocumentTypeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Direction = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    FormatPattern = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ResetPolicy = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NUMBERING_REGISTRIES", x => x.RegistryId);
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
                    ScopeType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    MustChangePassword = table.Column<bool>(type: "bit", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USERS", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "DEPARTMENTS",
                columns: table => new
                {
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DepartmentTypeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
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
                        name: "FK_DEPARTMENTS_DEPARTMENT_TYPES_DepartmentTypeCode",
                        column: x => x.DepartmentTypeCode,
                        principalTable: "DEPARTMENT_TYPES",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DOCUMENT_FILE_VERSIONS",
                columns: table => new
                {
                    VersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    StorageProviderCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
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
                });

            migrationBuilder.CreateTable(
                name: "DOCUMENT_MOVEMENT_EVENTS",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepNo = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
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
                    StepStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OutcomeNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    ReceivedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                });

            migrationBuilder.CreateTable(
                name: "DOCUMENT_NUMBER_HISTORY",
                columns: table => new
                {
                    HistoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentNumberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "DOCUMENT_PARTICIPANTS",
                columns: table => new
                {
                    ParticipantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TargetType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
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
                });

            migrationBuilder.CreateTable(
                name: "DOCUMENT_STATUS_HISTORY",
                columns: table => new
                {
                    HistoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatusCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ToStatusCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
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
                });

            migrationBuilder.CreateTable(
                name: "NOTIFICATION_OUTBOX",
                columns: table => new
                {
                    OutboxId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
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
                        name: "FK_NOTIFICATION_RECIPIENTS_NOTIFICATIONS_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "NOTIFICATIONS",
                        principalColumn: "NotificationId",
                        onDelete: ReferentialAction.Cascade);
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
                name: "USER_DEPARTMENT_ASSIGNMENTS",
                columns: table => new
                {
                    AssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignmentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_AUDIT_LOGS_DOC",
                table: "AUDIT_LOGS",
                column: "RelatedDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_AUDIT_LOGS_OCCURRED_AT",
                table: "AUDIT_LOGS",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_DEPARTMENTS_PARENT",
                table: "DEPARTMENTS",
                column: "ParentDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DEPARTMENTS_TYPE_ACTIVE",
                table: "DEPARTMENTS",
                columns: new[] { "DepartmentTypeCode", "IsActive" });

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
                name: "IX_DOC_ARCHIVE_EVENTS_DOC_DATE",
                table: "DOCUMENT_ARCHIVE_EVENTS",
                columns: new[] { "DocumentId", "OccurredAt" });

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
                name: "IX_DOC_MOVEMENT_EVENTS_MOV_DATE",
                table: "DOCUMENT_MOVEMENT_EVENTS",
                columns: new[] { "MovementId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "UX_DOC_MOVEMENT_STEPS_NO",
                table: "DOCUMENT_MOVEMENT_STEPS",
                columns: new[] { "MovementId", "StepNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DOC_MOVEMENTS_DEST_STATUS",
                table: "DOCUMENT_MOVEMENTS",
                columns: new[] { "DestinationDepartmentId", "StatusCode" });

            migrationBuilder.CreateIndex(
                name: "IX_DOC_MOVEMENTS_DOC_STATUS",
                table: "DOCUMENT_MOVEMENTS",
                columns: new[] { "DocumentId", "StatusCode" });

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_NUMBER_HISTORY_DocumentNumberId",
                table: "DOCUMENT_NUMBER_HISTORY",
                column: "DocumentNumberId");

            migrationBuilder.CreateIndex(
                name: "IX_DOC_NUMBERS_TEXT",
                table: "DOCUMENT_NUMBERS",
                column: "OfficialNumberText");

            migrationBuilder.CreateIndex(
                name: "UX_DOC_NUMBERS_REG_YEAR_SEQ",
                table: "DOCUMENT_NUMBERS",
                columns: new[] { "RegistryId", "NumberingYear", "SequenceValue" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DOC_PARTICIPANTS_DOC",
                table: "DOCUMENT_PARTICIPANTS",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "UX_DOC_RELATIONS_SRC_TGT_TYPE",
                table: "DOCUMENT_RELATIONS",
                columns: new[] { "SourceDocumentId", "TargetDocumentId", "RelationTypeCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DOC_STATUS_HISTORY_DOC_DATE",
                table: "DOCUMENT_STATUS_HISTORY",
                columns: new[] { "DocumentId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENTS_DEPT_STATUS",
                table: "DOCUMENTS",
                columns: new[] { "OwningDepartmentId", "CurrentStatusCode" });

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENTS_OFFICIAL_NUMBER",
                table: "DOCUMENTS",
                column: "OfficialNumberText");

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENTS_REG_DATE",
                table: "DOCUMENTS",
                column: "RegistrationDate");

            migrationBuilder.CreateIndex(
                name: "IX_EMPLOYEES_DEPT",
                table: "EMPLOYEES",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "UX_EXTERNAL_ORGS_CODE",
                table: "EXTERNAL_ORGANIZATIONS",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NOTIFICATION_OUTBOX_NotificationId",
                table: "NOTIFICATION_OUTBOX",
                column: "NotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_NOTIFICATION_OUTBOX_STATUS_RETRY",
                table: "NOTIFICATION_OUTBOX",
                columns: new[] { "Status", "NextRetryAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NOTIFICATION_RECIPIENTS_NotificationId",
                table: "NOTIFICATION_RECIPIENTS",
                column: "NotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_NOTIFICATION_RECIPIENTS_USER_READ",
                table: "NOTIFICATION_RECIPIENTS",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "UX_NUMBERING_COUNTERS_REG_YEAR",
                table: "NUMBERING_COUNTERS",
                columns: new[] { "RegistryId", "NumberingYear" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_NUMBERING_REGISTRIES_CODE",
                table: "NUMBERING_REGISTRIES",
                column: "Code",
                unique: true);

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
                name: "IX_USER_DEPT_ASSIGNMENTS_USER_ACTIVE",
                table: "USER_DEPARTMENT_ASSIGNMENTS",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_USERS_PRIMARY_DEPT",
                table: "USERS",
                column: "PrimaryDepartmentId");

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
                name: "DEPARTMENTS");

            migrationBuilder.DropTable(
                name: "DOCUMENT_ACCESS_GRANTS");

            migrationBuilder.DropTable(
                name: "DOCUMENT_ARCHIVE_EVENTS");

            migrationBuilder.DropTable(
                name: "DOCUMENT_FILE_TYPES");

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
                name: "DOCUMENT_RELATION_EVENTS");

            migrationBuilder.DropTable(
                name: "DOCUMENT_RELATION_TYPES");

            migrationBuilder.DropTable(
                name: "DOCUMENT_RELATIONS");

            migrationBuilder.DropTable(
                name: "DOCUMENT_STATUS_HISTORY");

            migrationBuilder.DropTable(
                name: "DOCUMENT_STATUS_TYPES");

            migrationBuilder.DropTable(
                name: "DOCUMENT_TYPES");

            migrationBuilder.DropTable(
                name: "EMPLOYEES");

            migrationBuilder.DropTable(
                name: "EXTERNAL_ORGANIZATIONS");

            migrationBuilder.DropTable(
                name: "FILE_STORAGE_PROVIDERS");

            migrationBuilder.DropTable(
                name: "INSTITUTION_SETTINGS");

            migrationBuilder.DropTable(
                name: "NOTIFICATION_OUTBOX");

            migrationBuilder.DropTable(
                name: "NOTIFICATION_RECIPIENTS");

            migrationBuilder.DropTable(
                name: "NOTIFICATION_TYPES");

            migrationBuilder.DropTable(
                name: "NUMBERING_COUNTERS");

            migrationBuilder.DropTable(
                name: "PERMISSIONS");

            migrationBuilder.DropTable(
                name: "ROLE_PERMISSIONS");

            migrationBuilder.DropTable(
                name: "SAVED_SEARCHES");

            migrationBuilder.DropTable(
                name: "USER_DEPARTMENT_ASSIGNMENTS");

            migrationBuilder.DropTable(
                name: "USER_ROLES");

            migrationBuilder.DropTable(
                name: "DEPARTMENT_TYPES");

            migrationBuilder.DropTable(
                name: "DOCUMENT_FILES");

            migrationBuilder.DropTable(
                name: "DOCUMENT_MOVEMENTS");

            migrationBuilder.DropTable(
                name: "DOCUMENT_NUMBERS");

            migrationBuilder.DropTable(
                name: "DOCUMENTS");

            migrationBuilder.DropTable(
                name: "NOTIFICATIONS");

            migrationBuilder.DropTable(
                name: "NUMBERING_REGISTRIES");

            migrationBuilder.DropTable(
                name: "ROLES");

            migrationBuilder.DropTable(
                name: "USERS");
        }
    }
}
