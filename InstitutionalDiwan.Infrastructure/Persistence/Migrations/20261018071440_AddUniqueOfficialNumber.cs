using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstitutionalDiwan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueOfficialNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DOC_NUMBERS_TEXT",
                table: "DOCUMENT_NUMBERS");

            migrationBuilder.CreateIndex(
                name: "UX_DOC_NUMBERS_TEXT_YEAR",
                table: "DOCUMENT_NUMBERS",
                columns: new[] { "OfficialNumberText", "NumberingYear" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_DOC_NUMBERS_TEXT_YEAR",
                table: "DOCUMENT_NUMBERS");

            migrationBuilder.CreateIndex(
                name: "IX_DOC_NUMBERS_TEXT",
                table: "DOCUMENT_NUMBERS",
                column: "OfficialNumberText");
        }
    }
}
