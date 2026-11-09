using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstitutionalDiwan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_Document_DisplayName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "DOCUMENTS",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "DOCUMENTS",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            // Existing documents take the same name a new one would: type and owning department's first
            // register number, or type and creation date when they have none.
            migrationBuilder.Sql(@"UPDATE d SET DisplayName = LEFT(t.DescAr + N' · ' + COALESCE(
                (SELECT TOP 1 n.OfficialNumberText FROM DOCUMENT_NUMBERS n
                 WHERE n.DocumentId = d.DocumentId AND n.ScopeId = 2 AND n.DepartmentId = d.OwningDepartmentId
                 ORDER BY n.NumberingYear),
                CONVERT(char(10), d.CreatedAt, 23)), 300)
                FROM DOCUMENTS d JOIN DOCUMENT_TYPES t ON t.Id = d.DocumentTypeId;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "DOCUMENTS");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "DOCUMENTS",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }
    }
}
