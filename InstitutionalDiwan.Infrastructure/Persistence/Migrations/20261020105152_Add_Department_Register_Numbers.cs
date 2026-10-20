using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstitutionalDiwan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_Department_Register_Numbers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_DOC_NUMBERS_REG_YEAR_SEQ",
                table: "DOCUMENT_NUMBERS");

            migrationBuilder.DropIndex(
                name: "UX_DOC_NUMBERS_TEXT_YEAR",
                table: "DOCUMENT_NUMBERS");

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "DOCUMENT_NUMBERS",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegistryDate",
                table: "DOCUMENT_NUMBERS",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScopeId",
                table: "DOCUMENT_NUMBERS",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveDate",
                table: "DOCUMENT_NUMBER_HISTORY",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RegistryNumberId",
                table: "DOCUMENT_MOVEMENT_STEPS",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DOCUMENT_NUMBER_SCOPES",
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
                    table.PrimaryKey("PK_DOCUMENT_NUMBER_SCOPES", x => x.Id);
                });

            // The seeded lookup is inserted here so existing DOCUMENT_NUMBERS rows, which are
            // backfilled with ScopeId = 1 below, satisfy the new foreign key immediately.
            migrationBuilder.InsertData(
                table: "DOCUMENT_NUMBER_SCOPES",
                columns: new[] { "Id", "CreatedAt", "Desc", "DescAr", "IsActive" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Central Diwan register", "سجل الديوان المركزي", true },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Department register", "سجل الدائرة", true }
                });

            // Every number issued before this migration belongs to the central register; its register
            // date is the moment it was issued.
            migrationBuilder.Sql(
                "UPDATE DOCUMENT_NUMBERS SET RegistryDate = IssuedAt WHERE RegistryDate IS NULL;");

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
                name: "UX_DOC_NUMBERS_DEPT_TEXT_YEAR",
                table: "DOCUMENT_NUMBERS",
                columns: new[] { "RegistryId", "OfficialNumberText", "NumberingYear" },
                unique: true,
                filter: "[ScopeId] = 2");

            migrationBuilder.CreateIndex(
                name: "UX_DOC_NUMBERS_DOC_DEPT",
                table: "DOCUMENT_NUMBERS",
                columns: new[] { "DocumentId", "DepartmentId" },
                unique: true,
                filter: "[DepartmentId] IS NOT NULL");

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

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENT_NUMBERS_DEPARTMENTS_DepartmentId",
                table: "DOCUMENT_NUMBERS",
                column: "DepartmentId",
                principalTable: "DEPARTMENTS",
                principalColumn: "DepartmentId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENT_NUMBERS_DOCUMENTS_DocumentId",
                table: "DOCUMENT_NUMBERS",
                column: "DocumentId",
                principalTable: "DOCUMENTS",
                principalColumn: "DocumentId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENT_NUMBERS_DOCUMENT_NUMBER_SCOPES_ScopeId",
                table: "DOCUMENT_NUMBERS",
                column: "ScopeId",
                principalTable: "DOCUMENT_NUMBER_SCOPES",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENT_NUMBERS_DEPARTMENTS_DepartmentId",
                table: "DOCUMENT_NUMBERS");

            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENT_NUMBERS_DOCUMENTS_DocumentId",
                table: "DOCUMENT_NUMBERS");

            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENT_NUMBERS_DOCUMENT_NUMBER_SCOPES_ScopeId",
                table: "DOCUMENT_NUMBERS");

            migrationBuilder.DropTable(
                name: "DOCUMENT_NUMBER_SCOPES");

            migrationBuilder.DropIndex(
                name: "IX_DOC_NUMBERS_DEPT_REG_DATE",
                table: "DOCUMENT_NUMBERS");

            migrationBuilder.DropIndex(
                name: "IX_DOC_NUMBERS_REG_YEAR",
                table: "DOCUMENT_NUMBERS");

            migrationBuilder.DropIndex(
                name: "IX_DOCUMENT_NUMBERS_ScopeId",
                table: "DOCUMENT_NUMBERS");

            migrationBuilder.DropIndex(
                name: "UX_DOC_NUMBERS_DEPT_TEXT_YEAR",
                table: "DOCUMENT_NUMBERS");

            migrationBuilder.DropIndex(
                name: "UX_DOC_NUMBERS_DOC_DEPT",
                table: "DOCUMENT_NUMBERS");

            migrationBuilder.DropIndex(
                name: "UX_DOC_NUMBERS_REG_YEAR_SEQ",
                table: "DOCUMENT_NUMBERS");

            migrationBuilder.DropIndex(
                name: "UX_DOC_NUMBERS_TEXT_YEAR",
                table: "DOCUMENT_NUMBERS");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "DOCUMENT_NUMBERS");

            migrationBuilder.DropColumn(
                name: "RegistryDate",
                table: "DOCUMENT_NUMBERS");

            migrationBuilder.DropColumn(
                name: "ScopeId",
                table: "DOCUMENT_NUMBERS");

            migrationBuilder.DropColumn(
                name: "EffectiveDate",
                table: "DOCUMENT_NUMBER_HISTORY");

            migrationBuilder.DropColumn(
                name: "RegistryNumberId",
                table: "DOCUMENT_MOVEMENT_STEPS");

            migrationBuilder.CreateIndex(
                name: "UX_DOC_NUMBERS_REG_YEAR_SEQ",
                table: "DOCUMENT_NUMBERS",
                columns: new[] { "RegistryId", "NumberingYear", "SequenceValue" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_DOC_NUMBERS_TEXT_YEAR",
                table: "DOCUMENT_NUMBERS",
                columns: new[] { "OfficialNumberText", "NumberingYear" },
                unique: true);
        }
    }
}
