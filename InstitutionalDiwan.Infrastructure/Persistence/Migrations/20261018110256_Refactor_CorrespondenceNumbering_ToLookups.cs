using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstitutionalDiwan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Refactor_CorrespondenceNumbering_ToLookups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DOCUMENTS_DEPT_STATUS",
                table: "DOCUMENTS");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DOCUMENT_TYPES",
                table: "DOCUMENT_TYPES");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DOCUMENT_STATUS_TYPES",
                table: "DOCUMENT_STATUS_TYPES");

            migrationBuilder.DropColumn(
                name: "Direction",
                table: "NUMBERING_REGISTRIES");

            migrationBuilder.DropColumn(
                name: "DocumentTypeCode",
                table: "NUMBERING_REGISTRIES");

            migrationBuilder.DropColumn(
                name: "ResetPolicy",
                table: "NUMBERING_REGISTRIES");

            migrationBuilder.DropColumn(
                name: "ConfidentialityLevel",
                table: "DOCUMENTS");

            migrationBuilder.DropColumn(
                name: "CurrentStatusCode",
                table: "DOCUMENTS");

            migrationBuilder.DropColumn(
                name: "Direction",
                table: "DOCUMENTS");

            migrationBuilder.DropColumn(
                name: "DocumentTypeCode",
                table: "DOCUMENTS");

            migrationBuilder.DropColumn(
                name: "PriorityLevel",
                table: "DOCUMENTS");

            migrationBuilder.DropColumn(
                name: "StatusBeforeArchiveCode",
                table: "DOCUMENTS");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "DOCUMENT_TYPES");

            migrationBuilder.DropColumn(
                name: "AllowedDirectionScope",
                table: "DOCUMENT_TYPES");

            migrationBuilder.DropColumn(
                name: "NameEn",
                table: "DOCUMENT_TYPES");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "DOCUMENT_STATUS_TYPES");

            migrationBuilder.DropColumn(
                name: "NameEn",
                table: "DOCUMENT_STATUS_TYPES");

            migrationBuilder.DropColumn(
                name: "FromStatusCode",
                table: "DOCUMENT_STATUS_HISTORY");

            migrationBuilder.DropColumn(
                name: "ToStatusCode",
                table: "DOCUMENT_STATUS_HISTORY");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "DOCUMENT_NUMBERS");

            migrationBuilder.DropColumn(
                name: "EventType",
                table: "DOCUMENT_NUMBER_HISTORY");

            migrationBuilder.RenameColumn(
                name: "NameAr",
                table: "DOCUMENT_TYPES",
                newName: "DescAr");

            migrationBuilder.RenameColumn(
                name: "NameAr",
                table: "DOCUMENT_STATUS_TYPES",
                newName: "DescAr");

            migrationBuilder.AddColumn<int>(
                name: "DirectionId",
                table: "NUMBERING_REGISTRIES",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DocumentTypeId",
                table: "NUMBERING_REGISTRIES",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResetPolicyId",
                table: "NUMBERING_REGISTRIES",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ConfidentialityLevelId",
                table: "DOCUMENTS",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentStatusId",
                table: "DOCUMENTS",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DirectionId",
                table: "DOCUMENTS",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DocumentTypeId",
                table: "DOCUMENTS",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PriorityLevelId",
                table: "DOCUMENTS",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StatusBeforeArchiveId",
                table: "DOCUMENTS",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "DOCUMENT_TYPES",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AllowedDirectionId",
                table: "DOCUMENT_TYPES",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Desc",
                table: "DOCUMENT_TYPES",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "DOCUMENT_STATUS_TYPES",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Desc",
                table: "DOCUMENT_STATUS_TYPES",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "DOCUMENT_STATUS_TYPES",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "FromStatusId",
                table: "DOCUMENT_STATUS_HISTORY",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ToStatusId",
                table: "DOCUMENT_STATUS_HISTORY",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StatusId",
                table: "DOCUMENT_NUMBERS",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EventTypeId",
                table: "DOCUMENT_NUMBER_HISTORY",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_DOCUMENT_TYPES",
                table: "DOCUMENT_TYPES",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DOCUMENT_STATUS_TYPES",
                table: "DOCUMENT_STATUS_TYPES",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "DOCUMENT_CONFIDENTIALITY_LEVELS",
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
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DOCUMENT_DIRECTIONS", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DOCUMENT_NUMBER_EVENT_TYPES",
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
                    table.PrimaryKey("PK_DOCUMENT_NUMBER_EVENT_TYPES", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DOCUMENT_NUMBER_STATUSES",
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
                    table.PrimaryKey("PK_DOCUMENT_NUMBER_STATUSES", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DOCUMENT_PRIORITY_LEVELS",
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
                    table.PrimaryKey("PK_DOCUMENT_PRIORITY_LEVELS", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NUMBERING_RESET_POLICIES",
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
                    table.PrimaryKey("PK_NUMBERING_RESET_POLICIES", x => x.Id);
                });

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
                name: "IX_DOCUMENTS_PriorityLevelId",
                table: "DOCUMENTS",
                column: "PriorityLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_TYPES_AllowedDirectionId",
                table: "DOCUMENT_TYPES",
                column: "AllowedDirectionId");

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_STATUS_HISTORY_FromStatusId",
                table: "DOCUMENT_STATUS_HISTORY",
                column: "FromStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_STATUS_HISTORY_ToStatusId",
                table: "DOCUMENT_STATUS_HISTORY",
                column: "ToStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_NUMBERS_StatusId",
                table: "DOCUMENT_NUMBERS",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_NUMBER_HISTORY_EventTypeId",
                table: "DOCUMENT_NUMBER_HISTORY",
                column: "EventTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENT_NUMBER_HISTORY_DOCUMENT_NUMBER_EVENT_TYPES_EventTypeId",
                table: "DOCUMENT_NUMBER_HISTORY",
                column: "EventTypeId",
                principalTable: "DOCUMENT_NUMBER_EVENT_TYPES",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENT_NUMBERS_DOCUMENT_NUMBER_STATUSES_StatusId",
                table: "DOCUMENT_NUMBERS",
                column: "StatusId",
                principalTable: "DOCUMENT_NUMBER_STATUSES",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENT_STATUS_HISTORY_DOCUMENT_STATUS_TYPES_FromStatusId",
                table: "DOCUMENT_STATUS_HISTORY",
                column: "FromStatusId",
                principalTable: "DOCUMENT_STATUS_TYPES",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENT_STATUS_HISTORY_DOCUMENT_STATUS_TYPES_ToStatusId",
                table: "DOCUMENT_STATUS_HISTORY",
                column: "ToStatusId",
                principalTable: "DOCUMENT_STATUS_TYPES",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENT_TYPES_DOCUMENT_DIRECTIONS_AllowedDirectionId",
                table: "DOCUMENT_TYPES",
                column: "AllowedDirectionId",
                principalTable: "DOCUMENT_DIRECTIONS",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENTS_DOCUMENT_CONFIDENTIALITY_LEVELS_ConfidentialityLevelId",
                table: "DOCUMENTS",
                column: "ConfidentialityLevelId",
                principalTable: "DOCUMENT_CONFIDENTIALITY_LEVELS",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENTS_DOCUMENT_DIRECTIONS_DirectionId",
                table: "DOCUMENTS",
                column: "DirectionId",
                principalTable: "DOCUMENT_DIRECTIONS",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENTS_DOCUMENT_PRIORITY_LEVELS_PriorityLevelId",
                table: "DOCUMENTS",
                column: "PriorityLevelId",
                principalTable: "DOCUMENT_PRIORITY_LEVELS",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENTS_DOCUMENT_STATUS_TYPES_CurrentStatusId",
                table: "DOCUMENTS",
                column: "CurrentStatusId",
                principalTable: "DOCUMENT_STATUS_TYPES",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENTS_DOCUMENT_TYPES_DocumentTypeId",
                table: "DOCUMENTS",
                column: "DocumentTypeId",
                principalTable: "DOCUMENT_TYPES",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NUMBERING_REGISTRIES_DOCUMENT_DIRECTIONS_DirectionId",
                table: "NUMBERING_REGISTRIES",
                column: "DirectionId",
                principalTable: "DOCUMENT_DIRECTIONS",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NUMBERING_REGISTRIES_DOCUMENT_TYPES_DocumentTypeId",
                table: "NUMBERING_REGISTRIES",
                column: "DocumentTypeId",
                principalTable: "DOCUMENT_TYPES",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NUMBERING_REGISTRIES_NUMBERING_RESET_POLICIES_ResetPolicyId",
                table: "NUMBERING_REGISTRIES",
                column: "ResetPolicyId",
                principalTable: "NUMBERING_RESET_POLICIES",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENT_NUMBER_HISTORY_DOCUMENT_NUMBER_EVENT_TYPES_EventTypeId",
                table: "DOCUMENT_NUMBER_HISTORY");

            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENT_NUMBERS_DOCUMENT_NUMBER_STATUSES_StatusId",
                table: "DOCUMENT_NUMBERS");

            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENT_STATUS_HISTORY_DOCUMENT_STATUS_TYPES_FromStatusId",
                table: "DOCUMENT_STATUS_HISTORY");

            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENT_STATUS_HISTORY_DOCUMENT_STATUS_TYPES_ToStatusId",
                table: "DOCUMENT_STATUS_HISTORY");

            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENT_TYPES_DOCUMENT_DIRECTIONS_AllowedDirectionId",
                table: "DOCUMENT_TYPES");

            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENTS_DOCUMENT_CONFIDENTIALITY_LEVELS_ConfidentialityLevelId",
                table: "DOCUMENTS");

            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENTS_DOCUMENT_DIRECTIONS_DirectionId",
                table: "DOCUMENTS");

            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENTS_DOCUMENT_PRIORITY_LEVELS_PriorityLevelId",
                table: "DOCUMENTS");

            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENTS_DOCUMENT_STATUS_TYPES_CurrentStatusId",
                table: "DOCUMENTS");

            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENTS_DOCUMENT_TYPES_DocumentTypeId",
                table: "DOCUMENTS");

            migrationBuilder.DropForeignKey(
                name: "FK_NUMBERING_REGISTRIES_DOCUMENT_DIRECTIONS_DirectionId",
                table: "NUMBERING_REGISTRIES");

            migrationBuilder.DropForeignKey(
                name: "FK_NUMBERING_REGISTRIES_DOCUMENT_TYPES_DocumentTypeId",
                table: "NUMBERING_REGISTRIES");

            migrationBuilder.DropForeignKey(
                name: "FK_NUMBERING_REGISTRIES_NUMBERING_RESET_POLICIES_ResetPolicyId",
                table: "NUMBERING_REGISTRIES");

            migrationBuilder.DropTable(
                name: "DOCUMENT_CONFIDENTIALITY_LEVELS");

            migrationBuilder.DropTable(
                name: "DOCUMENT_DIRECTIONS");

            migrationBuilder.DropTable(
                name: "DOCUMENT_NUMBER_EVENT_TYPES");

            migrationBuilder.DropTable(
                name: "DOCUMENT_NUMBER_STATUSES");

            migrationBuilder.DropTable(
                name: "DOCUMENT_PRIORITY_LEVELS");

            migrationBuilder.DropTable(
                name: "NUMBERING_RESET_POLICIES");

            migrationBuilder.DropIndex(
                name: "IX_NUMBERING_REGISTRIES_DirectionId",
                table: "NUMBERING_REGISTRIES");

            migrationBuilder.DropIndex(
                name: "IX_NUMBERING_REGISTRIES_DocumentTypeId",
                table: "NUMBERING_REGISTRIES");

            migrationBuilder.DropIndex(
                name: "IX_NUMBERING_REGISTRIES_ResetPolicyId",
                table: "NUMBERING_REGISTRIES");

            migrationBuilder.DropIndex(
                name: "IX_DOCUMENTS_ConfidentialityLevelId",
                table: "DOCUMENTS");

            migrationBuilder.DropIndex(
                name: "IX_DOCUMENTS_CurrentStatusId",
                table: "DOCUMENTS");

            migrationBuilder.DropIndex(
                name: "IX_DOCUMENTS_DEPT_STATUS",
                table: "DOCUMENTS");

            migrationBuilder.DropIndex(
                name: "IX_DOCUMENTS_DirectionId",
                table: "DOCUMENTS");

            migrationBuilder.DropIndex(
                name: "IX_DOCUMENTS_DocumentTypeId",
                table: "DOCUMENTS");

            migrationBuilder.DropIndex(
                name: "IX_DOCUMENTS_PriorityLevelId",
                table: "DOCUMENTS");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DOCUMENT_TYPES",
                table: "DOCUMENT_TYPES");

            migrationBuilder.DropIndex(
                name: "IX_DOCUMENT_TYPES_AllowedDirectionId",
                table: "DOCUMENT_TYPES");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DOCUMENT_STATUS_TYPES",
                table: "DOCUMENT_STATUS_TYPES");

            migrationBuilder.DropIndex(
                name: "IX_DOCUMENT_STATUS_HISTORY_FromStatusId",
                table: "DOCUMENT_STATUS_HISTORY");

            migrationBuilder.DropIndex(
                name: "IX_DOCUMENT_STATUS_HISTORY_ToStatusId",
                table: "DOCUMENT_STATUS_HISTORY");

            migrationBuilder.DropIndex(
                name: "IX_DOCUMENT_NUMBERS_StatusId",
                table: "DOCUMENT_NUMBERS");

            migrationBuilder.DropIndex(
                name: "IX_DOCUMENT_NUMBER_HISTORY_EventTypeId",
                table: "DOCUMENT_NUMBER_HISTORY");

            migrationBuilder.DropColumn(
                name: "DirectionId",
                table: "NUMBERING_REGISTRIES");

            migrationBuilder.DropColumn(
                name: "DocumentTypeId",
                table: "NUMBERING_REGISTRIES");

            migrationBuilder.DropColumn(
                name: "ResetPolicyId",
                table: "NUMBERING_REGISTRIES");

            migrationBuilder.DropColumn(
                name: "ConfidentialityLevelId",
                table: "DOCUMENTS");

            migrationBuilder.DropColumn(
                name: "CurrentStatusId",
                table: "DOCUMENTS");

            migrationBuilder.DropColumn(
                name: "DirectionId",
                table: "DOCUMENTS");

            migrationBuilder.DropColumn(
                name: "DocumentTypeId",
                table: "DOCUMENTS");

            migrationBuilder.DropColumn(
                name: "PriorityLevelId",
                table: "DOCUMENTS");

            migrationBuilder.DropColumn(
                name: "StatusBeforeArchiveId",
                table: "DOCUMENTS");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "DOCUMENT_TYPES");

            migrationBuilder.DropColumn(
                name: "AllowedDirectionId",
                table: "DOCUMENT_TYPES");

            migrationBuilder.DropColumn(
                name: "Desc",
                table: "DOCUMENT_TYPES");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "DOCUMENT_STATUS_TYPES");

            migrationBuilder.DropColumn(
                name: "Desc",
                table: "DOCUMENT_STATUS_TYPES");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "DOCUMENT_STATUS_TYPES");

            migrationBuilder.DropColumn(
                name: "FromStatusId",
                table: "DOCUMENT_STATUS_HISTORY");

            migrationBuilder.DropColumn(
                name: "ToStatusId",
                table: "DOCUMENT_STATUS_HISTORY");

            migrationBuilder.DropColumn(
                name: "StatusId",
                table: "DOCUMENT_NUMBERS");

            migrationBuilder.DropColumn(
                name: "EventTypeId",
                table: "DOCUMENT_NUMBER_HISTORY");

            migrationBuilder.RenameColumn(
                name: "DescAr",
                table: "DOCUMENT_TYPES",
                newName: "NameAr");

            migrationBuilder.RenameColumn(
                name: "DescAr",
                table: "DOCUMENT_STATUS_TYPES",
                newName: "NameAr");

            migrationBuilder.AddColumn<string>(
                name: "Direction",
                table: "NUMBERING_REGISTRIES",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentTypeCode",
                table: "NUMBERING_REGISTRIES",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResetPolicy",
                table: "NUMBERING_REGISTRIES",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ConfidentialityLevel",
                table: "DOCUMENTS",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CurrentStatusCode",
                table: "DOCUMENTS",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Direction",
                table: "DOCUMENTS",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DocumentTypeCode",
                table: "DOCUMENTS",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PriorityLevel",
                table: "DOCUMENTS",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StatusBeforeArchiveCode",
                table: "DOCUMENTS",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "DOCUMENT_TYPES",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AllowedDirectionScope",
                table: "DOCUMENT_TYPES",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameEn",
                table: "DOCUMENT_TYPES",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "DOCUMENT_STATUS_TYPES",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameEn",
                table: "DOCUMENT_STATUS_TYPES",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FromStatusCode",
                table: "DOCUMENT_STATUS_HISTORY",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ToStatusCode",
                table: "DOCUMENT_STATUS_HISTORY",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "DOCUMENT_NUMBERS",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EventType",
                table: "DOCUMENT_NUMBER_HISTORY",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DOCUMENT_TYPES",
                table: "DOCUMENT_TYPES",
                column: "Code");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DOCUMENT_STATUS_TYPES",
                table: "DOCUMENT_STATUS_TYPES",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENTS_DEPT_STATUS",
                table: "DOCUMENTS",
                columns: new[] { "OwningDepartmentId", "CurrentStatusCode" });
        }
    }
}
