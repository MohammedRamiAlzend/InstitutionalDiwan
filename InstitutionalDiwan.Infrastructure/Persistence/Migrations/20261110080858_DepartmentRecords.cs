using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstitutionalDiwan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DepartmentRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DOC_SCHEMA_VERSIONS_TYPE_STATUS",
                table: "DOCUMENT_TYPE_SCHEMA_VERSIONS");

            migrationBuilder.DropIndex(
                name: "UX_DOC_SCHEMA_VERSIONS_ONE_DRAFT",
                table: "DOCUMENT_TYPE_SCHEMA_VERSIONS");

            migrationBuilder.DropIndex(
                name: "UX_DOC_SCHEMA_VERSIONS_TYPE_NUMBER",
                table: "DOCUMENT_TYPE_SCHEMA_VERSIONS");

            migrationBuilder.DropIndex(
                name: "UX_DOC_REG_VISITS_NUMBER_DATE",
                table: "DOCUMENT_REGISTRY_VISITS");

            migrationBuilder.DropIndex(
                name: "UX_DOCUMENT_FIELDS_TYPE_KEY",
                table: "DOCUMENT_FIELDS");

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "DOCUMENT_TYPE_SCHEMA_VERSIONS",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CorrectedAt",
                table: "DOCUMENT_REGISTRY_VISITS",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CorrectedByUserId",
                table: "DOCUMENT_REGISTRY_VISITS",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrectionReason",
                table: "DOCUMENT_REGISTRY_VISITS",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedAt",
                table: "DOCUMENT_REGISTRY_VISITS",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RecordUpdatedAt",
                table: "DOCUMENT_REGISTRY_VISITS",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RecordUpdatedByUserId",
                table: "DOCUMENT_REGISTRY_VISITS",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SchemaVersionId",
                table: "DOCUMENT_REGISTRY_VISITS",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "DOCUMENT_FIELDS",
                type: "uniqueidentifier",
                nullable: true);

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
                name: "IX_DOCUMENT_REGISTRY_VISITS_SchemaVersionId",
                table: "DOCUMENT_REGISTRY_VISITS",
                column: "SchemaVersionId");

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
                name: "IX_DEPARTMENT_RECORD_VALUES_DocumentFieldId",
                table: "DEPARTMENT_RECORD_VALUES",
                column: "DocumentFieldId");

            migrationBuilder.CreateIndex(
                name: "UX_DEPT_RECORD_VALUES_VISIT_FIELD",
                table: "DEPARTMENT_RECORD_VALUES",
                columns: new[] { "VisitId", "DocumentFieldId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENT_FIELDS_DEPARTMENTS_DepartmentId",
                table: "DOCUMENT_FIELDS",
                column: "DepartmentId",
                principalTable: "DEPARTMENTS",
                principalColumn: "DepartmentId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENT_REGISTRY_VISITS_DOCUMENT_TYPE_SCHEMA_VERSIONS_SchemaVersionId",
                table: "DOCUMENT_REGISTRY_VISITS",
                column: "SchemaVersionId",
                principalTable: "DOCUMENT_TYPE_SCHEMA_VERSIONS",
                principalColumn: "SchemaVersionId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENT_TYPE_SCHEMA_VERSIONS_DEPARTMENTS_DepartmentId",
                table: "DOCUMENT_TYPE_SCHEMA_VERSIONS",
                column: "DepartmentId",
                principalTable: "DEPARTMENTS",
                principalColumn: "DepartmentId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENT_FIELDS_DEPARTMENTS_DepartmentId",
                table: "DOCUMENT_FIELDS");

            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENT_REGISTRY_VISITS_DOCUMENT_TYPE_SCHEMA_VERSIONS_SchemaVersionId",
                table: "DOCUMENT_REGISTRY_VISITS");

            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENT_TYPE_SCHEMA_VERSIONS_DEPARTMENTS_DepartmentId",
                table: "DOCUMENT_TYPE_SCHEMA_VERSIONS");

            migrationBuilder.DropTable(
                name: "DEPARTMENT_RECORD_VALUES");

            migrationBuilder.DropIndex(
                name: "IX_DOC_SCHEMA_VERSIONS_TYPE_STATUS",
                table: "DOCUMENT_TYPE_SCHEMA_VERSIONS");

            migrationBuilder.DropIndex(
                name: "IX_DOCUMENT_TYPE_SCHEMA_VERSIONS_DepartmentId",
                table: "DOCUMENT_TYPE_SCHEMA_VERSIONS");

            migrationBuilder.DropIndex(
                name: "UX_DOC_SCHEMA_VERSIONS_ONE_DEPT_DRAFT",
                table: "DOCUMENT_TYPE_SCHEMA_VERSIONS");

            migrationBuilder.DropIndex(
                name: "UX_DOC_SCHEMA_VERSIONS_ONE_DRAFT",
                table: "DOCUMENT_TYPE_SCHEMA_VERSIONS");

            migrationBuilder.DropIndex(
                name: "UX_DOC_SCHEMA_VERSIONS_TYPE_DEPT_NUMBER",
                table: "DOCUMENT_TYPE_SCHEMA_VERSIONS");

            migrationBuilder.DropIndex(
                name: "UX_DOC_SCHEMA_VERSIONS_TYPE_NUMBER",
                table: "DOCUMENT_TYPE_SCHEMA_VERSIONS");

            migrationBuilder.DropIndex(
                name: "IX_DOCUMENT_REGISTRY_VISITS_SchemaVersionId",
                table: "DOCUMENT_REGISTRY_VISITS");

            migrationBuilder.DropIndex(
                name: "UX_DOC_REG_VISITS_NUMBER_DATE",
                table: "DOCUMENT_REGISTRY_VISITS");

            migrationBuilder.DropIndex(
                name: "UX_DOC_REG_VISITS_NUMBER_STEP",
                table: "DOCUMENT_REGISTRY_VISITS");

            migrationBuilder.DropIndex(
                name: "IX_DOCUMENT_FIELDS_DepartmentId",
                table: "DOCUMENT_FIELDS");

            migrationBuilder.DropIndex(
                name: "UX_DOCUMENT_FIELDS_TYPE_DEPT_KEY",
                table: "DOCUMENT_FIELDS");

            migrationBuilder.DropIndex(
                name: "UX_DOCUMENT_FIELDS_TYPE_KEY",
                table: "DOCUMENT_FIELDS");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "DOCUMENT_TYPE_SCHEMA_VERSIONS");

            migrationBuilder.DropColumn(
                name: "CorrectedAt",
                table: "DOCUMENT_REGISTRY_VISITS");

            migrationBuilder.DropColumn(
                name: "CorrectedByUserId",
                table: "DOCUMENT_REGISTRY_VISITS");

            migrationBuilder.DropColumn(
                name: "CorrectionReason",
                table: "DOCUMENT_REGISTRY_VISITS");

            migrationBuilder.DropColumn(
                name: "LockedAt",
                table: "DOCUMENT_REGISTRY_VISITS");

            migrationBuilder.DropColumn(
                name: "RecordUpdatedAt",
                table: "DOCUMENT_REGISTRY_VISITS");

            migrationBuilder.DropColumn(
                name: "RecordUpdatedByUserId",
                table: "DOCUMENT_REGISTRY_VISITS");

            migrationBuilder.DropColumn(
                name: "SchemaVersionId",
                table: "DOCUMENT_REGISTRY_VISITS");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "DOCUMENT_FIELDS");

            migrationBuilder.CreateIndex(
                name: "IX_DOC_SCHEMA_VERSIONS_TYPE_STATUS",
                table: "DOCUMENT_TYPE_SCHEMA_VERSIONS",
                columns: new[] { "DocumentTypeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_DOC_SCHEMA_VERSIONS_ONE_DRAFT",
                table: "DOCUMENT_TYPE_SCHEMA_VERSIONS",
                column: "DocumentTypeId",
                unique: true,
                filter: "[Status] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_DOC_SCHEMA_VERSIONS_TYPE_NUMBER",
                table: "DOCUMENT_TYPE_SCHEMA_VERSIONS",
                columns: new[] { "DocumentTypeId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_DOC_REG_VISITS_NUMBER_DATE",
                table: "DOCUMENT_REGISTRY_VISITS",
                columns: new[] { "DocumentNumberId", "RegistryDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_DOCUMENT_FIELDS_TYPE_KEY",
                table: "DOCUMENT_FIELDS",
                columns: new[] { "DocumentTypeId", "Key" },
                unique: true);
        }
    }
}
