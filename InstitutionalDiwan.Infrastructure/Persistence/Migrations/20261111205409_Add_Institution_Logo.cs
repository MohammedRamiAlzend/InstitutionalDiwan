using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstitutionalDiwan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_Institution_Logo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LogoUpdatedAt",
                table: "INSTITUTION_SETTINGS",
                type: "datetime2(3)",
                nullable: true);

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

            // The shell used to print a fixed title; now it prints the stored name. Databases seeded with the
            // old placeholder name keep showing the same title until someone edits it in the settings page.
            migrationBuilder.Sql(
                "UPDATE INSTITUTION_SETTINGS SET InstitutionNameAr = N'الديوان المؤسسي' WHERE InstitutionNameAr = N'المؤسسة'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "INSTITUTION_LOGOS");

            migrationBuilder.DropColumn(
                name: "LogoUpdatedAt",
                table: "INSTITUTION_SETTINGS");
        }
    }
}
