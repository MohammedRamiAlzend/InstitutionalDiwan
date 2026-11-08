using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstitutionalDiwan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_Document_Field_Behavior : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Behavior",
                table: "DOCUMENT_TYPE_SCHEMA_VERSION_FIELDS",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "{\"DefaultValue\":null,\"IsReadOnly\":false,\"IsFilterable\":true,\"ShowInTable\":false,\"PlaceholderAr\":null,\"HelpTextAr\":null,\"MinLength\":null,\"MinDate\":null,\"MaxDate\":null,\"VisibleWhenFieldKey\":null,\"VisibleWhenValue\":null,\"IsHalfWidth\":false,\"SectionTitleAr\":null,\"OptionSource\":0,\"LookupCatalog\":null}");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Behavior",
                table: "DOCUMENT_TYPE_SCHEMA_VERSION_FIELDS");
        }
    }
}
