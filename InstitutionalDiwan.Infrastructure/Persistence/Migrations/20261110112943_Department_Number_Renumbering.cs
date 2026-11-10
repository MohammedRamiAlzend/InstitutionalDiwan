using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstitutionalDiwan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Department_Number_Renumbering : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_DOC_NUMBERS_DOC_DEPT",
                table: "DOCUMENT_NUMBERS");

            migrationBuilder.CreateIndex(
                name: "UX_DOC_NUMBERS_DOC_DEPT",
                table: "DOCUMENT_NUMBERS",
                columns: new[] { "DocumentId", "DepartmentId", "NumberingYear" },
                unique: true,
                filter: "[DepartmentId] IS NOT NULL AND [StatusId] <> 3");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_DOC_NUMBERS_DOC_DEPT",
                table: "DOCUMENT_NUMBERS");

            migrationBuilder.CreateIndex(
                name: "UX_DOC_NUMBERS_DOC_DEPT",
                table: "DOCUMENT_NUMBERS",
                columns: new[] { "DocumentId", "DepartmentId", "NumberingYear" },
                unique: true,
                filter: "[DepartmentId] IS NOT NULL");
        }
    }
}
