using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstitutionalDiwan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Drop_DecisionType_EnglishName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "DECISION_TYPES");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "DECISION_TYPES",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }
    }
}
